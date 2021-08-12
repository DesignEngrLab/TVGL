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
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;


        [STAThread]
        private static void Main(string[] args)
        {
            //JustShowMeThePolygons(BackoutToFolder("TestFiles\\polygons"));
            //PolygonOperationsTesting.DebugEdgeCases();
            //DebugIntersectCases(BackoutToFolder("TestFiles\\polygons"));
            //DebugOffsetCases(BackoutToFolder("TestFiles\\polygons"));
            DebugUnionCases(BackoutToFolder("TestFiles\\polygons"));
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
            var fileNames = dir.GetFiles("endles*.json").ToList();
            while (fileNames.Any())
            {
                //var filename = fileNames[0].Name;
                var filename = fileNames[r.Next(fileNames.Count)].Name;
                var nameSegments = filename.Split('.');
                var preName = string.Join('.', nameSegments.Take(nameSegments.Length - 1).ToArray());
                //var offset = double.Parse(nameSegments[^2]);
                var offset = -0.2;
                var polygons = new List<Polygon>();
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                if (polygons.All(p => p == null)) continue;
                Debug.WriteLine("Attempting: " + filename);
                Presenter.ShowAndHang(polygons);
                var result = polygons.OffsetSquare(offset, -0.002);
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

                var polygons = new List<Polygon>();
                foreach (var item in dir.GetFiles(preName + "*"))
                {
                    fileNames.RemoveAll(fn => fn.FullName == item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                Debug.WriteLine("Attempting: " + filename);
                //Presenter.ShowAndHang(polygons);
                var result = polygons.IntersectPolygons();
                //Presenter.ShowAndHang(result);
            }
        }
        public static void DebugUnionCases(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("union*.json").ToList();
            var polygons = new List<Polygon>();

            for (int i = 0; i < 5; i++)
            {
                var item = dir.GetFiles("unionProblem" + i + ".json").First(); 
                //var item = fileNames[i];
                Console.WriteLine(item.FullName);
                IO.Open(item.FullName, out Polygon p);
                polygons.Add(p);
            }
            //while (fileNames.Any())
            //{
            //    var filename = fileNames[r.Next(fileNames.Count)].Name;
            //    var nameSegments = filename.Split('.');
            //    var preName = string.Join('.', nameSegments.Take(nameSegments.Length - 2).ToArray());

            //    var polygons = new List<Polygon>();
            //    foreach (var item in dir.GetFiles(preName + "*"))
            //    {
            //        fileNames.RemoveAll(fn => fn.FullName == item.FullName);
            //        IO.Open(item.FullName, out Polygon p);
            //        polygons.Add(p);
            //    }
            //Debug.WriteLine("Attempting: " + filename);
            //Presenter.ShowAndHang(polygons.Take(4));
            //Presenter.ShowAndHang(polygons.Skip(4));
            //Presenter.ShowAndHang(polygons);
            var result = polygons.UnionPolygons();
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

            var poly1 = silhouetteBeforeFace.OffsetMiter(15.557500000000001, 0.08);
            var showe = new List<Polygon>();
            showe.AddRange(silhouetteBeforeFace);
            showe.AddRange(poly1);
            Presenter.ShowAndHang(showe);

            var poly2 = poly1.OffsetRound(-15.557500000000001, 0.08);

            showe.AddRange(poly2);
            Presenter.ShowAndHang(showe);
            //p.RemoveSelfIntersections(ResultType.BothPermitted);
            //p.TriangulateToCoordinates();
        }
    }
}
