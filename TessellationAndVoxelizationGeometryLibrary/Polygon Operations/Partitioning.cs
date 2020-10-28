// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    public enum MonotonicityChange { X, Y, Both, Neither, SameAsNeighbor }

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
        public static MonotonicityChange GetMonotonicityChange(this Vertex2D vertex)
        {
            var xPrev = vertex.EndLine.Vector.X;
            var yPrev = vertex.EndLine.Vector.Y;
            if (xPrev.IsNegligible() && yPrev.IsNegligible()) return MonotonicityChange.SameAsNeighbor;
            var xNext = vertex.StartLine.Vector.X;
            var yNext = vertex.StartLine.Vector.Y;
            if (xNext.IsNegligible() && yNext.IsNegligible()) return MonotonicityChange.SameAsNeighbor;

            /**** I wish this were enough (the next 6 lines) but it leads to problems
            var xProduct = xNext * xPrev;
            var yProduct = yNext * yPrev;
            if (xProduct > 0 && yProduct > 0) return MonotonicityChange.Neither;
            if (xProduct < 0 && yProduct < 0) return MonotonicityChange.Both;
            if (xProduct > 0) return MonotonicityChange.Y;
            return MonotonicityChange.X;
            ****/

            // first check the cases where all four numbers are not negligible
            var xChangesDir = (xPrev.IsLessThanNonNegligible() && xNext.IsGreaterThanNonNegligible())
                || (xPrev.IsGreaterThanNonNegligible() && xNext.IsLessThanNonNegligible());
            var yChangesDir = (yPrev.IsLessThanNonNegligible() && yNext.IsGreaterThanNonNegligible())
                || (yPrev.IsGreaterThanNonNegligible() && yNext.IsLessThanNonNegligible());
            if (xChangesDir && yChangesDir) return MonotonicityChange.Both;
            var xSameDir = (xPrev.IsLessThanNonNegligible() && xNext.IsLessThanNonNegligible())
                || (xPrev.IsGreaterThanNonNegligible() && xNext.IsGreaterThanNonNegligible());
            if (yChangesDir && xSameDir) return MonotonicityChange.Y;
            var ySameDir = (yPrev.IsLessThanNonNegligible() && yNext.IsLessThanNonNegligible())
                || (yPrev.IsGreaterThanNonNegligible() && yNext.IsGreaterThanNonNegligible());
            if (xChangesDir && ySameDir) return MonotonicityChange.X;
            if (xSameDir && ySameDir) return MonotonicityChange.Neither;

            // if at this point then one or more values in the vectors is zero/negligible) since the above booleans were
            // defined with this restriction
            if (xPrev.IsNegligible() && xNext.IsNegligible()) // then line is vertical
                return (Math.Sign(yPrev) == Math.Sign(yNext)) ? MonotonicityChange.Neither : MonotonicityChange.Y;
            // eww, the latter in that return is problematic at it would be a knife edge. but that's not this functions job to police
            if (yPrev.IsNegligible() && yNext.IsNegligible()) // then line is horizontal
                return (Math.Sign(xPrev) == Math.Sign(xNext)) ? MonotonicityChange.Neither : MonotonicityChange.X;

            // at this point, we've checked that 1) no vector is zero (return SameAsNeighbor), 2) all are nonnegligible,
            // 3) either both x's or both y's are negligible.
            // the last case is tricky. if one is zero...here we will rely on the fact that we are sorting first by X,
            // and then by Y. We can imagine an infinitesimal CW tilt in the polygon
            if (xPrev.IsNegligible()) //we know that yPrev != 0 (given first condition) and we know xNext != 0 (given
                // the condition before the last). it's possible that yNext is zero, but it doesn't affect the verdict
                xChangesDir = (Math.Sign(yPrev) != Math.Sign(xNext)); // recall infinitesimal tilt. if going up and turning
            // right (xNext is '+') then that's not a change in X or if going down and turning in the negative x dir.
            // if going up then x-negative (like "end" point in x-Monotone polygon) or going down and turn positive
            // (like "start" point in x-Monotone polygon).
            if (xNext.IsNegligible())
                xChangesDir = Math.Sign(xPrev) != Math.Sign(yNext);
            if (yPrev.IsNegligible())
                yChangesDir = Math.Sign(xPrev) == Math.Sign(yNext);
            if (yNext.IsNegligible())
                yChangesDir = (Math.Sign(yPrev) == Math.Sign(xNext));
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
            while (i % numPoints != initBoxIndex)
            {
                var vertex = polygon.Vertices[i % numPoints];
                var monoChange = GetMonotonicityChange(vertex);
                if (monoChange == MonotonicityChange.SameAsNeighbor)
                    throw new ArgumentException("Duplicate vertices in polygon provided to PartitionIntoMonotoneBoxes",
                        nameof(polygon));
                if (monoChange == MonotonicityChange.Both ||
                    (monoChange == MonotonicityChange.X && (divideAt == MonotonicityChange.X || divideAt == MonotonicityChange.Both)) ||
                    (monoChange == MonotonicityChange.Y && (divideAt == MonotonicityChange.Y || divideAt == MonotonicityChange.Both)))
                {
                    if (initBoxIndex < 0)
                    {
                        initBoxIndex = i;
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
                i++;
            }
            var lastVertex = polygon.Vertices[initBoxIndex];
            yield return new MonotoneBox(beginBoxVertex, lastVertex,
                beginBoxMonoChange, initBoxMonoChange,
                lastVertex.X - beginBoxVertex.X >= 0,
                lastVertex.Y - beginBoxVertex.Y >= 0);
        }

        /// <summary>
        /// Orders the vertices of the polygon in the x-direction.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>Vertex2D[].</returns>
        public static Vertex2D[] SortVerticesByXValue(this Polygon polygon)
        {
            var xStrands = new List<Vertex2D[]>();
            foreach (var monoBox in polygon.PartitionIntoMonotoneBoxes(MonotonicityChange.X))
            {
                var newStrand = new List<Vertex2D>();
                var vertex = monoBox.Vertex1;
                while (vertex != monoBox.Vertex2)
                {
                    newStrand.Add(vertex);
                    vertex = vertex.StartLine.ToPoint;
                }
                if (!monoBox.XInPositiveMonotonicity) newStrand.Reverse();
                xStrands.Add(newStrand.ToArray());
            }
            var n = polygon.Vertices.Count;
            var sortedVertices = new Vertex2D[n];
            var i = 0;
            foreach (var vertex in CombineSortedVerticesIntoOneCollection(xStrands))
                sortedVertices[i++] = vertex;
            return sortedVertices;
        }
    }
}