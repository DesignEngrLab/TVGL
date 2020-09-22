using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            //TVGL3Dto2DTests.TestSilhouette();
             TVGL3Dto2DTests.TestTriangulate();

#if PRESENT
            //var polys = TestCases.BenchKnown(16);
            var polys = TestCases.Ersatz["pinch"];
            var p1 = TestCases.C2Poly(polys.Item1);
            var p2 = TestCases.C2Poly(polys.Item2);
            Presenter.ShowAndHang(new[] { p1, p2 });
            Presenter.ShowAndHang(p1.Union(p2));

#else
            var stats = new List<(string, int, long, long)>();
            var pbt = new PolygonBooleanTester();
            var polys = TestCases.BenchKnown(10);
            pbt.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2), TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
            pbt.FullComparison();
#endif

            //var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
            //PolygonOperationsTesting.DebugBoolean();
            //PolygonOperationsTesting.TestBooleanCompare();
            //PolygonOperationsTesting.TestRemoveSelfIntersect();
            //PolygonOperationsTesting.DebugEdgeCases();
            //PolygonOperationsTesting.DebugOctagons();
            //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
            //PolygonOperationsTesting.TestUnionSimple();
        }
    }
}
