// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        #region TVGL Offsetting

        /// <summary>
        /// Offset the polygon with square corners.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetSquare(this Polygon polygon, double offset,
            double tolerance = double.NaN)
        {
            return Offset(polygon, offset, true, tolerance);
        }

        /// <summary>
        /// Offset the polygons with square corners. The resulting polygons are joined (unioned) if overlapping.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetSquare(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN)
        {
            return Offset(polygons, offset, true, tolerance);
        }

        /// <summary>
        /// Offset the polygon with miter (sharp) corners. This is the fastest of the three since the resulting polygon will have the 
        /// same number of points as the 
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetMiter(this Polygon polygon, double offset,
            double tolerance = double.NaN)
        {
            return Offset(polygon, offset, false, tolerance);
        }
        /// <summary>
        /// Offset the polygon with miter (sharp) corners. The resulting polygons are joined (unioned) if overlapping.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetMiter(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN)
        {
            return Offset(polygons, offset, false, tolerance);
        }


        /// <summary>
        /// Offset the polygon with "round" corners (in quotes since really lots of small polygon sides).
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="maxCircleDeviation">The maximum circle deviation. If none is provided, then vertices are
        ///     placed at every 1 degree (pi/180 radians).</param>
        /// <param name="tolerance"></param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetRound(this Polygon polygon, double offset,
            double tolerance = double.NaN, double maxCircleDeviation = double.NaN)
        {
            double deltaAngle = DefineDeltaAngle(offset, tolerance, maxCircleDeviation);
            return Offset(polygon, offset, true, tolerance, deltaAngle);
        }

        /// <summary>
        /// Offset the polygon with "round" corners (in quotes since really lots of small polygon sides).
        /// The resulting polygons are joined(unioned) if overlapping.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="maxCircleDeviation">The maximum circle deviation.</param>
        /// <param name="tolerance"></param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetRound(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN, double maxCircleDeviation = double.NaN)
        {
            double deltaAngle = DefineDeltaAngle(offset, tolerance, maxCircleDeviation);
            return Offset(polygons, offset, true, tolerance, deltaAngle);
        }

        private static double DefineDeltaAngle(double offset, double tolerance, double maxCircleDeviation)
        {
            if (double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                return Constants.DefaultRoundOffsetDeltaAngle;
            if (!double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                maxCircleDeviation = tolerance;
            return 2 * Math.Acos(1 - Math.Abs(maxCircleDeviation / offset));
        }


        private static List<Polygon> Offset(this IEnumerable<Polygon> polygons, double offset, bool notMiter,
            double tolerance, double deltaAngle = double.NaN)
        {
            polygons = polygons.CleanUpForBooleanOperations(out _);
#if CLIPPER
            return OffsetViaClipper(polygons, offset, notMiter, tolerance, deltaAngle);
#elif !COMPARE
            var allPolygons = new List<Polygon>();
            foreach (var polygon in polygons)
                allPolygons.AddRange(polygon.OffsetJust(offset, notMiter, deltaAngle));
            if (allPolygons.Count > 1)
                return allPolygons.UnionPolygons(PolygonCollection.PolygonWithHoles);
            return allPolygons;
#else
            sw.Restart();
            var pClipper = OffsetViaClipper(polygons, offset, notMiter, tolerance, deltaAngle);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();

            var allPolygons = new List<Polygon>();
            foreach (var polygon in polygons)
                allPolygons.AddRange(polygon.OffsetJust(offset, notMiter, deltaAngle));
            if (allPolygons.Count > 1)
                allPolygons = allPolygons.UnionPolygons(PolygonCollection.PolygonWithHoles);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(allPolygons, pClipper, "Offset", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygons);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(allPolygons);
#else
                var fileNameStart = "offsetFail" + DateTime.Now.ToOADate().ToString() + "." + offset;
                int i = 0;
                foreach (var poly in polygons)
                    TVGL.IOFunctions.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
#endif
        }

        private static List<Polygon> OffsetJust(this Polygon polygon, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            var bb = polygon.BoundingRectangle();
            // if the offset is negative then perhaps we just delete the entire polygon if it is smaller than
            // twice the offset. Notice how the RHS is negative and can only be true is offset is negative
            if (bb.Length1 < -2 * offset || bb.Length2 < -2 * offset)
                return new List<Polygon>();
            var longerLength = Math.Max(bb.Length1, bb.Length2);
            var longerLengthSquared = longerLength * longerLength; // 3 * offset * offset;
            var outerData = MainOffsetRoutine(polygon, offset, notMiter, longerLengthSquared, deltaAngle);
            var outer = new Polygon(outerData.points);
//#if PRESENT
//            Presenter.ShowAndHang(new[] { polygon, outer });
//#endif
            var outers = outer.RemoveSelfIntersections(ResultType.OnlyKeepPositive, outerData.knownWrongPoints);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHoleData = MainOffsetRoutine(hole, offset, notMiter, longerLengthSquared, deltaAngle);
                var newHoles = new Polygon(newHoleData.points);
                inners.AddRange(newHoles.RemoveSelfIntersections(ResultType.OnlyKeepNegative).Where(p => !p.IsPositive));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive).ToList();
            return outers.IntersectPolygonsFromOffset(inners).Where(p => p.IsPositive).ToList();
        }

        /// <summary>
        /// temporary
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> IntersectPolygonsFromOffset(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonsA = polygonsA.CleanUpForBooleanOperations(out _);
                if (polygonsB != null)
                    polygonsB = polygonsB.CleanUpForBooleanOperations(out _);
            }

            if (polygonsB is null)
                return UnionPolygons(polygonsA, outputAsCollectionType);

            var result = polygonsA.ToList();
            foreach (var polygon in polygonsB)
            {
                if (!result.Any()) break;
                result = result.SelectMany(r => r.Intersect(polygon)).ToList();
            }
            return result;
        }




        private static List<Polygon> Offset(this Polygon polygon, double offset, bool notMiter, double tolerance, double deltaAngle = double.NaN)
        {
#if CLIPPER
            return OffsetViaClipper(polygon, offset, notMiter, tolerance, deltaAngle);
#elif !COMPARE
            return polygon.OffsetJust(offset, notMiter, deltaAngle);
#else
            sw.Restart();
            var pClipper = OffsetViaClipper(polygon, offset, notMiter, tolerance, deltaAngle);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var pTVGL = polygon.OffsetJust(offset, notMiter, deltaAngle);
            sw.Stop();
            var tvglTime = sw.Elapsed;
            if (Compare(pTVGL, pClipper, "Offset", clipTime, tvglTime))
            {
#if PRESENT
                Presenter.ShowAndHang(polygon);
                Presenter.ShowAndHang(pClipper);
                Presenter.ShowAndHang(pTVGL);
#else
                var fileNameStart = "offsetFail" + DateTime.Now.ToOADate().ToString() + "." + offset;
                TVGL.IOFunctions.IO.Save(polygon, fileNameStart + ".0.json");
#endif
            }
            return pClipper;
#endif
        }

        private static (List<Vector2> points, List<bool> knownWrongPoints) MainOffsetRoutine(Polygon polygon, double offset, bool notMiter,
            double maxLengthSquared, double deltaAngle = double.NaN)
        {
            var tolerance = Math.Pow(10, -polygon.NumSigDigits);
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var numPoints = polygon.Edges.Length;
            int numFalsesToAdd;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var offsetSign = Math.Sign(offset);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(offsetSign * deltaAngle) : Matrix3x3.Null;
            if (notMiter && !roundCorners) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2>(startingListSize);
            var wrongPoints = new List<bool>(startingListSize);
            // previous line starts at the end of the list and then updates to whatever next line was. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction). The prevLineLengthReciprocal was originally
            // thought to have uses outside of the unit vector but it doesn't. Anyway, slight speed up in calculating it once
            var prevLine = polygon.Edges[0];
            var prevLineLengthReciprocal = 1.0 / prevLine.Length;
            var prevUnitNormal = new Vector2(prevLine.Vector.Y * prevLineLengthReciprocal, -prevLine.Vector.X * prevLineLengthReciprocal);
            for (int i = 1; i <= numPoints; i++)
            {
                var nextLine = (i == numPoints) ? polygon.Edges[0] : polygon.Edges[i];
                var nextLineLengthReciprocal = 1.0 / nextLine.Length;
                var nextUnitNormal = new Vector2(nextLine.Vector.Y * nextLineLengthReciprocal, -nextLine.Vector.X * nextLineLengthReciprocal);
                // establish the new offset points for the point connecting prevLine to nextLive. this is stored as "point".
                var point = nextLine.FromPoint.Coordinates;
                var cross = prevLine.Vector.Cross(nextLine.Vector);
                var dot = prevLine.Vector.Dot(nextLine.Vector);
                //var angle = Math.Atan2(cross, dot);
                // cross/dot is the tan(angle). both dot and cross are quicker than square-root or trigonometric functions
                // and essentially tan(angle) * offset will be the distance between two points emanating from the polygons edges at
                // this point. If it is less than the tolerance, then just make one point - it doesn't matter if offset is negative/positive
                // or if angle is convex or concave. Oh, the 100 is added to account for problems that arise when intersections weren't detected
                if ((cross * offset / dot).IsNegligible(100 * tolerance))
                {
                    if (prevUnitNormal.Dot(nextUnitNormal) > 0)
                        // if line is practically straight, and going the same direction, then simply offset it without all the complication below
                        pointsList.Add(point + offset * prevUnitNormal);
                    else pointsList.Add(point);
                }
                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                else if (cross * offset > 0)
                {
                    if (roundCorners)
                    {
                        var firstPoint = point + offset * prevUnitNormal;
                        pointsList.Add(firstPoint);
                        var lastPoint = point + offset * nextUnitNormal;
                        var firstToLastVector = lastPoint - firstPoint;
                        var firstToLastNormal = new Vector2(firstToLastVector.Y, -firstToLastVector.X);
                        // to avoid "costly" call to Math.Sin and Math.Cos, we create the transform matrix that 1) translates to origin
                        // 2) rotates by the angle, and 3) translates back
                        var transform = Matrix3x3.CreateTranslation(-point) * rotMatrix *
                                        Matrix3x3.CreateTranslation(point);
                        var nextPoint = firstPoint.Transform(transform);
                        // the challenge with this matrix transform is figuring out when to stop. But we know that all the new points must to on
                        // positive side of the line connecting the first and last points. This is defined by the following dot-product
                        while (offsetSign * firstToLastNormal.Dot(nextPoint - lastPoint) > 0)
                        {
                            pointsList.Add(nextPoint);
                            firstPoint = nextPoint;
                            nextPoint = firstPoint.Transform(transform);
                        }
                        pointsList.Add(lastPoint);
                    }
                    // if the cross is positive and the offset is positive, then we will need to make extra points for the 
                    // squaredCorners
                    else if (notMiter)
                    {
                        // find these two points by calling the LineLine2DIntersection function twice. 
                        var middleUnitVector = (prevUnitNormal + nextUnitNormal).Normalize();
                        var middlePoint = point + offset * middleUnitVector;
                        var middleDir = new Vector2(-middleUnitVector.Y, middleUnitVector.X);
                        pointsList.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, middlePoint, middleDir));
                        pointsList.Add(MiscFunctions.LineLine2DIntersection(middlePoint, middleDir,
                            point + offset * nextUnitNormal, nextLine.Vector));
                    }
                    // miter and concave connections are done the same way...
                    else
                    {
                        var intersection = MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, point + offset * nextUnitNormal, nextLine.Vector);
                        if (intersection.IsNull())
                            pointsList.Add(point + offset * prevUnitNormal);
                        else
                        {
                            // if the corner is too shape the new point will be placed far away (near infinity). This is to rein it in
                            var vectorToCorner = intersection - point;
                            var vectorToCornerLengthSquared = vectorToCorner.LengthSquared();
                            if (vectorToCornerLengthSquared > maxLengthSquared)
                                intersection =
                                    point + vectorToCorner * Math.Sqrt(maxLengthSquared / vectorToCornerLengthSquared);
                            pointsList.Add(intersection);
                        }
                    }
                }
                else
                {
                    numFalsesToAdd = pointsList.Count - wrongPoints.Count;
                    for (int k = 0; k < numFalsesToAdd; k++) wrongPoints.Add(false);
                    wrongPoints.Add(true);
                    wrongPoints.Add(true);
                    pointsList.Add(point + offset * prevUnitNormal);
                    pointsList.Add(point + offset * nextUnitNormal);
                }
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
            }
            numFalsesToAdd = pointsList.Count - wrongPoints.Count;
            for (int k = 0; k < numFalsesToAdd; k++) wrongPoints.Add(false);

            var slopeTolerance = Constants.LineSlopeTolerance;
            var forwardPoint = pointsList[0];
            var currentPoint = pointsList[^1];
            var forwardVector = forwardPoint - currentPoint;
            for (int i = pointsList.Count - 1; i >= 0; i--)
            {
                var backwardPoint = i == 0 ? pointsList[^1] : pointsList[i - 1];
                var backVector = currentPoint - backwardPoint;
                var cross = backVector.Cross(forwardVector);
                if (!wrongPoints[i] && cross.IsNegligible(slopeTolerance))
                // unlike SimplifyFast, we won't throw out points just because they're close. This further copmlicates
                // the wrong-point assignment
                {
                    pointsList.RemoveAt(i);
                    wrongPoints.RemoveAt(i);
                    forwardVector = forwardPoint - backwardPoint;
                    currentPoint = backwardPoint;
                    // furthermore, removing a point may make a point that was believed to be a wrong point - to become a 
                    // valid one.
                    var j = i;
                    while (j < wrongPoints.Count && wrongPoints[j])
                        wrongPoints[j++] = false;
                    j = i - 1;
                    while (j >= 0 && wrongPoints[j])
                        wrongPoints[j--] = false;
                }
                else
                {
                    forwardVector = backVector;
                    forwardPoint = currentPoint;
                    currentPoint = backwardPoint;
                }
            }
            return (pointsList, wrongPoints);
        }
        #endregion
    }
}