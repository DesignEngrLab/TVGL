using System;
using System.Collections;
using System.Collections.Generic;

namespace TVGL.Voxelization
{
    internal class VoxelBinSet : IEnumerable<VoxelBinClass>
    {
        private readonly VoxelBinClass[,,] voxelBins;
        private readonly long mask;
        internal readonly int[] size;
        private readonly int xShift, yShift, zShift;

        internal VoxelBinSet(int[] v, int numBitsInID)
        {
            size = v;
            voxelBins = new VoxelBinClass[size[0], size[1], size[2]];
            mask = (long)(Math.Pow(2, numBitsInID) - 1);
            xShift = 24 - numBitsInID;
            yShift = 44 - numBitsInID;
            zShift = 64 - numBitsInID;
            Count = 0;
        }


        internal int Count { get; private set; }

        public IEnumerator<VoxelBinClass> GetEnumerator()
        {
            return new VoxelBinEnumerator(voxelBins, size[0], size[1], size[2]);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        internal bool AddOrReplace(VoxelBinClass newVoxel)
        {
            var i = (int)((newVoxel.ID >> xShift) & mask);
            var j = (int)((newVoxel.ID >> yShift) & mask);
            var k = (int)((newVoxel.ID >> zShift) & mask);
            var addNew = (voxelBins[i, j, k] == null);
            if (addNew) Count++;
            voxelBins[i, j, k] = (VoxelBinClass)newVoxel;
            return addNew;
        }

        internal void AddRange(ICollection<VoxelBinClass> voxels)
        {
            foreach (var voxel in voxels)
                AddOrReplace(voxel);
        }

        internal bool Contains(VoxelBinClass item)
        {
            var i = (int)((item.ID >> xShift) & mask);
            var j = (int)((item.ID >> yShift) & mask);
            var k = (int)((item.ID >> zShift) & mask);
           // if (i >= size[0] || j >= size[1] || k >= size[2]) return false;
            return (voxelBins[i, j, k] != null);
        }

        internal bool Contains(long item)
        {
            var i = (int)((item >> xShift) & mask);
            var j = (int)((item >> yShift) & mask);
            var k = (int)((item >> zShift) & mask);
           // if (i >= xSize || j >= ySize || k >= zSize) return false;
            return (voxelBins[i, j, k] != null);
        }


        internal long GetFullVoxelID(long item)
        {
            var i = (int)((item >> xShift) & mask);
            var j = (int)((item >> yShift) & mask);
            var k = (int)((item >> zShift) & mask);
         //   if (i >= xSize || j >= ySize || k >= zSize) return 0L;
            return voxelBins[i, j, k].ID;
        }

        internal VoxelBinClass GetVoxel(long item)
        {
            var i = (int)((item >> xShift) & mask);
            var j = (int)((item >> yShift) & mask);
            var k = (int)((item >> zShift) & mask);
            if (i >= size[0] || j >= size[1] || k >= size[2]) return null;
            return voxelBins[i, j, k];
        }

        internal bool Remove(VoxelBinClass item)
        {
            var i = (int)((item.ID >> xShift) & mask);
            var j = (int)((item.ID >> yShift) & mask);
            var k = (int)((item.ID >> zShift) & mask);
            if (voxelBins[i, j, k] == null) return false;
            Count--;
            voxelBins[i, j, k] = null;
            return true;
        }

        internal bool Remove(long item)
        {
            var i = (int)((item >> xShift) & mask);
            var j = (int)((item >> yShift) & mask);
            var k = (int)((item >> zShift) & mask);
            if (voxelBins[i, j, k] == null) return false;
            Count--;
            voxelBins[i, j, k] = null;
            return true;
        }

        internal VoxelBinClass GetVoxel(int[] index)
        {
            return voxelBins[index[0], index[1], index[2]];
        }
        }

    public struct VoxelBinEnumerator : IEnumerator<VoxelBinClass>
    {
        private int i, j, k;
        private readonly int xSize, ySize, zSize;
        private VoxelBinClass[,,] voxelBins;

        public VoxelBinEnumerator(VoxelBinClass[,,] voxelBins, int xSize, int ySize, int zSize)
        {
            i = -1;
            j = k = 0;
            this.voxelBins = voxelBins;
            this.xSize = xSize;
            this.ySize = ySize;
            this.zSize = zSize;
        }

        public void Dispose()
        {
            i = -1;
            j = k = 0;
            voxelBins = new VoxelBinClass[xSize, ySize, zSize];
        }

        public bool MoveNext()
        {
            do
            {
                i++;
                if (i == xSize)
                {
                    i = 0;
                    j++;
                }
                if (j == ySize)
                {
                    j = 0;
                    k++;
                }
                if (k == zSize) return false;
            } while (voxelBins[i, j, k] == null);
            return true;
        }

        public VoxelBinClass Current => voxelBins[i, j, k];

        Object IEnumerator.Current => Current;

        void IEnumerator.Reset()
        {
            i = -1;
            j = k = 0;
            //  current = 0L;
        }
    }
}