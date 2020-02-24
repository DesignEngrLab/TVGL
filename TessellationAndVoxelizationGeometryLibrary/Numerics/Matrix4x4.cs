// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
// COMMENTEDCHANGE using System.Runtime.Intrinsics;
// COMMENTEDCHANGE using System.Runtime.Intrinsics.X86;

namespace TVGL.Numerics  // COMMENTEDCHANGE namespace System.Numerics
{
    /// <summary>
    /// A structure encapsulating a 4x4 matrix.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        private const double BillboardEpsilon = 1e-4;
        private const double BillboardMinAngle = 1.0 - (0.1 * (Math.PI / 180.0)); // 0.1 degrees
        private const double DecomposeEpsilon = 0.0001;

        #region Public Fields
        /// <summary>
        /// Value at row 1, column 1 of the matrix. This is the x scaling term.
        /// </summary>
        public double M11 { get; }
        /// <summary>
        /// Value at row 1, column 2 of the matrix.
        /// </summary>
        public double M12 { get; }
        /// <summary>
        /// Value at row 1, column 3 of the matrix.
        /// </summary>
        public double M13 { get; }
        /// <summary>
        /// Value at row 1, column 4 of the matrix. This is the x projective term.
        /// </summary>
        public double M14 { get; }

        /// <summary>
        /// Value at row 2, column 1 of the matrix.
        /// </summary>
        public double M21 { get; }
        /// <summary>
        /// Value at row 2, column 2 of the matrix. This is the y scaling term.
        /// </summary>
        public double M22 { get; }
        /// <summary>
        /// Value at row 2, column 3 of the matrix.
        /// </summary>
        public double M23 { get; }
        /// <summary>
        /// Value at row 2, column 4 of the matrix. This is the y projective term.
        /// </summary>
        public double M24 { get; }

        /// <summary>
        /// Value at row 3, column 1 of the matrix.
        /// </summary>
        public double M31 { get; }
        /// <summary>
        /// Value at row 3, column 2 of the matrix.
        /// </summary>
        public double M32 { get; }
        /// <summary>
        /// Value at row 3, column 3 of the matrix. This is the z scaling term.
        /// </summary>
        public double M33 { get; }
        /// <summary>
        /// Value at row 3, column 4 of the matrix. This is the z projective term.
        /// </summary>
        public double M34 { get; }

        /// <summary>
        /// Value at row 4, column 1 of the matrix. This is the x translation term.
        /// </summary>
        public double M41 { get; }
        /// <summary>
        /// Value at row 4, column 2 of the matrix. This is the y translation term.
        /// </summary>
        public double M42 { get; }
        /// <summary>
        /// Value at row 4, column 3 of the matrix. This is the z translation term.
        /// </summary>
        public double M43 { get; }
        /// <summary>
        /// Value at row 4, column 4 of the matrix. This is the global scaling term.
        /// </summary>
        public double M44 { get; }

        // Now the Projective Transform terms
        /// <summary>
        /// Gets a value indicating whether this instance is projective transform. This means that the fourth
        /// column has non-trivia values MX4 are nonzero or M44 is not unity (1).
        /// </summary>
        /// <value><c>true</c> if this instance is projective transform; otherwise, <c>false</c>.</value>
        public bool IsProjectiveTransform { get; }

        #endregion Public Fields
        /// <summary>
        /// Returns the multiplicative identity matrix.
        /// </summary>
        public static Matrix4x4 Identity => new Matrix4x4(
            1, 0, 0,
            0, 1, 0,
            0, 0, 1,
            0, 0, 0);

