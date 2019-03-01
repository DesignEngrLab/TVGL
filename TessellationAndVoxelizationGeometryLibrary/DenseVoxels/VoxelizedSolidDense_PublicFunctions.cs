using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL.DenseVoxels
{
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense
    {
        public int[][] GetNeighbors(int i, int j, int k)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] != 0) neighbors[1] = new[] { i + 1, j, k };
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] != 0) neighbors[3] = new[] { i, j + 1, k };
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] != 0) neighbors[5] = new[] { i, j, k + 1 };
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors[0] = new[] { i - 1, j, k };
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors[2] = new[] { i, j - 1, k };
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors[4] = new[] { i, j, k - 1 };
            return neighbors;
        }

        public int NumNeighbors(int i, int j, int k)
        {
            var neighbors = 0;
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] != 0) neighbors++;
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] != 0) neighbors++;
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] != 0) neighbors++;
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors++;
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors++;
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors++;
            return neighbors;
        }
        
        public void UpdateProperties()
        {
            SetCount();
            SetVolume();
            SetSurfaceArea();
        }

        private void SetCount()
        {
            var count = new ConcurrentDictionary<int, int>();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                var counter = 0;
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 1; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] != 0)
                        counter++;
                count.TryAdd(i, counter);
            });
            Count = count.Values.Sum();
        }

        private void SetVolume()
        {
            Volume = Count * Math.Pow(VoxelSideLength, 3);
        }

        private void SetSurfaceArea()
        {
            var neighbors = new ConcurrentDictionary<int, int>();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                var neighborCount = 0;
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 1; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] != 0)
                        neighborCount += NumNeighbors(i, j, k);
                neighbors.TryAdd(i, neighborCount);
            });
            SurfaceArea = (Count * 6 - neighbors.Values.Sum()) * Math.Pow(VoxelSideLength, 2);
        }

        public VoxelizedSolidDense Copy()
        {
            return new VoxelizedSolidDense(this);
        }

        public static VoxelizedSolidDense Copy(VoxelizedSolidDense vs)
        {
            return new VoxelizedSolidDense(vs);
        }

        public VoxelizedSolidDense CreateBoundingSolid()
        {
            return new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds, 1);
        }

        public VoxelizedSolidDense InvertToNewSolid()
        {
            var vs = new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds);
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] == 0)
                        vs.Voxels[i, j, k] = 1;
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense UnionToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = Copy();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                {
                    if (solids.Any(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 1;
                }
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense IntersectToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds);
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] == 1 && solids.All(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 1;
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense SubtractToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = Copy();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (solids.Any(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 0;
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense DraftToNewSolid(VoxelDirections vd)
        {
            var vs = Copy();

            var draftDir = (int) vd;
            var draftIndex = Math.Abs(draftDir) - 1;
            var planeIndices = new int[2];
            var ii = 0;
            for (var i = 0; i < 3; i++)
                if (i != draftIndex)
                {
                    planeIndices[ii] = i;
                    ii++;
                }

            Parallel.For(0, VoxelsPerSide[planeIndices[0]], m =>
            {
                for (var n = 0; n < VoxelsPerSide[planeIndices[1]]; n++)
                {
                    var fillAll = false;
                    for (var p = 0; p < VoxelsPerSide[draftIndex]; p++)
                    {
                        var q = draftDir > 0 ? p : VoxelsPerSide[draftIndex] - 1 - p;
                        int i;
                        int j;
                        int k;

                        switch (draftIndex)
                        {
                            case 0:
                                i = q;
                                j = m;
                                k = n;
                                break;
                            case 1:
                                i = m;
                                j = q;
                                k = n;
                                break;
                            case 2:
                                i = m;
                                j = n;
                                k = q;
                                break;
                            default:
                                continue;
                        }

                        if (fillAll)
                        {
                            vs.Voxels[i, j, k] = 1;
                            continue;
                        }
                        if (vs.Voxels[i, j, k] != 0)
                            fillAll = true;
                    }

                }
            });

            vs.UpdateProperties();
            return vs;
        }
    }
}
