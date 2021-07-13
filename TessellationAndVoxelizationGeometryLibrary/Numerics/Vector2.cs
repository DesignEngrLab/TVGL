// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using MIConvexHull;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL.Numerics  
{
    /// <summary>
    /// A structure encapsulating two single precision floating point values and provides hardware accelerated methods.
    /// </summary>
    
    public readonly partial struct Vector2 : IEquatable<Vector2>, IFormattable, IVertex2D, IVertex
    {
        #region Public Static Properties
        /// <summary>
        /// Returns the vector (NaN,NaN). This is often used in place of null.
        /// </summary>
        public static Vector2 Null =>
            
            new Vector2(double.NaN, double.NaN);


        /// <summary>
        /// Returns the vector (0,0).
        /// </summary>
        public static Vector2 Zero =>
            
            default;

        /// <summary>
        /// Returns the vector (1,1).
        /// </summary>
        public static Vector2 One =>
            
            new Vector2(1.0, 1.0);

        /// <summary>
        /// Returns the vector (1,0).
        /// </summary>
        public static Vector2 UnitX => new Vector2(1.0, 0.0);

        /// <summary>
        /// Returns the vector (0,1).
        /// </summary>
        public static Vector2 UnitY => new Vector2(0.0, 1.0);

        /// <summary>
        /// Makes a copy of the current Vector.
        /// </summary>
        public Vector2 Copy()
        {
            return new Vector2(X, Y);
        }
        double IVertex2D.X => X;

        double IVertex2D.Y => Y;

        [JsonIgnore]

        public double[] Position => new[] { X, Y };

        public double this[int i]
        {
            get
            {
                if (i == 0) return X;
                else  return Y;
            }
        }
        #endregion Public Static Properties

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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                double ls = Vector2.Dot(this, this);
                return Math.Sqrt(ls);
            }
            else
            {
                double ls = X * X + Y * Y;
                return Math.Sqrt(ls);
            }
        }

        /// <summary>
        /// Returns the length of the vector squared. This operation is cheaper than Length().
        /// </summary>
        /// <returns>The vector's length squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double LengthSquared()
        {
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                return Vector2.Dot(this, this);
            }
            else
            {
                return X * X + Y * Y;
            }
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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                Vector2 difference = value1 - value2;
                double ls = Vector2.Dot(difference, difference);
                return Math.Sqrt(ls);
            }
            else
            {
                double dx = value1.X - value2.X;
                double dy = value1.Y - value2.Y;

                double ls = dx * dx + dy * dy;

                return Math.Sqrt(ls);
            }
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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                Vector2 difference = value1 - value2;
                return Vector2.Dot(difference, difference);
            }
            else
            {
                double dx = value1.X - value2.X;
                double dy = value1.Y - value2.Y;

                return dx * dx + dy * dy;
            }
        }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Normalize(Vector2 value)
        {
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                double length = value.Length();
                return value / length;
            }
            else
            {
                double ls = value.X * value.X + value.Y * value.Y;
                double invNorm = 1.0 / Math.Sqrt(ls);

                return new Vector2(
                    value.X * invNorm,
                    value.Y * invNorm);
            }
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
            if (Constants.IsHardwareAccelerated) // COMMENTEDCHANGE (Vector.IsHardwareAccelerated)
            {
                double dot = Vector2.Dot(vector, normal);
                return vector - (2 * dot * normal);
            }
            else
            {
                double dot = vector.X * normal.X + vector.Y * normal.Y;

                return new Vector2(
                    vector.X - 2.0 * dot * normal.X,
                    vector.Y - 2.0 * dot * normal.Y);
            }
        }

        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
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
        /// of normal vectors requires that the input matrix be the transpose of the inverse of that matrix.
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
        /// of normal vectors requires that the input matrix be the transpose of the inverse of that matrix.
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
