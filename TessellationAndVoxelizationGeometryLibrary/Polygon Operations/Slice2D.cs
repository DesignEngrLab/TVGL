using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVGL.Numerics;


namespace TVGL.TwoDimensional
{
    public static partial class PolygonOperations
    {
        /// <summary>
        /// This function slices the [List(Polygon)] with a give direction and distance. If returnFurtherThanSlice = false, 
        /// it will return the partial shape before the cutting line, otherwise those beyond the cutting line.
        /// The returned partial shape is properly closed and ordered CCW+, CW-.
        /// OffsetAtLine allows the use to offset the intersection line a given distance in a direction opposite to the 
        /// returned partial shape (i.e., if returnFurtherThanSlice == true, a positive offsetAtLine value moves the  
        /// intersection points before the line).
        /// </summary>
        public static Vector2[] SliceAtLine(this IEnumerable<IEnumerable<Vector2>> shape, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var polyTrees = CreateShallowPolygonTrees(shape);
            return SliceAtLine(polyTrees, lineNormalDirection, distanceAlongDirection, out negativeSidePolygons, out positiveSidePolygons,
                   offsetAtLineForNegativeSide, offsetAtLineForPositiveSide);
        }

        public static Vector2[] SliceAtLine(this IEnumerable<Polygon> polyTrees, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            negativeSidePolygons = new List<List<Vector2>>();
            positiveSidePolygons = new List<List<Vector2>>();
            var intersections = new List<Vector2>();
            foreach (var shallowPolygonTree in polyTrees)
            {
                intersections.AddRange(SliceAtLine(shallowPolygonTree, lineNormalDirection, distanceAlongDirection, out var thisNegativeSidePolys,
                    out var thisPositiveSidePolys, offsetAtLineForNegativeSide, offsetAtLineForPositiveSide));
                negativeSidePolygons.AddRange(thisNegativeSidePolys);
                positiveSidePolygons.AddRange(thisPositiveSidePolys);
            }

            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            return intersections.OrderBy(v => lineDir.Dot(v)).ToArray();
        }

        public static Vector2[] SliceAtLine(this Polygon shallowPolygonTree, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            negativeSidePolygons = new List<List<Vector2>>();
            positiveSidePolygons = new List<List<Vector2>>();
            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersecting lines is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using 
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance      
            var intersectionLines = new HashSet<PolygonSegment>();
            var collinearSegments = new HashSet<PolygonSegment>();
            var collinearPoints = new HashSet<Vector2>();
            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            var anchorpoint = distanceAlongDirection * lineNormalDirection;
            var sortedPoints = new SortedList<double, (Vector2, PolygonSegment)>();
            foreach (var polygons in shallowPolygonTree.AllPolygons)
                foreach (var line in polygons.Lines)
                {
                    if (MiscFunctions.SegmentLine2DIntersection(line.FromPoint.Coordinates, line.ToPoint.Coordinates,
                         anchorpoint, lineDir, out var intersectionPoint, true))
                    {
                        if (intersectionPoint.IsNull()) // this only happens in polygon line segment is collinear with separation line
                        {
                            collinearSegments.Add(line);
                            /* In order to avoid adding points more than once, we only add if the "To" points. 
                             * The "From" points will be added when they are "To"'s */
                            collinearPoints.Add(line.ToPoint.Coordinates);
                        }
                        else
                        {
                            intersectionLines.Add(line);
                            if (line.ToPoint.Coordinates.IsPracticallySame(intersectionPoint))
                                collinearPoints.Add(line.ToPoint.Coordinates);
                        }
                        var distanceAlong = lineDir.Dot(intersectionPoint);
                        sortedPoints.Add(distanceAlong, (intersectionPoint, line));
                            //!line.ToPoint.Coordinates.Dot(lineNormalDirection).IsLessThanNonNegligible(distanceAlongDirection));
                    }
                }
            // we don't really need the distances, so  convert the values to an array
            var pointsOnLineTuples = sortedPoints.Values.ToArray();
            // this is what is returned. although now sure if this is useful in any case

            #region patching up negative side polygons
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
                    negSideFrom += offsetAtLineForNegativeSide * lineNormalDirection;
                    negSideTo += offsetAtLineForNegativeSide * lineNormalDirection;
                }
                // see if there is an existing polygon that ends where this one start
                var existingNegSidePolygon = negativeSidePolygons.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingNegSidePolygon == null)
                {
                    existingNegSidePolygon = new List<Vector2>();
                    negativeSidePolygons.Add(existingNegSidePolygon);
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
            #endregion
            #region patching up positive side polygons
            for (int i = sortedPoints.Count - 1; i >= 0; i--)
            {
                var newSegmentTupleA = pointsOnLineTuples[i];
                i--;
                var newSegmentTupleB = pointsOnLineTuples[i];

                var posSideFrom = newSegmentTupleA.Item1;
                var posSideTo = newSegmentTupleB.Item1;
                if (offsetAtLineForPositiveSide != 0)
                {
                    posSideFrom -= offsetAtLineForPositiveSide * lineNormalDirection;
                    posSideTo -= offsetAtLineForPositiveSide * lineNormalDirection;
                }
                var existingPosSidePolygon = positiveSidePolygons.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingPosSidePolygon == null)
                {
                    existingPosSidePolygon = new List<Vector2>();
                    positiveSidePolygons.Add(existingPosSidePolygon);
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
            #endregion
            return pointsOnLineTuples.Select(p => p.Item1).ToArray();
        }
    }
}
