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
    public class MinimumSphereTesting
    {
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        internal static void Test0()
        {
            var p1 = new Vector3(10, 10, 10);
            var p2 = new Vector3(20, 10, 10);
            var p3 = new Vector3(20, 20, 10);
            var p4 = new Vector3(10, 10, 20);
            var target = Sphere.CreateFrom4Points(p1, p2, p3, p4);
        }
        internal static void Test1(int dataSize, int numTests)
        {
            var p1 = new Vector3(100, -100, 100);
            var p2 = -p1;
            var target = Sphere.CreateFrom2Points(p1, p2);
            // so the answer should be a circle of radius 101 centered at origin
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");

                var points = Enumerable.Range(0, dataSize).Select(i => new Vector3(r100, r100, r100)).ToList();
                var index1 = r.Next(dataSize);
                points[index1] = p1;
                var index2 = index1;
                while (index1 == index2) // this is a silly but compact way to ensure that 
                    index2 = r.Next(dataSize); // index2 is different but random from index1
                points[index2] = p2;

                var sphere = TVGL.MinimumEnclosure.MinimumSphere(points);

                if (!sphere.Center.IsPracticallySame(target.Center)
                    || !sphere.Radius.IsPracticallySame(target.Radius))
                    throw new Exception("Old MinimumCircle failed");


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

                var circleOld = TVGL.MinimumEnclosure.MinimumCircle(points);
                if (!circleOld.Center.IsPracticallySame(target.Center)
                    || !circleOld.RadiusSquared.IsPracticallySame(target.RadiusSquared))
                    throw new Exception("Old MinimumCircle failed");

                //var circleBing = TVGL.MinimumEnclosure.MinimumCircleBing(points);
                //if (!circleBing.Center.IsPracticallySame(target.Center)
                //    || !circleBing.RadiusSquared.IsPracticallySame(target.RadiusSquared))
                //    throw new Exception("Bing MinimumCircle failed");

                //var circleMC = TVGL.MinimumEnclosure.MinimumCircleMC(points);
                //if (!circleMC.Center.IsPracticallySame(target.Center)
                //    || !circleMC.RadiusSquared.IsPracticallySame(target.RadiusSquared))
                //    throw new Exception("MC MinimumCircle failed");
            }
        }

        internal static void Test3(int dataSize, int numTests)
        {
            // so the answer should be a circle of radius 101 centered at origin
            for (var k = 0; k < numTests; k++)
            {
                var b1 = r100;
                var slope1 = Math.Tan(r.NextDouble() * Math.PI);
                var b2 = r100;
                var slope2 = Math.Tan(r.NextDouble() * Math.PI);
                Console.WriteLine($"Test {k}");
                var points = new Vector2[dataSize];
                for (int i = 0; i < dataSize; i += 2)
                {
                    var x = r100;
                    var y = slope1 * x + b1;
                    points[i] = new Vector2(x, y);
                    x = r100;
                    y = slope2 * x + b2;
                    points[i + 1] = new Vector2(x, y);
                }
                points = points.OrderBy(x => Guid.NewGuid()).ToArray();


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
                else Message.output("All three agree", 0);
                */
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

        /** in july 2023, I thought I had a cleaner version of the MinimumCircle algorithm, but it was actually slower
         * so I'm commenting it out for now. The code is translated for using MinimumSphere (although that is not yet
         * complete) and MinimumGaussSpherePlane, which was the motivation for the rewrite.
        [GlobalSetup]
        public void BenchmarkSetup()
        {
            int dataSize = 10000000;
            points = Enumerable.Range(0, dataSize).Select(i => new Vector2(r100, r100)).ToList();
        }

        [Benchmark]
        public Circle MinCircle_Old()
        {
            return TVGL.MinimumEnclosure.MinimumCircle(points);
        }

        [Benchmark]
        public Circle MinCircle_Bing()
        {
            return TVGL.MinimumEnclosure.MinimumCircleBing(points);
        }

        [Benchmark]
        public Circle MinCircle_NEWd()
        {
            return TVGL.MinimumEnclosure.MinimumCircleMC(points);
        }

        public IList<Vector2> points;
        ***/
    }
}
