using BenchmarkDotNet.Attributes;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.PointCloud;

namespace TVGLUnitTestsAndBenchmarking
{
    public class MinimumCircleTesting
    {
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        internal static void Test1(int dataSize, int numTests)
        {
            var p1 = new Vector2(100, 100);
            var p2 = -p1;
            var target = Circle.CreateFrom2Points(p1, p2);
            // so the answer should be a circle of radius 101 centered at origin
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");

                var points = Enumerable.Range(0, dataSize).Select(i => new Vector2(r100, r100)).ToList();
                var index1 = r.Next(dataSize);
                points[index1] = p1;
                var index2 = index1;
                while (index1 == index2) // this is a silly but compact way to ensure that 
                    index2 = r.Next(dataSize); // index2 is different but random from index1
                points[index2] = p2;

                var circle = TVGL.MinimumEnclosure.MinimumCircle(points);
                if (!circle.Center.IsPracticallySame(target.Center)
                    || !circle.RadiusSquared.IsPracticallySame(target.RadiusSquared))
                    throw new Exception("MinimumCircle failed");


            }
        }

        internal static void Test2(int dataSize, int numTests)
        {
            var p1 = new Vector2(-100, -100);
            var p2 = new Vector2(99, 101);
            var p3 = new Vector2(101, 99);
            Circle.CreateFrom3Points(p1, p2, p3, out var target);
            // so the answer should be a circle of radius 101 centered at origin
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");

                var points = Enumerable.Range(0, dataSize).Select(i => new Vector2(r100, r100)).ToList();
                var index1 = r.Next(dataSize);
                points[index1] = p1;
                var index2 = index1;
                while (index1 == index2) // this is a silly but compact way to ensure that 
                    index2 = r.Next(dataSize); // index2 is different but random from index1
                points[index2] = p2;
                var index3 = index1;
                while (index1 == index3 || index2 == index3)
                    index3 = r.Next(dataSize);
                points[index3] = p3;

                var circle = TVGL.MinimumEnclosure.MinimumCircle(points);
                if (!circle.Center.IsPracticallySame(target.Center)
                    || !circle.RadiusSquared.IsPracticallySame(target.RadiusSquared))
                    throw new Exception("MinimumCircle failed");
            }
        }

        internal static void Test3(int dataSize, int numTests)
        {
            // so the answer should be a circle of radius 101 centered at origin
            var numPoints = 2;
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");
                var center = new Vector2(r100, r100);
                var radius = Math.Abs(r100);
                var target = new Circle(center, radius * radius);
                var indices = new List<int>();

                for (int i = 0; i < numPoints; i++)
                {
                    int index;
                    do
                    {
                        index = r.Next(dataSize);
                    } while (indices.Contains(index));
                    indices.Add(index);
                }
                var points = new Vector2[dataSize];
                for (int i = 0; i < dataSize; i++)
                {
                    var thisRadius = indices.Contains(i) ? radius : radius * r.NextDouble();
                    var angle = r.NextDouble() * 2 * Math.PI;
                    points[i] = center + new Vector2(thisRadius * Math.Cos(angle), thisRadius * Math.Sin(angle));
                }

                var circle = TVGL.MinimumEnclosure.MinimumCircle(points);
                if (!circle.Center.IsPracticallySame(target.Center, 0.2)
                    || !circle.Radius.IsPracticallySame(target.Radius, 0.2))
                    // why the super loose tolerance here? Because the points are randomly generated about a center
                    // but this does not mean that the points are evenly distributed. So, the circle may be a little
                    // smaller
                    throw new Exception("MinimumCircle failed");

                numPoints++;
                if (numPoints == 5) numPoints = 2;
            }
        }



        internal static void Test4(IEnumerable<List<Polygon>> polygonalLayers)
        {
            foreach (var polygonSet in polygonalLayers)
            {
                var polygon = polygonSet.LargestPolygon();
                var points = polygon.Path;


                var circleOld = TVGL.MinimumEnclosure.MinimumCircle(points);
                /*
                var circleBing = TVGL.MinimumEnclosure.MinimumCircleBing(points);
                var circleMC = TVGL.MinimumEnclosure.MinimumCircleMC(points);

                var oldBingEqual = circleOld.Center.IsPracticallySame(circleBing.Center, 1e-10)
                    && circleOld.RadiusSquared.IsPracticallySame(circleBing.RadiusSquared, 1e-10);
                var oldMCEqual = circleOld.Center.IsPracticallySame(circleMC.Center, 1e-10)
                    && circleOld.RadiusSquared.IsPracticallySame(circleMC.RadiusSquared, 1e-10);
                var bingMCEqual = circleBing.Center.IsPracticallySame(circleMC.Center, 1e-10)
                    && circleBing.RadiusSquared.IsPracticallySame(circleMC.RadiusSquared, 1e-10);

                if (!oldBingEqual && !bingMCEqual && !oldMCEqual)
                    throw new Exception("all three min circle algorithms disagree!");

                else if (!oldBingEqual && !bingMCEqual)
                    Message.output("Old and MC agree but Bing is different", 0);
                else if (!oldBingEqual && !oldMCEqual)
                    Message.output("MC and Bing agree but old is different", 0);
                else if (!bingMCEqual && !oldMCEqual)
                    Message.output("Old and Bing agree but MC is different", 0);
                else Message.output("All three agree", 1);
            */
            }
        }
    }
}

