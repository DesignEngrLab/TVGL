namespace PointCloudNet
{
    public static partial class ConvexHullGJK
    {
        /// Finds the shortest distance between two convex hulls. This is an adaption of the GJK algorithm
        /// that was originally developed by Gilbert-Johnson-Keerthi in 1988. In 2017, Montanari, Petrinic, &Barbieri 
        /// improved upon it slightly (https://dl.acm.org/doi/abs/10.1145/3083724).
        /// The implementation here is a translation of the Montanari's' work
        /// https://github.com/MattiaMontanari/openGJK/


        /// <summary>
        /// Finds the distance between two convex hulls. A positive value is the shortest distance
        /// between the solids, a zero means they are touching or overlapping. This implementation does
        /// not produce negative values.
        /// </summary>
        /// <param name="cvxHullPoints1">The convex hull points for 1.</param>
        /// <param name="cvxHullPoints2">The  convex hull points 2.</param>
        /// <param name="other">The other convex hull points.</param>        
        /// <param name="v">The vector,dir, from the subject object to the other object.</param>
        /// <returns>The signed distance between the two convex hulls. This implementation is
        /// not accurate for negative values.</returns>
        public static double DistanceBetween<T1, T2>(this IList<T1> cvxHullPoints1, IList<T2> cvxHullPoints2, out Vector3 v)
            where T1 : IVector3D
            where T2 : IVector3D
        {
            int iter = 0;                /**< Iteration counter                 */
            const int maxIter = 1000;                 /**< Maximum number of GJK iterations  */
            const double relativeTolerance = Constants.BaseTolerance; /**< Tolerance on relative             */
            const double relativeToleranceSqd = relativeTolerance * relativeTolerance;
            const double absTolerance = Constants.BaseTolerance / 100; /**< Tolerance on absolute distance    */

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
                iter++;

                /* Support function */
                currentPt1 = FindMaxDotProduct(cvxHullPoints1, currentPt1, -v);
                currentPt2 = FindMaxDotProduct(cvxHullPoints2, currentPt2, v);
                var w = new Vector3(currentPt1.X - currentPt2.X, currentPt1.Y - currentPt2.Y, currentPt1.Z - currentPt2.Z);

                /* Test first exit condition (new point already in simplex/can't move further) */
                double deltaLength = vLength - v.Dot(w);
                if (deltaLength <= (relativeTolerance * vLength) || deltaLength < absTolerance)
                    break;

                if (vLength < relativeToleranceSqd)  // it a null V
                    break;

                /* Add new vertex to simplex */
                simplex[numVertsInSimplex] = w;
                numVertsInSimplex++;

                /* Invoke distance sub-algorithm */
                if (numVertsInSimplex == 4) v = NearestSimplex3(simplex, v, ref numVertsInSimplex);
                else if (numVertsInSimplex == 3) v = NearestSimplex2(simplex, v, ref numVertsInSimplex);
                else if (numVertsInSimplex == 2) v = NearestSimplex1(simplex, v, ref numVertsInSimplex);

                /* Test */
                for (int i = 0; i < numVertsInSimplex; i++)
                {
                    double tesnorm = simplex[i].LengthSquared();
                    if (tesnorm > norm2Wmax)
                    {
                        norm2Wmax = tesnorm;
                    }
                }
                vLength = v.LengthSquared();

                if (vLength <= (absTolerance * absTolerance * norm2Wmax))
                    break;

            } while (numVertsInSimplex != 4 && iter < maxIter);

            Debug.WriteLineIf(iter == maxIter, "\n * * * * * * * * * * * * MAXIMUM ITERATION NUMBER REACHED!!!  "
                + " * * * * * * * * * * * * * * \n");
            v = -v;
            return Math.Sqrt(vLength);
        }

        private static Vector3 NearestSimplex3(Vector3[] s, Vector3 v, ref int numVertsInSimplex)
        {
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
                numVertsInSimplex = 1; s[0] = s1;
                return v;
            }

            var det134 = determinant(s1s3, s1s4, s1s2);
            var sss = det134 <= 0;

            var testPlaneTwo = hff3(s1, s3, s4) != sss;
            var testPlaneThree = hff3(s1, s4, s2) != sss;
            var testPlaneFour = hff3(s1, s2, s3) != sss;
            int i, j, k;

            if (testPlaneTwo && testPlaneThree && testPlaneFour)
            {
                numVertsInSimplex = 4;
                return Vector3.Zero;
            }
            else if (!testPlaneTwo && testPlaneThree && testPlaneFour)
            {
                numVertsInSimplex = 3;
                s[2] = s[3];
                return NearestSimplex2(s, v, ref numVertsInSimplex);
            }
            else if (testPlaneTwo && !testPlaneThree && testPlaneFour)
            {
                numVertsInSimplex = 3;
                s[1] = s2;
                s[2] = s[3];
                return NearestSimplex2(s, v, ref numVertsInSimplex);
            }
            else if (testPlaneTwo && testPlaneThree && !testPlaneFour)
            {
                numVertsInSimplex = 3;
                s[0] = s3;
                s[1] = s2;
                s[2] = s[3];
                return NearestSimplex2(s, v, ref numVertsInSimplex);
            }
            else if (testPlaneTwo || testPlaneThree || testPlaneFour)
            {
                // Two triangles face the origins:
                //    The only positive hff3 is for triangle 1,i,j, therefore iter must be in
                //    the solution as it supports the the point of minimum norm.

                // 1,i,j, are the indices of the points on the triangle and remove iter from
                // simplex
                numVertsInSimplex = 3;
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
                            numVertsInSimplex = 3;
                            s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, si, sk, v);
                        }
                        else if (!hff2(s1, sk, sj))
                        {
                            numVertsInSimplex = 3;
                            s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else
                        {
                            numVertsInSimplex = 2;
                            s[1] = s[3]; s[0] = sk;
                            return projectOnLine(s1, sk, v);
                        }
                    }
                    else if (hff1_tests[i])
                    {
                        if (!hff2(s1, si, sk))
                        {
                            numVertsInSimplex = 3;
                            s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, si, sk, v);
                        }
                        else
                        {
                            numVertsInSimplex = 2;
                            s[1] = s[3]; s[0] = si;
                            return projectOnLine(s1, si, v);
                        }
                    }
                    else
                    {
                        if (!hff2(s1, sj, sk))
                        {
                            numVertsInSimplex = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else
                        {
                            numVertsInSimplex = 2; s[1] = s[3]; s[0] = sj;
                            return projectOnLine(s1, sj, v);
                        }
                    }
                }
                else if (dotTotal == 2)
                {
                    // Two edges have positive hff1, meaning that for two edges the origin'vertices
                    // project fall on the segement.
                    //  Certainly the edge 1,iter supports the the point of minimum norm, and so
                    //  hff1_1k is positive

                    if (hff1_tests[i])
                    {
                        if (!hff2(s1, sk, si))
                        {
                            if (!hff2(s1, si, sk))
                            {
                                numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                                return projectOnPlane(s1, si, sk, v);
                            }
                            else
                            {
                                numVertsInSimplex = 2; s[1] = s[3]; s[0] = sk;
                                return projectOnLine(s1, sk, v);
                            }
                        }
                        else
                        {
                            if (!hff2(s1, sk, sj))
                            {
                                numVertsInSimplex = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                                return projectOnPlane(s1, sj, sk, v);
                            }
                            else
                            {
                                numVertsInSimplex = 2; s[1] = s[3]; s[0] = sk;
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
                                numVertsInSimplex = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                                return projectOnPlane(s1, sj, sk, v);
                            }
                            else
                            {
                                numVertsInSimplex = 2; s[1] = s[3]; s[0] = sj;
                                return projectOnLine(s1, sj, v);
                            }
                        }
                        else
                        {
                            if (!hff2(s1, sk, si))
                            {
                                numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                                return projectOnPlane(s1, si, sk, v);
                            }
                            else
                            {
                                numVertsInSimplex = 2; s[1] = s[3]; s[0] = sk;
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
                    // sk is vertices.t. hff3 for sk < 0. So, sk must FindMaxDotProduct the origin because
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
                        numVertsInSimplex = 2; s[1] = s[3]; s[0] = sk;
                        return projectOnLine(s1, sk, v);
                    }
                    else if (hff2_ki)
                    {
                        // discard i
                        if (hff2_jk)
                        {
                            // discard iter
                            numVertsInSimplex = 2; s[1] = s[3]; s[0] = sj;
                            return projectOnLine(s1, sj, v);
                        }
                        else
                        {
                            numVertsInSimplex = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sk, sj, v);
                        }
                    }
                    else
                    {
                        // discard j
                        if (hff2_ik)
                        {
                            numVertsInSimplex = 2; s[1] = s[3]; s[0] = si;
                            return projectOnLine(s1, si, v);
                        }
                        else
                        {
                            numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
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
                        numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sj;
                        return projectOnPlane(s1, si, sj, v);
                    }
                    else if (!hff2(s1, si, sk))
                    {
                        numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                        return projectOnPlane(s1, si, sk, v);
                    }
                    else
                    {
                        numVertsInSimplex = 2; s[1] = s[3]; s[0] = si;
                        return projectOnLine(s1, si, v);
                    }
                }
                else if (dotTotal == 2)
                {
                    // Here si is set such that hff(s1,si) < 0
                    numVertsInSimplex = 3;
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
                            numVertsInSimplex = 3; s[2] = s[3]; s[1] = sj; s[0] = sk;
                            return projectOnPlane(s1, sj, sk, v);
                        }
                        else if (!hff2(s1, sk, si))
                        {
                            numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sk;
                            return projectOnPlane(s1, sk, si, v);
                        }
                        else
                        {
                            numVertsInSimplex = 2; s[1] = s[3]; s[0] = sk;
                            return projectOnLine(s1, sk, v);
                        }
                    }
                    else if (!hff2(s1, sj, si))
                    {
                        numVertsInSimplex = 3; s[2] = s[3]; s[1] = si; s[0] = sj;
                        return projectOnPlane(s1, si, sj, v);
                    }
                    else
                    {
                        numVertsInSimplex = 2; s[1] = s[3]; s[0] = sj;
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

        private static Vector3 NearestSimplex2(Vector3[] s, Vector3 v, ref int nvrtx)
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
                            return projectOnPlane(s1p, s2p, s3p, v); // Update vertices, no need to update c
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
                        return projectOnPlane(s1p, s2p, s3p, v); // Update vertices, no need to update c
                                                                 // Return V{1,2,3}
                    }
                }
                else
                {
                    nvrtx = 2; s[0] = s[2];
                    return projectOnLine(s1p, s2p, v); // Update dir
                }
            }
            else if (hff1f_s13)
            {
                var hff2f_32 = !hff2(s1p, s3p, s2p);
                if (hff2f_32)
                {
                    return projectOnPlane(s1p, s2p, s3p, v); // Update vertices, no need to update dir
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

        /// <summary>
        /// Find the vertex with the maximum dot product with the given direction. The datum is likely the minimum
        /// dot product vertex. In the original GJK work this function was can "Support", but that is a throw-away
        /// name for a function that merely supports the main loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vertices"></param>
        /// <param name="datum"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static T FindMaxDotProduct<T>(IList<T> vertices, T datum, Vector3 dir) where T : IVector3D
        {
            int maxIndex = -1;

            var maxValue = datum.Dot(dir);

            for (int i = 0; i < vertices.Count; ++i)
            {
                var dot = vertices[i].Dot(dir);
                if (dot > maxValue)
                {
                    maxValue = dot;
                    maxIndex = i;
                }
            }
            if (maxIndex >= 0)
                return vertices[maxIndex];
            else return datum;
        }
        private static Vector3 NearestSimplex1(Vector3[] vertices, Vector3 v, ref int nvrtx)
        {
            var s1p = vertices[1];
            var s2p = vertices[0];

            if (hff1(s1p, s2p))
                return projectOnLine(s1p, s2p, v); // Update dir, no need to update vertices
            else
            {
                vertices[0] = vertices[1];    // Update dir and vertices
                nvrtx = 1;
                return vertices[0];
            }
        }

        private static Vector3 projectOnLine(Vector3 p, Vector3 q, Vector3 v)
        {
            var pq = p - q;

            var tmp = p.Dot(pq) / pq.LengthSquared();
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
            var n = pq.Cross(pr);
            n = pq.Cross(n);

            return p.Dot(n) < 0;
        }

        private static bool hff3(Vector3 p, Vector3 q, Vector3 r)
        {
            var pq = q - p;
            var pr = r - p;
            var n = pq.Cross(pr);
            return p.Dot(n) <= 0; // discard vertices if true
        }

    }
}