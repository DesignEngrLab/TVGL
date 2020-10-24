using System;
using System.Collections.Generic;
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

            // 2. get the file path
            var fileName = dir.FullName + Path.DirectorySeparatorChar + "poly.json";
            TVGL.IOFunctions.IO.Open(fileName, out Polygon polygon);
            Presenter.ShowAndHang(polygon);
            var triangles = polygon.TriangulateToCoordinates();
            Presenter.ShowAndHang(triangles);

            //            brace.stl - holes showing up?
            // radiobox - missing holes - weird skip in outline
            // KnuckleTopOp flecks
            // mendel_extruder - one show up blank
            //var fileNames = dir.GetFiles("Obliq*").ToArray();
            var fileNames = dir.GetFiles("*").OrderBy(kjhgtfrden => r.NextDouble()).ToArray();
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                if (Path.GetExtension(filename) != ".stl") continue;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                //Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                if (name.Contains("yCastin")) continue;

                for (int j = 0; j < 9; j++)
                {
                    var direction = Vector3.UnitVector((CartesianDirections)(j % 3));
                    //var direction = new Vector3(r100, r100, r100);
                    Console.WriteLine(direction[0] + ", " + direction[1] + ", " + direction[2]);

                    solid.Vertices.GetLengthAndExtremeVertex(direction, out var btmVertex, out var topVertex);
                    var plane = new Plane(btmVertex.Coordinates.Lerp(topVertex.Coordinates, r.NextDouble()), direction);
                    var xsection = solid.GetCrossSection(plane);
                    var monoPolys = new List<Polygon>();
                    var error = false;
                    var totalArea = 0.0;
                    foreach (var monopoly in xsection.SelectMany(p => p.CreateXMonotonePolygons()))
                    {
                        monoPolys.Add(monopoly);
                        totalArea += monopoly.Area;
                        var extremeVerts = monopoly.Vertices.Where(v =>
                            v.GetMonotonicityChange() == MonotonicityChange.X ||
                            v.GetMonotonicityChange() == MonotonicityChange.Both).ToList();
                        if (extremeVerts.Count != 2 ||
                            !monopoly.MinX.IsPracticallySame(Math.Min(extremeVerts[0].X, extremeVerts[1].X)) ||
                            !monopoly.MaxX.IsPracticallySame(Math.Max(extremeVerts[0].X, extremeVerts[1].X)))
                            error = true;
                        else
                        {
                            //Console.WriteLine("testing triangulation.");
                            triangles = monopoly.TriangulateToCoordinates().ToList();
                            var triArea = triangles.Sum(tr => tr.Area());
                            if (!triArea.IsPracticallySame(monopoly.Area, monopoly.Area * Constants.BaseTolerance))
                            {
                                Console.WriteLine("Error triangulation.");
                                Presenter.ShowAndHang(triangles);
                            }
                        }
                    }

                    if (error || !totalArea.IsPracticallySame(xsection.Sum(x => x.Area), 1e-5))
                    {
                        Console.WriteLine("Error in x-monotone polygon.");
                        Presenter.ShowAndHang(xsection);
                        Presenter.ShowAndHang(monoPolys);
                    }
                }
            }

        }

        public static void TestTriangulate()
        {
            //var testcase = new Polygon(TestCases.EdgeCases["hand"].Item1[0]);
            var testcase = new Polygon(TestCases.MakeStarryCircularPolygon(13, 10, 7));
            //testcase.Transform(Matrix3x3.CreateRotation(Math.PI));
            Presenter.ShowAndHang(testcase);
            var triangles = testcase.TriangulateToCoordinates();
            Presenter.ShowAndHang(triangles);
        }

    }
}