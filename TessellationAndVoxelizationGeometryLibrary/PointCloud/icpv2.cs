using StarMathLib;
using System;
using System.Collections.Generic;
using System.IO;
using TVGL;

public class Icpv2
{
    public FileInfo sourcefile;
    public FileInfo TargetFile;

    static List<String> sourcetags;
    static List<String> targettags;

    static List<Vector3> sourceCloud;
    static List<Vector3> targetCloud;

    bool firstCall = true;

    //<Summary> 
    // returns updated list of vector3 points from source and target
    // parameter : None
    // returns   : List of Vector3 points in source cloud
    //</Summary>  


    public static void Main(string[] args)
    {
        ComputeICP(100, 0.0004f * GetCovariance(sourceCloud), targetCloud, targetCloud);

        // ComputeICP(100, 0.0004f * GetCovariance(GetVectorList(sourceGameObject)), GetVectorList(sourceGameObject), GetVectorList(targetGameObject));
        //       
    }

    public Icpv2()
    {

    }

    public void GetVectorList(string sourcefilepath, string targetfilepath)
    {
        sourcefile = new FileInfo(sourcefilepath);
        using (StreamReader sr = sourcefile.OpenText())
        {
            string[] data = sr.ToString().Split(',');
            sourceCloud.Add(new Vector3(Convert.ToSingle(data[0]), Convert.ToSingle(data[1]), Convert.ToSingle(data[2])));
            sourcetags.Add(sr.ToString().Split(',')[1]);
        }

        TargetFile = new FileInfo(targetfilepath);
        using (StreamReader sr = TargetFile.OpenText())
        {
            string[] data = sr.ToString().Split(',');
            targetCloud.Add(new Vector3(Convert.ToSingle(data[0]), Convert.ToSingle(data[1]), Convert.ToSingle(data[2])));
            targettags.Add(sr.ToString().Split(',')[1]);
        }
    }

    static void ExecuteProcess()
    {
        //System.Diagnostics.Process.Start(Application.dataPath + "/Plugins/py2.bat");

    }

    //<Summary> 
    // returns list of tags pf child gameobject from parsed gamebject
    // parameter : Gameobject
    // returns   : List of Strng ocntaining all tags of child transforms in the gameobject
    //</Summary>



    private void Start()
    {
        Vector3 centerOfmass = GetCenterOfMass(sourceCloud) - GetCenterOfMass(targetCloud);

        Matrix4x4 translate = Matrix4x4.CreateTranslation(centerOfmass);

        ///translate each point in the sourcecloud using the center of mass computed
        for (int i = 0; i < sourceCloud.Count; i++)
        {
            sourceCloud[i] = sourceCloud[i] + centerOfmass;
        }

    }

    //private void UpdateGraph()
    //{
    //    Texture2D tex = new Texture2D(2, 2);
    //    tex.LoadImage(imageAsset.bytes);
    //    //GetComponent<Renderer>().material.mainTexture = tex;
    //    ImageGraph.GetComponent<UnityEngine.UI.RawImage>().material.mainTexture = tex;
    //}


    //<Summary> 
    // populates two list of strings with the 
    // parameter : None
    // returns   :
    // Update is called once per frame
    //if Key Code A is hit, run ComputeICP supplying values for the parameters
    private static void OnTimedEvent(System.Object source, System.Timers.ElapsedEventArgs e)
    {
        Console.WriteLine(e.SignalTime);
    }

    //void Update()
    //{


    //    if (Input.GetKeyUp(KeyCode.Alpha2))
    //    {


    //        using (var sr = new StreamWriter(Application.dataPath + @"\Plugins\aTimer.csv"))
    //        {
    //            Console.WriteLine("start testing");
    //            System.Timers.Timer aTimer = new System.Timers.Timer(1000);
    //            aTimer.Elapsed += OnTimedEvent;
    //            aTimer.Start();
    //            Console.WriteLine("Basic ICP start");


    //            this.GetComponent<BasicICP>().PerformICP();
    //            ////sr.WriteLine(aTimer.ToString());
    //            Console.WriteLine("Basic ICP end");
    //            aTimer.Stop();


