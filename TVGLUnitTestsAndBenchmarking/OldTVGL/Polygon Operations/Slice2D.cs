using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace OldTVGL._2D
{
    public static class Slice2D
    {
        /// <summary>
        /// This function gets cuts shape [List(Polygon)] with a give direction and distance. If returnFurtherThanSlice = false, 
        /// it will return the partial shape before the cutting line, otherwise those beyond the cutting line.
        /// The returned partial shape is properly closed and ordered CCW+, CW-.
        /// OffsetAtLine allows the use to offset the intersection line a given distance in a direction opposite to the 
        /// returned partial shape (i.e., if returnFurtherThanSlice == true, a positive offsetAtLine value moves the  
        /// intersection points before the line).
        /// </summary>
        public static List<PolygonLight> OnLine(List<PolygonLight> shape, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, out List<Point> intersectionPoints,
            double offsetAtLine = 0.0, List<(Point, double)> sortedPoints = null)
        {
            var partialShape = new List<PolygonLight>();
            intersectionPoints = new List<Point>();
            var shallowPolygonsTrees = PolygonOperations.GetShallowPolygonTrees(shape);
            var dirX = direction2D[0];
            var dirY = direction2D[1];
            foreach (var shallowPolygonTree in shallowPolygonsTrees) //There is usually only one, but do them all
            {
                //Set the lines in all the polygons. These are needed for Slice.OnLine()
                foreach (var polygon in shallowPolygonTree.AllPolygons)
                {
                    polygon.SetPathLines();
                }

                //Get the sorted points
                var allPoints = new List<Point>();
                foreach (var path in shallowPolygonTree.AllPaths)
                {
                    allPoints.AddRange(path);
                }

                if (sortedPoints == null)
                {
                    MiscFunctions.SortAlongDirection(dirX, dirY, allPoints, out sortedPoints);
                }

                //Get the paths for each partial shape and add them together. The paths should not overlap, given
                //that they are from non-overlapping shallow polygon trees.
                var localSortedIntersectionOffsetPoints = new List<Point>();
                var paths = OnLine(shallowPolygonTree, direction2D, distanceAlongDirection, returnFurtherThanSlice, sortedPoints,
                    out localSortedIntersectionOffsetPoints, offsetAtLine);
                partialShape.AddRange(paths);
                intersectionPoints.AddRange(localSortedIntersectionOffsetPoints);
            }

            return partialShape;
        }

        /// <summary>
        /// This function gets cuts shape [List(List(Point))] with a give direction and distance. If returnFurtherThanSlice = false, 
        /// it will return the partial shape before the cutting line, otherwise those beyond the cutting line.
        /// The returned partial shape is properly closed and ordered CCW+, CW-.
        /// OffsetAtLine allows the use to offset the intersection line a given distance in a direction opposite to the 
        /// returned partial shape (i.e., if returnFurtherThanSlice == true, a positive offsetAtLine value moves the  
        /// intersection points before the line).
        /// </summary>
        public static List<List<PointLight>> OnLine(List<List<Point>> shape, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, out List<Point> intersectionPoints,
            double offsetAtLine = 0.0, List<(Point, double)> sortedPoints = null)
        {
            var partialShape = new List<List<PointLight>>();
            intersectionPoints = new List<Point>();
            var shallowPolygonsTrees = ShallowPolygonTree.GetShallowPolygonTrees(shape);
            var dirX = direction2D[0];
            var dirY = direction2D[1];
            foreach (var shallowPolygonTree in shallowPolygonsTrees) //There is usually only one, but do them all
            {
                //Set the lines in all the polygons. These are needed for Slice.OnLine()
                foreach (var polygon in shallowPolygonTree.AllPolygons)
                {
                    polygon.SetPathLines();
                }

                //Get the sorted points
                var allPoints = new List<Point>();
                foreach (var path in shallowPolygonTree.AllPaths)
                {
                    allPoints.AddRange(path);
                }

                if (sortedPoints == null)
                {
                    MiscFunctions.SortAlongDirection(dirX, dirY, allPoints, out sortedPoints);
                }

                //Get the paths for each partial shape and add them together. The paths should not overlap, given
                //that they are from non-overlapping shallow polygon trees.
                var paths = OnLine(shallowPolygonTree, direction2D, distanceAlongDirection, returnFurtherThanSlice, sortedPoints,
                    out var localSortedIntersectionOffsetPoints, offsetAtLine).Select(p => p.Path).ToList();
                partialShape.AddRange(paths);
                intersectionPoints.AddRange(localSortedIntersectionOffsetPoints);
            }

            return partialShape;
        }

        /// <summary>
        /// This function gets cuts ShallowPoygonTree with a give direction and distance. If returnFurtherThanSlice = false, 
        /// it will return the partial shape [List(List(Point))] before the cutting line, otherwise those beyond the cutting line.
        /// The returned partial shape is properly closed and ordered CCW+, CW-.
        /// OffsetAtLine allows the use to offset the intersection line a given distance in a direction opposite to the 
        /// returned partial shape (i.e., if returnFurtherThanSlice == true, a positive offsetAtLine value moves the intersection 
        /// intersection points before the line). 
        /// </summary>
        /// <param name="polyTree"></param>
        /// <param name="direction2D"></param>
        /// <param name="distanceAlongDirection"></param>
        /// <param name="returnFurtherThanSlice"></param>
        /// <param name="sortedPoints"></param>
        /// <param name="sortedIntersectionPoints"></param>
        /// <param name="offsetAtLine"></param>
        /// <returns></returns>
        public static List<PolygonLight> OnLine(ShallowPolygonTree polyTree, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, IEnumerable<(Point, double)> sortedPoints, out List<Point> sortedIntersectionPoints,
            double offsetAtLine = 0.0)
        {
            if (direction2D.Length != 2) throw new Exception("2D direction must have exactly 2 dimensions");

            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersection points is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using 
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance      
            var intersectionLines = new HashSet<Line>();
            var linesToLeft = new HashSet<Line>();
            foreach (var pair in sortedPoints)
            {
                var distanceAlong = pair.Item2;
                var point = pair.Item1;
                //If the search direction is forward, then the partial shape is defined with any lines
                //prior to the given distance. If reverse, then it is with lines further than the current distance.
                var furtherThanSlice = distanceAlong > distanceAlongDirection;
                if (returnFurtherThanSlice == furtherThanSlice)
                {
                    foreach (var line in point.Lines)
                    {
                        if (intersectionLines.Contains(line))
                        {
                            intersectionLines.Remove(line);
                            linesToLeft.Add(line);
                        }
                        else intersectionLines.Add(line);
                    }
                }
                //else break;
            }
            if (!linesToLeft.Any()) throw new Exception("There must be some lines to the left");

            //(2-3) Create and sort the intersection points
            sortedIntersectionPoints = GetSortedIntersectionPoints(intersectionLines, direction2D, distanceAlongDirection,
                out var intersectionLinesByRef, out var intersectionPointsByRef);

            //(4) Build the partial 2D shape
            var partialShape = new List<PolygonLight>();
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
                var path = new List<PointLight> { endPoint.Light };

                var nextLineIndex = currentPolygon.NextLineIndex(endLineIndex);
                //Since the line index can be duplicated between polygons, we also need to check the polygon index
                //It is the same line if its index and polygon index are the same. Then we stop.
                while (nextLineIndex != endLineIndex || currentPolygon.Index != endPolygonIndex)
                {
                    var currentLineIndex = nextLineIndex;
                    var currentLine = currentPolygon.PathLines[currentLineIndex];
                    path.Add(currentLine.FromPoint.Light);
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
                        path.Add(intersectionPoint.Light);

                        //(2) the offset point of the current intersection point
                        //Each intersection point corresponds to an intersection offset point. This point
                        //is equal to the current point + the offsetAtLine added along the search direction.
                        var position = returnFurtherThanSlice
                            ? intersectionPoint - (direction2D.multiply(offsetAtLine))
                            : intersectionPoint + (direction2D.multiply(offsetAtLine));

                        var intersectionOffsetPoint = new Point(position[0], position[1]);
                        path.Add(intersectionOffsetPoint.Light);
                        sortedIntersectionPoints.Add(intersectionOffsetPoint);

                        //(3) the offset point of the next intersection point
                        position = returnFurtherThanSlice
                            ? pairedIntersectionPoint - (direction2D.multiply(offsetAtLine))
                            : pairedIntersectionPoint + (direction2D.multiply(offsetAtLine));
                        var pairedIntersectionOffsetPoint = new Point(position[0], position[1]);
                        path.Add(pairedIntersectionOffsetPoint.Light);
                        sortedIntersectionPoints.Add(pairedIntersectionOffsetPoint);

                        //(4) the next intersection point. 
                        path.Add(pairedIntersectionPoint.Light);

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
                partialShape.Add(new PolygonLight(path));
            }
            return partialShape;
        }

        private static List<Point> GetSortedIntersectionPoints(HashSet<Line> intersectionLines, double[] direction2D,
            double distance, out Dictionary<int, Line> intersectionLinesByRef, out Dictionary<int, Point> intersectionPointsByRef)
        {
            intersectionLinesByRef = new Dictionary<int, Line>(intersectionLines.Count);
            intersectionPointsByRef = new Dictionary<int, Point>(intersectionLines.Count);
            //Any line that is left in line hash, must be an intersection line.
            var refIndex = 0;
            foreach (var line in intersectionLines)
            {
                var intersectionPoint = MiscFunctions.PointOnPlaneFromIntersectingLine(direction2D, distance, line);
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
                    out List<Point> sortedIntersectionPoints);
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
            IEnumerable<PolygonLight> shape, double lowerBound, double distanceBetweenLines, int numLines)
        {
            //var shapeForDebugging = new List<List<PointLight>>();
            //foreach (var polygon in shape)
            //{
            //    shapeForDebugging.Add(polygon.Path);//PolygonOperations.SimplifyFuzzy(
            //}


            //Set the lines in all the polygons. These are needed for Slice.OnLine()
            //Also, get the sorted points
            var polygons = shape.Select(p => new Polygon(p.Path.Select(point => new Point(point)), true));
            var allPoints = polygons.SelectMany(poly => poly.Path);
            var sortedPoints = allPoints.OrderBy(p => p.X).ToList();

            var i = 0;
            var distanceAlongDirection =
                (Math.Ceiling((sortedPoints[0].X - lowerBound) / distanceBetweenLines) * distanceBetweenLines) +
                lowerBound;
            var numIntersectionLines = 0;
            var intersectionLines = new HashSet<Line>();
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
                foreach (var line in point.Lines)
                {
                    if (intersectionLines.Contains(line))
                    {
                        intersectionLines.Remove(line);
                        numIntersectionLines--;
                    }
                    else
                    {
                        intersectionLines.Add(line);
                        numIntersectionLines++;
                    }
                }
            }

            //Presenter.ShowAndHang(intersectionPoints);
            return intersectionPoints;
        }

        public static Dictionary<double, List<double>> IntersectionPointsAtUniformDistancesAlongY(
            IEnumerable<PolygonLight> shape, double lowerBound, double distanceBetweenLines, int numLines)
        {
            //Set the lines in all the polygons. These are needed for Slice.OnLine()
            //Also, get the sorted points
            var polygons = shape.Select(p => new Polygon(p.Path.Select(point => new Point(point)), true));
            var allPoints = polygons.SelectMany(poly => poly.Path);
            var sortedPoints = allPoints.OrderBy(p => p.Y).ToList();

            var i = 0;
            var distanceAlongDirection =
                (Math.Ceiling((sortedPoints[1].Y - lowerBound) / distanceBetweenLines) * distanceBetweenLines) +
                lowerBound;
            var numIntersectionLines = 0;
            var intersectionLines = new HashSet<Line>();
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
                foreach (var line in point.Lines)
                {
                    if (intersectionLines.Contains(line))
                    {
                        intersectionLines.Remove(line);
                        numIntersectionLines--;
                    }
                    else
                    {
                        intersectionLines.Add(line);
                        numIntersectionLines++;
                    }
                }
            }
            //Presenter.ShowAndHang(intersectionPoints);
            return intersectionPoints;
        }

        private static double[] GetYIntersectionsSortedAlongY(HashSet<Line> intersectionLines, double x)
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

        private static double[] GetXIntersectionsSortedAlongX(HashSet<Line> intersectionLines, double y)
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
