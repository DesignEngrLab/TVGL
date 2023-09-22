// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MinimumSphere.cs" company="Design Engineering Lab">
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

            int numPointsInSphere = 1;
            Sphere sphere = default;
            var newPointFoundOutsideSphere = true;
            while (newPointFoundOutsideSphere)
            {
                sphere = FindSphere(points, ref numPointsInSphere);
                newPointFoundOutsideSphere = false;
                var maxDistSqared = sphere.Radius * sphere.Radius;
                var indexOfMaxDist = -1;
                for (int i = numPointsInSphere; i < numPoints; i++)
                {
                    var dist = (points[i] - sphere.Center).LengthSquared();

                    if (maxDistSqared < dist)
                    {
                        maxDistSqared = dist;
                        indexOfMaxDist = i;
                        newPointFoundOutsideSphere = true;
                    }
                }
                if (newPointFoundOutsideSphere)
                {
                    var maxPoint = points[indexOfMaxDist];
                    Array.Copy(points, 0, points, 1, indexOfMaxDist);
                    points[0] = maxPoint;
                    numPointsInSphere++;
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
        private static Sphere FindSphere(Vector3[] points, ref int numInSphere)
        {
            if (numInSphere == 1)
                return new Sphere(points[0], 0, null);
            if (numInSphere == 2)
                return Sphere.CreateFrom2Points(points[0], points[1]);
            if (numInSphere == 3)
            {
                // the most outside point has been moved to the zeroth position
                // it's likely that the next sphere would be defined by the points at 0, 1, & 2
                // but before we jump to the 3-point sphere, we should check if either of the two
                // 2-point spheres (which are guaranteed to be smaller than the 3-point sphere)
                // includes the other point: sphere from 0&1 check with 2, sphere with 0&2 and check with 1
                var sphere = Sphere.CreateFrom2Points(points[0], points[1]);
                if ((points[2] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                {
                    numInSphere = 2;
                    return sphere;
                }
                sphere = Sphere.CreateFrom2Points(points[0], points[2]);
                if ((points[1] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                {
                    numInSphere = 2;
                    // since 1 and 3 are furthest apart, we need to swap 2 and 3
                    // so that the two points in the sphere are at the beginning of the list
                    SwapPoints(1, 2, points);  // (why is this necessary?)
                    return sphere;
                }
                // otherwise, it's the 3-point sphere
                sphere = Sphere.CreateFrom3Points(points[0], points[1], points[2]);
                return sphere;
            }
            //if (numInSphere == 4)
            {
                // we know that 1,2,3 defined (were encompassed by) the last sphere
                // the new 0 is outside of the 1-2-3 sphere
                // so we need to
                // make the 0-1 sphere and check with 2 & 3
                // make the 0-2 sphere and check with 1 & 3
                // make the 0-3 sphere and check with 1 & 2
                // make the 0-1-2 sphere and check with 3
                // wait a second, flip those last two steps. doing 0-1,0-2,0-1-2 all compared to 3
                // well, we just did that above, so a little recursion is in order
                numInSphere = 3;
                var sphere = FindSphere(points, ref numInSphere);
                if ((points[3] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                    return sphere;
                //can't forget to check 0-3 and check with 1 & 2
                numInSphere = 2;
                sphere = Sphere.CreateFrom2Points(points[0], points[3]);
                if ((points[1] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius &&
                    (points[2] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                    return sphere;
                // 0-1-3 check with 2
                numInSphere = 3;
                sphere = Sphere.CreateFrom3Points(points[0], points[1], points[3]);
                if ((points[2] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                    return sphere;
                // 0-2-3 check with 1
                sphere = Sphere.CreateFrom3Points(points[0], points[2], points[3]);
                if ((points[1] - sphere.Center).LengthSquared() <= sphere.Radius * sphere.Radius)
                    return sphere;
                // 0-1-2-3
                numInSphere = 4;
                return Sphere.CreateFrom4Points(points[0], points[1], points[2], points[3]);
            }
        }
    }
}