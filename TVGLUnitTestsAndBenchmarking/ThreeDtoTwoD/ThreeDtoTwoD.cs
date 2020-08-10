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
            var fileNames = dir.GetFiles("Gift Box.3mf");
            //var fileNames = dir.GetFiles("*").OrderBy(x => r.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                if (Path.GetExtension(filename) == ".off") continue;
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

                for (int j = 0; j < 1; j++)
                {
                    // holes?
                    // problem for castle
                    //var direction = new Vector3( -13.620881137261577, 23.309691028348027, -89.34802738453635);
                    //"KnuckleTopOp.stl"
                    //var direction = new Vector3(52.040874097515314, -32.66982181587714, -64.49028084263685);
                    // "Gift Box.3mf" 
                    var direction = new Vector3(88.92079973077438, 89.22131629158804, 19.697288758911796);
                    //ar direction = new Vector3(r100, r100, r100);
                    var silhouette = solid.CreateSilhouette(direction);
                    Presenter.ShowAndHang(silhouette);
                }
            }

        }
    }
}