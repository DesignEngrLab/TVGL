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
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

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
        public static List<Polygon> MinkowskiSum(this Polygon a, Polygon b)
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
        public static List<Polygon> MinkowskiSum(this Polygon a, bool aIsConvex, Polygon b, bool bIsConvex)
        {
            if (aIsConvex && bIsConvex)
                return [MinkowskiSumConvex(a, b)];
            else if (aIsConvex)
            {
                var result = MinkowskiSumConcaveConvex(b, a);
                foreach (var poly in result)
                    poly.Transform(Matrix3x3.CreateScale(-1));
                return result;
            }
            else if (bIsConvex)
                return MinkowskiSumConcaveConvex(a, b);
            return [MinkowskiSumGeneralBrute(a, b)];
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

        private static List<Polygon> MinkowskiSumConcaveConvex(Polygon a, Polygon b)
        {
            var aStartEdge = a.Edges[FindMinY(a.Vertices)];
            var bStartEdge = b.Edges[FindMinY(b.Vertices)];
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new HashSet<(Vertex2D, Vertex2D)>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            ConvolutionCycle(a, b, aStartEdge, bStartEdge, visitedHash, aEdgeAngles, bEdgeAngles, out var result, out var knownWrongPoints);

            var complexPath = new Polygon(result);
            return complexPath.RemoveSelfIntersections(
                flipResult ? ResultType.OnlyKeepNegative : ResultType.OnlyKeepPositive, knownWrongPoints);
        }

        private static void ConvolutionCycle(Polygon a, Polygon b, PolygonEdge aStartEdge, PolygonEdge bStartEdge,
            HashSet<(Vertex2D, Vertex2D)> visitedHash, Dictionary<PolygonEdge, double> aEdgeAngles,
            Dictionary<PolygonEdge, double> bEdgeAngles, out List<Vector2> result, out List<bool> knownWrongPoints)
        {
            var prevAEdge = aStartEdge;
            var prevBEdge = bStartEdge;
            result = new List<Vector2> { prevAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates };
            knownWrongPoints = new List<bool> { false };
            var nextAEdge = prevAEdge.ToPoint.StartLine;
            var nextBEdge = prevBEdge.ToPoint.StartLine;
            do
            {
                var aAngle = aEdgeAngles[nextAEdge];
                var aPrevAngle = aEdgeAngles[prevAEdge];
                var bAngle = bEdgeAngles[nextBEdge];
                var bPrevAngle = bEdgeAngles[prevBEdge];
                if (firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle))
                {
                    result.Add(nextAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates);
                    visitedHash.Add((nextAEdge.ToPoint, prevBEdge.ToPoint));
                    var prevACrossNextA = prevAEdge.Vector.Cross(nextAEdge.Vector);
                    knownWrongPoints.Add(prevACrossNextA < 0);
                    prevAEdge = nextAEdge;
                    nextAEdge = nextAEdge.ToPoint.StartLine;
                }
                if (firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle))
                {
                    result.Add(nextBEdge.ToPoint.Coordinates + prevAEdge.ToPoint.Coordinates);
                    var prevBCrossNextB = prevBEdge.Vector.Cross(nextBEdge.Vector);
                    knownWrongPoints.Add(prevBCrossNextB < 0);
                    prevBEdge = nextBEdge;
                    nextBEdge = nextBEdge.ToPoint.StartLine;
                }
                //Presenter.ShowAndHang(result);

            } while (prevAEdge != aStartEdge || prevBEdge != bStartEdge);
        }

        private static bool firstAngleIsBetweenOthersCCW(double aAngle, double bPrevAngle, double bAngle)
        {
            if (bPrevAngle <= aAngle && aAngle <= bAngle) return true;
            if (bPrevAngle > bAngle)
                if (aAngle >= bPrevAngle || aAngle <= bAngle) return true;
            return false;
        }

        private static List<Polygon> MinkowskiSumConcaveConvexOLD(Polygon a, Polygon b)
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
                converged = false;
                lastPoint = lastPoint + edgeVector;
                result.Add(lastPoint);
                Presenter.ShowAndHang(result);

                if (Math.Sign(b.Edges[(j + bStartIndex) % bLength].Vector.Cross(a.Edges[(i + aStartIndex) % aLength].Vector)) == direction)
                {
                    edgeVector = direction * b.Edges[(j + bStartIndex) % bLength].Vector;
                    j += direction;
                    if (j < 0) j = bLength - 1;
                }
                else
                {
                    edgeVector = a.Edges[(i + aStartIndex) % aLength].Vector;
                    if (i == aLength)
                        converged = true;
                    i++;
                    var nextEdgeVector = a.Edges[(i + aStartIndex) % aLength].Vector;
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
            } while (!converged);

            var minkowski = new Polygon(result);
            //Presenter.ShowAndHang(minkowski);
            if (flipResult)
                minkowski = minkowski.Copy(true, true);
            minkowski = minkowski.RemoveSelfIntersections(ResultType.BothPermitted).LargestPolygon();
            minkowski.RemoveAllHoles();
            if (flipResult)
                minkowski = minkowski.Copy(true, true);
            return [minkowski];
        }

        private static Polygon MinkowskiSumGeneralBrute(Polygon a, Polygon b)
        {
            int aNum = a.Vertices.Count;
            var aPathD = new PathD(aNum);
            for (int i = 0; i < aNum; i++)
                aPathD.Add(new PointD(a.Path[i].X, a.Path[i].Y));

            int bNum = b.Vertices.Count;
            var bPathD = new PathD(bNum);
            for (int i = 0; i < bNum; i++)
                bPathD.Add(new PointD(b.Path[i].X, b.Path[i].Y));
            var pathsD = Clipper2Lib.Minkowski.Sum(aPathD, bPathD, true, 7);
            var resultPolygons = new List<Polygon>();
            foreach (var pathD in pathsD)
            {
                var path = new List<Vector2>();
                foreach (var pointD in pathD)
                    path.Add(new Vector2(pointD.x, pointD.y));
                resultPolygons.Add(new Polygon(path));
            }
            return resultPolygons.CreatePolygonTree(true).FirstOrDefault();
        }


        private static List<Polygon> MinkowskiSumGeneral(Polygon a, Polygon b)
        {
            var aConcaveVertices = a.Vertices.Where(v => !v.IsConvex.GetValueOrDefault(false)).ToList();
            var bConcaveVertices = b.Vertices.Where(v => !v.IsConvex.GetValueOrDefault(false)).ToList();
            if (aConcaveVertices.Count > bConcaveVertices.Count)
            {
                var result = MinkowskiSumGeneral(b, bConcaveVertices, a);
                foreach (var poly in result)
                    poly.Transform(Matrix3x3.CreateScale(-1));
                return result;
            }
            else return MinkowskiSumGeneral(a, aConcaveVertices, b);
        }
        private static List<Polygon> MinkowskiSumGeneral(Polygon a, List<Vertex2D> aConcaveVertices, Polygon b)
        {
            var aStartEdge = a.Edges[FindMinY(a.Vertices)];
            var bStartEdge = b.Edges[FindMinY(b.Vertices)];
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new HashSet<(Vertex2D, Vertex2D)>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            ConvolutionCycle(a, b, aStartEdge, bStartEdge, visitedHash, aEdgeAngles, bEdgeAngles,
                out var result, out var knownWrongPoints);

            foreach (var aConcavity in aConcaveVertices)
            {
                var aAngle = aEdgeAngles[aConcavity.StartLine];
                foreach (var bVertex in b.Vertices)
                {
                    if (visitedHash.Contains((aConcavity, bVertex)))
                        continue;
                    var bAngle = bEdgeAngles[bVertex.StartLine];
                    var bPrevAngle = bEdgeAngles[bVertex.EndLine];
                    if (firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle))
                        ConvolutionCycle(a, b, aConcavity.EndLine, bVertex.EndLine, visitedHash, aEdgeAngles, bEdgeAngles,
                            out var newResult, out var newKnownWrongPoints);
                }
            }
            var complexPath = new Polygon(result);
            return complexPath.RemoveSelfIntersections(
                flipResult ? ResultType.OnlyKeepNegative : ResultType.OnlyKeepPositive, knownWrongPoints);
        }

        private static Polygon MinkowskiDiffGeneral(Polygon a, Polygon b)
        {
            int aNum = a.Vertices.Count;
            var aPathD = new PathD(aNum);
            for (int i = 0; i < aNum; i++)
                aPathD.Add(new PointD(a.Path[i].X, a.Path[i].Y));

            int bNum = b.Vertices.Count;
            var bPathD = new PathD(bNum);
            for (int i = 0; i < bNum; i++)
                bPathD.Add(new PointD(b.Path[i].X, b.Path[i].Y));
            var pathsD = Clipper2Lib.Minkowski.Diff(aPathD, bPathD, true, 7);
            var resultPolygons = new List<Polygon>();
            foreach (var pathD in pathsD)
            {
                var path = new List<Vector2>();
                foreach (var pointD in pathD)
                    path.Add(new Vector2(pointD.x, pointD.y));
                resultPolygons.Add(new Polygon(path));
            }
            return resultPolygons.CreatePolygonTree(true).FirstOrDefault();
        }

    }

    internal class EdgePairToIntComparator : IEqualityComparer<(Vertex2D, Vertex2D)>
    {
        readonly int factor;
        public EdgePairToIntComparator(Polygon a, Polygon b)
        {
            factor = a.Edges.Count;
        }
        public bool Equals((Vertex2D, Vertex2D) x, (Vertex2D, Vertex2D) y)
        {
            return x.Item1 == y.Item1 && x.Item2 == y.Item2;
        }

        public int GetHashCode([DisallowNull] (Vertex2D, Vertex2D) obj)
        {
            return obj.Item1.IndexInList + obj.Item2.IndexInList * factor;
        }
    }
}