using Microsoft.VisualBasic;
using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.ConvexHull;
using TVGL.KDTree;

namespace TVGL
{
    public class IterativeClosestPoint3D
    {
        const int maxIterations = 100;

        // todo: using ICP point-to-plane method
        //public static Matrix4x4 Run(IEnumerable<TriangleFace> referenceFaces, IEnumerable<IPoint3D> inputPoints)
  

        public static Matrix4x4 Run(IList<Vector3> referencePoints, IList<Vector3> inputPoints)
        {
            var maxError = TVGL.MinimumEnclosure.FindAxisAlignedBoundingBox(inputPoints)
                .SortedDimensions[^1] / 1000;
            var numInputPoints = inputPoints.Count;
            var referenceCentroid = referencePoints.Aggregate((a, b) => a + b) / referencePoints.Count;
            var referencePointCloud = new TVGL.KDTree.KDTree<Vector3, object>(3,
                referencePoints.Select(p => p - referenceCentroid).ToList());
            var matchedPoints = inputPoints.Select(p => referencePointCloud.FindNearest(p, 1).First());
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
            var error = double.MaxValue;
            var changeInError = double.MaxValue;
            var iterations = 0;
            while (error < maxError && iterations++ < maxIterations)
            {

                //compute closestpoint
                var matchingTargets = ClosestPoints(inputPoints, referencePointCloud).ToList();

                //compute registration
                double[,] registrationMatrix = getRegistrationMatrix(inputPoints, matchingTargets);

                // get transformation vectors passing registration matrix and center of mass
                // for both Fpoint clouds and getting rotation matrix and translation vector into our out ariables
                var rotMatrix = GetTransformationVectors(registrationMatrix);


                //Compute mean square error
                //currError = MeanSquaredError(closestpoints, SourceCloud);
                var prevError = error;
                error = MeanSquaredError(inputPoints, matchingTargets);
                changeInError = Math.Abs(error - prevError);

                Message.output("Iteration = " + iterations, 4);
                Message.output("Change in Mean Squarred Error = " + changeInError, 4);
                Message.output("Mean Squared Error = " + error, 4);
            }
            return transform;
        }

        private static double MeanSquaredError(IList<Vector3> inputPoints, List<Vector3> matchingTargets)
        {
            var n = inputPoints.Count;
            var sum = 0.0;
            for (int i = 0; i < n; i++)
                sum += (inputPoints[i] - matchingTargets[i]).LengthSquared();
            return sum / n;
        }

        private static IEnumerable<Vector3> ClosestPoints(IList<Vector3> inputPoints, KDTree<Vector3, object> referencePointCloud)
        {
            return inputPoints.Select(p => referencePointCloud.FindNearest(p, 1).First());
        }


        // Compute the optimal rotation and translation vectors
        // Accepts the Matrix4X4 registration matrix, center of mass of source and target point sets
        //outputs the rotationMatrix and the translation vector
        public static Matrix4x4 GetTransformationVectors(double[,] registrationMatrix)
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
        public static double[,] GetCrossCovariance(IList<Vector3> inputPoints, IList<Vector3> matchingTargets)
        {
            double[,] CovarianceMatrixnew = new double[3, 3];

            for (int i = 0; i < inputPoints.Count; i++)
            {

                //outerproduct of the correspoding points in source and target
                double[,] multipliedMatrix = OuterProduct(inputPoints[i], matchingTargets[i]);

                //Sum up our converted matrix with our matrix in CovarianceMatrix
                CovarianceMatrixnew = CovarianceMatrixnew.Add(multipliedMatrix);
            }

            //do a matrix division of the covariant matrix with  the count of points in the source cloud
            for (int i = 0; i < CovarianceMatrixnew.GetLength(1); i++)
            {
                for (int j = 0; j < CovarianceMatrixnew.GetLength(0); j++)
                {
                    CovarianceMatrixnew[i, j] = (CovarianceMatrixnew[i, j] / inputPoints.Count);
                }
            }

            //transpose the target clouds center of mass and multiply both center of masses. 
            var CenterofMasses = OuterProduct(GetCenterOfMass(inputPoints), GetCenterOfMass(matchingTargets));

            // var CenterofMasses = MatrixMultiplication(getArray(getCenterOfMass(inputPoints)), getArray(getCenterOfMass(matchingTargets)));
            //do a matrix subraction of centerofMass product from sum of points product
            CovarianceMatrixnew = CovarianceMatrixnew.Subtract(CenterofMasses);

            return CovarianceMatrixnew;
        }

