using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TVGL;
using TVGL.TwoDimensional;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            TVGL3Dto2DTests.TestSilhouette();

#if PRESENT
            //var polys = TestCases.BenchKnown(16);
            var polys = TestCases.Ersatz["cutout"];
            var p1 = TestCases.C2Poly(polys.Item1);
            var p2 = TestCases.C2Poly(polys.Item2);
            Presenter.ShowAndHang(new[] { p1, p2 });
            Presenter.ShowAndHang(p1.Union(p2, TVGL.PolygonCollection.SeparateLoops));

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
