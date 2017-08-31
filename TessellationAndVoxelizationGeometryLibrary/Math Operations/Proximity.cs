using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL.MathOperations
{
    /// <summary>
    /// 
    /// </summary>
    public class Proximity
    {
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
        /// <param name="vectorOfPointInQuestionToTriangle"></param>
        /// <returns></returns>
        public static double[] ClosestVertexOnTriangleToVertex(double[] a, double[] b, double[] c, double[] p,
            out double[] vectorOfPointInQuestionToTriangle)
        {
            vectorOfPointInQuestionToTriangle = new double[] { };

            // degenerate triangle, singular
            if (MiscFunctions.DistancePointToPoint(a, b).IsNegligible() && MiscFunctions.DistancePointToPoint(a, c).IsNegligible())
            {
                vectorOfPointInQuestionToTriangle[0] = 1.0;
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

                vectorOfPointInQuestionToTriangle[0] = 1.0 - t;
                vectorOfPointInQuestionToTriangle[2] = t;

                return cps;

            }
            else if (MiscFunctions.DistancePointToPoint(a, c).IsNegligible() || MiscFunctions.DistancePointToPoint(b, c).IsNegligible())
            {
                double t;
                var cps = ClosestVertexOnSegmentToVertex(a, b, p, out t);
                vectorOfPointInQuestionToTriangle[0] = 1.0 - t;
                vectorOfPointInQuestionToTriangle[1] = t;
                return cps;
            }

            if (d1 <= 0.0 && d2 <= 0.0)
            {
                vectorOfPointInQuestionToTriangle[0] = 1.0;
                return a; // barycentric coordinates (1,0,0)
            }

            // Check if P in vertex region outside B
            var bp = p.subtract(b);
            double d3 = ab.dotProduct(bp), d4 = ac.dotProduct(bp);
            if (d3 >= 0.0 && d4 <= d3)
            {
                vectorOfPointInQuestionToTriangle[1] = 1.0;
                return b; // barycentric coordinates (0,1,0)
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                vectorOfPointInQuestionToTriangle[1] = d1 / (d1 - d3);
                vectorOfPointInQuestionToTriangle[0] = 1.0 - vectorOfPointInQuestionToTriangle[1];
                return a.add(ab.multiply(vectorOfPointInQuestionToTriangle[1])); // barycentric coordinates (1-v,v,0)
            }

            // Check if P in vertex region outside C
            var cp = p.subtract(c);
            double d5 = ab.dotProduct(cp), d6 = ac.dotProduct(cp);
            if (d6 >= 0.0 && d5 <= d6)
            {
                vectorOfPointInQuestionToTriangle[2] = 1.0;
                return c; // barycentric coordinates (0,0,1)
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                vectorOfPointInQuestionToTriangle[2] = d2 / (d2 - d6);
                vectorOfPointInQuestionToTriangle[0] = 1.0 - vectorOfPointInQuestionToTriangle[2];
                return a.add(ac.multiply(vectorOfPointInQuestionToTriangle[2])); // barycentric coordinates (1-w,0,w)
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                vectorOfPointInQuestionToTriangle[2] = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                vectorOfPointInQuestionToTriangle[1] = 1.0 - vectorOfPointInQuestionToTriangle[2];
                return b.add(c.subtract(b).multiply(vectorOfPointInQuestionToTriangle[2])); // b + uvw[2] * (c - b), barycentric coordinates (0,1-w,w)
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            var denom = 1.0 / (va + vb + vc);
            vectorOfPointInQuestionToTriangle[2] = vc * denom;
            vectorOfPointInQuestionToTriangle[1] = vb * denom;
            vectorOfPointInQuestionToTriangle[0] = 1.0 - vectorOfPointInQuestionToTriangle[1] - vectorOfPointInQuestionToTriangle[2];

            return a.add(ab.multiply(vectorOfPointInQuestionToTriangle[1]).add(ac.multiply(vectorOfPointInQuestionToTriangle[2])));
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
    }
}
