using System;
#if !PRESENT
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;
#endif
using TVGL.Numerics;
using TVGL;
using System.IO;
using TVGL.TwoDimensional;
using TVGL.IOFunctions;
using System.Linq;
using TVGL.Boolean_Operations;
using TVGL.Voxelization;
using System.Collections.Generic;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class Polygon_Testing_Functions
    {

        //[Fact]
        public static void TestSimplify(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("err*.json").ToArray();
            var polygons = new List<Polygon>();
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon polygon);
                polygons.Add(polygon);
            }
            Presenter.ShowAndHang(polygons);
            var polygonSimple = polygons.SimplifyMinLengthToNewPolygons(300).ToList();
            Presenter.ShowAndHang(polygonSimple);


        }



        internal static void TestSimplify2()
        {
            IEnumerable<Vector2> polygon = TestCases.MakeStarryCircularPolygon(150000, 30, 1);
            Presenter.ShowAndHang(polygon);
        }

    }
}