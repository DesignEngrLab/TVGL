// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Proximity.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class Develop3Dto2D
    {


        private sealed class ConeLoopData
        {
            internal ConeLoopData(List<Vector2> points2D, int loopID, bool windsPositive,
                List<(Vector3 point3D, Vector2 point2D)> sourcePointMappings)
            {
                Points2D = points2D;
                LoopID = loopID;
                WindsPositive = windsPositive;
                SourcePointMappings = sourcePointMappings;
            }

            internal List<Vector2> Points2D { get; }
            internal int LoopID { get; }
            internal bool WindsPositive { get; }
            internal List<(Vector3 point3D, Vector2 point2D)> SourcePointMappings { get; }
        }

        private readonly struct PathWindingData
        {
            internal PathWindingData(IList<Vector3> points, bool isClosed, double windingAngle,
                bool windsPositive)
            {
                Points = points;
                IsClosed = isClosed;
                WindingAngle = windingAngle;
                WindsPositive = windsPositive;
            }

            internal IList<Vector3> Points { get; }
            internal bool IsClosed { get; }
            internal double WindingAngle { get; }
            internal bool WindsPositive { get; }
        }


        private static void AddPointMappings(Dictionary<Vector3, Vertex2D> pointToVertexMap, Polygon polygon,
            IEnumerable<(Vector3 point3D, Vector2 point2D)> sourcePointMappings)
        {
            if (polygon.Vertices.Count == 0)
                return;

            // Polygon rounds its input coordinates and may collapse adjacent points at that precision. Build the
            // lookup from the actual retained vertices so callers receive references owned by the returned polygon.
            var verticesByCoordinate = new Dictionary<Vector2, Vertex2D>();
            foreach (var vertex in polygon.Vertices)
                verticesByCoordinate.TryAdd(vertex.Coordinates, vertex);

            foreach (var (point3D, point2D) in sourcePointMappings)
            {
                var roundedPoint = new Vector2(Math.Round(point2D.X, polygon.NumSigDigits),
                    Math.Round(point2D.Y, polygon.NumSigDigits));
                if (!verticesByCoordinate.TryGetValue(roundedPoint, out var vertex))
                {
                    // This fallback covers a point removed as a near-duplicate at a polygon boundary. The closest
                    // retained vertex is the representative that Polygon kept for that source coordinate.
                    vertex = polygon.Vertices.MinBy(candidate =>
                        Vector2.DistanceSquared(candidate.Coordinates, roundedPoint));
                }

                // A Dictionary cannot represent multiple polygon occurrences of an identical 3D coordinate. Keep
                // the first occurrence, which is deterministic in the caller's point-set order.
                pointToVertexMap.TryAdd(point3D, vertex);
            }
        }


        private static void ArrangeConeLoopAtSeam(List<Vector2> loop, bool shouldIncrease)
        {
            if (loop.Count < 2)
                return;

            var crossingIndex = FindConeSectorWrap(loop, out var wrapDelta);
            var isIncreasing = wrapDelta < 0;
            if (isIncreasing != shouldIncrease)
            {
                loop.Reverse();
                crossingIndex = FindConeSectorWrap(loop, out _);
            }

            var firstIndex = (crossingIndex + 1) % loop.Count;
            if (firstIndex == 0)
                return;

            loop.AddRange(loop.Take(firstIndex));
            loop.RemoveRange(0, firstIndex);
        }

        private static int FindConeSectorWrap(IReadOnlyList<Vector2> loop, out double wrapDelta)
        {
            var crossingIndex = 0;
            wrapDelta = 0;
            var largestJump = double.NegativeInfinity;
            for (var i = 0; i < loop.Count; i++)
            {
                var next = (i + 1) % loop.Count;
                var currentAngle = Math.Atan2(loop[i].Y, loop[i].X);
                var nextAngle = Math.Atan2(loop[next].Y, loop[next].X);
                var delta = nextAngle - currentAngle;
                if (Math.Abs(delta) <= largestJump)
                    continue;

                largestJump = Math.Abs(delta);
                crossingIndex = i;
                wrapDelta = delta;
            }

            return crossingIndex;
        }

        private static double GetConeSeamRadius(IReadOnlyList<Vector2> loop, double repeatAngle,
            double halfRepeatAngle, bool isIncreasing)
        {
            var first = loop[0];
            var last = loop[^1];
            var firstAngle = Math.Atan2(first.Y, first.X) + (isIncreasing ? repeatAngle : -repeatAngle);
            var equivalentFirst = new Vector2(first.Length() * Math.Cos(firstAngle),
                first.Length() * Math.Sin(firstAngle));
            var seamAngle = isIncreasing ? halfRepeatAngle : -halfRepeatAngle;
            var seamDirection = new Vector2(Math.Cos(seamAngle), Math.Sin(seamAngle));
            var edge = equivalentFirst - last;
            var denominator = edge.Cross(seamDirection);
            if (denominator.IsNegligible())
                return 0.5 * (first.Length() + last.Length());

            var fraction = -last.Cross(seamDirection) / denominator;
            return (last + fraction * edge).Dot(seamDirection);
        }

        private static double GetRootMeanSquareEdgeLength<T>(params IEnumerable<IList<T>> loops)
            where T : IVector2D
        {
            var totalLengthSqdSum = 0.0;
            var numLengths = 0;
            foreach (var loop in loops)
            {
                numLengths += Math.Max(0, loop.Count - 1);
                for (int j = 1; j < loop.Count; j++)
                    totalLengthSqdSum += (loop[j - 1].X - loop[j].X) * (loop[j - 1].X - loop[j].X) +
                        (loop[j - 1].Y - loop[j].Y) * (loop[j - 1].Y - loop[j].Y);
            }
            return numLengths == 0 ? double.NaN : Math.Sqrt(totalLengthSqdSum / numLengths);
        }

    }
}
