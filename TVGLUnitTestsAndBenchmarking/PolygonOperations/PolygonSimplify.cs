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
using System.Collections.Generic;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class Polygon_Testing_Functions
    {

        //[Fact]
        public static void TestSimplify()
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
            var fileNames = dir.GetFiles("poly*.json").ToArray();
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon polygon);
                Presenter.ShowAndHang(polygon);
                var polygonSimple = polygon.SimplifyMinLength(100);
                Presenter.ShowAndHang(new[] { polygon, polygonSimple });


            }

        }

        internal static void TestSimplify2()
        {
            IEnumerable<Vector2> polygon = TestCases.MakeStarryCircularPolygon(150000, 30, 1);
            Presenter.ShowAndHang(polygon);
        }

    }
}