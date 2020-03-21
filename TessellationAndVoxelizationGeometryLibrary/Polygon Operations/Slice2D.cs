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
        public static Vector2[] SliceAtLine(this List<List<Vector2>> shape, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var polyTrees = GetShallowPolygonTrees(shape);
            return SliceAtLine(polyTrees, lineNormalDirection, distanceAlongDirection, out negativeSidePolygons, out positiveSidePolygons,
                offsetAtLineForNegativeSide, offsetAtLineForPositiveSide);
        }

        public static Vector2[] SliceAtLine(this List<ShallowPolygonTree> polyTrees, Vector2 lineNormalDirection, double distanceAlongDirection,
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
            var linesPositiveSide = new List<PolygonSegment>();
            var linesNegativeSide = new List<PolygonSegment>();
            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            var anchorpoint = distanceAlongDirection * lineNormalDirection;
            var sortedPoints = new SortedList<double, (Vector2, PolygonSegment, bool)>();
            foreach (var shallowPolygonTree in polyTrees)
                foreach (var polygons in shallowPolygonTree.AllPolygons)
                    foreach (var line in polygons.Lines)
                    {
                        var fromPointAlongDir = line.FromPoint.Coordinates.Dot(lineNormalDirection);
                        var toPointAlongDir = line.ToPoint.Coordinates.Dot(lineNormalDirection);
                        if (fromPointAlongDir == distanceAlongDirection) collinearPoints.Add(line.FromPoint.Coordinates);
                        if (fromPointAlongDir <= distanceAlongDirection && toPointAlongDir <= distanceAlongDirection)
                            linesNegativeSide.Add(line);
                        else if (fromPointAlongDir >= distanceAlongDirection && toPointAlongDir >= distanceAlongDirection)
                            linesPositiveSide.Add(line);
                        else if (MiscFunctions.SegmentLine2DIntersection(line.FromPoint.Coordinates, line.ToPoint.Coordinates,
                            anchorpoint, lineDir, out var intersectionPoint, true))
                        {
                            if (intersectionPoint.IsNull()) // this only happens in polygon line segment is collinear with separation line
                                collinearSegments.Add(line);
                            var distanceAlong = lineDir.Dot(intersectionPoint);
                            sortedPoints.Add(distanceAlong, (intersectionPoint, line, toPointAlongDir > distanceAlongDirection));
                            intersectionLines.Add(line);
                        }
                        else throw new Exception("A line was not left nor right, nor crossing the line. That doesn't make sense.");
                    }
            // we don't really need the distances, so  convert the values to an array
            var pointsOnLineTuples = sortedPoints.Values.ToArray();
            // this is what is returned. although now sure if this is useful in any case
            var intersectionPoints = pointsOnLineTuples.Select(p => p.Item1).ToArray();

            #region patching up negative side polygons
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                // the first segment in the list should be passing from negative side to positive side (item3 should be true)
                var newSegmentTupleA = pointsOnLineTuples[i];
                i++;
                //  the second segment in the list should be passing from the positive to the negative side (item should be false)
                var newSegmentTupleB = pointsOnLineTuples[i];

                // get the coordinates of the points on the line. note that the use may want these shifted
                var negSideFrom = newSegmentTupleA.Item1 + offsetAtLineForNegativeSide * lineNormalDirection;
                var negSideTo = newSegmentTupleB.Item1 + offsetAtLineForNegativeSide * lineNormalDirection;

                if (!newSegmentTupleA.Item3 || newSegmentTupleB.Item3) throw new Exception("the first should always be positive and the second should be negative.");
              // see if there is an existing polygon that ends where this one start
                var existingNegSidePolygon = negativeSidePolygons.FirstOrDefault(p => p.Last().Equals(negSideFrom));
                if (existingNegSidePolygon == null)
                {
                    existingNegSidePolygon = new List<Vector2> { negSideFrom };
                    negativeSidePolygons.Add(existingNegSidePolygon);
                }
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

                var posSideFrom = newSegmentTupleA.Item1 - offsetAtLineForPositiveSide * lineNormalDirection;
                var posSideTo = newSegmentTupleB.Item1 - offsetAtLineForPositiveSide * lineNormalDirection;

                var existingPosSidePolygon = positiveSidePolygons.FirstOrDefault(p => p.Last().Equals(posSideFrom));
                if (existingPosSidePolygon == null)
                {
                    existingPosSidePolygon = new List<Vector2> { posSideFrom };
                    positiveSidePolygons.Add(existingPosSidePolygon);
                }
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
            return intersectionPoints;
        }
    }
}
