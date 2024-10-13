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
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    /// tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
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
            var (min, max) = GetDistanceToExtremeVertex(convexHullVertices, direction, out _, out _);
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
        /// Gets the bounding cylinder, given an anchor and axis (i.e., 3D line forming the central axis)
        /// This is really simple - just get the circle given the anchor as a center point.
        /// Useful in cases were other criteria is being used to determine the best fit of the cylinder, 
        /// like surface alignment.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>Cylinder.</returns>
        public static Cylinder BoundingCylinderAlongCenterLine(IEnumerable<Vector3> convexHullVertices, Vector3 anchor, Vector3 axis)
        {
            if (axis.IsNull() || axis == Vector3.Zero || anchor.IsNull() || convexHullVertices.Count() < 4)
                return null;

            //Get the furthest vertex distance from the center line.
            var radiusSquared = 0.0;
            foreach(var vertex in convexHullVertices)
            {
                var r2 = MiscFunctions.DistancePointToLine(vertex, anchor, axis, out _, true);
                if(r2 > radiusSquared)
                    radiusSquared = r2;
            }
            var center2D = anchor.ConvertTo2DCoordinates(axis, out var _);
            var circle = new Circle(center2D, radiusSquared);
            
            //Get the depth of the cylinder.
            var (min, max) = GetDistanceToExtremeVertex(convexHullVertices, axis, out _, out _);
            //var anchor = circle.Center.ConvertTo3DLocation(backTransform);
            return new Cylinder
            {
                Axis = axis,
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