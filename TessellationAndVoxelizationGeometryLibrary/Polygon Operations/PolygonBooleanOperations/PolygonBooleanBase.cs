using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        internal List<Polygon> Run(Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction, double minAllowableArea)
        {
            var delimiters = PolygonOperations.NumberVertiesAndGetPolygonVertexDelimiter(polygonA);
            delimiters = PolygonOperations.NumberVertiesAndGetPolygonVertexDelimiter(polygonB, delimiters[^1]);
            var intersectionLookup = interaction.MakeIntersectionLookupList(delimiters[^1]);
            var newPolygons = new List<Polygon>();
            while (GetNextStartingIntersection(interaction.IntersectionData, out var startingIntersection,
                out var startEdge, out var switchPolygon))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, interaction.IntersectionData, startingIntersection,
                    startEdge, switchPolygon).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                newPolygons.Add(new Polygon(polyCoordinates));
            }
            // to handle the non-intersecting subpolygons
            var nonIntersectingASubPolygons = new List<Polygon>(polygonA.AllPolygons);
            var nonIntersectingBSubPolygons = new List<Polygon>(polygonB.AllPolygons);
            foreach (var polyA in polygonA.AllPolygons)
                foreach (var polyB in polygonB.AllPolygons)
                {
                    var rel = interaction.GetRelationshipBetween(polyA, polyB);
                    if ((rel & (PolygonRelationship.EdgesCross | PolygonRelationship.CoincidentEdges
                        | PolygonRelationship.CoincidentVertices)) != 0b0)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                    }
                    else if (rel == PolygonRelationship.Equal)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                        HandleIdenticalPolygons(polyA, newPolygons, false);
                    }
                    else if (rel == PolygonRelationship.EqualButOpposite)
                    {
                        nonIntersectingASubPolygons.Remove(polyA);
                        nonIntersectingBSubPolygons.Remove(polyB);
                        HandleIdenticalPolygons(polyA, newPolygons, true);
                    }
                }
            foreach (var poly in nonIntersectingASubPolygons)
                HandleNonIntersectingSubPolygon(poly, newPolygons, interaction.GetRelationships(poly), false);
            foreach (var poly in nonIntersectingBSubPolygons)
                HandleNonIntersectingSubPolygon(poly, newPolygons, interaction.GetRelationships(poly), true);

            return newPolygons.CreateShallowPolygonTrees(true, out _, out _);
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
            out SegmentIntersection nextStartingIntersection, out PolygonSegment currentEdge, out bool switchPolygon)
        {
            foreach (var intersectionData in intersections)
            {
                if (ValidStartingIntersection(intersectionData, out currentEdge, out switchPolygon))
                {
                    nextStartingIntersection = intersectionData;
                    return true;
                }
            }
            switchPolygon = false;
            currentEdge = null;
            nextStartingIntersection = null;
            return false;
        }

        protected abstract bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonSegment currentEdge, out bool switchPolygon);



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
        protected List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup,
            List<SegmentIntersection> intersections, SegmentIntersection startingIntersection, PolygonSegment startingEdge, bool switchPolygon)
        {
            var newPath = new List<Vector2>();
            var intersectionData = startingIntersection;
            var currentEdge = startingEdge;
            var currentEdgeIsFromPolygonA = currentEdge == intersectionData.EdgeA;
            do
            {
                if (currentEdgeIsFromPolygonA)
                {
                    if (intersectionData.VisitedA) break;
                    intersectionData.VisitedA = true;
                }
                else
                {
                    if (intersectionData.VisitedB) break;
                    intersectionData.VisitedB = true;
                }
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                // only add the point to the path if it wasn't added below in the while loop. i.e. it is an intermediate point to the 
                // current polygon edge
                if (newPath.Count == 0 || (currentEdgeIsFromPolygonA && (intersectionData.WhereIntersection == WhereIsIntersection.Intermediate ||
                    intersectionData.WhereIntersection == WhereIsIntersection.AtStartOfB))
                 || (!currentEdgeIsFromPolygonA && (intersectionData.WhereIntersection == WhereIsIntersection.AtStartOfA ||
                 intersectionData.WhereIntersection == WhereIsIntersection.Intermediate))
                 || !newPath[^1].IsPracticallySame(intersectionCoordinates))
                    newPath.Add(intersectionCoordinates);
                if (switchPolygon)
                    currentEdgeIsFromPolygonA = !currentEdgeIsFromPolygonA;
                currentEdge = currentEdgeIsFromPolygonA ? intersectionData.EdgeA : intersectionData.EdgeB;

                // the following while loop add all the points along the subpath until the next intersection is encountered
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                        intersectionCoordinates, out intersectionData, out currentEdgeIsFromPolygonA, out switchPolygon))
                // when this returns true (a valid intersection is found - even if previously visited), then we break
                // out of the loop. The intersection is identified here, but processed above
                {
                    currentEdge = currentEdge.ToPoint.StartLine;
                    if ((currentEdge != startingIntersection.EdgeA || startingIntersection.WhereIntersection == WhereIsIntersection.AtStartOfB ||
                        intersectionData.WhereIntersection == WhereIsIntersection.Intermediate) &&
                        (currentEdge != startingIntersection.EdgeB || startingIntersection.WhereIntersection == WhereIsIntersection.AtStartOfA ||
                        intersectionData.WhereIntersection == WhereIsIntersection.Intermediate))
                        newPath.Add(currentEdge.FromPoint.Coordinates);
                    intersectionCoordinates = Vector2.Null; // this is set to null because its value is used in ClosestNextIntersectionOnThisEdge
                                                            // when multiple intersections cross the edge. If we got through the first pass then there are no previous intersections on 
                                                            // the edge that concern us. We want that function to report the first one for the edge
                }
            } while (intersectionData != startingIntersection);
            //} while (currentEdge != startingEdge || intersectionData != startingIntersection);
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
        /// <param name="newIntersection">The index of intersection.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<SegmentIntersection> allIntersections,
        Vector2 formerIntersectCoords, out SegmentIntersection newIntersection, out bool currentEdgeIsFromPolygonA,
        out bool switchPolygon)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            newIntersection = null;
            if (intersectionIndices == null)
            {
                switchPolygon = currentEdgeIsFromPolygonA = false;
                return false;
            }
            var minDistanceToIntersection = double.PositiveInfinity;
            var datum = !formerIntersectCoords.IsNull() ? formerIntersectCoords : currentEdge.FromPoint.Coordinates;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                if (formerIntersectCoords.Equals(thisIntersectData.IntersectCoordinates)) continue;
                var distance = currentEdge.Vector.Dot(thisIntersectData.IntersectCoordinates - datum);
                if (distance < 0) continue;
                if (minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    newIntersection = thisIntersectData;
                }
                else if (minDistanceToIntersection == distance)
                    ;
            }
            if (newIntersection != null)
            {
                currentEdgeIsFromPolygonA = newIntersection.EdgeA == currentEdge;
                switchPolygon = SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA);
                return true;
            }
            else
            {
                switchPolygon = currentEdgeIsFromPolygonA = false;
                return false;
            }
        }

        protected abstract bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA);


        protected abstract void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolygonRelationship, bool)> relationships, bool partOfPolygonB);


        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected abstract void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool equalAndOpposite);
        #endregion
    }
}