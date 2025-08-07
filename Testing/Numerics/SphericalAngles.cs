using System;
using Xunit;

using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class TVGLNumericsTests
    {
        static double r1 => 2.0 * r.NextDouble() - 1.0;

        [Fact]
        public static void SphericalAngleTest1()
        {
            for (int i = 0; i < 1000; i++)
            {
                var v1 = new Vector3(r1, r1, r1).Normalize();
                var sap = new SphericalAnglePair(v1);
                var v2 = sap.ToVector3();
                if (v1.IsPracticallySame(v2)) continue;
                Console.WriteLine($"v1 = {v1} v2 = {v2}");
            }
        }

    }
}
