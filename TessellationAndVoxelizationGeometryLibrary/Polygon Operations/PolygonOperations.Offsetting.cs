// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetSquare(this Polygon polygon, double offset,
            double tolerance = double.NaN,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            return Offset(polygon, offset, true, polygonSimplify, tolerance);
        }

        /// <summary>
        /// Offset the polygons with square corners. The resulting polygons are joined (unioned) if overlapping.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetSquare(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            return Offset(polygons, offset, true, polygonSimplify, tolerance);
        }

        /// <summary>
        /// Offset the polygon with miter (sharp) corners. This is the fastest of the three since the resulting polygon will have the 
        /// same number of points as the 
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetMiter(this Polygon polygon, double offset,
            double tolerance = double.NaN,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            return Offset(polygon, offset, false, polygonSimplify, tolerance);
        }
        /// <summary>
        /// Offset the polygon with miter (sharp) corners. The resulting polygons are joined (unioned) if overlapping.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="offset">The offset.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> OffsetMiter(this IEnumerable<Polygon> polygons, double offset,
            double tolerance = double.NaN,
            PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            return Offset(polygons, offset, false, polygonSimplify, tolerance);
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
            double tolerance = double.NaN,
            double maxCircleDeviation = double.NaN, PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            double deltaAngle = DefineDeltaAngle(offset, tolerance, maxCircleDeviation);
            return Offset(polygon, offset, true, polygonSimplify, tolerance, deltaAngle);
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
            double tolerance = double.NaN,
            double maxCircleDeviation = double.NaN, PolygonSimplify polygonSimplify = PolygonSimplify.CanSimplifyOriginal)
        {
            double deltaAngle = DefineDeltaAngle(offset, tolerance, maxCircleDeviation);
            return Offset(polygons, offset, true, polygonSimplify, tolerance, deltaAngle);
        }

        private static double DefineDeltaAngle(double offset, double tolerance, double maxCircleDeviation)
        {
            if (double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                return Constants.DefaultRoundOffsetDeltaAngle;
            if (!double.IsNaN(tolerance) && double.IsNaN(maxCircleDeviation))
                maxCircleDeviation = tolerance;
            return 2 * Math.Acos(1 - Math.Abs(maxCircleDeviation / offset));
        }


        private static List<Polygon> Offset(this Polygon polygon, double offset, bool notMiter,
            PolygonSimplify polygonSimplify, double tolerance, double deltaAngle = double.NaN)
        {
            polygon = polygon.CleanUpForBooleanOperations(polygonSimplify);
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

        private static List<Polygon> Offset(this IEnumerable<Polygon> polygons, double offset, bool notMiter,
            PolygonSimplify polygonSimplify, double tolerance, double deltaAngle = double.NaN)
        {
            polygons = polygons.CleanUpForBooleanOperations(polygonSimplify).ToList();
#if CLIPPER
            return OffsetViaClipper(polygons, offset, notMiter, tolerance, deltaAngle);
#elif !COMPARE
            var allPolygons = new List<Polygon>();
            foreach (var polygon in polygons)
                allPolygons.AddRange(polygon.OffsetJust(offset, notMiter, deltaAngle));
            if (allPolygons.Count > 1 && offset > 0)
                allPolygons = UnionPolygonsTVGL(allPolygons, PolygonSimplify.DoNotSimplify);
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
                allPolygons = UnionPolygonsTVGL(allPolygons, PolygonSimplify.DoNotSimplify);
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
            var diagLengthSqared = (new Vector2(bb.Length1 + 2 * Math.Max(0, offset), bb.Length2 + 2 * Math.Max(0, offset))).LengthSquared();
            var outer = CreateOffsetPoints(polygon, offset, notMiter, diagLengthSqared, deltaAngle, out var edgesToIgnore);
#if PRESENT
            Presenter.ShowAndHang(new[] { polygon, outer });
#endif
            var intersections = outer.GetSelfIntersections(edgesToIgnore)
                .Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
            var interaction = new PolygonInteractionRecord(outer, null);
            interaction.IntersectionData.AddRange(intersections);
            var intersectionLookup = interaction.MakeIntersectionLookupList(outer.Vertices.Count, true);
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            //AssignVisitedToWrongPolygons(outer, intersections, intersectionLookup, edgesToIgnore, polygonRemoveIntersections, offset < 0);

            var outers = (intersections.Count == 0) ? new List<Polygon> { outer }
            : polygonRemoveIntersections.Run(outer, intersections, ResultType.BothPermitted, false, edgesToIgnore);
            if (outers.Count > 0)
            {
                var maxOuterArea = outers.Max(p => p.PathArea);
                outers.RemoveAll(p => p.PathArea / maxOuterArea < 1e-5);
            }
            //#if PRESENT
            //            Presenter.ShowAndHang(outers);
            //#endif
            var inners = new List<Polygon>();
            foreach (var hole in polygon.InnerPolygons)
            {
                bb = hole.BoundingRectangle();
                // like the above, but a positive offset will close the hole
                if (hole.Vertices.Count == 0 || bb.Length1 < 2 * offset || bb.Length2 < 2 * offset) continue;
                var newHole = CreateOffsetPoints(hole, offset, notMiter, diagLengthSqared, deltaAngle, out edgesToIgnore);
                intersections = newHole.GetSelfIntersections().Where(intersect => intersect.Relationship != SegmentRelationship.NoOverlap).ToList();
                interaction.IntersectionData.Clear();
                interaction.IntersectionData.AddRange(intersections);
                intersectionLookup = interaction.MakeIntersectionLookupList(newHole.Vertices.Count, true);
                //AssignVisitedToWrongPolygons(newHole, intersections, intersectionLookup, edgesToIgnore, polygonRemoveIntersections, offset < 0);
                //#if PRESENT
                //                Presenter.ShowAndHang(new[] { polygon, newHole });
                //#endif
                if (intersections.Count == 0)
                    inners.Add(newHole);
                else inners.AddRange(polygonRemoveIntersections.Run(newHole, intersections, ResultType.OnlyKeepNegative, true, edgesToIgnore));
            }
            if (inners.Count == 0) return outers.Where(p => p.IsPositive && p.Vertices.Count > 2).ToList();
            var newPolygons = new List<Polygon>();
            foreach (var outer1 in outers)
            {
                newPolygons.AddRange(outer1.IntersectTVGL(inners, PolygonSimplify.DoNotSimplify));
            }
            return newPolygons;
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


        private static Polygon CreateOffsetPoints(Polygon polygon, double offset, bool notMiter, double maxLengthSquared, double deltaAngle,
            out HashSet<int> indicesOfWrongLines)
        {
            indicesOfWrongLines = new HashSet<int>();
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
            Vector2 lastPathPoint = Vector2.Null;
            // previous line starts as the first edge in the polygon then it updates to whatever nextLine was assigned to. In addition to the previous line, we
            // also want to capture the unit vector pointing outward (which is in the {Y, -X} direction) and the prevLineLengthReciprocal
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
                var cross = prevUnitNormal.Cross(nextUnitNormal);
                var dot = prevUnitNormal.Dot(nextUnitNormal);

                // if the cross is positive and the offset is positive (or there both negative), then we will need to make extra points
                // let's start with the roundCorners
                if (cross * offset > 0)
                {
                    if (dot > 0 && 2 * offsetSquared * (1 - dot) < minEdgeLengthSqd)
                        AddToOffsetPath(path, ref lastPathPoint, point + offset * prevUnitNormal, minEdgeLengthSqd);
                    else if (roundCorners)
                    {
                        var firstPoint = point + offset * prevUnitNormal;
                        AddToOffsetPath(path, ref lastPathPoint, firstPoint, minEdgeLengthSqd);
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
                            AddToOffsetPath(path, ref lastPathPoint, nextPoint, minEdgeLengthSqd);
                            firstPoint = nextPoint;
                            nextPoint = firstPoint.Transform(transform);
                        }
                        AddToOffsetPath(path, ref lastPathPoint, lastPoint, minEdgeLengthSqd);
                    }
                    // if the cross is positive and the offset is positive, then we will need to make extra points for the 
                    // squaredCorners
                    else if (notMiter)
                    {
                        // find these two points by calling the LineLine2DIntersection function twice. 
                        var middleUnitVector = (prevUnitNormal + nextUnitNormal).Normalize();
                        var middlePoint = point + offset * middleUnitVector;
                        var middleDir = new Vector2(-middleUnitVector.Y, middleUnitVector.X);
                        AddToOffsetPath(path, ref lastPathPoint, MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, middlePoint, middleDir), minEdgeLengthSqd);
                        AddToOffsetPath(path, ref lastPathPoint, MiscFunctions.LineLine2DIntersection(middlePoint, middleDir,
                            point + offset * nextUnitNormal, nextLine.Vector), minEdgeLengthSqd);
                    }
                    // miter and concave connections are done the same way...
                    else
                    {
                        var intersection = MiscFunctions.LineLine2DIntersection(point + offset * prevUnitNormal,
                            prevLine.Vector, point + offset * nextUnitNormal, nextLine.Vector);
                        if (intersection.IsNull())
                            AddToOffsetPath(path, ref lastPathPoint, point + offset * prevUnitNormal, minEdgeLengthSqd);
                        else
                        {
                            // if the corner is too shape the new point will be placed far away (near infinity). This is to rein it in
                            var vectorToCorner = intersection - point;
                            var vectorToCornerLengthSquared = vectorToCorner.LengthSquared();
                            if (vectorToCornerLengthSquared > maxLengthSquared)
                                intersection =
                                    point + vectorToCorner * Math.Sqrt(maxLengthSquared / vectorToCornerLengthSquared);
                            AddToOffsetPath(path, ref lastPathPoint, intersection, minEdgeLengthSqd);
                        }
                    }
                }
                else // here is where we add the wrong point. We have to add them because the intersections (both immediate and with
                     // lines far from this).
                {
                    // with the information found thus far, we can determine whether or not to put down one point or two.
                    // using law of cosines, we can find the distance between the two points that would result from offsetting lines at
                    // the current point. h^2 = 2*d^2*(1-cos(theta)).   Instead of solving for cos(theta), we can use the dot product
                    if (dot > 0 && 1.0002 * offsetSquared * (1 - dot) < minEdgeLengthSqd)
                    {
                        var combineNormal = (prevUnitNormal + nextUnitNormal).Normalize();
                        AddToOffsetPath(path, ref lastPathPoint, point + offset * combineNormal, minEdgeLengthSqd);
                    }
                    else
                    {
                        path.Add(point + offset * prevUnitNormal);
                        indicesOfWrongLines.Add(path.Count);
                        lastPathPoint = point + offset * nextUnitNormal;
                        path.Add(lastPathPoint);
                    }
                }
                prevLine = nextLine;
                prevUnitNormal = nextUnitNormal;
                //#if PRESENT
                //                                Presenter.ShowAndHang(new[] { polygon, new Polygon(path) });
                //#endif
            }
            #region SimplifyFast but with updates to indicesOfWrongPoints
            //minEdgeLengthSqd = Math.Pow(10, -(int)(1.7 * polygon.NumSigDigits));
            //var forwardPoint = path[0];
            //for (int i = path.Count - 1; i >= 0; i--)
            //{
            //    var currentPoint = path[i];
            //    var line = forwardPoint - currentPoint;
            //    if (line.LengthSquared() < minEdgeLengthSqd) // || cross.IsNegligible(minEdgeLengthSqd) )
            //    {
            //        var positionInWrongPoints = indicesOfWrongLines.Count - 1;
            //        while (positionInWrongPoints >= 0 && i < indicesOfWrongLines[positionInWrongPoints])
            //            indicesOfWrongLines[positionInWrongPoints--]--;
            //        if (positionInWrongPoints >= 0 && i == indicesOfWrongLines[positionInWrongPoints])
            //            indicesOfWrongLines.RemoveAt(positionInWrongPoints);
            //        path.RemoveAt(i);
            //        if (path.Count == i) forwardPoint = path[0];
            //        else forwardPoint = path[i];
            //    }
            //    else
            //        forwardPoint = currentPoint;
            //}
            //var nextIndex = path.Count;
            //for (int i = indicesOfWrongLines.Count - 1; i > 0; i--)
            //{
            //    var currentIndex = indicesOfWrongLines[i];
            //    if (currentIndex - indicesOfWrongLines[i - 1] > 1 &&
            //        nextIndex - currentIndex > 1)
            //    {
            //        indicesOfWrongLines.RemoveAt(i);
            //        //Debug.WriteLine("*** Lone wrong point!! ***");
            //        if (indicesOfWrongLines.Count == i) nextIndex = path.Count;
            //        else nextIndex = indicesOfWrongLines[i];
            //    }
            //    else nextIndex = currentIndex;
            //}

            #endregion
            return new Polygon(path);
        }

        private static void AddToOffsetPath(List<Vector2> path, ref Vector2 lastPathPoint, Vector2 newPoint, double minLengthSqared)
        {
            if ((newPoint - lastPathPoint).LengthSquared() < minLengthSqared) return;
            lastPathPoint = newPoint;
            path.Add(newPoint);
        }
        #endregion
    }
}