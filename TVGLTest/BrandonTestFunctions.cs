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
        private static void Main2(string[] args)
        {
            //  var position = new double[] { 0, 0, 0 };
            //   var vertex1 = new Vertex(position);
            //   var point1 = new Point(vertex1);
            var point0 = new Point(new Vertex(new[] { 0.0, 0.0, 0.0 }));         
            var point1 = new Point(new Vertex(new[]{ 1.0, 0.25, 0.0 }));
            var point2 = new Point(new Vertex(new[]{ 0.75, 0.5, 0.0}));
            var point3 = new Point(new Vertex(new[]{ 0.6, 0.4, 0.0  }));
            var point4 = new Point(new Vertex(new[]{ 0.4, 0.6, 0.0  }));
            var point5 = new Point(new Vertex(new[]{0.2, 0.1, 0.0}));

            var posLoop = new Point[] { point0, point1, point2, point3, point4, point5};
            var listPoints = new List<Point[]> { posLoop };
            var listBool = new Boolean[] { true };

            var listTriangles = TriangulatePolygon.Run(listPoints, listBool);


            //Print Triangles to Console
            var i = 1;
            Console.WriteLine("New Triangles");
            foreach (var triangle in listTriangles)
            {
                Console.WriteLine("Triangle: " + i);
                Console.WriteLine("(" + (triangle.Vertices[0].X) + " , " + (triangle.Vertices[0].Y) + ")");
                Console.WriteLine("(" + (triangle.Vertices[1].X) + " , " + (triangle.Vertices[1].Y) + ")");
                Console.WriteLine("(" + (triangle.Vertices[2].X) + " , " + (triangle.Vertices[2].Y) + ")");
                i++;
            }

            Console.ReadLine();

        }//End TesselatePolygons
    }//End TestFunction
}//End Namespace