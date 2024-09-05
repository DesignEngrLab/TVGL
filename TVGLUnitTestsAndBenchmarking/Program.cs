using Newtonsoft.Json.Linq;
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
        //public static string inputFolder = "Input";

        //public static string inputFolder = "OneDrive - medemalabs.com";
        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;


        [STAThread]
        private static void Main(string[] args)
        {
            var myWriter = new ConsoleTraceListener();
            Trace.Listeners.Add(myWriter);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            DirectoryInfo dir = Program.BackoutToFolder(inputFolder);
            //Voxels.TestVoxelization(dir);
            for (int iter = 0; iter < 100; iter++)
            {

                var axis = new Vector3(r1, r1, r1).Normalize();
                //var axis = Vector3.UnitZ;
                var anchor = new Vector3(r100, r100, r100) / 10;
                anchor = anchor - anchor.Dot(axis) * axis;
                var radius = Math.Sqrt(Math.Abs(r100));
                var cylinder = new Cylinder
                {
                    Anchor = anchor,
                    Axis = axis,
                    Radius = radius,
                };
                var cosAxis = axis.GetPerpendicularDirection();
                var sinAxis = axis.Cross(cosAxis);
                var tx = r100;
                var ty = r100;
                var k = 10;
                var zStep = 0.2;
                var angleStep = 0.5;
                var helixPoints = new Vector3[k];
                for (int i = 0; i < k; i++)
                {
                    var ctr = anchor + axis * zStep * i;
                    helixPoints[i] = ctr + radius * Math.Cos(angleStep * i) * cosAxis + radius * Math.Sin(angleStep * i) * sinAxis;
                    helixPoints[i] += 0.001 * new Vector3(r1, r1, r1);
                }
                cylinder.MinDistanceAlongAxis = helixPoints[0].Dot(axis);
                cylinder.MaxDistanceAlongAxis = helixPoints[^1].Dot(axis);
                cylinder.Tessellate();
                cylinder.SetColor(new Color(50, 250, 50, 250));
                var gq = GeneralQuadric.DefineFromPoints(helixPoints, out _);
                gq.Tessellate(-50, 50, -50, 50, -50, 50, 2);
                gq.SetColor(new Color(50, 50, 250, 250));
                Presenter.ShowVertexPathsWithFaces([helixPoints], cylinder.Faces.Concat(gq.Faces), 4);

                gq.DefineAsCylinder(out var cylGQ);
            }
            return;

            var index = 0;
            var valid3DFileExtensions = new HashSet<string> { ".stl", ".ply", ".obj", ".3mf", ".tvglz" };
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories).Where(f => valid3DFileExtensions.Contains(f.Extension.ToLower()))
                ; //.OrderBy(fi => fi.Length);
            foreach (var fileName in allFiles.Skip(index))
            {
                Console.WriteLine(index + ": Attempting to open: " + fileName.Name);
                TessellatedSolid[] solids = null;
                IO.Open(fileName.FullName, out solids);
                //var vs = VoxelizedSolid.CreateFrom(solids[0], 66);
                //Presenter.ShowAndHang(vs);
                var sw = Stopwatch.StartNew();
                Presenter.ShowAndHang(solids);
                //ConvexHull.Test2(solids.MaxBy(s => s.Volume));
                sw.Stop();
                index++;
            }
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
}
