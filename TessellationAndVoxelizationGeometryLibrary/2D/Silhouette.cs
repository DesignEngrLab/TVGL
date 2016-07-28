using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;
using TVGL._2D.Clipper;

namespace TVGL._2D
{
    /// <summary>
    /// The outline of a solid from a particular direction.
    /// </summary>
    public static class Silhouette
    {
        /// <summary>
        /// Gets the silhouette of a solid along a given normal.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        public static List<List<Point>> Run(TessellatedSolid ts, double[] normal)
        {
            //Get the negative faces
            var negativeFaces = new List<PolygonalFace>();
            foreach (var face in ts.Faces)
            {
                var dot = normal.dotProduct(face.Normal);
                if (Math.Sign(dot) < 0)
                {
                    negativeFaces.Add(face);
                }
            }
            
            //For each negative face.
            //1. Project it onto a plane perpendicular to the given normal.
            //2. Union it with the prior negative faces.
            var startFace = negativeFaces[0];
            negativeFaces.RemoveAt(0);
            var startPolygon = MiscFunctions.Get2DProjectionPoints(startFace.Vertices, normal, false).ToList();
            //Make this polygon positive CCW
            startPolygon = CCWPositive(startPolygon);
            var polygonList = new List<List<Point>> { startPolygon };

            while (negativeFaces.Any())
            {
                var negativeFace = negativeFaces[0];
                negativeFaces.RemoveAt(0);
                var nextPolygon = MiscFunctions.Get2DProjectionPoints(negativeFace.Vertices, normal, false).ToList();
                //Make this polygon positive CCW
                nextPolygon = CCWPositive(nextPolygon);
                polygonList = Union.Run(polygonList, nextPolygon);
            }
            return polygonList;
        }

        //Sets a convex polygon to counter clock wise positive
        // It is assumed that
        // 1. the polygon is closed
        // 2. the last point is not repeated.
        // 3. the polygon is simple (does not intersect itself or have holes)
        //http://debian.fmi.uni-sofia.bg/~sergei/cgsr/docs/clockwise.htm
        private static List<Point> CCWPositive(IList<Point> p)
        {
            var polygon = new List<Point>(p);
            var n = p.Count;
            var count = 0;

            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var k = (i + 2) % n;
                var z = (p[j].X - p[i].X) * (p[k].Y - p[j].Y);
                z -= (p[j].Y - p[i].Y) * (p[k].X - p[j].X);
                if (z < 0)
                    count--;
                else if (z > 0)
                    count++;
            }
            //The polygon has a CW winding if count is negative
            if (count < 0) polygon.Reverse();
            return polygon;
        } 
    }
}
