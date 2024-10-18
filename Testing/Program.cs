using Newtonsoft.Json.Linq;
using StarMathLib;
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

            var index = 0;
            var valid3DFileExtensions = new HashSet<string> { ".stl", ".ply", ".obj", ".3mf", ".tvglz" };
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories).Where(f => valid3DFileExtensions.Contains(f.Extension.ToLower()))
                ; //.OrderBy(fi => fi.Length);
            foreach (var fileName in allFiles.Skip(index))
            {
                Console.WriteLine(index + ": Attempting to open: " + fileName.Name);
                TessellatedSolid[] solids = null;
                IO.Open(fileName.FullName, out solids);
                var ts = solids[0];
                //Presenter.ShowAndHang(ts);
                var vert4Ds = new Vector4[ts.NumberOfVertices];
                for (int i = 0; i < ts.NumberOfVertices; i++)
                {
                    var pt = ts.Vertices[i].Coordinates;
                    var pt4 = new Vector4(pt, pt.X * pt.X + pt.Y * pt.Y + pt.Z * pt.Z);
                    vert4Ds[i] = pt4; // new Vertex4D(pt4, i);
                }
                ConvexHull4D.Create(vert4Ds, out var ch4d, out _);
                var faces = ts.Faces.ToList();
                //faces.Clear();
                var colorEnumerator = Color.GetRandomColors().GetEnumerator();
                var alpha = 0.15 * (ts.Bounds[1] - ts.Bounds[0]).Length();
                var alphaSqd = alpha * alpha;
                var tetsToDelete = new HashSet<ConvexHullFace4D>();
                foreach (var vp in ch4d.VertexPairs)
                {
                    var v1 = vp.Vertex1.Coordinates;
                    var v13D = new Vector3(v1.X, v1.Y, v1.Z);
                    var v2 = vp.Vertex2.Coordinates;
                    var v23D = new Vector3(v2.X, v2.Y, v2.Z);
                    if ((v13D-v23D).LengthSquared() > alphaSqd)
                        foreach (var tet in vp.Tetrahedra)
                            tetsToDelete.Add(tet);
                }

                foreach (var tetra in ch4d.Tetrahedra)
                {
                    if (tetsToDelete.Contains(tetra)) continue;
                    var color = colorEnumerator.MoveNext() ? colorEnumerator.Current : null;
                    foreach (var edge4D in tetra.Faces)
                    {
                        var aIndex = edge4D.A.IndexInList;
                        var bIndex = edge4D.B.IndexInList;
                        var cIndex = edge4D.C.IndexInList;
                        var face = new TriangleFace(ts.Vertices[aIndex], ts.Vertices[bIndex], ts.Vertices[cIndex]);
                        face.Color = new Color(133, color.R, color.G, color.B);
                        faces.Add(face);
                    }
                }
                Console.WriteLine("presenting...");
                Presenter.ShowAndHang(faces);
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
