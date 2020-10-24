// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
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
            var xProduct = xNext * xPrev;
            var yProduct = yNext * yPrev;
            if (xProduct > 0 && yProduct > 0) return MonotonicityChange.Neither;
            if (xProduct < 0 && yProduct < 0) return MonotonicityChange.Both;
            if (xProduct > 0) return MonotonicityChange.Y;
            return MonotonicityChange.X;
        }

        public static IEnumerable<MonotoneBox> PartitionIntoMonotoneBoxes(this Polygon polygon)
        {
            var numPoints = polygon.Vertices.Count;
            if (numPoints <= 1)
                yield break;
            if (numPoints <= 2)
                yield return
                    new MonotoneBox(polygon.Vertices[0], polygon.Vertices[1], MonotonicityChange.Both,
                        MonotonicityChange.Both, true, true);

            var initBoxIndex = -1;
            var initBoxMonoChange = MonotonicityChange.Neither;
            Vertex2D beginBoxVertex = null;
            var beginBoxMonoChange = MonotonicityChange.Neither;

            var i = 0;
            while (i % numPoints != initBoxIndex)
            {
                var vertex = polygon.Vertices[i];
                var monoChange = GetMonotonicityChange(vertex);
                if (monoChange == MonotonicityChange.SameAsNeighbor)
                    throw new ArgumentException("Duplicate vertices in polygon provided to PartitionIntoMonotoneBoxes",
                        nameof(polygon));
                if (monoChange != MonotonicityChange.Neither)
                {
                    if (initBoxIndex < 0)
                    {
                        initBoxIndex = i;
                        beginBoxVertex = vertex;
                        initBoxMonoChange = beginBoxMonoChange = monoChange;
                    }
                    if (beginBoxVertex == null)
                    {
                        beginBoxVertex = vertex;
                        beginBoxMonoChange = monoChange;
                    }
                    else
                    {
                        yield return new MonotoneBox(beginBoxVertex, vertex, beginBoxMonoChange, monoChange,
                            vertex.X - beginBoxVertex.X >= 0,
                            vertex.Y - beginBoxVertex.Y >= 0);
                        beginBoxVertex = null;
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
    }
}