    //            // populate source and traget gameobject tags into a List<String>

    //            aTimer.Start();
    //            // Console.WriteLine(aTimer.ToString());
    //            Console.WriteLine("peter ICP start");
    //            ComputeICP(100, 0.0004f * GetCovariance(GetVectorList(sourceGameObject)), GetVectorList(sourceGameObject), GetVectorList(targetGameObject));
    //            // StartCoroutine(ComputeICP(100, 0.0034f * GetCovariance(GetVectorList(sourceGameObject)), GetVectorList(sourceGameObject), GetVectorList(targetGameObject)));
    //            //sr.WriteLine(aTimer.ToString());
    //            // Console.WriteLine(aTimer.Elapsed);
    //            Console.WriteLine("peter ICP end");
    //            aTimer.Stop();
    //        }


    //        //Save distances between matching point to CSV file
    //        GenerateDataForGraph();

    //        //run python script  to generate graph images
    //        ExecuteProcess();
    //        //Update graph
    //        UpdateGraph();


    //    }
    //}

    //get ideal threshold  for a model shape by finding
    //Square root of the trace of the covariance of the model shape.
    //parameters list of Vector3 points of the shape
    //return double threshold 
    public static double GetCovariance(List<Vector3> cloud)
    {
        double[,] CovarianceMatrixnew = new double[3, 3];

        for (int i = 0; i < cloud.Count; i++)
        {

            //get the two converted matrix
            double[,] multipliedMatrix = OuterProduct(cloud[i], cloud[i]);

            //Sum up our converted matrix with our matrix in CovarianceMatrix
            CovarianceMatrixnew = MatrixAddition(CovarianceMatrixnew, multipliedMatrix);
        }

        //find trace of this matrix
        double trace = TraceMatrix(CovarianceMatrixnew);

        //find square root of the trace
        double threshold = (double)Math.Sqrt((double)trace);

        return threshold;

    }


    //Mean square error computes the mean distance between the closest points and the transformed sourcecloud after every iteration 
    //Paramters : List<Vector3> Closestpoints. List<Vector3> TransformedSourcepoint
    // returns: double meansquared error
    public static double MeanSquaredError(List<Vector3> closestpoint, List<Vector3> TransformedSourceCloud)
    {
        double distance = 0;

        for (int i = 0; i < closestpoint.Count; i++)
        {
            //Use.sqrMagnitude in unity;
            //distance += (closestpoint[i]-TransformedSourceCloud[i]).sqrMagnitude;
            distance += Vector3.Distance(closestpoint[i], TransformedSourceCloud[i]);
        }

        return (double)distance / TransformedSourceCloud.Count;
    }

    //Summary : return identity matrix of any dimension
    //Parameters: int rows and int coloumns
    //returns matrix with identity 
    public static double[,] getIdentityMatrix(int rows, int cols)
    {
        double[,] identitymat = new double[rows, cols];

        for (int i = 0; i < cols; i++)
        {

            for (int j = 0; j < cols; j++)
            {

                if (i == j)
                {
                    identitymat[i, j] = 1f;
                }
                else
                {
                    identitymat[i, j] = 0f;
                }

            }

        }

        return identitymat;
    }

    //<Summary> 
    // Calculates the center of mass from a list of Vector3 points
    // parameter : List<Vetcor3> points
    // returns   : double center of mass 
    //</Summary>     
    public static Vector3 GetCenterOfMass(List<Vector3> PointCloud)
    {
        Vector3 CenterOfMass = new Vector3(0, 0, 0);

        for (int i = 0; i < PointCloud.Count; i++)
        {
            CenterOfMass += PointCloud[i];
        }

        return CenterOfMass / PointCloud.Count;

    }

    //<Summary> 
    // Calculates the transpose of a matrix 
    // parameter : double matrix
    // returns : double tranpose 
    //</Summary> 
    public static double[,] TransposeMatrix(double[,] a)
    {

        double[,] transpose = new double[a.GetLength(1), a.GetLength(0)];

        for (int i = 0; i < a.GetLength(1); i++)
        {
            for (int j = 0; j < a.GetLength(0); j++)
            {
                transpose[i, j] = a[j, i];
            }
        }
        return transpose;
    }

