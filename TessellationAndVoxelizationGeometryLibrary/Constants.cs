// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
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
        internal const int MaxNumberFacesDefaultFullTS = 50000;
        public const double TwoPi = 2 * Math.PI;
        public const double HalfPi = Math.PI / 2;
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
        /// The minimum angle used to approximate a circle. An octagon is the largest sided polygon that any sane person would want to define
        /// without approximating a circle. For an regular octogon, this is an outer angle of 45 degrees (360/8). So that is the most
        /// conservative value for smooth. However, it is not uncommon to have slants in a model well below this. A 2-to-1 slope makes an
        /// angle of 26.6-degrees. So, we consider a little lower as the cutoff.
        /// </summary>
        public const double MinSmoothAngle = 25 * Math.PI / 180;  

        /// <summary>
        ///     The tolerance used for simplifying polygons by joining to similary sloped lines.
        /// </summary>
        public const double LineSlopeTolerance = 0.0003;

        /// <summary>
        ///     The tolerance used for simplifying polygons by removing tiny lines.
        /// </summary>
        public const double LineLengthMinimum = 1E-9;

        /// <summary>
        ///     The default color
        /// </summary>
        public const KnownColors DefaultColor = KnownColors.LightGray;

        /// <summary>
        ///     The error ratio used as a base for determining a good tolerance within a given tessellated solid.
        /// </summary>
        public const double BaseTolerance = 1E-11;


        /// <summary>
        /// The tolerance multiplier (multiplied into the minimum X or Y dimension of the polygon) for 
        /// detecting identical/repeat vertices and for intersection checks in various polygon functions.
        /// </summary>
        public const double PolygonSameTolerance = 1e-7;

        /// <summary>
        ///     The angle tolerance used in the Oriented Bounding Box calculations
        /// </summary>
        public const double OBBTolerance = 1e-5;

        /// <summary>
        ///     The error for face in surface
        /// </summary>
        public const double ErrorForFaceInSurface = 0.002;


        /// <summary>
        ///     The tolerance for the same normal of a face when two are dot-producted.
        ///     the angle would be acos(1 - SameFaceNormalDotTolerance). 
        ///     so, 0.01 would be 8-deg
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
        /// A high confidence percentage of 99% This is used in some boolean "is" checks,
        /// like Polygon.IsRectangular
        /// </summary>
        public const double MediumHighConfidence = 0.99;

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

        /// <summary>
        /// This is used to set the amount that polygon segments search outward to define the grid
        /// points that they affect.
        /// </summary>
        internal const int MarchingCubesBufferFactor = 5;

        internal const int MarchingCubesMissedFactor = 4;

        internal const double DefaultTessellationError = 0.08;

        internal const double DefaultTessellationMaxAngleError = 15; 
        /// <summary>
        /// The tessellation to voxelization intersection combinations. This is used in the unction that
        /// produces voxels on the edges and faces of a tessellated shape.
        /// </summary>
        internal static readonly List<int[]> TessellationToVoxelizationIntersectionCombinations = new List<int[]>()
        {
            new []{ 0, 0, 0},
            new []{ -1, 0, 0},
            new []{ 0, -1, 0},
            new []{ 0, 0, -1},
            new []{ -1, -1, 0},
            new []{ -1, 0, -1},
            new []{ 0, -1, -1},
            new []{ -1, -1, -1},
        };

        /// <summary>
        ///     Finds the index.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The items.</param>
        /// <param name="predicate">The predicate.</param>
        /// <returns>System.Int32.</returns>
        internal static int FindIndex<T>(this IEnumerable<T> items, Predicate<T> predicate)
        {
            var itemsList = items as IList<T> ?? items.ToList();
            var numItems = itemsList.Count;
            if (numItems == 0) return -1;
            var index = 0;
            foreach (var item in itemsList)
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
            var index = 0;
            foreach (var item in items)
            {
                if (predicate.Equals(item)) return index;
                index++;
            }
            return -1;
        }

        internal static PolyRelInternal SwitchAAndBPolygonRelationship(this PolyRelInternal relationship)
        {
            if ((relationship & PolyRelInternal.Intersection) == PolyRelInternal.AInsideB)
            {
                relationship |= PolyRelInternal.BInsideA;
                relationship &= ~PolyRelInternal.AInsideB;
            }
            else if ((relationship & PolyRelInternal.Intersection) == PolyRelInternal.BInsideA)
            {
                relationship |= PolyRelInternal.AInsideB;
                relationship &= ~PolyRelInternal.BInsideA;
            }
            return relationship;
        }

        internal const double DegreesToRadiansFactor = Math.PI / 180.0;
        internal const double DefaultRoundOffsetDeltaAngle = Math.PI / 180.0; // which is also one degree or 360 in a circle

    }

    /// <summary>
    /// Units of a specified coordinates within the shape or set of shapes.
    /// </summary>
    public enum UnitType
    {
        /// <summary>
        /// the unspecified state
        /// </summary>
        unspecified = 0,

        /// <summary>
        ///     The millimeter
        /// </summary>
        millimeter = 11,

        /// <summary>
        ///     The micron
        /// </summary>
        micron = 8,

        /// <summary>
        ///     The centimeter
        /// </summary>
        centimeter = 1,

        /// <summary>
        ///     The inch
        /// </summary>
        inch = 4,

        /// <summary>
        ///     The foot
        /// </summary>
        foot = 3,

        /// <summary>
        ///     The meter
        /// </summary>
        meter = 6
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
    /// Enum PolygonCollection
    /// </summary>
    public enum PolygonCollection
    {
        /// <summary>
        /// The separate loops
        /// </summary>
        SeparateLoops,
        /// <summary>
        /// The polygon with holes (or shallow polygon trees)
        /// </summary>
        PolygonWithHoles,
        /// <summary>
        /// The polygon trees
        /// </summary>
        PolygonTrees
    }

    /// <summary>
    /// Enum ResultTypes
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        /// only positive ones
        /// </summary>
        OnlyKeepPositive = +1,
        /// <summary>
        /// only negative ones
        /// </summary>
        OnlyKeepNegative = -1,
        /// <summary>
        /// both positive and negative permitted
        /// </summary>
        BothPermitted = 0,
        /// <summary>
        /// The convert all to positive
        /// </summary>
        ConvertAllToPositive = +2,
        /// <summary>
        /// The convert all to negative
        /// </summary>
        ConvertAllToNegative = -2
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
        ///     Wavefront 3D File Format
        /// </summary>
        OBJ,

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
        ///     Shell file...I think this was created as part of collaboration with an Oregon-based EDA company
        /// </summary>
        SHELL,

        /// <summary>
        ///     A human-readable, serialized version of TVGL Solid objects
        /// </summary>
        TVGL,

        /// <summary>
        ///     A compressed version of TVGL Solid objects. About 4X smaller than TVGL.
        /// </summary>
        TVGLz
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
    /// CartesianDirections: just the six cardinal directions for the voxelized box around the solid
    /// </summary>
    public enum CartesianDirections
    {
        /// <summary>
        /// <summary>
        /// Enum VoxelDirections
        /// </summary>
        /// Negative X Direction
        /// </summary>
        /// <summary>
        /// The x negative
        /// </summary>
        XNegative = -1,

        /// <summary>
        /// Negative Y Direction
        /// <summary>
        /// The x negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y negative
        /// </summary>
        YNegative = -2,

        /// <summary>
        /// Negative Z Direction
        /// <summary>
        /// The y negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z negative
        /// </summary>
        ZNegative = -3,

        /// <summary>
        /// Positive X Direction
        /// <summary>
        /// The z negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The x positive
        /// </summary>
        XPositive = 1,

        /// <summary>
        /// Positive Y Direction
        /// <summary>
        /// The x positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y positive
        /// </summary>
        YPositive = 2,

        /// <summary>
        /// Positive Z Direction
        /// <summary>
        /// The y positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z positive
        /// </summary>
        ZPositive = 3
    }

    internal enum VerticalLineReferenceType
    {
        Above,
        On,
        Below,
        NotIntersecting
    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>
    [Flags]
    internal enum PolyRelInternal
    {
        Separated = 0,
        EdgesCross = 1,
        CoincidentEdges = 2,
        CoincidentVertices = 4,
        InsideHole = 8,
        AInsideB = 16,
        BInsideA = 32,
        Intersection = AInsideB | BInsideA,
        Equal = 64,
        EqualButOpposite = 128
    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>

    public enum PolygonRelationship
    {
        /// <summary>
        /// The separated
        /// </summary>
        Separated = 0,

        /// <summary>
        /// a inside b
        /// </summary>
        AInsideB = 16,

        /// <summary>
        /// a is inside hole of b
        /// </summary>
        AIsInsideHoleOfB = 24,

        /// <summary>
        /// The b inside a
        /// </summary>
        BInsideA = 32,

        /// <summary>
        /// The b is inside hole of a
        /// </summary>
        BIsInsideHoleOfA = 40,

        /// <summary>
        /// The intersection
        /// </summary>
        Intersection = 48,

        /// <summary>
        /// The equal
        /// </summary>
        Equal = 64,

        /// <summary>
        /// The equal but opposite
        /// </summary>
        EqualButOpposite = 128
    }

    public enum SegmentRelationship
    {
        NoOverlap,
        Abutting,
        DoubleOverlap,
        BEnclosesA,
        AEnclosesB,
        CrossOver_BOutsideAfter,
        CrossOver_AOutsideAfter,
    }

    public enum CollinearityTypes
    {
        None,
        BothSameDirection,
        BothOppositeDirection,
        After,
        Before,
        AAfterBBefore, // case 14
        ABeforeBAfter
    }

    public enum WhereIsIntersection
    {
        Intermediate,
        AtStartOfA,
        AtStartOfB,
        BothStarts
    }

    /// <summary>
    ///     A comparer for optimization that can be used for either
    ///     ascending or descending.
    /// </summary>
    public class NoEqualSort : IComparer<double>
    {
        private readonly int direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoEqualSort"/> class.
        /// </summary>
        /// <param name="ascendingOrder">if set to <c>true</c> [ascending order].</param>
        public NoEqualSort(bool ascendingOrder = true)
        {
            direction = ascendingOrder ? -1 : 1;
        }

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
            if (x < y) return direction;
            return -direction;
        }
    }
}