using System.Runtime.Intrinsics;

namespace PointCloud.Numerics
{
    internal static class VectorExtensions
    {
        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector128&lt;Single&gt;" />, leaving the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        public static Vector128<double> AsVector128Unsafe(this Vector2 value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector128<double> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector128&lt;Single&gt;" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" /> with the new elements zeroed.</returns>
        public static Vector128<double> AsVector128(this Vector2 value) => Vector128.Create<double>([value.X, value.Y]);


        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this Vector128<double> value)
        {
            ref byte address = ref Unsafe.As<Vector128<double>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector2>(ref address);
        }

        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector256&lt;Single&gt;" />, leaving the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector256&lt;Single&gt;" />.</returns>
        public static Vector256<double> AsVector256Unsafe(this Vector2 value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector256<double> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<double>, byte>(ref result), value);
            return result;
        }

        /// <summary>Reinterprets a <see langword="Vector2" /> as a new <see cref="Vector256&lt;Single&gt;" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector256&lt;Single&gt;" /> with the new elements zeroed.</returns>
        public static Vector256<double> AsVector256(this Vector2 value) => Vector256.Create<double>([value.X, value.Y]);

        /// <summary>Reinterprets a <see langword="Vector4" /> as a new <see cref="Vector256&lt;Single&gt;" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector256&lt;Single&gt;" />.</returns>
        public static Vector256<double> AsVector256(this Vector4 value)
        {
            return Unsafe.BitCast<Vector4, Vector256<double>>(value);
        }
        /// <summary>Reinterprets a <see langword="Vector256&lt;Single&gt;" /> as a new <see cref="Vector2" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector2" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 AsVector2(this Vector256<double> value)
        {
            ref byte address = ref Unsafe.As<Vector256<double>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector2>(ref address);
        }


        /// <summary>Reinterprets a <see langword="Vector3" /> as a new <see cref="Vector256&lt;Single&gt;" />, leaving the new elements undefined.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector256&lt;Single&gt;" />.</returns>
        public static Vector256<double> AsVector256Unsafe(this Vector3 value)
        {
            // This relies on us stripping the "init" flag from the ".locals"
            // declaration to let the upper bits be uninitialized.

            Unsafe.SkipInit(out Vector256<double> result);
            Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<double>, byte>(ref result), value);
            return result;
        }


        /// <summary>Reinterprets a <see langword="Vector3" /> as a new <see cref="Vector256&lt;Single&gt;" /> with the new elements zeroed.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector256&lt;Single&gt;" /> with the new elements zeroed.</returns>
        public static Vector256<double> AsVector256(this Vector3 value) => Vector4.Create(value, 0).AsVector256();

        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector4" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector4" />.</returns>
        public static Vector4 AsVector4(this Vector256<double> value)
        {
            return Unsafe.BitCast<Vector256<double>, Vector4>(value);
        }



        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Vector3" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Vector3" />.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 AsVector3(this Vector256<double> value)
        {
            ref byte address = ref Unsafe.As<Vector256<double>, byte>(ref value);
            return Unsafe.ReadUnaligned<Vector3>(ref address);
        }


        /// <summary>Reinterprets a <see cref="Quaternion" /> as a new <see langword="Vector128&lt;Single&gt;" />.</summary>
        /// <param name="value">The quaternion to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see langword="Vector128&lt;Single&gt;" />.</returns>
        public static Vector256<double> AsVector256(this Quaternion value)
        {
            return Unsafe.BitCast<Quaternion, Vector256<double>>(value);
        }
        /// <summary>Reinterprets a <see langword="Vector128&lt;Single&gt;" /> as a new <see cref="Quaternion" />.</summary>
        /// <param name="value">The vector to reinterpret.</param>
        /// <returns><paramref name="value" /> reinterpreted as a new <see cref="Quaternion" />.</returns>
        public static Quaternion AsQuaternion(this Vector256<double> value)
        {
            return Unsafe.BitCast<Vector256<double>, Quaternion>(value);
        }
    }
}
