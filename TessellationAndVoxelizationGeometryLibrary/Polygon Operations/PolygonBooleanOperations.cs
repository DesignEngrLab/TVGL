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
    out List<PolygonSegmentIntersection> intersections)
        {
            intersections = new List<PolygonSegmentIntersection>();
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
            // instead of the prev. 3 lines a simpler solution would be the following line. However, we will need
            // to sort the vertices in the ArePointsInsidePolygon below. and - even though there is some expense
            // to setting up and checking the HashSet O(n) (since checking hashset n times), the above sort is faster
            // since the condition in line.XMin is avoided
            // var aLines = polygonA.Lines.OrderBy(line => line.XMin).ToList();
            var orderedBPoints = polygonB.Vertices.OrderBy(p => p.X).ToList();
            hashOfLines = polygonB.Lines.ToHashSet();
            var bLines = GetOrderedLines(orderedBPoints, hashOfLines);

            var aIndex = aLines.Length - 1;
            var bIndex = bLines.Length - 1;
            while (aIndex >= 0 && bIndex >= 0)
            {
                SetCurrentLineAndOtherLineArray(aLines, bLines, aIndex, bIndex, out var current, out var otherLines,
                    out var otherIndex);
                while (otherIndex >= 0 && current.XMin > otherLines[otherIndex].XMax)
                {
                    var other = otherLines[otherIndex];
                    var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                    if (segmentRelationship >= 0)
                        intersections.Add(new PolygonSegmentIntersection(current, other, intersection,
                            segmentRelationship));
                    otherIndex--;
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

        private static void SetCurrentLineAndOtherLineArray(PolygonSegment[] aLines, PolygonSegment[] bLines,
            int aIndex, int bIndex, out PolygonSegment currentLine, out PolygonSegment[] otherLines, out int otherIndex)
        {
            if (aLines[aIndex].XMax > bLines[bIndex].XMax)
            {
                currentLine = aLines[aIndex];
                otherLines = bLines;
                otherIndex = bIndex;
            }
            else
            {
                currentLine = bLines[bIndex];
                otherLines = aLines;
                otherIndex = aIndex;
            }
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

        public static List<PolygonSegmentIntersection> GetSelfIntersections(this Polygon polygonA)
        {
            var intersections = new List<PolygonSegmentIntersection>();
            var orderedLines = polygonA.Lines.OrderBy(line => line.XMin).ToList();
            var i = intersections.Count - 1;
            var current = orderedLines[i];
            while (i > 0)
            {
                var otherIndex = i - 1;
                while (otherIndex >= 0 && current.XMin > orderedLines[otherIndex].XMax)
                {
                    var other = orderedLines[otherIndex];
                    if (current.IsAdjacentTo(other)) continue;
                    var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                    if (segmentRelationship >= 0)
                        intersections.Add(new PolygonSegmentIntersection(current, other, intersection,
                            segmentRelationship));
                    otherIndex--;
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
    }
}