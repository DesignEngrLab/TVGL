// <copyright file="HyperRect.cs" company="Eric Regina">
// Copyright (c) Eric Regina. All rights reserved.
// </copyright>

namespace TVGL.PointCloud
{
    using System;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using TVGL.ConvexHullDetails;

    /// <summary>
    /// Represents a hyper-rectangle. An N-Dimensional rectangle.
    /// </summary>
    /// <typeparam name="T">The type of "i" in the metric space in which the hyper-rectangle lives.</typeparam>
    internal readonly struct HyperRect
    {
        internal double[] MinPoint { get; private init; }
        internal double[] MaxPoint { get; private init; }

        readonly int dimensions;

        /// <summary>
        /// Get a hyper rectangle which spans the entire implicit metric space.
        /// </summary>
        internal HyperRect(int dimensions)
        {
            this.dimensions = dimensions;
            MinPoint = Enumerable.Repeat(double.MinValue, dimensions).ToArray();
            MaxPoint = Enumerable.Repeat(double.MaxValue, dimensions).ToArray();
        }

        internal HyperRect(double[] minValues, double[] maxValues)
        {
            dimensions = minValues.Length;
            MinPoint = minValues.Clone() as double[];
            MaxPoint = maxValues.Clone() as double[];
        }


        /// <summary>
        /// Gets the point on the rectangle that is closest to the given point.
        /// If the point is within the rectangle, then the input point is the same as the
        /// output point.f the point is outside the rectangle then the point on the rectangle
        /// that is closest to the given point is returned.
        /// </summary>
        /// <param name="targetPoint">We try to find a point in or on the rectangle closest to this point.</param>
        /// <returns>The point on or in the rectangle that is closest to the given point.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal double GetClosestPoint(IPoint targetPoint)
        {
            var distSqd = 0.0;
            for (var i = 0; i < dimensions; i++)
            {
                if (MinPoint[i] > targetPoint[i])
                    distSqd += (MinPoint[i] - targetPoint[i]) * (MinPoint[i] - targetPoint[i]);
                else if (MaxPoint[i] < targetPoint[i])
                    distSqd += (MaxPoint[i] - targetPoint[i]) * (MaxPoint[i] - targetPoint[i]);
                // else Point is within rectangle, and the distance should not increase
            }
            return distSqd;
        }
    }
}
