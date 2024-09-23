// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Extensions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using StarMathLib;
using System.Collections.Generic;
using System.Runtime.CompilerServices;


namespace TVGL
{
    /// <summary>
    /// Class VectorExtensions.
    /// </summary>
    public static class VectorExtensions
    {
        #region for Vector2
        /// <summary>
        /// Returns the Euclidean distance between the two given points. Note that for fast applications where the
        /// actual distance (but rather the relative distance) is not needed, consider using DistanceSquared.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        public static double Distance(this Vector2 value1, Vector2 value2)
        { return Vector2.Distance(value1, value2); }

        /// <summary>
        /// Returns the Euclidean distance squared between the two given points. This is useful when the actual
        /// value of distance is not so imporant as the relative value to other distances. Taking the square-root
        /// is expensive when called many times.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        public static double DistanceSquared(this Vector2 value1, Vector2 value2)
        { return Vector2.DistanceSquared(value1, value2); }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static Vector2 Normalize(this Vector2 value)
        { return Vector2.Normalize(value); }

        /// <summary>
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        public static Vector2 Reflect(this Vector2 vector, Vector2 normal)
        { return Vector2.Reflect(vector, normal); }

        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>Vector2.</returns>
        public static Vector2 Clamp(this Vector2 value1, Vector2 min, Vector2 max)
        { return Vector2.Clamp(value1, min, max); }

        /// <summary>
        /// Linearly interpolates between two vectors based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        public static Vector2 Lerp(this Vector2 value1, Vector2 value2, double amount)
        { return Vector2.Lerp(value1, value2, amount); }


        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 Transform(this Vector2 position, Matrix3x3 matrix)
        { return Vector2.Transform(position, matrix); }

        /// <summary>
        /// Transforms a vector normal by the given matrix.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 TransformNoTranslate(this Vector2 normal, Matrix3x3 matrix)
        { return Vector2.TransformNoTranslate(normal, matrix); }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 Transform(this Vector2 position, Matrix4x4 matrix)
        { return Vector2.Transform(position, matrix); }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 TransformNoTranslate(this Vector2 normal, Matrix4x4 matrix)
        { return Vector2.TransformNoTranslate(normal, matrix); }

        /// <summary>
        /// Transforms a vector by the given Quaternion rotation value.
        /// </summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 Transform(this Vector2 value, Quaternion rotation)
        { return Vector2.Transform(value, rotation); }

        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        public static Vector2 Add(this Vector2 left, Vector2 right)
        { return Vector2.Add(left, right); }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        public static Vector2 Subtract(this Vector2 left, Vector2 right)
        { return Vector2.Subtract(left, right); }


        /// <summary>
        /// Multiplies two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        public static Vector2 Multiply(this Vector2 left, Vector2 right)
        { return Vector2.Multiply(left, right); }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector2 Multiply(this Vector2 left, double right)
        { return Vector2.Multiply(left, right); }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector2 Multiply(this double left, Vector2 right)
        { return Vector2.Multiply(left, right); }

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        public static Vector2 Divide(this Vector2 left, Vector2 right)
        { return Vector2.Divide(left, right); }

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        public static Vector2 Divide(this Vector2 left, double divisor)
        { return Vector2.Divide(left, divisor); }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector2 Negate(this Vector2 value)
        { return Vector2.Negate(value); }


        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector2 value1, Vector2 value2)
        { return Vector2.Dot(value1, value2); }

        /// <summary>
        /// Returns the z-value of the cross product of two vectors.
        /// Since the Vector2 is in the x-y plane, a 3D cross product
        /// only produces the z-value
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The value of the z-coordinate from the cross product.</returns>
        public static double Cross(this Vector2 value1, Vector2 value2)
        { return Vector2.Cross(value1, value2); }

        /// <summary>
        /// Converts to an array.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>System.Double[].</returns>
        public static double[] ToArray(this Vector2 value1)
        { return new double[] { value1.X, value1.Y }; }

        #endregion

        #region Vector3

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
        /// Returns the reflection of a vector off a surface that has the specified normal.
        /// </summary>
        /// <param name="vector">The source vector.</param>
        /// <param name="normal">The normal of the surface being reflected off.</param>
        /// <returns>The reflected vector.</returns>
        public static Vector3 Reflect(this Vector3 vector, Vector3 normal)
        { return Vector3.Reflect(vector, normal); }


        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The restricted vector.</returns>
        public static Vector3 Clamp(this Vector3 value1, Vector3 min, Vector3 max)
        { return Vector3.Clamp(value1, min, max); }

