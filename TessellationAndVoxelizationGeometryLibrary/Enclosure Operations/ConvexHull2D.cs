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

        /// <summary>
        /// Returns the 2D convex hull for given list of points. 
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
                var x = points[i].X;
                var y = points[i].Y;
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
            var extremeIndices = new[] { minXIndex, minSumIndex,minYIndex,maxDiffIndex,
                maxXIndex, maxSumIndex, maxYIndex, minDiffIndex };
            // however there could be repeats. If there are they will be next to each other in the list.
            // next, we gather these 3 to 8 points as the seed of the list which is returned.
            var convexHullCCW = new List<PointLight>();
            for (var i = 0; i < extremeIndices.Length; i++)
            {
                var index = extremeIndices[i];
                if (i > 0 && extremeIndices[i] == extremeIndices[i - 1])
                    // this curious condition is to check if there are repeats. If so, do not add them more than once
                    continue;
                convexHullCCW.Add(points[index]);
            }
            #endregion

            /* the following limits are used extensively in for-loop below. In order to reduce the arithmetic calls and
             * steamline the code, these are established. */
            var cvxVNum = convexHullCCW.Count;
            var last = cvxVNum - 1;

            #region Step 2 : Create the sorted zig-zag line for each extrema edge
            /* Of the 3 to 8 vertices identified in the convex hull, we now define a matrix called edgeUnitVectors, 
             * which includes the unit vectors of the edges that connect the vertices in a counter-clockwise loop. 
             * The first column corresponds to the X-value,and  the second column to the Y-value. Calculating this 
             * should not take long since there are only 3 to 8 members currently in hull, and it will save time 
             * comparing to all the result vertices. */
            var edgeUnitVectors = new double[cvxVNum, 2];
            double magnitude;
            for (var i = 0; i < last; i++)
            {
                edgeUnitVectors[i, 0] = (convexHullCCW[i + 1].X - convexHullCCW[i].X);
                edgeUnitVectors[i, 1] = (convexHullCCW[i + 1].Y - convexHullCCW[i].Y);
                magnitude = Math.Sqrt(edgeUnitVectors[i, 0] * edgeUnitVectors[i, 0] +
                                      edgeUnitVectors[i, 1] * edgeUnitVectors[i, 1]);
                edgeUnitVectors[i, 0] /= magnitude;
                edgeUnitVectors[i, 1] /= magnitude;
            }
            edgeUnitVectors[last, 0] = convexHullCCW[0].X - convexHullCCW[last].X;
            edgeUnitVectors[last, 1] = convexHullCCW[0].Y - convexHullCCW[last].Y;
            magnitude = Math.Sqrt(edgeUnitVectors[last, 0] * edgeUnitVectors[last, 0] +
                                  edgeUnitVectors[last, 1] * edgeUnitVectors[last, 1]);
            edgeUnitVectors[last, 0] /= magnitude;
            edgeUnitVectors[last, 1] /= magnitude;

            /* An array of sorted lists. As we find new candidate convex points, we store them here. The key in the
             * list is the "positionAlong" - this is used to order the nodes that
             * are found for a particular side (More on this in 23 lines). */
            var hullCands = new SortedList<double, PointLight>[cvxVNum];
            /* initialize the 3 to 8 Lists s.t. members can be added below. */
            for (var j = 0; j < cvxVNum; j++) hullCands[j] = new SortedList<double, PointLight>(new NoEqualSort());

            // the extreme indices are sorted from least to greatest in order to prevent the following
            // loop from checking this indices again
            extremeIndices = extremeIndices.Distinct().OrderBy(index => index).ToArray();
            var indexOfExtremeIndices = 0;
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
                    for (var j = 0; j < cvxVNum; j++) //cycle over the 3 to 8 edges. however, notice the break below. 
                    // once point is successfully added to one side, there is no need to check the remainder
                    {
                        var b = new[] { point.X - convexHullCCW[j].X, point.Y - convexHullCCW[j].Y };
                        double val = edgeUnitVectors[j, 0] * b[1] - edgeUnitVectors[j, 1] * b[0]; // Cross product 2D 
                        if (val > 0) continue; // then skip this point since it's not "to the left of" the edge
                        // if it is to be included "val" is rewritten as the "position along" which is the key to 
                        // the sorted dictionary
                        val = edgeUnitVectors[j, 0] * b[0] + edgeUnitVectors[j, 1] * b[1]; // GetRow + dot
                        hullCands[j].Add(val, point);
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
                    var hc = new List<PointLight>(hullCands[j - 1].Values);

                    /* put the known starting point as the beginning of the list. No need for the "positionAlong"
                     * anymore since the list is now sorted. At any rate, the positionAlong is zero. */
                    hc.Insert(0, convexHullCCW[j - 1]);
                    /* put the ending IVertexConvHull on the end of the list. Need to check if it wraps back around to 
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
                        if (zValue < 0)
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

