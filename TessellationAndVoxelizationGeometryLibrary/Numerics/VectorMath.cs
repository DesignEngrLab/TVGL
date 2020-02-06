// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
// COMMENTEDCHANGE using System.Runtime.Intrinsics;
// COMMENTEDCHANGE using System.Runtime.Intrinsics.X86;

namespace TVGL.Numerics  // COMMENTEDCHANGE namespace System.Numerics
{
    internal static class VectorMath
    {
        public static Vector128<double> Lerp(Vector128<double> a, Vector128<double> b, Vector128<double> t)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.Add(a, Sse.Multiply(Sse.Subtract(b, a), t));
        }

        public static bool Equal(Vector128<double> vector1, Vector128<double> vector2)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) == 0;
        }

        public static bool NotEqual(Vector128<double> vector1, Vector128<double> vector2)
        {
            Debug.Assert(Sse.IsSupported);
            return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) != 0;
        }
    }
}