    //<Summary> 
    // Calculates the trace of a matrix
    // parameter : double matrix
    // returns : double trace
    //</Summary> 
    public static double TraceMatrix(double[,] a)
    {
        double traceValue = 0f;

        for (int i = 0; i < a.GetLength(0); i++)
        {
            traceValue += a[i, i];
        }

        return traceValue;
    }

    //<Summary> 
    // perform a matrix subtraction of two matrices
    // parameter : matrix a and Matrix b
    // returns   : results of the value of the subtracted matrix A-B
    //</Summary> 
    public static double[,] MatrixSubtraction(double[,] a, double[,] b)
    {
        double[,] SubtractMatrix = new double[a.GetLength(0), a.GetLength(1)];

        //throw an error if matrix dimensions are not the same
        if (a.GetLength(0) != b.GetLength(0) || b.GetLength(1) != a.GetLength(1))
        {
            throw new System.NullReferenceException("Matrices must have same dimension");
        }

        //matrices need to be of same size
        for (int i = 0; i < b.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(1); j++)
            {
                SubtractMatrix[i, j] = a[i, j] - b[i, j];
            }

        }
        return SubtractMatrix;
    }

    //<Summary> 
    // Calculates the transpose of a matrix 
    // parameter : double matrix
    // returns : double tranpose 
    //</Summary> 
    public static double[,] MatrixMultiplicationByNumber(double[,] a, double num)
    {
        //multiply matrix with number
        double[,] multiplyMatrixNum = new double[a.GetLength(0), a.GetLength(1)];

        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < a.GetLength(1); j++)
            {
                multiplyMatrixNum[i, j] = a[i, j] * num;
            }
        }
        return multiplyMatrixNum;
    }

    //Method accepts two Vector3s
    //rowVector and ColumnVector 
    //The product of these two vectors are taken and returned as output

    //swapped paramter positions to test handedness
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

    //Multiply matrix
    //They must have same dimension
    //Method accepts two matrices, adds them up to create and return a new matrix
    public static double[,] MatrixAddition(double[,] b, double[,] a)
    {
        double[,] addMatrix = new double[a.GetLength(0), b.GetLength(1)];

        //matrices must have same dimension
        if (a.GetLength(0) != b.GetLength(0) || b.GetLength(1) != a.GetLength(1))
        {
            throw new System.NullReferenceException("Matrices must have same dimension");
        }

        //sum up values from same position in both matrices into same position in new matrix
        for (int i = 0; i < a.GetLength(0); i++)
        {
            for (int j = 0; j < b.GetLength(1); j++)
            {
                addMatrix[i, j] = a[i, j] + b[i, j];
            }

        }

        return addMatrix;
    }

    //compute the cross variance
    //Mehod accepts 2 lists of vectors containing source cloud points and targetcloud points
    public static double[,] GetCrossCovariance(List<Vector3> SourceCloud, List<Vector3> TargetCloud)
    {
        double[,] CovarianceMatrixnew = new double[3, 3];

        for (int i = 0; i < SourceCloud.Count; i++)
        {

            //outerproduct of the correspoding points in source and target
            double[,] multipliedMatrix = OuterProduct(SourceCloud[i], TargetCloud[i]);

            //Sum up our converted matrix with our matrix in CovarianceMatrix
            CovarianceMatrixnew = MatrixAddition(CovarianceMatrixnew, multipliedMatrix);
        }

        //do a matrix division of the covariant matrix with  the count of points in the source cloud
        for (int i = 0; i < CovarianceMatrixnew.GetLength(1); i++)
        {
            for (int j = 0; j < CovarianceMatrixnew.GetLength(0); j++)
            {
                CovarianceMatrixnew[i, j] = (CovarianceMatrixnew[i, j] / SourceCloud.Count);
            }
        }

        //transpose the target clouds center of mass and multiply both center of masses. 
        var CenterofMasses = OuterProduct(GetCenterOfMass(SourceCloud), GetCenterOfMass(TargetCloud));

        // var CenterofMasses = MatrixMultiplication(getArray(getCenterOfMass(SourceCloud)), getArray(getCenterOfMass(TargetCloud)));
        //do a matrix subraction of centerofMass product from sum of points product
        CovarianceMatrixnew = MatrixSubtraction(CovarianceMatrixnew, CenterofMasses);

        return CovarianceMatrixnew;
    }

    // This method  accepts two lists of Vector3 points, sourcecloud and targetcloud
    // sourcecloud is the initial cloud and target cloud is the cloud we want to match to

    public static double[,] getRegistrationMatrix(List<Vector3> SourceCloud, List<Vector3> closestpoints)
    {

        double[,] crosscovariance = GetCrossCovariance(SourceCloud, closestpoints);
        //get anti-symmetric
        double[,] antiSymmetricMatrix = MatrixSubtraction(crosscovariance, TransposeMatrix(crosscovariance));

        //form our column vector
        double[,] columnVector = new double[1, 3];

        columnVector[0, 0] = antiSymmetricMatrix[1, 2];
        columnVector[0, 1] = antiSymmetricMatrix[2, 0];
        columnVector[0, 2] = antiSymmetricMatrix[0, 1];


        // get the matrix to fill remaining contents of the registration matriX
        double[,] QComputedCells = MatrixSubtraction(MatrixAddition(crosscovariance, TransposeMatrix(crosscovariance)),
            MatrixMultiplicationByNumber(getIdentityMatrix(3, 3), TraceMatrix(crosscovariance)));



        //populate our matrix 4x4 with the values from  
        double[,] regMatrix = new double[4, 4];

        regMatrix[0, 0] = TraceMatrix(crosscovariance);
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

    //Computes the closest points of source point cloud in target point cloud
    //Acepts two list of Vector3 points , sourcecloud and target cloud
    //public List<Vector3> ClosestPoints(List<Vector3> SourceCloud, List<Vector3> TargetCloud)
    //{

    //    s_PreparePerfMarker.Begin();

    //    // new list to return list of closest points index positions map to source clouds index            
    //    List<Vector3> closestpoints = new List<Vector3>(SourceCloud.Count);


    //    double distance;
    //    int TargetIndex = 0;

    //    for (int i = 0; i < SourceCloud.Count; i++)
    //    {
    //        double min_distance = double.PositiveInfinity;

    //        for (int j = 0; j < TargetCloud.Count; j++)
    //        {

    //            ///test that tags of the gameobjects corresponding to these vectors are same
    //            if (sourcetags[i] == targettags[j])
    //            {
    //                //get distance between points
    //                distance = Vector3.Distance(SourceCloud[i], TargetCloud[j]);

    //                //compare distance with value in min_distance and update min_distance if distance is smaller
    //                if (distance < min_distance)
    //                {
    //                    //keep the current targetcloud item as it is the current point with smallest distance to  sourcelouds ith point
    //                    TargetIndex = j;
    //                    min_distance = distance;
    //                }


    //            }


    //        }

    //        closestpoints.Add(TargetCloud[TargetIndex]);

    //    }

    //    s_PreparePerfMarker.End();

    //    return closestpoints;


    //}

    public static List<Vector3> ClosestPoints(List<Vector3> SourceCloud, List<Vector3> TargetCloud)
    {


        // new list to return list of closest points index positions map to source clouds index            
        List<Vector3> closestpoints = new List<Vector3>(SourceCloud.Count);


        double distance;
        int TargetIndex = 0;

        System.Threading.Tasks.Parallel.For(0, SourceCloud.Count, i =>
        {
            double min_distance = double.PositiveInfinity;

            for (int j = 0; j < TargetCloud.Count; j++)
            {

                ///test that tags of the gameobjects corresponding to these vectors are same
                if (sourcetags[i] == targettags[j])
                {
                    //get distance between points
                    distance = Vector3.Distance(SourceCloud[i], TargetCloud[j]);

                    //compare distance with value in min_distance and update min_distance if distance is smaller
                    if (distance < min_distance)
                    {
                        //keep the current targetcloud item as it is the current point with smallest distance to  sourcelouds ith point
                        TargetIndex = j;
                        min_distance = distance;
                    }


                }


            }

            closestpoints.Add(TargetCloud[TargetIndex]);

        });



        return closestpoints;


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

        //return 
        ////initialise matrix builder
        //MatrixBuilder<double> regMatrix = Matrix<double>.Build;

        ////fill matrix builder with contents of our params registrationmatri to create a Matrix
        //Matrix<double> reg = regMatrix.DenseOfArray(registrationMatrix);

        ////EVD decompose to generate our eignevalues
        //Evd<double> registrationevd = reg.Evd();
        ////Cholesky<double> registrationevd = reg.Cholesky();
        //Console.WriteLine("Symmetric" + registrationevd.IsSymmetric);

        ////get index of maximum eigenvalue for correspond eigenvector
        //double maxValue = double.NegativeInfinity;

        ////int maxEigenValue = registrationevd.EigenValues.
        //int index = 0;

        //for (int i = 0; i < registrationevd.EigenValues.Count; i++)
        //{
        //    if (registrationevd.EigenValues[i].Real > maxValue)
        //    {
        //        maxValue = registrationevd.EigenValues[i].Real;
        //        index = i;
        //    }

        //}

        ////Get the eigenvalue index and copy the vector as our unit eigenvector.
        ////Our unit EigenVector below
        //MathNet.Numerics.LinearAlgebra.Vector<double> unitEigenVector = registrationevd.EigenVectors.Column(index);

        //double q0 = unitEigenVector.At(0);
        //double q1 = unitEigenVector.At(1);
        //double q2 = unitEigenVector.At(2);
        //double q3 = unitEigenVector.At(3);

        //UnityQuat = new Quaternion(q1, q2, q3, q0);

        //Matrix4x4 rotMatrix = Matrix4x4.CreateFromQuaternion(UnityQuat);



        //Vector3 y = new Vector3(0, 0, 0);

        ////get optimal translation vector     
        //// Vector3 y = Matrix4x4.Rotate(UnityQuat).MultiplyPoint(SourceCenterOfMass);


        //Vector3 optimalTranslation = TargeCenterOfMass - y;
        //TranslationVector = optimalTranslation;
    }
    /// <summary>
    /// writes a csv file containing list of matching distances that are distances between the source gameobject and target gameobject for all points
    /// </summary>
    /// <returns> void </returns>
    //public void GenerateDataForGraph()
    //{

    //    var fileDist = new FileInfo(Application.dataPath + @"\Plugins\distances.csv");

    //    if (fileDist.Exists) fileDist.Delete();


    //    //var filespeed = new FileInfo(Application.dataPath + @"\Plugins\speed.csv");

    //    //if (filespeed.Exists) filespeed.Delete();


    //    using (var sr = new StreamWriter(Application.dataPath + @"\Plugins\distances.csv"))
    //    {
    //        for (int i = 0; i < sourceGameObject.transform.childCount; i++)
    //        {
    //            double distance = Vector3.Distance(sourceGameObject.transform.GetChild(i).position, targetGameObject.transform.GetChild(i).position);
    //            sr.WriteLine(distance);
    //        }

    //    }

    //}

    /// <summary>
    /// Returns a value that indicates whether any pair of elements in two specified vectors is not equal.
    /// </summary>
    /// <param name="iteration">The iteration to loop </param>
    /// <param name="meanSquaredError">Mean square error value to to test and quit iteration</param>
    ///  <param name="SourceCloud">The first vector to compare.</param>
    /// <param name="TargetCloud">The second vector to compare.</param>
    /// <returns> void </returns>
    public /*IEnumerator */static void ComputeICP(int iteration, double meanSquaredErrorThreshold, List<Vector3> SourceCloud, List<Vector3> TargetCloud)
    {
        // GameObject test = null; ;
        double Error = 0f;
        List<Vector3> closestpoints;
        double prevError;
        double changeError = double.PositiveInfinity;
        //Matrix4x4 icpMatrix = Matrix4x4.identity;      

        int count = 0;

        while (iteration > count && changeError > meanSquaredErrorThreshold)
        {
            //compute closestpoint
            closestpoints = ClosestPoints(SourceCloud, TargetCloud);

            //compute registration
            double[,] registrationMatrix = getRegistrationMatrix(SourceCloud, closestpoints);

            // get transformation vectors passing registration matrix and center of mass
            // for both Fpoint clouds and getting rotation matrix and translation vector into our out ariables
            var transform = GetTransformationVectors(registrationMatrix);


            //Compute mean square error
            //currError = MeanSquaredError(closestpoints, SourceCloud);
            prevError = Error;
            Error = MeanSquaredError(closestpoints, SourceCloud);
            changeError = Math.Abs(Error - prevError);

            //Update source vector list with transformed vectors
            SourceCloud = sourceCloud;

            Console.WriteLine("Iteration Count  " + count);
            Console.WriteLine("Iteration Value  " + iteration);
            Console.WriteLine("Change in Mean Squarred Error " + changeError);
            Console.WriteLine("Mean Squarred Error Threshold " + meanSquaredErrorThreshold);
            count += 1;
            //yield return new WaitForSeconds(1);
            //  yield return null;
            //if this loop is quitting without a good match match and based on iteration count.
            //start all over with a different rotation value
        }

        //if (iteration == count && changeError > meanSquaredErrorThreshold)
        //{

        //    count = 0;
        //    Vector3 centerOfmass = GetCenterOfMass(GetVectorList(targetGameObject)) - GetCenterOfMass(GetVectorList(sourceGameObject));
        //    sourceGameObject.transform.Translate(centerOfmass, Space.World);

        //    Quaternion UnityQuat2 = new Quaternion(UnityEngine.Random.Range(-10.0f, 10.0f), UnityEngine.Random.Range(-10.0f, 10.10f), UnityEngine.Random.Range(-10.0f, 10.0f), UnityEngine.Random.Range(-10.0f, 10.0f));

        //    sourceGameObject.transform.Rotate(UnityQuat2.eulerAngles);

        //    Console.WriteLine(UnityQuat2);

        //    while (iteration > count && changeError > meanSquaredErrorThreshold)
        //    {

        //        //randomise the rotation


        //        closestpoints = ClosestPoints(SourceCloud, TargetCloud);

        //        //compute registration
        //        double[,] registrationMatrix = getRegistrationMatrix(SourceCloud, closestpoints);

        //        // get transformation vectors passing registration matrix and center of mass for both Fpoint clouds and getting rotation matrix and translation vector into our out ariables
        //        GetTransformationVectors(registrationMatrix, GetCenterOfMass(SourceCloud), GetCenterOfMass(TargetCloud), out Vector3 TranslationVector, out Quaternion UnityQuat);

        //        //// Applytransformation
        //        sourceGameObject.transform.Rotate(UnityQuat.eulerAngles);
        //        sourceGameObject.transform.Translate(TranslationVector);

        //        //Compute mean square error
        //        //currError = MeanSquaredError(closestpoints, SourceCloud);
        //        prevError = Error;
        //        Error = MeanSquaredError(closestpoints, SourceCloud);
        //        changeError = Math.Abs(Error - prevError);

        //        //Update source vector list with transformed vectors
        //        SourceCloud = GetVectorList(sourceGameObject);

        //        Console.WriteLine("Iteration Count  " + count);
        //        Console.WriteLine("Iteration Value  " + iteration);
        //        Console.WriteLine("Change in Mean Squarred Error " + changeError);
        //        Console.WriteLine("Mean Squarred Error Threshold " + meanSquaredErrorThreshold);
        //        count += 1;
        //        //yield return new WaitForSeconds(1);
        //        yield return null;

        //        //if this loop is quitting without a good match match and based on iteration count.
        //        //start all over with a different rotation value
        //        Console.WriteLine("Second iteration");
        //    }
        //}

        Console.WriteLine("Quit");
    }
    //void OnGUI()
    //{
    //    GUI.Label(new Rect(5, 5, 400, 20), "Press 2 to align point clouds with peter ICP");
    //}
}