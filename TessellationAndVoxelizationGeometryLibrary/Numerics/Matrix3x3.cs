// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;

namespace TVGL.Numerics  
{
    /// <summary>
    /// A structure encapsulating a 3x3 matrix.
    /// </summary>
    public readonly struct Matrix3x3 : IEquatable<Matrix3x3>
    {
        private const double RotationEpsilon = 0.001 * Math.PI / 180.0;     // 0.1% of a degree

        #region Public Fields
        /// <summary>
        /// The first element of the first row. This is the x-scaling term.
        /// </summary>
        public double M11 { get; }
        /// <summary>
        /// The second element of the first row. This is the y-skewing term.
        /// </summary>
        public double M12 { get; }
        /// <summary>
        /// The first element of the second row. This is the x-skewing term.
        /// </summary>
        public double M21 { get; }
        /// <summary>
        /// The second element of the second row. This is the y-scaling term.
        /// </summary>
        public double M22 { get; }
        /// <summary>
        /// The first element of the third row. This is the x-translation term.
        /// </summary>
        public double M31 { get; }
        /// <summary>
        /// The second element of the third row. This is the y-translation term.
        /// </summary>
        public double M32 { get; }

        // Now the Projective Transform terms
        /// <summary>
        /// Gets a value indicating whether this instance is projective transform. This means that the third
        /// column has non-trivia values MX3 are nonzero or M33 is not unity (1).
        /// </summary>
        /// <value><c>true</c> if this instance is projective transform; otherwise, <c>false</c>.</value>
        public bool IsProjectiveTransform { get; }
        /// <summary>
        /// The third element of the first row. This is the x-projective term.
        /// </summary>
        public double M13 { get; }
        /// <summary>
        /// The third element of the second row. This is the y-projective term.
        /// </summary>
        public double M23 { get; }
        /// <summary>
        /// The third element of the third row. This is the global scaling term.
        /// </summary>
        public double M33 { get; }
        #endregion Public Fields



        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix3x3 Identity =>
            new Matrix3x3(
                1, 0,
                0, 1,
                0, 0);

        /// <summary>
        /// Returns a null matrix, which means all values are set to Not-A-Number.
        /// </summary>
        public static Matrix3x3 Null =>
            new Matrix3x3(
                double.NaN, double.NaN,
                double.NaN, double.NaN,
                double.NaN, double.NaN);

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        /// // CHANGED from property to method
        public bool IsIdentity()
        {
            return M11 == 1.0 && M22 == 1.0 && // Check diagonal element first for early out.
                                M12 == 0.0 &&
                   M21 == 0.0 &&
                   M31 == 0.0 && M32 == 0.0 && (!IsProjectiveTransform ||
                   (M13 == 0.0 && M23 == 0.0 && M33 == 1.0));
        }
        /// <summary>
        /// Returns whether the matrix has any Not-A-Numbers or if all terms are zero.
        /// </summary>
        /// // CHANGED from property to method
        public bool IsNull()
        {
            return
                double.IsNaN(M11) || double.IsNaN(M12) || double.IsNaN(M13) ||
                double.IsNaN(M21) || double.IsNaN(M22) || double.IsNaN(M23) ||
                double.IsNaN(M31) || double.IsNaN(M32) || double.IsNaN(M33) ||
                (M11 == 0.0 && M12 == 0.0 && M13 == 0.0 &&
                M21 == 0.0 && M22 == 0.0 && M23 == 0.0 &&
                M31 == 0.0 && M32 == 0.0 && M33 == 0.0);
        }

        /// <summary>
        /// Gets or sets the translation component of this matrix.
        /// </summary>
        public Vector2 Translation => new Vector2(M31, M32);

        /// <summary>
        /// Constructs a Matrix3x3 from the given components.
        /// </summary>
        public Matrix3x3(double m11, double m12,
                         double m21, double m22,
                         double m31, double m32)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = 0.0;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = 0.0;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = 1.0;
            IsProjectiveTransform = false;
        }

