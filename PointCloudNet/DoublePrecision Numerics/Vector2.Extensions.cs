using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    public static  class Vector2Extensions
    {
        /// <summary>Reinterprets a <see cref="Vector2" /> to a new <see cref="Vector4" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted to a new <see cref="Vector4" /> with the new elements zeroed.</returns>
    
        public static Vector4 AsVector4(this Vector2 value) => value.AsVector4();

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector2 value1, Vector2 value2)
        { return Vector2.Dot(value1, value2); }

        /// <summary>
        /// Returns a vector with the same direction as the given vector, but with a length of 1.
        /// </summary>
        /// <param name="value">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        public static Vector2 Normalize(this Vector2 value)
        { return Vector2.Normalize(value); }
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
            =>Vector2.TransformNormal(normal, matrix);
        // it seems that 
        //{ return Vector2.TransformNoTranslate(normal, matrix); }

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
            =>Vector2.TransformNormal(normal, matrix);
        //{ return Vector2.TransformNoTranslate(normal, matrix); }

        /// <summary>
        /// Transforms a vector by the given Quaternion rotation value.
        /// </summary>
        /// <param name="value">The source vector to be rotated.</param>
        /// <param name="rotation">The rotation to apply.</param>
        /// <returns>The transformed vector.</returns>
        public static Vector2 Transform(this Vector2 value, Quaternion rotation)
        { return Vector2.Transform(value, rotation); }

        /// <summary>
        /// Returns the z-value of the cross product of two vectors.
        /// Since the Vector2 is in the x-y plane, a 3D cross product
        /// only produces the z-value
        /// </summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The value of the z-coordinate from the cross product.</returns>
        public static double Cross(this Vector2 value1, Vector2 value2)
        {
            return value1.X * value2.Y
                   - value1.Y * value2.X;
        }

        /// <summary>
        /// Converts to an array.
        /// </summary>
        /// <param name="value1">The value1.</param>
        /// <returns>System.Double[].</returns>
        public static double[] ToArray(this Vector2 value1)
        { return new double[] { value1.X, value1.Y }; }

    }
}
