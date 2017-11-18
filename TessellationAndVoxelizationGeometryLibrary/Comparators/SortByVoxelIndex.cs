using System.Collections.Generic;
using TVGL.Voxelization;

namespace TVGL
{
    internal class SortByVoxelIndex : IComparer<Voxel>
    {
        private int dimension;

        internal SortByVoxelIndex(int dimension)
        {
            this.dimension = dimension;
        }
        public int Compare(Voxel x, Voxel y)
        {
            //if (x.Index[dimension].Equals(y.Index[dimension])) return 0;
            if (x.Index[dimension] < y.Index[dimension]) return -1;
            else return 1;
        }
    }
}