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
        /// The Minkowski sum of the two polygons. This only functions on the outermost polygon (no holes).
        /// However, the operation does work on negative polygons, so the result can be fused totheger but this
        /// is left for the user's code due to ambiguities that may arise.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static List<Polygon> MinkowskiSum(this Polygon a, Polygon b)
        {
            var aHasHoles = a.InnerPolygons != null && a.InnerPolygons.Length > 0;
            var bHasHoles = b.InnerPolygons != null && b.InnerPolygons.Length > 0;
            var aCanBeInB = bHasHoles && ((a.MaxX - a.MinX) < (b.MaxX - b.MinX) &&
                (a.MaxY - a.MinY) < (b.MaxY - b.MinY));
            var bCanBeInA = !aCanBeInB && aHasHoles && ((a.MaxX - a.MinX) > (b.MaxX - b.MinX) &&
                (a.MaxY - a.MinY) > (b.MaxY - b.MinY));

            var result = new List<Polygon>();

            // first work on outer sums (not holes)
            if (a.IsConvex && b.IsConvex)
                result.Add(MinkowskiSumConvex(a, b));
            if (a.IsConvex)
                result.AddRange(MinkowskiSumConcaveConvex(a, b));
            else if (b.IsConvex)
                result.AddRange(MinkowskiSumConcaveConvex(b, a));
            else
            {
                var segments = BuildReducedConvolutionSegments(a, b);
                Global.Presenter2D.ShowAndHang(segments.Select(s => new[] { s.from, s.to }));
                result.AddRange(BuildCyclesFromDirectedSegments(segments));
            }
            if (aCanBeInB)
                PutHolesInProperOuter(a, b, result);
            else if (bCanBeInA)
                PutHolesInProperOuter(b, a, result);
            
            return result;
        }

        private static void PutHolesInProperOuter(Polygon a, Polygon b, List<Polygon> result)
        {
            foreach (var hole in b.InnerPolygons)
            {
                if ((a.MaxX - a.MinX) < (hole.MaxX - hole.MinX) &&
                    (a.MaxY - a.MinY) < (hole.MaxY - hole.MinY))
                {
                    if (a.IsConvex)
                        result.AddRange(MinkowskiSumConcaveConvex(a, hole));
                    else
                    {
                        var segments = BuildReducedConvolutionSegments(a, hole);
                        Polygon outer = null;
                        foreach (var loopFromHole in BuildCyclesFromDirectedSegments(segments))

                        {
                            if (outer == null)
                                outer = result.First(o => o.IsNonIntersectingPolygonInside(true, loopFromHole, out _, 1e-5).GetValueOrDefault(false));
                            outer.AddInnerPolygon(loopFromHole);
                        }
                    }
                }
            }
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
                result.Add(new Vertex2D(aVertex.Coordinates + bVertex.Coordinates, vertNum++, 0));
                var cross = aVertex.StartLine.Vector
                    // will this always be correct? I'm worried that angle could be greater than 180, and then a
                    // false result would be returned. ...although, I tried to come up with a case to break it
                    // and couldn't I guess because you can't have an angle greater than 180 on convex shapes
                    .Cross(bVertex.StartLine.Vector);
                if (cross >= 0 && !aCompleted)
                    aVertex = aVertex.StartLine.ToPoint;
                if (cross <= 0 && !bCompleted)
                    bVertex = bVertex.StartLine.ToPoint;
                aCompleted = aVertex == aStartVertex;
                bCompleted = bVertex == bStartVertex;
            } while (!aCompleted || !bCompleted);
            return new Polygon(result);
        }

        /// <summary>
        /// Finds the vertex2D with the minimum y-value. If there is a tie in y-values, 
        /// this method returns the minimum x-value.
        /// </summary>
        /// <param name="vertices"></param>
        /// <returns></returns>
        private static Vertex2D FindMinY(List<Vertex2D> vertices)
        {
            Vertex2D minVertex = null;
            var minX = double.MaxValue; // we will keep track of the minX when there
                                        // are multile vertices at the min y value.
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
            var aEdgeAngles = a.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector.X, e.Vector.Y));
            var bEdgeAngles = b.Edges.ToDictionary(e => e, e => Global.Pseudoangle(e.Vector.X, e.Vector.Y));

            var prevAEdge = aStartEdge;
            var prevBEdge = bStartEdge;
            var result = new List<Vector2> { prevAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates };
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
                    result.Add(nextAEdge.ToPoint.Coordinates + prevBEdge.ToPoint.Coordinates);
                    var prevBCrossNextB = prevBEdge.Vector.Cross(nextBEdge.Vector);
                    knownWrongPoints.Add(prevBCrossNextB < 0);
                    prevAEdge = nextAEdge;
                    nextAEdge = nextAEdge.ToPoint.StartLine;
                }
                if (firstAngleIsBetweenOthersCCW(bAngle, aPrevAngle, aAngle))
                {
                    result.Add(nextBEdge.ToPoint.Coordinates + prevAEdge.ToPoint.Coordinates);
                    var prevACrossNextA = prevAEdge.Vector.Cross(nextAEdge.Vector);
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


        private static bool firstAngleIsBetweenOthersCCW(double aAngle, double bPrevAngle, double bAngle)
        {
            if (bPrevAngle <= aAngle && aAngle <= bAngle) return true;
            if (bPrevAngle > bAngle)
                if (aAngle >= bPrevAngle || aAngle <= bAngle) return true;
            return false;
        }


        /// <summary>
        /// Computes the Minkowski sum of two polygons (each may have holes) using a C# port of the
        /// CGAL reduced convolution method (E. Behar & J-M. Lien, IROS 2011; A. Baram 2013).
        /// Notes / Simplifications:
        /// 1. The original CGAL implementation constructs an arrangement-with-history of the reduced
        ///    convolution segments to robustly extract outer boundary and holes. Here we:
        ///       a. Build the reduced convolution segments for every required loop pairing
        ///          (outerA with outerB, outerA with holeB, holeA with outerB – never hole-hole).
        ///       b. Stitch directed segments into maximal simple cycles (greedy walk). Open chains
        ///          are discarded.
        ///       c. Convert each closed chain into a Polygon and finally apply a polygon union to
        ///          merge overlapping cycles (leveraging existing TVGL + Clipper utilities).
        /// 2. This is a pragmatic translation intended to provide a fast, robust result using the
        ///    existing polygon kernel in TVGL. It may generate extra intermediate cycles which are
        ///    removed/merged by the union operation.
        /// 3. Holes are internally re-oriented CCW (positive area) for the directional tests, then
        ///    the final union establishes correct hole orientation.
        /// </summary>

        #region Reduced Convolution Core


        /// <summary>
        /// Builds reduced convolution directed segments for a pair of simple loops (both assumed CCW).
        /// </summary>
        private static List<(Vector2 from, Vector2 to)> BuildReducedConvolutionSegments(Polygon p1, Polygon p2)
        {
            var outSegments = new List<(Vector2 from, Vector2 to)>();
            var v1 = p1.Path;
            var v2 = p2.Path;
            var n1 = p1.Vertices.Count;
            var n2 = p2.Vertices.Count;
            // Directions (edge vectors) for each vertex: edge starting at vertex i
            var dir1 = GetEdgeDirections(v1);
            var dir2 = GetEdgeDirections(v2);

            var visited = new HashSet<(int i, int j)>();
            var queue = new Queue<(int i, int j)>();
            for (int i = n1 - 1; i >= 0; i--)
                queue.Enqueue((i, 0));

            while (queue.Count > 0)
            {
                var (i1, i2) = queue.Dequeue();
                if (!visited.Add((i1, i2))) continue;

                int next_i1 = (i1 + 1) % n1;
                int prev_i1 = (i1 - 1 + n1) % n1;
                int next_i2 = (i2 + 1) % n2;
                int prev_i2 = (i2 - 1 + n2) % n2;

                // Two possible steps: advance in polygon 1 OR in polygon 2
                for (int stepInP1 = 0; stepInP1 <= 1; stepInP1++)
                {
                    int new_i1, new_i2;
                    bool advancingP1 = stepInP1 == 1;
                    if (advancingP1) { new_i1 = next_i1; new_i2 = i2; }
                    else { new_i1 = i1; new_i2 = next_i2; }

                    bool belongsToConvolution;
                    if (advancingP1)
                    {
                        belongsToConvolution = IsCCWInBetween(dir1[i1], dir2[prev_i2], dir2[i2]) || DirectionsEqual(dir1[i1], dir2[i2]);
                    }
                    else
                    {
                        belongsToConvolution = IsCCWInBetween(dir2[i2], dir1[prev_i1], dir1[i1]) || DirectionsEqual(dir2[i2], dir1[prev_i1]);
                    }

                    if (!belongsToConvolution) continue;

                    queue.Enqueue((new_i1, new_i2));

                    // Reduced convolution keeps segments incident to convex vertices only (with respect to CCW orientation)
                    bool convex;
                    if (advancingP1)
                        convex = IsConvex(v2[prev_i2], v2[i2], v2[next_i2]);
                    else
                        convex = IsConvex(v1[prev_i1], v1[i1], v1[next_i1]);
                    if (!convex) continue;

                    var start = v1[i1] + v2[i2];
                    var end = v1[new_i1] + v2[new_i2];
                    outSegments.Add((start, end));
                }
            }
            return outSegments;
        }

        private static List<Vector2> GetEdgeDirections(IList<Vector2> verts)
        {
            int n = verts.Count; var dirs = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                var a = verts[i]; var b = verts[(i + 1) % n];
                dirs.Add(new Vector2(b.X - a.X, b.Y - a.Y));
            }
            return dirs;
        }

        private static bool IsConvex(Vector2 prev, Vector2 curr, Vector2 next)
        {
            var v1 = new Vector2(curr.X - prev.X, curr.Y - prev.Y);
            var v2 = new Vector2(next.X - curr.X, next.Y - curr.Y);
            return Vector2.Cross(v1, v2) > 0; // CCW left turn
        }

        private static bool DirectionsEqual(Vector2 d1, Vector2 d2)
        {
            var cross = Vector2.Cross(d1, d2);
            if (Math.Abs(cross) > 1e-12) return false;
            var dot = Vector2.Dot(d1, d2);
            if (dot <= 0) return false; // opposite or zero
            return Math.Abs(cross) < 1e-12;
        }

        private static bool IsCCWInBetween(Vector2 test, Vector2 from, Vector2 to)
        {
            // Compare angles in [0, 2pi)
            double a = Angle(test); double b = Angle(from); double c = Angle(to);
            if (b <= c) return b - 1e-14 <= a && a <= c + 1e-14;
            return a >= b - 1e-14 || a <= c + 1e-14; // wrapped interval
        }

        private static double Angle(Vector2 v)
        {
            var ang = Math.Atan2(v.Y, v.X);
            return ang < 0 ? ang + Math.PI * 2 : ang;
        }

        #endregion

        #region Segment Cycle Stitching

        private static IEnumerable<Polygon> BuildCyclesFromDirectedSegments(List<(Vector2 from, Vector2 to)> segments)
        {
            // Bucket outgoing edges by start point key (tolerance-based).
            var buckets = new Dictionary<PointKey, List<(Vector2 from, Vector2 to)>>();
            foreach (var s in segments)
            {
                var key = new PointKey(s.from);
                if (!buckets.TryGetValue(key, out var list))
                {
                    list = new List<(Vector2 from, Vector2 to)>();
                    buckets.Add(key, list);
                }
                list.Add(s);
            }

            var used = new HashSet<(Vector2 from, Vector2 to)>();

            foreach (var kvp in buckets.ToList())
            {
                foreach (var seg in kvp.Value)
                {
                    if (!used.Add(seg)) continue;
                    var loop = new List<Vector2> { seg.from, seg.to };
                    var startKey = new PointKey(seg.from);
                    var current = seg.to;
                    var prevDir = new Vector2(seg.to.X - seg.from.X, seg.to.Y - seg.from.Y);

                    while (true)
                    {
                        var currentKey = new PointKey(current);
                        if (!buckets.TryGetValue(currentKey, out var outgoing) || outgoing.Count == 0)
                            break; // dead end – open chain discarded

                        // Choose next segment by smallest left turn (angle) from prevDir
                        (Vector2 from, Vector2 to)? best = null;
                        double bestAngle = double.MaxValue;
                        foreach (var cand in outgoing)
                        {
                            if (used.Contains(cand)) continue;
                            var dir = new Vector2(cand.to.X - cand.from.X, cand.to.Y - cand.from.Y);
                            var angle = SignedCCWAngle(prevDir, dir);
                            if (angle < 0) angle += 2 * Math.PI; // normalize
                            if (angle < bestAngle)
                            {
                                bestAngle = angle;
                                best = cand;
                            }
                        }
                        if (best == null) break;
                        var nextSeg = best.Value;
                        used.Add(nextSeg);
                        if (startKey.Equals(new PointKey(nextSeg.to)))
                        {
                            loop.Add(nextSeg.to);
                            yield return new Polygon(loop);
                            break;
                        }
                        loop.Add(nextSeg.to);
                        prevDir = new Vector2(nextSeg.to.X - nextSeg.from.X, nextSeg.to.Y - nextSeg.from.Y);
                        current = nextSeg.to;
                        if (loop.Count > segments.Count + 5) // safety – prevent infinite loop
                            break;
                    }
                }
            }
        }

        private static double SignedCCWAngle(Vector2 from, Vector2 to)
        {
            var cross = Vector2.Cross(from, to);
            var dot = Vector2.Dot(from, to);
            return Math.Atan2(cross, dot);
        }

        private readonly struct PointKey : IEquatable<PointKey>
        {
            private readonly long x; private readonly long y;
            private const double Scale = 1e9; // quantization for hashing
            public PointKey(Vector2 v)
            {
                x = (long)Math.Round(v.X * Scale);
                y = (long)Math.Round(v.Y * Scale);
            }
            public bool Equals(PointKey other) => x == other.x && y == other.y;
            public override bool Equals(object obj) => obj is PointKey pk && Equals(pk);
            public override int GetHashCode() => System.HashCode.Combine(x, y);
        }

        #endregion

    }
}
