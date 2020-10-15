// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
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
        /// placed at every 1 degree (pi/180 radians).</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetRound(this Polygon polygon, double offset, double tolerance = double.NaN, double maxCircleDeviation = double.NaN)
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
            var allPolygons = new List<Polygon>();
            foreach (var polygon in polygons)
                allPolygons.AddRange(polygon.Offset(offset, notMiter, tolerance, deltaAngle));
            return allPolygons.UnionPolygons(PolygonCollection.PolygonWithHoles, tolerance);
        }
        private static List<Polygon> Offset(this Polygon polygon, double offset, bool notMiter,
            double tolerance, double deltaAngle = double.NaN)
        {
            var bb = polygon.BoundingRectangle();
            if (bb.Length1 < -2 * offset || bb.Length2 < -2 * offset)
                return new List<Polygon>();
            var outer = new Polygon(OffsetRoutineForward(polygon.Lines, offset, notMiter, deltaAngle));
            var outers = outer.RemoveSelfIntersections(false, out _, tolerance);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var invertedHole = hole.Copy(false, true);
                var newHoles = new Polygon(OffsetRoutineForward(invertedHole.Lines, -offset, notMiter, deltaAngle))
                    .RemoveSelfIntersections(false, out _, tolerance);
                inners.AddRange(newHoles);
            }
            return outers.Subtract(inners, tolerance: tolerance);
        }

        private static List<Vector2> OffsetRoutineForward(IEnumerable<PolygonEdge> lines, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var linesList = lines as IList<PolygonEdge> ?? lines.ToList();
            var numPoints = linesList.Count;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var offsetSign = Math.Sign(offset);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(offsetSign * deltaAngle) : Matrix3x3.Null;
            if (notMiter && !roundCorners) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2>(startingListSize);

            // previous line starts at the end of the list and then updates to whatever next line was. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction). The prevLineLengthReciprocal was originally
            // thought to have uses outside of the unit vector but it doesn't. Anyway, slight speed up in calculating it once
            var prevLine = linesList[^1];
            var prevLineLengthReciprocal = 1.0 / prevLine.Length;
            var prevUnitNormal = new Vector2(prevLine.Vector.Y * prevLineLengthReciprocal, -prevLine.Vector.X * prevLineLengthReciprocal);
            for (int i = 0; i < numPoints; i++)
            {
                var nextLine = linesList[i];
                var nextLineLengthReciprocal = 1.0 / nextLine.Length;
                var nextUnitNormal = new Vector2(nextLine.Vector.Y * nextLineLengthReciprocal, -nextLine.Vector.X * nextLineLengthReciprocal);
                // establish the new offset points for the point connecting prevLine to nextLive. this is stored as "point".
                var point = nextLine.FromPoint.Coordinates;
                var cross = prevLine.Vector.Cross(nextLine.Vector);
                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                if (cross.IsNegligible()) // if line is practically straight, simply offset it without all the complication below
                    pointsList.Add(point + offset * prevUnitNormal);
                else if (cross * offset > 0 && roundCorners)
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
                    // positive side of the line connectings the first and last points. This is defined by the following dot-product
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
                else if (cross * offset > 0 && notMiter)
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
                    if (pointsList.Count > 0 && prevLine.Vector.Dot(intersection - pointsList[^1]) < 0)
                        pointsList.RemoveAt(pointsList.Count - 1);
                    else pointsList.Add(intersection);
                }
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
            }
            return pointsList;
        }
        #endregion
    }
}