using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal abstract class PolygonBooleanBase
    {
        protected readonly bool isSubtract;
        protected PolygonBooleanBase(bool isSubtract)
        {
            this.isSubtract= isSubtract;
        }

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
        internal List<Polygon> Run(Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, double minAllowableArea)
        {
            var intersectionLookup = MakeIntersectionLookupList(intersections, polygonA, polygonB, out var positivePolygons,
                out var negativePolygons); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersections, out var startingIntersection,
                out var startEdge, out var switchPolygon))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, switchPolygon).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates));
                else positivePolygons.Add(area, new Polygon(polyCoordinates));
            }
            // for holes that were not participating in any intersection, we need to restore them to the result
            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values,
                out _);
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
        private bool GetNextStartingIntersection(List<IntersectionData> intersections,
            out IntersectionData nextStartingIntersection, out PolygonSegment currentEdge, out bool switchPolygon)
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

        protected abstract bool ValidStartingIntersection(IntersectionData intersectionData, out PolygonSegment currentEdge, out bool switchPolygon);



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
        private List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup,
            List<IntersectionData> intersections, IntersectionData startingIntersection, PolygonSegment startingEdge, bool switchPolygon)
        {
            var newPath = new List<Vector2>();
            var intersectionData = startingIntersection;
            var currentEdge = startingEdge;
            var forward = true; // as in following the edges in the forward direction (from...to). If false, then traverse backwards
            var currentEdgeIsFromPolygonA = currentEdge == intersectionData.EdgeA;
            do
            {
                if (currentEdgeIsFromPolygonA) intersectionData.VisitedA = true;
                else intersectionData.VisitedB = true;
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                // only add the point to the path if it wasn't added below in the while loop. i.e. it is an intermediate point to the 
                // current polygon edge
                if (!forward || (currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) == 0b0)
                 || (!currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) == 0b0))
                    newPath.Add(intersectionCoordinates);
                if (switchPolygon)
                    currentEdgeIsFromPolygonA = !currentEdgeIsFromPolygonA;
                currentEdge = currentEdgeIsFromPolygonA ? intersectionData.EdgeA : intersectionData.EdgeB;
                if (isSubtract) forward = !forward;
                if (!forward && ((currentEdgeIsFromPolygonA &&
                                  (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0) ||
                                 (!currentEdgeIsFromPolygonA &&
                                  (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)))
                    currentEdge = currentEdge.FromPoint.EndLine;

                // the following while loop add all the points along the subpath until the next intersection is encountered
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                        intersectionCoordinates, forward, out intersectionData, out currentEdgeIsFromPolygonA, out switchPolygon))
                // when this returns true (a valid intersection is found - even if previously visited), then we break
                // out of the loop. The intersection is identified here, but processed above
                {
                    if (forward)
                    {
                        newPath.Add(currentEdge.ToPoint.Coordinates);
                        currentEdge = currentEdge.ToPoint.StartLine;
                    }
                    else
                    {
                        newPath.Add(currentEdge.FromPoint.Coordinates);
                        currentEdge = currentEdge.FromPoint.EndLine;
                    }
                    intersectionCoordinates = Vector2.Null; // this is set to null because its value is used in ClosestNextIntersectionOnThisEdge
                                                            // when multiple intersections cross the edge. If we got through the first pass then there are no previous intersections on 
                                                            // the edge that concern us. We want that function to report the first one for the edge
                }
            } while ((currentEdge != startingEdge || !isSubtract) && intersectionData != startingIntersection);
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
        private bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<IntersectionData> allIntersections,
        Vector2 formerIntersectCoords, bool forward, out IntersectionData newIntersection, out bool currentEdgeIsFromPolygonA,
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
            var vector = forward ? currentEdge.Vector : -currentEdge.Vector;
            var datum = !formerIntersectCoords.IsNull() ? formerIntersectCoords :
                forward ? currentEdge.FromPoint.Coordinates : currentEdge.ToPoint.Coordinates;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                if (formerIntersectCoords.Equals(thisIntersectData.IntersectCoordinates)) continue;

                var distance = vector.Dot(thisIntersectData.IntersectCoordinates - datum);
                if (distance < 0) continue;
                if (minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    newIntersection = thisIntersectData;
                }
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

        protected virtual bool SwitchAtThisIntersection(IntersectionData newIntersection, bool currentEdgeIsFromPolygonA)
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
        internal List<int>[] MakeIntersectionLookupList(List<IntersectionData> intersections, Polygon polygonA,
            Polygon polygonB, out SortedDictionary<double, Polygon> positivePolygons, out SortedDictionary<double, Polygon> negativePolygons)
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

            // now make the lookupList. One list per vertex. If the vertex does not intersect, then it is left as null.
            // this is potentially memory intensive but speeds up the matching in when creating new polygons
            var lookupList = new List<int>[index];
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                intersection.VisitedA = false;
                intersection.VisitedB = false;
                index = intersection.EdgeA.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
            }

            positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store positive polygons in increasing area
            negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store negative in increasing (from -inf to 0) area
            // now we want to find the sub-polygons that are not intersecting anything and decide whether to keep them or not
            index = 0;
            foreach (var poly in allPolygons)
            {
                var isNonIntersecting = true; //start as true. once a case is found to be intersecting, set it to false
                var isIdentical = true; //start as true. once a case is found to be different, set it to false
                var identicalPolygonIsInverted = false;
                for (int j = polygonStartIndices[index]; j < polygonStartIndices[index + 1]; j++)
                {
                    if (lookupList[j] == null) //no intersection is good to check to skip code in 'else' but also it tells us that they are not identical
                        isIdentical = false;
                    else
                    {
                        isNonIntersecting = false; // now it is known that the two polygons intersect since lookupList[j] is not null

                        var intersectionIndex = lookupList[j].FindIndex(k => (intersections[k].Relationship & (PolygonSegmentRelationship.BothLinesStartAtPoint
                        | PolygonSegmentRelationship.CoincidentLines | PolygonSegmentRelationship.SameLineAfterPoint | PolygonSegmentRelationship.SameLineBeforePoint))
                        == (PolygonSegmentRelationship.BothLinesStartAtPoint | PolygonSegmentRelationship.CoincidentLines | PolygonSegmentRelationship.SameLineAfterPoint
                        | PolygonSegmentRelationship.SameLineBeforePoint));
                        if (intersectionIndex == -1)
                            isIdentical = false;
                        else identicalPolygonIsInverted =
                                ((intersections[lookupList[j][intersectionIndex]].Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0);
                    }
                    if (!isIdentical && !isNonIntersecting) break; //once it is found that it is both not identical and intersecting then no need to keep checking
                }
                if (isIdentical)
                {   // go back through the same indices and remove references to the intersections. Also, set the intersections to "visited"
                    // which is easier than deleting since the other references would collapse down
                    for (int j = polygonStartIndices[index]; j < polygonStartIndices[index + 1]; j++)
                    {
                        var intersectionIndex = lookupList[j].First(k => (intersections[k].Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0
                     && (intersections[k].Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0);
                        lookupList[j].Remove(intersectionIndex);
                        if (lookupList[j].Count == 0) lookupList[j] = null;
                        intersections[intersectionIndex].VisitedA = true;
                        intersections[intersectionIndex].VisitedB = true;
                        // note, in the next line - this has to be EdgeB since searching in order the A polygon will detect the duplicate - B will skip over
                        var otherLookupEntry = lookupList[intersections[intersectionIndex].EdgeB.IndexInList];
                        otherLookupEntry.Remove(intersectionIndex);
                        //if (otherLookupEntry.Count == 0) lookupList[intersections[intersectionIndex].EdgeB.IndexInList] = null;
                        // hmm, I commented the previous line for good reason but I do not recall why. It would seem (for symmetry sake) it should be happen, but no
                    }
                    HandleIdenticalPolygons(poly, positivePolygons, negativePolygons, identicalPolygonIsInverted);
                }

                else if (isNonIntersecting)
                    HandleNonIntersectingSubPolygon(poly, polygonA, polygonB, positivePolygons, negativePolygons, polygonStartIndices[index] >= startOfBVertices);
                index++;
            }
            return lookupList;
        }

        protected abstract void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, SortedDictionary<double, Polygon> positivePolygons, SortedDictionary<double,
            Polygon> negativePolygons, bool partOfPolygonB);


        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected abstract void HandleIdenticalPolygons(Polygon subPolygonA, SortedDictionary<double, Polygon> positivePolygons, SortedDictionary<double, Polygon> negativePolygons,
                    bool identicalPolygonIsInverted);



        /// <summary>
        /// Creates the shallow polygon trees following boolean operations. The name follows the public methods,
        /// this is meant to be used only internally as it requires several assumptions:
        /// 1. positive polygons are ordered by increasing area (from 0 to +inf)
        /// 2. negative polygons are ordered by increasing area (from -inf to 0)
        /// 3. there are not intersections between the polygons (this should be the result following the boolean
        /// operation; however, it is possible that they share a vertex (e.g. in XOR))
        /// </summary>
        /// <param name="Polygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <returns>Polygon[].</returns>
        /// <exception cref="Exception">Intersections still exist between hole and positive polygon.</exception>
        /// <exception cref="Exception">Negative polygon was not inside any positive polygons</exception>
        private List<Polygon> CreateShallowPolygonTreesPostBooleanOperation(List<Polygon> positivePolygons,
                IEnumerable<Polygon> negativePolygons, out List<Polygon> strayHoles)
        {
            //first, remove any positive polygons that are nested inside of bigger ones
            int i = 0;
            while (i < positivePolygons.Count)
            {
                var foundToBeInsideOfOther = false;
                for (int j = i + 1; j < positivePolygons.Count; j++)
                {
                    if (positivePolygons[j].IsNonIntersectingPolygonInside(positivePolygons[i], out _) == true)
                    {
                        foundToBeInsideOfOther = true;
                        break;
                    }
                }

                if (foundToBeInsideOfOther)
                    positivePolygons.RemoveAt(i);
                else i++;
            }
            strayHoles = new List<Polygon>();
            //  Find the positive polygon that this negative polygon is inside.
            //The negative polygon belongs to the smallest positive polygon that it fits inside.
            //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
            //and the reversed ordering, gaurantee that we get the correct shallow tree.
            foreach (var negativePolygon in negativePolygons)
            {
                var isInside = false;
                //Start with the smallest positive polygon           
                for (var j = 0; j < positivePolygons.Count; j++)
                {
                    var positivePolygon = positivePolygons[j];
                    if (positivePolygon.IsNonIntersectingPolygonInside(negativePolygon, out var onBoundary) == true)
                    {
                        isInside = true;
                        if (onBoundary)
                        {
                            var newPolys = positivePolygon.Intersect(negativePolygon);
                            positivePolygons[j] = newPolys[0]; // i don't know if this is a problem, but the
                            // new polygon at j may be smaller (now that it has a big hole in it ) than the preceding ones. I don't think
                            // we need to maintain ordered by area - since the first loop above will already merge positive loops
                            for (int k = 1; k < newPolys.Count; k++)
                                positivePolygons.Add(newPolys[i]);
                        }
                        else positivePolygon.AddHole(negativePolygon);

                        //The negative polygon ONLY belongs to the smallest positive polygon that it fits inside.
                        //isInside = true;
                        break;
                    }
                }

                if (!isInside) strayHoles.Add(negativePolygon);
                //this feels like it should come with a warning. but perhaps the user/developer intends to create 
                // a negative polygon
                // actually, in silhouette this function is called and may result in loose holes
            }
            //Set the polygon indices
            var index = 0;
            foreach (var polygon in positivePolygons)
            {
                polygon.Index = index++;
                foreach (var hole in polygon.Holes)
                {
                    hole.Index = polygon.Index;
                }
            }
            return positivePolygons;
        }




        #endregion
    }
}