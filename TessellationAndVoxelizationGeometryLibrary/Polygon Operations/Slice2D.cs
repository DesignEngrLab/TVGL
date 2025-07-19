using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Class PolygonOperations.
    /// </summary>
    public static partial class PolygonOperations
    {
        [Obsolete("Use Polygons as this functions constructs them anyway.")]
        /// <summary>
        /// Slices the polygons at the provided line. Note that the line is represented as 4 numbers. Think of it as a
        /// plane cutting through this 2D plane. Instead of the line direction, we receive the normal to the line, the lineNormalDirection.
        /// Instead of an anchor point on the line, all we need is the perpendicular distance to the line.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="perpendicularDistanceToLine">The distance along direction.</param>
        /// <param name="negativeSidePolygons">The negative side polygons.</param>
        /// <param name="positiveSidePolygons">The positive side polygons.</param>
        /// <param name="offsetAtLineForNegativeSide">The offset at line for negative side.</param>
        /// <param name="offsetAtLineForPositiveSide">The offset at line for positive side.</param>
        /// <returns>Vector2[].</returns>
        public static Vector2[] SliceAtLine(this IEnumerable<IEnumerable<Vector2>> shape, Vector2 lineNormalDirection, double perpendicularDistanceToLine,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var polyTrees = CreateShallowPolygonTrees(shape, false);
            return SliceAtLine(polyTrees, lineNormalDirection, perpendicularDistanceToLine, out negativeSidePolygons, out positiveSidePolygons,
                   offsetAtLineForNegativeSide, offsetAtLineForPositiveSide);
        }

        /// <summary>
        /// Slices the polygons at the provided line. Note that the line is represented as 4 numbers. Think of it as a
        /// plane cutting through this 2D plane. Instead of the line direction, we receive the normal to the line, the lineNormalDirection.
        /// Instead of an anchor point on the line, all we need is the perpendicular distance to the line.
        /// </summary>
        /// <param name="polyTrees">The poly trees.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="perpendicularDistanceToLine">The distance along direction.</param>
        /// <param name="negativeSidePolygons">The negative side polygons.</param>
        /// <param name="positiveSidePolygons">The positive side polygons.</param>
        /// <param name="offsetAtLineForNegativeSide">The offset at line for negative side.</param>
        /// <param name="offsetAtLineForPositiveSide">The offset at line for positive side.</param>
        /// <returns>Vector2[].</returns>
        public static Vector2[] SliceAtLine(this IEnumerable<Polygon> polyTrees, Vector2 lineNormalDirection, double perpendicularDistanceToLine,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            negativeSidePolygons = new List<Polygon>();
            positiveSidePolygons = new List<Polygon>();
            var intersections = new List<Vector2>();
            foreach (var shallowPolygonTree in polyTrees)
            {
                intersections.AddRange(SliceAtLine(shallowPolygonTree, lineNormalDirection, perpendicularDistanceToLine, out var thisNegativeSidePolys,
                    out var thisPositiveSidePolys, offsetAtLineForNegativeSide, offsetAtLineForPositiveSide));
                negativeSidePolygons.AddRange(thisNegativeSidePolys);
                positiveSidePolygons.AddRange(thisPositiveSidePolys);
            }

            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            return intersections.OrderBy(v => lineDir.Dot(v)).ToArray();
        }

        /// <summary>
        /// Slices the polygon at the provided line. Note that the line is represented as 4 numbers. Think of it as a
        /// plane cutting through this 2D plane. Instead of the line direction, we receive the normal to the line, the lineNormalDirection.
        /// Instead of an anchor point on the line, all we need is the perpendicular distance to the line.
        /// </summary>
        /// <param name="shallowPolygonTree">The shallow polygon tree.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="perpendicularDistanceToLine">The distance along direction.</param>
        /// <param name="negativeSidePolygons">The negative side polygons.</param>
        /// <param name="positiveSidePolygons">The positive side polygons.</param>
        /// <param name="offsetAtLineForNegativeSide">The offset at line for negative side.</param>
        /// <param name="offsetAtLineForPositiveSide">The offset at line for positive side.</param>
        /// <returns>Vector2[].</returns>
        /// <remarks>This function slices the [List(Polygon)] with a give direction and distance. If returnFurtherThanSlice = false,
        /// it will return the partial shape before the cutting line, otherwise those beyond the cutting line.
        /// The returned partial shape is properly closed and ordered CCW+, CW-.
        /// OffsetAtLine allows the use to offset the intersection line a given distance in a direction opposite to the
        /// returned partial shape (i.e., if returnFurtherThanSlice == true, a positive offsetAtLine value moves the
        /// intersection points before the line).</remarks>
        public static Vector2[] SliceAtLine(this Polygon shallowPolygonTree, Vector2 lineNormalDirection, double perpendicularDistanceToLine,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var lineNormal = new Vector2IP(lineNormalDirection.X, lineNormalDirection.Y, (long)(Vector2IP.InitialW*perpendicularDistanceToLine));
            var offsetNegSide = new RationalIP(offsetAtLineForNegativeSide);
            var offsetPosSide = new RationalIP(offsetAtLineForPositiveSide);
            var distances = new List<RationalIP>();
            foreach (var polygons in shallowPolygonTree.AllPolygons)
                foreach (var vertex in polygons.Vertices)
                    distances.Add(vertex.Coordinates.Dot2D(lineNormal));

            var positiveShift = 0.0;
            var negativeShift = 0.0;
            distances.SetPositiveAndNegativeShifts(perpendicularDistanceToLine, Math.Pow(10, -shallowPolygonTree.NumSigDigits), ref positiveShift, ref negativeShift);
            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersecting lines is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance
            var intersectionLines = new HashSet<PolygonEdge>();
            var lineDir = new Vector2IP(-lineNormalDirection.Y, lineNormalDirection.X);
            var anchorpoint = perpendicularDistanceToLine * lineNormalDirection;
            var sortedPoints = new SortedList<double, (Vector2, PolygonEdge)>();
            negativeSidePolygons = new List<Polygon>();
            positiveSidePolygons = new List<Polygon>();
            foreach (var polygon in shallowPolygonTree.AllPolygons)
            {
                var untouched = true;
                foreach (var line in polygon.Edges)
                {
                    var intersectionPoint=
                        PGA2D.PointAtLineAndPolyEdge(lineNormal, line, out _, out var onSegment);
                    if (onSegment)
                    {
                        intersectionLines.Add(line);
                        var distanceAlong = lineDir.Dot2D(intersectionPoint);
                        sortedPoints.Add(distanceAlong, (intersectionPoint, line));
                        untouched = false;
                        //!line.ToPoint.Coordinates.Dot(lineNormalDirection).IsLessThanNonNegligible(distanceAlongDirection));
                    }
                }
                if (untouched)
                {
                    if (polygon.Vertices[0].Coordinates.Dot(lineNormalDirection) > perpendicularDistanceToLine)
                        positiveSidePolygons.Add(polygon);
                    else negativeSidePolygons.Add(polygon);
                }
            }
            // we don't really need the distances, so  convert the values to an array
            var pointsOnLineTuples = sortedPoints.Values.ToArray();
            // this is what is returned. although now sure if this is useful in any case

            #region patching up negative side polygons

            var negSidePolyCoords = new List<List<Vector2IP>>();
            for (int i = 0; i < pointsOnLineTuples.Length; i++)
            {
                // the first segment in the list should be passing from negative side to positive side (item3 should be true)
                var newSegmentTupleA = pointsOnLineTuples[i];
                i++;
                //  the second segment in the list should be passing from the positive to the negative side (item should be false)
                var newSegmentTupleB = pointsOnLineTuples[i];
                // throw an error if that's not the case
                //if (!newSegmentTupleA.Item3 || newSegmentTupleB.Item3)
                //    throw new Exception("the first should always be positive and the second should be negative.");

                // get the coordinates of the points on the line. note that the user may want these shifted
                var negSideFrom = newSegmentTupleA.Item1;
                var negSideTo = newSegmentTupleB.Item1;
                if (offsetAtLineForNegativeSide != 0)
                {
                    negSideFrom += offsetNegSide * lineNormal;
                    negSideTo += offsetNegSide * lineNormal;
                }
                // see if there is an existing polygon that ends where this one start
                var existingNegSidePolygon = negSidePolyCoords.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingNegSidePolygon == null)
                {
                    existingNegSidePolygon = new List<Vector2IP>();
                    negSidePolyCoords.Add(existingNegSidePolygon);
                }
                existingNegSidePolygon.Add(negSideFrom);
                existingNegSidePolygon.Add(negSideTo);
                var polySegment = newSegmentTupleB.Item2;
                do
                {
                    if (existingNegSidePolygon[0].Equals(polySegment.ToPoint.Coordinates)) break;
                    existingNegSidePolygon.Add(polySegment.ToPoint.Coordinates);
                    polySegment = polySegment.ToPoint.StartLine;
                }
                while (!intersectionLines.Contains(polySegment));
            }
            negativeSidePolygons.AddRange(negSidePolyCoords.Select(loop => new Polygon(loop)));
            negativeSidePolygons = negativeSidePolygons.CreateShallowPolygonTrees(true);

            #endregion patching up negative side polygons

            #region patching up positive side polygons

            var posSidePolyCoords = new List<List<Vector2IP>>();
            for (int i = sortedPoints.Count - 1; i >= 0; i--)
            {
                var newSegmentTupleA = pointsOnLineTuples[i];
                i--;
                var newSegmentTupleB = pointsOnLineTuples[i];

                var posSideFrom = newSegmentTupleA.Item1;
                var posSideTo = newSegmentTupleB.Item1;
                if (offsetAtLineForPositiveSide != 0)
                {
                    posSideFrom -= offsetPosSide * lineNormal;
                    posSideTo -= offsetPosSide * lineNormal;
                }
                var existingPosSidePolygon = posSidePolyCoords.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingPosSidePolygon == null)
                {
                    existingPosSidePolygon = new List<Vector2IP>();
                    posSidePolyCoords.Add(existingPosSidePolygon);
                }
                existingPosSidePolygon.Add(posSideFrom);
                existingPosSidePolygon.Add(posSideTo);
                var polySegment = newSegmentTupleB.Item2;
                do
                {
                    if (existingPosSidePolygon[0].Equals(polySegment.ToPoint.Coordinates)) break;
                    existingPosSidePolygon.Add(polySegment.ToPoint.Coordinates);
                    polySegment = polySegment.ToPoint.StartLine;
                }
                while (!intersectionLines.Contains(polySegment));
            }
            positiveSidePolygons.AddRange(posSidePolyCoords.Select(loop => new Polygon(loop)));
            positiveSidePolygons= positiveSidePolygons.CreateShallowPolygonTrees(true);

            #endregion patching up positive side polygons

            return pointsOnLineTuples.Select(p => p.Item1.AsVector2).ToArray();
        }
    }
}