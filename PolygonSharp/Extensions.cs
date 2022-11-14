using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;

namespace PolygonSharp
{
    internal static class Extensions
    {
        internal static float Dot(this Vector2 vec1, Vector2 vec2)
        {
            return Vector2.Dot(vec1, vec2);
        }
        internal static bool IsNull(this Vector128<long> vector)
        {
            return !vector.Equals(NullVectorLong);
        }

        internal static bool IsNull(this Vector2 vec)
        {
            if (float.IsNaN(vec.X)) return true;
            if (float.IsNaN(vec.Y)) return true;
            return false;
        }

        internal static float Distance(this Vector2 vec1, Vector2 vec2)
        { return Vector2.Distance(vec1, vec2); }
        internal static double Cross(this Vector2 vec1, Vector2 vec2)
        {
            return vec1.X * vec2.Y - vec1.Y * vec2.X;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector128<float> Lerp(Vector128<float> a, Vector128<float> b, Vector128<float> t)
        {
            // This implementation is based on the DirectX Math Library XMVectorLerp method
            // https://github.com/microsoft/DirectXMath/blob/master/Inc/DirectXMathVector.inl
            if (AdvSimd.IsSupported)
            {
                return AdvSimd.FusedMultiplyAdd(a, AdvSimd.Subtract(b, a), t);
            }
            else if (Fma.IsSupported)
            {
                return Fma.MultiplyAdd(Sse.Subtract(b, a), t, a);
            }
            else if (Sse.IsSupported)
            {
                return Sse.Add(Sse.Multiply(a, Sse.Subtract(Vector128.Create(1.0f), t)), Sse.Multiply(b, t));
            }
            else
            {
                // Redundant test so we won't prejit remainder of this method on platforms without AdvSimd.
                throw new PlatformNotSupportedException();
            }
        }
    }
}