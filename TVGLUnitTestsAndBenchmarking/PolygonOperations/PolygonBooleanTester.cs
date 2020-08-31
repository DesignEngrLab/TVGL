using BenchmarkDotNet.Attributes;
using OldTVGL;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    public class PolygonBooleanTester
    {
        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public List<Polygon> TVGLUnion(Polygon polygon1, Polygon polygon2, List<List<PointLight>> cpolygon1, List<List<PointLight>> cpolygon2)
            => TVGL.TwoDimensional.PolygonOperations.Union(polygon1, polygon2);


        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public List<List<PointLight>> ClipperUnion(Polygon polygon1, Polygon polygon2, List<List<PointLight>> cpolygon1, List<List<PointLight>> cpolygon2)
       => OldTVGL.PolygonOperations.Union(cpolygon1, cpolygon2);

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public List<Polygon> TVGLIntersect(Polygon polygon1, Polygon polygon2, List<List<PointLight>> cpolygon1, List<List<PointLight>> cpolygon2)
            => TVGL.TwoDimensional.PolygonOperations.Intersect(polygon1, polygon2);

        [Benchmark]
        [ArgumentsSource(nameof(Data))]
        public List<List<PointLight>> ClipperIntersect(Polygon polygon1, Polygon polygon2, List<List<PointLight>> cpolygon1, List<List<PointLight>> cpolygon2)
       => OldTVGL.PolygonOperations.Intersection(cpolygon1, cpolygon2);



        internal void FullComparison()
        {
            foreach (var args in Data())
            {
                Compare(TVGLUnion((Polygon)args[0], (Polygon)args[1], (List<List<PointLight>>)args[2], (List<List<PointLight>>)args[3]),
                    ClipperUnion((Polygon)args[0], (Polygon)args[1], (List<List<PointLight>>)args[2], (List<List<PointLight>>)args[3]),
                    (Polygon)args[0], (Polygon)args[1], "Union");
                Compare(TVGLIntersect((Polygon)args[0], (Polygon)args[1], (List<List<PointLight>>)args[2], (List<List<PointLight>>)args[3]),
                    ClipperIntersect((Polygon)args[0], (Polygon)args[1], (List<List<PointLight>>)args[2], (List<List<PointLight>>)args[3]),
                    (Polygon)args[0], (Polygon)args[1], "Intersect");
            }
        }


        public IEnumerable<object[]> Data()
        {
            for (int numVerts = 13; numVerts < 10000; numVerts++) // numVerts = (int)(1.5 * numVerts))
            {
                for (int delta = 2; delta < 3; delta = (int)(1.5 * delta))
                {
                    var poly1 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    var poly2 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    Console.WriteLine("Chunky Square: numVerts = {0}, thick={1}", numVerts, delta);
                    yield return new object[] { poly1, poly2, TestCases.Poly2PLs(poly1), TestCases.Poly2PLs(poly2) };
                }
            }

            Console.WriteLine("trapezoidInTri");
            var coords1 = new[] { new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 10) } };
            var coords2 = new[] { new[] { new Vector2(0, 0), new Vector2(4, 0), new Vector2(6, 4), new Vector2(3, 7) } };
            yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2), TestCases.C2PLs(coords1), TestCases.C2PLs(coords2) };
            Console.WriteLine("innerTouch");
            coords1 = new[] { new [] { new Vector2(0,0), new Vector2(7,0), new Vector2(4,2.5), new Vector2(3,4),
                        new Vector2(1,6), new Vector2(3,7), new Vector2(0,10) } };
            coords2 = new[] { new [] { new Vector2(2,7), new Vector2(1,6), new Vector2(2,2), new Vector2(5,1),
                        new Vector2(3,4), new Vector2(2,5) } };
            yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2), TestCases.C2PLs(coords1), TestCases.C2PLs(coords2) };
            Console.WriteLine("nestedSquares");
            coords1 = new[] { new [] { new Vector2(0,0), new Vector2(10,0), new Vector2(10,10), new Vector2(0,10) },
                        new [] { new Vector2(2, 2), new Vector2(2, 8), new Vector2(8, 8), new Vector2(8, 2) } };
            coords2 = new[] { new [] { new Vector2(1,1), new Vector2(9,1), new Vector2(9, 9), new Vector2(1, 9) },
                        new [] { new Vector2(3, 3), new Vector2(3, 7), new Vector2(7, 7), new Vector2(7, 3) } };
            yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2), TestCases.C2PLs(coords1), TestCases.C2PLs(coords2) };



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
                                    if (k % 1 == 0)
                                    {
                                        Console.WriteLine("Octogon Case: " + k);
                                        coords1 = new[] { TestCases.MakeOctogonPolygon(0, 0, 2 * leftCut + leftWidth, 2 * leftCut + leftHeight, leftCut).ToArray() };
                                        coords2 = new[] { TestCases.MakeOctogonPolygon(9 - (2 * rightCut + rightWidth), 9 - (2 * rightCut + rightHeight), 9, 9, rightCut).ToArray() };
                                        yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2), TestCases.C2PLs(coords1), TestCases.C2PLs(coords2) };
                                        k++;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            var radius = 100;
            for (int numSides = 12; numSides < 3000; numSides *= 2)
            {
                for (int delta = 0; delta < radius / 4; delta = 1 + (2 * delta))
                {
                    (coords1, coords2) = TestCases.MakeBumpyRings(numSides, radius, delta);
                    Console.WriteLine("Bumpy Rings:{0}, {1}, {2}", numSides, radius, delta);
                    yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2), TestCases.C2PLs(coords1), TestCases.C2PLs(coords2) };
                }
            }



        }

        internal static void TestRemoveSelfIntersect()
        {
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine(i);
                //var coords = MakeWavyCircularPolygon(rand(500), rand(30), rand(300), rand(15.0)).ToList();
                var coords = TestCases.MakeRandomComplexPolygon(100, 30).ToList();
                Presenter.ShowAndHang(coords);
                var polygon = new Polygon(coords);
                var polygons = polygon.RemoveSelfIntersections(true, out _);
                Presenter.ShowAndHang(polygons);
                //Presenter.ShowAndHang(polygons.Path);
                //Presenter.ShowAndHang(new[] { coords }, new[] { polygon.Path });
            }
        }




        private void Compare(List<Polygon> tvglResult, List<List<PointLight>> clipperResult, Polygon polygon1, Polygon polygon2, string operationString)
        {
            var numVoxels = 500;
            var min = new Vector2(Math.Min(polygon1.MinX, polygon2.MinX),
                Math.Min(polygon1.MinY, polygon2.MinY));
            var max = new Vector2(Math.Max(polygon1.MaxX, polygon2.MaxX),
                Math.Max(polygon1.MaxY, polygon2.MaxY));
            var dimensions = max - min;
            var buffer = 0.01 * dimensions;
            min -= buffer;
            max += buffer;
            var vp1 = new VoxelizedSolid(new[] { polygon1 }, numVoxels, new[] { min, max });
            var vp2 = new VoxelizedSolid(new[] { polygon2 }, numVoxels, new[] { min, max });
            var correctVoxels = operationString switch
            {
                "Union" => vp1.UnionToNewSolid(vp2),
                "Intersect" => vp1.IntersectToNewSolid(vp2),
                "SubtractAB" => vp1.SubtractToNewSolid(vp2),
                "SubtractBA" => vp2.SubtractToNewSolid(vp1),
                _ => throw new NotImplementedException()
            };

            var tvglVResult = new VoxelizedSolid(tvglResult, 500, new[] { min, max });
            var clipperShallowPolyTree = TVGL.TwoDimensional.PolygonOperations.
                   CreateShallowPolygonTrees(clipperResult.Select(c => new Polygon(c.Select(v => new Vector2(v.X, v.Y)))), true, out _, out _);
            var clipperVResult = new VoxelizedSolid(clipperShallowPolyTree, 500, new[] { min, max });
            var showResult = false;
            var tvglError = false;
            var clipperError = false;
            if (tvglVResult.SubtractToNewSolid(correctVoxels).Count == 0 && correctVoxels.SubtractToNewSolid(tvglVResult).Count == 0)
                Console.WriteLine("TVGL result is correct.");
            else
            {
                Console.WriteLine("         ////////////   TVGL result is wrong.");
                showResult = tvglError = true;
            }
            if (clipperVResult.SubtractToNewSolid(correctVoxels).Count == 0 && correctVoxels.SubtractToNewSolid(clipperVResult).Count == 0)
                Console.WriteLine("Clipper result is correct.");
            else
            {
                Console.WriteLine(@"          \\\\\\\\\\\\ Clipper result is wrong.");
                showResult = clipperError = true;
            }


            var numPolygonsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Count());
            var numPolygonsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Count());
            var numPolyVertsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var numPolyVertsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var polyAreaTVGL = tvglResult.Sum(p => p.Area);
            var polyAreaClipper = clipperShallowPolyTree.Sum(p => p.Area);
            var polyPerimeterTVGL = tvglResult.Sum(p => p.Perimeter);
            var polyPerimeterClipper = clipperShallowPolyTree.Sum(p => p.Perimeter);
            if (numPolygonsTVGL == numPolygonsClipper
                && numPolyVertsTVGL == numPolyVertsClipper &&
                 polyAreaTVGL.IsPracticallySame(polyAreaClipper, (polyAreaTVGL + polyAreaClipper) * 1e-4) &&
                 polyPerimeterTVGL.IsPracticallySame(polyPerimeterClipper, (polyPerimeterTVGL + polyPerimeterClipper) * 1e-4)
                )
            {
                Console.WriteLine("*****{0} matches", operationString);
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("{0} does not match", operationString);
                if (numPolygonsTVGL == numPolygonsClipper)
                    Console.WriteLine("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                else Console.WriteLine("    --- polygons: TVGL={0}  : Clipper={1} ", numPolygonsTVGL, numPolygonsClipper);
                if (numPolyVertsTVGL == numPolyVertsClipper)
                    Console.WriteLine("+++ both have {0} vertices(s)", numPolyVertsTVGL);
                else Console.WriteLine("    --- verts: TVGL= {0}  : Clipper={1} ", numPolyVertsTVGL, numPolyVertsClipper);

                if (tvglResult.Sum(p => p.Area).IsPracticallySame(clipperShallowPolyTree.Sum(p => p.Area), 1e-3))
                    Console.WriteLine("+++ both have area of {0}", tvglResult.Sum(p => p.Area));
                else
                {
                    Console.WriteLine("    --- area: TVGL= {0}  : Clipper={1} ", tvglResult.Sum(p => p.Area), clipperShallowPolyTree.Sum(p => p.Area));
                    showResult = true;
                }
                if (tvglResult.Sum(p => p.Perimeter).IsPracticallySame(clipperShallowPolyTree.Sum(p => p.Perimeter), 1e-3))
                    Console.WriteLine("+++ both have perimeter of {0}", tvglResult.Sum(p => p.Perimeter));
                else
                {
                    Console.WriteLine("    --- perimeter: TVGL={0}  : Clipper={1} ", tvglResult.Sum(p => p.Perimeter), clipperShallowPolyTree.Sum(p => p.Perimeter));
                    showResult = true;
                }
                if (showResult)
                {
                    var input = polygon1.AllPolygons.ToList();
                    input.AddRange(polygon2.AllPolygons);
                    Presenter.ShowAndHang(input, "Arguments");
                    Presenter.ShowAndHang(tvglResult, "TVGLPro");
                    Presenter.ShowAndHang(clipperShallowPolyTree, "Clipper");
                    if (tvglError)
                    {
                        Console.WriteLine("showing tvgl error...");
                        Presenter.ShowAndHang(correctVoxels, tvglResult);
                    }
                    if (clipperError)
                    {
                        Console.WriteLine("showing clipper error...");
                        Presenter.ShowAndHang(correctVoxels, clipperShallowPolyTree);
                    }
                }

                Console.WriteLine();
            }
        }




    }
}
