using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    internal static class Program
    {
        static string inputFolder = "TestFiles";
        //const string inputFolder = "TestFiles\\bad";
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)
        {
            //ProximityTests.TestClosestPointToLines();
            DirectoryInfo dir = BackOutToFolder();
             Polygon_Testing_Functions.TestSimplify(dir);
            //TestConicIntersection();
            TVGL.Message.Verbosity = VerbosityLevels.Everything;
            //Voxels.TestVoxelization(dir);
            //TS_Testing_Functions.TestModify();
            //TVGL3Dto2DTests.TestSilhouette();
            //TS_Testing_Functions.TestClassify();
            TVGL3Dto2DTests.TestXSectionAndMonotoneTriangulate(dir);
#if PRESENT
            foreach (var fileName in dir.GetFiles("*").Skip(1))
            {
                Debug.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid[] solids);
                solids[0].Faces[0].Color =Color.ColorDictionary[ColorFamily.Red]["Red"];
                Presenter.ShowAndHang(solids);
                var css = CrossSectionSolid.CreateFromTessellatedSolid(solids[0], CartesianDirections.XPositive, 20);
                Presenter.ShowAndHang(css);
                //IO.Save(css, "test.CSSolid");
            }
#endif
        }

        private static void TestConicIntersection()
        {
            var a = 1.3;
            var b = -3.0;
            var c = -4.0;
            var d = -10.0;
            var e = 16.0;
            var f = 1.0;
            var conicH = new GeneralConicSection(a/f,b/f,c/f,d/f,e/f,false);
            a = 1;
            b = -3.4;
            c = -4.2;
            d = -4.1;
            e = 8.2;
            f = 1;
            var conicJ = new GeneralConicSection(a/f,b/f,c/f,d/f,e/f,false);
            foreach (var p in GeneralConicSection.IntersectingConics(conicH,conicJ))
            {
                Console.WriteLine(p);
            }
        }

        private static DirectoryInfo BackOutToFolder()
        {
            // 1. bubble up from the bin directories to find the TestFiles directory
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(Path.Combine(dir.FullName, inputFolder)))
                dir = dir.Parent;
            dir = new DirectoryInfo(Path.Combine(dir.FullName, inputFolder));
            var dirName = dir.FullName;
            return dir;
        }
    }
}