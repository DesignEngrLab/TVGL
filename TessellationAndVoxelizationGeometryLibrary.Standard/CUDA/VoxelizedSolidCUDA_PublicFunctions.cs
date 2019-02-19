using System;
using System.Collections.Generic;
using System.Text;
using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;

namespace TVGL.CUDA
{
    /// <summary>
    /// Class VoxelizedSolidCUDA.
    /// </summary>
    public partial class VoxelizedSolidCUDA
    {
        public int[][] GetNeighbors(int i, int j, int k)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] == 1) neighbors[0] = new[] { i + 1, j, k };
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] == 1) neighbors[1] = new[] { i, j + 1, k };
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] == 1) neighbors[2] = new[] { i, j, k + 1 };
            if (i != 0 && Voxels[i - 1, j, k] == 1) neighbors[3] = new[] { i - 1, j, k };
            if (j != 0 && Voxels[i, j - 1, k] == 1) neighbors[4] = new[] { i, j - 1, k };
            if (k != 0 && Voxels[i, j, k - 1] == 1) neighbors[5] = new[] { i, j, k - 1 };
            return neighbors;
        }

        public VoxelizedSolidCUDA Copy(VoxelizedSolidCUDA vs)
        {
            return new VoxelizedSolidCUDA(vs);
        }

        static void ANDKernel(Index3 index, ArrayView<byte, Index3> a, ArrayView<byte, Index3> b,
            ArrayView<byte, Index3> c)
        {
            a[index] = (byte) (b[index] & c[index]);
        }
        public VoxelizedSolidCUDA IntersectToNewSolid(VoxelizedSolidCUDA vs)
        {
            byte[,,] newVoxels;
            var idx = new Index3(VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]);
            using (var context = new Context())
            {
                using (var accelerator = new CPUAccelerator(context))
                {
                    var andKernel =
                        accelerator
                            .LoadAutoGroupedStreamKernel<Index3, ArrayView<byte, Index3>, ArrayView<byte, Index3>,
                                ArrayView<byte, Index3>>(ANDKernel);
                    using (var buffer = accelerator.Allocate<byte,Index3>(idx))
                    {
                        andKernel();
                        accelerator.Synchronize();
                        newVoxels = buffer.GetAsRawArray();
                    }
                }
            }
            return new VoxelizedSolidCUDA(newVoxels, Discretization, VoxelsPerSide, VoxelSideLength, Bounds);
        }
    }
}
