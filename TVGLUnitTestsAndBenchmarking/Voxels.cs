using Snapshooter;
using Snapshooter.Xunit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    public static class Voxels
    {
        public static void InitialTest()
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
            var random = new Random();
            var fileNames = dir.GetFiles("*ananas*").OrderBy(x => random.Next()).ToArray();
            //var fileNames = dir.GetFiles("*");
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                var ts = (TessellatedSolid)IO.Open(filename);
                if (ts.Errors != null)
                {
                    Console.WriteLine("    ===>" + filename + " has errors: " + ts.Errors.ToString());
                    continue;
                }
                ts.SolidColor = new Color(100, 200, 100, 50);
                Presenter.ShowAndHang(ts);
                var vs = new VoxelizedSolid(ts, 333);
                Presenter.ShowAndHang(vs);
                var extrudeSolid = vs.DraftToNewSolid(CartesianDirections.ZNegative);
                Presenter.ShowAndHang(extrudeSolid);
                var difference = extrudeSolid.SubtractToNewSolid(vs);
                Presenter.ShowAndHang(difference);

                //Snapshot.Match(vs, SnapshotNameExtension.Create(name));
            }
        }


    }
}
