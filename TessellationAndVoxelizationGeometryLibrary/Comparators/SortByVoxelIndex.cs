using System;
using System.Collections.Generic;
using TVGL.Voxelization;

namespace TVGL
{
    internal class SortByVoxelIndex : IComparer<IVoxel>
    {
        private int dimension;
        private int sense;

        internal SortByVoxelIndex(int dimension)
        {
            sense = Math.Sign(dimension);
            this.dimension = Math.Abs(dimension) - 1;
        }
        public int Compare(IVoxel x, IVoxel y)
        {
            if (x.Coordinates[dimension] > y.Coordinates[dimension]) return sense;
            else return -sense;
        }
    }
}