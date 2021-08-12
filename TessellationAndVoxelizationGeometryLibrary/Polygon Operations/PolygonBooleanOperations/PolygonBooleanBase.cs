// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal abstract class PolygonBooleanBase
    {
        #region Private Functions used by the above public methods

        /// <summary>
        /// All of the previous boolean operations are accomplished by this function. Note that the function RemoveSelfIntersections is also
        /// very simliar to this function.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="isSubtract">The switch direction.</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        internal List<Polygon> Run(Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction, PolygonCollection polygonCollection,
            double minimumArea = double.NaN)
        {
            if (double.IsNaN(minimumArea))
                minimumArea = (Math.Abs(polygonA.PathArea) + Math.Abs(polygonB.PathArea))
                    * Math.Pow(10, -(Math.Max(polygonA.NumSigDigits, polygonB.NumSigDigits)));

            var delimiters = NumberVerticesAndGetPolygonVertexDelimiter(polygonA);
            delimiters = NumberVerticesAndGetPolygonVertexDelimiter(polygonB, delimiters[^1]);
            var intersectionLookup = interaction.MakeIntersectionLookupList(delimiters[^1]);
            var newPolygons = new List<Polygon>();
            var indexIntersectionStart = 0;
            var polygonIndex = 0;
            while (GetNextStartingIntersection(interaction.IntersectionData, out var startingIntersection,
                out var startEdge, out var switchPolygon, ref indexIntersectionStart))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, interaction.IntersectionData, startingIntersection,
                    startEdge, switchPolygon, out _).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minimumArea)) continue;
                newPolygons.Add(new Polygon(polyCoordinates.SimplifyFastDestructiveList(), polygonIndex++));
            }
            // to handle the non-intersecting subpolygons
            var nonIntersectingASubPolygons = new List<Polygon>(polygonA.AllPolygons);
            var nonIntersectingBSubPolygons = new List<Polygon>(polygonB.AllPolygons);
            foreach (var polyA in polygonA.AllPolygons)
                foreach (var polyB in polygonB.AllPolygons)
                {
                    var rel = interaction.GetRelationshipBetween(polyA, polyB);
                    if ((rel & PolyRelInternal.Equal) != 0b0)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                        HandleIdenticalPolygons(polyA, newPolygons, false);
                    }
                    else if ((rel & PolyRelInternal.EqualButOpposite) != 0b0)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                        HandleIdenticalPolygons(polyA, newPolygons, true);
                    }
                    else if ((rel & (PolyRelInternal.EdgesCross | PolyRelInternal.CoincidentEdges
                        | PolyRelInternal.CoincidentVertices)) != 0b0)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                    }
                }
            foreach (var poly in nonIntersectingASubPolygons)
                if (HandleNonIntersectingSubPolygon(poly, newPolygons, interaction.GetRelationships(poly), false))
                    newPolygons.Add(poly.Copy(false, false));
            foreach (var poly in nonIntersectingBSubPolygons)
                if (HandleNonIntersectingSubPolygon(poly, newPolygons, interaction.GetRelationships(poly), true))
                    newPolygons.Add(poly.Copy(false, false));
            newPolygons.RemoveAll(p => p.Vertices.Count <= 2);
            switch (polygonCollection)
            {
                case PolygonCollection.SeparateLoops:
                    return newPolygons;

                case PolygonCollection.PolygonWithHoles:
                    return newPolygons.CreateShallowPolygonTrees(true);

                default:
                    return newPolygons.CreatePolygonTree(true);
            }
        }

        /// <summary>
        /// Gets the next intersection by looking through the intersectionLookupList. It'll return false, when there are none left.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="nextStartingIntersection">The next starting intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <returns><c>true</c> if a new starting intersection was found, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected bool GetNextStartingIntersection(List<SegmentIntersection> intersections,
            out SegmentIntersection nextStartingIntersection, out PolygonEdge currentEdge, out bool switchPolygon,
            ref int indexIntersectionStart)
        {
            if (intersections != null && intersections.Count > 0)
            {
                for (int i = indexIntersectionStart; i < intersections.Count; i++)
                {
                    SegmentIntersection intersectionData = intersections[i];
                    if (ValidStartingIntersection(intersectionData, out currentEdge, out bool startAgain))
                    {
                        if (startAgain) indexIntersectionStart = i;
                        else indexIntersectionStart = i + 1;
                        switchPolygon = SwitchAtThisIntersection(intersectionData, currentEdge == intersectionData.EdgeA);
                        nextStartingIntersection = intersectionData;
                        return true;
                    }
                }
            }
            switchPolygon = false;
            currentEdge = null;
            nextStartingIntersection = null;
            return false;
        }

        internal static List<int> NumberVerticesAndGetPolygonVertexDelimiter(Polygon polygon, int startIndex = 0)
        {
            var polygonStartIndices = new List<int>();
            // in addition, keep track of the vertex index that is the beginning of each polygon. Recall that there could be numerous
            // hole-polygons that need to be accounted for.
            var index = startIndex;
            foreach (var poly in polygon.AllPolygons)
            {
                polygonStartIndices.Add(index);
                foreach (var vertex in poly.Vertices)
                    vertex.IndexInList = index++;
            }
            polygonStartIndices.Add(index); // add a final exclusive top of the range for the for-loop below (not the next one, the one after)
            return polygonStartIndices;
        }

        protected abstract bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonEdge currentEdge,
            out bool startAgain);

        /// <summary>
        /// Makes the polygon through intersections. This is actually the heart of the matter here. The method is the main
        /// while loop that switches between segments everytime a new intersection is encountered. It is universal to all
        /// the boolean operations
        /// </summary>
        /// <param name="intersectionLookup">The readonly intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="isSubtract">if set to <c>true</c> [switch directions].</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup, List<SegmentIntersection> intersections,
            SegmentIntersection startingIntersection, PolygonEdge startingEdge, bool switchPolygon,
            out bool includesWrongPoints, List<bool> knownWrongPoints = null)

        {
            var maxNumPoints = knownWrongPoints != null ? knownWrongPoints.Count + intersections.Count : int.MaxValue;
            bool overMaxPoints = false;
            //Debug.WriteLine("starting MakePolygonThroughIntersections in" + this.GetType().ToString());
            bool? completed = null;
            includesWrongPoints = false;
            var newPath = new List<Vector2>();
            var intersectionData = startingIntersection;
            var currentEdge = startingEdge;
            do
            {
                if (currentEdge == intersectionData.EdgeA) intersectionData.VisitedA = true;
                else intersectionData.VisitedB = true;
                if (newPath.Count == 0 || newPath[^1] != intersectionData.IntersectCoordinates)
                    newPath.Add(intersectionData.IntersectCoordinates);
                if (switchPolygon)
                    currentEdge = (currentEdge == intersectionData.EdgeB) ? intersectionData.EdgeA : intersectionData.EdgeB;

                // the following while loop adds all the points along the subpath until the next intersection is encountered
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections, ref intersectionData, out switchPolygon))
                // when this returns true (a valid intersection is found - even if previously visited), then we break
                // out of the loop. The intersection is identified here, but processed above
                {
                    if (knownWrongPoints != null && knownWrongPoints[currentEdge.ToPoint.IndexInList]) includesWrongPoints = true;
                    currentEdge = currentEdge.ToPoint.StartLine;
                    newPath.Add(currentEdge.FromPoint.Coordinates);
//#if PRESENT
//                    Presenter.ShowAndHang(newPath);
//#endif
                }
                if (newPath.Count >= maxNumPoints)
                {
                    overMaxPoints = true;
                    completed = null;
                }
            } while (!overMaxPoints && false == (completed = PolygonCompleted(intersectionData, startingIntersection, currentEdge, startingEdge)));
