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
        /// The Minkowski sum of the two polygons. This only functions on the outermost
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Polygon MinkowskiSum(Polygon a, Polygon b)
        {
            var aIsConvex = a.IsConvex();
            var bIsConvex = b.IsConvex();
            if (a.IsConvex() && b.IsConvex())
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
            var flipResult = a.IsPositive != b.IsPositive;
            int aStartIndex = FindMinY(a.Vertices);
            int bStartIndex = FindMinY(b.Vertices);
            var i = 0;
            var j = 0;
            var aLength = a.Vertices.Count;
            var bLength = b.Vertices.Count;
            var lastPoint = a.Path[(i + aStartIndex) % aLength] + b.Path[(j + bStartIndex) % bLength];
            var result = new List<Vector2>();
            var direction = 1; // positive is counterclockwise
            aStartIndex++;  // because these are used for angles and edges in the remainder of the function,
            bStartIndex++;  // then we need to increment by one, since edge index, i, ends at vertex index, i
                            // (or rather, edge index i+1 starts at vertex index i)
            var edgeVector = Vector2.Zero;
            bool converged;
            do
            {
                converged = AddNextBennellSongPoint(a, b, aStartIndex, bStartIndex,
                    ref i, ref j, aLength, bLength, ref lastPoint, result, ref direction, ref edgeVector);
            } while (!converged);

            var minkowski = new Polygon(result);
            //Presenter.ShowAndHang(minkowski);
            if (flipResult)
                minkowski = minkowski.Copy(true, true);
            minkowski = minkowski.RemoveSelfIntersections(ResultType.OnlyKeepPositive).LargestPolygon();
            minkowski.RemoveAllHoles();
            if (flipResult)
                minkowski = minkowski.Copy(true, true);
            return minkowski;
        }

        private static Polygon MinkowskiSumGeneralBennellSong(Polygon a, Polygon b)
        {
            var bSpitIndices = new List<(int, bool)>();
            var aLength = a.Vertices.Count;
            var bLength = b.Vertices.Count;
            var flipResult = a.IsPositive != b.IsPositive;
            int aStartIndex = FindMinY(a.Vertices);
            int bStartIndex = FindMinY(b.Vertices);
            var lastIsConvex = true;
            for (int i = 0, j = bLength - 1; i < bLength; j = i++)
            {
                var cross = b.Edges[(j + bStartIndex) % bLength].Vector.Cross(b.Edges[(i + bStartIndex) % bLength].Vector);
                var thisIsConvex = cross >= 0;
                if (thisIsConvex != lastIsConvex) bSpitIndices.Add((j + bStartIndex, thisIsConvex));
                lastIsConvex = thisIsConvex;
            }
            if (!lastIsConvex) bSpitIndices.Insert(0, (bStartIndex, true));
            var minkowskis = new List<Polygon>();
            foreach ((int startIndex, bool isConvex) in bSpitIndices)
            {
                var i = 0;
                var j = 0;
                var bDelta = 1;
                var bAddSign = 1;
                var lastPoint = a.Path[(i + aStartIndex) % aLength] + b.Path[(j + bStartIndex) % bLength];
                var result = new List<Vector2>();
                var direction = 1; // positive is counterclockwise
                aStartIndex++;  // because these are used for angles and edges in the remainder of the function,
                bStartIndex++;  // then we need to increment by one, since edge index, i, ends at vertex index, i
                                // (or rather, edge index i+1 starts at vertex index i)
                var edgeVector = Vector2.Zero;
                bool converged;
                do
                {
                    converged = AddNextBennellSongPoint(a, b, aStartIndex, bStartIndex,
                        ref i, ref j, aLength, bLength, ref lastPoint, result, ref direction, ref edgeVector);
                } while (!converged);

                var thisSumPolygon = new Polygon(result);
                //Presenter.ShowAndHang(minkowski);
                if (flipResult)
                    thisSumPolygon = thisSumPolygon.Copy(true, true);
                thisSumPolygon = thisSumPolygon.RemoveSelfIntersections(ResultType.OnlyKeepPositive).LargestPolygon();
                thisSumPolygon.RemoveAllHoles();
                minkowskis.Add(thisSumPolygon);
            }
            var minkowski = minkowskis.UnionPolygons().LargestPolygon();
            minkowski.RemoveAllHoles();
            if (flipResult)
                minkowski = minkowski.Copy(true, true);
            return minkowski;
        }

        private static bool AddNextBennellSongPoint(Polygon aConcave, Polygon bConvex, int aStartIndex, int bStartIndex, ref int i, ref int j, int aLength, int bLength, ref Vector2 lastPoint, List<Vector2> result, ref int direction, ref Vector2 edgeVector)
        {
            var converged = false;
            lastPoint = lastPoint + edgeVector;
            result.Add(lastPoint);
            //Presenter.ShowAndHang(result);

            if (Math.Sign(bConvex.Edges[(j + bStartIndex) % bLength].Vector.Cross(aConcave.Edges[(i + aStartIndex) % aLength].Vector)) == direction)
            {
                edgeVector = direction * bConvex.Edges[(j + bStartIndex) % bLength].Vector;
                j += direction;
                if (j < 0) j = bLength - 1;
            }
            else
            {
                edgeVector = aConcave.Edges[(i + aStartIndex) % aLength].Vector;
                if (i == aLength)
                    converged = true;
                i++;
                var nextEdgeVector = aConcave.Edges[(i + aStartIndex) % aLength].Vector;
                var isConcave = edgeVector.Cross(nextEdgeVector) < 0;
                if (direction == 1 && isConcave)
                {
                    j--;
                    if (j < 0) j = bLength - 1;
                    direction = -1;
                }
                else if (direction == -1 && !isConcave)
                {
                    j++;
                    direction = 1;
                }
            }

            return converged;
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
            return MakeQuadrilateralsAndMerge(aNum, bNum, result);
        }

        public static Polygon MinkowskiDifference(Polygon a, Polygon b)
        {
            if (a.IsConvex() && b.IsConvex())
                return MinkowskiDiffConvex(a, b);
            else return MinkowskiDiffGeneral(a, b);
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
            sumPoly.RemoveAllHoles();
            //Presenter.ShowAndHang(sumPoly);
            return sumPoly;
        }
    }
}