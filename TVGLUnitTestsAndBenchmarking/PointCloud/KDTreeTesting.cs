using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.PointCloud;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class KDTreeTesting
    {
        internal static void Test1()
        {
            var points = new List<Vector2> {
                new Vector2(3, 1),
                new Vector2(2, 6),
                new Vector2(5,4),
                new Vector2(8,7),
                new Vector2(10,2),
                new Vector2(13,3),
            };
            var tree = new KDTree<Vector2, object>(2, points);
            var nearest = tree.FindNearest(new Vector2(9, 4));
            foreach (var n in nearest)
            {
                Console.WriteLine(n);
            }

        }
        internal static void Test2()
        {
            // make random points
            // make a random query point
            // find the nearest point
            // compare to brute force
        }
    }
}
