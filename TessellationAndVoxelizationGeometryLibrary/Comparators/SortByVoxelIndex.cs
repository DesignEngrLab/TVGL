using System;
using System.Collections.Generic;
using TVGL.Voxelization;

namespace TVGL
{
    internal class SortByVoxelIndex : IComparer<VoxelWithTessellationLinks>
    {
        private int dimension;
        private int sense;

        internal SortByVoxelIndex(int dimension)
        {
            sense = Math.Sign(dimension);
            this.dimension = Math.Abs(dimension) - 1;
        }
        public int Compare(VoxelWithTessellationLinks x, VoxelWithTessellationLinks y)
        {
            if (x.CoordinateIndices[dimension] > y.CoordinateIndices[dimension]) return sense;
            else return -sense;
        }
    }
}