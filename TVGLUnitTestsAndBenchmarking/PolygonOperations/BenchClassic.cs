using BenchmarkDotNet.Attributes;
using OldTVGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL;
using TVGL.Numerics;
using TVGL.TwoDimensional;
using TVGL.Voxelization;

namespace TVGLUnitTestsAndBenchmarking
{
    class BenchClassic
    {

        // NOTE: 
        // - Polygons used to test PolyBoolean required.
        // - Polygons are scaled down for PolyBoolean.c20 limits.
        private Polygon _PolygonSetA;

        private Polygon _PolygonSetB;

        public Polygon LoadWlrPolygonSet(string fileFullName, double scale)
        {
            try
            {
                IO.StreamReader reader = new IO.StreamReader(fileFullName);
                string line = reader.ReadLine;
                if ((int.Parse(line) != 1))
                {
                    return null;
                }

                line = reader.ReadLine;
                int polygonCount = int.Parse(line);
                PolygonSet polygonSet = new PolygonSet();
                while ((polygonCount > 0))
                {
                    line = reader.ReadLine;
                    int verticesCount = int.Parse(line);
                    Polygon polygon = new Polygon();
                    while ((verticesCount > 0))
                    {
                        line = reader.ReadLine;
                        string[] parts = line.Split(",");
                        Vertex vertex = new Vertex((int.Parse(parts[0]) * scale), (int.Parse(parts[1]) * scale));
                        polygon.Vertices.Add(vertex);
                        verticesCount = (verticesCount - 1);
                    }

                    polygonSet.Polygons.Add(polygon);
                    if ((polygonSet.Polygons.Count == 1))
                    {
                        if (!polygon.IsClockwise)
                        {
                            polygon.Reverse();
                        }

                    }
                    else if (polygon.IsClockwise)
                    {
                        polygon.Reverse();
                    }

                    polygonCount = (polygonCount - 1);
                }

                return polygonSet;
            }
            catch (Exception ex)
            {
                return null;
            }

        }

        private void SetPolygonSets(double scale, void =, void 1)
        {
            ((double)(lastScale)) = Double.NaN;
            // Warning!!! Optional parameters not supported
            if (((lastScale == scale)
                        && _PolygonSetA))
            {
                IsNot;
                (null && _PolygonSetB);
                IsNot;
                null;
                return;
            }

            lastScale = scale;
            _PolygonSetA = this.LoadWlrPolygonSet(IO.Path.Combine(Environment.CurrentDirectory, "s.wlr"), scale);
            _PolygonSetB = this.LoadWlrPolygonSet(IO.Path.Combine(Environment.CurrentDirectory, "c.wlr"), scale);
        }

        public void TestSpeedClassic(TestableObj testObj, int loops, double scale)
        {
            this.SetPolygonSets(scale);
            if (((_PolygonSetA == null)
                        || (_PolygonSetB == null)))
            {
                return;
            }

            object oA = testObj.GetAdaptedInputFromPolygonSet(_PolygonSetA);
            object oB = testObj.GetAdaptedInputFromPolygonSet(_PolygonSetB);
            Stopwatch watch = new Stopwatch();
            object oC = null;
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                // Threading.Thread.Sleep(1000)
                watch.Start();
                for (int k = 1; (k <= loops); k++)
                {
                    oC = testObj.GetIntersection(oA, oB);
                }

                watch.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }

            PolygonSet psA = testObj.GetAdaptedOutputToPolygonSet(oA);
            PolygonSet psB = testObj.GetAdaptedOutputToPolygonSet(oB);
            PolygonSet psC = testObj.GetAdaptedOutputToPolygonSet(oC);
            // Dim svgWriter As New SvgWriter
            // svgWriter.AdaptViewBox = True
            // Dim viewPort As SvgWriter.ViewportDefinition
            // viewPort.HeightCm = 20
            // viewPort.WidthCm = 20
            // svgWriter.Viewport = viewPort
            // svgWriter.StrokeWidth = 20
            // svgWriter.Write(psA, testObj.Id & ".CLSA")
            // svgWriter.Write(psB, testObj.Id & ".CLSB")
            // svgWriter.Write(psC, testObj.Id & ".CLSC")
            int sourcePolygonsCount = (GetPolygonsCount(psA) + GetPolygonsCount(psB));
            int sourceVerticesCount = (GetVerticesCount(psA) + GetVerticesCount(psB));
            int resultPolygonsCount = GetPolygonsCount(psC);
            int resultVerticesCount = GetVerticesCount(psC);
            string message = (string.Format("TC {0,-10} ", testObj.Id)
                        + (string.Format("l {0:0000} ", loops)
                        + (string.Format("ms {0:00000000} ", watch.ElapsedMilliseconds)
                        + (string.Format("PS {0:00000} ", sourcePolygonsCount)
                        + (string.Format("VS {0:00000000} ", sourceVerticesCount)
                        + (string.Format("PR {0:000000} ", resultPolygonsCount) + string.Format("VR {0:00000000}", resultVerticesCount)))))));
            Console.WriteLine(message);
        }
    } 
}