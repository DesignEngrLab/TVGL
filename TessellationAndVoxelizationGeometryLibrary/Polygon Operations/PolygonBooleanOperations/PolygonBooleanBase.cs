// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonBooleanBase.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
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
        /// <param name="interaction">The interaction.</param>
        /// <param name="polygonCollection">The polygon collection.</param>
        /// <param name="minimumArea">The minimum area.</param>
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
                newPolygons.Add(new Polygon(polyCoordinates.RemoveCollinearEdgesDestructiveList(), polygonIndex++));
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
        /// <param name="intersections">The intersections.</param>
        /// <param name="nextStartingIntersection">The next starting intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="switchPolygon">if set to <c>true</c> [switch polygon].</param>
        /// <param name="indexIntersectionStart">The index intersection start.</param>
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

        /// <summary>
        /// Numbers the vertices and get polygon vertex delimiter.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="startIndex">The start index.</param>
        /// <returns>List&lt;System.Int32&gt;.</returns>
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

        /// <summary>
        /// Valids the starting intersection.
        /// </summary>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="startAgain">if set to <c>true</c> [start again].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected abstract bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonEdge currentEdge,
            out bool startAgain);

        /// <summary>
        /// Makes the polygon through intersections. This is actually the heart of the matter here. The method is the main
        /// while loop that switches between segments everytime a new intersection is encountered. It is universal to all
        /// the boolean operations
        /// </summary>
        /// <param name="intersectionLookup">The readonly intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="startingIntersection">The starting intersection.</param>
        /// <param name="startingEdge">The starting edge.</param>
        /// <param name="switchPolygon">if set to <c>true</c> [switch polygon].</param>
        /// <param name="includesWrongPoints">if set to <c>true</c> [includes wrong points].</param>
        /// <param name="knownWrongPoints">The known wrong points.</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="NotImplementedException"></exception>
        protected List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup, List<SegmentIntersection> intersections,
            SegmentIntersection startingIntersection, PolygonEdge startingEdge, bool switchPolygon,
            out bool includesWrongPoints, List<bool> knownWrongPoints = null)
        {
            bool? completed;
            includesWrongPoints = false;
            var newPath = new List<Vector2>();
            var intersectionData = startingIntersection;
            var currentEdge = startingEdge;
            do
            {
                if (currentEdge == intersectionData.EdgeA) intersectionData.VisitedA = true;
                else intersectionData.VisitedB = true;
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                //if (newPathHash.Contains(intersectionCoordinates)) break;
                // there used to be some complex conditions here (at 12 lines down before the other newPath.Add(...)
                // to ensure that the added point wasn't the same as the last. However, for speed, we allow it
                // and add the check in the Polygon constructor. This also reduces code since sometimes the Vector2's
                // sent to that constructor would have duplicate points.
                newPath.Add(intersectionCoordinates);
                //newPathHash.Add(intersectionCoordinates);
                if (switchPolygon)
                    currentEdge = (currentEdge == intersectionData.EdgeB) ? intersectionData.EdgeA : intersectionData.EdgeB;

                // the following while loop add all the points along the subpath until the next intersection is encountered
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                        intersectionCoordinates, out intersectionData, out switchPolygon))
                // when this returns true (a valid intersection is found - even if previously visited), then we break
                // out of the loop. The intersection is identified here, but processed above
                {
                    currentEdge = currentEdge.ToPoint.StartLine;
                    if (knownWrongPoints != null && knownWrongPoints[currentEdge.FromPoint.IndexInList]) 
                        includesWrongPoints = true;
                    newPath.Add(currentEdge.FromPoint.Coordinates);
                    //newPathHash.Add(currentEdge.FromPoint.Coordinates);
                    intersectionCoordinates = Vector2.Null; // this is set to null because its value is used in ClosestNextIntersectionOnThisEdge
                                                            // when multiple intersections cross the edge. If we got through the first pass then there are no previous intersections on
                                                            // the edge that concern us. We want that function to report the first one for the edge
//#if PRESENT
                    Presenter.ShowAndHang(newPath);
//#endif
                }
            } while (false == (completed = PolygonCompleted(intersectionData, startingIntersection, currentEdge, startingEdge)));
            //#if PRESENT
            //            Presenter.ShowAndHang(newPath);
            //#endif
            if (completed == null) newPath.Clear();
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
        /// <param name="bestIntersection">The index of intersection.</param>
        /// <param name="switchPolygon">if set to <c>true</c> [switch polygon].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonEdge currentEdge, List<SegmentIntersection> allIntersections,
        Vector2 formerIntersectCoords, out SegmentIntersection bestIntersection, out bool switchPolygon)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            bestIntersection = null;
            if (intersectionIndices == null)
            {
                switchPolygon = false;
                return false;
            }
            var minDistanceToIntersection = double.PositiveInfinity;
            var datum = !formerIntersectCoords.IsNull() ? formerIntersectCoords : currentEdge.FromPoint.Coordinates;
            foreach (var index in intersectionIndices)
            {
                var candidateIntersect = allIntersections[index];
                var currentEdgeIsFromPolygonA = candidateIntersect.EdgeA == currentEdge;
                if (formerIntersectCoords.Equals(candidateIntersect.IntersectCoordinates)) continue;
                var distance = 0.0;
                if (!(formerIntersectCoords.IsNull() && (candidateIntersect.WhereIntersection == WhereIsIntersection.BothStarts ||
                    (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfA && currentEdgeIsFromPolygonA) ||
                    (candidateIntersect.WhereIntersection == WhereIsIntersection.AtStartOfB && !currentEdgeIsFromPolygonA))))
                    distance = currentEdge.Vector.Dot(candidateIntersect.IntersectCoordinates - datum);
                if (distance < 0) continue;
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
                    var bestAngle = currentEdge.Vector.SmallerAngleBetweenVectorsEndToEnd(bestEdge.Vector);
                    var newCandidateAngle = currentEdge.Vector.SmallerAngleBetweenVectorsEndToEnd(newCandidateEdge.Vector);
                    if (newCandidateAngle > bestAngle) bestIntersection = candidateIntersect;
                    if (newCandidateAngle == bestAngle)
                    {   // really?! if you are here than not only are there two segments that pass through currentEdge at the same
                        // point, but the do so at the same angle! So, we are going to choose the one that is shorter
                        var bestRemainingLength = (bestEdge.ToPoint.Coordinates - bestIntersection.IntersectCoordinates).LengthSquared();
                        var newCandRemainingLength = (newCandidateEdge.ToPoint.Coordinates - candidateIntersect.IntersectCoordinates).LengthSquared();
                        if (newCandRemainingLength < bestRemainingLength) bestIntersection = candidateIntersect;
                    }
                }
            }
            if (bestIntersection != null)
            {
                switchPolygon = SwitchAtThisIntersection(bestIntersection, bestIntersection.EdgeA == currentEdge);
                return true;
            }
            else
            {
                switchPolygon = false;
                return false;
            }
        }

        /// <summary>
        /// Switches at this intersection.
        /// </summary>
        /// <param name="newIntersection">The new intersection.</param>
        /// <param name="currentEdgeIsFromPolygonA">if set to <c>true</c> [current edge is from polygon a].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected abstract bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA);

        /// <summary>
        /// Polygons the completed.
        /// </summary>
        /// <param name="currentIntersection">The current intersection.</param>
        /// <param name="startingIntersection">The starting intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="startingEdge">The starting edge.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected abstract bool PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection,
           PolygonEdge currentEdge, PolygonEdge startingEdge);

        /// <summary>
        /// Handles the non intersecting sub polygon.
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="newPolygons">The new polygons.</param>
        /// <param name="relationships">The relationships.</param>
        /// <param name="partOfPolygonB">if set to <c>true</c> [part of polygon b].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected abstract bool HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB);

        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygonA">The sub polygon a.</param>
        /// <param name="newPolygons">The new polygons.</param>
        /// <param name="equalAndOpposite">if set to <c>true</c> [equal and opposite].</param>
        protected abstract void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool equalAndOpposite);

        #endregion Private Functions used by the above public methods
    }
}