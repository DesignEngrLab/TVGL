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
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

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
        internal const KnownColors DefaultColor = KnownColors.LightGray;

        /// <summary>
        ///     The error ratio used as a base for determining a good tolerance within a given tessellated solid.
        /// </summary>
        public const double BaseTolerance = 1E-9;


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
        public const double SameFaceNormalDotTolerance = 1e-1;
        /// <summary>
        /// The maximum allowable edge similarity score. This is used when trying to match stray edges when loading in 
        /// a tessellated model.
        /// </summary>
        internal static double MaxAllowableEdgeSimilarityScore = 0.2;
        
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
        ///     Polygon File Format
        /// </summary>
        PLY
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
        Face
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
}