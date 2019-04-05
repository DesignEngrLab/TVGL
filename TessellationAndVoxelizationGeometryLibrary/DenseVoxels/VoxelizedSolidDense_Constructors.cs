// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="VoxelizedSolidDense_Constructors.cs" company="Design Engineering Lab">
//     Copyright ©  2019
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIConvexHull;
using StarMathLib;
using TVGL.Boolean_Operations;
using TVGL.Enclosure_Operations;
using TVGL.Voxelization;
using TVGL._2D;

namespace TVGL.DenseVoxels
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense : Solid
    {
        #region Properties
        public byte[,,] Voxels;
        public readonly int Discretization;
        public int[] VoxelsPerSide;
        public int[][] VoxelBounds { get; set; }
        public double VoxelSideLength { get; internal set; }
        public double[] TessToVoxSpace { get; }
        private readonly double[] Dimensions;
        public double[] Offset => Bounds[0];
        public int Count { get; internal set; }
        public TessellatedSolid TS { get; set; }

        #endregion

        public VoxelizedSolidDense(int[] voxelsPerSide, int discretization, double voxelSideLength,
            IEnumerable<double[]> bounds, byte value = 0)
        {
            VoxelsPerSide = (int[]) voxelsPerSide.Clone();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];
            Count = 0;
            SurfaceArea = 0;
            Volume = 0;
            if (value != 0)
            {
                Parallel.For(0, xLim, m =>
                {
                    for (var n = 0; n < yLim; n++)
                    for (var o = 0; o < zLim; o++)
                        Voxels[m, n, o] = value;
                });
                UpdateBoundingProperties();

            }
            Discretization = discretization;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
        }

        public VoxelizedSolidDense(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds)
        {
            Voxels = (byte[,,])voxels.Clone();
            Discretization = discretization;
            VoxelsPerSide = voxelsPerSide;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolidDense(VoxelizedSolidDense vs)
        {
            Voxels = (byte[,,]) vs.Voxels.Clone();
            Discretization = vs.Discretization;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolidDense(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
            TS = ts;
            Discretization = discretization;
            SolidColor = new Color(Constants.DefaultColor);
            var voxelsOnLongSide = Math.Pow(2, Discretization);

            Bounds = new double[2][];
            Dimensions = new double[3];

            if (bounds != null)
            {
                Bounds[0] = (double[]) bounds[0].Clone();
                Bounds[1] = (double[]) bounds[1].Clone();
            }
            else
            {
                Bounds[0] = ts.Bounds[0];
                Bounds[1] = ts.Bounds[1];
            }
            for (var i = 0; i < 3; i++)
                Dimensions[i] = Bounds[1][i] - Bounds[0][i];

            //var longestSide = Dimensions.Max();
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int) Math.Round(d / VoxelSideLength)).ToArray();
            TessToVoxSpace = VoxelsPerSide.multiply(VoxelSideLength, 3).EltDivide(Dimensions, 3);

            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];

            VoxelizeSolid(ts);
            UpdateProperties();
        }

        private void VoxelizeSolid(TessellatedSolid ts, bool possibleNull = false)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];

            var xBegin = Bounds[0][0] + VoxelSideLength / 2;
            var zBegin = Bounds[0][2] + VoxelSideLength / 2;

            var decomp =
                DirectionalDecomposition.UniformDecompositionAlongZ(ts, zBegin, zLim,
                    VoxelSideLength);

            var slices = new List<List<List<PointLight>>>();
            if (!possibleNull)
                slices = decomp.Select(d => d.Paths).ToList();
            else if (decomp is null) return;
            
            //var crossSections = decomp.Select(d => d.Vertices).ToList();
            //Presenter.ShowVertexPathsWithSolid(crossSections, new List<TessellatedSolid> { ts });

            Parallel.For(0, zLim, k =>
            //for (var k = 0; k < VoxelsPerSide[2]; k++)
            {
                List<List<PointLight>> slice;
                if (!possibleNull)
                    slice = slices[k];
                else
                {
                    var sliceNullComparison = decomp[k];
                    if (sliceNullComparison is null) return;
                    slice = sliceNullComparison.Paths;
                }

                var intersectionPoints = Slice2D.IntersectionPointsAtUniformDistancesAlongX(
                    slice.Select(p => new PolygonLight(p)), xBegin, VoxelSideLength, xLim);
                    //parallel lines aligned with Y axis

                foreach (var intersections in intersectionPoints)
                {
                    var i = (int) Math.Floor((intersections.Key - xBegin) / VoxelSideLength);
                    var intersectValues = intersections.Value;
                    var n = intersectValues.Count;
                    for (var m = 0; m < n - 1; m += 2)
                    {
                        //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                        //Although other dimensions do not also do this. Everything operates with Round (effectively).
                        //Could reverse this to add more voxels
                        var sp = (int) Math.Round((intersectValues[m] - Bounds[0][1]) / VoxelSideLength);
                        if (sp < 0) sp = 0;
                        var ep = (int) Math.Round((intersectValues[m + 1] - Bounds[0][1]) / VoxelSideLength);
                        if (ep > yLim) ep = yLim;

                        for (var j = sp; j < ep; j++)
                            Voxels[i, j, k] = 1;
                    }
                }
            });
        }
    }
}
