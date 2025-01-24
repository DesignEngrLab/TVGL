// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.IsInside.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Gets the winding number of a point with respect to a polygon. Like the winding number, but in terms
        /// of angle. So instead of "1" (as in 1 cycle), this returns 2 * Math.PI. The min and max angles are also
        /// provided.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="center"></param>
        /// <param name="minAngle"> will be between -pi and +pi</param>
        /// <param name="maxAngle">the maximum angle. it is not necessarily the minAngle + totalAnlge</param>
        /// <returns>totalAngle</returns>
        public static double GetWindingAngles(this Polygon polygon, Vector2 center, out double minAngle, out double maxAngle)
            => polygon.Path.GetWindingAngles(center, true, out minAngle, out maxAngle);

        /// <summary>
        /// Gets the winding number of a point with respect to a polygon. Like the winding number, but in terms
        /// of angle. So instead of "1" (as in 1 cycle), this return 2 * Math.PI. The min and max angles are also
        /// provided.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="center"></param>
        /// <param name="closedPath">Should the first point be repeated at the end to represent a closed path.</param>
        /// <param name="minAngle"> will be between -pi and +pi</param>
        /// <param name="maxAngle">the maximum angle. it is not necessarily the minAngle + totalAnlge</param>
        /// <returns>totalAngle</returns>
        public static double GetWindingAngles(this IEnumerable<Vector2> coords, Vector2 center,
            bool closedPath, out double minAngle, out double maxAngle)
        {
            var startPoint = coords.First();
            var prevVector = startPoint - center;
            var angle = Math.Atan2(prevVector.Y, prevVector.X);
            var startAngle = angle;
            minAngle = angle;
            maxAngle = angle;
            var points = closedPath ? coords.Skip(1).Concat([startPoint]) : coords.Skip(1);
            foreach (var coord in points)
            {
                var nextVector = coord - center;
                var angleDelta = Math.Atan2(prevVector.Cross(nextVector), prevVector.Dot(nextVector));
                angle += angleDelta;
                if (minAngle > angle) minAngle = angle;
                if (maxAngle < angle) maxAngle = angle;
                prevVector = nextVector;
            }
            var totalAngle = angle - startAngle;
            while (minAngle < -Math.PI)
            {
                minAngle += Math.Tau;
                maxAngle += Math.Tau;
            }
            while (minAngle > Math.PI)
            {
                minAngle -= Math.Tau;
                maxAngle -= Math.Tau;
            }
            return Math.Abs(totalAngle);
        }


        /// <summary>
        /// Returns true if the outer polygon encompasses the inner one. This is a quick check involving only
        /// one point on the inner. It does not check if the two polygons intersect
        /// </summary>
        /// <param name="outer">The outer polygon.</param>
        /// <param name="onlyTopOuterPolygon">if set to <c>true</c> only top outer polygon is checked and none of the innner polygons.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="onlyTopInnerPolygon">if set to <c>true</c> [only top inner polygon].</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <returns>Polygon.</returns>
        public static bool? IsNonIntersectingPolygonInside(this Polygon outer, bool onlyTopOuterPolygon, Polygon inner,
            bool onlyTopInnerPolygon, out bool onBoundary)
        {
            onBoundary = false;
            foreach (var subPolygon in inner.AllPolygons)
            {
                foreach (var vector2 in subPolygon.Path)
                {
                    if (!outer.IsPointInsidePolygon(onlyTopOuterPolygon, vector2, out var thisPointOnBoundary))
                        // negative has a point outside of positive. no point in checking other points
                        return false;
                    if (thisPointOnBoundary) onBoundary = true;
                    else if (onlyTopInnerPolygon)
                        return true;
                    else break;
                }
                if (onlyTopInnerPolygon) break;
            }
            return null; //all points are on boundary, so it is unclear if it is inside
        }

        /// <summary>
        /// Determines whether all the sortedVertices are inside the sortedEdges. This is used internal when the lists are already
        /// available instead of working with the polygons
        /// </summary>
        /// <param name="sortedEdges">The sorted edges.</param>
        /// <param name="sortedVertices">The sorted vertices.</param>
        /// <returns><c>true</c> if [is non intersecting polygon inside] [the specified sorted vertices]; otherwise, <c>false</c>.</returns>
        internal static bool? IsNonIntersectingPolygonInside(this IList<PolygonEdge> sortedEdges, Vertex2D[] sortedVertices)
        {
            var edgeIndex = 0;
            foreach (var vertex in sortedVertices)
            {
                var onBoundary = false;
                var numberAbove = 0;
                var numberBelow = 0;
                var canRemoveEarlierEdge = true;
                for (var i = edgeIndex; i < sortedEdges.Count; i++)
                {
                    if (sortedEdges[i].XMax.IsLessThanVectorX(vertex.Coordinates))
                    {
                        if (canRemoveEarlierEdge) edgeIndex = i;
                        continue;
                    }
                    canRemoveEarlierEdge = false;
                    if (sortedEdges[i].XMin.IsGreaterThanVectorX(vertex.Coordinates))
                        break;
                    switch (DetermineLineToPointVerticalReferenceType(vertex.Coordinates, sortedEdges[i]))
                    {
                        case VerticalLineReferenceType.On:
                            onBoundary = true;
                            break;
                        case VerticalLineReferenceType.Above:
                            numberAbove++;
                            break;
                        case VerticalLineReferenceType.Below:
                            numberBelow++;
                            break;
                    }
                    if (onBoundary) break;
                }
                if (onBoundary) continue;
                var insideAbove = numberAbove % 2 != 0;
                var insideBelow = numberBelow % 2 != 0;
                if (insideAbove != insideBelow)
                {
                    continue;
                    //throw new ArgumentException("In IsPointInsidePolygon, the point in question is surrounded by" +
                    //" an undetermined number of lines which makes it impossible to determined if inside.");
                }
                return insideAbove;
                //if (insideAbove) return polygon.IsPositive;
                //else return !polygon.IsPositive;
            }
            return null; //all points are on boundary, so it is unclear if it is inside
        }


        public static bool IsPointInsidePolygon(this Polygon polygon, bool onlyTopPolygon, Vector2 pointInQuestion, out bool isExactlyOnEdge)
        => IsPointInsidePolygon(polygon, onlyTopPolygon, new Vector2IP(pointInQuestion), out isExactlyOnEdge);
        internal static bool IsPointInsidePolygon(this Polygon polygon, bool onlyTopPolygon, Vector2IP pointInQuestion, out bool isExactlyOnEdge)
        {
            var insideTopPolygon = IsPointInsidePolygon(polygon, pointInQuestion, out isExactlyOnEdge);
            if (onlyTopPolygon) return polygon.IsPositive == insideTopPolygon;
            if (!insideTopPolygon) return !polygon.IsPositive;
            foreach (var inner in polygon.InnerPolygons)
                if (IsPointInsidePolygon(inner, pointInQuestion, out isExactlyOnEdge))
                    return true;
            return false;
        }

        internal static bool IsPointInsidePolygon(this Polygon polygon, Vector2IP pointInQuestion, out bool isExactlyOnEdge)
        {
            isExactlyOnEdge = false;
            if (polygon.MinXIP.IsGreaterThanVectorX(pointInQuestion)
                || polygon.MinYIP.IsGreaterThanVectorY(pointInQuestion)
                || polygon.MaxXIP.IsLessThanVectorX(pointInQuestion)
                || polygon.MaxYIP.IsLessThanVectorY(pointInQuestion))
                return false;
            var above = 0;
            var below = 0;
            foreach (var edge in polygon.Edges)
            {
                var type = DetermineLineToPointVerticalReferenceType(pointInQuestion, edge);
                if (type == VerticalLineReferenceType.On)
                {
                    isExactlyOnEdge = true;
                    return true;
                }
                else if (type == VerticalLineReferenceType.Above) above++;
                else below++;
            }
            var decider = above > below ? above : below; // go with the larger number
            return decider % 2 != 0;
        }


        /// <summary>
        /// Determines the type of the line to point vertical reference.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="line">The line.</param>
        /// <returns>VerticalLineReferenceType.</returns>
        private static VerticalLineReferenceType DetermineLineToPointVerticalReferenceType(Vector2IP point, PolygonEdge line)
        {
            if (point == line.FromPoint.Coordinates)
            {
                var signOfOverallDirection = line.Vector.X * line.FromPoint.EndLine.Vector.X;
                // this is a cusp - where the polygon line turns around at this point
                if (signOfOverallDirection < 0) return VerticalLineReferenceType.NotIntersecting;
                return VerticalLineReferenceType.On;
            }
            // this is basically the function PolygonEdge.YGivenX, but it is a little different here since check if line is horizontal cusp
            if (RationalIP.IsEqualVectorX(point, line.FromPoint.Coordinates) && RationalIP.IsEqualVectorX(point, line.ToPoint.Coordinates))
            {
                var fromIsBelow = RationalIP.IsGreaterThanVectorY(point, line.FromPoint.Coordinates);
                var toIsBelow = RationalIP.IsGreaterThanVectorY(point, line.ToPoint.Coordinates);
                if (fromIsBelow != toIsBelow) return VerticalLineReferenceType.On;
                if (fromIsBelow) return VerticalLineReferenceType.Below;
                return VerticalLineReferenceType.Above;
            }

            if (RationalIP.IsLessThanVectorX(line.FromPoint.Coordinates, point) == RationalIP.IsLessThanVectorX(line.ToPoint.Coordinates, point))
                // if both true or both false then endpoints are on same side of point
                return VerticalLineReferenceType.NotIntersecting;

            var intersectionYValue = PGA2D.YValueGivenXOnLine(point.X, point.W, line.Normal);
            if (intersectionYValue.IsEqualVectorY(point))
                return VerticalLineReferenceType.On;
            if (intersectionYValue.IsGreaterThanVectorY(point))
                return VerticalLineReferenceType.Above;
            return VerticalLineReferenceType.Below;
        }

        /// <summary>
        /// Determines if a point is inside a polygon. The polygon can be positive or negative. In either case,
        /// the result is true is the polygon encloses the point. Additionally output parameters can be used to
        /// locate the closest line above or below the point.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="pointsInQuestion">The points in question.</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside polygon] [the specified point in question]; otherwise, <c>false</c>.</returns>
        internal static bool ArePointsInsidePolygon(this Polygon polygon, IEnumerable<Vertex2D> pointsInQuestion,
            out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var sortedLines = GetOrderedLines(polygon.OrderedXVertices);
            var sortedPoints = pointsInQuestion.OrderBy(pt => pt.X).ToList();
            return ArePointsInsidePolygonLines(sortedLines, sortedLines.Length, sortedPoints, out onBoundary, onBoundaryIsInside);
        }
        /// <summary>
        /// Ares the points inside polygon lines.
        /// </summary>
        /// <param name="sortedLines">The sorted lines.</param>
        /// <param name="numSortedLines">The number sorted lines.</param>
        /// <param name="sortedPoints">The sorted points.</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal static bool ArePointsInsidePolygonLines(IList<PolygonEdge> sortedLines, int numSortedLines, List<Vertex2D> sortedPoints,
            out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var evenNumberOfCrossings = true; // starting at zero. 
            var lineIndex = 0;
            onBoundary = false;
            foreach (var p in sortedPoints)
            {
                while (sortedLines[lineIndex].XMax.IsLessThanVectorX(p.Coordinates))
                {
                    lineIndex++;
                    if (lineIndex == numSortedLines) return false;
                }
                for (int i = lineIndex; i < numSortedLines; i++)
                {
                    var line = sortedLines[lineIndex];
                    if (line.XMin.IsGreaterThanVectorX(p.Coordinates)) break;
                    if (p.Coordinates == line.FromPoint.Coordinates || p.Coordinates == line.ToPoint.Coordinates)
                    {
                        onBoundary = true;
                        if (!onBoundaryIsInside) return false;
                    }
                    var lineYValue = PGA2D.YValueGivenXOnEdge(p.Coordinates.X, p.Coordinates.W, line, out var isBetweenEndPoints);
                    if (!isBetweenEndPoints) continue;
                    if (lineYValue.IsEqualVectorY(p.Coordinates))
                    {
                        onBoundary = true;
                        if (!onBoundaryIsInside) return false;
                    }
                    else if (lineYValue.IsGreaterThanVectorY(p.Coordinates))
                    {
                        evenNumberOfCrossings = !evenNumberOfCrossings;
                    }
                }
                if (evenNumberOfCrossings)
                    //then the number of lines above this are even (0, 2, 4), which means it's outside
                    return false;
            }
            return true;
        }


        /// <summary>
        /// Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        /// The polygon must not be self-intersecting but the direction of the polygon does not matter.
        /// Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside polygon] [the specified point in question]; otherwise, <c>false</c>.</returns>
        [Obsolete("Polygon methods should use the Polygon class for improved robustness and speed.")]
        public static bool IsPointInsidePolygon(this List<Vector2> path, Vector2 pointInQuestion, bool onBoundaryIsInside = false)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
                                         //Check if the point is the same as any of the polygon's points
            var tolerance = Constants.BaseTolerance;
            var polygonIsLeftOfPoint = false;
            var polygonIsRightOfPoint = false;
            var polygonIsAbovePoint = false;
            var polygonIsBelowPoint = false;
            foreach (var point in path)
            {
                if (point.IsPracticallySame(pointInQuestion, tolerance))
                    return onBoundaryIsInside;
                if (point.X > qX) polygonIsLeftOfPoint = true;
                else if (point.X < qX) polygonIsRightOfPoint = true;
                if (point.Y > qY) polygonIsAbovePoint = true;
                else if (point.Y < qY) polygonIsBelowPoint = true;
            }
            if (!(polygonIsAbovePoint && polygonIsBelowPoint && polygonIsLeftOfPoint && polygonIsRightOfPoint))
                // this is like the AABB check. 
                return false;

            //2) Next, see how many lines are to the right of the point. This is inspired by the compact 7 lines of 
            //   code is from W. Randolph Franklin https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html . However,
            //   extra conditions are added for boundary at little to no computational expense.
            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            // this novel for-loop implementation of i and j is brilliant (cryptic clever compact and efficient). use this in other places!!
            {
                if (path[i].Y == path[j].Y) // line is horizontal
                {
                    // see if point has same Y value
                    if (path[i].Y == pointInQuestion.Y && (path[i].X >= pointInQuestion.X) != (path[j].X >= pointInQuestion.X))
                        return onBoundaryIsInside;
                    else return false;
                }
                else if ((path[i].Y > pointInQuestion.Y) != (path[j].Y > pointInQuestion.Y))
                // we can use strict inequalities here since we check the endpoints in loop above
                {   // so, the polygon line starts above (higher Y-value) the point and end below it (lower Y-value) 
                    // what is the x coordinate on the line at the point's Y value
                    var xCoordWithSameY = (path[j].X - path[i].X) * (pointInQuestion.Y - path[i].Y) / (path[j].Y - path[i].Y) + path[i].X;
                    if (pointInQuestion.X.IsPracticallySame(xCoordWithSameY, tolerance))
                        return onBoundaryIsInside;
                    else if (pointInQuestion.X < xCoordWithSameY)
                        inside = !inside; // it is inside if the number of lines to the right of the point is odd
                }
            }
            return inside;
        }

        /// <summary>
        /// Returns Y value given an X value
        /// </summary>
        /// <param name="x">The xval.</param>
        /// <param name="isBetweenEndPoints">if set to <c>true</c> [is between end points].</param>
        /// <returns>System.Double.</returns>
        internal static RationalIP FindYGivenX(PolygonEdge segment, Int128 x, Int128 w, out bool isBetweenEndPoints)
        {
            var vertLine = new Vector2IP(x, Int128.Zero, w);
            var point = PGA2D.PointAtLineAndPolyEdge(vertLine, segment, out _, out isBetweenEndPoints);
            return new RationalIP(point.Y, point.W);
        }

        /// <summary>
        /// Returns X value given a Y value
        /// </summary>
        /// <param name="y">The y.</param>
        /// <param name="isBetweenEndPoints">if set to <c>true</c> [is between end points].</param>
        /// <returns>System.Double.</returns>
        internal static RationalIP FindXGivenY(PolygonEdge segment, Int128 y, Int128 w, out bool isBetweenEndPoints)
        {
            var horzLine = new Vector2IP(Int128.Zero, y, w);
            var point = PGA2D.PointAtLineAndPolyEdge(horzLine, segment, out _, out isBetweenEndPoints);
            return new RationalIP(point.X, point.W);
        }

        #region Line Intersections with Polygon

        /// <summary>
        /// All the polygon intersection points along line. The line is swept in it's normal direction. This normal or swept direction
        /// is provided instead of the usual line direction so as to be clear about the direction of the sweep. Additionally, the
        /// perpendicular distance from the origin to the line (meeting at a right angle is provided to indicate where the increments start from.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="sweepLineDirection">The line direction.</param>
        /// <param name="perpendicularDistanceToLine">The line reference.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;Vector2&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Vector2[][] AllPolygonIntersectionPointsAlongLines(this IEnumerable<Polygon> polygons, Vector2 sweepLineDirection,
            double perpendicularDistanceToLine, double stepSize, out int firstIntersectingIndex)
        {
            sweepLineDirection = sweepLineDirection.Normalize();
            var sweepDirX = (Int128)(sweepLineDirection.X * Vector2IP.InitialW);
            var sweepDirY = (Int128)(sweepLineDirection.Y * Vector2IP.InitialW);
            var startingNumerator = (Int128)(perpendicularDistanceToLine * Vector2IP.InitialW);
            var stepSizeNumerator = (Int128)(stepSize * Vector2IP.InitialW);
            var intersections = AllPolygonIntersectionPointsAlongLines(polygons, sweepDirX, sweepDirY,
                startingNumerator, stepSizeNumerator,
                Vector2IP.InitialW, out firstIntersectingIndex);
            var result = new Vector2[intersections.Length][];
            for (int i = 0; i < intersections.Length; i++)
                result[i] = intersections[i].Select(x => x.AsVector2).ToArray();

            return result;
        }

        internal static Vector2IP[][] AllPolygonIntersectionPointsAlongLines(this IEnumerable<Polygon> polygons,
            Int128 sweepDirX, Int128 sweepDirY,
            Int128 startingSweepDistance, Int128 stepSize, Int128 commonW, out int firstIntersectingIndex)
        {
            var startingSweepRash = new RationalIP(startingSweepDistance, commonW);
            var stepRash = new RationalIP(stepSize, commonW);
            // as the above overload shows, this method is meant to accept the sweepDir components as normalized
            var referencePlane = new Vector2IP(sweepDirX, sweepDirY, -startingSweepDistance * commonW);
            var sortedPoints = new List<Vertex2D>();
            var comparer = new VertexSortedByDirection(referencePlane);
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.Vertices.OrderBy(x => x, comparer), comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return Array.Empty<Vector2IP[]>();
            }
            var currentLines = new HashSet<PolygonEdge>();
            var firstVertex = sortedPoints.First();
            var lastVertexDot = sortedPoints.Last().Coordinates.Dot2D(referencePlane);
            var nextDistance = firstVertex.Coordinates.Dot2D(referencePlane);
            firstIntersectingIndex = 1 + (int)((nextDistance - startingSweepRash) / stepRash).AsInt128;
            nextDistance = new RationalIP(startingSweepDistance + firstIntersectingIndex * stepSize, commonW);
            var lastIntersectingIndex = 1 + (int)((lastVertexDot - startingSweepRash) / stepRash).AsInt128;
            var intersections = new Vector2IP[lastIntersectingIndex - firstIntersectingIndex][];
            var pointIndex = 0;
            for (var outerIndex = 0; outerIndex < lastIntersectingIndex; outerIndex++)
            {
                var thisPoint = sortedPoints[pointIndex];
                // this while loop updates the current lines. 
                var thisPointDot = thisPoint.Coordinates.Dot2D(referencePlane);
                while (nextDistance >= thisPointDot)
                {
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }

                var intersects = new List<Vector2IP>(currentLines.Count);
                var scanLine = new Vector2IP(sweepDirX, sweepDirY, -(nextDistance * commonW).AsInt128);
                foreach (var edge in currentLines)
                {
                    var point = PGA2D.PointAtLineAndPolyEdge(referencePlane, edge, out _, out var isOnLine);
                    if (isOnLine)
                        intersects.Add(point);
                }
                intersections[outerIndex] = intersects.OrderBy(m => m).ToArray();
                nextDistance += stepRash;
            }
            return intersections;
        }

        /// <summary>
        /// Find all the polygon intersection points along vertical lines.
        /// Returns a list of double arrays. the double array values correspond to only the y-coordinates. the x-coordinates are determined
        /// by the input. x = startingXValue + (i+firstIntersectingIndex)*stepSize
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="startingXValue">The starting x value.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        public static double[][] AllPolygonIntersectionPointsAlongVerticalLines(this IEnumerable<Polygon> polygons, double startingXValue,
             double stepSize, out int firstIntersectingIndex)
        {
            var startingNumerator = (Int128)startingXValue * Vector2IP.InitialW;
            var stepSizeNumerator = (Int128)stepSize * Vector2IP.InitialW;
            var intersections = AllPolygonIntersectionPointsAlongVerticalLines(polygons, startingNumerator, stepSizeNumerator,
                Vector2IP.InitialW, out firstIntersectingIndex);
            var result = new double[intersections.Length][];
            for (int i = 0; i < intersections.Length; i++)
                result[i] = intersections[i].Select(x => x.AsDouble).ToArray();

            return result;

        }

        internal static RationalIP[][] AllPolygonIntersectionPointsAlongVerticalLines(this IEnumerable<Polygon> polygons,
            Int128 startingXValue, Int128 stepSize, Int128 commonW, out int firstIntersectingIndex)
        {
            var xEnd = polygons.Max(p => p.MaxXIP);
            var startingXRash = new RationalIP(startingXValue, commonW);
            var stepRash = new RationalIP(stepSize, commonW);
            var sortedPoints = new List<Vertex2D>();
            var comparer = new TwoDSortXFirst();
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.Vertices.OrderBy(x => x, comparer), comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return Array.Empty<RationalIP[]>();
            }
            var currentLines = new HashSet<PolygonEdge>();
            var firstVertex = sortedPoints.First();
            var nextDistance = new RationalIP(firstVertex.Coordinates.X, firstVertex.Coordinates.W);
            firstIntersectingIndex = 1 + (int)((nextDistance - startingXRash) / stepRash).AsInt128;
            nextDistance = new RationalIP(startingXValue + firstIntersectingIndex * stepSize, commonW);
            var lastIntersectingIndex = 1 + (int)((xEnd - startingXRash) / stepRash).AsInt128;
            var intersections = new RationalIP[lastIntersectingIndex - firstIntersectingIndex][];
            var pointIndex = 0;
            for (var outerIndex = 0; outerIndex < lastIntersectingIndex; outerIndex++)
            {
                var thisPoint = sortedPoints[pointIndex];
                // this while loop updates the current lines. 
                while (nextDistance.IsGreaterThanVectorX(thisPoint.Coordinates)
                    || nextDistance.IsEqualVectorX(thisPoint.Coordinates))
                {
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }

                var intersects = new List<RationalIP>(currentLines.Count);
                foreach (var line in currentLines)
                {
                    var yRash = FindYGivenX(line, nextDistance.Num, nextDistance.Den, out var isOnLine);
                    if (isOnLine)
                        intersects.Add(yRash);
                }
                intersections[outerIndex] = intersects.OrderBy(m => m).ToArray();
                nextDistance += stepRash;
            }
            return intersections;
        }

        /// <summary>
        /// Find all the polygon intersection points along horizontal lines.
        /// Returns a list of double arrays. the double array values correspond to only the x-coordinates. the y-coordinates are
        /// determined by the input. y = startingYValue + (i+firstIntersectingIndex)*stepSize
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="startingYValue">The starting y value.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        public static double[][] AllPolygonIntersectionPointsAlongHorizontalLines(this IEnumerable<Polygon> polygons,
            double startingYValue, double stepSize, out int firstIntersectingIndex)
        {
            var startingNumerator = (Int128)startingYValue * Vector2IP.InitialW;
            var stepSizeNumerator = (Int128)stepSize * Vector2IP.InitialW;
            var intersections = AllPolygonIntersectionPointsAlongHorizontalLines(polygons, startingNumerator, stepSizeNumerator,
                Vector2IP.InitialW, out firstIntersectingIndex);
            var result = new double[intersections.Length][];
            for (int i = 0; i < intersections.Length; i++)
                result[i] = intersections[i].Select(x => x.AsDouble).ToArray();

            return result;
        }

        internal static RationalIP[][] AllPolygonIntersectionPointsAlongHorizontalLines(this IEnumerable<Polygon> polygons,
            Int128 startingYValue, Int128 stepSize, Int128 commonW, out int firstIntersectingIndex)
        {
            var yEnd = polygons.Max(p => p.MaxYIP);
            var startingYRash = new RationalIP(startingYValue, commonW);
            var stepRash = new RationalIP(stepSize, commonW);
            var sortedPoints = new List<Vertex2D>();
            var comparer = new TwoDSortYFirst();
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.Vertices.OrderBy(x => x, comparer), comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return Array.Empty<RationalIP[]>();
            }
            var currentLines = new HashSet<PolygonEdge>();
            var firstVertex = sortedPoints.First();
            var nextDistance = new RationalIP(firstVertex.Coordinates.Y, firstVertex.Coordinates.W);
            firstIntersectingIndex = 1 + (int)((nextDistance - startingYRash) / stepRash).AsInt128;
            nextDistance = new RationalIP(startingYValue + firstIntersectingIndex * stepSize, commonW);

            var lastIntersectingIndex = 1 + (int)((yEnd - startingYRash) / stepRash).AsInt128;
            var intersections = new RationalIP[lastIntersectingIndex - firstIntersectingIndex][];
            var pointIndex = 0;
            for (var outerIndex = 0; outerIndex < lastIntersectingIndex; outerIndex++)
            {
                var thisPoint = sortedPoints[pointIndex];
                // this while loop updates the current lines. 
                while (nextDistance.IsGreaterThanVectorY(thisPoint.Coordinates)
                    || nextDistance.IsEqualVectorY(thisPoint.Coordinates))
                {
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }

                var intersects = new List<RationalIP>(currentLines.Count);
                foreach (var line in currentLines)
                {
                    var xRash = FindXGivenY(line, nextDistance.Num, nextDistance.Den, out var isOnLine);
                    if (isOnLine)
                        intersects.Add(xRash);
                }
                intersections[outerIndex] = intersects.OrderBy(m => m).ToArray();
                nextDistance += stepRash;
            }
            return intersections;
        }

        /// <summary>
        /// Find all the polygon intersection points along a single horizontal line.
        /// Returns a list of intersections from lowest x to highest x along with the vertex that the line starts from.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="YValue">The y value.</param>
        /// <returns>SortedList&lt;System.Double, Vertex2D&gt;.</returns>
        public static SortedList<double, PolygonEdge> AllPolygonIntersectionPointsAlongHorizontal(this Polygon polygon, double YValue)
        {
            var yRational = new RationalIP(YValue);
            var intersectsRash = AllPolygonIntersectionPointsAlongHorizontal(polygon, yRational);
            return new SortedList<double, PolygonEdge>(intersectsRash.ToDictionary(x => x.Key.AsDouble, x => x.Value));
        }

        internal static SortedList<RationalIP, PolygonEdge> AllPolygonIntersectionPointsAlongHorizontal(this Polygon polygon, RationalIP yRational)
        {
            var result = new SortedList<RationalIP, PolygonEdge>(new NoEqualRationalSort());
            foreach (var poly in polygon.AllPolygons)
            {
                var startVertex = poly.Vertices[0];
                var current = startVertex;
                var currentIsBelow = yRational.IsLessThanVectorY(startVertex.Coordinates);
                do
                {
                    var line = current.StartLine;
                    var next = line.ToPoint;
                    var nextIsBelow = yRational.IsLessThanVectorY(next.Coordinates);
                    if (nextIsBelow != currentIsBelow)
                        result.Add(FindXGivenY(line, yRational.Num, yRational.Den, out _), line);
                    current = next;
                    currentIsBelow = nextIsBelow;
                } while (current != startVertex);
            }
            return result;
        }
        /// <summary>
        /// Find all the polygon intersection points along a single vertical line.
        /// Returns a list of intersections from lowest y to highest y along with the vertex that the line starts from.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="YValue">The y value.</param>
        /// <returns>SortedList&lt;System.Double, Vertex2D&gt;.</returns>
        public static SortedList<double, PolygonEdge> AllPolygonIntersectionPointsAlongVertical(this Polygon polygon, double XValue)
        {
            var xRational = new RationalIP(XValue);
            var intersectsRash = AllPolygonIntersectionPointsAlongVertical(polygon, xRational);
            return new SortedList<double, PolygonEdge>(intersectsRash.ToDictionary(x => x.Key.AsDouble, x => x.Value));
        }

        internal static SortedList<RationalIP, PolygonEdge> AllPolygonIntersectionPointsAlongVertical(this Polygon polygon, RationalIP xRational)
        {
            var result = new SortedList<RationalIP, PolygonEdge>(new NoEqualRationalSort());
            foreach (var poly in polygon.AllPolygons)
            {
                var startVertex = poly.Vertices[0];
                var current = startVertex;
                var currentIsToTheRight = xRational.IsLessThanVectorX(current.Coordinates);
                do
                {
                    var line = current.StartLine;
                    var next = line.ToPoint;
                    var nextIsToTheRight = xRational.IsLessThanVectorX(next.Coordinates);
                    if (nextIsToTheRight != currentIsToTheRight)
                        result.Add(FindYGivenX(line, xRational.Num, xRational.Den, out _), line);
                    current = next;
                    currentIsToTheRight = nextIsToTheRight;
                } while (current != startVertex);
            }
            return result;
        }
        #endregion

        /// <summary>
        /// Gets the polygon relationship of PolygonA to PolygonB and the intersections between them.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <returns>PolygonRelationship.</returns>
        public static PolygonInteractionRecord GetPolygonInteraction(this Polygon polygonA, Polygon polygonB)
        {
            var interactionRecord = new PolygonInteractionRecord(polygonA, polygonB);
            if (interactionRecord.Relationship == ABRelationships.Equal) return interactionRecord;
            // this would happen when the function detcts that polygonA and polygonB are the same
            var visited = new bool[interactionRecord.numPolygonsInA * interactionRecord.numPolygonsInB];
            RecursePolygonInteractions(polygonA, polygonB, interactionRecord, visited);
            interactionRecord.DefineOverallInteractionFromFinalListOfSubInteractions();
            return interactionRecord;
        }
        /// <summary>
        /// Recurse down the polygon trees to find the interactions.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interactionRecord">The interaction record.</param>
        /// <param name="visited">The visited.</param>
        /// <returns>PolygonRelationship.</returns>
        private static void RecursePolygonInteractions(Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
            bool[] visited)
        {
            var index = interactionRecord.findLookupIndex(polygonA, polygonB);
            if (visited[index]) return;
            var relationship = GetSinglePolygonRelationshipAndIntersections(polygonA, polygonB, out var localIntersections);
            interactionRecord.SetRelationshipBetween(index, relationship);
            interactionRecord.IntersectionData.AddRange(localIntersections);
            visited[index] = true;
            if ((relationship & PolyRelInternal.EqualButOpposite) == 0 &&
                (int)relationship >= (int)PolyRelInternal.AInsideB)
            {
                if ((relationship & PolyRelInternal.AInsideB) != 0 && (relationship & PolyRelInternal.BInsideA) == 0)
                {  // if A is inside B then all subpolygons to A are also Inside B and we don't need to check with recursing method
                    foreach (var innerPolyA in polygonA.InnerPolygons)
                        interactionRecord.SetRelationshipBetween(interactionRecord.findLookupIndex(innerPolyA, polygonB),
                            PolyRelInternal.AInsideB);
                }
                else // we need to check the inner polygons of A with this B polygon
                {
                    foreach (var innerPolyA in polygonA.InnerPolygons)
                        RecursePolygonInteractions(innerPolyA, polygonB, interactionRecord, visited);
                }
                if ((relationship & PolyRelInternal.BInsideA) != 0 && (relationship & PolyRelInternal.AInsideB) == 0)
                {  // if B is inside A then all subpolygons of B are also Inside A and we don't need to check with recursing method
                    foreach (var innerPolyB in polygonB.InnerPolygons)
                        interactionRecord.SetRelationshipBetween(interactionRecord.findLookupIndex(polygonA, innerPolyB),
                            PolyRelInternal.BInsideA);
                }
                else // we need to check the inner polygons of B with this A polygon
                {
                    foreach (var innerPolyB in polygonB.InnerPolygons)
                        RecursePolygonInteractions(polygonA, innerPolyB, interactionRecord, visited);
                }
            }
        }


        /// <summary>
        /// Gets the polygon relationship of PolygonA to PolygonB and the intersections between them.
        /// </summary>
        /// <param name="subPolygonA">The polygon a.</param>
        /// <param name="subPolygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonRelationship: but not all possibilities can be returned from here. It returns: Intersection,
        /// BIsCompletelyInsideA, BIsInsideAButEdgesTouch, BIsInsideAButVerticesTouch,
        /// AIsCompletelyInsideB,  AIsInsideBButEdgesTouch, AIsInsideBButVerticesTouch,
        /// Separated, SeparatedButEdgesTouch, SeparatedButVerticesTouch</returns>
        private static PolyRelInternal GetSinglePolygonRelationshipAndIntersections(this Polygon subPolygonA, Polygon subPolygonB,
            out List<SegmentIntersection> intersections)
        {
            intersections = new List<SegmentIntersection>();
            //As a first check, determine if the axis aligned bounding boxes overlap. If not, then we can
            // safely return that the polygons are separated.
            if (subPolygonA.MinXIP > subPolygonB.MaxXIP ||
                subPolygonA.MaxXIP < subPolygonB.MinXIP ||
                subPolygonA.MinYIP > subPolygonB.MaxYIP ||
                subPolygonA.MaxYIP < subPolygonB.MinYIP)
                return PolyRelInternal.Separated;

            subPolygonA.MakePolygonEdgesIfNonExistent();
            subPolygonB.MakePolygonEdgesIfNonExistent();
            //Else, we need to check for intersections between all lines of the two
            // To avoid an n-squared check (all of A's lines with all of B's), we sort the lines by their XMin
            // value. Instead of directly sorting the Lines, which will have many repeat XMin values (since every
            // pair of elsewhere so one it is done for a polygon, it is a good idea to store it.
            // the next function (GetOrderedLines),  requires that lines and vertices be properly connected
            var aLines = GetOrderedLines(subPolygonA.OrderedXVertices);
            //repeat for the lines in B
            var bLines = GetOrderedLines(subPolygonB.OrderedXVertices);

            var aIndex = 0;
            var bIndex = 0;
            while (aIndex < aLines.Length && bIndex < bLines.Length) // this while loop increments both B lines and A lines
            {
                var aXMin = aLines[aIndex].XMin;
                var bXMin = bLines[bIndex].XMin;
                if (aXMin < bXMin) // if the next A-line is lower compare it to all B-lines
                {
                    var aLine = aLines[aIndex++];
                    var aXMax = aLine.XMax;
                    var localBIndex = bIndex; //the localBIndex is incremented in the following loop, but we
                                              //need to come back to the main bIndex above
                    while (aXMax >= bXMin)
                    {
                        // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        AddIntersectionBetweenLines(aLine, bLines[localBIndex++], intersections);
                        if (localBIndex >= bLines.Length) break;
                        bXMin = bLines[localBIndex].XMin;
                    }
                }
                else
                {
                    var bLine = bLines[bIndex++];
                    var bXMax = bLine.XMax;
                    var localAIndex = aIndex;
                    while (bXMax >= aXMin)
                    {
                        AddIntersectionBetweenLines(aLines[localAIndex++], bLine, intersections);
                        if (localAIndex >= aLines.Length) break;
                        aXMin = aLines[localAIndex].XMin;
                    }
                }
            }


            var relationship = PolyRelInternal.Separated;
            if (intersections.Count == 0 || intersections.All(c => c.Relationship == SegmentRelationship.NoOverlap))
            // since there are no intersections all the nodeTypes of a vertices of a polygon should be the same
            // and they are either Inside or Outside. There can't be any OnBorder as these would have registered as intersections as well
            // however inner polygons could exhibit difference values than the outer (consider edge case: nested squares). For example,
            // A encompasses B but a hole in B is smaller and fits inside hole of A. This should be registered as Intersecting
            {
                if (subPolygonA.HasABoundingBoxThatEncompasses(subPolygonB) && IsNonIntersectingPolygonInside(aLines, subPolygonB.OrderedXVertices) == true)
                {
                    if (!subPolygonA.IsPositive) relationship |= PolyRelInternal.InsideHole;
                    return relationship | PolyRelInternal.BInsideA;
                }
                if (subPolygonB.HasABoundingBoxThatEncompasses(subPolygonA) && IsNonIntersectingPolygonInside(bLines, subPolygonA.OrderedXVertices) == true)
                {
                    if (!subPolygonB.IsPositive) relationship |= PolyRelInternal.InsideHole;
                    return relationship | PolyRelInternal.AInsideB;
                }
                return PolyRelInternal.Separated;
            }

            if (intersections.Any(intersection => intersection.CollinearityType != CollinearityTypes.None))
                relationship |= PolyRelInternal.CoincidentEdges;
            if (intersections.Any(intersection => intersection.WhereIntersection != WhereIsIntersection.Intermediate))
                relationship |= PolyRelInternal.CoincidentVertices;

            var isEqual = subPolygonA.PathArea.IsPracticallySame(subPolygonB.PathArea, Constants.BaseTolerance);
            if (isEqual)
                foreach (var intersect in intersections)
                {
                    if (intersect.CollinearityType != CollinearityTypes.Same)
                    {
                        isEqual = false;
                        break;
                    }
                }
            if (isEqual)
                return relationship | PolyRelInternal.Equal;
            var isOpposite = true;
            foreach (var intersect in intersections)
            {
                if (intersect.CollinearityType != CollinearityTypes.Opposite)
                {
                    isOpposite = false;
                    break;
                }
            }
            if (isOpposite)
            {
                if (subPolygonA.Area.IsPracticallySame(-subPolygonB.Area, Constants.BaseTolerance) && subPolygonA.MinXIP == subPolygonB.MinXIP
                    && subPolygonA.MinYIP == subPolygonB.MinYIP && subPolygonA.MaxXIP == subPolygonB.MaxXIP
                    && subPolygonA.MaxYIP == subPolygonB.MaxYIP)
                    return relationship | PolyRelInternal.EqualButOpposite;
                return relationship;
            }

            if (intersections.Any(intersection => intersection.Relationship == SegmentRelationship.CrossOver_BOutsideAfter ||
            intersection.Relationship == SegmentRelationship.CrossOver_AOutsideAfter ||
            (intersection.Relationship == SegmentRelationship.DoubleOverlap && (subPolygonA.IsPositive || subPolygonB.IsPositive))))
                return relationship | PolyRelInternal.EdgesCross | PolyRelInternal.Intersection;

            if (intersections.All(intersection => intersection.Relationship == SegmentRelationship.NoOverlap))
            {   // all intersections of  type, NoOverlap
                if (subPolygonA.IsPositive == subPolygonB.IsPositive)
                    return relationship | PolyRelInternal.Separated;
                // then one is positive and the other is negative
                if (subPolygonA.IsPositive) // therefore subPolygonB is a hole
                    return relationship | PolyRelInternal.InsideHole | PolyRelInternal.AInsideB;
                else // then B is positive and A is a negative polygon/hole
                    return relationship | PolyRelInternal.InsideHole | PolyRelInternal.BInsideA;
            }

            // given the previous conditions, we can only reach this point if ALL intersections are of type NoOverlap or Enclose
            // and there must be at least one Enclose
            var atLeastOneAEncloseB = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.CrossOver_AOutsideAfter);
            var atLeastOneBEncloseA = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.CrossOver_BOutsideAfter);

            if (atLeastOneAEncloseB && atLeastOneBEncloseA)
                return relationship | PolyRelInternal.EdgesCross | PolyRelInternal.Intersection;

            if (subPolygonA.IsPositive != subPolygonB.IsPositive)
                return relationship | PolyRelInternal.Separated;
            // should "InsideHole" be included in the returns below? I don't think so. Since the previous condition failed, then both
            // are positive or both are negative. The implications of "inside hole" are only meaningful when the shallow polygons can be 
            // considered as non-overlapping. if both postive or both negative then there is overlap in their material
            if (subPolygonA.IsPositive == atLeastOneAEncloseB)
                return relationship | PolyRelInternal.BInsideA;

            //if (atLeastOneBEncloseA && subPolygonB.IsPositive)
            return relationship | PolyRelInternal.AInsideB;

            //else throw new Exception("debug? no default polygon relationship found.")
        }

        /// <summary>
        /// Determines if Two polygon line segments intersect. Because they are part of a polygon, it is decided to make the
        /// fromPoint Inclusive, and the toPoint exclusive. Thus, if lines touch at their endpoints, it is only recorded
        /// if both points are from points. Also no "close" operations are used (e.g. IsPracticallySame). Because the method is
        /// intended to be invoked for all lines on the polygon, this prevents an intersection from being caught by multiple lines,
        /// and makes the methods simpler (easier to debug and edit) and quicker.
        /// </summary>
        /// <param name="lineA">The line a.</param>
        /// <param name="lineB">The line b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="possibleDuplicates">The possible duplicates.</param>
        /// <param name="numSigDigs">The number sig digs.</param>
        /// <param name="needToRoundA">if set to <c>true</c> [need to round a].</param>
        /// <param name="needToRoundB">if set to <c>true</c> [need to round b].</param>
        /// <returns>PolygonSegmentRelationship.</returns>
        internal static bool AddIntersectionBetweenLines(PolygonEdge lineA, PolygonEdge lineB,
            List<SegmentIntersection> intersections)
        {
            // first check if bounding boxes overlap. Actually, we don't need to check the x values (lineA.XMax < lineB.XMin || 
            // lineB.XMax < lineA.XMin)- this is already known from the calling function and the way it calls based on sorted x values
            if (lineA.YMax < lineB.YMin || lineB.YMax < lineA.YMin)
                // the two lines do not touch since their bounding boxes do not overlap
                return false;
            // okay, so bounding boxes DO overlap
            var sameStart = lineA.FromPoint.Coordinates == lineB.FromPoint.Coordinates;
            // first, see if the two lines are the same
            if (sameStart && lineA.ToPoint.Coordinates == lineB.ToPoint.Coordinates)
            {
                intersections.Add(new SegmentIntersection(lineA, lineB, lineA.FromPoint.Coordinates, SegmentRelationship.Equal,
                    WhereIsIntersection.BothStarts, CollinearityTypes.Same));
                return true;
            }
            // next, see if the edges have the same carrier line. Basically, check if the line normals are the same
            // we could call the VectorIP.Equals method, but we will unpack it since we will also want to check if lines
            // are parallel
            var parallel = lineA.Normal.X * lineB.Normal.Y == lineB.Normal.X * lineA.Normal.Y;
            var sameCarrier = parallel && (lineA.Normal.W == lineB.Normal.W
                || (lineA.Normal.X * lineB.Normal.W == lineB.Normal.X * lineA.Normal.W
                    && lineA.Normal.Y * lineB.Normal.W == lineB.Normal.Y * lineA.Normal.W));
            if (parallel && !sameCarrier)
                return false;
            if (sameCarrier)
                // This means that the two lines are collinear and we need to check if they overlap.
                return AddCollinearIntersection(lineA, lineB, intersections, sameStart);
            var cross = lineA.Normal.Cross(lineB.Normal);
            Vector2IP intersectionCoordinates;
            WhereIsIntersection where;
            //first a quick check to see if points are the same
            if (sameStart)
            {
                intersectionCoordinates = lineA.FromPoint.Coordinates;
                where = WhereIsIntersection.BothStarts;
            }
            else
            {
                intersectionCoordinates = PGA2D.PointAtPolyEdgeIntersection(lineA, lineB, out var t1, out var onSegment1, out var t2, out var onSegment2);
                where = t1.Num == 0 ? WhereIsIntersection.AtStartOfA : t2.Num == 0 ? WhereIsIntersection.AtStartOfB : WhereIsIntersection.Intermediate;
            }
            if (where == WhereIsIntersection.Intermediate)
                intersections.Add(new SegmentIntersection(lineA, lineB, intersectionCoordinates,
                cross.W > 0 ? SegmentRelationship.CrossOver_AOutsideAfter : SegmentRelationship.CrossOver_BOutsideAfter, WhereIsIntersection.Intermediate, CollinearityTypes.None));
            else
            {
                SegmentRelationship segmentRelationship = GetSegmentRelationship(lineA.Vector,-lineA.FromPoint.EndLine.Vector,
                    lineB.Vector, -lineB.FromPoint.EndLine.Vector, cross);
                intersections.Add(new SegmentIntersection(lineA, lineB, lineA.FromPoint.Coordinates,
                    segmentRelationship, where, CollinearityTypes.None));
            }
            return true;
        }

        private static SegmentRelationship GetSegmentRelationship(Vector2IP aOut, Vector2IP aIn, Vector2IP bOunt, Vector2IP bIn, Vector2IP cross)
        {
            var angleAOut = Constants.
        }

        private static bool AddCollinearIntersection(PolygonEdge lineA, PolygonEdge lineB, List<SegmentIntersection> intersections, bool sameStart)
        {
            double prevACrossPrevB, lineACrossPrevB, prevACrossLineB;
            Vector2 previousAVector, previousBVector;
            // based on where the intersection happens, we can quicken the calculation of these
            if (where == WhereIsIntersection.AtStartOfA)
            { //then lineB and prevB are the same
                var previousALine = edgeA.FromPoint.EndLine;

                previousAVector = previousALine.Vector;
                previousBVector = bVector;
                lineACrossPrevB = lineACrossLineB;
                prevACrossLineB = prevACrossPrevB = previousAVector.Cross(previousBVector);
            }
            else if (where == WhereIsIntersection.AtStartOfB)
            { //then lineA and prevA are the same
                previousAVector = aVector;
                var previousBLine = edgeB.FromPoint.EndLine;

                previousBVector = previousBLine.Vector;
                prevACrossLineB = lineACrossLineB;
                lineACrossPrevB = prevACrossPrevB = previousAVector.Cross(previousBVector);
            }
            else // then where == BothLinesStart
            {
                var previousALine = edgeA.FromPoint.EndLine;

                previousAVector = previousALine.Vector;
                var previousBLine = edgeB.FromPoint.EndLine;

                previousBVector = previousBLine.Vector;
                prevACrossLineB = previousAVector.Cross(bVector);
                lineACrossPrevB = aVector.Cross(previousBVector);
                prevACrossPrevB = previousAVector.Cross(previousBVector);
            }

            if (lineACrossLineB.IsNegligible() || Math.Abs(lineACrossLineB) / (aVector.Length() * bVector.Length()) < unitCrossIsParallel)
                lineACrossLineB = 0;
            if (prevACrossPrevB.IsNegligible() || Math.Abs(prevACrossPrevB) / (previousAVector.Length() * previousBVector.Length()) < unitCrossIsParallel)
                prevACrossPrevB = 0;
            if (lineACrossPrevB.IsNegligible() || Math.Abs(lineACrossPrevB) / (aVector.Length() * previousBVector.Length()) < unitCrossIsParallel)
                lineACrossPrevB = 0;
            if (prevACrossLineB.IsNegligible() || Math.Abs(prevACrossLineB) / (previousAVector.Length() * bVector.Length()) < unitCrossIsParallel)
                prevACrossLineB = 0;


            if (lineACrossLineB == 0 && prevACrossPrevB == 0 && lineACrossPrevB == 0 && prevACrossLineB == 0)
            {
                if (aVector.Dot(bVector) > 0)
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.Same);
                return (SegmentRelationship.Abutting, CollinearityTypes.Opposite);
            }
            // most restrictive is when both lines are parallel
            if (lineACrossLineB == 0 && prevACrossPrevB == 0)
            {
                var lineADotLineB = aVector.Dot(bVector);
                var prevADotPrevB = previousAVector.Dot(previousBVector);
                if (lineADotLineB > 0 && prevADotPrevB > 0)
                    //case 16
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.Same);
                if (lineADotLineB < 0 && prevADotPrevB < 0 && lineACrossPrevB != 0)
                // a rare version of case 5 or 6 where the lines enter and leave the point on parallel lines, but
                // there is no collinearity! A's cross product of the corner would be the same as B's. If this corner
                // cross is positive/convex, then no overlap. if concave, then double overlap
                {
                    var cross = previousAVector.Cross(aVector);
                    if (cross.IsNegligible())
                        return (SegmentRelationship.Abutting, CollinearityTypes.Opposite);
                    return (cross > 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                        CollinearityTypes.None);
                }
                if (prevADotPrevB < 0) // then lineADotLineB would be positive, and polygons were heading
                                       // right to each other on parallel lines before joining. this is a rare case 7 or 8
                    return (previousAVector.Cross(aVector) > 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter,
                        CollinearityTypes.After);
                if (lineADotLineB < 0) // then prevADotPrevB would be positive. the polygon diverges in 
                                       // opposite parallel directions. this is a rare case 9 or 10
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter,
                             CollinearityTypes.Before);
            }
            if (lineACrossPrevB == 0 && prevACrossLineB == 0)
            {
                var lineADotPrevB = aVector.Dot(previousBVector);
                var prevADotLineB = previousAVector.Dot(bVector);
                if (lineADotPrevB < 0 && prevADotLineB < 0)  // case 15
                    return (SegmentRelationship.Abutting, CollinearityTypes.Opposite);
                if (lineADotPrevB > 0 && prevADotLineB > 0)  // a very unusual case (although it shows up in the chunky polygon
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter,
                        CollinearityTypes.None);
                if (lineADotPrevB > 0) // then prevADotLineB would be negative. 
                                       // calculate if polygon A's corner is convex or concave. if convex then case 11 (no overlap) if concave then double (case 12)
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                        CollinearityTypes.ABeforeBAfter);
                if (prevADotLineB > 0) // then lineADotPrevB would be negative, 
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                             CollinearityTypes.AAfterBBefore);
            }
            // the remaining conditions require these (remember: positive = convex, negative = concave)
            var aCornerCross = previousAVector.Cross(aVector);
            var bCornerCross = previousBVector.Cross(bVector);

            // now to check if just one of these is zero
            if (lineACrossLineB == 0 && aVector.Dot(bVector) > 0)
            {   // if the dot product is less than zero than it'll be pulled in to conditions below like no overlap. So this is Case 7 & 8
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.After);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.CrossOver_BOutsideAfter, CollinearityTypes.After);
                return (prevACrossPrevB > 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.After);
            }
            if (prevACrossPrevB == 0 && previousAVector.Dot(previousBVector) > 0)
            {   //like the previous condition if this hasn't been captured by the aboe then lineACrossLineB !=0. So this is Case 9 & 10
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.Before);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.CrossOver_BOutsideAfter, CollinearityTypes.Before);
                return (lineACrossLineB > 0 ? SegmentRelationship.CrossOver_AOutsideAfter : SegmentRelationship.CrossOver_BOutsideAfter, CollinearityTypes.Before);
            }
            if (prevACrossLineB == 0 && previousAVector.Dot(bVector) < 0)
            {   //like the previous condition if this hasn't been captured by the above then lineACrossPrevB !=0. So this is Case 11 & 12
                if (aCornerCross > 0 && bCornerCross > 0) return (SegmentRelationship.NoOverlap, CollinearityTypes.ABeforeBAfter);
                if (aCornerCross < 0 && bCornerCross < 0) return (SegmentRelationship.DoubleOverlap, CollinearityTypes.ABeforeBAfter);
                return (lineACrossPrevB > 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap, CollinearityTypes.ABeforeBAfter);
            }
            if (lineACrossPrevB == 0 && aVector.Dot(previousBVector) < 0)
            {   //like the previous condition if this hasn't been captured by the above then prevACrossLineB !=0. So this is Case 13 & 14
                if (aCornerCross > 0 && bCornerCross > 0) return (SegmentRelationship.NoOverlap, CollinearityTypes.AAfterBBefore);
                if (aCornerCross < 0 && bCornerCross < 0) return (SegmentRelationship.DoubleOverlap, CollinearityTypes.AAfterBBefore);
                return (prevACrossLineB > 0 ? SegmentRelationship.DoubleOverlap : SegmentRelationship.NoOverlap, CollinearityTypes.AAfterBBefore);
            }
            // that's it for the collinear cases - now non-collinear Cases 1 to 6
            // In the remainder of this method, we determine what the overlap of regions is at this point
            // when A corner is convex (aCornerCross>=0), then we check if the two cross products made with the
            // a given b line are BOTH positive - meaning that the b line is between the A lines.
            // When A's corner is concave, we check for the opposite convex angle of A - to see if the B line is between those
            // that's the easy part! getting the changes in sign correct makes it more complicated.
            var lineBIsInsideA = (aCornerCross > 0 && lineACrossLineB > 0 && prevACrossLineB > 0) ||
                                 (aCornerCross <= 0 && !(lineACrossLineB < 0 && prevACrossLineB < 0));
            // this expression is the same, but the previous B vector is into the corner and thus, we need to negate it (or rather just check for negative)
            var prevLineBIsInsideA = (aCornerCross > 0 && lineACrossPrevB < 0 && prevACrossPrevB < 0) ||
                                     (aCornerCross <= 0 && !(lineACrossPrevB > 0 && prevACrossPrevB > 0));
            // we actually have to do the same with lineB - it's not enough to know if A is inside B
            var lineAIsInsideB = (bCornerCross > 0 && lineACrossLineB < 0 && lineACrossPrevB < 0) ||
                                 (bCornerCross <= 0 && !(lineACrossLineB > 0 && lineACrossPrevB > 0));
            var prevLineAIsInsideB = (bCornerCross > 0 && prevACrossLineB > 0 && prevACrossPrevB > 0) ||
                                     (bCornerCross <= 0 && !(prevACrossLineB < 0 && prevACrossPrevB < 0));
            // in the remaining conditions there are 16 possible combinations of the four booleans: lineBIsInsideA-prevLineBIsInsideA--lineAIsInsideB-prevLineAIsInsideB
            // first off, if they are all true, then it clearly is a "double overlap"
            if (lineBIsInsideA && prevLineBIsInsideA && lineAIsInsideB && prevLineAIsInsideB)
                // this is case 5
                return (SegmentRelationship.DoubleOverlap, CollinearityTypes.None);
            else if (lineBIsInsideA && prevLineAIsInsideB)
                // case 1 
                return (SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.None);
            else if (prevLineBIsInsideA && lineAIsInsideB)
                // case 2
                return (SegmentRelationship.CrossOver_BOutsideAfter, CollinearityTypes.None);
            // now check if A lines are inside b region
            else if (lineAIsInsideB && prevLineAIsInsideB)
                // case 3
                return (SegmentRelationship.CrossOver_BOutsideAfter, CollinearityTypes.None);
            // if only a positive on the A side then A encompasses B
            else if (lineBIsInsideA && prevLineBIsInsideA)
                // case 4
                return (SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.None);
            // else Case 6
            else return (SegmentRelationship.NoOverlap, CollinearityTypes.None);
        }

        /// <summary>
        /// Determines whether [has a bounding box that encompasses] [the specified polygon b].
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <returns><c>true</c> if [has a bounding box that encompasses] [the specified polygon b]; otherwise, <c>false</c>.</returns>
        internal static bool HasABoundingBoxThatEncompasses(this Polygon polygonA, Polygon polygonB)
        {
            return (polygonA.MaxXIP >= polygonB.MaxXIP
                && polygonA.MaxYIP >= polygonB.MaxYIP
                && polygonA.MinXIP <= polygonB.MinXIP
                && polygonA.MinYIP <= polygonB.MinYIP);
        }


        /// <summary>
        /// Gets the ordered lines.
        /// </summary>
        /// <param name="orderedPoints">The ordered points.</param>
        /// <returns>PolygonEdge[].</returns>
        private static PolygonEdge[] GetOrderedLines(Vertex2D[] orderedPoints)
        {
            var length = orderedPoints.Length;
            var result = new PolygonEdge[length];
            var k = 0;
            for (int i = 0; i < length; i++)
            {
                var point = orderedPoints[i];
                if (RationalIP.IsGreaterThanVectorX(point.StartLine.OtherPoint(point).Coordinates,
                    point.Coordinates) ||
                    RationalIP.IsEqualVectorX(point.StartLine.OtherPoint(point).Coordinates,
                    point.Coordinates))
                    result[k++] = point.StartLine;
                if (RationalIP.IsGreaterThanVectorX(point.EndLine.OtherPoint(point).Coordinates,
                    point.Coordinates))
                    result[k++] = point.EndLine;
                if (k >= length) break;
            }
            return result;
        }

        /// <summary>
        /// Gets the self intersections.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <returns>List&lt;SegmentIntersection&gt;.</returns>
        public static List<SegmentIntersection> GetSelfIntersections(this Polygon polygonA)
        {
            var intersections = new List<SegmentIntersection>();
            var numLines = polygonA.Edges.Count;
            var orderedLines = GetOrderedLines(polygonA.OrderedXVertices);
            for (int i = 0; i < numLines - 1; i++)
            {
                var current = orderedLines[i];
                for (int j = i + 1; j < numLines; j++)
                {
                    var other = orderedLines[j];
                    if (current.XMax < other.XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    AddIntersectionBetweenLines(current, other, intersections);
                }
            }
            return intersections;
        }


    }
}
