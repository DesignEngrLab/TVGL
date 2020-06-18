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
                    while (localBIndex < bLines.Length && aLine.XMax > bLines[localBIndex].XMin)
                        // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        PolygonSegmentIntersection(aLine, bLines[localBIndex++], intersections);
                }
                else
                {
                    var bLine = bLines[bIndex++];
                    var localAIndex = aIndex;
                    while (localAIndex < aLines.Length && bLine.XMax > aLines[localAIndex].XMin)
                        PolygonSegmentIntersection(aLines[localAIndex++], bLine, intersections);
                }
            }
            if (intersections.Any(intersect => ((byte)intersect.Relationship & 0b10) != 0))
            {
                if (ArePointsInsidePolygonLines(aLines, aLines.Length, orderedBPoints, out _, false))
                    return PolygonRelationship.BVerticesInsideAButLinesIntersect;
                if (ArePointsInsidePolygonLines(bLines, bLines.Length, orderedAPoints, out _, false))
                    return PolygonRelationship.AVerticesInsideBButLinesIntersect;
                return PolygonRelationship.Intersect;
            }
            // so, there are no interesections only places where the polygons touch are have collinear edges
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
            // In the last case, it is possible that two or more consecutive SegmentRelationships need to be checked
            // to determine if the polygons truly intersect (cross into each other). This is a bit of work, so an
            // additional function below handles this case.
            if (intersections.Any(intersect => ((byte)intersect.Relationship & 0b1000) != 0))
                return DetermineIfCoincidentPolygonsIntersect(intersections);
            else return PolygonRelationship.SeparatedButBordersTouch;
        }

        /// <summary>
        /// Determines if coincident polygons intersect.
        /// </summary>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonRelationship.</returns>
        private static PolygonRelationship DetermineIfCoincidentPolygonsIntersect(List<IntersectionData> intersections)
        {
            // gather  all the intersections that are coincident or reflect (by reflect, we mean not intersecting. 0b10=1 for intersect)
            var mergeIntersects = intersections.Where(intersect => ((byte)intersect.Relationship & 0b110000) != 0
                                                                   && ((byte)intersect.Relationship & 0b10) == 0).ToList();
            var allCoincidentChains = new List<List<IntersectionData>>();

            while (mergeIntersects.Count > 0)
            {
                var startingIntersection =
                    mergeIntersects.FirstOrDefault(intersect => ((byte)intersect.Relationship & 0b1000) != 0);
                if (startingIntersection == null) break;
                var sameDirection = ((byte)startingIntersection.Relationship & 64) == 0;
                var coincidentChain = new List<IntersectionData> { startingIntersection };
                mergeIntersects.Remove(startingIntersection);
                while (true)
                {
                    var nextIntersection = mergeIntersects.FirstOrDefault(intersect =>
                        (intersect.EdgeA == coincidentChain[0].EdgeA.FromPoint.EndLine && intersect.EdgeB == coincidentChain[0].EdgeB)
                        || (sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA && intersect.EdgeB == coincidentChain[0].EdgeB.FromPoint.EndLine)
                        || (sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA.FromPoint.EndLine && intersect.EdgeB == coincidentChain[0].EdgeB.FromPoint.EndLine)
                        || (!sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA && intersect.EdgeB == coincidentChain[0].EdgeB.ToPoint.StartLine)
                        || (!sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA.FromPoint.EndLine && intersect.EdgeB == coincidentChain[0].EdgeB.ToPoint.StartLine));
                    if (nextIntersection == null) break;
                    coincidentChain.Insert(0, nextIntersection);
                    mergeIntersects.Remove(nextIntersection);
                }
                while (true)
                {
                    var nextIntersection = mergeIntersects.FirstOrDefault(intersect =>
                        (intersect.EdgeA == coincidentChain[0].EdgeA.ToPoint.StartLine && intersect.EdgeB == coincidentChain[0].EdgeB)
                        || (sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA && intersect.EdgeB == coincidentChain[0].EdgeB.ToPoint.StartLine)
                        || (sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA.ToPoint.StartLine && intersect.EdgeB == coincidentChain[0].EdgeB.ToPoint.StartLine)
                        || (!sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA && intersect.EdgeB == coincidentChain[0].EdgeB.FromPoint.EndLine)
                        || (!sameDirection && intersect.EdgeA == coincidentChain[0].EdgeA.ToPoint.StartLine && intersect.EdgeB == coincidentChain[0].EdgeB.FromPoint.EndLine));
                    if (nextIntersection == null) break;
                    coincidentChain.Add(nextIntersection);
                    mergeIntersects.Remove(nextIntersection);
                }
                allCoincidentChains.Add(coincidentChain);
            }
            if (allCoincidentChains.Count <= 1) return PolygonRelationship.SeparatedButBordersTouch;
            foreach (var coincidentChain in allCoincidentChains)
            {
                if (coincidentChain[0].EdgeA.FromPoint.EndLine.Vector.Cross(coincidentChain[0].EdgeB.Vector)
                    * coincidentChain[^1].EdgeA.Vector.Cross(coincidentChain[^1].EdgeB.Vector) > 0)
                    return PolygonRelationship.Intersect;
            }
            return PolygonRelationship.SeparatedButBordersTouch;
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
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="intersectionPointForBMerging">The intersection point for b merging.</param>
        /// <returns>PolygonSegmentRelationship.</returns>
        private static void PolygonSegmentIntersection(PolygonSegment lineA, PolygonSegment lineB,
            List<IntersectionData> intersections)
        {
            // first check if bounding boxes overlap. If they don't then return false here
            if (lineA.YMax < lineB.YMin || lineB.YMax < lineA.YMin
                                        || lineA.XMax < lineB.XMin || lineB.XMax < lineA.XMin)
                return; // PolygonSegmentRelationship.Separated;
            // okay, so bounding boxes overlap
            //first a quick check to see if points are the same
            var vCross = lineA.Vector.Cross(lineB.Vector); //2D cross product, determines if parallel
            if (lineA.FromPoint.Coordinates.IsPracticallySame(lineB.FromPoint.Coordinates))
            {
                if (vCross.IsNegligible())
                {
                    intersections.Add(new IntersectionData(lineA, lineB, lineA.FromPoint.Coordinates,
                        lineA.Vector.Dot(lineB.Vector) > 0
                            ? PolygonSegmentRelationship.TJunctionAMergeSameDir
                            : PolygonSegmentRelationship.TJunctionAMergeOppDir));
                }
                else intersections.Add(new IntersectionData(lineA, lineB, lineA.FromPoint.Coordinates,
                   vCross * (lineA.FromPoint.EndLine.Vector.Cross(lineB.FromPoint.EndLine.Vector)) > 0
                       ? PolygonSegmentRelationship.EndPointsCross
                       : PolygonSegmentRelationship.EndPointsTouch));
                return;
            }

            var vStart = lineB.FromPoint.Coordinates - lineA.FromPoint.Coordinates; // the vector connecting starts
            if (vCross.IsNegligible()) // the two lines are parallel (cross product will be zero)
            {
                // if vStart is also parallel with the line vector (either 1 or 2 since they are parallel to each other)
                // and since bounding boxes do overlap, then the lines are collinear and overlapping
                if (vStart.Cross(lineA.Vector).IsNegligible())
                {
                    // there are four cases that need to be determined now: (#1) the from of line-A is on line-B (TJunctionAMerges),
                    // (#2) the from of line-B is on line-A (TJunctionBMerges), (#3) both are occurring (in which case we use the secondRareIntersectionPoint)
                    // or (#4) neither, which means that the toPoints of the lines are coincident with the overlapping line segment, but the from's 
                    // are not.
                    var sameDir = lineA.Vector.Dot(lineB.Vector) > 0;
                    if ((lineB.ToPoint.Coordinates - lineA.FromPoint.Coordinates).Dot(vStart) < 0)
                    {
                        intersections.Add(new IntersectionData(lineA, lineB, lineA.FromPoint.Coordinates,
                            sameDir
                                ? PolygonSegmentRelationship.TJunctionAMergeSameDir
                                : PolygonSegmentRelationship.TJunctionAMergeOppDir));
                    }
                    if ((lineB.FromPoint.Coordinates - lineA.ToPoint.Coordinates).Dot(vStart) < 0)
                    {
                        intersections.Add(new IntersectionData(lineA, lineB, lineB.FromPoint.Coordinates,
                            sameDir
                                ? PolygonSegmentRelationship.TJunctionBMergeSameDir
                                : PolygonSegmentRelationship.TJunctionBMergeOppDir));
                    }
                }
                return; // otherwise the lines are parallel but not at same distance/intercept
            }

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
            var oneOverdeterminnant = 1 / vCross;
            var t_1 = oneOverdeterminnant * (lineB.Vector.Y * vStart.X - lineB.Vector.X * vStart.Y);
            var t_2 = oneOverdeterminnant * (lineA.Vector.Y * vStart.X - lineA.Vector.X * vStart.Y);
            if (t_1 < 0 || t_1 > 1 || t_2 < 0 || t_2 > 1) return; // PolygonSegmentRelationship.Separated;
            if (t_1.IsNegligible())
            {
                // so, A's from is on B's line, but does polygon A cross here or does it reflect back
                // to the same side. if vCross is in the same direction as the previous line-A with line-B, then - yes, it crosses
                intersections.Add(new IntersectionData(lineA, lineB, lineA.FromPoint.Coordinates,
                    vCross * lineA.FromPoint.EndLine.Vector.Cross(lineB.Vector) > 0
                        ? PolygonSegmentRelationship.TJunctionACrosses
                        : PolygonSegmentRelationship.TJunctionAReflects));
            }
            else if (t_2.IsNegligible())
            {
                intersections.Add(new IntersectionData(lineA, lineB, lineB.FromPoint.Coordinates,
                    vCross * lineA.Vector.Cross(lineB.FromPoint.EndLine.Vector) > 0
                        ? PolygonSegmentRelationship.TJunctionBCrosses
                        : PolygonSegmentRelationship.TJunctionBReflects));
            }
            else
            {
                intersections.Add(new IntersectionData(lineA, lineB,
                    lineA.FromPoint.Coordinates + t_1 * lineA.Vector, PolygonSegmentRelationship.IntersectNominal));
            }
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
                    PolygonSegmentIntersection(current, other, intersections);
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
