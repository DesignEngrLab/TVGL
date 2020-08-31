using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            var pbt = new PolygonBooleanTester();
            pbt.FullComparison();
   
            var summary = BenchmarkRunner.Run(typeof(PolygonBooleanTester).Assembly);
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
