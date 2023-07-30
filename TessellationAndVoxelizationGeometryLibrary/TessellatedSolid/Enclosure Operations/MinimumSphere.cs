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
        public static Sphere MinimumSphere(this IEnumerable<Vector3> pointsInput)
        {
            //throw new NotImplementedException();
            var points = pointsInput.ToArray();
            var numPoints = points.Length;
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            if (numPoints <= 4)
                return FindSphere(points, ref numPoints);

            int numPointsInCircle = 1;
            Sphere sphere = default;
            var newPointFoundOutsideCircle = true;
            while (newPointFoundOutsideCircle)
            {
                sphere = FindSphere(points, ref numPointsInCircle);
                newPointFoundOutsideCircle = false;
                var maxDistSqared = sphere.Radius * sphere.Radius;
                var indexOfMaxDist = -1;
                for (int i = numPointsInCircle; i < numPoints; i++)
                {
                    var dist = (points[i] - sphere.Center).LengthSquared();

                    if (maxDistSqared < dist)
                    {
                        maxDistSqared = dist;
                        indexOfMaxDist = i;
                        newPointFoundOutsideCircle = true;
                    }
                }
                if (newPointFoundOutsideCircle)
                {
                    var maxPoint = points[indexOfMaxDist];
                    Array.Copy(points, 0, points, 1, indexOfMaxDist);
                    points[0] = maxPoint;
                    numPointsInCircle++;
                }
            }
            return sphere;
        }
        static void SwapPoints(int i, int j, Vector3[] points)
        {
            var temp = points[i];
            points[i] = points[j];
            points[j] = temp;
        }
        private static Sphere FindSphere(Vector3[] points, ref int numInCircle)
        {
            if (numInCircle == 1)
                return new Sphere(points[0], 0, null);
            if (numInCircle == 2)
                return CreateSphereFrom2Points(points[0], points[1]);
            if (numInCircle == 3)
            {
                // since the last point was outside the circle, we know it is on the circle
                // before we jump to the 3-point circle, we should check if either of the two
                // 2-point circles (which are guaranteed to be smaller than the 3-point cirlce)
                // includes the other point from the starting circle
                var circle = CreateSphereFrom2Points(points[0], points[1]);
                if ((points[2] - circle.Center).LengthSquared() <= circle.Radius * circle.Radius)
                {
                    numInCircle = 2;
                    return circle;
                }
                circle = CreateSphereFrom2Points(points[0], points[2]);
                if ((points[1] - circle.Center).LengthSquared() <= circle.Radius * circle.Radius)
                {
                    numInCircle = 2;
                    // since 1 and 3 are furthest apart, we need to swap 2 and 3
                    // so that the two points in the circle are at then end of the list
                    SwapPoints(1, 2, points);
                    return circle;
                }
                // otherwise, it's the 3-point circle
                circle = CreateSphereFrom3Points(points[0], points[1], points[2]);
                return circle;
            }
            { // else if (span.Length == 4)
              // (I know I could remove these curly braces, but it means that the variables
              // circle and tempPoint are indepedently defined in this scope, as well as the
              // Length == 3 case, which is nicer than coming up with separate names for them)
              // now we have a lot of cases to deal with. All we know is that the last point
              // was outside the circle, so it must be on the circle this time
                numInCircle = 3;
                // circle 0-1-2
                var circle = FindSphere(points, ref numInCircle); // a little recursion never hurt anyone
                                                                  // this allows us to check if it dropped back down to 2 points as well
                                                                  // the easiest and most likely case is that the 3 points at the end
                                                                  // of the list capture the fourth point
                if ((points[3] - circle.Center).LengthSquared() <= circle.Radius * circle.Radius)
                    return circle;
                // since the 4th point is outside the circle, we try again with a little
                // recursion after first swapping the 2nd & 3rd points
                SwapPoints(2, 3, points);
                // circle 0-1-3
                numInCircle = 3; //it probably already is, but the previous call may have dropped it to 2
                circle = FindSphere(points, ref numInCircle);
                if ((points[3] - circle.Center).LengthSquared() <= circle.Radius * circle.Radius)
                    return circle;
                // try once more with the original 2nd point as the 4th point
                if (numInCircle == 2) //very rare case here, but if you swapped above, and numInCircle has dropped to 2,
                                      //then 4 has been moved into the "2" position (the circle would not have been between
                                      //the orignial 1 and 2 since that would have been found already). In this case, the original
                                      //2 would be in the 3rd spot so, you need to swap 3 & 4 NOT 2 & 4 as below
                {
                    SwapPoints(2, 3, points);
                    numInCircle = 3;
                }
                else
                    SwapPoints(1, 3, points);
                // circle 1-3-4
                circle = FindSphere(points, ref numInCircle);
                if ((points[3] - circle.Center).LengthSquared() > circle.Radius * circle.Radius)
                    throw new Exception("This should never happen.");
                return circle;
            }
        }

        private static Sphere CreateSphereFrom4Points(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            throw new NotImplementedException();
            /* see comment at :https://math.stackexchange.com/questions/2585392/equation-of-the-sphere-that-passes-through-4-points 
             The technique here is straightforward.
             First take the general equation of a sphere:
            (x_1 − a)^2 + (y_1 − b)^2 + (z_1 − c)^2 = r^2
            where (a, b, c) is the center of the sphere and r is the radius.
            If this passes through the point, p1 (x_1, y_1, z_1)
            and you do the same for p2 (x_2, y_2, z_2)
            then you have two equations.
            Now subtract the second from the first
            x_1^2−x_2^2 + 2a(x2−x1) + y_1^2 − y_2^2 + 2b(y_2−y_1) + z_1^2 − z_2^2 + 2c(z2−z1)=0
            You now have a linear equation in three unknowns: a, b and c.
            do the same for p3 (x_3, y_3, z_3) and p4 (x_4, y_4, z_4)
            now you have 3 linear equations in three unknowns: a, b and c.  
            Solve for these, then easily obtain r.
             */
        }

        private static Sphere CreateSphereFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var plane = Plane.CreateFromVertices(p1, p2, p3);
            var transform = plane.Normal.TransformToXYPlane(out var backTransform);
            var p1_2D = p1.ConvertTo2DCoordinates(transform);
            var p2_2D = p2.ConvertTo2DCoordinates(transform);
            var p3_2D = p3.ConvertTo2DCoordinates(transform);
            Circle.CreateFrom3Points(p1_2D, p2_2D, p3_2D, out var circle);
            var center = circle.Center.ConvertTo3DLocation(backTransform);
            return new Sphere(center, (p1 - center).Length(), null);
        }

        private static Sphere CreateSphereFrom2Points(Vector3 p1, Vector3 p2)
        {
            var center = 0.5 * (p1 + p2);
            return new Sphere(center, (p1 - center).Length(), null);
        }
    }
}