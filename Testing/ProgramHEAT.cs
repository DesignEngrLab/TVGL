using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using WebGPUPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    internal partial class Program
    {
        private static void HeatMethodTest(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine(" TVGL Heat Method for Geodesic Distances (Port of CGAL)   ");
            Console.WriteLine("==========================================================\n");

            var dirInfo = IO.BackoutToFolder("TestFiles");
            var repoRoot = dirInfo.Parent?.FullName ?? dirInfo.FullName;

            // 1. Run Verification Test on Degenerate Mesh (matching CGAL intrinsic test)
            RunDegenerateMeshTest(repoRoot);

            // 2. Run Geodesic Distance Estimation & Benchmark on Elephant Mesh
            var testFile = dirInfo.GetFiles("*.stl").First();
            Console.WriteLine($"\nOpening test mesh: {testFile.Name}...");
            IO.Open(testFile.FullName, out TessellatedSolid ts);
            Console.WriteLine($"Mesh loaded: {ts.NumberOfVertices} vertices, {ts.NumberOfFaces} faces, {ts.NumberOfEdges} edges.\n");

            RunElephantBenchmark(ts);

            // 3. Visualize colored geodesic distance field
            bool noGui = args.Any(a => a.Equals("--no-gui", StringComparison.OrdinalIgnoreCase));
            if (!noGui)
            {
                OutputServices.Presenter2D = new Presenter2D();
                OutputServices.Presenter3D = new Presenter3D();
                Console.WriteLine("\nDisplaying colored geodesic distance field in WebGPUPresenter...");
                Presenter.ShowAndHang(ts);
            }
            else
            {
                Console.WriteLine("\n[--no-gui specified, skipping 3D viewer]");
            }
        }

        private static void RunDegenerateMeshTest(string repoRoot)
        {
            string degenPath = Path.Combine(repoRoot, "test", "Heat_method_3", "data", "rectangle_with_degenerate_faces.off");
            if (!File.Exists(degenPath))
            {
                Console.WriteLine($"[SKIPPED] Test file not found at: {degenPath}");
                return;
            }

            Console.WriteLine("Running verification test on 'rectangle_with_degenerate_faces.off'...");
            var surfaceOptions = new TessellatedSolidBuildOptions
            {
                AutomaticallyRepairHoles = false,
                AutomaticallyRepairNegligibleFaces = false,
                AutomaticallyInvertNegativeSolids = false,
                DefineConvexHull = false,
                FindNonsmoothEdges = false,
                CheckModelIntegrity = false
            };
            IO.Open(degenPath, out TessellatedSolid degenTs, surfaceOptions);

            var hm = new HeatMethod(degenTs, GeodesicDistanceMode.IntrinsicDelaunay);
            hm.AddSource(0);
            var distances = hm.EstimateGeodesicDistances();

            double maxDist = distances.Max();
            Console.WriteLine($"  Intrinsic Delaunay max distance: {maxDist:F4} (expected range: 1.0 to 2.26)");

            Debug.Assert(maxDist > 1.0 && maxDist < 2.26, $"Max distance {maxDist} is out of expected range (1.0, 2.26)");

            int farVertex = Array.IndexOf(distances, maxDist);
            hm.AddSource(farVertex);
            var twoSourceDistances = hm.EstimateGeodesicDistances();
            double twoSourceMax = twoSourceDistances.Max();
            Console.WriteLine($"  Two sources max distance: {twoSourceMax:F4}");

            hm.RemoveSource(farVertex);
            hm.ClearSources();
            hm.AddSources([0, farVertex]);
            var multiSourceDistances = hm.EstimateGeodesicDistances();
            Debug.Assert(Math.Abs(multiSourceDistances.Max() - twoSourceMax) < 1e-4);

            Console.WriteLine("  [PASS] Degenerate mesh test passed successfully!");
        }

        private static void RunElephantBenchmark(TessellatedSolid ts)
        {
            // Pick a source vertex: vertex 0 (or bottom of elephant foot)
            int sourceIndex = 0;
            Console.WriteLine($"Computing geodesic distances from source vertex #{sourceIndex}...");

            // --- Intrinsic Delaunay Mode ---
            var sw = Stopwatch.StartNew();
            var hmIdt = new HeatMethod(ts, GeodesicDistanceMode.IntrinsicDelaunay);
            sw.Stop();
            long idtSetupMs = sw.ElapsedMilliseconds;

            sw.Restart();
            hmIdt.AddSource(sourceIndex);
            var idtDistances = hmIdt.EstimateGeodesicDistances();
            sw.Stop();
            long idtSolveMs = sw.ElapsedMilliseconds;

            double idtMin = idtDistances.Min();
            double idtMax = idtDistances.Max();
            Console.WriteLine($"[Intrinsic Delaunay Mode]");
            Console.WriteLine($"  Setup (iDT + Pre-factoring): {idtSetupMs} ms");
            Console.WriteLine($"  Solve (Diffusion + Gradient + Poisson): {idtSolveMs} ms");
            Console.WriteLine($"  Distance range: [{idtMin:F4}, {idtMax:F4}]");

            // --- Re-query with second source ---
            sw.Restart();
            int farthestIndex = Array.IndexOf(idtDistances, idtMax);
            hmIdt.AddSource(farthestIndex);
            var twoSourceDistances = hmIdt.EstimateGeodesicDistances();
            sw.Stop();
            long idtRequeryMs = sw.ElapsedMilliseconds;
            Console.WriteLine($"  Re-query with second source (vertex #{farthestIndex}): {idtRequeryMs} ms (distance range: [{twoSourceDistances.Min():F4}, {twoSourceDistances.Max():F4}])");

            // --- Direct Mode ---
            sw.Restart();
            var hmDirect = new HeatMethod(ts, GeodesicDistanceMode.Direct);
            sw.Stop();
            long directSetupMs = sw.ElapsedMilliseconds;

            sw.Restart();
            hmDirect.AddSource(sourceIndex);
            var directDistances = hmDirect.EstimateGeodesicDistances();
            sw.Stop();
            long directSolveMs = sw.ElapsedMilliseconds;

            double directMin = directDistances.Min();
            double directMax = directDistances.Max();
            Console.WriteLine($"[Direct Mode]");
            Console.WriteLine($"  Setup: {directSetupMs} ms");
            Console.WriteLine($"  Solve: {directSolveMs} ms");
            Console.WriteLine($"  Distance range: [{directMin:F4}, {directMax:F4}]");

            // --- Color the mesh with geodesic distance field ---
            ColorMeshWithDistances(ts, idtDistances);
        }

        private static void ColorMeshWithDistances(TessellatedSolid ts, double[] distances)
        {
            double maxDist = distances.Max();
            if (maxDist <= 1e-12) return;

            ts.HasUniformColor = false;

            for (int i = 0; i < ts.NumberOfFaces; i++)
            {
                var face = ts.Faces[i];
                double d0 = distances[face.A.IndexInList];
                double d1 = distances[face.B.IndexInList];
                double d2 = distances[face.C.IndexInList];

                // Average distance on the face normalized to [0, 1]
                double avgDist = (d0 + d1 + d2) / (3.0 * maxDist);
                face.Color = GetTurboColorWithContourBands(avgDist);
            }

            Console.WriteLine("Mesh faces successfully colored with geodesic distance heatmap and contour bands.");
        }

        /// <summary>
        /// Converts normalized scalar [0, 1] to a Turbo colormap with contour bands.
        /// </summary>
        private static Color GetTurboColorWithContourBands(double t)
        {
            t = Math.Clamp(t, 0.0, 1.0);

            // Turbo colormap approximation
            double r = Math.Clamp(0.1357 + t * (4.5974 - t * (42.3277 - t * (130.5887 - t * (150.5614 - t * 58.1375)))), 0.0, 1.0);
            double g = Math.Clamp(0.0914 + t * (2.1856 + t * (4.8052 - t * (14.0195 - t * (4.2109 - t * 2.7747)))), 0.0, 1.0);
            double b = Math.Clamp(0.1067 + t * (12.5732 - t * (80.1471 - t * (228.6946 - t * (286.0645 - t * 133.0903)))), 0.0, 1.0);

            // Add sharp contour bands / isolines every 5%
            double contour = Math.Abs(Math.Sin(t * 20.0 * Math.PI));
            if (contour < 0.15)
            {
                double factor = 0.5 + 0.5 * (contour / 0.15);
                r *= factor;
                g *= factor;
                b *= factor;
            }

            byte rByte = (byte)(r * 255.0);
            byte gByte = (byte)(g * 255.0);
            byte bByte = (byte)(b * 255.0);

            return new Color(255, rByte, gByte, bByte);
        }
    }
}
