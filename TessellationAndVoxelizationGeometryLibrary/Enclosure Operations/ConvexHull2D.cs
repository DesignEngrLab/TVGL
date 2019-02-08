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
                var x = points[i].X;
                var y = points[i].Y;
                var sum = x + y;
                var diff = x - y;
                if (x <= minX)
                {
                    minXIndex = i;
                    minX = x;
                }
                if (y <= minY)
                {
                    minYIndex = i;
                    minY = y;
                }
                if (x >= maxX)
                {
                    maxXIndex = i;
                    maxX = x;
                }
                if (y >= maxY)
                {
                    maxYIndex = i;
                    maxY = y;
                }
                // so that's the Akl-Toussaint (to find extrema in x and y). here, we go a step 
                // farther and check the sum and difference of x and y. instead of a initial convex
                // quadrilateral we have (potentially) a convex octagon. Because we are adding or substracting
                // there is a slight time penalty, but that seems to be made up in the next two parts where
                // having more sortedlist (with fewer elements) is faster than fewer sortedlists (with more
                // elements. One add issue arose due to round-off error. Since the "sum" and "diff" sometimes
                // lacked the precision of individual values. Problems were arising when just the strict inequalities
                // (i.e. "<" and ">") were used. In one case, the maxX point had a lower Y-value than the maxDiff point
                // which lead to a small but problematic concavity. The added conditions do not impact time too much
                // and restore correct convex hulls for the tested cases.
                if ((sum < minSum)
                    || (sum == minSum && x <= points[maxDiffIndex].X &&
                        y <= points[maxDiffIndex].Y))
                {
                    minSumIndex = i;
                    minSum = sum;
                }
                if ((diff < minDiff)
                    || (diff == minDiff && x <= points[maxDiffIndex].X &&
                        y >= points[maxDiffIndex].Y))
                {
                    minDiffIndex = i;
                    minDiff = diff;
                }
                if ((sum > maxSum)
                    || (sum == maxSum && x >= points[maxDiffIndex].X &&
                        y >= points[maxDiffIndex].Y))
                {
                    maxSumIndex = i;
                    maxSum = sum;
                }
                if ((diff > maxDiff)
                    || (diff == maxDiff && x >= points[maxDiffIndex].X &&
                     y <= points[maxDiffIndex].Y))
                {
                    maxDiffIndex = i;
                    maxDiff = diff;
                }
            }
            //put these on a list in CCW direction
            var extremeIndices = new[] { minXIndex, minSumIndex, minYIndex, maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex };
            // however there could be repeats. If there are they will be next to each other in the list.
            // next, we gather these 3 to 8 points as the seed of the list which is returned.
            var convexHullCCW = new List<PointLight>();
            var removalIndices = new List<int>();
            for (var i = 0; i < 8; i++)
            {
                var thisExtremeIndex = extremeIndices[i];
                var prevExtremeIndex = (i == 0) ? extremeIndices[7] : extremeIndices[i - 1];
                if (thisExtremeIndex == prevExtremeIndex)
                    // this condition is to check if there are repeats. If so, do not add them more than once
                    continue;
                var current = points[thisExtremeIndex];
                var previous = points[prevExtremeIndex];
                var dx = current.X - previous.X;
                var dy = current.Y - previous.Y;
                if (dx == 0 && dy == 0)
                {
                    removalIndices.Add(thisExtremeIndex);
                    continue;
                }
                if ((i == 7 || i == 0) && (dx > 0 || dy > 0))
                {
                    removalIndices.Add(thisExtremeIndex);
                    continue;
                }
                if ((i == 1 || i == 2) && (dx < 0 || dy > 0))
                {
                    removalIndices.Add(thisExtremeIndex);
                    continue;
                }
                if ((i == 3 || i == 4) && (dx < 0 || dy < 0))
                {
                    removalIndices.Add(thisExtremeIndex);
                    continue;
                }
                if ((i == 5 || i == 6) && (dx > 0 || dy < 0))
                {
                    removalIndices.Add(thisExtremeIndex);
                    continue;
                }
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

            // the extreme indices are sorted from least to greatest in order to prevent the following
            // loop from checking this indices again
            extremeIndices = extremeIndices.Distinct().OrderBy(index => index).ToArray();
            var indexOfExtremeIndices = 0;

            //Set local variables for the points in the convex hull
            var p0 = convexHullCCW[0];
            var p0X = p0.X;
            var p0Y = p0.Y;
            var p1 = convexHullCCW[1];
            var p1X = p1.X;
            var p1Y = p1.Y;
            var p2 = convexHullCCW[2];
            var p2X = p2.X;
            var p2Y = p2.Y;
            var p3X = double.NaN;
            var p3Y = double.NaN;
            var p4X = double.NaN;
            var p4Y = double.NaN;
            var p5X = double.NaN;
            var p5Y = double.NaN;
            var p6X = double.NaN;
            var p6Y = double.NaN;
            var p7X = double.NaN;
            var p7Y = double.NaN;

            var v0X = p1X - p0X;
            var v0Y = p1Y - p0Y;
            var v1X = p2X - p1X;
            var v1Y = p2Y - p1Y;           
            var v2X = double.NaN;
            var v2Y = double.NaN;
            var v3X = double.NaN;
            var v3Y = double.NaN;         
            var v4X = double.NaN;
            var v4Y = double.NaN;           
            var v5X = double.NaN;
            var v5Y = double.NaN;          
            var v6X = double.NaN;
            var v6Y = double.NaN;
            var v7X = double.NaN;
            var v7Y = double.NaN;
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
                                //Wrap arounf from 7
                                v7X = p0X - p7X;
                                v7Y = p0Y - p7Y;
                            }
                            else //Wrap arounf from 6
                            {
                                v6X = p0X - p6X;
                                v6Y = p0Y - p6Y;
                            }
                        }
                        else //Wrap arounf from 5
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

            /* Now a big loop. For each of the original vertices, check them with the 3 to 8 edges to see if they 
             * are inside or out. If they are out, add them to the proper row of the hullCands array. */
            for (var i = 0; i < numPoints; i++)
            {
                if (indexOfExtremeIndices < extremeIndices.Length && i == extremeIndices[indexOfExtremeIndices])
                    //in order to avoid a contains function call, we know to only check with next extremeIndex in order
                    indexOfExtremeIndices++;
                else
                {
                    var point = points[i];
                    var pointX = point.X;
                    var pointY = point.Y;

                    //cycle over the 3 to 8 edges. however, notice the break below. 
                    // once point is successfully added to one side, there is no need to check the remainder
                    var chosenJ = -1;
                    var value = -1.0;
                    while(chosenJ == -1)
                    {
                        //First check p0 to p1
                        var b_X = pointX - p0X;
                        var b_Y = pointY - p0Y;
                        if (!(v0X * b_Y - v0Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v0X * b_X + v0Y * b_Y; // GetRow + dot
                            chosenJ = 0;
                            hullCands[0].Add(value, point);
                            break;
                        }
                        // else then skip this point since it's not "to the left of" the edge
                        //P1 to P2
                        b_X = pointX - p1X;
                        b_Y = pointY - p1Y;
                        if (!(v1X * b_Y - v1Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v1X * b_X + v1Y * b_Y; // GetRow + dot
                            chosenJ = 1;
                            hullCands[1].Add(value, point);
                            break;
                        }
                        //Next line P2 to next point (either p0 or p3)    
                        b_X = pointX - p2X;
                        b_Y = pointY - p2Y;
                        if (!(v2X * b_Y - v2Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v2X * b_X + v2Y * b_Y; // GetRow + dot
                            chosenJ = 2;
                            hullCands[2].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 3) break;
                        //Next line: P3 to next point (either p0 or p4)  
                        b_X = pointX - p3X;
                        b_Y = pointY - p3Y;
                        if (!(v3X * b_Y - v3Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v3X * b_X + v3Y * b_Y; // GetRow + dot
                            chosenJ = 3;
                            hullCands[3].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 4) break;
                        //Next line: P4 to next point (either p0 or p5)  
                        b_X = pointX - p4X;
                        b_Y = pointY - p4Y;
                        if (!(v4X * b_Y - v4Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v4X * b_X + v4Y * b_Y; // GetRow + dot
                            chosenJ = 4;
                            hullCands[4].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 5) break;
                        //Next line: P5 to next point (either p0 or p6)  
                        b_X = pointX - p5X;
                        b_Y = pointY - p5Y;
                        if (!(v5X * b_Y - v5Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v5X * b_X + v5Y * b_Y; // GetRow + dot
                            chosenJ = 5;
                            hullCands[5].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 6) break;
                        //Next line: P6 to next point (either p0 or p7)  
                        b_X = pointX - p6X;
                        b_Y = pointY - p6Y;
                        if (!(v6X * b_Y - v6Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v6X * b_X + v6Y * b_Y; // GetRow + dot
                            chosenJ = 6;
                            hullCands[6].Add(value, point);
                            break;
                        }
                        if (cvxVNum == 7) break;
                        //Next line: P7 to P0 
                        b_X = pointX - p7X;
                        b_Y = pointY - p7Y;
                        if (!(v7X * b_Y - v7Y * b_X > 0)) // Cross product 2D
                        {
                            // if it is to be included "val" is rewritten as the "position along" which is the key to 
                            // the sorted dictionary
                            value = v7X * b_X + v7Y * b_Y; // GetRow + dot
                            chosenJ = 7;
                            hullCands[7].Add(value, point);
                            break;
                        }
                        break;
                    }

                    //var current = convexHullCCW[0];
                    //var pX = current.X;
                    //var pY = current.Y;
                    //for (var j = 0; j < cvxVNum; j++) //cycle over the 3 to 8 edges. however, notice the break below. 
                    //// once point is successfully added to one side, there is no need to check the remainder
                    //{
                    //    var nextIndex = (j == last) ? 0 : j + 1;
                    //    var next = convexHullCCW[nextIndex];
                    //    var nextX = next.X;
                    //    var nextY = next.Y;
                    //    var vX = nextX - pX;
                    //    var vY = nextY - pY;
                    //    var bX = pointX - pX;
                    //    var bY = pointY - pY;
                    //    double val = vX * bY - vY * bX; // Cross product 2D
                    //    pX = nextX;
                    //    pY = nextY;
                    //    if (val > 0) continue; // then skip this point since it's not "to the left of" the edge
                    //    // if it is to be included "val" is rewritten as the "position along" which is the key to 
                    //    // the sorted dictionary
                    //    val = vX * bX + vY * bY; // GetRow + dot
                    //    //hullCands[j].Add(val, point);
                    //    if (chosenJ != j) { }
                    //    if (!value.IsPracticallySame(val)) { }
                    //    break;
                    //}
                }
            }
            #endregion

            //return convexHullCCW;

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
    }
}

