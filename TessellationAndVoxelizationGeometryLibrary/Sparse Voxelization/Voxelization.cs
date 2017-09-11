using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL.MathOperations;
using TVGL.Voxelization;

namespace TVGL.SparseVoxelization
{
    //public class VoxelIndexDictionary
    //{
    //    /// <summary>
    //    /// A dictionary of all the indices. Every X value has a dictionary of Y values.
    //    /// Every Y value has a HashSet of Z values.
    //    /// </summary>
    //    private Dictionary<int, Dictionary<int, HashSet<int>>> XToYToZ;

    //    public VoxelIndexDictionary()
    //    {
    //        XToYToZ = new Dictionary<int, Dictionary<int, HashSet<int>>>();
    //    }

    //    /// <summary>
    //    /// Returns whether the coordinate index is contained in the Dictionary. 
    //    /// Fast iteration, using 2 Dictionary Keys and one HashSet Contains function.
    //    /// </summary>
    //    /// <param name="index"></param>
    //    /// <returns></returns>
    //    public bool Contains(int[] index)
    //    {
    //        var x = index[0];
    //        var y = index[1];
    //        var z = index[2];
    //        if (!XToYToZ.ContainsKey(x)) return false;
    //        var YtoZ = XToYToZ[x];
    //        return YtoZ.ContainsKey(y) && YtoZ[y].Contains(z);
    //    }

    //    /// <summary>
    //    /// Safely adds a voxel index, handles duplicates (never adds one that is already in the 
    //    /// dictionary) 
    //    /// </summary>
    //    /// <param name="index"></param>
    //    public void Add(int[] index)
    //    {
    //        var x = index[0];
    //        var y = index[1];
    //        var z = index[2];
    //        if (XToYToZ.ContainsKey(x))
    //        {
    //            var YtoZ = XToYToZ[x];
    //            if (YtoZ.ContainsKey(y))
    //            {
    //                //We don't need to make a newZ or even check if it is contained, since HashSet.Add()
    //                //prevents duplicates
    //                YtoZ[y].Add(z);
    //            }
    //            else
    //            {
    //                //new Y and new Z for an existing X 
    //                var newZ = new HashSet<int> {z};
    //                YtoZ.Add(y, newZ);
    //            }
    //        }
    //        else
    //        {
    //            var newZ = new HashSet<int> { z };
    //            var newY = new Dictionary<int, HashSet<int>>();
    //            newY.Add(y, newZ);
    //            XToYToZ.Add(x, newY);
    //        }
    //    }
    //}

    public class Coord
    {
        public int[] Index;

        public Coord(int[] xyz, int magnitude )
        {
            Index = xyz;
            coordToOffset(magnitude);
        }

        public long coordToOffset(int Log2Dim)
        {

            Log2Dim = 3;
            var xyz = Index;
            var DIM = 1 << Log2Dim; //Literally shifts the binary by Log2Dim bits. Mathematically: 1*2^Log2Dim
            // dimension along one coordinate direction

            var x = xyz[0] & (DIM - 1u);
            var y = xyz[1] & (DIM - 1u);
            var z = xyz[2] & (DIM - 1u);

            //Shift X value by 2x BitShift, y by 1x, and leave z where it is.
            var value = ((xyz[0] & (DIM - 1u)) << 2 * Log2Dim)    
                + ((xyz[1] & (DIM - 1u)) << Log2Dim)
                + (xyz[2] & (DIM - 1u));

            //assert(n < (1 << 3 * Log2Dim));
            var n = value;
            xyz[0] = (int) (n >> 2 * Log2Dim);
            n &= ((1 << 2 * Log2Dim) - 1);
            xyz[1] = (int)(n >> Log2Dim);
            xyz[2] = (int)(n & ((1 << Log2Dim) - 1));

            return value;

            //Log2Dim = 3;
            //var xyz = Index;
            //var xComp = (xyz[0] << 2*Log2Dim);
            //var yComp = (xyz[1] << Log2Dim);
            //var zComp = (xyz[2]);
            //var all = xComp + yComp + zComp;
        }


       //public int[] offsetToLocalCoord(long index) 
       // {
       //     assert(n < (1 << 3*Log2Dim));
       //     Coord xyz;
       //     xyz.setX(n >> 2*Log2Dim);
       //     n &= ((1 << 2*Log2Dim) - 1);
       //     xyz.setY(n >> Log2Dim);
       //     xyz.setZ(n & ((1 << Log2Dim) - 1));
       //     return xyz;
       // }
    }

