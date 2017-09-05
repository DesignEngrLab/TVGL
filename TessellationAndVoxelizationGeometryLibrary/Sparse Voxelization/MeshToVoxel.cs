using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.MathOperations;
using TVGL.Voxelization;

namespace TVGL.SparseVoxelization
{
    public class Voxel
    {
        public Vertex Center;

        public AABB Bounds { get; set; }

        public int[] Index { get; set; }

        public string StringIndex { get; set; }

        public Voxel(string voxelString, double scale)
        {
            StringIndex = voxelString;

            var halfLength = scale/2;
            string[] words = voxelString.Split('|');
            Index = new int[3];
            for (var i = 0; i < 3; i++)
            {
                Index[i] = int.Parse(words[i]) ;
                if(Index[i] < 0) Debug.WriteLine("Negative Values work properly");
            }

            Center = new Vertex(new double[] { Index[0]*scale, Index[1]*scale, Index[2]*scale});
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
        public TessellatedSolid Solid;   //mMesh 
        public int PolygonLimit;
        public VoxelizationData Data;
        public double VoxelSizeInIntSpace;
        public double ScaleToIntSpace;
        public List<Voxel> Voxels;

        public void VoxelizeMesh(TessellatedSolid solid)
        {
            PolygonLimit = 50;
            Solid = solid;
            var dx = solid.XMax - solid.XMin;
            var dy = solid.YMax - solid.YMin;
            var dz = solid.ZMax - solid.ZMin;
            var maxDim = Math.Ceiling(Math.Max(dx, Math.Max(dy, dz)));
            var numberOfVoxelsAlongMaxDirection = 100;
            ScaleToIntSpace = numberOfVoxelsAlongMaxDirection / maxDim;

            VoxelSizeInIntSpace = 1.0; 

            Data = new VoxelizationData(); 
            VoxelizePolygons(solid.Faces);

            //Make all the voxels
            Voxels = new List<Voxel>();
            foreach (var voxelString in Data.IntersectingVoxels)
            {
                Voxels.Add(new Voxel(voxelString, 1/ScaleToIntSpace));
            }
        }

        private void VoxelizePolygons(IEnumerable<PolygonalFace> triangles)
        {
            var data = Data;
            foreach (var t in triangles)
            {
                if (t.Vertices.Count != 3) throw new NotImplementedException();
                EvaluateTriangle(t, ref data);
            }
        }

        private void EvaluateTriangle(PolygonalFace face, ref VoxelizationData data)
        {
            var triangle = new Triangle(face.A.Position, face.B.Position, face.C.Position, face.Normal, ScaleToIntSpace);
            var polygonCount = Solid.NumberOfFaces;

            //Note: Subdividing takes much longer than using the coordinate stack in Voxelize Triangle.
            //Only do subdivision if the solid is not detailed enough (less than the polygon limit)
            var subdivisionCount = polygonCount < PolygonLimit ? GetSubdivisionCount(triangle) : 0;

            if (subdivisionCount <= 0)
            {
                VoxelizeTriangle(triangle, ref data);
            }
            else
            {

                SubdivideAndVoxelizeTriangle(triangle, ref data, subdivisionCount, polygonCount);
            }
        }

        private int GetSubdivisionCount(Triangle prim)
        {
            double ax = prim.A[0], bx = prim.B[0], cx = prim.C[0];
            var dx = Math.Max(ax, Math.Max(bx, cx)) - Math.Min(ax, Math.Min(bx, cx));

            double ay = prim.A[1], by = prim.B[1], cy = prim.C[1];
            var dy = Math.Max(ay, Math.Max(by, cy)) - Math.Min(ay, Math.Min(by, cy));

            double az = prim.A[2], bz = prim.B[2], cz = prim.C[2];
            var dz = Math.Max(az, Math.Max(bz, cz)) - Math.Min(az, Math.Min(bz, cz));

            return (int)(Math.Max(dx, Math.Max(dy, dz)) / (VoxelSizeInIntSpace * 2));
        }

        /// <summary>
        /// This is a recursive operation to subdivide a triangle into four smaller triangles, reducing
        /// the max dim of a triangle by half with each division.
        /// </summary>
        private void SubdivideAndVoxelizeTriangle(Triangle mainPrim, ref VoxelizationData data, int subdivisionCount, int polygonCount)
        {
            //ToDo: This function was parallel, using a tast.Run for each recursive call. See "spawnTasks"
            subdivisionCount --;
            if (subdivisionCount == 0)
            {
                VoxelizeTriangle(mainPrim, ref data);
                return;
            }
            polygonCount *= 4; //Number of polygons goes up by four.

            var ac = mainPrim.A.add(mainPrim.C).multiply(0.5);
            var bc = mainPrim.B.add(mainPrim.C).multiply(0.5);
            var ab = mainPrim.A.add(mainPrim.B).multiply(0.5);

            var prim = new Triangle();
            prim.ID = mainPrim.ID;

            prim.A = mainPrim.A;
            prim.B = ab;
            prim.C = ac;
            SubdivideAndVoxelizeTriangle(prim, ref data, subdivisionCount, polygonCount);

            prim.A = ab;
            prim.B = bc;
            prim.C = ac;
            SubdivideAndVoxelizeTriangle(prim, ref data, subdivisionCount, polygonCount);

            prim.A = ab;
            prim.B = mainPrim.B;
            prim.C = bc;
            SubdivideAndVoxelizeTriangle(prim, ref data, subdivisionCount, polygonCount);

            prim.A = ac;
            prim.B = bc;
            prim.C = mainPrim.C;
            SubdivideAndVoxelizeTriangle(prim, ref data, subdivisionCount, polygonCount);
        }

        /// <summary>
        /// Voxelizes a triangle, assuming the triangle has no dimension larger than
        /// a voxel side. It starts with the voxel at the Floor of the first point in 
        /// the triangle, and then checks the 26 adjacent voxel. The face must be fully
        /// encompassed in these voxels, since it is smaller than a voxel.
        /// </summary>
        private void VoxelizeTriangle(Triangle prim, ref VoxelizationData data)
        {
            var coordindateList = new Stack<int[]>();

            //Gets the integer coordinates, rounded down for point A on the triangle 
            //This is a voxelCenter. We will check all voxels in a -1,+1 box around 
            //this coordinate. 
            var ijk = new[] 
            {
                (int)Math.Floor(prim.A[0]),
                (int)Math.Floor(prim.A[1]),
                (int)Math.Floor(prim.A[2])
            };

            //Set a new ID. This does not match up with TesselatedSolid.Faces.IDs,
            //Because of the subdivision of faces.
            var primId = data.GetNewPrimId();
            prim.ID = primId;
            ComputeDistance(ijk, prim, ref data);
            data.Triangles.Add(primId, prim);
            data.SetClosestFaceToVoxel(ijk, primId);
            coordindateList.Push(ijk);

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
                    //Locate the voxel using the primitive ID accessor for voxels (stored in data)
                    //If the voxel has not already been checked with this primitive,
                    //set the primIdAcc so that we don't repeat this voxel for this primitive. 
                    var voxelIndexString = data.GetStringFromIndex(nijk);
                    if (!data.ClosestFaceToVoxel.ContainsKey(voxelIndexString) || primId != data.ClosestFaceToVoxel[voxelIndexString])
                    {
                        data.SetClosestFaceToVoxel(nijk, primId);
                        if(ComputeDistance(nijk, prim, ref data)) coordindateList.Push(nijk);
                    }
                }
            }
        }

