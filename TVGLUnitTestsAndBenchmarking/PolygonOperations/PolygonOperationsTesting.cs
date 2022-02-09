using System;
#if !PRESENT
using Xunit;
using BenchmarkDotNet.Attributes;
#endif
using TVGL.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using TVGL.TwoDimensional;
using System.Linq;
using TVGL;
using TVGL.Voxelization;
using System.Runtime.CompilerServices;
using System.Windows;
using TVGLPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    public class PolygonOperationsTesting
    {
        static Random r = new Random(0);
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        internal static void DebugEdgeCases()
        {
            foreach (var name in TestCases.EdgeCases.Keys)
                DebugEdgeCases(name);
        }
        internal static void DebugEdgeCases(string name)
        {
            var coords1 = TestCases.EdgeCases[name].Item1;
            var polygon1 = new Polygon(coords1[0]);
            for (int i = 1; i < coords1.Length; i++)
                polygon1.AddInnerPolygon(new Polygon(coords1[i]));
            var coords2 = TestCases.EdgeCases[name].Item2;
            var polygon2 = new Polygon(coords2[0]);
            for (int i = 1; i < coords2.Length; i++)
                polygon2.AddInnerPolygon(new Polygon(coords2[i]));

            // polygon1.RemoveSelfIntersections();
            //Presenter.ShowAndHang(polygon1);

            DebugBooleanOperations(polygon1, polygon2);

            //Console.ReadKey();
        }

        private static void DebugBooleanOperations(IEnumerable<Vector2> coordinates1, IEnumerable<Vector2> coordinates2)
        {
            DebugBooleanOperations(new Polygon(coordinates1), new Polygon(coordinates2));
        }

        private static void DebugBooleanOperations(Polygon polygon1, Polygon polygon2)
        {
            Presenter.ShowAndHang(new[] { polygon1, polygon2 });

            var a = polygon1.GetPolygonInteraction(polygon2);
            List<Polygon> polygon3;

            polygon3 = polygon1.Union(polygon2, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.Intersect(polygon2, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.Subtract(polygon2, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon2.Subtract(polygon1, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.ExclusiveOr(polygon2, a);
            Presenter.ShowAndHang(polygon3);

        }
        internal static void DebugBoolean()
        {
            Vector2[][] coords1, coords2;
            (coords1, coords2) = TestCases.MakeBumpyRings(12, 25, 0);
            Presenter.ShowAndHang(coords1, coords2);

            var result = TestCases.C2Poly(coords1).Intersect(TestCases.C2Poly(coords2));

            Presenter.ShowAndHang(result);
        }

        internal static void TestRemoveSelfIntersect()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine(i);
                //var coords = MakeWavyCircularPolygon(rand(500), rand(30), rand(300), rand(15.0)).ToList();
                var coords = TestCases.MakeRandomComplexPolygon(100, 30).ToList();
                Presenter.ShowAndHang(coords);
                var polygon = new Polygon(coords);
                var polygons = polygon.RemoveSelfIntersections(ResultType.BothPermitted);
                Presenter.ShowAndHang(polygons);
                //Presenter.ShowAndHang(polygons.Path);
                //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
            }
        }



        //[Benchmark(Description = "from Ienumerable")]
        //[Arguments(10, 4)]
        //[Arguments(20, 4)]
        //[Arguments(10, 4000)]
        //[Arguments(20, 10000)]
        //[Theory]
        //[InlineData(10, 4)]
        //[InlineData(20, 4)]
        //[InlineData(10, 4000)]
        //[InlineData(2, 10000)]
        public void Perimeter(double radius, int numSides)
        {
            var perimeter = 2 * radius * numSides * Math.Sin(Math.PI / numSides);

            var polygon = TestCases.MakeCircularPolygon(numSides, radius);
#if !PRESENT
            Assert.Equal(perimeter, polygon.Perimeter(), 10);
#endif
        }



        //[Benchmark(Description = "from Ienumerable")]
        //[Arguments(10, 4)]
        //[Arguments(20, 4)]
        //[Arguments(10, 4000)]
        //[Arguments(20, 10000)]
        //[Theory]
        //[InlineData(10, 4)]
        //[InlineData(20, 4)]
        //[InlineData(10, 4000)]
        //[InlineData(2, 10000)]
        public void Area(double radius, int numSides)
        {
            var area = 0.5 * radius * radius * numSides * Math.Sin(2 * Math.PI / numSides);

            var polygon = TestCases.MakeCircularPolygon(numSides, radius);
#if !PRESENT
            Assert.Equal(area, polygon.Area(), 10);
#endif
        }

        public static void TestBoundingRectangle()
        {
            var polygon = TestCases.EdgeCases["boundingRectTest3"].Item1;
            //var polygon = TestCases.EdgeCases["boundingRectTest3"].Item1;
            //var polygon = TestCases.EdgeCases["boundingRectTest3"].Item1;
            var br = polygon[0].BoundingRectangle();
            Presenter.ShowAndHang(new[] { polygon[0], br.CornerPoints() });
        }
        public static void TestSlice2D()
        {
            //var polygon = PolygonTestCases.edgeCaseDictionary["sliceSimpleCase"].Item1;
            var polygon = TestCases.EdgeCases["sliceWithHole"].Item1;
            Presenter.ShowAndHang(polygon);
            polygon.SliceAtLine(new Vector2(1, -0.1).Normalize(), 0, out var negPolys, out var posPolys, -0.3, -0.3);

            Presenter.ShowAndHang(negPolys);
            Presenter.ShowAndHang(posPolys);
        }

    }
}

