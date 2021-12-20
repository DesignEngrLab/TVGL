using BenchmarkDotNet.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.IOFunctions;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    public class PolygonBooleanTester
    {
        //[Benchmark]
        [ArgumentsSource(nameof(Data))]
        public static List<Polygon> TVGLUnion(Polygon polygon1, Polygon polygon2)
            => TVGL.TwoDimensional.PolygonOperations.Union(polygon1, polygon2, PolygonSimplify.CanSimplifyOriginal, PolygonCollection.SeparateLoops);


        // [Benchmark]
        // [ArgumentsSource(nameof(Data))]
        // public static List<List<PointLight>> ClipperUnion(Polygon polygon1, Polygon polygon2, out long ticks)
        //=> OldTVGL.PolygonOperations.Union(cpolygon1, cpolygon2, out ticks);



        internal static void FullComparison()
        {
            var stats = new List<(string, int, long, long)>();
            int numTVGLErrors = 0;
            int numClipperErrors = 0;
            (Vector2[][], Vector2[][]) polys;

            for (var n = 10; n < 8000; n = (int)(n * 3.7782794))
            {
                polys = TestCases.BenchKnown(n);
                var errors = SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2));
                numTVGLErrors += errors.Item1;
                numClipperErrors += errors.Item2;
            }
            polys = TestCases.LoadWlrPolygonSet();
            SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2));
            var radius = 100;
            for (int numVerts = 10; numVerts < 10000; numVerts = (int)(3.5 * numVerts))
            {
                for (int delta = 0; delta < radius / 25; delta = 1 + (2 * delta))
                {
                    polys = TestCases.MakeBumpyRings(numVerts, radius, delta);
                    Console.WriteLine("Bumpy Rings:{0}, {1}, {2}", numVerts, radius, delta);
                    var errors = SingleCompare(stats, TestCases.C2Poly(polys.Item1), TestCases.C2Poly(polys.Item2));
                    numTVGLErrors += errors.Item1;
                    numClipperErrors += errors.Item2;
                }
            }

            for (int numVerts = 10; numVerts < 5000; numVerts = (int)(3 * numVerts))
            {
                for (int delta = 2; delta < 3; delta = (int)(1.5 * delta))
                {
                    var poly1 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    var poly2 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    //Console.WriteLine("Chunky Square: numVerts = {0}, thick={1}", numVerts, delta);
                    var errors = SingleCompare(stats, poly1, poly2);
                    numTVGLErrors += errors.Item1;
                    numClipperErrors += errors.Item2;
                }
            }

            foreach (var kvp in TestCases.TessellatedSolidCrossSections())
            {
                var poly1 = kvp.Value.Item1;
                var poly2 = kvp.Value.Item2;
                //Presenter.ShowAndHang(new[] { poly1, poly2 });
                Console.WriteLine("XSections From TS's: " + kvp.Key);
                var errors = SingleCompare(stats, poly1, poly2);
                numTVGLErrors += errors.Item1;
                numClipperErrors += errors.Item2;
            }

            Console.WriteLine("Number of TVGL Errors = " + numTVGLErrors);
            Console.WriteLine("Number of Clipper Errors = " + numClipperErrors);
            System.IO.StreamWriter SaveFile = new System.IO.StreamWriter("stats.csv");
            foreach (var item in stats)
            {
                SaveFile.WriteLine(item.Item1 + ", " + item.Item2 + ", " + item.Item3 + ", " + item.Item4);
            }
            SaveFile.Close();
        }

        internal static (int, int) SingleCompare(List<(string, int, long, long)> stats, Polygon p1, Polygon p2)
        {
            var stopWatch = new Stopwatch();
            var numIters = 1;
            var operationString = "";
            List<Polygon> tvglResult = null;
            List<Polygon> clipperResult = null;
            long elapsedTVGL;
            long elapsedClipper = 0;
            int numTVGLErrors = 0;
            int numClipperErrors = 0;
            //Presenter.ShowAndHang(new[] { p1, p2 });
            var numVerts = p1.AllPolygons.Sum(p => p.Vertices.Count) + p2.AllPolygons.Sum(p => p.Vertices.Count);
            /********** Union *********/
            operationString = "Union";
            Console.WriteLine("testing " + operationString + ": " + numVerts + " vertices");
            for (int i = 0; i < numIters; i++)
            {
                Console.WriteLine("    test  " + i);
                stopWatch.Restart();
                tvglResult = TVGLUnion(p1, p2);
                stopWatch.Stop();
                elapsedTVGL = stopWatch.ElapsedTicks;
                //clipperResult = ClipperUnion(p1, p2, v1, v2, out elapsedClipper);
                Console.WriteLine("Time for: TVGL = {0}   ,    Clipper = {1}\n\n", elapsedTVGL, elapsedClipper);
                stats.Add((operationString, numVerts, elapsedTVGL, elapsedClipper));
            }
            var errors = Compare(tvglResult, clipperResult, p1, p2, operationString);
            numTVGLErrors += errors.Item1;
            numClipperErrors += errors.Item2;
            /********** Intersection *********/
            p1 = tvglResult.LargestPolygonWithHoles();
            //v1 = clipperResult;
            operationString = "Intersect";
            Console.WriteLine("testing " + operationString + ": " + numVerts + " vertices");
            for (int i = 0; i < numIters; i++)
            {
                Console.WriteLine("    test  " + i);
                stopWatch.Restart();
                //tvglResult = TVGLIntersect(p1, p2);
                stopWatch.Stop();
                elapsedTVGL = stopWatch.ElapsedTicks;
                //clipperResult = ClipperIntersect(p1, p2, v1, v2, out elapsedClipper);
                Console.WriteLine("Time for: TVGL = {0}   ,    Clipper = {1}\n\n", elapsedTVGL, elapsedClipper);
                stats.Add((operationString, numVerts, elapsedTVGL, elapsedClipper));
            }
            errors = Compare(tvglResult, clipperResult, p1, p2, operationString);
            numTVGLErrors += errors.Item1;
            numClipperErrors += errors.Item2;
            /********** SubtractAB *********

            operationString = "SubtractAB";
            Console.WriteLine("testing " + operationString + ": " + numVerts + " vertices");
            for (int i = 0; i < numIters; i++)
            {
                Console.WriteLine("    test  " + i);
                stopWatch.Restart();
                tvglResult = TVGLASubtractB(p1, p2, v1, v2);
                stopWatch.Stop();
                elapsedTVGL = stopWatch.ElapsedTicks;
                clipperResult = ClipperASubtractB(p1, p2, v1, v2, out elapsedClipper);
                Console.WriteLine("Time for: TVGL = {0}   ,    Clipper = {1}\n\n", elapsedTVGL, elapsedClipper);
                stats.Add((operationString, numVerts, elapsedTVGL, elapsedClipper));
            }
            errors = Compare(tvglResult, clipperResult, p1, p2, operationString);
            numTVGLErrors += errors.Item1;
            numClipperErrors += errors.Item2;

            ********** SubtractBA *********/
            operationString = "SubtractBA";
            Console.WriteLine("testing " + operationString + ": " + numVerts + " vertices");
            for (int i = 0; i < numIters; i++)
            {
                Console.WriteLine("    test  " + i);
                stopWatch.Restart();
                //tvglResult = TVGLBSubtractA(p1, p2, v1, v2);
                stopWatch.Stop();
                elapsedTVGL = stopWatch.ElapsedTicks;
                //clipperResult = ClipperBSubtractA(p1, p2, v1, v2, out elapsedClipper);
                Console.WriteLine("Time for: TVGL = {0}   ,    Clipper = {1}\n\n", elapsedTVGL, elapsedClipper);
                stats.Add((operationString, numVerts, elapsedTVGL, elapsedClipper));
            }
            errors = Compare(tvglResult, clipperResult, p1, p2, operationString);
            numTVGLErrors += errors.Item1;
            numClipperErrors += errors.Item2;
            return (numTVGLErrors, numClipperErrors);
        }

        public IEnumerable<object[]> Data()
        {
            Vector2[][] coords1, coords2;


            foreach (var testcase in TestCases.GetAllTwoArgumentEdgeCases())
            {
                Console.WriteLine(testcase.Key);
                coords1 = testcase.Value.Item1;
                coords2 = testcase.Value.Item2;
                yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2) };
            }

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
                                        yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2) };
                                        k++;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            var radius = 100;
            for (int numVerts = 10; numVerts < 20000; numVerts = (int)(1.5 * numVerts))
            {
                for (int delta = 0; delta < radius / 25; delta = 1 + (2 * delta))
                {
                    (coords1, coords2) = TestCases.MakeBumpyRings(numVerts, radius, delta);
                    Console.WriteLine("Bumpy Rings:{0}, {1}, {2}", numVerts, radius, delta);
                    yield return new object[] { TestCases.C2Poly(coords1), TestCases.C2Poly(coords2) };
                }
            }


            /*
            for (int numVerts = 10; numVerts < 30000; numVerts = (int)(3 * numVerts))
            {
                for (int delta = 2; delta < 3; delta = (int)(1.5 * delta))
                {
                    var poly1 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    var poly2 = TestCases.MakeChunkySquarePolygon(numVerts, delta);
                    Console.WriteLine("Chunky Square: numVerts = {0}, thick={1}", numVerts, delta);
                    yield return new object[] { poly1, poly2, TestCases.Poly2PLs(poly1), TestCases.Poly2PLs(poly2) };
                }
            }
            */




        }



        private static (int, int) Compare(List<Polygon> tvglResult, List<Polygon> clipperResult, Polygon polygon1, Polygon polygon2, string operationString)
        {
            //return (0,0);
            var numVoxels = 1000;
            var tolerance = 1e-3;
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
            var showResult = false;
            var tvglError = false;
            var clipperError = false;
            var tvglVResult = new VoxelizedSolid(tvglResult, numVoxels, new[] { min, max });
            var clipperShallowPolyTree = TVGL.TwoDimensional.PolygonOperations.
                   CreateShallowPolygonTrees(clipperResult, true);
            var clipperVResult = new VoxelizedSolid(clipperShallowPolyTree, numVoxels, new[] { min, max });
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
            var vertsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var vertsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var areaTVGL = tvglResult.Sum(p => p.Area);
            var areaClipper = clipperShallowPolyTree.Sum(p => p.Area);
            var perimeterTVGL = tvglResult.Sum(p => p.Perimeter);
            var perimeterClipper = clipperShallowPolyTree.Sum(p => p.Perimeter);
            if (numPolygonsTVGL == numPolygonsClipper
                && vertsTVGL == vertsClipper &&
                 areaTVGL.IsPracticallySame(areaClipper, (areaTVGL + areaClipper) * tolerance) &&
                 perimeterTVGL.IsPracticallySame(perimeterClipper, (perimeterTVGL + perimeterClipper) * tolerance)
                )
            {
                Console.WriteLine("*****{0} matches", operationString);
                Console.WriteLine();
                return (0, 0);
            }
            else
            {
                Console.WriteLine("{0} does not match", operationString);
                if (numPolygonsTVGL == numPolygonsClipper)
                    Console.WriteLine("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                else Console.WriteLine("    --- polygons: TVGL={0}  : Clipper={1} ", numPolygonsTVGL, numPolygonsClipper);
                if (vertsTVGL == vertsClipper)
                    Console.WriteLine("+++ both have {0} vertices(s)", vertsTVGL);
                else Console.WriteLine("    --- verts: TVGL= {0}  : Clipper={1} ", vertsTVGL, vertsClipper);

                if (areaTVGL.IsPracticallySame(areaClipper, tolerance))
                    Console.WriteLine("+++ both have area of {0}", areaTVGL);
                else
                {
                    Console.WriteLine("    --- area: TVGL= {0}  : Clipper={1} ", areaTVGL, areaClipper);
                    showResult = true;
                }
                if (perimeterTVGL.IsPracticallySame(perimeterClipper, tolerance))
                    Console.WriteLine("+++ both have perimeter of {0}", perimeterTVGL);
                else
                {
                    Console.WriteLine("    --- perimeter: TVGL={0}  : Clipper={1} ", perimeterTVGL, perimeterClipper);
                    if (perimeterClipper - perimeterTVGL > 0 && Math.Round(perimeterClipper - perimeterTVGL) % 2 == 0)
                        Console.WriteLine("<><><><><><><> clipper is connecting separate poly's :", (int)(perimeterClipper - perimeterTVGL) / 2);
                    //else showResult = true;
                }
                showResult = false;
                if (showResult)
                {
                    var input = polygon1.AllPolygons.ToList();
                    input.AddRange(polygon2.AllPolygons);
                    Presenter.ShowAndHang(input, "Arguments");
                    Presenter.ShowAndHang(tvglResult, "TVGLPro");
                    Presenter.ShowAndHang(clipperShallowPolyTree, "Clipper");
                }
                if (tvglError && showResult)
                {
                    Console.WriteLine("showing tvgl error...");
                    var shallowTree = tvglResult.CreateShallowPolygonTrees(true);
                    Presenter.ShowAndHang(correctVoxels, shallowTree);
                }
                if (clipperError && showResult)
                {
                    Console.WriteLine("showing clipper error...");
                    Presenter.ShowAndHang(correctVoxels, clipperShallowPolyTree);
                }
                Console.WriteLine();
                return (tvglError ? 1 : 0, clipperError ? 1 : 0);
            }
        }


        public static void TestOffsetting()
        {
            //foreach (var polys in TestCases.GetAllTwoArgumentEdgeCases())
            //{
            var polysValue = TestCases.MakeBumpyRings(50, 28, 1.8);
            //var polysValue = TestCases.EdgeCases["tinyOffsetProb"];
            var polygon1 = new Polygon(polysValue.Item1);
            var polygon2 = new Polygon(polysValue.Item2);

            //Presenter.ShowAndHang(new[] { polygon1, polygon2 });
            var polygons = polygon1.Union(polygon2);

            //var polygons = new List<Polygon> { polygon1 };
            //var coords1 = TestCases.MakeStarryCircularPolygon(50, 28, 1.8).ToList();
            //var hole1 = TestCases.MakeStarryCircularPolygon(80, 14, 1.5).ToList();
            //hole1.Reverse();
            //var polygon1 = new Polygon(coords1);
            //polygon1 = polygon1.Intersect(new Polygon(hole1))[0];
            //Presenter.ShowAndHang(polygons);
            //var polygon1 = new Polygon(coords1, true);
            // Presenter.ShowAndHang(polygon1);
            //var polygons3 = polygon1.OffsetRound(88);
            var offsetBase = Math.Sqrt(polygons.MostPositivePolygon().PathArea);
            var factors = new[] { -.01, 0.01, -.03, 0.03, -.1, 0.1, -.3, 0.3, -1, 1, -3, 3, -10 };
            foreach (var factor in factors)
            {
                var offset = factor * offsetBase;
                // var pl3 = OldTVGL.PolygonOperations.OffsetRound(TestCases.Poly2PLs(polygons[0]), offset, .00254);
                var polygonsClipper = polygons[0].OffsetRound(offset, tolerance: 0.00254);
                var polygonsTVGL = polygons[0].OffsetRound(offset, tolerance: 0.00254);
                PolygonBooleanTester.CompareOffsetResults(polygonsTVGL, polygonsClipper, offset);
            }

            //polygons3.AddRange(polygons2);
            //polygons3.Add(polygon1);
            //Presenter.ShowAndHang(polygons3);
            // Presenter.ShowAndHang(new[] { polygons3[0], new Polygon(pl3[0].Select(v => new Vector2(v.X, v.Y))) });
            //}
        }

        public static void CompareOffsetResults(List<Polygon> tvglResult, List<Polygon> clipperResult, double offset)
        {
            var clipperShallowPolyTree = TVGL.TwoDimensional.PolygonOperations.CreateShallowPolygonTrees(clipperResult, true);
            var tolerance = Math.Sqrt(tvglResult.Sum(r => r.Area));
            var numPolygonsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Count());
            var numPolygonsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Count());
            var vertsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var vertsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var areaTVGL = tvglResult.Sum(p => p.Area);
            var areaClipper = clipperShallowPolyTree.Sum(p => p.Area);
            var perimeterTVGL = tvglResult.Sum(p => p.Perimeter);
            var perimeterClipper = clipperShallowPolyTree.Sum(p => p.Perimeter);
            if (numPolygonsTVGL == numPolygonsClipper
                && vertsTVGL == vertsClipper &&
                 areaTVGL.IsPracticallySame(areaClipper, (areaTVGL + areaClipper) * tolerance) &&
                 perimeterTVGL.IsPracticallySame(perimeterClipper, (perimeterTVGL + perimeterClipper) * tolerance)
                )
            {
                Console.WriteLine("*****offset {0} matches", offset);
                Console.WriteLine();
            }
            else
            {
                var showResult = false;
                Console.WriteLine("{0} does not match", offset);
                if (numPolygonsTVGL == numPolygonsClipper)
                    Console.WriteLine("+++ both have {0} polygon(s)", numPolygonsTVGL, numPolygonsClipper);
                else Console.WriteLine("    --- polygons: TVGL={0}  : Clipper={1} ", numPolygonsTVGL, numPolygonsClipper);
                if (vertsTVGL == vertsClipper)
                    Console.WriteLine("+++ both have {0} vertices(s)", vertsTVGL);
                else Console.WriteLine("    --- verts: TVGL= {0}  : Clipper={1} ", vertsTVGL, vertsClipper);

                if (areaTVGL.IsPracticallySame(areaClipper, tolerance))
                    Console.WriteLine("+++ both have area of {0}", areaTVGL);
                else
                {
                    Console.WriteLine("    --- area: TVGL= {0}  : Clipper={1} ", areaTVGL, areaClipper);
                    showResult = true;
                }
                if (perimeterTVGL.IsPracticallySame(perimeterClipper, tolerance))
                    Console.WriteLine("+++ both have perimeter of {0}", perimeterTVGL);
                else
                {
                    Console.WriteLine("    --- perimeter: TVGL={0}  : Clipper={1} ", perimeterTVGL, perimeterClipper);
                    if (perimeterClipper - perimeterTVGL > 0 && Math.Round(perimeterClipper - perimeterTVGL) % 2 == 0)
                        Console.WriteLine("<><><><><><><> clipper is connecting separate poly's :", (int)(perimeterClipper - perimeterTVGL) / 2);
                    else showResult = true;
                }
                if (showResult)
                {
                    Presenter.ShowAndHang(tvglResult, "TVGLPro");
                    Presenter.ShowAndHang(clipperShallowPolyTree, "Clipper");
                }
                Console.WriteLine();
            }
        }




    }
}
