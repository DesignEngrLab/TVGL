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
            //PolygonOperationsTesting.DebugEdgeCases();
            //DebugIntersectCases(BackoutToFolder("TestFiles\\polygons"));
            DebugOffsetCases(BackoutToFolder("TestFiles\\polygons"));
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
            var fileNames = dir.GetFiles("*.json").ToList();
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
                    fileNames.RemoveAll(fn => fn.FullName==item.FullName);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                if (polygons.All(p => p == null)) continue;
                Debug.WriteLine("Attempting: " + filename);
               Presenter.ShowAndHang(polygons);
                var result = polygons.OffsetSquare(offset);
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
                    fileNames.Remove(item);
                    IO.Open(item.FullName, out Polygon p);
                    polygons.Add(p);
                }
                Debug.WriteLine("Attempting: " + filename);
                //Presenter.ShowAndHang(polygons);
                var result = polygons.IntersectPolygons();
                //Presenter.ShowAndHang(result);
            }
        }

    }
}