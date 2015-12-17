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
            return AddInCornerVertices(OrientedBoundingBox(ts.ConvexHull.Vertices));
            //return AddInCornerVertices(OrientedBoundingBox(ts.ConvexHull));
        }

        /// <summary>
        /// Finds the minimum bounding box.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(IList<Vertex> convexHullVertices)
        {
            // here we create 13 directions. Why 13? basically it is all ternary combinations of x,y,and z.
            // skipping symmetric and 0,0,0. Another way to think of it is to make a Direction from a cube with
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
                Directions = new[] { v },
                Volume = double.PositiveInfinity
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
        /// are projected to the plane defined by "Direction". This returns a BoundingBox structure
        /// where the first Direction is the same as the prescribed Direction and the other two are
        /// in-plane unit vectors.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The Direction.</param>
        /// <returns>BoundingBox.</returns>
        /// <exception cref="System.Exception"></exception>
        public static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction, Vertex vDir1 = null, Vertex vDir2 = null)
        {
            List<Vertex> bottomVertices, topVertices;
            var direction1 = direction.normalize();
            var depth = GetLengthAndExtremeVertices(direction, vertices, out bottomVertices, out topVertices);

            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(vertices, direction, out backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);

            //Get the Direction vectors from rotating caliper and projection.
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
            var pointsOnFaces = new List<List<Vertex>>
            {
                bottomVertices,topVertices,
                boundingRectangle.PointsOnSides[0].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[1].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[2].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[3].SelectMany(p => p.References).ToList()
            };
            return new BoundingBox
            {
                Dimensions = new[] { depth, boundingRectangle.Dimensions[0], boundingRectangle.Dimensions[1] },
                Directions = new[] { direction1, direction2, direction3 },
                PointsOnFaces = pointsOnFaces.ToArray(),
                Volume = depth * boundingRectangle.Dimensions[0] * boundingRectangle.Dimensions[1]
            };
        }

        private static void FindOBBAlongDirection(BoundingBoxData boxData)
        {
            var direction0 = boxData.Direction = boxData.Direction.normalize();
            var height = direction0.dotProduct(boxData.rotatorEdge.From.Position.subtract(boxData.backVertex.Position));
            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(boxData.orthVertices, direction0, out backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);
           //Get the Direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.Directions[0][0], boundingRectangle.Directions[0][1],
                boundingRectangle.Directions[0][2], 1.0
            };
            var direction1 = backTransform.multiply(tempDirection).Take(3).ToArray();
            tempDirection = new[]
            {
                boundingRectangle.Directions[1][0], boundingRectangle.Directions[1][1],
                boundingRectangle.Directions[1][2], 1.0
            };
            var direction2 = backTransform.multiply(tempDirection).Take(3).ToArray();
            boxData.box =
                new BoundingBox
                {
                    Dimensions = new[] { height, boundingRectangle.Dimensions[0], boundingRectangle.Dimensions[1] },
                    Directions = new[] { direction0, direction1, direction2 },
                    PointsOnFaces = new[]
                    {
                        new List<Vertex> {boxData.rotatorEdge.From, boxData.rotatorEdge.To},
                        new List<Vertex> {boxData.backVertex},
                        boundingRectangle.PointsOnSides[0].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[1].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[2].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[3].SelectMany(p => p.References).ToList()
                    },
                    Volume = height * boundingRectangle.Dimensions[0] * boundingRectangle.Dimensions[1]
                };
        }

        #endregion

        #region Get Length And Extreme Vertices
        /// <summary>
        /// Given a Direction, dir, this function returns the maximum length along this Direction
        /// for the provided vertices as well as the two vertices that represent the extremes.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="vLow">The v low.</param>
        /// <param name="vHigh">The v high.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertices(double[] direction, IList<Vertex> vertices, out List<Vertex> bottomVertices,
            out List<Vertex> topVertices)
        {
            var dir = direction.normalize();
            var min_d = double.PositiveInfinity;
            bottomVertices = new List<Vertex>();
            topVertices = new List<Vertex>();
            var max_d = double.NegativeInfinity;
            var i = 0;
            foreach (var v in vertices)
            {
                var distance = dir.dotProduct(v.Position);
                if (distance.IsPracticallySame(min_d, Constants.BaseTolerance))
                    bottomVertices.Add(v);
                else if (distance < min_d)
                {
                    bottomVertices.Clear(); bottomVertices.Add(v);
                    min_d = distance;
                }
                if (distance.IsPracticallySame(max_d, Constants.BaseTolerance))
                    bottomVertices.Add(v);
                else if (distance > max_d)
                {
                    topVertices.Clear(); topVertices.Add(v);
                    max_d = distance;
                }
            }
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

            // extremeIndices[3] => max-Y
            extremeIndices[3] = cvxPoints.Count - 1;
            //Check if first point has a higher y value (only when point is both min-x and max-Y)
            if (cvxPoints[0][1] > cvxPoints[extremeIndices[3]][1]) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] > 0 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1])
                    extremeIndices[3]--;
            }
            // extremeIndices[2] => max-X
            extremeIndices[2] = extremeIndices[3] == 0 ? cvxPoints.Count - 1 : extremeIndices[3];
            while (extremeIndices[2] > 0 && cvxPoints[extremeIndices[2]][0] <= cvxPoints[extremeIndices[2] - 1][0])
                extremeIndices[2]--;
            // extremeIndices[1] => min-Y
            extremeIndices[1] = extremeIndices[2] == 0 ? cvxPoints.Count - 1 : extremeIndices[2];
            while (extremeIndices[1] > 0 && cvxPoints[extremeIndices[1]][1] >= cvxPoints[extremeIndices[1] - 1][1])
                extremeIndices[1]--;
            // extrememIndices[0] => min-X, this time count up from 0. The answers likely 0 but in case there are ties.
            extremeIndices[0] = 0;
            while (cvxPoints[extremeIndices[0]][0] >= cvxPoints[extremeIndices[0] + 1][0])
                extremeIndices[0]++;
            #endregion

            #region Cycle through 90-degrees

            var deltaAngles = new double[4];
            var offsetAngles = new[] { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };
            var bestRectangle = new BoundingRectangle { Area = double.PositiveInfinity };
            do
            {
                #region update the deltaAngles from the current orientation
                //For each of the 4 supporting points (those forming the rectangle),
                for (var i = 0; i < 4; i++)
                {
                    var index = extremeIndices[i];
                    var prev = (index == 0) ? numCvxPoints - 1 : index - 1;
                    var tempDelta = Math.Atan2(cvxPoints[prev][1] - cvxPoints[index][1],
                         cvxPoints[prev][0] - cvxPoints[index][0]);
                    deltaAngles[i] = offsetAngles[i] - tempDelta;
                    //If the angle has rotated beyond the 90 degree bounds, it will be negative
                    //And should never be chosen from then on.
                    if (deltaAngles[i] < 0) deltaAngles[i] = double.PositiveInfinity;
                }
                var angle = deltaAngles.Min();
                if (angle.IsGreaterThanNonNegligible(Math.PI / 2))
                    break;
                var refIndex = deltaAngles.FindIndex(angle);
                #endregion

                #region find area
                //Get unit normal for current edge
                var otherIndex = (extremeIndices[refIndex] == 0) ? numCvxPoints - 1 : extremeIndices[refIndex] - 1;
                var direction = cvxPoints[extremeIndices[refIndex]].Position2D.subtract(cvxPoints[otherIndex].Position2D).normalize();
                //If point type = 1 or 3, then use inversed Direction
                if (refIndex == 1 || refIndex == 3) { direction = new[] { -direction[1], direction[0] }; }
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
                var area = height * width;
                #endregion

                var xDir = new[] { angleVector1[0], angleVector1[1], 0.0 };
                var yDir = new[] { angleVector2[0], angleVector2[1], 0.0 };
                var pointsOnSides = new List<Point>[4];
                for (int i = 0; i < 4; i++)
                {
                    pointsOnSides[i] = new List<Point>();
                    var dir = (i % 2 == 0) ? xDir : yDir;
                    var distance = cvxPoints[extremeIndices[i]].Position.dotProduct(dir);
                    var prevIndex = extremeIndices[i];
                    do
                    {
                        extremeIndices[i] = prevIndex;
                        pointsOnSides[i].Add(cvxPoints[extremeIndices[i]]);
                        prevIndex = (extremeIndices[i] == 0) ? numCvxPoints - 1 : extremeIndices[i] - 1;
                    } while (distance.IsPracticallySame(cvxPoints[prevIndex].Position.dotProduct(dir), Constants.BaseTolerance));
                }

                if (bestRectangle.Area > area)
                {
                    bestRectangle.PointsOnSides = pointsOnSides;
                    bestRectangle.Area = area;
                    bestRectangle.Dimensions = new[] { width, height };
                    bestRectangle.Directions = new[] { xDir, yDir };
                }

            } while (true); //process will end on its own by the break statement in line 263
            #endregion
            return bestRectangle;
        }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="extremeVertices">The extreme vertices.</param>
        /// <param name="directions"></param>
        private static BoundingBox MakeBoundingBox(IList<double> dimensions, IList<double[]> directions,
            IList<List<Vertex>> pointsOnFaces)
        {
            var volume = double.PositiveInfinity;
            if (dimensions == null)
                dimensions = new double[3];
            else volume = dimensions[0] * dimensions[1] * dimensions[2];

            if (directions == null) directions = new double[3][];
            else directions = directions.Select(d => d.normalize()).ToArray();

            if (pointsOnFaces == null)
                pointsOnFaces = new List<Vertex>[6];
            return new BoundingBox
            {
                Dimensions = dimensions.ToArray(),
                Directions = directions.ToArray(),
                PointsOnFaces = pointsOnFaces.ToArray(),
                Volume = volume
            };
        }
        private static BoundingBox AddInCornerVertices(BoundingBox bb)
        {
            if (bb.CornerVertices != null) return bb;
            var cornerVertices = new Point[8];
            var normalMatrix = new[,] {{bb.Directions[0][0],bb.Directions[1][0],bb.Directions[2][0]},
                                        {bb.Directions[0][1],bb.Directions[1][1],bb.Directions[2][1]},
                                        {bb.Directions[0][2],bb.Directions[1][2],bb.Directions[2][2]}};
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                var tempVect = normalMatrix.transpose().multiply(bb.PointsOnFaces[i][0].Position);
                var xPrime = tempVect[0];
                for (var j = 0; j < 2; j++)
                {
                    tempVect = normalMatrix.transpose().multiply(bb.PointsOnFaces[j + 2][0].Position);
                    var yPrime = tempVect[1];
                    for (var k = 0; k < 2; k++)
                    {
                        tempVect = normalMatrix.transpose().multiply(bb.PointsOnFaces[k + 4][0].Position);
                        var zPrime = tempVect[2];
                        var offAxisPosition = new[] { xPrime, yPrime, zPrime };
                        //Rotate back into primary coordinates
                        var position = normalMatrix.multiply(offAxisPosition);
                        cornerVertices[count] = new Point(position);
                        count++;
                    }
                }
            }
            return new BoundingBox
            {
                CornerVertices = cornerVertices,
                Dimensions = bb.Dimensions,
                Directions = bb.Directions,
                PointsOnFaces = bb.PointsOnFaces,
                Volume = bb.Volume
            };
        }
    }
}