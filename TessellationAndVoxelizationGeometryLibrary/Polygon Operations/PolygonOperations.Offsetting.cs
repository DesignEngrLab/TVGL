using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        #region TVGL Offsetting

        public static List<Polygon> OffsetSquare(this Polygon path, double offset)
        {
            return Offset(path, offset, true);
        }

        public static List<Polygon> OffsetSquare(this List<Polygon> path, double offset)
        {
            return Offset(path, offset, true);
        }

        public static List<Polygon> OffsetMiter(this Polygon path, double offset)
        {
            return Offset(path, offset, false);
        }
        public static List<Polygon> OffsetMiter(this List<Polygon> path, double offset)
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
        public static List<Polygon> OffsetRound(this List<Polygon> path, double offset, double maxCircleDeviation = double.NaN)
        {
            var deltaAngle = double.IsNaN(maxCircleDeviation)
                ? Constants.DefaultRoundOffsetDeltaAngle
                : 2 * Math.Acos(1 - Math.Abs(maxCircleDeviation / offset));
            return Offset(path, offset, true, deltaAngle);
        }


        private static List<Polygon> Offset(this Polygon polygon, double offset, bool notMiter,
            double deltaAngle = double.NaN)
        {
            return Offset(new[] { polygon }, offset, notMiter, deltaAngle);
        }
        private static List<Polygon> Offset(this IEnumerable<Polygon> polygons, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            var positivePolygons = new List<Polygon>();
            var negativePolygons = new List<Polygon>();
            foreach (var polygon in polygons)
            {
                if (polygon.MaxX - polygon.MinX < -2 * offset || polygon.MaxY - polygon.MinY < -2 * offset) continue;
                positivePolygons.AddRange(new Polygon(OffsetRoutineForward(polygon.Lines, offset, notMiter, deltaAngle))
                    .RemoveSelfIntersections(true,out _));
                foreach (var hole in polygon.Holes)
                {
                    if (hole.MaxX - hole.MinX < 2 * offset || hole.MaxY - hole.MinY < 2 * offset) continue;
                    var holeCoords = OffsetRoutineBackwards(hole.Lines, -offset, false, deltaAngle);
                    var newHoles = new Polygon(holeCoords).RemoveSelfIntersections(true, out _);
                    foreach (var newHole in newHoles)
                        negativePolygons.Add(newHole);
                }
            }
            for (var i = 0; i < positivePolygons.Count; i++)
            {
                foreach (var hole in negativePolygons)
                {
                    var result = positivePolygons[i].Subtract(hole);
                    positivePolygons[i] = result[0];
                    for (int j = 1; j < result.Count; j++)
                        positivePolygons.Add(result[i]);
                }
            }

            return positivePolygons;
        }

        private static List<Vector2> OffsetRoutineForward(List<PolygonSegment> Lines, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var numPoints = Lines.Count;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(deltaAngle) : Matrix3x3.Null;
            if (notMiter && !roundCorners) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2>(startingListSize);

            // previous line starts at the end of the list and then updates to whatever next line was. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction). The prevLineLengthReciprocal was originally
            // thought to have uses outside of the unit vector but it doesn't. Anyway, slight speed up in calculating it once
            var prevLine = Lines[^1];
            var prevLineLengthReciprocal = 1.0 / prevLine.Length;
            var prevUnitNormal = new Vector2(prevLine.Vector.Y * prevLineLengthReciprocal, -prevLine.Vector.X * prevLineLengthReciprocal);
            for (int i = 0; i < numPoints; i++)
            {
                var nextLine = Lines[i];
                var nextLineLengthReciprocal = 1.0 / nextLine.Length;
                var nextUnitNormal = new Vector2(nextLine.Vector.Y * nextLineLengthReciprocal, -nextLine.Vector.X * nextLineLengthReciprocal);
                // establish the new offset points for the point connecting prevLine to nextLive. this is stored as "point".
                var point = nextLine.FromPoint.Coordinates;
                var cross = prevLine.Vector.Cross(nextLine.Vector);
                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                if (cross * offset > 0 && roundCorners)
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
                    while (firstToLastNormal.Dot(nextPoint - lastPoint) > 0)
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
                    pointsList.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                        prevLine.Vector,
                        point + offset * nextUnitNormal, nextLine.Vector));
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
            }
            return pointsList;
        }

        private static List<Vector2> OffsetRoutineBackwards(List<PolygonSegment> Lines, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var numPoints = Lines.Count;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(deltaAngle) : Matrix3x3.Null;
            if (notMiter) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2>(startingListSize);

            // previous line starts at the end of the list and then updates to whatever next line was. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction). The prevLineLengthReciprocal was originally
            // thought to have uses outside of the unit vector but it doesn't. Anyway, slight speed up in calculating it once
            var prevLine = Lines[0];
            var prevLineLengthReciprocal = 1.0 / prevLine.Length;
            var prevUnitNormal = new Vector2(-prevLine.Vector.Y * prevLineLengthReciprocal, prevLine.Vector.X * prevLineLengthReciprocal);
            for (int i = numPoints - 1; i >= 0; i--)
            {
                var nextLine = Lines[i];
                var nextLineLengthReciprocal = 1.0 / nextLine.Length;
                var nextUnitNormal = new Vector2(-nextLine.Vector.Y * nextLineLengthReciprocal, nextLine.Vector.X * nextLineLengthReciprocal);
                // establish the new offset points for the point connecting prevLine to nextLive. this is stored as "point".
                var point = nextLine.ToPoint.Coordinates;
                var cross = prevLine.Vector.Cross(nextLine.Vector);
                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                if (cross * offset > 0 && roundCorners)
                {
                    var firstPoint = point + offset * prevUnitNormal;
                    pointsList.Add(firstPoint);
                    var lastPoint = point + offset * nextUnitNormal;
                    var firstToLastVector = lastPoint - firstPoint;
                    // to avoid "costly" call to Math.Sin and Math.Cos, we create the transform matrix that 1) translates to origin
                    // 2) rotates by the angle, and 3) translates back
                    var transform = Matrix3x3.CreateTranslation(-point) * rotMatrix *
                                    Matrix3x3.CreateTranslation(point);
                    var nextPoint = firstPoint.Transform(transform);
                    // the problem with this matrix transform is figuring out when to stop. It turns out that the vector connecting
                    // the starting point of the curve with the last point must always be in the same direction (positive dot-product)
                    // with the new line segment
                    while (firstToLastVector.Dot(nextPoint - firstPoint) > 0)
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
                    pointsList.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                        prevLine.Vector,
                        point + offset * nextUnitNormal, nextLine.Vector));
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
            }
            return pointsList;
        }


        /// <summary>
        /// Creates the shallow polygon trees following boolean operations. The name follows the public methods,
        /// this is meant to be used only internally as it requires several assumptions:
        /// 1. positive polygons are ordered by increasing area (from 0 to +inf)
        /// 2. negative polygons are ordered by increasing area (from -inf to 0)
        /// 3. there are not intersections between the solids (this should be the result following the boolean
        /// operation; however, it is possible that they share a vertex (e.g. in XOR))
        /// </summary>
        /// <param name="Polygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <returns>Polygon[].</returns>
        /// <exception cref="Exception">Intersections still exist between hole and positive polygon.</exception>
        /// <exception cref="Exception">Negative polygon was not inside any positive polygons</exception>
        private static List<Polygon> CreateShallowPolygonTreesPostOffsetting(List<Polygon> polygons,
            IEnumerable<Polygon> negativePolygons)
        //SortedDictionary<double, Polygon>.ValueCollection negativePolygons)
        {
            int i = 0;
            while (i < polygons.Count)
            {
                var foundToBeInsideOfOther = false;
                int j = i;
                while (++j < polygons.Count)
                {
                    if (polygons[j].IsNonIntersectingPolygonInside(polygons[i], out _))
                    {
                        foundToBeInsideOfOther = true;
                        break;
                    }
                }
                if (foundToBeInsideOfOther)
                {
                    foreach (var hole in polygons[i].Holes)
                        polygons[j].AddHole(hole);
                    polygons.RemoveAt(i);
                }
                else i++;
            }
            //  Find the positive polygon that this negative polygon is inside.
            //The negative polygon belongs to the smallest positive polygon that it fits inside.
            //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
            //and the reversed ordering, gaurantee that we get the correct shallow tree.
            foreach (var negativePolygon in negativePolygons)
            {
                //var isInside = false;
                //Start with the smallest positive polygon           
                for (var j = 0; j < polygons.Count; j++)
                {
                    var positivePolygon = polygons[j];
                    if (positivePolygon.IsNonIntersectingPolygonInside(negativePolygon, out var onBoundary))
                    {
                        if (onBoundary)
                            polygons[j] = positivePolygon.Union(negativePolygon)[0]; // i don't know if this is a problem, but the
                                                                                     // new polygon at j may be smaller (now that it has a big hole in it ) than the preceding ones. I don't think
                                                                                     // we need to maintain ordered by area - since the first loop above will already merge positive loops
                        else positivePolygon.AddHole(negativePolygon);
                        //The negative polygon ONLY belongs to the smallest positive polygon that it fits inside.
                        //isInside = true;
                        break;
                    }
                }
                //if (!isInside) throw new Exception("Negative polygon was not inside any positive polygons");
            }
            //Set the polygon indices
            var index = 0;
            foreach (var polygon in polygons)
            {
                polygon.Index = index++;
                foreach (var hole in polygon.Holes)
                {
                    hole.Index = polygon.Index;
                }
            }
            return polygons;
        }


        #endregion
    }
}