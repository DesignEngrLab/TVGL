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

                //Check if this polygon is legitimate. If any of the points are the same, then skip.
                if (nextPolygon[0].Equals(nextPolygon[1]) || nextPolygon[0].Equals(nextPolygon[2]) ||
                    nextPolygon[1].Equals(nextPolygon[2])) continue;
                if(MiscFunctions.AreaOfPolygon(nextPolygon).IsNegligible()) continue;

                //Make this polygon positive CCW
                nextPolygon = PolygonOperations.CCWPositive(nextPolygon);
                polygonList = PolygonOperations.Union(polygonList, nextPolygon);
            }   
            var polygons = polygonList.Select(path => new Polygon(path)).ToList();

            //Get the minimum line length to use for the offset.
            var minLength = double.PositiveInfinity;
            var totalLength = 0.0;
            var maxArea = double.NegativeInfinity;
            foreach (var polygon in polygons)
            {
                foreach (var line in polygon.PathLines)
                {
                    if (line.Length < minLength)
                    {
                        minLength = line.Length;
                    }
                    totalLength += line.Length;
                }
                if (polygon.Area > maxArea)
                {
                    maxArea = polygon.Area;
                }
            }
            
            //Remove tiny polygons.
            var count = 0;
            for(var i =0; i < polygons.Count; i++)
            {
                if (!polygons[i].Area.IsNegligible(maxArea/10000)) continue;
                polygonList.RemoveAt(i-count);
                count ++;
            }

            #region Offset Testing 
            //var smallestX = double.PositiveInfinity;
            //var largestX = double.NegativeInfinity;
            //foreach (var path in polygonList)
            //{
            //    foreach (var point in path)
            //    {
            //        if (point.X < smallestX)
            //        {
            //            smallestX = point.X;
            //        }
            //        if (point.X > largestX)
            //        {
            //            largestX = point.X;
            //        }
            //    }
            //}
            //var scale = largestX - smallestX;

            //var offsetPolygons = PolygonOperations.OffsetRound(polygonList, totalLength/1000, scale/10);
            //polygonList.AddRange(offsetPolygons);
            #endregion

            return polygonList;
        }
    }
}
