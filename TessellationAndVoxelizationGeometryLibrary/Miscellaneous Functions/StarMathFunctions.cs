// ***********************************************************************
// Assembly         : StarMath
// Author           : MICampbell
// Created          : 05-14-2015
//
// Last Modified By : MICampbell
// Last Modified On : 07-07-2015
// ***********************************************************************
// <copyright file="constants.cs" company="Design Engineering Lab -- MICampbell">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
namespace TVGL.Numerics
{
    /// <summary>
    /// The main class in the StarMathLib. All functions are static
    /// functions located here.
    /// </summary>
    public static partial class StarMath
    {

        /// <summary>
        ///     Gets or sets the  tolerance for the equality functions: IsPracticallySame, IsNegligible, IsGreaterThanNonNegligible
        ///     IsLessThanNonNegligible.
        /// </summary>
        /// <value>The equality tolerance.</value>
        public static double EqualityTolerance { get; set; } = DefaultEqualityTolerance;
        private const double DefaultEqualityTolerance = 1e-15;
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
        /// Determines whether [is practically same] [the specified x].
        /// the norm is within 1e-15
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if [is practically same] [the specified x]; otherwise, <c>false</c>.</returns>
        public static bool IsPracticallySame(this double[] x, double[] y, double optionalTolerance = double.NaN)
        {
            if (double.IsNaN(optionalTolerance)) optionalTolerance = EqualityTolerance;
            var n = x.GetLength(0);
            if (n != y.GetLength(0)) return false;
            return IsNegligible(x.subtract(y), optionalTolerance);
        }

        /// <summary>
        /// Determines whether the specified x is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this double[] x, double optionalTolerance = DefaultEqualityTolerance)
        {
            return (x.norm2(true) <= optionalTolerance);
        }

        /// <summary>
        /// Determines whether the specified x is negligible (|x| lte 1e-15).
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="optionalTolerance">An optional tolerance.</param>
        /// <returns><c>true</c> if the specified x is negligible; otherwise, <c>false</c>.</returns>
        public static bool IsNegligible(this double x, double optionalTolerance = DefaultEqualityTolerance)
        {
            return (Math.Abs(x) <= optionalTolerance);
        }

        /// <summary>
        ///     Determines whether [is greater than] [the specified y] and not practically the same.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if [is greater than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsGreaterThanNonNegligible(this double x, double y = 0)
        {
            return (x > y && !IsPracticallySame(x, y));
        }

        /// <summary>
        ///     Determines whether [is less than] [the specified y] and not practically the same.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns><c>true</c> if [is less than non negligible] [the specified y]; otherwise, <c>false</c>.</returns>
        public static bool IsLessThanNonNegligible(this double x, double y = 0)
        {
            return (x < y && !IsPracticallySame(x, y));
        }


    }
}