using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    public enum MonotonicityChange { X, Y, Both, Neither }
    public readonly struct MonotoneBox
    {
        public readonly Vertex2D vertex1;
        public readonly Vertex2D vertex2;
        public readonly double Left;
        public readonly double Right;
        public readonly double Bottom;
        public readonly double Top;
        public readonly MonotonicityChange LowChange;
        public readonly MonotonicityChange HiChange;
        public readonly bool XInPositiveMonotonicity;
        public readonly bool YInPositiveMonotonicity;

        public MonotoneBox(Vertex2D vertex1, Vertex2D vertex2, MonotonicityChange lowMonoChange,
            MonotonicityChange hiMonoChange, bool xInPositiveMonotonicity, bool yInPositiveMonotonicity) : this()
        {
            this.vertex1 = vertex1;
            this.vertex2 = vertex2;
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
        public static IEnumerable<MonotoneBox> PartitionIntoMonotoneBoxes(this Polygon polygon)
        {
            var numPoints = polygon.Vertices.Count;
            if (numPoints <= 1)
                yield break; ;
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

  
        static MonotonicityChange GetMonotonicityChange(Vertex2D vertex)
        {
            var prevVector = vertex.EndLine.Vector;
            var nextVector = vertex.StartLine.Vector;
            if ((prevVector.X == 0 && prevVector.Y == 0) || (nextVector.X == 0 && nextVector.Y == 0))
                return MonotonicityChange.Neither;
            var xChange = (prevVector.X < 0 && nextVector.X > 0) || (prevVector.X > 0 && nextVector.X < 0);
            var yChange = (prevVector.Y < 0 && nextVector.Y > 0) || (prevVector.Y > 0 && nextVector.Y < 0);
            if (xChange && yChange) return MonotonicityChange.Both;
            var xSame = (prevVector.X < 0 && nextVector.X < 0) || (prevVector.X > 0 && nextVector.X > 0);
            if (yChange && xSame) return MonotonicityChange.Y;
            var ySame = (prevVector.Y < 0 && nextVector.Y < 0) || (prevVector.Y > 0 && nextVector.Y > 0);
            if (xChange && ySame) return MonotonicityChange.X;
            if (xSame && ySame) return MonotonicityChange.Neither;
            // if at this point then one or more values in the vectors is zero since the above booleans were defined with
            // strict inequality
            if (prevVector.X == 0)
                return yChange ? MonotonicityChange.Y : MonotonicityChange.Neither;
            if (prevVector.Y == 0)
                return xChange ? MonotonicityChange.X : MonotonicityChange.Neither;
            if (nextVector.X == 0)
            {
                var aheadLine = vertex.StartLine.ToPoint.StartLine;
                while (aheadLine.Vector.X == 0)
                    aheadLine = aheadLine.ToPoint.StartLine;
                if ((aheadLine.Vector.X > 0 && prevVector.X < 0) || (aheadLine.Vector.X < 0 && prevVector.X > 0))
                    return yChange ? MonotonicityChange.Both : MonotonicityChange.X;
                return yChange ? MonotonicityChange.Y : MonotonicityChange.Neither;
            }
            //if (nextVector.Y == 0)  //this must be true given the above conditions. So for simplicity of code and
            // speed, we simply comment it out
            {
                var aheadLine = vertex.StartLine.ToPoint.StartLine;
                while (aheadLine.Vector.Y == 0)
                    aheadLine = aheadLine.ToPoint.StartLine;
                if ((aheadLine.Vector.Y > 0 && prevVector.Y < 0) || (aheadLine.Vector.Y < 0 && prevVector.Y > 0))
                    return xChange ? MonotonicityChange.Both : MonotonicityChange.Y;
                return xChange ? MonotonicityChange.X : MonotonicityChange.Neither;
            }
        }
    }
}
