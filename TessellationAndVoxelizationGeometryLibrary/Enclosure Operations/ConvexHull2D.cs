// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 04-17-2015
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the area of the convex hull region, given a set of convex hull points.
        /// </summary>
        /// <param name="convexHullPoints2D"></param>
        /// <returns></returns>
        public static double ConvexHull2DArea(IList<Point> convexHullPoints2D)
        {
            //Set origin point to first point in convex hull
            var point1 = convexHullPoints2D[0];
            var totalArea = 0.0;

            //Find area of triangle between first point and every triangle that can be formed from the first point.
            for (var i = 1; i < convexHullPoints2D.Count - 1; i++)
            {
                var point2 = convexHullPoints2D[i];
                var point3 = convexHullPoints2D[i + 1];
                //Reference: <http://www.mathopenref.com/coordtrianglearea.html>
                var triangleArea = 0.5 *
                                   Math.Abs(point1.X * (point2.Y - point3.Y) + point2.X * (point3.Y - point1.Y) +
                                            point3.X * (point1.Y - point2.Y));
                totalArea = totalArea + triangleArea;
            }
            return totalArea;
        }

        /// <summary>
        /// Returns the 2D convex hull for given list of points. This only works on the x and y coordinates of the
        /// points and requires that the Z values be NaN. The term maximal refers to the fact that all points that lie on the convex hull are included in the result.
        /// This is useful if one if trying to identify such points. Otherwise, if the shape is what is important than the
        /// minimal approach is preferred.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// List&lt;Point&gt;.
        /// </returns>
        public static Point[] ConvexHull2D(IList<Point> points, double tolerance = double.NaN)
        {
            if (double.IsNaN(points[0].Z))
                if (double.IsNaN(tolerance))
                    return (Point[])MIConvexHull.ConvexHull.Create(points).Points;
                else return (Point[])MIConvexHull.ConvexHull.Create(points, tolerance).Points;

            var numPoints = points.Count;
            var zValues = points.Select(p => p.Z).ToArray();
            foreach (var point in points)
                point.Z = double.NaN;
            var cvxPoints = new Point[numPoints];
            if (double.IsNaN(tolerance))
                cvxPoints = (Point[])MIConvexHull.ConvexHull.Create(points).Points;
            else cvxPoints = (Point[])MIConvexHull.ConvexHull.Create(points, tolerance).Points;
            for (int i = 0; i < numPoints; i++)
                points[i].Z = zValues[i];
            return cvxPoints;
        }
    }
}