// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 04-17-2015
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="">
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
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a 
    /// tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        /// Minimums the circle.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <references>
        /// Based on Emo Welzl's "move-to-front hueristic" and this paper (algorithm 1).
        /// http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        /// This algorithm runs in near linear time. Visiting most points just a few times.
        /// Though a linear algorithm was found by Meggido, this algorithm is more robust
        /// (doesn't care about multiple points on a line and fewer rounding functions)
        /// and directly applicable to multiple dimensions (in our case, just 2 and 3 D).
        /// </references>
        public static BoundingCircle MinimumCircle(IList<Point> points)
        {
            //Randomize the list of points
            var r = new Random();
            var randomPoints = new List<Point>(points.OrderBy(p=>r.Next()));
           
            if (randomPoints.Count < 2) return new BoundingCircle(0.0, points[0]);
            //Get any two points in the list points.
            var point1 = randomPoints[0];
            var point2 = randomPoints[1];
            var previousPoints = new List<Point>();
            var circle = new InternalCircle(new List<Point> {point1, point2});
            var bestCircle = circle;
            var stallCounter = 0;
            var i = 0;

            while (i < randomPoints.Count && stallCounter < randomPoints.Count)
            {
                var currentPoint = randomPoints[i];
                //If the current point is part of the circle or inside the circle, go to the next iteration
                if (circle.Points.Contains(currentPoint) || circle.IsPointInsideCircle(currentPoint))
                {
                    i++;
                    continue;
                }

                //Else if the currentPoint is the previousPoint, increase dimension
                if (previousPoints.Contains(currentPoint))
                {
                    //Make a new circle from the current two-point circle and the current point
                    circle = new InternalCircle(new List<Point> {circle.Points[0], circle.Points[1], currentPoint});
                    if (circle.SqRadius < bestCircle.SqRadius)
                    {
                        bestCircle = circle;
                        stallCounter = 0;
                    }
                    else stallCounter++;
                    previousPoints.Remove(currentPoint);
                    i++;
                }
                else
                {
                    //Find the point in the circle furthest from new point. 
                    Point furthestPoint;
                    circle.Furthest(currentPoint, out furthestPoint, ref previousPoints);
                    //Make a new circle from the furthest point and current point
                    circle = new InternalCircle(new List<Point> {currentPoint, furthestPoint});
                    if(circle.SqRadius < bestCircle.SqRadius) 
                    {
                        bestCircle = circle;
                        stallCounter =0;
                    }
                    else stallCounter++;
                    //Add previousPoints to the front of the list
                    foreach (var previousPoint in previousPoints)
                    {
                        randomPoints.Remove(previousPoint);
                        randomPoints.Insert(0, previousPoint);
                    }
                    //Restart the search
                    i = 0;
                }
            }

            //Return information about minimum circle
            var radius = circle.SqRadius.IsNegligible() ? 0 : Math.Sqrt(circle.SqRadius);
            return new BoundingCircle(radius, circle.Center);
        }
        
        public static BoundingBox MinimumBoundingCylinder(IList<Vertex> convexHullVertices)
        {
            // here we create 13 directions. just like for bounding box
            var directions = new List<double[]>();
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    directions.Add(new[] { 1.0, i, j });
            directions.Add(new[] { 0.0, 0, 1 });
            directions.Add(new[] { 0.0, 1, 0 });
            directions.Add(new[] { 0.0, 1, 1 });
            directions.Add(new[] { 0.0, -1, 1 });

            var boxes = directions.Select(v => new BoundingBox
            {
                Volume = double.PositiveInfinity,
                Directions = new[] { v, null, null }
            }).ToList();
            for (int i = 0; i < 13; i++)
            {
                boxes[i] = Find_via_ChanTan_AABB_Approach(convexHullVertices, boxes[i]);
                for (int j = 0; j < 3; j++)
                {
                    var pointsOnFace_i = MiscFunctions.Get2DProjectionPoints(convexHullVertices, boxes[i].Directions[j]);
                    boxes[i].Width
                }
            }
            var minVol = boxes.Min(box => box.Volume);
            return boxes.First(box => box.Volume == minVol);
        }
        
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
        internal class InternalCircle
        {
            #region Properties
            /// <summary>
            /// Gets one point of the circle.
            /// </summary>
            internal List<Point> Points { get; private set; }

            /// <summary>
            /// Gets one point of the circle.
            /// </summary>
            internal Point Center { get; private set; }

            /// <summary>
            /// Gets one point of the circle.
            /// </summary>
            internal double SqRadius { get; private set; }
            #endregion

            #region Constructor
            /// <summary>
            /// Create a new circle from either 2 or 3 points
            /// /// </summary>
            /// <param name="points"></param>
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
                    //Check for special cases of vertical or horizintal lines
                    if (Math.Abs(rise1) < 1E-15) //If rise is approximately zero, x can be found directly
                    {
                        x = (Points[0].X + Points[1].X)/2;
                        //If run of other line is approximately zero as well, y can be found directly
                        if (Math.Abs(run2) < 1E-15) //If run is approximately zero, y can be found directly
                        {
                            y = (Points[1].Y + Points[2].Y)/2;
                            Center = new Point(new Vertex(new[] {x, y,0.0}));
                        }
                        else
                        {
                            var mPerp = (Points[1].X - Points[2].X)/(Points[2].Y - Points[1].Y);
                            y = (Points[1].Y + Points[2].Y)/2 + mPerp*(x - (Points[1].X + Points[2].X)/2);
                            Center = new Point(new Vertex(new[] {x, y,0.0}));
                        }
                    }
                    else if (Math.Abs(rise2) < 1E-15) //If rise is approximately zero, x can be found directly
                    {
                        x = (Points[1].X + Points[2].X)/2;
                        //If run of other line is approximately zero as well, y can be found directly
                        if (Math.Abs(run1) < 1E-15) //If run is approximately zero, y can be found directly
                        {
                            y = (Points[0].Y + Points[1].Y)/2;
                            Center = new Point(new Vertex(new[] {x, y,0.0}));
                        }
                        else
                        {
                            var mPerp = (Points[0].X - Points[1].X)/(Points[1].Y - Points[0].Y);
                            y = (Points[0].Y + Points[1].Y)/2 + mPerp*(x - (Points[0].X + Points[1].X)/2);
                            Center = new Point(new Vertex(new[] {x, y,0.0}));
                        }
                    }
                    else if (Math.Abs(run1) < 1E-15) //If run is approximately zero, y can be found directly
                    {
                        y = (Points[0].Y + Points[1].Y)/2;
                        var mPerp = (Points[1].X - Points[2].X)/(Points[2].Y - Points[1].Y);
                        x = (y - (Points[1].Y + Points[2].Y)/2 - mPerp*(Points[1].X + Points[2].X)/2)/mPerp;
                        Center = new Point(new Vertex(new[] {x, y,0.0}));
                    }
                    else if (Math.Abs(run2) < 1E-15) //If run is approximately zero, y can be found directly
                    {
                        y = (Points[1].Y + Points[2].Y)/2;
                        var mPerp = (Points[0].X - Points[1].X)/(Points[1].Y - Points[0].Y);
                        x = (y - (Points[0].Y + Points[1].Y)/2 - mPerp*(Points[0].X + Points[1].X)/2)/mPerp;
                        Center = new Point(new Vertex(new[] {x, y,0.0}));
                    }
                    else
                    {
                        var m1 = rise1/run1;
                        var m2 = rise2/run2;
                        x = (m1*m2*(Points[2].Y - Points[0].Y) + m1*(Points[1].X + Points[2].X) -
                             m2*(Points[0].X + Points[1].X))/(2*(m1 - m2));
                        y = -(1/m1)*(x - (Points[0].X + Points[1].X)/2) + (Points[0].Y + Points[1].Y)/2;
                        Center = new Point(new Vertex(new double[] {x, y,0.0}));
                    }
                    SqRadius = Math.Pow(Center.X - Points[0].X, 2) + Math.Pow(Center.Y - Points[0].Y, 2);
                }
                else if (Points.Count == 2)
                {
                    var vector = Points[0].Position2D.subtract(Points[1].Position2D);
                    Center = new Point(new Vertex(new[] {Points[1].X + vector[0]/2, Points[1].Y + vector[1]/2,0.0}));
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
            /// Gets X intercept given Y
            /// </summary>
            /// <param name="point"></param>
            /// <returns></returns>
            internal bool IsPointInsideCircle(Point point)
            {
                //Distance between point and center is greater than radius, it is outside the circle
                var distanceToPoint = (Center.X - point.X)*(Center.X - point.X)  + (Center.Y - point.Y)*(Center.Y - point.Y);
                return distanceToPoint <= SqRadius;
            }

            /// <summary>
            /// </summary>
            /// <param name="point"></param>
            /// <param name="furthestPoint"></param>
            /// <param name="previousPoints"></param>
            /// <returns></returns>
            internal void Furthest(Point point, out Point furthestPoint, ref List<Point> previousPoints)
            {
                if (previousPoints == null) throw new ArgumentNullException("previousPoints cannot be null");
                furthestPoint = null;
                previousPoints = new List<Point>(Points);
                var maxSquareDistance = double.NegativeInfinity;
                //Distance between point and center is greate than radius, it is outside the circle
                foreach (var containedPoint in Points)
                {

                    var squareDistance = Math.Pow(containedPoint.X - point.X, 2) +
                                         Math.Pow(containedPoint.Y - point.Y, 2);
                    if (squareDistance > maxSquareDistance)
                    {
                        maxSquareDistance = squareDistance;
                        furthestPoint = containedPoint;
                    }
                }
                previousPoints.Remove(furthestPoint);
            }
        }
    }
}