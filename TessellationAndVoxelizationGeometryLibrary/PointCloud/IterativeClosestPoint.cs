using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.PointCloud
{
    public class IterativeClosestPoint3D
    {
        const double AngThr = 0.8660254037844387;

        public static Matrix4x4 Run(IList<Vector3> targetPoints, IList<Vector3> originalData, double minError = 1e-7,
            int stepsSinceImprovement = 50, int maxIterations = 500)
        {
            return Run(KDTree.Create(targetPoints, Enumerable.Range(0, targetPoints.Count).ToList()),
                KDTree.Create(originalData, Enumerable.Range(0, originalData.Count).ToList()), minError, stepsSinceImprovement, maxIterations);
        }
        public static Matrix4x4 Run(IList<Vector3> targetPoints, IList<Vector3> originalPoints, IList<Vector3> targetNormals,
            IList<Vector3> originalNormals, double minError = 1e-7, int stepsSinceImprovement = 50, int maxIterations = 500)
        {
            return Run(KDTree.Create(targetPoints, Enumerable.Range(0, targetPoints.Count).ToList()), CalculateNormalInfo(targetNormals),
                KDTree.Create(originalPoints, Enumerable.Range(0, originalPoints.Count).ToList()),
                CalculateNormalInfo(originalNormals), minError, stepsSinceImprovement, maxIterations);
        }

        private static Matrix4x4 Run(KDTree<Vector3, int> targetCloud, KDTree<Vector3, int> origCloud, double minError = 1e-7,
            int stepsSinceImprovement = 50, int maxIterations = 500)
        {
            return Run(targetCloud, CalculateNormalInfo(targetCloud),
                origCloud, CalculateNormalInfo(origCloud), minError, stepsSinceImprovement, maxIterations);
        }

        private static Matrix4x4 Run(KDTree<Vector3, int> targetCloud, NormalInfo[] targetNormalInfo, KDTree<Vector3, int> startingCloud,
            NormalInfo[] startingNormalInfo, double minError, int maxStepsSinceImprovement, int maxIterations)
        {
            double DistThr = GetDistanceThreshold(targetCloud.OriginalPoints);
            var forwardTransform = GetTranslationMatrix(targetCloud.OriginalPoints, startingCloud.OriginalPoints);
            var bestTransform = forwardTransform;
            var bestError = double.MaxValue;
            var R = Matrix4x4.Identity;
            var success = false;
            var sigma_itr = 31.62;
            var DecayPram = 0.97;  // 1.03;
            var U0 = ConcatenateAlongColumns(startingNormalInfo.Select(n => n.U), startingNormalInfo.Length);
            var U1 = ConcatenateAlongColumns(targetNormalInfo.Select(n => n.U), targetNormalInfo.Length);
            var Norm_Ref = startingNormalInfo.Select(n => n.Normal).ToList();
            //var Norm_Ref = ConcatenateAlongColumns(refNormalInfo.Select(n => n.Normal), refNormalInfo.Length);//cat(2, RefInfo(:).normal);
            var Norm_Mov = targetNormalInfo.Select(n => n.Normal).ToList();
            //var Norm_Mov = ConcatenateAlongColumns(movingNormalInfo.Select(n => n.Normal), movingNormalInfo.Length);//cat(2, RefInfo(:).normal);
            var epsilon = 1e-3;
            var JArray = new List<double>();
            var error = double.MaxValue;
            var percentImproved = double.MaxValue;
            var numIterations = 0;
            var stepsSinceImprovement = 0;
            var fromStartingToTarget = startingCloud.OriginalPoints.Select(p => p.Transform(forwardTransform)).ToList();  // apply transformation to ref points.
            while (numIterations < maxIterations && error > minError && stepsSinceImprovement < maxStepsSinceImprovement)
            {
                numIterations++;
                Matrix4x4.Invert(forwardTransform, out var backTransform);
                // in addition to transforming the start points to the target, we also do the reverse
                var fromTargetToStarting = targetCloud.OriginalPoints.Select(p => p.Transform(backTransform)).ToList();  // apply transformation to move points.
                // use the nearest neighbors to find the closest point to each point in the start cloud
                var closestPointsStartToTarget = fromStartingToTarget.SelectMany(p => targetCloud.FindNearest(p, 1)).ToList();
                // use the nearest neighbors to find the closest point to each point in the target cloud
                var closestPointsTargetToStart = fromTargetToStarting.SelectMany(p => startingCloud.FindNearest(p, 1)).ToList();

                // here, we use the normals to find the difference in angles between the two (well, not actually angle, but dot-product)
                var Angle = GetAngles(Norm_Ref, Norm_Mov, closestPointsTargetToStart, R.Transpose());
                //var Angle = sum(Norm_Ref(:, NNIdx).*(R * Norm_Mov));

                // this one's complicated. We find the indices of target points that are within the threshold distance to the other target
                // point that their starting match was closest to. Yes, often these should be back to the same point. I think that'd part
                // of the point, use the points that are strongest and filter the rest 
                var bi_eff = Enumerable.Range(0, fromTargetToStarting.Count)
                    .Where(i => fromTargetToStarting[i].Distance(fromTargetToStarting[closestPointsStartToTarget[closestPointsTargetToStart[i].Item2].Item2]) < DistThr).ToList();
                var EffIdx_sim = Enumerable.Range(0, Angle.Count)
                    .Where(i => Math.Abs(Angle[i]) > AngThr && closestPointsTargetToStart[i].Item1.Distance(fromTargetToStarting[i]) < DistThr).ToList();
                //                EffIdx_sim = find(abs(Angle) > AngThr & DD' < DistThr );
                //                EffIdx = intersect(EffIdx_sim, bi_eff);
                var TargetIndices = EffIdx_sim.Intersect(bi_eff).ToList();
                var targetMatches = TargetIndices.Select(index => fromTargetToStarting[index]).ToList();
                //                TargetIndices = EffIdx;
                //                targetMatches = fromTargetToStarting(:, TargetIndices);
                var StartingIndices = TargetIndices.Select(index => closestPointsTargetToStart[index].Item2);
                var startMatches = StartingIndices.Select(index => startingCloud.OriginalPoints[index]);
                //                StartingIndices = NNIdx(EffIdx);
                //                startMatches = RefData(:, StartingIndices);
                var diffTemp = targetMatches.Zip(startMatches, (a, b) => a - b).ToList();
                var relevantRefNormalInfo = StartingIndices.Select(index => startingNormalInfo[index]).ToList();
                var relevantMovNormalInfo = TargetIndices.Select(index => targetNormalInfo[index]).ToList();
                //    %%%%%%%%%% after transformation, we need align fromTargetToStarting to RefData.
                //    %%%%%%%%%%% obtain rotation and translation via solving quadratic programming.
                CalHbCobig_Gabor(targetMatches, diffTemp, R, relevantRefNormalInfo, relevantMovNormalInfo, sigma_itr, out var H, out var b, out var J);
                //                    [H, b, J] = CalHbCobig_Gabor(targetMatches, targetMatches - startMatches, R, RefInfo(StartingIndices), MovInfo(TargetIndices), sigma_itr);
                if (!StarMath.solve(H, b, out var dx) || dx.Any(x => double.IsNaN(x)))
                {
                    RandomlyPerturbMatrix(H);
                    if (!StarMath.solve(H, b, out dx) || dx.Any(x => double.IsNaN(x)))
                    {
                        RandomlyPerturbMatrix(H);
                        if (!StarMath.solve(H, b, out dx) || dx.Any(x => double.IsNaN(x)))
                        {
                            // throw new Exception("H is singular");
                            Message.output("H is singular");
                            break;
                        }
                    }
                }
                dx = dx.Select(x => -x).ToArray();
                //                dx = -pinv(H) * b;
                double[,] dRMatrix = StarMath.ExpMatrix(SkewFun(dx.Take(3).ToArray()));
                //                dR = expm(SkewFun(dx(1:3)));
                var dR = new Matrix4x4(dRMatrix[0, 0], dRMatrix[0, 1], dRMatrix[0, 2], 0,
                    dRMatrix[1, 0], dRMatrix[1, 1], dRMatrix[1, 2], 0,
                    dRMatrix[2, 0], dRMatrix[2, 1], dRMatrix[2, 2], 0,
                    0, 0, 0, 1);
                R = R * dR;
                //var dTVector = new Vector3(dx[3], dx[4], dx[5]);
                //TVector -= dTVector;
                var Transl = GetTranslationMatrix(targetCloud.OriginalPoints, startingCloud.OriginalPoints.Select(p => p.Transform(R)));
                //var Transl = Matrix4x4.CreateTranslation(forwardTransform.M41 - dx[3], forwardTransform.M42 - dx[4], forwardTransform.M43 - dx[5]);
                forwardTransform = R * Transl;
                fromStartingToTarget = startingCloud.OriginalPoints.Select(p => p.Transform(forwardTransform)).ToList();  // apply transformation to ref points.

                //var newError = J / TargetIndices.Count;
                var newError = calculateError(forwardTransform, targetCloud, fromStartingToTarget);
                if (newError > error)
                {
                    percentImproved = double.MaxValue;
                    stepsSinceImprovement++;
                }
                else
                {
                    percentImproved = 0.5 * (error - newError) / (newError + error);
                    stepsSinceImprovement = 0;
                }
                error = newError;
                if (bestError > error)
                {
                    bestError = error;
                    bestTransform = forwardTransform;
                }
                if (stepsSinceImprovement > 0 && stepsSinceImprovement % ((int)(0.02 * maxIterations)) == 0)
                {
                    forwardTransform = bestTransform;
                    forwardTransform = RandomlyPerturbTransform(forwardTransform);
                }
                Console.WriteLine("iteration = " + numIterations + ", error = " + error, 1);
                sigma_itr = sigma_itr * DecayPram;
            }
            return bestTransform;
        }

        private static double GetDistanceThreshold(Vector3[] originalPoints)
        {
            var xMin = double.MaxValue;
            var yMin = double.MaxValue;
            var zMin = double.MaxValue;
            var xMax = double.MinValue;
            var yMax = double.MinValue;
            var zMax = double.MinValue;
            foreach (var p in originalPoints)
            {
                if (xMin > p.X) xMin = p.X;
                if (yMin > p.Y) yMin = p.Y;
                if (zMin > p.Z) zMin = p.Z;
                if (xMax < p.X) xMax = p.X;
                if (yMax < p.Y) yMax = p.Y;
                if (zMax < p.Z) zMax = p.Z;
            }
            return Math.Sqrt((xMax - xMin) * (xMax - xMin) + (yMax - yMin) * (yMax - yMin) +
                               (zMax - zMin) * (zMax - zMin)) / 100.0;
        }

        private static double calculateError(Matrix4x4 forwardTransform, KDTree<Vector3, int> targetCloud, List<Vector3> refAftData)
        {
            var error = 0.0;
            foreach (var p in refAftData)
            {
                var t = targetCloud.FindNearest(p, 1).First().Item1;
                error += (p - t).LengthSquared();
            }
            return error / refAftData.Count;

        }


        private static Matrix4x4 RandomlyPerturbTransform(Matrix4x4 transform)
        {
            var r = new Random();
            return transform * Matrix4x4.CreateFromYawPitchRoll(0.01 * r.NextDouble(), 0.01 * r.NextDouble(), 0.01 * r.NextDouble());
        }
        private static void RandomlyPerturbMatrix(double[,] h)
        {
            var avg = 0.0;
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    avg += Math.Abs(h[i, j]);
            avg /= 36.0;
            var perturb = avg * 1e-6;
            var r = new Random();
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    h[i, j] += 2 * perturb * r.NextDouble() - perturb;
        }

        private static Matrix4x4 AddTtoR(Matrix4x4 m, Vector3 t)
        {
            return new Matrix4x4(m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34, t.X, t.Y, t.Z, 1);
        }

        private static double norm(Matrix4x4 a4x4)
        {
            var aM = new double[,] { { a4x4.M11, a4x4.M12, a4x4.M13, a4x4.M14 },
                {a4x4.M21, a4x4.M22, a4x4.M23, a4x4.M24  }, {a4x4.M31, a4x4.M32, a4x4.M33, a4x4.M34  }, {a4x4.M41, a4x4.M42, a4x4.M43, a4x4.M44  } };
            var aMt = aM.Transpose();
            var maxEigenValue = 0.0;
            try
            {
                var eigenValues = StarMathLib.StarMath.GetEigenValues(aMt.multiply(aM));
                foreach (var eigenValue in eigenValues)
                {
                    var magSqd = eigenValue.Real * eigenValue.Real + eigenValue.Imaginary * eigenValue.Imaginary;
                    if (magSqd > maxEigenValue)
                        maxEigenValue = magSqd;
                }
            }
            catch { }
            return Math.Sqrt(Math.Sqrt(maxEigenValue));
        }

        private static void CalHbCobig_Gabor(IList<Vector3> Mov, List<Vector3> PtsDiff, Matrix4x4 R,
            List<NormalInfo> RefNormInfo, List<NormalInfo> MovNormInfo, double sigma, out double[,] H, out double[] b, out double J)
        {
            var p1 = Mov.Select(p => p.X).ToList();
            var p2 = Mov.Select(p => p.Y).ToList();
            var p3 = Mov.Select(p => p.Z).ToList();

            var v1 = PtsDiff.Select(p => p.X).ToList();
            var v2 = PtsDiff.Select(p => p.Y).ToList();
            var v3 = PtsDiff.Select(p => p.Z).ToList();

            var infoR1 = RefNormInfo.Select(rni => rni.Omega.M11).ToList();
            //infoR1 = infoRMat(1, 1:3:end);
            var infoR2 = RefNormInfo.Select(rni => rni.Omega.M12).ToList();
            //infoR2 = infoRMat(1, 2:3:end);
            var infoR3 = RefNormInfo.Select(rni => rni.Omega.M13).ToList();
            //infoR3 = infoRMat(1, 3:3:end);
            var infoR4 = RefNormInfo.Select(rni => rni.Omega.M22).ToList();
            //infoR4 = infoRMat(2, 2:3:end);
            var infoR5 = RefNormInfo.Select(rni => rni.Omega.M23).ToList();
            //infoR5 = infoRMat(2, 3:3:end);
            var infoR6 = RefNormInfo.Select(rni => rni.Omega.M33).ToList();
            //infoR6 = infoRMat(3, 3:3:end);
            //infoMMat = cat(2, MovNormInfo(:).omega);
            var infoM1 = MovNormInfo.Select(mni => mni.Omega.M11).ToList();
            //infoM1 = infoMMat(1, 1:3:end);
            var infoM2 = MovNormInfo.Select(mni => mni.Omega.M12).ToList();
            //infoM2 = infoMMat(1, 2:3:end);
            var infoM3 = MovNormInfo.Select(mni => mni.Omega.M13).ToList();
            //infoM3 = infoMMat(1, 3:3:end);
            var infoM4 = MovNormInfo.Select(mni => mni.Omega.M22).ToList();
            //infoM4 = infoMMat(2, 2:3:end);
            var infoM5 = MovNormInfo.Select(mni => mni.Omega.M23).ToList();
            //infoM5 = infoMMat(2, 3:3:end);
            var infoM6 = MovNormInfo.Select(mni => mni.Omega.M33).ToList();
            //infoM6 = infoMMat(3, 3:3:end);
            var R1 = R.M11;
            var R2 = R.M12;
            var R3 = R.M13;
            var R4 = R.M21;
            var R5 = R.M22;
            var R6 = R.M23;
            var R7 = R.M31;
            var R8 = R.M32;
            var R9 = R.M33;

            var infobi = new List<double[]>();
            var m1 = ZipCoefficientAdd((1, infoR1), (R1 * R1, infoM1), (R2 * R2, infoM4), (R3 * R3, infoM6), (R1 * R2 * 2, infoM2), (R1 * R3 * 2, infoM3), (R2 * R3 * 2, infoM5));
            infobi.Add(m1);
            var m2 = ZipCoefficientAdd((1, infoR2), (R4 * R1, infoM1), (R4 * R2, infoM2), (R4 * R3, infoM3), (R5 * R1, infoM2), (R5 * R2, infoM4), (R5 * R3, infoM5),
                (R6 * R1, infoM3), (R6 * R2, infoM5), (R6 * R3, infoM6));
            //infobi(end + 1, :) = infoR2 + R4.* (R1.* infoM1 + R2.* infoM2 + R3.* infoM3) + R5.* (R1.* infoM2 + R2.* infoM4 + R3.* infoM5) + R6.* (R1.* infoM3 + R2.* infoM5 + R3.* infoM6);
            var m3 = ZipCoefficientAdd((1, infoR3), (R7 * R1, infoM1), (R7 * R2, infoM2), (R7 * R3, infoM3), (R8 * R1, infoM2), (R8 * R2, infoM4), (R8 * R3, infoM5), (R9 * R1, infoM3), (R9 * R2, infoM5),
                (R9 * R3, infoM6));
            //infobi(end + 1, :) = infoR3 + R7.* (R1.* infoM1 + R2.* infoM2 + R3.* infoM3) + R8.* (R1.* infoM2 + R2.* infoM4 + R3.* infoM5) + R9.* (R1.* infoM3 + R2.* infoM5 + R3.* infoM6);
            var m4 = ZipCoefficientAdd((1, infoR4), (R4 * R4, infoM1), (R5 * R5, infoM4), (R6 * R6, infoM6), (R4 * R5 * 2, infoM2), (R4 * R6 * 2, infoM3), (R5 * R6 * 2, infoM5));
            //infobi(end + 1, :) = infoR4 + (R4.* R4).* infoM1 + (R5.* R5).* infoM4 + (R6.* R6).* infoM6 + R4.* R5.* infoM2 * 2.0 + R4.* R6.* infoM3 * 2.0 + R5.* R6.* infoM5 * 2.0;
            var m5 = ZipCoefficientAdd((1, infoR5), (R7 * R4, infoM1), (R7 * R5, infoM2), (R7 * R6, infoM3), (R8 * R4, infoM2), (R8 * R5, infoM4), (R8 * R6, infoM5), (R9 * R4, infoM3),
                (R9 * R5, infoM5), (R9 * R6, infoM6));
            //infobi(end + 1, :) = infoR5 + R7.* (R4.* infoM1 + R5.* infoM2 + R6.* infoM3) + R8.* (R4.* infoM2 + R5.* infoM4 + R6.* infoM5) + R9.* (R4.* infoM3 + R5.* infoM5 + R6.* infoM6);
            var m6 = ZipCoefficientAdd((1, infoR6), (R7 * R7, infoM1), (R8 * R8, infoM4), (R9 * R9, infoM6), (R7 * R8 * 2, infoM2), (R7 * R9 * 2, infoM3), (R8 * R9 * 2, infoM5));
            //infobi(end + 1, :) = infoR6 + (R7.* R7).* infoM1 + (R8.* R8).* infoM4 + (R9.* R9).* infoM6 + R7.* R8.* infoM2 * 2.0 + R7.* R9.* infoM3 * 2.0 + R8.* R9.* infoM5 * 2.0;

            // MArray = sum(infobi, 2);
            /*
                        m1 = infobi(1,:);
                        m2 = infobi(2,:);
                        m3 = infobi(3,:);
                        m4 = infobi(4,:);
                        m5 = infobi(5,:);
                        m6 = infobi(6,:);
            */
            var w = new double[v1.Count];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = -(v1[i] * (m1[i] * v1[i] + m2[i] * v2[i] + m3[i] * v3[i])
                    + v2[i] * (m2[i] * v1[i] + m4[i] * v2[i] + m5[i] * v3[i])
                    + v3[i] * (m3[i] * v1[i] + m5[i] * v2[i] + m6[i] * v3[i]));
                // w = -exp(-(v1.* (m1.* v1 + m2.* v2 + m3.* v3) + v2.* (m2.* v1 + m4.* v2 + m5.* v3) + v3.* (m3.* v1 + m5.* v2 + m6.* v3)) / (2.* sigma ^ 2)) / sigma ^ 2;
                w[i] = -Math.Exp(w[i] / (2 * sigma * sigma));
                w[i] /= sigma * sigma;
            }
            //w = w.Select(wi => -Math.Exp(wi / (2 * sigma * sigma)) / sigma * sigma).ToArray();
            //var sez = ZipMultiplyAdd((v1.Select(x => -x).ToList(), ZipMultiplyAdd((m1, v1), (m2, v2), (m3, v3))), (v2, ZipMultiplyAdd((m2, v1), (m4, v2), (m5, v3))),
            //    (v3, ZipMultiplyAdd((m3, v1), (m5, v2), (m6, v3))));
            //var w = ZipMultiplyAdd((v1.Select(x => -x).ToList(), ZipMultiplyAdd((m1, v1), (m2, v2), (m3, v3))), (v2, ZipMultiplyAdd((m2, v1), (m4, v2), (m5, v3))),
            //    (v3, ZipMultiplyAdd((m3, v1), (m5, v2), (m6, v3)))).Select(y => -Math.Exp(y / (2 * sigma * sigma)) / sigma * sigma).ToList();
            // w = -exp(-(v1.* (m1.* v1 + m2.* v2 + m3.* v3) + v2.* (m2.* v1 + m4.* v2 + m5.* v3) + v3.* (m3.* v1 + m5.* v2 + m6.* v3)) / (2.* sigma ^ 2)) / sigma ^ 2;
            H = new double[6, 6];
            for (int i = 0; i < w.Length; i++)
            {
                H[0, 0] += w[i] * (m4[i] * (p3[i] * p3[i]) + m6[i] * (p2[i] * p2[i]) - m5[i] * p2[i] * p3[i] * 2.0);
                H[1, 0] += -p3[i] * (m2[i] * p3[i] * w[i] - m3[i] * p2[i] * w[i]) + p1[i] * (m5[i] * p3[i] * w[i] - m6[i] * p2[i] * w[i]);
                H[2, 0] += p2[i] * (m2[i] * p3[i] * w[i] - m3[i] * p2[i] * w[i]) - p1[i] * (m4[i] * p3[i] * w[i] - m5[i] * p2[i] * w[i]);
                H[3, 0] += -w[i] * (m2[i] * p3[i] - m3[i] * p2[i]);
                H[4, 0] += -w[i] * (m4[i] * p3[i] - m5[i] * p2[i]);
                H[5, 0] += -w[i] * (m5[i] * p3[i] - m6[i] * p2[i]);
                H[0, 1] += -p3[i] * (m2[i] * p3[i] * w[i] - m5[i] * p1[i] * w[i]) + p2[i] * (m3[i] * p3[i] * w[i] - m6[i] * p1[i] * w[i]);
                H[1, 1] += w[i] * (m1[i] * (p3[i] * p3[i]) + m6[i] * (p1[i] * p1[i]) - m3[i] * p1[i] * p3[i] * 2.0);
                H[2, 1] += -p2[i] * (m1[i] * p3[i] * w[i] - m3[i] * p1[i] * w[i]) + p1[i] * (m2[i] * p3[i] * w[i] - m5[i] * p1[i] * w[i]);
                H[3, 1] += w[i] * (m1[i] * p3[i] - m3[i] * p1[i]);
                H[4, 1] += w[i] * (m2[i] * p3[i] - m5[i] * p1[i]);
                H[5, 1] += w[i] * (m3[i] * p3[i] - m6[i] * p1[i]);
                H[0, 2] += p3[i] * (m2[i] * p2[i] * w[i] - m4[i] * p1[i] * w[i]) - p2[i] * (m3[i] * p2[i] * w[i] - m5[i] * p1[i] * w[i]);
                H[1, 2] += -p3[i] * (m1[i] * p2[i] * w[i] - m2[i] * p1[i] * w[i]) + p1[i] * (m3[i] * p2[i] * w[i] - m5[i] * p1[i] * w[i]);
                H[2, 2] += w[i] * (m1[i] * (p2[i] * p2[i]) + m4[i] * (p1[i] * p1[i]) - m2[i] * p1[i] * p2[i] * 2.0);
                H[3, 2] += -w[i] * (m1[i] * p2[i] - m2[i] * p1[i]);
                H[4, 2] += -w[i] * (m2[i] * p2[i] - m4[i] * p1[i]);
                H[5, 2] += -w[i] * (m3[i] * p2[i] - m5[i] * p1[i]);
                H[0, 3] += -w[i] * (m2[i] * p3[i] - m3[i] * p2[i]);
                H[1, 3] += w[i] * (m1[i] * p3[i] - m3[i] * p1[i]);
                H[2, 3] += -w[i] * (m1[i] * p2[i] - m2[i] * p1[i]);
                H[3, 3] += m1[i] * w[i];
                H[4, 3] += m2[i] * w[i];
                H[5, 3] += m3[i] * w[i];
                H[0, 4] += -w[i] * (m4[i] * p3[i] - m5[i] * p2[i]);
                H[1, 4] += w[i] * (m2[i] * p3[i] - m5[i] * p1[i]);
                H[2, 4] += -w[i] * (m2[i] * p2[i] - m4[i] * p1[i]);
                H[3, 4] += m2[i] * w[i];
                H[4, 4] += m4[i] * w[i];
                H[5, 4] += m5[i] * w[i];
                H[0, 5] += -w[i] * (m5[i] * p3[i] - m6[i] * p2[i]);
                H[1, 5] += w[i] * (m3[i] * p3[i] - m6[i] * p1[i]);
                H[2, 5] += -w[i] * (m3[i] * p2[i] - m5[i] * p1[i]);
                H[3, 5] += m3[i] * w[i];
                H[4, 5] += m5[i] * w[i];
                H[5, 5] += m6[i] * w[i];
                //H = reshape(sum(H, 2), 6, 6);
            }
            b = new double[6];
            for (int i = 0; i < w.Length; i++)
            {
                b[0] += -v1[i] * (m2[i] * p3[i] * w[i] - m3[i] * p2[i] * w[i]) - v2[i] * (m4[i] * p3[i] * w[i] - m5[i] * p2[i] * w[i]) - v3[i] * (m5[i] * p3[i] * w[i] - m6[i] * p2[i] * w[i]);
                b[1] += v1[i] * (m1[i] * p3[i] * w[i] - m3[i] * p1[i] * w[i]) + v2[i] * (m2[i] * p3[i] * w[i] - m5[i] * p1[i] * w[i]) + v3[i] * (m3[i] * p3[i] * w[i] - m6[i] * p1[i] * w[i]);
                b[2] += -v1[i] * (m1[i] * p2[i] * w[i] - m2[i] * p1[i] * w[i]) - v2[i] * (m2[i] * p2[i] * w[i] - m4[i] * p1[i] * w[i]) - v3[i] * (m3[i] * p2[i] * w[i] - m5[i] * p1[i] * w[i]);
                b[3] += w[i] * (m1[i] * v1[i] + m2[i] * v2[i] + m3[i] * v3[i]);
                b[4] += w[i] * (m2[i] * v1[i] + m4[i] * v2[i] + m5[i] * v3[i]);
                b[5] += w[i] * (m3[i] * v1[i] + m5[i] * v2[i] + m6[i] * v3[i]);
                //b += sum(b, 2);
            }
            J = 0.0;
            for (int i = 0; i < w.Length; i++)
            {
                J += m1[i] * (v1[i] * v1[i]) + m4[i] * (v2[i] * v2[i]) + m6[i] * (v3[i] * v3[i]) + m2[i] * v1[i] * v2[i] * 2.0
                    + m3[i] * v1[i] * v3[i] * 2.0 + m5[i] * v2[i] * v3[i] * 2.0;
            }
            var maxValue = b.Max(x => Math.Abs(x));
            for (int i = 0; i < 6; i++)
                for (int j = 0; j < 6; j++)
                    if (maxValue < Math.Abs(H[i, j]))
                        maxValue = Math.Abs(H[i, j]);
            if (maxValue < 1e-6)
            {
                var k = 1 / maxValue;
                b = b.multiply(k);
                H = H.multiply(k);
            }
        }
        private static double[] ZipCoefficientAdd(params (double, IList<double>)[] terms)
        {
            var length = terms[0].Item2.Count;
            var result = new double[length];

            for (int i = 0; i < result.Length; i++)
            {
                var sum = 0.0;
                foreach (var term in terms)
                    sum += term.Item1 * term.Item2[i];
                result[i] = sum;
            }
            return result;
        }
        private static double[] ZipMultiplyAdd(params (IList<double>, IList<double>)[] terms)
        {
            var length = terms[0].Item2.Count;
            var result = new double[length];

            for (int i = 0; i < result.Length; i++)
            {
                var sum = 0.0;
                foreach (var term in terms)
                    sum += term.Item1[i] * term.Item2[i];
                result[i] = sum;
            }
            return result;
        }
        private static List<double> GetAngles(IList<Vector3> startNormals, IList<Vector3> targetNormals,
            List<(Vector3, int)> closestPointsTargetToStart, Matrix4x4 r)
        {
            var angles = new List<double>(startNormals.Count);
            for (int i = 0; i < startNormals.Count; i++)
                angles.Add(startNormals[closestPointsTargetToStart[i].Item2].Dot(targetNormals[i].Transform(r)));
            return angles;
        }

        private static double[,] SkewFun(double[] a)
        {
            if (a.Length == 3)
                return new double[,]  {
                { 0, -a[2], a[1]},
                { a[2],  0, -a[0]},
                               { -a[1], a[0], 0}
            };
            if (a.Length == 2)
                return new double[,]  {
                { a[1]},
                { -a[0]}
            };
            else throw new ArgumentException("a must be 2 or 3 dimensional");
        }


        private static double[,] ConcatenateAlongColumns(IEnumerable<Vector3> vectors, int length)
        {
            var result = new double[3, length];
            var i = 0;
            foreach (var v in vectors)
            {
                result[0, i] = v.X;
                result[1, i] = v.Y;
                result[2, i] = v.Z;
                i++;
            }
            return result;
        }

        private static double[,] ConcatenateAlongColumns(IEnumerable<Matrix3x3> matrices, int length)
        {
            var result = new double[3, 3 * length];
            var i = 0;
            foreach (var matrix in matrices)
            {
                result[0, i] = matrix.M11;
                result[1, 1] = matrix.M21;
                result[2, i] = matrix.M31;
                i++;
                result[0, i] = matrix.M12;
                result[1, i] = matrix.M22;
                result[2, i] = matrix.M32;
                i++;
                result[0, i] = matrix.M13;
                result[1, i] = matrix.M23;
                result[2, i] = matrix.M33;
                i++;
            }
            return result;
        }

        private static Matrix4x4 GetTranslationMatrix(IEnumerable<Vector3> targetPoints, IEnumerable<Vector3> startingPoints)
        {
            return Matrix4x4.CreateTranslation(GetTranslationVector(targetPoints, startingPoints));
        }

        private static Vector3 GetTranslationVector(IEnumerable<Vector3> targetPoints, IEnumerable<Vector3> startingPoints)
        {
            var targetCom = GetCenterOfMassPoints(targetPoints);
            var startingCom = GetCenterOfMassPoints(startingPoints);
            return targetCom - startingCom;
        }

        private static Vector3 GetCenterOfMassPoints(IEnumerable<Vector3> points)
        {
            var numPoints = 0;
            var center = Vector3.Zero;
            foreach (var p in points)
            {
                center += p;
                numPoints++;
            }
            center /= numPoints;
            return center;
        }




        static NormalInfo[] CalculateNormalInfo(KDTree<Vector3> data)
        {
            var numPoints = data.Count;
            var normals = new NormalInfo[numPoints];
            var numNeighbors = Math.Min(20, numPoints);
            var S = new Matrix3x3(0.001, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);
            //var mean =Vector3.Zero;
            //for (int i = 0; i < numPoints; i++)
            //    mean += data.Points[i];
            //mean /= numPoints;
            //var centeredData = new List<Vector3>(numPoints);
            //for (int i = 0; i < numPoints; i++)
            //{
            //    centeredData.Add(data.Points[i] - mean);
            //}
            for (int i = 0; i < numPoints; i++)
            {
                var point = data.OriginalPoints[i];
                var neighbors = data.FindNearest(point, numNeighbors).ToList();
                Matrix3x3 covariance = MakeCovarianceMatrix(neighbors);
                var eigenValues = covariance.GetEigenValuesAndVectors(out var eigenVectors);
                int[] orderOfEigens = OrderEigenValues(eigenValues[0], eigenValues[1], eigenValues[2]);

                var u = new Matrix3x3(eigenVectors[orderOfEigens[0]].X, eigenVectors[orderOfEigens[1]].X, eigenVectors[orderOfEigens[2]].X,
                    eigenVectors[orderOfEigens[0]].Y, eigenVectors[orderOfEigens[1]].Y, eigenVectors[orderOfEigens[2]].Y,
                    eigenVectors[orderOfEigens[0]].Z, eigenVectors[orderOfEigens[1]].Z, eigenVectors[orderOfEigens[2]].Z);
                covariance = u * S * (u.Transpose());
                Matrix3x3.Invert(covariance, out var omega);

                normals[i] = new NormalInfo { U = u, Normal = eigenVectors[orderOfEigens[0]], Omega = omega };
            }
            return normals;
        }



        private static NormalInfo[] CalculateNormalInfo(IList<Vector3> data)
        {
            var numPoints = data.Count;
            var normals = new NormalInfo[numPoints];
            var numNeighbors = Math.Min(20, numPoints);
            var S = new Matrix3x3(0.001, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);
            for (int i = 0; i < numPoints; i++)
            {
                var point = data[i];

                var inPlane1 = data[i].GetPerpendicularDirection();
                var inPlane2 = Vector3.Cross(data[i], inPlane1).Normalize();
                var u = new Matrix3x3(data[i].X, inPlane1.X, inPlane2.X,
                    data[i].Y, inPlane1.Y, inPlane2.Y,
                    data[i].Z, inPlane1.Z, inPlane2.Z);
               var covariance = u * S * (u.Transpose());
                Matrix3x3.Invert(covariance, out var omega);

                normals[i] = new NormalInfo { U = u, Normal = data[i], Omega = omega };
            }
            return normals;
        }

        private static int[] OrderEigenValues(ComplexNumber e1, ComplexNumber e2, ComplexNumber e3)
        {
            var e1Mag = e1.LengthSquared();
            var e2Mag = e2.LengthSquared();
            var e3Mag = e3.LengthSquared();
            if (e1Mag < e2Mag)
            {
                if (e1Mag < e3Mag)
                {
                    if (e2Mag < e3Mag) return new int[] { 0, 1, 2 };
                    else return new int[] { 0, 2, 1 };
                }
                else return new int[] { 2, 0, 1 };
            }
            else if (e2Mag < e3Mag)
            {
                if (e1Mag < e3Mag) return new int[] { 1, 0, 2 };
                else return new int[] { 1, 2, 0 };
            }
            else return new int[] { 2, 1, 0 };
        }


        private static Matrix3x3 MakeCovarianceMatrix(List<Vector3> vectors)
        {
            var num = vectors.Count;
            var m11 = 0.0;
            var m12 = 0.0;
            var m13 = 0.0;
            var m22 = 0.0;
            var m23 = 0.0;
            var m33 = 0.0;
            var average = Vector3.Zero;
            foreach (var neigbor in vectors)
                average += neigbor;
            average /= num;
            var centeredPoints = vectors.Select(n => n - average).ToList();
            foreach (var point in centeredPoints)
            {
                m11 += point.X * point.X;
                m12 += point.X * point.Y;
                m13 += point.X * point.Z;
                m22 += point.Y * point.Y;
                m23 += point.Y * point.Z;
                m33 += point.Z * point.Z;
            }
            var covariance = new Matrix3x3(m11, m12, m13, m12, m22, m23, m13, m23, m33);
            covariance *= 1.0 / (num - 1);
            return covariance;
        }

        internal struct NormalInfo  //readonly
        {
            public Vector3 Normal { get; init; }
            public Matrix3x3 U { get; init; }
            public Matrix3x3 Omega { get; init; }
        }
    }
}
