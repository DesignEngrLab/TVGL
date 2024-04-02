namespace ConvexHull.NET
{
    public static partial class ConvexHullGJK
    {
        /// Finds the shortest distance between two convex hulls this is an adaption of the
        /// GJK algorithm - specifically, the c version defined as OpenGJK:
        /// https://github.com/MattiaMontanari/openGJK/


        /// <summary>
        /// Finds the distance between two convex hulls. A positive value is the shortest distance
        /// between the solids, a negative value means the solids overlap. This implementation is
        /// not accurate for negative values.
        /// </summary>
        /// <param name="cvxHullPoints1">The convex hull points for 1.</param>
        /// <param name="cvxHullPoints2">The  convex hull points 2.</param>
        /// <param name="other">The other convex hull points.</param>        
        /// <param name="v">The vector,v, from the subject object to the other object.</param>
        /// <returns>The signed distance between the two convex hulls. This implementation is
        /// not accurate for negative values.</returns>
        public static double DistanceBetween<T1, T2>(this IList<T1> cvxHullPoints1, IList<T2> cvxHullPoints2, out Vector3 v)
            where T1 : IVector3D
            where T2 : IVector3D
        {
            int k = 0;                /**< Iteration counter                 */
            const int mk = 25;                 /**< Maximum number of GJK iterations  */
            const double eps_rel = Constants.BaseTolerance; /**< Tolerance on relative             */
            const double eps_tot = Constants.BaseTolerance / 100; /**< Tolerance on absolute distance    */

            const double eps_rel2 = eps_rel * eps_rel;
            double norm2Wmax = 0;

            /* Initialise search direction */
            var currentPt1 = cvxHullPoints1[0];
            var currentPt2 = cvxHullPoints2[0];
            v = new Vector3(currentPt1.X - currentPt2.X, currentPt1.Y - currentPt2.Y, currentPt1.Z - currentPt2.Z);
            var vLength = v.LengthSquared();
            /* Inialise simplex */
            var numVertsInSimplex = 1;
            var simplex = new Vector3[4];
            simplex[0] = v;
            /* Begin GJK iteration */
            do
            {
                k++;

                /* Support function */
                currentPt1 = support(cvxHullPoints1, currentPt1, -v);
                currentPt2 = support(cvxHullPoints2, currentPt2, v);
                var w = new Vector3(currentPt1.X - currentPt2.X, currentPt1.Y - currentPt2.Y, currentPt1.Z - currentPt2.Z);

                /* Test first exit condition (new point already in simplex/can't move
                 * further) */
                double exeedtol_rel = vLength - v.Dot(w);
                if (exeedtol_rel <= (eps_rel * vLength) || exeedtol_rel < eps_tot)
                    break;

                if (vLength < eps_rel2)  // it a null V
                    break;

                /* Add new vertex to simplex */
                simplex[numVertsInSimplex] = w;
                numVertsInSimplex++;

                /* Invoke distance sub-algorithm */
                if (numVertsInSimplex == 4) v = S3D(simplex, v, ref numVertsInSimplex);
                else if (numVertsInSimplex == 3) v = S2D(simplex, v, ref numVertsInSimplex);
                else if (numVertsInSimplex == 2) v = S1D(simplex, v, ref numVertsInSimplex);

                /* Test */
                for (int jj = 0; jj < numVertsInSimplex; jj++)
                {
                    double tesnorm = simplex[jj].LengthSquared();
                    if (tesnorm > norm2Wmax)
                    {
                        norm2Wmax = tesnorm;
                    }
                }
                vLength = v.LengthSquared();

                if (vLength <= (eps_tot * eps_tot * norm2Wmax))
                    break;

            } while (numVertsInSimplex != 4 && k != 25);

            Debug.WriteLineIf(k == mk, "\n * * * * * * * * * * * * MAXIMUM ITERATION NUMBER REACHED!!!  "
                + " * * * * * * * * * * * * * * \n");
            v = -v;
            return Math.Sqrt(vLength);
        }

        private static Vector3 S3D(Vector3[] s, Vector3 v, ref int nvrtx)
        {
            int i, j, k, t;
            var s1 = s[3];
            var s2 = s[2];
            var s3 = s[1];
            var s4 = s[0];
            var s1s2 = s2 - s[3];
            var s1s3 = s3 - s[3];
            var s1s4 = s4 - s[3];

            var hff1_tests = new bool[3];
            hff1_tests[2] = hff1(s1, s2);
            hff1_tests[1] = hff1(s1, s3);
            hff1_tests[0] = hff1(s1, s4);
            var testLineThree = hff1(s1, s3);
            var testLineFour = hff1(s1, s4);

            var dotTotal = 0;
            if (hff1(s1, s2)) dotTotal++;
            if (testLineThree) dotTotal++;
            if (testLineFour) dotTotal++;
            if (dotTotal == 0)
            { /* case 0.0 -------------------------------------- */
                v = s1;
                nvrtx = 1; s[0] = s1;
                return v;
            }

            var det134 = determinant(s1s3, s1s4, s1s2);
            var sss = det134 <= 0;

            var testPlaneTwo = hff3(s1, s3, s4) != sss;
            var testPlaneThree = hff3(s1, s4, s2) != sss;
            var testPlaneFour = hff3(s1, s2, s3) != sss;

            if (testPlaneTwo && testPlaneThree && testPlaneFour)
            {
                nvrtx = 4;
                return Vector3.Zero;
            }
            else if (!testPlaneTwo && testPlaneThree && testPlaneFour)
            {
                nvrtx = 3;
                s[2] = s[3];
                return S2D(s, v, ref nvrtx);
            }
            else if (testPlaneTwo && !testPlaneThree && testPlaneFour)
            {
                nvrtx = 3;
                s[1] = s2;
                s[2] = s[3];
                return S2D(s, v, ref nvrtx);
            }
            else if (testPlaneTwo && testPlaneThree && !testPlaneFour)
            {
                nvrtx = 3;
                s[0] = s3;
                s[1] = s2;
                s[2] = s[3];
                return S2D(s, v, ref nvrtx);
            }
            else if (testPlaneTwo || testPlaneThree || testPlaneFour)
            {
                // Two triangles face the origins:
                //    The only positive hff3 is for triangle 1,i,j, therefore k must be in
                //    the solution as it supports the the point of minimum norm.

                // 1,i,j, are the indices of the points on the triangle and remove k from
                // simplex
                nvrtx = 3;
                if (testPlaneTwo && !testPlaneThree && !testPlaneFour)
                {
                    k = 2; // s2
                    i = 1;
                    j = 0;
                }
                else if (!testPlaneTwo && testPlaneThree && !testPlaneFour)
                {
                    k = 1; // s3
                    i = 0;
                    j = 2;
                }
                else //if (!testPlaneTwo && !testPlaneThree && testPlaneFour)
                {
                    k = 0; // s4
                    i = 2;
                    j = 1;
                }
                var si = s[i];
                var sj = s[j];
                var sk = s[k];
                if (dotTotal == 1)
                {
                    if (hff1_tests[k])
                    {
                        if (!hff2(s1, sk, si))
                        {
                            nvrtx = 3;
                            s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, si, sk, v);
                        }
                        else if (!hff2(s1, sk, sj))
                        {
                            nvrtx = 3;
                            s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else
                        {
                            nvrtx = 2;
                            s[1] = s[3]; s[0] = sk;
                            return projectOnLine(s1, sk, v);
                        }
                    }
                    else if (hff1_tests[i])
                    {
                        if (!hff2(s1, si, sk))
                        {
                            nvrtx = 3;
                            s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, si, sk, v);
                        }
                        else
                        {
                            nvrtx = 2;
                            s[1] = s[3]; s[0] = si;
                            return projectOnLine(s1, si, v);
                        }
                    }
                    else
                    {
                        if (!hff2(s1, sj, sk))
                        {
                            nvrtx = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else
                        {
                            nvrtx = 2; s[1] = s[3]; s[0] = sj;
                            return projectOnLine(s1, sj, v);
                        }
                    }
                }
                else if (dotTotal == 2)
                {
                    // Two edges have positive hff1, meaning that for two edges the origin's
                    // project fall on the segement.
                    //  Certainly the edge 1,k supports the the point of minimum norm, and so
                    //  hff1_1k is positive

                    if (hff1_tests[i])
                    {
                        if (!hff2(s1, sk, si))
                        {
                            if (!hff2(s1, si, sk))
                            {
                                nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                                return projectOnPlane(s1, si, sk, v);
                            }
                            else
                            {
                                nvrtx = 2; s[1] = s[3]; s[0] = sk;
                                return projectOnLine(s1, sk, v);
                            }
                        }
                        else
                        {
                            if (!hff2(s1, sk, sj))
                            {
                                nvrtx = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                                return projectOnPlane(s1, sj, sk, v);
                            }
                            else
                            {
                                nvrtx = 2; s[1] = s[3]; s[0] = sk;
                                return projectOnLine(s1, sk, v);
                            }
                        }
                    }
                    else if (hff1_tests[j])
                    { //  there is no other choice
                        if (!hff2(s1, sk, sj))
                        {
                            if (!hff2(s1, sj, sk))
                            {
                                nvrtx = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                                return projectOnPlane(s1, sj, sk, v);
                            }
                            else
                            {
                                nvrtx = 2; s[1] = s[3]; s[0] = sj;
                                return projectOnLine(s1, sj, v);
                            }
                        }
                        else
                        {
                            if (!hff2(s1, sk, si))
                            {
                                nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                                return projectOnPlane(s1, si, sk, v);
                            }
                            else
                            {
                                nvrtx = 2; s[1] = s[3]; s[0] = sk;
                                return projectOnLine(s1, sk, v);
                            }
                        }
                    }
                    else
                    {
                        // ERROR;
                    }

                }
                else if (dotTotal == 3)
                {
                    // MM : ALL THIS HYPHOTESIS IS FALSE
                    // sk is s.t. hff3 for sk < 0. So, sk must support the origin because
                    // there are 2 triangles facing the origin.

                    var hff2_ik = hff2(s1, si, sk);
                    var hff2_jk = hff2(s1, sj, sk);
                    var hff2_ki = hff2(s1, sk, si);
                    var hff2_kj = hff2(s1, sk, sj);

                    if (!hff2_ki && !hff2_kj)
                    {
                        Debug.WriteLine("\n\n UNEXPECTED VALUES!!! \n\n");
                    }
                    if (hff2_ki && hff2_kj)
                    {
                        nvrtx = 2; s[1] = s[3]; s[0] = sk;
                        return projectOnLine(s1, sk, v);
                    }
                    else if (hff2_ki)
                    {
                        // discard i
                        if (hff2_jk)
                        {
                            // discard k
                            nvrtx = 2; s[1] = s[3]; s[0] = sj;
                            return projectOnLine(s1, sj, v);
                        }
                        else
                        {
                            nvrtx = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sk, sj, v);
                        }
                    }
                    else
                    {
                        // discard j
                        if (hff2_ik)
                        {
                            nvrtx = 2; s[1] = s[3]; s[0] = si;
                            return projectOnLine(s1, si, v);
                        }
                        else
                        {
                            nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, sk, si, v);
                        }
                    }
                }
            }
            else //if (!testPlaneTwo && !testPlaneThree && !testPlaneFour)
            {
                // The origin is outside all 3 triangles
                if (dotTotal == 1)
                {
                    // Here si is set such that hff(s1,si) > 0
                    if (testLineThree)
                    {
                        k = 2;
                        i = 1; // s3
                        j = 0;
                    }
                    else if (testLineFour)
                    {
                        k = 1; // s3
                        i = 0;
                        j = 2;
                    }
                    else
                    {
                        k = 0;
                        i = 2; // s2
                        j = 1;
                    }
                    var si = s[i];
                    var sj = s[j];
                    var sk = s[k];

                    if (!hff2(s1, si, sj))
                    {
                        nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sj;
                        return projectOnPlane(s1, si, sj, v);
                    }
                    else if (!hff2(s1, si, sk))
                    {
                        nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                        return projectOnPlane(s1, si, sk, v);
                    }
                    else
                    {
                        nvrtx = 2; s[1] = s[3]; s[0] = si;
                        return projectOnLine(s1, si, v);
                    }
                }
                else if (dotTotal == 2)
                {
                    // Here si is set such that hff(s1,si) < 0
                    nvrtx = 3;
                    if (!testLineThree)
                    {
                        k = 2;
                        i = 1; // s3
                        j = 0;
                    }
                    else if (!testLineFour)
                    {
                        k = 1;
                        i = 0; // s4
                        j = 2;
                    }
                    else
                    {
                        k = 0;
                        i = 2; // s2
                        j = 1;
                    }
                    var si = s[i];
                    var sj = s[j];
                    var sk = s[k];

                    if (!hff2(s1, sj, sk))
                    {
                        if (!hff2(s1, sk, sj))
                        {
                            nvrtx = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else if (!hff2(s1, sk, si))
                        {
                            nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, sk, si, v);
                        }
                        else
                        {
                            nvrtx = 2; s[1] = s[3]; s[0] = sk;
                            return projectOnLine(s1, sk, v);
                        }
                    }
                    else if (!hff2(s1, sj, si))
                    {
                        nvrtx = 3; s[2] = s[3]; s[1] = si; s[0] = sj;
                        return projectOnPlane(s1, si, sj, v);
                    }
                    else
                    {
                        nvrtx = 2; s[1] = s[3]; s[0] = sj;
                        return projectOnLine(s1, sj, v);
                    }
                }
            }
            throw new Exception("This should never happen");
        }

        private static double determinant(Vector3 p, Vector3 q, Vector3 r)
        {
            return p.X * ((q.Y * r.Z) - (r.Y * q.Z)) - p.Y * (q.X * r.Z - r.X * q.Z)
                + p.Z * (q.X * r.Y - r.X * q.Y);
        }

        private static Vector3 S2D(Vector3[] s, Vector3 v, ref int nvrtx)
        {
            var s1p = s[2];
            var s2p = s[1];
            var s3p = s[0];
            var hff1f_s12 = hff1(s1p, s2p);
            var hff1f_s13 = hff1(s1p, s3p);

            if (hff1f_s12)
            {
                var hff2f_23 = !hff2(s1p, s2p, s3p);
                if (hff2f_23)
                {
                    if (hff1f_s13)
                    {
                        var hff2f_32 = !hff2(s1p, s3p, s2p);
                        if (hff2f_32)
                        {
                            return projectOnPlane(s1p, s2p, s3p, v); // Update s, no need to update c
                                                                     // Return V{1,2,3}
                        }
                        else
                        {
                            nvrtx = 2;
                            s[1] = s[2];
                            return projectOnLine(s1p, s3p, v);
                        }
                    }
                    else
                    {
                        return projectOnPlane(s1p, s2p, s3p, v); // Update s, no need to update c
                                                                 // Return V{1,2,3}
                    }
                }
                else
                {
                    nvrtx = 2; s[0] = s[2];
                    return projectOnLine(s1p, s2p, v); // Update v
                }
            }
            else if (hff1f_s13)
            {
                var hff2f_32 = !hff2(s1p, s3p, s2p);
                if (hff2f_32)
                {
                    return projectOnPlane(s1p, s2p, s3p, v); // Update s, no need to update v
                                                             // Return V{1,2,3}
                }
                else
                {
                    nvrtx = 2;
                    s[1] = s[2];
                    return projectOnLine(s1p, s3p, v);
                }
            }
            else
            {
                v = s[2];
                nvrtx = 1;
                s[0] = s[2];
                return v;       // Return V{1}
            }

        }


        private static Vector3 projectOnPlane(Vector3 p, Vector3 q, Vector3 r, Vector3 v)
        {
            var pq = p - q;
            var pr = p - r;
            var n = pq.Cross(pr);
            var tmp = n.Dot(p) / n.Dot(n);
            return tmp * n;
        }

        private static T support<T>(IList<T> vertices, T thisS, Vector3 v) where T : IVector3D
        {
            int better = -1;

            var maxs = thisS.Dot(v);

            for (int i = 0; i < vertices.Count; ++i)
            {
                var vrt = vertices[i];
                var ss1 = vrt.Dot(v);
                if (ss1 > maxs)
                {
                    maxs = ss1;
                    better = i;
                }
            }
            if (better >= 0)
                return vertices[better];
            else return thisS;
        }
        private static Vector3 S1D(Vector3[] s, Vector3 v, ref int nvrtx)
        {
            var s1p = s[1];
            var s2p = s[0];

            if (hff1(s1p, s2p))
                return projectOnLine(s1p, s2p, v); // Update v, no need to update s
            else
            {
                s[0] = s[1];    // Update v and s
                nvrtx = 1;
                return s[0];
            }
        }

        private static Vector3 projectOnLine(Vector3 p, Vector3 q, Vector3 v)
        {
            var pq = p - q;

            var tmp = p.Dot(pq) / pq.Dot(pq);
            return p - tmp * pq;
        }

        private static bool hff1(Vector3 p, Vector3 q)
        {
            return (p.X * p.X - p.X * q.X) + (p.Y * p.Y - p.Y * q.Y) + (p.Z * p.Z - p.Z * q.Z) > 0;
        }

        private static bool hff2(Vector3 p, Vector3 q, Vector3 r)
        {
            var pq = q - p;
            var pr = r - p;
            var ntmp = pq.Cross(pr);
            var n = pq.Cross(ntmp);


            return p.Dot(n) < 0;
        }

        private static bool hff3(Vector3 p, Vector3 q, Vector3 r)
        {
            var pq = q - p;
            var pr = r - p;
            var n = pq.Cross(pr);
            return p.Dot(n) <= 0; // discard s if true
        }

    }
}