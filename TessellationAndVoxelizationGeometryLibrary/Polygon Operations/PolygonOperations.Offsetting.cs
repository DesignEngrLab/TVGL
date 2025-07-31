// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonOperations.Offsetting.cs" company="Design Engineering Lab">
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
        /// <param name="tolerance">The tolerance.</param>
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
        /// <param name="tolerance">The tolerance.</param>
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
        /// <param name="tolerance">The tolerance.</param>
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
        /// <param name="tolerance">The tolerance.</param>
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
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="maxCircleDeviation">The maximum circle deviation. If none is provided, then vertices are
        /// placed at every 1 degree (pi/180 radians).</param>
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
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="maxCircleDeviation">The maximum circle deviation.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetRound(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN, double maxCircleDeviation = double.NaN)
        {
            double deltaAngle = DefineDeltaAngle(offset, tolerance, maxCircleDeviation);
            return Offset(polygons, offset, true, tolerance, deltaAngle);
        }

        /// <summary>
        /// Defines the delta angle.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="maxCircleDeviation">The maximum circle deviation.</param>
        /// <returns>System.Double.</returns>
        private static double DefineDeltaAngle(double offset, double tolerance, double maxCircleDeviation)
        {
            if (double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                return Constants.DefaultRoundOffsetDeltaAngle;
            if (!double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                maxCircleDeviation = tolerance;
            return 2 * Math.Acos(1 - Math.Abs(maxCircleDeviation / offset));
        }


        /// <summary>
        /// Offsets the specified offset.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> Offset(this IEnumerable<Polygon> polygons, double offset, bool notMiter,
            double tolerance, double deltaAngle = double.NaN)
        {
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
                    TVGL.IO.Save(poly, fileNameStart + "." + (i++).ToString() + ".json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Offsets the just.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> OffsetJust(this Polygon polygon, double offset, bool notMiter, double deltaAngle = double.NaN)
        {
            var bb = polygon.BoundingRectangle();
            // if the offset is negative then perhaps we just delete the entire polygon if it is smaller than
            // twice the offset. Notice how the RHS is negative and can only be true is offset is negative
            if (bb.Length1 < -2 * offset || bb.Length2 < -2 * offset)
                return new List<Polygon>();
            var longerLength = Math.Max(bb.Length1, bb.Length2);
            var longerLengthSquared = longerLength * longerLength; // 3 * offset * offset;
            var outerData = MainOffsetRoutine(polygon, offset, notMiter, longerLengthSquared, out var maxNumberOfPolygons,
                deltaAngle);
            var outer = new Polygon(outerData.points);
            var outers = outer.RemoveSelfIntersections(ResultType.OnlyKeepPositive, outerData.knownWrongPoints);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHoleData = MainOffsetRoutine(hole, offset, notMiter, longerLengthSquared, out maxNumberOfPolygons, deltaAngle);
                var newHoles = new Polygon(newHoleData.points);
                inners.AddRange(newHoles.RemoveSelfIntersections(ResultType.OnlyKeepNegative, newHoleData.knownWrongPoints, maxNumberOfPolygons).Where(p => !p.IsPositive));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive).ToList();
            return outers.IntersectPolygons(inners).Where(p => p.IsPositive).ToList();
        }


        /// <summary>
        /// Offsets the specified offset.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> Offset(this Polygon polygon, double offset, bool notMiter, double tolerance, double deltaAngle = double.NaN)
        {
#if CLIPPER
            return OffsetViaClipper([polygon], offset, notMiter, tolerance, deltaAngle);
#elif !COMPARE
            var bb = polygon.BoundingRectangle();
            // if the offset is negative then perhaps we just delete the entire polygon if it is smaller than
            // twice the offset. Notice how the RHS is negative and can only be true is offset is negative
            if (bb.Length1 < -2 * offset || bb.Length2 < -2 * offset)
                return new List<Polygon>();
            var longerLength = Math.Max(bb.Length1, bb.Length2);
            var longerLengthSquared = longerLength * longerLength; // 3 * offset * offset;
            var outerData = MainOffsetRoutine(polygon, offset, notMiter, longerLengthSquared, out var maxNumberOfPolygons,
                deltaAngle);
            var outer = new Polygon(outerData.points);
            var outers = outer.RemoveSelfIntersections(ResultType.OnlyKeepPositive, outerData.knownWrongPoints);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHoleData = MainOffsetRoutine(hole, offset, notMiter, longerLengthSquared, out maxNumberOfPolygons, deltaAngle);
                var newHoles = new Polygon(newHoleData.points);
                inners.AddRange(newHoles.RemoveSelfIntersections(ResultType.OnlyKeepNegative, newHoleData.knownWrongPoints, maxNumberOfPolygons).Where(p => !p.IsPositive));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive).ToList();
            return outers.IntersectPolygons(inners).Where(p => p.IsPositive).ToList();
#else
            sw.Restart();
            var pClipper = OffsetViaClipper([polygon], offset, notMiter, tolerance, deltaAngle);
            sw.Stop();
            var clipTime = sw.Elapsed;
            sw.Restart();
            var bb = polygon.BoundingRectangle();
            // if the offset is negative then perhaps we just delete the entire polygon if it is smaller than
            // twice the offset. Notice how the RHS is negative and can only be true is offset is negative
            if (bb.Length1 < -2 * offset || bb.Length2 < -2 * offset)
                return new List<Polygon>();
            var longerLength = Math.Max(bb.Length1, bb.Length2);
            var longerLengthSquared = longerLength * longerLength; // 3 * offset * offset;
            var outerData = MainOffsetRoutine(polygon, offset, notMiter, longerLengthSquared, out var maxNumberOfPolygons,
                deltaAngle);
            var outer = new Polygon(outerData.points);
            var outers = outer.RemoveSelfIntersections(ResultType.OnlyKeepPositive, outerData.knownWrongPoints);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHoleData = MainOffsetRoutine(hole, offset, notMiter, longerLengthSquared, out maxNumberOfPolygons, deltaAngle);
                var newHoles = new Polygon(newHoleData.points);
                inners.AddRange(newHoles.RemoveSelfIntersections(ResultType.OnlyKeepNegative, newHoleData.knownWrongPoints, maxNumberOfPolygons).Where(p => !p.IsPositive));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive).ToList();
            var pTVGL = outers.IntersectPolygons(inners).Where(p => p.IsPositive).ToList();
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
                TVGL.IO.Save(polygon, fileNameStart + ".0.json");
#endif
            }
            return pClipper;
