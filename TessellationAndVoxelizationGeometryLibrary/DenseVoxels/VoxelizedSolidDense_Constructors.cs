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
        public readonly int[] VoxelsPerSide;
        public readonly int[] BytesPerSide;
        public double VoxelSideLength { get; internal set; }
        private readonly double[] Dimensions;
        public double[][] Bounds { get; protected set; }
        public double[] Offset => Bounds[0];
        public Color SolidColor { get; set; }
        public double Volume { get; internal set; }
        public double SurfaceArea { get; internal set; }
        public long Count { get; internal set; }
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
         * 
         * note that for speed, the integer divide, x / 8, is replaced with x >> 3
         * and the remainder x % 8 is replaced with x & 7 (which is the same thing, if you think about it).
         ***************************************************/
        public VoxelizedSolidDense(int[] voxelsPerSide, double voxelSideLength,
            IEnumerable<double[]> bounds, bool empty = false)
        {
            VoxelsPerSide = (int[])voxelsPerSide.Clone();
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;
            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };
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

        public VoxelizedSolidDense(byte[,,] voxels, double voxelSideLength, IEnumerable<double[]> bounds = null)
        {
            Voxels = (byte[,,])voxels.Clone();
            VoxelsPerSide = new[] { voxels.GetLength(0), voxels.GetLength(1), voxels.GetLength(2) };
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;
            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolidDense(VoxelizedSolidDense vs)
        {
            Voxels = (byte[,,])vs.Voxels.Clone();
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            BytesPerSide = vs.BytesPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolidDense(TessellatedSolid ts, int voxelsOnLongSide, IReadOnlyList<double[]> bounds = null)
        {
            SolidColor = new Color(Constants.DefaultColor);
            Bounds = new double[2][];
            Dimensions = new double[3];

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
            for (var i = 0; i < 3; i++)
                Dimensions[i] = Bounds[1][i] - Bounds[0][i];

            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;

            VoxelsPerSide = Dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLength)).ToArray();
            var bytesInX = VoxelsPerSide[0] >> 3;
            if ((VoxelsPerSide[0] & 7) != 0) bytesInX++;


            BytesPerSide = new[] { bytesInX, VoxelsPerSide[1], VoxelsPerSide[2] };

            Voxels = new byte[BytesPerSide[0], BytesPerSide[1], BytesPerSide[2]];

            VoxelizeSolid(ts);
            UpdateProperties();
        }

        private void VoxelizeSolid(TessellatedSolid ts)
        {
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
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
                var kB = k >> 3;
                var kS = 7 - (k & 7);

                foreach (var intersections in intersectionPoints)
                {
                    var i = (int)Math.Floor((intersections.Key - Bounds[0][0]) / VoxelSideLength); // - 1;
                    var iB = i >> 3;
                    var iS = 7 - (i & 7);

                    var intersectValues = intersections.Value;
                    var n = intersectValues.Count;
                    for (var m = 0; m < n - 1; m += 2)
                    {
                        //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                        //Floor/Floor seems to be okay
                        //Floor/ceiling with the yLim check also works
                        //Could reverse this to add more voxels
                        var sp = (int)Math.Floor((intersectValues[m] - Bounds[0][1]) / VoxelSideLength); // - 1;
                        if (sp == -1) sp = 0;
                        var ep = (int)Math.Ceiling((intersectValues[m + 1] - Bounds[0][1]) / VoxelSideLength); // - 1;
                        if (ep >= yLim) ep = yLim;

                        for (var j = sp; j < ep; j++)
                            Voxels[iB, j, k] += (byte)(1 << iS);

                    }
                }
            });
        }
    }
}