//#if PRESENT
//            Presenter.ShowAndHang(newPath);
//#endif
            if (completed == null) newPath.Clear();
            //Debug.WriteLine("    .... result has {0} vertices.", newPath.Count);
            return newPath;
        }

        /// <summary>
        /// This is invoked by the previous function, . It is possible that there are multiple intersections crossing the currentEdge. Based on the
        /// direction (forward?), the next closest one is identified.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="allIntersections">All intersections.</param>
        /// <param name="formerIntersectCoords">The former intersect coords.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <param name="bestIntersection">The index of intersection.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonEdge currentEdge, List<SegmentIntersection> allIntersections,
       ref SegmentIntersection formerIntersect, out bool switchPolygon)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            SegmentIntersection bestIntersection = null;
            if (intersectionIndices == null)
            {
                formerIntersect = null;
                switchPolygon = false;
                return false;
            }
            var minDistanceToIntersection = double.PositiveInfinity;
            var datum = formerIntersect != null ? formerIntersect.IntersectCoordinates : currentEdge.FromPoint.Coordinates;
            foreach (var index in intersectionIndices)
            {
                var candidateIntersect = allIntersections[index];
                if (formerIntersect == candidateIntersect) continue;
                var currentEdgeIsFromPolygonA = candidateIntersect.EdgeA == currentEdge;
                double distance;
                // only calculate the distance if 
                if (formerIntersect == null && (candidateIntersect.WhereIntersection == WhereIsIntersection.BothStarts ||
                    (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfA && currentEdgeIsFromPolygonA) ||
                    (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfB && !currentEdgeIsFromPolygonA)))
                    distance = 0.0;
                else
                // this is always true?
                //if (formerIntersect != null || candidateIntersect.WhereIntersection == WhereIsIntersection.Intermediate ||
                //     (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfA && !currentEdgeIsFromPolygonA) ||
                //     (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfB && currentEdgeIsFromPolygonA))
                {
                    distance = currentEdge.Vector.Dot(candidateIntersect.IntersectCoordinates - datum);
                    if (distance < 0
                        )
                        // this is now handled below in "else if (minDistanceToIntersection == distance)"
                        //|| (distance.IsNegligible() &&
                        //((candidateIntersect.VisitedA && currentEdgeIsFromPolygonA)
                        //|| (candidateIntersect.VisitedB && !currentEdgeIsFromPolygonA))))
                        continue;
                }
                //else continue;
                if (minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    bestIntersection = candidateIntersect;
                }
                else if (minDistanceToIntersection == distance)
                {   // this is super rare and likely only to happen in RemoveSelfIntersections
                    // basically we are going to choose the line that makes the sharpest (smallest) left turn (convex turn)
                    // into the polygon
                    var bestEdge = bestIntersection.EdgeA == currentEdge ? bestIntersection.EdgeB : bestIntersection.EdgeA;
                    var newCandidateEdge = candidateIntersect.EdgeA == currentEdge ? candidateIntersect.EdgeB : candidateIntersect.EdgeA;
                    var bestAngle = currentEdge.Vector.SmallerAngleBetweenVectors(bestEdge.Vector);
                    var newCandidateAngle = currentEdge.Vector.SmallerAngleBetweenVectors(newCandidateEdge.Vector);
                    if (newCandidateAngle < bestAngle) bestIntersection = candidateIntersect;
                    if (newCandidateAngle == bestAngle)
                    {   // really?! if you are here than not only are there two segments that pass through currentEdge at the same
                        // point, but they do so at the same angle! So, we are going to choose the one that is shorter
                        var bestRemainingLength = (bestEdge.ToPoint.Coordinates - bestIntersection.IntersectCoordinates).LengthSquared();
                        var newCandRemainingLength = (newCandidateEdge.ToPoint.Coordinates - candidateIntersect.IntersectCoordinates).LengthSquared();
                        if (newCandRemainingLength < bestRemainingLength) bestIntersection = candidateIntersect;
                    }
                }
            }
            if (bestIntersection != null)
            {
                switchPolygon = SwitchAtThisIntersection(bestIntersection, bestIntersection.EdgeA == currentEdge);
                formerIntersect = bestIntersection;
                return true;
            }
            else
            {
                switchPolygon = false;
                formerIntersect = null;
                return false;
            }
        }

        protected abstract bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA);

        protected abstract bool? PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection,
           PolygonEdge currentEdge, PolygonEdge startingEdge);

        protected abstract bool HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB);

        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected abstract void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool equalAndOpposite);

        #endregion Private Functions used by the above public methods
    }
}