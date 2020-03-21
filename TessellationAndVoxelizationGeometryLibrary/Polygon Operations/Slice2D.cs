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
        public static List<Vector2> SliceAtLine(this List<List<Vector2>> shape, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLine = 0.0)
        {
            var polyTrees = GetShallowPolygonTrees(shape);
            return SliceAtLine(polyTrees, lineNormalDirection, distanceAlongDirection, out negativeSidePolygons, out positiveSidePolygons, offsetAtLine);
        }

        public static List<Vector2> SliceAtLine(this List<ShallowPolygonTree> polyTrees, Vector2 lineNormalDirection, double distanceAlongDirection,
            out List<List<Vector2>> negativeSidePolygons, out List<List<Vector2>> positiveSidePolygons, double offsetAtLine)
        {
            negativeSidePolygons = new List<List<Vector2>>();
            positiveSidePolygons = new List<List<Vector2>>();
            var intersectionPoints = new List<Vector2>();
            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersecting lines is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using 
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance      
            var intersectionLines = new HashSet<PolygonSegment>();
            var collinearSegments = new HashSet<PolygonSegment>();
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
                        if (fromPointAlongDir < distanceAlongDirection && toPointAlongDir < distanceAlongDirection)
                            linesNegativeSide.Add(line);
                        else if (fromPointAlongDir > distanceAlongDirection && toPointAlongDir > distanceAlongDirection)
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
            var enumerator = sortedPoints.GetEnumerator();
            for (int i = 0; i < sortedPoints.Count; i+=2)
            {
                enumerator.MoveNext();
                var fromPoint = enumerator.Current;
                enumerator.MoveNext();
                var toPoint = enumerator.Current;

            }
            foreach (var pair in sortedPoints)
            {
                var point = pair.Key;
                var distanceAlong = pair.Item2;
                //If the search direction is forward, then the partial shape is defined with any lines
                //prior to the given distance. If reverse, then it is with lines further than the current distance.
                var furtherThanSlice = distanceAlong > distanceAlongDirection;
                if (returnFurtherThanSlice == furtherThanSlice)
                {
                    if (intersectionLines.Contains(point.StartLine))
                    {
                        intersectionLines.Remove(point.StartLine);
                        linesToLeft.Add(point.StartLine);
                    }
                    else intersectionLines.Add(point.StartLine);
                    if (intersectionLines.Contains(point.EndLine))
                    {
                        intersectionLines.Remove(point.EndLine);
                        linesToLeft.Add(point.EndLine);
                    }
                    else intersectionLines.Add(point.EndLine);
                }
                //else break;
            }
            if (!linesToLeft.Any()) throw new Exception("There must be some lines to the left");

            //(2-3) Create and sort the intersection points
            sortedIntersectionPoints = GetSortedIntersectionPoints(intersectionLines, lineNormalDirection, distanceAlongDirection,
                out var intersectionLinesByRef, out var intersectionPointsByRef);

            //(4) Build the partial 2D shape
            var partialShape = new List<List<Vector2>>();
            while (linesToLeft.Any())
            {
                //Note the line index is the same as the line.FromPoint.IndexInPath
                var endLine = linesToLeft.First();
                linesToLeft.Remove(endLine);

                //Get the corresponding polygon
                //Use copies of points, since creating a polygon will erase the 
                //point's line references
                var endPoint = endLine.FromPoint;
                var currentPolygon = polyTree.AllPolygons[endPoint.PolygonIndex];
                var endPolygonIndex = currentPolygon.Index;
                var endLineIndex = endLine.IndexInPath;
                var path = new List<Vector2> { endPoint };

                var nextLineIndex = currentPolygon.NextLineIndex(endLineIndex);
                //Since the line index can be duplicated between polygons, we also need to check the polygon index
                //It is the same line if its index and polygon index are the same. Then we stop.
                while (nextLineIndex != endLineIndex || currentPolygon.Index != endPolygonIndex)
                {
                    var currentLineIndex = nextLineIndex;
                    var currentLine = currentPolygon.Lines[currentLineIndex];
                    path.Add(currentLine.FromPoint);
                    if (intersectionLines.Contains(currentLine))
                    {
                        //Add the paired intersection points.
                        //Starting from the nextLine's intersection point, then
                        //going to its pair. From there, we may have switched polygons.
                        var intersectionPoint = intersectionPointsByRef[currentLine.ReferenceIndex];
                        var intersectionPointIndexInVerticalSort = intersectionPoint.IndexInPath;

                        var pairedIntersectionPoint = intersectionPointIndexInVerticalSort % 2 != 0
                            ? sortedIntersectionPoints[intersectionPointIndexInVerticalSort - 1]
                            : sortedIntersectionPoints[intersectionPointIndexInVerticalSort + 1];

                        //Add four points to the path, both intersection points and their offsets
                        //(1) the current intersection point. 
                        path.Add(intersectionPoint);

                        //(2) the offset point of the current intersection point
                        //Each intersection point corresponds to an intersection offset point. This point
                        //is equal to the current point + the offsetAtLine added along the search direction.
                        var position = returnFurtherThanSlice
                            ? intersectionPoint - (lineNormalDirection * offsetAtLine)
                            : intersectionPoint + (lineNormalDirection * offsetAtLine);

                        var intersectionOffsetPoint = new Vector2(position[0], position[1]);
                        path.Add(intersectionOffsetPoint);
                        sortedIntersectionPoints.Add(intersectionOffsetPoint);

                        //(3) the offset point of the next intersection point
                        position = returnFurtherThanSlice
                            ? pairedIntersectionPoint - (lineNormalDirection * offsetAtLine)
                            : pairedIntersectionPoint + (lineNormalDirection * offsetAtLine);
                        var pairedIntersectionOffsetPoint = new Vector2(position[0], position[1]);
                        path.Add(pairedIntersectionOffsetPoint);
                        sortedIntersectionPoints.Add(pairedIntersectionOffsetPoint);

                        //(4) the next intersection point. 
                        path.Add(pairedIntersectionPoint);

                        //Update the current polygons, since it may have changed, and then update the next line index
                        var nextLine = intersectionLinesByRef[pairedIntersectionPoint.ReferenceIndex];
                        currentPolygon = polyTree.AllPolygons[nextLine.FromPoint.PolygonIndex];
                        nextLineIndex = currentPolygon.NextLineIndex(nextLine.IndexInPath);
                    }
                    else
                    {
                        linesToLeft.Remove(currentLine);
                        nextLineIndex = currentPolygon.NextLineIndex(nextLineIndex);
                    }
                }

                //Because of the intelligent slicing operation, the paths should be ordered correctly CCW+ or CW-
                //A negative hole could be an entire path, as long as all its lines where to the left of the sweep.
                partialShape.Add(new List<Vector2>(path));
            }
            return partialShape;
        }

        private static List<Vector2> GetSortedIntersectionPoints(HashSet<PolygonSegment> intersectionLines, Vector2 direction2D,
            double distance, out Dictionary<int, PolygonSegment> intersectionLinesByRef, out Dictionary<int, Vector2> intersectionPointsByRef)
        {
            intersectionLinesByRef = new Dictionary<int, PolygonSegment>(intersectionLines.Count);
            intersectionPointsByRef = new Dictionary<int, Vector2>(intersectionLines.Count);
            //Any line that is left in line hash, must be an intersection line.
            var refIndex = 0;
            foreach (var line in intersectionLines)
            {
                var intersectionPoint = MiscFunctions.Vector2OnPlaneFromIntersectingLine(direction2D, distance, line);
                line.ReferenceIndex = refIndex;
                intersectionPoint.ReferenceIndex = refIndex;
                intersectionLinesByRef.Add(refIndex, line);
                intersectionPointsByRef.Add(refIndex, intersectionPoint);
                refIndex++;
            }
            if (intersectionLines.Count == 0 || intersectionLines.Count % 2 != 0)
            {
                throw new Exception("There must be a non-zero, even number of intersection lines");
            }
            //var searchDirectionPerpendicular = new[] {-direction2D[1], direction2D[0]};
            MiscFunctions.SortAlongDirection(-direction2D[1], direction2D[0], intersectionPointsByRef.Values.ToList(),
                    out List<Vector2> sortedIntersectionPoints);
            if (sortedIntersectionPoints.Count % 2 != 0)
            {
                throw new Exception("There must be an even number of intersection points");
            }
            //Set each points position in this sorted list
            //The points are paired together, such that sortedIntersectionPoints[0] has a line to 
            //sortedIntersectionPoints[1], [2]<=>[3], [4]<=>[5], and so on.
            for (var i = 0; i < sortedIntersectionPoints.Count; i++)
            {
                sortedIntersectionPoints[i].IndexInPath = i;
            }
            return sortedIntersectionPoints;
        }

        public static Dictionary<double, List<double>> IntersectionPointsAtUniformDistancesAlongX(
            IEnumerable<List<Vector2>> shape, double lowerBound, double distanceBetweenLines, int numLines)
        {
            //var shapeForDebugging = new List<List<Vector2>>();
            //foreach (var polygon in shape)
            //{
            //    shapeForDebugging.Add(polygon.Path);//PolygonOperations.SimplifyFuzzy(
            //}


            //Set the lines in all the polygons. These are needed for Slice.OnLine()
            //Also, get the sorted points
            var polygons = shape.Select(p => new Polygon(p.Path, true));
            var allPoints = polygons.SelectMany(poly => poly.Path);
            var sortedPoints = allPoints.OrderBy(p => p.X).ToList();

            var i = 0;
            var distanceAlongDirection =
                (Math.Ceiling((sortedPoints[0].X - lowerBound) / distanceBetweenLines) * distanceBetweenLines) +
                lowerBound;
            var numIntersectionLines = 0;
            var intersectionLines = new HashSet<PolygonSegment>();
            var intersectionPoints = new Dictionary<double, List<double>>(numLines);
            foreach (var point in sortedPoints)
            {
                var pointDistance = point.X;

                while (pointDistance > distanceAlongDirection)
                {
                    if (numIntersectionLines > 0)
                    {
                        intersectionPoints.Add(distanceAlongDirection,
                            GetYIntersectionsSortedAlongY(intersectionLines, distanceAlongDirection).ToList());
                        //Presenter.ShowAndHang(shapeForDebugging);
                    }

                    //Update the distance along
                    i++;
                    distanceAlongDirection += distanceBetweenLines;
                }

                //Update the intersection lines
                if (intersectionLines.Contains(point.StartLine))
                {
                    intersectionLines.Remove(point.StartLine);
                    numIntersectionLines--;
                }
                else
                {
                    intersectionLines.Add(point.StartLine);
                    numIntersectionLines++;
                }
                if (intersectionLines.Contains(point.EndLine))
                {
                    intersectionLines.Remove(point.EndLine);
                    numIntersectionLines--;
                }
                else
                {
                    intersectionLines.Add(point.EndLine);
                    numIntersectionLines++;
                }
            }

            //Presenter.ShowAndHang(intersectionPoints);
            return intersectionPoints;
        }

        public static Dictionary<double, List<double>> IntersectionPointsAtUniformDistancesAlongY(
            IEnumerable<List<Vector2>> shape, double lowerBound, double distanceBetweenLines, int numLines)
        {
            //Set the lines in all the polygons. These are needed for Slice.OnLine()
            //Also, get the sorted points
            var polygons = shape.Select(p => new Polygon(p.Path, true));
            var allPoints = polygons.SelectMany(poly => poly.Path);
            var sortedPoints = allPoints.OrderBy(p => p.Y).ToList();

            var i = 0;
            var distanceAlongDirection =
                (Math.Ceiling((sortedPoints[1].Y - lowerBound) / distanceBetweenLines) * distanceBetweenLines) +
                lowerBound;
            var numIntersectionLines = 0;
            var intersectionLines = new HashSet<PolygonSegment>();
            var intersectionPoints = new Dictionary<double, List<double>>(numLines);
            foreach (var point in sortedPoints)
            {
                var pointDistance = point.Y;

                while (pointDistance > distanceAlongDirection)
                {
                    if (numIntersectionLines > 0)
                    {
                        intersectionPoints.Add(distanceAlongDirection,
                            GetXIntersectionsSortedAlongX(intersectionLines, distanceAlongDirection).ToList());
                        //Presenter.ShowAndHang(shapeForDebugging);
                    }

                    //Update the distance along
                    i++;
                    distanceAlongDirection += distanceBetweenLines;
                }

                //Update the intersection lines
                if (intersectionLines.Contains(point.StartLine))
                {
                    intersectionLines.Remove(point.StartLine);
                    numIntersectionLines--;
                }
                else
                {
                    intersectionLines.Add(point.StartLine);
                    numIntersectionLines++;
                }
                if (intersectionLines.Contains(point.EndLine))
                {
                    intersectionLines.Remove(point.EndLine);
                    numIntersectionLines--;
                }
                else
                {
                    intersectionLines.Add(point.EndLine);
                    numIntersectionLines++;
                }
            }
            //Presenter.ShowAndHang(intersectionPoints);
            return intersectionPoints;
        }

        private static double[] GetYIntersectionsSortedAlongY(HashSet<PolygonSegment> intersectionLines, double x)
        {
            var n = intersectionLines.Count;
            var intersectionPoints = new List<double>(n);
            var refIndex = 0;
            foreach (var line in intersectionLines)
            {
                intersectionPoints.Add(line.YGivenX(x));
                refIndex++;
            }
            return intersectionPoints.OrderBy(p => p).ToArray();
        }

        private static double[] GetXIntersectionsSortedAlongX(HashSet<PolygonSegment> intersectionLines, double y)
        {
            var n = intersectionLines.Count;
            var intersectionPoints = new List<double>(n);
            var refIndex = 0;
            foreach (var line in intersectionLines)
            {
                intersectionPoints.Add(line.XGivenY(y));
                refIndex++;
            }
            return intersectionPoints.OrderBy(p => p).ToArray();
        }
    }
}
