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
                //var mOLD = q.TransformToXYPlaneOLD(out _);
                var mNEW = q.TransformToXYPlane(out _);
                //var err = (mNEW - mOLD).FrobeniusNorm();
                //if (err>1e-25)
                //{
                //    Console.WriteLine(err);
                //    Console.WriteLine(q);
                //    Console.WriteLine(mOLD);
                //    Console.WriteLine(mNEW);
                //}
            }
        }

        internal static void Test2()
        {
            var angle =  Math.PI / 2;
            var v1 = new Vector3(2e-12, 1, -7e-12).Normalize();
            var m1 = v1.GetOrthogonalDirection(angle);


            var v2 = Vector3.UnitY;
            var m2 = v2.GetOrthogonalDirection(angle);




            var v3 = new Vector3(0, -1, .1).Normalize();
            var m3 = v1.GetOrthogonalDirection(angle);


            var v4 = -Vector3.UnitY;
            var m4 = v2.GetOrthogonalDirection(angle);
        }
    }
}
