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
    public class BenchGrid
    {

        public enum ModeType
        {

            None,

            IntersectionOff,

            IntersectionOn,
        }

        private PolygonSet _PolygonSetA;

        private PolygonSet _PolygonSetB;

        public int HolesForRow
        {
        }
    }
while (((y + innerStep)
            < halfSideLength))
{
    coordinates = new double[] {
            (x + o),
            (y + o),
            ((x - o)
                        + innerStep),
            (y + o),
            ((x - o)
                        + innerStep),
            ((y - o)
                        + innerStep),
            (x + o),
            ((y - o)
                        + innerStep)};
polygonSet.Polygons.Add(new Polygon(coordinates));
y = (y
            + (innerStep * 2));
}

x = (x
            + (innerStep * 2));
LoopEndSubEndclass Unknown
{
}


private void SetPolygonSet(ref PolygonSet polygonSet, bool avoidIntersections, int pctIntersections)
{
    double halfSideLength = (_SideLengthOuter / 2);
    double[] coordinates;
    (halfSideLength * -1);
    (halfSideLength * -1);
    (halfSideLength * -1);
    halfSideLength;
    halfSideLength;
    halfSideLength;
    halfSideLength;
    (halfSideLength * -1);
    polygonSet = new PolygonSet();
    polygonSet.Polygons.Add(new Polygon(coordinates));
    double innerStep = (_SideLengthOuter / _HolesForRow);
    double holeSide = (innerStep * 0.375);
    int intersectionCounter = ((_HolesForRow | 2)
                * (pctIntersections / 100));
    // TODO: Warning!!! The operator should be an XOR ^ instead of an OR, but not available in CodeDOM
    for (int xCounter = 1; (xCounter <= _HolesForRow); xCounter++)
    {
        double x = ((halfSideLength * -1)
                    + (innerStep
                    * (xCounter - 1)));
        for (int yCounter = 1; (yCounter <= _HolesForRow); yCounter++)
        {
            double y = ((halfSideLength * -1)
                        + (innerStep
                        * (yCounter - 1)));
            double offset = (innerStep * 0.1);
            if (!avoidIntersections)
            {
                if ((intersectionCounter <= 0))
                {
                    offset = ((offset * 2)
                                + holeSide);
                }
                else
                {
                    offset = (offset
                                + (holeSide * 0.5));
                }

            }

            coordinates = new double[] {
                        (x + offset),
                        (y + offset),
                        (x
                                    + (offset + holeSide)),
                        (y + offset),
                        (x
                                    + (offset + holeSide)),
                        (y
                                    + (offset + holeSide)),
                        (x + offset),
                        (y
                                    + (offset + holeSide))};
            polygonSet.Polygons.Add(new Polygon(coordinates));
            intersectionCounter = (intersectionCounter - 1);
        }

    }

}

public void SetPolygonSets()
{
    SetPolygonSet(_PolygonSetA, ModeType.None);
    SetPolygonSet(_PolygonSetB, _Mode);
}

public void SetPolygonSets(int pctIntersections)
{
    SetPolygonSet(_PolygonSetA, true, 0);
    SetPolygonSet(_PolygonSetB, false, pctIntersections);
}

public void TestSpeedGrid(TestableObj testObj, int loops)
{
    object oA = testObj.GetAdaptedInputFromPolygonSet(_PolygonSetA);
    object oB = testObj.GetAdaptedInputFromPolygonSet(_PolygonSetB);
    object oC = null;
    Stopwatch watch = new Stopwatch();
    try
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        // Threading.Thread.Sleep(1000)
        watch.Start();
        for (int i = 1; (i <= loops); i++)
        {
            oC = testObj.GetIntersection(oA, oB);
        }

        watch.Stop();
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        if ((oA.GetType() == Region))
        {
            oC = new Region();
        }
        else
        {
            oC = new PolygonSet();
        }

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
    // svgWriter.StrokeWidth = _SideLengthOuter / 300
    // svgWriter.Write(psA, testObj.Id & ".GRDA")
    // svgWriter.Write(psB, testObj.Id & ".GRDB")
    // svgWriter.Write(psC, testObj.Id & ".GRDC")
    int sourcePolygonsCount = (GetPolygonsCount(psA) + GetPolygonsCount(psB));
    int sourceVerticesCount = (GetVerticesCount(psA) + GetVerticesCount(psB));
    int resultPolygonsCount = GetPolygonsCount(psC);
    int resultVerticesCount = GetVerticesCount(psC);
    string message = (string.Format("TG {0,-10} ", testObj.Id)
                + (string.Format("l {0:0000} ", loops)
                + (string.Format("ms {0:00000000} ", watch.ElapsedMilliseconds)
                + (string.Format("PS {0:00000} ", sourcePolygonsCount)
                + (string.Format("VS {0:00000000} ", sourceVerticesCount)
                + (string.Format("PR {0:000000} ", resultPolygonsCount) + string.Format("VR {0:00000000}", resultVerticesCount)))))));
    Console.WriteLine(message);
}
}
