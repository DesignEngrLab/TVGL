using System;
using System.Collections.Generic;
using ClipperLib;

namespace TVGL
{
    public static class Offset
    {
        /// <summary>
        ///     Triangulates a Polygon into faces.
        /// </summary>
        /// <param name="points2D">The 2D points represented in loops.</param>
        /// <param name="isPositive">Indicates whether the corresponding loop is positive or not.</param>
        /// <returns>List&lt;Point[]&gt;, which represents vertices of new faces.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static List<Vertex[]> Run(List<Point[]> points2D)
        {
            //Create return variable
            var offsetLoops = new List<Vertex[]>();

            //Create IntPoints for use in ClipperLib
            double scale = 100; //1E+6
            var paths = new List<List<IntPoint>>();
            var offset = new ClipperOffset();
            foreach (var loop in points2D)
            {
                var path = new List<IntPoint>();
                foreach (var point in loop)
                {
                    path.Add(new IntPoint(ScaleToInt(point.X, scale),ScaleToInt(point.Y,scale)));
                    offset.AddPath(path, JoinType.jtRound, EndType.etClosedPolygon);
                }
                paths.Add(path);
            }
            var solution = new List<List<IntPoint>>();
            offset.Execute(ref solution, -10);
            
            //Convert back to Double
            foreach (var path in solution)
            {
                var i = 0;
                var loop = new Vertex[path.Count];
                foreach (var intPoint in path)
                {
                    loop[i] = ConvertToVertex(intPoint, scale);
                    i++;
                }
                offsetLoops.Add(loop);
            }

            return offsetLoops;
        }



        public static int ScaleToInt(double value, double scale)
        {
            return Convert.ToInt32(Math.Round(value * scale));
        }

        public static Vertex ConvertToVertex(IntPoint intPoint, double scale)
        {
            double X = intPoint.X;
            double Xd = X; //* (1 / scale);
            double Y = intPoint.Y;
            double Yd = Y; // *(1 / scale);
            var v = new Vertex(new[] {Xd,Yd,0});
            return v;
        }


    }
}
