using System.Collections.Generic;
using System;

using MIConvexHull;

namespace  PolygonSharp

{
    public interface ICurve
    {
        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IVertex2D;

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static abstract bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error)
            where T : IVertex2D;
    }
}
