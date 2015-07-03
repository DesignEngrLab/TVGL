// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 04-17-2015
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using TVGL.Tessellation;
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
        /// <references>
        /// Based on Emo Welzl’s "move-to-front hueristic" and this paper (algorithm 1).
        /// http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        /// This algorithm runs in near linear time.
        /// </references>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="center">The center.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static double MinimumCircle(IList<Point> points, double[] direction, out Point center, out double radius)
        {
            center = null;
            radius = 0;
            if (points.Count < 2) return 0;
            //Get any two points in the list points.
            var point1 = points[0];
            var point2 = points[1];
            var previousPoints = new List<Point>();
            Point furthestPoint = null;
            var circle = new Circle(point1, point2);
            var d = 1;

            for (var i = 0; i < points.Count; i++)
            {
                var currentPoint = points[i];
                if (circle.AlreadyContains(currentPoint) == false)
                {
                    //If nextPoint in points is inside the circle
                    if (circle.Contains(currentPoint) == true)
                    {
                        if (previousPoints.Contains(currentPoint))
                        {
                            points.Remove(currentPoint);
                            //restart
                            i = 0;
                        }
                    }
                    else
                    { 
                        //If currentPoint = previousPoint, increase dimension
                        if (previousPoints.Contains(currentPoint))
                        {
                            d++;
                            //Make a new circle from the current two-point circle and the current point
                            circle = new Circle(circle.Points[0], circle.Points[1], currentPoint);
                        }
                        else
                        {
                            //Find the point in the circle furthest from new point. 
                            circle.Furthest(currentPoint, out furthestPoint, ref previousPoints);
                            //Make a new circle from the furthest point and current point
                            circle = new Circle(currentPoint, furthestPoint);
                            //Add previousPoints to the front of the list
                            foreach (var previousPoint in previousPoints)
                            {
                                points.Remove(previousPoint);
                                points.Insert(0, previousPoint);
                            }
                        }           
                    }
                } 
            }   

            //Return information about minimum circle
            center = circle.Center;
            radius = circle.Radius;
            return Math.Pow(radius, 2) * Math.PI;
        }

        internal class Circle
        {
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
            internal double Radius { get; private set; }

            #region Constructor
            /// <summary>
            /// Create a new circle from either 2 or 3 points
            /// /// </summary>
            /// <param name="point1"></param>
            /// <param name="point2"></param>
            /// <param name="point3"></param>
            internal Circle(Point point1, Point point2, Point point3 = null)
            {
                Points[0] = point1;
                Points[1] = point2; 
                if (point3 != null) Points[2] = point3;
            }
            #endregion

            /// <summary>
            /// Gets X intercept given Y
            /// </summary>
            /// <param name="y"></param>
            /// <returns></returns>
            internal bool AlreadyContains(Point point)
            {
                foreach (var containedPoint in Points)
                {
                    if (containedPoint == point) return true;
                }
                return false;
            }

            /// <summary>
            /// Gets X intercept given Y
            /// </summary>
            /// <param name="y"></param>
            /// <returns></returns>
            internal bool Contains(Point point)
            {
                //Distance between point and center is greate than radius, it is outside the circle
                if (Math.Pow(Center.X - point.X, 2) + Math.Pow(Center.Y - point.Y, 2) > Math.Pow(Radius, 2)) return false;
                return true;
            }

            /// <summary>
            /// Gets X intercept given Y
            /// </summary>
            /// <param name="y"></param>
            /// <returns></returns>
            internal void Furthest(Point point, out Point furthestPoint, ref List<Point> previousPoints)
            {    
                furthestPoint = null;
                previousPoints = Points;
                var maxSquareDistance = double.NegativeInfinity;
                //Distance between point and center is greate than radius, it is outside the circle
                foreach (var containedPoint in Points)
                {
                    
                    var squareDistance = Math.Pow(containedPoint.X - point.X, 2) + Math.Pow(containedPoint.Y - point.Y, 2);
                    if (squareDistance > maxSquareDistance)
                    {
                        maxSquareDistance = squareDistance;
                        furthestPoint = containedPoint;
                    }
                    previousPoints.Remove(furthestPoint);
                }

            }
        }
    }
}