// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="Design Engineering Lab">
//     Copyright ?  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Minimums the circle.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Bounding circle failed to converge</exception>
        /// <references>
        ///     Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
        ///     http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        ///     This algorithm runs in near linear time. Visiting most points just a few times.
        ///     Though a linear algorithm was found by Meggi do, this algorithm is more robust
        ///     (doesn't care about multiple points on a line and fewer rounding functions)
        ///     and directly applicable to multiple dimensions (in our case, just 2 and 3 D).
        /// </references>
        public static BoundingCircle MinimumCircle(IList<Point> points)
        {
            #region Algorithm 1

            //Randomize the list of points
            //var r = new Random();
            //var randomPoints = new List<Point>(points.OrderBy(p=>r.Next()));

            //if (randomPoints.Count < 2) return new BoundingCircle(0.0, points[0]);
            ////Get any two points in the list points.
            //var point1 = randomPoints[0];
            //var point2 = randomPoints[1];
            //var previousPoints = new List<Point>();
            //var circle = new InternalCircle(new List<Point> {point1, point2});
            //var stallCounter = 0;
            //var i = 0;

            //Algorithm 1
            //while (i < randomPoints.Count && stallCounter < points.Count * 2)
            //{
            //    var currentPoint = randomPoints[i];
            //    //If the current point is part of the circle or inside the circle, go to the next iteration
            //    if (circle.Points.Contains(currentPoint) || circle.IsPointInsideCircle(currentPoint))
            //    {
            //        i++;
            //        continue;
            //    }

            //    //Else if the currentPoint is a previousPoint, increase dimension
            //    if (previousPoints.Contains(currentPoint))
            //    {
            //        //Make a new circle from the current two-point circle and the current point
            //        circle = new InternalCircle(new List<Point> {circle.Points[0], circle.Points[1], currentPoint});
            //        previousPoints.Remove(currentPoint);
            //        i++;
            //    }
            //    else
            //    {
            //        //Find the point in the circle furthest from new point. 
            //        Point furthestPoint;
            //        circle.Furthest(currentPoint, out furthestPoint, ref previousPoints);
            //        //Make a new circle from the furthest point and current point
            //        circle = new InternalCircle(new List<Point> {currentPoint, furthestPoint});
            //        //Add previousPoints to the front of the list
            //        foreach (var previousPoint in previousPoints)
            //        {
            //            randomPoints.Remove(previousPoint);
            //            randomPoints.Insert(0, previousPoint);
            //        }
            //        //Restart the search
            //        stallCounter++;
            //        i = 0;
            //    }
            //}

            #endregion

            #region Algorithm 2: Furthest Point

            //Algorithm 2
            var listPoints = new List<Point>(points);
            var point1 = listPoints.First();
            var point2 = listPoints[listPoints.Count/2];
            var circle = new InternalCircle(new List<Point> {point1, point2});
            var stallCounter = 0;
            var previousPoints = new List<Point>();
            var successful = false;
            var stallLimit = listPoints.Count*3;
            if (stallLimit < 100) stallLimit = 100;

            while (!successful && stallCounter < stallLimit)
            {
                //Find the furthest point from the center point
                //If stallCounter is getting big, add a bit extra to the circle radius to ensure convergence
                if (stallCounter > stallLimit/2)
                    circle.SqRadius = circle.SqRadius + Constants.BaseTolerance*circle.SqRadius;
                //Add it a second time if stallCounter is even bigger
                if (stallCounter > stallLimit*2/3)
                    circle.SqRadius = circle.SqRadius + Constants.BaseTolerance*circle.SqRadius;
                var maxDistance = circle.SqRadius;
                Point nextPoint = null;
                var create3PointCircle = false;
                foreach (var point in listPoints)
                {
                    if (previousPoints.Contains(point) && !circle.IsPointInsideCircle(point))
                    {
                        create3PointCircle = true;
                        nextPoint = point;
                        break;
                    }
                    if (circle.Points.Contains(point)) continue; //Check if point is already part of the circle
                    var squareDistanceToPoint = (circle.Center.X - point.X)*(circle.Center.X - point.X) +
                                                (circle.Center.Y - point.Y)*(circle.Center.Y - point.Y);
                    if (squareDistanceToPoint <= maxDistance) continue; //Beginning with the circle's square radius

                    maxDistance = squareDistanceToPoint;
                    nextPoint = point;
                }
                if (nextPoint == null)
                {
                    successful = true;
                    continue;
                }

                //Create a new circle of either 2 or 3 points
                //if the currentPoint is a previousPoint, increase dimension
                if (create3PointCircle)
                {
                    //Make a new circle from the current two-point circle and the current point
                    circle = new InternalCircle(new List<Point> {circle.Points[0], circle.Points[1], nextPoint});
                    previousPoints.Remove(nextPoint);
                }
                else
                {
                    //Find the point in the circle furthest from new point. 
                    Point furthestPoint;
                    circle.Furthest(nextPoint, out furthestPoint, ref previousPoints);
                    //Make a new circle from the furthest point and current point
                    circle = new InternalCircle(new List<Point> {nextPoint, furthestPoint});
                    //Add previousPoints to the front of the list
                    foreach (var previousPoint in previousPoints)
                    {
                        listPoints.Remove(previousPoint);
                        listPoints.Insert(0, previousPoint);
                    }
                }
                stallCounter++;
            }

            #endregion

            #region Algorithm 3: Meggiddo's Linear-Time Algorithm

            //Pair up points into n/2 pairs. 
            //If an odd number of points.....

            //Construct a bisecting line for each pair of points. This sets their slope.

            //Order the slopes.
            //Find the median (halfway in the set) slope of the bisector lines 


            //Test

            #endregion

            //Return information about minimum circle
            if (stallCounter == stallLimit) throw new Exception("Bounding circle failed to converge");
            var radius = circle.SqRadius.IsNegligible() ? 0 : Math.Sqrt(circle.SqRadius);
            return new BoundingCircle(radius, circle.Center);
        }


        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(IList<List<Point>> paths, Point centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path)).ToList();
            return MaximumInnerCircle(new PolygonGroup(polygons), centerPoint);
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static BoundingCircle MaximumInnerCircle(List<Polygon> polygons, Point centerPoint)
        {
            return MaximumInnerCircle(new PolygonGroup(polygons), centerPoint);
        }

        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     The circle will either be inside a negative polygon or outside a positive polygon (e.g. C channel). 
        ///     Else it returns a negligible Bounding Circle
        /// </summary>
        public static BoundingCircle MaximumInnerCircle(PolygonGroup polyGroup, Point centerPoint)
        {
            //Check the distance from every line and point of every polygon in the group. 
            //Note: this function could possible be improved by determining which polygon is closest, 
            //but that did not seem to be a faster method. Also, a inner circle does not necessarily
            //need to be contained in a bounding box for positive solids (e.g. a C shape).
            var polygonsOfInterest = new List<Polygon>();

            //First, check if the point is inside any negative polygon.
            var minDistance = double.MaxValue;
            Polygon closestContainingPolygon = null;
            foreach (var negativePoly in polyGroup.NegativePolygons)
            {
                bool onBoundary;
                Line closestLineAbove;
                Line closestLineBelow;
                if (!MiscFunctions.IsPointInsidePolygon(negativePoly, centerPoint, out closestLineAbove,
                    out closestLineBelow, out onBoundary)) continue;
                if (onBoundary) return new BoundingCircle(0.0, centerPoint); //Null solution.
                var d = closestLineAbove.YGivenX(centerPoint.X) - centerPoint.Y; //Not negligible because not on Boundary
                if (d < minDistance)
                {
                    minDistance = d;
                    closestContainingPolygon = negativePoly;
                }
            }

            //If the point is inside the polygon.
            if (closestContainingPolygon != null)
            {
                polygonsOfInterest.Add(closestContainingPolygon);
            }
            //If not inside a negative polygon, check if the point is inside any positive polygons. If it is return null.
            else
            {
                foreach (var positivePoly in polyGroup.PositivePolygons)
                {
                    bool onBoundary;
                    Line closestLineAbove;
                    Line closestLineBelow;
                    if (MiscFunctions.IsPointInsidePolygon(positivePoly, centerPoint, out closestLineAbove,
                        out closestLineBelow, out onBoundary)) return new BoundingCircle(0.0, centerPoint);
                    polygonsOfInterest.Add(positivePoly);
                }
            }
            
            //Lastly, determine how big the inner circle can be.
            var shortestDistance = double.MaxValue;
            var smallestBoundingCircle = new BoundingCircle(0.0, centerPoint);
            foreach (var polygon in polygonsOfInterest)
            {
                var boundingCircle = MaximumInnerCircleInHole(polygon, centerPoint);
                if (boundingCircle.Radius < shortestDistance)
                {
                    shortestDistance = boundingCircle.Radius;
                    smallestBoundingCircle = boundingCircle;
                }
            }

            return smallestBoundingCircle;
        }

        private static BoundingCircle MaximumInnerCircleInHole(Polygon polygon, Point centerPoint)
        {
            var shortestDistance = double.MaxValue;
            //1. For every line on the path, get the closest point on the edge to the center point. 
            //   Skip if min distance to line (perpendicular) forms a point not on the line.
            foreach (var line in polygon.PathLines)
            {
                var v1 = line.ToPoint.Position2D.subtract(line.FromPoint.Position2D);
                //Correctly ordering the points should yield a negative area if the circle is inside a hole or outside a positive polygon.
                //Note also that zero area will occur when the points line up, which we want to ignore (the line ends will be checked anyways)
                if (!MiscFunctions.AreaOfPolygon(new List<Point> { line.FromPoint, line.ToPoint, centerPoint }).IsLessThanNonNegligible())
                    continue;

                //Figure out how far the center point is away from the line
                double[] pointOnLine;
                var d = MiscFunctions.DistancePointToLine(centerPoint.Position2D, line.FromPoint.Position2D, v1, out pointOnLine);
                if (d > shortestDistance) continue;

                //Now we need to figure out if the lines intersect
                var tempPoint = new Point(pointOnLine[0], pointOnLine[1]);
                var tempLine = new Line(centerPoint, tempPoint, false);
                Point intersectionPoint;
                if (!MiscFunctions.LineLineIntersection(line, tempLine, out intersectionPoint)) continue;
                //if(intersectionPoint != tempPoint) throw new Exception("Error in implementation. This should always be true.");
                shortestDistance = d;
            }

            //2. For every point in path and every closest edge point find distance to center.
            //   The shortest distance determines the diameter of the inner circle.
            foreach (var point in polygon.Path)
            {
                var d = MiscFunctions.DistancePointToPoint(point.Position2D, centerPoint.Position2D);
                if (d < shortestDistance) shortestDistance = d;
            }

            if (shortestDistance.IsPracticallySame(double.MaxValue)) return new BoundingCircle(0.0, centerPoint); //Not inside any hole or outside any positive polygon
            return new BoundingCircle(shortestDistance, centerPoint);
        }

        /// <summary>
        ///     Takes a set of elements and a metric for comparing them pairwise, and returns the median of the elements.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>Point.</returns>
        public static Point Median(List<Point> points)
        {
            return points[0];
        }

        /// <summary>
        ///     Takes a set of points and a line, and determines which side of the line the center of the MEC of  the points lies
        ///     on.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="line">The line.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool MEC_Center(List<Point> points, double[] line)
        {
            return false;
        }

        /// <summary>
        ///     Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox MinimumBoundingCylinder(IList<Vertex> convexHullVertices)
        {
            // here we create 13 directions. just like for bounding box
            var directions = new List<double[]>();
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                    directions.Add(new[] {1.0, i, j});
            directions.Add(new[] {0.0, 0, 1});
            directions.Add(new[] {0.0, 1, 0});
            directions.Add(new[] {0.0, 1, 1});
            directions.Add(new[] {0.0, -1, 1});

            var boxes = directions.Select(v => new BoundingBox
            {
                Directions = new[] {v},
                Volume = double.PositiveInfinity
            }).ToList();
            for (var i = 0; i < 13; i++)
            {
                boxes[i] = Find_via_ChanTan_AABB_Approach(convexHullVertices, boxes[i]);
                for (var j = 0; j < 3; j++)
                {
                    var pointsOnFace_i = MiscFunctions.Get2DProjectionPoints(convexHullVertices, boxes[i].Directions[j]);
                }
            }
            var minVol = boxes.Min(box => box.Volume);
            return boxes.First(box => box.Volume == minVol);
        }

        /// <summary>
        ///     Adjusts the orthogonal rotations.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="minOBB">The minimum obb.</param>
        /// <returns>BoundingBox.</returns>
        private static BoundingBox AdjustOrthogonalRotations(IList<Vertex> convexHullVertices, BoundingBox minOBB)
        {
            var failedConsecutiveRotations = 0;
            var k = 0;
            var i = 0;
            do
            {
                //Find new OBB along OBB.direction2 and OBB.direction3, keeping the best OBB.
                var newObb = FindOBBAlongDirection(convexHullVertices, minOBB.Directions[i++]);
                if (newObb.Volume.IsLessThanNonNegligible(minOBB.Volume))
                {
                    minOBB = newObb;
                    failedConsecutiveRotations = 0;
                }
                else failedConsecutiveRotations++;
                if (i == 3) i = 0;
                k++;
            } while (failedConsecutiveRotations < 3 && k < MaxRotationsForOBB);
            return minOBB;
        }

        /// <summary>
        ///     Class Bisector.
        /// </summary>
        internal class Bisector
        {
            #region Constructor

            /// <summary>
            ///     Initializes a new instance of the <see cref="Bisector" /> class.
            /// </summary>
            /// <param name="point1">The point1.</param>
            /// <param name="point2">The point2.</param>
            internal Bisector(Point point1, Point point2)
            {
            }

            #endregion

            #region Properties

            /// <summary>
            ///     Gets the slope.
            /// </summary>
            /// <value>The slope.</value>
            internal double Slope { get; private set; }

            #endregion
        }

        /// <summary>
        ///     Class InternalCircle.
        /// </summary>
        internal class InternalCircle
        {
            #region Constructor

            /// <summary>
            ///     Create a new circle from either 2 or 3 points
            /// </summary>
            /// <param name="points">The points.</param>
            internal InternalCircle(IEnumerable<Point> points)
            {
                Center = null;
                SqRadius = 0;
                Points = new List<Point>(points);

                //Find Circle center and radius
                if (Points.Count == 3)
                {
                    //Assume NO two points are exactly the same 
                    var rise1 = Points[1].Y - Points[0].Y;
                    var run1 = Points[1].X - Points[0].X;
                    var rise2 = Points[2].Y - Points[1].Y;
                    var run2 = Points[2].X - Points[1].X;
                    double x;
                    double y;
                    //Check for special cases of vertical or horizontal lines
                    if (rise1.IsNegligible(Constants.BaseTolerance)) //If rise is zero, x can be found directly
                    {
                        x = (Points[0].X + Points[1].X)/2;
                        //If run of other line is approximately zero as well, y can be found directly
                        if (run2.IsNegligible(Constants.BaseTolerance))
                            //If run is approximately zero, y can be found directly
                        {
                            y = (Points[1].Y + Points[2].Y)/2;
                            Center = new Point(new Vertex(new[] {x, y, 0.0}));
                        }
                        else
                        {
                            //Find perpendicular slope, and midpoint of line 2. 
                            //Then use the midpoint to find "b" and solve y = mx+b
                            //This is condensed into a single line because VS rounds the numbers 
                            //during division.
                            y = (Points[1].Y + Points[2].Y)/2 + -run2/rise2*(x - (Points[1].X + Points[2].X)/2);
                            Center = new Point(new Vertex(new[] {x, y, 0.0}));
                        }
                    }
                    else if (rise2.IsNegligible(Constants.BaseTolerance))
                        //If rise is approximately zero, x can be found directly
                    {
                        x = (Points[1].X + Points[2].X)/2;
                        //If run of other line is approximately zero as well, y can be found directly
                        if (run1.IsNegligible(Constants.BaseTolerance))
                            //If run is approximately zero, y can be found directly
                        {
                            y = (Points[0].Y + Points[1].Y)/2;
                            Center = new Point(new Vertex(new[] {x, y, 0.0}));
                        }
                        else
                        {
                            //Find perpendicular slope, and midpoint of line 2. 
                            //Then use the midpoint to find "b" and solve y = mx+b
                            //This is condensed into a single line because VS rounds the numbers 
                            //during division.
                            y = (Points[0].Y + Points[1].Y)/2 + -run1/rise1*(x - (Points[0].X + Points[1].X)/2);
                            Center = new Point(new Vertex(new[] {x, y, 0.0}));
                        }
                    }
                    else if (run1.IsNegligible(Constants.BaseTolerance))
                        //If run is approximately zero, y can be found directly
                    {
                        y = (Points[0].Y + Points[1].Y)/2;
                        //Find perpendicular slope, and midpoint of line 2. 
                        //Then use the midpoint to find "b" and solve y = mx+b
                        //This is condensed into a single line because VS rounds the numbers 
                        //during division.
                        x = (y - ((Points[1].Y + Points[2].Y)/2 - -run2/rise2*
                                  (Points[1].X + Points[2].X)/2))/(-run2/rise2);
                        Center = new Point(new Vertex(new[] {x, y, 0.0}));
                    }
                    else if (run2.IsNegligible(Constants.BaseTolerance))
                        //If run is approximately zero, y can be found directly
                    {
                        y = (Points[1].Y + Points[2].Y)/2;
                        //Find perpendicular slope, and midpoint of line 2. 
                        //Then use the midpoint to find "b" and solve y = mx+b
                        //This is condensed into a single line because VS rounds the numbers 
                        //during division.
                        x = (y - ((Points[1].Y + Points[0].Y)/2 - -run1/rise1*
                                  (Points[1].X + Points[0].X)/2))/(-run1/rise1);
                        Center = new Point(new Vertex(new[] {x, y, 0.0}));
                    }
                    else
                    {
                        //Didn't solve for slopes first because of rounding error in division
                        //ToDo: This does not always find a good center. Figure out why.
                        x = (rise1/run1*(rise2/run2)*(Points[2].Y - Points[0].Y) +
                             rise1/run1*(Points[1].X + Points[2].X) -
                             rise2/run2*(Points[0].X + Points[1].X))/(2*(rise1/run1 - rise2/run2));
                        y = -(1/(rise1/run1))*(x - (Points[0].X + Points[1].X)/2) +
                            (Points[0].Y + Points[1].Y)/2;
                        Center = new Point(new Vertex(new[] {x, y, 0.0}));
                    }
                    SqRadius = Math.Pow(Center.X - Points[0].X, 2) + Math.Pow(Center.Y - Points[0].Y, 2);
                }
                else if (Points.Count == 2)
                {
                    var vector = Points[0].Position2D.subtract(Points[1].Position2D);
                    Center = new Point(new Vertex(new[] {Points[1].X + vector[0]/2, Points[1].Y + vector[1]/2, 0.0}));
                    SqRadius = Math.Pow(vector[0]/2, 2) + Math.Pow(vector[1]/2, 2);
                }
                else //1 point
                {
                    Center = Points[0];
                    SqRadius = 0;
                }
            }

            #endregion

            /// <summary>
            ///     Gets X intercept given Y
            /// </summary>
            /// <param name="point">The point.</param>
            /// <returns><c>true</c> if [is point inside circle] [the specified point]; otherwise, <c>false</c>.</returns>
            internal bool IsPointInsideCircle(Point point)
            {
                //Distance between point and center is greater than radius, it is outside the circle
                var distanceToPoint = (Center.X - point.X)*(Center.X - point.X) +
                                      (Center.Y - point.Y)*(Center.Y - point.Y);
                if (distanceToPoint.IsPracticallySame(SqRadius)) return true;
                return distanceToPoint < SqRadius;
            }

            /// <summary>
            ///     Finds the furthest the specified point.
            /// </summary>
            /// <param name="point">The point.</param>
            /// <param name="furthestPoint">The furthest point.</param>
            /// <param name="previousPoints">The previous points.</param>
            /// <exception cref="ArgumentNullException">previousPoints cannot be null</exception>
            internal void Furthest(Point point, out Point furthestPoint, ref List<Point> previousPoints)
            {
                if (previousPoints == null) throw new ArgumentNullException("previousPoints cannot be null");
                furthestPoint = null;
                previousPoints = new List<Point>(Points);
                var maxSquareDistance = double.NegativeInfinity;
                //Distance between point and center is greater than radius, it is outside the circle
                foreach (var containedPoint in Points)
                {
                    var squareDistance = Math.Pow(containedPoint.X - point.X, 2) +
                                         Math.Pow(containedPoint.Y - point.Y, 2);
                    if (squareDistance <= maxSquareDistance) continue;
                    maxSquareDistance = squareDistance;
                    furthestPoint = containedPoint;
                }
                previousPoints.Remove(furthestPoint);
            }

            #region Properties

            /// <summary>
            ///     Gets one point of the circle.
            /// </summary>
            /// <value>The points.</value>
            internal List<Point> Points { get; }

            /// <summary>
            ///     Gets one point of the circle.
            /// </summary>
            /// <value>The center.</value>
            internal Point Center { get; }

            /// <summary>
            ///     Gets one point of the circle.
            /// </summary>
            /// <value>The sq radius.</value>
            internal double SqRadius { get; set; }

            #endregion
        }
    }
}