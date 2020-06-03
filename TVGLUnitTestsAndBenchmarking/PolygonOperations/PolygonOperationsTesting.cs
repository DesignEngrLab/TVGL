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
        static Random r = new Random(2);
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

        internal static IEnumerable<Vector2> MakeRandomComplexPolygon(int numSides, double boxRadius)
        {
            for (int i = 0; i < numSides; i++)
            {
                yield return new Vector2(2 * boxRadius * r.NextDouble() - boxRadius, 2 * boxRadius * r.NextDouble() - boxRadius);
            }
        }

        internal static IEnumerable<Vector2> MakeWavyCircularPolygon(int numSides, double radius, double delta, double frequency)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                yield return new Vector2(radius * Math.Cos(angle) + delta * Math.Cos(frequency * angle),
                    radius * Math.Sin(angle) + delta * Math.Sin(frequency * angle));
            }
        }

        internal static void TestRemoveSelfIntersect()
        {
            for (int i = 6; i < 200; i++)
            {
                r = new Random(i);
                Console.WriteLine(i);
                var coords = MakeRandomComplexPolygon(120, 30).ToList();
                Presenter.ShowAndHang(coords);
                var polygon = new Polygon(coords, true);
                var polygons = polygon.RemoveSelfIntersections();
                Presenter.ShowAndHang(polygons.Path);
                //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
            }
        }
        internal static void TestUnionSimple()
        {
            //for (int i = 6; i < 200; i++)
            //{
            //    r = new Random(i);
            //    Console.WriteLine(i);
            var coords1 = MakeStarryCircularPolygon(50, 30, 15).ToList();
            var coords2 = MakeWavyCircularPolygon(60, 25, 3, 8).Select(p => p + new Vector2(15, 10)).ToList();

            Presenter.ShowAndHang(new[] { coords1, coords2 });
            var polygon1 = new Polygon(coords1, true);
            var polygon2 = new Polygon(coords2, false);
            polygon1.GetPolygonRelationshipAndIntersections(polygon2, out var intersections);
            var polygon3 = polygon1.Union(polygon2, intersections);
            Presenter.ShowAndHang(new[] { coords1, coords2, polygon3[0].Path });
            var polygon4 = polygon1.Intersect(polygon2, intersections);
            Presenter.ShowAndHang(new[] { coords1, coords2, polygon4[0].Path });
            var polygon5 = polygon1.ExclusiveOr(polygon2, intersections);
            Presenter.ShowAndHang(polygon5.Select(p => p.Path));
            polygon5 = polygon1.Subtract(polygon2, intersections);
            Presenter.ShowAndHang(polygon5.Select(p => p.Path));
            polygon5 = polygon2.Subtract(polygon1, intersections);
            Presenter.ShowAndHang(polygon5.Select(p => p.Path));
            //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
            //}
        }
        [Params(10, 100, 1000, 10000)]
        public int N;

        private List<Vector2> coords1;
        private List<Vector2> coords2;
        private Polygon polygon1;
        private Polygon polygon2;

        [GlobalSetup]
        public void Setup()
        {
            coords1 = MakeStarryCircularPolygon(N, 30, 15).ToList();
            coords2 = MakeWavyCircularPolygon(60, 25, 3, 8).Select(p => p + new Vector2(15, 10)).ToList();
            polygon1 = new Polygon(coords1, true);
            polygon2 = new Polygon(coords2, true);
        }


        [Benchmark(Description = "my functions")]
        public void BenchmarkMyBooleanSimple()
        {
            var polygon3 = polygon1.Union(polygon2);
            polygon3 = polygon1.Intersect(polygon2);
            polygon3 = polygon1.ExclusiveOr(polygon2);
            polygon3 = polygon1.Subtract(polygon2);
        }


        [Benchmark(Description = "clipper")]
        public void BenchmarkClipperSimple()
        {
            var coords3 =PolygonOperations.Union(coords1,coords2);
            coords3 = PolygonOperations.Intersection(coords1, coords2);
            coords3 = coords1.Difference(coords2);
            coords3 = coords1.Xor(coords2);
        }

        internal static void TestSimplify()
        {
            IEnumerable<Vector2> polygon = MakeStarryCircularPolygon(150000, 30, 1);
            //Presenter.ShowAndHang(polygon);
            polygon = polygon.Simplify(20)[0];
            Presenter.ShowAndHang(polygon);
        }

        /*
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
        */
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
            var points = new[]{new Vector2(-101.5999985, -34.5535698), new
            TVGL.Numerics.Vector2(-88.9000015, -101.5999985), new            TVGL.Numerics.Vector2(-38.1000023, -158.5185089), new
            TVGL.Numerics.Vector2(38.1000023, -158.5185089), new            TVGL.Numerics.Vector2(88.9000015, -101.5999985), new
            TVGL.Numerics.Vector2(101.5999985, -34.5535698), new            TVGL.Numerics.Vector2(101.5999985, 34.6359444), new
            TVGL.Numerics.Vector2(88.9000015, 203.1999969), new            TVGL.Numerics.Vector2(-88.9000015, 203.1999969), new
            TVGL.Numerics.Vector2(-101.5999985, 34.6359444) };


            // var points = MakeWavyCircularPolygon(10000, 10, 1, 4.65432);
            var br = points.BoundingRectangle();
            Presenter.ShowAndHang(new[] { points, br.CornerPoints });
        }
        public static void TestSlice2D()
        {
            //var points = new[] {
            //    new Vector2(2.26970768, 4.28080463),
            //    new Vector2(5.84034252, 0),
            //    new Vector2(12.22331619, 2.24806976),
            //    new Vector2(23.56225014, 21.88767815),
            //    new Vector2(19.9916172, 26.16848373),
            //    new Vector2(13.60864258, 23.92041588)
            //        };
            var poly1 = new[] {
                new Vector2(6,1),
                new Vector2(5,2),
                new Vector2(-1,6),
                new Vector2(-2,5),
                new Vector2(-6,-1),
                new Vector2(-5,-2),
                new Vector2(1,-6),
                new Vector2(2,-5),
            };
            var hole1 = new[] {
                new Vector2(1,1),
                new Vector2(1,-1),
                new Vector2(-1,1),
                new Vector2(-2,2),
            };
            var polyTree = new[] { poly1, hole1 };
            Presenter.ShowAndHang(polyTree);
            polyTree.SliceAtLine(new Vector2(1, -0.1).Normalize(), 0, out var negPolys, out var posPolys, -0.3, -0.3);

            Presenter.ShowAndHang(new[] { negPolys, posPolys });
        }

    }
}
