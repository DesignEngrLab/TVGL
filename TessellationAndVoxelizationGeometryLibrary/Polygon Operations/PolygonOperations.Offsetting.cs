using ClipperLib;
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

        #region Clipper Offset

        /// <summary>
        /// Offets the given path by the given offset, rounding corners.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="maxCircleDeviation"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<Vector2> path, double offset,
            double maxCircleDeviation = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtRound, maxCircleDeviation);
        }


        /// <summary>
        /// Offsets all paths by the given offset value. Rounds the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="offset"></param>
        /// <param name="maxCircleDeviation"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double maxCircleDeviation = 0.0)
        {
            return Offset(paths, offset, JoinType.jtRound, maxCircleDeviation);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Miters the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtSquare, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtSquare, minLength);
        }

        private static List<List<Vector2>> Offset(IEnumerable<IEnumerable<Vector2>> paths, double offset,
            JoinType joinType,
            double minLength = 0.0)
        {
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject =
                paths.Select(loop => loop.Select(point =>
                        new IntPoint(point.X * Constants.DoubleToIntPointMultipler,
                            point.Y * Constants.DoubleToIntPointMultipler))
                    .ToList()).ToList();
            //if (minLength.IsNegligible())
            //{
            //    var totalLength = clipperSubject.Sum(Perimeter);
            //    minLength = totalLength * 0.001;
            //}

            //Setup Clipper
            var clip = new ClipperOffset(2, minLength * Constants.DoubleToIntPointMultipler);
            clip.AddPaths(clipperSubject, joinType, EndType.etClosedPolygon);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * Constants.DoubleToIntPointMultipler);

            //Convert back to points and return solution
            return clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler,
                    point.Y * Constants.IntPointToDoubleMultipler)).ToList()).ToList();
        }
        #endregion

        #region next TVGL Offsetting

        public static List<Polygon> OffsetSquare(this Polygon path, double offset)
        {
            return Offset(path, offset, true);
        }

        public static List<Polygon> OffsetMiter(this Polygon path, double offset)
        {
            return Offset(path, offset, false);
        }

        public static List<Polygon> OffsetRound(this Polygon path, double offset, double maxCircleDeviation = double.NaN)
        {
            var deltaAngle = double.IsNaN(maxCircleDeviation)
                ? Constants.DefaultRoundOffsetDeltaAngle
                : 2 * Math.Acos(1 - Math.Abs(maxCircleDeviation / offset));
            return Offset(path, offset, true, deltaAngle);

        }

        private static List<Polygon> Offset(this Polygon path, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            var positivePolygons = new Polygon(OffsetMainRoutine(path.Lines, offset, notMiter, deltaAngle), true)
                .RemoveSelfIntersections();
            var negativePolygons = new List<Polygon>();
            foreach (var polygon in path.Holes)
            {
                if (polygon.MaxX - polygon.MinX < 2 * offset || polygon.MaxY - polygon.MinY < 2 * offset) continue;
                var holeCoords = OffsetMainRoutine(polygon.Lines, offset, false, deltaAngle);
                holeCoords.Reverse();
                var newHoles = new Polygon(holeCoords, true).RemoveSelfIntersections();
                foreach (var newHole in newHoles)
                {
                    newHole.Reverse();
                    negativePolygons.Add(newHole);
                }
            }
            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.OrderBy(poly => poly.Area).ToList(),
                negativePolygons.OrderBy(poly => poly.Area));
        }

        private static List<Vector2> OffsetMainRoutine(List<PolygonSegment> Lines, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            var numPoints = Lines.Count;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(deltaAngle) : Matrix3x3.Null;
            if (notMiter) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2>(startingListSize);
            var prevLine = Lines[^1];
            var prevLineLengthReciprocal = 1.0 / prevLine.Length;
            var prevUnitNormal = new Vector2(prevLine.Vector.Y * prevLineLengthReciprocal, -prevLine.Vector.X * prevLineLengthReciprocal);
            for (int i = 0; i < numPoints; i++)
            {
                var nextLine = Lines[i];
                var point = nextLine.FromPoint.Coordinates;
                var nextLineLengthReciprocal = 1.0 / nextLine.Length;
                var nextUnitNormal = new Vector2(nextLine.Vector.Y * nextLineLengthReciprocal, -nextLine.Vector.X * nextLineLengthReciprocal);
                var cross = prevLine.Vector.Cross(nextLine.Vector);
                if (cross * offset > 0 && roundCorners)
                {
                    var firstPoint = point + offset * prevUnitNormal;
                    pointsList.Add(firstPoint);
                    var lastPoint = point + offset * nextUnitNormal;
                    var firstToLastVector = lastPoint - firstPoint;
                    var transform = Matrix3x3.CreateTranslation(-point) * rotMatrix *
                                    Matrix3x3.CreateTranslation(point);
                    var nextPoint = firstPoint.Transform(transform);
                    while (firstToLastVector.Dot(nextPoint - firstPoint) > 0)
                    {
                        pointsList.Add(nextPoint);
                        firstPoint = nextPoint;
                        nextPoint = firstPoint.Transform(transform);
                    }
                    pointsList.Add(lastPoint);
                }
                else if (cross * offset > 0 && notMiter)
                {
                    var middleUnitVector = (prevUnitNormal + nextUnitNormal).Normalize();
                    var middlePoint = point + offset * middleUnitVector;
                    var middleDir = new Vector2(-middleUnitVector.Y, middleUnitVector.X);
                    pointsList.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                        prevLine.Vector, middlePoint, middleDir));
                    pointsList.Add(MiscFunctions.LineLine2DIntersection(middlePoint, middleDir,
                        point + offset * nextUnitNormal, nextLine.Vector));
                }
                else
                    pointsList.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                        prevLine.Vector,
                        point + offset * nextUnitNormal, nextLine.Vector));
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
            }
            return pointsList;
        }

        #endregion
    }
}