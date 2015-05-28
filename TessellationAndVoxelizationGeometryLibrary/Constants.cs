// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-18-2015
// ***********************************************************************
// <copyright file="Constants.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///     Class Constants.
    /// </summary>
    public static class Constants
    {
        internal const KnownColors DefaultColor = KnownColors.PaleGoldenrod;

        /// <summary>
        ///     The reg ex solid
        /// </summary>
        public const string RegExSolid = @"solid\s+(?<Name>[^\r\n]+)?";

        /// <summary>
        ///     The reg ex coord
        /// </summary>
        public const string RegExCoord = @"\s*(facet normal|vertex)\s+(?<X>[^\s]+)\s+(?<Y>[^\s]+)\s+(?<Z>[^\s]+)";

        /// <summary>
        ///     The number style
        /// </summary>
        public const NumberStyles NumberStyle =
            (NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);

        /// <summary>
        ///     The epsilon same angle
        /// </summary>
        public const double EpsilonSameAngle = 1e-15;

        /// <summary>
        ///     The float size
        /// </summary>
        public const int FloatSize = sizeof(float);

        /// <summary>
        ///     The vertex size
        /// </summary>
        public const int VertexSize = (FloatSize * 3);

        /// <summary>
        ///     The look up string format
        /// </summary>
        public const string LookUpStringFormat = "F16";

        /// <summary>
        ///     The minimum probability for primitive classifier
        /// </summary>
        public const double MinProbabilityForPrimitiveClassifer = 0.001;

        /// <summary>
        ///     The error for face in surface
        /// </summary>
        public const double ErrorForFaceInSurface = 0.002;//0.02

        /// <summary>
        ///     The surface area fraction for primitive
        /// </summary>
        public const double SurfaceAreaFractionForPrimitive = 0.001;

        /// <summary>
        ///     The minimum probability for primitive of winged edge
        /// </summary>
        public const double MinProbabilityForPrimitiveOfWingedEdge = 0.3;

        /// <summary>
        ///     The maximum number edges per face
        /// </summary>
        public const int MaxNumberEdgesPerFace = 3;

        /// <summary>
        ///     Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>  
        internal static int FindIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
        {
            var numItems = items.Count();
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item)) return index;
                index++;
            }
            return -1;
        }
        /// <summary>
        /// Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>
        internal static int FindIndex<T>(this IEnumerable<T> items, T predicate)
        {
            var numItems = items.Count();
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in items)
            {
                if (predicate.Equals(item)) return index;
                index++;
            }
            return -1;
        }
    }

    /// <summary>
    ///     Enum CurvatureType
    /// </summary>
    public enum CurvatureType
    {
        /// <summary>
        ///     The concave
        /// </summary>
        Concave = -1,

        /// <summary>
        ///     The saddle or flat
        /// </summary>
        SaddleOrFlat = 0,

        /// <summary>
        ///     The convex
        /// </summary>
        Convex = 1,

        /// <summary>
        ///     The undefined
        /// </summary>
        Undefined
    }

    /// <summary>
    ///     Enum FileType
    /// </summary>
    public enum FileType
    {
        /// <summary>
        ///     The file is text.
        /// </summary>
        Text,
        /// <summary>
        /// The file is binary.
        /// </summary>
        Binary
    }

    internal enum PrimitiveSurfaceType
    {
        Unknown = 0,
        Dense = 123456789,
        Flat = 500,
        Cylinder = 501,
        Sphere = 502,
        Flat_to_Curve = 503,
        Sharp = 504
    }

    /// <summary>
    ///     A comparer for optimization that can be used for either
    ///     minimization or maximization.
    /// </summary>
    internal class NoEqualSort : IComparer<double>
    {
        /// <summary>
        ///     Compares two objects and returns a value indicating whether one is less than, equal to, or greater than the other.
        /// </summary>
        /// <param name="x">The first object to compare.</param>
        /// <param name="y">The second object to compare.</param>
        /// <returns>
        ///     A signed integer that indicates the relative values of <paramref name="x" /> and <paramref name="y" />, as
        ///     shown in the following table.Value Meaning Less than zero<paramref name="x" /> is less than <paramref name="y" />
        ///     .Zero<paramref name="x" /> equals <paramref name="y" />.Greater than zero<paramref name="x" /> is greater than
        ///     <paramref name="y" />.
        /// </returns>
        public int Compare(double x, double y)
        {
            if (x < y) return -1;
            return 1;
        }
    }
}