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
        internal readonly struct sphereLight
        {
            internal readonly Vector3 Center;
            internal readonly double RadiusSquared;

            internal sphereLight(Vector3 center, double radiusSquared)
            {
                Center = center;
                RadiusSquared = radiusSquared;
            }
        }
        public static Sphere MinimumSphere(this IEnumerable<Vector3> pointsInput)
        {
            //throw new NotImplementedException();
            var points = pointsInput.ToArray();
            var numPoints = points.Length;
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            else if (numPoints == 1)
                return new Sphere(points[0], 0, null);
            else if (numPoints == 2)
                return Sphere.CreateFrom2Points(points[0], points[1]);
            else if (numPoints == 3)
                return FirstSphereWith3Points(points[0], points[1], points[2]);

            var sphere = FirstSphere(points);
            var startIndex = 4;
            var maxDistSqared = sphere.RadiusSquared;
            bool newPointFoundOutsideSphere;
            var indexOfMaxDist = -1;
            do
            {
                newPointFoundOutsideSphere = false;
                for (int i = startIndex; i < numPoints; i++)
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
                    sphere = FindSphere(points);
                    maxDistSqared = sphere.RadiusSquared;
                    startIndex = 5;
                }
            } while (newPointFoundOutsideSphere);
            return new Sphere(sphere.Center, Math.Sqrt(sphere.RadiusSquared), null);
        }

        private static Sphere FirstSphereWith3Points(Vector3 p0, Vector3 p1, Vector3 p2)
        {
            // 0,1 & check 2
            var sphere = Sphere.CreateFrom2Points(p0, p1);
            if ((p2 - sphere.Center).Length() <= sphere.Radius)
                return sphere;
            // 0,2 & check 1 
            sphere = Sphere.CreateFrom2Points(p0, p2);
            if ((p1 - sphere.Center).Length() <= sphere.Radius)
                return sphere;
            // 1,2 & check 0 
            sphere = Sphere.CreateFrom2Points(p1, p2);
            if ((p0 - sphere.Center).Length() <= sphere.Radius)
                return sphere;
            return Sphere.CreateFrom3Points(p0, p1, p2);
        }

        /// <summary>
        /// Create the smallest sphere from the first four *unordered* points.
        /// This is needed because the main loop will simply call FindSphere 
        /// which makes the assumption that the zeroth point was outside the last sphere
        /// and, hence, must be included in the new sphere.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="numPointsInSphere">The num points in sphere.</param>
        /// <returns>A Sphere.</returns>
        private static sphereLight FirstSphere(Vector3[] points)
        {
            // first check diametrically opposing points
            // 0,1 & check 2 & 3
            var sphere = CreateFrom2Points(points[0], points[1]);
            if ((points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 0,2 & check 1 & 3
            sphere = CreateFrom2Points(points[0], points[2]);
            if ((points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 0,3 & check 1 & 2
            sphere = CreateFrom2Points(points[0], points[3]);
            if ((points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 1,2 & check 0 & 3
            sphere = CreateFrom2Points(points[1], points[2]);
            if ((points[0] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 1,3 & check 0 & 2
            sphere = CreateFrom2Points(points[1], points[3]);
            if ((points[0] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 2,3 & check 0 & 1
            sphere = CreateFrom2Points(points[2], points[3]);
            if ((points[0] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;

            var minRadiusSqd = double.PositiveInfinity;
            // now check 3-point spheres. here we need to find the smallest sphere! this wasn't the
            // case for the above diametrically opposed points (since the two defining points of the sphere
            // are farthest apart, but we need to check this here as 3 nearly collinear points will make
            // a huge sphere
            // 0,1,2 & check 3
            sphere = CreateFrom3Points(points[0], points[1], points[2]);
            if ((points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                minRadiusSqd = sphere.RadiusSquared;
            // 0,1,3 & check 2
            var swap3And2 = false;
            var tempSphere = CreateFrom3Points(points[0], points[1], points[3]);
            if ((points[2] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap3And2 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,2,3 & check 1
            var swap3And1 = false;
            tempSphere = CreateFrom3Points(points[0], points[2], points[3]);
            if ((points[1] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap3And1 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 1,2,3 & check 0
            var swap3And0 = false;
            tempSphere = CreateFrom3Points(points[1], points[2], points[3]);
            if ((points[0] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap3And0 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            var fourPointIsBest = false;
            tempSphere = CreateFrom4Points(points[0], points[1], points[2], points[3]);
            if (tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                fourPointIsBest = true;
            }
            if (!fourPointIsBest)
            {
                if (swap3And0) Constants.SwapItemsInList(3, 0, points);
                else if (swap3And1) Constants.SwapItemsInList(3, 1, points);
                else if (swap3And2) Constants.SwapItemsInList(3, 2, points);
            }
            return sphere;
        }

        private static sphereLight FindSphere(Vector3[] points)
        {
            // first check diametrically opposing points: the good news is that the zeroth
            // point must be in the set of points defining the sphere. The bad news is that
            // we need to include cases up to 5 points!
            // 0,1 & check 2, 3 & 4
            var sphere = CreateFrom2Points(points[0], points[1]);
            if ((points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[4] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 0,2 & check 1, 3 & 4
            sphere = CreateFrom2Points(points[0], points[2]);
            if ((points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[4] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 0,3 & check 1, 2 & 4
            sphere = CreateFrom2Points(points[0], points[3]);
            if ((points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[4] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;
            // 0,4 & check 1, 2 & 3
            sphere = CreateFrom2Points(points[0], points[4]);
            if ((points[1] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[2] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                return sphere;

            var minRadiusSqd = double.PositiveInfinity;
            // now check 3-point spheres. here we need to find the smallest sphere! this wasn't the
            // case for the above diametrically opposed points (since the two defining points of the sphere
            // are farthest apart, but we need to check this here as 3 nearly collinear points will make
            // a huge sphere
            // 0,1,2 & check 3 & 4
            sphere = CreateFrom3Points(points[0], points[1], points[2]);
            if ((points[3] - sphere.Center).LengthSquared() <= sphere.RadiusSquared
                && (points[4] - sphere.Center).LengthSquared() <= sphere.RadiusSquared)
                minRadiusSqd = sphere.RadiusSquared;
            // 0,1,3 & check 2 & 4
            var swap3And2 = false;
            var tempSphere = CreateFrom3Points(points[0], points[1], points[3]);
            if ((points[2] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && (points[4] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap3And2 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,1,4 & check 2 & 3
            var swap4And2 = false;
            tempSphere = CreateFrom3Points(points[0], points[1], points[4]);
            if ((points[2] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && (points[3] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap4And2 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,2,3 & check 1 & 4
            var swap3And1 = false;
            tempSphere = CreateFrom3Points(points[0], points[2], points[3]);
            if ((points[1] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && (points[4] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap3And1 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,2,4 & check 1 & 3
            var swap1With4 = false;
            tempSphere = CreateFrom3Points(points[0], points[2], points[4]);
            if ((points[1] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && (points[3] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap1With4 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,3,4 & check 1 & 2
            var swap12With34 = false;
            tempSphere = CreateFrom3Points(points[0], points[3], points[4]);
            if ((points[1] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && (points[2] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap12With34 = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // now the 4-point spheres
            var fourPointIsBest = false;
            // 0,1,2,3 & check 4
            tempSphere = CreateFrom4Points(points[0], points[1], points[2], points[3]);
            if (!(points[4] - tempSphere.Center).LengthSquared().IsGreaterThanNonNegligible(tempSphere.RadiusSquared)
                // this one uses IsGreaterThanNonNegligible to prevent infinite cycling when more points are on the sphere
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                fourPointIsBest = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,1,2,4 & check 3
            var swap4And3 = false;
            tempSphere = CreateFrom4Points(points[0], points[1], points[2], points[4]);
            if ((points[3] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap4And3 = fourPointIsBest = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,1,3,4 & check 2
            var swap4With2 = false;
            tempSphere = CreateFrom4Points(points[0], points[1], points[3], points[4]);
            if ((points[2] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap4With2 = fourPointIsBest = true;
                minRadiusSqd = sphere.RadiusSquared;
            }
            // 0,2,3,4 & check 1
            var swap4With1 = false;
            tempSphere = CreateFrom4Points(points[0], points[2], points[3], points[4]);
            if ((points[1] - tempSphere.Center).LengthSquared() <= tempSphere.RadiusSquared
                && tempSphere.RadiusSquared < minRadiusSqd)
            {
                sphere = tempSphere;
                swap4With1 = fourPointIsBest = true;
                //minRadiusSqd = sphere.RadiusSquared;
                // don't need this anymore...no more checks
            }

            if (fourPointIsBest)
            {
                if (swap4With1) Constants.SwapItemsInList(4, 1, points);
                else if (swap4With2) Constants.SwapItemsInList(4, 2, points);
                else if (swap4And3) Constants.SwapItemsInList(4, 3, points);
            }
            else
            {
                if (swap12With34)
                {
                    Constants.SwapItemsInList(3, 1, points);
                    Constants.SwapItemsInList(4, 2, points);
                }
                else if (swap1With4)
                    Constants.SwapItemsInList(1, 4, points);
                else if (swap3And1) Constants.SwapItemsInList(1, 3, points);
                else if (swap4And2) Constants.SwapItemsInList(4, 2, points);
                else if (swap3And2) Constants.SwapItemsInList(3, 2, points);
            }
            return sphere;
        }



        private static sphereLight CreateFrom4Points(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            /* see details in Sphere version on this function */
            var matrix = new Matrix3x3(
                2 * (p2.X - p1.X), 2 * (p2.Y - p1.Y), 2 * (p2.Z - p1.Z),
                2 * (p3.X - p2.X), 2 * (p3.Y - p2.Y), 2 * (p3.Z - p2.Z),
                2 * (p4.X - p3.X), 2 * (p4.Y - p3.Y), 2 * (p4.Z - p3.Z));
            var b = new Vector3(
                p2.X * p2.X - p1.X * p1.X + p2.Y * p2.Y - p1.Y * p1.Y + p2.Z * p2.Z - p1.Z * p1.Z,
                p3.X * p3.X - p2.X * p2.X + p3.Y * p3.Y - p2.Y * p2.Y + p3.Z * p3.Z - p2.Z * p2.Z,
                p4.X * p4.X - p3.X * p3.X + p4.Y * p4.Y - p3.Y * p3.Y + p4.Z * p4.Z - p3.Z * p3.Z);
            var center = matrix.Solve(b);
            return new sphereLight(center, (p1 - center).LengthSquared());
        }

        private static sphereLight CreateFrom3Points(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            var planeNormal = (p2 - p1).Cross(p3 - p1).Normalize();
            var midPoint1 = 0.5 * (p1 + p2);
            var planeDist = planeNormal.Dot(midPoint1);
            var normal1 = (p2 - p1).Normalize();
            var dist1 = normal1.Dot(midPoint1);
            var midPoint2 = 0.5 * (p1 + p3);
            var normal2 = (p3 - p1).Normalize();
            var dist2 = normal2.Dot(midPoint2);
            var center = MiscFunctions.PointCommonToThreePlanes(planeNormal, planeDist, normal1, dist1, normal2,
                dist2);
            return new sphereLight(center, (p1 - center).LengthSquared());
        }
        private static sphereLight CreateFrom2Points(Vector3 p1, Vector3 p2)
        {
            var center = 0.5 * (p1 + p2);
            return new sphereLight(center, (p1 - center).LengthSquared());
        }
    }
}
