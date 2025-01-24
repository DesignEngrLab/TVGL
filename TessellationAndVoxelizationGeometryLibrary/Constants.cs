// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Constants.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Class Constants.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// The two pi
        /// </summary>
        public const double TwoPi = Math.Tau;
        /// <summary>
        /// The half pi
        /// </summary>
        public const double HalfPi = Math.PI / 2;
        /// <summary>
        /// The cos15
        /// </summary>
        public readonly static double Cos15 = Math.Cos(15.0 * Math.PI / 180);
        /// <summary>
        /// The square root of long maximum value
        /// </summary>
        public const long SquareRootOfLongMaxValue = 3037000499; // 3 billion
        /// <summary>
        /// The cube root of long maximum value
        /// </summary>
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
        /// The default tessellation error
        /// </summary>
        internal const double DefaultTessellationError = 0.08;

        /// <summary>
        /// The default tessellation maximum angle error
        /// </summary>
        internal const double DefaultTessellationMaxAngleErrorDegrees = 15;

        /// <summary>
        /// The minimum angle used to approximate a circle. An octagon is the largest sided polygon that any sane person would want to define
        /// without approximating a circle. For an regular octogon, this is an outer angle of 45 degrees (360/8). So that is the most
        /// conservative value for smooth. However, it is not uncommon to have slants in a model well below this. A 2-to-1 slope makes an
        /// angle of 26.6-degrees. So, we consider a little lower as the cutoff.
        /// </summary>
        public const double MinSmoothAngle = 1.6 * DefaultTessellationMaxAngleErrorDegrees * Math.PI / 180;

        /// <summary>
        /// The tolerance used for simplifying polygons by joining to similary sloped lines.
        /// </summary>
        public const double LineSlopeTolerance = 0.0003;

        /// <summary>
        /// The tolerance used for simplifying polygons by removing tiny lines.
        /// </summary>
        public const double LineLengthMinimum = 1E-9;

        /// <summary>
        /// The default color
        /// </summary>
        public const KnownColors DefaultColor = KnownColors.LightGray;

        /// <summary>
        /// The error ratio used as a base for determining a good tolerance within a given tessellated solid.
        /// </summary>
        public const double BaseTolerance = 1E-9;


        /// <summary>
        /// The maximum number of iterations in a nonlinear solve routine.
        /// </summary>
        public const int MaxIterationsNonlinearSolve = 67;
        /// <summary>
        /// The tolerance multiplier (multiplied into the minimum X or Y dimension of the polygon) for
        /// detecting identical/repeat vertices and for intersection checks in various polygon functions.
        /// </summary>
        public const double PolygonSameTolerance = 1e-7;

        /// <summary>
        /// The angle tolerance used in the Oriented Bounding Box calculations
        /// </summary>
        public const double OBBTolerance = 1e-5;


        /// <summary>
        /// The default minimum angle to determine if two directions are aligned (in degrees).
        /// </summary>
        public const double DefaultSameAngleDegrees = 1;


        /// <summary>
        /// The default minimum angle to determine if two directions are aligned (in radians).
        /// </summary>
        public const double DefaultSameAngleRadians = 0.01745329251994329576923690768489; // DefaultSameAngleDegrees * Math.PI / 180;

        /// <summary>
        /// This is based on the DefaultMinAngleInPlaneDegrees. It is a value just below 1.0 (which is the cosine of 0-degrees)
        /// which signifies if two vectors have a dot product greater than this, then they are within the DefaultMinAngleInPlaneDegrees
        /// (and often effectively the same).
        public const double DotToleranceForSame = 0.99984769515639123915701155881391;
        // this is cos(DefaultSameAngleRadians);


        /// <summary>
        /// This is based on the DefaultMinAngleInPlaneDegrees. It is a value close to 0 (which is the cosine of 90-degrees)
        /// which signifies if two vectors have a dot product less than this, then they are within the 90 - DefaultMinAngleInPlaneDegrees - 
        /// they are effectively orthogonal.
        public const double DotToleranceOrthogonal = 0.01745240643728351281941897851632;
        // this is  cos(90-DefaultSameAngleRadians) or sin(DefaultMinAngleInPlaneDegrees);

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

        /// <summary>
        /// The marching cubes missed factor
        /// </summary>
        internal const int MarchingCubesMissedFactor = 4;


        internal const double DefaultEqualityTolerance = 1e-12;


        /// <summary>
        /// The tessellation to voxelization intersection combinations. This is used in the function that
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
        /// Finds the index.
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
        /// The Pseudoangle function is used to sort points in a counter-clockwise order.
        /// It is intended to be much faster than the atan2 function. 
        /// https://stackoverflow.com/questions/16542042/fastest-way-to-sort-vectors-by-angle-without-actually-computing-that-angle
        /// It is not as accurate as atan2, but it is monotonic and preserves the ordering starting
        /// with 0 at the positive x-axis and increasing counter-clockwise to 2 (at 180 degrees)
        /// and then increasing to 4 (at 360 degrees). It appears to be more than 10X faster than atan2.
        /// | Method      | Mean      | Error     | StdDev    |
        /// |------------ |----------:|----------:|----------:|
        /// | ATan2       | 3.3143 ns | 0.0660 ns | 0.1503 ns |
        /// | PseudoAngle | 0.2663 ns | 0.0120 ns | 0.0100 ns | (using BenchmarkDotNet)
        /// </summary>
        /// <param name="dx"></param>
        /// <param name="dy"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Pseudoangle(double dx, double dy)
        {
            var p = dx / (Math.Abs(dx) + Math.Abs(dy)); // -1 .. 1 increasing with x
            if (dy < 0) return 3 + p;  //  2 .. 4 increasing with x
            return 1 - p;  //  0 .. 2 decreasing with x
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Pseudoangle(Int128 dx, Int128 dy)
        {
            var denominator = Int128.Abs(dx) + Int128.Abs(dy);
            var p = new RationalIP(dx, denominator).AsDouble; // -1 .. 1 increasing with x
            if (dy < 0) return 3 + p;  //  2 .. 4 increasing with x
            return 1 - p;  //  0 .. 2 decreasing with x
        }

        internal static void SwapItemsInList<T>(int i, int j, IList<T> points)
        {
            var temp = points[i];
            points[i] = points[j];
            points[j] = temp;
        }
        /// <summary>
        /// Switches a and b polygon relationship.
        /// </summary>
        /// <param name="relationship">The relationship.</param>
        /// <returns>PolyRelInternal.</returns>
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

        /// <summary>
        /// Enumerates the thruple.
        /// </summary>
        /// <param name="thruple">The thruple.</param>
        /// <returns>A list of TS.</returns>
        internal static IEnumerable<T> EnumerateThruple<T>(this (T, T, T) thruple)
        {
            yield return thruple.Item1;
            yield return thruple.Item2;
            yield return thruple.Item3;
        }


        /// <summary>
        /// Finds the index where the value should be inserted into the collection to maintain
        /// increasing order.
        /// </summary>
        /// <param name="array">the sorted array of doubles</param>
        /// <param name="queryValue">the value to insert</param>
        /// <param name="inclusiveLowIndex">the inclusive starting low index</param>
        /// <param name="inclusiveHighIndex">the inclusive starting low index (usually one less than the count)</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IncreasingDoublesBinarySearch(this IList<double> array, double queryValue,
            int inclusiveLowIndex, int inclusiveHighIndex)
        {
            // This binary search is modified/simplified from Array.BinarySearch
            // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
            // here we are simply trying to order the doubles in increasing order
            while (inclusiveLowIndex <= inclusiveHighIndex)
            {
                // try the point in the middle of the range. note the >> 1 is a bit shift to quickly divide by 2
                int i = inclusiveLowIndex + ((inclusiveHighIndex - inclusiveLowIndex) >> 1);
                var valueAtIndex = array[i];
                if (queryValue == valueAtIndex) return i; //equal values could be in any order
                if (queryValue > valueAtIndex) inclusiveLowIndex = i + 1;
                else inclusiveHighIndex = i - 1;
            }
            return inclusiveLowIndex;
        }

        /// <summary>
        /// Finds the index where the value should be inserted into the collection to maintain
        /// increasing order.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int IncreasingDoublesBinarySearch(this IList<double> array, double value)
        => IncreasingDoublesBinarySearch(array, value, 0, array.Count - 1);

        /// <summary>
        /// The degrees to radians factor
        /// </summary>
        internal const double DegreesToRadiansFactor = Math.PI / 180.0;
        /// <summary>
        /// The default round offset delta angle
        /// </summary>
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
        /// The millimeter
        /// </summary>
        millimeter = 11,

        /// <summary>
        /// The micron
        /// </summary>
        micron = 8,

        /// <summary>
        /// The centimeter
        /// </summary>
        centimeter = 1,

        /// <summary>
        /// The inch
        /// </summary>
        inch = 4,

        /// <summary>
        /// The foot
        /// </summary>
        foot = 3,

        /// <summary>
        /// The meter
        /// </summary>
        meter = 6
    }

    /// <summary>
    /// Enum CurvatureType
    /// </summary>
    public enum CurvatureType
    {
        /// <summary>
        /// The concave
        /// </summary>
        Concave = -1,

        /// <summary>
        /// The saddle or flat
        /// </summary>
        SaddleOrFlat = 0,

        /// <summary>
        /// The convex
        /// </summary>
        Convex = 1,

        /// <summary>
        /// The undefined
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
    /// Enum FileType
    /// </summary>
    public enum FileType
    {
        /// <summary>
        /// represents an unspecified state
        /// </summary>
        unspecified,

        /// <summary>
        /// Stereolithography (STL) American Standard Code for Information Interchange (ASCII)
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_ASCII,

        /// <summary>
        /// Stereolithography (STL) Binary
        /// </summary>
        // ReSharper disable once InconsistentNaming
        STL_Binary,

        /// <summary>
        /// Mobile MultiModal Framework
        /// </summary>
        ThreeMF,

        /// <summary>
        /// Mobile MultiModal Framework
        /// </summary>
        Model3MF,

        /// <summary>
        /// Wavefront 3D File Format
        /// </summary>
        OBJ,

        /// <summary>
        /// Additive Manufacturing File Format
        /// </summary>
        AMF,

        /// <summary>
        /// Object File Format
        /// </summary>
        OFF,

        /// <summary>
        /// Polygon File Format as ASCII
        /// </summary>
        PLY_ASCII,

        /// <summary>
        /// Polygon File Format as Binary
        /// </summary>
        PLY_Binary,

        /// <summary>
        /// Shell file...I think this was created as part of collaboration with an Oregon-based EDA company
        /// </summary>
        SHELL,

        /// <summary>
        /// A human-readable, serialized version of TVGL Solid objects
        /// </summary>
        TVGL,

        /// <summary>
        /// A compressed version of TVGL Solid objects. About 4X smaller than TVGL.
        /// </summary>
        TVGLz
    }

    /// <summary>
    /// Enum FormatEndiannessType
    /// </summary>
    internal enum FormatEndiannessType
    {
        /// <summary>
        /// The ASCII
        /// </summary>
        ascii,
        /// <summary>
        /// The binary little endian
        /// </summary>
        binary_little_endian,
        /// <summary>
        /// The binary big endian
        /// </summary>
        binary_big_endian
    }

    /// <summary>
    /// Enum ShapeElement
    /// </summary>
    internal enum ShapeElement
    {
        /// <summary>
        /// The vertex
        /// </summary>
        Vertex,

        /// <summary>
        /// The edge
        /// </summary>
        Edge,
        /// <summary>
        /// The face
        /// </summary>
        Face,
        /// <summary>
        /// The uniform color
        /// </summary>
        Uniform_Color
    }

    /// <summary>
    /// Enum ColorElements
    /// </summary>
    internal enum ColorElements
    {
        /// <summary>
        /// The red
        /// </summary>
        Red,
        /// <summary>
        /// The green
        /// </summary>
        Green,
        /// <summary>
        /// The blue
        /// </summary>
        Blue,
        /// <summary>
        /// The opacity
        /// </summary>
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
        XNegative = -1,

        /// <summary>
        /// Negative Y Direction
        /// <summary>
        /// The x negative
        /// </summary>
        /// </summary>
        YNegative = -2,

        /// <summary>
        /// Negative Z Direction
        /// <summary>
        /// The y negative
        /// </summary>
        /// </summary>
        ZNegative = -3,

        /// <summary>
        /// Positive X Direction
        /// <summary>
        /// The z negative
        /// </summary>
        /// </summary>
        XPositive = 1,

        /// <summary>
        /// Positive Y Direction
        /// <summary>
        /// The x positive
        /// </summary>
        /// </summary>
        YPositive = 2,

        /// <summary>
        /// Positive Z Direction
        /// <summary>
        /// The y positive
        /// </summary>
        /// </summary>
        ZPositive = 3
    }

    /// <summary>
    /// Enum VerticalLineReferenceType
    /// </summary>
    internal enum VerticalLineReferenceType
    {
        /// <summary>
        /// The above
        /// </summary>
        Above,
        /// <summary>
        /// The on
        /// </summary>
        On,
        /// <summary>
        /// The below
        /// </summary>
        Below,
        /// <summary>
        /// The not intersecting
        /// </summary>
        NotIntersecting
    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>
    [Flags]
    internal enum PolyRelInternal
    {
        /// <summary>
        /// The separated
        /// </summary>
        Separated = 0,
        /// <summary>
        /// The edges cross
        /// </summary>
        EdgesCross = 1,
        /// <summary>
        /// The coincident edges
        /// </summary>
        CoincidentEdges = 2,
        /// <summary>
        /// The coincident vertices
        /// </summary>
        CoincidentVertices = 4,
        /// <summary>
        /// The inside hole
        /// </summary>
        InsideHole = 8,
        /// <summary>
        /// a inside b
        /// </summary>
        AInsideB = 16,
        /// <summary>
        /// The b inside a
        /// </summary>
        BInsideA = 32,
        /// <summary>
        /// The intersection
        /// </summary>
        Intersection = AInsideB | BInsideA,
        /// <summary>
        /// The equal
        /// </summary>
        Equal = 64,
        /// <summary>
        /// The equal but opposite
        /// </summary>
        EqualButOpposite = 128
    }

    /// <summary>
    /// Enum PolygonRelationship
    /// </summary>

    public enum ABRelationships
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

    /// <summary>
    /// Enum SegmentRelationship
    /// </summary>
    public enum SegmentRelationship
    {
        /// <summary>
        /// The no overlap
        /// </summary>
        NoOverlap,
        /// <summary>
        /// The abutting
        /// </summary>
        Abutting,
        /// <summary>
        /// The double overlap
        /// </summary>
        DoubleOverlap,
        /// <summary>
        /// The b encloses a
        /// </summary>
        BEnclosesA,
        /// <summary>
        /// a encloses b
        /// </summary>
        AEnclosesB,
        /// <summary>
        /// The cross over b outside after
        /// </summary>
        CrossOver_BOutsideAfter,
        /// <summary>
        /// The cross over a outside after
        /// </summary>
        CrossOver_AOutsideAfter,
        /// <summary>
        /// The two edges are identical!
        /// </summary>
        Equal,
    }

    /// <summary>
    /// Enum CollinearityTypes
    /// </summary>
    public enum CollinearityTypes
    {
        /// <summary>
        /// The none
        /// </summary>
        None,
        /// <summary>
        /// The both same direction
        /// </summary>
        Same,
        /// <summary>
        /// The both opposite direction
        /// </summary>
        Opposite,
        /// <summary>
        /// The after
        /// </summary>
        //After,
        /// <summary>
        /// The before
        /// </summary>
        //Before,
        /// <summary>
        /// a after b before
        /// </summary>
        //AAfterBBefore, // case 14
        /// <summary>
        /// a before b after
        /// </summary>
        //ABeforeBAfter
    }

    /// <summary>
    /// Enum WhereIsIntersection
    /// </summary>
    public enum WhereIsIntersection
    {
        /// <summary>
        /// The intermediate
        /// </summary>
        Intermediate,
        /// <summary>
        /// At start of a
        /// </summary>
        AtStartOfA,
        /// <summary>
        /// At start of b
        /// </summary>
        AtStartOfB,
        /// <summary>
        /// The both starts
        /// </summary>
        BothStarts
    }
    /// <summary>
    /// Enum BooleanOperationType
    /// </summary>
    public enum BooleanOperationType
    {
        /// <summary>
        /// The union
        /// </summary>
        Union,
        /// <summary>
        /// The intersect
        /// </summary>
        Intersect,
        /// <summary>
        /// The subtract ab
        /// </summary>
        SubtractAB,
        /// <summary>
        /// The subtract ba
        /// </summary>
        SubtractBA,
        /// <summary>
        /// The xor
        /// </summary>
        XOR
    }

}