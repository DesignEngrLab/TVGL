using ClipperLib;
using Newtonsoft.Json.Linq;
using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
                {          // where the order of A then B is used in defining segmentA and segmentB
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
                var noNominalIntersections = !intersections.Any(intersect =>
                intersect.relationship == PolygonSegmentRelationship.IntersectNominal);
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



        #region Clockwise / CounterClockwise Ordering

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        private static List<Vector2> CCWPositive(IEnumerable<Vector2> p)
        {
            var polygon = new List<Vector2>(p);
            var area = p.Area();
            if (area < 0) polygon.Reverse();
            return polygon;
        }

        /// <summary>
        /// Sets a polygon to clock wise negative
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Vector2> CWNegative(IEnumerable<Vector2> p)
        {
            var polygon = new List<Vector2>(p);
            var area = p.Area();
            if (area > 0) polygon.Reverse();
            return polygon;
        }

        #endregion

        #region Boolean Operations
        public static Polygon RemoveSelfIntersections(this Polygon polygon)
        {
            return RemoveSelfIntersections(polygon, polygon.GetSelfIntersections());
        }
        public static Polygon RemoveSelfIntersections(this Polygon polygon, List<IntersectionData> intersections)
        {
            if (intersections.Count == 0) return polygon;
            var readOnlyIntersectionLookup = MakeIntersectionLookupList(polygon.Lines.Count, intersections);
            var usedIntersectionLookup = new HashSet<int>[readOnlyIntersectionLookup.Length];
            var polygons = new List<Polygon>();
            while (GetNextIntersection(readOnlyIntersectionLookup, usedIntersectionLookup, out var startIndex, out var currentEdgeIndex))
            {
                Polygon thispolygon = MakePolygonThroughIntersections(readOnlyIntersectionLookup, usedIntersectionLookup, intersections, 
                    startIndex,polygon.Lines[currentEdgeIndex],false,false);
                if (!thispolygon.IsPositive) thispolygon.Reverse();
                polygons.Add(thispolygon);
            }
            polygons = polygons.OrderByDescending(p => p.Area).ToList();
            int i = 0;
            while (polygons.Count > 1)
            {
                var outerPolygon = polygons[i];
                var j = i + 1;
                while (j < polygons.Count)
                {
                    var polygonRelationship = outerPolygon.GetPolygonRelationshipAndIntersections(polygons[j], out _);
                    if (polygonRelationship == PolygonRelationship.BIsCompletelyInsideA || polygonRelationship == PolygonRelationship.Separated)
                        j++;
                    else
                    {
                        polygons[i] = outerPolygon = outerPolygon.Union(polygons[j])[0];
                        polygons.RemoveAt(j);
                    }
                }
                i++;
                if (i == polygons.Count) i = 0;
            }
            return polygons[0];
        }

        /// <summary>
        /// Gets the next intersection by looking throught the intersectionLookupList. It'll return false, when there are none left.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="start">The start.</param>
        /// <param name="segmentAIsReference">if set to <c>true</c> [segment a is reference].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool GetNextIntersection(List<int>[] readOnlyIntersectionLookup, HashSet<int>[] usedIntersectionLookup, 
          out int intersectionIndex, out int currentEdgeIndex)
        {
            intersectionIndex = -1;
            for (int edgeIndex = 0; edgeIndex < readOnlyIntersectionLookup.Length; edgeIndex++)
            {
                if (readOnlyIntersectionLookup[edgeIndex] == null) continue;
                foreach (var index in readOnlyIntersectionLookup[edgeIndex])
                {
                    if (usedIntersectionLookup[edgeIndex] == null || !usedIntersectionLookup[edgeIndex].Contains(index))
                    {
                        intersectionIndex = index;
                        currentEdgeIndex = edgeIndex;
                        return true;
                    }
                }
            }
            currentEdgeIndex = -1;
            return false;
        }

        private static Polygon MakePolygonThroughIntersections(List<int>[] readonlyIntersectionLookup, HashSet<int>[] usedIntersectionLookup,
            List<IntersectionData> intersections, int indexOfNewIntersection, PolygonSegment currentEdge, bool recordForBothEdges, bool switchDirections)
        {
            var newPath = new List<Vector2>();
            var forward=true; // as in following the edges in the forward direction (from...to). If false, then traverse backwards
            do
            {
                var intersectionData = intersections[indexOfNewIntersection];
                if (recordForBothEdges || currentEdge == intersectionData.segmentA)
                    intersectionData.EnteredA = true;
                if (recordForBothEdges || currentEdge == intersectionData.segmentA)
                    intersectionData.EnteredB = true;
                RecordIntersectionInUsedLookup(usedIntersectionLookup, currentEdge, indexOfNewIntersection,
                    recordForBothEdges, intersectionData);
                if (intersectionData.relationship == PolygonSegmentRelationship.CollinearAndOverlapping
                    || intersectionData.relationship == PolygonSegmentRelationship.ConnectInT
                    || intersectionData.relationship == PolygonSegmentRelationship.EndPointsTouch)
                    throw new NotImplementedException();
                currentEdge = (currentEdge == intersectionData.segmentA) ? intersectionData.segmentB
                    : intersectionData.segmentA;
                var intersectionCoordinates = intersectionData.intersectCoordinates;
                newPath.Add(intersectionCoordinates);
                while (!NextIntersectionOnThisEdge(readonlyIntersectionLookup, currentEdge, intersections,
                   intersectionCoordinates, out indexOfNewIntersection))
                {
                    if (switchDirections) forward = !forward;
                    if (forward)
                    {
                        newPath.Add(currentEdge.ToPoint.Coordinates);
                        currentEdge = currentEdge.ToPoint.StartLine;
                    }
                    else
                    {
                        newPath.Add(currentEdge.FromPoint.Coordinates);
                        currentEdge = currentEdge.FromPoint.StartLine;
                    }
                    intersectionCoordinates = Vector2.Null;
                }
            } while (usedIntersectionLookup[currentEdge.IndexInList] == null
            || !usedIntersectionLookup[currentEdge.IndexInList].Contains(indexOfNewIntersection));
            return new Polygon(newPath, false);
        }

        private static void RecordIntersectionInUsedLookup(HashSet<int>[] usedIntersectionLookup,
            PolygonSegment currentEdge, in int indexOfNewIntersection, in bool recordForBothEdges,
            IntersectionData intersectionData)
        {
            if (usedIntersectionLookup[currentEdge.IndexInList] == null)
                usedIntersectionLookup[currentEdge.IndexInList] = new HashSet<int>();
            usedIntersectionLookup[currentEdge.IndexInList].Add(indexOfNewIntersection);
            if (recordForBothEdges)
            {
                var otherEdgeIndex = intersectionData.segmentA.IndexInList == currentEdge.IndexInList
                    ? intersectionData.segmentB.IndexInList
                    : intersectionData.segmentA.IndexInList;
                if (usedIntersectionLookup[otherEdgeIndex] == null)
                    usedIntersectionLookup[otherEdgeIndex] = new HashSet<int>();
                usedIntersectionLookup[otherEdgeIndex].Add(indexOfNewIntersection);
            }
        }

        private static bool NextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<IntersectionData> allIntersections,
        Vector2 formerIntersectCoords, out int indexOfItersection)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            indexOfItersection = -1;
            if (intersectionIndices == null)
                return false;
            var minDistanceToIntersection = double.PositiveInfinity;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                var distance = double.NaN;
                if (thisIntersectData.relationship == PolygonSegmentRelationship.CollinearAndOverlapping)
                {
                    var otherLine = (thisIntersectData.segmentA == currentEdge) ? thisIntersectData.segmentB : thisIntersectData.segmentA;
                    var fromDist = currentEdge.Vector.Dot(otherLine.FromPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var toDist = currentEdge.Vector.Dot(otherLine.ToPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var thisLength = currentEdge.Vector.LengthSquared();
                    throw new NotImplementedException();
                }
                else distance = formerIntersectCoords.IsNull()
                        ? currentEdge.Vector.Dot(thisIntersectData.intersectCoordinates - currentEdge.FromPoint.Coordinates)
                        : currentEdge.Vector.Dot(thisIntersectData.intersectCoordinates - formerIntersectCoords);
                if (distance > 0 && minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    indexOfItersection = index;
                }
            }
            return indexOfItersection >= 0;
        }

        private static List<int>[] MakeIntersectionLookupList(int numLines, List<IntersectionData> intersections)
        {
            var result = new List<int>[numLines];
            for (int i = 0; i < intersections.Count; i++)
            {
                var index = intersections[i].segmentA.IndexInList;
                if (result[index] == null) result[index] = new List<int>();
                result[index].Add(i);
                index = intersections[i].segmentB.IndexInList;
                if (result[index] == null) result[index] = new List<int>();
                result[index].Add(i);
            }
            return result;
        }

        #endregion


        #region Boolean Operations

        #region Union
        public static Polygon[] Union(this Polygon polygonA, Polygon polygonB)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                    return new[] { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    return new[] { polygonA.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new[] { polygonB.Copy() };
                default:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.SeparatedButBordersTouch:
                    //case PolygonRelationship.BVerticesInsideAButLinesIntersect:
                    //case PolygonRelationship.BInsideAButBordersTouch:
                    //case PolygonRelationship.AVerticesInsideBButLinesIntersect:
                    //case PolygonRelationship.AInsideBButBordersTouch:
                    return new[] { Union(polygonA, polygonB, intersections) };
            }
        }
        public static Polygon Union(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections)
        {
            var id = 0;
            foreach (var polygon in polygonB.AllPolygons)
            {
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = id++;
            }

            foreach (var polygon in polygonA.AllPolygons)
            {
                foreach (var vertex in polygonA.Vertices)
                    vertex.IndexInList = id++;
            }
            var readOnlyIntersectionLookup = MakeIntersectionLookupList(id, intersections);
            var usedIntersectionLookup = new HashSet<int>[id];
            // get the first intersection
            var cross = intersections[0].segmentA.Vector.Cross(intersections[0].segmentB.Vector);
            var result = MakePolygonThroughIntersections(readOnlyIntersectionLookup, usedIntersectionLookup, intersections,
                0, cross > 0 ? intersections[0].segmentB : intersections[0].segmentA,true,false);
            id = 0;
            foreach (var vertex in polygonA.Vertices)
                vertex.IndexInList = id++;
            return result;
        }
        public static Polygon[] Intersect(this Polygon polygonA, Polygon polygonB)
        {
            switch (GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections))
            {
                case PolygonRelationship.Separated:
                    return new[] { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    return new[] { polygonB.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new[] { polygonA.Copy() };
                default:
                    return new[] { Intersect(polygonA, polygonB, intersections) };
            }
        }
        public static Polygon Intersect(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections)
        {
            var id = 0;
            foreach (var vertex in polygonB.Vertices)
                vertex.IndexInList = id++;
            foreach (var vertex in polygonA.Vertices)
                vertex.IndexInList = id++;
            var readOnlyIntersectionLookup = MakeIntersectionLookupList(id, intersections);
            var usedIntersectionLookup = new HashSet<int>[id];
            // get the first intersection
            var cross = intersections[0].segmentA.Vector.Cross(intersections[0].segmentB.Vector);
            var result = MakePolygonThroughIntersections(readOnlyIntersectionLookup, usedIntersectionLookup, intersections,
                0, cross < 0 ? intersections[0].segmentB : intersections[0].segmentA,true,false);
            id = 0;
            foreach (var vertex in polygonA.Vertices)
                vertex.IndexInList = id++;
            return result;
        }

        public static Polygon[] Subtract(this Polygon polygonA, Polygon polygonB)
        {
            var negativeB = polygonB.Copy();
            negativeB.Reverse();
            return Intersect(polygonA, negativeB);
        }
        public static Polygon Subtract(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections)
        {
            var id = 0;
            foreach (var vertex in polygonB.Vertices)
                vertex.IndexInList = id++;
            foreach (var vertex in polygonA.Vertices)
                vertex.IndexInList = id++;
            var readOnlyIntersectionLookup = MakeIntersectionLookupList(id, intersections);
            var usedIntersectionLookup = new HashSet<int>[id];
            // get the first intersection
            var cross = intersections[0].segmentA.Vector.Cross(intersections[0].segmentB.Vector);
            var result = MakePolygonThroughIntersections(readOnlyIntersectionLookup, usedIntersectionLookup, intersections,
                0, cross < 0 ? intersections[0].segmentB : intersections[0].segmentA, true, true);
            id = 0;
            foreach (var vertex in polygonA.Vertices)
                vertex.IndexInList = id++;
            return result;
        }

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