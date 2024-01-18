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
            ConvexHull3D.Create(sphere1.Vertices.ToList(), out var cvxHull1, false);

            var sphere2 = new Sphere(new Vector3(10, 0, 0), 6, true);
            sphere2.Tessellate();
            ConvexHull3D.Create(sphere2.Vertices.ToList(), out var cvxHull2, false);
            var linesegment = new[] { new Vertex(new Vector3(10, 0, 0)), new Vertex(new Vector3(10, 10, 0)) };
            var d = cvxHull1.Vertices.DistanceBetween(linesegment, out var v);
        }
    }
}