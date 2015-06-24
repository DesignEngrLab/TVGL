using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL;
using TVGL.Boolean_Operations;
using TVGL.IOFunctions;
using TVGL.Miscellaneous_Functions.TriangulatePolygon;
using TVGL.Tessellation;


namespace TVGL_Test
{
    internal partial class Program
    {

        [STAThread]
        private static void Main(string[] args)
        {
            //  var position = new double[] { 0, 0, 0 };
            //   var vertex1 = new Vertex(position);
            //   var point1 = new Point(vertex1);
            var point1 = new Point(new Vertex(new[] { 0.0, 0, 0 }));
          var  position = new double[] { 1, 0.25, 0 };
            var vertex2 = new Vertex(position);
            var point2 = new Point(vertex2);

            position = new double[] { 0.75, 0.5, 0 };
            var vertex3 = new Vertex(position);
            var point3 = new Point(vertex3);

            position = new double[] { 0.6, 0.4, 0 };
            var vertex4 = new Vertex(position);
            var point4 = new Point(vertex4);

            position = new double[] { 0.4, 0.6, 0 };
            var vertex5 = new Vertex(position);
            var point5 = new Point(vertex5);

            position = new double[] { .2, .1, 0 };
            var vertex6 = new Vertex(position);
            var point6 = new Point(vertex6);

            var posLoop = new Point[] { point1, point2, point3, point4, point5, point6 };
            var listPoints = new List<Point[]> { posLoop };
            var listBool = new Boolean[] { true };

            var listTriangles = TriangulatePolygon.Run(listPoints, listBool);


            //Print Triangles to Console
            var i = 1;
            Console.WriteLine("New Triangles");
            foreach (var triangle in listTriangles)
            {
                Console.WriteLine("Triangle: " + i);
                Console.WriteLine("(" + (triangle[0].X) + " , " + (triangle[0].Y) + ")");
                Console.WriteLine("(" + (triangle[1].X) + " , " + (triangle[1].Y) + ")");
                Console.WriteLine("(" + (triangle[2].X) + " , " + (triangle[2].Y) + ")");
                i++;
            }

            Console.ReadLine();

        }//End TesselatePolygons
    }//End TestFunction
}//End Namespace