        private static Vector3 GetCenterOfMass(IList<Vector3> inputPoints)
        {
            throw new NotImplementedException();
        }

        public static double[,] OuterProduct(Vector3 ColumnVector, Vector3 rowVector)
        {

            double[,] outerproduct = new double[3, 3];

            outerproduct[0, 0] = ColumnVector.X * rowVector.X;
            outerproduct[0, 1] = ColumnVector.X * rowVector.Y;
            outerproduct[0, 2] = ColumnVector.X * rowVector.Z;

            outerproduct[1, 0] = ColumnVector.Y * rowVector.X;
            outerproduct[1, 1] = ColumnVector.Y * rowVector.Y;
            outerproduct[1, 2] = ColumnVector.Y * rowVector.Z;

            outerproduct[2, 0] = ColumnVector.Z * rowVector.X;
            outerproduct[2, 1] = ColumnVector.Z * rowVector.Y;
            outerproduct[2, 2] = ColumnVector.Z * rowVector.Z;

            return outerproduct;
        }



        // This method  accepts two lists of Vector3 points, sourcecloud and targetcloud
        // sourcecloud is the initial cloud and target cloud is the cloud we want to match to
        public static double[,] getRegistrationMatrix(IList<Vector3> inputPoints, List<Vector3> matchingTargets)
        {

            double[,] crosscovariance = GetCrossCovariance(inputPoints, matchingTargets);
            //get anti-symmetric
            double[,] antiSymmetricMatrix = crosscovariance.Subtract(crosscovariance.transpose());
            //double[,] antiSymmetricMatrix = MatrixSubtraction(crosscovariance, TransposeMatrix(crosscovariance));

            //form our column vector
            double[,] columnVector = new double[1, 3];

            columnVector[0, 0] = antiSymmetricMatrix[1, 2];
            columnVector[0, 1] = antiSymmetricMatrix[2, 0];
            columnVector[0, 2] = antiSymmetricMatrix[0, 1];

            var diagSum = crosscovariance.SumOfDiagonals();
            // get the matrix to fill remaining contents of the registration matriX
            double[,] QComputedCells = crosscovariance.Add(crosscovariance.transpose())
                .Subtract(StarMath.MakeIdentity(3).multiply(diagSum));



            //populate our matrix 4x4 with the values from  
            double[,] regMatrix = new double[4, 4];

            regMatrix[0, 0] = diagSum;
            regMatrix[0, 1] = columnVector[0, 0];
            regMatrix[0, 2] = columnVector[0, 1];
            regMatrix[0, 3] = columnVector[0, 2];

            regMatrix[1, 0] = columnVector[0, 0];
            regMatrix[2, 0] = columnVector[0, 1];
            regMatrix[3, 0] = columnVector[0, 2];

            regMatrix[1, 1] = QComputedCells[0, 0];
            regMatrix[1, 2] = QComputedCells[0, 1];
            regMatrix[1, 3] = QComputedCells[0, 2];

            regMatrix[2, 1] = QComputedCells[1, 0];
            regMatrix[2, 2] = QComputedCells[1, 1];
            regMatrix[2, 3] = QComputedCells[1, 2];

            regMatrix[3, 1] = QComputedCells[2, 0];
            regMatrix[3, 2] = QComputedCells[2, 1];
            regMatrix[3, 3] = QComputedCells[2, 2];


            return regMatrix;
        }
    }
}
