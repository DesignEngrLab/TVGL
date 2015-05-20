// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt
// Created          : 04-17-2015
//
// Last Modified By : Matt
// Last Modified On : 04-17-2015
// ***********************************************************************
// <copyright file="ConvexHull2D.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a 
    /// tesselated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {

        /// <summary>
        /// Returns the 2D convex hull for the 3D vertices within the plane defined by the normal, direction.
        /// The returned points are a List of points (3D double array) that represent the points as they have been
        /// converted to the 2D plane. The vertices are not altered by this function. This is a simple two-line function
        /// that first call the non-destructive "Get2DProjectionPoints" and then the overload of "ConvexHull2D".
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        public static List<double[]> ConvexHull2D(IList<IVertex> vertices, double[] direction = null)
        {
            var points2D = Get2DProjectionPoints(vertices, direction);
            return ConvexHull2D(points2D);
        }

        /// <summary>                   
        /// Returns the 2D convex hull for the x & y values of the vertices - ignoring their z coordinate. This is essentially
        /// called by the other overload of "ConvexHull2D".
        /// </summary>
        /// <param name="origVertices">The original vertices.</param>
        /// <returns>List&lt;System.Double[]&gt;.</returns>
        public static List<double[]> ConvexHull2D(IList<double[]> origVertices)
        {
            var vertices = new List<double[]>(origVertices);
            var origVNum = vertices.Count;

            #region Step 1 : Define Convex Octogon

            /* The first step is to quickly identify the three to eight vertices based on the
             * Akl-Toussaint heuristic. */
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            var maxSum = double.NegativeInfinity;
            var maxDiff = double.NegativeInfinity;
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var minSum = double.PositiveInfinity;
            var minDiff = double.PositiveInfinity;

            /* the array of extreme is comprised of: 0.minX, 1. minSum, 2. minY, 3. maxDiff, 4. MaxX, 
             * 5. MaxSum, 6. MaxY, 7. MinDiff. */
            var extremeVertices = new int[8];
            //  int[] extremeVertexIndices = new int[8]; I thought that this might speed things up. That is, to use this to RemoveAt
            // as oppoaws to the Remove in line 91, which I thought might be slow. Turns out I was wrong - plus code is more succinct
            // way.
            for (var i = 0; i < origVNum; i++)
            {
                var n = vertices[i];
                if (n[0] < minX)
                {
                    extremeVertices[0] = i;
                    minX = n[0];
                }
                if ((n[0] + n[1]) < minSum)
                {
                    extremeVertices[1] = i;
                    minSum = n[0] + n[1];
                }
                if (n[1] < minY)
                {
                    extremeVertices[2] = i;
                    minY = n[1];
                }
                if ((n[0] - n[1]) > maxDiff)
                {
                    extremeVertices[3] = i;
                    maxDiff = n[0] - n[1];
                }
                if (n[0] > maxX)
                {
                    extremeVertices[4] = i;
                    maxX = n[0];
                }
                if ((n[0] + n[1]) > maxSum)
                {
                    extremeVertices[5] = i;
                    maxSum = n[0] + n[1];
                }
                if (n[1] > maxY)
                {
                    extremeVertices[6] = i;
                    maxY = n[1];
                }
                if ((n[0] - n[1]) >= minDiff) continue;
                extremeVertices[7] = i;
                minDiff = n[0] - n[1];
            }

            /* convexHullCCW is the result of this function. It is a list of 
             * vertices found in the original vertices and ordered to make a
             * counter-clockwise loop beginning with the leftmost (minimum
             * value of X) IVertexConvHull. */
            var convexHullCCW = new List<double[]> {vertices[extremeVertices[0]]};
            for (var i = 1; i < 8; i++)
                if (extremeVertices[i] != extremeVertices[i - 1])
                    convexHullCCW.Add(vertices[extremeVertices[i]]);
            extremeVertices = extremeVertices.Distinct().OrderByDescending(x => x).ToArray();

            #endregion

            /* the following limits are used extensively in for-loop below. In order to reduce the arithmetic calls and
             * steamline the code, these are established. */
            var cvxVNum = extremeVertices.GetLength(0);
            origVNum -= cvxVNum;
            var last = cvxVNum - 1;
            for (var i = 0; i < extremeVertices.GetLength(0); i++)
                vertices.RemoveAt(extremeVertices[i]);

            #region Step 2 : Find Signed-Distance to each convex edge

            /* Of the 3 to 8 vertices identified in the convex hull, we now define a matrix called edgeUnitVectors, 
             * which includes the unit vectors of the edges that connect the vertices in a counter-clockwise loop. 
             * The first column corresponds to the X-value,and  the second column to the Y-value. Calculating this 
             * should not take long since there are only 3 to 8 members currently in hull, and it will save time 
             * comparing to all the result vertices. */
            var edgeUnitVectors = new double[cvxVNum, 2];
            double magnitude;
            for (var i = 0; i < last; i++)
            {
                edgeUnitVectors[i, 0] = (convexHullCCW[i + 1][0] - convexHullCCW[i][0]);
                edgeUnitVectors[i, 1] = (convexHullCCW[i + 1][1] - convexHullCCW[i][1]);
                magnitude = Math.Sqrt(edgeUnitVectors[i, 0]*edgeUnitVectors[i, 0] +
                                      edgeUnitVectors[i, 1]*edgeUnitVectors[i, 1]);
                edgeUnitVectors[i, 0] /= magnitude;
                edgeUnitVectors[i, 1] /= magnitude;
            }
            edgeUnitVectors[last, 0] = convexHullCCW[0][0] - convexHullCCW[last][0];
            edgeUnitVectors[last, 1] = convexHullCCW[0][1] - convexHullCCW[last][1];
            magnitude = Math.Sqrt(edgeUnitVectors[last, 0]*edgeUnitVectors[last, 0] +
                                  edgeUnitVectors[last, 1]*edgeUnitVectors[last, 1]);
            edgeUnitVectors[last, 0] /= magnitude;
            edgeUnitVectors[last, 1] /= magnitude;

            /* Originally, I was storing all the distances from the vertices to the convex hull points
             * in a big 3D matrix. This is not necessary and the storage may be difficult to handle for large
             * sets. However, I have kept these lines of code here because they could be useful in establishing
             * the voronoi sets. */
            //var signedDists = new double[2, origVNum, cvxVNum];

            /* An array of sorted dictionaries! As we find new candidate convex points, we store them here. The second
             * part of the tuple (Item2 is a double) is the "positionAlong" - this is used to order the nodes that
             * are found for a particular side (More on this in 23 lines). */
            var hullCands = new SortedList<double, double[]>[cvxVNum];
            /* initialize the 3 to 8 Lists s.t. members can be added below. */
            for (var j = 0; j < cvxVNum; j++) hullCands[j] = new SortedList<double, double[]>();

            /* Now a big loop. For each of the original vertices, check them with the 3 to 8 edges to see if they 
             * are inside or out. If they are out, add them to the proper row of the hullCands array. */
            for (var i = 0; i < origVNum; i++)
            {
                for (var j = 0; j < cvxVNum; j++)
                {
                    var b = new[]
                    {
                        vertices[i][0] - convexHullCCW[j][0],
                        vertices[i][1] - convexHullCCW[j][1]
                    };
                    //signedDists[0, k, i] = signedDistance(convexVectInfo[i, 0], convexVectInfo[i, 1], bX, bY, convexVectInfo[i, 2]);
                    //signedDists[1, k, i] = positionAlong(convexVectInfo[i, 0], convexVectInfo[i, 1], bX, bY, convexVectInfo[i, 2]);
                    //if (signedDists[0, k, i] <= 0)
                    /* Again, these lines are commented because the signedDists has been removed. This data may be useful in 
                     * other applications though. In the condition below, any signed distance that is negative is outside of the
                     * original polygon. It is only possible for the IVertexConvHull to be outside one of the 3 to 8 edges, so once we
                     * add it, we break out of the inner loop (gotta save time where we can!). */
                    if (StarMath.crossProduct2(StarMath.GetRow(j, edgeUnitVectors), b) > 0) continue;
                    hullCands[j].Add(StarMath.GetRow(j, edgeUnitVectors).dotProduct(b, 2), vertices[i]);
                    break;
                }
            }

            #endregion

            #region Step 3: now check the remaining hull candidates

            /* Now it's time to go through our array of sorted lists of tuples. We search backwards through
             * the current convex hull points s.t. any additions will not confuse our for-loop indexers. */
            for (var j = cvxVNum; j > 0; j--)
            {
                if (hullCands[j - 1].Count == 1)
                    /* If there is one and only one candidate, it must be in the convex hull. Add it now. */
                    convexHullCCW.InsertRange(j, hullCands[j - 1].Values);
                else if (hullCands[j - 1].Count > 1)
                {
                    /* If there's more than one than...Well, now comes the tricky part. Here is where the
                     * most time is spent for large sets. this is the O(N*logN) part (the previous steps
                     * were all linear). The above octagon trick was to conquer and divide the candidates. */

                    /* a renaming for compactness and clarity */
                    var hc = new List<double[]>(hullCands[j - 1].Values);

                    /* put the known starting IVertexConvHull as the beginning of the list. No need for the "positionAlong"
                     * anymore since the list is now sorted. At any rate, the positionAlong is zero. */
                    hc.Insert(0, convexHullCCW[j - 1]);
                    /* put the ending IVertexConvHull on the end of the list. Need to check if it wraps back around to 
                     * the first in the loop (hence the simple condition). */
                    if (j == cvxVNum) hc.Add(convexHullCCW[0]);
                    else hc.Add(convexHullCCW[j]);

                    /* Now starting from second from end, work backwards looks for places where the angle 
                     * between the vertices in concave (which would produce a negative value of z). */
                    var i = hc.Count - 2;
                    while (i > 0)
                    {
                        var zValue = StarMath.crossProduct2(hc[i].subtract(hc[i - 1], 2),
                            hc[i + 1].subtract(hc[i], 2));
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