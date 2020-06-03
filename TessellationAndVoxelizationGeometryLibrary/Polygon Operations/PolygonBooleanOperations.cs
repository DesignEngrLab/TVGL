using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        public static PolygonRelationship GetPolygonRelationshipAndIntersections(this Polygon polygonA, Polygon polygonB,
    out List<IntersectionData> intersections)
        {
            intersections = new List<IntersectionData>();
            //As a first check, determine if the axis aligned bounding boxes overlap. If not, then we can
            // safely return that the polygons are separated.
            if (polygonA.MinX > polygonB.MaxX ||
                polygonA.MaxX < polygonB.MinX ||
                polygonA.MinY > polygonB.MaxY ||
                polygonA.MaxY < polygonB.MinY) return PolygonRelationship.Separated;
            //Else, we need to check for intersections between all lines of the two
            // To avoid an n-squared check (all of A's lines with all of B's), we sort the lines by their XMin
            // values and store in two separate queues
            var orderedAPoints = polygonA.Vertices.OrderBy(p => p.X).ToList();
            var hashOfLines = polygonA.Lines.ToHashSet();
            var aLines = GetOrderedLines(orderedAPoints, hashOfLines);
            // instead of the prev. 3 lines a simpler solution would be the following line.
            // var aLines = polygonA.Lines.OrderBy(line => line.XMin).ToList();
            // However, we will need to sort the vertices in the ArePointsInsidePolygon below. and - even 
            // though there is some expense in setting up and checking the HashSet O(n) (since checking 
            // hashset n times), the above sort is faster since the condition in line.XMin is avoided
            var orderedBPoints = polygonB.Vertices.OrderBy(p => p.X).ToList();
            hashOfLines = polygonB.Lines.ToHashSet();
            var bLines = GetOrderedLines(orderedBPoints, hashOfLines);

            var aIndex = 0;
            var bIndex = 0;
            while (aIndex < aLines.Length && bIndex < bLines.Length)
            {
                if (aLines[aIndex].XMin < bLines[bIndex].XMin)
                {
                    var current = aLines[aIndex++];
                    var otherIndex = bIndex;
                    while (otherIndex < bLines.Length && current.XMax > bLines[otherIndex].XMin)
                    {
                        var other = bLines[otherIndex++];
                        var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                        if (segmentRelationship >= 0)
                            intersections.Add(new IntersectionData(current, other, intersection,
                                segmentRelationship));
                    }
                }          // I hate that there is duplicate code here, but I don't know if there is a better way
                else       // (I tried several). The subtle difference in the last IntersectionData constructor
                {          // where the order of A then B is used in defining segmentA and EdgeB
                    var current = bLines[bIndex++];
                    var otherIndex = aIndex;
                    while (otherIndex < aLines.Length && current.XMax > aLines[otherIndex].XMin)
                    {
                        var other = aLines[otherIndex++];
                        var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                        if (segmentRelationship >= 0)
                            intersections.Add(new IntersectionData(other, current, intersection,
                                segmentRelationship));
                    }
                }
            }
            if (intersections.Count > 0)
            {
                var noNominalIntersections = intersections.All(intersect
                    => intersect.Relationship != PolygonSegmentRelationship.IntersectNominal);
                if (ArePointsInsidePolygonLines(aLines, aLines.Length, orderedBPoints, out _, false))
                {
                    if (noNominalIntersections) return PolygonRelationship.BInsideAButBordersTouch;
                    return PolygonRelationship.BVerticesInsideAButLinesIntersect;
                }
                if (ArePointsInsidePolygonLines(bLines, bLines.Length, orderedAPoints, out _, false))
                {
                    if (noNominalIntersections) return PolygonRelationship.AInsideBButBordersTouch;
                    return PolygonRelationship.AVerticesInsideBButLinesIntersect;
                }
                if (noNominalIntersections) return PolygonRelationship.SeparatedButBordersTouch;
                return PolygonRelationship.Intersect;
            }
            if (polygonA.IsPointInsidePolygon(polygonB.Vertices[0].Coordinates, out _, out _, out _, false))
                return PolygonRelationship.BIsCompletelyInsideA;
            if (polygonB.IsPointInsidePolygon(polygonA.Vertices[0].Coordinates, out _, out _, out _, false))
                return PolygonRelationship.AIsCompletelyInsideB;
            return PolygonRelationship.Separated;
            // todo: holes! how to check if B is inside a hole of A. what if it fully encompasses a hole of A
        }


        private static PolygonSegment[] GetOrderedLines(List<Vertex2D> orderedPoints, HashSet<PolygonSegment> hashOfLines)
        {
            var length = orderedPoints.Count;
            var result = new PolygonSegment[length];
            var k = 0;
            for (int i = 0; i < length; i++)
            {
                var point = orderedPoints[i];
                if (hashOfLines.Contains(point.EndLine))
                {
                    hashOfLines.Remove(point.EndLine);
                    result[k++] = point.EndLine;
                }
                if (hashOfLines.Contains(point.StartLine))
                {
                    hashOfLines.Remove(point.StartLine);
                    result[k++] = point.StartLine;
                }
            }
            return result;
        }

        public static List<IntersectionData> GetSelfIntersections(this Polygon polygonA)
        {
            var intersections = new List<IntersectionData>();
            var numLines = polygonA.Lines.Count;
            var orderedLines = polygonA.Lines.OrderBy(line => line.XMin).ToList();
            for (int i = 0; i < numLines - 1; i++)
            {
                var current = orderedLines[i];
                for (int j = i + 1; j < numLines; j++)
                {
                    var other = orderedLines[j];
                    if (current.XMax < orderedLines[j].XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                    if (segmentRelationship >= 0)
                        intersections.Add(new IntersectionData(current, other, intersection,
                            segmentRelationship));
                }
            }
            return intersections;
        }

        #region Boolean Operations
        public static Polygon RemoveSelfIntersections(this Polygon polygon, double minAllowableArea = Constants.BaseTolerance)
        {
            return RemoveSelfIntersections(polygon, polygon.GetSelfIntersections(), minAllowableArea);
        }
        public static Polygon RemoveSelfIntersections(this Polygon polygon, List<IntersectionData> intersections,
            double minAllowableArea = Constants.BaseTolerance)
        {
            if (intersections.Count == 0) return polygon;
            var intersectionLookup = MakeIntersectionLookupList(polygon.Lines.Count, intersections);
            var polygons = new List<Polygon>();
            var id = 0;
            while (GetNextStartingIntersection(intersectionLookup, intersections, false, 0, out var startIntersection,
                out var currentEdge))
            {
                var polygonCoordinates
                    = MakePolygonThroughIntersections(intersectionLookup, intersections,
                        startIntersection, currentEdge, false, false).ToList();
                var area = polygonCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) continue; //polygonCoordinates.Reverse();
                polygons.Add(new Polygon(polygonCoordinates, true, id++));
            }
            polygons = polygons.OrderByDescending(p => p.Area).ToList();
            var i = 0;
            while (polygons.Count > 1)
            {
                var outerPolygon = polygons[i];
                var j = i + 1;
                while (j < polygons.Count)
                {
                    var polygonRelationship = outerPolygon.GetPolygonRelationshipAndIntersections(polygons[j], out intersections);
                    if (polygonRelationship == PolygonRelationship.Separated) j++;
                    // this should follow the switch statement in Union
                    else
                    {
                        polygons[i] = polygons[i].Union(polygons[j], intersections, minAllowableArea)[0];
                        polygons.RemoveAt(j);
                    }
                }
                i++;
                if (i == polygons.Count) i = 0;
            }
            return polygons[0];
        }

        /// <summary>
        /// Gets the next intersection by looking through the intersectionLookupList. It'll return false, when there are none left.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="onlyVisitOnce">if set to <c>true</c> [only visit once].</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="intersectionIndex">Index of the intersection.</param>
        /// <param name="currentEdgeIndex">Index of the current edge.</param>
        /// <returns><c>true</c> if a new starting intersection was found, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool GetNextStartingIntersection(List<int>[] intersectionLookup, List<IntersectionData> intersections, bool onlyVisitOnce,
         int crossProductSign, out IntersectionData nextStartingIntersection, out PolygonSegment currentEdge)
        {
            for (int edgeIndex = 0; edgeIndex < intersectionLookup.Length; edgeIndex++)
            {
                if (intersectionLookup[edgeIndex] == null) continue;
                foreach (var index in intersectionLookup[edgeIndex])
                {
                    var intersectionData = intersections[index];
                    if (onlyVisitOnce && (intersectionData.EnteredA || intersectionData.EnteredB)) continue;
                    var enteringEdgeA = edgeIndex == intersectionData.EdgeA.IndexInList;
                    if (enteringEdgeA && intersectionData.EnteredA) continue; //you can enter twice but you've already entered A before (would need to use EdgeB to start here)
                    if (!enteringEdgeA && intersectionData.EnteredB) continue; //you can enter twice but you've already entered B before (would need to use EdgeA to start here)
                    var cross = (enteringEdgeA ? 1 : -1)
                                // cross product is from the entering edge to the other. We use the "enteringEdgeA" boolean to flip the sign if we are really entering B
                                * intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);

                   //if (crossProductSign != 0 && enteringEdgeA && crossProductSign * cross < 0) continue; // then will have to wait for EdgeB
                    //if (crossProductSign != 0 && !enteringEdgeA && crossProductSign * cross < 0) continue; // entering EdgeB but cross product is wrong
                    if (crossProductSign != 0 && crossProductSign * cross < 0) continue; // entering EdgeB but cross product is wrong
                    // what about when crossProduct is zero - like in a line Intersection.Relationship will be in line
                    currentEdge = enteringEdgeA ? intersectionData.EdgeA : intersectionData.EdgeB;
                    nextStartingIntersection = intersectionData;
                    return true;
                }
            }

            nextStartingIntersection = null;
            currentEdge = null;
            return false;
        }

        /// <summary>
        /// Makes the polygon through intersections.
        /// </summary>
        /// <param name="intersectionLookup">The readonly intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="intersectionIndex">The index of new intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="onlyVisitOnce">if set to <c>true</c> [only visit once].</param>
        /// <param name="switchDirections">if set to <c>true</c> [switch directions].</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup,
            List<IntersectionData> intersections, IntersectionData intersectionData, PolygonSegment currentEdge, bool onlyVisitOnce, bool switchDirections)
        {
            var newPath = new List<Vector2>();
            var forward = true; // as in following the edges in the forward direction (from...to). If false, then traverse backwards
            while ((onlyVisitOnce && !intersectionData.EnteredA && !intersectionData.EnteredB)
                || (!onlyVisitOnce && intersectionData.EdgeA == currentEdge && !intersectionData.EnteredA)
                || (!onlyVisitOnce && intersectionData.EdgeB == currentEdge && !intersectionData.EnteredB))
            {
                currentEdge = currentEdge == intersectionData.EdgeA ? intersectionData.EdgeB : intersectionData.EdgeA;
                if ((currentEdge == intersectionData.EdgeA) == forward) intersectionData.EnteredA = true;
                else intersectionData.EnteredB = true;
                //if (currentEdge == intersectionData.EdgeA)
                //{
                //    intersectionData.EnteredA = true;
                //    currentEdge = intersectionData.EdgeB;
                //}
                //else
                //{
                //    intersectionData.EnteredB = true;
                //    currentEdge = intersectionData.EdgeA;
                //}
                if (intersectionData.Relationship == PolygonSegmentRelationship.CollinearAndOverlapping
                    || intersectionData.Relationship == PolygonSegmentRelationship.ConnectInT
                    || intersectionData.Relationship == PolygonSegmentRelationship.EndPointsTouch)
                    throw new NotImplementedException();
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                newPath.Add(intersectionCoordinates);
                int intersectionIndex;
                if (switchDirections) forward = !forward;
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                   intersectionCoordinates, out intersectionIndex))
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
                    intersectionCoordinates = Vector2.Null;
                }
                intersectionData = intersections[intersectionIndex];
            }

            return newPath;
        }

        private static bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<IntersectionData> allIntersections,
        Vector2 formerIntersectCoords, out int indexOfIntersection)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            indexOfIntersection = -1;
            if (intersectionIndices == null)
                return false;
            var minDistanceToIntersection = double.PositiveInfinity;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                double distance;
                if (thisIntersectData.Relationship == PolygonSegmentRelationship.CollinearAndOverlapping)
                {
                    var otherLine = (thisIntersectData.EdgeA == currentEdge) ? thisIntersectData.EdgeB : thisIntersectData.EdgeA;
                    var fromDist = currentEdge.Vector.Dot(otherLine.FromPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var toDist = currentEdge.Vector.Dot(otherLine.ToPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var thisLength = currentEdge.Vector.LengthSquared();
                    throw new NotImplementedException();
                }
                else distance = formerIntersectCoords.IsNull()
                        ? currentEdge.Vector.Dot(thisIntersectData.IntersectCoordinates - currentEdge.FromPoint.Coordinates)
                        : currentEdge.Vector.Dot(thisIntersectData.IntersectCoordinates - formerIntersectCoords);
                if (distance > 0 && minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    indexOfIntersection = index;
                }
            }
            return indexOfIntersection >= 0;
        }

        private static List<int>[] MakeIntersectionLookupList(int numLines, List<IntersectionData> intersections)
        {
            var result = new List<int>[numLines];
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                intersection.EnteredA = false;
                intersection.EnteredB = false;
                var index = intersection.EdgeA.IndexInList;
                result[index] ??= new List<int>();
                result[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                result[index] ??= new List<int>();
                result[index].Add(i);
            }
            return result;
        }

        #endregion


        #region New Boolean Operations

        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    return new List<Polygon> { polygonA.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new List<Polygon> { polygonB.Copy() };
                default:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.SeparatedButBordersTouch:
                    //case PolygonRelationship.BVerticesInsideAButLinesIntersect:
                    //case PolygonRelationship.BInsideAButBordersTouch:
                    //case PolygonRelationship.AVerticesInsideBButLinesIntersect:
                    //case PolygonRelationship.AInsideBButBordersTouch:
                    return Union(polygonA, polygonB, intersections, minAllowableArea);
            }
        }
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            return BooleanOperation(polygonA, polygonB, intersections, true, false, -1, minAllowableArea);
        }
        public static List<Polygon> BooleanOperation(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, bool onlyVisitOnce, bool switchDirection,
            int crossProductSign, double minAllowableArea = Constants.BaseTolerance)
        {
            var id = 0;
            foreach (var polygon in polygonA.AllPolygons)
            {
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = id++;
            }
            // temporarily number the vertices so that each has a unique number. this is important for the Intersection Lookup List
            foreach (var polygon in polygonB.AllPolygons)
            {
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = id++;
            }
            var intersectionLookup = MakeIntersectionLookupList(id, intersections);
            var positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store positive polygons in increasing area
            var negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersectionLookup, intersections, onlyVisitOnce, crossProductSign, out var startIndex,
                out var startEdge))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startIndex,
                    startEdge, onlyVisitOnce, switchDirection).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates, false));
                else positivePolygons.Add(area, new Polygon(polyCoordinates, false));
            }
            // reset ids for polygon B
            id = 0;
            foreach (var vertex in polygonB.Vertices)
                vertex.IndexInList = id++;

            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values);
        }
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                    return new List<Polygon>();
                case PolygonRelationship.BIsCompletelyInsideA:
                    return new List<Polygon> { polygonB.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new List<Polygon> { polygonA.Copy() };
                default:
                    return Intersect(polygonA, polygonB, intersections, minAllowableArea);
            }
        }
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            return BooleanOperation(polygonA, polygonB, intersections, true, false, +1, minAllowableArea);
        }

        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButBordersTouch:
                    return new List<Polygon> { polygonA.Copy() };
                // hmm, what to do in this case. if no intersection make loop (MakePathThroughIntersections) will fail
                //case PolygonRelationship.BIsCompletelyInsideA:
                // return new[] { polygonB.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new List<Polygon>();
                default:
                    return Subtract(polygonA, polygonB, intersections, minAllowableArea);
            }
        }
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            return BooleanOperation(polygonA, polygonB, intersections, true, true, -1, minAllowableArea);
        }
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButBordersTouch:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                // hmm, what to do in this case. if no intersection make loop (MakePathThroughIntersections) will fail
                //case PolygonRelationship.BIsCompletelyInsideA:
                // return new[] { polygonB.Copy() };
                //case PolygonRelationship.AIsCompletelyInsideB:
                //    return Array.Empty<Polygon>();
                default:
                    return ExclusiveOr(polygonA, polygonB, intersections, minAllowableArea);
            }
        }
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            return BooleanOperation(polygonA, polygonB, intersections, false, true, -1, minAllowableArea);
        }
        #endregion

        #region Clipper Boolean Functions
        #region union
        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, bool simplifyPriorToUnion = true,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, (IEnumerable<List<Vector2>>)subject, null, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, clip, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, new[] { subject }, new[] { clip }, simplifyPriorToUnion);
        }

        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, new[] { clip }, simplifyPriorToUnion);
        }

        #endregion

        #region Difference
        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctDifference, subject, clip, simplifyPriorToDifference);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(subject, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, clip, simplifyPriorToDifference, polyFill);
        }
        #endregion

        #region Intersection
        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(subjects, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, clips, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctIntersection, subject, clip, simplifyPriorToIntersection);
        }
        #endregion

        #region Xor

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctXor, subject, clip, simplifyPriorToXor);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip,
            bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(subjects, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips.  
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToXor = true,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, clips, simplifyPriorToXor, polyFill);
        }

        #endregion

        private static List<List<Vector2>> BooleanOperation(PolygonFillType fillMethod, ClipType clipType,
            IEnumerable<IEnumerable<Vector2>> subject,
           IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToBooleanOperation = true)
        {
            var fillType = fillMethod switch
            {
                PolygonFillType.Positive => PolyFillType.pftPositive,
                PolygonFillType.Negative => PolyFillType.pftNegative,
                PolygonFillType.NonZero => PolyFillType.pftNonZero,
                PolygonFillType.EvenOdd => PolyFillType.pftEvenOdd,
                _ => throw new NotImplementedException(),
            };

            if (simplifyPriorToBooleanOperation)
            {
                subject = subject.Select(path => Simplify(path));
                //If not null
                clip = clip?.Select(path => Simplify(path));
            }

            if (!subject.Any())
            {
                if (clip == null || !clip.Any())
                {
                    return new List<List<Vector2>>();
                }
                //Use the clip as the subject if this is a union operation and the clip is not null.
                if (clipType == ClipType.ctUnion)
                {
                    subject = clip;
                    clip = null;
                }
            }

            //Setup Clipper
            var clipper = new Clipper() { StrictlySimple = true };

            //Convert Points (TVGL) to IntPoints (Clipper)
            var subjectIntLoops = new List<List<IntPoint>>();
            foreach (var loop in subject)
            {
                var intLoop = loop.Select(point
                    => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                if (intLoop.Count > 2) subjectIntLoops.Add(intLoop);
            }
            clipper.AddPaths(subjectIntLoops, PolyType.ptSubject, true);

            if (clip != null)
            {
                var clipIntLoops = new List<List<IntPoint>>();
                foreach (var loop in clip)
                {
                    var intLoop = loop.Select(point
                        => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                    if (intLoop.Count > 2) clipIntLoops.Add(intLoop);
                }
                clipper.AddPaths(clipIntLoops, PolyType.ptClip, true);
            }

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            var result = clipper.Execute(clipType, clipperSolution, fillType, fillType);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points
            var solution = clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler, point.Y * Constants.IntPointToDoubleMultipler))
            .ToList()).ToList();
            return solution;
        }
        #endregion

    }
}