        /// <summary>
        /// Compute Distance determines whether the voxel is intersected by the primitive.
        /// It also updates a voxel's closest intersecting face index.
        /// </summary>
        private bool ComputeDistance(int[] ijk, Triangle prim, ref VoxelizationData data)
        {
            //Voxel center is simply converting the integers to doubles.
            var voxelCenter = new double[] { ijk[0], ijk[1], ijk[2]};

            //Closest Point on Triangle is not restricted to A,B,C. 
            //It may be any point on the triangle.
            //Square this length, so we can compare it to 0.75 rather than  
            //√.75 = 0.866025...
            var dist = MiscFunctions.DistancePointToPoint(voxelCenter, 
                Proximity.ClosestVertexOnTriangleToVertex(prim, voxelCenter));
            //var dist = Proximity.SquareDistancesPointToTriangle(voxelCenter, prim);


            //Get the best distance of this voxel that has been set so far.
            var oldDist = double.MaxValue;
            var voxelIndexString = data.GetStringFromIndex(ijk);
            if (data.VoxelToFaceDistances.ContainsKey(voxelIndexString))
            {
                oldDist = data.VoxelToFaceDistances[voxelIndexString];
            }

            //If the distance of the voxel to this triangle is closer than the 
            //min distance thus far, update the best distance and attach the voxel
            //to this primitive.
            if (dist < oldDist)
            {
                data.SetVoxelToFaceDistance(ijk, dist);
                data.SetClosestFaceToVoxel(ijk, prim.ID);
            }
            else if (dist.IsPracticallySame(oldDist))
            {
                // Make the reduction deterministic when different polygons are equally
                // close to this voxel. Arbitrarily choose the face with the lower index.
                if (prim.ID < data.ClosestFaceToVoxel[voxelIndexString])
                {
                    data.SetClosestFaceToVoxel(ijk, prim.ID);
                }
            }

            //This assumes each voxel has a size of 1x1x1.
            //If the squared distance < 0.75 then it must intersect the voxel.
            //Returns true if the face intersects the voxel.
            //if (dist > 0.75) return false;

            var closestPoint = Proximity.ClosestVertexOnTriangleToVertex(prim, voxelCenter);
            if (Math.Round(closestPoint[0]) == ijk[0]
                && Math.Round(closestPoint[1]) == ijk[1]
                && Math.Round(closestPoint[2]) == ijk[2])
            {
                //Since IntersectingVoxels is a hashset, it will not add a duplicate voxel.
                data.IntersectingVoxels.Add(voxelIndexString);
                return true;
            }
            else
            {
                return false;
            }
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
        //TreeType distTree;
        //FloatTreeAcc distAcc;

        //Int32TreeType indexTree;
        //Int32TreeAcc indexAcc;

        //UCharTreeType primIdTree;
        //UCharTreeAcc primIdAcc;

        /// <summary>
        /// Stores the min distance found between the voxel and any face
        /// </summary>
        public Dictionary<string, double> VoxelToFaceDistances;  //distAcc

        /// <summary>
        /// Stores the closest face to the voxel, using the face index
        /// </summary>
        public Dictionary<string, int> ClosestFaceToVoxel; //primIdAcc

        public Dictionary<int, Triangle> Triangles; 

        public HashSet<string> IntersectingVoxels;  

        public int PrimCount;

        //ToDo:Set MaxPrimID. Not sure what it is yet.
        public int MaxPrimId;

        public VoxelizationData()
        {
            PrimCount = 0;
            MaxPrimId = 0;
            VoxelToFaceDistances = new Dictionary<string, double>();
            ClosestFaceToVoxel = new Dictionary<string, int>();
            IntersectingVoxels = new HashSet<string>();
            Triangles = new Dictionary<int, Triangle>();
        }

        public int GetNewPrimId()
        {
            //if (PrimCount == MaxPrimId || primIdTree.leafCount() > 1000)
            //{
            //    PrimCount = 0;
            //    primIdTree.clear();
            //}

            return PrimCount++;
        }

        public void SetClosestFaceToVoxel(int[] ijk, int primId)
        {
            var stringIndex = GetStringFromIndex(ijk);
            if (ClosestFaceToVoxel.ContainsKey(stringIndex))
            {
                ClosestFaceToVoxel[stringIndex] = primId;
            }
            else
            {
                ClosestFaceToVoxel.Add(stringIndex, primId);
            }
        }

        public void SetVoxelToFaceDistance(int[] ijk, double d)
        {
            var stringIndex = GetStringFromIndex(ijk);
            if (VoxelToFaceDistances.ContainsKey(stringIndex))
            {
                VoxelToFaceDistances[stringIndex] = d;
            }
            else
            {
                VoxelToFaceDistances.Add(stringIndex, d);
            }
        }
        public string GetStringFromIndex(int[] ijk)
        {
            return ijk[0] + "|"
                + ijk[1] + "|"
                + ijk[2];
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

        public Triangle(double[] a, double[] b, double[] c, double[] normal, double scale = 1.0)
        {
            A = a.multiply(scale);
            B = b.multiply(scale);
            C = c.multiply(scale);
            Normal = normal;
            ID = -1;
        }
    }
}
