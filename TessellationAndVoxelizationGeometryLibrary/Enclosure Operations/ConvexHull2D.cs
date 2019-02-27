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
using System.Runtime.CompilerServices;

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

        public static IEnumerable<PointLight> MIConvexHull2D(IList<PointLight> points, double tolerance = Constants.BaseTolerance)
        {
            //This only works on the x and y coordinates of the points and requires that the Z values be NaN. 
            var numPoints = points.Count;
            try
            {
                return double.IsNaN(tolerance) ?
                    MIConvexHull.ConvexHull.Create(points).Points :
                    MIConvexHull.ConvexHull.Create(points, tolerance).Points;
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

        public static List<PointLight> ConvexHull2D(IList<PointLight> points)
        {
            return ConvexHull2D(points, out _);
        }

        /// <summary>
        /// Returns the 2D convex hull for given list of points. The input points list is unaffected
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>
        /// List&lt;Point&gt;.
        /// </returns>
        public static List<PointLight> ConvexHull2D(IList<PointLight> points, out SortedList<double, (PointLight, double)>[] hullCands)
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
            var extremeIndices = new List<int>(new[]{ minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex });
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
                if ((nextPt.X - currentPt.X) * (prevPt.Y - currentPt.Y) + (nextPt.Y - currentPt.Y) * (currentPt.X - prevPt.X) > 0)
                    convexHullCCW.Insert(0, currentPt); //because we are counting backwards, we need to ensure that new points are added
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

            /* An array of sorted lists. As we find new candidate convex points, we store them here. The key in the
             * list is the "positionAlong" - this is used to order the nodes that
             * are found for a particular side (More on this in 23 lines). */
            hullCands = new SortedList<double, (PointLight, double)>[cvxVNum];
            /* initialize the 3 to 8 Lists s.t. members can be added below. */
            for (var j = 0; j < cvxVNum; j++) hullCands[j] = new SortedList<double, (PointLight, double)>();

            var indexOfUsedIndices = 0;

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
            double p2X = 0, p2Y = 0, p3X = 0, p3Y = 0, p4X = 0, p4Y = 0,
                p5X = 0, p5Y = 0, p6X = 0, p6Y = 0, p7X = 0, p7Y = 0;
            var v0X = p1X - p0X;
            var v0Y = p1Y - p0Y;
            double v1X, v1Y, v2X = 0, v2Y = 0, v3X = 0, v3Y = 0, v4X = 0, v4Y = 0,
                v5X = 0, v5Y = 0, v6X = 0, v6Y = 0, v7X = 0, v7Y = 0;
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

            /* Now a big loop. For each of the original vertices, check them with the 3 to 8 edges to see if they 
             * are inside or out. If they are out, add them to the proper row of the hullCands array. */
            var nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
            for (var i = 0; i < numPoints; i++)
            {
                if (indexOfUsedIndices < indicesUsed.Length && i == nextUsedIndex)
                    //in order to avoid a contains function call, we know to only check with next usedIndex in order
                    nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
                else
                {
                    var point = points[i];
                    if (AddToListAlong(hullCands[0], point, p0X, p0Y, v0X, v0Y)) continue;
                    if (AddToListAlong(hullCands[1], point, p1X, p1Y, v1X, v1Y)) continue;
                    if (AddToListAlong(hullCands[2], point, p2X, p2Y, v2X, v2Y)) continue;
                    if (cvxVNum == 3) continue;
                    if (AddToListAlong(hullCands[3], point, p3X, p3Y, v3X, v3Y)) continue;
                    if (cvxVNum == 4) continue;
                    if (AddToListAlong(hullCands[4], point, p4X, p4Y, v4X, v4Y)) continue;
                    if (cvxVNum == 5) continue;
                    if (AddToListAlong(hullCands[5], point, p5X, p5Y, v5X, v5Y)) continue;
                    if (cvxVNum == 6) continue;
                    if (AddToListAlong(hullCands[6], point, p6X, p6Y, v6X, v6Y)) continue;
                    if (cvxVNum == 7) continue;
                    if (AddToListAlong(hullCands[7], point, p7X, p7Y, v7X, v7Y)) continue;
                }
            }
            #endregion

            #region Step 3: now remove concave "zigs" from each sorted dictionary
            /* Now it's time to go through our array of sorted lists of tuples. We search backwards through
             * the current convex hull points s.t. any additions will not confuse our for-loop indexers.
             * This approach is linear over the zig-zag polyline defined by each sorted list. This linear approach
             * was defined long ago by a number of author pairs: McCallum and Avis, Tor and Middleditch (1984), or
             * Melkman (1985) */
            for (var j = cvxVNum; j > 0; j--)
            {
                if (hullCands[j - 1].Count == 1)
                    /* If there is one and only one candidate, it must be in the convex hull. Add it now. */
                    convexHullCCW.InsertRange(j, hullCands[j - 1].Select(pair => pair.Value.Item1));
                else if (hullCands[j - 1].Count > 1)
                {
                    /* a renaming for compactness and clarity */
                    var hc = new List<PointLight>();
                    /* put the known starting point as the beginning of the list.  */
                    hc.Add(convexHullCCW[j - 1]);
                    hc.AddRange(hullCands[j - 1].Select(pair => pair.Value.Item1));
                    /* put the ending point on the end of the list. Need to check if it wraps back around to 
                     * the first in the loop (hence the simple condition). */
                    if (j == cvxVNum) hc.Add(convexHullCCW[0]);
                    else hc.Add(convexHullCCW[j]);

                    /* Now starting from second from end, work backwards looks for places where the angle 
                     * between the vertices is concave (which would produce a negative value of z). */
                    var i = hc.Count - 2;
                    while (i > 0)
                    {
                        double lX = hc[i].X - hc[i - 1].X, lY = hc[i].Y - hc[i - 1].Y;
                        double rX = hc[i + 1].X - hc[i].X, rY = hc[i + 1].Y - hc[i].Y;
                        double zValue = lX * rY - lY * rX;
                        if (zValue <= 0)
                        {
                            /* remove any vertices that create concave angles. */
                            hc.RemoveAt(i);
                            /* but don't reduce k since we need to check the previous angle again. Well, 
                             * if you're back to the end you do need to reduce k (hence the line below). */
                            if (i == hc.Count - 1) i--;
                        }
                        /* if the angle is convex, then continue toward the start, k-- */
                        else i--;
                    }
                    /* for each of the remaining vertices in hullCands[i-1], add them to the convexHullCCW. 
                     * Here we insert them backwards (k counts down) to simplify the insert operation (k.e.
                     * since all are inserted @ i, the previous inserts are pushed up to i+1, i+2, etc. */
                    for (i = hc.Count - 2; i > 0; i--)
                        convexHullCCW.Insert(j, hc[i]);
                }
            }
            #endregion

            return convexHullCCW;
        }

        // this function adds the new point to the sorted list. The reason it is complicated is that
        // if it errors - it is because there are two points at the same distance along. So, we then
        // check if the new point or the existing one on the list should stay. Simply keep the one that is
        // furthest from the edge vector.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AddToListAlong(SortedList<double, (PointLight, double)> sortedList,
            PointLight newPoint, double basePointX, double basePointY, double edgeVectorX, double edgeVectorY)
        {
            var pointX = newPoint.X;
            var pointY = newPoint.Y;
            var vectorToNewPointX = pointX - basePointX;
            var vectorToNewPointY = pointY - basePointY;
            var newDxOut = vectorToNewPointX * edgeVectorY - vectorToNewPointY * edgeVectorX;
            if (newDxOut <= 0) return false;
            var newDxAlong = edgeVectorX * vectorToNewPointX + edgeVectorY * vectorToNewPointY;
            if (sortedList.ContainsKey(newDxAlong))
            {
                if (newDxOut > sortedList[newDxAlong].Item2)
                {
                    sortedList[newDxAlong] = (newPoint, newDxOut);
                }
            }
            else
            {
                sortedList.Add(newDxAlong, (newPoint, newDxOut));
            }
            return true;
        }

        public static List<PointLight> ConvexHull2D_BM(IReadOnlyList<PointLight> points)
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
            var extremeIndices = new List<int>(new[]{ minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                    maxXIndex, maxSumIndex, maxYIndex, minDiffIndex });
            var cvxVNum = 8; //in some cases, we need to reduce from this eight to a smaller set
                             // The next two loops handle this reduction from 8 to as few as 3.
                             // In the first loop, simply check if any indices are repeated. Thanks to the CCW order,
                             // any repeat indices are adjacent on the list. Start from the back of the loop and
                             // remove towards zero.
            //TODo: type 3, 3, 4, 4, 1, 1, 2, 2
            var hullCands = new List<Octant> { new OctantUpperRight(points), new OctantUpperRight(points), new OctantUpperRight(points),
                new OctantUpperRight(points), new OctantUpperRight(points), new OctantUpperRight(points), new OctantUpperLeft(points),
                new OctantUpperLeft(points) };
            for (int i = cvxVNum - 1; i >= 0; i--)
            {
                var thisExtremeIndex = extremeIndices[i];
                var nextExtremeIndex = (i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1];
                if (thisExtremeIndex == nextExtremeIndex)
                {
                    cvxVNum--;
                    extremeIndices.RemoveAt(i);
                    hullCands.RemoveAt(i);
                }
            }
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
                if ((nextPt.X - currentPt.X) * (prevPt.Y - currentPt.Y) + (nextPt.Y - currentPt.Y) * (currentPt.X - prevPt.X) > 0)
                    convexHullCCW.Insert(0, currentPt); //because we are counting backwards, we need to ensure that new points are added
                // to the front of the list
                else
                {
                    cvxVNum--;
                    extremeIndices.RemoveAt(i); //the only reason to do this is to ensure that - if the loop is to 
                    hullCands.RemoveAt(i);
                    //continue - that the vectors are made to the proper new adjacent vertices
                }
            }
            #endregion

            //Set the extreme points for the octacts
            for (var i = cvxVNum - 1; i >= 0; i--)
            {
                var currentPt = points[extremeIndices[i]];
                var nextPt = points[(i == cvxVNum - 1) ? extremeIndices[0] : extremeIndices[i + 1]];
                hullCands[i].FirstPoint = currentPt;
                hullCands[i].LastPoint = nextPt;
            }

            //Now, put each point into one of the eight boxes, defined by the eight lines
            convexHullCCW = new List<PointLight>();
            foreach (var octant in hullCands)
            {
                octant.Calc();
                convexHullCCW.AddRange(octant.HullPoints);
            }

            return convexHullCCW;
        }
    }

    public class OctantUpperRight : Octant
    {
        public OctantUpperRight(IReadOnlyList<PointLight> allPoints) : base(allPoints) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsGoodQuadrantForPoint(PointLight pt)
        {
            if (pt.X > this.RootPoint.X && pt.Y > this.RootPoint.Y)
            {
                return true;
            }

            return false;
        }

        protected override int TryAdd(double x, double y)
        {
            int indexLow = 0;
            int indexHi = HullPoints.Count - 1;

            while (indexLow != indexHi - 1)
            {
                int index = ((indexHi - indexLow) >> 1) + indexLow;

                if (x <= HullPoints[index].X && y <= HullPoints[index].Y)
                {
                    return -1; // No calc needed
                }

                if (x > HullPoints[index].X)
                {
                    indexHi = index;
                    continue;
                }

                if (x < HullPoints[index].X)
                {
                    indexLow = index;
                    continue;
                }

                if (x == HullPoints[index].X)
                {
                    if (y > HullPoints[index].Y)
                    {
                        indexLow = index;
                    }
                    else
                    {
                        return -1;
                    }
                }

                break;
            }

            if (y <= HullPoints[indexLow].Y)
            {
                return -1; // Eliminated without slope calc
            }

            return indexLow;
        }
    }

    public class OctantUpperLeft : Octant
    {
        public OctantUpperLeft(IReadOnlyList<PointLight> allPoints) : base(allPoints) { }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override bool IsGoodQuadrantForPoint(PointLight pt)
        {
            if (pt.X < this.RootPoint.X && pt.Y > this.RootPoint.Y)
            {
                return true;
            }

            return false;
        }

        protected override int TryAdd(double x, double y)
        {
            int indexLow = 0;
            int indexHi = HullPoints.Count - 1;

            while (indexLow != indexHi - 1)
            {
                int index = ((indexHi - indexLow) >> 1) + indexLow;

                if (x >= HullPoints[index].X && y <= HullPoints[index].Y)
                {
                    return -1; // No calc needed
                }

                if (x > HullPoints[index].X)
                {
                    indexHi = index;
                    continue;
                }

                if (x < HullPoints[index].X)
                {
                    indexLow = index;
                    continue;
                }

                if (x == HullPoints[index].X)
                {
                    if (y > HullPoints[index].Y)
                    {
                        indexLow = index;
                    }
                    else
                    {
                        return -1;
                    }
                }

                break;
            }

            if (y <= HullPoints[indexHi].Y)
            {
                return -1; // Eliminated without slope calc
            }

            return indexLow;

        }
    }

    public abstract class Octant
    {     
        public PointLight FirstPoint;
        public PointLight LastPoint;
        public PointLight RootPoint;

        public readonly List<PointLight> HullPoints = null;
        protected IReadOnlyList<PointLight> _listOfPoint;

        public Octant(IReadOnlyList<PointLight> listOfPoint)
        {
            _listOfPoint = listOfPoint;
        }

        // Very important the Quadrant should be always build in a way where dpiFirst has minus slope to center and dpiLast has maximum slope to center
        public Octant(IReadOnlyList<PointLight> listOfPoint, PointLight firstPoint, PointLight lastPoint)
        {
            _listOfPoint = listOfPoint;
            //HullPoints = new List<Point>(initialResultGuessSize);
            HullPoints = new List<PointLight>();
            SetRoot();
        }

        /// <summary>
        /// Initialize every values needed to extract values that are parts of the convex hull.
        /// This is where the first pass of all values is done the get maximum in every directions (x and y).
        /// </summary>
        protected void SetRoot()
        {
            RootPoint = new PointLight(LastPoint.X, FirstPoint.Y);
        }

        // ************************************************************************
        public void Calc()
        {
            if (!_listOfPoint.Any())
            {
                // There is no points at all. Hey don't try to crash me.
                return;
            }

            // Begin : General Init
            HullPoints.Add(FirstPoint);
            if (FirstPoint.Equals(LastPoint))
            {
                return; // Case where for weird distribution (like triangle or diagonal) there could be one or more quadrants without points.
            }
            HullPoints.Add(LastPoint);

            // Main Loop to extract ConvexHullPoints
            foreach (PointLight point in _listOfPoint)
            {
                if (!IsGoodQuadrantForPoint(point))
                {
                    continue;
                }

                int indexLow = TryAdd(point.X, point.Y);

                if (indexLow == -1)
                {
                    continue;
                }

                PointLight p1 = HullPoints[indexLow];
                PointLight p2 = HullPoints[indexLow + 1];

                if (!IsPointToTheRightOfOthers(p1, p2, point))
                {
                    continue;
                }

                int indexHi = indexLow + 1;

                // Find lower bound (remove point invalidate by the new one that come before)
                while (indexLow > 0)
                {
                    if (IsPointToTheRightOfOthers(HullPoints[indexLow - 1], point, HullPoints[indexLow]))
                    {
                        break; // We found the lower index limit of points to keep. The new point should be added right after indexLow.
                    }
                    indexLow--;
                }

                // Find upper bound (remove point invalidate by the new one that come after)
                int maxIndexHi = HullPoints.Count - 1;
                while (indexHi < maxIndexHi)
                {
                    if (IsPointToTheRightOfOthers(point, HullPoints[indexHi + 1], HullPoints[indexHi]))
                    {
                        break; // We found the higher index limit of points to keep. The new point should be added right before indexHi.
                    }
                    indexHi++;
                }

                if (indexLow + 1 == indexHi)
                {
                    // Insert Point
                    HullPoints.Insert(indexHi, point);
                }
                else
                {
                    HullPoints[indexLow + 1] = point;

                    // Remove any invalidated points if any
                    if (indexLow + 2 < indexHi)
                    {
                        HullPoints.RemoveRange(indexLow + 2, indexHi - indexLow - 2);
                    }
                }

            }
        }

        // ************************************************************************
        /// <summary>
        /// To know if to the right. It is meaninful when p1 is first and p2 is next.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="ptToCheck"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsPointToTheRightOfOthers(PointLight p1, PointLight p2, PointLight ptToCheck)
        {
            return ((p2.X - p1.X) * (ptToCheck.Y - p1.Y)) - ((p2.Y - p1.Y) * (ptToCheck.X - p1.X)) < 0;
        }

        // ************************************************************************
        /// <summary>
        /// Tell if should try to add and where. -1 ==> Should not add.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        protected abstract int TryAdd(double x, double y);

        // ************************************************************************
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected abstract bool IsGoodQuadrantForPoint(PointLight pt);

        // ************************************************************************

    }
}