    public class Voxel
    {
        public Vertex Center;

        public AABB Bounds { get; set; }

        public int[] Index { get; set; }

        public string StringIndex { get; set; }

        public Voxel(Coord coord, long xm, long ym, double scale)
        {
            var halfLength = scale / 2;
            
            //var x = uniqueCoordIndex/xm;
            //uniqueCoordIndex - x
            //var y = uniqueCoordIndex/ym;
            //var z = (int)(uniqueCoordIndex % ym);
            //uniqueCoordIndex -= z;
            //var y = (int)((uniqueCoordIndex % xm)/ym);
            //uniqueCoordIndex -= y;
            //var x = (int)(uniqueCoordIndex / xm);

            Index = coord.Index;
            Center = new Vertex(new double[] { Index[0] * scale, Index[1] * scale, Index[2] * scale });
            Bounds = new AABB
            {
                MinX = Center.X - halfLength,
                MinY = Center.Y - halfLength,
                MinZ = Center.Z - halfLength,
                MaxX = Center.X + halfLength,
                MaxY = Center.Y + halfLength,
                MaxZ = Center.Z + halfLength
            };
        }
    }

    public class VoxelSpace
    {
        public TessellatedSolid Solid;
        public VoxelizationData Data;
        public double VoxelSizeInIntSpace;
        public double ScaleToIntSpace;
        public List<Voxel> Voxels;
        public int YM;

        /// <summary>
        /// This method creates a hollow voxel grid of a solid. It is done by voxelizing each triangle 
        /// in the solid. To voxelize a triangle, start with the voxel group (3x3x3) that is gauranteed
        /// to intersect the face, then wrap along the face, collecting all the intersecting voxels. 
        /// This method is based on the implementation in OpenVDB, however, none of the code in this file 
        /// is verbatim. It has been simplified and adjusted to work better with exact geometry. The three 
        /// primary differences are 
        /// 
        /// (1) We determine voxel/face intersection using the rounded position of 
        /// the closest point on the triangle, not by comparing its distance to 0.75 (which is the squared
        /// distance from the center of a voxel to a corner). OpenVDB's approach is actually checking if the
        /// face intersects the sphere of squared radius = 0.75, centered at the voxel center. Our implementation
        /// checks for intersection based on the voxel cube, preventing false positives.   
        /// 
        /// (2) We do not consider triangle subdivision. It added complexity and was much slower in every 
        /// test we ran.
        /// 
        /// (3) We handle the voxels as integer strings and have a different set of stored information
        /// Example: OpenVDB captures the closest face to a voxel center, while we keep all the voxel/face intersections.
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="numberOfVoxelsAlongMaxDirection"></param>
        public void VoxelizeSolid(TessellatedSolid solid, int numberOfVoxelsAlongMaxDirection = 100000)
        {
            Solid = solid;
            var dx = solid.XMax - solid.XMin;
            var dy = solid.YMax - solid.YMin;
            var dz = solid.ZMax - solid.ZMin;
            var maxDim = Math.Ceiling(Math.Max(dx, Math.Max(dy, dz)));
            ScaleToIntSpace = numberOfVoxelsAlongMaxDirection / maxDim;

            //To get a unique integer value for each voxel based on its index, 
            //multiply x by the magnitude^2, add y*magnitude, and then add z to get a unique value.
            //Example: for a max magnitude of 1000, with x = 3, y = 345, z = 12
            //3000000 + 345000 + 12 = 3345012 => 3|345|012
            YM = (int)Math.Pow(10, Math.Ceiling(Math.Log10(maxDim*ScaleToIntSpace)));
            var maxInt = Math.Pow(long.MaxValue, 1.0/3);
            if (YM > maxInt) throw new Exception("Int64 will not work for a voxel space this large, using the current index setup"); 
            var XM = (long)Math.Pow(YM, 2);

            VoxelSizeInIntSpace = 1.0;

            Data = new VoxelizationData(XM, YM);
            foreach (var face in solid.Faces)
            {
                //Create a triangle, which is a simple and light version of the face class. 
                //It is required because we need to scale all the vertices/faces.
                var triangle = new Triangle(face, ScaleToIntSpace);
                VoxelizeTriangle(triangle, ref Data);
            }

            //Make all the voxels
            Voxels = new List<Voxel>();
            foreach (var uniqueCoordIndex in Data.IntersectingVoxels)
            {
                uniqueCoordIndex.coordToOffset((int)YM);
                Voxels.Add(new Voxel(uniqueCoordIndex, XM, YM, 1 / ScaleToIntSpace));
            }
        }

