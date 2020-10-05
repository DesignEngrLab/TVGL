// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    /// 
    /// </summary>
    public static class Proximity
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
        public static Vector3 ClosestVertexOnTriangleToVertex(Vector3 a, Vector3 b, Vector3 c, Vector3 p,
            out Vector3 uvw)
        {
            //UVW is the vector of the point in question (p) to the nearest point on the triangle (a,b,c), I think.
            double uvw1, uvw2;
            // degenerate triangle, singular
            if (a.Distance(b).IsNegligible() && a.Distance(c).IsNegligible())
            {
                uvw = new Vector3(1, 0, 0);
                return a;
            }

            var ab = b.Subtract(a);
            var ac = c.Subtract(a);
            var ap = p.Subtract(a);
            double d1 = ab.Dot(ap), d2 = ac.Dot(ap);

            // degenerate triangle edges
            if (a.Distance(b).IsNegligible())
            {
                var cps = ClosestVertexOnSegmentToVertex(a, c, p, out var t);
                uvw = new Vector3(1.0 - t, 0, t);
                return cps;

            }
            else if (a.Distance(c).IsNegligible() || b.Distance(c).IsNegligible())
            {
                var cps = ClosestVertexOnSegmentToVertex(a, b, p, out var t);
                uvw = new Vector3(1.0 - t, t, 0);
                return cps;
            }

            if (d1 <= 0.0 && d2 <= 0.0)
            {
                uvw = new Vector3(1, 0, 0);
                return a; // barycentric coordinates (1,0,0)
            }

            // Check if P in vertex region outside B
            var bp = p.Subtract(b);
            double d3 = ab.Dot(bp), d4 = ac.Dot(bp);
            if (d3 >= 0.0 && d4 <= d3)
            {
                uvw = new Vector3(0, 1, 0);
                return b; // barycentric coordinates (0,1,0)
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                uvw1 = d1 / (d1 - d3);
                uvw = new Vector3(1.0 - uvw1, uvw1, 0);
                return a + (ab * uvw1); // barycentric coordinates (1-v,v,0)
            }

            // Check if P in vertex region outside C
            var cp = p.Subtract(c);
            double d5 = ab.Dot(cp), d6 = ac.Dot(cp);
            if (d6 >= 0.0 && d5 <= d6)
            {
                uvw = new Vector3(0, 0, 1);
                return c; // barycentric coordinates (0,0,1)
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                uvw2 = d2 / (d2 - d6);
                uvw = new Vector3(1.0 - uvw2, 0, uvw2);
                return a + (ac * uvw2); // barycentric coordinates (1-w,0,w)
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                uvw2 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                uvw = new Vector3(0, 1.0 - uvw2, uvw2);
                return b + ((c - b) * uvw[2]); // b + uvw[2] * (c - b), barycentric coordinates (0,1-w,w)
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            var denom = 1.0 / (va + vb + vc);
            uvw2 = vc * denom;
            uvw1 = vb * denom;
            uvw = new Vector3(1.0 - uvw1 - uvw2, uvw1, uvw2);

            return a + (ab * uvw[1]) + (ac * uvw[2]);
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
        public static Vector3 ClosestVertexOnSegmentToVertex(Vector3 a, Vector3 b, Vector3 p, out double distanceToSegment)
        {
            var ab = b.Subtract(a);
            distanceToSegment = p.Subtract(a).Dot(ab);

            if (distanceToSegment <= 0.0)
            {
                // c projects outside the [a,b] interval, on the a side.
                distanceToSegment = 0.0;
                return a;
            }
            else
            {

                // always nonnegative since denom = ||ab||^2
                double denom = ab.Dot(ab);

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
                    return a + (ab * distanceToSegment); // a + (ab * t);
                }
            }
        }

        /// <summary>
        /// Gets the closest point on the line segment from the given point (p). 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector2 ClosestPointOnLineSegmentToPoint(this PolygonEdge line, Vector2 p)
        {
            //First, project the point in question onto the infinite line, getting its distance on the line from 
            //the line.FromPoint
            //There are three possible results:
            //(1) If the distance is <= 0, the infinite line intersection is outside the line segment interval, on the FromPoint side.
            //(2) If the distance is >= the line.Length, the infinite line intersection is outside the line segment interval, on the ToPoint side.
            //(3) Otherwise, the infinite line intersection is inside the line segment interval.
            var fromPoint = line.FromPoint;
            var lineVector = line.ToPoint.Coordinates - line.FromPoint.Coordinates;
            var distanceToSegment = (p - fromPoint.Coordinates).Dot(lineVector) / line.Length;

            if (distanceToSegment <= 0.0)
            {
                return fromPoint.Coordinates;
            }
            if (distanceToSegment >= line.Length)
            {
                return line.ToPoint.Coordinates;
            }
            distanceToSegment = distanceToSegment / line.Length;
            return new Vector2(fromPoint.X + lineVector.X * distanceToSegment,
                fromPoint.Y + lineVector.Y * distanceToSegment);
        }
    }
}
