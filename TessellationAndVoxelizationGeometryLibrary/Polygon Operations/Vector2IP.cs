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
        internal static long InitialW = 45720000;
        internal Int128 X { get; init; }
        internal Int128 Y { get; init; }
        internal Int128 W { get; init; }

        internal Vector2IP(Vector2 vector)
        => new Vector2IP(vector.X, vector.Y, InitialW);
        internal Vector2IP(double x, double y)
        => new Vector2IP(x, y, InitialW);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Vector2IP(double x, double y, Int128 w)
       => new Vector2IP((Int128)x * w, (Int128)y * w, w);
        internal Vector2IP(Int128 x, Int128 y, Int128 w)
        {
            X = x;
            Y = y;
            W = w;
        }

        public static Vector2IP Zero = default;
        public static Vector2IP UnitX = new Vector2IP(Int128.One, Int128.Zero, Int128.One);
        public static Vector2IP UnitY = new Vector2IP(Int128.Zero, Int128.One, Int128.One);

        internal bool IsNull()
        {
            return this == Zero;
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
        public Vector2IP Cross(Vector2IP that)
        {
            return new Vector2IP(this.Y * that.W - this.W * that.Y,
                this.W * that.X - this.X * that.W,
                this.X * that.Y - this.Y * that.X);
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



        #region Public Static Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP operator +(Vector2IP left, Vector2IP right)
        {
            if (left.W == right.W)
                return new Vector2IP(left.X + right.X, left.Y + right.Y, left.W);
            return new Vector2IP(left.X * right.W + right.X * left.W,
                left.Y * right.W + right.Y * left.W, left.W * right.W);
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2IP operator -(Vector2IP left, Vector2IP right)
        {
            if (left.W == right.W)
                return new Vector2IP(left.X - right.X, left.Y - right.Y, left.W);
            return new Vector2IP(left.X * right.W - right.X * left.W,
                left.Y * right.W - right.Y * left.W, left.W * right.W);
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