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
        ///     Optional booleans for what information should be set in the Bounding Rectangle.
        ///     Example: If you really just need the area, you don't need the corner points or
        ///     points on side. 
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull">if set to <c>true</c> [points are convex hull].</param>
        /// <param name="setCornerPoints"></param>
        /// <param name="setPointsOnSide"></param>
        /// <returns>BoundingRectangle.</returns>
        /*
        public static BoundingRectangle BoundingRectangle(IList<Point> points, bool pointsAreConvexHull = false,
            bool setCornerPoints = true, bool setPointsOnSide = true)
        {
            return RotatingCalipers2DMethod(points, pointsAreConvexHull, setCornerPoints, setPointsOnSide);
        }
        public static BoundingRectangle BoundingRectangle(Polygon polygon, bool pointsAreConvexHull = false,
            bool setCornerPoints = true, bool setPointsOnSide = true)
        {
            return RotatingCalipers2DMethod(polygon.Path, pointsAreConvexHull, setCornerPoints, setPointsOnSide);
        }
        */
        public static BoundingRectangle BoundingRectangle(List<PointLight> polygon, bool pointsAreConvexHull = false,
            bool setCornerPoints = true, bool setPointsOnSide = true)
        {
            return RotatingCalipers2DMethod(polygon, pointsAreConvexHull, setCornerPoints, setPointsOnSide);
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
                    directions.Add(new[] {1.0, i, j}.normalize(3));
            directions.Add(new[] {0.0, 0, 1}.normalize(3));
            directions.Add(new[] {0.0, 1, 0}.normalize(3));
            directions.Add(new[] {0.0, 1, 1}.normalize(3));
            directions.Add(new[] {0.0, -1, 1}.normalize(3));

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
            minBox.SetCornerVertices();
            return minBox;
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
        ///     for the provided vertices as well as the vertices that represent the extremes.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="bottomVertices">The bottom vertices.</param>
        /// <param name="topVertices">The top vertices.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertices(double[] direction, IEnumerable<Vertex> vertices,
            out List<Vertex> bottomVertices,
            out List<Vertex> topVertices)
        {
            var dir = direction.normalize(3);
            var minD = double.PositiveInfinity;
            bottomVertices = new List<Vertex>();
            topVertices = new List<Vertex>();
            var maxD = double.NegativeInfinity;
            foreach (var v in vertices)
            {
                var distance = dir.dotProduct(v.Position, 3);
                if (distance.IsPracticallySame(minD, Constants.BaseTolerance))
                    bottomVertices.Add(v);
                else if (distance < minD)
                {
                    bottomVertices.Clear();
                    bottomVertices.Add(v);
                    minD = distance;
                }
                if (distance.IsPracticallySame(maxD, Constants.BaseTolerance))
                    topVertices.Add(v);
                else if (distance > maxD)
                {
                    topVertices.Clear();
                    topVertices.Add(v);
                    maxD = distance;
                }
            }
            return maxD - minD;
        }

        /// <summary>
        ///     Given a Direction, dir, this function returns the maximum length along this Direction and one vertex 
        ///     that represents each extreme. Use this if you do not need all the vertices at the extremes.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="bottomVertex"></param>
        /// <param name="topVertex"></param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertex(double[] direction, IEnumerable<Vertex> vertices,
            out Vertex bottomVertex,
            out Vertex topVertex)
        {
            var dir = direction.normalize(3);
            var minD = double.PositiveInfinity;
            bottomVertex = null;
            topVertex = null;
            var maxD = double.NegativeInfinity;
            foreach (var v in vertices)
            {
                var distance = dir.dotProduct(v.Position, 3);
                if (distance < minD)
                {
                    bottomVertex = v;
                    minD = distance;
                }
                if (distance > maxD)
                {
                    topVertex = v;
                    maxD = distance;
                }
            }
            return maxD - minD;
        }

        /// <summary>
        ///     Given a Direction, dir, this function returns the maximum length along this Direction
        ///     for the provided points as well as the points that represent the extremes.
        /// </summary>
        /// <param name="direction2D">The direction.</param>
        /// <param name="points">The vertices.</param>
        /// <param name="bottomPoints">The bottom vertices.</param>
        /// <param name="topPoints">The top vertices.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremePoints(double[] direction2D, IList<Point> points,
            out List<Point> bottomPoints,
            out List<Point> topPoints)
        {
            var minD = double.PositiveInfinity;
            bottomPoints = new List<Point>();
            topPoints = new List<Point>();
            var maxD = double.NegativeInfinity;
            foreach (var point in points)
            {
                var distance = direction2D.dotProduct(point);
                if (distance.IsPracticallySame(minD, Constants.BaseTolerance))
                    bottomPoints.Add(point);
                else if (distance < minD)
                {
                    bottomPoints.Clear();
                    bottomPoints.Add(point);
                    minD = distance;
                }
                if (distance.IsPracticallySame(maxD, Constants.BaseTolerance))
                    bottomPoints.Add(point);
                else if (distance > maxD)
                {
                    topPoints.Clear();
                    topPoints.Add(point);
                    maxD = distance;
                }
            }
            return maxD - minD;
        }

        /// <summary>
        ///     Given a Direction, dir, this function returns the maximum length along this Direction
        ///     for the provided points as well as the points that represent the extremes.
        /// </summary>
        /// <param name="direction2D">The direction.</param>
        /// <param name="points">The vertices.</param>
        /// <param name="bottomPoints">The bottom vertices.</param>
        /// <param name="topPoints">The top vertices.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremePoints(double[] direction2D, IList<PointLight> points,
            out List<PointLight> bottomPoints,
            out List<PointLight> topPoints)
        {
            var minD = double.PositiveInfinity;
            bottomPoints = new List<PointLight>();
            topPoints = new List<PointLight>();
            var maxD = double.NegativeInfinity;
            foreach (var point in points)
            {
                var distance = direction2D.dotProduct(point);
                if (distance.IsPracticallySame(minD, Constants.BaseTolerance))
                    bottomPoints.Add(point);
                else if (distance < minD)
                {
                    bottomPoints.Clear();
                    bottomPoints.Add(point);
                    minD = distance;
                }
                if (distance.IsPracticallySame(maxD, Constants.BaseTolerance))
                    bottomPoints.Add(point);
                else if (distance > maxD)
                {
                    topPoints.Clear();
                    topPoints.Add(point);
                    maxD = distance;
                }
            }
            return maxD - minD;
        }

        #endregion

        #region 2D Rotating Calipers
        private static readonly double[] CaliperOffsetAngles = { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };

        /// <summary>
        ///     Rotating the calipers 2D method. Convex hull must be a counter clockwise loop.
        ///     Optional booleans for what information should be set in the Bounding Rectangle.
        ///     Example: If you really just need the area, you don't need the corner points or
        ///     points on side. 
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull">if set to <c>true</c> [points are convex hull].</param>
        /// <param name="setCornerPoints"></param>
        /// <param name="setPointsOnSide"></param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">
        ///     Area should never be negligilbe unless data is messed up.
        /// </exception>
        /*
        private static BoundingRectangle RotatingCalipers2DMethod(IList<Point> points, bool pointsAreConvexHull = false,
            bool setCornerPoints = true, bool setPointsOnSide = true)
        {
            #region Initialization
            if(points.Count < 3) throw new Exception("Rotating Calipers requires at least 3 points.");
            var cvxPoints = pointsAreConvexHull ? points : ConvexHull2D(points);
            //Simplify the points to make sure they are the minimal convex hull
            //Only set it as the convex hull if it contains more than three points.
            var cvxPointsSimple = PolygonOperations.SimplifyFuzzy(cvxPoints);
            if (cvxPointsSimple.Count >= 3) cvxPoints = cvxPointsSimple;
            // the cvxPoints will be arranged from a point with minimum X-value around in a CCW loop to the last point 
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
            if (cvxPoints[0].Y > cvxPoints[extremeIndices[3]].Y) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] > 0 && cvxPoints[extremeIndices[3]].Y <= cvxPoints[extremeIndices[3] - 1].Y)
                    extremeIndices[3]--;
            }
            // at this point, the max-Y point has been established. Next we walk backwards in the list until we hit the max-X point 
            // extremeIndices[2] => max-X, with min Y for ties
            extremeIndices[2] = extremeIndices[3] == 0 ? cvxPoints.Count - 1 : extremeIndices[3];
            while (extremeIndices[2] > 0 && cvxPoints[extremeIndices[2]].X <= cvxPoints[extremeIndices[2] - 1].X)
                extremeIndices[2]--;
            // extremeIndices[1] => min-Y, with min X for ties 
            extremeIndices[1] = extremeIndices[2] == 0 ? cvxPoints.Count - 1 : extremeIndices[2];
            while (extremeIndices[1] > 0 && cvxPoints[extremeIndices[1]].Y >= cvxPoints[extremeIndices[1] - 1].Y)
                extremeIndices[1]--;
            // extrememIndices[0] => min-X, with max Y for ties
            // First we check if the last point has an eqaully small x value, if it does we will need to walk backwards.
            if (cvxPoints.Last().X > cvxPoints[0].X) extremeIndices[0] = 0;
            else
            {
                extremeIndices[0] = cvxPoints.Count - 1;
                while (cvxPoints[extremeIndices[0]].X >= cvxPoints[extremeIndices[0] - 1].X)
                    extremeIndices[0]--;
            }

            #endregion

            #region Cycle through 90-degrees
            var bestRectangle = new BoundingRectangle { Area = double.MaxValue };
            var deltaAngles = new double[4];
            do
            {
                #region update the deltaAngles from the current orientation

                //For each of the 4 supporting points (those forming the rectangle),
                for (var i = 0; i < 4; i++)
                {
                    var index = extremeIndices[i];
                    var prev = index == 0 ? numCvxPoints - 1 : index - 1;
                    var tempDelta = Math.Atan2(cvxPoints[prev].Y - cvxPoints[index].Y,
                        cvxPoints[prev].X - cvxPoints[index].X);
                    deltaAngles[i] = CaliperOffsetAngles[i] - tempDelta;
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
                    cvxPoints[extremeIndices[refIndex]].Position.subtract(cvxPoints[otherIndex].Position, 2)
                        .normalize(2);
                //If point type = 1 or 3, then use inversed Direction
                if (refIndex == 1 || refIndex == 3)
                {
                    direction = new[] {-direction[1], direction[0]};
                }
                var vectorLength = new[]
                {
                    cvxPoints[extremeIndices[2]][0] - cvxPoints[extremeIndices[0]][0],
                    cvxPoints[extremeIndices[2]][1] - cvxPoints[extremeIndices[0]][1]
                };

                var angleVector1 = new[] {-direction[1], direction[0]};
                var length = Math.Abs(vectorLength.dotProduct(angleVector1, 2));
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0],
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                var angleVector2 = new[] {direction[0], direction[1]};
                var width = Math.Abs(vectorWidth.dotProduct(angleVector2, 2));
                var area = length * width;

                #endregion

                var d1Max = double.MinValue;
                var d1Min = double.MaxValue;
                var d2Max = double.MinValue;
                var d2Min = double.MaxValue;
                var pointsOnSides = new List<Point>[4];
                for (var i = 0; i < 4; i++)
                {
                    pointsOnSides[i] = new List<Point>();
                    var dir = i%2 == 0 ? angleVector1 : angleVector2;
                    var distance = cvxPoints[extremeIndices[i]].Position.dotProduct(dir, 2);
                    if (i % 2 == 0) //D1
                    {
                        if (distance > d1Max) d1Max = distance;
                        if (distance < d1Min) d1Min = distance;
                    }
                    else //D2
                    {
                        if (distance > d2Max) d2Max = distance;
                        if (distance < d2Min) d2Min = distance;
                    }
                    var prevIndex = extremeIndices[i];
                    do
                    {
                        extremeIndices[i] = prevIndex;
                        if (setPointsOnSide)
                        {
                            pointsOnSides[i].Add(cvxPoints[extremeIndices[i]]);
                        }
                        prevIndex = extremeIndices[i] == 0 ? numCvxPoints - 1 : extremeIndices[i] - 1;
                    } while (distance.IsPracticallySame(cvxPoints[prevIndex].Position.dotProduct(dir, 2),
                        Constants.BaseTolerance));
                }

                //If this is an improvement, set the parameters for the best bounding rectangle.
                if (area < bestRectangle.Area)
                {
                    bestRectangle.Area = area;
                    bestRectangle.Length = length; //Length corresponds with Direction 1
                    bestRectangle.Width = width;
                    bestRectangle.LengthDirection = angleVector1;
                    bestRectangle.WidthDirection = angleVector2;
                    bestRectangle.LengthDirectionMax = d1Max;
                    bestRectangle.LengthDirectionMin = d1Min;
                    bestRectangle.WidthDirectionMax = d2Max;
                    bestRectangle.WidthDirectionMin = d2Min;
                    bestRectangle.PointsOnSides = pointsOnSides;
                }
            } while (true); //process will end on its own by the break statement in line 314

            #endregion

            if (bestRectangle.Area.IsNegligible())
                throw new Exception("Area should never be negligilbe unless data is messed up.");

            if (setCornerPoints)
                bestRectangle.SetCornerPoints();

            return bestRectangle;
        }
        */

        /// <summary>
        ///     Rotating the calipers 2D method. Convex hull must be a counter clockwise loop.
        ///     Optional booleans for what information should be set in the Bounding Rectangle.
        ///     Example: If you really just need the area, you don't need the corner points or
        ///     points on side. 
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull">if set to <c>true</c> [points are convex hull].</param>
        /// <param name="setCornerPoints"></param>
        /// <param name="setPointsOnSide"></param>
        /// <returns>System.Double.</returns>
        /// <exception cref="Exception">
        ///     Area should never be negligible unless data is messed up.
        /// </exception>
        private static BoundingRectangle RotatingCalipers2DMethod(IList<PointLight> points, 
            bool pointsAreConvexHull = false, bool setCornerPoints = true, 
            bool setPointsOnSide = true)
        {
            if (points.Count < 3) throw new Exception("Rotating Calipers requires at least 3 points.");
            var cvxPoints = pointsAreConvexHull ? points : ConvexHull2D(points).ToList();

            //Simplify the points to make sure they are the minimal convex hull
            //Only set it as the convex hull if it contains more than three points.
            var cvxPointsSimple = PolygonOperations.SimplifyFuzzy(cvxPoints);
            if (cvxPointsSimple.Count >= 3) cvxPoints = cvxPointsSimple;
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
            var tempList = new List<PointLight>();
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

            #region Get Extreme Points
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
            if (cvxPoints[0].Y > cvxPoints[extremeIndices[3]].Y) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] > 0 && cvxPoints[extremeIndices[3]].Y <= cvxPoints[extremeIndices[3] - 1].Y)
                    extremeIndices[3]--;
            }
            /* at this point, the max-Y point has been established. Next we walk backwards in the list until we hit the max-X point */
            // extremeIndices[2] => max-X, with min Y for ties
            extremeIndices[2] = extremeIndices[3] == 0 ? cvxPoints.Count - 1 : extremeIndices[3];
            while (extremeIndices[2] > 0 && cvxPoints[extremeIndices[2]].X <= cvxPoints[extremeIndices[2] - 1].X)
                extremeIndices[2]--;
            // extremeIndices[1] => min-Y, with min X for ties 
            extremeIndices[1] = extremeIndices[2] == 0 ? cvxPoints.Count - 1 : extremeIndices[2];
            while (extremeIndices[1] > 0 && cvxPoints[extremeIndices[1]].Y >= cvxPoints[extremeIndices[1] - 1].Y)
                extremeIndices[1]--;
            // extrememIndices[0] => min-X, with max Y for ties
            // First we check if the last point has an eqaully small x value, if it does we will need to walk backwards.
            if (cvxPoints.Last().X > cvxPoints[0].X) extremeIndices[0] = 0;
            else
            {
                extremeIndices[0] = cvxPoints.Count - 1;
                while (cvxPoints[extremeIndices[0]].X >= cvxPoints[extremeIndices[0] - 1].X)
                    extremeIndices[0]--;
            }
            #endregion

            var bestRectangle = new BoundingRectangle {Area = double.MaxValue};  
            var PointsOnSides = new List<PointLight>[4];

            #region Cycle through 90-degrees
            var deltaAngles = new double[4];    
            do
            {
                #region update the deltaAngles from the current orientation

                //For each of the 4 supporting points (those forming the rectangle),
                var minAngle = double.PositiveInfinity;
                var refIndex = 0;
                for (var i = 0; i < 4; i++)
                {
                    var index = extremeIndices[i];
                    var prev = index == 0 ? cvxPoints[numCvxPoints - 1] : cvxPoints[index - 1];
                    var current = cvxPoints[index];
                    var tempDelta = Math.Atan2(prev.Y - current.Y, prev.X - current.X);
                    var angle = CaliperOffsetAngles[i] - tempDelta;

                    //If the angle has rotated beyond the 90 degree bounds, it will be negative
                    //And should never be chosen from then on.
                    if (angle < 0) deltaAngles[i] = double.PositiveInfinity;
                    else
                    {
                        deltaAngles[i] = angle;
                        if (angle < minAngle)
                        {
                            minAngle = angle;
                            refIndex = i;
                        }
                    }     
                }
                if (minAngle.IsGreaterThanNonNegligible(Math.PI / 2)) break;

                #endregion

                #region find area

                //Get unit normal for current edge
                var otherIndex = extremeIndices[refIndex] == 0 ? numCvxPoints - 1 : extremeIndices[refIndex] - 1;
                var direction = cvxPoints[extremeIndices[refIndex]].Subtract(cvxPoints[otherIndex]).normalize(2);
                //If point type = 1 or 3, then use inversed Direction
                if (refIndex == 1 || refIndex == 3)
                {
                    direction = new[] { -direction[1], direction[0] };
                }
                var vectorLength = new[]
                {
                    cvxPoints[extremeIndices[2]].X - cvxPoints[extremeIndices[0]].X,
                    cvxPoints[extremeIndices[2]].Y - cvxPoints[extremeIndices[0]].Y
                };

                var angleVector1 = new[] { -direction[1], direction[0] };
                var length = Math.Abs(vectorLength.dotProduct(angleVector1, 2));
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[3]].X - cvxPoints[extremeIndices[1]].X,
                    cvxPoints[extremeIndices[3]].Y - cvxPoints[extremeIndices[1]].Y
                };
                var angleVector2 = new[] { direction[0], direction[1] };
                var width = Math.Abs(vectorWidth.dotProduct(angleVector2, 2));
                var area = length * width;

                #endregion

                var d1Max = double.MinValue;
                var d1Min = double.MaxValue;
                var d2Max = double.MinValue;
                var d2Min = double.MaxValue;
                var pointsOnSides = new List<PointLight>[4];
                for (var i = 0; i < 4; i++)
                {
                    pointsOnSides[i] = new List<PointLight>();
                    var dir = i % 2 == 0 ? angleVector1 : angleVector2;
                    var distance = cvxPoints[extremeIndices[i]].dotProduct(dir);
                    if (i % 2 == 0) //D1
                    {
                        if (distance > d1Max) d1Max = distance;
                        if (distance < d1Min) d1Min = distance;
                    }
                    else //D2
                    {
                        if (distance > d2Max) d2Max = distance;
                        if (distance < d2Min) d2Min = distance;
                    }
                    var prevIndex = extremeIndices[i];
                    do
                    {
                        extremeIndices[i] = prevIndex;
                        if (setPointsOnSide)
                        {
                            pointsOnSides[i].Add(cvxPoints[extremeIndices[i]]);
                        }
                        prevIndex = extremeIndices[i] == 0 ? numCvxPoints - 1 : extremeIndices[i] - 1;
                    } while (distance.IsPracticallySame(cvxPoints[prevIndex].dotProduct(dir),
                        Constants.BaseTolerance));
                }

                //If this is an improvement, set the parameters for the best bounding rectangle.
                if (area < bestRectangle.Area)
                {
                    bestRectangle.Area = area;
                    bestRectangle.Length = length; //Lenght corresponds with direction 1.
                    bestRectangle.Width = width;
                    bestRectangle.LengthDirection = angleVector1;
                    bestRectangle.WidthDirection = angleVector2;
                    bestRectangle.LengthDirectionMax = d1Max;
                    bestRectangle.LengthDirectionMin = d1Min;
                    bestRectangle.WidthDirectionMax = d2Max;
                    bestRectangle.WidthDirectionMin = d2Min;
                    PointsOnSides = pointsOnSides;
                }
            } while (true); //process will end on its own by the break statement in line 314

            #endregion

            if (bestRectangle.Area.IsNegligible())
            {
                var polyLight = new PolygonLight(points);
                var date = DateTime.Now.ToString("MM.dd.yy_HH.mm");
                polyLight.Serialize("ConvexHullError_" + date + ".PolyLight");
                throw new Exception("Area should never be negligible unless data is messed up.");
            }


            if (setCornerPoints)
                bestRectangle.SetCornerPoints();

            if (setPointsOnSide)
            {
                var pointsOnSidesAsPoints = new List<Point>[4];
                for (var i = 0; i < 4; i++)
                {
                    pointsOnSidesAsPoints[i] = PointsOnSides[i].Select(p => new Point(p)).ToList();
                }
                bestRectangle.PointsOnSides = pointsOnSidesAsPoints;
            }

            return bestRectangle;
        }

        #endregion



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
            var boundingBox = FindOBBAlongDirection(vertices, direction);
            boundingBox.SetCornerVertices();
            return boundingBox;
        }

        private static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction)
        {
            var direction1 = direction.normalize(3);
            var depth = GetLengthAndExtremeVertices(direction, vertices, out var bottomVertices, out var topVertices);

            var points = MiscFunctions.Get2DProjectionPoints(vertices, direction, out var backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);

            //Get the Direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.LengthDirection[0], boundingRectangle.LengthDirection[1],
                0.0, 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction2 = new[] {tempDirection[0], tempDirection[1], tempDirection[2]};
            tempDirection = new[]
            {
                boundingRectangle.WidthDirection[0], boundingRectangle.WidthDirection[1],
                0.0, 1.0
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
            if ((depth * boundingRectangle.Length * boundingRectangle.Width).IsNegligible())
                throw new Exception("Volume should never be negligible, unless the input data is bad");
            //var dim2 = GetLengthAndExtremeVertices(direction2, vertices, out bottomVertices, out topVertices);
            //var dim3 = GetLengthAndExtremeVertices(direction3, vertices, out bottomVertices, out topVertices);
            //if (!dim2.IsPracticallySame(boundingRectangle.Dimensions[0], 0.000001)) throw new Exception("Error in implementation");
            //if (!dim3.IsPracticallySame(boundingRectangle.Dimensions[1], 0.000001)) throw new Exception("Error in implementation");
            return new BoundingBox
            {
                Dimensions = new[] {depth, boundingRectangle.Length, boundingRectangle.Width},
                Directions = new[] {direction1, direction2, direction3},
                PointsOnFaces = pointsOnFaces.ToArray(),
                Volume = depth * boundingRectangle.Length * boundingRectangle.Width
            };
        }

        /// <summary>
        ///     Finds the obb along direction.
        /// </summary>
        /// <param name="boxData">The box data.</param>
        private static void FindOBBAlongDirection(BoundingBoxData boxData)
        {
            var direction0 = boxData.Direction = boxData.Direction.normalize(3);
            var height = direction0.dotProduct(boxData.RotatorEdge.From.Position.subtract(boxData.BackVertex.Position, 3), 3);
            var points = MiscFunctions.Get2DProjectionPoints(boxData.OrthVertices, direction0, out var backTransform, false);
            var boundingRectangle = RotatingCalipers2DMethod(points);
            //Get the Direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.LengthDirection[0], boundingRectangle.LengthDirection[1],
                0.0, 1.0
            };
            var direction1 = backTransform.multiply(tempDirection).Take(3).ToArray();
            tempDirection = new[]
            {
                boundingRectangle.WidthDirection[0], boundingRectangle.WidthDirection[1],
                0.0, 1.0
            };
            var direction2 = backTransform.multiply(tempDirection).Take(3).ToArray();
            boxData.Box =
                new BoundingBox
                {
                    Dimensions = new[] {height, boundingRectangle.Length, boundingRectangle.Width},
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
                    Volume = height * boundingRectangle.Length * boundingRectangle.Width
                };
        }

        #endregion
    }
}