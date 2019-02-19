using System;
using System.Collections.Generic;
using System.Text;
using ILGPU;

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

        static void ANDKernal(Index3 index, ArrayView<byte> a, ArrayView<byte> b, ArrayView<byte> c)
        {
            a[index] = b[index] & c[index];
        }
        public VoxelizedSolidCUDA IntersectToNewSolid(VoxelizedSolidCUDA vs)
        {
            byte[,,] newVoxels;
            using (var context = new Context())
            {
                using (var accelerator = new CPUAccelerator(context))
                {
                    var AndKernal = accelerator.LoadAutoGroupedStreamKernel<Index3, ArrayView<byte>, ArrayView<byte>, ArrayView<byte>>(ANDKernal);
                    using (var buffer = accelerator.Allocate<int>(1024))
                    {
                        AndKernal(buffer.Lenght, buffer.View, 42);
                        accelerator.Synchronize();
                        var newVoxels = buffer.GetAsArray();
                    }
                }
            }
            return new VoxelizedSolidCUDA(newVoxels, Discretization, VoxelSideLength, Bounds);
        }
    }
}
