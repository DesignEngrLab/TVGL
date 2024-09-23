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
using System.Diagnostics;

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
            var t = polygons.Sum(p => p.AllPolygons.Sum(pp => pp.Edges.Count));
            polygons.SimplifyMinLength(300);
            Presenter.ShowAndHang(polygons);
            t = polygons.Sum(p => p.AllPolygons.Sum(pp => pp.Edges.Count));

        }



        internal static void TestSimplify2()
        {
            var polygon = new Polygon(TestCases.MakeStarryCircularPolygon(15000, 30, 1));
            Console.WriteLine(polygon.Area);
            Console.WriteLine(polygon.Perimeter);
            Console.WriteLine(polygon.Edges.Max(edge => edge.Length));
            Console.WriteLine(polygon.Edges.Min(edge => edge.Length));
            //Presenter.ShowAndHang(polygon);
            var sw = Stopwatch.StartNew();
            var simp = polygon.SimplifyByAreaChangeToNewPolygon(0.1);
            Console.WriteLine("time elapsed =" + sw.Elapsed.ToString());
            Presenter.ShowAndHang(new[] { polygon, simp });
            Console.WriteLine(simp.Area);
            Console.WriteLine(simp.Perimeter);
            Console.WriteLine(simp.Edges.Max(edge => edge.Length));
            Console.WriteLine(simp.Edges.Min(edge => edge.Length));
        }
    }
}