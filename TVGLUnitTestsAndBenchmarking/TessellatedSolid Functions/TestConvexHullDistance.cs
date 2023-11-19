using System;
#if !PRESENT
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;
#endif


using TVGL;
using System.IO;
using System.Linq;


namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class GJK_Testing
    {
        public static void Test1()
        {

            var sphere1 = new Sphere(new Vector3(0, 0, 0), 6, true);
            sphere1.Tessellate();
            var cvxHull1 = ConvexHull.Create(sphere1.Vertices.ToList());

            var sphere2 = new Sphere(new Vector3(10, 0, 0), 6, true);
            sphere2.Tessellate();
            var cvxHull2 = ConvexHull.Create(sphere2.Vertices.ToList());
            var linesegment = new[] { new Vector3(10, 0, 0), new Vector3(10, 10, 0) };
            var d = cvxHull1.Result.DistanceBetween(linesegment, out var v);
        }
    }
}