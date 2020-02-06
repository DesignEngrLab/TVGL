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


using System;
using System.Collections;
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
    public partial class VoxelizedSolid : Solid, IEnumerable<int[]>
    {
        #region Properties
        internal IVoxelRow[] voxels { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether the dense encoding is current.
        /// </summary>
        /// <value><c>true</c> if [dense encoding is current]; otherwise, <c>false</c>.</value>
        public long Count { get; private set; }
        public int[] VoxelsPerSide => new[] { numVoxelsX, numVoxelsY, numVoxelsZ };
        public int[][] VoxelBounds { get; }
        public double VoxelSideLength { get; private set; }
        public double[] TessToVoxSpace { get; }
        public double[] Dimensions { get; private set; }
        public double[] Offset => Bounds[0];
        public int numVoxelsX { get; private set; }
        public int numVoxelsY { get; private set; }
        public int numVoxelsZ { get; private set; }
        int zMultiplier => numVoxelsY;
        public double FractionDense { get; private set; }
        #endregion




        #region Constructors
        private VoxelizedSolid() { }

        public VoxelizedSolid(VoxelizedSolid vs) : this()
        {
            Bounds = new double[2][];
            Bounds[0] = (double[])vs.Bounds[0].Clone();
            Bounds[1] = (double[])vs.Bounds[1].Clone();
            Dimensions = Bounds[1].subtract(Bounds[0]);
            SolidColor = new Color(vs.SolidColor.A, vs.SolidColor.R, vs.SolidColor.G, vs.SolidColor.B);
            VoxelSideLength = vs.VoxelSideLength;
            numVoxelsX = vs.numVoxelsX;
            numVoxelsY = vs.numVoxelsY;
            numVoxelsZ = vs.numVoxelsZ;
            voxels = new IVoxelRow[numVoxelsY * numVoxelsZ];
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                voxels[i] = new VoxelRowSparse(vs.voxels[i], numVoxelsX);
            FractionDense = 0;
            UpdateProperties();
        }



        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelsOnLongSide">The voxels on long side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, int voxelsOnLongSide, IReadOnlyList<double[]> bounds = null) : this()
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
            SolidColor = new Color(ts.SolidColor.A, ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B);
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            var voxelsPerSide = Dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLength)).ToArray();
            numVoxelsX = voxelsPerSide[0];
            numVoxelsY = voxelsPerSide[1];
            numVoxelsZ = voxelsPerSide[2];
            voxels = new IVoxelRow[numVoxelsY * numVoxelsZ];
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                voxels[i] = new VoxelRowSparse(numVoxelsX);
            FillInFromTessellation(ts);
            FractionDense = 0;
            UpdateProperties();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelSideLength">Length of the voxel side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(TessellatedSolid ts, double voxelSideLength, IReadOnlyList<double[]> bounds = null) : this()
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
            var voxelsPerSide = Dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLength)).ToArray();
            numVoxelsX = voxelsPerSide[0];
            numVoxelsY = voxelsPerSide[1];
            numVoxelsZ = voxelsPerSide[2];
            voxels = new IVoxelRow[numVoxelsY * numVoxelsZ];
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                voxels[i] = new VoxelRowSparse();
            FillInFromTessellation(ts);
            FractionDense = 0;
            UpdateProperties();
        }

        #region Fill In From Tessellation Functions
        private void FillInFromTessellation(TessellatedSolid ts)
        {
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = CrossSectionSolid.GetUniformlySpacedSlices(ts,CartesianDirections.ZPositive, zBegin, numVoxelsZ, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time

            Parallel.For(0, numVoxelsZ, k =>
            //for (var k = 0; k < numVoxelsZ; k++)
            {
                var loops = decomp[k];
                if (loops.Any())
                {
                    var intersections =PolygonOperations.AllPolygonIntersectionPointsAlongY(loops.Select(p=>new Polygon(p)), yBegin, numVoxelsY, VoxelSideLength, out var yStartIndex);
                    var numYlines = intersections.Count;
                    for (int j = 0; j < numYlines; j++)
                    {
                        var intersectionPoints = intersections[j];
                        var numXRangesOnThisLine = intersectionPoints.Length;
                        for (var m = 0; m < numXRangesOnThisLine; m += 2)
                        {
                            var sp = (ushort)((intersectionPoints[m] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (sp < 0) sp = 0;
                            var ep = (ushort)((intersectionPoints[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                            if (ep >= numVoxelsX) ep = (ushort)(numVoxelsX - 1);
                            ((VoxelRowSparse)voxels[k * zMultiplier + yStartIndex + j]).indices.Add(sp);
                            ((VoxelRowSparse)voxels[k * zMultiplier + yStartIndex + j]).indices.Add(ep);
                        }
                    }
                }
            });
        }


        #endregion


        public static VoxelizedSolid CreateFullBlock(VoxelizedSolid vs)
        {
            return CreateFullBlock(vs.VoxelSideLength, vs.Bounds);
        }
        public static VoxelizedSolid CreateFullBlock(double voxelSideLength, IReadOnlyList<double[]> bounds)
        {
            var fullBlock = new VoxelizedSolid();
            fullBlock.Bounds = new double[2][];
            fullBlock.Bounds[0] = (double[])bounds[0].Clone();
            fullBlock.Bounds[1] = (double[])bounds[1].Clone();
            fullBlock.Dimensions = fullBlock.Bounds[1].subtract(fullBlock.Bounds[0]);
            fullBlock.SolidColor = new Color(Constants.DefaultColor);
            fullBlock.VoxelSideLength = voxelSideLength;
            var voxelsPerSide = fullBlock.Dimensions.Select(d => (int)Math.Ceiling(d / fullBlock.VoxelSideLength)).ToArray();
            fullBlock.numVoxelsX = voxelsPerSide[0];
            fullBlock.numVoxelsY = voxelsPerSide[1];
            fullBlock.numVoxelsZ = voxelsPerSide[2];
            fullBlock.voxels = new IVoxelRow[fullBlock.numVoxelsY * fullBlock.numVoxelsZ];
            for (int i = 0; i < fullBlock.numVoxelsY * fullBlock.numVoxelsZ; i++)
            {
                var fullRow = new VoxelRowSparse(fullBlock.numVoxelsX);
                fullRow.indices.Add(0);
                fullRow.indices.Add((ushort)fullBlock.numVoxelsX);
                fullBlock.voxels[i] = fullRow;
            }
            fullBlock.UpdateProperties();
            return fullBlock;
        }

        #endregion

        #region Conversion Methods
        public void UpdateToAllDense()
        {
            if (FractionDense == 1) return;
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
            {
                if (voxels[i] is VoxelRowDense) continue;
                voxels[i] = new VoxelRowDense((VoxelRowSparse)voxels[i], numVoxelsX);
            }
            FractionDense = 1;
        }
        public void UpdateToAllSparse()
        {
            if (FractionDense == 0) return;
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
            {
                if (voxels[i] is VoxelRowSparse) continue;
                voxels[i] = new VoxelRowSparse((VoxelRowDense)voxels[i], numVoxelsX);
            }
            FractionDense = 0;
        }


        internal string[] GetVoxelsAsStringArrays()
        {
            UpdateToAllSparse();
            var allRows = new List<string>();
            for (int i = 0; i < numVoxelsY; i++)
            {
                for (int j = 0; j < numVoxelsZ; j++)
                {
                    var sparseRow = (VoxelRowSparse)voxels[i + j * zMultiplier];
                    if (sparseRow.indices.Any())
                    {
                        var rowDetails = new List<ushort>(sparseRow.indices);
                        rowDetails.Insert(0, (ushort)j);
                        rowDetails.Insert(0, (ushort)i);
                        allRows.Add(BitConverter.ToString(rowDetails.SelectMany(u => BitConverter.GetBytes(u)).ToArray()));
                    }
                }
            }
            return allRows.ToArray();
        }
        public TessellatedSolid ConvertToTessellatedSolidRectilinear()
        {
            throw new NotImplementedException();
        }
        public TessellatedSolid ConvertToTessellatedSolidMarchingCubes(int voxelsPerTriangleSpacing)
        {
            var marchingCubes = new MarchingCubesDenseVoxels(this, voxelsPerTriangleSpacing);
            var ts = marchingCubes.Generate();
            return ts;
        }
        #endregion

        #region Overrides of Solid abstract members
        public override Solid Copy()
        {
            UpdateToAllSparse();
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

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new VoxelEnumerator(this);
        }

        public IEnumerator<int[]> GetEnumerator()
        {
            return new VoxelEnumerator(this);

        }

        public override double[,] InertiaTensor { get => base.InertiaTensor; set => base.InertiaTensor = value; }
        #endregion


    }
}
