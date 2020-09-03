using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using TVGL.TwoDimensional;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            var stats = new List<(string, int, long, long)>();

            var pbt = new PolygonBooleanTester();
            var polys = TestCases.BenchKnown(1000, 1,0);
            pbt.SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2), TestCases.C2PLs(polys.Item1), TestCases.C2PLs(polys.Item2));
   


             //  pbt.FullComparison();

            //var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
            //PolygonOperationsTesting.DebugBoolean();
            //PolygonOperationsTesting.TestBooleanCompare();
            //PolygonOperationsTesting.TestRemoveSelfIntersect();
            //PolygonOperationsTesting.DebugEdgeCases();
            //PolygonOperationsTesting.DebugOctagons();
            //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
            //TVGL3Dto2DTests.TestSilhouette();
            //PolygonOperationsTesting.TestUnionSimple();
        }
    }
}
