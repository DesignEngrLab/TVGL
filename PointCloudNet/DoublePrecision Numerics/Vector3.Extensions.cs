// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using StarMathLib;
using System.Diagnostics;

using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Converts a <see cref="Vector3" /> to a new <see cref="Vector4" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns><paramref name="value" /> converted to a new <see cref="Vector4" /> with the new elements zeroed.</returns>
    
        public static Vector4 AsVector4(this Vector3 value) => value.AsVector256().AsVector4();

        /// <summary>Converts a <see cref="Vector3" /> to a new <see cref="Vector4" /> with the new elements undefined.</summary>
        /// <param name="value">The vector to convert.</param>
        /// <returns><paramref name="value" /> converted to a new <see cref="Vector4" /> with the new elements undefined.</returns>
    
        public static Vector4 AsVector4Unsafe(this Vector3 value) => value.AsVector256Unsafe().AsVector4();


        /// <summary>
        /// Returns the Euclidean distance between the two given points. Note that for fast applications where the
        /// actual distance (but rather the relative distance) is not needed, consider using DistanceSquared.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        public static double Distance(this Vector3 value1, Vector3 value2)
        { return Vector3.Distance(value1, value2); }

        /// <summary>
        /// Returns the Euclidean distance squared between the two given points. This is useful when the actual
        /// value of distance is not so imporant as the relative value to other distances. Taking the square-root
        /// is expensive when called many times.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        public static double DistanceSquared(this Vector3 value1, Vector3 value2)
        { return Vector3.DistanceSquared(value1, value2); }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static Vector3 Normalize(this Vector3 value)
        { return Vector3.Normalize(value); }

        /// <summary>
        /// Computes the cross product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        public static Vector3 Cross(this Vector3 vector1, Vector3 vector2)
        { return Vector3.Cross(vector1, vector2); }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 Transform(this Vector3 position, Matrix4x4 matrix)
        { return Vector3.Transform(position, matrix); }


        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normals vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 TransformNoTranslate(this Vector3 vector, Matrix4x4 matrix)
        //=>Vector3.TransformNormal(vector, matrix);  // is this the same thing?! I think the TransformNormal is misleading 
        // in System.Numerics
        {
            return new Vector3(
                vector.X * matrix.M11 + vector.Y * matrix.M21 + vector.Z * matrix.M31,
                vector.X * matrix.M12 + vector.Y * matrix.M22 + vector.Z * matrix.M32,
                vector.X * matrix.M13 + vector.Y * matrix.M23 + vector.Z * matrix.M33);
        }
        /// <summary>
        /// Multiplies a matrix by a vector. Note that the matrix is before the vector, so each term
        /// is the dot product of a row of the matrix with the vector.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Multiply(this Matrix3x3 matrix, Vector3 position)
        {
            return new Vector3(
                position.X * matrix.M11 + position.Y * matrix.M12 + position.Z * matrix.M13,
                position.X * matrix.M21 + position.Y * matrix.M22 + position.Z * matrix.M23,
                position.X * matrix.M31 + position.Y * matrix.M32 + position.Z * matrix.M33);
        }

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector3 vector1, Vector3 vector2)
        { return Vector3.Dot(vector1, vector2); }

        /// <summary>
        /// Merges the bi directional axes.
        /// </summary>
        /// <param name="vector1">The vector1.</param>
        /// <param name="vector2">The vector2.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 MergeBiDirectionalAxes(this Vector3 vector1, Vector3 vector2)
        {
            if (vector1.IsNull() || vector1.IsNegligible()) return vector2;
            if (vector2.IsNull() || vector2.IsNegligible()) return vector1;
            var result = (vector1.Dot(vector2) < 0) ? vector1 - vector2 : vector1 + vector2;
            return result.Normalize();
        }


        public static bool IsNull(this Vector3 vector)
        {
            return double.IsNaN(vector.X) || double.IsNaN(vector.Y) || double.IsNaN(vector.Z);
        }

        public static ComplexNumber[] GetEigenValuesAndVectors(this Matrix3x3 A,
            out Vector3[] eigenVectors)
        {
            var matrix = new double[,]
            {
                { A.M11, A.M12, A.M13 },
                { A.M21, A.M22, A.M23 },
                { A.M31, A.M32, A.M33 }
            };
            var eigenValues = matrix.GetEigenValuesAndVectors(out var eigenVectorsArrays);
            eigenVectors = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                eigenVectors[i] = new Vector3(eigenVectorsArrays[i][0].Real, eigenVectorsArrays[i][1].Real, eigenVectorsArrays[i][2].Real);
            }
            return eigenValues;
        }

    }
}
