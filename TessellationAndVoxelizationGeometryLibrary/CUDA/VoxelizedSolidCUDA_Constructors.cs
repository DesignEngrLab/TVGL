// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Alan Grier
// Last Modified On : 02-18-2019
// ***********************************************************************
// <copyright file="CUDA.cs" company="Design Engineering Lab">
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

namespace TVGL.CUDA
{
    /// <summary>
    /// Class VoxelizedSolidCUDA.
    /// </summary>
    public partial class VoxelizedSolidCUDA
    {
        #region Properties
        public byte[,,] Voxels;
        public readonly int Discretization;
        public readonly int[] VoxelsPerSide;
        public double VoxelSideLength { get; internal set; }
        private readonly double[] Dimensions;
        public double[][] Bounds { get; protected set; }
        public double[] Offset => Bounds[0];
        public Color SolidColor { get; set; }
        public double Volume { get; internal set; }
        public double SurfaceArea { get; internal set; }
        public int Count { get; internal set; }
        #endregion

        public VoxelizedSolidCUDA(int[] voxelsPerSide, int discretization, double voxelSideLength,
            IEnumerable<double[]> bounds, byte value = 0)
        {
            VoxelsPerSide = (int[]) voxelsPerSide.Clone();
            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];
            if (value != 0)
            {
                for (var m = 0; m < VoxelsPerSide[0]; m++)
                for (var n = 0; n < VoxelsPerSide[1]; n++)
                for (var o = 0; o < VoxelsPerSide[2]; o++)
                    Voxels[m, n, o] = value;
            }
            Discretization = discretization;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            SolidColor = new Color(Constants.DefaultColor);
            Count = value == 0 ? 0 : VoxelsPerSide[0] * VoxelsPerSide[1] * VoxelsPerSide[2];
            UpdateProperties();
        }

        public VoxelizedSolidCUDA(byte[,,] voxels, int discretization, int[] voxelsPerSide, double voxelSideLength, IEnumerable<double[]> bounds)
        {
            Voxels = (byte[,,])voxels.Clone();
            Discretization = discretization;
            VoxelsPerSide = voxelsPerSide;
            VoxelSideLength = voxelSideLength;
            Bounds = bounds.ToArray();
            Dimensions = Bounds[1].subtract(Bounds[0], 3);
            SolidColor = new Color(Constants.DefaultColor);
            UpdateProperties();
        }

        public VoxelizedSolidCUDA(VoxelizedSolidCUDA vs)
        {
            Voxels = (byte[,,]) vs.Voxels.Clone();
            Discretization = vs.Discretization;
            VoxelsPerSide = vs.VoxelsPerSide.ToArray();
            VoxelSideLength = vs.VoxelSideLength;
            Dimensions = vs.Dimensions.ToArray();
            Bounds = vs.Bounds.ToArray();
            SolidColor = new Color(Constants.DefaultColor);
            Volume = vs.Volume;
            SurfaceArea = vs.SurfaceArea;
            Count = vs.Count;
        }

        public VoxelizedSolidCUDA(TessellatedSolid ts, int discretization, IReadOnlyList<double[]> bounds = null)
        {
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

            var longestSide = Dimensions.Max();
            VoxelSideLength = longestSide / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int) Math.Round(d / VoxelSideLength)).ToArray();

            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];
            Count = 0;

            VoxelizeSolid(ts);
            UpdateProperties();
        }

        private void VoxelizeSolid(TessellatedSolid ts)
        {
            var counts = new ConcurrentDictionary<int, int>();

            var projectionDirection = new []{ 0.0, 0, -1.0 }; //-Z
            var lineSweepDirection = new []{ 1.0, 0, 0 }; //+X

            var lineSweep2D = MiscFunctions.Get2DProjectionVector(lineSweepDirection, projectionDirection);
            var decomp = DirectionalDecomposition.UniformDecompositionAlongZ(ts, 
                VoxelSideLength / 2 - Bounds[0][2], VoxelSideLength);
            var slices = decomp.Select(d => d.Paths).ToList();

            //var crossSections = decomp.Select(d => d.Vertices).ToList();
            //Presenter.ShowVertexPathsWithSolid(crossSections, new List<TessellatedSolid> { ts });

            //Parallel.For(0, VoxelsPerSide[2], k =>
            for (var k = 0; k < VoxelsPerSide[2]; k++)
            {
                var kCount = 0;

                var intersectionPoints = Slice2D.IntersectionPointsAtUniformDistances(
                    slices[k].Select(p => new PolygonLight(p)), lineSweep2D, VoxelSideLength / 2 + Bounds[0][0],
                    VoxelSideLength, VoxelsPerSide[0]); //parallel lines aligned with Y axis

                foreach (var intersections in intersectionPoints)
                {
                    var i = (int) Math.Floor((intersections[0].X - Bounds[0][0]) / VoxelSideLength); // - 1;
                    for (var m = 0; m < intersections.Count - 1; m += 2)
                    {
                        //Use ceiling for lower bound and floor for upper bound to guarantee voxels are inside.
                        //Floor/Floor seems to be okay
                        //Could reverse this to add more voxels
                        var sp = (int) Math.Floor((intersections[m].Y - Bounds[0][1]) / VoxelSideLength); // - 1;
                        var ep = (int) Math.Floor((intersections[m + 1].Y - Bounds[0][1]) / VoxelSideLength); // - 1;

                        for (var j = sp; j < ep; j++)
                        {
                            Voxels[i, j, k] = 1;
                            kCount++;
                        }
                    }
                }

                counts.TryAdd(k, kCount);
            }//);

            foreach (var kvp in counts)
                Count += kvp.Value;
        }
    }
}
