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
        "../../../TestFiles/off_axis_box.STL",
        "../../../TestFiles/amf_Cube.amf",
        "../../../TestFiles/Mic_Holder_SW.stl",  
        "../../../TestFiles/Mic_Holder_JR.stl",
        "../../../TestFiles/3_bananas.amf",
        "../../../TestFiles/drillparts.amf",    
        "../../../TestFiles/wrenchsns.amf",     
        "../../../TestFiles/Rook.amf",   
        //"../../../TestFiles/trapezoid.4d.off",//breaks in OFFFileData
        //"../../../TestFiles/mushroom.off",   //breaks in OFFFileData
        "../../../TestFiles/ABF.STL",           
        "../../../TestFiles/Pump-1repair.STL",
        "../../../TestFiles/Pump-1.STL",
        "../../../TestFiles/Beam_Clean.STL",
        "../../../TestFiles/piston.stl",
        "../../../TestFiles/Z682.stl",   
        "../../../TestFiles/sth2.stl", 
        "../../../TestFiles/pump.stl", 
        "../../../TestFiles/bradley.stl",
        "../../../TestFiles/Cuboide.stl",
        "../../../TestFiles/new/5.STL",
        "../../../TestFiles/new/2.stl",
        "../../../TestFiles/new/6.stl",
        //"../../../TestFiles/new/4.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/radiobox.stl", 
        "../../../TestFiles/brace.stl",        
        //"../../../TestFiles/box.stl", //breaks in slice
        "../../../TestFiles/G0.stl",
        "../../../TestFiles/GKJ0.stl",
        //"../../../TestFiles/SCS12UU.stl", //Negative and positive loop values are identical??
        "../../../TestFiles/testblock2.stl",
        "../../../TestFiles/Z665.stl", 
        //"../../../TestFiles/Casing.stl", //breaks because one of its faces has no normal
        "../../../TestFiles/mendel_extruder.stl" 
        };

        [STAThread]
        private static void Main(string[] args)
        {
            //var writer = new TextWriterTraceListener(Console.Out);
            //Debug.Listeners.Add(writer);
            for (var i= 0; i < filenames.Count();i++)
            {
                var filename = filenames[i];
                Console.WriteLine("Attempting: " + filename);
                FileStream fileStream = File.OpenRead(filename);
                var ts = IO.Open(fileStream, filename, false);
                for (var j = 0; j < ts[0].Faces.Count(); j++ )
                {
                    var face = ts[0].Faces[j];
                    var d = Math.Abs(face.Normal[0]) + Math.Abs(face.Normal[1]) + Math.Abs(face.Normal[2]);
                    if (d.IsNegligible()) throw new Exception();
                }
                //TestClassification(ts[0]);
                //TestXSections(ts[0]);
                //TVGL_Helix_Presenter.HelixPresenter.Show(ts[0]);
                TestSlice(ts[0]);
                //TestOBB(ts[0]);
                //var filename2 = filenames[i+1];
                //FileStream fileStream2 = File.OpenRead(filename2);
                //var ts2= IO.Open(fileStream2, filename2, false);
                //TestInsideSolid(ts[0], ts2[0]);
            }
            Console.WriteLine("Completed.");
            Console.ReadKey();
        }

        //private static void TestClassification(TessellatedSolid ts)
        //{
        //    TesselationToPrimitives.Run(ts);
        //}

        private static void TestOBB(TessellatedSolid ts)
        {
            //var obb = MinimumEnclosure.Find_via_PCA_Approach(ts);
            //var obb = MinimumEnclosure.Find_via_ChanTan_AABB_Approach(ts);
            //var obb = MinimumEnclosure.Find_via_MC_ApproachOne(ts);
            //var obb = MinimumEnclosure.OrientedBoundingBox(ts);
            //var obb = MinimumEnclosure.Find_via_BM_ApproachOne(ts);
        }

        private static void TestInsideSolid(TessellatedSolid ts1, TessellatedSolid ts2)
        {
            var now = DateTime.Now;
            Console.WriteLine("start...");
            List<Vertex> insideVertices1;
            List<Vertex> outsideVertices1;
            List<Vertex> insideVertices2;
            List<Vertex> outsideVertices2;
            MiscFunctions.FindSolidIntersections(ts2, ts1, out insideVertices1, out outsideVertices1, out insideVertices2, out outsideVertices2, true);
            //var vertexInQuestion = new Vertex(new[] {0.0, 0.0, 0.0});
            //var isVertexInside = MinimumEnclosure.IsVertexInsideSolid(ts, vertexInQuestion);
            //ToDo: Run test multiple times and look for vertices that change. Get visual and determine cause of error.
            //ToDo: Also, check if boundary function works 
            Console.WriteLine("Is the Solid inside the Solid?");
            Console.WriteLine();
            Console.WriteLine("end...Time Elapsed = " + (DateTime.Now - now));
            Console.ReadLine();
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
            //try
            //{
                Slice.OnFlat(ts, new Flat(distToVLow + (length / 2), dir), out positiveSideSolids, out negativeSideSolids);
                TVGL_Helix_Presenter.HelixPresenter.Show(negativeSideSolids);
                TVGL_Helix_Presenter.HelixPresenter.Show(positiveSideSolids);
            //}
            //catch (Exception)
            //{
            //    Console.WriteLine("Failed to slice: {0}", ts.Name);
            //}

        }
    }
}