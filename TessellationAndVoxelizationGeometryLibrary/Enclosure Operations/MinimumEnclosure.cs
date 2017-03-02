// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="MinimumBoundingBox.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tesselated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     The maximum rotations for obb
        /// </summary>
        private const int MaxRotationsForOBB = 24;

        /// <summary>
        ///     Finds the minimum bounding rectangle given a set of points. Either send any set of points
        ///     OR the convex hull 2D.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull">if set to <c>true</c> [points are convex hull].</param>
        /// <returns>BoundingRectangle.</returns>
        public static BoundingRectangle BoundingRectangle(IList<Point> points, bool pointsAreConvexHull = false)
        {
            return RotatingCalipers2DMethod(points, pointsAreConvexHull);
        }

        /// <summary>
        ///     Finds the minimum bounding box.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(TessellatedSolid ts)
        {
            return OrientedBoundingBox(ts.ConvexHull.Vertices);
        }

        /// <summary>
        ///     Finds the minimum bounding box.
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
            for (var i = -1; i <= 1; i++)
                for (var j = -1; j <= 1; j++)
                    directions.Add(new[] {1.0, i, j}.normalize());
            directions.Add(new[] {0.0, 0, 1}.normalize());
            directions.Add(new[] {0.0, 1, 0}.normalize());
            directions.Add(new[] {0.0, 1, 1}.normalize());
            directions.Add(new[] {0.0, -1, 1}.normalize());

            var boxes = directions.Select(v => new BoundingBox
            {
                Directions = new[] {v},
                Volume = double.PositiveInfinity
            }).ToList();
            for (var i = 0; i < 13; i++)
                boxes[i] = Find_via_ChanTan_AABB_Approach(convexHullVertices, boxes[i]);
            var minVolume = double.PositiveInfinity;
            var minBox = boxes[0];

            foreach (var box in boxes)
            {
                if (box.Volume >= minVolume) continue;
                minVolume = box.Volume;
                minBox = box;
            }
            return AddInCornerVertices(minBox);
        }

        #region ChanTan AABB Approach

        /// <summary>
        ///     Find_via_s the chan tan_ aab b_ approach.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="minOBB">The minimum obb.</param>
        /// <returns>BoundingBox.</returns>
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

        #region Get Length And Extreme Vertices

        /// <summary>
        ///     Given a Direction, dir, this function returns the maximum length along this Direction
        ///     for the provided vertices as well as the two vertices that represent the extremes.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="bottomVertices">The bottom vertices.</param>
        /// <param name="topVertices">The top vertices.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertices(double[] direction, IList<Vertex> vertices,
            out List<Vertex> bottomVertices,
            out List<Vertex> topVertices)
        {
            var dir = direction.normalize();
            var minD = double.PositiveInfinity;
            bottomVertices = new List<Vertex>();
            topVertices = new List<Vertex>();
            var maxD = double.NegativeInfinity;
            foreach (var v in vertices)
            {
                var distance = dir.dotProduct(v.Position);
                if (distance.IsPracticallySame(minD, Constants.BaseTolerance))
                    bottomVertices.Add(v);
                else if (distance < minD)
                {
                    bottomVertices.Clear();
                    bottomVertices.Add(v);
                    minD = distance;
                }
                if (distance.IsPracticallySame(maxD, Constants.BaseTolerance))
                    bottomVertices.Add(v);
                else if (distance > maxD)
                {
                    topVertices.Clear();
                    topVertices.Add(v);
                    maxD = distance;
                }
            }
            return maxD - minD;
        }

        #endregion

        #region 2D Rotating Calipers

        /// <summary>
        ///     Rotating the calipers 2D method. Convex hull must be a counter clockwise loop.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull">if set to <c>true</c> [points are convex hull].</param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">
        ///     Area should never be negligilbe unless data is messed up.
        /// </exception>
        private static BoundingRectangle RotatingCalipers2DMethod(IList<Point> points, bool pointsAreConvexHull = false)
        {
            #region Initialization

            var cvxPoints = pointsAreConvexHull ? points : ConvexHull2D(points);
            //Simplify the points to make sure they are the minimal convex hull
            cvxPoints = PolygonOperations.SimplifyFuzzy(cvxPoints);
            /* the cvxPoints will be arranged from a point with minimum X-value around in a CCW loop to the last point */
            //First, check to make sure the given convex hull has the min x-value at 0.
            var minX = cvxPoints[0].X;
            var numCvxPoints = cvxPoints.Count;
            var startIndex = 0;
            for (var i = 1; i < numCvxPoints; i++)
            {
                if (!(cvxPoints[i].X < minX)) continue;
                minX = cvxPoints[i].X;
                startIndex = i;
            }
            //Reorder if necessary
            var tempList = new List<Point>();
            if (startIndex != 0)
            {
                for (var i = startIndex; i < numCvxPoints; i++)
                {
                    tempList.Add(cvxPoints[i]);
                }
                for (var i = 0; i < startIndex; i++)
                {
                    tempList.Add(cvxPoints[i]);
                }
                cvxPoints = tempList;
            }
            

            var extremeIndices = new int[4];

            //Good picture of extreme vertices in the following link
            //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.155.5671&rep=rep1&type=pdf
            //Godfried Toussaint: Solving Geometric Problems with the Rotating Calipers
            //Note that while these points are ordered counter clockwise, we are rotating the calipers in reverse (clockwise),
            //Which is why the points are directed this way.
            //Point0 = min X, with max Y for ties
            //Point1 = min Y, with min X for ties
            //Point2 = max X, with min Y for ties
            //Point3 = max Y, with max X for ties

            // extremeIndices[3] => max-Y, with max X for ties
            extremeIndices[3] = cvxPoints.Count - 1;
            // this is likely rare, but first we check if the first point has a higher y value (only when point is both min-x and max-Y)
            if (cvxPoints[0][1] > cvxPoints[extremeIndices[3]][1]) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] > 0 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1])
                    extremeIndices[3]--;
            }
            /* at this point, the max-Y point has been established. Next we walk backwards in the list until we hit the max-X point */
            // extremeIndices[2] => max-X, with min Y for ties
            extremeIndices[2] = extremeIndices[3] == 0 ? cvxPoints.Count - 1 : extremeIndices[3];
            while (extremeIndices[2] > 0 && cvxPoints[extremeIndices[2]][0] <= cvxPoints[extremeIndices[2] - 1][0])
                extremeIndices[2]--;
            // extremeIndices[1] => min-Y, with min X for ties 
            extremeIndices[1] = extremeIndices[2] == 0 ? cvxPoints.Count - 1 : extremeIndices[2];
            while (extremeIndices[1] > 0 && cvxPoints[extremeIndices[1]][1] >= cvxPoints[extremeIndices[1] - 1][1])
                extremeIndices[1]--;
            // extrememIndices[0] => min-X, with max Y for ties
            // First we check if the last point has an eqaully small x value, if it does we will need to walk backwards.
            if (cvxPoints.Last()[0] > cvxPoints[0][0]) extremeIndices[0] = 0;
            else
            {
                extremeIndices[0] = cvxPoints.Count - 1;
                while (cvxPoints[extremeIndices[0]][0] >= cvxPoints[extremeIndices[0] - 1][0])
                    extremeIndices[0]--;
            }

            ////Check code: this validates the extreme vertices and the convex hull.
            //var extremeIndicesValidation = new int[4];
            //extremeIndicesValidation[0] = 0; //This was minX from the intitialization
            //var minY = double.PositiveInfinity;
            //var maxX = double.NegativeInfinity;
            //var maxY = double.NegativeInfinity;
            //for (var i = 0; i < numCvxPoints; i++)
            //{
            //    //Point0 = min X
            //    if (cvxPoints[i][0] < minX)
            //    {
            //        minX = cvxPoints[i][0];
            //        extremeIndicesValidation[0] = i;
            //    }
            //    else if (cvxPoints[i][0] == minX)
            //    {
            //        //with max Y for ties
            //        if (cvxPoints[i][1] > cvxPoints[extremeIndicesValidation[0]][1])
            //        {
            //            minX = cvxPoints[i][0];
            //            extremeIndicesValidation[0] = i;
            //        }
            //    }
            //    //Point1 = min Y
            //    if (cvxPoints[i][1] < minY)
            //    {
            //        minY = cvxPoints[i][1];
            //        extremeIndicesValidation[1] = i;
            //    }
            //    else if (cvxPoints[i][1] == minY)
            //    {
            //        //with min X for ties
            //        if (cvxPoints[i][0] < cvxPoints[extremeIndicesValidation[1]][0])
            //        {
            //            minY = cvxPoints[i][1];
            //            extremeIndicesValidation[1] = i;
            //        }
            //    }

            //    //Point2 = max X
            //    if (cvxPoints[i][0] > maxX)
            //    {
            //        maxX = cvxPoints[i][0];
            //        extremeIndicesValidation[2] = i;
            //    }
            //    else if (cvxPoints[i][0] == maxX)
            //    {
            //        //with min Y for ties
            //        if (cvxPoints[i][1] < cvxPoints[extremeIndicesValidation[2]][1])
            //        {
            //            maxX = cvxPoints[i][0];
            //            extremeIndicesValidation[2] = i;
            //        }
            //    }

            //    //Point3 = max Y
            //    if (cvxPoints[i][1] > maxY)
            //    {
            //        maxY = cvxPoints[i][1];
            //        extremeIndicesValidation[3] = i;
            //    }
            //    else if (cvxPoints[i][1] == maxY)
            //    {
            //        //with max X for ties
            //        if (cvxPoints[i][0] > cvxPoints[extremeIndicesValidation[3]][0])
            //        {
            //            maxY = cvxPoints[i][1];
            //            extremeIndicesValidation[3] = i;
            //        }
            //    }
            //}

            //for (var i = 0; i < 4; i++)
            //{
            //    if (extremeIndices[i] != extremeIndicesValidation[i]) throw new Exception();
            //}

            #endregion

            #region Cycle through 90-degrees

            var deltaAngles = new double[4];
            var offsetAngles = new[] {Math.PI/2, Math.PI, -Math.PI/2, 0.0};
            var bestRectangle = new BoundingRectangle {Area = double.PositiveInfinity};
            do
            {
                #region update the deltaAngles from the current orientation

                //For each of the 4 supporting points (those forming the rectangle),
                for (var i = 0; i < 4; i++)
                {
                    var index = extremeIndices[i];
                    var prev = index == 0 ? numCvxPoints - 1 : index - 1;
                    var tempDelta = Math.Atan2(cvxPoints[prev][1] - cvxPoints[index][1],
                        cvxPoints[prev][0] - cvxPoints[index][0]);
                    deltaAngles[i] = offsetAngles[i] - tempDelta;
                    //If the angle has rotated beyond the 90 degree bounds, it will be negative
                    //And should never be chosen from then on.
                    if (deltaAngles[i] < 0) deltaAngles[i] = double.PositiveInfinity;
                }
                var angle = deltaAngles.Min();
                if (angle.IsGreaterThanNonNegligible(Math.PI/2))
                    break;
                var refIndex = deltaAngles.FindIndex(angle);

                #endregion

                #region find area

                //Get unit normal for current edge
                var otherIndex = extremeIndices[refIndex] == 0 ? numCvxPoints - 1 : extremeIndices[refIndex] - 1;
                var direction =
                    cvxPoints[extremeIndices[refIndex]].Position2D.subtract(cvxPoints[otherIndex].Position2D)
                        .normalize();
                //If point type = 1 or 3, then use inversed Direction
                if (refIndex == 1 || refIndex == 3)
                {
                    direction = new[] {-direction[1], direction[0]};
                }
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[2]][0] - cvxPoints[extremeIndices[0]][0],
                    cvxPoints[extremeIndices[2]][1] - cvxPoints[extremeIndices[0]][1]
                };

                var angleVector1 = new[] {-direction[1], direction[0]};
                var width = Math.Abs(vectorWidth.dotProduct(angleVector1));
                var vectorHeight = new[]
                {
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0],
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                var angleVector2 = new[] {direction[0], direction[1]};
                var height = Math.Abs(vectorHeight.dotProduct(angleVector2));
                var area = height*width;

                #endregion

                var xDir = new[] {angleVector1[0], angleVector1[1], 0.0};
                var yDir = new[] {angleVector2[0], angleVector2[1], 0.0};
                var pointsOnSides = new List<Point>[4];
                for (var i = 0; i < 4; i++)
                {
                    pointsOnSides[i] = new List<Point>();
                    var dir = i%2 == 0 ? xDir : yDir;
                    var distance = cvxPoints[extremeIndices[i]].Position.dotProduct(dir);
                    var prevIndex = extremeIndices[i];
                    do
                    {
                        extremeIndices[i] = prevIndex;
                        pointsOnSides[i].Add(cvxPoints[extremeIndices[i]]);
                        prevIndex = extremeIndices[i] == 0 ? numCvxPoints - 1 : extremeIndices[i] - 1;
                    } while (distance.IsPracticallySame(cvxPoints[prevIndex].Position.dotProduct(dir),
                        Constants.BaseTolerance));
                }

                if (bestRectangle.Area > area)
                {
                    bestRectangle.PointsOnSides = pointsOnSides;
                    bestRectangle.Area = area;
                    bestRectangle.Dimensions = new[] {width, height};
                    bestRectangle.Directions2D = new[] {xDir, yDir};
                }
            } while (true); //process will end on its own by the break statement in line 314

            #endregion

            if (bestRectangle.Area.IsNegligible())
                throw new Exception("Area should never be negligilbe unless data is messed up.");
            return bestRectangle;
        }

        #endregion

        /// <summary>
        ///     Adds the corner vertices (actually 3d points) to the bounding box
        /// </summary>
        /// <param name="bb">The bb.</param>
        /// <returns>BoundingBox.</returns>
        //ToDo: Fix this function. It does not currently give the correct vertices
        public static BoundingBox AddInCornerVertices(BoundingBox bb)
        {
            if (bb.CornerVertices != null) return bb;
            var cornerVertices = new Vertex[8];

            ////////////////////////////////////////
            //First, get the bottom corner.
            ////////////////////////////////////////
            
            //Collect all the points on faces
            var allPointsOnFaces = new List<Vertex>();
            foreach (var setOfPoints in bb.PointsOnFaces)
            {
                allPointsOnFaces.AddRange(setOfPoints);
            }

            //Get the low extreme vertices along each direction
            List<Vertex> vLows, vHighs;
            GetLengthAndExtremeVertices(bb.Directions[0], allPointsOnFaces, out vLows, out vHighs);
            var v0 = new Vertex(vLows.First().Position);
            GetLengthAndExtremeVertices(bb.Directions[1], allPointsOnFaces, out vLows, out vHighs);
            var v1 = new Vertex(vLows.First().Position);
            GetLengthAndExtremeVertices(bb.Directions[2], allPointsOnFaces, out vLows, out vHighs);
            var v2 = new Vertex(vLows.First().Position);

            //Start with v0 and move along direction[1] by projection
            var vector0To1 = v1.Position.subtract(v0.Position);
            var projectionOntoD1 = bb.Directions[1].multiply(bb.Directions[1].dotProduct(vector0To1));
            var v4 = v0.Position.add(projectionOntoD1);

            //Move along direction[2] by projection
            var vector4To2 = v2.Position.subtract(v4);
            var projectionOntoD2 = bb.Directions[2].multiply(bb.Directions[2].dotProduct(vector4To2));
            var bottomCorner = new Vertex(v4.add(projectionOntoD2));

            //Double Check to make sure it is the bottom corner
            allPointsOnFaces.Add(bottomCorner);
            GetLengthAndExtremeVertices(bb.Directions[0], allPointsOnFaces, out vLows, out vHighs);
            if(!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");
            GetLengthAndExtremeVertices(bb.Directions[1], allPointsOnFaces, out vLows, out vHighs);
            if (!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");
            GetLengthAndExtremeVertices(bb.Directions[2], allPointsOnFaces, out vLows, out vHighs);
            if (!vLows.Contains(bottomCorner)) throw new Exception("Error in defining bottom corner");

            //Create the vertices that make up the box and add them to the corner vertices array
            var count = 0;
            for (var i = 0; i < 2; i++)
            {
                var d0Vector = i == 0 ? new[] {0.0, 0.0, 0.0} : bb.Directions[0].multiply(bb.Dimensions[0]);
                for (var j = 0; j < 2; j++)
                {
                    var d1Vector = j == 0 ? new[] { 0.0, 0.0, 0.0 } : bb.Directions[1].multiply(bb.Dimensions[1]);
                    for (var k = 0; k < 2; k++)
                    {
                        var d2Vector = k == 0 ? new[] { 0.0, 0.0, 0.0 } : bb.Directions[2].multiply(bb.Dimensions[2]);
                        var newVertex = new Vertex(bottomCorner.Position.add(d0Vector).add(d1Vector).add(d2Vector));
                        cornerVertices[count] = newVertex;
                        count++;
                    }
                }
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0, 0.0 };
            foreach(var vertex in cornerVertices)
            {
                centerPosition[0] += vertex.Position[0];
                centerPosition[1] += vertex.Position[1];
                centerPosition[2] += vertex.Position[2];
            }
            centerPosition[0] = centerPosition[0] / cornerVertices.Count();
            centerPosition[1] = centerPosition[1] / cornerVertices.Count();
            centerPosition[2] = centerPosition[2] / cornerVertices.Count();

            return new BoundingBox
            {
                CornerVertices = cornerVertices,
                Center = new Vertex(centerPosition),
                Dimensions = bb.Dimensions,
                Directions = bb.Directions,
                PointsOnFaces = bb.PointsOnFaces,
                Volume = bb.Volume
            };
        }

        #region Find OBB Along Direction

        /// <summary>
        ///     Finds the minimum oriented bounding rectangle (2D). The 3D points of a tessellated solid
        ///     are projected to the plane defined by "Direction". This returns a BoundingBox structure
        ///     where the first Direction is the same as the prescribed Direction and the other two are
        ///     in-plane unit vectors.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The Direction.</param>
        /// <returns>BoundingBox.</returns>
        /// <exception cref="Exception">Volume should never be negligible, unless the input data is bad</exception>
        /// <exception cref="System.Exception"></exception>
        public static BoundingBox OBBAlongDirection(IList<Vertex> vertices, double[] direction)
        {
            return AddInCornerVertices(FindOBBAlongDirection(vertices, direction));
        }

        private static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction)
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
                boundingRectangle.Directions2D[0][0], boundingRectangle.Directions2D[0][1],
                boundingRectangle.Directions2D[0][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction2 = new[] {tempDirection[0], tempDirection[1], tempDirection[2]};
            tempDirection = new[]
            {
                boundingRectangle.Directions2D[1][0], boundingRectangle.Directions2D[1][1],
                boundingRectangle.Directions2D[1][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction3 = new[] {tempDirection[0], tempDirection[1], tempDirection[2]};
            var pointsOnFaces = new List<List<Vertex>>
            {
                bottomVertices,
                topVertices,
                boundingRectangle.PointsOnSides[0].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[1].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[2].SelectMany(p => p.References).ToList(),
                boundingRectangle.PointsOnSides[3].SelectMany(p => p.References).ToList()
            };
            if ((depth*boundingRectangle.Dimensions[0]*boundingRectangle.Dimensions[1]).IsNegligible())
                throw new Exception("Volume should never be negligible, unless the input data is bad");
            //var dim2 = GetLengthAndExtremeVertices(direction2, vertices, out bottomVertices, out topVertices);
            //var dim3 = GetLengthAndExtremeVertices(direction3, vertices, out bottomVertices, out topVertices);
            //if (!dim2.IsPracticallySame(boundingRectangle.Dimensions[0], 0.000001)) throw new Exception("Error in implementation");
            //if (!dim3.IsPracticallySame(boundingRectangle.Dimensions[1], 0.000001)) throw new Exception("Error in implementation");
            return new BoundingBox
            {
                Dimensions = new[] {depth, boundingRectangle.Dimensions[0], boundingRectangle.Dimensions[1]},
                Directions = new[] {direction1, direction2, direction3},
                PointsOnFaces = pointsOnFaces.ToArray(),
                Volume = depth*boundingRectangle.Dimensions[0]*boundingRectangle.Dimensions[1]
            };
        }

        /// <summary>
        ///     Finds the obb along direction.
        /// </summary>
        /// <param name="boxData">The box data.</param>
        private static void FindOBBAlongDirection(BoundingBoxData boxData)
        {
            var direction0 = boxData.Direction = boxData.Direction.normalize();
            var height = direction0.dotProduct(boxData.RotatorEdge.From.Position.subtract(boxData.BackVertex.Position));
            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(boxData.OrthVertices, direction0, out backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);
            //Get the Direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.Directions2D[0][0], boundingRectangle.Directions2D[0][1],
                boundingRectangle.Directions2D[0][2], 1.0
            };
            var direction1 = backTransform.multiply(tempDirection).Take(3).ToArray();
            tempDirection = new[]
            {
                boundingRectangle.Directions2D[1][0], boundingRectangle.Directions2D[1][1],
                boundingRectangle.Directions2D[1][2], 1.0
            };
            var direction2 = backTransform.multiply(tempDirection).Take(3).ToArray();
            boxData.Box =
                new BoundingBox
                {
                    Dimensions = new[] {height, boundingRectangle.Dimensions[0], boundingRectangle.Dimensions[1]},
                    Directions = new[] {direction0, direction1, direction2},
                    PointsOnFaces = new[]
                    {
                        new List<Vertex> {boxData.RotatorEdge.From, boxData.RotatorEdge.To},
                        new List<Vertex> {boxData.BackVertex},
                        boundingRectangle.PointsOnSides[0].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[1].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[2].SelectMany(p => p.References).ToList(),
                        boundingRectangle.PointsOnSides[3].SelectMany(p => p.References).ToList()
                    },
                    Volume = height*boundingRectangle.Dimensions[0]*boundingRectangle.Dimensions[1]
                };
        }

        #endregion
    }
}