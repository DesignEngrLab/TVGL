// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 06-07-2026
//
// ***********************************************************************
// <copyright file="PolygonOperations.InternalPoints.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class PolygonOperations
    {
        public static IEnumerable<Vector2> FindInternalPointsOffset(this Polygon polygon, double targetRadius)
        {
            var prevPolygons = new List<Polygon> { polygon };
            while (prevPolygons.Count > 0)
            {
                prevPolygons = prevPolygons.OffsetSquare(-targetRadius);
                prevPolygons.Complexify(targetRadius);
                prevPolygons.SimplifyMinLength(targetRadius);
                foreach (var v in prevPolygons.SelectMany(poly => poly.AllPolygons).SelectMany(p => p.Vertices))
                    yield return v.Coordinates;
            }
        }
        public static IEnumerable<Vector2> FindInternalPointsPoissonDisk(this Polygon polygon, double targetRadius)
        {
            throw new NotImplementedException();
        }
        public static IEnumerable<Vector2> FindInternalPointsVoronoi(this Polygon polygon, int numberPoints)
        {
            throw new NotImplementedException();

        }
    }
}
