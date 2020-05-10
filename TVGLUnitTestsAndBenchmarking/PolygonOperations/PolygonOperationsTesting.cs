using System;
using Xunit;
using TVGL.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using BenchmarkDotNet.Attributes;
using TVGL.TwoDimensional;
using System.Linq;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public class PolygonOperationsTesting
    {
        static Random r = new Random(1);
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        internal static IEnumerable<Vector2> MakeCircularPolygon(int numSides, double radius)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                yield return new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle));
            }
        }

        internal static IEnumerable<Vector2> MakeStarryCircularPolygon(int numSides, double radius, double delta)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                var thisRadius = radius + 2 * delta * r.NextDouble() - delta;
                yield return new Vector2(thisRadius * Math.Cos(angle), thisRadius * Math.Sin(angle));
            }
        }

        internal static IEnumerable<Vector2> MakeWavyCircularPolygon(int numSides, double radius, double delta, double frequency)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                yield return new Vector2(radius * Math.Cos(angle)+delta*Math.Cos(frequency*angle),
                    radius * Math.Sin(angle) + delta * Math.Sin(frequency * angle));
            }
        }

        internal static void TestSimplify()
        {
            IEnumerable<Vector2> polygon = MakeStarryCircularPolygon(150000, 30, 1);
            //Presenter.ShowAndHang(polygon);
            polygon = polygon.Simplify(20)[0];
            Presenter.ShowAndHang(polygon);
        }

        //[Benchmark(Description = "from Ienumerable")]
        //[Arguments(10, 4)]
        //[Arguments(20, 4)]
        //[Arguments(10, 4000)]
        [Arguments(20, 10000)]
        [Theory]
        [InlineData(10, 4)]
        [InlineData(20, 4)]
        [InlineData(10, 4000)]
        [InlineData(2, 10000)]
        public void Perimeter(double radius, int numSides)
        {
            var perimeter = 2 * radius * numSides * Math.Sin(Math.PI / numSides);

            var polygon = MakeCircularPolygon(numSides, radius);
            Assert.Equal(perimeter, polygon.Perimeter(), 10);
        }



        [Benchmark(Description = "from Ienumerable")]
        [Arguments(10, 4)]
        [Arguments(20, 4)]
        [Arguments(10, 4000)]
        [Arguments(20, 10000)]
        [Theory]
        [InlineData(10, 4)]
        [InlineData(20, 4)]
        [InlineData(10, 4000)]
        [InlineData(2, 10000)]
        public void Area(double radius, int numSides)
        {
            var area = 0.5 * radius * radius * numSides * Math.Sin(2 * Math.PI / numSides);

            var polygon = MakeCircularPolygon(numSides, radius);
            Assert.Equal(area, polygon.Area(), 10);
        }

        public static void TestBoundingRectangle()
        {
            //var points = new[] {
            //    new Vector2(2.26970768, 4.28080463),
            //    new Vector2(5.84034252, 0),
            //    new Vector2(12.22331619, 2.24806976),
            //    new Vector2(23.56225014, 21.88767815),
            //    new Vector2(19.9916172, 26.16848373),
            //    new Vector2(13.60864258, 23.92041588)
            //};
            //var points = new[] {
            //    new Vector2(6,1),
            //    new Vector2(5,2),
            //    new Vector2(-1,6),
            //    new Vector2(-2,5),
            //    new Vector2(-6,-1),
            //    new Vector2(-5,-2),
            //    new Vector2(1,-6),
            //    new Vector2(2,-5),
            //};
            var points = MakeWavyCircularPolygon(1000000, 10, 1, 4.65432);
            var br =points.BoundingRectangle();
            Presenter.ShowAndHang(new[] { points, br.CornerPoints });
        }

    }
}
