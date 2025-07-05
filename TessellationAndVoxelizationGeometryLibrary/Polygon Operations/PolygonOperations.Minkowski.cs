// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 12-08-2024
//
// Last Modified By : matth
// Last Modified On : 12-08-2024
// ***********************************************************************
// <copyright file="PolygonOperations.Minkowski.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Clipper2Lib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Creates the minkowski sum of the two polygons. There are flat (hole-less) polygons.
        /// If you want the minkowski sum of a hole, you have to do that separately.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static List<Polygon> MinkowskiSum(this Polygon a, Polygon b)
        {
            int aVertCount = a.Vertices.Count, bVertCount = b.Vertices.Count;
            var tmp = new Polygon[bVertCount];
            var jj = 0;
            foreach (var pathPt in b.Vertices)
            {
                var path2 = new Vector2[aVertCount];
                var ii = 0;
                foreach (var basePt in a.Vertices)
                    path2[ii++] = pathPt.Coordinates + basePt.Coordinates;
                tmp[jj++] = new Polygon(path2);
            }

            var result = new Polygon[bVertCount * aVertCount];
            var k = 0;
            int g = bVertCount - 1;

            int h = aVertCount - 1;
            for (int i = 0; i < bVertCount; i++)
            {
                for (int j = 0; j < aVertCount; j++)
                {
                    var quad = new Polygon([ tmp[g].Vertices[h], tmp[i].Vertices[h], 
                        tmp[i].Vertices[j], tmp[g].Vertices[j] ]);
                    if (!quad.IsPositive)
                        quad.Reverse(); //result.Add(Clipper.ReversePath(quad));
                    else
                        result[k++] = quad;
                    h = j;
                }
                g = i;
            }
            return result.UnionPolygons(PolygonCollection.SeparateLoops);
        }

    }
}