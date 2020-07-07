using Priority_Queue;
using System;
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
        private static bool IsNonIntersectingPolygonInside(this Polygon outer, Polygon inner, out bool onBoundary)
        {
            onBoundary = false;
            if (Math.Abs(inner.Area) > outer.Area) return false;
            foreach (var vector2 in inner.Path)
            {
                if (!outer.IsPointInsidePolygon(vector2, out _, out _, out var thisPointOnBoundary, true))
                    // negative has a point outside of positive. no point in checking other points
                    return false;
                if (thisPointOnBoundary) onBoundary = true;
                else
                    return true;
            }
            return true; //all points are on boundary!
        }

        #region Line Intersections with Polygon

        public static List<Vector2> AllPolygonIntersectionPointsAlongLine(IEnumerable<List<Vector2>> polygons, Vector2 lineReference, double lineDirection,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongLine(polygons.Select(p => new Polygon(p, false)), lineReference,
                lineDirection, numSteps, stepSize, out firstIntersectingIndex);
        }
        public static List<Vector2> AllPolygonIntersectionPointsAlongLine(IEnumerable<Polygon> polygons, Vector2 lineReference, double lineDirection,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            throw new NotImplementedException();
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongX(IEnumerable<List<Vector2>> polygons, double startingXValue,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongX(polygons.Select(p => new Polygon(p, false)), startingXValue,
                numSteps, stepSize, out firstIntersectingIndex);
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongX(IEnumerable<Polygon> polygons, double startingXValue,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Vertices).OrderBy(p => p.X).ToList();
            var currentLines = new HashSet<PolygonSegment>();
            var nextDistance = sortedPoints.First().X;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingXValue) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var x = startingXValue + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.X <= x)
                {
                    if (x.IsPracticallySame(thisPoint.X)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    x += Math.Min(stepSize, sortedPoints[pIndex + 1].X) / 10.0;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.YGivenX(x, out _);
                intersections.Add(intersects.OrderBy(y => y).ToArray());
            }
            return intersections;
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<IEnumerable<Vector2>> polygons, double startingYValue, int numSteps, double stepSize,
              out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongY(polygons.Select(p => new Polygon(p, false)), startingYValue,
                numSteps, stepSize, out firstIntersectingIndex);
        }
        /// <summary>
        /// Returns a list of double arrays. the double array values correspond to only the x-coordinates. the y-coordinates are determined by the input.
        /// y = startingYValue + (i+firstIntersectingIndex)*stepSize
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="startingYValue">The starting y value.</param>
        /// <param name="numSteps">The number steps.</param>
        /// <param name="stepSize">Size of the step.</param>
        /// <param name="firstIntersectingIndex">First index of the intersecting.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        public static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<Polygon> polygons, double startingYValue, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Vertices).OrderBy(p => p.Y).ToList();
            var currentLines = new HashSet<PolygonSegment>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingYValue) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = startingYValue + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.Y <= y)
                {
                    if (y.IsPracticallySame(thisPoint.Y)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    y += Math.Min(stepSize, sortedPoints[pIndex].Y) / 10.0;

                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.XGivenY(y, out _);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }
        #endregion


        /// <summary>
        /// Gets the polygon relationship of PolygonA to PolygonB and the intersections between them.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonRelationship.</returns>
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
            // value. Instead of directly sorting the Lines, which will have many repeat XMin values (since every
            // pair of lines meet at the a given point), the points are sorted first. These sorted points are
            // used in the ArePointsInsidePolygon function as well.
            // the prev. 3 lines a simpler solution would be the following line.
            var orderedAPoints = polygonA.AllPolygons.SelectMany(poly => poly.Vertices).OrderBy(p => p.X).ToList();
            if (polygonA.Lines.Count == polygonB.Lines.Count) ; //this silly little line is simply to ensure that the Lines
            // property has been invoked before the next function (GetOrderedLines), which requires that lines and vertices be properly connected
            var aLines = GetOrderedLines(orderedAPoints);
            //repeat for the lines in B
            var orderedBPoints = (polygonB.AllPolygons.SelectMany(poly => poly.Vertices)).OrderBy(p => p.X).ToList();
            var bLines = GetOrderedLines(orderedBPoints);

            var aIndex = 0;
            var bIndex = 0;
            while (aIndex < aLines.Length && bIndex < bLines.Length) // this while loop increments both B lines and A lines
            {
                if (aLines[aIndex].XMin < bLines[bIndex].XMin) // if the next A-line is lower compare it to all B-lines
                {
                    var aLine = aLines[aIndex++];
                    var localBIndex = bIndex; //the localBIndex is incremented in the following loop, but we
                                              //need to come back to the main bIndex above
                    while (localBIndex < bLines.Length && aLine.XMax >= bLines[localBIndex].XMin)
                        // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        AddIntersectionBetweenLines(aLine, bLines[localBIndex++], intersections);
                }
                else
                {
                    var bLine = bLines[bIndex++];
                    var localAIndex = aIndex;
                    while (localAIndex < aLines.Length && bLine.XMax >= aLines[localAIndex].XMin)
                        AddIntersectionBetweenLines(aLines[localAIndex++], bLine, intersections);
                }
            }
            if (CheckForIntersection(intersections))
            {
                // I don't have a reason to check the following cases after all. It is still an intersection and other
                // parts of the code treat it as such.
                //if (ArePointsInsidePolygonLines(aLines, aLines.Length, orderedBPoints, out _, true))
                //    return PolygonRelationship.BVerticesInsideAButLinesIntersect;
                //if (ArePointsInsidePolygonLines(bLines, bLines.Length, orderedAPoints, out _, true))
                //    return PolygonRelationship.AVerticesInsideBButLinesIntersect;
                return PolygonRelationship.Intersect;
            }
            // so, there are no interesections that indicate an overlap - only places where the polygons 
            // touch or have collinear edges
            bool onBoundary;
            if (BoundingBoxEncompasses(polygonA, polygonB))
            {
                foreach (var hole in polygonA.Holes)
                {
                    if (hole.IsNonIntersectingPolygonInside(polygonB, out onBoundary))
                    {
                        if (intersections.Count > 0) return PolygonRelationship.BInsideAButBordersTouch;
                        return PolygonRelationship.BIsInsideHoleOfA;
                    }
                }
                if (polygonA.IsNonIntersectingPolygonInside(polygonB, out onBoundary))
                {
                    if (intersections.Count > 0) return PolygonRelationship.BInsideAButBordersTouch;
                    return PolygonRelationship.BIsCompletelyInsideA;
                }
            }
            else if (BoundingBoxEncompasses(polygonB, polygonA))
            {
                foreach (var hole in polygonB.Holes)
                {
                    if (hole.IsNonIntersectingPolygonInside(polygonA, out onBoundary))
                    {
                        if (intersections.Count > 0) return PolygonRelationship.AInsideBButBordersTouch;
                        return PolygonRelationship.AIsInsideHoleOfB;
                    }
                }
                if (polygonB.IsNonIntersectingPolygonInside(polygonA, out onBoundary))
                {
                    if (intersections.Count > 0) return PolygonRelationship.AInsideBButBordersTouch;
                    return PolygonRelationship.AIsCompletelyInsideB;
                }
            }
            if (intersections.Count == 0) return PolygonRelationship.Separated;
            // what remains are polygons that are exterior to one another but touch: #1 only vertex to vertex
            // (PolygonSegmentRelationship.EndPointsTouch), #2 only vertex to edge (PolygonSegmentRelationship.
            // TJunctionXReflects), or #3 edge to edge (PolygonSegmentRelationship.TJunctionBMerges).
            else return PolygonRelationship.SeparatedButBordersTouch;
        }

        private static bool CheckForIntersection(List<IntersectionData> intersections)
        {
            var atLeastOneAEncompassingB = false;
            var atLeastOneBEncompassingA = false;
            foreach (var intersectionData in intersections)
            {
                if ((byte)intersectionData.Relationship >= 28)
                    return true;
                else if ((byte)intersectionData.Relationship >= 13 && (byte)intersectionData.Relationship <= 15)
                {
                    if (atLeastOneBEncompassingA)
                        return true;
                    atLeastOneAEncompassingB = true;
                }
                else if ((byte)intersectionData.Relationship >= 21 && (byte)intersectionData.Relationship <= 23)
                {
                    if (atLeastOneAEncompassingB)
                        return true;
                    atLeastOneBEncompassingA = true;
                }
            }
            return false;
        }


        /// <summary>
        /// Returns true is polygons overlap at more than their boundary. This is simply a small wrapper to the bigger function: 
        /// GetPolygonRelationshipAndIntersections. However, in certain contexts, one may simply want a boolean especially if used
        /// in conditional expressions. This is meant to serve in those purposes.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="relationship">The relationship.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool PolygonsOverlap(this Polygon polygonA, Polygon polygonB, out List<IntersectionData> intersections, out PolygonRelationship relationship)
        {
            relationship = polygonA.GetPolygonRelationshipAndIntersections(polygonB, out intersections);
            return relationship != PolygonRelationship.Separated && relationship != PolygonRelationship.SeparatedButBordersTouch;
        }


        /// <summary>
        /// Determines if Two polygon line segments intersect. Because they are part of a polygon, it is decided to make the
        /// fromPoint Inclusive, and the toPoint exclusive. This if lines seem to touch are their endpoints, it is only recorded
        /// if both points are from points. Also no "close" operations are used (e.g. IsPracticallySame). Because the method is
        /// intended to be invoked for all lines on the polygon, this prevents an intersection from being caught by muliple lines,
        /// makes the methods simpler (easier to debug and edit) and quicker.
        /// If two lines are collinear, they are not considered intersecting.
        /// </summary>
        /// <param name="lineA">The line a.</param>
        /// <param name="lineB">The line b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonSegmentRelationship.</returns>
        private static void AddIntersectionBetweenLines(PolygonSegment lineA, PolygonSegment lineB,
            List<IntersectionData> intersections)
        {
            // first check if bounding boxes overlap. If they don't then return false here
            if (lineA.YMax < lineB.YMin || lineB.YMax < lineA.YMin
                                        || lineA.XMax < lineB.XMin || lineB.XMax < lineA.XMin)
                return; // the two lines do not touch since their bounding boxes do not overlap
            // okay, so bounding boxes DO overlap
            var intersectionCoordinates = Vector2.Null;
            PolygonSegmentRelationship relationship = PolygonSegmentRelationship.Unknown;
            var lineACrossLineB = lineA.Vector.Cross(lineB.Vector); //2D cross product, determines if parallel
            var prevA = lineA.FromPoint.EndLine;
            var prevB = lineB.FromPoint.EndLine;
            //first a quick check to see if points are the same
            if (lineA.FromPoint.Coordinates.IsPracticallySame(lineB.FromPoint.Coordinates))
            {
                intersectionCoordinates = lineA.FromPoint.Coordinates;
                relationship = PolygonSegmentRelationship.LinesSharePoint;
                if (lineACrossLineB.IsNegligible()) // the two lines are parallel (cross product will be zero)
                {
                    if (lineA.Vector.Dot(lineB.Vector) < 0) relationship |= PolygonSegmentRelationship.OppositeDirections;
                    else relationship |= PolygonSegmentRelationship.SameLineAfterPoint;
                }
            }
            else
            {
                var fromPointCross = lineB.FromPoint.Coordinates - lineA.FromPoint.Coordinates; // the vector connecting starts
                if (lineACrossLineB.IsNegligible()) // the two lines are parallel (cross product will be zero)
                {
                    var intersectionFound = false;
                    if (fromPointCross.Cross(lineA.Vector).IsNegligible())
                    {
                        // if fromPointCross is also parallel with the line vector (either lineA or lineB since they are parallel to each other)
                        // and since bounding boxes do overlap, then the lines are collinear and overlapping
                        // While there are technically infinite points that are intersecting, we only record when the start of the line
                        // is common. It is possible that the starts (FromPoints) are not overlapping at all - in which case nothing is added.
                        // It is also possible that both FromPoints are on the other line - if so, then we add both. This is the one other place 
                        // where a second IntersectionData is added
                        if (lineA.Vector.Dot(lineB.Vector) < 0) relationship |= PolygonSegmentRelationship.OppositeDirections;
                        if ((lineB.ToPoint.Coordinates - lineA.FromPoint.Coordinates).Dot(fromPointCross) < 0)
                        {   // since vStart goes from lineA.FromPoint to lineB.FromPoint - if going from line.FromPoint to lineB.ToPoint is
                            // opposite then lineA.FromPoint is on lineB
                            intersectionCoordinates = lineA.FromPoint.Coordinates;
                            relationship |= PolygonSegmentRelationship.AtStartOfA | PolygonSegmentRelationship.SameLineAfterPoint;
                            intersectionFound = true;
                            prevB = lineB;
                        }
                        if ((lineB.FromPoint.Coordinates - lineA.ToPoint.Coordinates).Dot(fromPointCross) < 0)
                        { // now check the otherway. Note, since vStart is backwards here, we just make the other vector backwards as well
                            prevA = lineA;
                            if (intersectionFound) // okay, well, you need to add TWO points. Going to go ahead and finish off the lineB point here
                                intersections.Add(new IntersectionData(lineA, lineB, lineB.FromPoint.Coordinates,
                                    DeterminePolygonSegmentRelationship(lineA, lineB, prevA, prevB, lineACrossLineB,
                                     relationship | PolygonSegmentRelationship.AtStartOfB | PolygonSegmentRelationship.SameLineAfterPoint)));
                            else
                            {
                                relationship |= PolygonSegmentRelationship.AtStartOfB | PolygonSegmentRelationship.SameLineAfterPoint;
                                intersectionCoordinates = lineB.FromPoint.Coordinates;
                                intersectionFound = true;
                            }
                        }
                        //technically the lines overlap even if the previous two condition are not met, but since the overlap doesn't include
                        // either from Point, then we do not record it. It will be recorded when the next segment is checked
                    }
                    if (!intersectionFound) return;// otherwise the lines are parallel but not at same distance/intercept or they are
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
                    var t_1 = oneOverdeterminnant * (lineB.Vector.Y * fromPointCross.X - lineB.Vector.X * fromPointCross.Y);
                    var t_2 = oneOverdeterminnant * (lineA.Vector.Y * fromPointCross.X - lineA.Vector.X * fromPointCross.Y);
                    if (t_1 < 0 || t_1 >= 1 || t_2 < 0 || t_2 >= 1) return; // PolygonSegmentRelationship.Separated;
                    if (t_1.IsNegligible())
                    {
                        intersectionCoordinates = lineA.FromPoint.Coordinates;
                        relationship = PolygonSegmentRelationship.AtStartOfA;
                        prevB = lineB;
                    }
                    else if (t_2.IsNegligible())
                    {
                        intersectionCoordinates = lineB.FromPoint.Coordinates;
                        relationship = PolygonSegmentRelationship.AtStartOfB;
                        prevA = lineA;
                    }
                    else
                    {
                        intersectionCoordinates = lineA.FromPoint.Coordinates + t_1 * lineA.Vector;
                        relationship = PolygonSegmentRelationship.Overlapping;
                    }
                }
            }
            intersections.Add(new IntersectionData(lineA, lineB, intersectionCoordinates,
                DeterminePolygonSegmentRelationship(lineA, lineB, prevA, prevB, lineACrossLineB, relationship)));
        }

        private static PolygonSegmentRelationship DeterminePolygonSegmentRelationship(PolygonSegment lineA, PolygonSegment lineB,
            PolygonSegment prevA, PolygonSegment prevB, double lineACrossLineB, PolygonSegmentRelationship relationship)
        {
            if (relationship == PolygonSegmentRelationship.Overlapping) return relationship; //this only happens when line-A and line-B are not parallel and
            // it is known that there is an intermediate point (the default case). So, the value of the relationshipByte is already set.
            var prevACrossPrevB = prevA.Vector.Cross(prevB.Vector);
            if (prevACrossPrevB.IsNegligible()) relationship |= PolygonSegmentRelationship.SameLineBeforePoint;

            var aCross = prevA.Vector.Cross(lineA.Vector);

            if ((relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 && prevACrossPrevB.IsNegligible()
                && aCross.IsNegligible())
                return relationship | PolygonSegmentRelationship.AllParallel;

            var bCross = prevB.Vector.Cross(lineB.Vector);
            var lineACrossPrevB = lineA.Vector.Cross(prevB.Vector);
            var prevACrossLineB = prevA.Vector.Cross(lineB.Vector);
            // regarding the next two booleans...when A corner is convex (aCross>=0), then we check if the two cross products made with the
            // a given b line are BOTH positive - meaning that the b line is between the A lines.
            // When A's corner is concave, we check for the oppositve convex angle of A - to see if the B line is between those
            // that's the easy part! getting the changes in sign correct makes it more complicated.
            var lineBIsInsideA = (aCross >= 0 && lineACrossLineB > 0 && prevACrossLineB > 0) ||
                                     (aCross < 0 && !(lineACrossLineB < 0 && prevACrossLineB < 0));
            // this expression is the same, but the previous B vector is into the corner and thus, we need to negate it (or rather just check for negative)
            var prevLineBIsInsideA = (aCross >= 0 && lineACrossPrevB < 0 && prevACrossPrevB < 0) ||
                                         (aCross < 0 && !(lineACrossPrevB > 0 && prevACrossPrevB > 0));
            // we actually have to do the same with lineB - it's not enough to know if A is inside B
            var lineAIsInsideB = (bCross >= 0 && lineACrossLineB < 0 && lineACrossPrevB < 0) ||
                                     (bCross < 0 && !(lineACrossLineB > 0 && lineACrossPrevB > 0));
            var prevLineAIsInsideB = (bCross > 0 && prevACrossLineB > 0 && prevACrossPrevB > 0) ||
                                        (bCross < 0 && !(prevACrossLineB < 0 && prevACrossPrevB < 0));
            // in the remaining conditions there are 16 possible combinations of the four booleans: lineBIsInsideA-prevLineBIsInsideA--lineAIsInsideB-prevLineAIsInsideB
            // first off, if they are all false, then it clearly is a "glance"
            if (!(lineBIsInsideA || prevLineBIsInsideA || lineAIsInsideB || prevLineAIsInsideB)) // FF-FF
                relationship += 0b00100; //glance
                                         // if there is a true on both sides then we have an overlap
            else
            if ((lineBIsInsideA || prevLineBIsInsideA) && (lineAIsInsideB || prevLineAIsInsideB))
                // TT-TT, TT-FT, TT-TF, TF-TT, TF-TF, TF-FT, FT-FT, FT-TF, FT-TT
                relationship |= PolygonSegmentRelationship.Overlapping;
            else if ((lineBIsInsideA || prevLineBIsInsideA) && (!lineAIsInsideB && !prevLineAIsInsideB))
                // TF-FF, FT-FF, TT-FF (although, I don't think TF-FF or FT-FF can occur)
                relationship |= PolygonSegmentRelationship.AEncompassesB;
            else if ((!lineBIsInsideA && !prevLineBIsInsideA) && (lineAIsInsideB || prevLineAIsInsideB))
                // FF-TF, FF-FT, FF-TT (although, I don't think FF-TF or FF-FT can occur)
                relationship |= PolygonSegmentRelationship.BEncompassesA;
            /*else*/ // both lineACrossLineB is Negligible and prevCross is negligible, which mean all the lines are parallel
            // which means unknown, which means  add 0b000000 to the relationByte, which mean do nothing!
            //}
            return (PolygonSegmentRelationship)relationship;
        }

        private static bool BoundingBoxEncompasses(this Polygon polygonA, Polygon polygonB)
        {
            return (polygonA.MaxX >= polygonB.MaxX
                && polygonA.MaxY >= polygonB.MaxY
                && polygonA.MinX <= polygonB.MinX
                && polygonA.MinY <= polygonB.MinY);
        }


        private static PolygonSegment[] GetOrderedLines(List<Vertex2D> orderedPoints)
        {
            var length = orderedPoints.Count;
            var result = new PolygonSegment[length];
            var smallHashOfLinesOfEqualX = new HashSet<PolygonSegment>();
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

        private static List<IntersectionData> GetSelfIntersections(this Polygon polygonA)
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
                    AddIntersectionBetweenLines(current, other, intersections);
                }
            }
            return intersections;
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
        internal static bool IsPointInsidePolygon(this Polygon polygon, Vector2 pointInQuestion, out PolygonSegment closestLineAbove,
            out PolygonSegment closestLineBelow, out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
                                         //This function has three layers of checks. 
                                         //(1) Check if the point is inside the axis aligned bounding box. If it is not, then return false.
                                         //(2) Check if the point is == to a polygon point, return onBoundaryIsInside.
                                         //(3) Use line-sweeping / ray casting to determine if the polygon contains the point.
            closestLineAbove = null;
            closestLineBelow = null;
            onBoundary = false;
            //1) Check if center point is within bounding box of each polygon
            if (qX < polygon.MinX || qY < polygon.MinY ||
                qX > polygon.MaxX || qY > polygon.MaxY)
                return false;
            //2) If the point in question is == a point in points, then it is inside the polygon
            if (polygon.Path.Any(point => point.IsPracticallySame(pointInQuestion)))
            {
                onBoundary = true;
                return onBoundaryIsInside;
            }
            var numberAbove = 0;
            var numberBelow = 0;
            var minDistAbove = double.PositiveInfinity;
            var minDistBelow = double.PositiveInfinity;
            foreach (var line in polygon.Lines)
            {
                if ((line.FromPoint.X < qX) == (line.ToPoint.X < qX))
                    // if the X values are both on the same side, then ignore it. We are looking for
                    // lines that 'straddle' the x-values. Then we want to know if the lines' y values
                    // are above or below
                    continue;
                var lineYValue = line.YGivenX(qX, out _); //this out parameter is the same condition
                                                          //as 5 lines earlier, but that check is kept for efficiency
                var yDistance = lineYValue - qY;
                if (yDistance > 0)
                {
                    numberAbove++;
                    if (minDistAbove > yDistance)
                    {
                        minDistAbove = yDistance;
                        closestLineAbove = line;
                    }
                }
                else if (yDistance < 0)
                {
                    yDistance = -yDistance;
                    numberBelow++;
                    if (minDistBelow > yDistance)
                    {
                        minDistBelow = yDistance;
                        closestLineBelow = line;
                    }
                }
                else //else, the point is on a line in the polygon
                {
                    closestLineAbove = closestLineBelow = line;
                    onBoundary = true;
                    return true;
                }
            }
            if (numberBelow != numberAbove)
            {
                Trace.WriteLine("In IsPointInsidePolygon, the number of points above is not equal to the number below");
                numberAbove = numberBelow = Math.Max(numberBelow, numberAbove);
            }
            return numberAbove % 2 != 0;
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
            out bool onBoundary, bool onBoundaryIsInside = true, double tolerance = Constants.BaseTolerance)
        {
            var sortedLines = polygon.Lines.OrderBy(line => line.XMin).ToList();
            var sortedPoints = pointsInQuestion.OrderBy(pt => pt.X).ToList();
            return ArePointsInsidePolygonLines(sortedLines, sortedLines.Count, sortedPoints, out onBoundary, onBoundaryIsInside, tolerance);
        }
        internal static bool ArePointsInsidePolygonLines(IList<PolygonSegment> sortedLines, int numSortedLines, List<Vertex2D> sortedPoints,
            out bool onBoundary, bool onBoundaryIsInside = true, double tolerance = Constants.BaseTolerance)
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
                    var lineYValue = line.YGivenX(p.X, out _);
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
        public static bool IsPointInsidePolygon(this List<Vector2> path, Vector2 pointInQuestion, bool onBoundaryIsInside = false)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
                                         //Check if the point is the same as any of the polygon's points
            var polygonIsLeftOfPoint = false;
            var polygonIsRightOfPoint = false;
            var polygonIsAbovePoint = false;
            var polygonIsBelowPoint = false;
            foreach (var point in path)
            {
                if (point.IsPracticallySame(pointInQuestion))
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
                    if (pointInQuestion.X.IsPracticallySame(xCoordWithSameY))
                        return onBoundaryIsInside;
                    else if (pointInQuestion.X < xCoordWithSameY)
                        inside = !inside; // it is inside if the number of lines to the right of the point is odd
                }
            }
            return inside;
        }

    }
}
