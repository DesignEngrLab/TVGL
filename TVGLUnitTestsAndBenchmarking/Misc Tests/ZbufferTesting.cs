using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking.Misc_Tests
{
    internal static class ZbufferTesting
    {
        internal static void Test1()
        {
            DirectoryInfo dir = Program.BackoutToFolder(Program.inputFolder);
            foreach (var fileName in dir.GetFiles("*").Skip(0))
            {
                //Console.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid solid);
                if (solid == null) continue;
                //Presenter.ShowAndHang(solid);
                var direction = -Vector3.UnitZ;
                //var direction = new Vector3(1, 1, 1).Normalize();
                var (minD, maxD) = solid.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
                var displacement = (minD - maxD) * direction;
                //Console.Write("zbuffer start...");
                var sw = Stopwatch.StartNew();
                var zbuffer = ZBuffer.Run(solid, direction, 500);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.Ticks);
                //Console.WriteLine("end:  "+sw.Elapsed);
                //continue; 
                var paths = new List<List<Vector3>>();
                for (int i = 0; i < zbuffer.XCount; i++)
                {
                    var xLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.YCount; j++)
                        xLine.Add(displacement + zbuffer.Get3DPoint(i, j));
                    paths.Add(xLine);
                }
                for (int i = 0; i < zbuffer.YCount; i++)
                {
                    var yLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.XCount; j++)
                        yLine.Add(displacement + zbuffer.Get3DPoint(j, i));
                    paths.Add(yLine);
                }
                var colors = paths.Select(c => new Color(KnownColors.DodgerBlue));
                Presenter.ShowVertexPathsWithSolids(new[] { paths }, new[] { solid }, 1, colors);

            }
        }

        internal static void Test2()
        {
            DirectoryInfo dir = Program.BackoutToFolder(Program.inputFolder);
            foreach (var fileName in dir.GetFiles("*").Skip(0))
            {
                //Console.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid solid);
                if (solid == null) continue;
                //Presenter.ShowAndHang(solid);
                var direction = -Vector3.UnitZ;
                //var direction = new Vector3(1, 1, 1).Normalize();
                var (minD, maxD) = solid.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
                var displacement = (minD - maxD) * direction;
                //Console.Write("zbuffer start...");
                var sw = Stopwatch.StartNew();
                var zbuffer = CylindricalBuffer.Run(solid, direction, solid.Center, 500);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.Ticks);
                //Console.WriteLine("end:  "+sw.Elapsed);
                //continue; 
                var paths = new List<List<Vector3>>();
                for (int i = 0; i < zbuffer.XCount; i++)
                {
                    var xLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.YCount; j++)
                        xLine.Add(displacement + zbuffer.Get3DPoint(i, j));
                    paths.Add(xLine);
                }
                for (int i = 0; i < zbuffer.YCount; i++)
                {
                    var yLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.XCount; j++)
                        yLine.Add(displacement + zbuffer.Get3DPoint(j, i));
                    paths.Add(yLine);
                }
                var colors = paths.Select(c => new Color(KnownColors.DodgerBlue));
                Presenter.ShowVertexPathsWithSolids(new[] { paths }, new[] { solid }, 1, colors);

            }
        }

        internal static void Test3()
        {
            DirectoryInfo dir = Program.BackoutToFolder(Program.inputFolder);
            foreach (var fileName in dir.GetFiles("*").Skip(0))
            {
                //Console.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid solid);
                if (solid == null) continue;
                //Presenter.ShowAndHang(solid);
                var direction = -Vector3.UnitZ;
                //var direction = new Vector3(1, 1, 1).Normalize();
                var (minD, maxD) = solid.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
                var displacement = (minD - maxD) * direction;
                //Console.Write("zbuffer start...");
                var sw = Stopwatch.StartNew();
                var zbuffer = SphericalBuffer.Run(solid, solid.Center, 500);
                sw.Stop();
                Console.WriteLine(sw.Elapsed.Ticks);
                //Console.WriteLine("end:  "+sw.Elapsed);
                //continue; 
                var paths = new List<List<Vector3>>();
                for (int i = 0; i < zbuffer.XCount; i++)
                {
                    var xLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.YCount; j++)
                        xLine.Add(displacement + zbuffer.Get3DPoint(i, j));
                    paths.Add(xLine);
                }
                for (int i = 0; i < zbuffer.YCount; i++)
                {
                    var yLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.XCount; j++)
                        yLine.Add(displacement + zbuffer.Get3DPoint(j, i));
                    paths.Add(yLine);
                }
                var colors = paths.Select(c => new Color(KnownColors.DodgerBlue));
                Presenter.ShowVertexPathsWithSolids(new[] { paths }, new[] { solid }, 1, colors);

            }
        }

    }
}
