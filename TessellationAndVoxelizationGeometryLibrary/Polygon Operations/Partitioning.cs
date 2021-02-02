// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    public enum MonotonicityChange { X, Y, Both, Neither, SameAsPrevious }

    public readonly struct MonotoneBox
    {
        public readonly double Bottom;
        public readonly MonotonicityChange HiChange;
        public readonly double Left;
        public readonly MonotonicityChange LowChange;
        public readonly double Right;
        public readonly double Top;
        public readonly Vertex2D Vertex1;
        public readonly Vertex2D Vertex2;
        public readonly bool XInPositiveMonotonicity;
        public readonly bool YInPositiveMonotonicity;

        public MonotoneBox(Vertex2D vertex1, Vertex2D vertex2, MonotonicityChange lowMonoChange,
            MonotonicityChange hiMonoChange, bool xInPositiveMonotonicity, bool yInPositiveMonotonicity) : this()
        {
            this.Vertex1 = vertex1;
            this.Vertex2 = vertex2;
            this.LowChange = lowMonoChange;
            this.HiChange = hiMonoChange;
            XInPositiveMonotonicity = xInPositiveMonotonicity;
            YInPositiveMonotonicity = yInPositiveMonotonicity;
            Left = Math.Min(vertex1.X, vertex2.X);
            Right = Math.Max(vertex1.X, vertex2.X);
            Bottom = Math.Min(vertex1.Y, vertex2.Y);
            Top = Math.Max(vertex1.Y, vertex2.Y);
        }

        public double Area()
        {
            return (Right - Left) * (Top - Bottom);
        }
    }

    public static partial class PolygonOperations
    {
        /// <summary>Gets the monotonicity change.</summary>
        /// <param name="vertex">The vertex.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>MonotonicityChange.</returns>
        /// <exception cref="ArgumentException">
        /// vertex does not conenct to polygon edges. Be sure to invoke" +
        ///           " MakePolygonEdgesIfNonExistent on parent polygon before this calling this method. - vertex
        /// </exception>
        public static MonotonicityChange GetMonotonicityChange(this Vertex2D vertex, double tolerance)
        {
            if (vertex.EndLine == null) throw new ArgumentException("vertex does not connect to polygon edges. Be sure to invoke" +
             " MakePolygonEdgesIfNonExistent on parent polygon before this calling this method.", nameof(vertex));
            var xPrev = vertex.EndLine.Vector.X;
            var yPrev = vertex.EndLine.Vector.Y;
            if (xPrev.IsNegligible() && yPrev.IsNegligible()) return MonotonicityChange.SameAsPrevious;
            double xNext, yNext;
            var neighborVertex = vertex;
            do
            {
                neighborVertex = neighborVertex.StartLine.ToPoint;
                xNext = neighborVertex.EndLine.Vector.X;
                yNext = neighborVertex.EndLine.Vector.Y;
            }
            while (xNext.IsNegligible(tolerance) && yNext.IsNegligible(tolerance));

            //at this point one or both of the x&y deltas are non-negligible.
            /**** I wish this were enough (the next 6 lines) but it leads to problems
            var xProduct = xNext * xPrev;
            var yProduct = yNext * yPrev;
            if (xProduct > 0 && yProduct > 0) return MonotonicityChange.Neither;
            if (xProduct < 0 && yProduct < 0) return MonotonicityChange.Both;
            if (xProduct > 0) return MonotonicityChange.Y;
            return MonotonicityChange.X;
            ****/

            // first check the cases where all four numbers are not negligible
            var xChangesDir = (xPrev.IsNegativeNonNegligible(tolerance) && xNext.IsPositiveNonNegligible(tolerance))
                || (xPrev.IsPositiveNonNegligible(tolerance) && xNext.IsNegativeNonNegligible(tolerance));
            var yChangesDir = (yPrev.IsNegativeNonNegligible(tolerance) && yNext.IsPositiveNonNegligible(tolerance))
                || (yPrev.IsPositiveNonNegligible(tolerance) && yNext.IsNegativeNonNegligible(tolerance));
            if (xChangesDir && yChangesDir) return MonotonicityChange.Both;
            var xSameDir = (xPrev.IsNegativeNonNegligible(tolerance) && xNext.IsNegativeNonNegligible(tolerance))
                || (xPrev.IsPositiveNonNegligible(tolerance) && xNext.IsPositiveNonNegligible(tolerance));
            if (yChangesDir && xSameDir) return MonotonicityChange.Y;
            var ySameDir = (yPrev.IsNegativeNonNegligible(tolerance) && yNext.IsNegativeNonNegligible(tolerance))
                || (yPrev.IsPositiveNonNegligible(tolerance) && yNext.IsPositiveNonNegligible(tolerance));
            if (xChangesDir && ySameDir) return MonotonicityChange.X;
            if (xSameDir && ySameDir) return MonotonicityChange.Neither;

            // if at this point then one or more values in the vectors is zero/negligible since the above booleans were
            // defined with this restriction
            if (xPrev.IsNegligible(tolerance) && xNext.IsNegligible(tolerance)) // then line is vertical
                return (Math.Sign(yPrev) == Math.Sign(yNext)) ? MonotonicityChange.Neither : MonotonicityChange.Y;
            // eww, the latter in that return is problematic at it would be a knife edge. but that's not this functions job to police
            if (yPrev.IsNegligible(tolerance) && yNext.IsNegligible(tolerance)) // then line is horizontal
                return (Math.Sign(xPrev) == Math.Sign(xNext)) ? MonotonicityChange.Neither : MonotonicityChange.X;

            // at this point, we've checked that 1) no vector is zero, 2) all are nonnegligible,
            // 3) either both x's or both y's are negligible.
            neighborVertex = vertex;
            if (xPrev.IsNegligible(tolerance)) //we know that yPrev != 0 (given first condition) and we know xNext != 0 (given
                                      // the condition before the last). it's possible that yNext is zero, but it doesn't affect the approach
            {
                do
                {
                    neighborVertex = neighborVertex.EndLine.FromPoint;
                    xPrev = neighborVertex.EndLine.Vector.X;
                } while (xPrev.IsNegligible(tolerance));
                xChangesDir = Math.Sign(xPrev) != Math.Sign(xNext);
            }
            else if (xNext.IsNegligible(tolerance))
            {
                do
                {
                    neighborVertex = neighborVertex.StartLine.ToPoint;
                    xNext = neighborVertex.EndLine.Vector.X;
                } while (xNext.IsNegligible(tolerance));
                xChangesDir = Math.Sign(xPrev) != Math.Sign(xNext);
            }
            neighborVertex = vertex;
            if (yPrev.IsNegligible(tolerance))
            {
                do
                {
                    neighborVertex = neighborVertex.EndLine.FromPoint;
                    yPrev = neighborVertex.EndLine.Vector.Y;
                } while (yPrev.IsNegligible(tolerance));
                yChangesDir = (Math.Sign(yPrev) != Math.Sign(yNext));
            }
            else if (yNext.IsNegligible(tolerance))
            {
                do
                {
                    neighborVertex = neighborVertex.StartLine.ToPoint;
                    yNext = neighborVertex.EndLine.Vector.Y;
                } while (yNext.IsNegligible(tolerance));
                yChangesDir = Math.Sign(yPrev) != Math.Sign(yNext);
            }
            if (xChangesDir && yChangesDir) return MonotonicityChange.Both;
            if (xChangesDir) return MonotonicityChange.X;
            if (yChangesDir) return MonotonicityChange.Y;
            return MonotonicityChange.Neither;
        }

        /// <summary>
        /// Partitions the into monotone boxes.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="divideAt">The divide at.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.TwoDimensional.MonotoneBox&gt;.</returns>
        /// <exception cref="MonotoneBox">polygon.Vertices[0], polygon.Vertices[1], MonotonicityChange.Both, MonotonicityChange.Both, true, true</exception>
        public static IEnumerable<MonotoneBox> PartitionIntoMonotoneBoxes(this Polygon polygon, MonotonicityChange divideAt = MonotonicityChange.Both)
        {
            polygon.MakePolygonEdgesIfNonExistent();
            var numPoints = polygon.Vertices.Count;
            if (numPoints <= 1)
            {
                yield return
                    new MonotoneBox(polygon.Vertices[0], polygon.Vertices[0], MonotonicityChange.Both,
                        MonotonicityChange.Both, true, true);
                yield break;
            }
            if (numPoints <= 2)
            {
                yield return
                    new MonotoneBox(polygon.Vertices[0], polygon.Vertices[1], MonotonicityChange.Both,
                        MonotonicityChange.Both, true, true);
                yield break;
            }

            var initBoxIndex = -1;
            var initBoxMonoChange = MonotonicityChange.Neither;
            Vertex2D beginBoxVertex = null;
            var beginBoxMonoChange = MonotonicityChange.Neither;
            var i = 0;
            var vertexIndex = 0;
            while ((vertexIndex = i++ % numPoints) != initBoxIndex)
            {
                var vertex = polygon.Vertices[vertexIndex];
                var monoChange = GetMonotonicityChange(vertex, polygon.Tolerance);
                if (monoChange == MonotonicityChange.SameAsPrevious || monoChange == MonotonicityChange.Neither) continue;
                if (monoChange == MonotonicityChange.Both ||
                    (monoChange == MonotonicityChange.X && (divideAt == MonotonicityChange.X || divideAt == MonotonicityChange.Both)) ||
                    (monoChange == MonotonicityChange.Y && (divideAt == MonotonicityChange.Y || divideAt == MonotonicityChange.Both)))
                {
                    if (initBoxIndex < 0)
                    {
                        initBoxIndex = vertexIndex;
                        beginBoxVertex = vertex;
                        initBoxMonoChange = beginBoxMonoChange = monoChange;
                    }
                    else
                    {
                        yield return new MonotoneBox(beginBoxVertex, vertex, beginBoxMonoChange, monoChange,
                            vertex.X >= beginBoxVertex.X, vertex.Y >= beginBoxVertex.Y);
                        beginBoxVertex = vertex;
                        beginBoxMonoChange = monoChange;
                    }
                }
            }
            var lastVertex = polygon.Vertices[initBoxIndex];
            yield return new MonotoneBox(beginBoxVertex, lastVertex,
                beginBoxMonoChange, initBoxMonoChange,
                lastVertex.X - beginBoxVertex.X >= 0,
                lastVertex.Y - beginBoxVertex.Y >= 0);
        }
    }
}