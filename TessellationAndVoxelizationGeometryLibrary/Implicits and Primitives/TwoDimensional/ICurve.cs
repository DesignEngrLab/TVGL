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
using System.Collections.Generic;
using System;

using TVGL.ConvexHullDetails;

namespace TVGL
{
    /// <summary>
    /// Interface ICurve
    /// </summary>
    public interface ICurve
    {
        /// <summary>
        /// Returns the squared error of new point. This should be the square of the
        /// actual distance to the curve. Squared is canonical since 1) usually fits
        /// would be minimum least squares, 2) saves from doing square root operation
        /// which is an undue computational expense
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public double SquaredErrorOfNewPoint<T>(T point) where T : IPoint;

        /// <summary>
        /// Creates from points.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="points">The points.</param>
        /// <param name="curve">The curve.</param>
        /// <param name="error">The error.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error)
        {
            throw new NotImplementedException("please implement static method in curve: static ICurve " +
                "CreateFromPoints(IEnumerable<Vector2> points)");
        }

        // Switch to this when C#11 comes out (which it has but waiting for .NET8 in Nov.2023
        //public static abstract bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error)
        //    where T : IPoint2D;
    }
}
