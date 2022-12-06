using System;
using System.IO;
using System.Linq;
using TVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class TS_Testing_Functions
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
            //            brace.stl - holes showing up?
            // radiobox - missing holes - weird skip in outline
            // KnuckleTopOp flecks
            // mendel_extruder - one show up blank
            //var fileNames = dir.GetFiles("Obliq*").ToArray();
            var fileNames = dir.GetFiles("*").ToArray();
            for (var i = 7; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                if (Path.GetExtension(filename) != ".stl") continue;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var solid = (TessellatedSolid)IO.Open(filename);
                Presenter.ShowAndHang(solid);
                if (solid.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + solid.Errors.ToString());
                    continue;
                }
                if (name.Contains("yCastin")) continue;

                for (int j = 0; j < 3; j++)
                {
                    var direction = Vector3.UnitVector((CartesianDirections)j);
                    //var direction = new Vector3(r100, r100, r100);
                    Console.WriteLine(direction[0] + ", " + direction[1] + ", " + direction[2]);
                    //var silhouette = solid.CreateSilhouetteSimple(direction);
                    //Presenter.ShowAndHang(silhouette);
                    var polys = solid.GetCrossSection(new Plane(solid.Center, direction), out _);
                    //solid.SliceOnInfiniteFlat(new Plane(solid.Center, direction),out var solids, out var contactData);
                    Presenter.ShowAndHang(polys);
                    IO.Save(polys[0], "ring.json");
                }
            }

        }
    }
}