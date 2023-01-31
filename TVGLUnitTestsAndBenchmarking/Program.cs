using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Diagnostics.Tracing.Parsers.FrameworkEventSource;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGLUnitTestsAndBenchmarking.Misc_Tests;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class Program
    {
       public static string inputFolder = "TestFiles";
        //const string inputFolder = "TestFiles\\bad";
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)
        {
            var A = new Matrix3x3(1, -11, 111, -2, 222, 22, -333, 33, 3);
            A.Eigen(out var eValues, out var eVectors);

            //ZbufferTesting.Test();
            //return;
            var sphere1 = new Sphere(new Vector3(2, 3, 4), 10, true);
            var sphere2 = new Sphere(new Vector3(8, 7, 6), 10, true);
            var implicitSolid = new ImplicitSolid(sphere1, sphere2, BooleanOperationType.SubtractAB);
            var cyl1 = new Cylinder(Vector3.UnitX, new Vector3(5, 5, 5), new Circle(Vector2.Zero, 16), -18, 18);
            var capsule = new Capsule(Vector3.Zero, 8, new Vector3(10, 14, 18), 2, true);

            implicitSolid.AddNewTopOfTree(capsule, BooleanOperationType.Union);
            implicitSolid.AddNewTopOfTree(BooleanOperationType.SubtractAB, cyl1);
            implicitSolid.Bounds = new[] { new Vector3(-20, -20, -20), new Vector3(20, 20, 20) };
            var tessellatedSolid = implicitSolid.ConvertToTessellatedSolid(0.25);
            Presenter.ShowAndHang(new[] { tessellatedSolid });

            return;

            var plane1 = new Plane(17.0, Vector3.UnitZ);
            var matrix = Matrix4x4.CreateRotationY(Math.PI / 2);
            matrix *= Matrix4x4.CreateTranslation(0, 4, 5);
            plane1.Transform(matrix);
            //ProximityTests.TestClosestPointToLines();
            DirectoryInfo dir = Program.    BackoutToFolder();
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
                solids[0].Faces[0].Color = Color.ColorDictionary[ColorFamily.Red]["Red"];
                Presenter.ShowAndHang(solids);
                var css = CrossSectionSolid.CreateFromTessellatedSolid(solids[0], CartesianDirections.XPositive, 20);
                Presenter.ShowAndHang(css);
                //IO.Save(css, "test.CSSolid");
            }
#endif
        }


        public static DirectoryInfo BackoutToFolder(string folderName = "")
        {
            var dir = new DirectoryInfo(".");
            while (!Directory.Exists(Path.Combine(dir.FullName, folderName)))
            {
                if (dir == null) throw new FileNotFoundException("Folder not found", folderName);
                dir = dir.Parent;
            }
            return new DirectoryInfo(Path.Combine(dir.FullName, folderName));
        }

        public static void DebugOffsetCases(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("offsetFail*.json").Skip(0).ToList();
            //var offset = -0.2;
            while (fileNames.Any())
            {
                var polygons = new List<Polygon>();
                var filename = fileNames[0].Name;
                //var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = string.Join('.', nameSegments.Take(2).ToArray());
                var offset = double.Parse(nameSegments[^4] + "." + nameSegments[^3]);
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                if (polygons.All(p => p == null)) continue;
                Debug.WriteLine("Attempting: " + filename);
                Presenter.ShowAndHang(polygons);
                var result = polygons.OffsetRound(offset, 0.02); //, polygonSimplify: PolygonSimplify.DoNotSimplify);
                Presenter.ShowAndHang(result);
            }
        }

        public static void DebugIntersectCases(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("intersect*.json").ToList();
            while (fileNames.Any())
            {
                var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = string.Join('.', nameSegments.Take(nameSegments.Length - 2).ToArray());

                var polygonsA = new List<Polygon>();
                var polygonsB = new List<Polygon>();
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    if (item.Name.Contains("B"))
                        polygonsB.Add(p);
                    else polygonsA.Add(p);
                }
                Debug.WriteLine("Attempting: " + filename);
                Presenter.ShowAndHang(polygonsA);
                Presenter.ShowAndHang(polygonsB);
                Presenter.ShowAndHang(new[] { polygonsA, polygonsB }.SelectMany(p => p));
                var result = polygonsA.IntersectPolygons(polygonsB);
                Presenter.ShowAndHang(result);
            }
        }
        public static void DebugUnionCases(DirectoryInfo dir)
        {
            var polygonsA = new List<Polygon>();
            var polygonsB = new List<Polygon>();

            foreach (var item in dir.GetFiles("union*.json"))
            {
                IO.Open(item.FullName, out Polygon p);
                if (item.Name.Contains("B", StringComparison.InvariantCulture))
                    polygonsB.Add(p);
                else polygonsA.Add(p);
            }

            Presenter.ShowAndHang(polygonsA);
            Presenter.ShowAndHang(polygonsB);
            Presenter.ShowAndHang(new[] { polygonsA, polygonsB }.SelectMany(p => p));
            var result = polygonsA.UnionPolygons(polygonsB);
            Presenter.ShowAndHang(result);
        }
        public static void JustShowMeThePolygons(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("endles*.json").ToList();
            var silhouetteBeforeFace = new List<Polygon>();
            foreach (var fileName in fileNames.Take(1))
            {
                //Debug.WriteLine("Attempting: " + fileName);
                IO.Open(fileName.FullName, out Polygon p);
                silhouetteBeforeFace.Add(p);
            }
            Presenter.ShowAndHang(silhouetteBeforeFace);

            var poly1 = silhouetteBeforeFace.OffsetMiter(15.557500000000001, tolerance: 0.08);
            var showe = new List<Polygon>();
            showe.AddRange(silhouetteBeforeFace);
            showe.AddRange(poly1);
            Presenter.ShowAndHang(showe);

            var poly2 = poly1.OffsetRound(-15.557500000000001, tolerance: 0.08);

            showe.AddRange(poly2);
            Presenter.ShowAndHang(showe);
            //p.RemoveSelfIntersections(ResultType.BothPermitted);
            //p.TriangulateToCoordinates();
        }
        private static void TestConicIntersection()
        {
            var a = 1.3;
            var b = -3.0;
            var c = -4.0;
            var d = -10.0;
            var e = 16.0;
            var f = 1.0;
            var conicH = new GeneralConicSection(a / f, b / f, c / f, d / f, e / f, false);
            a = 1;
            b = -3.4;
            c = -4.2;
            d = -4.1;
            e = 8.2;
            f = 1;
            var conicJ = new GeneralConicSection(a / f, b / f, c / f, d / f, e / f, false);
            foreach (var p in GeneralConicSection.IntersectingConics(conicH, conicJ))
            {
                Console.WriteLine(p);
            }
        }

    }
}
