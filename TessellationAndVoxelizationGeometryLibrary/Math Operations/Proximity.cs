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
            uvw = new[] { 0.0, 0.0, 0.0 };

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
            //ToDo: To cut down the time by 26%, avoid back transform and ToArray call. 
            //ToDo: Changing AddRange to Add First may reduce the time a bit. 
            //Use the edge function from http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.157.4621&rep=rep1&type=pdf
            //To determine if the point is inside the triangle, is closest to an edge, or closest to a corner vertex.
            //Case 1: The point is to the left of every triangle edge (it is inside the triangle).
            //        The closest point can be defined by p's coordinates.
            //Case 2: The point is to the right of only one edge. 
            //        (2a) The point is between the perpendicular lines of a line that had the point to its right.
            //        (2b) The point is between the perpendicular lines of a triangle corner.
            //        In both sub-cases, the closest point must be on this line segment, which includes the triangle corner.
            //Case 3: The point is to the right of two edges. This can have many special cases if you implement 
            //        it differently, but I strived to implement it so it had as few sub-cases as possible.
            //        (3a) The point is on a perpendicular line.
            //        The closest point must be the perpendicular line.FromPoint, which is a triangle corner. 
            //        (3b) The point is between the perpendicular lines of a line that had the point to its right.
            //        The closest point must be on this line segment.
            //        (3c) The point is between the perpendicular lines of a triangle corner.
            //        The closest point must be this triangle corner.

            //Other Notes: If the point is on a triangle edge or vertex, it will be taken care of with one of these cases.
            //It cannot ever be to the right of 3 edges, unless the triangle is a hole, which we prevent by making
            //sure the triangles are positive.

            var oldPLocation = new[] { p[0], p[1], p[2], 1.0 };
            var newPointLocation = prim.RotTransMatrixTo2D.multiply(oldPLocation);
            //Since this point is moved onto the YZ plane, set Point.X = Y' & Point.Y = Z'
            var pPrime = new[] { newPointLocation[1], newPointLocation[2] };

            var numRightEdges = 0;
            var rightLines = new List<Line>(); //Lines that have point "p" to their right.
            var tempResult = new double[] { };
            var leftLinePerpendicularLines = new List<Line>();
            foreach (var line in prim.Polygon2D.PathLines)
            {
                //If the edgeFunction == 0, then it is on the line. > 0 is to the right, and < 0 to the left.
                var edgeFunctionValue = (pPrime[0] - line.FromPoint.X) * line.dY - (pPrime[1] - line.FromPoint.Y) * line.dX;
                if (edgeFunctionValue.IsNegligible())
                {
                    //The point intersects the infinite line of this triangle edge. This does not mean it 
                    //is closest to this edge, unless it actually intersects the edge segment. Treat it as
                    //a right line and the other cases will take care of it.
                    numRightEdges++;
                    rightLines.Add(line);
                }
                else if (edgeFunctionValue > 0)
                {
                    numRightEdges++;
                    rightLines.Add(line);
                }
                // else it is to the left side.
                else
                {
                    leftLinePerpendicularLines.AddRange(prim.PerpendicularLines[line.IndexInList]);
                }
            }


            if (numRightEdges == 0)
            {
                //Case 1: The point is to the left of every triangle edge. It must be inside the triangle.
                //The closest point on the triangle has the Y, Z coordinates of pPrime's X',Y' coordinates and X == 0. 
                tempResult = new[] { 0.0, pPrime[0], pPrime[1], 1.0 };
            }
            else if (numRightEdges == 1)
            {
                //Case 2: The point is to the right of only one edge. 
                var pointOnLine = ClosestPointOnLineSegmentToPoint(rightLines[0], pPrime);
                //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 };
            }
            else if (numRightEdges == 2)
            {
                //Case 3: The point is to the right of two edges.
                //This works for one or two right lines.
                //If Left then Right, we found the correct section
                //If the two perpendiculars have the same FromPoint, then the closest point is that FromPoint
                //Else the two perpendiculars have the same Line, which is used to find the closest point.

                //First, correctly order the right lines. This will help properly order the perpendicular lines.
                if (rightLines[0].ToPoint == rightLines[1].FromPoint)
                {
                    //It is correctly ordered [CCW] (Example: AB to BC or BC to CA)
                }
                else if (rightLines[1].ToPoint == rightLines[0].FromPoint)
                {
                    //It needs to be reversed (Example: CA to AB)
                    var temp = rightLines[0];
                    rightLines[0] = rightLines[1];
                    rightLines[1] = temp;
                }
                else throw new NotImplementedException("I don't believe this can ever be the case");

                //Now get the perpendicular lines in the correct order
                //Add to start the other perpendicular line for the starting point
                //Add to the end, the other perpendicular line for the ending point
                var perpendicularLinesOfInterest = new List<Line>();
                var startPoint = rightLines[0].FromPoint;
                var endPoint = rightLines[1].ToPoint;
                perpendicularLinesOfInterest.AddRange(
                    leftLinePerpendicularLines.Where(perp => perp.FromPoint == startPoint));
                if (perpendicularLinesOfInterest.Count != 1) throw new Exception("Should only have added one line");
                foreach (var rightLine in rightLines)
                {
                    perpendicularLinesOfInterest.AddRange(prim.PerpendicularLines[rightLine.IndexInList]);
                }
                var count = perpendicularLinesOfInterest.Count;
                perpendicularLinesOfInterest.AddRange(
                    leftLinePerpendicularLines.Where(perp => perp.FromPoint == endPoint));
                if (perpendicularLinesOfInterest.Count != count + 1)
                    throw new Exception("Should only have added one line");

                Line priorPerpendicular = null;
                var priorPerpendicularHadPointToLeft = false;
                var c = 0; //0 at left line , 1 at first side right line, 2 at second side of right line, 4 at second side of second right line
                foreach (var perpendicular in perpendicularLinesOfInterest)
                {
                    bool currentPerpendicularHadPointToLeft;
                    var edgeFunctionValue = (pPrime[0] - perpendicular.FromPoint.X) * perpendicular.dY -
                                            (pPrime[1] - perpendicular.FromPoint.Y) * perpendicular.dX;
                    if (edgeFunctionValue.IsNegligible())
                    {
                        if (c == 0)
                        {
                            //If it intersects the perp of a left line, the left line is not the closest.
                            //If the shared corner vertex is the closest, then it will also intersect the perp
                            //of the right line, which would be handled below. Say it is to right, so that it 
                            //does not put it in the corner in the next iteration.
                            currentPerpendicularHadPointToLeft = false;
                        }
                        else if (c == 5) throw new Exception("I don't believe this case should ever happen.");
                        else
                        {
                            //(3a) The point is on a perpendicular line.
                            //The closest point must be the perpendicular line.FromPoint, which is a triangle corner. 
                            tempResult = new[] { 0.0, perpendicular.FromPoint[0], perpendicular.FromPoint[1], 1.0 };
                            break;
                        }
                    }
                    else if (edgeFunctionValue > 0)
                    {
                        currentPerpendicularHadPointToLeft = false;
                    }
                    else
                    {
                        currentPerpendicularHadPointToLeft = true;
                    }

                    if (priorPerpendicular != null && priorPerpendicularHadPointToLeft &&
                        !currentPerpendicularHadPointToLeft)
                    {
                        if (priorPerpendicular.FromPoint == perpendicular.FromPoint)
                        {
                            //(3c) The point is between the perpendicular lines of a triangle corner.
                            //The closest point must be this triangle corner.
                            tempResult = new[] { 0.0, perpendicular.FromPoint[0], perpendicular.FromPoint[1], 1.0 };
                        }
                        else
                        {
                            //(3b) The point is between the perpendicular lines of a line that had the point to its right.
                            //The closest point must be on this line segment.
                            var line = rightLines[(c / 2) - 1];
                            var pointOnLine = ClosestPointOnLineSegmentToPoint(line, pPrime);
                            //Since this is the YZ plane, X' corresponds to Y and Y' to Z. The X value is 0.0;
                            tempResult = new[] { 0.0, pointOnLine[0], pointOnLine[1], 1.0 };
                        }
                        break;
                    }

                    priorPerpendicular = perpendicular;
                    priorPerpendicularHadPointToLeft = currentPerpendicularHadPointToLeft;
                    c++;
                }
            }
            else
            {
                throw new Exception("There cannot be three right lines");
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
            var distanceToSegment = p.subtract(fromPoint).dotProduct(lineVector) / line.Length;

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
