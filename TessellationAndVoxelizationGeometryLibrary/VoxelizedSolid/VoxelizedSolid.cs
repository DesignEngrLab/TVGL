// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="VoxelizedSparseDense_Constructors.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TVGL.IOFunctions;

namespace TVGL.Voxelization
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSparseDense.
    /// </summary>
    public partial class VoxelizedSolid : Solid
    {
        #region Properties
        #region Sparse Representation
        internal int[] zSlices;
        internal List<int> yOffsetsAndXIndices;
        internal List<int> xRanges; //inclusive!
        /// <summary>
        /// Gets or sets a value indicating whether the sparse encoding is current.
        /// </summary>
        /// <value><c>true</c> if sparse encoding is current; otherwise, <c>false</c>.</value>
        public bool SparseIsCurrent { get; set; }
        #endregion

        #region Dense Representation
        public byte[,,] Dense { get; private set; }
        public int[] BytesPerSide { get; private set; }
        /// <summary>
        /// Gets or sets a value indicating whether the dense encoding is current.
        /// </summary>
        /// <value><c>true</c> if [dense encoding is current]; otherwise, <c>false</c>.</value>
        public bool DenseIsCurrent { get; set; }
        #endregion
        public long Count { get; private set; }
        public int[] VoxelsPerSide { get; private set; }
        public int[][] VoxelBounds { get; private set; }
        public double VoxelSideLength { get; private set; }
        public double[] TessToVoxSpace { get; private set; }
        private double[] Dimensions { get; set; }
        public double[] Offset => Bounds[0];

        #endregion

        #region Public Methods that Branch
        public bool GetVoxel(int xCoord, int yCoord, int zCoord)
        {
            if (DenseIsCurrent) return GetVoxelDense(xCoord, yCoord, zCoord);
            return GetVoxelSparse(xCoord, yCoord, zCoord);
        }
        public void SetVoxel(bool value, int xCoord, int yCoord, int zCoord)
        {
            if (DenseIsCurrent) SetVoxelDense(value, xCoord, yCoord, zCoord);
            else SetVoxelSparse(value, xCoord, yCoord, zCoord);
        }
        public bool GetNeighbors(int xCoord, int yCoord, int zCoord, out int[][] neighbors)
        {
            if (DenseIsCurrent) neighbors = GetNeighborsDense(xCoord, yCoord, zCoord);
            else neighbors = GetNeighborsSparse(xCoord, yCoord, zCoord);
            return !neighbors.Any(n => n != null);
        }
        #endregion


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="voxelsPerSide">The voxels per side.</param>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="empty">if set to <c>true</c> [empty].</param>
        public VoxelizedSolid(int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds, bool empty = false)
        {
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;
            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);

            Dense = new byte[BytesPerSide[0], BytesPerSide[1], BytesPerSide[2]];
            if (empty)
            {
                // Sparse loop
                zSlices = new int[VoxelsPerSide[2] + 1];
                yOffsetsAndXIndices = new List<int> { 0, 0, 0 }; //represents: zero y-offset, zero starting x, zero end
                xRanges = new List<int> { 0 };
                Count = 0;
                SurfaceArea = Volume = 0;
            }
            else
            {
                // Dense loop
                Parallel.For(0, bytesInX, m =>
                {
                    for (var n = 0; n < VoxelsPerSide[1]; n++)
                        for (var o = 0; o < VoxelsPerSide[2]; o++)
                            Dense[m, n, o] = byte.MaxValue;
                });
                // Sparse loop
                var xLim = VoxelsPerSide[0];
                var yLim = VoxelsPerSide[1];
                var zLim = VoxelsPerSide[2];
                /*** bug: something's not working here. You need a clear description of this sparse approach!
                zSlices = new int[zLim + 1];
                var yArray = new int[zLim * (1 + yLim)];
                var xArray = new int[2 * yLim * zLim];
                for (int i = 1; i <= zLim; i++)
                {
                    var yStart = zSlices[i-1] + 1 + yLim;
                    zSlices[i] = yStart;
                    yArray[yStart] = 0;
                    for (int j = 0; j < yLim; j++)
                    {
                        yArray[yStart + j + 1] = 2 * yLim * i + j;
                        //bug: something not right with y
                        xArray[2 * yLim * i + 2 * j] = 0;
                        xArray[2 * yLim * i + 2 * j + 1] = xLim - 1;
                    }
                }
                
                yOffsetsAndXIndices = yArray.ToList();
                xRanges = xArray.ToList();
               */
                Count = xLim * yLim * zLim;
                SurfaceArea = 2 * (xLim * yLim + zLim * yLim + xLim * zLim);
            }
            SparseIsCurrent = false;
            DenseIsCurrent = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="voxels">The voxels.</param>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(byte[,,] voxels, double voxelSideLength, IEnumerable<double[]> bounds = null)
        {
            VoxelSideLength = voxelSideLength;
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds.First().Clone();
                Bounds[1] = (double[])bounds.Last().Clone();
                Dimensions = Bounds[1].subtract(Bounds[0], 3);
            }
            SolidColor = new Color(Constants.DefaultColor);
            Dense = (byte[,,])voxels.Clone();
            VoxelsPerSide = new[] { voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2) };
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;
            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };
            UpdatePropertiesDense();
            DenseIsCurrent = true;
            SparseIsCurrent = false;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="zSlices">The z slices.</param>
        /// <param name="yOffsetsAndXIndices">The y offsets and x indices.</param>
        /// <param name="xRanges">The x ranges.</param>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(int[] zSlices, List<int> yOffsetsAndXIndices, List<int> xRanges,
            double voxelSideLength, IEnumerable<double[]> bounds = null)
        {
            VoxelSideLength = voxelSideLength;
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds.First().Clone();
                Bounds[1] = (double[])bounds.Last().Clone();
                Dimensions = Bounds[1].subtract(Bounds[0], 3);
            }
            SolidColor = new Color(Constants.DefaultColor);
            this.zSlices = (int[])zSlices.Clone();
            this.yOffsetsAndXIndices = new List<int>(yOffsetsAndXIndices);
            this.xRanges = new List<int>(xRanges);
            var xLim = xRanges.Max() + 1;
            var zLim = zSlices.Length - 1;
            var yLim = 0;
            for (int i = 0; i < zLim; i++)
            {
                var yStartIndex = zSlices[i];
                var yOffset = yOffsetsAndXIndices[yStartIndex];
                var numYLines = zSlices[i + 1] - 1 - yStartIndex;
                if (yLim < yOffset + numYLines) yLim = yOffset + numYLines;
            }
            VoxelsPerSide = new[] { xLim, yLim, zLim };
            UpdatePropertiesSparse();
            SparseIsCurrent = true;
            DenseIsCurrent = false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="vs">The vs.</param>
        /// <exception cref="ArgumentException">The voxelized solid constructor receiving another" +
        ///                 " voxelized solid as input must have an up-to-date sparse representation.</exception>
        public VoxelizedSolid(VoxelizedSolid vs)
        //: this(vs.zSlices, vs.yOffsetsAndXIndices, vs.xRanges, vs.VoxelSideLength, vs.Bounds)
        {
            // if (!vs.SparseIsCurrent) throw new ArgumentException("The voxelized solid constructor receiving another" +
            //    " voxelized solid as input must have an up-to-date sparse representation.");
            if (vs.DenseIsCurrent)
            {
                VoxelSideLength = vs.VoxelSideLength;
                Bounds = new double[2][];
                Bounds[0] = (double[])vs.Bounds.First().Clone();
                Bounds[1] = (double[])vs.Bounds.Last().Clone();
                Dimensions = Bounds[1].subtract(Bounds[0], 3);
                SolidColor = vs.SolidColor;
                Dense = (byte[,,])vs.Dense.Clone();
                VoxelsPerSide = (int[])vs.VoxelsPerSide.Clone();
                BytesPerSide = (int[])vs.BytesPerSide.Clone();
                this.DenseIsCurrent = true;
            }
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelsOnLongSide">The voxels on long side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, int voxelsOnLongSide, IReadOnlyList<double[]> bounds = null)
        {
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
            }
            else
            {
                Bounds[0] = (double[])ts.Bounds[0].Clone();
                Bounds[1] = (double[])ts.Bounds[1].Clone();
            }
            Dimensions = Bounds[1].subtract(Bounds[0]);
            SolidColor = new Color(Constants.DefaultColor);
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLength)).ToArray();
            FillInFromTessellation(ts);
            //UpdatePropertiesSparse();  //temporary until this function is fixed and made more efficient
            SparseIsCurrent = true;
            DenseIsCurrent = false;
            UpdateDenseFromSparse();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, double voxelSideLength, IReadOnlyList<double[]> bounds = null)
        {
            Bounds = new double[2][];
            if (bounds != null)
            {
                Bounds[0] = (double[])bounds[0].Clone();
                Bounds[1] = (double[])bounds[1].Clone();
            }
            else
            {
                Bounds[0] = (double[])ts.Bounds[0].Clone();
                Bounds[1] = (double[])ts.Bounds[1].Clone();
            }
            Dimensions = Bounds[1].subtract(Bounds[0]);
            SolidColor = new Color(Constants.DefaultColor);
            VoxelSideLength = voxelSideLength;
            VoxelsPerSide = Dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLength)).ToArray();
            FillInFromTessellation(ts);
            //UpdatePropertiesSparse();  //temporary until this function is fixed and made more efficient
            SparseIsCurrent = true;
            DenseIsCurrent = false;
            UpdateDenseFromSparse();
        }
        #region Fill In From Tessellation Functions
        private void FillInFromTessellation(TessellatedSolid ts, bool possibleNullSlices = false)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            zSlices = new int[zLim + 1];
            yOffsetsAndXIndices = new List<int>();  // { 0 };
            xRanges = new List<int>();
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = AllSlicesAlongZ(ts, zBegin, zLim, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time

            //Parallel.For(0, zLim, k =>
            for (var k = 0; k < zLim; k++)
            {
                var loops = decomp[k];
                if (loops.Any())
                {
                    var intersections = AllPolygonIntersectionPointsAlongY(loops, yBegin, yLim, VoxelSideLength, out var yStartIndex);
                    var numYlines = intersections.Count;
                    yOffsetsAndXIndices.Add(yStartIndex);
                    for (int j = 0; j < numYlines; j++)
                    {
                        var intersectionPoints = intersections[j];
                        var numXRangesOnThisLine = intersectionPoints.Length;
                        yOffsetsAndXIndices.Add(xRanges.Count);
                        for (var m = 0; m < numXRangesOnThisLine; m += 2)
                        {
                            //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                            //Although other dimensions do not also do this. Everything operates with Round (effectively).
                            //Could reverse this to add more voxels
                            var sp = (int)((intersectionPoints[m] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (sp < 0) sp = 0;
                            var ep = (int)((intersectionPoints[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (ep >= xLim) ep = xLim - 1;
                            xRanges.Add(sp);
                            xRanges.Add(ep);
                        }
                    }
                }
                zSlices[k + 1] = yOffsetsAndXIndices.Count;
            }   //);
            yOffsetsAndXIndices.Add(-1);
            yOffsetsAndXIndices.Add(xRanges.Count);
        }
        private static List<List<PointLight>> GetZLoops(HashSet<Edge> penetratingEdges, double ZOfPlane)
        {
            var loops = new List<List<PointLight>>();

            var unusedEdges = new HashSet<Edge>(penetratingEdges);
            while (unusedEdges.Any())
            {
                var loop = new List<PointLight>();
                var firstEdgeInLoop = unusedEdges.First();
                var finishedLoop = false;
                var currentEdge = firstEdgeInLoop;
                do
                {
                    unusedEdges.Remove(currentEdge);
                    var intersectVertex = MiscFunctions.PointLightOnZPlaneFromIntersectingLine(ZOfPlane, currentEdge.From, currentEdge.To);
                    loop.Add(intersectVertex);
                    var nextFace = (currentEdge.From.Z < ZOfPlane) ? currentEdge.OtherFace : currentEdge.OwnedFace;
                    Edge nextEdge = null;
                    foreach (var whichEdge in nextFace.Edges)
                    {
                        if (currentEdge == whichEdge) continue;
                        if (whichEdge == firstEdgeInLoop)
                        {
                            finishedLoop = true;
                            loops.Add(loop);
                            break;
                        }
                        else if (unusedEdges.Contains(whichEdge))
                        {
                            nextEdge = whichEdge;
                            break;
                        }
                    }
                    if (!finishedLoop && nextEdge == null)
                    {
                        Console.WriteLine("Incomplete loop.");
                        loops.Add(loop);
                    }
                    else currentEdge = nextEdge;
                } while (!finishedLoop);
            }
            return loops;
        }

        static List<List<PointLight>>[] AllSlicesAlongZ(TessellatedSolid ts, double startDistance, int numSteps, double stepSize)
        {
            List<List<PointLight>>[] loopsAlongZ = new List<List<PointLight>>[numSteps];
            //First, sort the vertices along the given axis. Duplicate distances are not important.
            var sortedVertices = ts.Vertices.OrderBy(v => v.Z).ToArray();
            var currentEdges = new HashSet<Edge>();
            var nextDistance = sortedVertices.First().Z;
            var vIndex = 0;
            for (int step = 0; step < numSteps; step++)
            {
                var z = startDistance + step * stepSize;
                var thisVertex = sortedVertices[vIndex];
                var needToOffset = false;
                while (thisVertex.Z <= z)
                {
                    if (thisVertex.Z == z) needToOffset = true;
                    foreach (var edge in thisVertex.Edges)
                    {
                        if (currentEdges.Contains(edge)) currentEdges.Remove(edge);
                        else currentEdges.Add(edge);
                    }
                    vIndex++;
                    thisVertex = sortedVertices[vIndex];
                }
                if (needToOffset)
                    z += (z + Math.Min(stepSize / 2, sortedVertices[vIndex + 1].Z)) / 2;
                if (currentEdges.Any()) loopsAlongZ[step] = GetZLoops(currentEdges, z);
                else loopsAlongZ[step] = new List<List<PointLight>>();
            }
            return loopsAlongZ;
        }

        internal static List<double[]> AllPolygonIntersectionPointsAlongY(List<List<PointLight>> loops, double start, int numSteps, double stepSize,
            out int firstIntersectingIndex)
        {
            return AllPolygonIntersectionPointsAlongY(loops.Select(p => new Polygon(p.Select(point => new Point(point)), true)), start, numSteps, stepSize,
                out firstIntersectingIndex);
        }
        internal static List<double[]> AllPolygonIntersectionPointsAlongY(IEnumerable<Polygon> polygons, double start, int numSteps, double stepSize,
                out int firstIntersectingIndex)
        {
            var intersections = new List<double[]>();
            var sortedPoints = polygons.SelectMany(polygon => polygon.Path).OrderBy(p => p.Y).ToList();
            var currentLines = new HashSet<Line>();
            var nextDistance = sortedPoints.First().Y;
            firstIntersectingIndex = (int)Math.Ceiling((nextDistance - start) / stepSize);
            var pIndex = 0;
            for (int i = firstIntersectingIndex; i < numSteps; i++)
            {
                var y = start + i * stepSize;
                var thisPoint = sortedPoints[pIndex];
                var needToOffset = false;
                while (thisPoint.Y <= y)
                {
                    if (thisPoint.Y == y) needToOffset = true;
                    foreach (var line in thisPoint.Lines)
                    {
                        if (currentLines.Contains(line)) currentLines.Remove(line);
                        else currentLines.Add(line);
                    }
                    pIndex++;
                    if (pIndex == sortedPoints.Count) return intersections;
                    thisPoint = sortedPoints[pIndex];
                }
                if (needToOffset)
                    y += (y + Math.Min(stepSize / 2, sortedPoints[pIndex + 1].Y)) / 2;
                var numIntersects = currentLines.Count;
                var intersects = new double[numIntersects];
                var index = 0;
                foreach (var line in currentLines)
                    intersects[index++] = line.XGivenY(y);
                intersections.Add(intersects.OrderBy(x => x).ToArray());
            }
            return intersections;
        }

        #endregion
        #endregion

        #region Conversion Methods
        public void UpdateDenseFromSparse()
        {
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;
            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };
            Dense = new byte[bytesInX, VoxelsPerSide[1], VoxelsPerSide[2]];
            for (int k = 0; k < VoxelsPerSide[2]; k++)
            //Parallel.For(0, VoxelsPerSide[2], k =>
            {
                var yStartIndex = zSlices[k];
                var yOffset = yOffsetsAndXIndices[yStartIndex];
                yStartIndex++;
                var yEnd = zSlices[k + 1];
                for (int j = yStartIndex; j < yEnd; j++)
                {
                    for (int i = yOffsetsAndXIndices[j]; i < yOffsetsAndXIndices[j + 1]; i += 2)
                    {
                        var xBegin = xRanges[i];
                        var xEnd = xRanges[i + 1];
                        AddVoxelRangeToDense(xBegin, xEnd, yOffset, k);
                    }
                    yOffset++;
                }
            } //);
            DenseIsCurrent = true;
        }

        private void AddVoxelRangeToDense(int xBegin, int xEnd, int yCoord, int zCoord)
        {
            var xByte = xBegin >> 3;
            var xByteEnd = xEnd >> 3;
            var bitPostion = xBegin & 7;
            switch (bitPostion)
            {
                case 0: Dense[xByte, yCoord, zCoord] += 0b11111111; break;
                case 1: Dense[xByte, yCoord, zCoord] += 0b01111111; break;
                case 2: Dense[xByte, yCoord, zCoord] += 0b00111111; break;
                case 3: Dense[xByte, yCoord, zCoord] += 0b00011111; break;
                case 4: Dense[xByte, yCoord, zCoord] += 0b00001111; break;
                case 5: Dense[xByte, yCoord, zCoord] += 0b00000111; break;
                case 6: Dense[xByte, yCoord, zCoord] += 0b00000011; break;
                default: Dense[xByte, yCoord, zCoord] += 0b00000001; break;
            }
            while (++xByte < xByteEnd)
                Dense[xByte, yCoord, zCoord] = 0b11111111;
            bitPostion = xEnd & 7;
            switch (bitPostion)
            {
                case 1: Dense[xByte, yCoord, zCoord] = 0b10000000; break;
                case 2: Dense[xByte, yCoord, zCoord] = 0b11000000; break;
                case 3: Dense[xByte, yCoord, zCoord] = 0b11100000; break;
                case 4: Dense[xByte, yCoord, zCoord] = 0b11110000; break;
                case 5: Dense[xByte, yCoord, zCoord] = 0b11111000; break;
                case 6: Dense[xByte, yCoord, zCoord] = 0b11111100; break;
                case 7: Dense[xByte, yCoord, zCoord] = 0b11111110; break;
                default: break;
            }
        }

        public void UpdateSparseFromDense()
        {

        }
        public VoxelizedSolid(TVGLFileData fileData, string fileName) : base(fileData, fileName)
        {
        }



        internal string[] GetVoxelsAsStringArrays()
        {
            var zSlicesBitArray = zSlices.SelectMany(slice => BitConverter.GetBytes(slice)).ToArray();
            var yStartsBitArray = yOffsetsAndXIndices.SelectMany(yStart => BitConverter.GetBytes(yStart)).ToArray();
            var xRangeBitArray = xRanges.SelectMany(xRange => BitConverter.GetBytes(xRange)).ToArray();

            return new[]{
                BitConverter.ToString(zSlicesBitArray).Replace("-", ""),
                BitConverter.ToString(yStartsBitArray).Replace("-", ""),
                BitConverter.ToString(xRangeBitArray).Replace("-", "") };
        }


        public TessellatedSolid ConvertToTessellatedSolid(Color color)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Overrides of Solid abstract members
        public override Solid Copy()
        {
            if (!SparseIsCurrent) UpdateSparseFromDense();
            return new VoxelizedSolid(this);
        }

        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }
        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            throw new NotImplementedException();
        }
        public override double[,] InertiaTensor { get => base.InertiaTensor; protected set => base.InertiaTensor = value; }
        #endregion

        private class SameCoordinates : EqualityComparer<int[]>
        {
            public override bool Equals(int[] a1, int[] a2)
            {
                if (a1 == null && a2 == null)
                    return true;
                if (a1 == null || a2 == null)
                    return false;
                return (a1[0] == a2[0] &&
                        a1[1] == a2[1] &&
                        a1[2] == a2[2]);
            }
            public override int GetHashCode(int[] ax)
            {
                if (ax is null) return 0;
                var hCode = ax[0] + (ax[1] << 10) + (ax[2] << 20);
                return hCode.GetHashCode();
            }
        }
    }
}
