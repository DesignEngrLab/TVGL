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
        /// Finds the minimum bounding circle
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">Bounding circle failed to converge</exception>
        /// <references>
        /// Based on Emo Welzl's "move-to-front heuristic" and this paper (algorithm 1).
        /// http://www.inf.ethz.ch/personal/gaertner/texts/own_work/esa99_final.pdf
        /// This algorithm runs in near linear time. Visiting most points just a few times.
        /// Though a linear algorithm was found by Meggi do, this algorithm is more robust
        /// (doesn't care about multiple points on a line and fewer rounding functions)
        /// and directly applicable to multiple dimensions (in our case, just 2 and 3 D).
        /// </references>
        public static Circle MinimumCircleMC(this IEnumerable<Vector2> pointsInput)
        {
            var points = pointsInput.ToArray();
            var numPoints = points.Length;
            if (numPoints == 0)
                throw new ArgumentException("No points provided.");
            else if (numPoints <= 4)
                return FindCircle(points, out _);

            // make a circle from the first three points
            var circle = FirstCircle(points, out var numPointsInCircle);
            var maxDistSqared = circle.RadiusSquared;
            bool newPointFoundOutsideCircle;
            var indexOfMaxDist = -1;
            do
            {
                newPointFoundOutsideCircle = false;
                for (int i = numPointsInCircle; i < numPoints; i++)
                {
                    var dist = (points[i] - circle.Center).LengthSquared();

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
                    circle = FindCircle(points, out numPointsInCircle);
                    maxDistSqared = circle.RadiusSquared;
                }
            } while (newPointFoundOutsideCircle);
            return circle;
        }

        private static Circle FirstCircle(Vector2[] points, out int numInCircle)
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
            {
                numInCircle = 2;
                return circle;
            }
            circle = Circle.CreateFrom2Points(points[0], points[2]);
            if ((points[^2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                numInCircle = 2;
                // since 0 and 2 are furthest apart, we need to swap 1 and 2
                // so that the two points in the circle are at the beinning of the list
                Constants.SwapItemsInList(1, 2, points);
                return circle;
            }
            circle = Circle.CreateFrom2Points(points[1], points[2]);
            if ((points[^2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                numInCircle = 2;
                // since 1 and 2 are furthest apart, we need to swap 0 and 2
                // so that the two points in the circle are at the beinning of the list
                Constants.SwapItemsInList(0, 2, points);
                return circle;
            }
            // otherwise, it's the 3-point circle
            numInCircle = 3;
            Circle.CreateFrom3Points(points[0], points[1], points[2], out circle);
            return circle;
        }
        private static Circle FindCircle(Vector2[] points, out int numInCircle)
        { // else if (numInCircle == 4)
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
            {
                numInCircle = 2;
                return circle;
            }
            // 2. make the 0-2 circle and check with 1 & 3
            circle = Circle.CreateFrom2Points(points[0], points[2]);
            if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
                && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                numInCircle = 2;
                Constants.SwapItemsInList(1, 2, points);
                return circle;
            }
            // 3. make the 0-3 circle and check with 1 & 2
            circle = Circle.CreateFrom2Points(points[0], points[3]);
            if ((points[1] - circle.Center).LengthSquared() <= circle.RadiusSquared
                && (points[2] - circle.Center).LengthSquared() <= circle.RadiusSquared)
            {
                numInCircle = 2;
                Constants.SwapItemsInList(1, 3, points);
                return circle;
            }
            numInCircle = 3;
            Circle tempCircle;
            // circle 0-1-2
            var minRadiusSqd = double.PositiveInfinity;
            if (Circle.CreateFrom3Points(points[0], points[1], points[2], out circle)
                && (points[3] - circle.Center).LengthSquared() <= circle.RadiusSquared)
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
            // circle 0-1-3
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
    }
}
