using ClipperLib;
using Newtonsoft.Json.Linq;
using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
            PolygonSegment current; // = null;
            PolygonSegment[] otherLines;
            int otherLimit, otherIndex;
            while (aIndex < aLines.Length && bIndex < bLines.Length)
            {
                if (aLines[aIndex].XMin < bLines[bIndex].XMin)
                {
                    current = aLines[aIndex++];
                    otherLines = bLines;
                    otherLimit = bLines.Length;
                    otherIndex = bIndex;
                }
                else
                {
                    current = bLines[bIndex++];
                    otherLines = aLines;
                    otherLimit = aLines.Length;
                    otherIndex = aIndex;
                }
                while (otherIndex < otherLimit && current.XMax > otherLines[otherIndex].XMin)
                {
                    var other = otherLines[otherIndex++];
                    var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                    if (segmentRelationship >= 0)
                        intersections.Add(new IntersectionData(current, other, intersection,
                            segmentRelationship));
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
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon)
        {
            return RemoveSelfIntersections(polygon, polygon.GetSelfIntersections());
        }
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, List<IntersectionData> intersections)
        {
            if (intersections.Count == 0) return new List<Polygon> { polygon };
            var readOnlyIntersectionLookup = MakeIntersectionLookupList(polygon, intersections);
            var usedIntersectionLookup = new HashSet<int>[readOnlyIntersectionLookup.Length];
            var polygons = new List<Polygon>();
            while (GetNextIntersection(readOnlyIntersectionLookup, usedIntersectionLookup, polygon.Lines, intersections, out var startIndex, out var currentEdge))
            {
                Polygon thispolygon = MakePolygonThroughIntersections(readOnlyIntersectionLookup, usedIntersectionLookup, intersections, startIndex, currentEdge);
                if (!thispolygon.IsPositive) thispolygon.Reverse();
                polygons.Add(thispolygon);
            }
            return polygons;
            //polygons = polygons.OrderByDescending(p => p.Area).ToList();
            //int i = 0;
            //while (polygons.Count > 1)
            //{
            //    var outerPolygon = polygons[i];
            //    var j = i + 1;
            //    while (j < polygons.Count)
            //    {
            //        var polygonRelationship = outerPolygon.GetPolygonRelationshipAndIntersections(polygons[j], out _);
            //        if (polygonRelationship == PolygonRelationship.BIsCompletelyInsideA || polygonRelationship == PolygonRelationship.Separated)
            //            j++;
            //        else
            //        {
            //            polygons[i] = outerPolygon = outerPolygon.Union(polygons[j]);
            //            polygons.RemoveAt(j);
            //        }
            //    }
            //    i++;
            //    if (i == polygons.Count) i = 0;
            //}
            //return polygons[0];
        }

        private static void RemoveIntersectionReferencesinLooKup(List<int>[] intersectionLookup, Polygon thispolygon)
        {
            throw new NotImplementedException();
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
        private static bool GetNextIntersection(List<int>[] readOnlyIntersectionLookup, HashSet<int>[] usedIntersectionLookup, List<PolygonSegment> lines, List<IntersectionData> intersections,
            out int intersectionIndex, out PolygonSegment currentEdge)
        {
            intersectionIndex = -1;
            for (int i = 0; i < readOnlyIntersectionLookup.Length; i++)
            {
                if (readOnlyIntersectionLookup[i] == null) continue;
                foreach (var index in readOnlyIntersectionLookup[i])
                {
                    if (usedIntersectionLookup[i] == null || !usedIntersectionLookup[i].Contains(index))
                    {
                        intersectionIndex = index;
                        currentEdge = lines[i];
                        return true;
                    }
                }
            }
            currentEdge = null;
            return false;
        }

        private static Polygon MakePolygonThroughIntersections(List<int>[] readonlyIntersectionLookup, HashSet<int>[] usedIntersectionLookup,
            List<IntersectionData> intersections, int indexOfNewIntersection, PolygonSegment currentEdge)
        {
            var newPath = new List<Vector2>();
            do
            {
                if (usedIntersectionLookup[currentEdge.IndexInList] == null)
                    usedIntersectionLookup[currentEdge.IndexInList] = new HashSet<int>();
                usedIntersectionLookup[currentEdge.IndexInList].Add(indexOfNewIntersection);
                var intersectionData = intersections[indexOfNewIntersection];
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
                    newPath.Add(currentEdge.ToPoint.Coordinates);
                    currentEdge = currentEdge.ToPoint.StartLine;
                    intersectionCoordinates = Vector2.Null;
                }
            } while (usedIntersectionLookup[currentEdge.IndexInList] == null
            || !usedIntersectionLookup[currentEdge.IndexInList].Contains(indexOfNewIntersection));
            return new Polygon(newPath, false);
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

        private static List<int>[] MakeIntersectionLookupList(Polygon polygon, List<IntersectionData> intersections)
        {
            var numLines = polygon.Lines.Count;
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
        public static Polygon Union(this Polygon polygonA, Polygon polygonB)
        {
            throw new NotImplementedException();
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