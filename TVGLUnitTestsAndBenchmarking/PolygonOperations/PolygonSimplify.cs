using System;
#if !PRESENT
using Xunit;
using Snapshooter.Xunit;
using Snapshooter;
#endif

using TVGL;
using System.IO;
using System.Linq;

using System.Collections.Generic;

namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class Polygon_Testing_Functions
    {

        //[Fact]
        public static void TestSimplify(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("poly*.json").ToArray();
            for (var i = 0; i < fileNames.Length - 0; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon polygon);
                Presenter.ShowAndHang(polygon);
                var polygonSimple = polygon.SimplifyMinLengthToNewPolygon(100);
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