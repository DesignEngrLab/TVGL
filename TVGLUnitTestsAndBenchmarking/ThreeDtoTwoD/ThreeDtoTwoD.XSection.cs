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
            var fileNames = dir.GetFiles("errorP*").OrderByDescending(x => x.Length).ToArray();
            for (var i = 0; i < 20; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon poly);
                Presenter.ShowAndHang(poly);
                continue;
                TestTriangulate(poly);
                var solid = (TessellatedSolid)IO.Open(filename);
                //Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                var sw = new Stopwatch();
                for (int j = 1; j < 10; j++)
                {
                    //var direction = Vector3.UnitVector((CartesianDirections)(j % 3));
                    var direction = new Vector3(r100, r100, r100).Normalize();
                    //Console.WriteLine(direction[0] + ", " + direction[1] + ", " + direction[2]);

                    solid.Vertices.GetLengthAndExtremeVertex(direction, out var btmVertex, out var topVertex);
                    var plane = new Plane(btmVertex.Coordinates.Lerp(topVertex.Coordinates, r.NextDouble()), direction);
                    var xsection = solid.GetCrossSection(plane);
                    Vertex2D[] sortedverts1 = null;
                    Vertex2D[] sortedverts2 = null;
                    //Presenter.ShowAndHang(xsection);
                    //sw.Restart();
                    //for (int k = 0; k < 1000; k++)
                    //{
                    //    sortedverts1 = xsection.SortVerticesByXValue();
                    //}
                    //sw.Stop();
                    //var functionTime = sw.Elapsed;
                    sw.Restart();
                    for (int k = 0; k < 1000; k++)
                    {
                        sortedverts2 = xsection.SelectMany(p => p.Vertices).OrderBy(v => v.X).ToArray();
                    }
                    sw.Stop();
                    var linqTime = sw.Elapsed;
                    //Console.WriteLine(functionTime + "          " + linqTime);
                    //for (int m = 0; m < sortedverts1.Length; m++)
                    //{
                    //    if (sortedverts1[m] != sortedverts2[m])
                    //    {
                    //        Console.WriteLine("difference at " + m);
                    //        break;
                    //    }
                    //}
                }
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