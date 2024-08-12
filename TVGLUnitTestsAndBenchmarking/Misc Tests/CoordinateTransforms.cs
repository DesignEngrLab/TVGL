using System;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking.Misc_Tests
{
    internal static class CoordinateTransforms
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        internal static void Test1()
        {
            for (int i = 0; i < 1000; i++)
            {
                var q = new Vector3(r1, r1, r1);
                if (i % 2 == 0) q = q.Normalize();
                var mOLD = q.TransformToXYPlaneOLD(out _);
                var mNEW = q.TransformToXYPlane(out _);
                var err = (mNEW - mOLD).FrobeniusNorm();
                if (err>1e-25)
                {
                    Console.WriteLine(err);
                    Console.WriteLine(q);
                    Console.WriteLine(mOLD);
                    Console.WriteLine(mNEW);
                }
            }
        }
    }
}
