// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="Constants.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///     Class Constants.
    /// </summary>
    public static class Constants
    {
        internal const double TwoPi = 2 * Math.PI;
        internal const double HalfPi = Math.PI / 2;
        internal const long SquareRootOfLongMaxValue = 3037000499; // 3 billion
        internal const long CubeRootOfLongMaxValue = 2097151; //2 million
                                                              /// <summary>
                                                              /// VertexCheckSumMultiplier is the checksum multiplier to be used for face and edge references.
                                                              /// Since the edges connect two vertices the maximum value this can be is
                                                              /// the square root of the max. value of a long (see above). However, during
                                                              /// debugging, it is nice to see the digits of the vertex indices embedded in 
                                                              /// check, so when debugging, this is reducing to 1 billion instead of 3 billion.
                                                              /// This way if you are connecting vertex 1234 with 5678, you will get a checksum = 5678000001234
                                                              /// </summary>
#if DEBUG
        public const long VertexCheckSumMultiplier = 1000000000;
#else
        public const long VertexCheckSumMultiplier = SquareRootOfLongMaxValue;
#endif
        /// <summary>
        ///     The convex hull radius for robustness. This is only used when ConvexHull fails on the model.
        /// </summary>
        internal const double ConvexHullRadiusForRobustness = 0.0000001;

        /// <summary>
        ///     The default color
        /// </summary>
        public const KnownColors DefaultColor = KnownColors.LightGray;

        /// <summary>
        ///     The error ratio used as a base for determining a good tolerance within a given tessellated solid.
        /// </summary>
        public const double BaseTolerance = 1E-9;

        /// <summary>
        ///     The tolerance used for simplifying polygons by joining to similary sloped lines.
        /// </summary>
        public const double LineSlopeTolerance = 0.0003;

        /// <summary>
        ///     The tolerance used for simplifying polygons by removing tiny lines.
        /// </summary>
        public const double LineLengthMinimum = 1E-7;

        /// <summary>
        ///     The angle tolerance used in the Oriented Bounding Box calculations
        /// </summary>
        public const double OBBAngleTolerance = 1e-5;

        /// <summary>
        ///     The error for face in surface
        /// </summary>
        public const double ErrorForFaceInSurface = 0.002;

        /// <summary>
        ///     The tolerance for the same normal of a face when two are dot-producted.
        /// </summary>
        public const double SameFaceNormalDotTolerance = 1e-2;
        /// <summary>
        /// The maximum allowable edge similarity score. This is used when trying to match stray edges when loading in 
        /// a tessellated model.
        /// </summary>
        internal const double MaxAllowableEdgeSimilarityScore = 0.2;

        /// <summary>
        /// A high confidence percentage of 0.997 (3 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double HighConfidence = 0.997;

        /// <summary>
        /// A medium confidence percentage of 0.95 (2 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double MediumConfidence = 0.95;

        /// <summary>
        /// A low confidence percentage of 0.68 (1 sigma). This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double LowConfidence = 0.68;

        internal const double VoxelScaleSize = 255.8; // Math.Pow(2, 20) - 0.2;

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
        ///     Finds the index.
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
    /// Units of a specified coordinates within the shape or set of shapes.
    /// </summary>
    public enum UnitType
    {
        /// <summary>
        /// the unspecified state
        /// </summary>
        unspecified = -1,
        /// <summary>
        ///     The millimeter
        /// </summary>
        millimeter,

        /// <summary>
        ///     The micron
        /// </summary>
        micron,


        /// <summary>
        ///     The centimeter
        /// </summary>
        centimeter,

        /// <summary>
        ///     The inch
        /// </summary>
        inch,

        /// <summary>
        ///     The foot
        /// </summary>
        foot,

        /// <summary>
        ///     The meter
        /// </summary>
        meter
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
        /// represents an unspecified state
        /// </summary>
        unspecified,
        /// <summary>
        ///     Stereolithography (STL) American Standard Code for Information Interchange (ASCII)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_ASCII,

        /// <summary>
        ///     Stereolithography (STL) Binary
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_Binary,

        /// <summary>
        ///     Mobile MultiModal Framework
        /// </summary>
        ThreeMF,

        /// <summary>
        ///     Mobile MultiModal Framework
        /// </summary>
        Model3MF,

        /// <summary>
        ///     Additive Manufacturing File Format
        /// </summary>
        AMF,

        /// <summary>
        ///     Object File Format
        /// </summary>
        OFF,

        /// <summary>
        ///     Polygon File Format as ASCII
        /// </summary>
        PLY_ASCII,
        /// <summary>
        ///     Polygon File Format as Binary
        /// </summary>
        PLY_Binary,
        /// <summary>
        ///     A serialized version of the TessellatedSolid object
        /// </summary>
        TVGL
    }

    internal enum FormatEndiannessType
    {
        ascii,
        binary_little_endian,
        binary_big_endian
    }
    /// <summary>
    ///     Enum ShapeElement
    /// </summary>
    internal enum ShapeElement
    {
        /// <summary>
        ///     The vertex
        /// </summary>
        Vertex,
        Edge,
        Face,
        Uniform_Color
    }

    /// <summary>
    ///     Enum ColorElements
    /// </summary>
    internal enum ColorElements
    {
        Red,
        Green,
        Blue,
        Opacity
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

    public class Utilities
    {
        /// <summary>
        /// Stores the offsets for the 26 adjacent voxels.
        /// 6 Voxel-face adjacent neghbours
        /// 12 Voxel-edge adjacent neghbours
        /// 8 Voxel-corner adjacent neghbours        
        /// </summary>
        public static readonly List<int[]> CoordinateOffsets = new List<int[]>()
        {
            new[] {1,  0,  0}, /// Voxel-face adjacent neghbours
            new[] {-1,  0,  0}, /// 0 to 5
            new[] { 0,  1,  0},
            new[] { 0, -1,  0},
            new[] { 0,  0,  1},
            new[] { 0,  0, -1},
            new[] { 1,  0, -1}, /// Voxel-edge adjacent neghbours
            new[] {-1,  0, -1}, /// 6 to 17
            new[] { 1,  0,  1},
            new[] {-1,  0,  1},
            new[] { 1,  1,  0},
            new[] {-1,  1,  0},
            new[] { 1, -1,  0},
            new[] {-1, -1,  0},
            new[] { 0, -1,  1},
            new[] { 0, -1, -1},
            new[] { 0,  1,  1},
            new[] { 0,  1, -1},
            new[] {-1, -1, -1}, /// Voxel-corner adjacent neghbours
            new[] {-1, -1,  1}, /// 18 to 25
            new[] { 1, -1,  1},
            new[] { 1, -1, -1},
            new[] {-1,  1, -1},
            new[] {-1,  1,  1},
            new[] { 1,  1,  1},
            new[] { 1,  1, -1}
        };
    }
}