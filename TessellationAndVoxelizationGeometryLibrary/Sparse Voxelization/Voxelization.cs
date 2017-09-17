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
        public double[] Center;

        public AABB Bounds { get; set; }

        public int[] Index { get; set; }

        public long ID { get; set; }
        public List<TessellationBaseClass> TessellationElements { get; internal set; }

        public Voxel(int[] index, long ID, double voxelLength, TessellationBaseClass tessellationObject = null)
        {
            var halfLength = voxelLength / 2;
            TessellationElements = new List<TessellationBaseClass>();
            if (tessellationObject != null) TessellationElements.Add(tessellationObject);
            Index = index;
            this.ID = ID;
            Center = new[] { Index[0] * voxelLength, Index[1] * voxelLength, Index[2] * voxelLength };
            Bounds = new AABB
            {
                MinX = Center[0] - halfLength,
                MinY = Center[1] - halfLength,
                MinZ = Center[2] - halfLength,
                MaxX = Center[0] + halfLength,
                MaxY = Center[1] + halfLength,
                MaxZ = Center[2] + halfLength
            };
        }
    }

    public class VoxelSpace
    {
        public TessellatedSolid Solid;
        public HashSet<long> VoxelIDHashSet;

        /// <summary>
        /// Stores the faces that intersect a voxel, using the face index, which is the same
        /// as the the Triangle.ID.  
        /// </summary>                                          
        public Dictionary<long, Voxel> Voxels;

        long xIndexOffset;
        long yIndexOffset;
        long signIndexOffset;
        double voxelLength;
        public double ScaleToIntSpace;

        /// <summary>
        /// This method creates a hollow voxel grid of a solid. It is done by voxelizing each triangle 
        /// in the solid. To voxelize a triangle, start with the voxel group (3x3x3) that is guaranteed
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
        /// <param name="ts"></param>
        /// <param name="numberOfVoxelsAlongMaxDirection"></param>
        public void VoxelizeSolid(TessellatedSolid ts, int numberOfVoxelsAlongMaxDirection = 100)
        {
            Solid = ts;
            SetUpIndexingParameters(numberOfVoxelsAlongMaxDirection);
            Voxels = new Dictionary<long, Voxel>(); //todo:approximate capacity based on tessellated volume
            VoxelIDHashSet = new HashSet<long>();
            var vertices = new double[Solid.NumberOfVertices][];
            for (int i = 0; i < Solid.NumberOfVertices; i++)
            {
                var vertex = Solid.Vertices[i];
                var coord = vertex.Position.multiply(ScaleToIntSpace);
                //Gets the integer coordinates, rounded down for point A on the triangle 
                //This is a voxelCenter. We will check all voxels in a -1,+1 box around 
                //this coordinate. 
                var ijk = new[]
                { (int) Math.Round(coord[0]),(int) Math.Round(coord[1]),(int) Math.Round(coord[2]) };
                storeVoxel(ijk, vertex);
                vertices[i] = coord;
            }

            foreach (var face in Solid.Faces)
            {
                if (noIntermediateVoxels(face)) continue;
                var xLength = Math.Max(Math.Max(Math.Abs(face.A.X - face.B.X), Math.Abs(face.B.X - face.C.X)),
                    Math.Abs(face.C.X - face.A.X));
                var yLength = Math.Max(Math.Max(Math.Abs(face.A.Y - face.B.Y), Math.Abs(face.B.Y - face.C.Y)),
                    Math.Abs(face.C.Y - face.A.Y));
                var zLength = Math.Max(Math.Max(Math.Abs(face.A.Z - face.B.Z), Math.Abs(face.B.Z - face.C.Z)),
                    Math.Abs(face.C.Z - face.A.Z));
                var sweepDim = 0;
                var dim1 = 1;
                var dim2 = 2;
                if (yLength > xLength)
                {
                    sweepDim = 1;
                    dim1 = 2;
                    dim2 = 0;
                }
                if (zLength > yLength && zLength > xLength)
                {
                    sweepDim = 2;
                    dim1 = 0;
                    dim2 = 1;
                }
                var startVertex = face.A;
                var leftVertex = face.B;
                var leftEdge = face.Edges[0];
                var rightVertex = face.C;
                var rightEdge = face.Edges[2];
                if (face.B.Position[sweepDim] < face.A.Position[sweepDim])
                {
                    startVertex = face.B;
                    leftVertex = face.C;
                    leftEdge = face.Edges[1];
                    rightVertex = face.A;
                    rightEdge = face.Edges[0];
                }
                if (face.C.Position[sweepDim] < face.B.Position[sweepDim] &&
                    face.C.Position[sweepDim] < face.A.Position[sweepDim])
                {
                    startVertex = face.C;
                    leftVertex = face.A;
                    leftEdge = face.Edges[2];
                    rightVertex = face.B;
                    rightEdge = face.Edges[1];
                }
                var startPoint = vertices[startVertex.IndexInList];
                var valueSweepDim = (int)Math.Ceiling(startPoint[sweepDim]);
                var leftPoint = vertices[leftVertex.IndexInList];
                double leftCoordD1, leftCoordD2;
                findWhereLineCrossesPlane(startPoint, leftPoint, sweepDim, valueSweepDim, out leftCoordD1, out leftCoordD2);
                getIDsIn2DPlane(startPoint[dim1], startPoint[dim2], leftCoordD1, leftCoordD2, valueSweepDim, dim1, dim2, sweepDim, leftEdge);
                getIDsIn2DPlane(startPoint[dim2], startPoint[dim1], leftCoordD2, leftCoordD1, valueSweepDim, dim2, dim1, sweepDim, leftEdge);

                var rightPoint = vertices[rightVertex.IndexInList];
                double rightCoordD1, rightCoordD2;
                findWhereLineCrossesPlane(startPoint, rightPoint, sweepDim, valueSweepDim, out rightCoordD1, out rightCoordD2);
                getIDsIn2DPlane(startPoint[dim1], startPoint[dim2], rightCoordD1, rightCoordD2, valueSweepDim, dim1, dim2, sweepDim, rightEdge);
                getIDsIn2DPlane(startPoint[dim2], startPoint[dim1], rightCoordD2, rightCoordD1, valueSweepDim, dim2, dim1, sweepDim, rightEdge);

                getIDsIn2DPlane(leftCoordD1, leftCoordD2, rightCoordD1, rightCoordD2, valueSweepDim, dim1, dim2, sweepDim, face);
                getIDsIn2DPlane(leftCoordD2, leftCoordD1, rightCoordD2, rightCoordD1, valueSweepDim, dim2, dim1, sweepDim, face);
            }
        }

        private bool noIntermediateVoxels(PolygonalFace face)
        {
            if (face.A.Voxels[0] == face.B.Voxels[0] && face.A.Voxels[0] == face.C.Voxels[0])
            {
                var voxel = face.A.Voxels[0];
                face.AddVoxel(voxel);
                foreach (var edge in face.Edges)
                    edge.AddVoxel(voxel);
                return true;
            }
            /*
            else if (face.A.Voxel.Index[0] == face.B.Voxel.Index[0] && face.A.Voxel.Index[0] == face.C.Voxel.Index[0] &&
                     face.A.Voxel.Index[1] == face.B.Voxel.Index[1] && face.A.Voxel.Index[1] == face.C.Voxel.Index[1] &&
                     face.A.Voxel.Index[2] == face.B.Voxel.Index[2] && face.A.Voxel.Index[2] == face.C.Voxel.Index[2])
            {

            }
            */
            return false;
        }

        private void getIDsIn2DPlane(double startX, double startY, double endX, double endY, int valueSweepDim,
            int xDim, int yDim, int sweepDim, TessellationBaseClass tsObject)
        {
            var xRange = endX - startX;
            if (Math.Abs(xRange) < 0.5) return; //then there are not crossings
            var yRange = endY - startY;
            var increment = Math.Sign(xRange);
            var nextX = startX;
            var ijk = new int[3];
            do
            {
                nextX += increment;
                var x = Math.Round(nextX);
                var y = (int)((yRange * (x - startX) / xRange) + startY);
                if (y <= endY)
                {
                    ijk[xDim] = (int)x;
                    ijk[yDim] = y;
                    ijk[sweepDim] = valueSweepDim;
                    storeVoxel(ijk, tsObject);
                }
                else return;
            } while (increment * nextX < increment * endX);
        }

        private void storeVoxel(int[] ijk, TessellationBaseClass tsObject)
        {
            var voxelID = IndicesToVoxelID(ijk);
            Voxel voxel;
            if (VoxelIDHashSet.Contains(voxelID))
            {
                voxel = Voxels[voxelID];
                if (voxel.TessellationElements.Contains(tsObject))
                    return;
                voxel.TessellationElements.Add(tsObject);
            }
            else
            {
                VoxelIDHashSet.Add(voxelID);
                voxel = new Voxel(ijk, voxelID, voxelLength, tsObject);
                Voxels.Add(voxelID, voxel);
            }
            tsObject.AddVoxel(voxel);
        }

        private void findWhereLineCrossesPlane(double[] startPoint, double[] endPoint, int sweepDim, double valueSweepDim, out double valueD1, out double valueD2)
        {
            if (endPoint[sweepDim] < valueSweepDim || endPoint[sweepDim].IsPracticallySame(startPoint[sweepDim]))
            {
                valueD1 = endPoint[(sweepDim + 1) % 3];
                valueD2 = endPoint[(sweepDim + 2) % 3];
                return;
            }
            var fraction = (valueSweepDim - startPoint[sweepDim]) / (endPoint[sweepDim] - startPoint[sweepDim]);
            var dim = (sweepDim + 1) % 3;
            valueD1 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
            dim = (dim + 1) % 3;
            valueD2 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
        }
        #region Indexing Functions
        private void SetUpIndexingParameters(int numberOfVoxelsAlongMaxDirection)
        {
            var dx = Solid.XMax - Solid.XMin;
            var dy = Solid.YMax - Solid.YMin;
            var dz = Solid.ZMax - Solid.ZMin;
            var maxDim = Math.Ceiling(Math.Max(dx, Math.Max(dy, dz)));
            voxelLength = maxDim / numberOfVoxelsAlongMaxDirection;
            ScaleToIntSpace = 1 / voxelLength;
            //To get a unique integer value for each voxel based on its index, 
            //multiply x by the (magnitude)^2, add y*(magnitude), and then add z to get a unique value.
            //Example: for a max magnitude of 1000, with x = -3, y = 345, z = -12
            //3000000 + 345000 + 12 = 3345012 => 3|345|012
            //In addition, we want to capture sign in one digit. So add (magnitude)^3*(negInt) where
            //-X = 1, -Y = 3, -Z = 5; //Example 0 = (+X+Y+Z); 1 = (-X+Y+Z); 3 = (+X-Y+Z);  5 = (+X+Y-Z); 
            //OpenVDB does this more compactly with binaries, using the &, <<, and >> functions to 
            //manipulate the X,Y,Z values to store them. Their function is called "coordToOffset" and is 
            //in the LeafNode.h file around lines 1050-1070. I could not understand this.
            yIndexOffset = (long)Math.Pow(10, Math.Ceiling(Math.Log10(maxDim * ScaleToIntSpace)) + 1);
            var maxInt = Math.Pow(long.MaxValue, 1.0 / 3);
            if (yIndexOffset * 10 > maxInt)
                throw new Exception("Int64 will not work for a voxel space this large, using the current index setup");
            xIndexOffset = (long)Math.Pow(yIndexOffset, 2);
            signIndexOffset = (long)Math.Pow(yIndexOffset, 3); //Sign Magnitude Multiplier
        }

        public long IndicesToVoxelID(int[] ijk)
        {
            //To get a unique integer value for each voxel based on its index, 
            //multiply x by the (magnitude)^2, add y*(magnitude), and then add z to get a unique value.
            //Example: for a max magnitude of 1000, with x = -3, y = 345, z = -12
            //3000000 + 345000 + 12 = 3345012 => 3|345|012
            //In addition, we want to capture sign in one digit. So add (magnitude)^3*(negInt) where
            //-X = 1, -Y = 3, -Z = 5;
            //Example 0 = (+X+Y+Z); 1 = (-X+Y+Z); 3 = (+X-Y+Z);  5 = (+X+Y-Z); 
            // 4 = (-X-Y+Z); 6 = (-X+Y-Z);  8 = (+X-Y-Z);  9 = (-X-Y-Z)
            var signValue = 0;
            if (Math.Sign(ijk[0]) < 0) signValue += 1;
            if (Math.Sign(ijk[1]) < 0) signValue += 3;
            if (Math.Sign(ijk[2]) < 0) signValue += 5;
            return signIndexOffset * signValue + Math.Abs(ijk[0]) * xIndexOffset + Math.Abs(ijk[1]) * yIndexOffset + Math.Abs(ijk[2]);
        }

        public int[] VoxelIDToIndices(long voxelID)
        {
            var z = (int)(voxelID % yIndexOffset);
            //uniqueCoordIndex -= z;
            var y = (int)((voxelID % xIndexOffset) / yIndexOffset);
            //uniqueCoordIndex -= y*ym;
            var x = (int)((voxelID % signIndexOffset) / xIndexOffset);
            //uniqueCoordIndex -= x*xm;
            var s = (int)(voxelID / signIndexOffset);

            //In addition, we want to capture sign in one digit. So add (magnitude)^3*(negInt) where
            //-X = 1, -Y = 3, -Z = 5;
            switch (s)
            {
                case 0: //(+X+Y+Z)
                    break;
                case 1: //(-X+Y+Z)
                    x = -x;
                    break;
                case 3: //(+X-Y+Z)
                    y = -y;
                    break;
                case 5: //(+X+Y-Z)
                    z = -z;
                    break;
                case 4: //(-X-Y+Z)
                    x = -x;
                    y = -y;
                    break;
                case 6: //(-X+Y-Z)
                    x = -x;
                    z = -z;
                    break;
                case 8: //(+X-Y-Z)
                    y = -y;
                    z = -z;
                    break;
                case 9: //(-X-Y-Z)
                    x = -x;
                    y = -y;
                    z = -z;
                    break;
            }
            return new[] { x, y, z };
        }
        #endregion
    }
}