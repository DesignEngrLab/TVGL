using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGLUnitTestsAndBenchmarking;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {

        static readonly Stopwatch stopwatch = new Stopwatch();
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;


        [STAThread]
        private static void Main(string[] args)
        {
            //PolygonOperationsTesting.DebugOctagons();
             //PolygonOperationsTesting.DebugEdgeCases();
            //PolygonOperationsTesting.DebugEdgeCases("nestedSquares");
            TVGL3Dto2DTests.TestSilhouette();
          //  PolygonOperationsTesting.TestEdgeCase1();
          //PolygonOperationsTesting.TestUnionSimple();
            Console.ReadKey();
        }
    }
}
