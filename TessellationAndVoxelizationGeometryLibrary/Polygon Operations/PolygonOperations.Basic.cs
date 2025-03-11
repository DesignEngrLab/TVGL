// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.Basic.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Get the largest polygon by net area from the collections of polygons
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>Polygon.</returns>
        public static Polygon LargestPolygon(this IEnumerable<Polygon> polygons)
        {
            return polygons.MaxBy(p => p.Area);
        }
        /// <summary>
        /// Get the smallest polygon by net area from the collections of polygons
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>Polygon.</returns>
        public static Polygon SmallestPolygon(this IEnumerable<Polygon> polygons)
        {
            return polygons.MinBy(p => p.Area);
        }

        /// <summary>
        /// Gets the perimeter for a 2D set of points. Consider using Polygon class when possible.
        /// </summary>
        /// <param name="paths">The paths.</param>
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
        /// Calculate the area for a 2D set of points. Consider using Polygon class when possible.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns>System.Double.</returns>
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
            if (polygon.Count() < 3) return area;
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
        /// <exception cref="System.ArgumentException">An odd number of coordinates have been provided to " +
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
        /// <param name="polygon">The polygon.</param>
        /// <param name="dimensions">The dimensions.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns><c>true</c> if the specified dimensions is rectangular; otherwise, <c>false</c>.</returns>
        /// <exception cref="System.Exception">Confidence percentage must be between 0 and 1</exception>
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

        /// <summary>
        /// Determines whether the specified polygon is circular.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns><c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this Polygon polygon, out Circle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            return IsCircular(polygon.Path, out minCircle, confidencePercentage);
        }

        /// <summary>
        /// Determines whether the specified polygon is circular.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns><c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
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
        /// Reflects the on x.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="System.Exception">Areas do not match after mirroring the polygons</exception>
        public static Polygon ReflectOnX(this Polygon shape)
        {
            var relfection = new List<Polygon>();
            foreach (var polygon in shape.AllPolygons)
            {
                var newPath = new List<Vector2>();
                for (var i = polygon.Path.Count - 1; i >= 0; i--)//increment backwards to avoid need to reverse points.
                    newPath.Add(new Vector2(polygon.Path[i].X, -polygon.Path[i].Y));
                var newPolygon = new Polygon(newPath);
                relfection.Add(newPolygon);
                if (!newPolygon.Area.IsPracticallySame(polygon.Area, Math.Abs(polygon.Area * (1 - Constants.HighConfidence))))
                {
                    throw new Exception("Areas do not match after mirroring the polygons");
                }
            }
            return relfection.CreatePolygonTree(true).First();
        }

        /// <summary>
        /// Mirrors the specified polgyon along the direction, and at same midpoint of the provide polygon.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="direction2D">The direction2 d.</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="System.Exception">Areas do not match after mirroring the polygons</exception>
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

        /// <summary>
        /// Simplifies (reduces the number of edges) and smoothes (removes concave points) the polygon.
        /// Why concave? One must choose either convex or concave. In the methods envisioned
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="pixelSideLength"></param>
        public static void SimplifyAndSmoothRectilinearPolygon(this Polygon polygon, double pixelSideLength, bool movesMayBeDiagonal)
        {
            SimplifyAndSmoothRectilinearPolygonTop(polygon, pixelSideLength, movesMayBeDiagonal);
            foreach (var hole in polygon.InnerPolygons)
                SimplifyAndSmoothRectilinearPolygon(hole, pixelSideLength, movesMayBeDiagonal);
        }
        private static void SimplifyAndSmoothRectilinearPolygonTop(this Polygon polygon, double pixelSideLength, bool movesMayBeDiagonal)
        {
            var smallEdgeLengthSqd = pixelSideLength * pixelSideLength;
            if (movesMayBeDiagonal)
                smallEdgeLengthSqd *= 2;

            const double longEdgeFactor = 5;
            var longEdgeLengthSqd = longEdgeFactor * longEdgeFactor * smallEdgeLengthSqd;

            polygon.RemoveCollinearEdges();
            var unitLengthEdges = new Stack<int>();
            var medEdges = new Stack<int>();
            var longEdges = new Stack<int>();
            double length;
            var n = polygon.Edges.Count - 1;
            for (int i = 0; i <= n; i++)
            {
                if (!polygon.IsClosed && i == 0) continue;
                length = polygon.Edges[i].Vector.LengthSquared();
                if (!length.IsGreaterThanNonNegligible(smallEdgeLengthSqd))
                    unitLengthEdges.Push(i);
                else if (!length.IsLessThanNonNegligible(longEdgeLengthSqd))
                    longEdges.Push(i);
                else
                    medEdges.Push(i);
            }
            PixelEdgeLength nextEdgeIs = PixelEdgeLength.Med; // this doesn't invoke any changes
            if (polygon.IsClosed)
            {
                length = polygon.Edges[0].Vector.LengthSquared();
                nextEdgeIs = !length.IsGreaterThanNonNegligible(smallEdgeLengthSqd)
                    ? PixelEdgeLength.Unit : !length.IsLessThanNonNegligible(longEdgeLengthSqd) ?
                    PixelEdgeLength.Long : PixelEdgeLength.Med;
            }
            length = polygon.Edges[^1].Vector.LengthSquared();
            var lastEdgeIs = pixelEdgeType(n, unitLengthEdges, medEdges, longEdges);
            var currentEdgeIs = lastEdgeIs;
            var nextVector = polygon.Edges[0] != null ? polygon.Edges[0].Vector.Normalize() : Vector2.Null;
            for (int i = n; i >= 0; i--)
            {
                if (!polygon.IsClosed && i == 0) break;
                var currentEdge = polygon.Edges[i];
                var currVector = currentEdge.Vector.Normalize();
                var prevEdgeIs = i == 0 ? lastEdgeIs : pixelEdgeType(i - 1, unitLengthEdges, medEdges, longEdges);
                // four possibilities
                // 4. unit length edges that are followed by a long length edge, then the long edge is rounded off
                // (like case 3 above) (also note that this is not else if since it'd have been caught by case 1 as well)
                if (currentEdgeIs == PixelEdgeLength.Unit && nextEdgeIs == PixelEdgeLength.Long)
                {
                    var newCoord = currentEdge.ToPoint.Coordinates + pixelSideLength * nextVector;
                    polygon.InsertVertex(i + 1, newCoord);
                }
                if (polygon.IsClosed || (i != n && i != n - 1)) // don't modify the endpoint of a polyline (start and end edge[i].ToPoint's
                {                                               // when polygon is not closed
                    // 1. unit length edge are all reduced to centerpoint
                    // 2. med length edge are reduced to centerpoint if they neighbor two unit length edges
                    if (currentEdgeIs == PixelEdgeLength.Unit
                        || (currentEdgeIs == PixelEdgeLength.Med && nextEdgeIs == PixelEdgeLength.Unit && prevEdgeIs == PixelEdgeLength.Unit))
                        currentEdge.ToPoint.Coordinates = 0.5 * (currentEdge.FromPoint.Coordinates + currentEdge.ToPoint.Coordinates);
                    // 3. long length edges is followed by a unit length edge then reduce by one pixel
                    else if (currentEdgeIs == PixelEdgeLength.Long && nextEdgeIs == PixelEdgeLength.Unit)
                        currentEdge.ToPoint.Coordinates -= pixelSideLength * currVector;
                }
                nextVector = currVector;
                nextEdgeIs = currentEdgeIs;
                currentEdgeIs = prevEdgeIs;
            }
        }

        private static PixelEdgeLength pixelEdgeType(int n, Stack<int> unitLengthEdges, Stack<int> medEdges, Stack<int> longEdges)
        {
            if (unitLengthEdges.TryPeek(out var m) && m == n)
            {
                unitLengthEdges.Pop();
                return PixelEdgeLength.Unit;
            }
            if (medEdges.TryPeek(out m) && m == n)
            {
                medEdges.Pop();
                return PixelEdgeLength.Med;
            }
            if (longEdges.TryPeek(out m) && m == n)
            {
                longEdges.Pop();
                return PixelEdgeLength.Long;
            }
            return PixelEdgeLength.Med; // if polygon is not closed, and Med does the least in terms of changes.
        }
        private enum PixelEdgeLength
        {
            Unit,
            Med,
            Long
        }
    }
}