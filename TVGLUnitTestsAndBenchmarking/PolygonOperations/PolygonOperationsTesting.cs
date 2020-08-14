using System;
using Xunit;
using TVGL.Numerics;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using BenchmarkDotNet.Attributes;
using TVGL.TwoDimensional;
using System.Linq;
using TVGL;
//using OldTVGL;

namespace TVGLUnitTestsAndBenchmarking
{
    public class PolygonOperationsTesting
    {
        static Random r = new Random(2);

        static double rand(double radius)
        {
            return 2.0 * radius * r.NextDouble() - radius;
        }
        static int rand(int radius)
        {
            return (int)r.Next(radius);
        }

        internal static IEnumerable<Vector2> MakeCircularPolygon(int numSides, double radius)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                yield return new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle));
            }
        }
        internal static IEnumerable<Vector2> MakeOctogonPolygon(double xMin, double yMin, double xMax, double yMax, double cornerClip)
        {
            yield return new Vector2(xMin + cornerClip, yMin);
            yield return new Vector2(xMax - cornerClip, yMin);
            yield return new Vector2(xMax, yMin + cornerClip);
            yield return new Vector2(xMax, yMax - cornerClip);
            yield return new Vector2(xMax - cornerClip, yMax);
            yield return new Vector2(xMin + cornerClip, yMax);
            yield return new Vector2(xMin, yMax - cornerClip);
            yield return new Vector2(xMin, yMin + cornerClip);
        }

        internal static IEnumerable<Vector2> MakeStarryCircularPolygon(int numSides, double radius, double delta)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                var thisRadius = radius + 2 * delta * r.NextDouble() - delta;
                yield return new Vector2(thisRadius * Math.Cos(angle), thisRadius * Math.Sin(angle));
            }
        }

        internal static IEnumerable<Vector2> MakeRandomComplexPolygon(int numSides, double boxRadius)
        {
            for (int i = 0; i < numSides; i++)
            {
                yield return new Vector2(2 * boxRadius * r.NextDouble() - boxRadius, 2 * boxRadius * r.NextDouble() - boxRadius);
            }
        }

        internal static IEnumerable<Vector2> MakeWavyCircularPolygon(int numSides, double radius, double delta, double frequency)
        {
            var angleIncrement = 2 * Math.PI / numSides;

            for (int i = 0; i < numSides; i++)
            {
                var angle = i * angleIncrement;
                yield return new Vector2(radius * Math.Cos(angle) + delta * Math.Cos(frequency * angle),
                    radius * Math.Sin(angle) + delta * Math.Sin(frequency * angle));
            }
        }

        internal static void TestRemoveSelfIntersect()
        {
            for (int i = 0; i < 200; i++)
            {
                r = new Random(i);
                Console.WriteLine(i);
                var coords = MakeWavyCircularPolygon(rand(500), rand(30), rand(300), rand(15.0)).ToList();
                //var coords = MakeRandomComplexPolygon(10, 30).ToList();
                Presenter.ShowAndHang(coords);
                var polygon = new Polygon(coords);
                var polygons = polygon.RemoveSelfIntersections(true, out _);
                Presenter.ShowAndHang(polygons);
                //Presenter.ShowAndHang(polygons.Path);
                //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
            }
        }
        internal static void OctagonTest()
        {
            //for (int i = 6; i < 200; i++)
            //{
            //r = new Random(i);
            //    Console.WriteLine(i);
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    for (int k = 0; k < 4; k++)
                    {
                        for (int m = 0; m < 4; m++)
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                var coords1 = MakeOctogonPolygon(0, 0, i, j, k).ToList();
                                var coords2 = MakeOctogonPolygon(3, 3, m, m, n).ToList();

                                //var hole1 = MakeStarryCircularPolygon(8, 15, 3).ToList();
                                //hole1.Reverse();
                                //var coords2 = MakeWavyCircularPolygon(10000, 30, 10, 5).ToList();
                                //var coords2 = new[] { new Vector2(5, 40), new Vector2(-5, 40), new Vector2(-5, -40), new Vector2(5, -40) };
                                var stopWatch = new Stopwatch();
                                stopWatch.Restart();
                                var polygon1 = new Polygon(coords1);
                                // polygon1.RemoveSelfIntersections();
                                // polygon1 = polygon1.Union(new Polygon(hole1, false))[0];
                                //Presenter.ShowAndHang(polygon1);
                                var polygon2 = new Polygon(coords2);

                                Presenter.ShowAndHang(new[] { polygon1, polygon2 });

                                PolygonInteractionRecord a = polygon1.GetShallowPolygonTreeRelationshipAndIntersections(polygon2);
                                
                                var polygon3 = polygon1.Union(polygon2, a);
                                Presenter.ShowAndHang(polygon3);

                                polygon3 = polygon2.Intersect(polygon1, a);
                                Presenter.ShowAndHang(polygon3);

                                polygon3 = polygon1.Subtract(polygon2, a);
                                Presenter.ShowAndHang(polygon3);

                                polygon3 = polygon2.Subtract(polygon1, a);
                                Presenter.ShowAndHang(polygon3);

                                polygon3 = polygon2.ExclusiveOr(polygon1, a);
                                Presenter.ShowAndHang(polygon3);
                                //stopWatch.Restart();
                                //var coords3 = PolygonOperations.Union(coords1, coords2);
                                //Console.Write(stopWatch.Elapsed);
                                //Presenter.ShowAndHang(new[] { coords3[0], polygon3[0].Path });
                                //polygon3 = polygon1.Intersect(polygon3[0]);
                                //Presenter.ShowAndHang(new[] { coords1, coords2, polygon3[0].Path });
                                //polygon3 = polygon1.ExclusiveOr(polygon2);
                                //Presenter.ShowAndHang(polygon3.Select(p => p.Path));
                                //polygon3 = polygon1.Subtract(polygon2,a, intersections);
                                //Presenter.ShowAndHang(polygon3.Select(p => p.Path));
                                //polygon3 = polygon2.Subtract(polygon1,a, intersections);
                                //Presenter.ShowAndHang(polygon3.Select(p => p.Path));
                                //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
                                //}
                            }
                        }
                    }
                }
            }

            Console.ReadKey();
        }

        internal static Dictionary<string, (Vector2[][], Vector2[][])> edgeCaseDictionary =
            new Dictionary<string, (Vector2[][], Vector2[][])>
            {
                { "trapInTri", (
                    new [] { new [] { new Vector2(0,0), new Vector2(10,0), new Vector2(0,10) } },
                    new [] { new [] { new Vector2(0,0), new Vector2(4,0), new Vector2(6,4), new Vector2(3,7) } })},
                { "innerTouch", (
                    new [] { new [] { new Vector2(0,0), new Vector2(7,0), new Vector2(4,2.5), new Vector2(3,4),
                        new Vector2(1,6), new Vector2(3,7), new Vector2(0,10) } },
                    new [] { new [] { new Vector2(2,7), new Vector2(1,6), new Vector2(2,2), new Vector2(5,1),
                        new Vector2(3,4), new Vector2(2,5) } })},
                { "nestedSquares", (
                    new [] { new [] { new Vector2(0,0), new Vector2(10,0), new Vector2(10,10), new Vector2(0,10) },
                        new [] { new Vector2(2, 2), new Vector2(2, 8), new Vector2(8, 8), new Vector2(8, 2) } },
                    new [] { new [] { new Vector2(1,1), new Vector2(9,1), new Vector2(9, 9), new Vector2(1, 9) },
                        new [] { new Vector2(3, 3), new Vector2(3, 7), new Vector2(7, 7), new Vector2(7, 3) } })},
            };

        internal static void DebugEdgeCases()
        {
            foreach (var key in edgeCaseDictionary.Keys)
                DebugEdgeCases(key);
        }
        internal static void DebugEdgeCases(string name)
        {
            var coords1 = edgeCaseDictionary[name].Item1;
            var polygon1 = new Polygon(coords1[0]);
            for (int i = 1; i < coords1.Length; i++)
                polygon1.AddHole(new Polygon(coords1[i]));
            var coords2 = edgeCaseDictionary[name].Item2;
            var polygon2 = new Polygon(coords2[0]);
            for (int i = 1; i < coords2.Length; i++)
                polygon2.AddHole(new Polygon(coords2[i]));

            // polygon1.RemoveSelfIntersections();
            //Presenter.ShowAndHang(polygon1);

            DebugEdgeCases(polygon1, polygon2);

            Console.ReadKey();
        }

        private static void DebugEdgeCases(IEnumerable<Vector2> coordinates1, IEnumerable<Vector2> coordinates2)
        {
            DebugEdgeCases(new Polygon(coordinates1), new Polygon(coordinates2));
        }

        private static void DebugEdgeCases(Polygon polygon1, Polygon polygon2)
        {
            Presenter.ShowAndHang(new[] { polygon1, polygon2 });

            var a = polygon1.GetShallowPolygonTreeRelationshipAndIntersections(polygon2);

            var polygon3 = polygon1.Union(polygon2, a);
                Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.Subtract(polygon2, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon2.Subtract(polygon1, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.ExclusiveOr(polygon2, a);
            Presenter.ShowAndHang(polygon3);

            polygon3 = polygon1.Intersect(polygon2, a);
            Presenter.ShowAndHang(polygon3);
        }


        internal static void DebugOctagons()
        {
            int k = 0;
            for (int leftCut = 1; leftCut <= 4; leftCut++)
            {
                for (int leftWidth = 5 - leftCut; leftWidth < 11 - 2 * leftCut; leftWidth++)
                {
                    for (int leftHeight = 5 - leftCut; leftHeight < 11 - 2 * leftCut; leftHeight++)
                    {
                        for (int rightCut = 1; rightCut <= 4; rightCut++)
                        {
                            for (int rightWidth = 5 - rightCut; rightWidth < 11 - 2 * rightCut; rightWidth++)
                            {
                                for (int rightHeight = 5 - rightCut; rightHeight < 11 - 2 * rightCut; rightHeight++)
                                {
                                    if (k % 100000 == 0)
                                    {
                                        DebugEdgeCases(MakeOctogonPolygon(0,0,2*leftCut+leftWidth,2*leftCut+leftHeight,leftCut),
                                            MakeOctogonPolygon(9-(2*rightCut+rightWidth),9-(2*rightCut+rightHeight),9,9,rightCut));
                                    }
                                    Console.WriteLine(k++);
                                }
                            }
                        }
                    }
                }
            }
        }


        //internal static void TestBooleanCompare()
        //{
        //    var stopwatch = new Stopwatch();
        //    for (int i = 4; i < 10000; i *= 2)
        //    {
        //        Console.WriteLine(i + " sides");
        //        var coords1 = MakeStarryCircularPolygon(i, 30, 1).ToList();
        //        var hole1 = MakeStarryCircularPolygon(i, 21, 1).Reverse().ToList();
        //        var coords2 = MakeStarryCircularPolygon(i, 30, 1).ToList();
        //        for (int j = 0; j < coords2.Count; j++)
        //            coords2[j] += new Vector2(15, 10);
        //        var hole2 = MakeStarryCircularPolygon(i, 21, 1).Reverse().ToList();
        //        for (int j = 0; j < hole2.Count; j++)
        //            hole2[j] += new Vector2(15, 11);
        //        //polygon1 = new Polygon(coords1, true);
        //        //polygon2 = new Polygon(coords2, true);

        //        var polygon1 = new Polygon(coords1);
        //        polygon1.AddHole(new Polygon(hole1));
        //        var polygon2 = new Polygon(coords2);
        //        polygon2.AddHole(new Polygon(hole2));
        //        //polygon2.Transform(Matrix3x3.CreateTranslation(15, 15));
        //        var clipperPoly1 = new[] {
        //            coords1.Select(v=>new OldTVGL.PointLight(v.X, v.Y)).ToList(),
        //            hole1.Select(v=>new OldTVGL.PointLight(v.X, v.Y)).ToList()
        //        };
        //        var clipperPoly2 = new[] {
        //            coords2.Select(v=>new OldTVGL.PointLight(v.X, v.Y)).ToList(),
        //            hole2.Select(v=>new OldTVGL.PointLight(v.X, v.Y)).ToList()
        //        };
        //        //Presenter.ShowAndHang(new[] { polygon1, polygon2 });

        //        stopwatch.Restart();

        //        var polygon3 = polygon1.Union(polygon2);
        //        Console.WriteLine("union mine: {0}", stopwatch.Elapsed);
        //        stopwatch.Restart();
        //        var coords3 = OldTVGL.PolygonOperations.Union(clipperPoly1, clipperPoly2);
        //        Console.WriteLine("union clipper: {0}", stopwatch.Elapsed);
        //        ShowComparison(i, "union", polygon3, coords3);
        //        stopwatch.Restart();
        //        polygon3 = polygon1.Intersect(polygon2);
        //        Console.WriteLine("interset mine: {0}", stopwatch.Elapsed);
        //        stopwatch.Restart();
        //        coords3 = OldTVGL.PolygonOperations.Intersection(clipperPoly1, clipperPoly2);
        //        Console.WriteLine("intersect clipper: {0}", stopwatch.Elapsed);
        //        ShowComparison(i, "intersect", polygon3, coords3);
        //        //polygon3 = polygon1.ExclusiveOr(polygon2);
        //        //coords3 = clipperPoly1.Xor(clipperPoly2);
        //        //ShowComparison(i, "exclusive or", polygon3, coords3);
        //        stopwatch.Restart();
        //        polygon3 = polygon1.Subtract(polygon2);
        //        Console.WriteLine("subtract mine: {0}", stopwatch.Elapsed);
        //        stopwatch.Restart();
        //        coords3 = OldTVGL.PolygonOperations.Difference(clipperPoly1, clipperPoly2);
        //        Console.WriteLine("subtract clipper: {0}", stopwatch.Elapsed);
        //        ShowComparison(i, "1subtract2", polygon3, coords3);
        //        stopwatch.Restart();
        //        polygon3 = polygon2.Subtract(polygon1);
        //        Console.WriteLine("subtract mine: {0}", stopwatch.Elapsed);
        //        stopwatch.Restart();
        //        coords3 = OldTVGL.PolygonOperations.Difference(clipperPoly2, clipperPoly1);
        //        Console.WriteLine("subtract clipper: {0}", stopwatch.Elapsed);
        //        ShowComparison(i, "2subtract1", polygon3, coords3);


        //    }
        //}

        //private static void ShowComparison(int trial, string name, List<Polygon> polygon3, List<List<OldTVGL.PointLight>> pointLights)
        //{
        //    var coords3 = new List<List<Vector2>>(pointLights.Select(listOfPoints
        //        => listOfPoints.Select(v => new Vector2(v.X, v.Y)).ToList()));
        //    var area = coords3.Sum(poly => poly.Area());
        //    var peri = coords3.Perimeter();
        //    var numPolygons = polygon3.Sum(poly => poly.AllPolygons.Count());
        //    var numPolyVerts = polygon3.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
        //    if (numPolygons == coords3.Count
        //        && numPolyVerts == coords3.Sum(loop => loop.Count)
        //        && area.IsPracticallySame(polygon3.Sum(p => p.Area), 1e-3)
        //        && peri.IsPracticallySame(polygon3.Sum(p => p.Perimeter), 1e-3))
        //        Console.WriteLine("*****{0}: {1} matches", trial, name);
        //    else
        //    {
        //        Console.WriteLine("{0}: {1} does not match", trial, name);
        //        Console.WriteLine("number: {0}  : {1} ", numPolygons, coords3.Count);
        //        Console.WriteLine("verts: {0}  : {1} ", numPolyVerts, coords3.Sum(loop => loop.Count));
        //        Console.WriteLine("area: {0}  : {1} ", polygon3.Sum(p => p.Area), area);
        //        Console.WriteLine("perimeter: {0}  : {1} ", polygon3.Sum(p => p.Perimeter), peri);

        //        Presenter.ShowAndHang(polygon3);
        //        Presenter.ShowAndHang(coords3);
        //        var polyAsCoords = polygon3.SelectMany(polygon => polygon.AllPolygons.Select(p => p.Path)).ToList();
        //        polyAsCoords.AddRange(coords3);
        //        Presenter.ShowAndHang(polyAsCoords);
        //        Console.WriteLine();
        //        Console.WriteLine();
        //    }
        //}

        [Params(10, 30, 100, 300, 1000, 3000, 5000)]
        public int N;

        private List<Vector2> coords1;
        private List<Vector2> coords2;
        private List<List<Vector2>> coords3;
        private Polygon polygon1;
        private Polygon polygon2;

        [GlobalSetup]
        public void Setup()
        {
            coords1 = MakeStarryCircularPolygon(N, 30, 15).ToList();
            coords2 = MakeWavyCircularPolygon(60, 25, 3, 8).Select(p => p + new Vector2(15, 10)).ToList();
            //coords1 = coords1.Xor(coords2)[0];
            polygon1 = new Polygon(coords1);
            //polygon1 = new Polygon(coords1, true);
            //polygon2 = new Polygon(coords2, true);
        }


        // [Benchmark(Description = "my functions")]
        public void BenchmarkMyBooleanSimple()
        {
            var polygon3 = polygon1.OffsetRound(5.0, 0.005);
            //polygon1 = new Polygon(coords1, true);
            //polygon2 = new Polygon(coords2, true);
            //var polygon3 = polygon1.Union(polygon2);
            //polygon3 = polygon1.Intersect(polygon2);
            //polygon3 = polygon1.ExclusiveOr(polygon2);
            //polygon3 = polygon1.Subtract(polygon2);
        }


        //[Benchmark(Description = "clipper")]
        public void BenchmarkClipperSimple()
        {
            //var coords4 = coords1.OffsetRound(5.0, 0.005);
            //var coords3 = PolygonOperations.Union(coords1, coords2);
            //coords3 = PolygonOperations.Intersection(coords1, coords2);
            //coords3 = coords1.Difference(coords2);
            //coords3 = coords1.Xor(coords2);
        }

        internal static void TestSimplify()
        {
            IEnumerable<Vector2> polygon = MakeStarryCircularPolygon(150000, 30, 1);
            //Presenter.ShowAndHang(polygon);
            polygon = polygon.Simplify(20)[0];
            Presenter.ShowAndHang(polygon);
        }

        /*
        [Benchmark(Description = "from Ienumerable")]
        [Arguments(10, 4)]
        [Arguments(20, 4)]
        [Arguments(10, 4000)]
        [Arguments(20, 10000)]
        [Theory]
        [InlineData(10, 4)]
        [InlineData(20, 4)]
        [InlineData(10, 4000)]
        [InlineData(2, 10000)]
        public void Perimeter(double radius, int numSides)
        {
            var perimeter = 2 * radius * numSides * Math.Sin(Math.PI / numSides);

            var polygon = MakeCircularPolygon(numSides, radius);
            Assert.Equal(perimeter, polygon.Perimeter(), 10);
        }



        [Benchmark(Description = "from Ienumerable")]
        [Arguments(10, 4)]
        [Arguments(20, 4)]
        [Arguments(10, 4000)]
        [Arguments(20, 10000)]
        [Theory]
        [InlineData(10, 4)]
        [InlineData(20, 4)]
        [InlineData(10, 4000)]
        [InlineData(2, 10000)]
        public void Area(double radius, int numSides)
        {
            var area = 0.5 * radius * radius * numSides * Math.Sin(2 * Math.PI / numSides);

            var polygon = MakeCircularPolygon(numSides, radius);
            Assert.Equal(area, polygon.Area(), 10);
        }
        */
        public static void TestBoundingRectangle()
        {
            //var points = new[] {
            //    new Vector2(2.26970768, 4.28080463),
            //    new Vector2(5.84034252, 0),
            //    new Vector2(12.22331619, 2.24806976),
            //    new Vector2(23.56225014, 21.88767815),
            //    new Vector2(19.9916172, 26.16848373),
            //    new Vector2(13.60864258, 23.92041588)
            //};
            //var points = new[] {
            //    new Vector2(6,1),
            //    new Vector2(5,2),
            //    new Vector2(-1,6),
            //    new Vector2(-2,5),
            //    new Vector2(-6,-1),
            //    new Vector2(-5,-2),
            //    new Vector2(1,-6),
            //    new Vector2(2,-5),
            //};
            var points = new[]{new Vector2(-101.5999985, -34.5535698), new
            TVGL.Numerics.Vector2(-88.9000015, -101.5999985), new            TVGL.Numerics.Vector2(-38.1000023, -158.5185089), new
            TVGL.Numerics.Vector2(38.1000023, -158.5185089), new            TVGL.Numerics.Vector2(88.9000015, -101.5999985), new
            TVGL.Numerics.Vector2(101.5999985, -34.5535698), new            TVGL.Numerics.Vector2(101.5999985, 34.6359444), new
            TVGL.Numerics.Vector2(88.9000015, 203.1999969), new            TVGL.Numerics.Vector2(-88.9000015, 203.1999969), new
            TVGL.Numerics.Vector2(-101.5999985, 34.6359444) };


            // var points = MakeWavyCircularPolygon(10000, 10, 1, 4.65432);
            var br = points.BoundingRectangle();
            Presenter.ShowAndHang(new[] { points, br.CornerPoints });
        }
        public static void TestSlice2D()
        {
            //var points = new[] {
            //    new Vector2(2.26970768, 4.28080463),
            //    new Vector2(5.84034252, 0),
            //    new Vector2(12.22331619, 2.24806976),
            //    new Vector2(23.56225014, 21.88767815),
            //    new Vector2(19.9916172, 26.16848373),
            //    new Vector2(13.60864258, 23.92041588)
            //        };
            var poly1 = new[] {
                new Vector2(6,1),
                new Vector2(5,2),
                new Vector2(-1,6),
                new Vector2(-2,5),
                new Vector2(-6,-1),
                new Vector2(-5,-2),
                new Vector2(1,-6),
                new Vector2(2,-5),
            };
            var hole1 = new[] {
                new Vector2(1,1),
                new Vector2(1,-1),
                new Vector2(-1,1),
                new Vector2(-2,2),
            };
            var polyTree = new[] { poly1, hole1 };
            Presenter.ShowAndHang(polyTree);
            polyTree.SliceAtLine(new Vector2(1, -0.1).Normalize(), 0, out var negPolys, out var posPolys, -0.3, -0.3);

            Presenter.ShowAndHang(new[] { negPolys, posPolys });
        }

        public static void TestOffsetting()
        {
            var coords1 = MakeStarryCircularPolygon(50, 28, 8).ToList();
            var hole1 = MakeStarryCircularPolygon(80, 14, 5).ToList();
            hole1.Reverse();
            var polygon1 = new Polygon(coords1);
            polygon1 = polygon1.Intersect(new Polygon(hole1))[0];
            //Presenter.ShowAndHang(polygon1);
            //var polygon1 = new Polygon(coords1, true);
            // Presenter.ShowAndHang(polygon1);
            // var polygons2 = polygon1.OffsetRound(2, 0.05);
            var polygons3 = polygon1.OffsetRound(1, 0.05);
            //polygons3.AddRange(polygons2);
            //polygons3.Add(polygon1);
            Presenter.ShowAndHang(polygons3);


        }
    }
}
