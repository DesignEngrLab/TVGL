using System;
using System.Collections.Generic;
using TVGL;


namespace TVGL_Test
{
    internal partial class Program
    {

        [STAThread]
        private static void Main2(string[] args)
        {
            Test9();
        }

        private static void Test1()
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
                Console.WriteLine("(" + (triangle[0].X) + " , " + (triangle[0].Y) + ")");
                Console.WriteLine("(" + (triangle[1].X) + " , " + (triangle[1].Y) + ")");
                Console.WriteLine("(" + (triangle[2].X) + " , " + (triangle[2].Y) + ")");
                i++;
            }

            Console.ReadLine();

        }//End TestFunction

        private static void Test2()
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { -1.0, -1.0, 0.0 }));         
            var point1 = new Point(new Vertex(new[]{ -0.5, -1.5, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 0.0, -2.0, 0.0 }));
            var point3 = new Point(new Vertex(new[]{ 0.0, 0.0, 0.0}));
            var point4 = new Point(new Vertex(new[]{ 0.001, -2.001, 0.0  }));
            var point5 = new Point(new Vertex(new[]{ -1.0, -2.0, 0.0  }));
            var point6 = new Point(new Vertex(new[] { 0.001, -2.002, 0.0 }));
            var point7 = new Point(new Vertex(new[] { 0.3, 1.2, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 0.2, 1.2, 0.0 }));
            var point9 = new Point(new Vertex(new[] { 0.2, 0.4, 0.0 }));
            var point10 = new Point(new Vertex(new[] { -0.1, 0.4, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8, point9, point10};

            //Clockwise ordered negative loop inside positive loop
            var point11 = new Point(new Vertex(new[] { -0.99, -1.0, 0.0 }));
            var point12 = new Point(new Vertex(new[] { -.001, 0.0, 0.0 }));    
            var point13 = new Point(new Vertex(new[] {-0.2, -1.0, 0.0 }));
            var negLoop1 = new Point[] { point11, point12, point13 };

            //Add loops to a list of loops
            var listPoints = new List<Point[]> { posLoop1, negLoop1};
            var listBool = new Boolean[] { true,false };

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

        }//End TestFunction

        private static void Test3() //ClipperOffset
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { -0.1, -0.1, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 1.0, 0.25, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 0.4, 1.2, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 0.75, 0.5, 0.0 }));
            var point4 = new Point(new Vertex(new[] { 0.6, 0.4, 0.0 }));
            var point5 = new Point(new Vertex(new[] { 0.4, 0.6, 0.0 }));
            var point6 = new Point(new Vertex(new[] { 0.2, 0.1, 0.0 }));
            var point7 = new Point(new Vertex(new[] { 0.3, 1.2, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 0.2, 1.4, 0.0 }));
            var point9 = new Point(new Vertex(new[] { 0.2, 0.4, 0.0 }));
            var point10 = new Point(new Vertex(new[] { -0.1, 1.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8, point9, point10 };


            //Add loops to a list of loops
            var listPoints = new List<Point[]> { posLoop1 };
            var offsets = TVGL.Offset.Run(listPoints);


            //Print Triangles to Console
            var i = 1;
            Console.WriteLine("New Loops");
            foreach (var Loop in offsets)
            {
                Console.WriteLine("Triangle: " + i);           
                foreach (var vertex in Loop)
                {
                    Console.WriteLine("(" + (vertex.X) + " , " + (vertex.Y) + ")");
                }
                i++;
            }

            Console.ReadLine();

        }//End TestFunction

        private static void Test4() //Bounding Rectangle
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { -0.1, -0.1, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 1.0, 0.25, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 0.4, 1.2, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 0.75, 0.5, 0.0 }));
            var point4 = new Point(new Vertex(new[] { 0.6, 0.4, 0.0 }));
            var point5 = new Point(new Vertex(new[] { 0.4, 0.6, 0.0 }));
            var point6 = new Point(new Vertex(new[] { 0.2, 0.1, 0.0 }));
            var point7 = new Point(new Vertex(new[] { 0.3, 1.2, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 0.2, 1.4, 0.0 }));
            var point9 = new Point(new Vertex(new[] { 0.2, 0.4, 0.0 }));
            var point10 = new Point(new Vertex(new[] { -0.1, 1.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8, point9, point10 };


            //Add loops to a list of loops
            var boundingRectangle = TVGL.MinimumEnclosure.RotatingCalipers2DMethod(posLoop1);
            Console.WriteLine("Best Angle for Bounding Box:");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle, 3) + " radians");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle * 180 / Math.PI, 3) + " degrees (Clockwise rotation of left caliper,");
            Console.WriteLine("which could now look like its on top.)");
            Console.WriteLine();
            Console.WriteLine("Minimum Bounding Area:");
            Console.WriteLine(boundingRectangle.Area);
            Console.ReadLine();
        }//End TestFunction


        private static void Test5() //Bounding Rectangle
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { 0.0, 0.0, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 2.0, 0.0, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 6.0, 1.0, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 9.0, 3.0, 0.0 }));
            var point4 = new Point(new Vertex(new[] { 8.0, 5.0, 0.0 }));
            var point5 = new Point(new Vertex(new[] { 5.0, 4.5, 0.0 }));
            var point6 = new Point(new Vertex(new[] { 3.0, 3.4, 0.0 }));
            var point7 = new Point(new Vertex(new[] { 2.0, 2.5, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 1.0, 1.5, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8 };



            //Add loops to a list of loops
            var boundingRectangle = TVGL.MinimumEnclosure.RotatingCalipers2DMethod(posLoop1);
            Console.WriteLine("Best Angle for Bounding Box:");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle, 3) + " radians");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle * 180 / Math.PI, 3) + " degrees (Clockwise rotation of left caliper,");
            Console.WriteLine("which could now look like its on top.)");
            Console.WriteLine();
            Console.WriteLine("Minimum Bounding Area:");
            Console.WriteLine(boundingRectangle.Area);
            Console.ReadLine();
        }//End TestFunction

        private static void Test6() //Bounding Rectangle: Simple Box
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { 0.0, 0.0, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 1.0, 0.0, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 1.0, 1.0, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 0.0, 1.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3};



            //Add loops to a list of loops
            var boundingRectangle = TVGL.MinimumEnclosure.RotatingCalipers2DMethod(posLoop1);
            Console.WriteLine("Best Angle for Bounding Box:");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle, 3) + " radians");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle * 180 / Math.PI, 3) + " degrees (Clockwise rotation of left caliper,");
            Console.WriteLine("which could now look like its on top.)");
            Console.WriteLine();
            Console.WriteLine("Minimum Bounding Area:");
            Console.WriteLine(boundingRectangle.Area);
            Console.ReadLine();
        }//End TestFunction

        private static void Test7() //Bounding Rectangle: Simple Triangle
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { 1.25, 1.0, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 0.0, 0.75, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 1.0, 0.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2 };



            //Add loops to a list of loops
            var boundingRectangle = TVGL.MinimumEnclosure.RotatingCalipers2DMethod(posLoop1);
            Console.WriteLine("Best Angle for Bounding Box:");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle, 3) + " radians");
            Console.WriteLine(Math.Round(boundingRectangle.BestAngle * 180 / Math.PI, 3) + " degrees (Clockwise rotation of left caliper,");
            Console.WriteLine("which could now look like its on top.)");
            Console.WriteLine();
            Console.WriteLine("Minimum Bounding Area:");
            Console.WriteLine(boundingRectangle.Area);
            Console.ReadLine();
        }//End TestFunction

        private static void Test8() //Bounding Circle
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { 0.0, 0.0, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 1.0, 0.0, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 1.0, 1.0, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 0.0, 1.0, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3 };
            Point center;
            double radius;



            //Add loops to a list of loops
            var minArea = TVGL.MinimumEnclosure.MinimumCircle(new List<Point> (posLoop1), out center, out radius);
            Console.WriteLine("Minimum Area Circle:");
            Console.WriteLine(Math.Round(minArea, 3));
            Console.WriteLine();
            Console.WriteLine("Radius:");
            Console.WriteLine(Math.Round(radius,3));
            Console.WriteLine();
            Console.WriteLine("Center:");
            Console.WriteLine("(" + Math.Round(center.X,3) + "," + Math.Round(center.Y,3) + ")");
            Console.ReadLine();
        }//End TestFunction

        private static void Test9() //Bounding Circle
        {
            //Counterclockwise ordered positive loop
            var point0 = new Point(new Vertex(new[] { 0.0, 0.0, 0.0 }));
            var point1 = new Point(new Vertex(new[] { 2.0, 0.0, 0.0 }));
            var point2 = new Point(new Vertex(new[] { 6.0, 1.0, 0.0 }));
            var point3 = new Point(new Vertex(new[] { 9.0, 3.0, 0.0 }));
            var point4 = new Point(new Vertex(new[] { 8.0, 5.0, 0.0 }));
            var point5 = new Point(new Vertex(new[] { 5.0, 4.5, 0.0 }));
            var point6 = new Point(new Vertex(new[] { 3.0, 3.4, 0.0 }));
            var point7 = new Point(new Vertex(new[] { 2.0, 2.5, 0.0 }));
            var point8 = new Point(new Vertex(new[] { 1.0, 1.5, 0.0 }));
            var posLoop1 = new Point[] { point0, point1, point2, point3, point4, point5, point6, point7, point8 };
            Point center;
            double radius;



            //Add loops to a list of loops
            var minArea = TVGL.MinimumEnclosure.MinimumCircle(new List<Point>(posLoop1), out center, out radius);
            Console.WriteLine("Minimum Area Circle:");
            Console.WriteLine(Math.Round(minArea, 3));
            Console.WriteLine();
            Console.WriteLine("Radius:");
            Console.WriteLine(Math.Round(radius, 3));
            Console.WriteLine();
            Console.WriteLine("Center:");
            Console.WriteLine("(" + Math.Round(center.X, 3) + "," + Math.Round(center.Y, 3) + ")");
            Console.ReadLine();
        }//End TestFunction
    }

}//End Namespace