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

using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        /// <summary>
        /// Creates the coordiantes of the corresponding convex hull polygon.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>List&lt;Vector2&gt;.</returns>
        public static List<Vector2> ConvexHull2D(this IEnumerable<Vector2> points)
        {
            var pointList = points as IList<Vector2> ?? points.ToList();
            return (List<Vector2>)MIConvexHull.ConvexHull.Create2D(pointList).Result;
        }

        /// <summary>
        /// Creates the convex hull polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>Polygon.</returns>
        public static Polygon ConvexHull2D(this Polygon polygon)
        {
            return new Polygon((List<Vector2>)MIConvexHull.ConvexHull.Create2D(polygon.Path).Result);
        }
    }
}