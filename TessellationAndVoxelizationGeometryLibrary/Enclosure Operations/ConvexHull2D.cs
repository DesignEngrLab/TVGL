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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using MIConvexHull;

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
                var triangleArea =
                    Math.Abs(point1.X * (point2.Y - point3.Y) + point2.X * (point3.Y - point1.Y) +
                             point3.X * (point1.Y - point2.Y));
                totalArea += triangleArea;
            }

            return 0.5 * totalArea;
        }

        public static IEnumerable<PointLight> ConvexHull2D(IList<PointLight> points) 
        {
            return MIConvexHull.ConvexHull.Create(points);
        }
    }
}