// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
namespace TVGL.Numerics
{
    /// <summary>
    /// The main class in the StarMathLib. All functions are static
    /// functions located here.
    /// </summary>
    public static class EqualityExtensions
    {
        /// <summary>
        ///     Gets or sets the  tolerance for the equality functions: IsPracticallySame, IsNegligible, IsGreaterThanNonNegligible
        ///     IsLessThanNonNegligible.
        /// </summary>
        /// <value>The equality tolerance.</value>
        public static double EqualityTolerance { get; set; } = DefaultEqualityTolerance;
        private const double DefaultEqualityTolerance = 1e-12;
        /// <summary>
        /// Determines whether [is practically same] [the specified x].
        /// the norm is within 1e-15
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if [is practically same] [the specified x]; otherwise, <c>false</c>.</returns>
        public static bool IsPracticallySame(this double x, double y, double optionalTolerance = DefaultEqualityTolerance)
        {
            return IsNegligible(x - y, optionalTolerance);
        }

        /// <summary>
        /// Determines whether [is practically same] [the specified v1].
        /// the norm is within 1e-15
        /// </summary>
        /// <param name="v1">The first vector.</param>
        /// <param name="v2">The second vector.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if [is practically same] [the specified x]; otherwise, <c>false</c>.</returns>
        public static bool IsPracticallySame(this Vector2 v1, Vector2 v2, double optionalTolerance = DefaultEqualityTolerance)
        {
            return IsNegligible(v1 - v2, optionalTolerance);
        }

        /// <summary>
        /// Determines whether [is practically same] [the specified x].
        /// the norm is within 1e-15
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if [is practically same] [the specified x]; otherwise, <c>false</c>.</returns>
        public static bool IsPracticallySame(this Vector3 v1, Vector3 v2, double optionalTolerance = DefaultEqualityTolerance)
        {
            return IsNegligible(v1 - v2, optionalTolerance);
        }

        /// <summary>
        /// Determines whether the specified v1 is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="v1">The vector.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this Vector2 v1, double optionalTolerance = DefaultEqualityTolerance)
        {
            return Math.Abs(v1.X) <= optionalTolerance && Math.Abs(v1.Y) <= optionalTolerance;
        }
        /// <summary>
        /// Determines whether the specified v1 is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="v1">The vector.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this Vector3 v1, double optionalTolerance = DefaultEqualityTolerance)
        {
            return Math.Abs(v1.X) <= optionalTolerance && Math.Abs(v1.Y) <= optionalTolerance && Math.Abs(v1.Z) <= optionalTolerance;
        }

        /// <summary>
        /// Determines whether the specified x is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this double x, double optionalTolerance = DefaultEqualityTolerance)
        {
            return Math.Abs(x) <= optionalTolerance;
        }

        /// <summary>
        /// Determines whether [is greater than] [the specified y] and not practically the same.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="optionalTolerance">The optional tolerance.</param>
        /// <returns><c>true</c> if [is greater than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsGreaterThanNonNegligible(this double x, double y = 0, double optionalTolerance = DefaultEqualityTolerance)
        //public static bool IsGreaterThanNonNegligible(this double x, double y , double optionalTolerance )
        {
            return (x > y && !IsPracticallySame(x, y, optionalTolerance));
        }

        /// <summary>
        /// Determines whether [is less than] [the specified y] and not practically the same.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="optionalTolerance">The optional tolerance.</param>
        /// <returns><c>true</c> if [is less than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsLessThanNonNegligible(this double x, double y = 0, double optionalTolerance = DefaultEqualityTolerance)
        //public static bool IsLessThanNonNegligible(this double x, double y, double optionalTolerance)
        {
            return (x < y && !IsPracticallySame(x, y, optionalTolerance));
        }


    }
}