// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using MIConvexHull;
using Newtonsoft.Json;

namespace TVGL.Numerics  // COMMENTEDCHANGE namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating three single precision floating point values and provides hardware accelerated methods.
    /// </summary>
    [Intrinsic]
    public readonly partial struct Vector3 : IEquatable<Vector3>, IFormattable, IVertex3D, IVertex
    {

        /// <summary>
        /// The X component of the vector.
        /// </summary>
        public double X { get; }
        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        public double Y { get; }
        /// <summary>
        /// The Z component of the vector.
        /// </summary>
        public double Z { get; }

        #region Constructors
        /// <summary>
        /// Constructs a vector whose elements are all the single specified value.
        /// </summary>
        /// <param name="value">The element to fill the vector with.</param>
        [Intrinsic]
        internal Vector3(double value) : this(value, value, value) { }


        /// <summary>
        /// Constructs a Vector3 from the given Vector2 and a third value.
        /// </summary>
        /// <param name="value">The Vector to extract X and Y components from.</param>
        /// <param name="z">The Z component.</param>
        [Intrinsic]
        public Vector3(Vector2 value, double z) : this(value.X, value.Y, z) { }
        public Vector3(Vector3 value) : this(value.X, value.Y, value.Z) { }

        /// <summary>
        /// Constructs a vector with the given individual elements.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        /// <param name="z">The Z component.</param>
        [Intrinsic]
        public Vector3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3(double[] d) : this()
        {
            X = d[0];
            Y = d[1];
            Z = d[2];
        }
        //public Vector3(Vector4 vector4) : this()
        //{
        //    var multiplier = 1 / vector4.W;
        //    X = vector4.X * multiplier;
        //    Y = vector4.Y * multiplier;
        //    Z = vector4.Z * multiplier;
        //}
        #endregion Constructors

        #region Public Instance Methods
        /// <summary>
        /// Copies the contents of the vector into the given array.
        /// </summary>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(double[] array)
        {
            CopyTo(array, 0);
        }



        /// <summary>
        /// Copies the contents of the vector into the given array, starting from index.
        /// </summary>
        /// <exception cref="ArgumentNullException">If array is null.</exception>
        /// <exception cref="RankException">If array is multidimensional.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero.</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array.</exception>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(double[] array, int index)
        {
            if (array == null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(); // COMMENTEDCHANGE SR.Arg_NullArgumentNullRef);
            }
            if (index < 0 || index >= array.Length)
            {
                throw new ArgumentOutOfRangeException(); // COMMENTEDCHANGE nameof(index), SR.Format(SR.Arg_ArgumentOutOfRangeException, index));
            }
            if ((array.Length - index) < 3)
            {
                throw new ArgumentException(); // COMMENTEDCHANGE SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, index));
            }
            array[index] = X;
            array[index + 1] = Y;
            array[index + 2] = Z;
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Vector3 is equal to this Vector3 instance.
        /// </summary>
        /// <param name="other">The Vector3 to compare this instance to.</param>
        /// <returns>True if the other Vector3 is equal to this instance; False otherwise.</returns>
        [Intrinsic]
        public bool Equals(Vector3 other)
        {
            return X == other.X &&
                   Y == other.Y &&
                   Z == other.Z;
        }
        #endregion Public Instance Methods

        #region Public Static Methods
        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector3 vector1, Vector3 vector2)
        {
            return vector1.X * vector2.X +
                   vector1.Y * vector2.Y +
                   vector1.Z * vector2.Z;
        }

        /// <summary>
        /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <returns>The minimized vector.</returns>
        [Intrinsic]
        public static Vector3 Min(Vector3 value1, Vector3 value2)
        {
            return new Vector3(
                (value1.X < value2.X) ? value1.X : value2.X,
                (value1.Y < value2.Y) ? value1.Y : value2.Y,
                (value1.Z < value2.Z) ? value1.Z : value2.Z);
        }

        /// <summary>
        /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <returns>The maximized vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Max(Vector3 value1, Vector3 value2)
        {
            return new Vector3(
                (value1.X > value2.X) ? value1.X : value2.X,
                (value1.Y > value2.Y) ? value1.Y : value2.Y,
                (value1.Z > value2.Z) ? value1.Z : value2.Z);
        }

        /// <summary>
        /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The absolute value vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Abs(Vector3 value)
        {
            return new Vector3(Math.Abs(value.X), Math.Abs(value.Y), Math.Abs(value.Z));
        }

        /// <summary>
        /// Returns a vector whose elements are the square root of each of the source vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The square root vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 SquareRoot(Vector3 value)
        {
            return new Vector3(Math.Sqrt(value.X), Math.Sqrt(value.Y), Math.Sqrt(value.Z));
        }
        #endregion Public Static Methods

        #region Public Static Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator +(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }

        /// <summary>
        /// Multiplies two vectors together. This produces a new Vector3 where the components are
        /// x_1 * x_2, y_1 * y_2, and z_1 * z_2
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X * right.X, left.Y * right.Y, left.Z * right.Z);
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(Vector3 left, double right)
        {
            return left * new Vector3(right);
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator *(double left, Vector3 right)
        {
            return new Vector3(left) * right;
        }

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 left, Vector3 right)
        {
            return new Vector3(left.X / right.X, left.Y / right.Y, left.Z / right.Z);
        }

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator /(Vector3 value1, double value2)
        {
            return value1 / new Vector3(value2);
        }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 operator -(Vector3 value)
        {
            return Zero - value;
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are equal; False otherwise.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector3 left, Vector3 right)
        {
            return (left.X == right.X &&
                    left.Y == right.Y &&
                    left.Z == right.Z);
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given vectors are not equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are not equal; False if they are equal.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector3 left, Vector3 right)
        {
            return (left.X != right.X ||
                    left.Y != right.Y ||
                    left.Z != right.Z);
        }
        #endregion Public Static Operators


        #region Public Static Properties
        /// <summary>
        /// Returns the vector (0,0,0).
        /// </summary>
        [Intrinsic]
        public static Vector3 Zero =>
        default;

        /// <summary>
        /// Returns the vector (1,1,1).
        /// </summary>
        [Intrinsic]
        public static Vector3 One =>
        new Vector3(1.0, 1.0, 1.0);

        /// <summary>
        /// Returns the vector (NaN, NaN, NaN).
        /// </summary>
        [Intrinsic]
        public static Vector3 Null =>
        new Vector3(double.NaN, double.NaN, double.NaN);

        public bool IsNull()
        {
            return double.IsNaN(X) || double.IsNaN(Y) || double.IsNaN(Z);
        }
        /// <summary>
        /// Makes a copy of the current Vector.
        /// </summary>
        public Vector3 Copy()
        {
            return new Vector3(X, Y, Z);
        }

        /// <summary>
        /// Returns the vector (1,0,0).
        /// </summary>
        public static Vector3 UnitX => new Vector3(1.0, 0.0, 0.0);

        /// <summary>
        /// Returns the vector (0,1,0).
        /// </summary>
        public static Vector3 UnitY => new Vector3(0.0, 1.0, 0.0);

        /// <summary>
        /// Returns the vector (0,0,1).
        /// </summary>
        public static Vector3 UnitZ => new Vector3(0.0, 0.0, 1.0);

        public static Vector3 UnitVector(CartesianDirections direction)
        {
            switch (direction)
            {
                case CartesianDirections.XNegative: return new Vector3(-1, 0, 0);
                case CartesianDirections.XPositive: return new Vector3(1, 0, 0);
                case CartesianDirections.YNegative: return new Vector3(0, -1, 0);
                case CartesianDirections.YPositive: return new Vector3(0, 1, 0);
                case CartesianDirections.ZNegative: return new Vector3(0, 0, -1);
                default: return new Vector3(0, 0, 1);
            }
        }
        public static Vector3 UnitVector(int direction)
        {
            if (direction == 0) return new Vector3(1, 0, 0);
            if (direction == 1) return new Vector3(0, 1, 0);
            return new Vector3(0, 0, 1);
        }

        public double this[int i]
        {
            get
            {
                if (i == 0) return X;
                else if (i == 1) return Y;
                else return Z;
            }
        }

        [JsonIgnore]
        public double[] Position => new[] { X, Y, Z };

        #endregion Public Static Properties

        #region Public Instance Methods

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.X.GetHashCode(), this.Y.GetHashCode(), this.Z.GetHashCode());
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this Vector3 instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this Vector3; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Vector3))
                return false;
            return Equals((Vector3)obj);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given vector is aligned or exactly in the opposite direction.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal or opposite to this Vector3; False otherwise.</returns>
        public bool IsAlignedOrReverse(Vector3 other, double dotTolerance = Constants.SameFaceNormalDotTolerance)
        {
            //Perform a quick check to see if they are perfectly equal or opposite
            if (X == other.X && Y == other.Y && Z == other.Z) return true;
            if (X == -other.X && Y == -other.Y && Z == -other.Z) return true;
            // if the magnitude of the cross product is nearly zero than the two vectors are aligned
            var crossMagnitudeSquared = this.Cross(other).LengthSquared();
            crossMagnitudeSquared /= this.LengthSquared();
            crossMagnitudeSquared /= other.LengthSquared();
            return crossMagnitudeSquared.IsNegligible(dotTolerance * dotTolerance);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given vector is aligned or exactly in the opposite direction.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal or opposite to this Vector3; False otherwise.</returns>
        public bool IsAlignedOrReverse(Vector3 other, out bool isReverse, double dotTolerance = Constants.SameFaceNormalDotTolerance)
        {
            isReverse = this.Dot(other) < 0;
            //Perform a quick check to see if they are perfectly equal or opposite
            if (X == other.X && Y == other.Y && Z == other.Z) return true;
            if (X == -other.X && Y == -other.Y && Z == -other.Z) return true;
            // if the magnitude of the cross product is nearly zero than the two vectors are aligned
            var crossMagnitudeSquared = this.Cross(other).LengthSquared();
            crossMagnitudeSquared /= this.LengthSquared();
            crossMagnitudeSquared /= other.LengthSquared();
            return crossMagnitudeSquared.IsNegligible(dotTolerance * dotTolerance);
        }

        /// <summary>
        /// Determines whether the specified d2 is aligned.
        /// </summary>
        /// <param name="d1">The d1.</param>
        /// <param name="d2">The d2.</param>
        /// <param name="reverseIsDuplicate">if set to <c>true</c> [reverse is duplicate].</param>
        /// <param name="dotTolerance">The dot tolerance.</param>
        /// <returns><c>true</c> if the specified d2 is aligned; otherwise, <c>false</c>.</returns>
        public bool IsAligned(Vector3 other, double dotTolerance = Constants.SameFaceNormalDotTolerance)
        {
            if (X == other.X && Y == other.Y && Z == other.Z) return true;
            // but also need to check that they are in the same direction
            if (this.Dot(other) < 0) return false;
            // if the magnitude of the cross product is nearly zero than the two vectors are aligned
            var crossMagnitudeSquared = this.Cross(other).LengthSquared();
            crossMagnitudeSquared /= this.LengthSquared();
            crossMagnitudeSquared /= other.LengthSquared();
            return crossMagnitudeSquared.IsNegligible(dotTolerance * dotTolerance);
        }


        /// <summary>
        /// Returns a String representing this Vector3 instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return ToString("F3", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this Vector3 instance, using the specified format to format individual elements.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this Vector3 instance, using the specified format to format individual elements
        /// and the given IFormatProvider.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            //sb.Append('<');
            sb.Append(((IFormattable)this.X).ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(((IFormattable)this.Y).ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(((IFormattable)this.Z).ToString(format, formatProvider));
            //sb.Append('>');
            return sb.ToString();
        }

        /// <summary>
        /// Returns the length of the vector.
        /// </summary>
        /// <returns>The vector's length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            double ls = X * X + Y * Y + Z * Z;
            return Math.Sqrt(ls);
        }

        /// <summary>
        /// Returns the length of the vector squared. This operation is cheaper than Length().
        /// </summary>
        /// <returns>The vector's length squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSquared()
        {
            return X * X + Y * Y + Z * Z;

        }
        #endregion Public Instance Methods

        #region Public Static Methods
        /// <summary>
        /// Returns the Euclidean distance between the two given points. Note that for fast applications where the
        /// actual distance (but rather the relative distance) is not needed, consider using DistanceSquared.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector3 value1, Vector3 value2)
        {
            double dx = value1.X - value2.X;
            double dy = value1.Y - value2.Y;
            double dz = value1.Z - value2.Z;

            double ls = dx * dx + dy * dy + dz * dz;

            return Math.Sqrt(ls);
        }

        /// <summary>
        /// Returns the Euclidean distance squared between the two given points. This is useful when the actual
        /// value of distance is not so imporant as the relative value to other distances. Taking the square-root
        /// is expensive when called many times.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSquared(Vector3 value1, Vector3 value2)
        {
            double dx = value1.X - value2.X;
            double dy = value1.Y - value2.Y;
            double dz = value1.Z - value2.Z;

            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Normalize(Vector3 value)
        {
            double ls = value.X * value.X + value.Y * value.Y + value.Z * value.Z;
            if (ls.IsPracticallySame(1.0)) return value;
            double lengthfactor = 1 / Math.Sqrt(ls);
            return new Vector3(value.X * lengthfactor, value.Y * lengthfactor, value.Z * lengthfactor);
        }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Cross(in Vector3 vector1, Vector3 vector2)
        {
            return new Vector3(
                vector1.Y * vector2.Z - vector1.Z * vector2.Y,
                vector1.Z * vector2.X - vector1.X * vector2.Z,
                vector1.X * vector2.Y - vector1.Y * vector2.X);
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Reflect(in Vector3 vector, Vector3 normal)
        {
            double dot = vector.X * normal.X + vector.Y * normal.Y + vector.Z * normal.Z;
            double tempX = normal.X * dot * 2.0;
            double tempY = normal.Y * dot * 2.0;
            double tempZ = normal.Z * dot * 2.0;
            return new Vector3(vector.X - tempX, vector.Y - tempY, vector.Z - tempZ);
        }

        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The restricted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Clamp(Vector3 value1, Vector3 min, Vector3 max)
        {
            // This compare order is very important!!!
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            double x = value1.X;
            x = (min.X > x) ? min.X : x;  // max(x, minx)
            x = (max.X < x) ? max.X : x;  // min(x, maxx)

            double y = value1.Y;
            y = (min.Y > y) ? min.Y : y;  // max(y, miny)
            y = (max.Y < y) ? max.Y : y;  // min(y, maxy)

            double z = value1.Z;
            z = (min.Z > z) ? min.Z : z;  // max(z, minz)
            z = (max.Z < z) ? max.Z : z;  // min(z, maxz)

            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Linearly interpolates between two vectors based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Lerp(Vector3 value1, Vector3 value2, double amount)
        {
            var oppositeAmount = 1 - amount;
            return new Vector3(
             oppositeAmount * value1.X + value2.X * amount,
             oppositeAmount * value1.Y + value2.Y * amount,
             oppositeAmount * value1.Z + value2.Z * amount);
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 position, Matrix4x4 matrix)
        {
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (position.X * matrix.M14 + position.Y * matrix.M24 + position.Z * matrix.M34 + matrix.M44);
                return new Vector3(
                  factor * (position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41),
                  factor * (position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42),
                  factor * (position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43));
            }
            return new Vector3(
                position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31 + matrix.M41,
                position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32 + matrix.M42,
                position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33 + matrix.M43);
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 position, Matrix3x3 matrix)
        {
            return new Vector3(
                position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31,
                position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32,
                position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33);
        }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the inverse of that matrix.
        /// </summary>
        /// <param name="normal">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformNoTranslate(Vector3 normal, Matrix4x4 matrix)
        {
            return new Vector3(
                normal.X * matrix.M11 + normal.Y * matrix.M21 + normal.Z * matrix.M31,
                normal.X * matrix.M12 + normal.Y * matrix.M22 + normal.Z * matrix.M32,
                normal.X * matrix.M13 + normal.Y * matrix.M23 + normal.Z * matrix.M33);
        }

        /// <summary>
        /// Transforms a vector by the given Quaternion rotation value.
        /// </summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 value, Quaternion rotation)
        {
            double x2 = rotation.X + rotation.X;
            double y2 = rotation.Y + rotation.Y;
            double z2 = rotation.Z + rotation.Z;

            double wx2 = rotation.W * x2;
            double wy2 = rotation.W * y2;
            double wz2 = rotation.W * z2;
            double xx2 = rotation.X * x2;
            double xy2 = rotation.X * y2;
            double xz2 = rotation.X * z2;
            double yy2 = rotation.Y * y2;
            double yz2 = rotation.Y * z2;
            double zz2 = rotation.Z * z2;

            return new Vector3(
                value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2) + value.Z * (xz2 + wy2),
                value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2) + value.Z * (yz2 - wx2),
                value.X * (xz2 - wy2) + value.Y * (yz2 + wx2) + value.Z * (1.0 - xx2 - yy2));
        }
        #endregion Public Static Methods

        #region Public operator methods

        // All these methods should be inlined as they are implemented
        // over JIT intrinsics

        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Add(Vector3 left, Vector3 right)
        {
            return left + right;
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Subtract(Vector3 left, Vector3 right)
        {
            return left - right;
        }

        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 left, Vector3 right)
        {
            return left * right;
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(Vector3 left, double right)
        {
            return left * right;
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(double left, Vector3 right)
        {
            return left * right;
        }

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(Vector3 left, Vector3 right)
        {
            return left / right;
        }

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Divide(Vector3 left, double divisor)
        {
            return left / divisor;
        }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Negate(Vector3 value)
        {
            return -value;
        }
        #endregion
    }

    public interface IVertex3D
    {
        double X { get; }
        double Y { get; }
        double Z { get; }
    }

}
