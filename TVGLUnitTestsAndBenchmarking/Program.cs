using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)

        {
            //TS_Testing_Functions.TestModify();
            //TVGL3Dto2DTests.TestSilhouette();
            TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

#if PRESENT

            // 1. bubble up from the bin directories to find the TestFiles directory
            var polysValue = TestCases.MakeBumpyRings(50, 28, 1.8);
            //var polysValue = TestCases.EdgeCases["tinyOffsetProb"];
            var polygon1 = new Polygon(polysValue.Item1);
            var polygon2 = new Polygon(polysValue.Item2);
            //Presenter.ShowAndHang(new[] { polygon1, polygon2 });
            var polygons = polygon1.Union(polygon2);
            //var polygons = new List<Polygon> { polygon1 };
            //var coords1 = TestCases.MakeStarryCircularPolygon(50, 28, 1.8).ToList();
            //var hole1 = TestCases.MakeStarryCircularPolygon(80, 14, 1.5).ToList();
            //hole1.Reverse();
            //var polygon1 = new Polygon(coords1);
            //polygon1 = polygon1.Intersect(new Polygon(hole1))[0];
            //Presenter.ShowAndHang(polygons);
            //var polygon1 = new Polygon(coords1, true);
            // Presenter.ShowAndHang(polygon1);
            //var polygons3 = polygon1.OffsetRound(88);
            var offsetBase = Math.Sqrt(polygons.LargestPolygon().Area);
            var factors = new[] { -.03, 0.03, -.1, 0.1, -.3, 0.3, -1, 1, -3, 3, -10 };
            foreach (var factor in factors)
            {
                var offset = factor * offsetBase;
                var polygons3 = polygons[0].OffsetRound(offset, 0.00254);
                Presenter.ShowAndHang(polygons3);
}

#else
            //#endif
            //PolygonBooleanTester.FullComparison();
            //var stats = new List<(string, int, long, long)>();

            //foreach (var testCase in TestCases.GetAllTwoArgumentEdgeCases())
            //{
            //    var polys = testCase.Value;
            //    PolygonBooleanTester.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2),
            //        TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
            //}
            PolygonBooleanTester.TestOffsetting();
#endif
            //var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
            //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
            //PolygonOperationsTesting.TestRemoveSelfIntersect();
            //PolygonOperationsTesting.DebugEdgeCases();
            //PolygonOperationsTesting.DebugOctagons();
            //PolygonOperationsTesting.TestUnionSimple();
        }
    }
}