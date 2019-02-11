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
using StarMathLib;

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
        public static List<PointLight> ConvexHull2D(IList<PointLight> points, out SortedList<double, PointLight>[] hullCands)
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
                // farther and check the sum and difference of x and y. instead of a initial convex
                // quadrilateral we have (potentially) a convex octagon. Because we are adding or substracting
                // there is a slight time penalty, but that seems to be made up in the next two parts where
                // having more sortedlist (with fewer elements) is faster than fewer sortedlists (with more
                // elements. 
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
            //put these on a list in CCW direction
            var extremeIndices = new[] { minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex };
            // Gather these 3 to 8 points as the seed of the list which is returned.
            // However there could be repeats. In this first loop, check if the Sum and Diff
            // points are contributing to the convex hull, not just considered because of round-off error.
            // In the next for loop, we will consider if the other extreme points are the same.
            var tempConvexHull = new List<PointLight>();
            for (var i = 0; i < 8; i++)
            {
                var thisExtremeIndex = extremeIndices[i];
                var prevExtremeIndex = (i == 0) ? extremeIndices[7] : extremeIndices[i - 1];
                var nextExtremeIndex = (i == 7) ? extremeIndices[0] : extremeIndices[i + 1];
                if (i % 2 == 1 && thisExtremeIndex == prevExtremeIndex)
                    // this condition is to check if there are repeats. If so, do not add them more than once
                    continue;
                var previous = points[prevExtremeIndex];
                var current = points[thisExtremeIndex];
                var next = points[nextExtremeIndex];
                // due to repeated points and round-off errors, we now check to see that the consecutive points
                // and indeed following CCW in convex hull order. If points are on top of each other or there
                // is a slight round-off error (usually due to the sum or diff terms above having a slightly
                // reduced precision), then it has been observed that a concave edge is produced. This ruins
                // the remainder of checks.
                // For this problem, check that the Sum and Diff points did not get added because of round-off (and order),
                // but because they are actually better. If the point is practically the same, it provides us no benefit. Ignore it.
                var previousSum = previous.X + previous.Y;
                var currentSum = current.X + current.Y;
                var previousDiff = previous.X - previous.Y;
                var currentDiff = current.X - current.Y;
                var nextSum = next.X + next.Y;
                var nextDiff = next.X - next.Y; 
                if ((i == 1 || i == 5) && (currentSum.IsPracticallySame(previousSum) || currentSum.IsPracticallySame(nextSum))) continue;            
                if ((i == 3 || i == 7) && (currentDiff.IsPracticallySame(previousDiff) || currentDiff.IsPracticallySame(nextDiff))) continue;               
                tempConvexHull.Add(current);
            }
            //Consider whether any of the remaining points are the same
            var n = tempConvexHull.Count;
            var convexHullCCW = new List<PointLight>();
            for (var i = 0; i < n; i++)
            {
                var current = tempConvexHull[i];
                var previous = (i == 0) ? tempConvexHull[n - 1] : tempConvexHull[i - 1];
                if (previous.X == current.X && previous.Y == current.Y) continue;
                convexHullCCW.Add(current);
            }

            #endregion

            /* the following limits are used extensively in for-loop below. In order to reduce the arithmetic calls and
             * steamline the code, these are established. */
            var cvxVNum = convexHullCCW.Count;

            #region Step 2 : Create the sorted zig-zag line for each extrema edge
            /* Of the 3 to 8 vertices identified in the convex hull, ... */

            /* An array of sorted lists. As we find new candidate convex points, we store them here. The key in the
             * list is the "positionAlong" - this is used to order the nodes that
             * are found for a particular side (More on this in 23 lines). */
            hullCands = new SortedList<double, PointLight>[cvxVNum];
            /* initialize the 3 to 8 Lists s.t. members can be added below. */
            for (var j = 0; j < cvxVNum; j++) hullCands[j] = new SortedList<double, PointLight>(new NoEqualSort());

            // The used indices are sorted from least to greatest in order to prevent the following
            // loop from checking this indices again
            var noDuplicates = new HashSet<int>(extremeIndices); //gets rid of duplicates
            var indicesUsed = new List<int>(noDuplicates.OrderBy(index => index));
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
                if (indexOfUsedIndices < indicesUsed.Count && i == nextUsedIndex)
                {
                    //in order to avoid a contains function call, we know to only check with next usedIndex in order
                    nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
                }      
                else
                {
                    var point = points[i];
                    var pointX = point.X;
                    var pointY = point.Y;
                    //if (pointX.IsPracticallySame(6.01, 0.01) && pointY.IsPracticallySame(34.37, 0.01)) { }
                    //cycle over the 3 to 8 edges. however, notice the break below. 
                    // once point is successfully added to one side, there is no need to check the remainder
                    while(true)
                    {
                        //First check p0 to p1
                        var bX = pointX - p0X;
                        var bY = pointY - p0Y;
                        if ((v0X * bY - v0Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v0X * bX + v0Y * bY; // GetRow + dot
                            hullCands[0].Add(value, point);
                            break;
                        }
                        //P1 to P2
                        bX = pointX - p1X;
                        bY = pointY - p1Y;
                        if ((v1X * bY - v1Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v1X * bX + v1Y * bY; // GetRow + dot
                            hullCands[1].Add(value, point);
                            break;
                        }
                        //Next line P2 to next point (either p0 or p3)    
                        bX = pointX - p2X;
                        bY = pointY - p2Y;
                        if ((v2X * bY - v2Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v2X * bX + v2Y * bY; // GetRow + dot
                            hullCands[2].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 3) break;
                        //Next line: P3 to next point (either p0 or p4)  
                        bX = pointX - p3X;
                        bY = pointY - p3Y;
                        if ((v3X * bY - v3Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v3X * bX + v3Y * bY; // GetRow + dot
                            hullCands[3].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 4) break;
                        //Next line: P4 to next point (either p0 or p5)  
                        bX = pointX - p4X;
                        bY = pointY - p4Y;
                        if ((v4X * bY - v4Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v4X * bX + v4Y * bY; // GetRow + dot
                            hullCands[4].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 5) break;
                        //Next line: P5 to next point (either p0 or p6)  
                        bX = pointX - p5X;
                        bY = pointY - p5Y;
                        if ((v5X * bY - v5Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v5X * bX + v5Y * bY; // GetRow + dot
                            hullCands[5].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 6) break;
                        //Next line: P6 to next point (either p0 or p7)  
                        bX = pointX - p6X;
                        bY = pointY - p6Y;
                        if ((v6X * bY - v6Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v6X * bX + v6Y * bY; // GetRow + dot
                            hullCands[6].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 7) break;
                        //Next line: P7 to P0 
                        bX = pointX - p7X;
                        bY = pointY - p7Y;
                        if ((v7X * bY - v7Y * bX).IsLessThanNonNegligible()) // Cross product 2D
                        {
                            var value = v7X * bX + v7Y * bY; // GetRow + dot
                            hullCands[7].Add(value, point);
                            break;
                        }
                        break;
                    }
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
                    convexHullCCW.InsertRange(j, hullCands[j - 1].Values);
                else if (hullCands[j - 1].Count > 1)
                {
                    /* a renaming for compactness and clarity */
                    var hc = new List<PointLight>();
                    /* put the known starting point as the beginning of the list.  */
                    hc.Add(convexHullCCW[j - 1]);
                    hc.AddRange(hullCands[j - 1].Values);
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
                        if (!zValue.IsGreaterThanNonNegligible(Constants.LineSlopeTolerance)) // <= 0
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
    }
}

