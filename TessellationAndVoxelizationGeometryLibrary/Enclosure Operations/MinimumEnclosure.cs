// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-15-2015
// ***********************************************************************
// <copyright file="MinimumBoundingBox.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Enclosure_Operations;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a 
    /// tesselated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        private const int MaxRotationsForOBB = 24;

        /// <summary>
        /// Finds the minimum bounding rectangle given a set of points. Either send any set of points
        /// OR the convex hull 2D. 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="pointsAreConvexHull"></param>
        /// <returns></returns>
        public static BoundingRectangle BoundingRectangle(IList<Point> points, bool pointsAreConvexHull = false)
        {
            return RotatingCalipers2DMethod(points, pointsAreConvexHull);
        }
        /// <summary>
        /// Finds the minimum bounding box.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(TessellatedSolid ts)
        {
            return OrientedBoundingBox(ts.ConvexHullVertices);
        }

        /// <summary>
        /// Finds the minimum bounding box.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="convexHullFaces">The convex hull faces.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(IList<Vertex> convexHullVertices)
        {
            // here we create 13 directions. Why 13? basically it is all ternary combinations of x,y,and z.
            // skipping symmetric and 0,0,0. Another way to think of it is to make a direction from a cube with
            // vectors emanating from every vertex, edge, and face. that would be 8+12+6 = 26. And since there
            // is no need to do mirror image directions this is 26/2 or 13.
            var directions = new List<double[]>();
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                    directions.Add(new[] { 1.0, i, j });
            directions.Add(new[] { 0.0, 0, 1 });
            directions.Add(new[] { 0.0, 1, 0 });
            directions.Add(new[] { 0.0, 1, 1 });
            directions.Add(new[] { 0.0, -1, 1 });

            var boxes = directions.Select(v => new BoundingBox
            {
                Volume = double.PositiveInfinity,
                Directions = new[] { v, null, null }
            }).ToList();
            for (int i = 0; i < 13; i++)
                boxes[i] = Find_via_ChanTan_AABB_Approach(convexHullVertices, boxes[i]);
            var minVol = boxes.Min(box => box.Volume);
            return boxes.First(box => box.Volume == minVol);
        }


        #region ChanTan AABB Approach

        private static BoundingBox Find_via_ChanTan_AABB_Approach(IList<Vertex> convexHullVertices, BoundingBox minOBB)
        {
            var failedConsecutiveRotations = 0;
            var k = 0;
            var i = 0;
            do
            {
                //Find new OBB along OBB.direction2 and OBB.direction3, keeping the best OBB.
                var newObb = FindOBBAlongDirection(convexHullVertices, minOBB.Directions[i++]);
                if (newObb.Volume.IsLessThanNonNegligible(minOBB.Volume))
                {
                    minOBB = newObb;
                    failedConsecutiveRotations = 0;
                }
                else failedConsecutiveRotations++;
                if (i == 3) i = 0;
                k++;
            } while (failedConsecutiveRotations < 3 && k < MaxRotationsForOBB);
            return minOBB;
        }
        #endregion


        #region Find OBB Along Direction
        /// <summary>
        /// Finds the minimum oriented bounding rectangle (2D). The 3D points of a tessellated solid
        /// are projected to the plane defined by "direction". This returns a BoundingBox structure
        /// where the first direction is the same as the prescribed direction and the other two are
        /// in-plane unit vectors.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>BoundingBox.</returns>
        /// <exception cref="System.Exception"></exception>
        public static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction)
        {
            Vertex v1Low, v1High;
            var depth = GetLengthAndExtremeVertices(direction, vertices, out v1Low, out v1High);
            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(vertices, direction, out backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);
            //Get reference vertices from boundingRectangle
            var v2Low = boundingRectangle.PointPairs[0][0].References[0];
            var v2High = boundingRectangle.PointPairs[0][1].References[0];
            var v3Low = boundingRectangle.PointPairs[1][0].References[0];
            var v3High = boundingRectangle.PointPairs[1][1].References[0];

            //Get the direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.Directions[0][0], boundingRectangle.Directions[0][1],
                boundingRectangle.Directions[0][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction2 = new[] { tempDirection[0], tempDirection[1], tempDirection[2] };
            tempDirection = new[]
            {
                boundingRectangle.Directions[1][0], boundingRectangle.Directions[1][1],
                boundingRectangle.Directions[1][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction3 = new[] { tempDirection[0], tempDirection[1], tempDirection[2] };
            var direction1 = direction2.crossProduct(direction3);
            depth = GetLengthAndExtremeVertices(direction1, vertices, out v1Low, out v1High);
            //todo: Fix Get2DProjectionPoints, which seems to be transforming the points to 2D, but not normal to
            //the given direction vector. If it was normal, direction1 should equal direction or its direction.inverse.

            return new BoundingBox(depth, boundingRectangle.Area, new[] { v1Low, v1High, v2Low, v2High, v3Low, v3High },
                new[] { direction1, direction2, direction3 }, boundingRectangle.EdgeVertices);
        }
        #endregion

        #region Get Length And Extreme Vertices
        /// <summary>
        /// Given a direction, dir, this function returns the maximum length along this direction
        /// for the provided vertices as well as the two vertices that represent the extremes.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="vLow">The v low.</param>
        /// <param name="vHigh">The v high.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertices(double[] direction, IList<Vertex> vertices, out Vertex vLow, out Vertex vHigh)
        {
            var dir = direction.normalize();
            var dotProducts = new double[vertices.Count];
            var i = 0;
            foreach (var v in vertices)
                dotProducts[i++] = dir.dotProduct(v.Position);
            var min_d = dotProducts.Min();
            var max_d = dotProducts.Max();
            vLow = vertices[dotProducts.FindIndex(min_d)];
            vHigh = vertices[dotProducts.FindIndex(max_d)];
            return max_d - min_d;
        }
        #endregion

        #region 2D Rotating Calipers
        /// <summary>
        /// Rotating the calipers2 d method.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull"></param>
        /// <returns>System.Double.</returns>
        private static BoundingRectangle RotatingCalipers2DMethod(IList<Point> points, bool pointsAreConvexHull = false)
        {
            #region Initialization
            var cvxPoints = pointsAreConvexHull ? points : ConvexHull2D(points);
            var numCvxPoints = cvxPoints.Count;
            var extremeIndices = new int[4];

            //        extremeIndices[3] => max-Y
            extremeIndices[3] = cvxPoints.Count - 1;
            //Check if first point has a higher y value (only when point is both min-x and max-Y)
            if (cvxPoints[0][1] > cvxPoints[extremeIndices[3]][1]) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] >= 1 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1])
                    extremeIndices[3]--;
            }

            //        extremeIndices[2] => max-X
            extremeIndices[2] = extremeIndices[3];
            while (extremeIndices[2] >= 1 && cvxPoints[extremeIndices[2]][0] <= cvxPoints[extremeIndices[2] - 1][0])
                extremeIndices[2]--;


            //        extremeIndices[1] => min-Y
            extremeIndices[1] = extremeIndices[2];
            while (extremeIndices[1] >= 1 && cvxPoints[extremeIndices[1]][1] >= cvxPoints[extremeIndices[1] - 1][1])
                extremeIndices[1]--;

            //        extremeIndices[0] => min-X 
            // A bit more complicated, since it needs to look past the zero index.
            var currentIndex = -1;
            var previousIndex = -1;
            var stallCounter = 0;
            extremeIndices[0] = extremeIndices[1];
            do
            {
                currentIndex = extremeIndices[0];
                extremeIndices[0]--;
                if (extremeIndices[0] < 0) { extremeIndices[0] = numCvxPoints - 1; }
                previousIndex = extremeIndices[0];
                stallCounter++;
            } while (cvxPoints[currentIndex][0] >= cvxPoints[previousIndex][0] && stallCounter < points.Count());
            extremeIndices[0]++;
            if (extremeIndices[0] > numCvxPoints - 1) { extremeIndices[0] = 0; }

            #endregion

            #region Cycle through 90-degrees
            var angle = 0.0;
            var bestAngle = double.NegativeInfinity;
            var direction1 = new double[3];
            var direction2 = new double[3];
            var deltaToUpdateIndex = -1;
            var deltaAngles = new double[4];
            var offsetAngles = new[] { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };
            Point[] pointPair1 = null;
            Point[] pointPair2 = null;
            Vertex edgeVertex1 = null;
            Vertex edgeVertex2 = null;
            var minArea = double.PositiveInfinity;
            var flag = false;
            var cons = Math.PI / 2;
            do
            {
                #region update the deltaAngles from the current orientation
                //For each of the 4 supporting points (those forming the rectangle),
                for (var i = 0; i < 4; i++)
                {
                    //Update all angles on first pass. For each additional pass, only update one deltaAngle.
                    if (deltaToUpdateIndex == -1 || i == deltaToUpdateIndex)
                    {
                        var index = extremeIndices[i];
                        var prev = (index == 0) ? numCvxPoints - 1 : index - 1;
                        var tempDelta = Math.Atan2(cvxPoints[prev][1] - cvxPoints[index][1],
                             cvxPoints[prev][0] - cvxPoints[index][0]);
                        deltaAngles[i] = offsetAngles[i] - tempDelta;
                        //If the angle has rotated beyond the 90 degree bounds, it will be negative
                        //And should never be chosen from then on.
                        if (deltaAngles[i] < 0) { deltaAngles[i] = 2 * Math.PI; }
                    }
                }
                var delta = deltaAngles.Min();
                angle = delta;
                if (angle > Math.PI / 2 && !angle.IsPracticallySame(Math.PI / 2))
                {
                    flag = true; //Exit while
                    continue;
                }

                deltaToUpdateIndex = deltaAngles.FindIndex(delta);
                #endregion

                var currentPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];
                extremeIndices[deltaToUpdateIndex]--;
                if (extremeIndices[deltaToUpdateIndex] < 0) { extremeIndices[deltaToUpdateIndex] = numCvxPoints - 1; }
                var previousPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];

                #region find area
                //Get unit normal for current edge
                var direction = previousPoint.Position2D.subtract(currentPoint.Position2D).normalize();
                //If point type = 1 or 3, then use inversed direction
                if (deltaToUpdateIndex == 1 || deltaToUpdateIndex == 3) { direction = new[] { -direction[1], direction[0] }; }
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[2]][0] - cvxPoints[extremeIndices[0]][0],
                    cvxPoints[extremeIndices[2]][1] - cvxPoints[extremeIndices[0]][1]
                };

                var angleVector1 = new[] { -direction[1], direction[0] };
                var width = Math.Abs(vectorWidth.dotProduct(angleVector1));
                var vectorHeight = new[]
                {
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0],
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                var angleVector2 = new[] { direction[0], direction[1] };
                var height = Math.Abs(vectorHeight.dotProduct(angleVector2));
                var tempArea = height * width;
                #endregion

                if (minArea > tempArea)
                {
                    minArea = tempArea;
                    bestAngle = angle;
                    pointPair1 = new[] { cvxPoints[extremeIndices[2]], cvxPoints[extremeIndices[0]] };
                    pointPair2 = new[] { cvxPoints[extremeIndices[3]], cvxPoints[extremeIndices[1]] };
                    direction1 = new[] { angleVector1[0], angleVector1[1], 0.0 };
                    direction2 = new[] { angleVector2[0], angleVector2[1], 0.0 };
                    edgeVertex1 = previousPoint.References[0];
                    edgeVertex2 = currentPoint.References[0];
                }

            } while (!flag || angle.IsPracticallySame(Math.PI / 2)); //Don't check beyond a 90 degree angle.
                                                                     //If best angle is 90 degrees, then don't bother to rotate. 
            if (bestAngle.IsPracticallySame(Math.PI / 2)) { bestAngle = 0.0; }
            #endregion

            var directions = new List<double[]> { direction1, direction2 };
            var extremePoints = new List<Point[]> { pointPair1, pointPair2 };
            if (pointPair1 == null) minArea = 0.0;
            var boundingRectangle = new BoundingRectangle(minArea, bestAngle, directions, extremePoints, new[] { edgeVertex1, edgeVertex2 });
            return boundingRectangle;
        }
        #endregion
    }
}