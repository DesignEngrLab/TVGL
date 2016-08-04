using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
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
            startPolygon = PolygonOperations.CCWPositive(startPolygon);
            var polygonList = new List<List<Point>> { startPolygon };

            while (negativeFaces.Any())
            {
                var negativeFace = negativeFaces[0];
                negativeFaces.RemoveAt(0);
                var nextPolygon = MiscFunctions.Get2DProjectionPoints(negativeFace.Vertices, normal, false).ToList();
                //Make this polygon positive CCW
                nextPolygon = PolygonOperations.CCWPositive(nextPolygon);
                polygonList = PolygonOperations.Union(polygonList, nextPolygon);
            }

            
            var smallestX = double.PositiveInfinity;
            var largestX = double.NegativeInfinity;
            foreach (var path in polygonList)
            {
                foreach (var point in path)
                {
                    if (point.X < smallestX)
                    {
                        smallestX = point.X;
                    }
                    if (point.X > largestX)
                    {
                        largestX = point.X;
                    }
                }
            }
            var scale = largestX - smallestX;

            var offsetPolygons = PolygonOperations.OffsetRound(polygonList, scale/10);
            polygonList.AddRange(offsetPolygons);

            return polygonList;
        }
    }
}