        /// <summary>
        /// Constructs a Matrix3x3 from the given components.
        /// </summary>
        public Matrix3x3(double m11, double m12, double m13,
                         double m21, double m22, double m23,
                         double m31, double m32, double m33)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M21 = m21;
            this.M22 = m22;
            this.M31 = m31;
            this.M32 = m32;

            if (m13.IsNegligible() && m23.IsNegligible() && m33.IsPracticallySame(1.0))
            {
                IsProjectiveTransform = false;
                this.M13 = 0.0;
                this.M23 = 0.0;
                this.M33 = 1.0;
            }
            else
            {
                IsProjectiveTransform = true;
                this.M13 = m13;
                this.M23 = m23;
                this.M33 = m33;
            }
        }

        /// <summary>
        /// Creates a translation matrix from the given vector.
        /// </summary>
        /// <param name="position">The translation position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix3x3 CreateTranslation(Vector2 position)
        {
            return CreateTranslation(position.X, position.Y);
        }

        /// <summary>
        /// Creates a translation matrix from the given X and Y components.
        /// </summary>
        /// <param name="xPosition">The X position.</param>
        /// <param name="yPosition">The Y position.</param>
        /// <returns>A translation matrix.</returns>
        public static Matrix3x3 CreateTranslation(double xPosition, double yPosition)
        {
            return new Matrix3x3(
                1.0, 0,
                0, 1,
                xPosition, yPosition);
        }

        /// <summary>
        /// Creates a scale matrix from the given X and Y components.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double xScale, double yScale)
        {
            return new Matrix3x3(
                xScale, 0,
                0, yScale,
                0, 0);
        }

        /// <summary>
        /// Creates a scale matrix that is offset by a given center point.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double xScale, double yScale, Vector2 centerPoint)
        {
            double tx = centerPoint.X * (1 - xScale);
            double ty = centerPoint.Y * (1 - yScale);

            return new Matrix3x3(
                xScale, 0,
                0, yScale,
                tx, ty);
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(Vector2 scales)
        {
            return CreateScale(scales.X, scales.Y);
        }

        /// <summary>
        /// Creates a scale matrix from the given vector scale with an offset from the given center point.
        /// </summary>
        /// <param name="scales">The scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(Vector2 scales, Vector2 centerPoint)
        {
            return CreateScale(scales.X, scales.Y, centerPoint);
        }

        /// <summary>
        /// Creates a scale matrix that scales uniformly with the given scale.
        /// </summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double scale)
        {
            return CreateScale(scale, scale);
        }

        /// <summary>
        /// Creates a scale matrix that scales uniformly with the given scale with an offset from the given center.
        /// </summary>
        /// <param name="scale">The uniform scale to use.</param>
        /// <param name="centerPoint">The center offset.</param>
        /// <returns>A scaling matrix.</returns>
        public static Matrix3x3 CreateScale(double scale, Vector2 centerPoint)
        {
            return CreateScale(scale, scale, centerPoint);
        }

        /// <summary>
        /// Creates a skew matrix from the given angles in radians.
        /// </summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <returns>A skew matrix.</returns>
        public static Matrix3x3 CreateSkew(double radiansX, double radiansY)
        {
            double xTan = Math.Tan(radiansX);
            double yTan = Math.Tan(radiansY);
            return new Matrix3x3(
             1.0, yTan,
             xTan, 1.0,
             0, 0);
        }

        /// <summary>
        /// Creates a skew matrix from the given angles in radians and a center point.
        /// </summary>
        /// <param name="radiansX">The X angle, in radians.</param>
        /// <param name="radiansY">The Y angle, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A skew matrix.</returns>
        public static Matrix3x3 CreateSkew(double radiansX, double radiansY, Vector2 centerPoint)
        {
            double xTan = Math.Tan(radiansX);
            double yTan = Math.Tan(radiansY);
            double tx = -centerPoint.Y * xTan;
            double ty = -centerPoint.X * yTan;
            return new Matrix3x3(
                  1.0, yTan,
                  xTan, 1.0,
                  tx, ty);
        }

        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix3x3 CreateRotation(double radians)
        {
            GetSineCosineOfAngle(radians, out var s, out var c);
            return new Matrix3x3(
                c, s,
                -s, c,
                0, 0);
        }


        /// <summary>
        /// Creates a rotation matrix using the given rotation in radians and a center point.
        /// </summary>
        /// <param name="radians">The amount of rotation, in radians.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>A rotation matrix.</returns>
        public static Matrix3x3 CreateRotation(double radians, Vector2 centerPoint)
        {
            GetSineCosineOfAngle(radians, out var s, out var c);
            double x = centerPoint.X * (1 - c) + centerPoint.Y * s;
            double y = centerPoint.Y * (1 - c) - centerPoint.X * s;
            return new Matrix3x3(
            c, s,
            -s, c,
            x, y);
        }

        private static void GetSineCosineOfAngle(double radians, out double s, out double c)
        {
            radians = Math.IEEERemainder(radians, Math.PI * 2);

            if (radians > -RotationEpsilon && radians < RotationEpsilon)
            {
                // Exact case for zero rotation.
                c = 1;
                s = 0;
            }
            else if (radians > Math.PI / 2 - RotationEpsilon && radians < Math.PI / 2 + RotationEpsilon)
            {
                // Exact case for 90 degree rotation.
                c = 0;
                s = 1;
            }
            else if (radians < -Math.PI + RotationEpsilon || radians > Math.PI - RotationEpsilon)
            {
                // Exact case for 180 degree rotation.
                c = -1;
                s = 0;
            }
            else if (radians > -Math.PI / 2 - RotationEpsilon && radians < -Math.PI / 2 + RotationEpsilon)
            {
                // Exact case for 270 degree rotation.
                c = 0;
                s = -1;
            }
            else
            {
                // Arbitrary rotation.
                c = Math.Cos(radians);
                s = Math.Sin(radians);
            }
        }

        /// <summary>
        /// Transposes the specified matrix. Recall that this flips the matrix about its diagonal (rows
        /// become columns and columns become rows)
        /// </summary>
        /// <param name="m">The matrix to be transponsed.</param>
        /// <returns>Matrix3x3.</returns>
        public static Matrix3x3 Transpose(Matrix3x3 m)
        { return new Matrix3x3(m.M11, m.M21, m.M31, m.M12, m.M22, m.M32, m.M13, m.M23, m.M33); }

        /// <summary>
        /// Calculates the determinant for this matrix.
        /// The determinant is calculated by expanding the matrix with a third column whose values are (0,0,1).
        /// </summary>
        /// <returns>The determinant.</returns>
        public double GetDeterminant()
        {
            if (!IsProjectiveTransform)
                return (M11 * M22) - (M21 * M12);
            return M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32
                - M13 * M22 * M31 - M12 * M21 * M33 - M11 * M23 * M32;
        }

        /// <summary>
        /// Attempts to invert the given matrix. If the operation succeeds, the inverted matrix is stored in the result parameter.
        /// </summary>
        /// <param name="m">The source matrix.</param>
        /// <param name="result">The output matrix.</param>
        /// <returns>True if the operation succeeded, False otherwise.</returns>
        public static bool Invert(Matrix3x3 m, out Matrix3x3 result)
        {
            double det = m.GetDeterminant();

            if (det == 0)
            {
                result = new Matrix3x3(double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN);
                return false;
            }
            double invDet = 1.0 / det;
            if (!m.IsProjectiveTransform)
                result = new Matrix3x3(
                    m.M22 * invDet, -m.M12 * invDet,
                    -m.M21 * invDet, m.M11 * invDet,
                    (m.M21 * m.M32 - m.M31 * m.M22) * invDet, (m.M31 * m.M12 - m.M11 * m.M32) * invDet);
            else
                result = new Matrix3x3(
                // First row
                 (m.M22 * m.M33 - m.M23 * m.M32) * invDet,
                 (m.M13 * m.M32 - m.M12 * m.M33) * invDet,
                 (m.M12 * m.M23 - m.M13 * m.M22) * invDet,
                // Second row
                 (m.M23 * m.M31 - m.M21 * m.M33) * invDet,
                 (m.M11 * m.M33 - m.M13 * m.M31) * invDet,
                 (m.M13 * m.M21 - m.M11 * m.M23) * invDet,
                 // Third row
                 (m.M21 * m.M32 - m.M31 * m.M22) * invDet,
                 (m.M31 * m.M12 - m.M11 * m.M32) * invDet,
                 (m.M11 * m.M22 - m.M12 * m.M21) * invDet
                 );
            return true;
        }

        /// <summary>
        /// Linearly interpolates from matrix1 to matrix2, based on the third parameter.
        /// </summary>
        /// <param name="matrix1">The first source matrix.</param>
        /// <param name="matrix2">The second source matrix.</param>
        /// <param name="amount">The relative weighting of matrix2.</param>
        /// <returns>The interpolated matrix.</returns>
        public static Matrix3x3 Lerp(Matrix3x3 matrix1, Matrix3x3 matrix2, double amount)
        {
            return new Matrix3x3(
                // First row
                matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount,
                matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount,
                matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount,
                // Second row
                matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount,
                matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount,
                matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount,
                // Third row
                matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount,
                matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount,
                matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount
                );
        }

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix3x3 Negate(Matrix3x3 value)
        {
            if (value.IsProjectiveTransform)
                return new Matrix3x3(
                    -value.M11, -value.M12, -value.M13,
                    -value.M21, -value.M22, -value.M23,
                    -value.M31, -value.M32, -value.M33
                    );
            else
                return new Matrix3x3(
                    -value.M11, -value.M12,
                    -value.M21, -value.M22,
                    -value.M31, -value.M32
                    );
        }

        /// <summary>
        /// Adds each matrix element in value1 with its corresponding element in value2.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the summed values.</returns>
        public static Matrix3x3 Add(Matrix3x3 value1, Matrix3x3 value2)
        {
            return new Matrix3x3(
                value1.M11 + value2.M11, value1.M12 + value2.M12, value1.M13 + value2.M13,
                value1.M21 + value2.M21, value1.M22 + value2.M22, value1.M23 + value2.M23,
                value1.M31 + value2.M31, value1.M32 + value2.M32, value1.M33 + value2.M33);
        }

        /// <summary>
        /// Subtracts each matrix element in value2 from its corresponding element in value1.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the resulting values.</returns>
        public static Matrix3x3 Subtract(Matrix3x3 value1, Matrix3x3 value2)
        {
            return new Matrix3x3(
                value1.M11 - value2.M11, value1.M12 - value2.M12, value1.M13 - value2.M13,
                value1.M21 - value2.M21, value1.M22 - value2.M22, value1.M23 - value2.M23,
                value1.M31 - value2.M31, value1.M32 - value2.M32, value1.M33 - value2.M33);
        }

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix3x3 Multiply(Matrix3x3 value1, Matrix3x3 value2)
        {
            if (value1.IsProjectiveTransform || value2.IsProjectiveTransform)
                return new Matrix3x3(
                  // First row
                  value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31,
                  value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32,
                  value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33,

                 // Second row
                 value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31,
                 value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32,
                 value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33,

                 // Third row
                 value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31,
                 value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32,
                 value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33
                 );

            return new Matrix3x3(
              // First row
              value1.M11 * value2.M11 + value1.M12 * value2.M21,
              value1.M11 * value2.M12 + value1.M12 * value2.M22,
             //0 ,

             // Second row
             value1.M21 * value2.M11 + value1.M22 * value2.M21,
             value1.M21 * value2.M12 + value1.M22 * value2.M22,
             // 0 ,

             // Third row
             value1.M31 * value2.M11 + value1.M32 * value2.M21 + value2.M31,
             value1.M31 * value2.M12 + value1.M32 * value2.M22 + value2.M32
             //, 1
             );
        }

        /// <summary>
        /// Scales all elements in a matrix by the given scalar factor.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix3x3 Multiply(Matrix3x3 value1, double value2)
        {
            if (value1.IsProjectiveTransform)
                return new Matrix3x3(
                    value2 * value1.M11, value2 * value1.M12, value2 * value1.M13,
                    value2 * value1.M21, value2 * value1.M22, value2 * value1.M23,
                    value2 * value1.M31, value2 * value1.M32, value2 * value1.M33
                    );
            else
                return new Matrix3x3(
                    value2 * value1.M11, value2 * value1.M12,
                    value2 * value1.M21, value2 * value1.M22,
                    value2 * value1.M31, value2 * value1.M32
                    );
        }

        /// <summary>
        /// Negates the given matrix by multiplying all values by -1.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix3x3 operator -(Matrix3x3 value)
        {
            return Negate(value);
        }

        /// <summary>
        /// Adds each matrix element in value1 with its corresponding element in value2.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the summed values.</returns>
        public static Matrix3x3 operator +(Matrix3x3 value1, Matrix3x3 value2)
        {
            return Add(value1, value2);
        }

        /// <summary>
        /// Subtracts each matrix element in value2 from its corresponding element in value1.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The matrix containing the resulting values.</returns>
        public static Matrix3x3 operator -(Matrix3x3 value1, Matrix3x3 value2)
        {
            return Subtract(value1, value2);
        }

        /// <summary>
        /// Multiplies two matrices together and returns the resulting matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The product matrix.</returns>
        public static Matrix3x3 operator *(Matrix3x3 value1, Matrix3x3 value2)
        {
            return Multiply(value1, value2);
        }

        /// <summary>
        /// Scales all elements in a matrix by the given scalar factor.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix3x3 operator *(Matrix3x3 value1, double value2)
        {
            return Multiply(value1, value2);
        }
        /// <summary>
        /// Scales all elements in a matrix by the given scalar factor.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling value to use.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix3x3 operator *(double value1, Matrix3x3 value2)
        {
            return Multiply(value2, value1);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public static bool operator ==(Matrix3x3 value1, Matrix3x3 value2)
        {
            return value1.Equals(value2);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given matrices are not equal.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>True if the matrices are not equal; False if they are equal.</returns>
        public static bool operator !=(Matrix3x3 value1, Matrix3x3 value2)
        {
            return !value1.Equals(value2);
        }

        /// <summary>
        /// Returns a boolean indicating whether the matrix is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The other matrix to test equality against.</param>
        /// <returns>True if this matrix is equal to other; False otherwise.</returns>
        public bool Equals(Matrix3x3 other)
        {
            if (IsProjectiveTransform != other.IsProjectiveTransform) return false;
            if (IsProjectiveTransform)
                return (M11 == other.M11 && M22 == other.M22 && // Check diagonal element first for early out.
                                                    M12 == other.M12 &&
                        M21 == other.M21 &&
                        M31 == other.M31 && M32 == other.M32 &&
                        M13 == other.M13 &&
                        M23 == other.M23 && M33 == other.M33);

            return (M11 == other.M11 && M22 == other.M22 && // Check diagonal element first for early out.
                                                M12 == other.M12 &&
                    M21 == other.M21 &&
                    M31 == other.M31 && M32 == other.M32);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Matrix3x3 matrix3X3)
            {
                return Equals(matrix3X3);
            }

            return false;
        }

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{ {{M11:{0} M12:{1} M12:{2}}} {{M21:{4} M22:{4} M23:{5}}} {{M31:{6} M32:{7} M33:{8}}} }}",
                                 M11, M12, M13,
                                 M21, M22, M23,
                                 M31, M32, M33);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return unchecked(M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() +
                             M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() +
                             M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode());
        }
    }
}
