using BenchmarkDotNet.Attributes;
using Microsoft.Diagnostics.Tracing.Parsers.MicrosoftWindowsTCPIP;
#if !PRESENT
using OldTVGL;
#endif
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    public static class TestCases
    {

        static Random r = new Random(1);


        internal static Polygon C2Poly(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            var result = new Polygon(coordinates.First());
            if (!result.IsPositive) result.Reverse();
            foreach (var inner in coordinates.Skip(1))
            {
                //inner.Reverse();
                result.AddInnerPolygon(new Polygon(inner));
            }
            return result;
        }
#if !PRESENT

        internal static List<List<PointLight>> C2PLs(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            var outerCoords = coordinates.First();
            var outer = outerCoords.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList();

            if (outerCoords.Area() < 0) outer.Reverse();
            var result = new List<List<PointLight>>();
            result.Add(outer);
            foreach (var inner in coordinates.Skip(1))
            {
                //inner.Reverse();
                result.Add(inner.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList());
            }
            return result;
        }

        internal static List<List<PointLight>> Poly2PLs(Polygon p)
        {
            return p.AllPolygons.Select(innerPoly => innerPoly.Path.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList()).ToList();
        }
#endif



        internal static IEnumerable<KeyValuePair<string, (Vector2[][], Vector2[][])>> GetAllSingleArgumentEdgeCases()
        {
            foreach (var kvp in EdgeCases)
                if (kvp.Value.Item2 == null)
                    yield return kvp;
        }
        internal static IEnumerable<KeyValuePair<string, (Vector2[][], Vector2[][])>> GetAllTwoArgumentEdgeCases()
        {
            foreach (var kvp in EdgeCases)
                if (kvp.Value.Item2 != null)
                    yield return kvp;
        }

        // used as a substitute, typically an inferior one, for something else

        internal static Dictionary<string, (Vector2[][], Vector2[][])> EdgeCases =
            new Dictionary<string, (Vector2[][], Vector2[][])>
            {
                { "claw", (
                    new [] { new [] { new Vector2(0,0), new Vector2(10,10), new Vector2(2, 5), new Vector2(2, 20), new Vector2(10, 15), new Vector2(0, 25) } },
                    null)},
                { "tinyOffsetProb", (
                    new [] { new [] { new Vector2(79.66782032532383, 55.43041375392381),
                        new Vector2(80.10308177750458,55.16161954594162), new Vector2(79.74617295481619,55.5026089914283),
                        new Vector2(79.49108255907875,55.59107482211029) ,new Vector2(79.78187368483665, 55.35998048399254)} },
                    null)},
{ "skyline", (
                    new [] { new [] { new Vector2(0,4), new Vector2(0,0), new Vector2(8, 0), new Vector2(8, 4), new Vector2(7, 4), new Vector2(7, 2), new Vector2(6,2), new Vector2(4,2), new Vector2(4,3), new Vector2(1,3), new Vector2(1,4) } },
                    new [] { new [] { new Vector2(0,3), new Vector2(0,0), new Vector2(8, 0), new Vector2(8, 4), new Vector2(7,4), new Vector2(7, 3), new Vector2(6, 3), new Vector2(2, 3), new Vector2(2, 4), new Vector2(1,4), new Vector2(1, 3) } })},
                 { "cutout", (
                    new [] { new [] { new Vector2(0,0), new Vector2(10, 0), new Vector2(10, 10), new Vector2(8, 8), new Vector2(8, 3), new Vector2(3, 3) } },
                    new [] { new [] { new Vector2(3,3), new Vector2(8,3), new Vector2(8,8) } })},
                { "pinch", (
                    new [] { new [] { new Vector2(-3,-3), new Vector2(10, 0), new Vector2(0, 0), new Vector2(0, 10) } },
                    new [] { new [] { new Vector2(0,10), new Vector2(0,5), new Vector2(10,0) } })},

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
                { "hand", (
                    new [] { new [] { new Vector2(0,5), new Vector2(3, 4), new Vector2(1, 3), new Vector2(4, 2) , new Vector2(2, 1),
                    new Vector2(12,5),new Vector2(2.2,9),new Vector2(4.4,8),new Vector2(1.1,7),new Vector2(3.3,6)
                    } }, null) },
               {"boundingRectTest1",(
                    new[] { new[] {
                        new Vector2(2.26970768, 4.28080463), new Vector2(5.84034252, 0), new Vector2(12.22331619, 2.24806976),
                        new Vector2(23.56225014, 21.88767815), new Vector2(19.9916172, 26.16848373), new Vector2(13.60864258, 23.92041588) } }, null) },
                {"boundingRectTest2",(
                    new[] { new[] {
                        new Vector2(6,1), new Vector2(5,2), new Vector2(-1,6), new Vector2(-2,5),
                        new Vector2(-6,-1), new Vector2(-5,-2), new Vector2(1,-6), new Vector2(2,-5) } }, null) },
                {"boundingRectTest3",(
                    new[] { new[] {
                        new Vector2(-101.5999985, -34.5535698), new Vector2(-88.9000015, -101.5999985), new Vector2(-38.1000023, -158.5185089),
                        new Vector2(38.1000023, -158.5185089), new Vector2(88.9000015, -101.5999985), new Vector2(101.5999985, -34.5535698),
                        new Vector2(101.5999985, 34.6359444), new Vector2(88.9000015, 203.1999969), new Vector2(-88.9000015, 203.1999969),
                        new Vector2(-101.5999985, 34.6359444) } }, null) },
                {"sliceSimpleCase",(
                    new[] { new[] {
                        new Vector2(2.26970768, 4.28080463), new Vector2(5.84034252, 0), new Vector2(12.22331619, 2.24806976),
                        new Vector2(23.56225014, 21.88767815), new Vector2(19.9916172, 26.16848373), new Vector2(13.60864258, 23.92041588) } }, null) },
                {"sliceWithHole",(
                    new[] { new[] { new Vector2(6,1), new Vector2(5,2), new Vector2(-1,6),new Vector2(-2,5),new Vector2(-6,-1),
                        new Vector2(-5,-2), new Vector2(1,-6), new Vector2(2,-5) },
                        new[] {new Vector2(1,1), new Vector2(1,-1), new Vector2(-1,1), new Vector2(-2,2) } },null)}
            };


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
            //return new Polygon(result);
            var polygons = new Polygon(result).RemoveSelfIntersections(ResultType.OnlyKeepPositive, 1e-9);
            var maxArea = polygons.Max(p => p.Area);
            var polygon = polygons.First(polygons => polygons.Area == maxArea);
            polygon.Transform(Matrix3x3.CreateRotation(1));
            return polygon;
        }

        internal static (Vector2[][], Vector2[][]) MakeBumpyRings(int i, double radius, double delta)
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



        internal static (Vector2[][], Vector2[][]) LoadWlrPolygonSet()
        {
            var polys = new Vector2[2][][];
            var fileNames = new[] { "s.wlr", "c.wlr" };
            for (int k = 0; k < 2; k++)
            {

                var reader = new StreamReader(Path.Combine("../../../../TestFiles", fileNames[k]));
                string line = reader.ReadLine();

                line = reader.ReadLine();
                int polygonCount = int.Parse(line);
                var polygonSet = new Vector2[polygonCount][];
                for (int i = 0; i < polygonCount; i++)
                {
                    line = reader.ReadLine();
                    int verticesCount = int.Parse(line);
                    var polygon = new Vector2[verticesCount];
                    for (int j = 0; j < verticesCount; j++)
                    {
                        line = reader.ReadLine();
                        string[] parts = line.Split(",");
                        var vertex = new Vector2(double.Parse(parts[0]), double.Parse(parts[1]));
                        polygon[j] = vertex;
                    }

                    if (i == 0)
                    {
                        if (polygon.Area() < 0) polygon = polygon.Reverse().ToArray();
                    }
                    else if (polygon.Area() < 0) polygon = polygon.Reverse().ToArray();


                    polygonSet[i] = polygon;
                }

                polys[k] = polygonSet;
            }

            return (polys[0], polys[1]);
        }

        internal static (Vector2[][], Vector2[][]) BenchKnown(int intersections)
        {
            int rays = (int)Math.Floor(Math.Sqrt(intersections / 2.0));
            int rings = rays;
            if ((intersections / 2) - rays * rings > rays)
                rays++;

            double internalRadius = 10;
            double raysRadius = 1000;

            var psA = GetTestClockwiseGearPolygon(rays, raysRadius, internalRadius);

            var psB = new List<Vector2[]>();
            var distBetweenRings = (raysRadius - internalRadius) / rings;
            var ringRadius = raysRadius - (distBetweenRings / 2);
            for (int i = 0; i < rings; i++)
            {
                var poly = GetTestClockwisePolygon(rays * 2, ringRadius);
                if (i % 2 == 1)
                    poly = poly.Reverse().ToArray();
                psB.Add(poly);
                ringRadius -= distBetweenRings;
            }
            return (new[] { psA }, psB.ToArray());
        }

        internal static Vector2[] GetTestClockwiseGearPolygon(int rays, double raysRadius, double internalRadius, double angleOffset = 0)
        {
            var p = new Vector2[4 * rays];
            double rotationStep = Math.PI / rays;
            var angle = angleOffset;
            for (int rayIndex = 0; rayIndex < rays; rayIndex++)
            {
                double x = internalRadius * Math.Cos(angle);
                double y = internalRadius * Math.Sin(angle);
                p[4 * rayIndex] = new Vector2(x, y);

                x = raysRadius * Math.Cos(angle);
                y = raysRadius * Math.Sin(angle);
                p[4 * rayIndex + 1] = new Vector2(x, y);

                angle += rotationStep;
                x = raysRadius * Math.Cos(angle);
                y = raysRadius * Math.Sin(angle);
                p[4 * rayIndex + 2] = new Vector2(x, y);

                x = internalRadius * Math.Cos(angle);
                y = internalRadius * Math.Sin(angle);
                p[4 * rayIndex + 3] = new Vector2(x, y);
                angle += rotationStep;
            }
            return p;
        }

        private static Vector2[] GetTestClockwisePolygon(int rays, double ringRadius)
        {
            var p = new Vector2[rays];
            double rotationStep = 2 * Math.PI / rays;
            var angle = 0.0;
            for (int i = 0; i < rays; i++)
            {
                double x = ringRadius * Math.Cos(angle);
                double y = ringRadius * Math.Sin(angle);
                p[i] = new Vector2(x, y);
                angle += rotationStep;
            }
            return p;
        }


    }
}
