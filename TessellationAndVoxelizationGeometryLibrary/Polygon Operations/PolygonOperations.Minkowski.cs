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
            if (a == null || b == null) return new List<Polygon>();

            //Convert to int points and remove collinear edges
            var clipperSubject = ConvertToClipperPaths(a).First();
            var clipperClip = ConvertToClipperPaths(b).First();

            var clipperSolution = Clipper.MinkowskiSum(clipperSubject, clipperClip, true);
            //Convert back to points and return solution
            var solution = clipperSolution.Select(clipperPath
                => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale))));

            return solution.ToList();
        }



    }
}