        /// <summary>
        /// Returns whether the matrix is the identity matrix.
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return !IsProjectiveTransform &&
                    M11 == 1.0 && M22 == 1.0 && M33 == 1.0 && M44 == 1.0 && // Check diagonal element first for early out.
                    M12 == 0.0 && M13 == 0.0 && M14 == 0.0 &&
                    M21 == 0.0 && M23 == 0.0 && M24 == 0.0 &&
                    M31 == 0.0 && M32 == 0.0 && M34 == 0.0 &&
                    M41 == 0.0 && M42 == 0.0 && M43 == 0.0;
            }
        }

        /// <summary>
        /// Gets or sets the translation component of this matrix.
        /// </summary>
        public Vector3 Translation => new Vector3(M41, M42, M43);

        /// <summary>
        /// Constructs a Matrix4x4 from the given components.
        /// </summary>
        public Matrix4x4(double m11, double m12, double m13, double m14,
                         double m21, double m22, double m23, double m24,
                         double m31, double m32, double m33, double m34,
                         double m41, double m42, double m43, double m44)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;

            if (m14.IsNegligible() && m24.IsNegligible() && m34.IsNegligible() && m44.IsPracticallySame(1.0))
            {
                this.M14 = 0.0;
                this.M24 = 0.0;
                this.M34 = 0.0;
                this.M44 = 1.0;
                IsProjectiveTransform = false;
            }
            else
            {
                this.M14 = m14;
                this.M24 = m24;
                this.M34 = m34;
                this.M44 = m44;
                IsProjectiveTransform = true;
            }
        }

        /// <summary>
        /// Constructs a Matrix4x4 from the given components.
        /// </summary>
        public Matrix4x4(double m11, double m12, double m13,
                         double m21, double m22, double m23,
                         double m31, double m32, double m33,
                         double m41, double m42, double m43)
        {
            this.M11 = m11;
            this.M12 = m12;
            this.M13 = m13;
            this.M14 = 0.0;

            this.M21 = m21;
            this.M22 = m22;
            this.M23 = m23;
            this.M24 = 0.0;

            this.M31 = m31;
            this.M32 = m32;
            this.M33 = m33;
            this.M34 = 0.0;

            this.M41 = m41;
            this.M42 = m42;
            this.M43 = m43;
            this.M44 = 1.0;
            IsProjectiveTransform = false;
        }

        /// <summary>
        /// Constructs a Matrix4x4 from the given Matrix3x3.
        /// </summary>
        /// <param name="value">The source Matrix3x3.</param>
        public Matrix4x4(Matrix3x3 matrix) : this(
            matrix.M11, matrix.M12, 0, matrix.M13,
            matrix.M21, matrix.M22, 0, matrix.M23,
            0, 0, 1, 0,
            matrix.M31, matrix.M32, 0, 1)
        { }

        /// <summary>
        /// Creates a spherical billboard that rotates around a specified object position.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="cameraUpVector">The up vector of the camera.</param>
        /// <param name="cameraForwardVector">The forward vector of the camera.</param>
        /// <returns>The created billboard matrix</returns>
        public static Matrix4x4 CreateBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 cameraUpVector, Vector3 cameraForwardVector)
        {
            Vector3 zaxis = new Vector3(
                objectPosition.X - cameraPosition.X,
                objectPosition.Y - cameraPosition.Y,
                objectPosition.Z - cameraPosition.Z);

            double norm = zaxis.LengthSquared();

            if (norm < BillboardEpsilon)
            {
                zaxis = -cameraForwardVector;
            }
            else
            {
                zaxis = Vector3.Multiply(zaxis, 1.0 / Math.Sqrt(norm));
            }

            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));

            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            return new Matrix4x4(
              xaxis.X, xaxis.Y, xaxis.Z, //0.0,
              yaxis.X, yaxis.Y, yaxis.Z, //0.0, commenting these leads to the simpler affine constructor which has 4x3 arguments
              zaxis.X, zaxis.Y, zaxis.Z, //0.0,
              objectPosition.X, objectPosition.Y, objectPosition.Z//, 1.0
             );
        }

        /// <summary>
        /// Creates a cylindrical billboard that rotates around a specified axis.
        /// </summary>
        /// <param name="objectPosition">Position of the object the billboard will rotate around.</param>
        /// <param name="cameraPosition">Position of the camera.</param>
        /// <param name="rotateAxis">Axis to rotate the billboard around.</param>
        /// <param name="cameraForwardVector">Forward vector of the camera.</param>
        /// <param name="objectForwardVector">Forward vector of the object.</param>
        /// <returns>The created billboard matrix.</returns>
        public static Matrix4x4 CreateConstrainedBillboard(Vector3 objectPosition, Vector3 cameraPosition, Vector3 rotateAxis, Vector3 cameraForwardVector, Vector3 objectForwardVector)
        {
            // Treat the case when object and camera positions are too close.
            Vector3 faceDir = new Vector3(
                objectPosition.X - cameraPosition.X,
                objectPosition.Y - cameraPosition.Y,
                objectPosition.Z - cameraPosition.Z);

            double norm = faceDir.LengthSquared();

            if (norm < BillboardEpsilon)
            {
                faceDir = -cameraForwardVector;
            }
            else
            {
                faceDir = Vector3.Multiply(faceDir, (1.0 / Math.Sqrt(norm)));
            }

            Vector3 yaxis = rotateAxis;
            Vector3 xaxis;
            Vector3 zaxis;

            // Treat the case when angle between faceDir and rotateAxis is too close to 0.
            double dot = Vector3.Dot(rotateAxis, faceDir);

            if (Math.Abs(dot) > BillboardMinAngle)
            {
                zaxis = objectForwardVector;

                // Make sure passed values are useful for compute.
                dot = Vector3.Dot(rotateAxis, zaxis);

                if (Math.Abs(dot) > BillboardMinAngle)
                {
                    zaxis = (Math.Abs(rotateAxis.Z) > BillboardMinAngle) ? new Vector3(1, 0, 0) : new Vector3(0, 0, -1);
                }

                xaxis = Vector3.Normalize(Vector3.Cross(rotateAxis, zaxis));
                zaxis = Vector3.Normalize(Vector3.Cross(xaxis, rotateAxis));
            }
            else
            {
                xaxis = Vector3.Normalize(Vector3.Cross(rotateAxis, faceDir));
                zaxis = Vector3.Normalize(Vector3.Cross(xaxis, yaxis));
            }
            return new Matrix4x4(
                xaxis.X, xaxis.Y, xaxis.Z, //0.0,
                yaxis.X, yaxis.Y, yaxis.Z, //0.0,  commenting these leads to the simpler affine constructor which has 4x3 arguments
                zaxis.X, zaxis.Y, zaxis.Z, //0.0,
                objectPosition.X, objectPosition.Y, objectPosition.Z//, 1.0
                );
        }

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="position">The amount to translate in each axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(Vector3 position)
        {
            return CreateTranslation(position.X, position.Y, position.Z);
        }

        /// <summary>
        /// Creates a translation matrix.
        /// </summary>
        /// <param name="xPosition">The amount to translate on the X-axis.</param>
        /// <param name="yPosition">The amount to translate on the Y-axis.</param>
        /// <param name="zPosition">The amount to translate on the Z-axis.</param>
        /// <returns>The translation matrix.</returns>
        public static Matrix4x4 CreateTranslation(double xPosition, double yPosition, double zPosition)
        {
            return new Matrix4x4(
                1, 0, 0, //0,
                0, 1, 0, //0,   commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, 1, //0,
                xPosition, yPosition, zPosition//, 1.0
                );
        }

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="zScale">Value to scale by on the Z-axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(double xScale, double yScale, double zScale)
        {
            return new Matrix4x4(
                xScale, 0, 0, //0,
                0, yScale, 0, //0, commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, zScale, //0,
                0, 0, 0 //, 1
                );
        }

        /// <summary>
        /// Creates a scaling matrix with a center point.
        /// </summary>
        /// <param name="xScale">Value to scale by on the X-axis.</param>
        /// <param name="yScale">Value to scale by on the Y-axis.</param>
        /// <param name="zScale">Value to scale by on the Z-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(double xScale, double yScale, double zScale, Vector3 centerPoint)
        {
            double tx = centerPoint.X * (1 - xScale);
            double ty = centerPoint.Y * (1 - yScale);
            double tz = centerPoint.Z * (1 - zScale);

            return new Matrix4x4(
                xScale, 0, 0, //0,
                0, yScale, 0, //0,  commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, zScale, //0,
                tx, ty, tz//, 1
                );
        }

        /// <summary>
        /// Creates a scaling matrix.
        /// </summary>
        /// <param name="scales">The vector containing the amount to scale by on each axis.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales)
        {
            return CreateScale(scales.X, scales.Y, scales.Z);
        }

        /// <summary>
        /// Creates a scaling matrix with a center point.
        /// </summary>
        /// <param name="scales">The vector containing the amount to scale by on each axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(Vector3 scales, Vector3 centerPoint)
        {
            return CreateScale(scales.X, scales.Y, scales.Z, centerPoint);
        }

        /// <summary>
        /// Creates a uniform scaling matrix that scales equally on each axis.
        /// </summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(double scale)
        {
            return CreateScale(scale, scale, scale);
        }

        /// <summary>
        /// Creates a uniform scaling matrix that scales equally on each axis with a center point.
        /// </summary>
        /// <param name="scale">The uniform scaling factor.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The scaling matrix.</returns>
        public static Matrix4x4 CreateScale(double scale, Vector3 centerPoint)
        {
            return CreateScale(scale, scale, scale, centerPoint);
        }

        /// <summary>
        /// Creates a matrix for rotating points around the X-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(double radians)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);

            // [  1  0  0  0 ]
            // [  0  c  s  0 ]
            // [  0 -s  c  0 ]
            // [  0  0  0  1 ]
            return new Matrix4x4(
            1, 0, 0, //0,
            0, c, s, //0,   commenting these leads to the simpler affine constructor which has 4x3 arguments
            0, -s, c, //0,
            0, 0, 0//, 1
            );
        }

        /// <summary>
        /// Creates a matrix for rotating points around the X-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the X-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationX(double radians, Vector3 centerPoint)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);
            double y = centerPoint.Y * (1 - c) + centerPoint.Z * s;
            double z = centerPoint.Z * (1 - c) - centerPoint.Y * s;

            // [  1  0  0  0 ]
            // [  0  c  s  0 ]
            // [  0 -s  c  0 ]
            // [  0  y  z  1 ]
            return new Matrix4x4(
                1, 0, 0, //0,
                0, c, s, //0, commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, -s, c, //0,
                0, y, z //, 1
                );
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Y-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(double radians)
        {
            double c = Math.Cos(radians);
            double s = Math.Sin(radians);

            // [  c  0 -s  0 ]
            // [  0  1  0  0 ]
            // [  s  0  c  0 ]
            // [  0  0  0  1 ]
            return new Matrix4x4(
                c, 0, -s, //0,
                0, 1, 0, //0, commenting these leads to the simpler affine constructor which has 4x3 arguments
                s, 0, c, //0,
                0, 0, 0 //, 1
                );
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Y-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Y-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationY(double radians, Vector3 centerPoint)
        {
            Matrix4x4 result;

            double c = Math.Cos(radians);
            double s = Math.Sin(radians);

            double x = centerPoint.X * (1 - c) - centerPoint.Z * s;
            double z = centerPoint.Z * (1 - c) + centerPoint.X * s;

            // [  c  0 -s  0 ]
            // [  0  1  0  0 ]
            // [  s  0  c  0 ]
            // [  x  0  z  1 ]
            return new Matrix4x4(
                c, 0, -s, //0,
                0, 1, 0, //0, commenting these leads to the simpler affine constructor which has 4x3 arguments
                s, 0, c, //0,
                x, 0, z //, 1
                );
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Z-axis.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(double radians)
        {
            Matrix4x4 result;

            double c = Math.Cos(radians);
            double s = Math.Sin(radians);

            // [  c  s  0  0 ]
            // [ -s  c  0  0 ]
            // [  0  0  1  0 ]
            // [  0  0  0  1 ]
            return new Matrix4x4(
                c, s, 0, //0,
                -s, c, 0, //0,  commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, 1, //0,
                0, 0, 0 //, 1
                );
        }

        /// <summary>
        /// Creates a matrix for rotating points around the Z-axis, from a center point.
        /// </summary>
        /// <param name="radians">The amount, in radians, by which to rotate around the Z-axis.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateRotationZ(double radians, Vector3 centerPoint)
        {
            Matrix4x4 result;

            double c = Math.Cos(radians);
            double s = Math.Sin(radians);

            double x = centerPoint.X * (1 - c) + centerPoint.Y * s;
            double y = centerPoint.Y * (1 - c) - centerPoint.X * s;

            // [  c  s  0  0 ]
            // [ -s  c  0  0 ]
            // [  0  0  1  0 ]
            // [  x  y  0  1 ]
            return new Matrix4x4(
                c, s, 0, //0,
                -s, c, 0, //0,  commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, 1, //0,
                x, y, 0 //, 1
                );
        }

        /// <summary>
        /// Creates a matrix that rotates around an arbitrary vector.
        /// </summary>
        /// <param name="axis">The axis to rotate around.</param>
        /// <param name="angle">The angle to rotate around the given axis, in radians.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromAxisAngle(Vector3 axis, double angle)
        {
            // a: angle
            // x, y, z: unit vector for axis.
            //
            // Rotation matrix M can compute by using below equation.
            //
            //        T               T
            //  M = uu + (cos a)( I-uu ) + (sin a)S
            //
            // Where:
            //
            //  u = ( x, y, z )
            //
            //      [  0 -z  y ]
            //  S = [  z  0 -x ]
            //      [ -y  x  0 ]
            //
            //      [ 1 0 0 ]
            //  I = [ 0 1 0 ]
            //      [ 0 0 1 ]
            //
            //
            //     [  xx+cosa*(1-xx)   yx-cosa*yx-sina*z zx-cosa*xz+sina*y ]
            // M = [ xy-cosa*yx+sina*z    yy+cosa(1-yy)  yz-cosa*yz-sina*x ]
            //     [ zx-cosa*zx-sina*y zy-cosa*zy+sina*x   zz+cosa*(1-zz)  ]
            //
            double x = axis.X, y = axis.Y, z = axis.Z;
            double sa = Math.Sin(angle), ca = Math.Cos(angle);
            double xx = x * x, yy = y * y, zz = z * z;
            double xy = x * y, xz = x * z, yz = y * z;

            return new Matrix4x4(
              // First Row
              xx + ca * (1.0 - xx),
              xy - ca * xy + sa * z,
              xz - ca * xz - sa * y,
              //0.0,
              // Second Row
              xy - ca * xy - sa * z,
              yy + ca * (1.0 - yy),
              yz - ca * yz + sa * x,
              //0.0,   commenting these leads to the simpler affine constructor which has 4x3 arguments
              // Third Row
              xz - ca * xz + sa * y,
              yz - ca * yz - sa * x,
              zz + ca * (1.0 - zz),
              //0.0,
              // Fourth Row
              0, 0, 0 //, 1
              );
        }

        /// <summary>
        /// Creates a perspective projection matrix based on a field of view, aspect ratio, and near and far view plane distances.
        /// </summary>
        /// <param name="fieldOfView">Field of view in the y direction, in radians.</param>
        /// <param name="aspectRatio">Aspect ratio, defined as view space width divided by height.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspectiveFieldOfView(double fieldOfView, double aspectRatio, double nearPlaneDistance, double farPlaneDistance)
        {
            if (fieldOfView <= 0.0 || fieldOfView >= Math.PI)
                throw new ArgumentOutOfRangeException(nameof(fieldOfView));

            if (nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            double yScale = 1.0 / Math.Tan(fieldOfView * 0.5);
            double xScale = yScale / aspectRatio;
            var negFarRange = double.IsPositiveInfinity(farPlaneDistance) ? -1.0 : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            return new Matrix4x4(
            // First Row
            xScale, 0, 0, 0,
            // Second Row
            0, yScale, 0, 0,
            // Third Row - here's where things get complicated
            0, 0, negFarRange, -1,
            // Fourth Row
            0, 0, nearPlaneDistance * negFarRange, 0);
        }

        /// <summary>
        /// Creates a perspective projection matrix from the given view volume dimensions.
        /// </summary>
        /// <param name="width">Width of the view volume at the near view plane.</param>
        /// <param name="height">Height of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspective(double width, double height, double nearPlaneDistance, double farPlaneDistance)
        {
            if (nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));
            var negFarRange = double.IsPositiveInfinity(farPlaneDistance) ? -1.0 : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);
            return new Matrix4x4(
                // first row
                2.0 * nearPlaneDistance / width, 0, 0, 0,
                // second row
                0, 2.0 * nearPlaneDistance / height, 0, 0,
                // third row
                0, 0, negFarRange, -1,
                // fourth row
                0, 0, nearPlaneDistance * negFarRange, 0); //is M44 really 0 and not 1?
        }

        /// <summary>
        /// Creates a customized, perspective projection matrix.
        /// </summary>
        /// <param name="left">Minimum x-value of the view volume at the near view plane.</param>
        /// <param name="right">Maximum x-value of the view volume at the near view plane.</param>
        /// <param name="bottom">Minimum y-value of the view volume at the near view plane.</param>
        /// <param name="top">Maximum y-value of the view volume at the near view plane.</param>
        /// <param name="nearPlaneDistance">Distance to the near view plane.</param>
        /// <param name="farPlaneDistance">Distance to of the far view plane.</param>
        /// <returns>The perspective projection matrix.</returns>
        public static Matrix4x4 CreatePerspectiveOffCenter(double left, double right, double bottom, double top, double nearPlaneDistance, double farPlaneDistance)
        {
            if (nearPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));

            if (farPlaneDistance <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(farPlaneDistance));

            if (nearPlaneDistance >= farPlaneDistance)
                throw new ArgumentOutOfRangeException(nameof(nearPlaneDistance));
            var negFarRange = double.IsPositiveInfinity(farPlaneDistance) ? -1.0 : farPlaneDistance / (nearPlaneDistance - farPlaneDistance);

            return new Matrix4x4(
                // first row
                2.0 * nearPlaneDistance / (right - left), 0, 0, 0,
                // second row
                0, 2.0 * nearPlaneDistance / (top - bottom), 0, 0,
                // third row
                (left + right) / (right - left), (top + bottom) / (top - bottom), negFarRange, -1.0,
                // fourth row
                0, 0, nearPlaneDistance * negFarRange, 0); //is M44 really 0 and not 1?
        }

        /// <summary>
        /// Creates an orthographic perspective matrix from the given view volume dimensions.
        /// </summary>
        /// <param name="width">Width of the view volume.</param>
        /// <param name="height">Height of the view volume.</param>
        /// <param name="zNearPlane">Minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum Z-value of the view volume.</param>
        /// <returns>The orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographic(double width, double height, double zNearPlane, double zFarPlane)
        {
            return new Matrix4x4(
                2.0 / width, 0, 0, // 0,
                0, 2.0 / height, 0, // 0,   commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, 1.0 / (zNearPlane - zFarPlane), // 0,
                0, 0, zNearPlane / (zNearPlane - zFarPlane) //, 1.0
                );
        }

        /// <summary>
        /// Builds a customized, orthographic projection matrix.
        /// </summary>
        /// <param name="left">Minimum X-value of the view volume.</param>
        /// <param name="right">Maximum X-value of the view volume.</param>
        /// <param name="bottom">Minimum Y-value of the view volume.</param>
        /// <param name="top">Maximum Y-value of the view volume.</param>
        /// <param name="zNearPlane">Minimum Z-value of the view volume.</param>
        /// <param name="zFarPlane">Maximum Z-value of the view volume.</param>
        /// <returns>The orthographic projection matrix.</returns>
        public static Matrix4x4 CreateOrthographicOffCenter(double left, double right, double bottom, double top, double zNearPlane, double zFarPlane)
        {
            return new Matrix4x4(
                2.0 / (right - left), 0, 0, //0,
                0, 2.0 / (top - bottom), 0, //0,    commenting these leads to the simpler affine constructor which has 4x3 arguments
                0, 0, 1.0 / (zNearPlane - zFarPlane), //0,
                (left + right) / (left - right), (top + bottom) / (bottom - top), zNearPlane / (zNearPlane - zFarPlane) //, 1
                );
        }

        /// <summary>
        /// Creates a view matrix.
        /// </summary>
        /// <param name="cameraPosition">The position of the camera.</param>
        /// <param name="cameraTarget">The target towards which the camera is pointing.</param>
        /// <param name="cameraUpVector">The direction that is "up" from the camera's point of view.</param>
        /// <returns>The view matrix.</returns>
        public static Matrix4x4 CreateLookAt(Vector3 cameraPosition, Vector3 cameraTarget, Vector3 cameraUpVector)
        {
            Vector3 zaxis = Vector3.Normalize(cameraPosition - cameraTarget);
            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(cameraUpVector, zaxis));
            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            return new Matrix4x4(
             xaxis.X, yaxis.X, zaxis.X, //0.0,
             xaxis.Y, yaxis.Y, zaxis.Y, //0.0, commenting out last column leads to the simpler affine constructor which has 4x3 arguments
             xaxis.Z, yaxis.Z, zaxis.Z, //0.0,
             -Vector3.Dot(xaxis, cameraPosition), -Vector3.Dot(yaxis, cameraPosition), -Vector3.Dot(zaxis, cameraPosition)
            //, 1.0
            );
        }

        /// <summary>
        /// Creates a world matrix with the specified parameters.
        /// </summary>
        /// <param name="position">The position of the object; used in translation operations.</param>
        /// <param name="forward">Forward direction of the object.</param>
        /// <param name="up">Upward direction of the object; usually [0, 1, 0].</param>
        /// <returns>The world matrix.</returns>
        public static Matrix4x4 CreateWorld(Vector3 position, Vector3 forward, Vector3 up)
        {
            Vector3 zaxis = Vector3.Normalize(-forward);
            Vector3 xaxis = Vector3.Normalize(Vector3.Cross(up, zaxis));
            Vector3 yaxis = Vector3.Cross(zaxis, xaxis);

            return new Matrix4x4(
            xaxis.X, xaxis.Y, xaxis.Z, //0.0,
            yaxis.X, yaxis.Y, yaxis.Z,//0.0, commenting out last column leads to the simpler affine constructor which has 4x3 arguments
             zaxis.X, zaxis.Y, zaxis.Z,//0.0,
            position.X, position.Y, position.Z //, 1.0
            );
        }

        /// <summary>
        /// Creates a rotation matrix from the given Quaternion rotation value.
        /// </summary>
        /// <param name="quaternion">The source Quaternion.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromQuaternion(Quaternion quaternion)
        {
            Matrix4x4 result;

            double xx = quaternion.X * quaternion.X;
            double yy = quaternion.Y * quaternion.Y;
            double zz = quaternion.Z * quaternion.Z;

            double xy = quaternion.X * quaternion.Y;
            double wz = quaternion.Z * quaternion.W;
            double xz = quaternion.Z * quaternion.X;
            double wy = quaternion.Y * quaternion.W;
            double yz = quaternion.Y * quaternion.Z;
            double wx = quaternion.X * quaternion.W;

            return new Matrix4x4(
            // first row
            1.0 - 2.0 * (yy + zz),
             2.0 * (xy + wz),
            2.0 * (xz - wy),
            //0.0, commenting out last column leads to the simpler affine constructor which has 4x3 arguments
            // second row
            2.0 * (xy - wz),
            1.0 - 2.0 * (zz + xx),
            2.0 * (yz + wx),
            //0.0,
            // third row
            2.0 * (xz + wy),
            2.0 * (yz - wx),
            1.0 - 2.0 * (yy + xx),
            //0.0,
            // fourth row
            0, 0, 0 //, 1
            );
        }

        /// <summary>
        /// Creates a rotation matrix from the specified yaw, pitch, and roll.
        /// </summary>
        /// <param name="yaw">Angle of rotation, in radians, around the Y-axis.</param>
        /// <param name="pitch">Angle of rotation, in radians, around the X-axis.</param>
        /// <param name="roll">Angle of rotation, in radians, around the Z-axis.</param>
        /// <returns>The rotation matrix.</returns>
        public static Matrix4x4 CreateFromYawPitchRoll(double yaw, double pitch, double roll)
        {
            Quaternion q = Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll);

            return Matrix4x4.CreateFromQuaternion(q);
        }

        /// <summary>
        /// Creates a Matrix that flattens geometry into a specified Plane as if casting a shadow from a specified light source.
        /// </summary>
        /// <param name="lightDirection">The direction from which the light that will cast the shadow is coming.</param>
        /// <param name="plane">The Plane onto which the new matrix should flatten geometry so as to cast a shadow.</param>
        /// <returns>A new Matrix that can be used to flatten geometry onto the specified plane from the specified direction.</returns>
        public static Matrix4x4 CreateShadow(Vector3 lightDirection, Plane plane)
        {
            Plane p = Plane.Normalize(plane);

            double dot = p.Normal.X * lightDirection.X + p.Normal.Y * lightDirection.Y + p.Normal.Z * lightDirection.Z;
            double a = -p.Normal.X;
            double b = -p.Normal.Y;
            double c = -p.Normal.Z;
            double d = -p.D;

            return new Matrix4x4(
                // first row
                a * lightDirection.X + dot, a * lightDirection.Y, a * lightDirection.Z, 0.0,
                //second row
                b * lightDirection.X, b * lightDirection.Y + dot, b * lightDirection.Z, 0.0,
                // third row
                c * lightDirection.X, c * lightDirection.Y, c * lightDirection.Z + dot, 0.0,
                // fourth row
                d * lightDirection.X, d * lightDirection.Y, d * lightDirection.Z, dot);
        }

        /// <summary>
        /// Creates a Matrix that reflects the coordinate system about a specified Plane.
        /// </summary>
        /// <param name="value">The Plane about which to create a reflection.</param>
        /// <returns>A new matrix expressing the reflection.</returns>
        public static Matrix4x4 CreateReflection(Plane value)
        {
            value = Plane.Normalize(value);

            double a = value.Normal.X;
            double b = value.Normal.Y;
            double c = value.Normal.Z;

            double fa = -2.0 * a;
            double fb = -2.0 * b;
            double fc = -2.0 * c;

            return new Matrix4x4(
                fa * a + 1.0, fb * a, fc * a, //0.0,
                fa * b, fb * b + 1.0, fc * b, //0.0,
                fa * c, fb * c, fc * c + 1.0, //0.0,
                fa * value.D, fb * value.D, fc * value.D //,1.0
                );
        }

        /// <summary>
        /// Calculates the determinant of the matrix.
        /// </summary>
        /// <returns>The determinant of the matrix.</returns>
        public double GetDeterminant()
        {
            // | a b c d |     | f g h |     | e g h |     | e f h |     | e f g |
            // | e f g h | = a | j k l | - b | i k l | + c | i j l | - d | i j k |
            // | i j k l |     | n o p |     | m o p |     | m n p |     | m n o |
            // | m n o p |
            //
            //   | f g h |
            // a | j k l | = a ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
            //   | n o p |
            //
            //   | e g h |
            // b | i k l | = b ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
            //   | m o p |
            //
            //   | e f h |
            // c | i j l | = c ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
            //   | m n p |
            //
            //   | e f g |
            // d | i j k | = d ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
            //   | m n o |
            //
            // Cost of operation
            // 17 adds and 28 muls.
            //
            // add: 6 + 8 + 3 = 17
            // mul: 12 + 16 = 28
            if (IsProjectiveTransform)
            {
                double a = M11, b = M12, c = M13, d = M14;
                double e = M21, f = M22, g = M23, h = M24;
                double i = M31, j = M32, k = M33, l = M34;
                double m = M41, n = M42, o = M43, p = M44;

                double kp_lo = k * p - l * o;
                double jp_ln = j * p - l * n;
                double jo_kn = j * o - k * n;
                double ip_lm = i * p - l * m;
                double io_km = i * o - k * m;
                double in_jm = i * n - j * m;

                return a * (f * kp_lo - g * jp_ln + h * jo_kn) -
                       b * (e * kp_lo - g * ip_lm + h * io_km) +
                       c * (e * jp_ln - f * ip_lm + h * in_jm) -
                       d * (e * jo_kn - f * io_km + g * in_jm);
            }
            else
            {
                return M11 * (M22 * M33 - M23 * M32) -
                       M12 * (M21 * M33 - M23 * M31) +
                       M13 * (M21 * M32 - M22 * M31);
            }
        }

        /// <summary>
        /// Attempts to calculate the inverse of the given matrix. If successful, result will contain the inverted matrix.
        /// </summary>
        /// <param name="matrix">The source matrix to invert.</param>
        /// <param name="result">If successful, contains the inverted matrix.</param>
        /// <returns>True if the source matrix could be inverted; False otherwise.</returns>
        public static bool Invert(Matrix4x4 matrix, out Matrix4x4 result)
        {
            //                                       -1
            // If you have matrix M, inverse Matrix M   can compute
            //
            //     -1       1
            //    M   = --------- A
            //            det(M)
            //
            // A is adjugate (adjoint) of M, where,
            //
            //      T
            // A = C
            //
            // C is Cofactor matrix of M, where,
            //           i + j
            // C   = (-1)      * det(M  )
            //  ij                    ij
            //
            //     [ a b c d ]
            // M = [ e f g h ]
            //     [ i j k l ]
            //     [ m n o p ]
            //
            // First Row
            //           2 | f g h |
            // C   = (-1)  | j k l | = + ( f ( kp - lo ) - g ( jp - ln ) + h ( jo - kn ) )
            //  11         | n o p |
            //
            //           3 | e g h |
            // C   = (-1)  | i k l | = - ( e ( kp - lo ) - g ( ip - lm ) + h ( io - km ) )
            //  12         | m o p |
            //
            //           4 | e f h |
            // C   = (-1)  | i j l | = + ( e ( jp - ln ) - f ( ip - lm ) + h ( in - jm ) )
            //  13         | m n p |
            //
            //           5 | e f g |
            // C   = (-1)  | i j k | = - ( e ( jo - kn ) - f ( io - km ) + g ( in - jm ) )
            //  14         | m n o |
            //
            // Second Row
            //           3 | b c d |
            // C   = (-1)  | j k l | = - ( b ( kp - lo ) - c ( jp - ln ) + d ( jo - kn ) )
            //  21         | n o p |
            //
            //           4 | a c d |
            // C   = (-1)  | i k l | = + ( a ( kp - lo ) - c ( ip - lm ) + d ( io - km ) )
            //  22         | m o p |
            //
            //           5 | a b d |
            // C   = (-1)  | i j l | = - ( a ( jp - ln ) - b ( ip - lm ) + d ( in - jm ) )
            //  23         | m n p |
            //
            //           6 | a b c |
            // C   = (-1)  | i j k | = + ( a ( jo - kn ) - b ( io - km ) + c ( in - jm ) )
            //  24         | m n o |
            //
            // Third Row
            //           4 | b c d |
            // C   = (-1)  | f g h | = + ( b ( gp - ho ) - c ( fp - hn ) + d ( fo - gn ) )
            //  31         | n o p |
            //
            //           5 | a c d |
            // C   = (-1)  | e g h | = - ( a ( gp - ho ) - c ( ep - hm ) + d ( eo - gm ) )
            //  32         | m o p |
            //
            //           6 | a b d |
            // C   = (-1)  | e f h | = + ( a ( fp - hn ) - b ( ep - hm ) + d ( en - fm ) )
            //  33         | m n p |
            //
            //           7 | a b c |
            // C   = (-1)  | e f g | = - ( a ( fo - gn ) - b ( eo - gm ) + c ( en - fm ) )
            //  34         | m n o |
            //
            // Fourth Row
            //           5 | b c d |
            // C   = (-1)  | f g h | = - ( b ( gl - hk ) - c ( fl - hj ) + d ( fk - gj ) )
            //  41         | j k l |
            //
            //           6 | a c d |
            // C   = (-1)  | e g h | = + ( a ( gl - hk ) - c ( el - hi ) + d ( ek - gi ) )
            //  42         | i k l |
            //
            //           7 | a b d |
            // C   = (-1)  | e f h | = - ( a ( fl - hj ) - b ( el - hi ) + d ( ej - fi ) )
            //  43         | i j l |
            //
            //           8 | a b c |
            // C   = (-1)  | e f g | = + ( a ( fk - gj ) - b ( ek - gi ) + c ( ej - fi ) )
            //  44         | i j k |
            //
            // Cost of operation
            // 53 adds, 104 muls, and 1 div.
            if (matrix.IsProjectiveTransform)
            {
                double a = matrix.M11, b = matrix.M12, c = matrix.M13, d = matrix.M14;
                double e = matrix.M21, f = matrix.M22, g = matrix.M23, h = matrix.M24;
                double i = matrix.M31, j = matrix.M32, k = matrix.M33, l = matrix.M34;
                double m = matrix.M41, n = matrix.M42, o = matrix.M43, p = matrix.M44;

                double kp_lo = k * p - l * o;
                double jp_ln = j * p - l * n;
                double jo_kn = j * o - k * n;
                double ip_lm = i * p - l * m;
                double io_km = i * o - k * m;
                double in_jm = i * n - j * m;

                double a11 = +(f * kp_lo - g * jp_ln + h * jo_kn);
                double a12 = -(e * kp_lo - g * ip_lm + h * io_km);
                double a13 = +(e * jp_ln - f * ip_lm + h * in_jm);
                double a14 = -(e * jo_kn - f * io_km + g * in_jm);

                double det = a * a11 + b * a12 + c * a13 + d * a14;

                if (Math.Abs(det) < double.Epsilon)
                {
                    result = new Matrix4x4(double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN);
                    return false;
                }

                double invDet = 1.0 / det;

                double gp_ho = g * p - h * o;
                double fp_hn = f * p - h * n;
                double fo_gn = f * o - g * n;
                double ep_hm = e * p - h * m;
                double eo_gm = e * o - g * m;
                double en_fm = e * n - f * m;
                double gl_hk = g * l - h * k;
                double fl_hj = f * l - h * j;
                double fk_gj = f * k - g * j;
                double el_hi = e * l - h * i;
                double ek_gi = e * k - g * i;
                double ej_fi = e * j - f * i;

                result = new Matrix4x4(
                    // first row
                    a11 * invDet,
                    -(b * kp_lo - c * jp_ln + d * jo_kn) * invDet,
                    +(b * gp_ho - c * fp_hn + d * fo_gn) * invDet,
                    -(b * gl_hk - c * fl_hj + d * fk_gj) * invDet,
                    // second row
                    a12 * invDet,
                    +(a * kp_lo - c * ip_lm + d * io_km) * invDet,
                    -(a * gp_ho - c * ep_hm + d * eo_gm) * invDet,
                    +(a * gl_hk - c * el_hi + d * ek_gi) * invDet,
                    // third row
                    a13 * invDet,
                    -(a * jp_ln - b * ip_lm + d * in_jm) * invDet,
                    +(a * fp_hn - b * ep_hm + d * en_fm) * invDet,
                    -(a * fl_hj - b * el_hi + d * ej_fi) * invDet,
                    // fourth row
                    a14 * invDet,
                    +(a * jo_kn - b * io_km + c * in_jm) * invDet,
                    -(a * fo_gn - b * eo_gm + c * en_fm) * invDet,
                    +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet
                );
                return true;
            }
            else
            {
                double a = matrix.M11, b = matrix.M12, c = matrix.M13;
                double e = matrix.M21, f = matrix.M22, g = matrix.M23;
                double i = matrix.M31, j = matrix.M32, k = matrix.M33;
                double m = matrix.M41, n = matrix.M42, o = matrix.M43;

                double jo_kn = j * o - k * n;
                double io_km = i * o - k * m;
                double in_jm = i * n - j * m;

                double a11 = +(f * matrix.M33 - g * matrix.M32);
                double a12 = -(e * matrix.M33 - g * matrix.M31);
                double a13 = +(e * matrix.M32 - f * matrix.M31);
                double a14 = -(e * jo_kn - f * io_km + g * in_jm);

                double det = a * a11 + b * a12 + c * a13;

                if (Math.Abs(det) < double.Epsilon)
                {
                    result = new Matrix4x4(double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN,
                                           double.NaN, double.NaN, double.NaN, double.NaN);
                    return false;
                }

                double invDet = 1.0 / det;

                double fo_gn = f * o - g * n;
                double eo_gm = e * o - g * m;
                double en_fm = e * n - f * m;
                double fk_gj = f * k - g * j;
                double ek_gi = e * k - g * i;
                double ej_fi = e * j - f * i;

                result = new Matrix4x4(
                    // first row
                    a11 * invDet,
                    -(b * matrix.M33 - c * matrix.M32) * invDet,
                    +(b * matrix.M23 - c * matrix.M22) * invDet,
                    0,
                    // second row
                    a12 * invDet,
                    +(a * matrix.M33 - c * matrix.M31) * invDet,
                    -(a * matrix.M23 - c * matrix.M21) * invDet,
                    0,
                    // third row
                    a13 * invDet,
                    -(a * matrix.M32 - b * matrix.M31) * invDet,
                    +(a * matrix.M22 - b * matrix.M21) * invDet,
                    0,
                    // fourth row
                    a14 * invDet,
                    +(a * jo_kn - b * io_km + c * in_jm) * invDet,
                    -(a * fo_gn - b * eo_gm + c * en_fm) * invDet,
                    +(a * fk_gj - b * ek_gi + c * ej_fi) * invDet
                );
                return true;
            }
        }


        private struct CanonicalBasis
        {
            public Vector3 Row0;
            public Vector3 Row1;
            public Vector3 Row2;
        };


        private struct VectorBasis
        {
            public unsafe Vector3* Element0;
            public unsafe Vector3* Element1;
            public unsafe Vector3* Element2;
        }

        /// <summary>
        /// Attempts to extract the scale, translation, and rotation components from the given scale/rotation/translation matrix.
        /// If successful, the out parameters will contained the extracted values.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="scale">The scaling component of the transformation matrix.</param>
        /// <param name="rotation">The rotation component of the transformation matrix.</param>
        /// <param name="translation">The translation component of the transformation matrix</param>
        /// <returns>True if the source matrix was successfully decomposed; False otherwise.</returns>
        public static bool Decompose(Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        {
            bool result = true;

            unsafe
            {
                fixed (Vector3* scaleBase = &scale)
                {
                    double* pfScales = (double*)scaleBase;
                    double det;

                    VectorBasis vectorBasis;
                    Vector3** pVectorBasis = (Vector3**)&vectorBasis;

                    Matrix4x4 matTemp = Matrix4x4.Identity;
                    CanonicalBasis canonicalBasis = default;
                    Vector3* pCanonicalBasis = &canonicalBasis.Row0;

                    canonicalBasis.Row0 = new Vector3(1.0, 0.0, 0.0);
                    canonicalBasis.Row1 = new Vector3(0.0, 1.0, 0.0);
                    canonicalBasis.Row2 = new Vector3(0.0, 0.0, 1.0);

                    translation = new Vector3(
                        matrix.M41,
                        matrix.M42,
                        matrix.M43);

                    pVectorBasis[0] = (Vector3*)&matTemp.M11;
                    pVectorBasis[1] = (Vector3*)&matTemp.M21;
                    pVectorBasis[2] = (Vector3*)&matTemp.M31;

                    *(pVectorBasis[0]) = new Vector3(matrix.M11, matrix.M12, matrix.M13);
                    *(pVectorBasis[1]) = new Vector3(matrix.M21, matrix.M22, matrix.M23);
                    *(pVectorBasis[2]) = new Vector3(matrix.M31, matrix.M32, matrix.M33);

                    scale.X = pVectorBasis[0]->Length();
                    scale.Y = pVectorBasis[1]->Length();
                    scale.Z = pVectorBasis[2]->Length();

                    uint a, b, c;
                    #region Ranking
                    double x = pfScales[0], y = pfScales[1], z = pfScales[2];
                    if (x < y)
                    {
                        if (y < z)
                        {
                            a = 2;
                            b = 1;
                            c = 0;
                        }
                        else
                        {
                            a = 1;

                            if (x < z)
                            {
                                b = 2;
                                c = 0;
                            }
                            else
                            {
                                b = 0;
                                c = 2;
                            }
                        }
                    }
                    else
                    {
                        if (x < z)
                        {
                            a = 2;
                            b = 0;
                            c = 1;
                        }
                        else
                        {
                            a = 0;

                            if (y < z)
                            {
                                b = 2;
                                c = 1;
                            }
                            else
                            {
                                b = 1;
                                c = 2;
                            }
                        }
                    }
                    #endregion

                    if (pfScales[a] < DecomposeEpsilon)
                    {
                        *(pVectorBasis[a]) = pCanonicalBasis[a];
                    }

                    *pVectorBasis[a] = Vector3.Normalize(*pVectorBasis[a]);

                    if (pfScales[b] < DecomposeEpsilon)
                    {
                        uint cc;
                        double fAbsX, fAbsY, fAbsZ;

                        fAbsX = Math.Abs(pVectorBasis[a]->X);
                        fAbsY = Math.Abs(pVectorBasis[a]->Y);
                        fAbsZ = Math.Abs(pVectorBasis[a]->Z);

                        #region Ranking
                        if (fAbsX < fAbsY)
                        {
                            if (fAbsY < fAbsZ)
                            {
                                cc = 0;
                            }
                            else
                            {
                                if (fAbsX < fAbsZ)
                                {
                                    cc = 0;
                                }
                                else
                                {
                                    cc = 2;
                                }
                            }
                        }
                        else
                        {
                            if (fAbsX < fAbsZ)
                            {
                                cc = 1;
                            }
                            else
                            {
                                if (fAbsY < fAbsZ)
                                {
                                    cc = 1;
                                }
                                else
                                {
                                    cc = 2;
                                }
                            }
                        }
                        #endregion

                        *pVectorBasis[b] = Vector3.Cross(*pVectorBasis[a], *(pCanonicalBasis + cc));
                    }

                    *pVectorBasis[b] = Vector3.Normalize(*pVectorBasis[b]);

                    if (pfScales[c] < DecomposeEpsilon)
                    {
                        *pVectorBasis[c] = Vector3.Cross(*pVectorBasis[a], *pVectorBasis[b]);
                    }

                    *pVectorBasis[c] = Vector3.Normalize(*pVectorBasis[c]);

                    det = matTemp.GetDeterminant();

                    // use Kramer's rule to check for handedness of coordinate system
                    if (det < 0.0)
                    {
                        // switch coordinate system by negating the scale and inverting the basis vector on the x-axis
                        pfScales[a] = -pfScales[a];
                        *pVectorBasis[a] = -(*pVectorBasis[a]);

                        det = -det;
                    }

                    det -= 1.0;
                    det *= det;

                    if ((DecomposeEpsilon < det))
                    {
                        // Non-SRT matrix encountered
                        rotation = Quaternion.Identity;
                        result = false;
                    }
                    else
                    {
                        // generate the quaternion from the matrix
                        rotation = Quaternion.CreateFromRotationMatrix(matTemp);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Transforms the given matrix by applying the given Quaternion rotation.
        /// </summary>
        /// <param name="value">The source matrix to transform.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed matrix.</returns>
        public static Matrix4x4 Transform(Matrix4x4 value, Quaternion rotation)
        {
            // Compute rotation matrix.
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

            double q11 = 1.0 - yy2 - zz2;
            double q21 = xy2 - wz2;
            double q31 = xz2 + wy2;

            double q12 = xy2 + wz2;
            double q22 = 1.0 - xx2 - zz2;
            double q32 = yz2 - wx2;

            double q13 = xz2 - wy2;
            double q23 = yz2 + wx2;
            double q33 = 1.0 - xx2 - yy2;

            return new Matrix4x4(
            // First row
             value.M11 * q11 + value.M12 * q21 + value.M13 * q31,
            value.M11 * q12 + value.M12 * q22 + value.M13 * q32,
            value.M11 * q13 + value.M12 * q23 + value.M13 * q33,
            value.M14,
            // Second row
            value.M21 * q11 + value.M22 * q21 + value.M23 * q31,
            value.M21 * q12 + value.M22 * q22 + value.M23 * q32,
            value.M21 * q13 + value.M22 * q23 + value.M23 * q33,
            value.M24,
            // Third row
            value.M31 * q11 + value.M32 * q21 + value.M33 * q31,
            value.M31 * q12 + value.M32 * q22 + value.M33 * q32,
            value.M31 * q13 + value.M32 * q23 + value.M33 * q33,
            value.M34,
            // Fourth row
            value.M41 * q11 + value.M42 * q21 + value.M43 * q31,
            value.M41 * q12 + value.M42 * q22 + value.M43 * q32,
            value.M41 * q13 + value.M42 * q23 + value.M43 * q33,
            value.M44);
        }

        /// <summary>
        /// Transposes the rows and columns of a matrix.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <returns>The transposed matrix.</returns>
        public static unsafe Matrix4x4 Transpose(Matrix4x4 matrix)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //var row1 = Sse.LoadVector128(&matrix.M11);
                //var row2 = Sse.LoadVector128(&matrix.M21);
                //var row3 = Sse.LoadVector128(&matrix.M31);
                //var row4 = Sse.LoadVector128(&matrix.M41);

                //var l12 = Sse.UnpackLow(row1, row2);
                //var l34 = Sse.UnpackLow(row3, row4);
                //var h12 = Sse.UnpackHigh(row1, row2);
                //var h34 = Sse.UnpackHigh(row3, row4);

                //Sse.Store(&matrix.M11, Sse.MoveLowToHigh(l12, l34));
                //Sse.Store(&matrix.M21, Sse.MoveHighToLow(l34, l12));
                //Sse.Store(&matrix.M31, Sse.MoveLowToHigh(h12, h34));
                //Sse.Store(&matrix.M41, Sse.MoveHighToLow(h34, h12));

                //return matrix;
            }
            return new Matrix4x4(
                matrix.M11, matrix.M21, matrix.M31, matrix.M41,
                matrix.M12, matrix.M22, matrix.M32, matrix.M42,
                matrix.M13, matrix.M23, matrix.M33, matrix.M43,
                matrix.M14, matrix.M24, matrix.M34, matrix.M44
                );
        }

        /// <summary>
        /// Linearly interpolates between the corresponding values of two matrices.
        /// </summary>
        /// <param name="matrix1">The first source matrix.</param>
        /// <param name="matrix2">The second source matrix.</param>
        /// <param name="amount">The relative weight of the second source matrix.</param>
        /// <returns>The interpolated matrix.</returns>
        public static unsafe Matrix4x4 Lerp(Matrix4x4 matrix1, Matrix4x4 matrix2, double amount)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //Vector128<double> amountVec = Vector128.Create(amount);
                //Sse.Store(&matrix1.M11, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M11), Sse.LoadVector128(&matrix2.M11), amountVec));
                //Sse.Store(&matrix1.M21, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M21), Sse.LoadVector128(&matrix2.M21), amountVec));
                //Sse.Store(&matrix1.M31, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M31), Sse.LoadVector128(&matrix2.M31), amountVec));
                //Sse.Store(&matrix1.M41, VectorMath.Lerp(Sse.LoadVector128(&matrix1.M41), Sse.LoadVector128(&matrix2.M41), amountVec));
                //return matrix1;
            }
            return new Matrix4x4(
            // First row
            matrix1.M11 + (matrix2.M11 - matrix1.M11) * amount,
            matrix1.M12 + (matrix2.M12 - matrix1.M12) * amount,
            matrix1.M13 + (matrix2.M13 - matrix1.M13) * amount,
            matrix1.M14 + (matrix2.M14 - matrix1.M14) * amount,

            // Second row
            matrix1.M21 + (matrix2.M21 - matrix1.M21) * amount,
            matrix1.M22 + (matrix2.M22 - matrix1.M22) * amount,
            matrix1.M23 + (matrix2.M23 - matrix1.M23) * amount,
            matrix1.M24 + (matrix2.M24 - matrix1.M24) * amount,

            // Third row
            matrix1.M31 + (matrix2.M31 - matrix1.M31) * amount,
            matrix1.M32 + (matrix2.M32 - matrix1.M32) * amount,
            matrix1.M33 + (matrix2.M33 - matrix1.M33) * amount,
            matrix1.M34 + (matrix2.M34 - matrix1.M34) * amount,

            // Fourth row
            matrix1.M41 + (matrix2.M41 - matrix1.M41) * amount,
            matrix1.M42 + (matrix2.M42 - matrix1.M42) * amount,
            matrix1.M43 + (matrix2.M43 - matrix1.M43) * amount,
            matrix1.M44 + (matrix2.M44 - matrix1.M44) * amount
            );
        }

        /// <summary>
        /// Returns a new matrix with the negated elements of the given matrix.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static Matrix4x4 Negate(Matrix4x4 value) => -value;

        /// <summary>
        /// Adds two matrices together.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The resulting matrix.</returns>
        public static Matrix4x4 Add(Matrix4x4 value1, Matrix4x4 value2) => value1 + value2;

        /// <summary>
        /// Subtracts the second matrix from the first.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the subtraction.</returns>
        public static Matrix4x4 Subtract(Matrix4x4 value1, Matrix4x4 value2) => value1 - value2;

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Matrix4x4 Multiply(Matrix4x4 value1, Matrix4x4 value2) => value1 * value2;

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling factor.</param>
        /// <returns>The scaled matrix.</returns>
        public static Matrix4x4 Multiply(Matrix4x4 value1, double value2) => value1 * value2;

        /// <summary>
        /// Returns a new matrix with the negated elements of the given matrix.
        /// </summary>
        /// <param name="value">The source matrix.</param>
        /// <returns>The negated matrix.</returns>
        public static unsafe Matrix4x4 operator -(Matrix4x4 value)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //Vector128<double> zero = Vector128<double>.Zero;
                //Sse.Store(&value.M11, Sse.Subtract(zero, Sse.LoadVector128(&value.M11)));
                //Sse.Store(&value.M21, Sse.Subtract(zero, Sse.LoadVector128(&value.M21)));
                //Sse.Store(&value.M31, Sse.Subtract(zero, Sse.LoadVector128(&value.M31)));
                //Sse.Store(&value.M41, Sse.Subtract(zero, Sse.LoadVector128(&value.M41)));

                //return value;
            }
            return new Matrix4x4(
                -value.M11, -value.M12, -value.M13, -value.M14,
                -value.M21, -value.M22, -value.M23, -value.M24,
                -value.M31, -value.M32, -value.M33, -value.M34,
                -value.M41, -value.M42, -value.M43, -value.M44
            );
        }

        /// <summary>
        /// Adds two matrices together.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The resulting matrix.</returns>
        public static unsafe Matrix4x4 operator +(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //Sse.Store(&value1.M11, Sse.Add(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)));
                //Sse.Store(&value1.M21, Sse.Add(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)));
                //Sse.Store(&value1.M31, Sse.Add(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)));
                //Sse.Store(&value1.M41, Sse.Add(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41)));
                //return value1;
            }
            return new Matrix4x4(
            value1.M11 + value2.M11, value1.M12 + value2.M12, value1.M13 + value2.M13, value1.M14 + value2.M14,
            value1.M21 + value2.M21, value1.M22 + value2.M22, value1.M23 + value2.M23, value1.M24 + value2.M24,
            value1.M31 + value2.M31, value1.M32 + value2.M32, value1.M33 + value2.M33, value1.M34 + value2.M34,
            value1.M41 + value2.M41, value1.M42 + value2.M42, value1.M43 + value2.M43, value1.M44 + value2.M44
            );
        }

        /// <summary>
        /// Subtracts the second matrix from the first.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the subtraction.</returns>
        public static unsafe Matrix4x4 operator -(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //Sse.Store(&value1.M11, Sse.Subtract(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)));
                //Sse.Store(&value1.M21, Sse.Subtract(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)));
                //Sse.Store(&value1.M31, Sse.Subtract(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)));
                //Sse.Store(&value1.M41, Sse.Subtract(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41)));
                //return value1;
            }
            return new Matrix4x4(
                value1.M11 - value2.M11, value1.M12 - value2.M12, value1.M13 - value2.M13, value1.M14 - value2.M14,
                value1.M21 - value2.M21, value1.M22 - value2.M22, value1.M23 - value2.M23, value1.M24 - value2.M24,
                value1.M31 - value2.M31, value1.M32 - value2.M32, value1.M33 - value2.M33, value1.M34 - value2.M34,
                value1.M41 - value2.M41, value1.M42 - value2.M42, value1.M43 - value2.M43, value1.M44 - value2.M44
                );
        }

        /// <summary>
        /// Multiplies a matrix by another matrix.
        /// </summary>
        /// <param name="value1">The first source matrix.</param>
        /// <param name="value2">The second source matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static unsafe Matrix4x4 operator *(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //var row = Sse.LoadVector128(&value1.M11);
                //Sse.Store(&value1.M11,
                //    Sse.Add(Sse.Add(Sse * Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                //                    Sse * Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                //            Sse.Add(Sse * Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                //                    Sse * Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                //// 0x00 is _MM_SHUFFLE(0,0,0,0), 0x55 is _MM_SHUFFLE(1,1,1,1), etc.
                //// TODO: Replace with a method once it's added to the API.

                //row = Sse.LoadVector128(&value1.M21);
                //Sse.Store(&value1.M21,
                //    Sse.Add(Sse.Add(Sse * Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                //                    Sse * Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                //            Sse.Add(Sse * Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                //                    Sse * Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                //row = Sse.LoadVector128(&value1.M31);
                //Sse.Store(&value1.M31,
                //    Sse.Add(Sse.Add(Sse * Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                //                    Sse * Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                //            Sse.Add(Sse * Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                //                    Sse * Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));

                //row = Sse.LoadVector128(&value1.M41);
                //Sse.Store(&value1.M41,
                //    Sse.Add(Sse.Add(Sse * Sse.Shuffle(row, row, 0x00), Sse.LoadVector128(&value2.M11)),
                //                    Sse * Sse.Shuffle(row, row, 0x55), Sse.LoadVector128(&value2.M21))),
                //            Sse.Add(Sse * Sse.Shuffle(row, row, 0xAA), Sse.LoadVector128(&value2.M31)),
                //                    Sse * Sse.Shuffle(row, row, 0xFF), Sse.LoadVector128(&value2.M41)))));
                //return value1;
            }
            if (value1.IsProjectiveTransform && value2.IsProjectiveTransform)
                return new Matrix4x4(
            // First row
            value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41,
            value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42,
            value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43,
            value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34 + value1.M14 * value2.M44,

            // Second row
            value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41,
            value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42,
            value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43,
            value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34 + value1.M24 * value2.M44,

            // Third row
            value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41,
            value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42,
            value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43,
            value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34 + value1.M34 * value2.M44,

            // Fourth row
            value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41,
            value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42,
            value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43,
            value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value1.M44 * value2.M44);

            if (value1.IsProjectiveTransform)
                return new Matrix4x4(
            // First row
            value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31 + value1.M14 * value2.M41,
            value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32 + value1.M14 * value2.M42,
            value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33 + value1.M14 * value2.M43,
            value1.M14,

            // Second row
            value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31 + value1.M24 * value2.M41,
            value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32 + value1.M24 * value2.M42,
            value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33 + value1.M24 * value2.M43,
            value1.M24,

            // Third row
            value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31 + value1.M34 * value2.M41,
            value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32 + value1.M34 * value2.M42,
            value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33 + value1.M34 * value2.M43,
            value1.M34,

            // Fourth row
            value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value1.M44 * value2.M41,
            value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value1.M44 * value2.M42,
            value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value1.M44 * value2.M43,
            value1.M44);

            if (value2.IsProjectiveTransform)
                return new Matrix4x4(
            // First row
            value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31,
            value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32,
            value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33,
            value1.M11 * value2.M14 + value1.M12 * value2.M24 + value1.M13 * value2.M34,

            // Second row
            value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31,
            value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32,
            value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33,
            value1.M21 * value2.M14 + value1.M22 * value2.M24 + value1.M23 * value2.M34,

            // Third row
            value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31,
            value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32,
            value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33,
            value1.M31 * value2.M14 + value1.M32 * value2.M24 + value1.M33 * value2.M34,

            // Fourth row
            value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value2.M41,
            value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value2.M42,
            value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value2.M43,
            value1.M41 * value2.M14 + value1.M42 * value2.M24 + value1.M43 * value2.M34 + value2.M44);


            return new Matrix4x4(
        // First row
        value1.M11 * value2.M11 + value1.M12 * value2.M21 + value1.M13 * value2.M31,
        value1.M11 * value2.M12 + value1.M12 * value2.M22 + value1.M13 * value2.M32,
        value1.M11 * value2.M13 + value1.M12 * value2.M23 + value1.M13 * value2.M33,
        // 0,

        // Second row
        value1.M21 * value2.M11 + value1.M22 * value2.M21 + value1.M23 * value2.M31,
        value1.M21 * value2.M12 + value1.M22 * value2.M22 + value1.M23 * value2.M32,
        value1.M21 * value2.M13 + value1.M22 * value2.M23 + value1.M23 * value2.M33,
        // 0,

        // Third row
        value1.M31 * value2.M11 + value1.M32 * value2.M21 + value1.M33 * value2.M31,
        value1.M31 * value2.M12 + value1.M32 * value2.M22 + value1.M33 * value2.M32,
        value1.M31 * value2.M13 + value1.M32 * value2.M23 + value1.M33 * value2.M33,
        // 0,

        // Fourth row
        value1.M41 * value2.M11 + value1.M42 * value2.M21 + value1.M43 * value2.M31 + value2.M41,
        value1.M41 * value2.M12 + value1.M42 * value2.M22 + value1.M43 * value2.M32 + value2.M42,
        value1.M41 * value2.M13 + value1.M42 * value2.M23 + value1.M43 * value2.M33 + value2.M43 //, 1
        );

        }

        /// <summary>
        /// Multiplies a matrix by a scalar value.
        /// </summary>
        /// <param name="value1">The source matrix.</param>
        /// <param name="value2">The scaling factor.</param>
        /// <returns>The scaled matrix.</returns>
        public static unsafe Matrix4x4 operator *(Matrix4x4 value1, double value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //Vector128<double> value2Vec = Vector128.Create(value2);
                //Sse.Store(&value1.M11, Sse * Sse.LoadVector128(&value1.M11), value2Vec));
                //Sse.Store(&value1.M21, Sse * Sse.LoadVector128(&value1.M21), value2Vec));
                //Sse.Store(&value1.M31, Sse * Sse.LoadVector128(&value1.M31), value2Vec));
                //Sse.Store(&value1.M41, Sse * Sse.LoadVector128(&value1.M41), value2Vec));
                //return value1;
            }
            if (value1.IsProjectiveTransform)
                return new Matrix4x4(
                value1.M11 * value2, value1.M12 * value2, value1.M13 * value2, value1.M14 * value2,
                value1.M21 * value2, value1.M22 * value2, value1.M23 * value2, value1.M24 * value2,
                value1.M31 * value2, value1.M32 * value2, value1.M33 * value2, value1.M34 * value2,
                value1.M41 * value2, value1.M42 * value2, value1.M43 * value2, value1.M44 * value2
                );

            return new Matrix4x4(
            value1.M11 * value2, value1.M12 * value2, value1.M13 * value2,
            value1.M21 * value2, value1.M22 * value2, value1.M23 * value2,
            value1.M31 * value2, value1.M32 * value2, value1.M33 * value2,
            value1.M41 * value2, value1.M42 * value2, value1.M43 * value2
            );

        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are equal; False otherwise.</returns>
        public static unsafe bool operator ==(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //return
                //    VectorMath.Equal(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)) &&
                //    VectorMath.Equal(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)) &&
                //    VectorMath.Equal(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)) &&
                //    VectorMath.Equal(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41));
            }

            return (value1.IsProjectiveTransform == value2.IsProjectiveTransform &&
                value1.M11 == value2.M11 && value1.M22 == value2.M22 && value1.M33 == value2.M33 && value1.M44 == value2.M44 && // Check diagonal element first for early out.
                    value1.M12 == value2.M12 && value1.M13 == value2.M13 && value1.M14 == value2.M14 && value1.M21 == value2.M21 &&
                    value1.M23 == value2.M23 && value1.M24 == value2.M24 && value1.M31 == value2.M31 && value1.M32 == value2.M32 &&
                    value1.M34 == value2.M34 && value1.M41 == value2.M41 && value1.M42 == value2.M42 && value1.M43 == value2.M43);
        }

        /// <summary>
        /// Returns a boolean indicating whether the given two matrices are not equal.
        /// </summary>
        /// <param name="value1">The first matrix to compare.</param>
        /// <param name="value2">The second matrix to compare.</param>
        /// <returns>True if the given matrices are not equal; False if they are equal.</returns>
        public static unsafe bool operator !=(Matrix4x4 value1, Matrix4x4 value2)
        {
            if (false) // COMMENTEDCHANGE (Sse.IsSupported)
            {
                //return
                //    VectorMath.NotEqual(Sse.LoadVector128(&value1.M11), Sse.LoadVector128(&value2.M11)) ||
                //    VectorMath.NotEqual(Sse.LoadVector128(&value1.M21), Sse.LoadVector128(&value2.M21)) ||
                //    VectorMath.NotEqual(Sse.LoadVector128(&value1.M31), Sse.LoadVector128(&value2.M31)) ||
                //    VectorMath.NotEqual(Sse.LoadVector128(&value1.M41), Sse.LoadVector128(&value2.M41));
            }

            return !(value1 == value2);
        }

        /// <summary>
        /// Returns a boolean indicating whether this matrix instance is equal to the other given matrix.
        /// </summary>
        /// <param name="other">The matrix to compare this instance to.</param>
        /// <returns>True if the matrices are equal; False otherwise.</returns>
        public bool Equals(Matrix4x4 other) => this == other;

        /// <summary>
        /// Returns a boolean indicating whether the given Object is equal to this matrix instance.
        /// </summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this matrix; False otherwise.</returns>
        public override bool Equals(object obj) => (obj is Matrix4x4 other) && (this == other);

        /// <summary>
        /// Returns a String representing this matrix instance.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture, "{{ {{M11:{0} M12:{1} M13:{2} M14:{3}}} {{M21:{4} M22:{5} M23:{6} M24:{7}}} {{M31:{8} M32:{9} M33:{10} M34:{11}}} {{M41:{12} M42:{13} M43:{14} M44:{15}}} }}",
                                 M11, M12, M13, M14,
                                 M21, M22, M23, M24,
                                 M31, M32, M33, M34,
                                 M41, M42, M43, M44);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return M11.GetHashCode() + M12.GetHashCode() + M13.GetHashCode() + M14.GetHashCode() +
                       M21.GetHashCode() + M22.GetHashCode() + M23.GetHashCode() + M24.GetHashCode() +
                       M31.GetHashCode() + M32.GetHashCode() + M33.GetHashCode() + M34.GetHashCode() +
                       M41.GetHashCode() + M42.GetHashCode() + M43.GetHashCode() + M44.GetHashCode();
            }
        }
    }
}
