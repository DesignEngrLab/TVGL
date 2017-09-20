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
            Index = index;
            this.ID = ID;
            var minX = Index[0] * voxelLength;
            var minY = Index[1] * voxelLength;
            var minZ = Index[2] * voxelLength;
            var halfLength = voxelLength / 2;
            TessellationElements = new List<TessellationBaseClass>();
            if (tessellationObject != null) TessellationElements.Add(tessellationObject);
            Center = new[] { minX + halfLength, minY + halfLength, minZ + halfLength };
            Bounds = new AABB
            {
                MinX = minX,
                MinY = minY,
                MinZ = minZ,
                MaxX = minX + voxelLength,
                MaxY = minY + voxelLength,
                MaxZ = minZ + voxelLength
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
                { (int) Math.Floor(coord[0]), (int) Math.Floor(coord[1]), (int) Math.Floor(coord[2]) };
                storeVoxel(ijk, vertex);
                vertices[i] = coord;
            }
            foreach (var face in Solid.Faces)
            {
                if (simpleCase(face)) continue;
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
                var maxSweepValue = (int)Math.Ceiling(Math.Max(vertices[face.B.IndexInList][sweepDim], vertices[face.C.IndexInList][sweepDim]));
                if (face.B.Position[sweepDim] < face.A.Position[sweepDim])
                {
                    startVertex = face.B;
                    leftVertex = face.C;
                    leftEdge = face.Edges[1];
                    rightVertex = face.A;
                    rightEdge = face.Edges[0];
                    maxSweepValue = (int)Math.Ceiling(Math.Max(vertices[face.C.IndexInList][sweepDim], vertices[face.A.IndexInList][sweepDim]));
                }
                if (face.C.Position[sweepDim] < face.B.Position[sweepDim] &&
                    face.C.Position[sweepDim] < face.A.Position[sweepDim])
                {
                    startVertex = face.C;
                    leftVertex = face.A;
                    leftEdge = face.Edges[2];
                    rightVertex = face.B;
                    rightEdge = face.Edges[1];
                    maxSweepValue = (int)Math.Ceiling(Math.Max(vertices[face.A.IndexInList][sweepDim], vertices[face.B.IndexInList][sweepDim]));
                }
                var leftStartPoint = (double[])vertices[startVertex.IndexInList].Clone();
                var rightStartPoint = (double[])leftStartPoint.Clone();
                var valueSweepDim = (int)Math.Ceiling(leftStartPoint[sweepDim]);
                var leftEndPoint = vertices[leftVertex.IndexInList];
                var rightEndPoint = vertices[rightVertex.IndexInList];
                var voxelizeLeft = leftEdge.Voxels == null || !leftEdge.Voxels.Any();
                var voxelizeRight = rightEdge.Voxels == null || !rightEdge.Voxels.Any();
                while (valueSweepDim <= maxSweepValue)
                {
                    double leftCoordD1, leftCoordD2;
                    var reachedOtherVertex = findWhereLineCrossesPlane(leftStartPoint, leftEndPoint, sweepDim,
                        valueSweepDim, out leftCoordD1, out leftCoordD2);
                    if (voxelizeLeft)
                    {
                        makeVoxelsAlongLineInPlane(leftStartPoint[dim1], leftStartPoint[dim2], leftCoordD1, leftCoordD2,
                            valueSweepDim, dim1, dim2, sweepDim, leftEdge);
                        makeVoxelsAlongLineInPlane(leftStartPoint[dim2], leftStartPoint[dim1], leftCoordD2, leftCoordD1,
                            valueSweepDim, dim2, dim1, sweepDim, leftEdge);
                    }
                    if (reachedOtherVertex)
                    {
                        leftStartPoint = (double[])leftEndPoint.Clone();
                        leftEndPoint = rightEndPoint;
                        leftEdge = face.OtherEdge(startVertex);
                        voxelizeLeft = leftEdge.Voxels == null || !leftEdge.Voxels.Any();

                        findWhereLineCrossesPlane(leftStartPoint, leftEndPoint, sweepDim,
                            valueSweepDim, out leftCoordD1, out leftCoordD2);
                        if (voxelizeLeft)
                        {
                            makeVoxelsAlongLineInPlane(leftStartPoint[dim1], leftStartPoint[dim2], leftCoordD1, leftCoordD2,
                                valueSweepDim, dim1, dim2, sweepDim, leftEdge);
                            makeVoxelsAlongLineInPlane(leftStartPoint[dim2], leftStartPoint[dim1], leftCoordD2, leftCoordD1,
                               valueSweepDim, dim2, dim1, sweepDim, leftEdge);
                        }
                        leftStartPoint[dim1] = leftCoordD1;
                        leftStartPoint[dim2] = leftCoordD2;
                        leftStartPoint[sweepDim] = valueSweepDim;
                    }
                    else
                    {
                        leftStartPoint[dim1] = leftCoordD1;
                        leftStartPoint[dim2] = leftCoordD2;
                        leftStartPoint[sweepDim] = valueSweepDim;
                    }
                    double rightCoordD1, rightCoordD2;
                    reachedOtherVertex = findWhereLineCrossesPlane(rightStartPoint, rightEndPoint, sweepDim, valueSweepDim,
                        out rightCoordD1, out rightCoordD2);
                    if (voxelizeRight)
                    {
                        makeVoxelsAlongLineInPlane(rightStartPoint[dim1], rightStartPoint[dim2], rightCoordD1,
                           rightCoordD2, valueSweepDim, dim1, dim2, sweepDim, rightEdge);
                        makeVoxelsAlongLineInPlane(rightStartPoint[dim2], rightStartPoint[dim1], rightCoordD2, rightCoordD1,
                            valueSweepDim, dim2, dim1, sweepDim, rightEdge);
                    }
                    if (reachedOtherVertex)
                    {
                        rightStartPoint = (double[])rightEndPoint.Clone();
                        rightEndPoint = leftEndPoint;
                        rightEdge = face.OtherEdge(startVertex);
                        voxelizeRight = rightEdge.Voxels == null || !rightEdge.Voxels.Any();

                        findWhereLineCrossesPlane(rightStartPoint, rightEndPoint, sweepDim, valueSweepDim,
                            out rightCoordD1, out rightCoordD2);
                        if (voxelizeRight)
                        {
                            makeVoxelsAlongLineInPlane(rightStartPoint[dim1], rightStartPoint[dim2], rightCoordD1,
                                rightCoordD2, valueSweepDim, dim1, dim2, sweepDim, rightEdge);
                            makeVoxelsAlongLineInPlane(rightStartPoint[dim2], rightStartPoint[dim1], rightCoordD2, rightCoordD1,
                                valueSweepDim, dim2, dim1, sweepDim, rightEdge);
                        }
                        rightStartPoint[dim1] = rightCoordD1;
                        rightStartPoint[dim2] = rightCoordD2;
                        rightStartPoint[sweepDim] = valueSweepDim;
                    }
                    else
                    {
                        rightStartPoint[dim1] = rightCoordD1;
                        rightStartPoint[dim2] = rightCoordD2;
                        rightStartPoint[sweepDim] = valueSweepDim;
                    }
                    makeVoxelsAlongLineInPlane(leftCoordD1, leftCoordD2, rightCoordD1, rightCoordD2, valueSweepDim, dim1, dim2,
                        sweepDim, face);
                    makeVoxelsAlongLineInPlane(leftCoordD2, leftCoordD1, rightCoordD2, rightCoordD1, valueSweepDim, dim2, dim1,
                        sweepDim, face);
                    valueSweepDim++;
                }
            }
        }

        private bool simpleCase(PolygonalFace face)
        {
            if (face.A.Voxel == face.B.Voxel && face.A.Voxel == face.C.Voxel)
            {
                var voxel = face.A.Voxel;
                face.AddVoxel(voxel);
                foreach (var edge in face.Edges)
                    edge.AddVoxel(voxel);
                return true;
            }
            if (face.A.Voxel.Index[0] == face.B.Voxel.Index[0] &&
                     face.A.Voxel.Index[0] == face.C.Voxel.Index[0] &&
                     face.A.Voxel.Index[1] == face.B.Voxel.Index[1] &&
                     face.A.Voxel.Index[1] == face.C.Voxel.Index[1])
            {
                makeVoxelsForFaceInCardinalLine(face, 2);
                return true;
            }
            if (face.A.Voxel.Index[0] == face.B.Voxel.Index[0] &&
                     face.A.Voxel.Index[0] == face.C.Voxel.Index[0] &&
                     face.A.Voxel.Index[2] == face.B.Voxel.Index[2] &&
                     face.A.Voxel.Index[2] == face.C.Voxel.Index[2])
            {
                makeVoxelsForFaceInCardinalLine(face, 1);
                return true;
            }
            if (face.A.Voxel.Index[2] == face.B.Voxel.Index[2] &&
                     face.A.Voxel.Index[2] == face.C.Voxel.Index[2] &&
                     face.A.Voxel.Index[1] == face.B.Voxel.Index[1] &&
                     face.A.Voxel.Index[1] == face.C.Voxel.Index[1])
            {
                makeVoxelsForFaceInCardinalLine(face, 0);
                return true;
            }
            return false;
        }

        private void makeVoxelsForFaceInCardinalLine(PolygonalFace face, int dim)
        {
            var minIndex = int.MaxValue;
            var maxIndex = int.MinValue;
            var voxelIndex = (int[])face.A.Voxel.Index.Clone();
            foreach (var faceEdge in face.Edges)
            {
                var lowVIndex = faceEdge.From.Voxel.Index[dim];
                var highVIndex = faceEdge.To.Voxel.Index[dim];
                if (highVIndex < lowVIndex)
                {
                    var temp = highVIndex;
                    highVIndex = lowVIndex;
                    lowVIndex = temp;
                }
                if (minIndex > lowVIndex) minIndex = lowVIndex;
                if (maxIndex < highVIndex) maxIndex = highVIndex;
                for (int i = lowVIndex; i <= highVIndex; i++)
                {
                    voxelIndex[dim] = i;
                    storeVoxel(voxelIndex, faceEdge);
                }
            }
            for (int i = minIndex; i <= maxIndex; i++)
            {
                voxelIndex[dim] = i;
                storeVoxel(voxelIndex, face);
            }
        }

        private void makeVoxelsAlongLineInPlane(double startX, double startY, double endX, double endY, int valueSweepDim,
            int xDim, int yDim, int sweepDim, TessellationBaseClass tsObject)
        {
            var ijk = new int[3];
            var x = (int)Math.Floor(startX);
            ijk[xDim] = x;
            var y = (int)Math.Floor(startY);
            ijk[yDim] = y;
            ijk[sweepDim] = valueSweepDim - 1;
            storeVoxel(ijk, tsObject);
            var xRange = endX - startX;
            if (xRange.IsNegligible()) return;
            var yRange = endY - startY;
            var incrementX = Math.Sign(xRange);
            if (incrementX > 0) x++;
            while (incrementX * x < incrementX * endX)
            {
                ijk[xDim] = x - (incrementX < 0 ? 1 : 0);
                var yDouble = yRange * (x - startX) / xRange + startY;
                y = (int)Math.Floor(yDouble);
                ijk[yDim] = y;
                storeVoxel(ijk, tsObject);
                if (yDouble.IsPracticallySame(y))
                {
                    ijk[yDim] = y - 1;
                    storeVoxel(ijk, tsObject);
                }
                x += incrementX;
            }
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
                //if (VoxelIDHashSet.Count % 1000 == 0) Console.WriteLine(VoxelIDHashSet.Count);
            }
            tsObject.AddVoxel(voxel);
        }

        private bool findWhereLineCrossesPlane(double[] startPoint, double[] endPoint, int sweepDim, double valueSweepDim, out double valueD1, out double valueD2)
        {
            if (endPoint[sweepDim] <= valueSweepDim)
            {
                valueD1 = endPoint[(sweepDim + 1) % 3];
                valueD2 = endPoint[(sweepDim + 2) % 3];
                return true;
            }
            var fraction = (valueSweepDim - startPoint[sweepDim]) / (endPoint[sweepDim] - startPoint[sweepDim]);
            var dim = (sweepDim + 1) % 3;
            valueD1 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
            dim = (dim + 1) % 3;
            valueD2 = fraction * (endPoint[dim] - startPoint[dim]) + startPoint[dim];
            return false;
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