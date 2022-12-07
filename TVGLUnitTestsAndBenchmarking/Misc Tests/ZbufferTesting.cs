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
        internal static void Test()
        {
            DirectoryInfo dir = Program.BackoutToFolder("TestFiles");

            foreach (var fileName in dir.GetFiles("*").Skip(0))
            {
                //Console.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid solid);
                //Presenter.ShowAndHang(solid);
                var direction = -Vector3.UnitY;
                //var direction = new Vector3(1, 1, 1).Normalize();
                var (minD,maxD) = solid.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
                var displacement = (minD-maxD) * direction;
                //Console.Write("zbuffer start...");
                var sw = Stopwatch.StartNew();
                var zbuffer = new ZBuffer(solid, direction, 500);
                zbuffer.Run();
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
                        yLine.Add(displacement + zbuffer.Get3DPoint(j,i));
                    paths.Add(yLine);
                }
                var colors = paths.Select(c =>new Color(KnownColors.DodgerBlue));
                Presenter.ShowVertexPathsWithSolids(new[] { paths }, new[] { solid }, 1, colors);

            }
        }

    }
}
