using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
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
            var d = new Dictionary<int, List<int>>();
            var a = new test(d);
            //TVGL3Dto2DTests.TestSilhouette();
            //TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

#if PRESENT
            var polys = TestCases.Ersatz["nestedSquares"];
            var p1 = TestCases.C2Poly(polys.Item1);
            var p2 = TestCases.C2Poly(polys.Item2);
            Presenter.ShowAndHang(new[] { p1, p2 });
            Presenter.ShowAndHang(p1.Union(p2));

#else
            //#endif
            var stats = new List<(string, int, long, long)>();

            foreach (var testCase in TestCases.GetAllTwoArgumentErsatzCases())
            {
                var polys = testCase.Value;
                PolygonBooleanTester.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2),
                    TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
            }
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

    class test
    {
        public test(Dictionary<int, IList<int>> boogers)
        {

        }
    }
}
