using System;
using System.Collections.Generic;
using TVGL;
using TVGL.Miscellaneous_Functions.TriangulatePolygon;
using TVGL.Tessellation;


namespace TVGL_Test
{
    internal partial class Program
    {

        [STAThread]
        private static void Main2(string[] args)
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { -0.1, -0.1, 0.0 }));         
            var point1 = new Point(new Vertex(new[]{ 1.0, 0.25, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 0.4, 1.2, 0.0 }));
            var point3 = new Point(new Vertex(new[]{ 0.75, 0.5, 0.0}));
            var point4 = new Point(new Vertex(new[]{ 0.6, 0.4, 0.0  }));
            var point5 = new Point(new Vertex(new[]{ 0.4, 0.6, 0.0  }));
            var point6 = new Point(new Vertex(new[]{0.2, 0.1, 0.0}));
            var point7 = new Point(new Vertex(new[] { 0.3, 1.2, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 0.2, 1.4, 0.0 }));
            var point9 = new Point(new Vertex(new[] { 0.2, 0.4, 0.0 }));
            var point10 = new Point(new Vertex(new[] { -0.1, 1.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8, point9, point10};

            //Clockwise ordered negative loop inside positive loop
            var point11 = new Point(new Vertex(new[] { 0.4, 0.2, 0.0 }));
            var point12 = new Point(new Vertex(new[] { 0.3, 0.3, 0.0 }));
            var point13 = new Point(new Vertex(new[] { 0.6, 0.25, 0.0 }));
            var negLoop1 = new Point[] { point11, point12, point13 };

            //2nd Clockwise ordered negative loop inside positive loop
            var point14 = new Point(new Vertex(new[] { 0.1, 0.5, 0.0 }));
            var point15 = new Point(new Vertex(new[] { 0.2, 0.2, 0.0 }));
            var point16 = new Point(new Vertex(new[] { 0.1, 0.2, 0.0 }));
            var point17 = new Point(new Vertex(new[] { 0.1, 0.4, 0.0 }));
            var negLoop2 = new Point[] { point14, point15, point16, point17 };

            //2nd Counterclockwise ordered positive loop
            var point18 = new Point(new Vertex(new[] { 0.3, 0.4, 0.0 }));
            var point19 = new Point(new Vertex(new[] { 0.4, 0.8, 0.0 }));
            var point20 = new Point(new Vertex(new[] { 0.3, 1.6, 0.0 }));
            var point21 = new Point(new Vertex(new[] { 0.375, 0.75, 0.0 }));
            var posLoop2 = new Point[] { point18, point19, point20, point21};

            //Add loops to a list of loops
            var listPoints = new List<Point[]> { posLoop1, negLoop1, posLoop2, negLoop2};
            var listBool = new Boolean[] { true,false, true, false };

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