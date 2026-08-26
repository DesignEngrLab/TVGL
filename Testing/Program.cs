using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;
using WebGPUPresenter;
//using WindowsDesktopPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class Program
    {
        public static string inputFolder = "TestFiles";

        static Random r = new Random();
        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;


        [STAThread]
        private static void Main(string[] args)
        {
            OutputServices.Presenter2D = new Presenter2D();
            OutputServices.Presenter3D = new Presenter3D();
            var dirInfo = IO.BackoutToFolder(inputFolder);

            var files = dirInfo.GetFiles("*.tvgl*");
            foreach (var fileName in files.Skip(1))
            {
                Console.WriteLine("Attempting to open: " + fileName.Name);
                var solids = IO.Open(fileName.FullName);
                if (solids is not TessellatedSolid ts)
                    continue;
                Presenter.ShowAndHang(ts);
                Presenter.ShowAndHang(GetRandomPolygonThroughSolids(ts));
            }
        }

        /// <summary>
        /// Runs the browser-presenter checks without changing the historical default
        /// STL/cross-section loop. Use <c>presenter2d</c>, <c>presenter3d</c>, or
        /// <c>presenters</c> as the first command-line argument.
        /// </summary>
        private static bool RunPresenterOverrideTests(string[] args, DirectoryInfo testFiles)
        {
            var mode = args.FirstOrDefault()?.Trim().ToLowerInvariant();
            if (mode is not ("presenter2d" or "presenter3d" or "presenters"))
                return false;

            var sample = LoadPresenterSample(testFiles);
            if (mode is "presenter2d" or "presenters")
                TestPresenter2DOverrides(sample);
            if (mode is "presenter3d" or "presenters")
                TestPresenter3DOverrides(sample);
            return true;
        }

        private static TessellatedSolid LoadPresenterSample(DirectoryInfo testFiles)
        {
            foreach (var file in testFiles.EnumerateFiles("*", SearchOption.TopDirectoryOnly)
                         .Where(f => string.Equals(f.Extension, ".stl", StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Presenter sample: " + file.Name);
                if (IO.Open(file.FullName) is TessellatedSolid solid)
                    return solid;
            }

            throw new FileNotFoundException("No top-level STL file in the TestFiles directory could be opened as a TessellatedSolid.");
        }

        /// <summary>
        /// Visual test coverage for every implemented interactive IPresenter2D overload.
        /// Each ShowAndHang call requires Continue (or Close) in the browser.
        /// </summary>
        private static void TestPresenter2DOverrides(TessellatedSolid sample)
        {
            var presenter = OutputServices.Presenter2D;
            var outer = new[] { new Vector2(-8, -5), new Vector2(8, -5), new Vector2(8, 5), new Vector2(-8, 5) };
            var hole = new[] { new Vector2(-3, -2), new Vector2(-3, 2), new Vector2(3, 2), new Vector2(3, -2) };
            var polygonWithHole = new Polygon([outer, hole]);
            var triangle = new[] { new Vector2(-6, -4), new Vector2(0, 7), new Vector2(6, -4) };
            var wave = Enumerable.Range(0, 41).Select(i => new Vector2(i - 20, 4 * Math.Sin(i * Math.PI / 10))).ToArray();
            var scatter = Enumerable.Range(0, 17).Select(i => new Vector2(i - 8, ((i * i) % 11) - 5)).ToArray();
            var matrices = new[] { MakeHeatmap(0), MakeHeatmap(0.7), MakeHeatmap(1.4) };

            presenter.ShowAndHang(wave, "2D: one path / line / markers", Plot2DType.Line, false, MarkerType.Circle);
            presenter.ShowAndHang(new[] { outer, triangle }, "2D: multiple paths / area", Plot2DType.Area, true, MarkerType.Diamond);
            presenter.ShowAndHang(new[] { new[] { outer }, new[] { triangle, wave } }, "2D: stepped path groups", Plot2DType.Line, false, MarkerType.Plus);
            presenter.ShowAndHang(polygonWithHole, "2D: polygon with a hole", Plot2DType.Line, MarkerType.Square);
            presenter.ShowAndHang(new[] { polygonWithHole, new Polygon(triangle) }, "2D: polygon collection", Plot2DType.Line, MarkerType.None);
            presenter.ShowAndHang(new[] { outer }, new[] { triangle }, "2D: paired path collections", Plot2DType.Scatter, true, MarkerType.Circle, MarkerType.Cross);
            presenter.ShowAndHang(sample.Vertices.Take(40), Vector3.UnitZ, "2D: projected STL vertices", Plot2DType.Scatter, false, MarkerType.Star);
            presenter.ShowAndHang(new[] { sample.Vertices.Take(20), sample.Vertices.Skip(20).Take(20) }, Vector3.UnitY,
                "2D: projected STL vertex groups", Plot2DType.Line, false, MarkerType.Triangle);
            presenter.ShowAndHang(matrices[0], "2D: matrix heatmap");

            var grid = new Grid<double>();
            grid.Initialize(-5, 5, -5, 5, pixelsPerRow: 18);
            for (var x = 0; x < grid.XCount; x++)
            for (var y = 0; y < grid.YCount; y++)
                grid[x, y] = Math.Sin(x * 0.4) * Math.Cos(y * 0.35);
            presenter.ShowAndHang(grid, value => value, normalizeValues: true);
            presenter.ShowHeatmap(matrices[1], normalizeValues: true);
            presenter.ShowStepsAndHang(matrices, "2D: stepped heatmaps");
            presenter.ShowStepsAndHang(matrices, new[] { outer, triangle, scatter }, connectPointsInLine: true,
                title: "2D: heatmaps with one overlay per step");
            presenter.ShowStepsAndHang(matrices,
                new[] { new[] { outer, scatter }, new[] { triangle }, new[] { outer, triangle } },
                new[] { true, false }, "2D: heatmaps with mixed overlays");

            // Persistent panels are non-blocking; inspect them while the final blocking
            // panel is open. Immediate replaces ID 200, while AddToQueue uses ID 201.
            presenter.Show(outer, "2D live panel: immediate", Plot2DType.Line, true, MarkerType.Circle, HoldType.Immediate, id: 200);
            presenter.Show(triangle, "2D live panel: replacement", Plot2DType.Area, true, MarkerType.Diamond, HoldType.Immediate, id: 200);
            presenter.Show(new[] { wave, scatter }, "2D live panel: queued", Plot2DType.Line, new[] { false, false }, MarkerType.Star,
                HoldType.AddToQueue, timetoShow: 1200, id: 201);
            presenter.ShowAndHang(scatter, "2D: inspect persistent live panels", Plot2DType.Scatter, false, MarkerType.Cross);
        }

        /// <summary>
        /// Visual test coverage for each implemented IPresenter3D overload using an
        /// actual TestFiles STL wherever a tessellated solid is required.
        /// </summary>
        private static void TestPresenter3DOverrides(TessellatedSolid sample)
        {
            var presenter = OutputServices.Presenter3D;
            var shifted = (TessellatedSolid)sample.TransformToNewSolid(Matrix4x4.CreateTranslation(sample.XMax - sample.XMin, 0, 0));
            shifted.SolidColor = new Color(KnownColors.CornflowerBlue);
            var transparent = sample.Copy();
            transparent.SolidColor = new Color(90, 255, 140, 30);
            var vertices = sample.Vertices.Take(48).ToList();
            var loop = new[]
            {
                new Vector3(sample.XMin, sample.YMin, sample.ZMin), new Vector3(sample.XMax, sample.YMin, sample.ZMin),
                new Vector3(sample.XMax, sample.YMax, sample.ZMin), new Vector3(sample.XMin, sample.YMax, sample.ZMin)
            };
            var diagonal = new[]
            {
                new Vector3(sample.XMin, sample.YMin, sample.ZMin), new Vector3(sample.XMax, sample.YMax, sample.ZMax)
            };

            presenter.ShowAndHang(sample, "3D presenter contract", "one STL solid", "uniform/per-face color conversion");
            presenter.ShowAndHang(new Solid[] { sample, shifted }, "3D presenter contract", "multiple STL solids", "separate mesh entries");
            presenter.ShowAndHang(sample.Faces.Take(120), "3D presenter contract", "triangle-face subset", "faces overload");
            presenter.ShowAndHang(loop, closePaths: true, lineThickness: 3, color: new Color(KnownColors.Orange), sample);
            presenter.ShowAndHang(new[] { loop, diagonal }, new[] { true, false }, new[] { 2.0, 5.0 },
                new[] { new Color(KnownColors.Red), new Color(KnownColors.Cyan) }, true, sample);
            presenter.ShowAndHang(new[] { new[] { loop }, new[] { diagonal } }, new[] { true, false }, new[] { 2.0, 5.0 },
                new[] { new Color(KnownColors.Green), new Color(KnownColors.Blue) }, sample);
            presenter.ShowAndHang(new[] { loop }, new[] { true }, new[] { 4.0 }, new[] { new Color(KnownColors.Magenta) }, sample.Faces.Take(80));
            presenter.ShowPointsAndHang(vertices.Select(v => v.Coordinates), radius: Math.Max(1, (sample.XMax - sample.XMin) / 100), color: new Color(KnownColors.Red));
            presenter.ShowPointsAndHang(new[] { vertices.Take(24).Select(v => v.Coordinates), vertices.Skip(24).Select(v => v.Coordinates) },
                radius: Math.Max(1, (sample.XMax - sample.XMin) / 140), colors: new[] { new Color(KnownColors.Yellow), new Color(KnownColors.Cyan) });
            presenter.ShowAndHangTransparentsAndSolids(new[] { transparent }, new[] { shifted });
            presenter.ShowGaussSphereWithIntensity(vertices.Take(16),
                Enumerable.Range(0, 16).Select(i => new Color((byte)255, (byte)(i * 15), (byte)(255 - i * 15), (byte)80)).ToList(), sample);

            presenter.Show(sample, "3D live panel: immediate", HoldType.Immediate, id: 300);
            presenter.Show(shifted, "3D live panel: replacement", HoldType.Immediate, id: 300);
            presenter.Show(new[] { loop, diagonal }, new[] { true, false }, new[] { 2.0, 5.0 },
                new[] { new Color(KnownColors.Orange), new Color(KnownColors.Purple) }, "3D live panel: queued", HoldType.AddToQueue, 1200, 301, sample);

            var pathSteps = new[] { new[] { loop, diagonal } };
            var pathTransforms = new[] { new[] { Matrix4x4.Identity, Matrix4x4.CreateTranslation(0, 0, sample.ZMax - sample.ZMin) } };
            var solidSteps = new[] { new Solid[] { sample, shifted } };
            var solidTransforms = new[] { new[] { Matrix4x4.Identity, Matrix4x4.Null } };
            presenter.ShowStepsAndHang(pathSteps, pathTransforms, solidSteps, solidTransforms,
                new[] { true, false }, new[] { 2.0, 5.0 }, new[] { new Color(KnownColors.Green), new Color(KnownColors.Red) });

            var faceSteps = new[] { new IEnumerable<TriangleFace>[] { sample.Faces.Take(60), sample.Faces.Skip(60).Take(60) } };
            var faceTransforms = new[] { new[] { Matrix4x4.Identity, Matrix4x4.CreateTranslation(sample.XMax - sample.XMin, 0, 0) } };
            presenter.ShowStepsAndHang(pathSteps, pathTransforms, faceSteps, faceTransforms,
                new[] { true, false }, new[] { 2.0, 5.0 }, new[] { new Color(KnownColors.Blue), new Color(KnownColors.Orange) });
        }

        private static double[,] MakeHeatmap(double phase)
        {
            const int size = 25;
            var values = new double[size, size];
            for (var x = 0; x < size; x++)
            for (var y = 0; y < size; y++)
            {
                var dx = x - (size - 1) / 2.0;
                var dy = y - (size - 1) / 2.0;
                values[x, y] = Math.Sin(Math.Sqrt(dx * dx + dy * dy) + phase);
            }
            return values;
        }


        private static void consolePrint(Polygon a)
        {
            foreach (var v in System.Linq.Enumerable.Reverse(a.Vertices))
            {
                Console.WriteLine(v.X + ", " + v.Y);
            }
        }

        public static List<Polygon> GetRandomPolygonThroughSolids(TessellatedSolid solid)
        {
            List<Polygon> polygons = null;
            do
            {
                var normal = (new Vector3(r1, r1, r1)).Normalize();
                var plane = new Plane(solid.Center.Dot(normal), normal);

                polygons = solid.GetCrossSection(plane, out _);
            } while (polygons == null);
            return polygons;
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
