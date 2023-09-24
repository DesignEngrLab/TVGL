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
        static double rPolar => Math.PI * r.NextDouble();
        static double rAzimuth => 2.0 * Math.PI * r.NextDouble() - Math.PI;

        internal static void Test0()
        {
            var p1 = new Vector3(0, 0, 0);
            var p2 = new Vector3(2, 2, 2);
            var p3 = new Vector3(0, 2, 0);
            var p4 = new Vector3(2, 2, 0);
            var target = Sphere.CreateFrom4Points(p1, p2, p3, p4);
        }

        internal static void Test1(int dataSize, int numTests)
        {
            // so the answer should be a circle of radius 101 centered at origin
            var numExtrema = 2;
            for (var k = 0; k < numTests; k++)
            {
                Console.WriteLine($"Test {k}");
                var center = new Vector3(r100, r100, r100);
                var radius = Math.Abs(r100);
                var target = new Sphere(center, radius, null);
                var indices = new List<int>();
                var extrema = new Vector3[numExtrema];
                var polarOffset = r.NextDouble() * Math.PI;
                var azimuthOffset = 2 * r.NextDouble() * Math.PI - Math.PI;
                for (int i = 0; i < numExtrema; i++)
                {
                    int index;
                    do
                    {
                        index = r.Next(dataSize);
                    } while (indices.Contains(index));
                    indices.Add(index);
                    if (i == 0)
                        extrema[i] = center + SphericalAnglePair.ConvertSphericalToCartesian(radius, polarOffset, azimuthOffset);
                    else if (i == 1)
                        extrema[i] = center + SphericalAnglePair.ConvertSphericalToCartesian(radius, polarOffset + Math.PI,
                            azimuthOffset);
                    else
                    {
                        var polarAngle = r.NextDouble() * Math.PI;
                        var azimuthAngle = 2 * r.NextDouble() * Math.PI - Math.PI;
                        extrema[i] = center + SphericalAnglePair.ConvertSphericalToCartesian(radius, polarAngle, azimuthAngle);
                    }
                }
                var points = new Vector3[dataSize];
                var m = 0;
                for (int i = 0; i < dataSize; i++)
                {
                    if (indices.Contains(i)) points[i] = extrema[m++];
                    else
                    {
                        var thisRadius = radius * r.NextDouble();
                        var polarAngle = r.NextDouble() * Math.PI;
                        var azimuthAngle = 2 * r.NextDouble() * Math.PI - Math.PI;
                        points[i] = center + SphericalAnglePair.ConvertSphericalToCartesian(thisRadius, polarAngle, azimuthAngle);
                    }
                }

                var sphere = TVGL.MinimumEnclosure.MinimumSphere(extrema);
                if (!sphere.Center.IsPracticallySame(target.Center, 1e-8)
                    || !sphere.Radius.IsPracticallySame(target.Radius, 1e-8))
                    // why the super loose tolerance here? Because the points are randomly generated about a center
                    // but this does not mean that the points are evenly distributed. So, the circle may be a little
                    // smaller
                    throw new Exception("Minimum Sphere failed");

                numExtrema++;
                if (numExtrema == 6) numExtrema = 2;
            }
        }
    }
}

