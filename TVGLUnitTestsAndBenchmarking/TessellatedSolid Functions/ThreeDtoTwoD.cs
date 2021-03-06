﻿using System;
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
            var fileNames = dir.GetFiles("*").Reverse().ToArray();
            for (var i = 10; i < fileNames.Length - 0; i++)
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
                    solid.SliceOnInfiniteFlat(new Plane(solid.Center, direction),out var solids, out var contactData);
                    Presenter.ShowAndHang(solids);
                }
            }

        }
    }
}