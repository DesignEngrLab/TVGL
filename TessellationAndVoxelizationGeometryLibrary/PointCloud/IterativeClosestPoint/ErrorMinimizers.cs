using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace TVGL.PointMatcherNet
{
    public class PointToPlaneErrorMinimizer : IErrorMinimizer
    {
        public EuclideanTransform SolveForTransform(ErrorElements mPts)
        {
            if (!mPts.reference.containsNormals)
            {
                throw new ArgumentException("Reference points must have computed normals. Use appropriate input filter.");
            }

            var readingPts = mPts.reading.points;
            var refPts = mPts.reference.points;

            // Compute cross product of cross = cross(reading X normalRef)
            // wF = [ weights*cross   ]
            //      [ weights*normals ]
            //
            // F  = [ cross   ]
            //      [ normals ]
            var wF = new double[6, readingPts.Length];
            var F = new double[6, readingPts.Length];
            for (int i = 0; i < readingPts.Length; i++)
            {
                var cross = Vector3.Cross(readingPts[i].point, refPts[i].normal);
                var wCross = mPts.weights[i] * cross;
                var wNormal = mPts.weights[i] * refPts[i].normal;
                wF[0, i] = wCross.X;
                wF[1, i] = wCross.Y;
                wF[2, i] = wCross.Z;
                wF[3, i] = wNormal.X;
                wF[4, i] = wNormal.Y;
                wF[5, i] = wNormal.Z;
                F[0, i] = cross.X;
                F[1, i] = cross.Y;
                F[2, i] = cross.Z;
                F[3, i] = refPts[i].normal.X;
                F[4, i] = refPts[i].normal.Y;
                F[5, i] = refPts[i].normal.Z;
            }

            // Unadjust covariance A = wF * F'
            var A = wF.multiply(F.transpose());

            // dot product of dot = dot(deltas, normals)
            var dotProd = new double[mPts.reading.points.Length];
            for (int i = 0; i < readingPts.Length; i++)
            {
                var delta = readingPts[i].point - refPts[i].point;
                dotProd[i] = Vector3.Dot(delta, refPts[i].normal);
            }

            // b = -(wF' * dot)
            var b = ((wF.transpose()).multiply(dotProd)).multiply(-1);

            // Cholesky decomposition
            A.solve(b, out var x, true);

            EuclideanTransform transform;
            Vector3 axis = new Vector3(x[0], x[1], x[2]);
            var len = axis.Length();
            transform.rotation = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(axis / len, len));
            transform.translation = new Vector3(x[3], x[4], x[5]);

            return transform;
        }
    }

    public class ErrorElements
    {
        public DataPoints reading;
        public DataPoints reference;
        public float[] weights;
    }

    public static class ErrorMinimizerHelper
    {
        public static EuclideanTransform Compute(
            DataPoints filteredReading,
            DataPoints filteredReference,
            Matches matches,
            IErrorMinimizer minimizer)
        {
            ErrorElements mPts = ErrorMinimizerHelper.GetMatchedPoints(filteredReading, filteredReference, matches);
            return minimizer.SolveForTransform(mPts);
        }

        private static ErrorElements GetMatchedPoints(
            DataPoints requestedPts,
            DataPoints sourcePts,
            Matches matches)
        {
            int knn = matches.Dists.Length;

            int maxPointsCount = matches.Ids.GetLength(0) * matches.Ids.GetLength(1);

            var keptPoints = new List<DataPoint>(maxPointsCount);
            var matchedPoints = new List<DataPoint>(maxPointsCount);

            //float weightedPointUsedRatio = 0;
            for (int k = 0; k < knn; k++) // knn
            {
                for (int i = 0; i < requestedPts.points.Length; ++i) //nb pts
                {
                    keptPoints.Add(requestedPts.points[i]);
                    float matchIdx = matches.Ids[k, i];
                    matchedPoints.Add(sourcePts.points[(int)matchIdx]);
                }
            }

            var result = new ErrorElements
            {
                reading = new DataPoints
                {
                    points = keptPoints.ToArray(),
                    containsNormals = requestedPts.containsNormals,
                },
                reference = new DataPoints
                {
                    points = matchedPoints.ToArray(),
                    containsNormals = sourcePts.containsNormals,
                }
            };
            return result;
        }
    }
}
