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
            //TS_Testing_Functions.TestModify();
            //TVGL3Dto2DTests.TestSilhouette();
            //TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

#if PRESENT


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