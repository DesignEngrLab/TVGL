using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;


namespace TVGLUnitTestsAndBenchmarking
{
    public static class Voxels
    {
        public static void TestVoxelization(DirectoryInfo dir)
        {
            Presenter.NVEnable();
            var random = new Random();
            var fileNames = dir.GetFiles("*.*"); //.OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 1; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out TessellatedSolid ts);
                if (ts.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + ts.Errors.ToString());
                    continue;
                }
                //IO.Save(ts, dir + "/3_bananas");
                //Presenter.ShowAndHang(ts);
                var sw = Stopwatch.StartNew();
                Console.WriteLine("creating...");
                var vs = VoxelizedSolid.CreateFrom(ts, 80);
                vs.HasUniformColor = true;
                vs.SolidColor = new Color(KnownColors.Black);
                ts.SolidColor = new Color(100, 200, 100, 50);
                Console.WriteLine(sw.Elapsed.ToString());
                sw.Restart();
                Console.WriteLine("extruding...");
                //Presenter.ShowAndHang(vs.ConvertToTessellatedSolidRectilinear());
                //Presenter.ShowAndHang(new Solid[] { vs });
                //continue;
                var extrudeSolid = vs.DraftToNewSolid(CartesianDirections.YNegative);
                Console.WriteLine(sw.Elapsed.ToString());
                //Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidRectilinear());
                Console.WriteLine("subtracting...");
                sw.Restart();
                vs.SolidColor = new Color(100, 20, 20, 250);

                var erode = VoxelizedSolid.MinkowskiSubtractOne(vs);
                erode.SolidColor = new Color(100, 20, 20, 250);

                var erodeNew = VoxelizedSolid.MinkowskiSubtractOneNew(vs);

                Console.WriteLine(sw.Elapsed.ToString());
                erodeNew.HasUniformColor = true;
                erodeNew.SolidColor = new Color(200, 250, 20, 20);
                Presenter.ShowAndHang(new[] { erode.ConvertToTessellatedSolidRectilinear(), erodeNew.ConvertToTessellatedSolidRectilinear() });

                var block = VoxelizedSolid.CreateFullBlock(extrudeSolid);
                (block, var _) = block.SliceOnPlane(new Plane(2, new Vector3(1, 1, 1)));
                Presenter.ShowAndHang(block.ConvertToTessellatedSolidRectilinear());
                Console.WriteLine(sw.Elapsed.ToString());

                //Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidMarchingCubes(5));

                //Snapshot.Match(vs, SnapshotNameExtension.Create(name));
            }
        }


    }
}
