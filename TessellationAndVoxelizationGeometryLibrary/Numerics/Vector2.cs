// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Vector2.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using TVGL.ConvexHull;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL  // COMMENTEDCHANGE namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating two single precision floating point values and provides hardware accelerated methods.
    /// </summary>
    public readonly partial struct Vector2 : IEquatable<Vector2>, IFormattable, IPoint2D, IPoint
    {
        #region Public Static Properties
        /// <summary>
        /// Returns the vector (NaN,NaN). This is often used in place of null.
        /// </summary>
        /// <value>The null.</value>
        public static Vector2 Null = new Vector2(double.NaN, double.NaN);

        /// <summary>
        /// Returns the vector (0,0).
        /// </summary>
        /// <value>The zero.</value>
        public static Vector2 Zero = default;

        /// <summary>
        /// Returns the vector (1,1).
        /// </summary>
        /// <value>The one.</value>
        public static Vector2 One = new Vector2(1.0, 1.0);

        /// <summary>
        /// Returns the vector (1,0).
        /// </summary>
        /// <value>The unit x.</value>
        public static Vector2 UnitX = new Vector2(1.0, 0.0);

        /// <summary>
        /// Returns the vector (0,1).
        /// </summary>
        /// <value>The unit y.</value>
        public static Vector2 UnitY = new Vector2(0.0, 1.0);
        #endregion

        #region Public Instance Properties
        /// <summary>
        /// Gets the x.
        /// </summary>
        /// <value>The x.</value>
        double IPoint2D.X => X;

        /// <summary>
        /// Gets the y.
        /// </summary>
        /// <value>The y.</value>
        double IPoint2D.Y => Y;

        /// <summary>
        /// Gets the <see cref="System.Double"/> with the specified i.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <returns>System.Double.</returns>
        public double this[int i]
        {
            get
            {
                if (i == 0) return X;
                else  return Y;
            }
        }


        /// <summary>
        /// The X component of the vector.
        /// </summary>
        /// <value>The x.</value>
        public double X { get; }
        /// <summary>
        /// The Y component of the vector.
        /// </summary>
        /// <value>The y.</value>
        public double Y { get; }

        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a vector whose elements are all the single specified value.
        /// </summary>
        /// <param name="value">The element to fill the vector with.</param>
        internal Vector2(double value) : this(value, value) { }

        /// <summary>
        /// Constructs a vector with the given individual elements.
        /// </summary>
        /// <param name="x">The X component.</param>
        /// <param name="y">The Y component.</param>
        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        /// Determines whether this instance is null.
        /// </summary>
        /// <returns><c>true</c> if this instance is null; otherwise, <c>false</c>.</returns>
        public bool IsNull()
        {
            return double.IsNaN(X) || double.IsNaN(Y);
        }
        #endregion Constructors

        #region Public Instance Methods
        /// <summary>
        /// Copies the contents of the vector into the given array.
        /// </summary>
        /// <param name="array">The destination array.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void CopyTo(double[] array)
        {
            CopyTo(array, 0);
        }

        /// <summary>
        /// Copies the contents of the vector into the given array, starting from the given index.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="index">The index.</param>
        /// <exception cref="System.NullReferenceException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// <exception cref="System.ArgumentException"></exception>
        /// <exception cref="ArgumentNullException">If array is null.</exception>
        /// <exception cref="RankException">If array is multidimensional.</exception>
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
            if ((array.Length - index) < 2)
            {
                throw new ArgumentException(); // COMMENTEDCHANGE SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, index));
            }
            array[index] = X;
            array[index + 1] = Y;
        }

        /// <summary>
        /// Makes a copy of the current Vector.
        /// </summary>
        /// <returns>Vector2.</returns>
        public Vector2 Copy()
        {
            return new Vector2(X, Y);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Vector2 is equal to this Vector2 instance.
        /// </summary>
        /// <param name="other">The Vector2 to compare this instance to.</param>
        /// <returns>True if the other Vector2 is equal to this instance; False otherwise.</returns>
        public bool Equals(Vector2 other)
        {
            return this.X == other.X && this.Y == other.Y;
        }
        #endregion Public Instance Methods

        #region Public Static Methods
        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector2 value1, Vector2 value2)
        {
            return value1.X * value2.X +
                   value1.Y * value2.Y;
        }

        /// <summary>
        /// Returns the z-value of the cross product of two vectors.
        /// Since the Vector2 is in the x-y plane, a 3D cross product
        /// only produces the z-value
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The value of the z-coordinate from the cross product.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vector2 value1, Vector2 value2)
        {
            return value1.X * value2.Y
                   - value1.Y * value2.X;
        }

        /// <summary>
        /// Returns a vector whose elements are the minimum of each of the pairs of elements in the two source vectors.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <returns>The minimized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Min(Vector2 value1, Vector2 value2)
        {
            return new Vector2(
                (value1.X < value2.X) ? value1.X : value2.X,
                (value1.Y < value2.Y) ? value1.Y : value2.Y);
        }

        /// <summary>
        /// Returns a vector whose elements are the maximum of each of the pairs of elements in the two source vectors
        /// </summary>
        /// <param name="value1">The first source vector</param>
        /// <param name="value2">The second source vector</param>
        /// <returns>The maximized vector</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Max(Vector2 value1, Vector2 value2)
        {
            return new Vector2(
                (value1.X > value2.X) ? value1.X : value2.X,
                (value1.Y > value2.Y) ? value1.Y : value2.Y);
        }

        /// <summary>
        /// Returns a vector whose elements are the absolute values of each of the source vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The absolute value vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Abs(Vector2 value)
        {
            return new Vector2(Math.Abs(value.X), Math.Abs(value.Y));
        }

        /// <summary>
        /// Returns a vector whose elements are the square root of each of the source vector's elements.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The square root vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 SquareRoot(Vector2 value)
        {
            return new Vector2(Math.Sqrt(value.X), Math.Sqrt(value.Y));
        }
        #endregion Public Static Methods

        #region Public Static Operators
        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator +(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X + right.X, left.Y + right.Y);
        }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X - right.X, left.Y - right.Y);
        }

        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X * right.X, left.Y * right.Y);
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(double left, Vector2 right)
        {
            return new Vector2(left, left) * right;
        }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator *(Vector2 left, double right)
        {
            return left * new Vector2(right, right);
        }

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 left, Vector2 right)
        {
            return new Vector2(left.X / right.X, left.Y / right.Y);
        }

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator /(Vector2 value1, double value2)
        {
            return value1 / new Vector2(value2);
        }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 operator -(Vector2 value)
        {
            return Zero - value;
        }

        /// <summary>
        /// Returns a boolean indicating whether the two given vectors are equal.
        /// </summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if the vectors are equal; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2 left, Vector2 right)
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
        public static bool operator !=(Vector2 left, Vector2 right)
        {
            return !(left == right);
        }
        #endregion Public Static Operators
        #region Public instance methods
        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(this.X.GetHashCode(), this.Y.GetHashCode());
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this Vector2 instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this Vector2; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            if (!(obj is Vector2))
                return false;
            return Equals((Vector2)obj);
        }

        /// <summary>
        /// Returns a String representing this Vector2 instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return ToString("G", CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this Vector2 instance, using the specified format to format individual elements.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format)
        {
            return ToString(format, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Returns a String representing this Vector2 instance, using the specified format to format individual elements
        /// and the given IFormatProvider.
        /// </summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            sb.Append(this.X.ToString(format, formatProvider));
            sb.Append(separator);
            sb.Append(' ');
            sb.Append(this.Y.ToString(format, formatProvider));
            sb.Append('>');
            return sb.ToString();
        }

        /// <summary>
        /// Returns the length of the vector.
        /// </summary>
        /// <returns>The vector's length.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Length()
        {
            double ls = X * X + Y * Y;
            return Math.Sqrt(ls);
        }

        /// <summary>
        /// Returns the length of the vector squared. This operation is cheaper than Length().
        /// </summary>
        /// <returns>The vector's length squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSquared()
        {
            return X * X + Y * Y;
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
        public static double Distance(Vector2 value1, Vector2 value2)
        {
            double dx = value1.X - value2.X;
            double dy = value1.Y - value2.Y;

            double ls = dx * dx + dy * dy;

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
        public static double DistanceSquared(Vector2 value1, Vector2 value2)
        {
            double dx = value1.X - value2.X;
            double dy = value1.Y - value2.Y;

            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Normalize(Vector2 value)
        {
            double ls = value.X * value.X + value.Y * value.Y;
            double invNorm = 1.0 / Math.Sqrt(ls);

            return new Vector2(
                value.X * invNorm,
                value.Y * invNorm);
        }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Reflect(Vector2 vector, Vector2 normal)
        {
            double dot = vector.X * normal.X + vector.Y * normal.Y;

            return new Vector2(
                vector.X - 2.0 * dot * normal.X,
                vector.Y - 2.0 * dot * normal.Y);
        }

        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>Vector2.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Clamp(Vector2 value1, Vector2 min, Vector2 max)
        {
            // This compare order is very important!!!
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            double x = value1.X;
            x = (min.X > x) ? min.X : x;  // max(x, minx)
            x = (max.X < x) ? max.X : x;  // min(x, maxx)

            double y = value1.Y;
            y = (min.Y > y) ? min.Y : y;  // max(y, miny)
            y = (max.Y < y) ? max.Y : y;  // min(y, maxy)

            return new Vector2(x, y);
        }

        /// <summary>
        /// Linearly interpolates between two vectors based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Lerp(Vector2 value1, Vector2 value2, double amount)
        {
            return new Vector2(
                value1.X + (value2.X - value1.X) * amount,
                value1.Y + (value2.Y - value1.Y) * amount);
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 position, Matrix3x3 matrix)
        {
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (position.X * matrix.M13 + position.Y * matrix.M23 + matrix.M33);
                return new Vector2(
                    factor * (position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M31),
                    factor * (position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M32));
            }
            return new Vector2(
                position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M31,
                position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M32);
        }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 position, Matrix4x4 matrix)
        {
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (position.X * matrix.M14 + position.Y * matrix.M24 + matrix.M44);
                return new Vector2(
                    factor * (position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41),
                    factor * (position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42));
            }
            return new Vector2(
                position.X * matrix.M11 + position.Y * matrix.M21 + matrix.M41,
                position.X * matrix.M12 + position.Y * matrix.M22 + matrix.M42);

        }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 TransformNoTranslate(Vector2 position, Matrix3x3 matrix)
        {
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (position.X * matrix.M13 + position.Y * matrix.M23);
                return new Vector2(
                    factor * (position.X * matrix.M11 + position.Y * matrix.M21),
                    factor * (position.X * matrix.M12 + position.Y * matrix.M22));
            }
            return new Vector2(
                position.X * matrix.M11 + position.Y * matrix.M21,
                position.X * matrix.M12 + position.Y * matrix.M22);
        }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 TransformNoTranslate(Vector2 position, Matrix4x4 matrix)
        {
            if (matrix.IsProjectiveTransform)
            {
                var factor = 1 / (position.X * matrix.M14 + position.Y * matrix.M24);
                return new Vector2(
                    factor * (position.X * matrix.M11 + position.Y * matrix.M21),
                    factor * (position.X * matrix.M12 + position.Y * matrix.M22));
            }
            return new Vector2(
                position.X * matrix.M11 + position.Y * matrix.M21,
                position.X * matrix.M12 + position.Y * matrix.M22);
        }



        /// <summary>
        /// Transforms a vector by the given Quaternion rotation value.
        /// </summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Transform(Vector2 value, Quaternion rotation)
        {
            double x2 = rotation.X + rotation.X;
            double y2 = rotation.Y + rotation.Y;
            double z2 = rotation.Z + rotation.Z;

            double wz2 = rotation.W * z2;
            double xx2 = rotation.X * x2;
            double xy2 = rotation.X * y2;
            double yy2 = rotation.Y * y2;
            double zz2 = rotation.Z * z2;

            return new Vector2(
                value.X * (1.0 - yy2 - zz2) + value.Y * (xy2 - wz2),
                value.X * (xy2 + wz2) + value.Y * (1.0 - xx2 - zz2));
        }
        #endregion Public Static Methods

        #region Public operator methods
        // all the below methods should be inlined as they are
        // implemented over JIT intrinsics

        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Add(Vector2 left, Vector2 right)
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
        public static Vector2 Subtract(Vector2 left, Vector2 right)
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
        public static Vector2 Multiply(Vector2 left, Vector2 right)
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
        public static Vector2 Multiply(Vector2 left, double right)
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
        public static Vector2 Multiply(double left, Vector2 right)
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
        public static Vector2 Divide(Vector2 left, Vector2 right)
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
        public static Vector2 Divide(Vector2 left, double divisor)
        {
            return left / divisor;
        }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Negate(Vector2 value)
        {
            return -value;
        }
        #endregion Public operator methods
    }
}
