﻿// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Partitioning.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Enum MonotonicityChange
    /// </summary>
    public enum MonotonicityChange { X, Y, Both, Neither, SameAsPrevious }

    /// <summary>
    /// Struct MonotoneBox
    /// </summary>
    public readonly struct MonotoneBox
    {
        /// <summary>
        /// The rectangle
        /// </summary>
        public readonly AxisAlignedRectangle Rectangle;
        /// <summary>
        /// The hi change
        /// </summary>
        public readonly MonotonicityChange HiChange;
        /// <summary>
        /// The low change
        /// </summary>
        public readonly MonotonicityChange LowChange;
        /// <summary>
        /// The vertex1
        /// </summary>
        public readonly Vertex2D Vertex1;
        /// <summary>
        /// The vertex2
        /// </summary>
        public readonly Vertex2D Vertex2;
        /// <summary>
        /// The x in positive monotonicity
        /// </summary>
        public readonly bool XInPositiveMonotonicity;
        /// <summary>
        /// The y in positive monotonicity
        /// </summary>
        public readonly bool YInPositiveMonotonicity;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonotoneBox"/> struct.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="lowMonoChange">The low mono change.</param>
        /// <param name="hiMonoChange">The hi mono change.</param>
        /// <param name="xInPositiveMonotonicity">if set to <c>true</c> [x in positive monotonicity].</param>
        /// <param name="yInPositiveMonotonicity">if set to <c>true</c> [y in positive monotonicity].</param>
        public MonotoneBox(Vertex2D vertex1, Vertex2D vertex2, MonotonicityChange lowMonoChange,
            MonotonicityChange hiMonoChange, bool xInPositiveMonotonicity, bool yInPositiveMonotonicity) : this()
        {
            this.Vertex1 = vertex1;
            this.Vertex2 = vertex2;
            this.LowChange = lowMonoChange;
            this.HiChange = hiMonoChange;
            XInPositiveMonotonicity = xInPositiveMonotonicity;
            YInPositiveMonotonicity = yInPositiveMonotonicity;
            Rectangle = new AxisAlignedRectangle(vertex1, vertex2);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MonotoneBox"/> struct.
        /// </summary>
        /// <param name="vertex1">The vertex1.</param>
        /// <param name="vertex2">The vertex2.</param>
        /// <param name="lowMonoChange">The low mono change.</param>
        /// <param name="hiMonoChange">The hi mono change.</param>
        /// <param name="xInPositiveMonotonicity">if set to <c>true</c> [x in positive monotonicity].</param>
        /// <param name="yInPositiveMonotonicity">if set to <c>true</c> [y in positive monotonicity].</param>
        /// <param name="rect">The rect.</param>
        public MonotoneBox(Vertex2D vertex1, Vertex2D vertex2, MonotonicityChange lowMonoChange,
            MonotonicityChange hiMonoChange, bool xInPositiveMonotonicity, bool yInPositiveMonotonicity, AxisAlignedRectangle rect)
            : this(vertex1, vertex2, lowMonoChange, hiMonoChange, xInPositiveMonotonicity, yInPositiveMonotonicity)
        {
            Rectangle = rect;
        }
    }

    /// <summary>
    /// Class PolygonOperations.
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Gets the monotonicity change.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        /// <returns>MonotonicityChange.</returns>
        /// <exception cref="System.ArgumentException">vertex does not connect to polygon edges. Be sure to invoke" +
        ///              " MakePolygonEdgesIfNonExistent on parent polygon before this calling this method. - vertex</exception>
        public static MonotonicityChange GetMonotonicityChange(this Vertex2D vertex)
        {
            if (vertex.EndLine == null) throw new ArgumentException("vertex does not connect to polygon edges. Be sure to invoke" +
             " MakePolygonEdgesIfNonExistent on parent polygon before this calling this method.", nameof(vertex));
            var xPrev = vertex.EndLine.Vector.X;
            var yPrev = vertex.EndLine.Vector.Y;
            if (xPrev == 0 && yPrev == 0) return MonotonicityChange.SameAsPrevious;
            double xNext, yNext;
            var neighborVertex = vertex;
            do
            {
                neighborVertex = neighborVertex.StartLine.ToPoint;
                xNext = neighborVertex.EndLine.Vector.X;
                yNext = neighborVertex.EndLine.Vector.Y;
            }
            while (xNext == 0 && yNext == 0);

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
            var xChangesDir = (xPrev < 0 && xNext > 0) || (xPrev > 0 && xNext < 0);
            var yChangesDir = (yPrev < 0 && yNext > 0) || (yPrev > 0 && yNext < 0);
            if (xChangesDir && yChangesDir) return MonotonicityChange.Both;
            var xSameDir = (xPrev < 0 && xNext < 0)
                || (xPrev > 0 && xNext > 0);
            if (yChangesDir && xSameDir) return MonotonicityChange.Y;
            var ySameDir = (yPrev < 0 && yNext < 0)
                || (yPrev > 0 && yNext > 0);
            if (xChangesDir && ySameDir) return MonotonicityChange.X;
            if (xSameDir && ySameDir) return MonotonicityChange.Neither;

            // if at this point then one or more values in the vectors is zero/negligible since the above booleans were
            // defined with this restriction
            if (xPrev == 0 && xNext == 0) // then line is vertical
                return (Math.Sign(yPrev) == Math.Sign(yNext)) ? MonotonicityChange.Neither : MonotonicityChange.Y;
            // eww, the latter in that return is problematic at it would be a knife edge. but that's not this functions job to police
            if (yPrev == 0 && yNext == 0) // then line is horizontal
                return (Math.Sign(xPrev) == Math.Sign(xNext)) ? MonotonicityChange.Neither : MonotonicityChange.X;

            // at this point, we've checked that 1) no vector is zero, 2) all are nonnegligible,
            // 3) either both x's or both y's are negligible.
            neighborVertex = vertex;
            if (xPrev == 0) //we know that yPrev != 0 (given first condition) and we know xNext != 0 (given
                            // the condition before the last). it's possible that yNext is zero, but it doesn't affect the approach
            {
                do
                {
                    neighborVertex = neighborVertex.EndLine.FromPoint;
                    xPrev = neighborVertex.EndLine.Vector.X;
                } while (xPrev == 0);
                xChangesDir = Math.Sign(xPrev) != Math.Sign(xNext);
            }
            else if (xNext == 0)
            {
                do
                {
                    neighborVertex = neighborVertex.StartLine.ToPoint;
                    xNext = neighborVertex.EndLine.Vector.X;
                } while (xNext == 0);
                xChangesDir = Math.Sign(xPrev) != Math.Sign(xNext);
            }
            neighborVertex = vertex;
            if (yPrev == 0)
            {
                do
                {
                    neighborVertex = neighborVertex.EndLine.FromPoint;
                    yPrev = neighborVertex.EndLine.Vector.Y;
                } while (yPrev == 0);
                yChangesDir = (Math.Sign(yPrev) != Math.Sign(yNext));
            }
            else if (yNext == 0)
            {
                do
                {
                    neighborVertex = neighborVertex.StartLine.ToPoint;
                    yNext = neighborVertex.EndLine.Vector.Y;
                } while (yNext == 0);
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
                var monoChange = GetMonotonicityChange(vertex);
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