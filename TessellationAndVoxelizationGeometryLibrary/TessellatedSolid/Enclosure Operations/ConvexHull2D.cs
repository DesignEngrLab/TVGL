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
using TVGL.Numerics;

namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        public static IList<Vector2> ConvexHull2D(this IEnumerable<Vector2> points) 
        {
            var pointList = (points is IList<Vector2>) ? (IList<Vector2>)points : points.ToList();
            return MIConvexHull.ConvexHull.Create2D(pointList).Result;
        }
    }
}