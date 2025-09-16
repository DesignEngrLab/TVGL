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
using System.Globalization;
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
                result.AddRange(segments.ArrangementUnion());
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
                        foreach (var loopFromHole in segments.ArrangementUnion())

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
            var p1Angles = p1.Edges.Select(e => Global.Pseudoangle(e.Vector.X, e.Vector.Y)).ToList();
            // unfortunately need to do this shift because edge i ends at vertex i, but the angle at the vertex
            // is with the next edge, so we need to shift the directions list down a spot and wrap around
            p1Angles.Add(p1Angles[0]);
            p1Angles.RemoveAt(0);
            var dir2 = p2.Edges.Select(e => Global.Pseudoangle(e.Vector.X, e.Vector.Y)).ToList();
            dir2.Add(dir2[0]);
            dir2.RemoveAt(0);
            //var dir2 = GetEdgeDirections(v2);

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
                var advancedP2 = TryAddReducedConvolutionSegment(p1, p2, outSegments, p1Angles, dir2, queue,
                          i1, i2, next_i1, prev_i1, next_i2, prev_i2, false);
                var advancedP1 = TryAddReducedConvolutionSegment(p1, p2, outSegments, p1Angles, dir2, queue,
                     i1, i2, next_i1, prev_i1, next_i2, prev_i2, true);

            }
            return outSegments;
        }

        private static bool TryAddReducedConvolutionSegment(Polygon p1, Polygon p2, List<(Vector2 from, Vector2 to)> outSegments,
             List<double> p1Angles, List<double> p2Angles, Queue<(int i, int j)> queue, int i1,
            int i2, int next_i1, int prev_i1, int next_i2, int prev_i2, bool advancingP1)
        {
            bool belongsToConvolution;
            if (advancingP1)
                belongsToConvolution = IsCCWInBetween(p1Angles[i1], p2Angles[prev_i2], p2Angles[i2])
                    || DirectionsEqual(p1Angles[i1], p2Angles[i2]);
            else
                belongsToConvolution = IsCCWInBetween(p2Angles[i2], p1Angles[prev_i1], p1Angles[i1])
                    || DirectionsEqual(p2Angles[i2], p1Angles[prev_i1]);

            if (!belongsToConvolution) return false;

            int new_i1, new_i2;
            if (advancingP1) { new_i1 = next_i1; new_i2 = i2; }
            else { new_i1 = i1; new_i2 = next_i2; }

            queue.Enqueue((new_i1, new_i2));

            // Reduced convolution keeps segments incident to convex vertices only (with respect to CCW orientation)
            bool convex;
            if (advancingP1)
                convex = p2.Vertices[i2].IsConvex.GetValueOrDefault(false); //IsConvex(  v2[prev_i2], v2[i2], v2[next_i2]);
            else
                convex = p1.Vertices[i1].IsConvex.GetValueOrDefault(false);  // IsConvex(v1[prev_i1], v1[i1], v1[next_i1]);
            if (!convex) return false;

            var start = p1.Path[i1] + p2.Path[i2];
            var end = p1.Path[new_i1] + p2.Path[new_i2];
            outSegments.Add((start, end));
            return true;
        }

        private static bool DirectionsEqual(double d1, double d2)
        {
            if (d1.IsPracticallySame(d2)) return true;
            return Math.Abs(d1 - d2).IsPracticallySame(4);
        }

        //private static bool IsCCWInBetween(Vector2 test, Vector2 from, Vector2 to)
        private static bool IsCCWInBetween(double query, double from, double to)
        {
            if (from <= to)
                return !query.IsLessThanNonNegligible(from)
                    && !query.IsGreaterThanNonNegligible(to);
            //from - 1e-14 <= query && query <= to + 1e-14;
            // wrapped interval
            return !query.IsLessThanNonNegligible(from)
                    || !query.IsGreaterThanNonNegligible(to);
            //return query >= from - 1e-14 || query <= to + 1e-14; // wrapped interval
        }

    }
}
