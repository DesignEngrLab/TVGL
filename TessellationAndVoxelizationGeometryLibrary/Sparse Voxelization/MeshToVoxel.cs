using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;
using TVGL.MathOperations;

namespace TVGL.SparseVoxelization
{
    public class MeshToVoxel
    {
        public TessellatedSolid Solid;   //mMesh 
        public int PolygonLimit;
        public VoxelizationData Data;
        public double VoxelSizeInIntSpace;
        public double ScaleToIntSpace;

        public void VoxelizeMesh(TessellatedSolid solid)
        {
            Solid = solid;
            var dx = solid.XMax - solid.XMin;
            var dy = solid.YMax - solid.YMin;
            var dz = solid.ZMax - solid.ZMin;
            var maxDim = Math.Max(dx, Math.Max(dy, dz));
            var numberOfVoxelsAlongMaxDirection = 10;
            ScaleToIntSpace = numberOfVoxelsAlongMaxDirection / maxDim;
            VoxelSizeInIntSpace = 1.0; 

            Data = new VoxelizationData(); 
            VoxelizePolygons(solid.Faces);
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
            var triangle = new Triangle(face.A.Position, face.B.Position, face.C.Position, ScaleToIntSpace);
            //ToDo: Is polygon count needed? It is not used anywhere.
            var polygonCount = Solid.NumberOfFaces;
            //ToDo: Do I need to consider ever not doing subdivision? Isn't it always required?
            //var subdivisionCount = polygonCount < 1000 ? evalSubdivisionCount(triangle) : 0;
            var subdivisionCount = GetSubdivisionCount(triangle);

            if (subdivisionCount <= 0)
            {
                VoxelizeTriangle(triangle, ref data);
            }
            else
            {
                //ToDo: we still need to voxelize these triangles.
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
        private static void SubdivideAndVoxelizeTriangle(Triangle mainPrim, ref VoxelizationData data, int subdivisionCount, int polygonCount)
        {
            //ToDo: This function was parallel, using a tast.Run for each recursive call. See "spawnTasks"
            subdivisionCount --;
            if (subdivisionCount == 0)
            {
                //VoxelizeTriangle(mainPrim, ref data);
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
            data.SetClosestFaceToVoxel(ijk, primId);
          
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
                if (!data.ClosestFaceToVoxel.ContainsKey(nijk) || primId != data.ClosestFaceToVoxel[nijk])
                {
                    data.SetClosestFaceToVoxel(nijk, primId);
                    ComputeDistance(nijk, prim, ref data);
                }
            }
        }

        /// <summary>
        /// Compute Distance determines whether the voxel is intersected by the primitive.
        /// It also updates a voxel's closest intersecting face index.
        /// </summary>
        private bool ComputeDistance(int[] ijk, Triangle prim, ref VoxelizationData data)
        {
            //uvw is just initialized, not set. 
            double[] uvw;

            //ToDo: figure out voxel coordinates and indices
            //The voxels are set on real world coordinates ?? Voxel Center gets the RealWorld coordinates for the voxel, given its index-space.
            var voxelCenter = new double[] { ijk[0], ijk[1], ijk[2]};

            //Closest Point on Triangle is not restricted to A,B,C. 
            //It may be any point on the triangle.
            //Square this length, so we can compare it to 0.75 rather than  
            //√.75 = 0.866025...
            var dist = MiscFunctions.SquareDistancePointToPoint(voxelCenter, Proximity.ClosestVertexOnTriangleToVertex(prim.A,
                prim.B, prim.C, voxelCenter, out uvw));

            //Get the best distance of this voxel that has been set so far.
            var oldDist = double.MaxValue;
            if (data.VoxelToFaceDistances.ContainsKey(ijk))
            {
                oldDist = data.VoxelToFaceDistances[ijk];
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
                if (prim.ID < data.ClosestFaceToVoxel[ijk])
                {
                    data.SetClosestFaceToVoxel(ijk, prim.ID);
                }
            }

            //This assumes each voxel has a size of 1x1x1.
            //If the squared distance < 0.75 then it must intersect the voxel.
            //Returns true if the face intersects the voxel.
            if(!(dist > 0.75 * VoxelSizeInIntSpace)) data.IntersectingVoxels.Add(ijk);
            return !(dist > 0.75 * VoxelSizeInIntSpace); 
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
        public Dictionary<int[], double> VoxelToFaceDistances;  //distAcc

        /// <summary>
        /// Stores the closest face to the voxel, using the face index
        /// </summary>
        public Dictionary<int[], int> ClosestFaceToVoxel; //primIdAcc

        public HashSet<int[]> IntersectingVoxels;  

        public int PrimCount;

        //ToDo:Set MaxPrimID. Not sure what it is yet.
        public int MaxPrimId;

        public VoxelizationData()
        {
            PrimCount = 0;
            MaxPrimId = 0;
            VoxelToFaceDistances = new Dictionary<int[], double>();
            ClosestFaceToVoxel = new Dictionary<int[], int>();
            IntersectingVoxels = new HashSet<int[]>();
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
            if (ClosestFaceToVoxel.ContainsKey(ijk))
            {
                ClosestFaceToVoxel[ijk] = primId;
            }
            else
            {
                ClosestFaceToVoxel.Add(ijk, primId);
            }
        }

        public void SetVoxelToFaceDistance(int[] ijk, double d)
        {
            if (VoxelToFaceDistances.ContainsKey(ijk))
            {
                VoxelToFaceDistances[ijk] = d;
            }
            else
            {
                VoxelToFaceDistances.Add(ijk, d);
            }
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

        /// <summary>
        ///     The index 
        /// </summary>
        public int ID;

        public Triangle(double[] a, double[] b, double[] c, double scale = 1.0)
        {
            A = a.multiply(scale);
            B = b.multiply(scale);
            C = c.multiply(scale);
            ID = -1;
        }
    }

    public class Coord
    {
        public double X => Coordinates[0];
        public double Y => Coordinates[1];
        public double Z => Coordinates[2];
        public double[] Coordinates;

        public Coord(double[] coordinate)
        {
            Coordinates = coordinate;
        }

        public Coord(double x , double y , double z)
        {
            Coordinates = new[] {x, y, z};
        }

        public Coord(int x, int y, int z)
        {
            Coordinates = new double[] { x, y, z };
        }

        public Coord(IVertex vertex)
        {
            Coordinates = vertex.Position;
        }

        public Coord Add(Coord c)
        {
            return new Coord(Coordinates.add(c.Coordinates));
        }

        public Coord Subtract(Coord c)
        {
            return new Coord(Coordinates.subtract(c.Coordinates));
        }
}

    public class VoxelizationDataType
    {
        
    }
}
