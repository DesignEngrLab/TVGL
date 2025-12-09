using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.PointCloud;
using WindowsDesktopPresenter;

namespace TVGLUnitTestsAndBenchmarking
{
    internal class ICPTesting
    {
        static Random r = new Random();

        static double r1 => 2.0 * r.NextDouble() - 1.0;
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        internal static void Test1()
        {
            var points = new List<Vector3> {
                new Vector3(3, 1,1),
                new Vector3(2, 6,1),
                new Vector3(5,4,1),
                new Vector3(8,7, 2),
                new Vector3(10,2, 2),
                new Vector3(13,3, 2),
            };
            var quat = new Quaternion(new Vector4(3,2,1,1).Normalize());
            var translate = Matrix4x4.CreateTranslation(1, 2, 3);
            var transfrom = Matrix4x4.CreateFromQuaternion(quat) * translate;
            var targetPoints = points.Select(p => p.Transform(transfrom)).ToList();
            var tPredicted = IterativeClosestPoint3D.Run(points, targetPoints);
            OutputServices.Logger.LogInformation(transfrom.ToString());
            OutputServices.Logger.LogInformation(tPredicted.ToString());
        }
        public static void TestPoints(DirectoryInfo dir)
        {
            var attenuation = 0.1;

            //#if PRESENT
            var index = 2;
            var valid3DFileExtensions = new HashSet<string> { ".stl", ".ply", ".obj", ".3mf" };// ".tvglz", 
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories).Where(f => valid3DFileExtensions.Contains(f.Extension.ToLower()))
                ; //.OrderBy(fi => fi.Length);
            foreach (var fileName in allFiles.Skip(index))
            {
                Console.Write(index + ": Attempting to open: " + fileName.Name);
                TessellatedSolid[] solids = null;
                var sw = Stopwatch.StartNew();

                //IO.Open(fileName.FullName, out  solids, TessellatedSolidBuildOptions.Minimal);
                IO.Open(fileName.FullName, out solids);
                sw.Stop();
                if (solids.Length == 0) continue;
                Console.WriteLine("," + solids[0].NumberOfVertices + "," + solids[0].NumberOfEdges + "," +
                    solids[0].NumberOfFaces + "," + sw.ElapsedTicks);
                var solid = solids[0];
                solid.HasUniformColor = true;
                solid.SolidColor = new Color(100, 100, 100, 100);
                var transform = Matrix4x4.CreateFromYawPitchRoll(attenuation * Math.Tau * r1, attenuation * Math.Tau * r1, attenuation * Math.Tau * r1) * Matrix4x4.CreateTranslation(r100, r100, r100);
                var origPoints = solid.Vertices.Select(v => v.Coordinates).ToList();
                var target = (TessellatedSolid)solid.TransformToNewSolid(transform);
                target.SolidColor = new Color(100, 10, 200, 200);
                Presenter.ShowAndHang(new[] { solid, target });
                var targetPoints = target.Vertices.Select(v => v.Coordinates).ToList();
                var matrix = TVGL.PointCloud.IterativeClosestPoint3D.Run(targetPoints, origPoints);
                var result = (TessellatedSolid)solid.TransformToNewSolid(matrix);
                var resultPoints = result.Vertices.Select(v => v.Coordinates).ToList();
                result.SolidColor = new Color(100, 200, 10, 200);
                Presenter.ShowAndHang(new[] { solid, target, result });

                var err = targetPoints.Zip(resultPoints, (t, r) => (t - r).LengthSquared()).Sum() / targetPoints.Count;
                Console.WriteLine("Error: " + err);
                index++;
            }
        }

        public static void TestFaces(DirectoryInfo dir)
        {
            var attenuation = 1.0;

            //#if PRESENT
            var index = 2;
            var valid3DFileExtensions = new HashSet<string> { ".stl", ".ply", ".obj", ".3mf" };// ".tvglz", 
            var allFiles = dir.GetFiles("*", SearchOption.AllDirectories).Where(f => valid3DFileExtensions.Contains(f.Extension.ToLower()))
                ; //.OrderBy(fi => fi.Length);
            foreach (var fileName in allFiles.Skip(index))
            {
                Console.Write(index + ": Attempting to open: " + fileName.Name);
                TessellatedSolid[] solids = null;
                var sw = Stopwatch.StartNew();

                //IO.Open(fileName.FullName, out  solids, TessellatedSolidBuildOptions.Minimal);
                IO.Open(fileName.FullName, out solids);
                sw.Stop();
                if (solids.Length == 0) continue;
                Console.WriteLine("," + solids[0].NumberOfVertices + "," + solids[0].NumberOfEdges + "," +
                    solids[0].NumberOfFaces + "," + sw.ElapsedTicks);
                var solid = solids[0];
                solid.HasUniformColor = true;
                solid.SolidColor = new Color(100, 100, 100, 100);
                var transform = Matrix4x4.CreateFromYawPitchRoll(attenuation * Math.Tau * r1, attenuation * Math.Tau * r1, attenuation * Math.Tau * r1) * Matrix4x4.CreateTranslation(r100, r100, r100);
                var origPoints = solid.Faces.Select(v => v.Center).ToList();
                var target = (TessellatedSolid)solid.TransformToNewSolid(transform);
                target.SolidColor = new Color(100, 10, 200, 200);
                Presenter.ShowAndHang(new[] { solid, target });
                var targetPoints = target.Faces.Select(v => v.Center).ToList();

                var matrix = TVGL.PointCloud.IterativeClosestPoint3D.Run(targetPoints, origPoints);

                var result = (TessellatedSolid)solid.TransformToNewSolid(matrix);
                var resultPoints = result.Faces.Select(v => v.Center).ToList();
                result.SolidColor = new Color(100, 200, 10, 200);
                Presenter.ShowAndHang(new[] { solid, target, result });

                var err = targetPoints.Zip(resultPoints, (t, r) => (t - r).LengthSquared()).Sum() / targetPoints.Count;
                Console.WriteLine("Error: " + err);
                index++;
            }
        }


    }
}
