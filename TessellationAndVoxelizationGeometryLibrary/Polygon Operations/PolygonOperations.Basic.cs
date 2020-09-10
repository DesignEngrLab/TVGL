using Priority_Queue;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Gets the perimeter for a 2D set of points. Consider using Polygon class when possible.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Perimeter(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return paths.Sum(path => path.Perimeter());
        }
        /// <summary>
        /// Gets the perimeter for a 2D set of points. Consider using Polygon class when possible.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Perimeter(this IEnumerable<Vector2> polygon)
        {
            var perimeter = 0.0;
            var firstpass = true;
            var firstPoint = Vector2.Null;
            var prevPoint = Vector2.Null;
            foreach (var currentPt in polygon)
            {
                if (firstpass)
                {
                    firstpass = false;
                    firstPoint = prevPoint = currentPt;
                }
                else
                {
                    perimeter += Vector2.Distance(prevPoint, currentPt);
                    prevPoint = currentPt;
                }
            }
            perimeter += Vector2.Distance(prevPoint, firstPoint);

            return perimeter;
        }


        /// <summary>
        ///     Calculate the area for a 2D set of points. Consider using Polygon class when possible.
        /// </summary>
        public static double Area(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return paths.Sum(path => path.Area());
        }


        /// <summary>
        /// Gets the area for a 2D set of points defining a polygon. Consider using Polygon class when possible.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Area(this IEnumerable<Vector2> polygon)
        {
            var area = 0.0;
            var enumerator = polygon.GetEnumerator();
            enumerator.MoveNext();
            var basePoint = enumerator.Current;
            enumerator.MoveNext();
            var prevPoint = enumerator.Current;
            foreach (var currentPt in polygon.Skip(2))
            {
                area += (prevPoint - basePoint).Cross(currentPt - basePoint);
                prevPoint = currentPt;
            }
            return 0.5 * area;
        }


        /// <summary>
        /// Converts the 2D coordaintes into a 1D collection of doubles. e.g. { X1, Y1, X2, Y2, ... }
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>IEnumerable&lt;System.Double&gt;.</returns>
        public static IEnumerable<double> ConvertTo1DDoublesCollection(this IEnumerable<Vector2> coordinates)
        {
            foreach (var coordinate in coordinates)
            {
                yield return coordinate.X;
                yield return coordinate.Y;
            }
        }

        /// <summary>
        /// Converts a 1D collection of doubles. e.g. { X1, Y1, X2, Y2, ... } into a collection of 2D coordiantes.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        /// <exception cref="ArgumentException">An odd number of coordinates have been provided to " +
        ///                    "convert the 1D array of double to an array of vectors.</exception>
        public static IEnumerable<Vector2> ConvertToVector2s(this IEnumerable<double> coordinates)
        {
            var enumerator = coordinates.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var x = enumerator.Current;
                if (!enumerator.MoveNext())
                    throw new ArgumentException("An odd number of coordinates have been provided to " +
                   "convert the 1D array of double to an array of vectors.");
                var y = enumerator.Current;
                yield return new Vector2(x, y);
            }
        }

        /// <summary>
        /// Gets whether a polygon is rectangular by using the minimum bounding rectangle.
        /// The rectangle my be in any orientation and contain any number of points greater than three.
        /// Confidence Percentage can be decreased to identify polygons that are close to rectangular.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="dimensions"></param>
        /// <param name="confidencePercentage"></param>
        /// <returns></returns>
        public static bool IsRectangular(this IEnumerable<Vector2> polygon, out Vector2 dimensions, double confidencePercentage = Constants.HighConfidence)
        {
            if (confidencePercentage > 1.0 || Math.Sign(confidencePercentage) < 0)
                throw new Exception("Confidence percentage must be between 0 and 1");
            var tolerancePercentage = 1.0 - confidencePercentage;
            //For it to be rectangular, Area = l*w && Perimeter = 2*l + 2*w.
            //This can only gaurantee that it is not a rectangle if false.
            //If true, then check the polygon area vs. its minBoundingRectangle area. 
            //The area / perimeter check is not strictly necessary, but can provide some speed-up
            //For obviously not rectangular pieces
            var perimeter = polygon.Perimeter();
            var area = polygon.Area();
            var sqrRootTerm = Math.Sqrt(perimeter * perimeter - 16 * area);
            var length = 0.25 * (perimeter + sqrRootTerm);
            var width = 0.25 * (perimeter - sqrRootTerm);
            dimensions = new Vector2(length, width);
            var areaCheck = length * width;
            var perimeterCheck = 2 * length + 2 * width;
            if (!area.IsPracticallySame(areaCheck, area * tolerancePercentage) &&
                !perimeter.IsPracticallySame(perimeterCheck, perimeter * tolerancePercentage))
            {
                return false;
            }

            var minBoundingRectangle = MinimumEnclosure.BoundingRectangle(polygon);
            return area.IsPracticallySame(minBoundingRectangle.Area, area * tolerancePercentage);
        }


        /// <summary>Determines whether the specified polygon is circular.</summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns>
        ///   <c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this Polygon polygon, out BoundingCircle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            return IsCircular(polygon.Path, out minCircle, confidencePercentage);
        }

        /// <summary>Determines whether the specified polygon is circular.</summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns>
        ///   <c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this IEnumerable<Vector2> polygon, out BoundingCircle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            var tolerancePercentage = 1.0 - confidencePercentage;
            minCircle = MinimumEnclosure.MinimumCircle(polygon);

            //Check if areas are close to the same
            var polygonArea = polygon.Area();
            return polygonArea.IsPracticallySame(minCircle.Area, polygonArea * tolerancePercentage);
        }


        //Mirrors a shape along a given direction, such that the mid line is the same for both the original and mirror
        public static List<List<Vector2>> Mirror(List<List<Vector2>> shape, Vector2 direction2D)
        {
            var mirror = new List<List<Vector2>>();
            var points = shape.SelectMany(path => path).ToList();

            MinimumEnclosure.GetLengthAndExtremePoints(points, direction2D, out var bottomPoints, out _);
            var distanceFromOriginToClosestPoint = bottomPoints[0].Dot(direction2D);
            foreach (var polygon in shape)
            {
                var newPath = new List<Vector2>();
                foreach (var point in polygon)
                {
                    //Get the distance to the point along direction2D
                    //Then subtract 2X the distance along direction2D
                    var d = point.Dot(direction2D) - distanceFromOriginToClosestPoint;
                    newPath.Add(new Vector2(point.X - direction2D.X * 2 * d, point.Y - direction2D.Y * 2 * d));
                }
                //Reverse the new path so that it retains the same CW/CCW direction of the original
                newPath.Reverse();
                mirror.Add(new List<Vector2>(newPath));
                //if (!mirror.Last().Area().IsPracticallySame(polygon.Area, Constants.BaseTolerance))
                //{
                //   throw new Exception("Areas do not match after mirroring the polygons");
                //} ********commenting out this check. It should be in unit testing - not slow down this method*** 
            }
            return mirror;
        }
    }
}
