using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL._2D
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
        public static List<Polygon> OnLine(List<Polygon> shape, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, out List<Point> intersectionPoints, double offsetAtLine = 0.0)
        {
            var partialShape = new List<Polygon>();
            intersectionPoints = new List<Point>();
            var shallowPolygonsTrees = ShallowPolygonTree.GetShallowPolygonTrees(shape);
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
                List<Tuple<Point, double>> sortedPoints;
                MiscFunctions.SortAlongDirection(direction2D, allPoints, out sortedPoints);

                //Get the paths for each partial shape and add them together. The paths should not overlap, given
                //that they are from non-overlapping shallow polygon trees.
                var localSortedIntersectionOffsetPoints = new List<Point>();
                var paths = OnLine(shallowPolygonTree, direction2D, distanceAlongDirection, returnFurtherThanSlice, sortedPoints,
                    out localSortedIntersectionOffsetPoints);
                partialShape.AddRange(paths.Select(path => new Polygon(path)));
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
        public static List<List<Point>> OnLine(List<List<Point>> shape, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, out List<Point> intersectionPoints, double offsetAtLine = 0.0)
        {
            var partialShape = new List<List<Point>>();
            intersectionPoints = new List<Point>();
            var shallowPolygonsTrees = ShallowPolygonTree.GetShallowPolygonTrees(shape);
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
                List<Tuple<Point, double>> sortedPoints;
                MiscFunctions.SortAlongDirection(direction2D, allPoints, out sortedPoints);

                //Get the paths for each partial shape and add them together. The paths should not overlap, given
                //that they are from non-overlapping shallow polygon trees.
                var localSortedIntersectionOffsetPoints = new List<Point>();
                var paths = OnLine(shallowPolygonTree, direction2D, distanceAlongDirection, returnFurtherThanSlice, sortedPoints,
                    out localSortedIntersectionOffsetPoints);
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
        /// <param name="intersectionPoints"></param>
        /// <param name="offsetAtLine"></param>
        /// <returns></returns>
        public static List<List<Point>> OnLine(ShallowPolygonTree polyTree, double[] direction2D, double distanceAlongDirection,
            bool returnFurtherThanSlice, IEnumerable<Tuple<Point, double>> sortedPoints, out List<Point> intersectionPoints,
            double offsetAtLine = 0.0)
        {
            if (direction2D.Length != 2) throw new Exception("2D direction must have exactly 2 dimensions");

            /*   First (1), a line hash is used to find all the lines to the left and the intersection lines.
                 Second (2), the intersection point for each of the intersection points is found.
                 Third (3), these intersection points are ordered in the perpendicular direction to the search direction
                 Fourth (4), a smart slicing algorithm is used to cut the full shape into a partial shape, using 
                 the intersection points and lines.*/

            //(1) Find the intersection lines and the lines to the left of the current distance      
            var lineHash = new HashSet<Line>();
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
                        if (lineHash.Contains(line))
                        {
                            lineHash.Remove(line);
                            linesToLeft.Add(line);
                        }
                        else lineHash.Add(line);
                    }
                }
                //else break;
            }
            if (!linesToLeft.Any()) throw new Exception("There must be some lines to the left");

            //(2) Create the intersection points
            var intersectionLines = new HashSet<Line>();
            var intersectionLinesByRef = new Dictionary<int, Line>();
            var intersectionPointsByRef = new Dictionary<int, Point>();
            //Any line that is left in line hash, must be an intersection line.
            var refIndex = 0;
            foreach (var line in lineHash)
            {
                intersectionLines.Add(line);
                var intersectionPoint = MiscFunctions.PointOnPlaneFromIntersectingLine(direction2D, distanceAlongDirection, line);

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

            //(3) Sort the intersection points
            List<Tuple<Point, double>> sortedIntersectionPoints;
            var searchDirectionPerpendicular = new[] { -direction2D[1], direction2D[0] };
            MiscFunctions.SortAlongDirection(searchDirectionPerpendicular, intersectionPointsByRef.Values.ToList(),
                out sortedIntersectionPoints);
            if (sortedIntersectionPoints.Count % 2 != 0)
            {
                throw new Exception("There must be an even number of intersection points");
            }
            //Set each points position in this sorted list
            //The points are paired together, such that sortedIntersectionPoints[0] has a line to 
            //sortedIntersectionPoints[1], [2]<=>[3], [4]<=>[5], and so on.
            for (var i = 0; i < sortedIntersectionPoints.Count; i++)
            {
                var intersectionPoint = sortedIntersectionPoints[i].Item1;
                intersectionPoint.IndexInPath = i;
            }

            //(4) Build the partial 2D shape
            intersectionPoints = new List<Point>();
            var partialShape = new List<List<Point>>();
            while (linesToLeft.Any())
            {
                //Note the line index is the same as the line.FromPoint.IndexInPath
                var endLine = linesToLeft.First();
                linesToLeft.Remove(endLine);

                //Get the corresponding polygon
                var endPoint = endLine.FromPoint;
                var currentPolygon = polyTree.AllPolygons[endPoint.PolygonIndex];
                var endPolygonIndex = currentPolygon.Index;
                var endLineIndex = endLine.IndexInPath;
                var path = new List<Point>() { endPoint };

                var nextLineIndex = currentPolygon.NextLineIndex(endLineIndex);
                //Since the line index can be duplicated between polygons, we also need to check the polygon index
                //It is the same line if its index and polygon index are the same. Then we stop.
                while (nextLineIndex != endLineIndex || currentPolygon.Index != endPolygonIndex)
                {
                    var currentLineIndex = nextLineIndex;
                    var currentLine = currentPolygon.PathLines[currentLineIndex];
                    path.Add(currentLine.FromPoint);
                    if (intersectionLines.Contains(currentLine))
                    {
                        //Add the paired intersection points.
                        //Starting from the nextLine's intersection point, then
                        //going to its pair. From there, we may have switched polygons.
                        var intersectionPoint = intersectionPointsByRef[currentLine.ReferenceIndex];
                        var intersectionPointIndexInVerticalSort = intersectionPoint.IndexInPath;

                        var pairedIntersectionPoint = intersectionPointIndexInVerticalSort % 2 != 0
                            ? sortedIntersectionPoints[intersectionPointIndexInVerticalSort - 1].Item1
                            : sortedIntersectionPoints[intersectionPointIndexInVerticalSort + 1].Item1;

                        //Add four points to the path, both intersection points and their offsets
                        //(1) the current intersection point. 
                        path.Add(intersectionPoint);

                        //(2) the offset point of the current intersection point
                        //Each intersection point corresponds to an intersection offset point. This point
                        //is equal to the current point + the offsetAtLine added along the search direction.
                        var position = returnFurtherThanSlice
                            ? intersectionPoint.Position2D.subtract(direction2D.multiply(offsetAtLine))
                            : intersectionPoint.Position2D.add(direction2D.multiply(offsetAtLine));

                        var intersectionOffsetPoint = new Point(position[0], position[1]);
                        path.Add(intersectionOffsetPoint);
                        intersectionPoints.Add(intersectionOffsetPoint);

                        //(3) the offset point of the next intersection point
                        position = returnFurtherThanSlice
                            ? pairedIntersectionPoint.Position2D.subtract(direction2D.multiply(offsetAtLine))
                            : pairedIntersectionPoint.Position2D.add(direction2D.multiply(offsetAtLine));
                        var pairedIntersectionOffsetPoint = new Point(position[0], position[1]);
                        path.Add(pairedIntersectionOffsetPoint);
                        intersectionPoints.Add(pairedIntersectionOffsetPoint);

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
                partialShape.Add(path);
            }
            return partialShape;
        }
    }
}
