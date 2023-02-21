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
    public static class ProximityTests
    {
        static Random r = new Random();
        static double r10 => 20 * r.NextDouble() - 10;

        //[Fact]
        public static void TestClosestPointToLines()
        {
            for (int j = 0; j < 5; j++)
            {
                var lines = new List<(Vector2, Vector2)>();
                var lineSegments = new List<Vector2[]>();
                for (int i = 0; i < 10; i++)
                {
                    var startPoint = new Vector2(r10, r10);
                    var endPoint = new Vector2(r10, r10);
                    lineSegments.Add(new[] { startPoint, endPoint });
                    lines.Add((startPoint, (endPoint - startPoint).Normalize()));
                }
                var center = MiscFunctions.ClosestPointToLines(lines);
                lineSegments.Add(new[] { center, center });
                Presenter.ShowAndHang(lineSegments);
            }
        }
    }
}