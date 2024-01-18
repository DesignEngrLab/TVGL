/******************************************************************************
 *
 * The MIT License (MIT)
 *
 * MIConvexHull, Copyright (c) 2015 David Sehnal, Matthew Campbell
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *  
 *****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TVGL.PointCloud;

namespace TVGL
{
    public static partial class ConvexHullAlgorithm
    {
        public static List<T> CreateConvexHullMaximal<T>(this IList<T> points, out List<int> convexHullIndices,
            double tolerance = Constants.DefaultEqualityTolerance)
            where T : IVector2D, new()
        {
            var kdTree = KDTree.Create(points, Enumerable.Range(0, points.Count).ToArray());
            var convexHull = points.CreateConvexHull(out convexHullIndices);
            var usedIndices = new HashSet<int>(convexHullIndices);
            var nextEndPoint = points[^1];
            for (int i = convexHull.Count - 1; i >= 0; i++)
            {
                var currentEndPoint = convexHull[i];
                var vX = nextEndPoint.X - currentEndPoint.X;
                var vY = nextEndPoint.Y - currentEndPoint.Y;
                var vLengthSqd = vX * vX + vY * vY;
                var midPoint = new T
                {
                    X = 0.5 * (nextEndPoint.X + currentEndPoint.X),
                    Y = 0.5 * (nextEndPoint.Y + currentEndPoint.Y)
                };
                var radius = Math.Sqrt(vLengthSqd) * 0.5;
                var sortedPoints = new List<(T point, double distance, int index)>
                { (currentEndPoint, 0, convexHullIndices[i]) };
                foreach (var pointData in kdTree.FindNearest(midPoint, radius))
                {
                    if (usedIndices.Contains(pointData.Item2)) continue;
                    if (AddToListAlongMaximal(sortedPoints, pointData.Item1, pointData.Item2,
                        currentEndPoint.X, currentEndPoint.Y, vX, vY, vLengthSqd, tolerance))
                        usedIndices.Add(pointData.Item2);
                }
                for (int j = 1; j < sortedPoints.Count; j++)
                {
                    convexHull.Insert(i + j, sortedPoints[j].point);
                    convexHullIndices.Insert(i + j, sortedPoints[j].index);
                }
                nextEndPoint = currentEndPoint;
            }
            return convexHull;
        }
        public static List<Vertex2D> CreateConvexHull(this Polygon polygon, out List<int> convexHullIndices)
            => CreateConvexHull(polygon.Vertices, out convexHullIndices);
        public static List<T> CreateConvexHull<T>(this IList<T> points, out List<int> convexHullIndices)
        where T : IVector2D
        {
            // instead of calling points.Count several times, we create this variable. 
            // by the ways points is unaffected by this method
            var numPoints = points.Count;
            if (numPoints == 2)
            {
                convexHullIndices = new List<int> { 0, 1 };
                return points.ToList();
            }
            if (numPoints < 2) throw new ArgumentException("Cannot define the 2D convex hull for less than two points.");
            #region Step 1 : Define Convex Octogon

            /* The first step is to quickly identify the three to eight vertices based on the
             * Akl-Toussaint heuristic. */
            var maxX = double.NegativeInfinity;
            var maxXIndex = -1;
            var maxY = double.NegativeInfinity;
            var maxYIndex = -1;
            var minX = double.PositiveInfinity;
            var minXIndex = -1;
            var minY = double.PositiveInfinity;
            var minYIndex = -1;
            // search of all points to find the extrema. What is stored here is the position (or index) within
            // points and the value
            for (var i = 0; i < numPoints; i++)
            {
                var p = points[i];
                var x = p.X;
                var y = p.Y;
                if (x < minX || (x == minX && y < points[minXIndex].Y))
                {
                    minXIndex = i;
                    minX = x;
                }

                if (y < minY || (y == minY && x > points[maxXIndex].X))
                {
                    minYIndex = i;
                    minY = y;
                }

                if (x > maxX || (x == maxX && y > points[maxXIndex].Y))
                {
                    maxXIndex = i;
                    maxX = x;
                }

                if (y > maxY || (y == maxY && x < points[minXIndex].X))
                {
                    maxYIndex = i;
                    maxY = y;
                }
            }
            // what if all points are on a horizontal line? temporarily set to max and min Y to min X. This'll be fixed
            // in the function: FindIntermediatePointsForLongSkinny
            if (minY == maxY) minYIndex = maxYIndex = minXIndex;
            // what if all points are on a vertical line? then do the opposite
            if (minX == maxX) minXIndex = maxXIndex = minYIndex;
            //put these on a list in counter-clockwise (CCW) direction
            var extremes = new List<(T point, double distance, int index)> {
                (points[minXIndex],0, minXIndex),
                (points[minYIndex],0,minYIndex),
                (points[maxXIndex], 0, maxXIndex),
                (points[maxYIndex], 0, maxYIndex)
            };
            var cvxVNum = 4;
            //  check if any indices are repeated. Thanks to the CCW order,
            // any repeat indices are adjacent on the list. Start from the back of the loop and
            // remove towards zero.
            var j = 0;
            for (int i = cvxVNum - 1; i >= 0; i--)
            {
                var thisExtreme = extremes[i];
                var nextExtreme = extremes[j];
                if (thisExtreme.index == nextExtreme.index)
                {
                    cvxVNum--;
                    extremes.RemoveAt(i);
                }
                else j = i;
            }
            // before we check if points are on top of one another or have some round-off error issues, these
            // indices are stored and sorted numerically for use in the second half of part 2 where we go through
            // all the points a second time. 
            var indicesUsed = extremes.Select(x => x.index).OrderBy(x => x).ToArray();

            #endregion

            #region Step 2 : Create the sorted zig-zag line for each extrema edge

            /* Of the 2 to 4 vertices identified in the convex hull, ... */

            #region Set local variables for the points in the convex hull

            //This is used to limit the number of calls to convexHullCCW[] and point.X and point.Y, which 
            //can take a significant amount of time. 
            //Initialize the point locations and vectors:
            //At minimum, the convex hull must contain two points (e.g. consider three points in a near line,
            //the third point will be added later, since it was not an extreme.)
            var p0 = extremes[0].point;
            var p0X = p0.X;
            var p0Y = p0.Y;
            var p1 = extremes[1].point;
            var p1X = p1.X;
            var p1Y = p1.Y;
            var v0X = p1X - p0X;
            var v0Y = p1Y - p0Y;
            double p2X = 0, p2Y = 0, p3X = 0, p3Y = 0;
            double v1X, v1Y, v2X = 0, v2Y = 0, v3X = 0, v3Y = 0;
            //A big if statement to make sure the convex hull wraps properly, since the number of initial cvxHull points changes
            if (cvxVNum > 2)
            {
                var p2 = extremes[2].point;
                p2X = p2.X;
                p2Y = p2.Y;
                v1X = p2X - p1X;
                v1Y = p2Y - p1Y;
                if (cvxVNum > 3)
                {
                    var p3 = extremes[3].point;
                    p3X = p3.X;
                    p3Y = p3.Y;
                    v2X = p3X - p2X;
                    v2Y = p3Y - p2Y;
                    //Wrap around from 3
                    v3X = p0X - p3X;
                    v3Y = p0Y - p3Y;
                }
                else
                {
                    //Wrap around from 2
                    v2X = p0X - p2X;
                    v2Y = p0Y - p2Y;
                }
            }
            else
            {
                //Wrap around from 1
                v1X = p0X - p1X;
                v1Y = p0Y - p1Y;
            }
            var v0LengthSqd = v0X * v0X + v0Y * v0Y;
            var v1LengthSqd = v1X * v1X + v1Y * v1Y;
            var v2LengthSqd = v2X * v2X + v2Y * v2Y;
            var v3LengthSqd = v3X * v3X + v3Y * v3Y;
            #endregion

            /* An array of arrays of new convex hull points along the sides of the polygon created by the 3 or 4 points
             * above. These are to be sorted arrays and they are sorted by the distances (stored in sortedDistances) from the
             * started extrema vertex to the last. We are going to make each array really big so that we don't have to waste
             * time extending them later. The sizes array keeps the true length. */
            var sortedPoints = new (T point, double distance, int index)[cvxVNum][];
            var sizes = new int[cvxVNum];
            for (int i = 0; i < cvxVNum; i++)
            {
                sortedPoints[i] = new (T point, double distance, int index)[numPoints];
                sortedPoints[i][0] = (extremes[i].point, 0, extremes[i].index);
                sizes[i] = 1;
            }
            var indexOfUsedIndices = 0;
            var nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
            /* Now a big loop. For each of the original vertices, check them with the 3 to 8 edges to see if they 
             * are inside or out. If they are out, add them to the proper row of the hullCands array. */
            for (var i = 0; i < numPoints; i++)
            {
                if (indexOfUsedIndices < indicesUsed.Length && i == nextUsedIndex)
                    //in order to avoid a contains function call, we know to only check with next usedIndex in order
                    nextUsedIndex =
                        indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
                else
                {
                    var point = points[i];
                    var newPointX = point.X;
                    var newPointY = point.Y;
                    if (AddToListAlong(sortedPoints[0], ref sizes[0], point, i, p0X, p0Y, v0X, v0Y, v0LengthSqd))
                        continue;
                    if (AddToListAlong(sortedPoints[1], ref sizes[1], point, i, p1X, p1Y, v1X, v1Y, v1LengthSqd))
                        continue;
                    if (cvxVNum == 2) continue;
                    if (AddToListAlong(sortedPoints[2], ref sizes[2], point, i, p2X, p2Y, v2X, v2Y, v2LengthSqd))
                        continue;
                    if (cvxVNum == 3) continue;
                    AddToListAlong(sortedPoints[3], ref sizes[3], point, i, p3X, p3Y, v3X, v3Y, v3LengthSqd);
                }
            }
            #endregion

            #region Step 3: now remove concave "zigs" from each sorted dictionary

            /* Now it's time to go through our sorted arrays. We search backwards through
             * the current convex hull points s.t. any additions will not confuse our for-loop indexers.
             * This approach is linear over the zig-zag polyline defined by each sorted list. This linear approach
             * was defined long ago by a number of authors: McCallum and Avis, Tor and Middleditch (1984), or
             * Melkman (1985) */
            var convexHull = new List<T>(sortedPoints.SelectMany(edge => edge.Select(pd => pd.point)));
            convexHullIndices = new List<int>(sortedPoints.SelectMany(edge => edge.Select(pd => pd.index)));
            var nextPt = convexHull[0];
            for (int i = convexHull.Count - 1; i > 0; i--)
            {
                var currentPt = convexHull[i];
                var prevPt = convexHull[i - 1];
                var lX = currentPt.X - prevPt.X;
                var lY = currentPt.Y - prevPt.Y;
                var rX = nextPt.X - currentPt.X;
                var rY = nextPt.Y - currentPt.Y;
                double zValue = lX * rY - lY * rX;
                if (zValue < 0)
                {
                    convexHull.RemoveAt(i);
                    convexHullIndices.RemoveAt(i);
                }
                else nextPt = currentPt;
            }
            #endregion

            return convexHull;
        }

        // this function adds the new point to the sorted array. The reason it is complicated is that
        // if it errors - it is because there are two points at the same distance along. So, we then
        // check if the new point or the existing one on the list should stay. Simply keep the one that is
        // furthest from the edge vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AddToListAlong<T>((T point, double distance, int index)[] sortedPoints, ref int size,
                T newPoint, int newPtIndex, double basePointX, double basePointY,
                double edgeVectorX, double edgeVectorY, double edgeVectorLengthSqd) where T : IVector2D
        {
            var vectorToNewPointX = newPoint.X - basePointX;
            var vectorToNewPointY = newPoint.Y - basePointY;
            var newDxOut = vectorToNewPointX * edgeVectorY - vectorToNewPointY * edgeVectorX;
            if (newDxOut < 0) return false;
            var newDxAlong = edgeVectorX * vectorToNewPointX + edgeVectorY * vectorToNewPointY;
            if (newDxAlong < 0 || newDxAlong > edgeVectorLengthSqd) return false;
            int index = sortedPoints.IncreasingDxAlongBinarySearch(newDxAlong, 1, size - 1);
            if (index >= size)
            {   // if at the end, then no need to increment any other members.
                sortedPoints[index] = (newPoint, newDxAlong, newPtIndex);
                size++;
            }
            else if (newDxAlong == sortedPoints[index].distance)
            {
                //  the same key is found. In this case, we only want to keep
                // the one vertex that sticks out the farthest.
                var ptOnList = sortedPoints[index].point;
                var onListDxOut = (ptOnList.X - basePointX) * edgeVectorY - (ptOnList.Y - basePointY) * edgeVectorX;
                if (newDxOut > onListDxOut)
                    sortedPoints[index] = (newPoint, newDxAlong, newPtIndex);
            }
            else // then 1 <= index < size)
            {
                // here a new value is found. 
                // as a slight time saver, we can check the two points that will surround this new point. 
                // If it makes a concave corner then don't add it. this part is actually in the middle 
                // condition ("else if (index < size)"). We don't need to perform this check if the insertion
                // is at either at. At the beginning ("index == 0"), we still need to increment the rest of the list

                var prevPt = sortedPoints[index - 1].point;
                var nextPt = sortedPoints[index].point;
                double lX = newPoint.X - prevPt.X, lY = newPoint.Y - prevPt.Y;
                double rX = nextPt.X - newPoint.X, rY = nextPt.Y - newPoint.Y;
                double zValue = lX * rY - lY * rX;
                // if cross produce is negative
                // then don't add it.
                // also, don't add it if the point is nearly identical (again, within the tolerance) of the previous point.
                if (zValue >= 0)
                {
                    Array.Copy(sortedPoints, index, sortedPoints, index + 1, size - index);
                    sortedPoints[index] = (newPoint, newDxAlong, newPtIndex);
                    size++;
                }
                //else return false; Actually, we don't need to return false here. We can just continue on. We don't need
                // other initial extrema edges trying this one out anymore.
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AddToListAlongMaximal<T>(IList<(T point, double distance, int index)> sortedPoints,
                T newPoint, int newPtIndex, double basePointX, double basePointY,
                double edgeVectorX, double edgeVectorY, double edgeVectorLengthSqd, double tolerance) where T : IVector2D
        {
            var vectorToNewPointX = newPoint.X - basePointX;
            var vectorToNewPointY = newPoint.Y - basePointY;
            var newDxOut = vectorToNewPointX * edgeVectorY - vectorToNewPointY * edgeVectorX;
            if (newDxOut < 0) return false;
            var newDxAlong = edgeVectorX * vectorToNewPointX + edgeVectorY * vectorToNewPointY;
            if (newDxAlong < 0 || newDxAlong > edgeVectorLengthSqd) return false;
            int index = sortedPoints.IncreasingDxAlongBinarySearch(newDxAlong, 0, sortedPoints.Count - 1);
            if (index >= sortedPoints.Count) return false;
            sortedPoints.Insert(index, (newPoint, newDxAlong, newPtIndex));
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int IncreasingDxAlongBinarySearch<T>(this IList<(T point, double distance, int index)> array,
            double queryValue, int inclusiveLowIndex, int inclusiveHighIndex) where T : IVector2D
        {
            while (inclusiveLowIndex <= inclusiveHighIndex)
            {
                // try the point in the middle of the range. note the >> 1 is a bit shift to quickly divide by 2
                int i = inclusiveLowIndex + ((inclusiveHighIndex - inclusiveLowIndex) >> 1);
                var valueAtIndex = array[i].distance;
                if (queryValue == valueAtIndex) return i; //equal values could be in any order
                if (queryValue > valueAtIndex) inclusiveLowIndex = i + 1;
                else inclusiveHighIndex = i - 1;
            }
            return inclusiveLowIndex;
        }
    }
}