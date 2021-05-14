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

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class TVGL3Dto2DTests
    {

        //[Fact]
        public static void TestXSectionAndMonotoneTriangulate()
        {
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(dir.FullName + Path.DirectorySeparatorChar + "TestFiles"))
                dir = dir.Parent;
            dir = new DirectoryInfo(dir.FullName + Path.DirectorySeparatorChar + "TestFiles");

            //var fileName = dir.FullName + Path.DirectorySeparatorChar + "test.json";
            var fileNames = dir.GetFiles("*.json").OrderByDescending(x => x.Length).ToArray();
            for (var i = 0; i < 20; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon polygon);
                Presenter.ShowAndHang(polygon);
                polygon = polygon.SimplifyMinLengthToNewPolygon(0.01);
                //polygon = polygon.RemoveSelfIntersections(ResultType.OnlyKeepPositive).LargestPolygon();
                TestTriangulate(polygon);
                Presenter.ShowAndHang(polygon);
            }
            Console.ReadKey();
        }

        public static void TestTriangulate(Polygon testcase)
        {
            if (testcase == null)
                testcase = new Polygon(TestCases.MakeStarryCircularPolygon(13, 10, 7));
            Presenter.ShowAndHang(testcase);
            var triangles = testcase.TriangulateToCoordinates();
            Presenter.ShowAndHang(triangles);
        }

    }
}