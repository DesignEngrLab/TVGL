// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using TVGL.Enclosure_Operations;
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
            if (allPolygons.Count > 1 && offset > 0)
                allPolygons = UnionPolygonsFromOtherOps(allPolygons, PolygonCollection.PolygonWithHoles);
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
            var longerLength = Math.Max(bb.Length1, bb.Length2) + 2 * Math.Max(0, offset);
            var longerLengthSquared = longerLength * longerLength; // 3 * offset * offset;
            var outer = CreateOffsetPoints(polygon, offset, notMiter, longerLengthSquared, deltaAngle, out var wrongPoints);
//#if PRESENT
//            Presenter.ShowAndHang(new[] { polygon, outer });
//#endif
            var intersections = outer.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            var interaction = new PolygonInteractionRecord(outer, null);
            interaction.IntersectionData.AddRange(intersections);
            var intersectionLookup = interaction.MakeIntersectionLookupList(outer.Vertices.Count);
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            AssignVisitedToWrongPolygons(outer, intersections, intersectionLookup, wrongPoints, polygonRemoveIntersections, offset < 0);

            var outers = (intersections.Count == 0) ? new List<Polygon> { outer }
            : polygonRemoveIntersections.Run(outer, intersections, ResultType.BothPermitted, offset>0, false);
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHole = CreateOffsetPoints(hole, offset, notMiter, longerLengthSquared, deltaAngle, out wrongPoints);
                intersections = newHole.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
                interaction.IntersectionData.Clear();
                interaction.IntersectionData.AddRange(intersections);
                intersectionLookup = interaction.MakeIntersectionLookupList(outer.Vertices.Count);
                AssignVisitedToWrongPolygons(newHole, intersections, intersectionLookup, wrongPoints, polygonRemoveIntersections, offset < 0);
                //#if PRESENT
                //                Presenter.ShowAndHang(new[] { polygon, newHoles });
                //#endif
                if (intersections.Count == 0)
                    inners.Add(newHole);
                else inners.AddRange(polygonRemoveIntersections.Run(newHole, intersections, ResultType.OnlyKeepNegative, true, false));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive && p.Vertices.Count > 2).ToList();
            return outers.IntersectPolygonsFromOtherOps(inners).Where(p => p.IsPositive).ToList();
        }

        private static void AssignVisitedToWrongPolygons(Polygon outer, List<SegmentIntersection> intersections, List<int>[] intersectionLookup, List<int> wrongPointIndices,
            PolygonRemoveIntersections polygonRemoveIntersections, bool shapeIsOnlyNegative)
        {
            var wrongPoints = wrongPointIndices.Select(index => outer.Vertices[index]).ToHashSet();
            while (wrongPoints.Any())
            {
                var wrongPoint = wrongPoints.First();
                var endPoint = wrongPoint;
                wrongPoints.Remove(endPoint);
                PolygonEdge currentEdge = null;
                SegmentIntersection intersectionData = null;
                do
                {
                    if (endPoint != null)
                        currentEdge = endPoint.StartLine;
                    var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
                    if (intersectionIndices == null)
                    {
                        endPoint = currentEdge.ToPoint;
                        wrongPoints.Remove(endPoint);
                    }
                    else
                    {
                        var closestDistance = double.PositiveInfinity;
                        SegmentIntersection closestIntersection = null;
                        var datum = intersectionData != null ? intersectionData.IntersectCoordinates : endPoint.Coordinates;
                        foreach (var interIndex in intersectionIndices)
                        {
                            var inter = intersections[interIndex];
                            if (inter == intersectionData) continue;
                            var distance = currentEdge.Vector.Dot(inter.IntersectCoordinates - datum);
                            if (distance < 0) continue;
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestIntersection = inter;
                            }
                        }
                        if (closestIntersection == null)
                        {
                            endPoint = currentEdge.ToPoint;
                            wrongPoints.Remove(endPoint);
                            intersectionData = null;
                            continue;
                        }
                        intersectionData = closestIntersection;
                        endPoint = null;
                        if (!intersectionData.VisitedA && currentEdge == intersectionData.EdgeA)
                        {
                            intersectionData.VisitedA = true;
                            if (polygonRemoveIntersections.SwitchAtThisIntersectionFromOffsetting(intersectionData, true, shapeIsOnlyNegative))
                                currentEdge = intersectionData.EdgeB;
                        }
                        else if (!intersectionData.VisitedB && currentEdge != intersectionData.EdgeA)
                        {
                            intersectionData.VisitedB = true;
                            if (polygonRemoveIntersections.SwitchAtThisIntersectionFromOffsetting(intersectionData, false, shapeIsOnlyNegative))
                                currentEdge = intersectionData.EdgeA;
                        }
                        else break;
                    }
                } while (wrongPoint != endPoint);
            }
        }

        /// <summary>
        /// temporary
        /// </summary>
        /// <param name="polygonsA">The polygons a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        private static List<Polygon> IntersectPolygonsFromOtherOps(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles)
        {
            if (areaSimplificationFraction > 0)
            {
                polygonsA = polygonsA.CleanUpForBooleanOperations(out _);
                if (polygonsB != null)
                    polygonsB = polygonsB.CleanUpForBooleanOperations(out _);
            }

            if (polygonsB is null)
                return polygonsA.ToList();

            var result = polygonsA.ToList();
            foreach (var polygon in polygonsB.ToList())
            {
                if (!result.Any()) break;
                result = result.SelectMany(r =>
                {
                    var relationship = GetPolygonInteraction(r, polygon);
                    return Intersect(r, polygon, relationship, outputAsCollectionType);
                }).ToList();
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

        private static Polygon CreateOffsetPoints(Polygon polygon, double offset, bool notMiter, double maxLengthSquared, double deltaAngle,
            out List<int> indicesOfWrongPoints)
        {
            indicesOfWrongPoints = new List<int>();
            var minEdgeLengthSqd = 1E-6 * maxLengthSquared; // this means that the minimum edge would be one-thousandth of the longer side
            var offsetSquared = offset * offset;
            // set up the return list (predict size to prevent re-allocation) and rotation matrix for OffsetRound
            var numPoints = polygon.Edges.Length;
            var startingListSize = numPoints;
            var roundCorners = !double.IsNaN(deltaAngle);
            if (roundCorners) startingListSize += (int)(2 * Math.PI / deltaAngle);
            var offsetSign = Math.Sign(offset);
            var rotMatrix = roundCorners ? Matrix3x3.CreateRotation(offsetSign * deltaAngle) : Matrix3x3.Null;
            if (notMiter && !roundCorners) startingListSize = (int)(1.5 * startingListSize);
            var path = new List<Vector2>(startingListSize);
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

                // with the information found thus far, we can determine whether or not to put down one point or two.
                // using law of cosines, we can find the distance between the two points that would result from offsetting lines at
                // the current point. h^2 = 2*d^2*(1-cos(theta).   Instead of solving for cos(theta), we can use the dot product
                // and the line-length-reciprocals. 
                if (dot > 0 && 2 * offsetSquared * (1 - prevLineLengthReciprocal * nextLineLengthReciprocal * dot) < minEdgeLengthSqd)
                    path.Add(point + offset * prevUnitNormal);
                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                else if (cross * offset > 0)
                {
                    if (roundCorners)
                    {
                        var firstPoint = point + offset * prevUnitNormal;
                        path.Add(firstPoint);
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
                            path.Add(nextPoint);
                            firstPoint = nextPoint;
                            nextPoint = firstPoint.Transform(transform);
                        }
                        path.Add(lastPoint);
                    }
                    // if the cross is positive and the offset is positive, then we will need to make extra points for the 
                    // squaredCorners
                    else if (notMiter)
                    {
                        // find these two points by calling the LineLine2DIntersection function twice. 
                        var middleUnitVector = (prevUnitNormal + nextUnitNormal).Normalize();
                        var middlePoint = point + offset * middleUnitVector;
                        var middleDir = new Vector2(-middleUnitVector.Y, middleUnitVector.X);
                        path.Add(MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, middlePoint, middleDir));
                        path.Add(MiscFunctions.LineLine2DIntersection(middlePoint, middleDir,
                            point + offset * nextUnitNormal, nextLine.Vector));
                    }
                    // miter and concave connections are done the same way...
                    else
                    {
                        var intersection = MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, point + offset * nextUnitNormal, nextLine.Vector);
                        if (intersection.IsNull())
                            path.Add(point + offset * prevUnitNormal);
                        else
                        {
                            // if the corner is too shape the new point will be placed far away (near infinity). This is to rein it in
                            var vectorToCorner = intersection - point;
                            var vectorToCornerLengthSquared = vectorToCorner.LengthSquared();
                            if (vectorToCornerLengthSquared > maxLengthSquared)
                                intersection =
                                    point + vectorToCorner * Math.Sqrt(maxLengthSquared / vectorToCornerLengthSquared);
                            path.Add(intersection);
                        }
                    }
                }
                else
                {
                    indicesOfWrongPoints.Add(path.Count);
                    path.Add(point + offset * prevUnitNormal);
                    indicesOfWrongPoints.Add(path.Count);
                    path.Add(point + offset * nextUnitNormal);
                }
                prevLine = nextLine;
                prevLineLengthReciprocal = nextLineLengthReciprocal;
                prevUnitNormal = nextUnitNormal;
                //#if PRESENT
                //                Presenter.ShowAndHang(new[] { polygon, new Polygon(path) });
                //#endif
            }
            #region SimplifyFast but with updates to indicesOfWrongPoints
            minEdgeLengthSqd = Math.Pow(10, -(int)(1.7 * polygon.NumSigDigits));
            var forwardPoint = path[0];
            for (int i = path.Count - 1; i >= 0; i--)
            {
                var currentPoint = path[i];
                var line = forwardPoint - currentPoint;
                if (line.LengthSquared() < minEdgeLengthSqd) // || cross.IsNegligible(minEdgeLengthSqd) )
                {
                    var positionInWrongPoints = indicesOfWrongPoints.Count - 1;
                    while (positionInWrongPoints >= 0 && i < indicesOfWrongPoints[positionInWrongPoints])
                        indicesOfWrongPoints[positionInWrongPoints--]--;
                    if (positionInWrongPoints >= 0 && i == indicesOfWrongPoints[positionInWrongPoints])
                        indicesOfWrongPoints.RemoveAt(positionInWrongPoints);
                    path.RemoveAt(i);
                    if (path.Count == i) forwardPoint = path[0];
                    else forwardPoint = path[i];
                }
                else
                    forwardPoint = currentPoint;
            }
            var nextIndex = path.Count;
            for (int i = indicesOfWrongPoints.Count - 1; i > 0; i--)
            {
                var currentIndex = indicesOfWrongPoints[i];
                if (currentIndex - indicesOfWrongPoints[i - 1] > 1 &&
                    nextIndex - currentIndex > 1)
                {
                    indicesOfWrongPoints.RemoveAt(i);
                    //Debug.WriteLine("*** Lone wrong point!! ***");
                    if (indicesOfWrongPoints.Count == i) nextIndex = path.Count;
                    else nextIndex = indicesOfWrongPoints[i];
                }
                else nextIndex = currentIndex;
            }

            #endregion
            return new Polygon(path);
        }
        #endregion
    }
}