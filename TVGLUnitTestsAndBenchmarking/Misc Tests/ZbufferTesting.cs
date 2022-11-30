using BenchmarkDotNet.ConsoleArguments.ListBenchmarks;
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
            DirectoryInfo dir = Program.BackOutToFolder();

            foreach (var fileName in dir.GetFiles("*").Skip(1))
            {
                Debug.WriteLine("\n\n\nAttempting to open: " + fileName.Name);
                IO.Open(fileName.FullName, out TessellatedSolid solid);
                //Presenter.ShowAndHang(solid);
                var direction = Vector3.UnitZ;
                //var direction = new Vector3(1,1,1).Normalize();
                var (shift,_) = solid.Vertices.GetDistanceToExtremeVertex(direction, out _, out _);
                var displacement = shift * direction;
                var zbuffer = new ZBuffer(solid, direction, 100, 0);
                zbuffer.Run();
                var paths = new List<List<Vector3>>();
                for (int i = 0; i < zbuffer.XCount; i++)
                {
                    var xLine = new List<Vector3>();
                    for (int j = 0; j < zbuffer.YCount; j++)
                        xLine.Add(displacement + zbuffer.Get3DPoint(i, j));
                    paths.Add(xLine);
                }
                Presenter.ShowVertexPathsWithSolids(new[] { paths }, new[] { solid }, 1);

            }
        }

    }
}
