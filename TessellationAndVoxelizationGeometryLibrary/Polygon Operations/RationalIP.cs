// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 12-10-2024
//
// Last Modified By : matth
// Last Modified On : 12-10-2024
// ***********************************************************************
// <copyright file="Vector2.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Runtime.CompilerServices;

namespace TVGL
{
    internal readonly struct RationalIP : IEquatable<RationalIP>, IComparable<RationalIP>
    {
        internal Int128 Num { get; init; }
        internal Int128 Den { get; init; }

        internal RationalIP(Int128 y, Int128 w)
        {
            Num = y;
            Den = w;
        }
        internal double AsDouble => (double)Num / (double)Den;


        #region internal Static Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        public static RationalIP operator +(RationalIP left, RationalIP right)
       => Add(left.Num, left.Den, right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RationalIP Add(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        {
            if (leftDen == rightDen)
                return new RationalIP(leftNum + rightNum, leftDen);
            return new RationalIP(leftNum * rightDen + rightNum * leftDen, leftDen * rightDen);
        }

        public static RationalIP operator -(RationalIP left, RationalIP right)
       => Subtract(left.Num, left.Den, right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RationalIP Subtract(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        {
            if (leftDen == rightDen)
                return new RationalIP(leftNum - rightNum, leftDen);
            return new RationalIP(leftNum * rightDen - rightNum * leftDen, leftDen * rightDen);
        }

        public static RationalIP operator *(RationalIP left, RationalIP right)
       => Multiply(left.Num, left.Den, right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RationalIP Multiply(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        => new RationalIP(leftNum * rightNum, leftDen * rightDen);

        public static RationalIP operator /(RationalIP left, RationalIP right)
       => Divide(left.Num, left.Den, right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RationalIP Divide(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
       => new RationalIP(leftNum * rightDen, leftDen * rightNum);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RationalIP operator -(RationalIP value)
        => new RationalIP(-value.Num, value.Den);

        public static bool operator ==(RationalIP left, RationalIP right)
        => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(RationalIP left, RationalIP right)
        => !left.Equals(right);

        public override bool Equals(object obj)
        => obj is RationalIP && Equals((RationalIP)obj);

        public bool Equals(RationalIP other)
        => Equals(Num, Den, other.Num, other.Den);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool Equals(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        {
            // we could call the CompareTo to avoid WETness, but the code is short and more performant this way
            if (leftDen == rightDen)
                return leftNum == rightNum;
            return leftNum * rightDen == rightNum * leftDen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(RationalIP left, RationalIP right)
            => CompareTo(left.Num, left.Den, right.Num, right.Den) >= 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(RationalIP left, RationalIP right)
            => CompareTo(left.Num, left.Den, right.Num, right.Den) > 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(RationalIP left, RationalIP right)
            => CompareTo(left.Num, left.Den, right.Num, right.Den) <= 0;
        public static bool operator <(RationalIP left, RationalIP right)
            => CompareTo(left.Num, left.Den, right.Num, right.Den) < 0;

        public int CompareTo(RationalIP that)
        => CompareTo(Num, Den, that.Num, that.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int CompareTo(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        {
            if (leftDen == rightDen)
            {
                if (leftNum == rightNum) return 0;
                else if (leftNum > rightNum) return 1;
                else return -1;
            }
            var left = leftNum * rightDen;
            var right = rightNum * leftDen;
            if (left == right) return 0;
            else if (left > right) return 1;
            else return -1;
        }
        #endregion
    }
}