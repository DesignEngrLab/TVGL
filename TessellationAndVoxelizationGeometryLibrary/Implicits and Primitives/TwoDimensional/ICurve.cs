// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="ICurve.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Represents a planar curve that can evaluate fitting error and construct a fit from points.
    /// </summary>
    public interface ICurve
    {
        /// <summary>
        /// Returns the squared distance from a point to the curve. Squared distance is
        /// useful for least-squares fitting because it avoids a square-root operation.
        /// </summary>
        /// <typeparam name="T">The point type.</typeparam>
        /// <param name="point">The point to evaluate.</param>
        /// <returns>The squared distance from <paramref name="point"/> to the curve.</returns>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IVector;

        /// <summary>
        /// Fits a curve to a collection of planar points.
        /// </summary>
        /// <typeparam name="T">The point type.</typeparam>
        /// <param name="points">The points used for fitting.</param>
        /// <param name="curve">When this method returns, receives the fitted curve when fitting succeeds.</param>
        /// <param name="error">When this method returns, receives the fitting error.</param>
        /// <returns><see langword="true"/> when a curve is fitted successfully; otherwise, <see langword="false"/>.</returns>
        public static abstract bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error)
            where T : IVector2D;
    }
}
