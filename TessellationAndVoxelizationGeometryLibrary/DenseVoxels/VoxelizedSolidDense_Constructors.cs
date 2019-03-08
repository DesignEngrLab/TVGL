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
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense
    {
        #region Properties
        public byte[,,] Voxels;
        public readonly int Discretization;
        public readonly int LongDimension;
        public readonly int[] VoxelsPerSide;
        public readonly int[] BytesPerSide;
        public readonly int BytesOnLongSide;
        public double VoxelSideLength { get; internal set; }
        public double ByteSideLength => VoxelSideLength * 8;
        private readonly double[] Dimensions;
        public double[][] Bounds { get; protected set; }
        public double[] Offset => Bounds[0];
        public Color SolidColor { get; set; }
        public double Volume { get; internal set; }
        public double SurfaceArea { get; internal set; }
        public int Count { get; internal set; }
        #endregion

        /****************************************************
         * Each byte represents 8 voxels along the longest dimension of the part
         * In 2 dimensions, a simple Voxel array might look like so, where x is the long dimension:
         *
         * y
         * ^
         * |
         * [ 205 255 241 ]
         * [ 64  143 31  ]
         * [ 225 194 248 ] -> x
         *
         * Represented in binary, that would look like this:
         *
         * y
         * ^
         * | Byte 0           Byte 1           Byte 2
         * [ 1 1 0 0 1 1 0 1  1 1 1 1 1 1 1 1  1 1 1 1 0 0 0 1 ]
         * [ 0 1 0 0 0 0 0 0  1 0 0 0 1 1 1 1  0 0 0 1 1 1 1 1 ]
         * [ 1 1 1 0 0 0 0 1  1 1 0 0 0 0 1 0  1 1 1 1 1 0 0 0 ] -> x
         *   7             0  7             0  7             0   Bit within each byte
         *   0             7  8            15 16            31   Voxel coordinate
         *
         * Checking if a certain voxel exists requires left shifting (<<) the byte which contains it
         * by (V % 8) and then right shifting that same byte by 7, where V is voxel coordinate. This
         * isolates the corresponding bit for the queried voxel, and the value of that byte will be
         * one (1) if it exists, and zero (0) if it doesn't.
         ***************************************************/
        public VoxelizedSolidDense(int[] voxelsPerSide, int discretization, double voxelSideLength, int longDimension,
            IEnumerable<double[]> bounds, bool empty = false)
        {
            Discretization = discretization;
            LongDimension = longDimension;
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            BytesOnLongSide = VoxelsPerSide[LongDimension] / 8;
            BytesPerSide = new[]
            {
                LongDimension == 0 ? BytesOnLongSide : VoxelsPerSide[0],
                LongDimension == 1 ? BytesOnLongSide : VoxelsPerSide[1],
                LongDimension == 2 ? BytesOnLongSide : VoxelsPerSide[2]
            };
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            SolidColor = new Color(Constants.DefaultColor);

            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Voxels = new byte[BytesPerSide[0], BytesPerSide[1], BytesPerSide[2]];
            if (empty == false)
            {
                Parallel.For(0, xLim, m =>
                {
                    for (var n = 0; n < yLim; n++)
                    for (var o = 0; o < zLim; o++)
                        Voxels[m, n, o] = byte.MaxValue;
                });
            }

            UpdateProperties();
        }

        public VoxelizedSolidDense(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength,
            int longDimension, IEnumerable<double[]> bounds)
        {
            Voxels = (byte[,,])voxels.Clone();
            Discretization = discretization;
            LongDimension = longDimension;
            VoxelsPerSide = voxelsPerSide;
            BytesOnLongSide = VoxelsPerSide[LongDimension] / 8;
            BytesPerSide = new[]
            {
                LongDimension == 0 ? BytesOnLongSide : VoxelsPerSide[0],
                LongDimension == 1 ? BytesOnLongSide : VoxelsPerSide[1],
                LongDimension == 2 ? BytesOnLongSide : VoxelsPerSide[2]
            };
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolidDense(VoxelizedSolidDense vs)
        {
            Voxels = (byte[,,])vs.Voxels.Clone();
            Discretization = vs.Discretization;
            LongDimension = vs.LongDimension;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            BytesPerSide = vs.BytesPerSide.ToArray();
            BytesOnLongSide = vs.BytesOnLongSide;
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolidDense(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
            Discretization = discretization;
            SolidColor = new Color(Constants.DefaultColor);
            if (discretization < 3)
                throw new ArgumentException("Discretization must be greater than or equal to 3.");
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

            LongDimension = Dimensions.FindIndex(Dimensions.Max());
            VoxelSideLength = Dimensions[LongDimension] / voxelsOnLongSide;

            VoxelsPerSide = Dimensions.Select(d => (int) Math.Round(d / VoxelSideLength)).ToArray();
            BytesOnLongSide = VoxelsPerSide[LongDimension] / 8;


            BytesPerSide = new[]
            {
                LongDimension == 0 ? BytesOnLongSide : VoxelsPerSide[0],
                LongDimension == 1 ? BytesOnLongSide : VoxelsPerSide[1],
                LongDimension == 2 ? BytesOnLongSide : VoxelsPerSide[2]
            };

            Voxels = new byte[BytesPerSide[0], BytesPerSide[1], BytesPerSide[2]];

            VoxelizeSolid(ts);
            UpdateProperties();
        }

        private void VoxelizeSolid(TessellatedSolid ts)
        {
            var xLim = VoxelsPerSide[0];
            //var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            var decomp = DirectionalDecomposition.UniformDecompositionAlongZ(ts, 
                VoxelSideLength / 2 - Bounds[1][2], zLim, VoxelSideLength);
            var slices = decomp.Select(d => d.Paths).ToList();
            
            //var crossSections = decomp.Select(d => d.Vertices).ToList();
            //Presenter.ShowVertexPathsWithSolid(crossSections, new List<TessellatedSolid> { ts });

            Parallel.For(0, zLim, k =>
            //for (var k = 0; k < VoxelsPerSide[2]; k++)
            {
                var intersectionPoints = Slice2D.IntersectionPointsAtUniformDistancesAlongX(
                    slices[k].Select(p => new PolygonLight(p)), Bounds[0][0],
                    VoxelSideLength, xLim); //parallel lines aligned with Y axis
                var kB = k / 8;
                var kS = 7 - (k % 8);

                foreach (var intersections in intersectionPoints)
                {
                    var i = (int) Math.Floor((intersections.Key - Bounds[0][0]) / VoxelSideLength); // - 1;
                    var iB = i / 8;
                    var iS = 7 - (i % 8);

                    var intersectValues = intersections.Value;
                    var n = intersectValues.Count;
                    for (var m = 0; m < n - 1; m += 2)
                    {
                        //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                        //Floor/Floor seems to be okay
                        //Could reverse this to add more voxels
                        var sp = (int) Math.Floor((intersectValues[m] - Bounds[0][1]) / VoxelSideLength); // - 1;
                        if (sp == -1) sp = 0;
                        var ep = (int) Math.Floor((intersectValues[m + 1] - Bounds[0][1]) / VoxelSideLength); // - 1;

                        if (LongDimension == 0)
                        {
                            for (var j = sp; j < ep; j++)
                                Voxels[iB, j, k] += (byte) (1 << iS);
                        }
                        else if (LongDimension == 1)
                        {
                            for (var j = sp; j < ep; j++)
                                Voxels[i, j / 8, k] += (byte)(1 << (7 - (j % 8)));
                        }
                        else
                        {
                            for (var j = sp; j < ep; j++)
                                Voxels[i, j, kB] += (byte)(1 << kS);
                        }
                    }
                }
            });
        }
    }
}
