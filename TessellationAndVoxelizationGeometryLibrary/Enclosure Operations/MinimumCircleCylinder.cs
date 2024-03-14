// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MinimumCircleCylinder.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Timers;

namespace TVGL
{
    /// <summary>
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    /// tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        /// Finds the minimum bounding circle
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Bounding circle failed to converge</exception>
        /// <references>
        /// Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
        /// http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        /// This algorithm runs in near linear time. Visiting most points just a few times.
        /// </references>
        public static Circle MinimumCircle(this IEnumerable<Vector2> pointsInput)
        {
            var points = pointsInput.ToArray();
            var numPoints = points.Length;
            var maxNumStalledIterations = (int)(1.1 * numPoints);
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            else if (numPoints == 1)
                return new Circle(points[0], 0.0);
            else if (numPoints == 2)
                return Circle.CreateFrom2Points(points[0], points[1]);

            // make a circle from the first three points
            var circle = FirstCircle(points);
            var startIndex = 3;
            var maxDistSqared = circle.RadiusSquared;
            bool newPointFoundOutsideCircle;
            var stallCounter = 0;
            var indexOfMaxDist = -1;
            var timer = new Stopwatch();
            timer.Start();
            TimeSpan timeTaken = timer.Elapsed;
            do
            {
                newPointFoundOutsideCircle = false;
                for (int i = startIndex; i < numPoints; i++)
                {
                    var dist = (points[i] - circle.Center).LengthSquared();

                    if (dist > maxDistSqared)
                    {
                        maxDistSqared = dist;
                        if (indexOfMaxDist == i) stallCounter++;
                        else stallCounter = 0;
                        indexOfMaxDist = i;
                        newPointFoundOutsideCircle = true;
                    }
                }
                if (newPointFoundOutsideCircle)
                {
                    var maxPoint = points[indexOfMaxDist];
                    Array.Copy(points, 0, points, 1, indexOfMaxDist);
                    points[0] = maxPoint;
                    circle = FindCircle(points);
                    maxDistSqared = circle.RadiusSquared;
                    startIndex = 4;      
                }
                if(timer.Elapsed.TotalSeconds > 10)
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    var datetime = DateTime.Now;
                    var filename = Path.Join(desktop, "points0.csv");
                    var i = 1;
                    while (File.Exists(filename))
                    {
                        filename = Path.Join(desktop, "points", i.ToString(), ".csv");
                        i++;
                    }      
                    File.WriteAllLines(filename, pointsInput.Select(p => p.X + "," + p.Y)); ;
                    break;
                }
            } while (newPointFoundOutsideCircle && stallCounter < maxNumStalledIterations);
            timer.Stop();
            return circle;
        }

        private static Circle FirstCircle(Vector2[] points)
        {
            // during the main loop, the most outside point will be moved to the front
            // of the list. As can be seen in FindCircle, this greatly reduces the number
            // of circles to check. However, we do not have that luxury at first (the luxury
            // of knowing which point is part of the new circle). To prevent complicated
            // FindCircle, we can make a circle from the first 3 points - and check all four
            // permutations in this function. This will ensure that FindCircle runs faster (without
            // extra conditions and ensures that it won't miss a case
            var circle = Circle.CreateFrom2Points(points[0], points[1]);
            if ((points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
                return circle;
            circle = Circle.CreateFrom2Points(points[0], points[2]);
            if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                // since 0 and 2 are furthest apart, we need to swap 1 and 2
                // so that the two points in the circle are at the beinning of the list
                Constants.SwapItemsInList(1, 2, points);
                return circle;
            }
            circle = Circle.CreateFrom2Points(points[1], points[2]);
            if ((points[0] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                // since 1 and 2 are furthest apart, we need to swap 0 and 2
                // so that the two points in the circle are at the beinning of the list
                Constants.SwapItemsInList(0, 2, points);
                return circle;
            }
            // otherwise, it's the 3-point circle
            Circle.CreateFrom3Points(points[0], points[1], points[2], out circle);
            return circle;
        }
        private static Circle FindCircle(Vector2[] points)
        {
            // we know that 1,2,3 defined (were encompassed by) the last circle
            // the new 0 is outside of the 1-2-3 circle
            // so we need to
            // 1. make the 0-1 circle and check with 2 & 3
            // 2. make the 0-2 circle and check with 1 & 3
            // 3. make the 0-3 circle and check with 1 & 2
            // 4. make the 0-1-2 circle and check with 3 
            // 5. make the 0-1-3 circle and check with 2
            // 6. make the 0-2-3 circle and check with 1
            // for the latter 3 we want to return the smallest that includes the 4th point

            // 1. make the 0-1 circle and check with 2 & 3
            var circle = Circle.CreateFrom2Points(points[0], points[1]);
            if ((points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared
                && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
                return circle;

            // 2. make the 0-2 circle and check with 1 & 3
            circle = Circle.CreateFrom2Points(points[0], points[2]);
            if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
                && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                Constants.SwapItemsInList(1, 2, points);
                return circle;
            }
            // 3. make the 0-3 circle and check with 1 & 2
            circle = Circle.CreateFrom2Points(points[0], points[3]);
            if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
                && (points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                Constants.SwapItemsInList(1, 3, points);
                return circle;
            }

            Circle tempCircle;
            // circle 0-1-2
            var minRadiusSqd = double.PositiveInfinity;
            if (Circle.CreateFrom3Points(points[0], points[1], points[2], out circle)
                && !(points[3] - circle.Center).LengthSquared().IsGreaterThanNonNegligible(circle.RadiusSquared))
                // this one uses IsGreaterThanNonNegligible to prevent infinite cycling when more points are on the circle
                minRadiusSqd = circle.RadiusSquared;
            // circle 0-1-3
            var swap3And2 = false;
            if (Circle.CreateFrom3Points(points[0], points[1], points[3], out tempCircle)
                && (points[2] - tempCircle.Center).LengthSquared() <= tempCircle.RadiusSquared
                && tempCircle.RadiusSquared < minRadiusSqd)
            {
                swap3And2 = true;
                circle = tempCircle;
                minRadiusSqd = circle.RadiusSquared;
            }
            // circle 0-2-3
            var swap3And1 = false;
            if (Circle.CreateFrom3Points(points[0], points[2], points[3], out tempCircle)
                && (points[1] - tempCircle.Center).LengthSquared() <= tempCircle.RadiusSquared
                && tempCircle.RadiusSquared < minRadiusSqd)
            {
                swap3And1 = true;
                circle = tempCircle;
            }
            if (swap3And1) Constants.SwapItemsInList(3, 1, points);
            else if (swap3And2) Constants.SwapItemsInList(3, 2, points);
            return circle;
        }


        /// <summary>
        /// Gets the maximum inner circle given a group of polygons and a center point.
        /// If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>BoundingBox.</returns>
        public static Circle MaximumInnerCircle(this IEnumerable<IEnumerable<Vector2>> paths, Vector2 centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path)).ToList();
            return MaximumInnerCircle(polygons, centerPoint);
        }


        /// <summary>
        /// Gets the maximum inner circle given a group of polygons and a center point.
        /// The circle will either be inside a negative polygon or outside a positive polygon (e.g. C channel).
        /// Else it returns a negligible Bounding Circle
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>BoundingBox.</returns>
        public static Circle MaximumInnerCircle(this List<Polygon> polygons, Vector2 centerPoint)
        {
            var negativePolygons = new List<Polygon>();
            var positivePolygons = new List<Polygon>();
            foreach (var outerPoly in polygons)
            {
                foreach (var polygon in outerPoly.AllPolygons)
                {
                    if (polygon.IsPositive) positivePolygons.Add(polygon);
                    else negativePolygons.Add(polygon);
                }
            }
            //Check the distance from every line and point of every polygon in the group. 
            //Note: this function could possible be improved by determining which polygon is closest, 
            //but that did not seem to be a faster method. Also, a inner circle does not necessarily
            //need to be contained in a bounding box for positive solids (e.g. a C shape).
            var polygonsOfInterest = new List<Polygon>();

            //First, check if the point is inside any negative polygon.
            var minDistance = double.MaxValue;
            Polygon closestContainingPolygon = null;
            foreach (var negativePoly in negativePolygons)
            {
                // note that this condition is true, but within the method, IsPointInsidePolygon, the enclosure
                // return the value of the "IsPositive 
                if (negativePoly.IsPointInsidePolygon(true, centerPoint, out var onBoundary)) continue;
                if (onBoundary) return new Circle(centerPoint, 0.0); //Empty solution.

                //var d = closestLineAbove.YGivenX(centerPoint.X, out _) - centerPoint.Y; //Not negligible because not on Boundary
                var d = double.NaN; //how to correctly calculate this? the above line is not correct and is no longer a by-product
                                    // of IsPointInsidePolygon
                if (d < minDistance)
                {
                    minDistance = d;
                    closestContainingPolygon = negativePoly;
                }
            }

            //If the point is inside the polygon.
            if (closestContainingPolygon != null)
            {
                polygonsOfInterest.Add(closestContainingPolygon);
            }
            //If not inside a negative polygon, check if the point is inside any positive polygons. If it is return null.
            else
            {
                foreach (var positivePoly in positivePolygons)
                {
                    if (positivePoly.IsPointInsidePolygon(true, centerPoint, out _)) return new Circle(centerPoint, 0.0);
                    polygonsOfInterest.Add(positivePoly);
                }
            }

            //Lastly, determine how big the inner circle can be.
            var shortestDistance = double.MaxValue;
            var smallestBoundingCircle = new Circle(centerPoint, 0.0);
            foreach (var polygon in polygonsOfInterest)
            {
                var boundingCircle = MaximumInnerCircleInHole(polygon, centerPoint);
                if (boundingCircle.Radius < shortestDistance)
                {
                    shortestDistance = boundingCircle.Radius;
                    smallestBoundingCircle = boundingCircle;
                }
            }

            return smallestBoundingCircle;
        }

        /// <summary>
        /// Maximums the inner circle in hole.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="centerPoint">The center point.</param>
        /// <returns>Circle.</returns>
        private static Circle MaximumInnerCircleInHole(Polygon polygon, Vector2 centerPoint)
        {
            var shortestDistance = double.MaxValue;
            //1. For every line on the path, get the closest point on the edge to the center point. 
            //   Skip if min distance to line (perpendicular) forms a point not on the line.
            foreach (var line in polygon.Edges)
            {
                var v1 = line.ToPoint.Coordinates - line.FromPoint.Coordinates;
                //Correctly ordering the points should yield a negative area if the circle is inside a hole or outside a positive polygon.
                //Note also that zero area will occur when the points line up, which we want to ignore (the line ends will be checked anyways)
                if (!(new List<Vector2> { line.FromPoint.Coordinates, line.ToPoint.Coordinates, centerPoint })
                    .Area().IsNegativeNonNegligible())
                    continue;

                //Figure out how far the center point is away from the line
                var d = MiscFunctions.DistancePointToLine(centerPoint, line.FromPoint.Coordinates, v1, out var pointOnLine);
                if (d > shortestDistance) continue;

                //Now we need to figure out if the lines intersect
                if (!MiscFunctions.SegmentSegment2DIntersection(line.FromPoint.Coordinates, line.ToPoint.Coordinates,
                    centerPoint, pointOnLine, out _, out _, out _)) continue;
                //if(intersectionPoint != tempPoint) throw new Exception("Error in implementation. This should always be true.");
                shortestDistance = d;
            }

            //2. For every point in path and every closest edge point find distance to center.
            //   The shortest distance determines the diameter of the inner circle.
            foreach (var point in polygon.Path)
            {
                var d = point.DistanceSquared(centerPoint);
                if (d < shortestDistance) shortestDistance = d;
            }

            if (shortestDistance.IsPracticallySame(double.MaxValue)) return new Circle(centerPoint, 0.0); //Not inside any hole or outside any positive polygon
            return new Circle(centerPoint, shortestDistance);
        }

        /// <summary>
        /// Minimums the bounding cylinder.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="box">The box.</param>
        /// <returns>Cylinder.</returns>
        public static Cylinder MinimumBoundingCylinder(TessellatedSolid ts, BoundingBox box)
        {
            if(ts.ConvexHull != null)
                return MinimumBoundingCylinder(ts.ConvexHull.Vertices, box.Directions);
            return MinimumBoundingCylinder(ts.Vertices, box.Directions);
        }

        /// <summary>
        /// Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static Cylinder MinimumBoundingCylinder<T>(this IEnumerable<T> convexHullVertices) where T : IVector3D
        {
            // here we create 13 directions. just like for bounding box
            var directions = new List<Vector3>();
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                    directions.Add(new Vector3(1, i, j).Normalize());
            directions.Add(new Vector3(0, 0, 1).Normalize());
            directions.Add(new Vector3(0, 1, 0).Normalize());
            directions.Add(new Vector3(0, 1, 1).Normalize());
            directions.Add(new Vector3(0, -1, 1).Normalize());
            return MinimumBoundingCylinder(convexHullVertices, directions);
        }

        /// <summary>
        /// Minimums the bounding cylinder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="directions">The directions.</param>
        /// <returns>Cylinder.</returns>
        public static Cylinder MinimumBoundingCylinder<T>(IEnumerable<T> convexHullVertices, IEnumerable<Vector3> directions) where T : IVector3D
        {
            Cylinder minCylinder = null;
            var cvxHullVertsList = convexHullVertices as IList<T> ?? convexHullVertices.ToList();
            var minCylinderVolume = double.PositiveInfinity;
            foreach (var direction in directions)
            {
                var cylinder = MinimumBoundingCylinder(cvxHullVertsList, direction);
                if (cylinder.Volume < minCylinderVolume)
                {
                    minCylinderVolume = cylinder.Volume;
                    minCylinder = cylinder;
                }  
            }
            return minCylinder;
        }

        /// <summary>
        /// Minimums the bounding cylinder.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>Cylinder.</returns>
        public static Cylinder MinimumBoundingCylinder<T>(IList<T> convexHullVertices, Vector3 direction) where T : IVector3D
        {
            if (direction.IsNull() || direction == Vector3.Zero)
                return null;
            var pointsOnFace = convexHullVertices.ProjectTo2DCoordinates(direction, out var backTransform);
            var circle = MinimumCircle(pointsOnFace);
            var (min, max) = TVGL.MinimumEnclosure.GetDistanceToExtremeVertex(convexHullVertices, direction, out _, out _);
            var anchor = circle.Center.ConvertTo3DLocation(backTransform);
            return new Cylinder
            {
                Axis = direction,
                Anchor = anchor,
                Circle = circle,//Set circle directly from projection, since cylinder.Circle set function was not aligned on center. 
                Radius = circle.Radius,
                MinDistanceAlongAxis = min,
                MaxDistanceAlongAxis = max
            };
        }

        /// <summary>
        /// The maximum minimum bound cyl iterations
        /// </summary>
        const int MaxMinBoundCylIterations = 120;

        /// <summary>
        /// Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vertices">The vertices.</param>
        /// <param name="likelyAxis">The likely axis.</param>
        /// <returns>BoundingBox.</returns>
        public static Vector3 MinimumBoundingCylinderAxis<T>(this IEnumerable<T> vertices, Vector3 likelyAxis) where T : IVector3D
        {
            BoundingBox<T> box = null;
            int j = 0;
            var movement = 1.0;
            var maxIters = MaxMinBoundCylIterations;
            while (movement > 0.001 && maxIters-- > 0)
            {
                var perp1 = likelyAxis.GetPerpendicularDirection();
                box = TVGL.MinimumEnclosure.FindOBBAlongDirection(vertices, perp1);
                var d1 = Math.Abs(box.Directions[0].Dot(likelyAxis));
                var d2 = Math.Abs(box.Directions[1].Dot(likelyAxis));
                var d3 = Math.Abs(box.Directions[2].Dot(likelyAxis));
                j = 2;
                double d = d3;
                if (d1 > d2 && d1 > d3)
                {
                    j = 0;
                    d = d1;
                }
                else if (d2 > d3)
                {
                    j = 1;
                    d = d2;
                }

                var newAxis = box.Directions[j];
                //Repeat along second perpendicular
                var perp2 = newAxis.Cross(perp1);
                box = TVGL.MinimumEnclosure.FindOBBAlongDirection(vertices, perp2);
                d1 = Math.Abs(box.Directions[0].Dot(newAxis));
                d2 = Math.Abs(box.Directions[1].Dot(newAxis));
                d3 = Math.Abs(box.Directions[2].Dot(newAxis));
                j = 2;
                d = d3;
                if (d1 > d2 && d1 > d3)
                {
                    j = 0;
                    d = d1;
                }
                else if (d2 > d3)
                {
                    j = 1;
                    d = d2;
                }
                likelyAxis = box.Directions[j];
                movement = 1 - d;
            }
            return likelyAxis;
        }
    }
}