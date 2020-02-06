using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using TVGL.Numerics;
using TVGL.Voxelization;

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
        /// <param name="uvw"></param>
        /// <returns></returns>
        public static Vector2 ClosestVertexOnTriangleToVertex(Vector2 a, Vector2 b, Vector2 c, Vector2 p,
            out Vector2 uvw)
        {
            //UVW is the vector of the point in question (p) to the nearest point on the triangle (a,b,c), I think.
            uvw = new[] { 0.0, 0.0, 0.0 };

            // degenerate triangle, singular
            if (MiscFunctions.DistancePointToPoint(a, b).IsNegligible() && MiscFunctions.DistancePointToPoint(a, c).IsNegligible())
            {
                uvw[0] = 1.0;
                return a;
            }

            var ab = b.subtract(a, 3);
            var ac = c.subtract(a, 3);
            var ap = p.subtract(a, 3);
            double d1 = ab.Dot(ap, 3), d2 = ac.Dot(ap, 3);

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
            var bp = p.subtract(b, 3);
            double d3 = ab.Dot(bp, 3), d4 = ac.Dot(bp, 3);
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
                return a.add(ab.multiply(uvw[1]), 3); // barycentric coordinates (1-v,v,0)
            }

            // Check if P in vertex region outside C
            var cp = p.subtract(c, 3);
            double d5 = ab.Dot(cp, 3), d6 = ac.Dot(cp, 3);
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
                return a.add(ac.multiply(uvw[2]), 3); // barycentric coordinates (1-w,0,w)
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                uvw[2] = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                uvw[1] = 1.0 - uvw[2];
                return b.add(c.subtract(b, 3).multiply(uvw[2]), 3); // b + uvw[2] * (c - b), barycentric coordinates (0,1-w,w)
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            var denom = 1.0 / (va + vb + vc);
            uvw[2] = vc * denom;
            uvw[1] = vb * denom;
            uvw[0] = 1.0 - uvw[1] - uvw[2];

            return a.add(ab.multiply(uvw[1]).add(ac.multiply(uvw[2]), 3), 3);
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
        public static Vector2 ClosestVertexOnSegmentToVertex(Vector2 a, Vector2 b, Vector2 p, out double distanceToSegment)
        {
            var ab = b.subtract(a, 3);
            distanceToSegment = p.subtract(a, 3).Dot(ab, 3);

            if (distanceToSegment <= 0.0)
            {
                // c projects outside the [a,b] interval, on the a side.
                distanceToSegment = 0.0;
                return a;
            }
            else
            {

                // always nonnegative since denom = ||ab||^2
                double denom = ab.Dot(ab, 3);

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
                    return a.add(ab.multiply(distanceToSegment), 3); // a + (ab * t);
                }
            }
        }

        /// <summary>
        /// Gets the closest point on the line segment from the given point (p). 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector2 ClosestPointOnLineSegmentToPoint(Line line, Vector2 p)
        {
            //First, project the point in question onto the infinite line, getting its distance on the line from 
            //the line.FromPoint
            //There are three possible results:
            //(1) If the distance is <= 0, the infinite line intersection is outside the line segment interval, on the FromPoint side.
            //(2) If the distance is >= the line.Length, the infinite line intersection is outside the line segment interval, on the ToPoint side.
            //(3) Otherwise, the infinite line intersection is inside the line segment interval.
            var fromPoint = line.FromPoint.Light;
            var lineVector = line.ToPoint - line.FromPoint;
            var distanceToSegment = (p - fromPoint).Dot(lineVector) / line.Length;

            if (distanceToSegment <= 0.0)
            {
                return fromPoint;
            }
            if (distanceToSegment >= line.Length)
            {
                return line.ToPoint.Light;
            }
            distanceToSegment = distanceToSegment / line.Length;
            return new Vector2(fromPoint.X + lineVector[0] * distanceToSegment,
                fromPoint.Y + lineVector[1] * distanceToSegment);
        }
    }
}
