using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using StarMathLib;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;


namespace TVGL_Test
{
    internal partial class Program
    {
        private static string[] filenames = {     
                                                "../../../TestFiles/amf_Cube.amf",
       "../../../TestFiles/Mic_Holder_SW.stl",  
          "../../../TestFiles/Mic_Holder_JR.stl",
                                               "../../../TestFiles/3_bananas.amf",    
                                             //  "../../../TestFiles/drillparts.amf",    
                                             //    "../../../TestFiles/wrenchsns.amf",     
                                                "../../../TestFiles/Rook.amf",   
      //  "../../../TestFiles/trapezoid.4d.off",
       //      "../../../TestFiles/mushroom.off",   
           "../../../TestFiles/ABF.STL",           
          "../../../TestFiles/Pump-1repair.STL",
          "../../../TestFiles/Pump-1.STL",
          "../../../TestFiles/Beam_Clean.STL",
        "../../../TestFiles/piston.stl",
        "../../../TestFiles/Z682.stl",   
    //    "../../../TestFiles/85408.stl",
        "../../../TestFiles/sth2.stl",
           "../../../TestFiles/pump.stl", 
        "../../../TestFiles/bradley.stl",
      //  "../../../TestFiles/45.stl",
        "../../../TestFiles/Cuboide.stl",
        "../../../TestFiles/new/5.STL",
         "../../../TestFiles/new/2.stl",
        "../../../TestFiles/new/6.stl",
        "../../../TestFiles/new/4.stl",
       "../../../TestFiles/radiobox.stl",
        "../../../TestFiles/brace.stl",        
        "../../../TestFiles/box.stl",
        "../../../TestFiles/G0.stl",
        "../../../TestFiles/GKJ0.stl",
        "../../../TestFiles/SCS12UU.stl",
        "../../../TestFiles/testblock2.stl",
        "../../../TestFiles/Z665.stl",
        "../../../TestFiles/Casing.stl",
        "../../../TestFiles/mendel_extruder.stl"
        };

        [STAThread]
        private static void Main(string[] args)
        {
            //var writer = new TextWriterTraceListener(Console.Out);
            //Debug.Listeners.Add(writer);
            foreach (var filename in filenames)
            {
                FileStream fileStream = File.OpenRead(filename);
                var ts = IO.Open(fileStream, filename, false);
                //TestClassification(ts[0]);
                //TestXSections(ts[0]);
                //TVGL_Helix_Presenter.HelixPresenter.Show(ts[0]);
                // MinimumEnclosure.Find_via_ContinuousPCA_Approach(ts[0]);
                TestSlice(ts[0]);
                //TestOBB(ts[0]);       

            }
            Console.ReadKey();
        }

        //private static void TestClassification(TessellatedSolid ts)
        //{
        //    TesselationToPrimitives.Run(ts);
        //}

        private static void TestOBB(TessellatedSolid ts)
        {
            var obb = MinimumEnclosure.OrientedBoundingBox(ts);
        }

        private static void TestXSections(TessellatedSolid ts)
        {
            var now = DateTime.Now;
            Debug.WriteLine("start...");
            var crossAreas = new double[3][,];
            var maxSlices = 100;
            var delta = Math.Max((ts.Bounds[1][0] - ts.Bounds[0][0]) / maxSlices,
                Math.Max((ts.Bounds[1][1] - ts.Bounds[0][1]) / maxSlices,
                    (ts.Bounds[1][2] - ts.Bounds[0][2]) / maxSlices));
            //Parallel.For(0, 3, i =>
            for (int i = 0; i < 3; i++)
            {
                //var max = ts.Bounds[1][i];
                //var min = ts.Bounds[0][i];
                //var numSteps = (int)Math.Ceiling((max - min) / delta);
                var coordValues = ts.Vertices.Select(v => v.Position[i]).Distinct().OrderBy(x => x).ToList();
                var numSteps = coordValues.Count;
                var direction = new double[3];
                direction[i] = 1.0;
                crossAreas[i] = new double[numSteps, 2];
                for (var j = 0; j < numSteps; j++)
                {
                    var dist = crossAreas[i][j, 0] = coordValues[j];
                    //Console.WriteLine("slice at Coord " + i + " at " + coordValues[j]);
                    crossAreas[i][j, 1] = Slice.DefineContact(new Flat(dist, direction), ts, false).Area;
                }
            }//);
            Debug.WriteLine("end...Time Elapsed = " + (DateTime.Now - now));

            //Console.ReadKey();
            //for (var i = 0; i < 3; i++)
            //{
            //    Debug.WriteLine("\nfor direction " + i);
            //    for (var j = 0; j < crossAreas[i].GetLength(0); j++)
            //    {
            //        Debug.WriteLine(crossAreas[i][j, 0] + ", " + crossAreas[i][j, 1]);
            //    }
            //}
        }
        private static void TestSlice(TessellatedSolid ts)
        {
            //var a= ContactData.Divide(new Flat { DistanceToOrigin = 40 , Normal = new []{0,1.0,0} }, ts).Area;
            //                          Debug.WriteLine(a);
            //Console.ReadKey();
            //return;
            var now = DateTime.Now;
            Debug.WriteLine("start...");
            var dir = new[] { 1.0, 0, 0 };
            dir.normalize();
            Vertex vLow, vHigh;
            List<TessellatedSolid> positiveSideSolids, negativeSideSolids;
            var length = MinimumEnclosure.GetLengthAndExtremeVertices(dir, ts.Vertices, out vLow, out vHigh);
            var distToVLow = vLow.Position.dotProduct(dir);
            try
            {
                Slice.OnFlat(ts, new Flat(distToVLow + (length / 2), dir), out positiveSideSolids, out negativeSideSolids);
                TVGL_Helix_Presenter.HelixPresenter.Show(negativeSideSolids);
                TVGL_Helix_Presenter.HelixPresenter.Show(positiveSideSolids);
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to slice: {0}", ts.Name);
            }

        }
    }
}