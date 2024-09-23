using System;
using System.IO;
using System.Linq;
using TVGL;



namespace TVGLUnitTestsAndBenchmarking
{
    public static partial class TVGL3Dto2DTests
    {
        public static void TestXSectionAndMonotoneTriangulate(DirectoryInfo dir)
        {
            var fileNames = dir.GetFiles("*.json").OrderByDescending(x => x.Length).ToArray();
            for (var i = 0; i < fileNames.Length; i++)
            {
                var filename = fileNames[i].FullName;
                var name = fileNames[i].Name;
                Console.WriteLine("Attempting: " + filename);
                IO.Open(filename, out Polygon polygon);
                Presenter.ShowAndHang(polygon);
                polygon = polygon.SimplifyMinLengthToNewPolygon(0.01);
                //polygon = polygon.RemoveSelfIntersections(ResultType.OnlyKeepPositive).LargestPolygon();
                TestTriangulate(polygon);
                Presenter.ShowAndHang(polygon);
            }
            Console.ReadKey();
        }

        public static void TestTriangulate(Polygon testcase)
        {
            if (testcase == null)
                testcase = new Polygon(TestCases.MakeStarryCircularPolygon(13, 10, 7));
            Presenter.ShowAndHang(testcase);
            var triangles = testcase.TriangulateToCoordinates();
            Presenter.ShowAndHang(triangles);
        }

    }
}