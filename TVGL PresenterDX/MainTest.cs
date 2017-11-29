// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Voxelization;

namespace TVGLPresenterDX
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Windows.Media.Media3D;

    using HelixToolkit.Wpf.SharpDX;
    using Microsoft.Win32;
    using System.Windows.Input;
    using System.IO;
    using System.ComponentModel;

    public partial class MainViewModel : ObservableObject
    {

        public void Test()
        {

            //Difference2();
            var writer = new TextWriterTraceListener(Console.Out);
            Debug.Listeners.Add(writer);
            TVGL.Message.Verbosity = VerbosityLevels.OnlyCritical;
            var dir = new DirectoryInfo("../../../TestFiles");
            var fileNames = dir.GetFiles("*.stl");
            for (var i = 0; i < fileNames.Length; i++)
            {
                //var filename = FileNames[i];
                var filename = fileNames[i].FullName;
                // Console.WriteLine("Attempting: " + filename);
                Stream fileStream;
                List<TessellatedSolid> ts;
                if (!File.Exists(filename)) continue;
                using (fileStream = File.OpenRead(filename))
                    ts = IO.Open(fileStream, filename);
                //filename += "1.ply";
                //using (fileStream = File.OpenWrite(filename))
                //    IO.Save(fileStream, ts, FileType.PLY_Binary);
                //using (fileStream = File.OpenRead(filename))
                //    ts = IO.Open(fileStream, filename);


                //TestPolygon(ts[0]);
                //TestSegmentation(ts[0]);
                //Presenter.ShowAndHang(ts);
                TestVoxelization(ts[0]);
                //TestOctreeVoxelization(ts[0]);

                //TestSilhouette(ts[0]);
                //TestAdditiveVolumeEstimate(ts[0]);
            }

            Console.WriteLine("Completed.");

        }


        public void TestVoxelization(TessellatedSolid ts)
        {
            //var startTime = DateTime.Now;
            //var voxelSpace1 = new OldVoxelization.VoxelizedSolid(ts, 200, true);
            //var totalTime1 = DateTime.Now - startTime;
            //Console.WriteLine("{0}\t|  {1} verts  |  {2} ms  |  {3} voxels", ts.FileName, ts.NumberOfVertices,
            //    totalTime1.TotalSeconds,
            //    voxelSpace1.VoxelIDHashSet.Count);
            // Presenter.ShowAndHangVoxelization(ts, voxelSpace1);
            var startTime = DateTime.Now;
            var voxelSpace2 = new VoxelizedSolid(ts, VoxelDiscretization.Coarse);
            var totalTime2 = DateTime.Now - startTime;
            Console.WriteLine("{2}:{1}\t{0}", totalTime2.TotalSeconds, voxelSpace2.NumVoxelsTotal / 1000000.0, ts.FileName);
            //Present(ts, voxelSpace2);
            //Console.WriteLine("{0}\t{1}", totalTime1.TotalSeconds, totalTime2.TotalSeconds);
            // Presenter.ShowAndHangVoxelization(ts, voxelSpace2);
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