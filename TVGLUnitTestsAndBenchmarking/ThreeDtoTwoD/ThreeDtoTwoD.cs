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
            var fileNames = dir.GetFiles("*butoir*").OrderBy(x => r.Next()).ToArray();
            //var fileNames = dir.GetFiles("*").OrderBy(x => r.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                solid.SolidColor = new Color(100, 200, 100, 50);

                for (int j = 0; j < 33; j++)
                {
                    //var direction = new Vector3(53.57573896347347, -2.8016050824902976, -84.52266118699808);
                    var direction = new Vector3(r100, r100, r100);
                    var silhouette = solid.CreateSilhouette(direction);
                    Presenter.ShowAndHang(silhouette);
                }
            }

        }
    }
}