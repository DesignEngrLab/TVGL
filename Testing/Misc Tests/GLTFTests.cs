using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.GLTFImportExport;

namespace TVGLUnitTestsAndBenchmarking.Misc_Tests
{
    internal static class GLTFTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== Starting TVGL GLTF / GLB Verification Tests ===");

            TestSingleSolidRoundTrip();
            TestMultiColorSolidRoundTrip();
            TestSolidAssemblyRoundTrip();
            TestStreamRoundTrip();
            TestThirdPartyModel();

            Console.WriteLine("=== ALL GLTF / GLB VERIFICATION TESTS PASSED! ===");
        }

        private static void TestSingleSolidRoundTrip()
        {
            Console.WriteLine("1. Testing Single Solid Round-Trip (.glb and .gltf)...");
            var box = CreateTestBox(10.0, 20.0, 30.0, new Color(KnownColors.Red), "TestBox");
            double expectedVolume = 10.0 * 20.0 * 30.0; // 6000.0

            var tempGlb = Path.Combine(Path.GetTempPath(), $"tvgl_box_{Guid.NewGuid():N}.glb");
            var tempGltf = Path.Combine(Path.GetTempPath(), $"tvgl_box_{Guid.NewGuid():N}.gltf");

            try
            {
                // GLB test
                bool savedGlb = GLTF.Save(tempGlb, box);
                if (!savedGlb || !File.Exists(tempGlb))
                    throw new Exception("GLTF.Save failed to create .glb file.");

                var loadedGlb = GLTF.Open(tempGlb);
                if (loadedGlb == null)
                    throw new Exception("GLTF.Open returned null for .glb.");

                double volGlb = loadedGlb.Volume;
                double errGlb = Math.Abs(volGlb - expectedVolume) / expectedVolume;
                Console.WriteLine($"   [GLB] Faces: {loadedGlb.NumberOfFaces}, Vertices: {loadedGlb.NumberOfVertices}, Volume: {volGlb:F2} (err: {errGlb:P3})");
                if (errGlb > 0.01)
                    throw new Exception($"Volume mismatch on .glb reload: actual {volGlb} vs expected {expectedVolume}");
                if (loadedGlb.SolidColor.R != box.SolidColor.R || loadedGlb.SolidColor.G != box.SolidColor.G || loadedGlb.SolidColor.B != box.SolidColor.B)
                    throw new Exception($"Solid color mismatch on .glb reload: actual {loadedGlb.SolidColor} vs expected {box.SolidColor}");

                // GLTF test
                bool savedGltf = GLTF.Save(tempGltf, box);
                if (!savedGltf || !File.Exists(tempGltf))
                    throw new Exception("GLTF.Save failed to create .gltf file.");

                var loadedGltf = GLTF.Open(tempGltf);
                if (loadedGltf == null)
                    throw new Exception("GLTF.Open returned null for .gltf.");

                double volGltf = loadedGltf.Volume;
                double errGltf = Math.Abs(volGltf - expectedVolume) / expectedVolume;
                Console.WriteLine($"   [GLTF] Faces: {loadedGltf.NumberOfFaces}, Vertices: {loadedGltf.NumberOfVertices}, Volume: {volGltf:F2} (err: {errGltf:P3})");
                if (errGltf > 0.01)
                    throw new Exception($"Volume mismatch on .gltf reload: actual {volGltf} vs expected {expectedVolume}");
            }
            finally
            {
                TryDelete(tempGlb);
                TryDelete(tempGltf);
                var binPath = Path.ChangeExtension(tempGltf, ".bin");
                TryDelete(binPath);
            }
        }

        private static void TestMultiColorSolidRoundTrip()
        {
            Console.WriteLine("2. Testing Multi-Color Solid Round-Trip...");
            var red = new Color(KnownColors.Red);
            var blue = new Color(KnownColors.Blue);

            var colors = new Color[12];
            for (int i = 0; i < 6; i++) colors[i] = red;
            for (int i = 6; i < 12; i++) colors[i] = blue;

            var box = CreateTestBox(10.0, 20.0, 30.0, colors, "MultiColorBox");
            var tempGlb = Path.Combine(Path.GetTempPath(), $"tvgl_multicolor_{Guid.NewGuid():N}.glb");

            try
            {
                bool saved = GLTF.Save(tempGlb, box);
                if (!saved) throw new Exception("GLTF.Save failed for multi-color solid.");

                var loaded = GLTF.Open(tempGlb);
                if (loaded == null) throw new Exception("GLTF.Open returned null for multi-color solid.");

                int redFaces = loaded.Faces.Count(f => (f.Color ?? loaded.SolidColor) is { } c && c.R == red.R && c.G == red.G && c.B == red.B);
                int blueFaces = loaded.Faces.Count(f => (f.Color ?? loaded.SolidColor) is { } c && c.R == blue.R && c.G == blue.G && c.B == blue.B);

                Console.WriteLine($"   Multi-color faces found: {redFaces} Red, {blueFaces} Blue (Total: {loaded.NumberOfFaces})");
                if (redFaces != 6 || blueFaces != 6)
                    throw new Exception($"Expected 6 red and 6 blue faces, got {redFaces} red and {blueFaces} blue.");
            }
            finally
            {
                TryDelete(tempGlb);
            }
        }

        private static void TestSolidAssemblyRoundTrip()
        {
            Console.WriteLine("3. Testing SolidAssembly Round-Trip...");
            var box1 = CreateTestBox(10.0, 10.0, 10.0, new Color(KnownColors.Green), "Box1");
            var assembly = new SolidAssembly("TestAssembly");
            assembly.RootAssembly.Add(box1, Matrix4x4.Identity);
            assembly.RootAssembly.Add(box1, Matrix4x4.CreateTranslation(50.0, 0.0, 0.0));
            assembly.CompleteInitialization();

            var tempGlb = Path.Combine(Path.GetTempPath(), $"tvgl_assembly_{Guid.NewGuid():N}.glb");

            try
            {
                bool saved = GLTF.Save(tempGlb, assembly);
                if (!saved) throw new Exception("GLTF.Save failed for SolidAssembly.");

                // Test OpenSolidAssembly
                var loadedAssembly = GLTF.OpenSolidAssembly(tempGlb);
                if (loadedAssembly == null) throw new Exception("GLTF.OpenSolidAssembly returned null.");

                Console.WriteLine($"   Loaded Assembly distinct solids: {loadedAssembly.Solids?.Length ?? 0}");
                if (loadedAssembly.Solids == null || loadedAssembly.Solids.Length != 1)
                    throw new Exception($"Expected 1 distinct solid in assembly, got {loadedAssembly.Solids?.Length}");

                // Test OpenSolids
                var worldSolids = GLTF.OpenSolids(tempGlb);
                Console.WriteLine($"   Loaded World Solids count: {worldSolids.Count}");
                if (worldSolids.Count != 2)
                    throw new Exception($"Expected 2 world solids, got {worldSolids.Count}");

                foreach (var s in worldSolids)
                {
                    Console.WriteLine($"   Solid {s.Name}: XMin={s.XMin}, XMax={s.XMax}, CenterX={s.Center.X}");
                }
                var minXs = worldSolids.Select(s => s.XMin).OrderBy(x => x).ToList();
                if (Math.Abs(minXs[0] - 0.0) > 0.1 || Math.Abs(minXs[1] - 50.0) > 0.1)
                    throw new Exception($"Incorrect translated solid XMin positions: {minXs[0]}, {minXs[1]}");
            }
            finally
            {
                TryDelete(tempGlb);
            }
        }

        private static void TestStreamRoundTrip()
        {
            Console.WriteLine("4. Testing MemoryStream Round-Trip...");
            var box = CreateTestBox(5.0, 5.0, 5.0, new Color(KnownColors.Yellow), "StreamBox");
            double expectedVolume = 125.0;

            using var ms = new MemoryStream();
            bool saved = GLTF.Save(ms, box, asBinary: true);
            if (!saved) throw new Exception("GLTF.Save to Stream failed.");

            ms.Position = 0;
            var loaded = GLTF.Open(ms);
            if (loaded == null) throw new Exception("GLTF.Open from Stream returned null.");

            double vol = loaded.Volume;
            double err = Math.Abs(vol - expectedVolume) / expectedVolume;
            Console.WriteLine($"   Stream volume: {vol:F2} (err: {err:P3})");
            if (err > 0.01)
                throw new Exception($"Volume mismatch on stream reload: {vol} vs {expectedVolume}");
        }

        private static void TestThirdPartyModel()
        {
            const string examplePath = @"C:\Users\campmatt\source\repos\SharpGLTF\examples\Example1.glb";
            if (!File.Exists(examplePath))
            {
                Console.WriteLine("5. Skipping third-party model (Example1.glb not found).");
                return;
            }

            Console.WriteLine("5. Testing third-party model import (Example1.glb)...");
            var solids = GLTF.OpenSolids(examplePath);
            Console.WriteLine($"   Imported solids from Example1.glb: {solids.Count}");
            if (solids.Count == 0)
                throw new Exception("Failed to import any solids from Example1.glb");

            foreach (var s in solids)
            {
                Console.WriteLine($"   - Solid: '{s.Name}', Vertices: {s.NumberOfVertices}, Faces: {s.NumberOfFaces}, Volume: {s.Volume:F2}");
            }
        }

        private static TessellatedSolid CreateTestBox(double x, double y, double z, Color color, string name)
        {
            return CreateTestBox(x, y, z, new[] { color }, name);
        }

        private static TessellatedSolid CreateTestBox(double x, double y, double z, IList<Color> colors, string name)
        {
            var vertices = new Vector3[]
            {
                new(0, 0, 0), new(x, 0, 0), new(x, y, 0), new(0, y, 0),
                new(0, 0, z), new(x, 0, z), new(x, y, z), new(0, y, z)
            };

            var faces = new (int, int, int)[]
            {
                // bottom (-Z)
                (0, 2, 1), (0, 3, 2),
                // top (+Z)
                (4, 5, 6), (4, 6, 7),
                // front (-Y)
                (0, 1, 5), (0, 5, 4),
                // back (+Y)
                (3, 6, 2), (3, 7, 6),
                // left (-X)
                (0, 4, 7), (0, 7, 3),
                // right (+X)
                (1, 2, 6), (1, 6, 5)
            };

            return new TessellatedSolid(vertices, faces, colors, name: name);
        }

        private static void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                try { File.Delete(path); } catch { }
            }
        }
    }
}

