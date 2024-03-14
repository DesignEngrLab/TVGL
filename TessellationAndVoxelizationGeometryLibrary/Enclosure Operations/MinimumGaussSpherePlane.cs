using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        /// <summary>
        /// Gets the minimums the gauss sphere plane. That is, the plane closest to the origin that
        /// encompasses all the provided points. 
        /// This is similar to the minimum bounding circle and sphere algorithms
        /// minimizes the maximum distance to the points.
        /// </summary>
        /// <param name="pointsInput">The input points should all be unit vectors.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A Plane.</returns>
        public static Plane MinimumGaussSpherePlane(this IEnumerable<Vector3> pointsInput, double tolerance = Constants.BaseTolerance)
        {
            var points = pointsInput as Vector3[] ?? pointsInput.ToArray();
            return MinimumGaussSpherePlane(points, points.Aggregate((v1, v2) => v1 + v2).Normalize(), tolerance);
        }

        /// <summary>
        /// Gets the minimums the gauss sphere plane. That is, the plane closest to the origin that
        /// encompasses all the provided points. 
        /// This is similar to the minimum bounding circle and sphere algorithms
        /// minimizes the maximum distance to the points.
        /// </summary>
        /// <param name="pointsInput">The input points should all be unit vectors.</param>
        /// <param name="orientingVector">The orienting vector is used to flip the plane so that it is found 
        /// in a particular direction. If the dot-product between the orienting vector and the plane normal is
        /// negative then the plane will be flipped.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>A Plane.</returns>
        public static Plane MinimumGaussSpherePlane(this IEnumerable<Vector3> pointsInput, Vector3 orientingVector,
            double tolerance = Constants.BaseTolerance)
        {
            var points = pointsInput as Vector3[] ?? pointsInput.ToArray();
            var numPoints = points.Length;
            var maxNumStalledIterations = 10;
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            if (numPoints <= 4)
                return FindBoundingPlane(points, orientingVector, tolerance, ref numPoints);

            int numPointsInPlane = 1;
            Plane plane = default;
            var stallCounter = 0;
            var newPointFoundOutsidePlane = true;
            while (newPointFoundOutsidePlane && stallCounter < maxNumStalledIterations)
            {
                plane = FindBoundingPlane(points, orientingVector, tolerance, ref numPointsInPlane);
                newPointFoundOutsidePlane = false;
                var minDist = plane.DistanceToOrigin;
                var indexOfMinDist = -1;
                for (int i = numPointsInPlane; i < numPoints; i++)
                {
                    var dist = points[i].Dot(plane.Normal);
                    if (minDist > dist)
                    {
                        minDist = dist;
                        if (indexOfMinDist == i) stallCounter++;
                        else stallCounter = 0;
                        indexOfMinDist = i;
                        newPointFoundOutsidePlane = true;
                    }
                }
                if (newPointFoundOutsidePlane)
                {
                    var maxPoint = points[indexOfMinDist];
                    Array.Copy(points, 0, points, 1, indexOfMinDist);
                    points[0] = maxPoint;
                    numPointsInPlane++;
                }
            }
            return plane;
        }
        private static Plane FindBoundingPlane(Vector3[] points, Vector3 orientingVector, double tolerance, ref int numInPlane)
        {
            if (numInPlane == 1)
                return new Plane(points[0], points[0]);
            if (numInPlane == 2)
                return CreatePlaneFrom2Points(points[0], points[1], orientingVector);
            if (numInPlane == 3)
            {
                // since the last point was outside the circle, we know it is on the circle
                // before we jump to the 3-point circle, we should check if either of the two
                // 2-point circles (which are guaranteed to be smaller than the 3-point cirlce)
                // includes the other point from the starting circle
                var plane = CreatePlaneFrom2Points(points[0], points[1], orientingVector);
                if (!points[2].Dot(plane.Normal).IsLessThanNonNegligible(plane.DistanceToOrigin, tolerance))
                {
                    numInPlane = 2;
                    return plane;
                }
                plane = CreatePlaneFrom2Points(points[0], points[2], orientingVector);
                if (!points[1].Dot(plane.Normal).IsLessThanNonNegligible(plane.DistanceToOrigin, tolerance))
                {
                    numInPlane = 2;
                    // since 1 and 3 are furthest apart, we need to swap 2 and 3
                    // so that the two points in the circle are at then end of the list
                    Constants.SwapItemsInList(1, 2, points);
                    return plane;
                }
                // otherwise, it's the 3-point circle
                plane = Plane.CreateFromVertices(points[0], points[1], points[2]);
                if (plane.Normal.Dot(orientingVector) < 0)
                {
                    plane.Normal *= -1;
                    plane.DistanceToOrigin *= -1;
                }
                return plane;
            }
            { // else if (span.Length == 4)
              // (I know I could remove these curly braces, but it means that the variables
              // circle and tempPoint are indepedently defined in this scope, as well as the
              // Length == 3 case, which is nicer than coming up with separate names for them)
                for (int i = 0; i < 4; i++)
                {
                    numInPlane = 3;
                    // circle 0-1-2
                    var circle = FindBoundingPlane(points, orientingVector, tolerance, ref numInPlane); // a little recursion never hurt anyone
                                                                                                        // this allows us to check if it dropped back down to 2 points as well
                                                                                                        // the easiest and most likely case is that the 3 points at the end
                                                                                                        // of the list capture the fourth point
                    if (!points[3].Dot(circle.Normal).IsLessThanNonNegligible(circle.DistanceToOrigin, tolerance))
                        return circle;
                    var tempPoint = points[3];
                    points[3] = points[2];
                    points[2] = points[1];
                    points[1] = points[0];
                    points[0] = tempPoint;
                }
                throw new Exception("This should never happen.");
            }
        }

        private static Plane CreatePlaneFrom2Points(Vector3 p1, Vector3 p2, Vector3 orientingVector)
        {
            var center = (p1 + p2) / 2;
            var plane = new Plane(center, center);
            if (plane.Normal.IsNull())
            {  // the two points midpoint happens to be the origin
                plane.Normal = orientingVector.Normalize();
                plane.DistanceToOrigin = 0;
            }
            if (plane.Normal.Dot(orientingVector) < 0)
            {
                plane.Normal *= -1;
                plane.DistanceToOrigin *= -1;
            }
            return plane;
        }

    }
}
