// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 04-17-2015
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TVGL
{
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the area of the convex hull region, given a set of convex hull points.
        /// </summary>
        /// <param name="convexHullPoints2D"></param>
        /// <returns></returns>
        public static double ConvexHull2DArea(IList<Point> convexHullPoints2D)
        {
            //Set origin point to first point in convex hull
            var point1 = convexHullPoints2D[0];
            var totalArea = 0.0;

            //Find area of triangle between first point and every triangle that can be formed from the first point.
            for (var i = 1; i < convexHullPoints2D.Count - 1; i++)
            {
                var point2 = convexHullPoints2D[i];
                var point3 = convexHullPoints2D[i + 1];
                //Reference: <http://www.mathopenref.com/coordtrianglearea.html>
                var triangleArea = 0.5 *
                                   Math.Abs(point1.X * (point2.Y - point3.Y) + point2.X * (point3.Y - point1.Y) +
                                            point3.X * (point1.Y - point2.Y));
                totalArea = totalArea + triangleArea;
            }

            return totalArea;
        }

        /// <summary>
        /// Returns the 2D convex hull for given list of points. 
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// List&lt;Point&gt;.
        /// </returns>
        public static Point[] ConvexHull2D(IList<Point> points, double tolerance = Constants.BaseTolerance)
        {
            //This only works on the x and y coordinates of the points and requires that the Z values be NaN. 
            var numPoints = points.Count;
            var cvxPoints = new Point[numPoints];
            try
            {
                if (double.IsNaN(tolerance))
                    cvxPoints = (Point[])MIConvexHull.ConvexHull.Create(points).Points;
                else cvxPoints = (Point[])MIConvexHull.ConvexHull.Create(points, tolerance).Points;
            }
            catch
            {
                Debug.WriteLine("ConvexHull2D failed on first iteration");
                try
                {
                    cvxPoints = (Point[])MIConvexHull.ConvexHull.Create(points, 0.01).Points;
                }
                catch
                {
                    throw new Exception("ConvexHull2D failed on second attempt");
                }
            }

            return cvxPoints;
        }

        public static IEnumerable<PointLight> MIConvexHull2D(IList<PointLight> points,
            double tolerance = Constants.BaseTolerance)
        {
            //This only works on the x and y coordinates of the points and requires that the Z values be NaN. 
            var numPoints = points.Count;
            try
            {
                return double.IsNaN(tolerance)
                    ? MIConvexHull.ConvexHull.Create(points).Points
                    : MIConvexHull.ConvexHull.Create(points, tolerance).Points;
            }
            catch
            {
                Debug.WriteLine("ConvexHull2D failed on first iteration");
                try
                {
                    return MIConvexHull.ConvexHull.Create(points, 0.01).Points;
                }
                catch
                {
                    throw new Exception("ConvexHull2D failed on second attempt");
                }
            }
        }

        //From https://stackoverflow.com/questions/14671206/convex-hull-library
        //Note: DList provides O(1) insertion at beginning and end, but it is complicated, so I've just used a list.
        public static List<PointLight> MonotoneChain(List<PointLight> points)
        {
            points.Sort((a, b) =>
                a.X == b.X ? a.Y.CompareTo(b.Y) : (a.X > b.X ? 1 : -1));

            // Importantly, DList provides O(1) insertion at beginning and end
            //var hull = new DList<PointLight>();
            var hull = new List<PointLight>();
            int L = 0, U = 0; // size of lower and upper hulls

            // Builds a hull such that the output polygon starts at the leftmost point.
            for (int i = points.Count - 1; i >= 0; i--)
            {
                PointLight p = points[i], p1;

                // build lower hull (at end of output list)
                while (L >= 2 && CrossProduct2D((p1 = hull.Last()).Subtract(hull[hull.Count - 2]), p.Subtract(p1)) >= 0)
                {
                    hull.RemoveAt(hull.Count - 1);
                    L--;
                }

                //hull.PushLast(p);
                hull.Add(p);
                L++;

                // build upper hull (at beginning of output list)
                while (U >= 2 && CrossProduct2D((p1 = hull.First()).Subtract(hull[1]), p.Subtract(p1)) <= 0)
                {
                    hull.RemoveAt(0);
                    U--;
                }

                if (U != 0) // when U=0, share the point added above
                    hull.Insert(0, p);
                //hull.PushFirst(p);

                U++;
                //Debug.Assert(U + L == hull.Count + 1);
            }

            hull.RemoveAt(hull.Count - 1);

            return hull;
        }

        private static double CrossProduct2D(double[] a, double[] b)
        {
            return a[0] * b[1] - a[1] * b[0];
        }


        /// <summary>
        /// Returns the 2D convex hull for given list of points. The input points list is unaffected
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// List&lt;Point&gt;.
        /// </returns>
        public static List<PointLight> ConvexHull2D(IList<PointLight> points)
        {
            // instead of calling points.Count several times, we create this variable. 
            // by the ways points is unaffected by this method
            var numPoints = points.Count;

            #region Step 1 : Define Convex Octogon

            /* The first step is to quickly identify the three to eight vertices based on the
             * Akl-Toussaint heuristic. */
            var maxX = double.NegativeInfinity;
            var maxXIndex = -1;
            var maxY = double.NegativeInfinity;
            var maxYIndex = -1;
            var maxSum = double.NegativeInfinity;
            var maxSumIndex = -1;
            var maxDiff = double.NegativeInfinity;
            var maxDiffIndex = -1;
            var minX = double.PositiveInfinity;
            var minXIndex = -1;
            var minY = double.PositiveInfinity;
            var minYIndex = -1;
            var minSum = double.PositiveInfinity;
            var minSumIndex = -1;
            var minDiff = double.PositiveInfinity;
            var minDiffIndex = -1;
            // search of all points to find the extrema. What is stored here is the position (or index) within
            // points and the value
            for (var i = 0; i < numPoints; i++)
            {
                var p = points[i];
                var x = p.X;
                var y = p.Y;
                var sum = x + y;
                var diff = x - y;
                if (x < minX)
                {
                    minXIndex = i;
                    minX = x;
                }

                if (y < minY)
                {
                    minYIndex = i;
                    minY = y;
                }

                if (x > maxX)
                {
                    maxXIndex = i;
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxYIndex = i;
                    maxY = y;
                }

                // so that's the Akl-Toussaint (to find extrema in x and y). here, we go a step 
                // further and check the sum and difference of x and y. instead of a initial convex
                // quadrilateral we have (potentially) a convex octagon. Because we are adding or substracting
                // there is a slight time penalty, but that seems to be made up in the next two parts where
                // having more sortedlists (with fewer elements each) is faster than fewer sortedlists (with more
                // elements). 
                if (sum < minSum)
                {
                    minSumIndex = i;
                    minSum = sum;
                }

                if (diff < minDiff)
                {
                    minDiffIndex = i;
                    minDiff = diff;
                }

                if (sum > maxSum)
                {
                    maxSumIndex = i;
                    maxSum = sum;
                }

                if (diff > maxDiff)
                {
                    maxDiffIndex = i;
                    maxDiff = diff;
                }
            }

            //put these on a list in counter-clockwise (CCW) direction
            var extremeIndices = new List<int>(new[]
            {
                minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex
            });
            var cvxVNum = 8; //in some cases, we need to reduce from this eight to a smaller set
            // The next two loops handle this reduction from 8 to as few as 3.
            // In the first loop, simply check if any indices are repeated. Thanks to the CCW order,
            // any repeat indices are adjacent on the list. Start from the back of the loop and
            // remove towards zero.
            for (int i = cvxVNum - 1; i >= 0; i--)
            {
                var thisExtremeIndex = extremeIndices[i];
                var nextExtremeIndex = (i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1];
                if (thisExtremeIndex == nextExtremeIndex)
                {
                    cvxVNum--;
                    extremeIndices.RemoveAt(i);
                }
            }
            if (cvxVNum == 2) throw new NotImplementedException();
            // before we check if points are on top of one another or have some round-off error issues, these
            // indices are stored and sorted numerically for use in the second half of part 2 where we go through
            // all the points a second time. 
            var indicesUsed = extremeIndices.OrderBy(x => x).ToArray();

            // create the list that is eventually returned by the function. Initially it will have the 3 to 8 extrema
            // (as is produced in the following loop).
            var convexHullCCW = new List<PointLight>();
            for (var i = cvxVNum - 1; i >= 0; i--)
            {
                // in rare cases, often due to some roundoff error, the extrema point will produce a concavity with its
                // two neighbors. Here, we check that case. If it does make a concavity we don't use it in the initial convex
                // hull (we have captured its index and will still skip it below. it will not be searched a second time).
                // counting backwards again, we grab the previous and next point and check the "cross product" to see if the 
                // vertex in convex. if it is we add it to the returned list. 
                var currentPt = points[extremeIndices[i]];
                var prevPt = points[(i == 0) ? extremeIndices[cvxVNum - 1] : extremeIndices[i - 1]];
                var nextPt = points[(i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1]];
                if ((nextPt.X - currentPt.X) * (prevPt.Y - currentPt.Y) +
                    (nextPt.Y - currentPt.Y) * (currentPt.X - prevPt.X) > 0)
                    convexHullCCW.Insert(0,
                        currentPt); //because we are counting backwards, we need to ensure that new points are added
                // to the front of the list
                else
                {
                    cvxVNum--;
                    extremeIndices.RemoveAt(i); //the only reason to do this is to ensure that - if the loop is to 
                    //continue - that the vectors are made to the proper new adjacent vertices
                }
            }

            #endregion

            #region Step 2 : Create the sorted zig-zag line for each extrema edge

            /* Of the 3 to 8 vertices identified in the convex hull, ... */

            #region Set local variables for the points in the convex hull

            //This is used to limit the number of calls to convexHullCCW[] and point.X and point.Y, which 
            //can take a significant amount of time. 
            //Initialize the point locations and vectors:
            //At minimum, the convex hull must contain two points (e.g. consider three points in a near line,
            //the third point will be added later, since it was not an extreme.)
            var p0 = convexHullCCW[0];
            var p0X = p0.X;
            var p0Y = p0.Y;
            var p1 = convexHullCCW[1];
            var p1X = p1.X;
            var p1Y = p1.Y;
            double p2X = 0,
                p2Y = 0,
                p3X = 0,
                p3Y = 0,
                p4X = 0,
                p4Y = 0,
                p5X = 0,
                p5Y = 0,
                p6X = 0,
                p6Y = 0,
                p7X = 0,
                p7Y = 0;
            var v0X = p1X - p0X;
            var v0Y = p1Y - p0Y;
            double v1X,
                v1Y,
                v2X = 0,
                v2Y = 0,
                v3X = 0,
                v3Y = 0,
                v4X = 0,
                v4Y = 0,
                v5X = 0,
                v5Y = 0,
                v6X = 0,
                v6Y = 0,
                v7X = 0,
                v7Y = 0;
            //A big if statement to make sure the convex hull wraps properly, since the number of initial cvxHull points changes
            if (cvxVNum > 2)
            {
                var p2 = convexHullCCW[2];
                p2X = p2.X;
                p2Y = p2.Y;
                v1X = p2X - p1X;
                v1Y = p2Y - p1Y;
                if (cvxVNum > 3)
                {
                    var p3 = convexHullCCW[3];
                    p3X = p3.X;
                    p3Y = p3.Y;
                    v2X = p3X - p2X;
                    v2Y = p3Y - p2Y;
                    if (cvxVNum > 4)
                    {
                        var p4 = convexHullCCW[4];
                        p4X = p4.X;
                        p4Y = p4.Y;
                        v3X = p4X - p3X;
                        v3Y = p4Y - p3Y;
                        if (cvxVNum > 5)
                        {
                            var p5 = convexHullCCW[5];
                            p5X = p5.X;
                            p5Y = p5.Y;
                            v4X = p5X - p4X;
                            v4Y = p5Y - p4Y;
                            if (cvxVNum > 6)
                            {
                                var p6 = convexHullCCW[6];
                                p6X = p6.X;
                                p6Y = p6.Y;
                                v5X = p6X - p5X;
                                v5Y = p6Y - p5Y;
                                if (cvxVNum > 7)
                                {
                                    var p7 = convexHullCCW[7];
                                    p7X = p7.X;
                                    p7Y = p7.Y;
                                    v6X = p7X - p6X;
                                    v6Y = p7Y - p6Y;
                                    //Wrap around from 7
                                    v7X = p0X - p7X;
                                    v7Y = p0Y - p7Y;
                                }
                                else //Wrap around from 6
                                {
                                    v6X = p0X - p6X;
                                    v6Y = p0Y - p6Y;
                                }
                            }
                            else //Wrap around from 5
                            {
                                v5X = p0X - p5X;
                                v5Y = p0Y - p5Y;
                            }
                        }
                        else
                        {
                            //Wrap around from 4
                            v4X = p0X - p4X;
                            v4Y = p0Y - p4Y;
                        }
                    }
                    else
                    {
                        //Wrap around from 3
                        v3X = p0X - p3X;
                        v3Y = p0Y - p3Y;
                    }
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

            #endregion

            /* An array of sorted lists. As we find new candidate convex points, we store them here. The key in the
             * list is the "positionAlong" - this is used to order the nodes that
             * are found for a particular side (More on this in 23 lines). */
            var sortedPoints = new PointLight[cvxVNum][];
            var sortedDistances = new double[cvxVNum][];
            var sizes = new int[cvxVNum];
            for (int i = 0; i < cvxVNum; i++)
            {
                sizes[i] = 0;
                sortedPoints[i] = new PointLight[numPoints];
                sortedDistances[i] = new double[numPoints];
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
                    if (AddToListAlong(sortedPoints[0], sortedDistances[0], ref sizes[0], point, newPointX, newPointY, p0X, p0Y, v0X, v0Y)) continue;
                    if (AddToListAlong(sortedPoints[1], sortedDistances[1], ref sizes[1], point, newPointX, newPointY, p1X, p1Y, v1X, v1Y)) continue;
                    if (AddToListAlong(sortedPoints[2], sortedDistances[2], ref sizes[2], point, newPointX, newPointY, p2X, p2Y, v2X, v2Y)) continue;
                    if (cvxVNum == 3) continue;
                    if (AddToListAlong(sortedPoints[3], sortedDistances[3], ref sizes[3], point, newPointX, newPointY, p3X, p3Y, v3X, v3Y)) continue;
                    if (cvxVNum == 4) continue;
                    if (AddToListAlong(sortedPoints[4], sortedDistances[4], ref sizes[4], point, newPointX, newPointY, p4X, p4Y, v4X, v4Y)) continue;
                    if (cvxVNum == 5) continue;
                    if (AddToListAlong(sortedPoints[5], sortedDistances[5], ref sizes[5], point, newPointX, newPointY, p5X, p5Y, v5X, v5Y)) continue;
                    if (cvxVNum == 6) continue;
                    if (AddToListAlong(sortedPoints[6], sortedDistances[6], ref sizes[6], point, newPointX, newPointY, p6X, p6Y, v6X, v6Y)) continue;
                    if (cvxVNum == 7) continue;
                    if (AddToListAlong(sortedPoints[7], sortedDistances[7], ref sizes[7], point, newPointX, newPointY, p7X, p7Y, v7X, v7Y)) continue;
                }
            }

            #endregion

            #region Step 3: now remove concave "zigs" from each sorted dictionary

            /* Now it's time to go through our array of sorted lists of tuples. We search backwards through
             * the current convex hull points s.t. any additions will not confuse our for-loop indexers.
             * This approach is linear over the zig-zag polyline defined by each sorted list. This linear approach
             * was defined long ago by a number of author pairs: McCallum and Avis, Tor and Middleditch (1984), or
             * Melkman (1985) */
            for (var j = cvxVNum - 1; j >= 0; j--)
            {
                var size = sizes[j];
                if (size == 1)
                    /* If there is one and only one candidate, it must be in the convex hull. Add it now. */
                    convexHullCCW.Insert(j + 1, sortedPoints[j][0]);
                else if (size > 1)
                {
                    /* a renaming for compactness and clarity */
                    var pointsAlong = new List<PointLight>();
                    /* put the known starting point as the beginning of the list.  */
                    pointsAlong.Add(convexHullCCW[j]);
                    for (int k = 0; k < size; k++)
                        pointsAlong.Add(sortedPoints[j][k]);
                    /* put the ending point on the end of the list. Need to check if it wraps back around to 
                     * the first in the loop (hence the simple condition). */
                    if (j == cvxVNum - 1) pointsAlong.Add(convexHullCCW[0]);
                    else pointsAlong.Add(convexHullCCW[j + 1]);

                    /* Now starting from second from end, work backwards looks for places where the angle 
                     * between the vertices is concave (which would produce a negative value of z). */
                    var i = size;
                    var nextPoint = (j == cvxVNum - 1) ? convexHullCCW[0] : convexHullCCW[j + 1];
                    while (i > 0)
                    {
                        //var currentPoint =
                        double lX = pointsAlong[i].X - pointsAlong[i - 1].X, lY = pointsAlong[i].Y - pointsAlong[i - 1].Y;
                        double rX = pointsAlong[i + 1].X - pointsAlong[i].X, rY = pointsAlong[i + 1].Y - pointsAlong[i].Y;
                        double zValue = lX * rY - lY * rX;
                        if (zValue <= 0)
                        {
                            /* remove any vertices that create concave angles. */
                            pointsAlong.RemoveAt(i);
                            /* but don't reduce k since we need to check the previous angle again. Well, 
                             * if you're back to the end you do need to reduce k (hence the line below). */
                            if (i == pointsAlong.Count - 1) i--;
                        }
                        /* if the angle is convex, then continue toward the start, k-- */
                        else i--;
                    }

                    /* for each of the remaining vertices in hullCands[i-1], add them to the convexHullCCW. 
                     * Here we insert them backwards (k counts down) to simplify the insert operation (k.e.
                     * since all are inserted @ i, the previous inserts are pushed up to i+1, i+2, etc. */
                    for (i = pointsAlong.Count - 2; i > 0; i--)
                        convexHullCCW.Insert(j + 1, pointsAlong[i]);
                }
            }

            #endregion

            return convexHullCCW;
        }

        // this function adds the new point to the sorted list. The reason it is complicated is that
        // if it errors - it is because there are two points at the same distance along. So, we then
        // check if the new point or the existing one on the list should stay. Simply keep the one that is
        // furthest from the edge vector.
        private static bool AddToListAlong(PointLight[] sortedPoints, double[] sortedKeys, ref int size,
            PointLight newPoint, double newPointX, double newPointY, double basePointX, double basePointY, double edgeVectorX, double edgeVectorY)
        {
            var vectorToNewPointX = newPointX - basePointX;
            var vectorToNewPointY = newPointY - basePointY;
            var newDxOut = vectorToNewPointX * edgeVectorY - vectorToNewPointY * edgeVectorX;
            if (newDxOut <= 0) return false;
            var newDxAlong = edgeVectorX * vectorToNewPointX + edgeVectorY * vectorToNewPointY;
            int index = Array.BinarySearch(sortedKeys, 0, size, newDxAlong);
            if (index >= 0)
            {
                var ptOnList = sortedPoints[index];
                var onListDxOut = (ptOnList.X - basePointX) * edgeVectorY - (ptOnList.Y - basePointY) * edgeVectorX;
                if (newDxOut > onListDxOut)
                    sortedPoints[index] = newPoint;
            }
            else
            {
                index = ~index;
                if (index < size)
                {
                    for (int i = size; i > index; i--)
                    {
                        sortedKeys[i] = sortedKeys[i - 1];
                        sortedPoints[i] = sortedPoints[i - 1];
                    }
                    //Array.Copy(sortedKeys, index, sortedKeys, index + 1, size - index);
                    //Array.Copy(sortedPoints, index, sortedPoints, index + 1, size - index);
                }
                sortedKeys[index] = newDxAlong;
                sortedPoints[index] = newPoint;
                size++;
            }
            return true;
        }

    }
}