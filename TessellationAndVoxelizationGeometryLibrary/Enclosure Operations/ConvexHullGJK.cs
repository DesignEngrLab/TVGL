using ClipperLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace TVGL
{
    public static partial class ConvexHull
    {
        /// <summary>
        /// Finds the distance between two convex hulls. A positive value is the shortest distance
        /// between the solids, a negative value means the solids overlap. This implementation is
        /// not accurate for negative values.
        /// </summary>
        /// <param name="cvxHullA">The convex hull points for 1.</param>
        /// <param name="cvxHullB">The  convex hull points 2.</param>
        /// <param name="other">The other convex hull points.</param>        
        /// <param name="v">The vector,v, from the subject object to the other object.</param>
        /// <returns>True is convex hulls overlap, false if they don't.</returns>
        public static bool DistanceBetween<T, U>(this IList<T> cvxHullA, IList<U> cvxHullB, out Vector3 direction)
            where T : IVector3D
            where U : IVector3D
        {
            var simplex = new List<Vector3>(4);
            direction = new Vector3(cvxHullA[0].X - cvxHullB[0].X,
                cvxHullA[0].Y - cvxHullB[0].Y, cvxHullA[0].Z - cvxHullB[0].Z);
            if (direction.IsNegligible()) direction = Vector3.UnitX;
            while (true)
            {
                if (direction.IsNegligible() ) return false;
                var supportVector = GetSupportVector(direction, cvxHullA, cvxHullB, out var distance);
                if (distance <= 0)
                {
                    return false;
                }
                simplex.Add(supportVector);
                if (UpdateSimplex(simplex, ref direction)) //, out distance))
                {
                    return true;
                }
            }
        }

        private static bool UpdateSimplex(List<Vector3> simplex, ref Vector3 direction) //, out double newDistance)
        {
            var numVertices = simplex.Count;
            if (numVertices == 4)
                return Tetrahedron(simplex, ref direction);
            if (numVertices == 3)
                return Triangle(simplex, ref direction);
            if (numVertices == 2)
                return Line(simplex, ref direction);
            else //if (numVertices == 1)
            {
                direction = -simplex[0];
                return false;
            }
        }

        private static bool Tetrahedron(List<Vector3> points, ref Vector3 direction)
        {
            var a = points[3];
            var b = points[2];
            var c = points[1];
            var d = points[0];

            var ab = b - a;
            var ac = c - a;
            var ad = d - a;

            var abc =ab.Cross(ac);
            var acd =ac.Cross(ad);
            var adb =ad.Cross(ab);

            if (abc.Dot(a)<0)
            {
                points.RemoveAt(0);
                return Triangle(points, ref direction);
            }

            if (acd.Dot(a)<0)
            {
                points.RemoveAt(2);
                return Triangle(points ,ref direction);
            }

            if (adb.Dot(a)<0)
            {
                points[0] = b;
                points[1] = d;
                points[2] = a;
                points.RemoveAt(3);
                return Triangle(points,ref direction);
            }
            return true;
        }

        private static bool Line(List<Vector3> simplex, ref Vector3 direction)
        {
            var p1 = simplex[0];
            var p2 = simplex[1];

            var v = p1 - p2;

            if (v.Dot(p2) < 0)
                direction = v.Cross(-p2).Cross(v);
            else
            {
                simplex.RemoveAt(0);
                direction = -p2;
            }
            return false;
        }

        private static Vector3 GetSupportVector<T, U>(Vector3 dir, IList<T> cvxHullA, IList<U> cvxHullB,
            out double distance)
            where T : IVector3D
            where U : IVector3D
        {
            var distA = cvxHullA.GetDistanceToExtremeVertex(dir, out _, out var ptA);
            var distB = cvxHullB.GetDistanceToExtremeVertex(dir, out var ptB, out _);
            distance = distA.topDistance - distB.btmDistance;
            return new Vector3(ptB.X - ptA.X, ptB.Y - ptA.Y, ptB.Z - ptA.Z);
        }


        private static bool Triangle(List<Vector3> points, ref Vector3 direction)
        {
            var a = points[2];
            var b = points[1];
            var c = points[0];

            var ab = b - a;
            var ac = c - a;

            var abc = ab.Cross(ac);

            if (abc.Cross(ac).Dot(a) < 0)
            {
                if (ac.Dot(a) < 0)
                {
                    points.RemoveAt(1);
                    direction = ac.Cross(-a).Cross(ac);
                }

                else
                {
                    points.RemoveAt(0);
                    return Line(points, ref direction);
                }
            }
            else
            {
                if (ab.Cross(abc).Dot(a) < 0)
                {
                    points.RemoveAt(0);
                    return Line(points, ref direction);
                }

                else
                {
                    if (abc.Dot(a) < 0)
                        direction = abc;
                    else
                    {
                        points[0] = a;
                        points[1] = c;
                        points[2]=b;
                        direction = -abc;
                    }
                }
            }
            return false;
        }

    }
}