using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.TessellatedSolid.Enclosure_Operations
{
    public static partial class MinimumEnclosure
    {
        public static Plane MinimumGaussSpherePlane(this IEnumerable<Vector3> pointsInput)
        {
            var points = pointsInput.ToArray();
            var numPoints = points.Length;
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            if (numPoints <= 4)
                return FindBoundingPlane(points, ref numPoints);

            int numPointsInCircle = 1;
            Sphere sphere = default;
            var newPointFoundOutsideCircle = true;
            while (newPointFoundOutsideCircle)
            {
                sphere = FindBoundingPlane(points, ref numPointsInCircle);
                newPointFoundOutsideCircle = false;
                var maxDistSqared = sphere.Radius * sphere.Radius;
                var indexOfMaxDist = -1;
                for (int i = numPointsInCircle; i < numPoints; i++)
                {
                    var dist = LengthSquared(points[i], sphere.Center);

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
        private static Plane FindBoundingPlane(Vector3[] points, ref int numInPlane)
        {
            if (numInPlane == 1)
                return new Sphere(new Vector3(points[0].X, points[0].Y, points[0].Z), 0, true);
            if (numInPlane == 2)
                return CreateSphereFrom2Points(points[0], points[1]);
            if (numInPlane == 3)
            {
                // since the last point was outside the circle, we know it is on the circle
                // before we jump to the 3-point circle, we should check if either of the two
                // 2-point circles (which are guaranteed to be smaller than the 3-point cirlce)
                // includes the other point from the starting circle
                var circle = CreateSphereFrom2Points(points[0], points[1]);
                if (LengthSquared(points[2], circle.Center) <= circle.Radius * circle.Radius)
                {
                    numInPlane = 2;
                    return circle;
                }
                circle = CreateSphereFrom2Points(points[0], points[2]);
                if (LengthSquared(points[1], circle.Center) <= circle.Radius * circle.Radius)
                {
                    numInPlane = 2;
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
                numInPlane = 3;
                // circle 0-1-2
                var circle = FindSphere(points, ref numInPlane); // a little recursion never hurt anyone
                                                                  // this allows us to check if it dropped back down to 2 points as well
                                                                  // the easiest and most likely case is that the 3 points at the end
                                                                  // of the list capture the fourth point
                if (LengthSquared(points[3], circle.Center) <= circle.Radius * circle.Radius)
                    return circle;
                // since the 4th point is outside the circle, we try again with a little
                // recursion after first swapping the 2nd & 3rd points
                SwapPoints(2, 3, points);
                // circle 0-1-3
                numInPlane = 3; //it probably already is, but the previous call may have dropped it to 2
                circle = FindSphere(points, ref numInPlane);
                if (LengthSquared(points[3], circle.Center) <= circle.Radius * circle.Radius)
                    return circle;
                // try once more with the original 2nd point as the 4th point
                if (numInPlane == 2) //very rare case here, but if you swapped above, and numInCircle has dropped to 2,
                                      //then 4 has been moved into the "2" position (the circle would not have been between
                                      //the orignial 1 and 2 since that would have been found already). In this case, the original
                                      //2 would be in the 3rd spot so, you need to swap 3 & 4 NOT 2 & 4 as below
                {
                    SwapPoints(2, 3, points);
                    numInPlane = 3;
                }
                else
                    SwapPoints(1, 3, points);
                // circle 1-3-4
                circle = FindSphere(points, ref numInPlane);
                if (LengthSquared(points[3], circle.Center) > circle.Radius * circle.Radius)
                    throw new Exception("This should never happen.");
                return circle;
            }
        }
    }
}
