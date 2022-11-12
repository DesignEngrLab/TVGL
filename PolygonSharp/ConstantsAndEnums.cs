using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PolygonSharp
{
    internal static class Constants
    {
        internal const int realToLongScale = 2 * 2 * 2 * 3 * 5 * 5 * 5 * 127;
        internal const float longToRealScale = 1f / realToLongScale;
        internal const float TwoPi = 2 * MathF.PI;
        internal const float HalfPi = MathF.PI / 2;
        internal const double HighConfidence = 0.997;
        internal static float coordinateMaxValue = longToRealScale * MathF.Pow(2, 31.5f); //7,971;
        internal static Vector2 NullVector = new Vector2(float.NaN);

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

    internal enum PolygonRelationship
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

    internal enum SegmentRelationship
    {
        NoOverlap,
        Abutting,
        DoubleOverlap,
        BEnclosesA,
        AEnclosesB,
        CrossOver_BOutsideAfter,
        CrossOver_AOutsideAfter,
    }

    internal enum CollinearityTypes
    {
        None,
        BothSameDirection,
        BothOppositeDirection,
        After,
        Before,
        AAfterBBefore, // case 14
        ABeforeBAfter
    }

    internal enum WhereIsIntersection
    {
        Intermediate,
        AtStartOfA,
        AtStartOfB,
        BothStarts
    }



    /// <summary>
    /// Enum PolygonCollection
    /// </summary>
    internal enum PolygonCollection
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
    internal enum ResultType
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
    ///     A comparer for optimization that can be used for either
    ///     ascending or descending.
    /// </summary>
    internal class NoEqualSort : IComparer<double>
    {
        private readonly int direction;

        /// <summary>
        /// Initializes a new instance of the <see cref="NoEqualSort"/> class.
        /// </summary>
        /// <param name="ascendingOrder">if set to <c>true</c> [ascending order].</param>
        internal NoEqualSort(bool ascendingOrder = true)
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
        internal int Compare(double x, double y)
        {
            if (x < y) return direction;
            return -direction;
        }

        int IComparer<double>.Compare(double x, double y)
        {
            if (x < y) return direction;
            return -direction;
        }
    }
}
