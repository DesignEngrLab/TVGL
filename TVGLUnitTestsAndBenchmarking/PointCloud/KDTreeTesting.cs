using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.PointCloud;
using Xunit;

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
            var tree = KDTree.Create(points);
            var nearest = tree.FindNearest(new Vector2(9, 4));
            foreach (var n in nearest)
            {
                Console.WriteLine(n);
            }

        }
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;
        internal static void Test2()
        {
            var dataSize = 1000000;
            var numTests = 10;
            var numNearest = 1000;
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");
                var points = new List<Vector3>();
                for (int i = 0; i < dataSize; i++)
                    points.Add(new Vector3(r100, r100, r100));
                var tree = KDTree.Create(points);
                var testPoint = new Vector3(r100, r100, r100);
                var nearest = tree.FindNearest(testPoint, numNearest);
                List<Vector3> nearest2 = FindNearestBruteForce(testPoint, points, numNearest);
                var j = 0;
                foreach (var np in nearest)
                {
                    if (np != nearest2[j++])
                        throw new Exception("KDTree failed");
                    Console.Write('.');
                }
                // make random points
                // make a random query point
                // find the nearest point
                // compare to brute force
            }
        }

        internal static void Test3()
        {
            var dataSize = 100000;
            var numTests = 20;
            var numNearest = 100000;
            var radius = 33.3;
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");
                var points = new List<Vector3>();
                for (int i = 0; i < dataSize; i++)
                    points.Add(new Vector3(r100, r100, r100));
                var tree = new KDTree<Vector3, string>(3, points, points.Select(p => p.ToString()).ToList());
                var testPoint = new Vector3(r100, r100, r100);
                var nearest = tree.FindNearest(testPoint, radius).ToList();
                List<Vector3> nearest2 = FindNearestBruteForceWithRadius(testPoint, points, radius, numNearest);
                if (nearest.Count != nearest2.Count) throw new Exception("KDTree failed");
                var j = 0;
                foreach (var np in nearest)
                {
                    if (np.Item1 != nearest2[j++])
                        throw new Exception("KDTree failed");
                    Console.Write('.');
                }
            }
        }

        private static List<Vector3> FindNearestBruteForceWithRadius(Vector3 testPoint, List<Vector3> points, double radius, int numNearest)
        {
            if (numNearest <= 0) numNearest = points.Count;
            var radiusSquared = radius * radius;
            var minDistance = double.PositiveInfinity;
            var nearest = new SortedList<double, Vector3>();
            foreach (var p in points)
            {
                var d = (p - testPoint).LengthSquared();
                if (d > radiusSquared) continue;
                if (nearest.Count < numNearest)
                {
                    nearest.Add(d, p);
                    minDistance = nearest.Last().Key;
                }
                else if (d < minDistance)
                {
                    nearest.RemoveAt(numNearest - 1);
                    nearest.Add(d, p);
                    minDistance = nearest.Keys[numNearest - 1];
                }
            }
            return nearest.Values.ToList();
        }

        private static List<Vector3> FindNearestBruteForce(Vector3 testPoint, List<Vector3> points, int numNearest)
        {
            var minDistance = double.PositiveInfinity;
            var nearest = new SortedList<double, Vector3>();
            foreach (var p in points)
            {
                var d = (p - testPoint).LengthSquared();
                if (nearest.Count < numNearest)
                {
                    nearest.Add(d, p);
                    minDistance = nearest.Last().Key;
                }
                else if (d < minDistance)
                {
                    nearest.RemoveAt(numNearest - 1);
                    nearest.Add(d, p);
                    minDistance = nearest.Keys[numNearest - 1];
                }
            }
            return nearest.Values.ToList();
        }
    }
}
