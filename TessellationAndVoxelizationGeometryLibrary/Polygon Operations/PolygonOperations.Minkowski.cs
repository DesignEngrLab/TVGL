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
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Windows.Input;

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
        {
            var aConcaveVertices = a.GetConcaveVertices().ToList();
            var bConcaveVertices = b.GetConcaveVertices().ToList();
            if (aConcaveVertices.Count == 0 & bConcaveVertices.Count == 0)
                return [MinkowskiSumConvex(a, b)];
            else if (aConcaveVertices.Count == 0)
                return MinkowskiSumConvexConcave(a, b);
            else if (bConcaveVertices.Count == 0)
                return MinkowskiSumConvexConcave(b, a);
            if (aConcaveVertices.Count < bConcaveVertices.Count)
                return MinkowskiSumGeneral(a, aConcaveVertices, b);
            else return MinkowskiSumGeneral(b, bConcaveVertices, a);
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

        private static List<Polygon> MinkowskiSumConvexConcave(Polygon a, Polygon b)
        {
            var aStartEdge = a.Vertices[FindMinY(a.Vertices)];
            var bStartEdge = b.Vertices[FindMinY(b.Vertices)];
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var complexPaths = new List<Polygon>();
            var knownWrongPoints = new List<List<bool>>();
            ConvolutionCycle(a, b, aStartEdge, bStartEdge, visitedHash, aEdgeAngles, bEdgeAngles, complexPaths, knownWrongPoints);

            return complexPaths[0].RemoveSelfIntersections(
                flipResult ? ResultType.OnlyKeepNegative : ResultType.OnlyKeepPositive, knownWrongPoints[0]);
        }

        private static void ConvolutionCycle(Polygon a, Polygon b, Vertex2D aStart, Vertex2D bStart,
            Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D> visitedHash, Dictionary<PolygonEdge, double> aEdgeAngles,
            Dictionary<PolygonEdge, double> bEdgeAngles, List<Polygon> results, List<List<bool>> allKnownWrongPoints)
        {
            var aVertex = aStart;
            var bVertex = bStart;
            var result = new List<Vertex2D>(); // { aVertex.ToPoint.Coordinates + bVertex.ToPoint.Coordinates };
            var knownWrongPoints = new List<bool>(); // { bVertex.Vector.Cross(nextBEdge.Vector) < 0 };
            var currentLoopIndex = results.Count;
            while (true)
            {
                var newPointAdded = false;
                var nextAEdge = aVertex.StartLine;
                var nextBEdge = bVertex.StartLine;
                var aAngle = aEdgeAngles[nextAEdge];
                var aPrevAngle = aEdgeAngles[aVertex.EndLine];
                var bAngle = bEdgeAngles[nextBEdge];
                var bPrevAngle = bEdgeAngles[bVertex.EndLine];
                if (!visitedHash.TryGetValue((aVertex, bVertex, true), out var aExistingVertex)
                    && firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle))
                {
                    var newVertex = new Vertex2D(aVertex.Coordinates + bVertex.Coordinates, visitedHash.Count, currentLoopIndex);
                    visitedHash.Add((aVertex, bVertex, true), newVertex);
                    result.Add(newVertex);
                    var prevBCrossNextB = bVertex.EndLine.Vector.Cross(nextBEdge.Vector);
                    knownWrongPoints.Add(prevBCrossNextB < 0);
                    aVertex = nextAEdge.ToPoint;
                    newPointAdded = true;
                }
                if (!visitedHash.TryGetValue((aVertex, bVertex, false), out var bExistingVertex) &&
                    ((!newPointAdded && firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle))
                    || (newPointAdded && aAngle.IsPracticallySame(bAngle))))
                {
                    var newVertex = new Vertex2D(aVertex.Coordinates + bVertex.Coordinates, visitedHash.Count, currentLoopIndex);
                    visitedHash.Add((aVertex, bVertex, false), newVertex);
                    result.Add(newVertex);
                    var prevACrossNextA = aVertex.EndLine.Vector.Cross(nextAEdge.Vector);
                    knownWrongPoints.Add(prevACrossNextA < 0);
                    bVertex = nextBEdge.ToPoint;
                    newPointAdded = true;
                }
                //Presenter.ShowAndHang(result.Select(v => v.Coordinates), closeShape: false);
                if (!newPointAdded)
                {
                    if (result.Count == 0) return;
                    //var repeatVertex = aExistingVertex ?? bExistingVertex;
                    for (var i = 0; i < result.Count; i++)
                    {
                        if (aExistingVertex == result[i] || bExistingVertex == result[i])
                        {
                            allKnownWrongPoints.Add(knownWrongPoints.Skip(i).ToList());
                            results.Add(new Polygon(result.Skip(i), currentLoopIndex, true));
                            if (i > 0)
                            {
                                currentLoopIndex++;
                                foreach (var vert in result.Take(i))
                                    vert.LoopID = currentLoopIndex;
                                results.Add(new Polygon(result.Take(i + 1), currentLoopIndex, false));
                                allKnownWrongPoints.Add(knownWrongPoints.Take(i + 1).ToList());
                            }
                            return;
                        }
                    }
                    for (int i = 0; i < results.Count; i++)
                    {
                        Polygon loop = results[i];
                        if (loop.IsClosed) continue;
                        if (loop.Vertices[0] == aExistingVertex || loop.Vertices[0] == bExistingVertex)
                        {
                            foreach (var vert in result)
                                vert.LoopID = i;
                            if (loop.Vertices[^1] == result[0])
                            {
                                results[i] = new Polygon(result.Concat(loop.Vertices.Take(loop.Vertices.Count - 1)), i, true);
                                allKnownWrongPoints[i] = knownWrongPoints.Concat(allKnownWrongPoints[i]).ToList();
                            }
                            else
                            {
                                results[i] = new Polygon(result.Concat(loop.Vertices), i, false);
                                allKnownWrongPoints[i] = knownWrongPoints.Concat(allKnownWrongPoints[i]).ToList();
                            }
                            return;
                        }
                    }
                    var repeatVertex = WhichExistingToUse(results, aExistingVertex, bExistingVertex);
                    results.Add(new Polygon(result.Concat([repeatVertex]), currentLoopIndex, false));
                    allKnownWrongPoints.Add(knownWrongPoints);
                    return;
                }
            }
        }

        private static Vertex2D WhichExistingToUse(List<Polygon> results, Vertex2D aExistingVertex, Vertex2D bExistingVertex)
        {
            if (aExistingVertex == null ) return bExistingVertex;
            if (bExistingVertex == null) return aExistingVertex;
            if (results[aExistingVertex.LoopID].IsClosed) return bExistingVertex;
            if ( results[bExistingVertex.LoopID].IsClosed) return aExistingVertex;
            return null;
        }

        private static bool firstAngleIsBetweenOthersCCW(double aAngle, double bPrevAngle, double bAngle)
        {
            if (bPrevAngle <= aAngle && aAngle <= bAngle) return true;
            if (bPrevAngle > bAngle)
                if (aAngle >= bPrevAngle || aAngle <= bAngle) return true;
            return false;
        }

        private static List<Polygon> MinkowskiSumGeneral(Polygon a, List<Vertex2D> aConcaveVertices, Polygon b)
        {
            var polygons = new List<Polygon>();
            var aStartEdge = a.Edges[FindMinY(a.Vertices)].FromPoint;
            var bStartEdge = b.Edges[FindMinY(a.Vertices)].FromPoint;
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Constants.Pseudoangle(e.Vector.X, e.Vector.Y));
            var complexPaths = new List<Polygon>();
            var knownWrongPoints = new List<List<bool>>();
            ConvolutionCycle(a, b, aStartEdge, bStartEdge,
                visitedHash, aEdgeAngles, bEdgeAngles, complexPaths, knownWrongPoints);
            visitedHash.Clear();
            //Presenter.ShowAndHang(complexPaths);
            foreach (var aConcavity in aConcaveVertices)
            {
                //var aAngle = aEdgeAngles[aConcavity.StartLine];
                foreach (var bVertex in b.Vertices)
                {
                    //if (!visitedHash.ContainsKey((aConcavity, bVertex)))
                    //{
                    //var bPrevAngle = bEdgeAngles[bVertex.EndLine];
                    //var bAngle = bEdgeAngles[bVertex.StartLine];
                    //if (firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle))
                    //{
                    ConvolutionCycle(a, b, aConcavity, bVertex,
                        visitedHash, aEdgeAngles, bEdgeAngles, complexPaths, knownWrongPoints);
                    //Presenter.ShowAndHang(complexPaths);
                    //}
                    //}
                }
            }
            Presenter.ShowAndHang(complexPaths.Skip(2));
            MergeIncompletePolygons(complexPaths, knownWrongPoints);
            foreach (var c in complexPaths)
                c.IsClosed = true;
            Presenter.ShowAndHang(complexPaths);
            return complexPaths.UnionPolygons().CreatePolygonTree(true);
            //return polygons.CreatePolygonTree(true);
        }

        private static void MergeIncompletePolygons(List<Polygon> complexPaths, List<List<bool>> knownWrongPoints)
        {
            for (int i = complexPaths.Count - 1; i >= 0; i--)
            {
                if (complexPaths[i].IsClosed) continue;
                var loop = complexPaths[i];
                for (int j = i - 1; j >= 0; j--)
                {
                    if (complexPaths[j].IsClosed) continue;
                    if (loop.Vertices[0] == complexPaths[j].Vertices[0]) ;
                }
            }
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

    internal class EdgePairToIntComparator : IEqualityComparer<(Vertex2D, Vertex2D, bool)>
    {
        readonly int baseFactor1;
        readonly int baseFactor2;
        public EdgePairToIntComparator(Polygon a, Polygon b)
        {
            baseFactor1 = a.Vertices.Count;
            baseFactor2 = a.Vertices.Count * b.Vertices.Count;
        }
        public bool Equals((Vertex2D, Vertex2D, bool) x, (Vertex2D, Vertex2D, bool) y)
        {
            return x.Item3 == y.Item3 && x.Item2 == y.Item2 && x.Item1 == y.Item1;
        }

        public int GetHashCode([DisallowNull] (Vertex2D, Vertex2D, bool) obj)
        {
            return obj.Item1.IndexInList + obj.Item2.IndexInList * baseFactor1
                + (obj.Item3 ? baseFactor2 : 0);
        }
    }
}