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
    /// <summary>
    /// A structure encapsulating three extralong integer as a 2D coordinate.
    /// IP stands for Integer Projective.
    /// </summary>
    internal readonly struct Vector2IP : IEquatable<Vector2IP>, IFormattable
    {
        internal Int128 X { get; init; }
        internal Int128 Y { get; init; }
        internal Int128 W { get; init; }
        public Vector2 AsVector2 => new Vector2(RationalIP.AsDoubleValue(X, W),
            RationalIP.AsDoubleValue(Y, W));

        internal Vector2IP(Vector2 vector) : this(vector.X, vector.Y) { }
        internal Vector2IP(double x, double y)
        {
            if (Math.Abs(x - (int)x) < RationalIP.toIntError && Math.Abs(y - (int)y) < RationalIP.toIntError)
            {   // if already an integer, then just return value with a denominator of 1
                X = (int)x;
                Y = (int)y;
                W = Int128.One;
            }
            else if (Math.Abs(127 * x - (int)(127 * x)) < RationalIP.toIntError &&
                Math.Abs(127 * y - (int)(127 * y)) < RationalIP.toIntError)
            {   // 127?! is weird but a common conversion from millimeter to inch
                X = (int)(127 * x);
                X = (int)(127 * x);
                W = 127;
            }
            else if (Math.Abs(3 * x - (int)(3 * x)) < RationalIP.toIntError &&
                Math.Abs(3 * y - (int)(3 * y)) < RationalIP.toIntError)
            {   // 127?! is weird but a common conversion from millimeter to inch
                X = (int)(3 * x);
                X = (int)(3 * x);
                W = 3;
            }
            else
            {   // now we do the detective work to find what reductions can be 
                // made to the MaxToIntFactor
                var longX = (long)(x * RationalIP.MaxToIntFactor);
                var longY = (long)(y * RationalIP.MaxToIntFactor);
                var num2s = Math.Min(6, Math.Min(long.TrailingZeroCount(longX), long.TrailingZeroCount(longY)));
                // since MaxToIntFactor has 6 two's in it, we can reduce by up to 32
                var xStr = longX.ToString();
                var yStr = longY.ToString(); //assuming the fives are paired with 2's then we'd have leading zeros
                var num5s = Math.Min(4, Math.Min(xStr.Length - xStr.TrimEnd('0').Length, yStr.Length - yStr.TrimEnd('0').Length));
                var reductionFactor = (int)(Math.Pow(2, num2s) * Math.Pow(5, num5s));
                X = longX / reductionFactor;
                Y = longY / reductionFactor;
                W = RationalIP.MaxToIntFactor / reductionFactor;
                var possible5s = num5s - num2s;
                for (int i = 0; i < possible5s; i++)
                {
                    if (X % 5 == 0 && Y % 5 == 0)
                    {
                        X /= 5;
                        Y /= 5;
                        W /= 5;
                    }
                    else break;
                }
                if (X % 3 == 0 && Y % 3 == 0)
                {
                    X /= 3;
                    Y /= 3;
                    W /= 3;
                    if (X % 3 == 0 && Y % 3 == 0)
                    {
                        X /= 3;
                        Y /= 3;
                        W /= 3;
                    }
                }
                if (X % 127 == 0 && Y % 127 == 0)
                {
                    X /= 127;
                    Y /= 127;
                    W /= 127;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector2IP(double x, double y, long w) : this((Int128)(x * w), (Int128)(y * w), w) { }
        internal Vector2IP(Int128 x, Int128 y, Int128 w)
        {
            X = x;
            Y = y;
            W = w;
        }

        public static Vector2IP Zero = default;
        public static Vector2IP UnitX = new Vector2IP(Int128.One, Int128.Zero, Int128.One);
        public static Vector2IP UnitY = new Vector2IP(Int128.Zero, Int128.One, Int128.One);

        public Vector2 AsNormalizedVector2()
        {
            var length = (X * X + Y * Y).SquareRoot();
            return new Vector2(RationalIP.AsDoubleValue(X, length),
                RationalIP.AsDoubleValue(Y, length));
        }
        internal bool IsNull()
        {
            return W == 0 && Y == 0 && X == 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RationalIP Dot2D(Vector2IP that)
        {
            return new RationalIP(this.X * that.X + this.Y * that.Y, this.W * that.W);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Int128 Dot3D(Vector2IP that)
        {
            return this.X * that.X + this.Y * that.Y + this.W * that.W;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2IP Cross3D(Vector2IP that)
        {
            return new Vector2IP(this.Y * that.W - this.W * that.Y,
                this.W * that.X - this.X * that.W,
                this.X * that.Y - this.Y * that.X);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public RationalIP Cross2D(Vector2IP that)
        {
            return new RationalIP(this.X * that.Y - this.Y * that.X,
                this.W * that.W);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int CrossSign(Vector2IP that)
        {
            var thisXThatY = this.X * that.Y;
            var thisYThatX = this.Y * that.X;
            if (thisXThatY == thisYThatX)
                return 0; // collinear
            var numeratorSign = (thisXThatY > thisYThatX) ? 1 : -1;
            if (this.W == 0 || that.W == 0)
                return numeratorSign;
            var denominatorSign = Int128.Sign(this.W) == Int128.Sign(that.W) ? 1 : -1;
            if (numeratorSign == denominatorSign)
                return 1;
            return -1;
        }

        internal Vector2IP Transform(Matrix3x3 matrix)
        {
            throw new NotImplementedException();
            //if (matrix)
            //return
            //new Vector2IP
            //((Int128)(X * matrix.M11 + Y * matrix.M21 + W*matrix.M31),
            //    X * matrix.M12 + Y * matrix.M22 + W*matrix.M32,
            //    X * matrix.M13 + Y * matrix.M23 + W*matrix.M33);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            throw new NotImplementedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static RationalIP DistanceSquared2D(Vector2IP left, Vector2IP right)
        {
            if (left.W == right.W)
                return new RationalIP((left.X - right.X) * (left.X - right.X) + (left.Y - right.Y) * (left.Y - right.Y),
                             left.W * left.W);
            else
                return new RationalIP((left.X * right.W - right.X * left.W) * (left.X * right.W - right.X * left.W) +
                               (left.Y * right.W - right.Y * left.W) * (left.Y * right.W - right.Y * left.W),
                              left.W * left.W * right.W * right.W);
        }

        internal Int128 Length3D()
        {
            return LengthSquared3D().SquareRoot();
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Int128 LengthSquared3D()
        {
            return X * X + Y * Y + W * W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal RationalIP LengthSquared2D()
        {
            return new RationalIP(X * X + Y * Y, W * W);
        }



        #region Public Static Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP Add2D(Vector2IP left, Vector2IP right)
        {
            if (left.W == right.W)
                return new Vector2IP(left.X + right.X, left.Y + right.Y, left.W);
            return new Vector2IP(left.X * right.W + right.X * left.W,
                left.Y * right.W + right.Y * left.W, left.W * right.W);
        }

        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP Add3D(Vector2IP left, Vector2IP right)
        {
            return new Vector2IP(left.X + right.X, left.Y + right.Y, left.W + right.W);
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP Minus2D(Vector2IP left, Vector2IP right)
        {
            if (left.W == right.W)
                return new Vector2IP(left.X - right.X, left.Y - right.Y, left.W);
            return new Vector2IP(left.X * right.W - right.X * left.W,
                left.Y * right.W - right.Y * left.W, left.W * right.W);
        }


        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP Minus3D(Vector2IP left, Vector2IP right)
        {
            return new Vector2IP(left.X - right.X, left.Y - right.Y, left.W - right.W);
        }


        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP operator -(Vector2IP value)
        {
            return new Vector2IP(-value.X, -value.Y, value.W);
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are equal; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2IP left, Vector2IP right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given vectors are not equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are not equal; False if they are equal.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2IP left, Vector2IP right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2IP && Equals((Vector2IP)obj);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2IP that)
        {
            if (this.IsNull() && that.IsNull()) return true;
            if (this.IsNull() || that.IsNull()) return false;

            if (this.W == 0 && that.W == 0)
                return this.X * that.Y == that.X * this.Y;
            if (this.W == that.W)
                return this.X == that.X && this.Y == that.Y;
            return this.X * that.W == that.X * this.W &&
                this.Y * that.W == that.Y * this.W;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector2IP MidPoint(Vector2IP left, Vector2IP right)
        {
            return new Vector2IP(left.X * right.W + right.X * left.W,
                left.Y * right.W + right.Y * left.W, 2 * left.W * right.W);
        }
        #endregion Public Static Operators
    }
}