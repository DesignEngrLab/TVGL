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
using Microsoft.Extensions.Logging;
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
        public static List<Polygon> MinkowskiSumClipper(this Polygon a, Polygon b)
        {
            int aVertCount = a.Vertices.Count;
            int bVertCount = b.Vertices.Count;
            var aAtEveryBPt = new Polygon[bVertCount];
            var polyIndex = 0;
            // make a copy of the 'a' polygon at every point of 'b'
            foreach (var bVertex in b.Vertices)
            {
                var bCoords = bVertex.Coordinates;
                var path = new Vector2IP[aVertCount];
                var vertIndex = 0;
                foreach (var aVertex in a.Vertices)
                    path[vertIndex++] = Vector2IP.Add2D(bCoords, aVertex.Coordinates);
                aAtEveryBPt[polyIndex++] = new Polygon(path);
            }

            var quadrilaterals = new Polygon[bVertCount * aVertCount];
            var quadIndex = 0;
            int prevBIndex = bVertCount - 1;
            for (int bIndex = 0; bIndex < bVertCount; bIndex++)
            {
                int prevAIndex = aVertCount - 1;
                for (int aIndex = 0; aIndex < aVertCount; aIndex++)
                {
                    var quad = new Polygon([ aAtEveryBPt[prevBIndex].Vertices[prevAIndex],
                        aAtEveryBPt[bIndex].Vertices[prevAIndex],
                        aAtEveryBPt[bIndex].Vertices[aIndex],
                        aAtEveryBPt[prevBIndex].Vertices[aIndex] ]);
                    if (!quad.IsPositive)
                        quad.Reverse(); //result.Add(Clipper.ReversePath(quad));
                    else
                        quadrilaterals[quadIndex++] = quad;
                    prevAIndex = aIndex;
                }
                prevBIndex = bIndex;
            }
            return quadrilaterals.UnionPolygons(PolygonCollection.SeparateLoops);
        }

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
            var aConcaveVertices = a.GetConcaveVertices().ToArray();
            var bConcaveVertices = b.GetConcaveVertices().ToArray();
            if (aConcaveVertices.Length == 0 & bConcaveVertices.Length == 0)
                return [MinkowskiSumConvex(a, b)];
            else if (aConcaveVertices.Length == 0)
                //return MinkowskiSumConcaveConvex(a, b);
                return MinkowskiSumMain(a, Array.Empty<Vertex2D>(), b);
            else if (bConcaveVertices.Length == 0)
                //return MinkowskiSumConcaveConvex(b, a);
                return MinkowskiSumMain(b, Array.Empty<Vertex2D>(), a);
            if (aConcaveVertices.Length < bConcaveVertices.Length)
                return MinkowskiSumClipper(a, b);
            else return MinkowskiSumClipper(b, a);
        }

        private static Polygon MinkowskiSumConvex(Polygon a, Polygon b)
        {
            var aStartVertex = FindMinY(a.Vertices);
            var bStartVertex = FindMinY(b.Vertices);
            var aVertex = aStartVertex;
            var bVertex = bStartVertex;
            var result = new List<Vertex2D>();
            var vertNum = 0;
            var aCompleted = false;
            var bCompleted = false;
            do
            {
                result.Add(new Vertex2D(Vector2IP.Add2D(aVertex.Coordinates, bVertex.Coordinates), vertNum++, 0));
                var cross = aVertex.StartLine.Vector3D.CrossSign(bVertex.StartLine.Vector3D);
                // will this always be correct? I'm worried that angle could be greater than 180, and then a
                // false result would be returned. ...although, I tried to come up with a case to break it
                // and couldn't I guess because you can't have an angle greater than 180 on convex shapes
                if (cross >= 0 && !aCompleted)
                    aVertex = aVertex.StartLine.ToPoint;
                if (cross <= 0 && !bCompleted)
                    bVertex = bVertex.StartLine.ToPoint;
                aCompleted = aVertex == aStartVertex;
                bCompleted = bVertex == bStartVertex;
            } while (!aCompleted || !bCompleted);
            return new Polygon(result);
        }

        private static Vertex2D FindMinY(List<Vertex2D> vertices)
        {
            Vertex2D minVertex = null;
            var minX = double.MaxValue;
            var minY = double.MaxValue;
            foreach (var v in vertices)
            {
                if (v.Y < minY)
                {
                    minVertex = v;
                    minX = v.X;
                    minY = v.Y;
                }
                else if (v.Y.IsPracticallySame(minY) && v.X < minX)
                {
                    minVertex = v;
                    minX = v.X;
                }
            }
            return minVertex;
        }

        private static List<Polygon> MinkowskiSumConcaveConvex(Polygon a, Polygon b)
        {
            var aStartEdge = FindMinY(a.Vertices).EndLine;
            var bStartEdge = FindMinY(b.Vertices).EndLine;
            var flipResult = a.IsPositive != b.IsPositive;
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector3D.X, e.Vector3D.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector3D.X, e.Vector3D.Y));

            var prevAEdge = aStartEdge;
            var prevBEdge = bStartEdge;
            var result = new List<Vector2IP> { Vector2IP.Add2D(prevAEdge.ToPoint.Coordinates, prevBEdge.ToPoint.Coordinates) };
            var knownWrongPoints = new List<bool> { false };
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
                    result.Add(Vector2IP.Add2D(nextAEdge.ToPoint.Coordinates, prevBEdge.ToPoint.Coordinates));
                    var prevBCrossNextB = prevBEdge.Vector3D.CrossSign(nextBEdge.Vector3D);
                    knownWrongPoints.Add(prevBCrossNextB < 0);
                    prevAEdge = nextAEdge;
                    nextAEdge = nextAEdge.ToPoint.StartLine;
                }
                if (firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle))
                {
                    result.Add(Vector2IP.Add2D(nextBEdge.ToPoint.Coordinates, prevAEdge.ToPoint.Coordinates));
                    var prevACrossNextA = prevAEdge.Vector3D.CrossSign(nextAEdge.Vector3D);
                    knownWrongPoints.Add(prevACrossNextA < 0);
                    prevBEdge = nextBEdge;
                    nextBEdge = nextBEdge.ToPoint.StartLine;
                }
            } while (prevAEdge != aStartEdge || prevBEdge != bStartEdge);

            var clipperPaths = PolygonOperations.ConvertToClipperPaths([new Polygon(result)]);
            if (flipResult)
            {
                //foreach (var c in clipperPaths)
                //    Clipper.ReversePath(c);
                clipperPaths = Clipper.Union(clipperPaths, FillRule.Negative);
            }
            //else
            clipperPaths = Clipper.Union(clipperPaths, FillRule.Positive);

            var polygons = clipperPaths.Select(clipperPath
              => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale)))).ToList();
            //Presenter.ShowAndHang(polygons);
            return polygons;
        }



        private static List<Polygon> MinkowskiSumMain(Polygon a, Vertex2D[] aConcaveVertices, Polygon b)
        {
            var polygons = new List<Polygon>();
            var flipResult = a.IsPositive != b.IsPositive;
            var visitedHash = new Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D>(new EdgePairToIntComparator(a, b));
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector3D.X, e.Vector3D.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector3D.X, e.Vector3D.Y));
            var startsQueue = new Stack<(Vertex2D, Vertex2D)>();
            startsQueue.Push((FindMinY(a.Vertices), FindMinY(b.Vertices)));
            foreach (var aConcavity in aConcaveVertices)
                foreach (var bVertex in b.Vertices)
                    startsQueue.Push((aConcavity, bVertex));

            var knownWrongPoints = new List<List<bool>>();
            while (startsQueue.Count > 0)
            {
                var (aStartVertex, bStartVertex) = startsQueue.Pop();
                var aParent = aStartVertex.EndLine.FromPoint;
                var aAngle = aEdgeAngles[aParent.StartLine];
                var aPrevAngle = aEdgeAngles[aParent.EndLine];
                var bAngle = bEdgeAngles[bStartVertex.StartLine];
                var bPrevAngle = bEdgeAngles[bStartVertex.EndLine];
                var followDir = GetFollowDirection(visitedHash, aParent, bStartVertex, out _, out _, aAngle, aPrevAngle,
                    bAngle, bPrevAngle);
                if (followDir == ConvolutionDirection.A || followDir == ConvolutionDirection.AButQueueUpB || followDir == ConvolutionDirection.Both)
                {
                    var startVertex = new Vertex2D(Vector2IP.Add2D(aStartVertex.Coordinates, bStartVertex.Coordinates), 0, polygons.Count);
                    visitedHash.Add((aParent, bStartVertex, true), startVertex);
                    ConvolutionCycle(a, b, aStartVertex, bStartVertex,
                        visitedHash, aEdgeAngles, bEdgeAngles, startsQueue, polygons, knownWrongPoints, new List<Vertex2D> { startVertex },
                      0, new List<bool> { false });
                }
                else
                {
                    var bParent = bStartVertex.EndLine.FromPoint;
                    aAngle = aEdgeAngles[aStartVertex.StartLine];
                    aPrevAngle = aEdgeAngles[aStartVertex.EndLine];
                    bAngle = bEdgeAngles[bParent.StartLine];
                    bPrevAngle = bEdgeAngles[bParent.EndLine];
                    followDir = GetFollowDirection(visitedHash, aStartVertex, bParent, out _, out _, aAngle, aPrevAngle,
                       bAngle, bPrevAngle);
                    if (followDir == ConvolutionDirection.B || followDir == ConvolutionDirection.Both)
                    {
                        var startVertex = new Vertex2D(Vector2IP.Add2D(aStartVertex.Coordinates, bStartVertex.Coordinates), 0, polygons.Count);
                        visitedHash.Add((aStartVertex, bParent, false), startVertex);
                        ConvolutionCycle(a, b, aStartVertex, bStartVertex,
                            visitedHash, aEdgeAngles, bEdgeAngles, startsQueue, polygons, knownWrongPoints, new List<Vertex2D> { startVertex },
                          0, new List<bool> { false });
                    }
                }
            }
            //Presenter.ShowAndHang(polygons);
            var clipperPaths = PolygonOperations.ConvertToClipperPaths(polygons);
            if (flipResult)
            {
                //foreach (var c in clipperPaths)
                //    Clipper.ReversePath(c);
                clipperPaths = Clipper.Union(clipperPaths, FillRule.Negative);
            }
            //else
            clipperPaths = Clipper.Union(clipperPaths, FillRule.Positive);

            polygons = clipperPaths.Select(clipperPath
              => new Polygon(clipperPath.Select(point => new Vector2(point.X / scale, point.Y / scale)))).ToList();
            //Presenter.ShowAndHang(polygons);
            return polygons;
            polygons.RemoveAll(c => !c.IsClosed || c.Vertices.Count <= 2);

            if (flipResult)
            {
                foreach (var c in polygons)
                    c.Reverse();
                polygons = polygons.IntersectPolygons(PolygonCollection.SeparateLoops); //.CreatePolygonTree(true);
                foreach (var c in polygons)
                    c.Reverse();
                return polygons;
            }
            return polygons.UnionPolygons(PolygonCollection.SeparateLoops); //.CreatePolygonTree(true);
                                                                            //Presenter.ShowAndHang(polygons);
        }

        private static void ConvolutionCycle(Polygon a, Polygon b, Vertex2D aStart, Vertex2D bStart,
            Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D> visitedHash, Dictionary<PolygonEdge, double> aEdgeAngles,
            Dictionary<PolygonEdge, double> bEdgeAngles, Stack<(Vertex2D, Vertex2D)> startsQueue, List<Polygon> results,
            List<List<bool>> allKnownWrongPoints, List<Vertex2D> result, int currentLoopIndex, List<bool> knownWrongPoints)
        {
            var aVertex = aStart;
            var bVertex = bStart;
            while (true)
            {
                //Presenter.ShowAndHang(result.Select(v => v.Coordinates), closeShape: false);
                var nextAEdge = aVertex.StartLine;
                var aAngle = aEdgeAngles[nextAEdge];
                var aPrevAngle = aEdgeAngles[aVertex.EndLine];
                var nextBEdge = bVertex.StartLine;
                var bAngle = bEdgeAngles[nextBEdge];
                var bPrevAngle = bEdgeAngles[bVertex.EndLine];
                var followDirection = GetFollowDirection(visitedHash, aVertex, bVertex, out var aExistingVertex,
                    out var bExistingVertex, aAngle, aPrevAngle, bAngle, bPrevAngle);

                switch (followDirection)
                {
                    case ConvolutionDirection.Both:
                        var newVertex = new Vertex2D(Vector2IP.Add2D(aVertex.Coordinates, nextBEdge.ToPoint.Coordinates), visitedHash.Count, currentLoopIndex);
                        visitedHash.Add((aVertex, bVertex, false), newVertex);
                        visitedHash.Add((aVertex, bVertex, true), newVertex);
                        result.Add(newVertex);
                        var prevACrossNextA = aVertex.EndLine.Vector3D.CrossSign(nextAEdge.Vector3D);
                        knownWrongPoints.Add(prevACrossNextA < 0);
                        bVertex = nextBEdge.ToPoint;
                        newVertex = new Vertex2D(Vector2IP.Add2D(nextAEdge.ToPoint.Coordinates, bVertex.Coordinates), visitedHash.Count, currentLoopIndex);
                        //visitedHash.Add((aVertex, bVertex, true), newVertex);
                        result.Add(newVertex);
                        var prevBCrossNextB = bVertex.EndLine.Vector3D.CrossSign(nextBEdge.Vector3D);
                        knownWrongPoints.Add(prevBCrossNextB < 0);
                        aVertex = nextAEdge.ToPoint;
                        break;
                    case ConvolutionDirection.AButQueueUpB:
                        startsQueue.Push((aVertex, nextBEdge.ToPoint));
                        goto case ConvolutionDirection.A;
                    case ConvolutionDirection.A:
                        newVertex = new Vertex2D(Vector2IP.Add2D(nextAEdge.ToPoint.Coordinates, bVertex.Coordinates), visitedHash.Count, currentLoopIndex);
                        visitedHash.Add((aVertex, bVertex, true), newVertex);
                        result.Add(newVertex);
                        prevBCrossNextB = bVertex.EndLine.Vector3D.CrossSign(nextBEdge.Vector3D);
                        knownWrongPoints.Add(prevBCrossNextB < 0);
                        aVertex = nextAEdge.ToPoint;
                        break;
                    case ConvolutionDirection.B:
                        newVertex = new Vertex2D(Vector2IP.Add2D(aVertex.Coordinates, nextBEdge.ToPoint.Coordinates), visitedHash.Count, currentLoopIndex);
                        visitedHash.Add((aVertex, bVertex, false), newVertex);
                        result.Add(newVertex);
                        prevACrossNextA = aVertex.EndLine.Vector3D.CrossSign(nextAEdge.Vector3D);
                        knownWrongPoints.Add(prevACrossNextA < 0);
                        bVertex = nextBEdge.ToPoint;
                        break;
                    default:

                        //Presenter.ShowAndHang(result.Select(v => v.Coordinates), closeShape: false);
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
                                    foreach (((Vertex2D, Vertex2D, bool) key, Vertex2D value) in visitedHash)
                                    {
                                        if (value == result[i - 1])
                                        {
                                            aVertex = key.Item1;
                                            bVertex = key.Item2;
                                            break;
                                        }
                                    }

                                    ConvolutionCycle(a, b, aVertex, bVertex, visitedHash, aEdgeAngles, bEdgeAngles, startsQueue,
                                        results, allKnownWrongPoints, result.Take(i).ToList(), currentLoopIndex,
                                        knownWrongPoints.Take(i).ToList());
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

        private static ConvolutionDirection GetFollowDirection(Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D> visitedHash, Dictionary<PolygonEdge, double> aEdgeAngles,
            Dictionary<PolygonEdge, double> bEdgeAngles, Vertex2D aVertex, Vertex2D bVertex, PolygonEdge nextAEdge, PolygonEdge nextBEdge,
            out Vertex2D aExistingVertex, out Vertex2D bExistingVertex)
        {
            var aAngle = aEdgeAngles[nextAEdge];
            var aPrevAngle = aEdgeAngles[aVertex.EndLine];
            var bAngle = bEdgeAngles[nextBEdge];
            var bPrevAngle = bEdgeAngles[bVertex.EndLine];
            return GetFollowDirection(visitedHash, aVertex, bVertex, out aExistingVertex, out bExistingVertex, aAngle, aPrevAngle,
                bAngle, bPrevAngle);
        }

        private static ConvolutionDirection GetFollowDirection(Dictionary<(Vertex2D, Vertex2D, bool), Vertex2D> visitedHash, Vertex2D aVertex,
            Vertex2D bVertex, out Vertex2D aExistingVertex, out Vertex2D bExistingVertex, double aAngle, double aPrevAngle, double bAngle,
            double bPrevAngle)
        {
            var sameAngle = aAngle.IsPracticallySame(bAngle);
            var canFollowA = firstAngleIsBetweenOthersCCW(aAngle, bPrevAngle, bAngle);
            var aAlreadyVisited = visitedHash.TryGetValue((aVertex, bVertex, true), out aExistingVertex);
            var canFollowB = firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle);
            var bAlreadyVisited = visitedHash.TryGetValue((aVertex, bVertex, false), out bExistingVertex);
            if (sameAngle && !aAlreadyVisited && !bAlreadyVisited)
                return ConvolutionDirection.Both;
            else if (!aAlreadyVisited && canFollowA)
            {
                if (!bAlreadyVisited && canFollowB)
                    return ConvolutionDirection.AButQueueUpB;
                return ConvolutionDirection.A;
            }
            else if (!bAlreadyVisited && canFollowB)
                return ConvolutionDirection.B;
            return ConvolutionDirection.None;
        }

        private static Vertex2D WhichExistingToUse(List<Polygon> results, Vertex2D aExistingVertex, Vertex2D bExistingVertex)
        {
            if (aExistingVertex == null) return bExistingVertex;
            if (bExistingVertex == null) return aExistingVertex;
            if (results[aExistingVertex.LoopID].IsClosed) return bExistingVertex;
            if (results[bExistingVertex.LoopID].IsClosed) return aExistingVertex;
            return null;
        }

        private static bool firstAngleIsBetweenOthersCCW(double aAngle, double bPrevAngle, double bAngle)
        {
            if (bPrevAngle <= aAngle && aAngle <= bAngle) return true;
            if (bPrevAngle > bAngle)
                if (aAngle >= bPrevAngle || aAngle <= bAngle) return true;
            return false;
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

    }
    internal enum ConvolutionDirection
    {
        None,
        A,
        B,
        Both,
        AButQueueUpB
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
