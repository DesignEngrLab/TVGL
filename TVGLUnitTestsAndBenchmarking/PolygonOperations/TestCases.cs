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


        internal static List<Polygon> C2Poly(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            var result = new List<Polygon>();
            foreach (var poly in coordinates)
                result.Add(new Polygon(poly));
            return result;
        }
#if !PRESENT

        internal static List<List<PointLight>> C2PLs(IEnumerable<IEnumerable<Vector2>> coordinates)
        {
            return coordinates.Select(innerPoly => innerPoly.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList()).ToList();
        }

        internal static List<List<PointLight>> Poly2PLs(Polygon p)
        {
            return p.AllPolygons.Select(innerPoly => innerPoly.Path.Select(v => new OldTVGL.PointLight(v.X, v.Y)).ToList()).ToList();
        }
#endif



        internal static IEnumerable<KeyValuePair<string, (Vector2[][], Vector2[][])>> GetAllSingleArgumentErsatzCases()
        {
            foreach (var kvp in Ersatz)
                if (kvp.Value.Item2 == null)
                    yield return kvp;
        }
        internal static IEnumerable<KeyValuePair<string, (Vector2[][], Vector2[][])>> GetAllTwoArgumentErsatzCases()
        {
            foreach (var kvp in Ersatz)
                if (kvp.Value.Item2 != null)
                    yield return kvp;
        }

        internal static Dictionary<string, (Vector2[][], Vector2[][])> Ersatz =
            new Dictionary<string, (Vector2[][], Vector2[][])>
            {
                { "skyline", (
                    new [] { new [] { new Vector2(0,4), new Vector2(0,0), new Vector2(8, 0), new Vector2(8, 4), new Vector2(7, 4), new Vector2(7, 2), new Vector2(6,2), new Vector2(4,2), new Vector2(4,3), new Vector2(1,3), new Vector2(1,4) } },
                    new [] { new [] { new Vector2(0,3), new Vector2(0,0), new Vector2(8, 0), new Vector2(8, 4), new Vector2(7,4), new Vector2(7, 3), new Vector2(6, 3), new Vector2(2, 3), new Vector2(2, 4), new Vector2(1,4), new Vector2(1, 3) } })},
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
            var polygons = new Polygon(result).RemoveSelfIntersections(true, out _, 1e-9);
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
                        if (polygon.Area() < 0)
                            polygon.Reverse();
                    }
                    else if (polygon.Area() < 0)
                        polygon.Reverse();

                    polygonSet[i] = polygon;
                }

                polys[k] = polygonSet;
            }

            return (polys[0], polys[1]);
        }

        internal static (Vector2[][], Vector2[][]) BenchKnown(int intersections, int loops, double angleOffset)
        {
            int intersections4 = intersections / 4;
            int rays = (int)Math.Floor(Math.Sqrt(intersections4));
            int cilinders = rays;
            if (intersections4 - rays * cilinders > rays)
                rays++;

            int longerRays = intersections4 - (rays * cilinders);
            if (longerRays == rays)
            {
                rays++;
                longerRays = 0;
            }

            const double radiusStep = 2.6;
            int internalRadius = (int)(radiusStep * rays * Math.PI * 2);
            double raysRadius = internalRadius + (cilinders * radiusStep) + radiusStep;
            double longerRaysRadius = raysRadius + (radiusStep * 2);

            var psA = GetTestClockwiseGearPolygon(rays, longerRays, raysRadius, longerRaysRadius, internalRadius);

            var psB = new List<Vector2[]>();
            if (longerRays > 0)
                cilinders++;

            for (int counter = 1; counter <= cilinders; counter++)
            {
                psB.Add(GetTestClockwisePolygon(rays * 2, internalRadius + (counter * radiusStep) + (radiusStep / 2), 0));
                var hole = GetTestClockwisePolygon(rays * 2, internalRadius + counter * radiusStep, 0);
                hole.Reverse();
                psB.Add(hole);
            }
            return (new[] { psA }, psB.ToArray());
        }

        internal static Vector2[] GetTestClockwiseGearPolygon(int rays, int longerRays, double raysRadius, double longerRaysRadius, double internalRadius, double angleOffset = 0)
        {
            var p = new List<Vector2>();
            double rotationStep = -Math.PI / rays;
            for (int rayIndex = 0; rayIndex < rays; rayIndex++)
            {
                double angle = (rotationStep * 2 * rayIndex) + angleOffset;
                double x = internalRadius * Math.Cos(angle);
                double y = internalRadius * Math.Sin(angle);
                p.Add(new Vector2(x, y));
                double externalRadius = raysRadius;
                if (longerRays > 0)
                {
                    externalRadius = longerRaysRadius;
                    longerRays -= 1;
                }

                x = (externalRadius * Math.Cos(angle));
                y = (externalRadius * Math.Sin(angle));
                p.Add(new Vector2(x, y));

                angle += rotationStep;
                x = (externalRadius * Math.Cos(angle));
                y = (externalRadius * Math.Sin(angle));
                p.Add(new Vector2(x, y));

                x = (internalRadius * Math.Cos(angle));
                y = (internalRadius * Math.Sin(angle));
                p.Add(new Vector2(x, y));

            }
            return p.ToArray();
        }


        internal static Vector2[] GetTestClockwisePolygon(int externalSidesCount, double externalRadius, double internalRadius, 
            double angleOffset = 0, double xOffset = 0, double yOffset = 0)
        {
            var p = new List<Vector2>();
            bool addExternalFirst = false;
            bool isGear = internalRadius != 0;
            double rotationStep = -Math.PI * 2 / externalSidesCount;
            for (int sideIndex = 0; sideIndex <= externalSidesCount; sideIndex++)
            {
                double angle = (rotationStep * sideIndex) + angleOffset;
                double xExternal = externalRadius * Math.Cos(angle) + xOffset;
                double yExternal = externalRadius * Math.Sin(angle) + yOffset;
                if (isGear)
                {
                    if ((sideIndex == externalSidesCount && addExternalFirst)  || sideIndex == 0)
                    {
                        p.Add(new Vector2(xExternal, yExternal));
                    }
                    else
                    {
                        double xInternal = internalRadius * Math.Cos(angle) + xOffset;
                        double yInternal = internalRadius * Math.Sin(angle) + yOffset;
                        if (addExternalFirst)
                        {
                            p.Add(new Vector2(xExternal, yExternal));
                            p.Add(new Vector2(xInternal, yInternal));
                        }
                        else
                        {
                            p.Add(new Vector2(xInternal, yInternal));
                            p.Add(new Vector2(xExternal, yExternal));
                        }

                    }

                    addExternalFirst = !addExternalFirst;
                }
                else
                {
                    p.Add(new Vector2(xExternal, yExternal));
                }
            }
            return p.ToArray();
        }

    }
}
