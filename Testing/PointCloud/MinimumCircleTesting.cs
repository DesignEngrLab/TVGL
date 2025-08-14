using BenchmarkDotNet.Attributes;
using SharpDX.Direct2D1.Effects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public class MinimumCircleTesting
    {
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        internal static void Test1(int dataSize, int numTests)
        {
            // so the answer should be a circle of radius 101 centered at origin
            var numExtrema = 2;
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");
                var center = new Vector2(r100, r100);
                var radius = Math.Abs(r100);
                var target = new Circle(center, radius * radius);
                var indices = new List<int>();
                var extrema = new Vector2[numExtrema];
                var offset = r.NextDouble() * 2 * Math.PI / numExtrema;
                var angleBetweenExtrema = 2 * Math.PI / numExtrema;
                for (int i = 0; i < numExtrema; i++)
                {
                    int index;
                    do
                    {
                        index = r.Next(dataSize);
                    } while (indices.Contains(index));
                    indices.Add(index);
                    var angle = offset + i * angleBetweenExtrema;
                    extrema[i] = center + new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle));
                }
                var points = new Vector2[dataSize];
                var m = 0;
                for (int i = 0; i < dataSize; i++)
                {
                    if (indices.Contains(i)) points[i] = extrema[m++];
                    else
                    {
                        var thisRadius = radius * r.NextDouble();
                        var angle = r.NextDouble() * 2 * Math.PI;
                        points[i] = center + new Vector2(thisRadius * Math.Cos(angle), thisRadius * Math.Sin(angle));
                    }
                }

                var circle = TVGL.MinimumEnclosure.MinimumCircle(points);
                if (!circle.Center.IsPracticallySame(target.Center)
                    || !circle.Radius.IsPracticallySame(target.Radius))
                    throw new Exception("MinimumCircle failed");

                numExtrema++;
                if (numExtrema == 5) numExtrema = 2;
            }
        }

        internal static void Test2()
        {
            var path = @"../../../PointCloud/points0.csv";
            var points = new List<Vector2>();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var values = line.Split(',');
                points.Add(new Vector2(double.Parse(values[0]), double.Parse(values[1])));
            }
            var circle = TVGL.MinimumEnclosure.MinimumCircle(points);
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
                    Global.Logger.LogInformation("Old and MC agree but Bing is different", 0);
                else if (!oldBingEqual && !oldMCEqual)
                    Global.Logger.LogInformation("MC and Bing agree but old is different", 0);
                else if (!bingMCEqual && !oldMCEqual)
                    Global.Logger.LogInformation("Old and Bing agree but MC is different", 0);
                else Global.Logger.LogInformation("All three agree", 1);
            */
            }
        }
    }
}

