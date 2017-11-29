using System.Collections.Generic;
using TVGL.Voxelization;

namespace TVGL
{
    internal class SortByVoxelIndex : IComparer<VoxelClass>
    {
        private int dimension;

        internal SortByVoxelIndex(int dimension)
        {
            this.dimension = dimension;
        }
        public int Compare(VoxelClass x, VoxelClass y)
        {
            if (x.Coordinates[dimension] < y.Coordinates[dimension]) return -1;
            else return 1;
        }
    }
}