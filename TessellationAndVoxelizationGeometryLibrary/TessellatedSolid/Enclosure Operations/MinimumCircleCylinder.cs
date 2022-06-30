// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the minimum bounding circle
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Bounding circle failed to converge</exception>
        /// <references>
        ///     Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
        ///     http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        ///     This algorithm runs in near linear time. Visiting most points just a few times.
        ///     Though a linear algorithm was found by Meggi do, this algorithm is more robust
        ///     (doesn't care about multiple points on a line and fewer rounding functions)
        ///     and directly applicable to multiple dimensions (in our case, just 2 and 3 D).
        /// </references>
        public static Circle MinimumCircle(this IEnumerable<Vector2> points)
        {
            #region Algorithm 1

            ////Randomize the list of points
            //var r = new Random(0);
            //var randomPoints = new List<Vector2>(points.OrderBy(p => r.Next()));

            //if (randomPoints.Count < 2) return new Circle(0.0, points[0]);
            ////Get any two points in the list points.
            //var point1 = randomPoints[0];
            //var point2 = randomPoints[1];
            //var previousPoints = new HashSet<Vector2>();
            //var circle = new Circle(point1, point2);
            //var stallCounter = 0;
            //var i = 0;

            //while (i < randomPoints.Count && stallCounter < points.Count * 2)
            //{
            //    var currentPoint = randomPoints[i];
            //    //If the current point is part of the circle or inside the circle, go to the next iteration
            //    if (circle.Point0.Equals(currentPoint) ||
            //        circle.Point1.Equals(currentPoint) || 
            //        circle.Point2.Equals(currentPoint) || 
            //        circle.IsPointInsideCircle(currentPoint))
            //    {
            //        i++;
            //        continue;
            //    }

            //    //Else if the currentPoint is a previousPoint, increase dimension
            //    if (previousPoints.Contains(currentPoint))
            //    {
            //        //Make a new circle from the current two-point circle and the current point
            //        circle = new Circle(circle.Point0, circle.Point1, currentPoint);
            //        previousPoints.Remove(currentPoint);
            //        i++;
            //    }
            //    else
            //    {
            //        //Find the point in the circle furthest from new point. 
            //        circle.Furthest(currentPoint, out var furthestPoint, ref previousPoints);
            //        //Make a new circle from the furthest point and current point
            //        circle = new Circle(currentPoint, furthestPoint);
            //        //Add previousPoints to the front of the list
            //        foreach (var previousPoint in previousPoints)
            //        {
            //            randomPoints.Remove(previousPoint);
            //            randomPoints.Insert(0, previousPoint);
            //        }
            //        //Restart the search
            //        stallCounter++;
            //        i = 0;
            //    }
            //}

            #endregion

            #region Algorithm 2: Furthest Point
            //var r = new Random(0);
            //var randomPoints = new List<Vector2>(points.OrderBy(p => r.Next()));

            //Algorithm 2
            //I tried using the extremes (X, Y, and also tried Sum, Diff) to do a first pass at the circle
            //or to define the starting circle for max dX or dY, but all of these were slower do to the extra
            //for loop at the onset. The current approach is faster and simpler; just start with some arbitrary points.
            var pointList = points as IList<Vector2> ?? points.ToList();
            var pointsOnCircle = new[] { pointList[0], pointList[pointList.Count / 2] };
            var prevPointsOnCircle = pointsOnCircle;
            var circle = GetCircleFrom2DiametricalPoints(pointsOnCircle[0], pointsOnCircle[1]);
            var dummyPoint = Vector2.Null;
            var stallCounter = 0;
            var successful = false;
            var stallLimit = pointList.Count * 1.5;
            if (stallLimit < 100) stallLimit = 100;
            //var centerX = circle.Center.X;
            //var centerY = circle.Center.Y;
            var sqTolerance = Math.Sqrt(Constants.BaseTolerance);
            var sqRadiusPlusTolerance = circle.RadiusSquared + sqTolerance;
            var nextPoint = dummyPoint;
            var priorRadius = circle.RadiusSquared;
            while (!successful && stallCounter < stallLimit)
            {
                //If stallCounter is getting big, add a bit extra to the circle radius to ensure convergence
                if (stallCounter > stallLimit / 2)
                    sqRadiusPlusTolerance += Constants.BaseTolerance * sqRadiusPlusTolerance;
                //Add it a second time if stallCounter is even bigger
                if (stallCounter > stallLimit * 2 / 3)
                    sqRadiusPlusTolerance += Constants.BaseTolerance * sqRadiusPlusTolerance;

                //Find the furthest point from the center point
                var maxDistancePlusTolerance = sqRadiusPlusTolerance;
                var nextPointIsSet = false;
                foreach (var point in pointList)
                {
                    var squareDistanceToPoint = (circle.Center - point).LengthSquared();
                    //If the square distance is less than or equal to the max square distance, continue.
                    if (squareDistanceToPoint < maxDistancePlusTolerance) continue;
                    //Otherwise, set this as the next point to go to.
                    maxDistancePlusTolerance = squareDistanceToPoint + sqTolerance;
                    nextPoint = point;
                    nextPointIsSet = true;
                }
                if (!nextPointIsSet)
                {
                    successful = true;
                    continue;
                }

                //Create a new circle with 2 points
                //Find the point in the circle furthest from new point.
                var furthestPoint = FindFurthestPoint(nextPoint, pointsOnCircle);
                //Make a new circle from the furthest point and current point
                pointsOnCircle = new[] { nextPoint, furthestPoint };
                circle = GetCircleFrom2DiametricalPoints(nextPoint, furthestPoint);
                sqRadiusPlusTolerance = circle.RadiusSquared + sqTolerance;

                //Now check if the previous points are outside this circle.
                //To be outside the circle, it must be further out than the specified tolerance. 
                //Otherwise, the loop can get caught in a loop due to rounding error 
                //If you wanted to use a tighter tolerance, you would need to take the square roots to get the radius.   
                //If they are, increase the dimension and use three points in a circle
                foreach (var point in prevPointsOnCircle)
                {
                    if (point == furthestPoint) continue;
                    var distanceSquared = (circle.Center - point).LengthSquared();
                    if (distanceSquared > sqRadiusPlusTolerance)
                    {
                        //Make a new circle from the current two-point circle and the current point
                        circle = GetCircleFrom3Points(nextPoint, furthestPoint, point);
                        pointsOnCircle = new[] { nextPoint, furthestPoint, point };
                        sqRadiusPlusTolerance = circle.RadiusSquared + sqTolerance;
                    }
                }

                if (circle.RadiusSquared < priorRadius)
                {
                    Debug.WriteLine("Bounding circle got smaller during this iteration");
                }
                priorRadius = circle.RadiusSquared;
                prevPointsOnCircle = pointsOnCircle;
                stallCounter++;
            }
            if (stallCounter >= stallLimit) Debug.WriteLine("Bounding circle failed to converge to within " + (Constants.BaseTolerance * circle.Radius * 2));

            #endregion

            #region Algorithm 3: Meggiddo's Linear-Time Algorithm

            //Pair up points into n/2 pairs. 
            //If an odd number of points.....

            //Construct a bisecting line for each pair of points. This sets their slope.

            //Order the slopes.
            //Find the median (halfway in the set) slope of the bisector lines 


            //Test

            #endregion

            return circle;
        }

        private static Vector2 FindFurthestPoint(Vector2 reference, IEnumerable<Vector2> pointsOnCircle)
        {
            var furthestPoint = Vector2.Null;
            var furthestDistance = double.NegativeInfinity;
            foreach (var p in pointsOnCircle)
            {
                var distanceSquared = (p - reference).LengthSquared();
                if (furthestDistance < distanceSquared)
                {
                    furthestPoint = p;
                    furthestDistance = distanceSquared;
                }
            }
            return furthestPoint;
        }



        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     If there are no negative polygons, the function will return a negligible Bounding Circle
        /// </summary>
        /// <returns>BoundingBox.</returns>
        public static Circle MaximumInnerCircle(this IEnumerable<IEnumerable<Vector2>> paths, Vector2 centerPoint)
        {
            var polygons = paths.Select(path => new Polygon(path)).ToList();
            return MaximumInnerCircle(polygons, centerPoint);
        }


        /// <summary>
        ///     Gets the maximum inner circle given a group of polygons and a center point.
        ///     The circle will either be inside a negative polygon or outside a positive polygon (e.g. C channel). 
        ///     Else it returns a negligible Bounding Circle
        /// </summary>
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
                if (onBoundary) return new Circle(centerPoint, 0.0); //Null solution.

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
                    centerPoint, pointOnLine, out _)) continue;
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

        public static Cylinder MinimumBoundingCylinder(TessellatedSolid ts, BoundingBox box)
        {
            return MinimumBoundingCylinder(ts.ConvexHull.Vertices, box.Directions);
        }

        /// <summary>
        ///     Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static Cylinder MinimumBoundingCylinder<T>(this IEnumerable<T> convexHullVertices) where T : IVertex3D
        {
            // here we create 13 directions. just like for bounding box
            var directions = new List<Vector3>();
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                    directions.Add(new Vector3(1, i, j));
            directions.Add(new Vector3(0, 0, 1));
            directions.Add(new Vector3(0, 1, 0));
            directions.Add(new Vector3(0, 1, 1));
            directions.Add(new Vector3(0, -1, 1));
            return MinimumBoundingCylinder(convexHullVertices, directions);
        }

        public static Cylinder MinimumBoundingCylinder<T>(IEnumerable<T> convexHullVertices, IEnumerable<Vector3> directions) where T : IVertex3D
        {
            Cylinder minCylinder = null;
            var cvxHullVertsList = convexHullVertices as IList<T> ?? convexHullVertices.ToList();
            var minCylinderVolume = double.PositiveInfinity;
            foreach (var direction in directions)
            {
                var cylinder = MinimumBoundingCylinder(cvxHullVertsList, direction);
                if (minCylinderVolume > cylinder.Volume)
                    minCylinder = cylinder;
            }
            return minCylinder;
        }

        private static Cylinder MinimumBoundingCylinder<T>(IList<T> convexHullVertices, Vector3 direction) where T : IVertex3D
        {
            var pointsOnFace = convexHullVertices.ProjectTo2DCoordinates(direction, out var backTransform);
            var circle = MinimumCircle(pointsOnFace);
            var (min, max) = GetDistanceToExtremeVertex(convexHullVertices, direction, out _, out _);
            var anchor = circle.Center.ConvertTo3DLocation(backTransform);
            return new Cylinder(direction, anchor, circle, min, max);
        }

        const int MaxMinBoundCylIterations = 120;

        /// <summary>
        ///     Gets the minimum bounding cylinder using 13 guesses for the depth direction
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static Vector3 MinimumBoundingCylinderAxis<T>(this IEnumerable<T> vertices, Vector3 likelyAxis) where T : IVertex3D
        {
            BoundingBox<T> box = null;
            int j = 0;
            var movement = 1.0;
            var maxIters = MaxMinBoundCylIterations;
            while (movement > 0.001 && maxIters-- > 0)
            {
                var perp1 = likelyAxis.GetPerpendicularDirection();
                box = FindOBBAlongDirection(vertices, perp1);
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
                box = FindOBBAlongDirection(vertices, perp2);
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


        public static Circle GetCircleFrom2DiametricalPoints(Vector2 p0, Vector2 p1)
        {
            var center = (p0 + p1) / 2;
            return new Circle(center, (p0 - center).LengthSquared());
        }
        public static Circle GetCircleFrom3Points(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            var segment1 = (p1 - p0) / 2;
            var midPoint1 = (p0 + p1) / 2;
            var bisector1Dir = new Vector2(-segment1.Y, segment1.X);
            var segment2 = (p2 - p1) / 2;
            var midPoint2 = (p1 + p2) / 2;
            var bisector2Dir = new Vector2(-segment2.Y, segment2.X);
            var center = MiscFunctions.LineLine2DIntersection(midPoint1, bisector1Dir, midPoint2, bisector2Dir);
            return new Circle(center, (p0 - center).LengthSquared());
        }

        public static Circle GetCircleFrom2DiametricalPoints(Vector3 p0, Vector3 p1, Plane plane)
        {
            return GetCircleFrom2DiametricalPoints(p0.ConvertTo2DCoordinates(plane.AsTransformToXYPlane),
                            p1.ConvertTo2DCoordinates(plane.AsTransformToXYPlane));
        }
        public static Circle GetCircleFrom3Points(Vector3 p0, Vector3 p1, Vector3 p2, Plane plane)
        {
            return GetCircleFrom3Points(p0.ConvertTo2DCoordinates(plane.AsTransformToXYPlane),
                p1.ConvertTo2DCoordinates(plane.AsTransformToXYPlane), p2.ConvertTo2DCoordinates(plane.AsTransformToXYPlane));
        }
    }
}