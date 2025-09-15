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
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    internal readonly struct RationalIP : IEquatable<RationalIP>, IComparable<RationalIP>
    {
        internal const int MaxToIntFactor = 45720000; // this is (2^6)*(3^2)*(5^4)*127 = 45720000
                                                      // why this number? see my reasoning here: https://github.com/DesignEngrLab/TVGL/wiki/Determining-the-Double-to-Long-Dimension-Multiplier
        internal const double toIntError = 1e-7; // can't be less than reciprocal of MaxToIntFactor (2.18e-8)

        internal Int128 Num { get; init; }
        internal Int128 Den { get; init; }

        internal RationalIP(Int128 r, Int128 w)
        {
            Num = r;
            Den = w;
        }

        public RationalIP(double r, long w) : this()
        {
            Num = (Int128)(r * w);
            Den = w;
        }

        /// <summary>
        /// Make the real number into a rational. This is the classic Diophantine problem.
        /// To solve well, we should check all integers, but that'll take a while
        /// and we would want to stop at our limit define as MaxToIntFactor. So, knowing the
        /// prime factorization of MaxToIntFactor, we solve this with some conditions.
        /// </summary>
        /// <param name="r"></param>
        public RationalIP(double r)
        {
            if (Math.Abs(r - (int)r) < toIntError)
            {   // if already an integer, then just return value with a denominator of 1
                Num = (int)r;
                Den = 1;
            }
            else if (Math.Abs(127 * r - (int)(127 * r)) < toIntError)
            {   // 127?! is weird but a common conversion from millimeter to inch
                Num = (int)(127 * r);
                Den = 127;
            }
            else if (Math.Abs(3 * r - (int)(3 * r)) < toIntError)
            {   // also check 1/3's which may happen often enough
                Num = (int)(3 * r);
                Den = 3;
            }
            else
            {   // now we do the detective work to find what reductions can be 
                // made to the MaxToIntFactor
                var numLong = (long)(r * MaxToIntFactor);
                var num2s = Math.Min(6, long.TrailingZeroCount(numLong)); // since MaxToIntFactor 
                // has 6 two's in it, we can reduce by upto 32
                var pStr = Num.ToString(); //assuming the fives are paired with 2's then we'd have leading zeros
                var num5s = Math.Min(4, pStr.Length - pStr.TrimEnd('0').Length);
                var reductionFactor = (int)(Math.Pow(2, num2s) * Math.Pow(5, num5s));
                Num = numLong / reductionFactor;
                Den = MaxToIntFactor / reductionFactor;
                var possible5s = num5s - num2s;
                for (int i = 0; i < possible5s; i++)
                {
                    if (Num % 5 == 0)
                    {
                        Num /= 5;
                        Den /= 5;
                    }
                    else break;
                }
                if (Num % 3 == 0)
                {
                    Num /= 3;
                    Den /= 3;
                    if (Num % 3 == 0)
                    {
                        Num /= 3;
                        Den /= 3;
                    }
                }
                if (Num % 127 == 0)
                {
                    Num /= 127;
                    Den /= 127;
                }
            }
        }

        internal double AsDouble => AsDoubleValue(Num, Den);

        internal static double AsDoubleValue(Int128 num, Int128 den)
        {
            if (num == Int128.Zero) return 0.0;
            (Int128 quotient, Int128 remainder) = Int128.DivRem(num, den);
            // to increase precision, we add the remainder divided by the denominator
            if (remainder == 0) return (double)quotient;
            return (double)quotient + ((double)remainder / (double)den);
        }

        internal Int128 AsInt128 => Num / Den;
        public RationalIP SquareRoot() => new RationalIP(Num.SquareRoot(), Den.SquareRoot());

        public static RationalIP One = new RationalIP(1, 1);

        public static RationalIP Zero = default;
        public static RationalIP PositiveInfinity = new RationalIP(Int128.MaxValue, Int128.Zero);
        public static RationalIP NegativeInfinity = new RationalIP(Int128.MinValue, Int128.Zero);
        internal bool IsNull()
        {
            return this == Zero;
        }

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


        public static RationalIP operator -(Int128 left, RationalIP right)
       => Subtract(left * right.Den, right.Den, right.Num, right.Den);


        public static RationalIP operator *(RationalIP left, RationalIP right)
       => Multiply(left.Num, left.Den, right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static RationalIP Multiply(Int128 leftNum, Int128 leftDen, Int128 rightNum, Int128 rightDen)
        => new RationalIP(leftNum * rightNum, leftDen * rightDen);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RationalIP operator *(Int128 left, RationalIP right)
       => new RationalIP(left * right.Num, right.Den);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RationalIP operator *(RationalIP left, Int128 right)
       => new RationalIP(left.Num * right, left.Den);

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
            if (leftDen == 0)
            {
                var lNumSign = Int128.Sign(leftNum);
                if (lNumSign != 0) return lNumSign;
                else // then left is zero
                {
                    var rNumSign = Int128.Sign(rightNum);
                    if (rNumSign == 0) return 0;
                    return (rNumSign == Int128.Sign(rightDen)) ? 1 : -1;
                }
            }
            if (rightDen == 0)
            {
                var rNumSign = Int128.Sign(rightNum);
                if (rNumSign != 0) return rNumSign;
                else // then left is zero
                {
                    var lNumSign = Int128.Sign(leftNum);
                    if (lNumSign == 0) return 0;
                    return (lNumSign == Int128.Sign(leftDen)) ? 1 : -1;
                }
            }
            var left = leftNum * rightDen;
            var right = rightNum * leftDen;
            if (left == right) return 0;
            else if (left > right) return 1;
            else return -1;
        }

        internal bool IsLessThanVectorX(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.X, coordinates.W) < 0;
        internal static bool IsLessThanVectorX(Vector2IP left, Vector2IP right)
        => CompareTo(left.X, left.W, right.X, right.W) < 0;
        internal bool IsLessThanVectorY(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.Y, coordinates.W) < 0;
        internal static bool IsLessThanVectorY(Vector2IP left, Vector2IP right)
        => CompareTo(left.Y, left.W, right.Y, right.W) < 0;
        internal bool IsGreaterThanVectorX(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.X, coordinates.W) > 0;
        internal static bool IsGreaterThanVectorX(Vector2IP left, Vector2IP right)
        => CompareTo(left.X, left.W, right.X, right.W) > 0;
        internal bool IsGreaterThanVectorY(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.Y, coordinates.W) > 0;
        internal static bool IsGreaterThanVectorY(Vector2IP left, Vector2IP right)
        => CompareTo(left.Y, left.W, right.Y, right.W) > 0;

        internal bool IsEqualVectorX(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.X, coordinates.W) == 0;
        internal static bool IsEqualVectorX(Vector2IP left, Vector2IP right)
        => CompareTo(left.X, left.W, right.X, right.W) == 0;
        internal bool IsEqualVectorY(Vector2IP coordinates)
        => CompareTo(Num, Den, coordinates.Y, coordinates.W) == 0;
        internal static bool IsEqualVectorY(Vector2IP left, Vector2IP right)
        => CompareTo(left.Y, left.W, right.Y, right.W) == 0;

        internal static bool IsInfinity(RationalIP r)
        {
            return r.Den == Int128.Zero;
        }


        internal int Sign()
        {
            if (Num == Int128.Zero) return 0;
            if (Den == Int128.Zero) return Int128.Sign(Num);
            return Int128.IsPositive(Num) == Int128.IsPositive(Den) ? 1 : -1;
        }

        internal static RationalIP Abs(RationalIP rationalIP)
        {
            return new RationalIP(Int128.Abs(rationalIP.Num), Int128.Abs(rationalIP.Den));
        }

        #endregion
    }
}