#endif
        }

        /// <summary>
        /// Mains the offset routine.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="notMiter">if set to <c>true</c> [not miter].</param>
        /// <param name="maxLengthSquared">The maximum length squared.</param>
        /// <param name="maxNumberOfPolygons">The maximum number of polygons.</param>
        /// <param name="deltaAngle">The delta angle.</param>
        /// <returns>System.ValueTuple&lt;List&lt;Vector2&gt;, List&lt;System.Boolean&gt;&gt;.</returns>
        private static (List<Vector2IP> points, List<bool> knownWrongPoints) MainOffsetRoutine(Polygon polygon, double offset, bool notMiter,
            double maxLengthSquared, out int maxNumberOfPolygons, double deltaAngle = double.NaN)
        {
            var rashOffset = new RationalIP(offset);
            //var tolerance = Math.Pow(10, -polygon.NumSigDigits);
            maxNumberOfPolygons = 1;
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var numPoints = polygon.Edges.Count;
            int numFalsesToAdd;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var offsetSign = Math.Sign(offset);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(offsetSign * deltaAngle) : Matrix3x3.Null;
            if (notMiter && !roundCorners) startingListSize = (int)(1.5 * startingListSize);
            var pointsList = new List<Vector2IP>(startingListSize);
            var wrongPoints = new List<bool>(startingListSize);
            // previous line starts at the end of the list and then updates to whatever next line was. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction). The prevLineLengthReciprocal was originally
            // thought to have uses outside of the unit vector but it doesn't. Anyway, slight speed up in calculating it once
            var startLine = polygon.Edges[0];
            var startNormal = startLine.Normal;
            var start2DMag = (startNormal.X * startNormal.X + startNormal.Y * startNormal.Y).SquareRoot();
            var startOffsetNormal = new Vector2IP(startNormal.X, startNormal.Y, (startNormal.W - start2DMag * rashOffset).AsInt128);
            var prevLine = startLine;
            var prevNormal = startNormal;
            var prev2DMag = start2DMag;
            var prevOffsetNormal = startOffsetNormal;
            for (int i = 1; i <= numPoints; i++)
            {
                PolygonEdge thisLine;
                Vector2IP thisNormal;
                Int128 this2DMag;
                // tilt plane away so that it represents offset line
                Vector2IP thisOffsetNormal; 
                if (i == numPoints)
                {
                    thisLine = startLine;
                    thisNormal = startNormal;
                    this2DMag = start2DMag;
                    thisOffsetNormal = startOffsetNormal;
                }
                else
                {
                     thisLine = (i == numPoints) ? polygon.Edges[0] : polygon.Edges[i];
                     thisNormal = thisLine.Normal;
                     this2DMag = (thisNormal.X * thisNormal.X + thisNormal.Y * thisNormal.Y).SquareRoot();
                    // tilt plane away so that it represents offset line
                     thisOffsetNormal = new Vector2IP(thisNormal.X, thisNormal.Y, (thisNormal.W - this2DMag * rashOffset).AsInt128);
                }
                var isConvex = prevLine.Normal.Cross3D(thisLine.Normal).W > 0;
                if (isConvex == offset > 0) // then making a positive corner (not an overlapping feature)
                {
                    if ((polygon.IsPositive && offset < 0) || (!polygon.IsPositive && offset > 0)) maxNumberOfPolygons++;
                    if (roundCorners)
                    {
                        // the nominal points are at the intersection of the offset lines (thisOffsetNormal and prevOffsetNormal) and the
                        // plane perpendicular to the line representing the point and the W-axis [0,0,1]...actually, this is the edge vector
                        // with the w component removed.
                        var firstPoint = PGA2D.PointAtLineIntersection(prevOffsetNormal, new Vector2IP(prevLine.Vector.X, prevLine.Vector.Y, 0));
                        pointsList.Add(firstPoint);
                        var lastPoint = PGA2D.PointAtLineIntersection(prevOffsetNormal, new Vector2IP(thisLine.Vector.X, thisLine.Vector.Y, 0));
                        
                        /*
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
                        */
                        pointsList.Add(lastPoint);
                    }
                    // if the cross is positive and the offset is positive, then we will need to make extra points for the 
                    // squaredCorners
                    else if (notMiter)
                        pointsList.Add(PGA2D.PointAtLineIntersection(prevOffsetNormal, thisOffsetNormal));
                    else
                    {
                        var firstPoint = PGA2D.PointAtLineIntersection(prevOffsetNormal, new Vector2IP(prevLine.Vector.X, prevLine.Vector.Y, 0));
                        pointsList.Add(firstPoint);
                        var lastPoint = PGA2D.PointAtLineIntersection(prevOffsetNormal, new Vector2IP(thisLine.Vector.X, thisLine.Vector.Y, 0));
                        pointsList.Add(lastPoint);
                    }
                }
                else
                    pointsList.Add(PGA2D.PointAtLineIntersection(thisOffsetNormal, prevOffsetNormal));

                prevLine = thisLine;
                prevNormal = thisNormal;
                prev2DMag = this2DMag;
                prevOffsetNormal = thisOffsetNormal;
            }
            numFalsesToAdd = pointsList.Count - wrongPoints.Count;
            for (int k = 0; k < numFalsesToAdd; k++) wrongPoints.Add(false);
            return (pointsList, wrongPoints);
        }
        #endregion
    }
}