        /// <summary>
        /// Voxelizes a triangle starting with the voxel at the Floor of the first point in 
        /// the triangle, and then checks the 26 adjacent voxel. If any of the adjacent voxels
        /// are found to be intersecting, they are added to a stack to search their 26 adjacent 
        /// voxels. In this way, it wraps along the face collecting all the intersecting voxels.
        /// </summary>
        private void VoxelizeTriangle(Triangle triangle, ref VoxelizationData data)
        {
            var consideredVoxels = new HashSet<Coord>();
            var coordindateList = new Stack<int[]>();

            //Gets the integer coordinates, rounded down for point A on the triangle 
            //This is a voxelCenter. We will check all voxels in a -1,+1 box around 
            //this coordinate. 
            var ijk = new[]
            {
                (int)Math.Floor(triangle.A[0]), //X
                (int)Math.Floor(triangle.A[1]), //Y
                (int)Math.Floor(triangle.A[2])  //Z
            };
    

            //Set a new ID. This may not match up with TesselatedSolid.Faces.IDs,
            //if the subdivision of faces is used.
            IsTriangleIntersectingVoxel(ijk, triangle, ref data);
            coordindateList.Push(ijk);
            consideredVoxels.Add(new Coord(ijk, YM));

            while (coordindateList.Any())
            {
                ijk = coordindateList.Pop();
                // For every surrounding voxel (6+12+8=26)
                // 6 Voxel-face adjacent neghbours
                // 12 Voxel-edge adjacent neghbours
                // 8 Voxel-corner adjacent neghbours
                // Voxels are in IntSpace, so we just use 
                // every combination of -1, 0, and 1 for offsets
                for (var i = 0; i < 26; ++i)
                {
                    var nijk = ijk.add(Utilities.CoordinateOffsets[i]);

                    //If the voxel has not already been checked with this primitive,
                    //consider it and add it to the list of considered voxels. 
                    var voxelIndexString = new Coord(nijk, YM);
                    if (!consideredVoxels.Contains(voxelIndexString))
                    {
                        consideredVoxels.Add(voxelIndexString);
                        if (IsTriangleIntersectingVoxel(nijk, triangle, ref data)) coordindateList.Push(nijk);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the voxel is intersected by the triangle.
        /// If it is, it adds the information to the VoxelizationData.
        /// </summary>
        private bool IsTriangleIntersectingVoxel(int[] ijk, Triangle prim, ref VoxelizationData data)
        {
            //Voxel center is simply converting the integers to doubles.
            var voxelCenter = new double[] { ijk[0], ijk[1], ijk[2] };
            var voxelIndex = new Coord(ijk, YM);

            //This assumes each voxel has a size of 1x1x1 and is in an interger grid.
            //First, find the closest point on the triangle to the center of the voxel.
            //Second, if that point is in the same integer grid as the voxel (using regular
            //rounding, not floor or ceiling) then it must intersect the voxel.
            //Closest Point on Triangle is not restricted to A,B,C. 
            //It may be any point on the triangle.
            var closestPoint = Proximity.ClosestVertexOnTriangleToVertex(prim, voxelCenter);
            //var closestPoint = Proximity.ClosestPointOnTriangle(voxelCenter, prim);
            //if (!closestPoint[0].IsPracticallySame(closestPointMethod2[0], 0.001) ||
            //   !closestPoint[1].IsPracticallySame(closestPointMethod2[1], 0.001) ||
            //   !closestPoint[2].IsPracticallySame(closestPointMethod2[2], 0.001))
            //    throw new Exception("Methods do not match");

            if ((int)Math.Round(closestPoint[0]) == ijk[0]
                && (int)Math.Round(closestPoint[1]) == ijk[1]
                && (int)Math.Round(closestPoint[2]) == ijk[2])
            {
                //Since IntersectingVoxels is a hashset, it will not add a duplicate voxel.
                data.IntersectingVoxels.Add(voxelIndex);
                data.AddFaceVoxelIntersection(voxelIndex, prim.ID);
                return true;
            }
            return false;
        }
    }

    /// @brief TBB body object to voxelize a mesh of triangles and/or quads into a collection
    /// of VDB grids, namely a squared distance grid, a closest primitive grid and an
    /// intersecting voxels grid (masks the mesh intersecting voxels)
    /// @note Only the leaf nodes that intersect the mesh are allocated, and only voxels in
    /// a narrow band (of two to three voxels in proximity to the mesh's surface) are activated.
    /// They are populated with distance values and primitive indices.
    public class VoxelizationData
    {
        /// <summary>
        /// Stores the faces that intersect a voxel, using the face index, which is the same
        /// as the the Triangle.ID.  
        /// </summary>                                          
        public readonly Dictionary<Coord, HashSet<int>> FacesIntersectingVoxels;
        public long XM;
        public long YM;

        public HashSet<Coord> IntersectingVoxels;

        public VoxelizationData(long xm, long ym)
        {
            XM = xm;
            YM = ym;
            FacesIntersectingVoxels = new Dictionary<Coord, HashSet<int>>();
            IntersectingVoxels = new HashSet<Coord>();
        }

        /// <summary>
        /// Adds a voxel / face intersection to the dictionary of intersections
        /// </summary>
        /// <param name="voxelIndex"></param>
        /// <param name="primId"></param>
        public void AddFaceVoxelIntersection(Coord voxelIndex, int primId)
        {
            if (FacesIntersectingVoxels.ContainsKey(voxelIndex))
            {
                //HashSets take care of duplicates, so they do not need to be checked
                FacesIntersectingVoxels[voxelIndex].Add(primId);
            }
            else
            {
                FacesIntersectingVoxels.Add(voxelIndex, new HashSet<int> { primId });
            }
        }

        //ToDo: Using a single index value would reduce total voxelization time by 50%
        public long GetUniqueCoordIndexFromIndices(int[] ijk)
        {
            return ijk[0] * XM + ijk[1] * YM + ijk[2];
        }
    }

    /// <summary>
    /// A very light structure, used to represent triangles in the voxelization routines.
    /// </summary>
    public struct Triangle
    {
        /// <summary>
        ///     Gets the first vertex
        /// </summary>
        /// <value>The vertices.</value>
        public double[] A;

        /// <summary>
        ///     Gets the second vertex
        /// </summary>
        /// <value>The vertices.</value>
        public double[] B;

        /// <summary>
        ///     Gets the third vertex
        /// </summary>
        /// <value>The vertices.</value>
        public double[] C;

        public double[] Normal;

        /// <summary>
        ///     The index 
        /// </summary>
        public int ID;

        public List<double[]> Triangle2D { get; set; }
        public Polygon Polygon2D { get; set; }
        public double[,] RotTransMatrixTo2D { get; set; }
        public double[,] RotTransMatrixTo3D { get; set; }
        public Dictionary<int, List<Line>> PerpendicularLines { get; set; }

        /// <summary>
        /// Builds a Triangle from points. Scales accordingly.
        /// </summary>
        public Triangle(double[] a, double[] b, double[] c, double[] normal, double scale = 1.0)
        {
            A = a.multiply(scale);
            B = b.multiply(scale);
            C = c.multiply(scale);
            Normal = normal;
            ID = -1;

            Triangle2D = null;
            Polygon2D = null;
            RotTransMatrixTo2D = new double[,] { };
            RotTransMatrixTo3D = new double[,] { };
            PerpendicularLines = new Dictionary<int, List<Line>>();
            //SetTransformationMatrix();
        }

        /// <summary>
        /// Builds a Triangle from a face. Scales accordingly.
        /// </summary>
        /// <param name="face"></param>
        /// <param name="scale"></param>
        public Triangle(PolygonalFace face, double scale = 1.0)
        {
            A = face.A.Position.multiply(scale);
            B = face.B.Position.multiply(scale);
            C = face.C.Position.multiply(scale);
            Normal = face.Normal;
            ID = face.IndexInList;

            Triangle2D = null;
            Polygon2D = null;
            RotTransMatrixTo2D = new double[,] { };
            RotTransMatrixTo3D = new double[,] { };
            PerpendicularLines = new Dictionary<int, List<Line>>();
            //SetTransformationMatrix();
        }

        public void SetTransformationMatrix()
        {
            //ToDo: precompute the transformation matrix for every triangle
            //Calculate the translation and rotation matrices so that A lies on the origin, B, 
            //lies on the Y axis, and C lies in the zy plane.
            var xDir = B[0] - A[0];
            var yDir = B[1] - A[1];
            var zDir = B[2] - A[2];
            var originToB = Math.Sqrt(xDir * xDir + yDir * yDir + zDir * zDir);

            //Get the transformation matrix
            var transformMatrix = StarMath.makeIdentity(4);
            transformMatrix[0, 3] = -A[0];
            transformMatrix[1, 3] = -A[1];
            transformMatrix[2, 3] = -A[2];

            var tempB = new[]
            {
                xDir, yDir, zDir, 1.0
            };

            var showTrianglesForDebug = false;
            //if (ID == 29)
            //{
            //    showTrianglesForDebug = true;
            //    Debug.WriteLine("Error Triangle Reached");
            //}

            //Rotate Z, then X, then Y
            double[,] rotateX, rotateY, rotateZ, backRotateZ, backRotateX, backRotateY;
            //If xDir and zDir are negligible, then B is already in the correct position, 
            //we just need to rotate in the y direction to put C on the zy plane.
            if (xDir.IsNegligible() && zDir.IsNegligible())
            {
                backRotateX = rotateX = backRotateZ = rotateZ = StarMath.makeIdentity(4);
            }
            //If zDir and yDir are negligible, then point B lies along the X axis
            //Rotate PI/2*Sign(xDir) on the Z axis 
            else if (zDir.IsNegligible() && yDir.IsNegligible())
            {
                rotateZ = StarMath.RotationZ(Math.Sign(xDir) * Math.PI / 2, true);
                backRotateZ = StarMath.RotationZ(-Math.Sign(xDir) * Math.PI / 2, true);

                //var tempB2 = rotateZ.multiply(tempB);
                backRotateX = rotateX = StarMath.makeIdentity(4);
            }
            //If xDir and yDir are negligible, then point B lies along the Z axis
            //Rotate PI/2*Sign(xDir) on the X axis 
            else if (xDir.IsNegligible() && yDir.IsNegligible())
            {
                backRotateZ = rotateZ = StarMath.makeIdentity(4);

                rotateX = StarMath.RotationX(-Math.Sign(zDir) * Math.PI / 2, true);
                backRotateX = StarMath.RotationX(Math.Sign(zDir) * Math.PI / 2, true);

                //var tempB2 = rotateX.multiply(tempB);
            }
            //Point B lies on the xy plane, X rotation is zero.
            else if (zDir.IsNegligible())
            {
                var rotZAngle = Math.Atan(xDir / yDir);
                rotateZ = StarMath.RotationZ(rotZAngle, true);
                backRotateZ = StarMath.RotationZ(-rotZAngle, true);

                backRotateX = rotateX = StarMath.makeIdentity(4);
            }
            //Point B lies on the yx plane, Z rotation is zero.
            else if (xDir.IsNegligible())
            {
                backRotateZ = rotateZ = StarMath.makeIdentity(4);

                var rotXAngle = -Math.Atan(zDir / yDir);
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);

                //var tempB2 = rotateX.multiply(tempB);
            }
            //Point B lies on the xz plane. Z rotation is PI/2 and X rotation is simple.
            else if (yDir.IsNegligible())
            {
                //Rotate on Z to put B in the Positive Y direction
                rotateZ = StarMath.RotationZ(Math.Sign(xDir) * Math.PI / 2, true);
                backRotateZ = StarMath.RotationZ(-Math.Sign(xDir) * Math.PI / 2, true);

                //var tempB2 = rotateZ.multiply(tempB);

                var rotXAngle = -Math.Atan(zDir / Math.Abs(xDir));
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);

                //var tempB3 = rotateX.multiply(tempB2);
            }
            else
            {
                //If Sign(yDir) = 1, then B will be rotated toward the positive Y axis, since it is closest.
                //If = 1-, then it will go to the negative Y axis. 
                var rotZAngle = Math.Atan(xDir / yDir);
                rotateZ = StarMath.RotationZ(rotZAngle, true);
                backRotateZ = StarMath.RotationZ(-rotZAngle, true);


                var rotXAngle = -Math.Sign(yDir) * Math.Asin(zDir / originToB);
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }

            //Do the final rotation to put point C on the zy plane. 
            //First, apply the transformation thus far
            var tempR = rotateX.multiply(rotateZ);
            var oldCPosition = new[]
            {
                C[0], C[1], C[2], 1.0
            };
            var tempC = tempR.multiply(transformMatrix.multiply(oldCPosition));
            //Then, rotate along the yAxis. 
            //If C has a negligible Z value, then rotate by Pi/2 
            if (tempC[2].IsNegligible())
            {
                rotateY = StarMath.RotationY(-Math.Sign(tempC[0]) * Math.PI / 2, true);
                backRotateY = StarMath.RotationY(Math.Sign(tempC[0]) * Math.PI / 2, true);
            }
            else
            {
                var rotYAngle = -Math.Atan(tempC[0] / tempC[2]); // -arcTan(X/Z)
                rotateY = StarMath.RotationY(rotYAngle, true);
                backRotateY = StarMath.RotationY(-rotYAngle, true);

                //var tempC2 = rotateY.multiply(tempC);
            }

            //Transformation Matrices read from right to left. So first, transform so that point A is at the origin, 
            //Then rotate Z, X, and lastly Y.
            var rotationMatrix = rotateY.multiply(rotateX.multiply(rotateZ));
            RotTransMatrixTo2D = rotationMatrix.multiply(transformMatrix);
            transformMatrix = StarMath.makeIdentity(4);
            transformMatrix[0, 3] = A[0];
            transformMatrix[1, 3] = A[1];
            transformMatrix[2, 3] = A[2];
            RotTransMatrixTo3D = transformMatrix.multiply(backRotateZ.multiply(backRotateX.multiply(backRotateY)));

            //ZY plane
            var oldAPosition = new[]
            {
                   A[0], A[1], A[2] , 1.0
            };
            var newALocation = RotTransMatrixTo2D.multiply(oldAPosition);
            //var testA = RotTransMatrixTo3D.multiply(newALocation);
            if (!newALocation[0].IsNegligible(0.000001) &&
                !newALocation[1].IsNegligible(0.000001) &&
                !newALocation[2].IsNegligible(0.000001))
            {
                showTrianglesForDebug = true;
                Debug.WriteLine("Point A should be on the origin");
            }
            var aPrime = new Point(newALocation[1], newALocation[2]);

            var oldBPosition = new[]
            {
                   B[0], B[1], B[2] , 1.0
            };
            var newBLocation = RotTransMatrixTo2D.multiply(oldBPosition);
            //var testB = RotTransMatrixTo3D.multiply(newBLocation);
            if (!newBLocation[0].IsNegligible() && !newBLocation[2].IsNegligible(0.000001))
            {
                showTrianglesForDebug = true;
                Debug.WriteLine("Point B should be on the Y axis, and have X = Z = 0");
            }
            var bPrime = new Point(newBLocation[1], newBLocation[2]);

            var newCLocation = RotTransMatrixTo2D.multiply(oldCPosition);
            //var testC = RotTransMatrixTo3D.multiply(newCLocation);
            if (!newCLocation[0].IsNegligible(0.000001))
            {
                showTrianglesForDebug = true;
                Debug.WriteLine("Point C should be on the YZ plane (X = 0)");
            }
            var cPrime = new Point(newCLocation[1], newCLocation[2]);


            Triangle2D = new List<double[]>() { newALocation, newBLocation, newCLocation };


            //If the 2D version of the new point is inside the new 2D triangle,
            //Then the distance is simply its X value.
            Polygon2D = new Polygon(new List<Point>() { aPrime, bPrime, cPrime });
            //Make sure the polygon is positive, in case it got rotated so that it was backwards.
            if (!Polygon2D.IsPositive) Polygon2D.Reverse();
            var oldVertexList = new List<Vertex>() { new Vertex(A), new Vertex(B), new Vertex(C) };
            //var oldArea = MiscFunctions.AreaOf3DPolygon(oldVertexList, Normal);
            //if (!Polygon2D.Area.IsPracticallySame(oldArea, 0.1*oldArea)) Debug.WriteLine("Areas do not match");
            Polygon2D.SetPathLines();


            foreach (var line in Polygon2D.PathLines)
            {
                var leftHandPerpendicular = new[] { line.dY, -line.dX };
                var leftHandPerpendicularLines = new List<Line>
                {
                    new Line(line.FromPoint, leftHandPerpendicular),
                    new Line(line.ToPoint, leftHandPerpendicular)
                };
                PerpendicularLines.Add(line.IndexInList, leftHandPerpendicularLines);
            }

            //if (showTrianglesForDebug)
            //{
            //    var allPathsOfInterest = new List<List<double[]>> { Triangle2D, new List<double[]> { A, B, C } };
            //    Presenter.ShowVertexPaths(allPathsOfInterest);
            //}
        }
    }
}
