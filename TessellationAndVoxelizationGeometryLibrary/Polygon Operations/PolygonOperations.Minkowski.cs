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
            //if (a.IsConvex)
            //    result.AddRange(MinkowskiSumConcaveConvex(a, b));
            //else if (b.IsConvex)
            //    result.AddRange(MinkowskiSumConcaveConvex(b, a));
            else
            {
                var segments = BuildReducedConvolutionSegments(a, b);
                Global.Presenter2D.ShowAndHang(segments.Select(s => new[] { s.from, s.to }));
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
        /// Based on CGAL reduced convolution algorithm (E. Behar & J-M. Lien, IROS 2011; A. Baram 2013).
        /// </summary>
        private static List<(Vector2 from, Vector2 to)> BuildReducedConvolutionSegments(Polygon p1, Polygon p2)
        {
            var outSegments = new List<(Vector2 from, Vector2 to)>();
            var v1 = p1.Path;
            var v2 = p2.Path;
            var n1 = p1.Vertices.Count;
            var n2 = p2.Vertices.Count;
            
            if (n1 == 0 || n2 == 0) return outSegments;

            // Get edge directions for each polygon - direction of edge FROM vertex i
            var p1Directions = GetEdgeDirections(p1);
            var p2Directions = GetEdgeDirections(p2);

            var visited = new HashSet<(int i, int j)>();
            var queue = new Queue<(int i, int j)>();
            
            // Initialize queue with states from first column (i, 0) where i goes from n1-1 down to 0
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

                // Try two transitions: From (i1,i2) to (i1+1,i2) and to (i1,i2+1)
                // Step in polygon 1 (advance i1)
                TryAddReducedConvolutionSegmentCorrected(p1, p2, outSegments, p1Directions, p2Directions, queue,
                    i1, i2, next_i1, prev_i1, next_i2, prev_i2, true);

                // Step in polygon 2 (advance i2)  
                TryAddReducedConvolutionSegmentCorrected(p1, p2, outSegments, p1Directions, p2Directions, queue,
                    i1, i2, next_i1, prev_i1, next_i2, prev_i2, false);
            }
            return outSegments;
        }

        /// <summary>
        /// Get edge directions for a polygon - each direction corresponds to the edge FROM vertex i
        /// </summary>
        private static List<double> GetEdgeDirections(Polygon polygon)
        {
            var directions = new List<double>();
            var vertices = polygon.Path;
            int n = vertices.Count;
            
            for (int i = 0; i < n; i++)
            {
                int next_i = (i + 1) % n;
                var edgeVector = vertices[next_i] - vertices[i];
                directions.Add(Global.Pseudoangle(edgeVector.X, edgeVector.Y));
            }
            return directions;
        }

        private static bool TryAddReducedConvolutionSegmentCorrected(Polygon p1, Polygon p2, List<(Vector2 from, Vector2 to)> outSegments,
             List<double> p1Directions, List<double> p2Directions, Queue<(int i, int j)> queue, int i1,
            int i2, int next_i1, int prev_i1, int next_i2, int prev_i2, bool stepInP1)
        {
            int new_i1, new_i2;
            if (stepInP1) 
            { 
                new_i1 = next_i1; 
                new_i2 = i2; 
            }
            else 
            { 
                new_i1 = i1; 
                new_i2 = next_i2; 
            }

            // Check if segment direction lies counterclockwise between the vertex directions
            bool belongsToConvolution;
            if (stepInP1)
            {
                // Direction of edge from i1 should be CCW between prev and current directions at vertex i2
                belongsToConvolution = IsAngleCCWInBetween(p1Directions[i1], p2Directions[prev_i2], p2Directions[i2])
                    || DirectionsEqualCorrected(p1Directions[i1], p2Directions[i2]);
            }
            else
            {
                // Direction of edge from i2 should be CCW between prev and current directions at vertex i1  
                belongsToConvolution = IsAngleCCWInBetween(p2Directions[i2], p1Directions[prev_i1], p1Directions[i1])
                    || DirectionsEqualCorrected(p2Directions[i2], p1Directions[prev_i1]);
            }

            if (!belongsToConvolution) return false;

            queue.Enqueue((new_i1, new_i2));

            // For reduced convolution: only keep segments incident to convex vertices
            bool convex;
            if (stepInP1)
            {
                // Check convexity at vertex i2 in polygon p2
                convex = IsConvexVertex(p2.Path[prev_i2], p2.Path[i2], p2.Path[next_i2]);
            }
            else
            {
                // Check convexity at vertex i1 in polygon p1
                convex = IsConvexVertex(p1.Path[prev_i1], p1.Path[i1], p1.Path[next_i1]);
            }

            if (!convex) return false;

            var start = p1.Path[i1] + p2.Path[i2];
            var end = p1.Path[new_i1] + p2.Path[new_i2];
            outSegments.Add((start, end));
            return true;
        }

        /// <summary>
        /// Check if angle a is between angles b and c in counter-clockwise order
        /// </summary>
        private static bool IsAngleCCWInBetween(double a, double b, double c)
        {
            // Normalize angles to [0, 4) range (since pseudoangle range is [0, 4))
            var tolerance = 1e-10;
            if (DirectionsEqualCorrected(b, c)) return DirectionsEqualCorrected(a, b);
            
            // Check if a is between b and c in CCW order
            if (c > b)
            {
                return a >= b && a <= c;
            }
            else // c < b (wrapping around)
            {
                return a >= b || a <= c;
            }
        }

        /// <summary>
        /// Check if a vertex is convex (makes a left turn) assuming CCW orientation
        /// </summary>
        private static bool IsConvexVertex(Vector2 prev, Vector2 curr, Vector2 next)
        {
            var v1 = curr - prev;
            var v2 = next - curr;
            return v1.Cross(v2) > 0; // Left turn = convex for CCW polygon
        }

        private static bool DirectionsEqualCorrected(double d1, double d2)
        {
            const double tolerance = 1e-10;
            if (Math.Abs(d1 - d2) < tolerance) return true;
            // Handle wrap-around at 4.0 (since pseudoangle range is [0, 4))
            return Math.Abs(Math.Abs(d1 - d2) - 4.0) < tolerance;
        }
    }
}
