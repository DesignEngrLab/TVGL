using PclWCommon;
using PclWTestCommon;
class BenchKnown
{

    public static void TestSpeedKnown(TestableObj testObj, int intersections, int loops, double angleOffset)
    {
        int intersections4 = Fix((intersections / 4));
        int rays = Math.Floor(Math.Sqrt(intersections4));
        int cilinders = rays;
        if (((intersections4
                    - (rays * cilinders))
                    > rays))
        {
            rays = (rays + 1);
        }

        int longerRays = (intersections4
                    - (rays * cilinders));
        if ((longerRays == rays))
        {
            rays = (rays + 1);
            longerRays = 0;
        }

        const double radiusStep = 2.6;
        int internalRadius = (radiusStep
                    * (rays
                    * (Math.PI * 2)));
        double raysRadius = (internalRadius
                    + ((cilinders * radiusStep)
                    + radiusStep));
        double longerRaysRadius = (raysRadius
                    + (radiusStep * 2));
        PolygonSet psA = new PolygonSet();
        psA.Polygons.Add(GetTestClockwiseGearPolygon(rays, longerRays, raysRadius, longerRaysRadius, internalRadius));
        object oA = testObj.GetAdaptedInputFromPolygonSet(psA);
        PolygonSet psB = new PolygonSet();
        if ((longerRays > 0))
        {
            cilinders = (cilinders + 1);
        }

        for (int counter = 1; (counter <= cilinders); counter++)
        {
            psB.Polygons.Add(GetTestClockwisePolygon((rays * 2), (internalRadius
                                + ((counter * radiusStep)
                                + (radiusStep / 2))), 0));
            psB.Polygons.Add(GetTestClockwisePolygon((rays * 2), (internalRadius
                                + (counter * radiusStep)), 0));
            ReverseLastPolygon(psB);
        }

        object oB = testObj.GetAdaptedInputFromPolygonSet(psB);
        object oC = null;
        Stopwatch watch = new Stopwatch();
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

        PolygonSet psC = testObj.GetAdaptedOutputToPolygonSet(oC);
        // Dim svgWriter As New SvgWriter
        // svgWriter.AdaptViewBox = True
        // svgWriter.StrokeWidth = 3 * (Fix(longerRaysRadius / 600) + 1)
        // svgWriter.Write(psA, testObj.Id & ".KNAA" & intersections.ToString("00"))
        // svgWriter.Write(psB, testObj.Id & ".KNAB" & intersections.ToString("00"))
        // svgWriter.Write(psC, testObj.Id & ".KNAC" & intersections.ToString("00"))
        int sourcePolygonsCount = (GetPolygonsCount(psA) + GetPolygonsCount(psB));
        int sourceVerticesCount = (GetVerticesCount(psA) + GetVerticesCount(psB));
        int resultPolygonsCount = GetPolygonsCount(psC);
        int resultVerticesCount = GetVerticesCount(psC);
        string message = (string.Format("TK {0,-10} ", testObj.Id)
                    + (string.Format("l {0:0000} ", loops)
                    + (string.Format("ms {0:00000000} ", watch.ElapsedMilliseconds)
                    + (string.Format("PS {0:00000} ", sourcePolygonsCount)
                    + (string.Format("VS {0:00000000} ", sourceVerticesCount)
                    + (string.Format("PR {0:000000} ", resultPolygonsCount) + string.Format("VR {0:00000000}", resultVerticesCount)))))));
        Console.WriteLine(message);
    }
}