        /// <summary>
        /// Linearly interpolates between two vectors based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        public static Vector3 Lerp(this Vector3 value1, Vector3 value2, double amount)
        { return Vector3.Lerp(value1, value2, amount); }

        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 Transform(this Vector3 position, Matrix4x4 matrix)
        { return Vector3.Multiply(position, matrix); }

        /// <summary>
        /// Pre-multiplies the vector to the matrix. Here, the vector is treated
        /// as a single row, so the result is also a single-row vector. Each cell
        /// is a dot-product between the vector and the column of the matrix.
        /// This is the same as transforming by the matrix.
        /// </summary>
        /// <param name="position">The vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 Multiply(this Vector3 position, Matrix3x3 matrix)
        { return Vector3.Multiply(position, matrix); }

        /// <summary>
        /// Pre-multiplies the vector to the matrix. Here, the vector is treated
        /// as a single row, so the result is also a single-row vector. Each cell
        /// is a dot-product between the vector and the column of the matrix.
        /// This is the same as transforming by the matrix.
        /// </summary>
        /// <param name="position">The vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 Multiply(this Vector3 position, Matrix4x4 matrix)
        { return Vector3.Multiply(position, matrix); }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 TransformNoTranslate(this Vector3 position, Matrix4x4 matrix)
        { return Vector3.TransformNoTranslate(position, matrix); }

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
        /// Transforms a vector by the given Quaternion rotation value.
        /// </summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector3 Transform(this Vector3 value, Quaternion rotation)
        { return Vector3.Multiply(value, rotation); }

        /// <summary>
        /// Adds two vectors together.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        public static Vector3 Add(this Vector3 left, Vector3 right)
        { return Vector3.Add(left, right); }

        /// <summary>
        /// Subtracts the second vector from the first.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        public static Vector3 Subtract(this Vector3 left, Vector3 right)
        { return Vector3.Subtract(left, right); }

        /// <summary>
        /// Multiplies two vectors together. It produces the components of a dot product
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        public static Vector3 Multiply(this Vector3 left, Vector3 right)
        { return Vector3.Multiply(left, right); }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector3 Multiply(this Vector3 left, double right)
        { return Vector3.Multiply(left, right); }

        /// <summary>
        /// Multiplies a vector by the given scalar.
        /// </summary>
        /// <param name="left">The scalar value.</param>
        /// <param name="right">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        public static Vector3 Multiply(this double left, Vector3 right)
        { return Vector3.Multiply(left, right); }

        /// <summary>
        /// Divides the first vector by the second.
        /// </summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        public static Vector3 Divide(this Vector3 left, Vector3 right)
        { return Vector3.Divide(left, right); }

        /// <summary>
        /// Divides the vector by the given scalar.
        /// </summary>
        /// <param name="left">The source vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        public static Vector3 Divide(this Vector3 left, double divisor)
        { return Vector3.Divide(left, divisor); }

