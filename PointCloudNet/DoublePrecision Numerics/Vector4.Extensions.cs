// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    public static unsafe partial class Vector
    {
        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Plane" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Plane" />.</returns>
    /*
        public static Plane AsPlane(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Plane>(ref value);
#else
            return Unsafe.BitCast<Vector4, Plane>(value);
#endif
        }
    */
        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Quaternion" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
    
        public static Quaternion AsQuaternion(this Vector4 value)
        {
#if MONO
            return Unsafe.As<Vector4, Quaternion>(ref value);
#else
            return Unsafe.BitCast<Vector4, Quaternion>(value);
#endif
        }

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
    
        public static Vector2 AsVector2(this Vector4 value) => value.AsVector256().AsVector2();

        /// <summary>Reinterprets a <see cref="Vector4" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
    
        public static Vector3 AsVector3(this Vector4 value) => value.AsVector256().AsVector3();

        /// <summary>Reinterprets a <see langword="Vector4" /> as a new <see cref="Vector128&lt;Double&gt;" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Double&gt;" />.</returns>
        public static Vector256<double> AsVector128(this Vector4 value)
        {
            return Unsafe.BitCast<Vector4, Vector256<double>>(value);
        }


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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector4 Cross(this Vector4 vector1, Vector4 vector2)
        {
            return new Vector4(
                vector1.Y * vector2.Z - vector1.Z * vector2.Y,
                vector1.Z * vector2.X - vector1.X * vector2.Z,
                vector1.X * vector2.Y - vector1.Y * vector2.X,
                vector1.W * vector2.W);
        }

        /// <summary>
        /// Returns the dot product of two vectors.
        /// </summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
        public static double Dot(this Vector4 vector1, Vector4 vector2)
        { return Vector4.Dot(vector1, vector2); }

    }
}
