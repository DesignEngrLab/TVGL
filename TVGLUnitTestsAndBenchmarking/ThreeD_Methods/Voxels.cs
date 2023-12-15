using System;
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
            var fileNames = dir.GetFiles("*.*").OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 0; i < fileNames.Length - 0; i++)
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
                ts.SolidColor = new Color(100, 200, 100, 50);
                //Presenter.ShowAndHang(ts);
                var vs = new VoxelizedSolid(ts, 800);
                Console.WriteLine("presenting...");
                //Presenter.ShowAndHang(vs.ConvertToTessellatedSolidMarchingCubes(250));
                Presenter.ShowAndHang(new Solid[] { vs, ts });
                //continue;
                var extrudeSolid = vs.DraftToNewSolid(CartesianDirections.XNegative);
                Presenter.ShowAndHang(extrudeSolid);
                extrudeSolid.Subtract(vs);
                Presenter.ShowAndHang(extrudeSolid);
                Presenter.ShowAndHang(extrudeSolid.ConvertToTessellatedSolidMarchingCubes(50));

                //Snapshot.Match(vs, SnapshotNameExtension.Create(name));
            }
        }


    }
}
