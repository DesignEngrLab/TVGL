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
    public static partial class PolygonOperations
    {
        [Obsolete("Use Polygons as this functions constructs them anyway.")]
        /// <summary>
        /// Slices the polygons at the provided line.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="distanceAlongDirection">The distance along direction.</param>
        /// <param name="negativeSidePolygons">The negative side polygons.</param>
        /// <param name="positiveSidePolygons">The positive side polygons.</param>
        /// <param name="offsetAtLineForNegativeSide">The offset at line for negative side.</param>
        /// <param name="offsetAtLineForPositiveSide">The offset at line for positive side.</param>
        /// <returns>Vector2[].</returns>
        public static Vector2[] SliceAtLine(this IEnumerable<IEnumerable<Vector2>> shape, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var polyTrees = CreateShallowPolygonTrees(shape, false, out _);
            return SliceAtLine(polyTrees, lineNormalDirection, distanceAlongDirection, out negativeSidePolygons, out positiveSidePolygons,
                   offsetAtLineForNegativeSide, offsetAtLineForPositiveSide);
        }

        /// <summary>
        /// Slices the polygons at the provided line.
        /// </summary>
        /// <param name="polyTrees">The poly trees.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="distanceAlongDirection">The distance along direction.</param>
        /// <param name="negativeSidePolygons">The negative side polygons.</param>
        /// <param name="positiveSidePolygons">The positive side polygons.</param>
        /// <param name="offsetAtLineForNegativeSide">The offset at line for negative side.</param>
        /// <param name="offsetAtLineForPositiveSide">The offset at line for positive side.</param>
        /// <returns>Vector2[].</returns>
        public static Vector2[] SliceAtLine(this IEnumerable<Polygon> polyTrees, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            negativeSidePolygons = new List<Polygon>();
            positiveSidePolygons = new List<Polygon>();
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

        /// <summary>
        /// Slices the polygon at the provided line.
        /// </summary>
        /// <param name="shallowPolygonTree">The shallow polygon tree.</param>
        /// <param name="lineNormalDirection">The line normal direction.</param>
        /// <param name="distanceAlongDirection">The distance along direction.</param>
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
        public static Vector2[] SliceAtLine(this Polygon shallowPolygonTree, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<Polygon> negativeSidePolygons, out List<Polygon> positiveSidePolygons, double offsetAtLineForNegativeSide = 0.0,
            double offsetAtLineForPositiveSide = 0.0)
        {
            var tolerance = Constants.BaseTolerance * Math.Min(shallowPolygonTree.MaxX - shallowPolygonTree.MinX,
                    shallowPolygonTree.MaxY - shallowPolygonTree.MinY);
            // like 3D slicing, it is too complicated to try and manage collinear points or line segments. it is better to just change the slice
            // distance by some small amount. This is checked and handled in the ShiftLineToAvoidCollinearPoints
            var distances = new List<double>();
            foreach (var polygons in shallowPolygonTree.AllPolygons)
                foreach (var vertex in polygons.Vertices)
                    distances.Add(vertex.Coordinates.Dot(lineNormalDirection));

            var positiveShift = 0.0;
            var negativeShift = 0.0;
            distances.SetPositiveAndNegativeShifts(distanceAlongDirection, tolerance, ref positiveShift, ref negativeShift);
            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersecting lines is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance
            var intersectionLines = new HashSet<PolygonEdge>();
            var lineDir = new Vector2(-lineNormalDirection.Y, lineNormalDirection.X);
            var anchorpoint = distanceAlongDirection * lineNormalDirection;
            var sortedPoints = new SortedList<double, (Vector2, PolygonEdge)>();
            foreach (var polygons in shallowPolygonTree.AllPolygons)
                foreach (var line in polygons.Lines)
                {
                    if (MiscFunctions.SegmentLine2DIntersection(line.FromPoint.Coordinates, line.ToPoint.Coordinates,
                         anchorpoint, lineDir, out var intersectionPoint))
                    {
                        intersectionLines.Add(line);
                        var distanceAlong = lineDir.Dot(intersectionPoint);
                        sortedPoints.Add(distanceAlong, (intersectionPoint, line));
                        //!line.ToPoint.Coordinates.Dot(lineNormalDirection).IsLessThanNonNegligible(distanceAlongDirection));
                    }
                }
            // we don't really need the distances, so  convert the values to an array
            var pointsOnLineTuples = sortedPoints.Values.ToArray();
            // this is what is returned. although now sure if this is useful in any case

            #region patching up negative side polygons

            var negSidePolyCoords = new List<List<Vector2>>();
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
                var existingNegSidePolygon = negSidePolyCoords.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingNegSidePolygon == null)
                {
                    existingNegSidePolygon = new List<Vector2>();
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
            negativeSidePolygons = negSidePolyCoords.Select(loop => new Polygon(loop)).ToList();

            #endregion patching up negative side polygons

            #region patching up positive side polygons

            var posSidePolyCoords = new List<List<Vector2>>();
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
                var existingPosSidePolygon = posSidePolyCoords.FirstOrDefault(p => p.Last().Equals(newSegmentTupleA.Item2.FromPoint.Coordinates));
                if (existingPosSidePolygon == null)
                {
                    existingPosSidePolygon = new List<Vector2>();
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
            positiveSidePolygons = posSidePolyCoords.Select(loop => new Polygon(loop)).ToList();

            #endregion patching up positive side polygons

            return pointsOnLineTuples.Select(p => p.Item1).ToArray();
        }

        private static (double, double) ShiftLineToAvoidCollinearPoints(Polygon shallowPolygonTree,
            Vector2 lineNormalDirection, double distanceAlongDirection, double tolerance = double.NaN)
        {
            if (double.IsNaN(tolerance))
                tolerance = Constants.BaseTolerance * Math.Min(shallowPolygonTree.MaxX - shallowPolygonTree.MinX,
                    shallowPolygonTree.MaxY - shallowPolygonTree.MinY);

            // search through all points to see if any are collinear. If not, keep track of the closest points
            var distances = new List<double>();
            foreach (var polygons in shallowPolygonTree.AllPolygons)
                foreach (var vertex in polygons.Vertices)
                    distances.Add(vertex.Coordinates.Dot(lineNormalDirection));

            var positiveShift = 0.0;
            var negativeShift = 0.0;
            distances.SetPositiveAndNegativeShifts(distanceAlongDirection, tolerance, ref positiveShift, ref negativeShift);
            return (positiveShift, negativeShift);
        }
    }
}