// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        /// <summary>
        /// Creates the coordinates of the corresponding convex hull polygon.
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