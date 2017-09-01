using System;
using System.Collections.Generic;
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

        //Returns the squared distance from a point to a triangle. Take the square root to find the actual distance.
        public static double SquareDistancesPointToTriangle(double[] p, Triangle prim)
        {   
            //note:precomput this.
            //Calculate the translation and rotation matrices so that A lies on the origin, B, 
            //lies on the Y axis, and C lies in the xy plane.
            var xDir = prim.B[0] - prim.A[0];
            var yDir = prim.B[1] - prim.A[1];
            var zDir = prim.B[2] - prim.A[2];
            var originToB = Math.Sqrt(xDir *xDir + yDir*yDir + zDir*zDir);

            //Rotate Z, then X, then Y
            double[,] rotateX, rotateY, rotateZ, backRotateZ, backRotateX, backRotateY;
            //if (xDir.IsNegligible() && zDir.IsNegligible())
            //{
            //    rotateX = StarMath.RotationX(Math.Sign(yDir) * Math.PI / 2, true);
            //    backRotateX = StarMath.RotationX(-Math.Sign(yDir) * Math.PI / 2, true);
            //    backRotateY = rotateY = StarMath.makeIdentity(4);
            //}
            //else if (zDir.IsNegligible())
            //{
            //    rotateY = StarMath.RotationY(-Math.Sign(xDir) * Math.PI / 2, true);
            //    backRotateY = StarMath.RotationY(Math.Sign(xDir) * Math.PI / 2, true);
            //    var rotXAngle = Math.Atan(yDir / Math.Abs(xDir));
            //    rotateX = StarMath.RotationX(rotXAngle, true);
            //    backRotateX = StarMath.RotationX(-rotXAngle, true);
            //}
            //else
            //{

            var rotZAngle = -Math.Atan(xDir / yDir);
            rotateZ = StarMath.RotationY(rotZAngle, true);
            backRotateZ = StarMath.RotationY(-rotZAngle, true);

            var rotXAngle = Math.Sign(zDir) * Math.Asin(zDir / originToB);
            rotateX = StarMath.RotationX(rotXAngle, true);
            backRotateX = StarMath.RotationX(-rotXAngle, true);

            //At this point, C is very difficult to determine. Just do the rotation first
            var tempR = rotateX.multiply(rotateZ);
            var tempC = tempR.multiply(prim.C);
            var rotYAngle = -Math.Atan(tempC[1] / tempC[2]);
            rotateY = StarMath.RotationY(rotYAngle, true);
            backRotateY = StarMath.RotationY(-rotYAngle, true);        
            //}

            var rotationTransform = rotateY.multiply(rotateX.multiply(rotateZ));
            var transformationMatrix = new[,]
            {
                {rotationTransform[0,0], rotationTransform[0,1], rotationTransform[0,2], -prim.A[0]},
                {rotationTransform[1,0], rotationTransform[1,1], rotationTransform[1,2], -prim.A[1]},
                {rotationTransform[2,0], rotationTransform[2,1], rotationTransform[2,2], -prim.A[2]},
                {0.0, 0.0, 0.0, 1.0}
            };
            
            //Rotate all the points. A is at 0,0,0.
            var oldVertexPosition = new double[]
            {
                p[0], p[1], p[2], 1.0
            };
            //We can ignore this point's Z coordinate to put it on the XY plane
            var newPLocation = transformationMatrix.multiply(oldVertexPosition);
            var pPrime = new Point(newPLocation[2], newPLocation[1]);

            //ZY plane
            var aPrime = new Point(0.0, 0.0);
            var oldBPosition = new[]
            {
                prim.B[0], prim.B[1], prim.B[2], 1.0
            };
            var newBLocation = transformationMatrix.multiply(oldBPosition);
            if(!newBLocation[2].IsNegligible()) throw new Exception("Point B should be on the Y axis, and have Z = 0");
            var bPrime = new Point(newBLocation[2], newBLocation[1]);
            var oldCPosition = new[]
{
                prim.C[0], prim.C[1], prim.C[2], 1.0
            };
            var newCLocation = transformationMatrix.multiply(oldCPosition);
            var cPrime = new Point(newCLocation[2], newCLocation[1]);

            //If the 2D version of the new point is inside the new 2D triangle,
            //Then the distance is simply its X value.
            if (MiscFunctions.IsPointInsidePolygon(new List<Point>() {aPrime, bPrime, cPrime}, pPrime))
            {
                return newPLocation[0];
            }

            //if not inside, then check edges.

            //If it is to the right of only one edge, that edge is the closest.
            //If it is the right of two edges, it is in a corner, closest to whichever point is in the corner.
            //It cannot ever be to the right of 3 edges, unless the triangle is a hole, which we prevent.


            return 0.0;
        }
    }
}
