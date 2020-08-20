using System;
using Xunit;
using TVGL.Numerics;
using TVGL;
using System.IO;
using TVGL.TwoDimensional;
using TVGL.IOFunctions;
using Snapshooter.Xunit;
using Snapshooter;
using System.Linq;
using TVGL.Boolean_Operations;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    public static class TVGL3Dto2DTests
    {
        static Random r = new Random();
        static double r100 => 200.0 * r.NextDouble() - 100.0;

        //[Fact]
        public static void TestSilhouette()
        {
            DirectoryInfo dir;
            if (Directory.Exists("../../../../TestFiles"))
            {
                //x64
                dir = new DirectoryInfo("../../../../TestFiles");
            }
            else
            {
                //x86
                dir = new DirectoryInfo("../../../TestFiles");
            }
            var fileNames = dir.GetFiles("Castle*").ToArray();
            //var fileNames = dir.GetFiles("*").OrderBy(x => r.Next()).ToArray();
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                if (Path.GetExtension(filename) == ".off") continue;
                if (Path.GetExtension(filename) == ".ply") continue;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }

                for (int j = 0; j < 2; j++)
                {
                    var direction = new Vector3(-26.69179911105512, -8.905440433372476, -10.751895844355175);
                    //var direction = new Vector3(r100, r100, r100);
                    Console.WriteLine(direction[0] + ", " + direction[1] + ", " + direction[2]);
                    var silhouette = solid.CreateSilhouette(direction);
                    Presenter.ShowAndHang(silhouette);
                }
            }

        }
    }
}