// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="EqualityExtensions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
namespace TVGL
{
    /// <summary>
    /// The main class in the StarMathLib. All functions are static
    /// functions located here.
    /// </summary>
    public static class EqualityExtensions
    {
        /// <summary>
        /// Gets or sets the  tolerance for the equality functions: IsPracticallySame, IsNegligible, IsGreaterThanNonNegligible
        /// IsLessThanNonNegligible.
        /// </summary>
        /// <value>The equality tolerance.</value>
        public static double EqualityTolerance { get; set; } = DefaultEqualityTolerance;
        /// <summary>
        /// The default equality tolerance
        /// </summary>
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
        /// Determines whether [is practically same] [the specified x].
        /// the norm is within 1e-15
        /// </summary>
        /// <param name="a">The v1.</param>
        /// <param name="b">The v2.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if [is practically same] [the specified x]; otherwise, <c>false</c>.</returns>
        internal static bool IsPracticallySame(this ComplexNumber a, ComplexNumber b, double optionalTolerance = DefaultEqualityTolerance)
        {
            return IsNegligible(a - b, optionalTolerance);

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
        /// Determines whether the specified v1 is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this ComplexNumber a, double optionalTolerance = DefaultEqualityTolerance)
        {
            return Math.Abs(a.Real) <= optionalTolerance && Math.Abs(a.Imaginary) <= optionalTolerance;
        }

        /// <summary>
        /// Determines whether [is greater than] [the specified y] and not practically the same.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="optionalTolerance">The optional tolerance.</param>
        /// <returns><c>true</c> if [is greater than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsGreaterThanNonNegligible(this double x, double y, double optionalTolerance = DefaultEqualityTolerance)
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
        public static bool IsLessThanNonNegligible(this double x, double y, double optionalTolerance = DefaultEqualityTolerance)
        //public static bool IsLessThanNonNegligible(this double x, double y, double optionalTolerance)
        {
            return (x < y && !IsPracticallySame(x, y, optionalTolerance));
        }

        /// <summary>
        /// Determines whether [is greater than zero] and not practically the same as zero.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="optionalTolerance">The optional tolerance.</param>
        /// <returns><c>true</c> if [is greater than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsPositiveNonNegligible(this double x, double optionalTolerance = DefaultEqualityTolerance)
        //public static bool IsGreaterThanNonNegligible(this double x, double y , double optionalTolerance )
        {
            return (x > 0.0 && !IsNegligible(x, optionalTolerance));
        }

        /// <summary>
        /// Determines whether [is less than zero] and not practically the same as zero.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="optionalTolerance">The optional tolerance.</param>
        /// <returns><c>true</c> if [is less than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsNegativeNonNegligible(this double x, double optionalTolerance = DefaultEqualityTolerance)
        //public static bool IsLessThanNonNegligible(this double x, double y, double optionalTolerance)
        {
            return (x < 0.0 && !IsNegligible(x, optionalTolerance));
        }


    }
}