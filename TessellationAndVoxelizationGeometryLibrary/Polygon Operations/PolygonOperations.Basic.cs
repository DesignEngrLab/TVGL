// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
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
        /// Get the largest polygon by net area from the collections of polygons
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static Polygon LargestPolygonWithHoles(this IEnumerable<Polygon> polygons)
        {
            return polygons
                .Aggregate((poly1, poly2) => poly1.Area > poly2.Area ? poly1 : poly2);
            // the preceding use of "Aggregate" is a cryptic but quick trick to get the max using Linq. See: 
            //https://stackoverflow.com/questions/3188693/how-can-i-get-linq-to-return-the-object-which-has-the-max-value-for-a-given-prop
            // Union returns a collection of polygons, but we know that it should be one polygon with holes - since the tessellated solid
            // was one body. Getting the max one makes sense since there may be smaller artifacts returned from the operation.
        }

        /// <summary>
        /// Get the polygon with the most positive area (i.e. largest positive polygon) from the collections of polygons.
        /// Holes are ignored. This is only considering the path of the given polygon - no inner ones are considered.
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static Polygon MostPositivePolygon(this IEnumerable<Polygon> polygons)
        {
            return polygons
                .Aggregate((poly1, poly2) => poly1.PathArea > poly2.PathArea ? poly1 : poly2);
        }


        /// <summary>
        /// Get the polygon with the most negative area (i.e. largest negative polygon) from the collections of polygons
        /// Holes are ignored. This is only considering the path of the given polygon - no inner ones are considered.
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static Polygon MostNegativePolygon(this IEnumerable<Polygon> polygons)
        {
            return polygons
                .Aggregate((poly1, poly2) => poly1.PathArea < poly2.PathArea ? poly1 : poly2);
        }

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
            using var enumerator = polygon.GetEnumerator();
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
        /// Converts the 2D coordinates into a 1D collection of doubles. e.g. { X1, Y1, X2, Y2, ... }
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
            using var enumerator = coordinates.GetEnumerator();
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
            var polygonList = polygon as IList<Vector2> ?? polygon.ToList();
            var perimeter = polygonList.Perimeter();
            var area = polygonList.Area();
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

            var minBoundingRectangle = polygonList.BoundingRectangle();
            return area.IsPracticallySame(minBoundingRectangle.Area, area * tolerancePercentage);
        }

        /// <summary>Determines whether the specified polygon is circular.</summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns>
        ///   <c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this Polygon polygon, out Circle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            return IsCircular(polygon.Path, out minCircle, confidencePercentage);
        }

        /// <summary>Determines whether the specified polygon is circular.</summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns>
        ///   <c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this IEnumerable<Vector2> polygon, out Circle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            var tolerancePercentage = 1.0 - confidencePercentage;
            var points = polygon as IList<Vector2> ?? polygon.ToList();
            minCircle = points.MinimumCircle();

            //Check if areas are close to the same
            var polygonArea = Math.Abs(points.Area());
            return polygonArea.IsPracticallySame(minCircle.Area, polygonArea * tolerancePercentage);
        }


        /// <summary>
        /// Mirrors the specified polgyon along the direction, and at same midpoint of the provide polygon.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="direction2D">The direction2 d.</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="Exception">Areas do not match after mirroring the polygons</exception>
        public static Polygon Mirror(this Polygon shape, Vector2 direction2D)
        {
            var mirror = new List<Polygon>();
            var points = new List<Vector2>();
            foreach (var path in shape.AllPolygons)
            {
                foreach (var point in path.Path)
                {
                    points.Add(point);
                }
            }
            points.GetLengthAndExtremePoints(direction2D, out var bottomPoints, out _);
            var distanceFromOriginToClosestPoint = bottomPoints[0].Dot(direction2D);
            foreach (var polygon in shape.AllPolygons)
            {
                var newPath = new List<Vector2>();
                foreach (var point in polygon.Path)
                {
                    //Get the distance to the point along direction2D
                    //Then subtract 2X the distance along direction2D
                    var d = point.Dot(direction2D) - distanceFromOriginToClosestPoint;
                    newPath.Add(new Vector2(point.X - direction2D[0] * 2 * d, point.Y - direction2D[1] * 2 * d));
                }
                //Reverse the new path so that it retains the same CW/CCW direction of the original
                newPath.Reverse();
                mirror.Add(new Polygon(newPath));
                if (!mirror.Last().Area.IsPracticallySame(polygon.Area, Constants.BaseTolerance))
                {
                    throw new Exception("Areas do not match after mirroring the polygons");
                }
            }
            return mirror.CreatePolygonTree(true).First();
        }
    }
}