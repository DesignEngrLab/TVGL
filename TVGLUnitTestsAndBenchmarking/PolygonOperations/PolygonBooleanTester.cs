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
            //Console.WriteLine("trapezoidInTri");
            var coords1 = new[] { new[] { new Vector2(0, 0), new Vector2(10, 0), new Vector2(0, 10) } };
            var coords2 = new[] { new[] { new Vector2(0, 0), new Vector2(4, 0), new Vector2(6, 4), new Vector2(3, 7) } };
            //yield return new object[] { C2Poly(coords1), C2Poly(coords2), C2PLs(coords1), C2PLs(coords2) };
            //Console.WriteLine("innerTouch");
            //coords1 = new[] { new [] { new Vector2(0,0), new Vector2(7,0), new Vector2(4,2.5), new Vector2(3,4),
            //            new Vector2(1,6), new Vector2(3,7), new Vector2(0,10) } };
            //coords2 = new[] { new [] { new Vector2(2,7), new Vector2(1,6), new Vector2(2,2), new Vector2(5,1),
            //            new Vector2(3,4), new Vector2(2,5) } };
            //yield return new object[] { C2Poly(coords1), C2Poly(coords2), C2PLs(coords1), C2PLs(coords2) };
            //Console.WriteLine("nestedSquares");
            //coords1 = new[] { new [] { new Vector2(0,0), new Vector2(10,0), new Vector2(10,10), new Vector2(0,10) },
            //            new [] { new Vector2(2, 2), new Vector2(2, 8), new Vector2(8, 8), new Vector2(8, 2) } };
            //coords2 = new[] { new [] { new Vector2(1,1), new Vector2(9,1), new Vector2(9, 9), new Vector2(1, 9) },
            //            new [] { new Vector2(3, 3), new Vector2(3, 7), new Vector2(7, 7), new Vector2(7, 3) } };
            //yield return new object[] { C2Poly(coords1), C2Poly(coords2), C2PLs(coords1), C2PLs(coords2) };

            //Console.WriteLine("Octogons");
            //int k = 0;
            //for (int leftCut = 1; leftCut <= 4; leftCut++)
            //{
            //    for (int leftWidth = 5 - leftCut; leftWidth < 11 - 2 * leftCut; leftWidth++)
            //    {
            //        for (int leftHeight = 5 - leftCut; leftHeight < 11 - 2 * leftCut; leftHeight++)
            //        {
            //            for (int rightCut = 1; rightCut <= 4; rightCut++)
            //            {
            //                for (int rightWidth = 5 - rightCut; rightWidth < 11 - 2 * rightCut; rightWidth++)
            //                {
            //                    for (int rightHeight = 5 - rightCut; rightHeight < 11 - 2 * rightCut; rightHeight++)
            //                    {
            //                        Console.WriteLine(k++);
            //                        if (k++ % 1234 == 0)
            //                        {
            //                            coords1 = new[] { MakeOctogonPolygon(0, 0, 2 * leftCut + leftWidth, 2 * leftCut + leftHeight, leftCut).ToArray() };
            //                            coords2 = new[] { MakeOctogonPolygon(9 - (2 * rightCut + rightWidth), 9 - (2 * rightCut + rightHeight), 9, 9, rightCut).ToArray() };
            //                            yield return new object[] { C2Poly(coords1), C2Poly(coords2), C2PLs(coords1), C2PLs(coords2) };
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}


            var radius = 25;
            for (int i = 12; i < 97; i *= 2)
            {
                for (int delta = 0; delta < 20; delta = 1 + (2 * delta))
                {
                    (coords1, coords2) = MakeBumpyRings(i, radius, delta);
                    yield return new object[] { C2Poly(coords1), C2Poly(coords2), C2PLs(coords1), C2PLs(coords2) };
                }

            }
        }
        Polygon C2Poly(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            var result = new Polygon(coordinates.First());
            foreach (var inner in coordinates.Skip(1))
                result.AddInnerPolygon(new Polygon(inner));
            return result;
        }
        List<List<PointLight>> C2PLs(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            return coordinates.Select(innerPoly => innerPoly.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList()).ToList();
        }


        static Random r = new Random(1);

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

        internal static Polygon MakeChunkySquarePolygon(int sideLength, int bufferThickness)
        {
            var result = new List<Vector2>();
            //var prevPoint = new Vector2(-sideLength / 2, -sideLength / 2);
            var dirs = new[] { new Vector2(1, 0), new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1) };
            var sign = 1;
            foreach (var dir in dirs)
            {
                var cross = new Vector2(dir.Y, -dir.X);
                var point = -(sideLength / 2) * dir + (bufferThickness + sideLength / 2) * cross;
                result.Add(point);
                var thisSide = 0;
                var i = bufferThickness;
                var along = 3;
                while (thisSide < sideLength)
                {
                    if (i-- == 0)
                    {
                        i = bufferThickness;
                        sign *= -1;
                    }
                    point += along * dir;
                    result.Add(point);
                    point += sign * cross;
                    result.Add(point);
                    along = r.Next(3);
                    thisSide += along;
                }
            }
            var polygons = new Polygon(result).RemoveSelfIntersections(true, out _, 1e-9);
            var maxArea = polygons.Max(p => p.Area);
            return polygons.First(polygons => polygons.Area == maxArea);
        }

        internal static void TestRemoveSelfIntersect()
        {
            for (int i = 0; i < 20; i++)
            {
                r = new Random(i);
                Console.WriteLine(i);
                //var coords = MakeWavyCircularPolygon(rand(500), rand(30), rand(300), rand(15.0)).ToList();
                var coords = MakeRandomComplexPolygon(100, 30).ToList();
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
            if (tvglVResult.SubtractToNewSolid(correctVoxels).Count == 0 && correctVoxels.SubtractToNewSolid(tvglVResult).Count == 0)
                Console.WriteLine("TVGL result is correct.");
            else
            {
                Console.WriteLine("         ////////////   TVGL result is wrong.");
                showResult = true;
            }
            if (clipperVResult.SubtractToNewSolid(correctVoxels).Count == 0 && correctVoxels.SubtractToNewSolid(clipperVResult).Count == 0)
                Console.WriteLine("Clipper result is correct.");
            else
            {
                Console.WriteLine(@"             \\\\\\\\\\\\ Clipper result is wrong.");
                showResult = true;
            }


            var numPolygonsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Count());
            var numPolygonsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Count());
            var numPolyVertsTVGL = tvglResult.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            var numPolyVertsClipper = clipperShallowPolyTree.Sum(poly => poly.AllPolygons.Sum(innerpoly => innerpoly.Vertices.Count));
            if (numPolygonsTVGL == numPolygonsClipper
                && numPolyVertsTVGL == numPolyVertsClipper &&
                 tvglResult.Sum(p => p.Area).IsPracticallySame(clipperShallowPolyTree.Sum(p => p.Area), 1e-3) &&
                 tvglResult.Sum(p => p.Perimeter).IsPracticallySame(clipperShallowPolyTree.Sum(p => p.Perimeter), 1e-3)
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
                }
                Console.WriteLine();
            }
        }


        internal (Vector2[][], Vector2[][]) MakeBumpyRings(int i, double radius, double delta)
        {
            var holeRadius = 0.67 * radius - 3 * delta;
            var coords1 = new[] {
                MakeStarryCircularPolygon(i, radius, delta).ToArray(),
                MakeStarryCircularPolygon(i, holeRadius, delta).Reverse().ToArray()
            };

            var coords2 = MakeStarryCircularPolygon(i, radius, delta).ToArray();
            for (int j = 0; j < coords2.Length; j++)
                coords2[j] += new Vector2(radius, 0.67 * radius);
            var hole2 = MakeStarryCircularPolygon(i, holeRadius, delta).Reverse().ToArray();
            for (int j = 0; j < hole2.Length; j++)
                hole2[j] += new Vector2(radius, 0.67 * radius);

            var outer = new[] { coords2, hole2 };
            return (coords1, outer);
        }


    }
}
