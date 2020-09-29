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
        internal static bool? IsNonIntersectingPolygonInside(this IList<PolygonEdge> sortedEdges, List<Vertex2D> sortedVertices, double tolerance)
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
                    if (sortedEdges[i].XMax.IsLessThanNonNegligible(vertex.X, tolerance))
                    {
                        if (canRemoveEarlierEdge) edgeIndex = i;
                        continue;
                    }
                    canRemoveEarlierEdge = false;
                    if (sortedEdges[i].XMin.IsGreaterThanNonNegligible(vertex.X, tolerance))
                        break;
                    switch (DetermineLineToPointVerticalReferenceType(vertex.Coordinates, sortedEdges[i], tolerance))
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
        internal static bool IsPointInsidePolygon(this Polygon polygon, bool onlyTopPolygon, Vector2 pointInQuestion,
            out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var tolerance = Math.Min(polygon.MaxX - polygon.MinX, polygon.MaxY - polygon.MinY) * Constants.BaseTolerance;
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
            if (polygon.Path.Any(point => point.IsPracticallySame(pointInQuestion, tolerance)))
            {
                onBoundary = true;
                return onBoundaryIsInside;
            }
            var numberAbove = 0;
            var numberBelow = 0;
            foreach (var subPolygon in polygon.AllPolygons)
            {
                foreach (var line in subPolygon.Lines)
                {
                    switch (DetermineLineToPointVerticalReferenceType(pointInQuestion, line, tolerance))
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


        private static VerticalLineReferenceType DetermineLineToPointVerticalReferenceType(Vector2 point, PolygonEdge line, double tolerance)
        {
            // this is basically the function PolygonEdge.YGivenX, but it is a little different here since check if line is horizontal cusp
            if (point.IsPracticallySame(line.FromPoint.Coordinates, tolerance) &&
                point.IsPracticallySame(line.ToPoint.Coordinates, tolerance))
            {   // this means the line is vertical and lines up with the point. Other adjacent line segments will be found
                return VerticalLineReferenceType.NotIntersecting;
            }
            if (point.IsPracticallySame(line.FromPoint.Coordinates, tolerance))
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
            if (intersectionYValue.IsPracticallySame(point.Y, tolerance))
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
            var tolerance = Math.Min(polygon.MaxX - polygon.MinX, polygon.MaxY - polygon.MinY) * Constants.BaseTolerance;
            var sortedLines = polygon.Lines.OrderBy(line => line.XMin).ToList();
            var sortedPoints = pointsInQuestion.OrderBy(pt => pt.X).ToList();
            return ArePointsInsidePolygonLines(sortedLines, sortedLines.Count, sortedPoints, out onBoundary, tolerance, onBoundaryIsInside);
        }
        internal static bool ArePointsInsidePolygonLines(IList<PolygonEdge> sortedLines, int numSortedLines, List<Vertex2D> sortedPoints,
            out bool onBoundary, double tolerance, bool onBoundaryIsInside = true)
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
                    if (p.Coordinates.IsPracticallySame(line.FromPoint.Coordinates, tolerance) ||
                     p.Coordinates.IsPracticallySame(line.ToPoint.Coordinates, tolerance))
                    {
                        onBoundary = true;
                        if (!onBoundaryIsInside) return false;
                    }
                    var lineYValue = line.FindYGivenX(p.X, out var isBetweenEndPoints);
                    if (!isBetweenEndPoints) continue;
                    var yDistance = lineYValue - p.Y;
                    if (yDistance.IsNegligible(tolerance))
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
            // this novel for-loop implementation of i and j is brilliant (compact and efficient). use this in other places!!
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
        /// All the polygon intersection points along line.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="lineReference">The line reference.</param>
        /// <param name="lineDirection">The line direction.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;Vector2&gt;.</returns>
        /// <exception cref="NotImplementedException"></exception>
        public static List<Vector2> AllPolygonIntersectionPointsAlongLine(this IEnumerable<Polygon> polygons, Vector2 lineReference, double lineDirection,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            throw new NotImplementedException();
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
            var sortedPoints = polygons.SelectMany(polygon => polygon.Vertices).OrderBy(p => p.X).ToList();
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
                    x += Math.Min(stepSize, sortedPoints[pointIndex + 1].X) / 10.0;
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
        public static List<double[]> AllPolygonIntersectionPointsAlongHorizontalLines(this IEnumerable<Polygon> polygons, double startingYValue, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.AllPolygons.SelectMany(
                subPolygon => subPolygon.Vertices)).OrderBy(p => p.Y).ToList();
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
                    y += Math.Min(stepSize, sortedPoints[pointIndex].Y) / 10.0;

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
        public static PolygonInteractionRecord GetPolygonInteraction(this Polygon polygonA, Polygon polygonB, double tolerance = double.NaN)
        {
            if (double.IsNaN(tolerance))
            {
                tolerance = Math.Min(polygonA.MaxX - polygonA.MinX,
                   Math.Min(polygonA.MaxY - polygonA.MinY,
                   Math.Min(polygonB.MaxX - polygonB.MinX, polygonB.MaxY - polygonB.MinY))) * Constants.BaseTolerance;
            }
            var interactionRecord = new PolygonInteractionRecord(polygonA, polygonB);
            var visited = new bool[interactionRecord.numPolygonsInA * interactionRecord.numPolygonsInB];
            var topRelation = RecursePolygonInteractions(polygonA, polygonB, interactionRecord, visited, tolerance);
            foreach (var rel in interactionRecord.GetRelationships(polygonA))
                topRelation |= rel.Item1;
            foreach (var rel in interactionRecord.GetRelationships(polygonB))
                topRelation |= rel.Item1;
            interactionRecord.Relationship = topRelation;
            return interactionRecord;
        }
        /// <summary>
        /// Recurse down the polygon trees to find the interactions.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interactionRecord">The interaction record.</param>
        /// <param name="visited">The visited.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>PolygonRelationship.</returns>
        private static PolygonRelationship RecursePolygonInteractions(Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
            bool[] visited, double tolerance)
        {
            var index = interactionRecord.findLookupIndex(polygonA, polygonB);
            if (visited[index]) return interactionRecord.GetRelationshipBetween(polygonA, polygonB);
            var relationship = GetSinglePolygonRelationshipAndIntersections(polygonA, polygonB, out var localIntersections, tolerance);
            interactionRecord.IntersectionData.AddRange(localIntersections);
            visited[index] = true;
            if (relationship != PolygonRelationship.Separated &&
                relationship != PolygonRelationship.SeparatedButEdgesTouch &&
                relationship != PolygonRelationship.SeparatedButVerticesTouch)
            {
                if (relationship != PolygonRelationship.AIsCompletelyInsideB &&
                    relationship != PolygonRelationship.AIsInsideBButEdgesTouch &&
                    relationship != PolygonRelationship.AIsInsideBButVerticesTouch &&
                    relationship != PolygonRelationship.AIsInsideHoleOfB &&
                    relationship != PolygonRelationship.AIsInsideHoleOfBButEdgesTouch &&
                    relationship != PolygonRelationship.AIsInsideHoleOfBButVerticesTouch)
                {
                    foreach (var innerPolyA in polygonA.InnerPolygons)
                    {
                        var rel = RecursePolygonInteractions(innerPolyA, polygonB, interactionRecord, visited, tolerance);
                        if ((rel & PolygonRelationship.BIsInsideHoleOfA) == PolygonRelationship.BIsInsideHoleOfA)
                            relationship |= PolygonRelationship.InsideHole;
                    }
                }
                if (relationship != PolygonRelationship.BIsCompletelyInsideA &&
                    relationship != PolygonRelationship.BIsInsideAButEdgesTouch &&
                    relationship != PolygonRelationship.BIsInsideAButVerticesTouch &&
                relationship != PolygonRelationship.BIsInsideHoleOfA &&
                relationship != PolygonRelationship.BIsInsideHoleOfABButEdgesTouch &&
                relationship != PolygonRelationship.BIsInsideHoleOfABButVerticesTouch)
                {
                    foreach (var innerPolyB in polygonB.InnerPolygons)
                    {
                        var rel = RecursePolygonInteractions(polygonA, innerPolyB, interactionRecord, visited, tolerance);
                        if ((rel & PolygonRelationship.AIsInsideHoleOfB) == PolygonRelationship.AIsInsideHoleOfB)
                            relationship |= PolygonRelationship.InsideHole;
                    }
                }
            }
            interactionRecord.SetRelationshipBetween(index, relationship);
            return relationship;
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
        private static PolygonRelationship GetSinglePolygonRelationshipAndIntersections(this Polygon subPolygonA, Polygon subPolygonB,
            out List<SegmentIntersection> intersections, double tolerance)
        {
            intersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int, PolygonEdge, PolygonEdge)>();
            //As a first check, determine if the axis aligned bounding boxes overlap. If not, then we can
            // safely return that the polygons are separated.
            if (subPolygonA.MinX > subPolygonB.MaxX ||
                subPolygonA.MaxX < subPolygonB.MinX ||
                subPolygonA.MinY > subPolygonB.MaxY ||
                subPolygonA.MaxY < subPolygonB.MinY) return PolygonRelationship.Separated;
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
                if (aLines[aIndex].XMin < bLines[bIndex].XMin) // if the next A-line is lower compare it to all B-lines
                {
                    var aLine = aLines[aIndex++];
                    var localBIndex = bIndex; //the localBIndex is incremented in the following loop, but we
                                              //need to come back to the main bIndex above
                    while (localBIndex < bLines.Length && !aLine.XMax.IsLessThanNonNegligible(bLines[localBIndex].XMin, tolerance))
                        // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        AddIntersectionBetweenLines(aLine, bLines[localBIndex++], intersections, possibleDuplicates, tolerance);
                }
                else
                {
                    var bLine = bLines[bIndex++];
                    var localAIndex = aIndex;
                    while (localAIndex < aLines.Length && !bLine.XMax.IsLessThanNonNegligible(aLines[localAIndex].XMin, tolerance))
                        AddIntersectionBetweenLines(aLines[localAIndex++], bLine, intersections, possibleDuplicates, tolerance);
                }
            }

            if (intersections.Count == 0) // since there are no intersections all the nodeTypes of a vertices of a polygon should be the same
                                          // and they are either Inside or Outside. There can be any OnBorder as these would have registered as intersections as well
                                          // however inner polygons could exhibit difference values than the outer (consider edge case: nested squares). For example,
                                          // A encompasses B but a hole in B is smaller and fits inside hole of A. This should be registered as Intersecting
            {
                if (subPolygonA.HasABoundingBoxThatEncompasses(subPolygonB) && IsNonIntersectingPolygonInside(aLines, subPolygonB.OrderedXVertices, tolerance) == true)
                    return subPolygonA.IsPositive ? PolygonRelationship.BIsCompletelyInsideA : PolygonRelationship.BIsInsideHoleOfA;
                if (subPolygonB.HasABoundingBoxThatEncompasses(subPolygonA) && IsNonIntersectingPolygonInside(bLines, subPolygonA.OrderedXVertices, tolerance) == true)
                    return subPolygonB.IsPositive ? PolygonRelationship.AIsCompletelyInsideB : PolygonRelationship.AIsInsideHoleOfB;
                return PolygonRelationship.Separated;
            }

            RemoveDuplicateIntersections(possibleDuplicates, intersections, tolerance);

            var isEqual = true;
            foreach (var intersect in intersections)
            {
                if (intersect.Relationship != SegmentRelationship.DoubleOverlap || intersect.CollinearityType != CollinearityTypes.BothSameDirection)
                {
                    isEqual = false;
                    break;
                }
            }
            if (isEqual) return PolygonRelationship.Equal;

            var isOpposite = true;
            foreach (var intersect in intersections)
            {
                if (intersect.Relationship != SegmentRelationship.NoOverlap || intersect.CollinearityType != CollinearityTypes.BothOppositeDirection)
                {
                    isOpposite = false;
                    break;
                }
            }
            if (isOpposite) return PolygonRelationship.EqualButOpposite;

            if (intersections.Any(intersection => intersection.Relationship == SegmentRelationship.CrossOver_BOutsideAfter ||
            intersection.Relationship == SegmentRelationship.CrossOver_AOutsideAfter ||
            (intersection.Relationship == SegmentRelationship.DoubleOverlap && (subPolygonA.IsPositive || subPolygonB.IsPositive))))
                return PolygonRelationship.Intersection | PolygonRelationship.EdgesCross;

            // given the previous comment, we can only reach this point if ALL intersections are of type NoOverlap or Enclose

            var atLeastOneCoincidentEdge = intersections.Any(intersection => intersection.CollinearityType != CollinearityTypes.None);

            if (intersections.All(intersection => intersection.Relationship == SegmentRelationship.NoOverlap))
            {   // all intersections of  type, NoOverlap
                if (subPolygonA.IsPositive == subPolygonB.IsPositive)
                    return atLeastOneCoincidentEdge ? PolygonRelationship.SeparatedButEdgesTouch
                   : PolygonRelationship.SeparatedButVerticesTouch;
                // then one is positive and the other is negative
                if (subPolygonA.IsPositive) // therefore subPolygonB is a hole
                    return atLeastOneCoincidentEdge ? PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                                       : PolygonRelationship.AIsInsideHoleOfBButVerticesTouch;
                else // then B is positive and A is a negative polygon/hole
                    return atLeastOneCoincidentEdge ? PolygonRelationship.BIsInsideHoleOfABButEdgesTouch
                                       : PolygonRelationship.BIsInsideHoleOfABButVerticesTouch;
            }

            // given the previous conditions, we can only reach this point if ALL intersections are of type NoOverlap or Enclose
            // and there must be at least one Enclose
            var atLeastOneAEncloseB = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.AEnclosesB);
            var atLeastOneBEncloseA = intersections.Any(intersection => intersection.Relationship == SegmentRelationship.BEnclosesA);

            if (atLeastOneAEncloseB && atLeastOneBEncloseA)
                return PolygonRelationship.Intersection | PolygonRelationship.EdgesCross;

            if (subPolygonA.IsPositive != subPolygonB.IsPositive)
                return atLeastOneCoincidentEdge ? PolygonRelationship.SeparatedButEdgesTouch
               : PolygonRelationship.SeparatedButVerticesTouch;

            // should "InsideHole" be included in the returns below? I don't think so. Since the previous condition failed, then both
            // are positive or both are negative. The implications of "inside hole" are only meaningful when the shallow polygons can be 
            // considered as non-overlapping. if both postive or both negative then there is overlap in their material
            if (subPolygonA.IsPositive == atLeastOneAEncloseB)
                return atLeastOneCoincidentEdge ? PolygonRelationship.BIsInsideAButEdgesTouch
                                   : PolygonRelationship.BIsInsideAButVerticesTouch;

            //if (atLeastOneBEncloseA && subPolygonB.IsPositive)
            return atLeastOneCoincidentEdge ? PolygonRelationship.AIsInsideBButEdgesTouch
                               : PolygonRelationship.AIsInsideBButVerticesTouch;

            //else throw new Exception("debug? no default polygon relationship found.")
        }


        private static void RemoveDuplicateIntersections(List<(int index, PolygonEdge lineA, PolygonEdge lineB)> possibleDuplicates, List<SegmentIntersection> intersections,
            double tolerance)
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
                       duplicateIntersection.IntersectCoordinates.IsPracticallySame(intersections[i].IntersectCoordinates, tolerance))
                    {
                        intersections.RemoveAt(dupeData.index);
                        break;
                    }
                }
            }
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
        /// <returns>PolygonSegmentRelationship.</returns>
        internal static bool AddIntersectionBetweenLines(PolygonEdge lineA, PolygonEdge lineB,
            List<SegmentIntersection> intersections, List<(int, PolygonEdge, PolygonEdge)> possibleDuplicates, double tolerance)
        {
            // first check if bounding boxes overlap. Actually, we don't need to check the x values (lineA.XMax < lineB.XMin || 
            // lineB.XMax < lineA.XMin)- this is already known from the calling function and the way it calls based on sorted x values
            if (lineA.YMax.IsLessThanNonNegligible(lineB.YMin, tolerance) || lineB.YMax.IsLessThanNonNegligible(lineA.YMin, tolerance))
                // the two lines do not touch since their bounding boxes do not overlap
                return false;
            // okay, so bounding boxes DO overlap
            var intersectionCoordinates = Vector2.Null;
            var where = WhereIsIntersection.Intermediate;

            var lineACrossLineB = lineA.Vector.Cross(lineB.Vector); //2D cross product, determines if parallel
            if (lineACrossLineB.IsNegligible(tolerance))
                lineACrossLineB = 0; //this avoid a problem where further inequalities ask is <0 but the value is like -1e-15

            //var prevA = lineA.FromPoint.EndLine;
            //var prevB = lineB.FromPoint.EndLine;
            //first a quick check to see if points are the same
            if (lineA.FromPoint.Coordinates.IsPracticallySame(lineB.FromPoint.Coordinates, tolerance))
            {
                intersectionCoordinates = lineA.FromPoint.Coordinates;
                where = WhereIsIntersection.BothStarts;
            }
            else
            {
                var fromPointVector = lineB.FromPoint.Coordinates - lineA.FromPoint.Coordinates; // the vector connecting starts
                if (lineACrossLineB == 0) // the two lines are parallel (cross product will be zero)
                {
                    var intersectionFound = false;
                    if (fromPointVector.Cross(lineA.Vector).IsNegligible(tolerance))
                    {
                        // if fromPointCross is also parallel with the line vector (either lineA or lineB since they are parallel to each other)
                        // and since bounding boxes do overlap, then the lines are collinear and overlapping
                        // While there are technically infinite points that are intersecting, we only record when the start of the line
                        // is common. It is possible that the starts (FromPoints) are not overlapping at all - in which case nothing is added.
                        // It is also possible that both FromPoints are on the other line - if so, then we add both. This is the one other place 
                        // where a second IntersectionData is added
                        if (((lineB.ToPoint.Coordinates - lineA.FromPoint.Coordinates).Dot(fromPointVector)).IsLessThanNonNegligible(0, tolerance))
                        {   // since fromPointVector goes from lineA.FromPoint to lineB.FromPoint - if going from line.FromPoint to lineB.ToPoint is
                            // opposite then lineA.FromPoint is on lineB
                            intersectionCoordinates = lineA.FromPoint.Coordinates;
                            where = WhereIsIntersection.AtStartOfA;
                            intersectionFound = true;
                        }
                        if (((lineB.FromPoint.Coordinates - lineA.ToPoint.Coordinates).Dot(fromPointVector)).IsLessThanNonNegligible(0, tolerance))
                        { // now check the other way. Note, since fromPointVector is backwards here, we just make the other vector backwards as well

                            if (intersectionFound) // okay, well, you need to add TWO points. Going to go ahead and finish off the lineB point here
                            {
                                CollinearityTypes collinearB;
                                SegmentRelationship relationshipB;
                                (relationshipB, collinearB) = DeterminePolygonSegmentRelationship(lineA, lineB, WhereIsIntersection.AtStartOfB,
                                    lineACrossLineB, tolerance);

                                intersections.Add(new SegmentIntersection(lineA, lineB, lineB.FromPoint.Coordinates, relationshipB,
                                    WhereIsIntersection.AtStartOfB, collinearB));
                            }
                            else
                            {
                                where = WhereIsIntersection.AtStartOfB;
                                intersectionCoordinates = lineB.FromPoint.Coordinates;
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
                    var t_1 = oneOverdeterminnant * (lineB.Vector.Y * fromPointVector.X - lineB.Vector.X * fromPointVector.Y);
                    if ((!t_1.IsNegligible(tolerance) && t_1 < 0) || t_1 >= 1)
                        return false;
                    var t_2 = oneOverdeterminnant * (lineA.Vector.Y * fromPointVector.X - lineA.Vector.X * fromPointVector.Y);
                    if ((!t_2.IsNegligible(tolerance) && t_2 < 0) || t_2 >= 1)
                        return false;
                    if (t_1.IsNegligible(tolerance))
                    {
                        intersectionCoordinates = lineA.FromPoint.Coordinates;
                        where = WhereIsIntersection.AtStartOfA;
                        if (t_2.IsPracticallySame(1.0, tolerance))
                            possibleDuplicates.Insert(0, (intersections.Count, lineA, lineB.ToPoint.StartLine));
                    }
                    else if (t_2.IsNegligible(tolerance))
                    {
                        intersectionCoordinates = lineB.FromPoint.Coordinates;
                        where = WhereIsIntersection.AtStartOfB;
                        if (t_1.IsPracticallySame(1.0, tolerance))
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB));
                    }
                    else
                    {
                        intersectionCoordinates = lineA.FromPoint.Coordinates + t_1 * lineA.Vector;
                        where = WhereIsIntersection.Intermediate;
                        if (t_1.IsPracticallySame(1.0, tolerance) && t_2.IsPracticallySame(1.0, tolerance))
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB.ToPoint.StartLine));
                        else if (t_1.IsPracticallySame(1.0, tolerance))
                            possibleDuplicates.Insert(0, (intersections.Count, lineA.ToPoint.StartLine, lineB));
                        else if (t_2.IsPracticallySame(1.0, tolerance))
                            possibleDuplicates.Insert(0, (intersections.Count, lineA, lineB.ToPoint.StartLine));
                    }
                }
            }
            CollinearityTypes collinear;
            SegmentRelationship relationship;
            (relationship, collinear) = DeterminePolygonSegmentRelationship(lineA, lineB, where, lineACrossLineB, tolerance);
            intersections.Add(new SegmentIntersection(lineA, lineB, intersectionCoordinates, relationship, where, collinear));
            return true;
        }


        internal static (SegmentRelationship, CollinearityTypes) DeterminePolygonSegmentRelationship(in PolygonEdge lineA,
            in PolygonEdge lineB, in WhereIsIntersection where, in double lineACrossLineB, in double tolerance)
        {
            // first off - handle the intermediate case right away. since it's simple and happens often
            if (where == WhereIsIntersection.Intermediate)
                return (lineACrossLineB < 0 ? SegmentRelationship.CrossOver_BOutsideAfter : SegmentRelationship.CrossOver_AOutsideAfter, CollinearityTypes.None);
            // set up other useful vectors and cross products
            double prevACrossPrevB, lineACrossPrevB, prevACrossLineB;
            Vector2 prevA, prevB;
            // based on where the intersection happens, we can quicken the calculation of these
            if (where == WhereIsIntersection.AtStartOfA)
            { //then lineB and prevB are the same
                prevA = lineA.FromPoint.EndLine.Vector;
                prevB = lineB.Vector;
                lineACrossPrevB = lineACrossLineB;
                prevACrossLineB = prevACrossPrevB = prevA.Cross(prevB);
            }
            else if (where == WhereIsIntersection.AtStartOfB)
            { //then lineA and prevA are the same
                prevA = lineA.Vector;
                prevB = lineB.FromPoint.EndLine.Vector;
                prevACrossLineB = lineACrossLineB;
                lineACrossPrevB = prevACrossPrevB = prevA.Cross(prevB);
            }
            else // then where == BothLinesStart
            {
                prevA = lineA.FromPoint.EndLine.Vector;
                prevB = lineB.FromPoint.EndLine.Vector;
                prevACrossLineB = prevA.Cross(lineB.Vector);
                lineACrossPrevB = lineA.Vector.Cross(prevB);
                prevACrossPrevB = prevA.Cross(prevB);
            }
            if (prevACrossPrevB.IsNegligible(tolerance)) prevACrossPrevB = 0;
            if (lineACrossPrevB.IsNegligible(tolerance)) lineACrossPrevB = 0;
            if (prevACrossLineB.IsNegligible(tolerance)) prevACrossLineB = 0;


            if (lineACrossLineB == 0 && prevACrossPrevB == 0 && lineACrossPrevB == 0 && prevACrossLineB == 0)
            {
                if (lineA.Vector.Dot(lineB.Vector) > 0)
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.BothSameDirection);
                return (SegmentRelationship.NoOverlap, CollinearityTypes.BothOppositeDirection);
            }
            // most restrictive is when both lines are parallel
            if (lineACrossLineB == 0 && prevACrossPrevB == 0)
            {
                var lineADotLineB = lineA.Vector.Dot(lineB.Vector);
                var prevADotPrevB = prevA.Dot(prevB);
                if (lineADotLineB > 0 && prevADotPrevB > 0)
                    //case 16
                    return (SegmentRelationship.DoubleOverlap, CollinearityTypes.BothSameDirection);
                if (lineADotLineB < 0 && prevADotPrevB < 0 && lineACrossPrevB != 0)
                    // a rare version of case 5 or 6 where the lines enter and leave the point on parallel lines, but
                    // there is no collinearity! A's cross product of the corner would be the same as B's. If this corner
                    // cross is positive/convex, then no overlap. if concave, then double overlap
                    return (prevA.Cross(lineA.Vector) > 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                        CollinearityTypes.None);
                if (prevADotPrevB < 0) // then lineADotLineB would be positive, and polygons were heading
                                       // right to each other on parallel lines before joining. this is a rare case 7 or 8
                    return (prevA.Cross(lineA.Vector) > 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
                        CollinearityTypes.After);
                if (lineADotLineB < 0) // then prevADotPrevB would be positive. the polygon diverges in 
                                       // opposite parallel directions. this is a rare case 9 or 10
                    return (prevA.Cross(lineA.Vector) >= 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
                             CollinearityTypes.Before);
            }
            if (lineACrossPrevB == 0 && prevACrossLineB == 0)
            {
                var lineADotPrevB = lineA.Vector.Dot(prevB);
                var prevADotLineB = prevA.Dot(lineB.Vector);
                if (lineADotPrevB < 0 && prevADotLineB < 0)  // case 15
                    return (SegmentRelationship.NoOverlap, CollinearityTypes.BothOppositeDirection);
                if (lineADotPrevB > 0 && prevADotLineB > 0)  // a very unusual case (although it shows up in the chunky polygon
                    return (prevA.Cross(lineA.Vector) >= 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB,
                        CollinearityTypes.None);
                if (lineADotPrevB > 0) // then prevADotLineB would be negative. 
                                       // calculate if polygon A's corner is convex or concave. if convex then case 11 (no overlap) if concave then double (case 12)
                    return (prevA.Cross(lineA.Vector) >= 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                        CollinearityTypes.ABeforeBAfter);
                if (prevADotLineB > 0) // then lineADotPrevB would be negative, 
                    return (prevA.Cross(lineA.Vector) >= 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap,
                             CollinearityTypes.AAfterBBefore);
            }
            // the remaining conditions require these (remember: positive = convex, negative = concave)
            var aCornerCross = prevA.Cross(lineA.Vector);
            var bCornerCross = prevB.Cross(lineB.Vector);

            // now to check if just one of these is zero
            if (lineACrossLineB == 0 && lineA.Vector.Dot(lineB.Vector) > 0)
            {   // if the dot product is less than zero than it'll be pulled in to conditions below like no overlap. So this is Case 7 & 8
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.AEnclosesB, CollinearityTypes.After);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.BEnclosesA, CollinearityTypes.After);
                return (prevACrossPrevB > 0 ? SegmentRelationship.BEnclosesA : SegmentRelationship.AEnclosesB, CollinearityTypes.After);
            }
            if (prevACrossPrevB == 0 && prevA.Dot(prevB) > 0)
            {   //like the previous condition if this hasn't been captured by the aboe then lineACrossLineB !=0. So this is Case 9 & 10
                if (aCornerCross < 0 && bCornerCross > 0) return (SegmentRelationship.AEnclosesB, CollinearityTypes.Before);
                if (aCornerCross > 0 && bCornerCross < 0) return (SegmentRelationship.BEnclosesA, CollinearityTypes.Before);
                return (lineACrossLineB > 0 ? SegmentRelationship.AEnclosesB : SegmentRelationship.BEnclosesA, CollinearityTypes.Before);
            }
            if (prevACrossLineB == 0 && prevA.Dot(lineB.Vector) < 0)
            {   //like the previous condition if this hasn't been captured by the above then lineACrossPrevB !=0. So this is Case 11 & 12
                if (aCornerCross > 0 && bCornerCross > 0) return (SegmentRelationship.NoOverlap, CollinearityTypes.ABeforeBAfter);
                if (aCornerCross < 0 && bCornerCross < 0) return (SegmentRelationship.DoubleOverlap, CollinearityTypes.ABeforeBAfter);
                return (lineACrossPrevB > 0 ? SegmentRelationship.NoOverlap : SegmentRelationship.DoubleOverlap, CollinearityTypes.ABeforeBAfter);
            }
            if (lineACrossPrevB == 0 && lineA.Vector.Dot(prevB) < 0)
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


        private static PolygonEdge[] GetOrderedLines(List<Vertex2D> orderedPoints)
        {
            var length = orderedPoints.Count;
            var result = new PolygonEdge[length];
            var smallHashOfLinesOfEqualX = new HashSet<PolygonEdge>();
            var k = 0;
            for (int i = 0; i < length; i++)
            {

                var point = orderedPoints[i];
                if (point.EndLine.OtherPoint(point).X > point.X)
                    result[k++] = point.EndLine;
                else if (point.EndLine.OtherPoint(point).X == point.X &&
                         !smallHashOfLinesOfEqualX.Contains(point.EndLine))
                {
                    result[k++] = point.EndLine;
                    smallHashOfLinesOfEqualX.Add(point.EndLine);
                }
                if (point.StartLine.OtherPoint(point).X > point.X)
                    result[k++] = point.StartLine;
                else if (point.StartLine.OtherPoint(point).X == point.X &&
                         !smallHashOfLinesOfEqualX.Contains(point.StartLine))
                {
                    result[k++] = point.StartLine;
                    smallHashOfLinesOfEqualX.Add(point.StartLine);
                }

                if (k >= length) break;
            }
            return result;
        }

        private static List<SegmentIntersection> GetSelfIntersections(this Polygon polygonA, double tolerance)
        {
            var intersections = new List<SegmentIntersection>();
            var possibleDuplicates = new List<(int index, PolygonEdge lineA, PolygonEdge lineB)>();
            var numLines = polygonA.Lines.Count;
            var orderedLines = GetOrderedLines(polygonA.OrderedXVertices);
            for (int i = 0; i < numLines - 1; i++)
            {
                var current = orderedLines[i];
                for (int j = i + 1; j < numLines; j++)
                {
                    var other = orderedLines[j];
                    if (current.XMax < orderedLines[j].XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    AddIntersectionBetweenLines(current, other, intersections, possibleDuplicates, tolerance);
                }
            }
            RemoveDuplicateIntersections(possibleDuplicates, intersections, tolerance);
            return intersections;
        }


    }
}
