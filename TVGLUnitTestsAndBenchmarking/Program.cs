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

        [STAThread]
        private static void Main(string[] args)
        {

            //var summary = BenchmarkRunner.Run(typeof(PolygonOperationsTesting).Assembly);

            //PolygonOperationsTesting.TestOffsetting();
            //PolygonOperationsTesting.TestBooleanCompare();
            PolygonOperationsTesting.TestUnionSimple();
            PolygonOperationsTesting.TestEdgeCase2();
            PolygonOperationsTesting.TestRemoveSelfIntersect();
            //Voxels.InitialTest();
            //return;
            //var tVGL3Dto2DTests = new TVGL3Dto2DTests();
            //tVGL3Dto2DTests.BoxSilhouette();

            //PolygonOperationsTesting.TestSlice2D();
            //PolygonOperationsTesting.TestBoundingRectangle();
            //PolygonOperationsTesting.TestSimplify();


        }

        //BenchmarkRunner.Run<PolygonOperationsTesting>();
        //    var po = new PolygonOperationsTesting();
        //po.Perimeter(4, 10);
        //Console.WriteLine(summary);
    }

}
