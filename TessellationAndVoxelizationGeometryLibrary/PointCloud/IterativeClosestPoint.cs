using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.PointCloud
{
    public class IterativeClosestPoint3D
    {
        const int maxIterations = 100;

        // todo: using ICP point-to-plane method
        //public static Matrix4x4 Run(IEnumerable<TriangleFace> referenceFaces, IEnumerable<IPoint3D> inputPoints)


        public static Matrix4x4 Run(IEnumerable<Vector3> inputPoints, IEnumerable<Vector3> referencePoints)
        {
            var inputCloud =  inputPoints.ToList(); // 
            var maxError = TVGL.MinimumEnclosure.FindAxisAlignedBoundingBox(inputCloud)
                .SortedDimensions[^1] / 1000; // one-thousandth of the largest dimension. so a meter long part could be off by a millimeter
            // realistically there are many occasion when the error cannot be minimized this far since the input cloud is not a perfect match
            // to the reference cloud. Here, the maximum number of iterations prevents an infinite loop.
            var referencePointList = referencePoints as IList<Vector3> ?? referencePoints.ToList(); // there is no way
            // to avoid two calls to referencePoints (once to get centroid, and once to build the KDTree). So, if it is a list, use it.
            // otherwise, make a new list to avoid re-enumeration
            var referenceCentroid = GetCentroid(referencePointList);
            // note that the reference points are centered at the origin now
            var referencePointCloud = new KDTree<Vector3>(3,
                referencePointList.Select(p => p - referenceCentroid).ToList());
            var error = double.MaxValue;
            var changeInError = double.MaxValue;
            var iterations = 0;
            var transform = Matrix4x4.Identity;
            var inputCentroid = GetCentroid(inputCloud);
            Vector3 matchingCentroid;
            while (error > maxError && iterations++ < maxIterations)
            {
                // the matched points are points from the reference cloud that match one-to-one with input points
                var matchingTargets = FindClosestPoints(inputCloud, referencePointCloud);
                matchingCentroid = GetCentroid(matchingTargets);
                //compute registration matrix
                var registrationMatrix = getRegistrationMatrix(inputCloud, matchingTargets, inputCentroid, matchingCentroid);

                // get transformation vectors passing registration matrix and center of mass
                // for both point clouds and getting rotation matrix and translation
                transform = GetTransformationVectors(registrationMatrix).Transpose();
                //transform = Matrix4x4.CreateTranslation(matchingCentroid - inputCentroid)*transform;
                transform *= Matrix4x4.CreateTranslation(matchingCentroid - inputCentroid);
                inputCloud = inputCloud.Select(p=>p.Transform(transform)).ToList();

                //Compute mean square error
                var prevError = error;
                error = MeanSquaredError(inputCloud, matchingTargets);
                changeInError = Math.Abs(error - prevError);

                Message.output("Iteration = " + iterations, 4);
                Message.output("Change in Mean Squarred Error = " + changeInError, 4);
                Message.output("Mean Squared Error = " + error, 4);
            }
            return transform;
        }

        private static Matrix4x4 MakeTranslationMatrix(IList<Vector3> inputPoints, IEnumerable<Vector3> matchedPoints)
        {
            int numInputPoints = inputPoints.Count;
            var translationVectors = new Vector3[numInputPoints];
            var averageTranslationVector = new Vector3();
            var index = 0;
            foreach (var m in matchedPoints)
            {
                translationVectors[index] = m - inputPoints[index];
                averageTranslationVector += translationVectors[index];
            }
            averageTranslationVector /= numInputPoints;
            var transform = Matrix4x4.CreateTranslation(averageTranslationVector);
            return transform;
        }

        private static Vector3[] FindClosestPoints(IList<Vector3> inputPoints, KDTree<Vector3> referencePointCloud)
        {
            var matchingTargets = new Vector3[inputPoints.Count];
            for (int i = 0; i < inputPoints.Count; i++)
            {
                matchingTargets[i] = referencePointCloud.FindNearest(inputPoints[i], 1).First();
            }
            return matchingTargets;
        }

        private static double MeanSquaredError(IList<Vector3> inputPoints, IList<Vector3> matchingTargets)
        {
            var n = inputPoints.Count;
            var sum = 0.0;
            for (int i = 0; i < n; i++)
                sum += (inputPoints[i] - matchingTargets[i]).LengthSquared();
            return sum / n;
        }


        // Compute the optimal rotation and translation vectors
        // Accepts the Matrix4X4 registration matrix, center of mass of source and target point sets
        //outputs the rotationMatrix and the translation vector
        public static Matrix4x4 GetTransformationVectors(Matrix4x4 registrationMatrix)
        {
            var eigenValues = registrationMatrix.GetEigenValuesAndVectors(out var eigenVectors);
            var maxEigenRealValue = double.MinValue;
            double[] maxEigenVector = null;
            for (int i = 0; i < eigenValues.Length; i++)
            {
                if (maxEigenRealValue < eigenValues[i].Real)
                {
                    maxEigenRealValue = eigenValues[i].Real;
                    maxEigenVector = eigenVectors[i];
                }
            }
            var q = new Quaternion(maxEigenVector[0], maxEigenVector[1], maxEigenVector[2], maxEigenVector[3]);
            return Matrix4x4.CreateFromQuaternion(q);
        }


        //compute the cross variance
        //Mehod accepts 2 lists of vectors containing source cloud points and targetcloud points
        public static Matrix3x3 GetCrossCovariance(IList<Vector3> inputPoints, Vector3[] matchingTargets, Vector3 inputCentroid,
            Vector3 matchingCentroid)
        {
            var CovarianceMatrixnew = new Matrix3x3();
            for (int i = 0; i < inputPoints.Count; i++)
            {
                //outerproduct of the correspoding points in source and target
                var multipliedMatrix = OuterProduct(inputPoints[i], matchingTargets[i]);
                //Sum up our converted matrix with our matrix in CovarianceMatrix
                CovarianceMatrixnew += multipliedMatrix;
            }

            //do a matrix division of the covariant matrix with  the count of points in the source cloud
            CovarianceMatrixnew *= 1.0 / inputPoints.Count;

            //transpose the target clouds center of mass and multiply both center of masses. 
            var CenterofMasses = OuterProduct(inputCentroid, matchingCentroid);

            // var CenterofMasses = MatrixMultiplication(getArray(getCenterOfMass(inputPoints)), getArray(getCenterOfMass(matchingTargets)));
            //do a matrix subraction of centerofMass product from sum of points product
            CovarianceMatrixnew -= CenterofMasses;

            return CovarianceMatrixnew;
        }

        private static Vector3 GetCentroid(IList<Vector3> points)
        {
            return points.Aggregate((a, b) => a + b) / points.Count;

        }

        public static Matrix3x3 OuterProduct(Vector3 ColumnVector, Vector3 rowVector)
        {

            return new Matrix3x3(
                ColumnVector.X * rowVector.X, ColumnVector.X * rowVector.Y, ColumnVector.X * rowVector.Z,
                ColumnVector.Y * rowVector.X, ColumnVector.Y * rowVector.Y, ColumnVector.Y * rowVector.Z,
                ColumnVector.Z * rowVector.X, ColumnVector.Z * rowVector.Y, ColumnVector.Z * rowVector.Z
                );
        }



        // This method  accepts two lists of Vector3 points, sourcecloud and targetcloud
        // sourcecloud is the initial cloud and target cloud is the cloud we want to match to
        public static Matrix4x4 getRegistrationMatrix(IList<Vector3> inputPoints, Vector3[] matchingTargets, Vector3 inputCentroid,
            Vector3 matchingCentroid)
        {
            var crosscovariance = GetCrossCovariance(inputPoints, matchingTargets, inputCentroid, matchingCentroid);
            var crosscovarianceTranspose = crosscovariance.Transpose();
            //get anti-symmetric
            var antiSymmetricMatrix = crosscovariance - crosscovarianceTranspose;
            //double[,] antiSymmetricMatrix = MatrixSubtraction(crosscovariance, TransposeMatrix(crosscovariance));

            //form our column vector
            var columnVector = new Vector3(antiSymmetricMatrix.M23, antiSymmetricMatrix.M31, antiSymmetricMatrix.M12);

            var diagSum = crosscovariance.M11 + crosscovariance.M22 + crosscovariance.M33;
            // get the matrix to fill remaining contents of the registration matriX
            var QComputedCells = (crosscovariance + crosscovarianceTranspose) - diagSum * Matrix3x3.Identity;

            //populate our matrix 4x4 with the values from  
            return new Matrix4x4(
                diagSum, columnVector.X, columnVector.Y, columnVector.Z,
                columnVector.X, QComputedCells.M11, QComputedCells.M12, QComputedCells.M13,
                columnVector.Y, QComputedCells.M21, QComputedCells.M22, QComputedCells.M23,
                columnVector.Z, QComputedCells.M31, QComputedCells.M32, QComputedCells.M33
            );
        }
    }
}
