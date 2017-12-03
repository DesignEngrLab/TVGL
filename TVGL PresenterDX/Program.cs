using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StarMathLib;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Voxelization;


namespace TVGLPresenterDX
{
    internal class Program
    {
        private static readonly string[] FileNames = {
           "../../../TestFiles/Binary.stl",
            "../../../TestFiles/ABF.ply",
             "../../../TestFiles/Beam_Boss.STL",
             "../../../TestFiles/Beam_Clean.STL",

        "../../../TestFiles/bigmotor.amf",
        "../../../TestFiles/DxTopLevelPart2.shell",
        "../../../TestFiles/Candy.shell",
        "../../../TestFiles/amf_Cube.amf",
        "../../../TestFiles/train.3mf",
        "../../../TestFiles/Castle.3mf",
        "../../../TestFiles/Raspberry Pi Case.3mf",
       //"../../../TestFiles/shark.ply",
       "../../../TestFiles/bunnySmall.ply",
        "../../../TestFiles/cube.ply",
        "../../../TestFiles/airplane.ply",
        "../../../TestFiles/TXT - G5 support de carrosserie-1.STL.ply",
        "../../../TestFiles/Tetrahedron.STL",
        "../../../TestFiles/off_axis_box.STL",
           "../../../TestFiles/Wedge.STL",
        "../../../TestFiles/Mic_Holder_SW.stl",
        "../../../TestFiles/Mic_Holder_JR.stl",
        "../../../TestFiles/3_bananas.amf",
        "../../../TestFiles/drillparts.amf",  //Edge/face relationship contains errors
        "../../../TestFiles/wrenchsns.amf", //convex hull edge contains a concave edge outside of tolerance
        "../../../TestFiles/hdodec.off",
        "../../../TestFiles/tref.off",
        "../../../TestFiles/mushroom.off",
        "../../../TestFiles/vertcube.off",
        "../../../TestFiles/trapezoid.4d.off",
        "../../../TestFiles/ABF.STL",
        "../../../TestFiles/Pump-1repair.STL",
        "../../../TestFiles/Pump-1.STL",
        "../../../TestFiles/SquareSupportWithAdditionsForSegmentationTesting.STL",
        "../../../TestFiles/Beam_Clean.STL",
        "../../../TestFiles/Square_Support.STL",
        "../../../TestFiles/Aerospace_Beam.STL",
        "../../../TestFiles/Rook.amf",
       "../../../TestFiles/bunny.ply",

        "../../../TestFiles/piston.stl",
        "../../../TestFiles/Z682.stl",
        "../../../TestFiles/sth2.stl",
        "../../../TestFiles/Cuboide.stl", //Note that this is an assembly 
        "../../../TestFiles/new/5.STL",
       "../../../TestFiles/new/2.stl", //Note that this is an assembly 
        "../../../TestFiles/new/6.stl", //Note that this is an assembly  //breaks in slice at 1/2 y direction
       "../../../TestFiles/new/4.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/radiobox.stl",
        "../../../TestFiles/brace.stl",  //Convex hull fails in MIconvexHull
        "../../../TestFiles/G0.stl",
        "../../../TestFiles/GKJ0.stl",
        "../../../TestFiles/testblock2.stl",
        "../../../TestFiles/Z665.stl",
        "../../../TestFiles/Casing.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/mendel_extruder.stl",

       "../../../TestFiles/MV-Test files/holding-device.STL",
       "../../../TestFiles/MV-Test files/gear.STL"
        };

        [STAThread]
        private static void Main(string[] args)
        {
            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            var dir = new DirectoryInfo("../../../TestFiles");
            var fileNames = dir.GetFiles("*.stl");
            for (var i = 0; i < fileNames.Count(); i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                // Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                List<TessellatedSolid> ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    ts = IO.Open(fileStream, filename);
               // PresenterShowAndHang(ts);
                TestVoxelization(ts[0]);
            }

            Console.WriteLine("Completed.");
        }


