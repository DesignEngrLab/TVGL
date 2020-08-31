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
using TVGL.Numerics;
using TVGL.TwoDimensional;

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
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        public long Count { get; private set; }
        public int[] VoxelsPerSide => new[] { numVoxelsX, numVoxelsY, numVoxelsZ };
        public int[][] VoxelBounds { get; }
        public double VoxelSideLength { get; private set; }
        public Vector3 Dimensions { get; private set; }
        public Vector3 Offset => Bounds[0];
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
            Bounds = new[] { vs.Bounds[0], vs.Bounds[1] };
            Dimensions = Bounds[1].Subtract(Bounds[0]);
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
        public VoxelizedSolid(TessellatedSolid ts, int voxelsOnLongSide, IReadOnlyList<Vector3> bounds = null) : this()
        {
            if (bounds != null)
                Bounds = new[] { bounds[0], bounds[1] };
            else
                Bounds = new[] { ts.Bounds[0], ts.Bounds[1] };
            Dimensions = Bounds[1].Subtract(Bounds[0]);
            SolidColor = new Color(ts.SolidColor.A, ts.SolidColor.R, ts.SolidColor.G, ts.SolidColor.B);
            VoxelSideLength = Math.Max(Dimensions.X, Math.Max(Dimensions.Y, Dimensions.Z)) / voxelsOnLongSide;
            numVoxelsX = (int)Math.Ceiling(Dimensions.X / VoxelSideLength);
            numVoxelsY = (int)Math.Ceiling(Dimensions.Y / VoxelSideLength);
            numVoxelsZ = (int)Math.Ceiling(Dimensions.Z / VoxelSideLength);
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
        public VoxelizedSolid(TessellatedSolid ts, double voxelSideLength, IReadOnlyList<Vector3> bounds = null) : this()
        {
            if (bounds != null)
                Bounds = new[] { bounds[0], bounds[1] };
            else
                Bounds = new[] { ts.Bounds[0], ts.Bounds[1] };
            Dimensions = Bounds[1].Subtract(Bounds[0]);
            SolidColor = new Color(Constants.DefaultColor);
            VoxelSideLength = voxelSideLength;
            numVoxelsX = (int)Math.Ceiling(Dimensions.X / VoxelSideLength);
            numVoxelsY = (int)Math.Ceiling(Dimensions.Y / VoxelSideLength);
            numVoxelsZ = (int)Math.Ceiling(Dimensions.Z / VoxelSideLength);
            voxels = new IVoxelRow[numVoxelsY * numVoxelsZ];
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                voxels[i] = new VoxelRowSparse();
            FillInFromTessellation(ts);
            FractionDense = 0;
            UpdateProperties();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="voxelsOnLongSide">The voxels on long side.</param>
        /// <param name="bounds">The bounds.</param>
        public VoxelizedSolid(IEnumerable<Polygon> loops, int voxelsOnLongSide, IReadOnlyList<Vector2> bounds) : this()
        {
            Bounds = new[] { new Vector3(bounds[0],0), new Vector3(bounds[1],1) };
            Dimensions = Bounds[1].Subtract(Bounds[0]);
            VoxelSideLength = Math.Max(Dimensions.X, Math.Max(Dimensions.Y, Dimensions.Z)) / voxelsOnLongSide;
            numVoxelsX = (int)Math.Ceiling(Dimensions.X / VoxelSideLength);
            numVoxelsY = (int)Math.Ceiling(Dimensions.Y / VoxelSideLength);
            numVoxelsZ = 1;
            voxels = new IVoxelRow[numVoxelsY * numVoxelsZ];
            for (int i = 0; i < numVoxelsY * numVoxelsZ; i++)
                voxels[i] = new VoxelRowSparse(numVoxelsX);

            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time
            if (loops.Any())
            {
                var intersections = PolygonOperations.AllPolygonIntersectionPointsAlongY(loops, yBegin, numVoxelsY,
                                VoxelSideLength, out var yStartIndex);
                var numYlines = intersections.Count;
                for (int j = 0; j < numYlines; j++)
                {
                    var intersectionPoints = intersections[j];
                    var numXRangesOnThisLine = intersectionPoints.Length;
                    for (var m = 0; m < numXRangesOnThisLine; m += 2)
                    {
                        var sp = (ushort)((intersectionPoints[m] - Bounds[0][0]) * inverseVoxelSideLength);
                        var ep = (ushort)((intersectionPoints[m + 1] - Bounds[0][0]) * inverseVoxelSideLength);
                        if (ep >= numVoxelsX) ep = (ushort)(numVoxelsX - 1);
                        ((VoxelRowSparse)voxels[yStartIndex + j]).indices.Add(sp);
                        ((VoxelRowSparse)voxels[yStartIndex + j]).indices.Add(ep);
                    }
                }
            }
            FractionDense = 0;
            UpdateProperties();
        }

        #region Fill In From Tessellation Functions
        private void FillInFromTessellation(TessellatedSolid ts)
        {
            var yBegin = Bounds[0][1] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;
            var decomp = CrossSectionSolid.GetUniformlySpacedSlices(ts, CartesianDirections.ZPositive, zBegin, numVoxelsZ, VoxelSideLength);
            var inverseVoxelSideLength = 1 / VoxelSideLength; // since its quicker to multiple then to divide, maybe doing this once at the top will save some time

            //Parallel.For(0, numVoxelsZ, k =>
            for (var k = 0; k < numVoxelsZ; k++)
            {
                var loops = decomp[k];
                if (loops.Any())
                {
                    var intersections = PolygonOperations.AllPolygonIntersectionPointsAlongY(loops, yBegin, numVoxelsY,
                        VoxelSideLength, out var yStartIndex);
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
            }
            //);
        }


        #endregion


        public static VoxelizedSolid CreateFullBlock(VoxelizedSolid vs)
        {
            return CreateFullBlock(vs.VoxelSideLength, vs.Bounds);
        }
        public static VoxelizedSolid CreateFullBlock(double voxelSideLength, IReadOnlyList<Vector3> bounds)
        {
            var fullBlock = new VoxelizedSolid();
            fullBlock.Bounds = new[] { bounds[0], bounds[1] };
            fullBlock.Dimensions = fullBlock.Bounds[1].Subtract(fullBlock.Bounds[0]);
            fullBlock.SolidColor = new Color(Constants.DefaultColor);
            fullBlock.VoxelSideLength = voxelSideLength;
            fullBlock.numVoxelsX = (int)Math.Ceiling(fullBlock.Dimensions.X / fullBlock.VoxelSideLength);
            fullBlock.numVoxelsY = (int)Math.Ceiling(fullBlock.Dimensions.Y / fullBlock.VoxelSideLength);
            fullBlock.numVoxelsZ = (int)Math.Ceiling(fullBlock.Dimensions.Z / fullBlock.VoxelSideLength);
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

        public override void Transform(Matrix4x4 transformMatrix)
        {
            throw new NotImplementedException();
        }
        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
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

        protected override void CalculateCenter()
        {

            Count = 0;
            var xTotal = 0;
            var yTotal = 0;
            var zTotal = 0;
            for (int j = 0; j < numVoxelsY; j++)
                for (int k = 0; k < numVoxelsZ; k++)
                {
                    var voxelRow = voxels[j + zMultiplier * k];
                    var rowCount = voxelRow.Count;
                    xTotal += rowCount * voxelRow.TotalXPosition();
                    yTotal += rowCount * j;
                    zTotal += rowCount * k;
                    Count += rowCount;
                }
            _center = new Vector3  //is this right?
            (
                VoxelSideLength * xTotal / Count,
                VoxelSideLength * yTotal / Count,
                VoxelSideLength * zTotal / Count
            );
        }

        protected override void CalculateVolume()
        {
            _volume = Count * Math.Pow(VoxelSideLength, 3);
        }

        protected override void CalculateSurfaceArea()
        {
            throw new NotImplementedException();
        }

        protected override void CalculateInertiaTensor()
        {
            throw new NotImplementedException();
        }

        #endregion


    }
}
