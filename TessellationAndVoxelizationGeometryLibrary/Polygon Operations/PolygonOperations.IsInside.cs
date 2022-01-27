// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
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
        /// <summary>
        /// Returns the single polygon that is encompasses the point or is closest to it. This will be a 
        /// simple polygon as the method transverses each input as a polygon tree and simply find the loop
        /// (positive or negative/hole) the polygon.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="point">The point.</param>
        /// <returns>Polygon.</returns>
        //public Polygon ClosestPolygon(this IEnumerable<Polygon> polygons, Vector2 point)
        //{

        //}
        #region IsPointInSidePolygon methods 
        /// <summary>
        /// Determines whether the inner polygon is inside the specified outer polygon. This is a simpler and faster check
        /// when it is already known that the two polygons are non-intersecting polygon.
        /// </summary>
        /// <param name="outer">The outer polygon.</param>
        /// <param name="onlyTopOuterPolygon">if set to <c>true</c> only top outer polygon is checked and none of the innner polygons.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="onlyTopInnerPolygon">if set to <c>true</c> [only top inner polygon].</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <returns><c>true</c> if [is non intersecting polygon inside] [the specified only top outer polygon]; otherwise, <c>false</c>.</returns>
        internal static bool? IsNonIntersectingPolygonInside(this Polygon outer, bool onlyTopOuterPolygon, Polygon inner,
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
        /// <param name="tolerance">The tolerance.</param>
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
                    if (sortedEdges[i].XMax < vertex.X)
                    {
                        if (canRemoveEarlierEdge) edgeIndex = i;
                        continue;
                    }
                    canRemoveEarlierEdge = false;
                    if (sortedEdges[i].XMin > vertex.X)
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

        /// <summary>
        ///     Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        ///     And the polygon is not self-intersecting
        ///     This is a newer basically the same as our other method, but is less verbose.
        ///     Making use of W. Randolph Franklin's compact algorithm
        ///     https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        ///     Major Assumptions: 
        ///     1) The polygon can be convex
        ///     2) The direction of the polygon does not matter  
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="closestLineAbove">The closest line above.</param>
        /// <param name="closestLineBelow">The closest line below.</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside polygon] [the specified point in question]; otherwise, <c>false</c>.</returns>
        public static bool IsPointInsidePolygon(this Polygon polygon, bool onlyTopPolygon, Vector2 pointInQuestion)
        {
            var insideTopPolygon = IsPointInsidePolygon(polygon, pointInQuestion);
            if (onlyTopPolygon || !insideTopPolygon) return insideTopPolygon;

            //Else, it is inside the top polygon and we need to check the inner polygons
            var smallestArea = polygon.PathArea;
            var smallestEnclosingPolygon = polygon;
            foreach (var subPolygon in polygon.AllPolygons.Skip(1))
            {
                if (IsPointInsidePolygon(subPolygon, pointInQuestion))
                {
                    var absArea = Math.Abs(subPolygon.PathArea);
                    if (absArea < smallestArea)
                    {
                        smallestArea = absArea;
                        smallestEnclosingPolygon = subPolygon;
                    }
                }
            }
            //The point is inside the smallest polygon. If that polygon is positive,
            //then return true. If it is a hole, then return false.
            return smallestEnclosingPolygon.IsPositive;
        }

        private static bool IsPointInsidePolygon(this Polygon polygon, Vector2 pointInQuestion)
        {
            //1) Get the axis aligned bounding box of the path. This is super fast.
            //If the point is inside the bounding box, continue to check with more detailed methods, 
            //Else, retrun false.
            var p = pointInQuestion;
            var path = polygon.Path;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            foreach (var point in path)
            {
                if (point.X < xMin) xMin = point.X;
                if (point.X > xMax) xMax = point.X;
                if (point.Y < yMin) yMin = point.Y;
                if (point.Y > yMax) yMax = point.Y;
            }
            if (p.Y < yMin || p.Y > yMax || p.X < xMin || p.X > xMax) return false;

            //2) Next, see how many lines are to the left of the point, using a fixed y value.
            //This compact, effecient 7 lines of code is from W. Randolph Franklin
            //<https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html>
            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                if ((path[i].Y > p.Y) != (path[j].Y > p.Y) &&
                    p.X < (path[j].X - path[i].X) * (p.Y - path[i].Y) / (path[j].Y - path[i].Y) + path[i].X)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Determines if a point is inside a polygon. The polygon can be positive or negative. In either case,
        /// the result is true is the polygon encloses the point. Additionally output parameters can be used to
        /// locate the closest line above or below the point.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="closestLineAbove">The closest line above.</param>
        /// <param name="closestLineBelow">The closest line below.</param>
        /// <param name="onBoundary">if set to <c>true</c> [on boundary].</param>
        /// <param name="onBoundaryIsInside">if set to <c>true</c> [on boundary is inside].</param>
        /// <returns><c>true</c> if [is point inside polygon] [the specified point in question]; otherwise, <c>false</c>.</returns>
        public static bool IsPointInsidePolygon(this Polygon polygon, bool onlyTopPolygon, Vector2 pointInQuestion,
            out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
            //This function has three layers of checks. 
            //(1) Check if the point is inside the axis aligned bounding box. If it is not, then return false.
            //(2) Check if the point is == to a polygon point, return onBoundaryIsInside.
            //(3) Use line-sweeping / ray casting to determine if the polygon contains the point.
            onBoundary = false;
            //1) Check if center point is within bounding box of each polygon
            if (qX < polygon.MinX || qY < polygon.MinY ||
                qX > polygon.MaxX || qY > polygon.MaxY)
                return false;
            //2) If the point in question is == a point in points, then it is inside the polygon
            if (polygon.Path.Any(point => point.Equals(pointInQuestion)))
            {
                onBoundary = true;
                return onBoundaryIsInside;
            }
            var numberAbove = 0;
            var numberBelow = 0;
            foreach (var subPolygon in polygon.AllPolygons)
            {
                foreach (var line in subPolygon.Edges)
                {
                    switch (DetermineLineToPointVerticalReferenceType(pointInQuestion, line))
                    {
                        case VerticalLineReferenceType.On:
                            onBoundary = true;
                            return onBoundaryIsInside;
                        case VerticalLineReferenceType.Above:
                            numberAbove++;
                            break;
                        case VerticalLineReferenceType.Below:
                            numberBelow++;
                            break;
                    }
                }
                if (onlyTopPolygon) break;
            }
            var insideAbove = numberAbove % 2 != 0;
            var insideBelow = numberBelow % 2 != 0;
            if (insideAbove != insideBelow)
            {
                throw new ArgumentException("In IsPointInsidePolygon, the point in question is surrounded by" +
                    " an undetermined number of lines which makes it impossible to determined if inside.");
            }
            return insideAbove;
        }


        private static VerticalLineReferenceType DetermineLineToPointVerticalReferenceType(Vector2 point, PolygonEdge line)
        {
            // this is basically the function PolygonEdge.YGivenX, but it is a little different here since check if line is horizontal cusp
            if (point.X == line.FromPoint.Coordinates.X && point.X == line.ToPoint.Coordinates.X)
            {   // this means the line is vertical and lines up with the point. Other adjacent line segments will be found
                return VerticalLineReferenceType.NotIntersecting;
            }
            if (point == line.FromPoint.Coordinates)
            {
                var signOfOverallDirection = line.Vector.X * line.FromPoint.EndLine.Vector.X;
                // this is a cusp - where the polygon line turns around at this point
                if (signOfOverallDirection < 0) return VerticalLineReferenceType.NotIntersecting;
            }
            //if (point.IsPracticallySame(line.ToPoint.Coordinates, tolerance))
            // this is commented (and left for instruction) since it will be captured by the line segment after this one         
            if ((line.FromPoint.X < point.X) == (line.ToPoint.X < point.X)) return VerticalLineReferenceType.NotIntersecting;
            // if both true or both false then endpoints are on same side of point
            var intersectionYValue = line.VerticalSlope * (point.X - line.FromPoint.X) + line.FromPoint.Y;
            if (intersectionYValue == point.Y)
                return VerticalLineReferenceType.On;
            if (intersectionYValue > point.Y)
                return VerticalLineReferenceType.Above;
            return VerticalLineReferenceType.Below;
        }

        /// <summary>
        /// Determines if a point is inside a polygon. The polygon can be positive or negative. In either case,
        /// the result is true is the polygon encloses the point. Additionally output parameters can be used to
        /// locate the closest line above or below the point.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="pointInQuestion">The point in question.</param>
        /// <param name="closestLineAbove">The closest line above.</param>
        /// <param name="closestLineBelow">The closest line below.</param>
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
        internal static bool ArePointsInsidePolygonLines(IList<PolygonEdge> sortedLines, int numSortedLines, List<Vertex2D> sortedPoints,
            out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var evenNumberOfCrossings = true; // starting at zero. 
            var lineIndex = 0;
            onBoundary = false;
            foreach (var p in sortedPoints)
            {
                while (p.X > sortedLines[lineIndex].XMax)
                {
                    lineIndex++;
                    if (lineIndex == numSortedLines) return false;
                }
                for (int i = lineIndex; i < numSortedLines; i++)
                {
                    var line = sortedLines[lineIndex];
                    if (line.XMin > p.X) break;
                    if (p.Coordinates == line.FromPoint.Coordinates || p.Coordinates == line.ToPoint.Coordinates)
                    {
                        onBoundary = true;
                        if (!onBoundaryIsInside) return false;
                    }
                    var lineYValue = line.FindYGivenX(p.X, out var isBetweenEndPoints);
                    if (!isBetweenEndPoints) continue;
                    var yDistance = lineYValue - p.Y;
                    if (yDistance == 0)
                    {
                        onBoundary = true;
                        if (!onBoundaryIsInside) return false;
                    }
                    else if (yDistance > 0)
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

        #endregion
        #region Line Intersections with Polygon

        /// <summary>
        /// All the polygon intersection points along line. The line is swept in it's normal direction. This normal or swept direction
        /// is provided instead of the usual line direction so as to be clear about the direction of the sweep. Additionally, the 
        /// perpendicular distance from the origin to the line (meeting at a right angle is provided to indicate where the increments start from.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="perpendicularDistanceToLine">The line reference.</param>
        /// <param name="lineNormalDirection">The line direction.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;Vector2&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static List<Vector2[]> AllPolygonIntersectionPointsAlongLine(this IEnumerable<Polygon> polygons, Vector2 lineNormalDirection,
            double perpendicularDistanceToLine, int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            var lineReference = perpendicularDistanceToLine * lineNormalDirection;
            var intersections = new List<Vector2[]>();
            var sortedPoints = new List<Vertex2D>();
            var comparer = new VertexSortedByDirection(lineNormalDirection);
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.Vertices.OrderBy(x => x, comparer), comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return intersections;
            }
            var firstDistance = sortedPoints[0].Coordinates.Dot(lineNormalDirection);
            var lastDistance = sortedPoints[^1].Coordinates.Dot(lineNormalDirection);
            var tolerance = (lastDistance - firstDistance) * Constants.BaseTolerance;
            var currentLines = new HashSet<PolygonEdge>();
            var nextDistance = firstDistance;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - perpendicularDistanceToLine) / stepSize);
            var pointIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var d = perpendicularDistanceToLine + i * stepSize;
                var thisPoint = sortedPoints[pointIndex];
                var needToOffset = false;
                // this while loop updates the current lines. 
                var thisPointD = thisPoint.Coordinates.Dot(lineNormalDirection);
                while (thisPointD <= d)
                {
                    if (d.IsPracticallySame(thisPointD, tolerance)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }
                if (needToOffset)
                    d += Math.Min(stepSize, sortedPoints[pointIndex].Coordinates.Dot(lineNormalDirection) - d) / 10.0;

                var numIntersects = currentLines.Count;
                var intersects = new Vector2[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = MiscFunctions.LineLine2DIntersection(line.FromPoint.Coordinates, line.Vector, lineReference, lineDir);
                intersections.Add(intersects.OrderBy(x => x.Dot(lineDir)).ToArray());
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
        public static List<double[]> AllPolygonIntersectionPointsAlongVerticalLines(this IEnumerable<Polygon> polygons, double startingXValue,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = new List<Vertex2D>();
            var comparer = new VertexSortedByXFirst();
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.OrderedXVertices, comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return intersections;
            }
            var tolerance = (sortedPoints[^1].X - sortedPoints[0].X) * Constants.BaseTolerance;
            var currentLines = new HashSet<PolygonEdge>();
            var nextDistance = sortedPoints.First().X;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingXValue) / stepSize);
            var pointIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var x = startingXValue + i * stepSize;
                var thisPoint = sortedPoints[pointIndex];
                var needToOffset = false;
                while (thisPoint.X <= x)
                {
                    if (x.IsPracticallySame(thisPoint.X, tolerance)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }
                if (needToOffset)
                    x += Math.Min(stepSize, sortedPoints[pointIndex + 1].X - x) / 10.0;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.FindYGivenX(x, out _);
                intersections.Add(intersects.OrderBy(y => y).ToArray());
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
        public static List<double[]> AllPolygonIntersectionPointsAlongHorizontalLines(this IEnumerable<Polygon> polygons,
            double startingYValue, int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = new List<Vertex2D>();
            var comparer = new VertexSortedByYFirst();
            foreach (var polygon in polygons.SelectMany(p => p.AllPolygons))
            {
                polygon.MakePolygonEdgesIfNonExistent();
                sortedPoints = CombineSortedVertexLists(sortedPoints, polygon.Vertices.OrderBy(x => x, comparer), comparer).ToList();
            }
            if (sortedPoints.Count == 0)
            {
                firstIntersectingIndex = -1;
                return intersections;
            }
            var tolerance = (sortedPoints[^1].Y - sortedPoints[0].Y) * Constants.BaseTolerance;
            var currentLines = new HashSet<PolygonEdge>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingYValue) / stepSize);
            var pointIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = startingYValue + i * stepSize;
                var thisPoint = sortedPoints[pointIndex];
                var needToOffset = false;
                // this while loop updates the current lines. 
                while (thisPoint.Y <= y)
                {
                    if (y.IsPracticallySame(thisPoint.Y, tolerance)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pointIndex++;
                    if (pointIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pointIndex];
                }
                if (needToOffset)
                    y += Math.Min(stepSize, sortedPoints[pointIndex].Y - y) / 10.0;

                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.FindXGivenY(y, out _);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }
        #endregion

        /// <summary>
        /// Gets the polygon relationship of PolygonA to PolygonB and the intersections between them.
        /// </summary>
        /// <param name="subPolygonA">The polygon a.</param>
        /// <param name="subPolygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonRelationship.</returns>
        public static PolygonInteractionRecord GetPolygonInteraction(this Polygon polygonA, Polygon polygonB)
        {
            var interactionRecord = new PolygonInteractionRecord(polygonA, polygonB);
            if (interactionRecord.Relationship == PolygonRelationship.Equal) return interactionRecord;
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
        /// Separated, SeparatedButEdgesTouch, SeparatedButVerticesTouch
        ///  </returns>
        private static PolyRelInternal GetSinglePolygonRelationshipAndIntersections(this Polygon subPolygonA, Polygon subPolygonB,
            out List<SegmentIntersection> intersections)
        {
            var numSigDigs = Math.Min(subPolygonA.NumSigDigits, subPolygonB.NumSigDigits);
            var needToRoundA = subPolygonA.NumSigDigits != numSigDigs;
            var needToRoundB = subPolygonB.NumSigDigits != numSigDigs;
            intersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>();
            //As a first check, determine if the axis aligned bounding boxes overlap. If not, then we can
            // safely return that the polygons are separated.
            if (subPolygonA.MinX > subPolygonB.MaxX ||
                subPolygonA.MaxX < subPolygonB.MinX ||
                subPolygonA.MinY > subPolygonB.MaxY ||
                subPolygonA.MaxY < subPolygonB.MinY)
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
                var aXMin = needToRoundA ? Math.Round(aLines[aIndex].XMin, numSigDigs) : aLines[aIndex].XMin;
                var bXMin = needToRoundB ? Math.Round(bLines[bIndex].XMin, numSigDigs) : bLines[bIndex].XMin;
                if (aXMin < bXMin) // if the next A-line is lower compare it to all B-lines
                {
                    var aLine = aLines[aIndex++];
                    var aXMax = needToRoundA ? Math.Round(aLine.XMax, numSigDigs) : aLine.XMax;
                    var localBIndex = bIndex; //the localBIndex is incremented in the following loop, but we
                                              //need to come back to the main bIndex above
                    while (aXMax >= bXMin)
                    {
                        // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        AddIntersectionBetweenLines(aLine, bLines[localBIndex++], intersections, possibleDuplicates, numSigDigs, needToRoundA, needToRoundB);
                        if (localBIndex >= bLines.Length) break;
                        bXMin = needToRoundB ? Math.Round(bLines[localBIndex].XMin, numSigDigs) : bLines[localBIndex].XMin;
                    }
                }
                else
                {
                    var bLine = bLines[bIndex++];
                    var bXMax = needToRoundB ? Math.Round(bLine.XMax, numSigDigs) : bLine.XMax;
                    var localAIndex = aIndex;
                    while (bXMax >= aXMin)
                    {
                        AddIntersectionBetweenLines(aLines[localAIndex++], bLine, intersections, possibleDuplicates, numSigDigs, needToRoundA, needToRoundB);
                        if (localAIndex >= aLines.Length) break;
                        aXMin = needToRoundA ? Math.Round(aLines[localAIndex].XMin, numSigDigs) : aLines[localAIndex].XMin;
                    }
                }
            }

            var relationship = PolyRelInternal.Separated;

            if (intersections.Count == 0) // since there are no intersections all the nodeTypes of a vertices of a polygon should be the same
                                          // and they are either Inside or Outside. There can be any OnBorder as these would have registered as intersections as well
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

            RemoveDuplicateIntersections(possibleDuplicates, intersections);
            if (intersections.Any(intersection => intersection.CollinearityType != CollinearityTypes.None))
                relationship |= PolyRelInternal.CoincidentEdges;
            if (intersections.Any(intersection => intersection.WhereIntersection != WhereIsIntersection.Intermediate))
                relationship |= PolyRelInternal.CoincidentVertices;
            var equalTolerance = Math.Pow(10, -numSigDigs);
            var isEqual = subPolygonA.PathArea.IsPracticallySame(subPolygonB.PathArea, equalTolerance);
            foreach (var intersect in intersections)
            {
                if (!isEqual) break;
                isEqual = !(intersect.Relationship != SegmentRelationship.DoubleOverlap || intersect.CollinearityType != CollinearityTypes.BothSameDirection);
            }
            if (isEqual)
                return relationship | PolyRelInternal.Equal;
            var isOpposite = true;
            foreach (var intersect in intersections)
            {
                if (intersect.Relationship != SegmentRelationship.NoOverlap || intersect.Relationship != SegmentRelationship.Abutting || 
                    intersect.CollinearityType != CollinearityTypes.BothOppositeDirection)
                {
                    isOpposite = false;
                    break;
                }
            }
            if (isOpposite)
            {
                if (subPolygonA.Area.IsPracticallySame(-subPolygonB.Area, equalTolerance) && subPolygonA.MinX.IsPracticallySame(subPolygonB.MinX, equalTolerance)
                    && subPolygonA.MinY.IsPracticallySame(subPolygonB.MinY, equalTolerance) && subPolygonA.MaxX.IsPracticallySame(subPolygonB.MaxX, equalTolerance)
                    && subPolygonA.MaxY.IsPracticallySame(subPolygonB.MaxY, equalTolerance))
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
            var atLeastOneAEncloseB = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.AEnclosesB);
            var atLeastOneBEncloseA = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.BEnclosesA);

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


        private static void RemoveDuplicateIntersections(List<(int index, PolygonEdge lineA, PolygonEdge lineB)> possibleDuplicates,
            List<SegmentIntersection> intersections)
        {
            foreach (var dupeData in possibleDuplicates)
            {
                var duplicateIntersection = intersections[dupeData.index];

                for (int i = 0; i < intersections.Count; i++)
                {
                    if (i == dupeData.index) continue;
                    if (((intersections[i].EdgeA == dupeData.lineA && intersections[i].EdgeB == dupeData.lineB) ||
                        (intersections[i].EdgeA == dupeData.lineB && intersections[i].EdgeB == dupeData.lineA)) &&
                        intersections[i].WhereIntersection != WhereIsIntersection.Intermediate &&
                       duplicateIntersection.IntersectCoordinates == intersections[i].IntersectCoordinates)
                    {
                        intersections.RemoveAt(dupeData.index);
                        break;
                    }
                }
            }
        }

        const double unitCrossIsParallel = 1e-10;

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
        /// <returns>PolygonSegmentRelationship.</returns>
        internal static bool AddIntersectionBetweenLines(PolygonEdge lineA, PolygonEdge lineB,
            List<SegmentIntersection> intersections, List<(int, PolygonEdge, PolygonEdge)> possibleDuplicates, int numSigDigs,
            bool needToRoundA, bool needToRoundB)
        {
            #region initialize local variables
            var aFrom = needToRoundA ? new Vector2(Math.Round(lineA.FromPoint.X, numSigDigs), Math.Round(lineA.FromPoint.Y, numSigDigs))
                : lineA.FromPoint.Coordinates;
            var aTo = needToRoundA ? new Vector2(Math.Round(lineA.ToPoint.X, numSigDigs), Math.Round(lineA.ToPoint.Y, numSigDigs))
                : lineA.ToPoint.Coordinates;
            if (needToRoundA && aTo == aFrom) return false;
            var aVector = aTo - aFrom;
            var bFrom = needToRoundB ? new Vector2(Math.Round(lineB.FromPoint.X, numSigDigs), Math.Round(lineB.FromPoint.Y, numSigDigs))
                : lineB.FromPoint.Coordinates;
            var bTo = needToRoundB ? new Vector2(Math.Round(lineB.ToPoint.X, numSigDigs), Math.Round(lineB.ToPoint.Y, numSigDigs))
                : lineB.ToPoint.Coordinates;
            if (needToRoundB && bTo == bFrom) return false;
            var bVector = bTo - bFrom;
            #endregion

            // first check if bounding boxes overlap. Actually, we don't need to check the x values (lineA.XMax < lineB.XMin || 
            // lineB.XMax < lineA.XMin)- this is already known from the calling function and the way it calls based on sorted x values
            if (Math.Max(aFrom.Y, aTo.Y) < Math.Min(bFrom.Y, bTo.Y) || Math.Max(bFrom.Y, bTo.Y) < Math.Min(aFrom.Y, aTo.Y))
                // the two lines do not touch since their bounding boxes do not overlap
                return false;
            // okay, so bounding boxes DO overlap
            var intersectionCoordinates = Vector2.Null;
            var where = WhereIsIntersection.Intermediate;

            var lineACrossLineB = aVector.Cross(bVector); //2D cross product, determines if parallel
            //first a quick check to see if points are the same
            if (aFrom == bFrom)
            {
                intersectionCoordinates = aFrom;
                where = WhereIsIntersection.BothStarts;
            }
            else
            {
                var fromPointVector = bFrom - aFrom; // the vector connecting starts
                if (lineACrossLineB == 0) // the two lines are parallel (cross product will be zero)
                {
                    var intersectionFound = false;
                    if (fromPointVector.Cross(aVector) == 0)
                    {
                        // if fromPointCross is also parallel with the line vector (either lineA or lineB since they are parallel to each other)
                        // and since bounding boxes do overlap, then the lines are collinear and overlapping
                        // While there are technically infinite points that are intersecting, we only record when the start of the line
                        // is common. It is possible that the starts (FromPoints) are not overlapping at all - in which case nothing is added.
                        // It is also possible that both FromPoints are on the other line - if so, then we add both. This is the one other place 
                        // where a second IntersectionData is added
                        if ((bTo - aFrom).Dot(fromPointVector) < 0)
                        {   // since fromPointVector goes from lineA.FromPoint to lineB.FromPoint - if going from line.FromPoint to lineB.ToPoint is
                            // opposite then lineA.FromPoint is on lineB
                            intersectionCoordinates = aFrom;
                            where = WhereIsIntersection.AtStartOfA;
                            intersectionFound = true;
                        }
                        if ((bFrom - aTo).Dot(fromPointVector) < 0)
                        { // now check the other way. Note, since fromPointVector is backwards here, we just make the other vector backwards as well

                            if (intersectionFound) // okay, well, you need to add TWO points. Going to go ahead and finish off the lineB point here
                            {
                                CollinearityTypes collinearB;
                                SegmentRelationship relationshipB;
                                (relationshipB, collinearB) = DeterminePolygonSegmentRelationship(lineA, lineB, aVector, bVector, numSigDigs, needToRoundA, needToRoundB,
                                    WhereIsIntersection.AtStartOfB, lineACrossLineB);

                                intersections.Add(new SegmentIntersection(lineA, lineB, bFrom, relationshipB,
                                    WhereIsIntersection.AtStartOfB, collinearB));
                            }
                            else
                            {
                                where = WhereIsIntersection.AtStartOfB;
                                intersectionCoordinates = bFrom;
                                intersectionFound = true;
                            }
                        }
                        //technically the lines overlap even if the previous two condition are not met, but since the overlap doesn't include
                        // either from Point, then we do not record it. It will be recorded when the next segment is checked
                    }
                    if (!intersectionFound) return false; // otherwise the lines are parallel but not at same distance/intercept. Or, they are
                                                          //inline but the from's other both segments are outside of the intersection - we consider this not an intersection. 
                                                          //Instead those will be solved in the next/adjacent polygon segment
                }
                else
                {
                    // now check the intersection by detecting where non-parallel lines cross
                    // solve for the t scalar values for the two lines.
                    // the line is define as all values of t from 0 to 1 in the equations
                    // line1(t_1) = (1 - t_1)*line1.From + t_1*line1.To
                    // line2(t_2) = (1 - t_2)*line2.From + t_2*line2.To
                    // ...solving for the x-value at the intersection...
                    // xIntersect =  (1 - t_1)*line1.From.X + t_1*line1.To.X = (1 - t_2)*line2.From.X + t_2*line2.To.X (Eq.1)
                    // yIntersect =  (1 - t_1)*line1.From.Y + t_1*line1.To.Y = (1 - t_2)*line2.From.Y + t_2*line2.To.Y (Eq.2)
                    //rewriting Eq.1 as...
                    // t_1*(line1.To.X - line1.From.X) + t_2*(line2.From.X - line2.To.X) = line2.From.X - line1.From.X 
                    // which can be simplified to...
                    // t_1*(line1.Vector.X) - t_2*(line2.Vector.X) = vStart.X
                    // similiarly for Y
                    // t_1*(line1.Vector.Y) - t_2*(line2.Vector.Y) = vStart.Y
                    // solve as a system of two equations
                    //   |   line1.Vector.X      -line2.Vector.X   | |  t_1  |    | vStart.X  |
                    //   |                                         |*|       | =  |           |
                    //   |   line1.Vector.Y      -line2.Vector.Y   | |  t_2  |    | vStart.Y  |
                    var oneOverdeterminnant = 1 / lineACrossLineB;
                    var t_1 = oneOverdeterminnant * (bVector.Y * fromPointVector.X - bVector.X * fromPointVector.Y);
                    if (t_1 < 0 || t_1 >= 1)
                        //if (t_1.IsLessThanNonNegligible(0, Constants.PolygonSameTolerance)
                        //    || !t_1.IsLessThanNonNegligible(1.0, Constants.PolygonSameTolerance))
                        return false;
                    var t_2 = oneOverdeterminnant * (aVector.Y * fromPointVector.X - aVector.X * fromPointVector.Y);
                    if (t_2 < 0 || t_2 >= 1)
                        return false;
                    var aIntersection = new Vector2(
                            Math.Round(aFrom.X + t_1 * aVector.X, numSigDigs),
                            Math.Round(aFrom.Y + t_1 * aVector.Y, numSigDigs));
                    var bIntersection = new Vector2(
                            Math.Round(bFrom.X + t_2 * bVector.X, numSigDigs),
                            Math.Round(bFrom.Y + t_2 * bVector.Y, numSigDigs));
                    if (aIntersection == aFrom)
                    {
                        intersectionCoordinates = aFrom;
                        where = WhereIsIntersection.AtStartOfA;
                        if (aFrom == bTo)
                            possibleDuplicates.Insert(0, (intersections.Count, lineA, lineB.ToPoint.StartLine));
                    }
                    else if (bIntersection == bFrom)
                    {
                        intersectionCoordinates = bFrom;
                        where = WhereIsIntersection.AtStartOfB;
                        if (bFrom == aTo)
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB));
                    }
                    else
                    {
                        intersectionCoordinates = new Vector2(
                            Math.Round((aFrom.X + t_1 * aVector.X + bFrom.X + t_2 * bVector.X) / 2, numSigDigs),
                            Math.Round((aFrom.Y + t_1 * aVector.Y + bFrom.Y + t_2 * bVector.Y) / 2, numSigDigs));
                        where = WhereIsIntersection.Intermediate;
                        if (intersectionCoordinates == aTo && intersectionCoordinates == bTo)
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB.ToPoint.StartLine));
                        else if (intersectionCoordinates == aTo)
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB));
                        else if (intersectionCoordinates == bTo)
                            possibleDuplicates.Insert(0, (intersections.Count, lineA, lineB.ToPoint.StartLine));
                    }
                }
            }
            CollinearityTypes collinear;
            SegmentRelationship relationship;
            (relationship, collinear) = DeterminePolygonSegmentRelationship(lineA, lineB, aVector, bVector, numSigDigs, needToRoundA, needToRoundB, where, lineACrossLineB);
            intersections.Add(new SegmentIntersection(lineA, lineB, intersectionCoordinates, relationship, where, collinear));
            return true;
        }


        internal static (SegmentRelationship, CollinearityTypes) DeterminePolygonSegmentRelationship(PolygonEdge edgeA, PolygonEdge edgeB,
            in Vector2 aVector, in Vector2 bVector, int numSigDigs, bool needToRoundA, bool needToRoundB, in WhereIsIntersection where, double lineACrossLineB)
        {
            // first off - handle the intermediate case right away. since it's simple and happens often
            if (where == WhereIsIntersection.Intermediate)
                return (lineACrossLineB < 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.None);
            // set up other useful vectors and cross products
            double prevACrossPrevB, lineACrossPrevB, prevACrossLineB;
            Vector2 previousAVector, previousBVector;
            // based on where the intersection happens, we can quicken the calculation of these
            if (where == WhereIsIntersection.AtStartOfA)
            { //then lineB and prevB are the same
                var previousALine = edgeA.FromPoint.EndLine;
                if (needToRoundA)
                {
                    previousAVector = new Vector2(
                        Math.Round(previousALine.ToPoint.X, numSigDigs) - Math.Round(previousALine.FromPoint.X, numSigDigs),
                        Math.Round(previousALine.ToPoint.Y, numSigDigs) - Math.Round(previousALine.FromPoint.Y, numSigDigs)
                        );
                }
                else previousAVector = previousALine.Vector;
                previousBVector = bVector;
                lineACrossPrevB = lineACrossLineB;
                prevACrossLineB = prevACrossPrevB = previousAVector.Cross(previousBVector);
            }
            else if (where == WhereIsIntersection.AtStartOfB)
            { //then lineA and prevA are the same
                previousAVector = aVector;
                var previousBLine = edgeB.FromPoint.EndLine;
                if (needToRoundB)
                {
                    previousBVector = new Vector2(
                        Math.Round(previousBLine.ToPoint.X - previousBLine.FromPoint.X, numSigDigs),
                        Math.Round(previousBLine.ToPoint.Y - previousBLine.FromPoint.Y, numSigDigs)
                        );
                }
                else previousBVector = previousBLine.Vector;
                prevACrossLineB = lineACrossLineB;
                lineACrossPrevB = prevACrossPrevB = previousAVector.Cross(previousBVector);
            }
            else // then where == BothLinesStart
            {
                var previousALine = edgeA.FromPoint.EndLine;
                if (needToRoundA)
                {
                    previousAVector = new Vector2(
                        Math.Round(previousALine.ToPoint.X - previousALine.FromPoint.X, numSigDigs),
                        Math.Round(previousALine.ToPoint.Y - previousALine.FromPoint.Y, numSigDigs)
                        );
                }
                else previousAVector = previousALine.Vector;
                var previousBLine = edgeB.FromPoint.EndLine;
                if (needToRoundB)
                {
                    previousBVector = new Vector2(
                        Math.Round(previousBLine.ToPoint.X - previousBLine.FromPoint.X, numSigDigs),
                        Math.Round(previousBLine.ToPoint.Y - previousBLine.FromPoint.Y, numSigDigs)
                        );
                    //previousBVector = new Vector2(
                    //    Math.Round(previousBLine.ToPoint.X, numSigDigs) - Math.Round(previousBLine.FromPoint.X, numSigDigs),
                    //    Math.Round(previousBLine.ToPoint.Y, numSigDigs) - Math.Round(previousBLine.FromPoint.Y, numSigDigs)
                    //    );
                }
                else previousBVector = previousBLine.Vector;
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
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.BothSameDirection);
                return (SegmentRelationship.Abutting, CollinearityTypes.BothOppositeDirection);
            }
            // most restrictive is when both lines are parallel
            if (lineACrossLineB == 0 && prevACrossPrevB == 0)
            {
                var lineADotLineB = aVector.Dot(bVector);
                var prevADotPrevB = previousAVector.Dot(previousBVector);
                if (lineADotLineB > 0 && prevADotPrevB > 0)
                    //case 16
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.BothSameDirection);
                if (lineADotLineB < 0 && prevADotPrevB < 0 && lineACrossPrevB != 0)
                // a rare version of case 5 or 6 where the lines enter and leave the point on parallel lines, but
                // there is no collinearity! A's cross product of the corner would be the same as B's. If this corner
                // cross is positive/convex, then no overlap. if concave, then double overlap
                {
                    var cross = previousAVector.Cross(aVector);
                    if (cross.IsNegligible())
                        return (SegmentRelationship.Abutting, CollinearityTypes.BothOppositeDirection);
                    return (cross > 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                        CollinearityTypes.None);
                }
                if (prevADotPrevB < 0) // then lineADotLineB would be positive, and polygons were heading
                                       // right to each other on parallel lines before joining. this is a rare case 7 or 8
                    return (previousAVector.Cross(aVector) > 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
                        CollinearityTypes.After);
                if (lineADotLineB < 0) // then prevADotPrevB would be positive. the polygon diverges in 
                                       // opposite parallel directions. this is a rare case 9 or 10
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
                             CollinearityTypes.Before);
            }
            if (lineACrossPrevB == 0 && prevACrossLineB == 0)
            {
                var lineADotPrevB = aVector.Dot(previousBVector);
                var prevADotLineB = previousAVector.Dot(bVector);
                if (lineADotPrevB < 0 && prevADotLineB < 0)  // case 15
                    return (SegmentRelationship.Abutting, CollinearityTypes.BothOppositeDirection);
                if (lineADotPrevB > 0 && prevADotLineB > 0)  // a very unusual case (although it shows up in the chunky polygon
                    return (previousAVector.Cross(aVector) >= 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
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
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.AEnclosesB, CollinearityTypes.After);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.BEnclosesA, CollinearityTypes.After);
                return (prevACrossPrevB > 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB, CollinearityTypes.After);
            }
            if (prevACrossPrevB == 0 && previousAVector.Dot(previousBVector) > 0)
            {   //like the previous condition if this hasn't been captured by the aboe then lineACrossLineB !=0. So this is Case 9 & 10
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.AEnclosesB, CollinearityTypes.Before);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.BEnclosesA, CollinearityTypes.Before);
                return (lineACrossLineB > 0 ? SegmentRelationship.AEnclosesB : SegmentRelationship.BEnclosesA, CollinearityTypes.Before);
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
                return (SegmentRelationship.BEnclosesA, CollinearityTypes.None);
            // if only a positive on the A side then A encompasses B
            else if (lineBIsInsideA && prevLineBIsInsideA)
                // case 4
                return (SegmentRelationship.AEnclosesB, CollinearityTypes.None);
            // else Case 6
            else return (SegmentRelationship.NoOverlap, CollinearityTypes.None);
        }

        internal static bool HasABoundingBoxThatEncompasses(this Polygon polygonA, Polygon polygonB)
        {
            return (polygonA.MaxX >= polygonB.MaxX
                && polygonA.MaxY >= polygonB.MaxY
                && polygonA.MinX <= polygonB.MinX
                && polygonA.MinY <= polygonB.MinY);
        }


        private static PolygonEdge[] GetOrderedLines(Vertex2D[] orderedPoints)
        {
            var length = orderedPoints.Length;
            var result = new PolygonEdge[length];
            var k = 0;
            for (int i = 0; i < length; i++)
            {
                var point = orderedPoints[i];
                if (point.StartLine.OtherPoint(point).X >= point.X)
                    result[k++] = point.StartLine;
                if (point.EndLine.OtherPoint(point).X > point.X)
                    result[k++] = point.EndLine;
                if (k >= length) break;
            }
            return result;
        }

        private static List<SegmentIntersection> GetSelfIntersections(this Polygon polygonA)
        {
            var intersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int index, PolygonEdge lineA, PolygonEdge lineB)>();
            var numLines = polygonA.Edges.Length;
            var orderedLines = GetOrderedLines(polygonA.OrderedXVertices);
            for (int i = 0; i < numLines - 1; i++)
            {
                var current = orderedLines[i];
                for (int j = i + 1; j < numLines; j++)
                {
                    var other = orderedLines[j];
                    if (current.XMax < orderedLines[j].XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    AddIntersectionBetweenLines(current, other, intersections, possibleDuplicates, polygonA.NumSigDigits, false, false);
                }
            }
            RemoveDuplicateIntersections(possibleDuplicates, intersections);
            return intersections;
        }


    }
}