        private static void PresenterShowAndHang(List<TessellatedSolid> ts)
        {
            PresenterShowAndHang(ts.Cast<Solid>().ToList());
        }
        private static void PresenterShowAndHang(IList<Solid> ts)
        {
            var mainWindow = new MainWindow
            {
                Solids = ts.ToList()
            };
            mainWindow.ShowDialog();
        }




        public static void TestVoxelization(TessellatedSolid ts)
        {
            var startTime = DateTime.Now;
            //var voxelSpace1 = new VoxelizedSolid(ts, 200, true);
            //var totalTime1 = DateTime.Now - startTime;
            //Console.WriteLine("{0}\t|  {1} verts  |  {2} ms  |  {3} voxels", ts.FileName, ts.NumberOfVertices,
            //    totalTime1.TotalSeconds,
            //    voxelSpace1.VoxelIDHashSet.Count);
            // Presenter.ShowAndHangVoxelization(ts, voxelSpace1);
            //startTime = DateTime.Now;
            var voxelSpace2 = new VoxelizedSolid(ts, VoxelDiscretization.Coarse);
            var totalTime2 = DateTime.Now - startTime;
            Console.WriteLine("tsvol:{0}\tvol:{1}\ttime{2}",ts.Volume, voxelSpace2.Volume, totalTime2.TotalSeconds);
            //Console.WriteLine("{0}\t{1}", totalTime1.TotalSeconds, totalTime2.TotalSeconds);
           //  PresenterShowAndHang(new Solid[] { ts, voxelSpace2 });
            //return;
            //startTime = DateTime.Now;
            //var voxelSpace2 = new VoxelSpace();
            //voxelSpace2.VoxelizeSolid(ts, 100);
            //var totalTime2 = DateTime.Now - startTime;
            //Console.WriteLine("{0}\t\t|  {1} verts  |  {2} ms  |  {3} voxels", ts.FileName, ts.NumberOfVertices,
            //    totalTime2.TotalSeconds,
            //    voxelSpace2.Voxels.Count);
            //Presenter.ShowAndHangVoxelization(ts, voxelSpace2);

            //int counter1 = 0;
            //foreach (var key in voxelSpace1.VoxelIDHashSet)
            //{
            //    if (voxelSpace2.VoxelIDHashSet.Contains(key))
            //    {
            //        voxelSpace1.Voxels.Remove(key);
            //    }
            //    else counter1++;
            //}
            //Console.WriteLine(counter1+" voxels in new that are not in old.");
            // Presenter.ShowAndHangVoxelization(ts, voxelSpace1);
            /*
             Voxel voxelOfInterest = null;
            int maxYalongMinZ = Int32.MinValue;
            int minZ = Int32.MaxValue;
            foreach (var voxel in voxelSpace1.Voxels)
            {
                if (voxel.Value.Index[2] < minZ)
                {
                    minZ = voxel.Value.Index[2];
                    maxYalongMinZ = voxel.Value.Index[1];
                }
                else if (voxel.Value.Index[2] == minZ)
                {
                    if (voxel.Value.Index[1] > maxYalongMinZ)
                    {
                        maxYalongMinZ = voxel.Value.Index[1];
                        voxelOfInterest = voxel.Value;
                    }
                }
            }
            Presenter.ShowAndHangVoxelization(ts, new List<Voxel>() {voxelOfInterest});

            var counter2 = 0;
            foreach (var key in voxelSpace2.VoxelIDHashSet)
            {
                if (voxelSpace1.VoxelIDHashSet.Contains(key))
                {
                    voxelSpace2.Voxels.Remove(key);
                }
                else counter2++;
            }
            if (counter2 > 0)
            {
                Console.WriteLine(counter2 + " voxels in old that are not in new.");
                Presenter.ShowAndHangVoxelization(ts, voxelSpace2);
            }*/
        }
    }
}