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
    public  static partial class TS_Testing_Functions
    {

        //[Fact]
        public static void TestClassify()
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
            //            brace.stl - holes showing up?
            // radiobox - missing holes - weird skip in outline
            // KnuckleTopOp flecks
            // mendel_extruder - one show up blank
            //var fileNames = dir.GetFiles("Obliq*").ToArray();
            var fileNames = dir.GetFiles("*").ToArray();
            for (var i = 7; i < fileNames.Length - 0; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                //Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                   // continue;
                }
                solid.ClassifyPrimitiveSurfaces();
                //solid.SetToOriginAndSquare(out _);
                 //   Presenter.ShowAndHang(solid);  
                //solid.Simplify(0.3 * solid.NumberOfFaces);
              

            }

        }
    }
}