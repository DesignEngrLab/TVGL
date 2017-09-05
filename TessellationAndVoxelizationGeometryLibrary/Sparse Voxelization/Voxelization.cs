using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        public TessellatedSolid Solid; 
        public VoxelizationData Data;
        public double VoxelSizeInIntSpace;
        public double ScaleToIntSpace;
        public List<Voxel> Voxels;

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
        public void VoxelizeSolid(TessellatedSolid solid, int numberOfVoxelsAlongMaxDirection = 50)
        {
            Solid = solid;
            var dx = solid.XMax - solid.XMin;
            var dy = solid.YMax - solid.YMin;
            var dz = solid.ZMax - solid.ZMin;
            var maxDim = Math.Ceiling(Math.Max(dx, Math.Max(dy, dz)));
            ScaleToIntSpace = numberOfVoxelsAlongMaxDirection / maxDim;

            VoxelSizeInIntSpace = 1.0; 

            Data = new VoxelizationData();
            foreach (var face in solid.Faces)
            {
                //Create a triangle, which is a simple and light version of the face class. 
                //It is required because we need to scale all the vertices/faces.
                var triangle = new Triangle(face, ScaleToIntSpace);
                VoxelizeTriangle(triangle, ref Data);
            }

            //Make all the voxels
            Voxels = new List<Voxel>();
            foreach (var voxelString in Data.IntersectingVoxels)
            {
                Voxels.Add(new Voxel(voxelString, 1/ScaleToIntSpace));
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
            var consideredVoxels = new HashSet<string>();
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
            consideredVoxels.Add(data.GetStringFromIndex(ijk));

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
                    var voxelIndexString = data.GetStringFromIndex(nijk);
                    if (!consideredVoxels.Contains(voxelIndexString))
                    {
                        consideredVoxels.Add(voxelIndexString);
                        if(IsTriangleIntersectingVoxel(nijk, triangle, ref data)) coordindateList.Push(nijk);
                    }
                }
            }
        }

        /// <summary>
        /// Determines whether the voxel is intersected by the triangle.
        /// If it is, it adds the information to the VoxelizationData.
        /// </summary>
        private static bool IsTriangleIntersectingVoxel(int[] ijk, Triangle prim, ref VoxelizationData data)
        {
            //Voxel center is simply converting the integers to doubles.
            var voxelCenter = new double[] { ijk[0], ijk[1], ijk[2]};
            var voxelIndexString = data.GetStringFromIndex(ijk);

            //This assumes each voxel has a size of 1x1x1 and is in an interger grid.
            //First, find the closest point on the triangle to the center of the voxel.
            //Second, if that point is in the same integer grid as the voxel (using regular
            //rounding, not floor or ceiling) then it must intersect the voxel.
            //Closest Point on Triangle is not restricted to A,B,C. 
            //It may be any point on the triangle.
            var closestPoint = Proximity.ClosestVertexOnTriangleToVertex(prim, voxelCenter);
            if ((int)Math.Round(closestPoint[0]) == ijk[0]
                && (int)Math.Round(closestPoint[1]) == ijk[1]
                && (int)Math.Round(closestPoint[2]) == ijk[2])
            {
                //Since IntersectingVoxels is a hashset, it will not add a duplicate voxel.
                data.IntersectingVoxels.Add(voxelIndexString);
                data.AddFaceVoxelIntersection(voxelIndexString, prim.ID);
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
        public readonly Dictionary<string, HashSet<int>> FacesIntersectingVoxels; 

        public HashSet<string> IntersectingVoxels;  

        public VoxelizationData()
        {
 
            FacesIntersectingVoxels = new Dictionary<string, HashSet<int>>();
            IntersectingVoxels = new HashSet<string>();
        }

        /// <summary>
        /// Adds a voxel / face intersection to the dictionary of intersections
        /// </summary>
        /// <param name="voxelIndex"></param>
        /// <param name="primId"></param>
        public void AddFaceVoxelIntersection(string voxelIndex, int primId)
        {
            if (FacesIntersectingVoxels.ContainsKey(voxelIndex))
            {
                //HashSets take care of duplicates, so they do not need to be checked
                FacesIntersectingVoxels[voxelIndex].Add(primId);
            }
            else
            {
                FacesIntersectingVoxels.Add(voxelIndex, new HashSet<int>{primId});
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
        }
    }
}
