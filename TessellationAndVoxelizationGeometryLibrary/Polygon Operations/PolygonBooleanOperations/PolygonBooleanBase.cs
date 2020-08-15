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
            var intersectionLookup = MakeIntersectionLookupList(interaction, polygonA, polygonB, out var newPolygons);
            while (GetNextStartingIntersection(interaction.IntersectionData, out var startingIntersection,
                out var startEdge, out var switchPolygon))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, interaction.IntersectionData, startingIntersection,
                    startEdge, switchPolygon).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                newPolygons.Add(new Polygon(polyCoordinates));
            }
            // todo: add in duplicate or non-intersecting polygons here
            // for holes that were not participating in any intersection, we need to restore them to the result
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
        protected bool GetNextStartingIntersection(List<PolygonSegmentIntersectionRecord> intersections,
            out PolygonSegmentIntersectionRecord nextStartingIntersection, out PolygonSegment currentEdge, out bool switchPolygon)
        {
            foreach (var intersectionData in intersections)
            {
                if (intersectionData.Relationship == (PolygonSegmentRelationship.BothLinesStartAtPoint | PolygonSegmentRelationship.CoincidentLines |
                    PolygonSegmentRelationship.SameLineAfterPoint | PolygonSegmentRelationship.SameLineBeforePoint))
                    continue;
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

        protected abstract bool ValidStartingIntersection(PolygonSegmentIntersectionRecord intersectionData, out PolygonSegment currentEdge, out bool switchPolygon);



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
            List<PolygonSegmentIntersectionRecord> intersections, PolygonSegmentIntersectionRecord startingIntersection, PolygonSegment startingEdge, bool switchPolygon)
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
                if ((currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) == 0b0)
                 || (!currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) == 0b0))
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
                    newPath.Add(currentEdge.ToPoint.Coordinates);
                    currentEdge = currentEdge.ToPoint.StartLine;
                    intersectionCoordinates = Vector2.Null; // this is set to null because its value is used in ClosestNextIntersectionOnThisEdge
                                                            // when multiple intersections cross the edge. If we got through the first pass then there are no previous intersections on 
                                                            // the edge that concern us. We want that function to report the first one for the edge
                }
            } while (currentEdge != startingEdge && intersectionData != startingIntersection);
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
        private bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<PolygonSegmentIntersectionRecord> allIntersections,
        Vector2 formerIntersectCoords, out PolygonSegmentIntersectionRecord newIntersection, out bool currentEdgeIsFromPolygonA,
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

        protected virtual bool SwitchAtThisIntersection(PolygonSegmentIntersectionRecord newIntersection, bool currentEdgeIsFromPolygonA)
        {
            // if the intersection is a point that both share but the lines are the same (and in same direction)
            return newIntersection.Relationship != (PolygonSegmentRelationship.BothLinesStartAtPoint
                | PolygonSegmentRelationship.CoincidentLines | PolygonSegmentRelationship.SameLineAfterPoint
                | PolygonSegmentRelationship.SameLineBeforePoint);
        }

        /// <summary>
        /// Makes the intersection lookup table that allows us to quickly find the intersections for a given edge.
        /// </summary>
        /// <param name="numLines">The number lines.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>System.Collections.Generic.List&lt;System.Int32&gt;[].</returns>
        internal List<int>[] MakeIntersectionLookupList(PolygonInteractionRecord interaction, Polygon polygonA,
            Polygon polygonB, out List<Polygon> polygonList)
        {
            // first off, number all the vertices with a unique index between 0 and n. These are used in the lookupList to connect the 
            // edges to the intersections that they participate in.
            var index = 0;
            var polygonStartIndices = new List<int>();
            // in addition, keep track of the vertex index that is the beginning of each polygon. Recall that there could be numerous
            // hole-polygons that need to be accounted for.
            var allPolygons = polygonA.AllPolygons.ToList();
            foreach (var polygon in allPolygons)
            {
                polygonStartIndices.Add(index);
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = index++;
            }
            var startOfBVertices = index; //yeah, also keep track of when the second polygon tree argument starts
            if (polygonB != null)
                foreach (var polygon in polygonB.AllPolygons)
                {
                    allPolygons.Add(polygon);
                    polygonStartIndices.Add(index);
                    foreach (var vertex in polygon.Vertices)
                        vertex.IndexInList = index++;
                }
            polygonStartIndices.Add(index); // add a final exclusive top of the range for the for-loop below (not the next one, the one after)

            var nonIntersectionPolygonIndices = Enumerable.Range(0, polygonStartIndices.Count);

            for (int i = 0; i < interaction.num; i++)
            {

            }
            // now make the lookupList. One list per vertex. If the vertex does not intersect, then it is left as null.
            // this is potentially memory intensive but speeds up the matching  when creating new polygons
            var lookupList = new List<int>[index];
            for (int i = 0; i < interaction.IntersectionData.Count; i++)
            {
                var intersection = interaction.IntersectionData[i];
                intersection.VisitedA = false;
                intersection.VisitedB = false;
                index = intersection.EdgeA.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
            }

            polygonList = new List<Polygon>();
            // now we want to find the sub-polygons that are not intersecting anything and decide whether to keep them or not
            index = 0;
            foreach (var poly in allPolygons)
            {
                if (isIdentical)
                {   // go back through the same indices and remove references to the intersections. Also, set the intersections to "visited"
                    // which is easier than deleting since the other references would collapse down
                    for (int j = polygonStartIndices[index]; j < polygonStartIndices[index + 1]; j++)
                    {
                        var intersectionIndex = lookupList[j].First(k => (interaction.IntersectionData[k].Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0
                     && (interaction.IntersectionData[k].Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0);
                        lookupList[j].Remove(intersectionIndex);
                        if (lookupList[j].Count == 0) lookupList[j] = null;
                        interaction.IntersectionData[intersectionIndex].VisitedA = true;
                        interaction.IntersectionData[intersectionIndex].VisitedB = true;
                        // note, in the next line - this has to be EdgeB since searching in order the A polygon will detect the duplicate - B will skip over
                        var otherLookupEntry = lookupList[interaction.IntersectionData[intersectionIndex].EdgeB.IndexInList];
                        otherLookupEntry.Remove(intersectionIndex);
                        //if (otherLookupEntry.Count == 0) lookupList[intersections[intersectionIndex].EdgeB.IndexInList] = null;
                        // hmm, I commented the previous line for good reason but I do not recall why. It would seem (for symmetry sake) it should be happen, but no
                    }
                    HandleIdenticalPolygons(poly, polygonList, identicalPolygonIsInverted);
                }

                else if (isNonIntersecting)
                    HandleNonIntersectingSubPolygon(poly, polygonA, polygonB, polygonList, polygonStartIndices[index] >= startOfBVertices);
                index++;
            }
            return lookupList;
        }

        protected abstract void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB,
            List<Polygon> newPolygons, bool partOfPolygonB);


        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected abstract void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons,
                    bool identicalPolygonIsInverted);
        #endregion
    }
}