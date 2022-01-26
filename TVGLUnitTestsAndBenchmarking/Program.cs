using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        const string inputFolder = @"C:\Users\matth\medemalabs.com\Executive Team - Documents\PartAnalyzer\OBJ Testing";
        //const string inputFolder = "TestFiles\\bad";
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)

        {
            //TestVoxelization();
            //TS_Testing_Functions.TestModify();
            //TVGL3Dto2DTests.TestSilhouette();
            // Polygon_Testing_Functions.TestSimplify();
            //TS_Testing_Functions.TestClassify();
            //TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate();

//#if PRESENT

            TVGL.Message.Verbosity = VerbosityLevels.Everything;
            // 1. bubble up from the bin directories to find the TestFiles directory
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(Path.Combine(dir.FullName, inputFolder)))
                dir = dir.Parent;
            dir = new DirectoryInfo(Path.Combine(dir.FullName, inputFolder));
            var dirName = dir.FullName;
            foreach (var fileName in dir.GetFiles("*"))
            {
                Debug.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                var stopwatch = new Stopwatch();
                stopwatch.Start();
                IO.Open(fileName.FullName, out TessellatedSolid[] solids);
                stopwatch.Stop();
                Console.WriteLine(stopwatch.Elapsed.ToString());

                Presenter.ShowAndHang(solids);
                //var css = CrossSectionSolid.CreateFromTessellatedSolid(ts, CartesianDirections.XPositive, 20);
                //Presenter.ShowAndHang(css);
                //IO.Save(css, "test.CSSolid");
            }
//#endif
        }

    }
}