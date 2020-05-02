using ClipperLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Perimeter(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return paths.Sum(path => path.Perimeter());
        }
        /// <summary>
        /// Gets the perimeter for a 2D set of points.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Perimeter(this IEnumerable<Vector2> polygon)
        {
            var perimeter = 0.0;
            var firstpass = true;
            var firstPoint = Vector2.Null;
            var prevPoint = Vector2.Null;
            foreach (var currentPt in polygon)
            {
                if (firstpass)
                {
                    firstpass = false;
                    firstPoint = prevPoint = currentPt;
                }
                else
                {
                    perimeter += Vector2.Distance(prevPoint, currentPt);
                    prevPoint = currentPt;
                }
            }
            perimeter += Vector2.Distance(prevPoint, firstPoint);

            return perimeter;
        }


        /// <summary>
        ///     Calculate the area of any non-intersecting polygon.
        /// </summary>
        public static double Area(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return paths.Sum(path => path.Area());
        }


        /// <summary>
        /// Gets the area for a 2D set of points defining a polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static double Area(this IEnumerable<Vector2> polygon)
        {
            var area = 0.0;
            var enumerator = polygon.GetEnumerator();
            enumerator.MoveNext();
            var basePoint = enumerator.Current;
            enumerator.MoveNext();
            var prevPoint = enumerator.Current;
            foreach (var currentPt in polygon)
            {
                area += (prevPoint - basePoint).Cross(currentPt - basePoint);
                prevPoint = currentPt;
            }
            return area / 2;
        }

        /// <summary>
        /// Gets the area for a 2D set of points defining a polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns>System.Double.</returns>
        public static bool IsPositive(this IEnumerable<Vector2> polygon)
        {
            return polygon.Area() > 0;
        }

        public static IEnumerable<double> ConvertTo1DDoublesCollection(this IEnumerable<Vector2> coordinates)
        {
            return coordinates.SelectMany(p => p.Position);
        }

        public static IEnumerable<Vector2> ConvertToVector2s(this IEnumerable<double> coordinates)
        {
            var enumerator = coordinates.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var x = enumerator.Current;
                if (!enumerator.MoveNext())
                    throw new ArgumentException("An odd number of coordinates have been provided to " +
                   "convert the 1D array of double to an array of vectors.");
                var y = enumerator.Current;
                yield return new Vector2(x, y);
            }
        }

        /// <summary>
        /// Gets the Shallow Polygon Trees for a given set of paths. 
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static List<ShallowPolygonTree> GetShallowPolygonTrees(this IEnumerable<IEnumerable<Vector2>> paths)
        {
            return ShallowPolygonTree.GetShallowPolygonTrees(paths);
        }
        public static List<ShallowPolygonTree> GetShallowPolygonTrees(this IEnumerable<Polygon> paths)
        {
            return ShallowPolygonTree.GetShallowPolygonTrees(paths);
        }


        #region Line Intersections with Polygon

        public static List<Vector2> AllPolygonIntersectionPointsAlongLine(IEnumerable<List<Vector2>> polygons, Vector2 lineReference, double lineDirection,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongLine(polygons.Select(p => new Polygon(p)), lineReference,
                lineDirection, numSteps, stepSize, out firstIntersectingIndex);
        }
        public static List<Vector2> AllPolygonIntersectionPointsAlongLine(IEnumerable<Polygon> polygons, Vector2 lineReference, double lineDirection,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            throw new NotImplementedException();
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongX(IEnumerable<List<Vector2>> polygons, double startingXValue,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongX(polygons.Select(p => new Polygon(p)), startingXValue,
                numSteps, stepSize, out firstIntersectingIndex);
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongX(IEnumerable<Polygon> polygons, double startingXValue,
              int numSteps, double stepSize, out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Points).OrderBy(p => p.X).ToList();
            var currentLines = new HashSet<PolygonSegment>();
            var nextDistance = sortedPoints.First().X;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingXValue) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var x = startingXValue + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.X <= x)
                {
                    if (x.IsPracticallySame(thisPoint.X)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    x += Math.Min(stepSize, sortedPoints[pIndex + 1].X) / 10.0;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.YGivenX(x, out _);
                intersections.Add(intersects.OrderBy(y => y).ToArray());
            }
            return intersections;
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<List<Vector2>> polygons, double startingYValue, int numSteps, double stepSize,
              out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongY(polygons.Select(p => new Polygon(p)), startingYValue,
                numSteps, stepSize, out firstIntersectingIndex);
        }
        public static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<Polygon> polygons, double startingYValue, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Points).OrderBy(p => p.Y).ToList();
            var currentLines = new HashSet<PolygonSegment>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - startingYValue) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = startingYValue + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.Y <= y)
                {
                    if (y.IsPracticallySame(thisPoint.Y)) needToOffset = true;
                    if (currentLines.Contains(thisPoint.StartLine)) currentLines.Remove(thisPoint.StartLine);
                    else currentLines.Add(thisPoint.StartLine);
                    if (currentLines.Contains(thisPoint.EndLine)) currentLines.Remove(thisPoint.EndLine);
                    else currentLines.Add(thisPoint.EndLine);
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    y += Math.Min(stepSize, sortedPoints[pIndex + 1].Y) / 10.0;

                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.XGivenY(y, out _);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }
        #endregion

        /// <summary>
        /// Gets whether a polygon is rectangular by using the minimum bounding rectangle.
        /// The rectangle my be in any orientation and contain any number of points greater than three.
        /// Confidence Percentage can be decreased to identify polygons that are close to rectangular.
        /// </summary>
        /// <param name="polygon"></param>
        /// <param name="dimensions"></param>
        /// <param name="confidencePercentage"></param>
        /// <returns></returns>
        public static bool IsRectangular(this IEnumerable<Vector2> polygon, out Vector2 dimensions, double confidencePercentage = Constants.HighConfidence)
        {
            if (confidencePercentage > 1.0 || Math.Sign(confidencePercentage) < 0)
                throw new Exception("Confidence percentage must be between 0 and 1");
            var tolerancePercentage = 1.0 - confidencePercentage;
            //For it to be rectangular, Area = l*w && Perimeter = 2*l + 2*w.
            //This can only gaurantee that it is not a rectangle if false.
            //If true, then check the polygon area vs. its minBoundingRectangle area. 
            //The area / perimeter check is not strictly necessary, but can provide some speed-up
            //For obviously not rectangular pieces
            var perimeter = polygon.Perimeter();
            var area = polygon.Area();
            var sqrRootTerm = Math.Sqrt(perimeter * perimeter - 16 * area);
            var length = 0.25 * (perimeter + sqrRootTerm);
            var width = 0.25 * (perimeter - sqrRootTerm);
            dimensions = new Vector2(length, width);
            var areaCheck = length * width;
            var perimeterCheck = 2 * length + 2 * width;
            if (!area.IsPracticallySame(areaCheck, area * tolerancePercentage) &&
                !perimeter.IsPracticallySame(perimeterCheck, perimeter * tolerancePercentage))
            {
                return false;
            }

            var minBoundingRectangle = MinimumEnclosure.BoundingRectangle(polygon);
            return area.IsPracticallySame(minBoundingRectangle.Area, area * tolerancePercentage);
        }


        /// <summary>Determines whether the specified polygon is circular.</summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="minCircle">The minimum circle.</param>
        /// <param name="confidencePercentage">The confidence percentage.</param>
        /// <returns>
        ///   <c>true</c> if the specified polygon is circular; otherwise, <c>false</c>.</returns>
        public static bool IsCircular(this Polygon polygon, out BoundingCircle minCircle, double confidencePercentage = Constants.HighConfidence)
        {
            var tolerancePercentage = 1.0 - confidencePercentage;
            minCircle = MinimumEnclosure.MinimumCircle(polygon.Path.ToList());

            //Check if areas are close to the same
            var polygonArea = Math.Abs(polygon.Area);
            return polygonArea.IsPracticallySame(minCircle.Area, polygonArea * tolerancePercentage);
        }

        /// <summary>
        ///     Determines if a point is inside a polygon, using ray casting. This is slower than the method
        ///     below, but does allow determination of whether a point is on the boundary.
        /// </summary>
        internal static bool IsPointInsidePolygon(this Polygon polygon, Vector2 pointInQuestion, out PolygonSegment closestLineAbove,
            out PolygonSegment closestLineBelow, out bool onBoundary, bool onBoundaryIsInside = true)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
                                         //This function has three layers of checks. 
                                         //(1) Check if the point is inside the axis aligned bounding box. If it is not, then return false.
                                         //(2) Check if the point is == to a polygon point, return onBoundaryIsInside.
                                         //(3) Use line-sweeping / ray casting to determine if the polygon contains the point.
            closestLineAbove = null;
            closestLineBelow = null;
            onBoundary = false;
            //1) Check if center point is within bounding box of each polygon
            if (qX < polygon.MinX || qY < polygon.MinY ||
                qX > polygon.MaxX || qY > polygon.MaxY)
                return false;
            //2) If the point in question is == a point in points, then it is inside the polygon
            if (polygon.Path.Any(point => point.IsPracticallySame(pointInQuestion)))
            {
                onBoundary = true;
                return onBoundaryIsInside;
            }
            var numberAbove = 0;
            var numberBelow = 0;
            var minDistAbove = double.PositiveInfinity;
            var minDistBelow = double.PositiveInfinity;
            foreach (var line in polygon.Lines)
            {
                if ((line.FromPoint.X < qX) == (line.ToPoint.X < qX))
                    // if the X values are both on the same side, then ignore it. We are looking for
                    // lines that 'straddle' the x-values. Then we want to know if the lines' y values
                    // are above or below
                    continue;
                var lineYValue = line.YGivenX(qX, out _); //this out parameter is the same condition
                                                          //as 5 lines earlier, but that check is kept for efficiency
                var yDistance = lineYValue - qY;
                if (yDistance > 0)
                {
                    numberAbove++;
                    if (minDistAbove > yDistance)
                    {
                        minDistAbove = yDistance;
                        closestLineAbove = line;
                    }
                }
                else if (yDistance < 0)
                {
                    yDistance = -yDistance;
                    numberBelow++;
                    if (minDistBelow > yDistance)
                    {
                        minDistBelow = yDistance;
                        closestLineBelow = line;
                    }
                }
                else //else, the point is on a line in the polygon
                {
                    closestLineAbove = closestLineBelow = line;
                    onBoundary = true;
                    return true;
                }
            }
            if (numberBelow != numberAbove)
            {
                Trace.WriteLine("In IsPointInsidePolygon, the number of points above is not equal to the number below");
                numberAbove = numberBelow = Math.Max(numberBelow, numberAbove);
            }
            return numberAbove % 2 != 0;
        }


        /// <summary>
        ///     Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        ///     And the polygon is not self-intersecting. This is a newer, much faster implementation than prior
        ///     the prior method, making use of W. Randolph Franklin's compact algorithm
        ///     https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html
        ///     Major Assumptions: 
        ///     1) The polygon can be convex
        ///     2) The direction of the polygon does not matter  
        /// 
        ///     Updated by Brandon Massoni: 8.11.2017
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pointInQuestion"></param>
        public static bool IsPointInsidePolygon(this List<Vector2> path, Vector2 pointInQuestion, bool onBoundaryIsInside = false)
        {
            var qX = pointInQuestion.X;  // for conciseness and the smallest bit of additional speed,
            var qY = pointInQuestion.Y;  // we declare these local variables.
                                         //Check if the point is the same as any of the polygon's points
            var polygonIsLeftOfPoint = false;
            var polygonIsRightOfPoint = false;
            var polygonIsAbovePoint = false;
            var polygonIsBelowPoint = false;
            foreach (var point in path)
            {
                if (point == pointInQuestion)
                    return onBoundaryIsInside;
                if (point.X > qX) polygonIsLeftOfPoint = true;
                else if (point.X < qX) polygonIsRightOfPoint = true;
                if (point.Y > qY) polygonIsAbovePoint = true;
                else if (point.Y < qY) polygonIsBelowPoint = true;
            }
            if (!(polygonIsAbovePoint && polygonIsBelowPoint && polygonIsLeftOfPoint && polygonIsRightOfPoint))
                // this is like the AABB check. 
                return false;

            //2) Next, see how many lines are to the left of the point, using a fixed y value.
            //This compact, effecient 7 lines of code is from W. Randolph Franklin
            //<https://wrf.ecse.rpi.edu//Research/Short_Notes/pnpoly.html>
            var inside = false;
            for (int i = 0, j = path.Count - 1; i < path.Count; j = i++)
            {
                if ((path[i].Y > pointInQuestion.Y) != (path[j].Y > pointInQuestion.Y) &&
                     pointInQuestion.X < (path[j].X - path[i].X) * (pointInQuestion.Y - path[i].Y) / (path[j].Y - path[i].Y) + path[i].X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// This algorithm returns whether two polygons intersect. It can be used to triangle/triangle intersections,
        /// or any abitrary set of polygons. If two polygons are touching, they are not considered to be intersecting.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <returns></returns>
        //To get the area of intersection, use the Sutherland–Hodgman algorithm for polygon clipping
        // https://en.wikipedia.org/wiki/Sutherland%E2%80%93Hodgman_algorithm
        public static bool IsPolygonIntersectingPolygon(this Polygon subject, Polygon clip)
        {
            //Get the axis aligned bounding box of the path. This is super fast.
            //If the point is inside the bounding box, continue to check with more detailed methods, 
            //Else, return false.
            if (subject.MinX > clip.MaxX ||
                subject.MaxX < clip.MinX ||
                subject.MinY > clip.MaxY ||
                subject.MaxY < clip.MinY) return false;

            //Check if either polygon is fully encompassed by the other
            if (clip.Path.Any(p => IsPointInsidePolygon(subject, p, out _, out _, out _))) return true;
            if (subject.Path.Any(p => IsPointInsidePolygon(clip, p, out _, out _, out _))) return true;

            //Else, any remaining intersection will be defined by one or more crossing lines
            //Check for intersections between all but one of the clip lines with all of the subject lines.
            //This is enough to test for intersection because we have already performed a point check.
            //This makes very little difference for large polygons, but cuts 9 operations down to 6 for 
            //a triangle/triangle intersection
            var clipPathLength = clip.Path.Count;
            var subjectPathLength = subject.Path.Count;
            //This next section gathers the points rather than using polygon.PathLines, so that the 
            //PathLines do not need to be set (we don't even use them in LineLineIntersection).
            for (var i = 0; i < clipPathLength - 1; i++) //-1 since we only need two lines
            {
                var point1 = clip.Path[i];
                var point2 = (i == clipPathLength - 1) ? clip.Path[0] : clip.Path[i + 1]; //Wrap back around to 0. Else use i+1
                for (var j = 0; j < subjectPathLength; j++) //Need to consider all the lines
                {
                    var point3 = subject.Path[j];
                    var point4 = (j == subjectPathLength - 1) ? subject.Path[0] : subject.Path[j + 1]; //Wrap back around to 0. Else use i+1
                    if (MiscFunctions.SegmentSegment2DIntersection(point1, point2, point3, point4, out var intersectionPoint, false))
                    {
                        if (intersectionPoint == point1 ||
                            intersectionPoint == point1 ||
                            intersectionPoint == point3 ||
                            intersectionPoint == point4)
                        {
                            continue;
                        }
                        //Else
                        return true;
                    }
                }
            }

            //No intersections identified. Return false.
            return false;
        }


        #region Clockwise / CounterClockwise Ordering

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        private static List<Vector2> CCWPositive(IEnumerable<Vector2> p)
        {
            var polygon = new List<Vector2>(p);
            var area = p.Area();
            if (area < 0) polygon.Reverse();
            return polygon;
        }

        /// <summary>
        /// Sets a polygon to clock wise negative
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Vector2> CWNegative(IEnumerable<Vector2> p)
        {
            var polygon = new List<Vector2>(p);
            var area = p.Area();
            if (area > 0) polygon.Reverse();
            return polygon;
        }

        #endregion

        #region Simplify
        public static List<List<Vector2>> Simplify(this IEnumerable<IEnumerable<Vector2>> paths, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            return paths.Select(p => Simplify(p, allowableChangeInAreaFraction)).ToList();
        }

        /// <summary>
        /// Simplifies the lines on a polygon to use fewer points when possible.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<Vector2> Simplify(this IEnumerable<Vector2> path, double allowableChangeInAreaFraction = Constants.SimplifyDefaultDeltaArea)
        {
            var polygon = path.ToArray();
            var numPoints = polygon.Length;
            var origArea = Math.Abs(polygon.Area()); //take absolute value s.t. it works on holes as well
            #region build initial list of cross products
            // crossProductToCornerDict stores all the cross-products with the indices of the polygon corners that have that cross-product.
            var crossProductToCornerDict = new Dictionary<double, HashSet<int>>();
            // cross-products which are kept in the same order as the corners they represent. This is solely used with the above
            // dictionary - to essentially do the reverse lookup. given a corner-index, crossProductsArray will instanly tell us the 
            // cross-product. The cross-product is used as the key in the dictionary - to find corner-indices.
            var crossProductsArray = new double[numPoints];
            // now keep two sorted lists of all crossproducts and the number of corners that have this value (not their indices)
            var sortedPositiveCrossList = new SortedList<double, int>();
            var sortedNegativeCrossList = new SortedList<double, int>(new ReverseSort());
            // make the cross-products. this is a for-loop that is preceded with the first element (requiring the last element, "^1" in 
            // C# 8 terms) and succeeded by one for the last corner 
            AddCrossProductToOneOfTheLists(polygon[^1], polygon[0], polygon[1], crossProductToCornerDict,
                crossProductsArray, 0, sortedPositiveCrossList, sortedNegativeCrossList);
            for (int i = 1; i < numPoints - 1; i++)
                AddCrossProductToOneOfTheLists(polygon[i - 1], polygon[i], polygon[i + 1], crossProductToCornerDict,
                crossProductsArray, i, sortedPositiveCrossList, sortedNegativeCrossList);
            AddCrossProductToOneOfTheLists(polygon[^2], polygon[^1], polygon[0], crossProductToCornerDict,
                crossProductsArray, numPoints - 1, sortedPositiveCrossList, sortedNegativeCrossList);
            #endregion

            // after much thought, the idea to split up into positive and negative sorted lists is so that we don't over remove vertices
            // by bouncing back and forth between convex and concave while staying with the target deltaArea. So, we do as many convex corners
            // before reaching a reducation of deltaArea - followed by a reduction of concave edges so that no omre than deltaArea is re-added
            double smallestArea;
            int numberWithSameCross;
            int[] indicesToRemove;
            for (int sign = 1; sign >= -1; sign -= 2)
            {
                var deltaArea = 2 * allowableChangeInAreaFraction * origArea; //multiplied by 2 in order to reduce all the divide by 2
                                                                              // that happens when we change cross-product to area of a triangle
                var relevantSortedList = (sign == 1) ? sortedPositiveCrossList : sortedNegativeCrossList;
                // first we remove any convex corners that would reduce the area
                do
                {
                    PopNextSmallestArea(relevantSortedList, crossProductToCornerDict, out smallestArea, out numberWithSameCross);
                    if (double.IsNaN(smallestArea) || deltaArea < sign * smallestArea) break;
                    if (numberWithSameCross * sign * smallestArea <= deltaArea) //the budget in deltaArea is bigger than all the corners at this value
                        indicesToRemove = crossProductToCornerDict[smallestArea].ToArray();
                    else
                    {  // there are more corners here than we need to hit the budget from deltaArea
                        numberWithSameCross = (int)(deltaArea / sign * smallestArea);
                        indicesToRemove = crossProductToCornerDict[smallestArea].Take(numberWithSameCross).ToArray();
                    }
                    deltaArea -= numberWithSameCross * sign * smallestArea;
                    foreach (var index in indicesToRemove)
                    {
                        // here's some clean up and updating
                        // 1. remove cross-product from dictionary (or at least reduce by 1 the number of corners exhibiting this value
                        RemoveFromDictionary(index, crossProductToCornerDict, crossProductsArray);
                        // 2. set the corner to null. we'll remove null corners at the end. for now, just set to null. 
                        // this is for speed and keep the indices correct in the various collections
                        polygon[index] = Vector2.Null;
                        // 3. find the four neighbors - two on each side. the closest two (prevIndex and nextIndex) need to be updated
                        // which requires each other (now that the corner in question has been removed) and their neighbors on the other side
                        // (nextnextIndex and prevprevIndex)
                        int nextIndex = FindValidNeighborIndex(index, true, polygon, numPoints);
                        RemoveFromDictionary(nextIndex, crossProductToCornerDict, crossProductsArray);
                        int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygon, numPoints);
                        int prevIndex = FindValidNeighborIndex(index, false, polygon, numPoints);
                        RemoveFromDictionary(prevIndex, crossProductToCornerDict, crossProductsArray);
                        int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygon, numPoints);
                        //4. now, add these new crossproducts both to the dictionary and to the sortedLists. Note, that nothing is
                        //removed from the sorted lists here. it is more efficient to just remove them if they bubble to the top of the list, 
                        // which is done in PopNextSmallestArea
                        AddCrossProductToOneOfTheLists(polygon[prevIndex], polygon[nextIndex], polygon[nextnextIndex], crossProductToCornerDict,
                            crossProductsArray, nextIndex, sortedPositiveCrossList, sortedNegativeCrossList);
                        AddCrossProductToOneOfTheLists(polygon[prevprevIndex], polygon[prevIndex], polygon[nextIndex], crossProductToCornerDict,
                            crossProductsArray, prevIndex, sortedPositiveCrossList, sortedNegativeCrossList);
                    }
                } while (true);
            }
            return polygon.Where(v => !v.IsNull()).ToList();
        }

        private static void PopNextSmallestArea(SortedList<double, int> sortedCrossList, Dictionary<double, HashSet<int>> crossProductToCornerDict,
            out double smallestArea, out int numberWithSameCross)
        {
            do
            {
                if (sortedCrossList.Count == 0)
                {
                    smallestArea = double.NaN;
                    numberWithSameCross = 0;
                    return;
                }
                smallestArea = sortedCrossList.Keys[0];
                numberWithSameCross = sortedCrossList.Values[0];
                sortedCrossList.RemoveAt(0);
                // the following while loop seems strange. why keep popping off elements? The reason is that when we remove a corner, we have to update
                // the two neighbors. Doing so from the crossProductToCornerDict is quick, but finding them in the sortedList would be log-N. So, we
                // do the clean-up here. So, sortedCrossList may grow larger than the real number of corners, but we only care about the first element.
                // if the first element is no longer valid. then now, we remove it and carry on.
            } while (!crossProductToCornerDict.ContainsKey(smallestArea));
        }

        /// <summary>
        /// Simplifies the lines on a polygon to use fewer points when possible.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<List<Vector2>> Simplify(this IEnumerable<Vector2> path, int targetNumberOfPoints)
        { return Simplify(new[] { path }, targetNumberOfPoints); }

        public static List<List<Vector2>> Simplify(this IEnumerable<IEnumerable<Vector2>> path, int targetNumberOfPoints)
        {
            var polygons = path.Select(p => p.ToArray()).ToArray();
            var numPoints = polygons.Sum(p => p.Length);
            var numToRemove = numPoints - targetNumberOfPoints;
            #region build initial list of cross products
            var crossProductToCornerDict = new Dictionary<double, HashSet<int>>();
            var crossProductsArray = new double[numPoints];
            var sortedCrosses = new SortedList<double, int>();
            foreach (var polygon in polygons)
            {
                AddCrossProductToLists(polygon[^1], polygon[0], polygon[1], crossProductToCornerDict,
                    crossProductsArray, 0, sortedCrosses);
                for (int i = 1; i < numPoints - 1; i++)
                    AddCrossProductToLists(polygon[i - 1], polygon[i], polygon[i + 1], crossProductToCornerDict,
                    crossProductsArray, i, sortedCrosses);
                AddCrossProductToLists(polygon[numPoints - 2], polygon[numPoints - 1], polygon[0], crossProductToCornerDict,
                    crossProductsArray, numPoints - 1, sortedCrosses);
            }
            #endregion
            PopNextSmallestArea(sortedCrosses, crossProductToCornerDict, out var smallestArea, out var numberWithSameCross);
            //var smallestArea = sortedPositiveCrossList.Keys[0];
            //var numberWithSameCross = sortedPositiveCrossList.Values[0];
            while (numToRemove > 0)
            {
                List<int> indicesToRemove = null;
                if (numberWithSameCross >= numToRemove)
                {  // there are more corners here than we need to hit the budget from deltaArea
                    indicesToRemove = new List<int>(crossProductToCornerDict[smallestArea].Take(numToRemove));
                }
                else  //the budget in deltaArea is bigger
                    indicesToRemove = new List<int>(crossProductToCornerDict[smallestArea]);
                numToRemove -= numberWithSameCross;
                foreach (var index in indicesToRemove)
                {
                    var cornerIndex = index;
                    var polygonIndex = 0;
                    while (cornerIndex >= polygons[polygonIndex].Length) cornerIndex -= polygons[polygonIndex].Length;
                    polygons[polygonIndex][cornerIndex] = Vector2.Null;
                    RemoveFromDictionary(index, crossProductToCornerDict, crossProductsArray);
                    int nextIndex = FindValidNeighborIndex(cornerIndex, true, polygons[polygonIndex], numPoints);
                    RemoveFromDictionary(nextIndex, crossProductToCornerDict, crossProductsArray);
                    int nextnextIndex = FindValidNeighborIndex(nextIndex, true, polygons[polygonIndex], numPoints);
                    int prevIndex = FindValidNeighborIndex(cornerIndex, false, polygons[polygonIndex], numPoints);
                    RemoveFromDictionary(prevIndex, crossProductToCornerDict, crossProductsArray);
                    int prevprevIndex = FindValidNeighborIndex(prevIndex, false, polygons[polygonIndex], numPoints);
                    if (nextnextIndex == prevIndex || nextIndex == prevprevIndex) // then reduced to two points.
                    {
                        polygons[polygonIndex][nextIndex] = Vector2.Null;
                        polygons[polygonIndex][nextnextIndex] = Vector2.Null;
                        numToRemove -= 2;
                    }
                    AddCrossProductToLists(polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex], polygons[polygonIndex][nextnextIndex], crossProductToCornerDict,
                        crossProductsArray, nextIndex, sortedCrosses);
                    AddCrossProductToLists(polygons[polygonIndex][prevprevIndex], polygons[polygonIndex][prevIndex], polygons[polygonIndex][nextIndex], crossProductToCornerDict,
                        crossProductsArray, prevIndex, sortedCrosses);
                }
                PopNextSmallestArea(sortedCrosses, crossProductToCornerDict, out smallestArea, out numberWithSameCross);
            }
            var result = new List<List<Vector2>>();
            foreach (var polygon in polygons)
            {
                var resultPolygon = new List<Vector2>();
                foreach (var corner in polygon)
                    if (!corner.IsNull()) resultPolygon.Add(corner);
                if (resultPolygon.Count > 0)
                    result.Add(resultPolygon);
            }
            return result;
        }

        private static void RemoveFromDictionary(int index, Dictionary<double, HashSet<int>> crossProductToCornerDict, double[] crossProductArray)
        {
            var cross = crossProductArray[index];
            var indicesWithSameCrossProduct = crossProductToCornerDict[cross];
            if (indicesWithSameCrossProduct.Count == 1) crossProductToCornerDict.Remove(cross);
            else indicesWithSameCrossProduct.Remove(index);
        }

        private static int FindValidNeighborIndex(int index, bool forward, Vector2[] polygon, int numPoints)
        {
            int increment = forward ? 1 : -1;
            do
            {
                index += increment;
                if (index < 0) index = numPoints - 1;
                else if (index == numPoints) index = 0;
            }
            while (polygon[index].IsNull());
            return index;
        }

        private class ReverseSort : IComparer<double>
        {
            public int Compare(double x, double y)
            {
                if (x == y) return 0;
                if (x < y) return 1;
                return -1;
            }
        }

        private static void AddCrossProductToOneOfTheLists(Vector2 fromPoint, Vector2 currentPoint,
            Vector2 nextPoint, Dictionary<double, HashSet<int>> crossProductToCornerDict,
            double[] crossProductsList, int index, SortedList<double, int> sortedPositiveCrosses,
            SortedList<double, int> sortedNegativeCrosses)
        {
            var cross = (currentPoint - fromPoint).Cross(nextPoint - currentPoint);
            if (crossProductToCornerDict.ContainsKey(cross))
            {
                crossProductToCornerDict[cross].Add(index);
                if (cross < 0) sortedNegativeCrosses[cross]++;
                else sortedPositiveCrosses[cross]++;
            }
            else
            {
                crossProductToCornerDict.Add(cross, new HashSet<int> { index });
                try
                {
                    if (cross < 0) sortedNegativeCrosses.Add(cross, 1);
                    else sortedPositiveCrosses.Add(cross, 1);
                }
                catch
                {
                    //I know, it seems problematic to have a try-catch here, but the reason is that in very
                    //rare cases, the cross-product will already be in the sorted list, but not in the dictionary
                    //checked at the begninning of the if-else. in order to 'fix' we'd need to update RemoveFromDictionary
                    //to update the sorted-list the same time as the dictionary. however, finding and removing from the 
                    //sortedlist is O(n) and we get rid of the important ones in the front of the list in PopNextSmallestArea.
                }
            }
            crossProductsList[index] = cross;
        }
        private static void AddCrossProductToLists(Vector2 fromPoint, Vector2 currentPoint,
            Vector2 nextPoint, Dictionary<double, HashSet<int>> crossProductToCornerDict,
            double[] crossProductsList, int index, SortedList<double, int> sortedCrosses)
        {
            var cross = Math.Abs((currentPoint - fromPoint).Cross(nextPoint - currentPoint));
            if (crossProductToCornerDict.ContainsKey(cross))
            {
                crossProductToCornerDict[cross].Add(index);
                sortedCrosses[cross]++;
            }
            else
            {
                crossProductToCornerDict.Add(cross, new HashSet<int> { index });
                sortedCrosses.Add(cross, 1);
            }
            crossProductsList[index] = cross;
        }
        #endregion

        #region Offset

        /// <summary>
        /// Offets the given path by the given offset, rounding corners.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<Vector2> path, double offset,
            double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtRound, minLength);
        }


        /// <summary>
        /// Offsets all paths by the given offset value. Rounds the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetRound(this IEnumerable<IEnumerable<Vector2>> paths, double offset,
            double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtRound, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<Vector2> path, double offset, double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Miters the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetMiter(this IEnumerable<IEnumerable<Vector2>> paths, double offset, double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtMiter, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="offset"></param>
        /// <param name="minLength"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<Vector2> path, double offset, double minLength = 0.0)
        {
            return Offset(new List<IEnumerable<Vector2>> { path }, offset, JoinType.jtSquare, minLength);
        }

        /// <summary>
        /// Offsets all paths by the given offset value. Squares the corners.
        /// Offest value may be positive or negative.
        /// Loops must be ordered CCW positive.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="minLength"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static List<List<Vector2>> OffsetSquare(this IEnumerable<IEnumerable<Vector2>> paths, double offset, double minLength = 0.0)
        {
            return Offset(paths, offset, JoinType.jtSquare, minLength);
        }

        private static List<List<Vector2>> Offset(IEnumerable<IEnumerable<Vector2>> paths, double offset, JoinType joinType,
            double minLength = 0.0)
        {
            //Convert Points (TVGL) to IntPoints (Clipper)
            var clipperSubject =
                paths.Select(loop => loop.Select(point =>
                new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler))
                .ToList()).ToList();
            if (minLength.IsNegligible())
            {
                var totalLength = clipperSubject.Sum(Perimeter);
                minLength = totalLength * 0.001;
            }
            //Setup Clipper
            var clip = new ClipperOffset(2, minLength);
            clip.AddPaths(clipperSubject, joinType, EndType.etClosedPolygon);

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            clip.Execute(clipperSolution, offset * Constants.DoubleToIntPointMultipler);

            //Convert back to points and return solution
            return clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler, point.Y * Constants.IntPointToDoubleMultipler)).ToList()).ToList();
        }

        private static double Perimeter(this List<IntPoint> polygon)
        {
            var value1 = polygon[^1];
            var value2 = polygon[0];
            var dx = value1.X - value2.X;
            var dy = value1.Y - value2.Y;
            double perimeter = Math.Sqrt(dx * dx + dy * dy);
            for (var i = 1; i < polygon.Count; i++)
            {
                value1 = polygon[i - 1];
                value2 = polygon[i];
                dx = value1.X - value2.X;
                dy = value1.Y - value2.Y;
                perimeter += Math.Sqrt(dx * dx + dy * dy);
            }
            return perimeter;
        }

        #endregion

        #region Boolean Operations

        #region Union

        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use GetShallowPolygonTrees to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, bool simplifyPriorToUnion = true,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, (IEnumerable<List<Vector2>>)subject, null, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use GetShallowPolygonTrees to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, clip, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use GetShallowPolygonTrees to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, new[] { subject }, new[] { clip }, simplifyPriorToUnion);
        }

        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use GetShallowPolygonTrees to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, new[] { clip }, simplifyPriorToUnion);
        }

        #endregion

        #region Difference
        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctDifference, subject, clip, simplifyPriorToDifference);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(subject, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, clip, simplifyPriorToDifference, polyFill);
        }
        #endregion

        #region Intersection
        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(subjects, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, clips, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToIntersection = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctIntersection, subject, clip, simplifyPriorToIntersection);
        }
        #endregion

        #region Xor

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctXor, subject, clip, simplifyPriorToXor);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip,
            bool simplifyPriorToXor = true, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(subjects, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips.  
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToXor = true,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, clips, simplifyPriorToXor, polyFill);
        }

        #endregion

        private static List<List<Vector2>> BooleanOperation(PolygonFillType fillMethod, ClipType clipType,
            IEnumerable<IEnumerable<Vector2>> subject,
           IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToBooleanOperation = true)
        {
            var fillType = fillMethod switch
            {
                PolygonFillType.Positive => PolyFillType.pftPositive,
                PolygonFillType.Negative => PolyFillType.pftNegative,
                PolygonFillType.NonZero => PolyFillType.pftNonZero,
                PolygonFillType.EvenOdd => PolyFillType.pftEvenOdd,
                _ => throw new NotImplementedException(),
            };

            if (simplifyPriorToBooleanOperation)
            {
                subject = subject.Select(path => Simplify(path));
                //If not null
                clip = clip?.Select(path => Simplify(path));
            }

            if (!subject.Any())
            {
                if (clip == null || !clip.Any())
                {
                    return new List<List<Vector2>>();
                }
                //Use the clip as the subject if this is a union operation and the clip is not null.
                if (clipType == ClipType.ctUnion)
                {
                    subject = clip;
                    clip = null;
                }
            }

            //Setup Clipper
            var clipper = new Clipper() { StrictlySimple = true };

            //Convert Points (TVGL) to IntPoints (Clipper)
            var subjectIntLoops = new List<List<IntPoint>>();
            foreach (var loop in subject)
            {
                var intLoop = loop.Select(point
                    => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                if (intLoop.Count > 2) subjectIntLoops.Add(intLoop);
            }
            clipper.AddPaths(subjectIntLoops, PolyType.ptSubject, true);

            if (clip != null)
            {
                var clipIntLoops = new List<List<IntPoint>>();
                foreach (var loop in clip)
                {
                    var intLoop = loop.Select(point
                        => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                    if (intLoop.Count > 2) clipIntLoops.Add(intLoop);
                }
                clipper.AddPaths(clipIntLoops, PolyType.ptClip, true);
            }

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            var result = clipper.Execute(clipType, clipperSolution, fillType, fillType);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points
            var solution = clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler, point.Y * Constants.IntPointToDoubleMultipler))
            .ToList()).ToList();
            return solution;
        }
        #endregion

        private static int NumberOfLinesBelow(SweepEvent se1, SweepList sweepLines)
        {
            var linesBelow = 0;
            //Any indices above se1 can be ignored
            for (var i = se1.IndexInList - 1; i > -1; i--)
            {
                var se2 = sweepLines.Item(i);
                var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                if (IsPointOnSegment(se2.Point, se2.OtherEvent.Point, new Vector2(se1.Point.X, se2Y)))
                {
                    linesBelow++;
                }
            }
            return linesBelow;
        }

        #region Top Level Boolean Operation Method
        /// <reference>
        /// This aglorithm is based on on the paper:
        /// A simple algorithm for Boolean operations on polygons. Martínez, et. al. 2013. Advances in Engineering Software.
        /// Links to paper: http://dx.doi.org/10.1016/j.advengsoft.2013.04.004 OR http://www.sciencedirect.com/science/article/pii/S0965997813000379
        /// </reference>
        private static List<List<Vector2>> BooleanOperation(IList<List<Vector2>> subject, IList<List<Vector2>> clip, BooleanOperationType booleanOperationType)
        {
            //1.Find intersections with vertical sweep line
            //1.Subdivide the edges of the polygons at their intersection points.
            //2.Select those subdivided edges that lie inside—or outside—the other polygon.
            //3.Join the selected edges to form the contours of the result polygon and compute the child contours.
            var unsortedSweepEvents = new List<SweepEvent>();

            #region Build Sweep PathID and Order Them Lexicographically
            //Build the sweep events and order them lexicographically (Low X to High X, then Low Y to High Y).
            foreach (var path in subject)
            {
                var n = path.Count;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    SweepEvent se1, se2;
                    if (path[i].X.IsPracticallySame(path[j].X))
                    {
                        if (path[i].Y.IsPracticallySame(path[j].Y)) continue; //Ignore this 
                        if (path[i].Y < path[j].Y)
                        {
                            se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                            se2 = new SweepEvent(path[j], false, false, PolygonType.Subject);
                        }
                        else
                        {
                            se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                            se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                        }
                    }
                    else if (path[i].X < path[j].X)
                    {
                        se1 = new SweepEvent(path[i], true, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], false, false, PolygonType.Subject);
                    }
                    else
                    {
                        se1 = new SweepEvent(path[i], false, true, PolygonType.Subject);
                        se2 = new SweepEvent(path[j], true, false, PolygonType.Subject);
                    }
                    se1.OtherEvent = se2;
                    se2.OtherEvent = se1;
                    unsortedSweepEvents.Add(se1);
                    unsortedSweepEvents.Add(se2);
                }
            }
            foreach (var path in clip)
            {
                var n = path.Count;
                for (var i = 0; i < n; i++)
                {
                    var j = (i + 1) % n; //Next position in path. Goes to 0 when i = n-1; 
                    SweepEvent se1, se2;
                    if (path[i].X.IsPracticallySame(path[j].X))
                    {
                        if (path[i].Y.IsPracticallySame(path[j].Y)) continue; //Ignore this 
                        if (path[i].Y < path[j].Y)
                        {
                            se1 = new SweepEvent(path[i], true, true, PolygonType.Clip);
                            se2 = new SweepEvent(path[j], false, false, PolygonType.Clip);
                        }
                        else
                        {
                            se1 = new SweepEvent(path[i], false, true, PolygonType.Clip);
                            se2 = new SweepEvent(path[j], true, false, PolygonType.Clip);
                        }
                    }
                    else if (path[i].X < path[j].X)
                    {
                        se1 = new SweepEvent(path[i], true, true, PolygonType.Clip);
                        se2 = new SweepEvent(path[j], false, false, PolygonType.Clip);
                    }
                    else
                    {
                        se1 = new SweepEvent(path[i], false, true, PolygonType.Clip);
                        se2 = new SweepEvent(path[j], true, false, PolygonType.Clip);
                    }
                    se1.OtherEvent = se2;
                    se2.OtherEvent = se1;
                    unsortedSweepEvents.Add(se1);
                    unsortedSweepEvents.Add(se2);
                }
            }
            var orderedSweepEvents = new OrderedSweepEventList(unsortedSweepEvents);
            #endregion

            var result = new List<SweepEvent>();
            var sweepLines = new SweepList();
            while (orderedSweepEvents.Any())
            {
                var sweepEvent = orderedSweepEvents.First();
                orderedSweepEvents.RemoveAt(0);
                SweepEvent nextSweepEvent = null;
                if (orderedSweepEvents.Any())
                {
                    nextSweepEvent = orderedSweepEvents.First();
                }
                if (sweepEvent.Left) //left endpoint
                {

                    //Inserting the event into the sweepLines list
                    var index = sweepLines.Insert(sweepEvent);
                    sweepEvent.IndexInList = index;
                    bool goBack1; //goBack is used to processes line segments from some collinear intersections
                    CheckAndResolveIntersection(sweepEvent, sweepLines.Next(index), ref sweepLines, ref orderedSweepEvents, out goBack1);
                    bool goBack2;
                    CheckAndResolveIntersection(sweepLines.Previous(index), sweepEvent, ref sweepLines, ref orderedSweepEvents, out goBack2);
                    if (goBack1 || goBack2) continue;

                    //First, we need to check if the this sweepEvent has the same Point and is collinear with the next line. 
                    //To determine collinearity, we need to make sure we are using the same criteria as everywhere else in the code, and here-in lies the problem
                    //1. m1 != m2 but LineLineIntersection function says collinear. 
                    //2. m1 =! m2 && LineLineIntersection says non-collinear, but yintercept at shorter lines other.X, yeilds shorter line's other point.
                    //Which should we use? Should we adjust tolerances? - We need to use the least precise method, which should be the last one.
                    if (nextSweepEvent != null && nextSweepEvent.Point == sweepEvent.Point)
                    {
                        //If the slopes are practically the same then the lines are collinear 
                        //If remotely similar, we need to use the intersection, which is used later on to determine collinearity. (basically, we have to be consistent).
                        if (sweepEvent.Slope.IsPracticallySame(nextSweepEvent.Slope, 0.00001))
                        {
                            Vector2 intersectionPoint;
                            if (MiscFunctions.SegmentSegment2DIntersection(sweepEvent.Point, sweepEvent.OtherEvent.Point,
                                nextSweepEvent.Point, nextSweepEvent.OtherEvent.Point, out intersectionPoint, true) &&
                                intersectionPoint == null)
                            {
                                //If they belong to the same polygon type, they are overlapping, but we still use the other polygon like normal
                                //to determine if they are in the result.
                                if (sweepEvent.PolygonType == nextSweepEvent.PolygonType)
                                    throw new NotImplementedException();
                                sweepEvent.DuplicateEvent = nextSweepEvent;
                                nextSweepEvent.DuplicateEvent = sweepEvent;
                                SetInformation(sweepEvent, null, booleanOperationType, true);
                            }
                            else
                            {
                                //Set information updates the OtherInOut property and uses this to determine if the sweepEvent is part of the result.
                                //Select the closest edge downward that belongs to the other polygon.
                                SetInformation(sweepEvent, sweepLines.PreviousOther(index), booleanOperationType);
                            }
                        }
                        else
                        {
                            //Set information updates the OtherInOut property and uses this to determine if the sweepEvent is part of the result.
                            //Select the closest edge downward that belongs to the other polygon.
                            SetInformation(sweepEvent, sweepLines.PreviousOther(index), booleanOperationType);
                        }
                    }
                    else
                    {
                        //Select the closest edge downward that belongs to the other polygon.
                        //Set information updates the OtherInOut property and uses this to determine if the sweepEvent is part of the result.
                        SetInformation(sweepEvent, sweepLines.PreviousOther(index), booleanOperationType);
                    }
                    //Get the previous (first directly below starting point) event in result (using the sweeplines)
                    if (sweepEvent.InResult)
                    {
                        sweepEvent.PrevInResult = sweepLines.PreviousInResult(index);
                    }
                }
                else //The sweep event corresponds to the right endpoint
                {
                    var index = sweepLines.Find(sweepEvent.OtherEvent);
                    if (index == -1) throw new Exception("Other event not found in list. Error in implementation");
                    var next = sweepLines.Next(index);
                    var prev = sweepLines.Previous(index);
                    sweepLines.RemoveAt(index);
                    bool goBack;
                    CheckAndResolveIntersection(prev, next, ref sweepLines, ref orderedSweepEvents, out goBack);
                }
                if (sweepEvent.InResult || sweepEvent.OtherEvent.InResult)
                {
                    if (sweepEvent.InResult && !sweepEvent.Left) throw new Exception("error in implementation");
                    if (sweepEvent.OtherEvent.InResult && sweepEvent.Left) throw new Exception("error in implementation");
                    if (sweepEvent.Point == sweepEvent.OtherEvent.Point) continue; //Ignore this negligible length line.
                    result.Add(sweepEvent);
                }
            }

            //Next stage. Find the paths
            var hashResult = new HashSet<SweepEvent>(result);
            var hashPoints = new HashSet<Vector2>();
            for (var i = 0; i < result.Count; i++)
            {
                result[i].PositionInResult = i;
                result[i].Processed = false;
                if (!hashResult.Contains(result[i].OtherEvent))
                    throw new Exception("Error in implementation. Both sweep events in the pair should be in this list.");
                var point = result[i].Point;
                if (hashPoints.Contains(point))
                    hashPoints.Remove(point);
                else hashPoints.Add(point);
            }
            if (hashPoints.Select(point => hashPoints.Where(otherPoint => !point.Equals(otherPoint)).Any(otherPoint => point == otherPoint)).Any(duplicateFound => !duplicateFound))
            {
                throw new Exception("Point appears in list an odd number of times. This means there are missing sweep events or one too many.");
            }

            var solution = new List<List<Vector2>>();
            var currentPathID = 0;
            List<Vector2> previousPath = null;
            foreach (var se1 in result.Where(se1 => !se1.Processed))
            {
                int parentID;
                var depth = ComputeDepth(se1, previousPath, out parentID);
                var path = ComputePath(se1, currentPathID, depth, parentID, result);
                if (depth % 2 != 0) //Odd
                {
                    path = CWNegative(path);
                }
                solution.Add(path);
                //if (parent != -1) //parent path ID
                //{
                //    solution[parent].AddChild(currentPathID);
                //}
                currentPathID++;
                previousPath = path;
            }

            return solution;
        }
        #endregion

        #region Set Information
        private static void SetInformation(SweepEvent sweepEvent, SweepEvent previous, BooleanOperationType booleanOperationType, bool isFirstOfDuplicatePair = false)
        {
            //Consider whether the previous edge from the other polygon is an inside-outside transition or outside-inside, based on a vertical ray starting below
            // the previous edge and pointing upwards. //If the transition is outside-inside, the sweepEvent lays inside other polygon, otherwise it lays outside.
            if (previous == null || !previous.LeftToRight)
            {
                //Then it must lie outside the other polygon
                sweepEvent.OtherInOut = false;
                sweepEvent.OtherEvent.OtherInOut = false;
            }
            else //It lies inside the other polygon
            {
                sweepEvent.OtherInOut = true;
                sweepEvent.OtherEvent.OtherInOut = true;
            }

            //Determine if it should be in the results
            if (booleanOperationType == BooleanOperationType.Union)
            {
                //If duplicate (overlapping) edges, the second of the duplicate pair is never kept in the result 
                //The first duplicate pair is in the result if the lines have the same left to right properties.
                //Otherwise, the lines are inside.
                if (sweepEvent.DuplicateEvent != null)
                {
                    if (!isFirstOfDuplicatePair) sweepEvent.InResult = false;
                    else sweepEvent.InResult = sweepEvent.LeftToRight == sweepEvent.DuplicateEvent.LeftToRight;
                }
                else sweepEvent.InResult = !sweepEvent.OtherInOut;
            }
            else if (booleanOperationType == BooleanOperationType.Intersection)
            {
                if (sweepEvent.DuplicateEvent != null) throw new NotImplementedException();
                sweepEvent.InResult = sweepEvent.OtherInOut;
            }
            else throw new NotImplementedException();
        }
        #endregion

        #region Compute Depth
        private static int ComputeDepth(SweepEvent se, List<Vector2> previousPath, out int parentID)
        {
            //This function shoots a ray down from the sweep event point, which is the first point of the path.
            //it must be the left bottom (min X, then min Y) this sweep event is called se2.
            //Then we use the bool properties of the se2 to determine whether it is inside the polygon that
            //se2 belongs to, its parent, or none.
            if (previousPath != null && se.PrevInResult != null)
            {
                if (se.PrevInResult.LeftToRight)
                {
                    //else, outside-inside transition. It is inside the previous polygon.
                    parentID = se.PrevInResult.PathID;
                    return se.PrevInResult.Depth + 1;
                }
                if (se.PrevInResult.ParentPathID != -1)
                {
                    //It must share the same parent path and depth
                    parentID = se.PrevInResult.ParentPathID;
                    return se.PrevInResult.Depth;
                }
                //else, there is a polygon below it, but it is not inside any polygons
                parentID = -1;
                return 0;
            }
            //else, not inside any other polygons
            parentID = -1;
            return 0;
        }

        #endregion

        #region Compute Paths
        //This can return paths that contain the same point more than once (does this instead of making + and - loop).
        //Could chop them up, but I'm not sure that this is necessary.
        private static List<Vector2> ComputePath(SweepEvent startEvent, int pathID, int depth, int parentID, IList<SweepEvent> result)
        {
            //First, get the proper start event, given the current guess.
            //The proper start event will be the lowest OtherEvent.Y from neighbors at the startEvent.Point.
            //This will ensure we move CW positive around the path, regardless of previous To/From ordering.
            //Which allows us to handle points with multiple options.
            //We will determine To/From ordering based on the path depth.
            var neighbors = FindAllNeighbors(startEvent, result);
            neighbors.Add(startEvent);
            var yMin = startEvent.OtherEvent.Point.Y;
            var xMin = neighbors.Select(neighbor => neighbor.OtherEvent.Point.X).Concat(new[] { double.PositiveInfinity }).Min();
            foreach (var neighbor in neighbors)
            {
                //We need the lowest neighbor line, not necessarily the lowest Y value.
                if (!neighbor.Left) throw new Exception("First event should always be left.");
                var neighborYIntercept = LineIntercept(neighbor, xMin);
                if (neighborYIntercept < yMin)
                {
                    yMin = neighborYIntercept;
                    startEvent = neighbor;
                }
            }

            var updateAll = new List<SweepEvent> { startEvent };
            var path = new List<Vector2>();
            startEvent.Processed = false; //This will be to true right at the end of the while loop. 
            var currentSweepEvent = startEvent;
            do
            {
                //Get the other event (endpoint) for this line. 
                currentSweepEvent = currentSweepEvent.OtherEvent;
                currentSweepEvent.Processed = true;
                updateAll.Add(currentSweepEvent);

                //Since result is sorted lexicographically, the event we are looking for will be adjacent to the current sweep event (note that we are staying on the same point)
                neighbors = FindAllNeighbors(currentSweepEvent, result);
                if (!neighbors.Any()) throw new Exception("Must have at least one neighbor");
                if (neighbors.Count == 1)
                {
                    currentSweepEvent = neighbors.First();
                }
                else
                {
                    //Get the minimum signed angle between the two vectors 
                    var minAngle = double.PositiveInfinity;
                    var v1 = currentSweepEvent.Point - currentSweepEvent.OtherEvent.Point;
                    foreach (var neighbor in neighbors)
                    {
                        var v2 = neighbor.OtherEvent.Point - neighbor.Point;
                        var angle = MiscFunctions.InteriorAngleBetweenEdgesInCCWList(v1, v2);
                        if (angle < 0 || angle > 2 * Math.PI) throw new Exception("Error in my assumption of output from above function");
                        if (angle < minAngle)
                        {
                            minAngle = angle;
                            currentSweepEvent = neighbor;
                        }
                    }
                }
                currentSweepEvent.Processed = true;
                updateAll.Add(currentSweepEvent);

                //if (!currentSweepEvent.From) throw new Exception("Error in implementation");
                path.Add(currentSweepEvent.Point); //Add the "From" Point  

            } while (currentSweepEvent != startEvent);

            //Once all the events of the path are found, update their PathID, ParentID, Depth fields
            foreach (var sweepEvent in updateAll)
            {
                sweepEvent.PathID = pathID;
                sweepEvent.Depth = depth;
                sweepEvent.ParentPathID = parentID;

            }

            ////Check if the path should be chopped up with interior paths
            //var alreadyConsidered = new HashSet<Point>();
            //foreach (var point in path.Where(point => point.InResultMultipleTimes))
            //{
            //    if (!alreadyConsidered.Contains(point)) alreadyConsidered.Add(point);
            //    else
            //    {

            //    }
            //}
            return path;
        }

        private static List<SweepEvent> FindAllNeighbors(SweepEvent se1, IList<SweepEvent> result)
        {
            var neighbors = new List<SweepEvent>();
            //Search points upward (incrementing)
            var i = se1.PositionInResult;
            var thisDirection = true;
            do
            {
                i++;
                if (i > result.Count - 1 || se1.Point != result[i].Point) thisDirection = false;
                else if (result[i].Processed) continue;
                else neighbors.Add(result[i]);
            } while (thisDirection);

            //Also check lower decrementing
            i = se1.PositionInResult;
            thisDirection = true;
            do
            {
                i--;
                if (i < 0 || se1.Point != result[i].Point) thisDirection = false;
                else if (result[i].Processed) continue;
                else neighbors.Add(result[i]);
            } while (thisDirection);

            return neighbors;
        }
        #endregion

        #region Check and Resolve Intersection between two lines
        private static void CheckAndResolveIntersection(SweepEvent se1, SweepEvent se2, ref SweepList sweepLines, ref OrderedSweepEventList orderedSweepEvents, out bool goBack)
        {
            goBack = false;
            if (se1 == null || se2 == null) return;
            //if (se1.DuplicateEvent == se2) return;

            var newSweepEvents = new List<SweepEvent>();

            Vector2 intersectionPoint;
            if (MiscFunctions.SegmentSegment2DIntersection(se1.Point, se1.OtherEvent.Point,
                se2.Point, se2.OtherEvent.Point, out intersectionPoint, true) && intersectionPoint == null)
            {
                #region SPECIAL CASE: Collinear
                //SPECIAL CASE: Collinear
                if (se1.Point == se2.Point)
                {
                    if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X))
                    {
                        //if (se1.PolygonType == se2.PolygonType) throw new NotImplementedException();
                        //Else set duplicates
                    }
                    else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                    {
                        //Order goes (1) se1.Point == se2.Point, (2) se1.OtherEvent.Point, (3) se2.OtherEvent.Point
                        //Segment se2 
                        newSweepEvents.AddRange(Segment(se2, se1.OtherEvent.Point));
                    }
                    else
                    {
                        //Order goes (1) se1.Point == se2.Point, (2) se2.OtherEvent.Point, (3) se1.OtherEvent.Point
                        //Segment se1 
                        newSweepEvents.AddRange(Segment(se1, se2.OtherEvent.Point));
                    }
                    //Set DuplicateEvents
                    se1.DuplicateEvent = se2;
                    se1.OtherEvent.DuplicateEvent = se2.OtherEvent;
                    se2.DuplicateEvent = se1;
                    se2.OtherEvent.DuplicateEvent = se1.OtherEvent;
                }

                else
                {
                    //Reorder if necessary (reduces the amount of code)
                    if (se1.Point.X > se2.Point.X)
                    {
                        var temp = se1;
                        se1 = se2;
                        se2 = temp;
                    }

                    if (se1.OtherEvent.Point == se2.OtherEvent.Point)
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se1.OtherEvent.Point == se2.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        var se1Other = se1.OtherEvent;
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se1Other.DuplicateEvent = se2.OtherEvent;
                        se2.OtherEvent.DuplicateEvent = se1Other;
                    }
                    else if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X)) throw new NotImplementedException();
                    else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se1.OtherEvent.Point, (4) se2.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        var se1Other = se1.OtherEvent;
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Segment se2
                        newSweepEvents.AddRange(Segment(se2, se1Other.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se1Other.DuplicateEvent = se2.OtherEvent;
                        se2.OtherEvent.DuplicateEvent = se1Other;
                    }
                    else
                    {
                        //Order goes, (1) se1.Point, (2) se2.Point, (3) se2.OtherEvent.Point, (4) se1.OtherEvent.Point
                        goBack = true;
                        sweepLines.RemoveAt(se2.IndexInList);
                        orderedSweepEvents.Insert(se2);

                        //Segment se1
                        newSweepEvents.AddRange(Segment(se1, se2.Point));

                        //Segment second new sweep event
                        newSweepEvents.AddRange(Segment(newSweepEvents[1], se2.OtherEvent.Point));

                        //Set DuplicateEvents
                        se2.DuplicateEvent = newSweepEvents[1];
                        newSweepEvents[1].DuplicateEvent = se2;
                        se2.OtherEvent.DuplicateEvent = newSweepEvents[2];
                        newSweepEvents[2].DuplicateEvent = se2.OtherEvent;
                    }
                }

                //Add all new sweep events
                foreach (var sweepEvent in newSweepEvents)
                {
                    orderedSweepEvents.Insert(sweepEvent);
                }
                return;
                #endregion
            }

            //GENERAL CASE: Lines share a point and cannot possibly intersect. It was not collinear, so return.
            if (se1.Point == se2.Point || se1.Point == se2.OtherEvent.Point ||
                se1.OtherEvent.Point == se2.Point || se1.OtherEvent.Point == se2.OtherEvent.Point)
            {
                return;
            }
            //GENERAL CASE: Lines do not intersect.
            if (intersectionPoint == null) return;

            //SPECIAL CASE: Intersection point is the same as one of previousSweepEvent's line end points.
            if (intersectionPoint == se1.Point)
            {
                var se2Other = se2.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se1.Point, false, !se2.From, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se1.Point, true, !se2Other.From, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            else if (intersectionPoint == se1.OtherEvent.Point)
            {
                var se2Other = se2.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se1.OtherEvent.Point, false, !se2.From, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se1.OtherEvent.Point, true, !se2Other.From, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            //SPECIAL CASE: Intersection point is the same as one of se2's line end points. 
            else if (intersectionPoint == se2.Point)
            {
                var se1Other = se1.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se2.Point, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se2.Point, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            else if (intersectionPoint == se2.OtherEvent.Point)
            {
                var se1Other = se1.OtherEvent;

                var newSweepEvent1 = new SweepEvent(se2.OtherEvent.Point, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(se2.OtherEvent.Point, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
            }

            //GENERAL CASE: Lines are not parallel and only intersct once, between the end points of both lines.
            else
            {
                var se1Other = se1.OtherEvent;
                var se2Other = se2.OtherEvent;

                //Split Sweep Event 1 (previousSweepEvent)
                var newSweepEvent1 = new SweepEvent(intersectionPoint, false, !se1.From, se1.PolygonType) { OtherEvent = se1 };
                se1.OtherEvent = newSweepEvent1;

                var newSweepEvent2 = new SweepEvent(intersectionPoint, true, !se1Other.From, se1.PolygonType) { OtherEvent = se1Other };
                se1Other.OtherEvent = newSweepEvent2;

                //Split Sweep Event 2 (se2)
                var newSweepEvent3 = new SweepEvent(intersectionPoint, false, !se2.From, se2.PolygonType) { OtherEvent = se2 };
                se2.OtherEvent = newSweepEvent3;

                var newSweepEvent4 = new SweepEvent(intersectionPoint, true, !se2Other.From, se2.PolygonType) { OtherEvent = se2Other };
                se2Other.OtherEvent = newSweepEvent4;

                //Add all new sweep events
                orderedSweepEvents.Insert(newSweepEvent1);
                orderedSweepEvents.Insert(newSweepEvent2);
                orderedSweepEvents.Insert(newSweepEvent3);
                orderedSweepEvents.Insert(newSweepEvent4);
            }
        }

        private static IEnumerable<SweepEvent> Segment(SweepEvent sweepEvent, Vector2 point)
        {
            var sweepEventOther = sweepEvent.OtherEvent;
            //Split Sweep Event 1 (previousSweepEvent)
            var newSweepEvent1 = new SweepEvent(point, false, !sweepEvent.From, sweepEvent.PolygonType) { OtherEvent = sweepEvent };
            sweepEvent.OtherEvent = newSweepEvent1;

            var newSweepEvent2 = new SweepEvent(point, true, !sweepEventOther.From, sweepEvent.PolygonType) { OtherEvent = sweepEventOther };
            sweepEventOther.OtherEvent = newSweepEvent2;

            return new List<SweepEvent> { newSweepEvent1, newSweepEvent2 };
        }

        #endregion

        #region SweepList for Boolean Operations
        private class SweepList
        {
            private List<SweepEvent> _sweepEvents;

            public int Count => _sweepEvents.Count;

            public SweepEvent Next(int i)
            {
                if (i == _sweepEvents.Count - 1) return null;
                var sweepEvent = _sweepEvents[i + 1];
                sweepEvent.IndexInList = i + 1;
                return sweepEvent;
            }

            public SweepEvent Previous(int i)
            {
                if (i == 0) return null;
                var sweepEvent = _sweepEvents[i - 1];
                sweepEvent.IndexInList = i - 1;
                return sweepEvent;
            }

            public SweepEvent PreviousOther(int i)
            {
                var current = _sweepEvents[i];
                while (i > 0)
                {
                    i--; //Decrement
                    var previous = _sweepEvents[i];
                    if (current.PolygonType == previous.PolygonType) continue;
                    if (current.Point.Y.IsPracticallySame(previous.Point.Y)) return previous; //The Y's are the same, so use the upper most sweepEvent (earliest in list) to determine if inside.
                    if (current.Point.Y < previous.Point.Y && current.Point.Y < previous.OtherEvent.Point.Y)
                    {
                        //Note that it is possible for either the previous.Point or previous.OtherEvent.Point to be below the current point, as long as the previous point is to the left
                        //of the current.Point and is sloped below or above the current point.
                        throw new Exception("Error in implemenation (sorting?). This should never happen.");
                    }
                    return previous;
                }
                //No other polygon event was found. Return null (or duplicate event if it exists).
                return current.DuplicateEvent;
            }

            public SweepEvent PreviousInResult(int i)
            {
                while (i > 0)
                {
                    i--; //Decrement
                    var previous = _sweepEvents[i];
                    if (!previous.InResult) continue;
                    return previous;
                }
                //No other polygon event was found. Return null.
                return null;
            }

            public void RemoveAt(int i)
            {
                _sweepEvents.RemoveAt(i);
            }

            //Insert, ordered min Y to max Y for the intersection of the line with xval.
            public int Insert(SweepEvent se1)
            {
                if (se1 == null) throw new Exception("Must not be null");
                if (!se1.Left) throw new Exception("Right end point sweep events are not supposed to go into this list");

                if (_sweepEvents == null)
                {
                    _sweepEvents = new List<SweepEvent> { se1 };
                    return 0;
                }

                var se1Y = se1.Point.Y;
                var i = 0;
                foreach (var se2 in _sweepEvents)
                {
                    if (se1.Point == se2.Point)
                    {
                        var m1 = se1.Slope;
                        var m2 = se2.Slope;
                        if (m1.IsPracticallySame(m2) || m1 < m2) //if collinear or se1 is below se2
                        {
                            break; //ok to insert before (Will be marked as a duplicate event)               
                        }
                        //Else increment and continue
                        i++;
                        continue;
                    }

                    var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                    if (se1Y.IsPracticallySame(se2Y)) //se1 intersects se2 with its left endpoint. Both edges are facing to the right.
                    {
                        var m1 = se1.Slope;
                        var m2 = se2.Slope;
                        if (m1.IsPracticallySame(m2) || m1 < m2) //if collinear or se1 is below se2
                        {
                            break; //ok to insert before (Will be marked as a duplicate event)   
                        }
                    }
                    else if (se1Y < se2Y)
                    {
                        break;
                    } //Else increment 
                    i++;
                }
                _sweepEvents.Insert(i, se1);
                return i;
            }


            public int Find(SweepEvent se)
            {
                //ToDo: Could store the position to avoid this time consuming function call
                return _sweepEvents.IndexOf(se);
            }

            public SweepEvent Item(int i)
            {
                return _sweepEvents[i];
            }
        }
        #endregion

        #region SweepEvent and OrderedSweepEventList
        //Sweep Event is used for the boolean operations.
        //We don't want to use lines, because maintaining them (and their referencesis incredibly difficult.
        //There are two sweep events for each line, one for the left edge and one for the right edge
        //Only the sweep events for the left edge are ever added to the sweep list
        private class SweepEvent
        {
            public int IndexInList { get; set; }
            public Vector2 Point { get; } //the point for this sweep event
            public bool Left { get; } //The left endpoint of the line
            public bool From { get; } //The point comes first in the path.
            public SweepEvent OtherEvent { get; set; } //The event of the other endpoint of this line
            public PolygonType PolygonType { get; } //Whether this line was part of the Subject or Clip
            public bool LeftToRight { get; }
            //represents an inside/outside transition in the its polygon tree (Suject or Clip). This occurs when the edge's "Left" has a higher X value.
            public bool OtherInOut { get; set; }
            //represents an inside/outside transition in the other polygon tree (Suject or Clip). This occurs when the edge's "Left" has a higher X value.
            public bool InResult { get; set; }
            //A bool to track which sweep events are part of the result (set depending on boolean operation).
            public SweepEvent PrevInResult { get; set; }
            //A pointer to the closest ende downwards in S that belongs to the result polgyon. Used to calculate depth and parentIDs.
            public int PositionInResult { get; set; }
            //public bool ResultInsideOut { get; set; } //The field ResultInsideOut is set to true if the right endpoint sweep event precedes the left endpoint sweepevent in the path.
            public int PathID { get; set; }
            public int ParentPathID { get; set; }
            public bool Processed { get; set; } //If this sweep event has already been processed in the sweep
            public int Depth { get; set; }
            public SweepEvent DuplicateEvent { get; set; }


            public SweepEvent(Vector2 point, bool isLeft, bool isFrom, PolygonType polyType)
            {
                Point = point;
                Left = isLeft;
                From = isFrom;
                PolygonType = polyType;
                LeftToRight = From == Left; //If both left and from, or both right and To, then LeftToRight = true;
                DuplicateEvent = null;
                _slope = new Lazy<double>(GetSlope);
            }

            //Slope as a lazy property, since it is not required for all sweep events
            private readonly Lazy<double> _slope;
            public double Slope => _slope.Value;

            private double GetSlope()
            {
                //Solve for slope and y intercept. 
                if (Point.X.IsPracticallySame(OtherEvent.Point.X)) //If vertical line, set slope = inf.
                {
                    return double.MaxValue;
                }
                if (Point.Y.IsPracticallySame(OtherEvent.Point.Y)) //If horizontal line, set slope = 0.
                {
                    return 0.0;
                }
                //Else y = mx + Yintercept
                return (OtherEvent.Point.Y - Point.Y) / (OtherEvent.Point.X - Point.X);
            }
        }

        private class OrderedSweepEventList
        {
            private readonly List<SweepEvent> _sweepEvents;

            public OrderedSweepEventList(IEnumerable<SweepEvent> sweepEvents)
            {
                _sweepEvents = new List<SweepEvent>();
                foreach (var sweepEvent in sweepEvents)
                {
                    Insert(sweepEvent);
                }
            }

            public void Insert(SweepEvent se1)
            {
                //Find the index for p1
                var i = 0;
                var breakIfNotNear = false;
                SweepEvent previousSweepEvent = null;
                foreach (var se2 in _sweepEvents)
                {
                    if (se1.Point == se2.Point) //reference is the same
                    {
                        if (se1.OtherEvent.Point.X.IsPracticallySame(se2.OtherEvent.Point.X))
                        {
                            //If the slopes are practically collinear (the point is the same, so we only need partial slopes (assume pt 1 = 0,0)
                            var m1 = se1.OtherEvent.Point.Y / se1.OtherEvent.Point.X;
                            var m2 = se2.OtherEvent.Point.Y / se2.OtherEvent.Point.X;
                            if (se1.OtherEvent.Point.Y.IsPracticallySame(se2.OtherEvent.Point.Y) || m1.IsPracticallySame(m2))
                            {
                                if (previousSweepEvent == null || previousSweepEvent.PolygonType == se2.PolygonType)
                                {
                                    break; //ok to insert before (Will be marked as a duplicate event)
                                    //If the previousSweepEvent and se2 have the same polygon type, insert se1 before se2.
                                    //This is to help with determining the result using the previous other line.
                                }
                                //Else increment and continue;                         
                            }
                            else if (se1.OtherEvent.Point.Y < se2.OtherEvent.Point.Y)
                            {
                                //Insert before se2
                                break;
                            }   //Else increment and continue;
                        }
                        //If both left endpoints, add whichever line is lower than the other
                        //To determine this, use whichever X is more left to determine a Y intercept for the other line at that x value.
                        //If the calculated Y is > or = to the OtherPoint.Y then it is considered above line with the lower x value..
                        else if (se1.Left && se2.Left)
                        {
                            double se1Y, se2Y;
                            if (!se1.OtherEvent.Point.X.IsGreaterThanNonNegligible(se2.OtherEvent.Point.X)) // <= is equivalent to !GreaterThanNonNegligible(value)
                            {
                                se1Y = se1.OtherEvent.Point.Y;
                                se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.OtherEvent.Point.X);
                            }
                            else
                            {
                                se1Y = LineIntercept(se1.Point, se1.OtherEvent.Point, se2.OtherEvent.Point.X);
                                se2Y = se2.OtherEvent.Point.Y;
                            }
                            if (se1Y < se2Y)
                            {
                                //Insert before se2
                                break;
                            }   //Else increment and continue;
                        }
                        else if (se1.OtherEvent.Point.X < se2.OtherEvent.Point.X)
                        {
                            //Insert before se2
                            break;
                        }   //Else increment and continue;
                    }
                    else if (se1.Point.X.IsPracticallySame(se2.Point.X))
                    {
                        //if (se1.Point.Y.IsPracticallySame(se2.Point.Y)) throw new NotImplementedException("Sweep Events need to be merged"); 
                        if (se1.Point.Y < se2.Point.Y) break;
                        breakIfNotNear = true;
                    }
                    else if (breakIfNotNear) break;
                    else if (se1.Point.X < se2.Point.X) break;
                    i++;
                    previousSweepEvent = se2;
                }
                _sweepEvents.Insert(i, se1);
            }
            public bool Any()
            {
                return _sweepEvents.Any();
            }

            public SweepEvent First()
            {
                return _sweepEvents.First();
            }

            public void RemoveAt(int i)
            {
                _sweepEvents.RemoveAt(i);
            }
        }
        #endregion

        #region Other Various Private Functions: PreviousInResult, LineIntercept, & IsPointOnSegment
        private static SweepEvent PreviousInResult(SweepEvent se1, IList<SweepEvent> result)
        {
            //Get the first sweep event that goes below previousSweepEvent
            var i = se1.PositionInResult;
            var se1Y = se1.Point.Y;
            while (i > 0)
            {
                i--; //Decrement
                var se2 = result[i];
                var se2Y = LineIntercept(se2.Point, se2.OtherEvent.Point, se1.Point.X);
                var tempPoint = new Vector2(se1.Point.X, se2Y);
                if (se2Y < se1Y && IsPointOnSegment(se2.Point, se2.OtherEvent.Point, tempPoint))
                {
                    return se2;
                }
            }
            return null;
        }

        private static double LineIntercept(SweepEvent se, double xval)
        {
            return LineIntercept(se.Point, se.OtherEvent.Point, xval);
        }

        private static double LineIntercept(Vector2 p1, Vector2 p2, double xval)
        {
            if (p1.X.IsPracticallySame(p2.X)) //Vertical line
            {
                //return lower value Y
                return p1.Y < p2.Y ? p1.Y : p2.Y;
            }
            if (p1.Y.IsPracticallySame(p2.Y))//Horizontal Line
            {
                return p1.Y;
            }
            //Else, find the slope and then solve for y
            var m = (p2.Y - p1.Y) / (p2.X - p1.X);
            return m * (xval - p1.X) + p1.Y;
        }

        private static bool IsPointOnSegment(Vector2 p1, Vector2 p2, Vector2 pointInQuestion)
        {
            if ((pointInQuestion.X < p1.X && pointInQuestion.X < p2.X) ||
                (pointInQuestion.X > p1.X && pointInQuestion.X > p2.X) ||
                (pointInQuestion.Y < p1.Y && pointInQuestion.Y < p2.Y) ||
                (pointInQuestion.Y > p1.Y && pointInQuestion.Y > p2.Y)) return false;
            return true;
        }
        #endregion

        //Mirrors a shape along a given direction, such that the mid line is the same for both the original and mirror
        public static List<List<Vector2>> Mirror(List<List<Vector2>> shape, Vector2 direction2D)
        {
            var mirror = new List<List<Vector2>>();
            var points = shape.SelectMany(path => path).ToList();

            MinimumEnclosure.GetLengthAndExtremePoints(points, direction2D, out var bottomPoints, out _);
            var distanceFromOriginToClosestPoint = bottomPoints[0].Dot(direction2D);
            foreach (var polygon in shape)
            {
                var newPath = new List<Vector2>();
                foreach (var point in polygon)
                {
                    //Get the distance to the point along direction2D
                    //Then subtract 2X the distance along direction2D
                    var d = point.Dot(direction2D) - distanceFromOriginToClosestPoint;
                    newPath.Add(new Vector2(point.X - direction2D.X * 2 * d, point.Y - direction2D.Y * 2 * d));
                }
                //Reverse the new path so that it retains the same CW/CCW direction of the original
                newPath.Reverse();
                mirror.Add(new List<Vector2>(newPath));
                //if (!mirror.Last().Area().IsPracticallySame(polygon.Area, Constants.BaseTolerance))
                //{
                //   throw new Exception("Areas do not match after mirroring the polygons");
                //} ********commenting out this check. It should be in unit testing - not slow down this method*** 
            }
            return mirror;
        }
    }
}
