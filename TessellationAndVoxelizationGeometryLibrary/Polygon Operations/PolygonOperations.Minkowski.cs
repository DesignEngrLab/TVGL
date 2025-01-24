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
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// The Minkowski sum of the two polygons. This only functions on the outermost polygon (no holes).
        /// However, the operation does work on negative polygons, so the result can be fused totheger but this
        /// is left for the user's code due to ambiguities that may arise.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Polygon MinkowskiSum(this Polygon a, Polygon b)
            => MinkowskiSum(a, a.IsConvex(), b, b.IsConvex());

        /// <summary>
        /// The Minkowski sum of the two polygons. This only functions on the outermost polygon (no holes).
        /// However, the operation does work on negative polygons, so the result can be fused totheger but this
        /// is left for the user's code due to ambiguities that may arise.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="aIsConvex"></param>
        /// <param name="b"></param>
        /// <param name="bIsConvex"></param>
        /// <returns></returns>
        public static Polygon MinkowskiSum(this Polygon a, bool aIsConvex, Polygon b, bool bIsConvex)
        {
            if (aIsConvex && bIsConvex)
                return MinkowskiSumConvex(a, b);
            if (aIsConvex && bIsConvex)
            {
                return MinkowskiSumConvex(a, b);
            }
            else if (aIsConvex)
            {
                var result = MinkowskiSumConcaveConvex(b, a);
                return new Polygon(result.Path.Select(p => -p));
            }
            else if (bIsConvex)
                return MinkowskiSumConcaveConvex(a, b);
            return MinkowskiSumGeneral(a, b);
        }

        /// <summary>
        /// The Minkowski difference of the two polygons. This only functions on the outermost polygon (no holes).
        /// Note that this is NOT the same as the Minkowski sum of the negative of the second polygon (as is the case
        /// of the NoFitPolygon). Instead, this is the method used to calculate the polygon used to find overlap 
        /// (like in the GJK algorithm).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Polygon MinkowskiDifference(this Polygon a, Polygon b)
            => MinkowskiDifference(a, a.IsConvex(), b, b.IsConvex());


        /// <summary>
        /// The Minkowski difference of the two polygons. This only functions on the outermost polygon (no holes).
        /// Note that this is NOT the same as the Minkowski sum of the negative of the second polygon (as is the case
        /// of the NoFitPolygon). Instead, this is the method used to calculate the polygon used to find overlap 
        /// (like in the GJK algorithm).
        /// </summary>
        /// <param name="a"></param>
        /// <param name="aIsConvex"></param>
        /// <param name="b"></param>
        /// <param name="bIsConvex"></param>
        /// <returns></returns>
        public static Polygon MinkowskiDifference(this Polygon a, bool aIsConvex, Polygon b, bool bIsConvex)
        {
            if (aIsConvex && bIsConvex)
                return MinkowskiDiffConvex(a, b);
            else return MinkowskiDiffGeneral(a, b);
        }

        private static Polygon MinkowskiSumConvex(Polygon a, Polygon b)
        {
            int aStartIndex = FindMinY(a.Vertices);
            int bStartIndex = FindMinY(b.Vertices);

            var result = new List<Vector2>();
            var i = 0;
            var j = 0;
            var aLength = a.Vertices.Count;
            var bLength = b.Vertices.Count;
            while (i < aLength || j < bLength)
            {
                result.Add(a.Path[(i + aStartIndex) % aLength] + b.Path[(j + bStartIndex) % bLength]);
                var cross = (a.Path[(i + 1 + aStartIndex) % aLength] - a.Path[(i + aStartIndex) % aLength])
                    // will this always be correct? I'm worried that angle could be greater than 180, and then a
                    // false result would be returned. ...although, I tried to come up with a case to break it
                    // and couldn't I guess because you can't have an angle greater than 180 on convex shapes
                    .Cross(b.Path[(j + 1 + bStartIndex) % bLength] - b.Path[(j + bStartIndex) % bLength]);
                if (cross >= 0 && i < aLength)
                    ++i;
                if (cross <= 0 && j < bLength)
                    ++j;
            }
            return new Polygon(result);
        }

        private static Polygon MinkowskiDiffConvex(Polygon a, Polygon b)
        {
            int aStartIndex = FindMinY(a.Vertices);
            int bStartIndex = FindMinY(b.Vertices);

            var result = new List<Vector2>();
            var i = 0;
            var j = 0;
            var aLength = a.Vertices.Count;
            var bLength = b.Vertices.Count;
            while (i < aLength || j < bLength)
            {
                result.Add(a.Path[(i + aStartIndex) % aLength] - b.Path[(j + bStartIndex) % bLength]);
                var cross = (a.Path[(i + 1 + aStartIndex) % aLength] - a.Path[(i + aStartIndex) % aLength])
                    .Cross(b.Path[(j + 1 + bStartIndex) % bLength] - b.Path[(j + bStartIndex) % bLength]);
                if (cross >= 0 && i < aLength)
                    ++i;
                if (cross <= 0 && j < bLength)
                    ++j;
            }
            return new Polygon(result).RemoveSelfIntersections(ResultType.OnlyKeepPositive)[0];
        }

        private static int FindMinY(List<Vertex2D> vertices)
        {
            var minIndex = -1;
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (vertices[i].Y < minY)
                {
                    minIndex = i;
                    minX = vertices[i].X;
                    minY = vertices[i].Y;
                }
                else if (vertices[i].Y.IsPracticallySame(minY) && vertices[i].X < minX)
                {
                    minIndex = i;
                    minX = vertices[i].X;
                }
            }
            return minIndex;
        }

        private static Polygon MinkowskiSumConcaveConvex(Polygon a, Polygon b)
        {
            throw new NotImplementedException();
        }


        private static Polygon MinkowskiSumGeneral(Polygon a, Polygon b)
        {
            int aNum = a.Vertices.Count;
            int bNum = b.Vertices.Count;
            var result = new Vector2[aNum][];
            for (int i = 0; i < aNum; i++)
            {
                var p = new Vector2[bNum];
                for (int j = 0; j < bNum; j++)
                    p[j] = a.Path[i] + b.Path[j];

                result[i] = p;
            }
            //Presenter.ShowAndHang([result.Concat([a.Path.ToArray(), b.Path.ToArray()])]);
            return MakeQuadrilateralsAndMerge(aNum, bNum, result);
        }

        private static Polygon MinkowskiDiffGeneral(Polygon a, Polygon b)
        {
            int aNum = a.Vertices.Count;
            int bNum = b.Vertices.Count;
            var result = new Vector2[aNum][];
            for (int i = 0; i < aNum; i++)
            {
                var p = new Vector2[bNum];
                for (int j = 0; j < bNum; j++)
                    p[j] = a.Path[i] - b.Path[j];

                result[i] = p;
            }
            return MakeQuadrilateralsAndMerge(aNum, bNum, result);
        }

        private static Polygon MakeQuadrilateralsAndMerge(int aNum, int bNum, Vector2[][] result)
        {
            var k = 0;
            var quads = new Polygon[aNum * bNum];
            for (int i = 0; i < aNum; i++)
                for (int j = 0; j < bNum; j++)
                {
                    var quad = new Vector2[]
                    {
                    result[i % aNum][j % bNum],
                    result[(i + 1) % aNum][j % bNum],
                    result[(i + 1) % aNum][(j + 1) % bNum],
                    result[i % aNum][(j + 1) % bNum]
                    };
                    var quadArea = quad.Area();
                    if (quadArea.IsNegligible()) continue;
                    if (quad.Area() < 0)
                        quads[k++] = new Polygon(quad.Reverse());
                    else quads[k++] = new Polygon(quad);
                }
            var sumPoly = quads.Take(k).UnionPolygons()[0];
            //Presenter.ShowAndHang(sumPoly);
            //sumPoly.RemoveAllHoles();
            return sumPoly;
        }
    }
}