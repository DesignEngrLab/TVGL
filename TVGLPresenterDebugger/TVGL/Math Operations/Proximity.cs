using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using StarMathLib;
using TVGL.SparseVoxelization;

namespace TVGL.MathOperations
{
    /// <summary>
    /// 
    /// </summary>
    public class Proximity
    {
        /// <summary>
        /// Finds the closest vertex (3D Point) on a triangle to the given vertex (p).
        /// </summary>
        public static double[] ClosestVertexOnTriangleToVertex(Triangle t, double[] p)
        {
            double[] uvw;
            return ClosestVertexOnTriangleToVertex(t.A, t.B, t.C, p, out uvw);
        }


        /// <summary>
        /// Finds the closest vertex (3D Point) on a triangle (a,b,c) to the given vertex (p).
        /// It may be one of the three given points (a,b,c), a point on the edge, 
        /// or a point on the face.
        /// </summary>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnTriangleToPoint 
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="uvw"></param>
        /// <returns></returns>
        public static double[] ClosestVertexOnTriangleToVertex(double[] a, double[] b, double[] c, double[] p,
            out double[] uvw)
        {
            //UVW is the vector of the point in question (p) to the nearest point on the triangle (a,b,c), I think.
            uvw = new[] {0.0, 0.0, 0.0 };

            // degenerate triangle, singular
            if (MiscFunctions.DistancePointToPoint(a, b).IsNegligible() && MiscFunctions.DistancePointToPoint(a, c).IsNegligible())
            {
                uvw[0] = 1.0;
                return a;
            }

            var ab = b.subtract(a);
            var ac = c.subtract(a);
            var ap = p.subtract(a);
            double d1 = ab.dotProduct(ap), d2 = ac.dotProduct(ap);

            // degenerate triangle edges
            if (MiscFunctions.DistancePointToPoint(a, b).IsNegligible())
            {
                double t;
                var cps = ClosestVertexOnSegmentToVertex(a, c, p, out t);

                uvw[0] = 1.0 - t;
                uvw[2] = t;

                return cps;

            }
            else if (MiscFunctions.DistancePointToPoint(a, c).IsNegligible() || MiscFunctions.DistancePointToPoint(b, c).IsNegligible())
            {
                double t;
                var cps = ClosestVertexOnSegmentToVertex(a, b, p, out t);
                uvw[0] = 1.0 - t;
                uvw[1] = t;
                return cps;
            }

            if (d1 <= 0.0 && d2 <= 0.0)
            {
                uvw[0] = 1.0;
                return a; // barycentric coordinates (1,0,0)
            }

            // Check if P in vertex region outside B
            var bp = p.subtract(b);
            double d3 = ab.dotProduct(bp), d4 = ac.dotProduct(bp);
            if (d3 >= 0.0 && d4 <= d3)
            {
                uvw[1] = 1.0;
                return b; // barycentric coordinates (0,1,0)
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                uvw[1] = d1 / (d1 - d3);
                uvw[0] = 1.0 - uvw[1];
                return a.add(ab.multiply(uvw[1])); // barycentric coordinates (1-v,v,0)
            }

            // Check if P in vertex region outside C
            var cp = p.subtract(c);
            double d5 = ab.dotProduct(cp), d6 = ac.dotProduct(cp);
            if (d6 >= 0.0 && d5 <= d6)
            {
                uvw[2] = 1.0;
                return c; // barycentric coordinates (0,0,1)
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                uvw[2] = d2 / (d2 - d6);
                uvw[0] = 1.0 - uvw[2];
                return a.add(ac.multiply(uvw[2])); // barycentric coordinates (1-w,0,w)
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                uvw[2] = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                uvw[1] = 1.0 - uvw[2];
                return b.add(c.subtract(b).multiply(uvw[2])); // b + uvw[2] * (c - b), barycentric coordinates (0,1-w,w)
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            var denom = 1.0 / (va + vb + vc);
            uvw[2] = vc * denom;
            uvw[1] = vb * denom;
            uvw[0] = 1.0 - uvw[1] - uvw[2];

            return a.add(ab.multiply(uvw[1]).add(ac.multiply(uvw[2])));
            //a + ab*uvw[1] + ac*uvw[2]; // = u*a + v*b + w*c , u= va*denom = 1.0-v-w
        }

        /// <summary>
        /// Gets the closest vertex (3D Point) on line segment (ab) from the given point (p). 
        /// It also returns the distance to the line segment.
        /// </summary>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnSegmentToPoint
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <param name="distanceToSegment"></param>
        /// <returns></returns>
        public static double[] ClosestVertexOnSegmentToVertex(double[] a, double[] b, double[] p, out double distanceToSegment)
        {
            var ab = b.subtract(a);
            distanceToSegment = p.subtract(a).dotProduct(ab);

            if (distanceToSegment <= 0.0)
            {
                // c projects outside the [a,b] interval, on the a side.
                distanceToSegment = 0.0;
                return a;
            }
            else
            {

                // always nonnegative since denom = ||ab||^2
                double denom = ab.dotProduct(ab);

                if (distanceToSegment >= denom)
                {
                    // c projects outside the [a,b] interval, on the b side.
                    distanceToSegment = 1.0;
                    return b;
                }
                else
                {
                    // c projects inside the [a,b] interval.
                    distanceToSegment = distanceToSegment / denom;
                    return a.add(ab.multiply(distanceToSegment)); // a + (ab * t);
                }
            }
        }

        //Returns the closest point of a triangle to a point in question. 
        //Based on http://cs.swan.ac.uk/~csmark/PDFS/1995_3D_distance_point_to_triangle
        public static double[] ClosestPointOnTriangle(double[] p, Triangle prim)
        {
            var oldPLocation = new double[]
            {
                p[0], p[1], p[2], 1.0
            };
            var newPointLocation = prim.RotTransMatrixTo2D.multiply(oldPLocation);
            var oldP = prim.RotTransMatrixTo3D.multiply(newPointLocation);
            //Since this point is moved onto the YZ plane, set Point.X = Y' & Point.Y = Z'
            var pPrime = new[] {newPointLocation[1], newPointLocation[2]};

            var onEdge = false;
            var numRightEdges = 0;
            var rightLines = new List<Line>();
            Line onThisLine = null;
            var tempResult = new double[] { };
            var closestPointFound = false;

            //If the point is to the left of every triangle edge, or on any triangle edge, then the closest
            //point is found from the point's coordinates, with Z' = 0.
            //Otherwise, it is to the right of at least one, but not more than two lines (triangle edge).
            //Foreach line that has the point to its right, check that line's perpendicular lines to see
            //if the point is between them. If it is between them, then the closest point is on that triangle line.
            
            //

            //If the point is on an edge, then find the closest point.
            //If the point is to the left of all the edges, then it is inside.
            //If it is to the right of only one edge, that edge is the closest.
            //If it is the right of two edges, it is in a corner, closest to whichever point is in the corner.
            //It cannot ever be to the right of 3 edges, unless the triangle is a hole, which we prevent.
            var priorPerpHasPointToRight = false;
            Line priorPerpLine = null;
            foreach (var line in prim.Polygon2D.PathLines)
            {
                //Use the edge function from http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.157.4621&rep=rep1&type=pdf
                var edgeFunctionValue = (pPrime[0] - line.FromPoint.X) * line.dY - (pPrime[1] - line.FromPoint.Y) * line.dX;
                if (edgeFunctionValue.IsNegligible())
                {
                    //It is exactly on the line. 
                    onEdge = true;
                    onThisLine = line;
                    break; 
                }
                else if (edgeFunctionValue > 0)
                {
                    //It is to the right side, which is outside 
                    //Check if it is within this line's perpendiculars
                    //The First Perpendicular Line is based on the Line's FromPoint, the second is based on its ToPoint.
                    var currentPerpHasPointToRight = false;
                    foreach (var perpendicular in prim.PerpendicularLines[line.IndexInList])
                    {
                        var edgeFunctionValue2 = (pPrime[0] - perpendicular.FromPoint.X) * perpendicular.dY - (pPrime[1] - perpendicular.FromPoint.Y) * perpendicular.dX;
                        if (edgeFunctionValue2.IsNegligible())
                        {
                            //It is closest to this perpendicular line's fromPoint
                            tempResult = new[] { 0.0, perpendicular.FromPoint[0], perpendicular.FromPoint[1], 1.0 };
                            closestPointFound = true;
                            break;
                        }
                        else if (edgeFunctionValue > 0)
                        {
                            currentPerpHasPointToRight = true;
                        }
                        else
                        {
                            currentPerpHasPointToRight = false;
                        }
                        if (priorPerpLine == null)
                        {
                            //This is the first perpendicular edge we have checked, so just update the prior
                            priorPerpHasPointToRight = currentPerpHasPointToRight;
                            priorPerpLine = perpendicular;
                        }
                        else if (!priorPerpHasPointToRight && currentPerpHasPointToRight)
                        {
                            //If the prior perpendicular had the point to its left and the current had it to its right, 
                            //then the point is between the current perpendicular and the prior.
                            //If they belong to the same line, use that line.
                            //Otherwise, they will share the same FromPoint

                        }
                    }

                   
                    //It is between the two perpendiculars of this line, so use this line
                    if (c == 0)
                    {
                        var pointOnLine = ClosestPointOnLineSegmentToPoint(line, pPrime);
                        //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                        tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 };
                        closestPointFound = true;
                        break;
                    }
                }
                // else it is to the left side.

                if (closestPointFound) break;
            }


            if (onEdge)
            {
                var pointOnLine = ClosestPointOnLineSegmentToPoint(onThisLine, pPrime);
                //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 }; 
            } 
            else if (numRightEdges == 0)
            {
                //The closest point on the triangle has the Y, Z coordinates of pPrime's X',Y' coordinates and X == 0. 
                tempResult = new[] { 0.0, pPrime[0], pPrime[1], 1.0 };
                //result = backRotationMatrix.multiply(tempResult).subtract(transformVector);
            }
            else if (numRightEdges == 1)
            {   
                var line = rightLines[0];
                var pointOnLine = ClosestPointOnLineSegmentToPoint(line, pPrime);
                //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 };                
            }
            else if (numRightEdges == 2)
            {
                //If it is to the right of two edges, then we need to use the perpendicular edge cases to determine
                //which edge is closest to the point.
                var closerToLineThanCorner = false;
                var foundCorner = false;
                foreach (var line in rightLines)
                {
                    var c = 0;
                    foreach (var perpendicular in prim.PerpendicularLines[line.IndexInList])
                    {
                        var edgeFunctionValue = (pPrime[0] - perpendicular.FromPoint.X) * perpendicular.dY - (pPrime[1] - perpendicular.FromPoint.Y) * perpendicular.dX;
                        if (edgeFunctionValue.IsNegligible())
                        {
                            //It is closest to this perpendicular line's fromPoint
                            tempResult = new[] { 0.0, perpendicular.FromPoint[0], perpendicular.FromPoint[1], 1.0 };
                            foundCorner = true;
                            break;
                        }
                        else if (edgeFunctionValue > 0)
                        {
                            c++;
                        }
                        else
                        {
                            c--;
                        }    
                    }
                    //It is between the two perpendiculars of this line, so use this line
                    if (c == 0)
                    {
                        var pointOnLine = ClosestPointOnLineSegmentToPoint(line, pPrime);
                        //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                        tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 };
                        closerToLineThanCorner = true;
                        break;
                    }
                    if (foundCorner) break;
                }
                if (!closerToLineThanCorner && !foundCorner)
                {
                    //It must be the shared point
                    if (rightLines[0].ToPoint == rightLines[1].FromPoint)
                    {
                        //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                        tempResult = new[] { 0.0, rightLines[0].ToPoint.X, rightLines[0].ToPoint.Y, 1.0 };
                    }
                    else
                    {
                        tempResult = new[] { 0.0, rightLines[0].FromPoint.X, rightLines[0].FromPoint.Y, 1.0 };
                    }
                }               
            }
            
            var result = prim.RotTransMatrixTo3D.multiply(tempResult);
            //allPointsOfInterest.Add(new List<double[]>() {tempResult.Take(3).ToArray(), result.Take(3).ToArray()});
            //Presenter.ShowVertexPaths(allPointsOfInterest);

            return result.Take(3).ToArray();
        }


        /// <summary>
        /// Gets the closest point on the line segment from the given point (p). 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static double[] ClosestPointOnLineSegmentToPoint(Line line, double[] p)
        {
            //First, project the point in question onto the infinite line, getting its distance on the line from 
            //the line.FromPoint
            //There are three possible results:
            //(1) If the distance is <= 0, the infinite line intersection is outside the line segment interval, on the FromPoint side.
            //(2) If the distance is >= the line.Length, the infinite line intersection is outside the line segment interval, on the ToPoint side.
            //(3) Otherwise, the infinite line intersection is inside the line segment interval.
            var fromPoint = line.FromPoint.Position2D;
            var lineVector = line.ToPoint.Position2D.subtract(line.FromPoint.Position2D);
            var distanceToSegment = p.subtract(fromPoint).dotProduct(lineVector)/line.Length;

            if (distanceToSegment <= 0.0)
            {
                return fromPoint;
            }
            if (distanceToSegment >= line.Length)
            {
                return line.ToPoint.Position2D;
            }
            distanceToSegment = distanceToSegment / line.Length;
            return fromPoint.add(lineVector.multiply(distanceToSegment));

            //var t = (lineVector[0] * (p[0] - fromPoint[0]) + lineVector[1] * (p[1] - fromPoint[1]))
            //           / (lineVector[0] * lineVector[0] + lineVector[1] * lineVector[1]);
            //var pointOnInfiniteLine = new[] { fromPoint[0] + lineVector[0] * t, fromPoint[1] + lineVector[1] * t };
            //var ap2 = MiscFunctions.SquareDistancePointToPoint(pointOnInfiniteLine, fromPoint);
            //var bp2 = MiscFunctions.SquareDistancePointToPoint(pointOnInfiniteLine, line.ToPoint.Position2D);
        }
    }
}
