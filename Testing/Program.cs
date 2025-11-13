using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGLUnitTestsAndBenchmarking.Misc_Tests;
using WindowsDesktopPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class Program
    {
        public static string inputFolder = "TVGL";

        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;


        [STAThread]
        private static void Main(string[] args)
        {
            Global.Presenter2D = new Presenter2D();
            Global.Presenter3D = new Presenter3D();
            var dirInfo = IO.BackoutToFolder(inputFolder);

            var paraboloid = new GeneralQuadric(.1,.51,0,0,0,0,0,0,1,-10);
            Presenter.ShowAndHang(paraboloid.Tessellate(-10,10, -10, 10, -10, 10, 0.5));
            paraboloid.Copy();
            IO.Open(Path.Combine(dirInfo.FullName, "a.json"), out Polygon A);
            IO.Open(Path.Combine(dirInfo.FullName, "b.json"), out Polygon B);
            var union = A.MinkowskiSum(B);
            foreach (var fileName in dirInfo.GetFiles("*"))
            {
                Console.WriteLine("Attempting to open: " + fileName.Name);
                var solid = IO.Open(fileName.FullName);
                Presenter.ShowAndHang(solid);
                var circlePath = new List<Vector3>();
                var transforms = new List<Matrix4x4>();
                for (int i = 0; i < 12; i++)
                {
                    circlePath.Add(new Vector3(10 * Math.Cos(i * Math.PI / 12), 10 * Math.Sin(i * Math.PI / 12), 0));
                    transforms.Add(Matrix4x4.CreateRotationZ(i * Math.PI / 12));
                }
                //var paths = new List<List<Vector3>>();
                //for (int i = 1; i < 12; i++)
                //    paths.Add([circlePath[i - 1], circlePath[i]]);

                //Presenter.ShowStepsAndHang([paths], [solid], [transforms]);
            }
        }

        private static void consolePrint(Polygon a)
        {
           foreach (var v in System.Linq.Enumerable.Reverse(a.Vertices))
            {
                Console.WriteLine(v.X+", "+v.Y);
            }
        }

        public static IEnumerable<List<Polygon>> GetRandomPolygonThroughSolids(DirectoryInfo dir)
        {
            var index = 0;
            var valid3DFileExtensions = new HashSet<string> { ".stl", ".ply", ".obj", ".3mf", ".tvglz" };
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories)
                .Where(f => valid3DFileExtensions.Contains(f.Extension.ToLower()))
                .OrderBy(x => Guid.NewGuid());
            foreach (var fileName in allFiles.Skip(index))
            {
                Console.Write(index + ": Attempting to open: " + fileName.Name);
                TessellatedSolid[] solids = null;
                var sw = Stopwatch.StartNew();

                //IO.Open(fileName.FullName, out  solids, TessellatedSolidBuildOptions.Minimal);
                IO.Open(fileName.FullName, out solids);
                if (solids.Length == 0) continue;
                var solid = solids.MaxBy(s => s.Volume);
                var normal = (new Vector3(r1, r1, r1)).Normalize();
                var distanceAlong = solid.Vertices.GetLengthAndExtremeVertex(normal, out var loVertex, out _);
                var planeDistance = distanceAlong * r.NextDouble();
                var plane = new Plane(planeDistance, normal);
                var polygons = solid.GetCrossSection(plane, out _);
                if (polygons.Count > 0) yield return polygons;
                index++;
            }
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
                Console.WriteLine("Attempting: " + filename);
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
                Console.WriteLine("Attempting: " + filename);
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
                //Console.WriteLine("Attempting: " + fileName, 1);
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
    // Add this extension method to enable shuffling of Vector3[] arrays.
    public static class ArrayExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this T[] array)
        {
            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (array[n], array[k]) = (array[k], array[n]);
            }
        }
    }
}