        /// <summary>
        /// Negates a given vector.
        /// </summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        public static Vector3 Negate(this Vector3 value)
        { return Vector3.Negate(value); }

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector3 vector1, Vector3 vector2)
        { return Vector3.Dot(vector1, vector2); }

        /// <summary>
        /// Returns the dot product of two vertex and the vector.
        /// </summary>
        /// <param name="vector1">The vertex or position in 3D space</param>
        /// <param name="vector2">The vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this IVector3D vector1, IVector3D vector2)
        {
            if (vector1 == null) return double.NaN;
            return vector1.X * vector2.X +
                   vector1.Y * vector2.Y +
                   vector1.Z * vector2.Z;
        }

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
        /// <summary>
        /// Converts to an array.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>System.Double[].</returns>
        public static double[] ToArray(this Vector3 value1)
        { return new double[] { value1.X, value1.Y, value1.Z }; }

        #endregion

        #region Vector4

        /// <summary>
        /// Returns a vector whose elements are the absolute values of each of the specified vector's elements.
        /// </summary>
        /// <param name="vector1">The vector1.</param>
        /// <returns>The absolute value vector.</returns>
        public static Vector4 Abs(this Vector4 vector1)
        {
            return Vector4.Abs(vector1);
        }
        /// <summary>
        /// Returns the Euclidean distance between the two given points. Note that for fast applications where the
        /// actual distance (but rather the relative distance) is not needed, consider using DistanceSquared.
        /// </summary>
        /// <param name="vector1">The vector1.</param>
        /// <param name="vector2">The vector2.</param>
        /// <returns>The distance.</returns>
        public static double Distance(this Vector4 vector1, Vector4 vector2)
        {
            return Vector4.Distance(vector1, vector2);
        }

        /// <summary>
        /// Returns the Euclidean distance squared between the two given points. This is useful when the actual
        /// value of distance is not so imporant as the relative value to other distances. Taking the square-root
        /// is expensive when called many times.
        /// </summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        public static double DistanceSquared(this Vector4 value1, Vector4 value2)
        { return Vector4.DistanceSquared(value1, value2); }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static Vector4 Normalize(this Vector4 value)
        { return Vector4.Normalize(value); }


        /// <summary>
        /// Computes the cross product of two vectors. Note that this is really
        /// the 3D cross product, but the W term that scales each vector is simply
        /// the product of the two weights. This makes correct geometric sense in 3D
        /// space.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The cross product.</returns>
        public static Vector4 Cross(this Vector4 vector1, Vector4 vector2)
        { return Vector4.Cross(vector1, vector2); }

        /// <summary>
        /// Restricts a vector between a min and max value.
        /// </summary>
        /// <param name="value1">The source vector.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The restricted vector.</returns>
        public static Vector4 Clamp(this Vector4 value1, Vector4 min, Vector4 max)
        { return Vector4.Clamp(value1, min, max); }

        /// <summary>
        /// Linearly interpolates between two vectors based on the given weighting.
        /// </summary>
        /// <param name="value1">The first source vector.</param>
        /// <param name="value2">The second source vector.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of the second source vector.</param>
        /// <returns>The interpolated vector.</returns>
        public static Vector4 Lerp(this Vector4 value1, Vector4 value2, double amount)
        { return Vector4.Lerp(value1, value2, amount); }


        /// <summary>
        /// Transforms a vector by the given matrix.
        /// </summary>
        /// <param name="position">The position vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Transform(this Vector4 position, Matrix4x4 matrix)
        { return Vector4.Multiply(position, matrix); }

        /// <summary>
        /// Pre-multiplies the vector to the matrix. Here, the vector is treated
        /// as a single row, so the result is also a single-row vector. Each cell
        /// is a dot-product between the vector and the column of the matrix.
        /// This is the same as transforming by the matrix.
        /// </summary>
        /// <param name="position">The vector.</param>
        /// <param name="matrix">The matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 Multiply(this Vector4 position, Matrix4x4 matrix)
        { return Vector4.Multiply(position, matrix); }

        /// <summary>
        /// Transforms a vector by the given matrix without the translation component.
        /// This is often used for transforming normals, however note that proper transformations
        /// of normal vectors requires that the input matrix be the transpose of the Inverse of that matrix.
        /// </summary>
        /// <param name="position">The source vector.</param>
        /// <param name="matrix">The transformation matrix.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector4 TransformNoTranslate(this Vector4 position, Matrix4x4 matrix)
        { return Vector4.TransformNoTranslate(position, matrix); }


        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector4 vector1, Vector4 vector2)
        { return Vector4.Dot(vector1, vector2); }

        /// <summary>
        /// Returns the dot product of two vertex and the vector.
        /// </summary>
        /// <param name="vector1">The vertex or position in 3D space</param>
        /// <param name="vector2">The vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this IVector3D vector1, Vector4 vector2)
        {
            if (vector1 == null) return double.NaN;
            return vector1.X * vector2.X +
                   vector1.Y * vector2.Y +
                   vector1.Z * vector2.Z +
                   vector2.W;
        }

        /// <summary>
        /// Converts to an array.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>System.Double[].</returns>
        public static double[] ToArray(this Vector4 value1)
        { return new double[] { value1.X, value1.Y, value1.Z, value1.W }; }

        #endregion

        #region Matrix3x3, Matrix4x4, Quaternion, and Plane

        /// <summary>
        /// Transposes the specified matrix. Recall that this flips the matrix about its diagonal (rows
        /// become columns and columns become rows)
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>Matrix3x3.</returns>
        public static Matrix3x3 Transpose(this Matrix3x3 matrix)
        { return Matrix3x3.Transpose(matrix); }

        /// <summary>
        /// Transposes the specified matrix. Recall that this flips the matrix
        /// about its diagonal (rows become columns and columns become rows).
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <returns>The transposed matrix.</returns>
        public static Matrix4x4 Transpose(this Matrix4x4 matrix)
        { return Matrix4x4.Transpose(matrix); }

        /// <summary>
        /// Inverts the matrix. For geometric transformations this the
        /// inverse does the reverse movement.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <returns>A Matrix4x4.</returns>
        public static Matrix4x4 Inverse(this Matrix4x4 matrix)
        {
            if (Matrix4x4.Invert(matrix, out var result))
                return result;
            else return Matrix4x4.Null;
        }

        #region Solve Ax=b
        /// <summary>
        /// Solves for the value x in Ax=b. This is also represented as the
        /// backslash operation
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="b">The b.</param>
        /// <returns>Vector4.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Solve(this Matrix3x3 matrix, Vector3 b)
        {
            if (!Matrix3x3.Invert(matrix, out var invert))
                return Vector3.Null;
            return invert.Multiply(b);
        }
        #endregion

        /// <summary>
        /// Attempts to extract the scale, translation, and rotation components from the given scale/rotation/translation matrix.
        /// If successful, the out parameters will contained the extracted values.
        /// </summary>
        /// <param name="matrix">The source matrix.</param>
        /// <param name="scale">The scaling component of the transformation matrix.</param>
        /// <param name="rotation">The rotation component of the transformation matrix.</param>
        /// <param name="translation">The translation component of the transformation matrix</param>
        /// <returns>True if the source matrix was successfully decomposed; False otherwise.</returns>
        public static bool Decompose(this Matrix4x4 matrix, out Vector3 scale, out Quaternion rotation, out Vector3 translation)
        { return Matrix4x4.Decompose(matrix, out scale, out rotation, out translation); }

        /// <summary>
        /// Transforms the given matrix by applying the given Quaternion rotation.
        /// </summary>
        /// <param name="value">The source matrix to transform.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed matrix.</returns>
        public static Matrix4x4 Transform(this Matrix4x4 value, Quaternion rotation)
        { return Matrix4x4.Transform(value, rotation); }

        /// <summary>
        /// Divides each component of the Quaternion by the length of the Quaternion.
        /// </summary>
        /// <param name="value">The source Quaternion.</param>
        /// <returns>The normalized Quaternion.</returns>
        public static Quaternion Normalize(this Quaternion value)
        { return Quaternion.Normalize(value); }

        /// <summary>
        /// Creates the conjugate of a specified Quaternion.
        /// </summary>
        /// <param name="value">The Quaternion of which to return the conjugate.</param>
        /// <returns>A new Quaternion that is the conjugate of the specified one.</returns>
        public static Quaternion Conjugate(this Quaternion value)
        { return Quaternion.Conjugate(value); }

        /// <summary>
        /// Returns the Inverse of a Quaternion.
        /// </summary>
        /// <param name="value">The source Quaternion.</param>
        /// <returns>The inverted Quaternion.</returns>
        public static Quaternion Inverse(this Quaternion value)
        { return Quaternion.Inverse(value); }

        /// <summary>
        /// Calculates the dot product of two Quaternions.
        /// </summary>
        /// <param name="quaternion1">The first source Quaternion.</param>
        /// <param name="quaternion2">The second source Quaternion.</param>
        /// <returns>The dot product of the Quaternions.</returns>
        public static double Dot(this Quaternion quaternion1, Quaternion quaternion2)
        { return Quaternion.Dot(quaternion1, quaternion2); }


        /// <summary>
        /// Concatenates two Quaternions; the result represents the value1 rotation followed by the value2 rotation.
        /// </summary>
        /// <param name="quaternion1">The first Quaternion rotation in the series.</param>
        /// <param name="quaternion2">The second Quaternion rotation in the series.</param>
        /// <returns>A new Quaternion representing the concatenation of the value1 rotation followed by the value2 rotation.</returns>
        public static Quaternion Concatenate(this Quaternion quaternion1, Quaternion quaternion2)
        { return Quaternion.Concatenate(quaternion1, quaternion2); }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the normal vector of this Plane plus the distance (D) value of the Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The Vector3.</param>
        /// <returns>The resulting value.</returns>
        public static double DotCoordinate(this Plane plane, Vector3 value)
        { return Plane.DotCoordinate(plane, value); }

        /// <summary>
        /// Returns the dot product of a specified Vector3 and the Normal vector of this Plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="value">The Vector3.</param>
        /// <returns>The resulting dot product.</returns>
        public static double DotNormal(this Plane plane, Vector3 value)
        { return Plane.DotNormal(plane, value); }


        #region Eigenvalues and EigenVectors

        public static ComplexNumber[] GetEigenValuesAndVectors(this Matrix3x3 A,
            out ComplexNumber[][] eigenVectors)
        {
            return StarMath.GetEigenValuesAndVectors3(A.M11, A.M12, A.M13,
                A.M21, A.M22, A.M23,
                A.M31, A.M32, A.M33, out eigenVectors);
        }
        public static ComplexNumber[] GetEigenValues(this Matrix3x3 A)
        {
            return StarMath.GetEigenValues3(A.M11, A.M12, A.M13,
                A.M21, A.M22, A.M23,
                A.M31, A.M32, A.M33);
        }
        public static List<double> GetRealEigenValues(this Matrix3x3 A)
        {
            var eigValues = StarMath.GetEigenValues3(A.M11, A.M12, A.M13,
                A.M21, A.M22, A.M23,
                A.M31, A.M32, A.M33);
            var realEigenValues = new List<double>();
            if (eigValues[0].IsRealNumber)
                realEigenValues.Add(eigValues[0].Real);
            if (eigValues[1].IsRealNumber)
                realEigenValues.Add(eigValues[1].Real);
            if (eigValues[2].IsRealNumber)
                realEigenValues.Add(eigValues[2].Real);
            return realEigenValues;
        }
        public static ComplexNumber[][] GetEigenVectors(this Matrix3x3 A, ComplexNumber[] eigenValues)
        {
            return StarMath.GetEigenVectors3(A.M11, A.M12, A.M13,
                A.M21, A.M22, A.M23,
                A.M31, A.M32, A.M33, eigenValues);
        }
        public static ComplexNumber[] GetEigenVector(this Matrix3x3 A, ComplexNumber eigenValue)
        {
            return StarMath.GetEigenVector3(A.M11, A.M12, A.M13,
                A.M21, A.M22, A.M23,
                A.M31, A.M32, A.M33, eigenValue);
        }


        public static ComplexNumber[] GetEigenValuesAndVectors(this Matrix4x4 A,
            out ComplexNumber[][] eigenVectors)
        {
            return StarMath.GetEigenValuesAndVectors4(A.M11, A.M12, A.M13,A.M14,
                A.M21, A.M22, A.M23, A.M24,
                A.M31, A.M32, A.M33, A.M34,
                A.M41, A.M42, A.M43, A.M44, out eigenVectors);
        }
        public static ComplexNumber[] GetEigenValues(this Matrix4x4 A)
        {
            return StarMath.GetEigenValues4(A.M11, A.M12, A.M13, A.M14,
                A.M21, A.M22, A.M23, A.M24,
                A.M31, A.M32, A.M33, A.M34,
                A.M41, A.M42, A.M43, A.M44);
        }
        public static List<double> GetRealEigenValues(this Matrix4x4 A)
        {
            var eigValues = StarMath.GetEigenValues4(A.M11, A.M12, A.M13, A.M14,
                A.M21, A.M22, A.M23, A.M24,
                A.M31, A.M32, A.M33, A.M34,
                A.M41, A.M42, A.M43, A.M44);
            var realEigenValues = new List<double>();
            if (eigValues[0].IsRealNumber)
                realEigenValues.Add(eigValues[0].Real);
            if (eigValues[1].IsRealNumber)
                realEigenValues.Add(eigValues[1].Real);
            if (eigValues[2].IsRealNumber)
                realEigenValues.Add(eigValues[2].Real);
            if (eigValues[3].IsRealNumber)
                realEigenValues.Add(eigValues[3].Real);
            return realEigenValues;
        }
        public static ComplexNumber[][] GetEigenVectors(this Matrix4x4 A, ComplexNumber[] eigenValues)
        {
            return StarMath.GetEigenVectors4(A.M11, A.M12, A.M13, A.M14,
                A.M21, A.M22, A.M23, A.M24,
                A.M31, A.M32, A.M33, A.M34,
                A.M41, A.M42, A.M43, A.M44, eigenValues);
        }
        public static ComplexNumber[] GetEigenVector(this Matrix4x4 A, ComplexNumber eigenValue)
        {
            return StarMath.GetEigenVector4(A.M11, A.M12, A.M13, A.M14,
                A.M21, A.M22, A.M23, A.M24,
                A.M31, A.M32, A.M33, A.M34,
                A.M41, A.M42, A.M43, A.M44, eigenValue);
        }
        #endregion
        #endregion
    }
}
