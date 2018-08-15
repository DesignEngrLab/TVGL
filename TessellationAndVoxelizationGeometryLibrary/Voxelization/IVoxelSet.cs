using System;
using System.Collections;
using System.Collections.Generic;

namespace TVGL.Voxelization
{
    internal abstract class IVoxelSet : IEnumerable<IVoxel>
    {
       internal abstract int Count { get; }

        internal abstract bool AddOrReplace(IVoxel newVoxel);
        internal abstract void AddRange(ICollection<IVoxel> voxels);
        internal abstract bool Contains(IVoxel item);
        internal abstract bool Contains(long item);
        internal abstract int CountDescendants(long ancestor, int ancestorLevel);
        internal abstract int CountDescendants(long ancestor, int ancestorLevel, VoxelRoleTypes role);
        internal abstract List<IVoxel> GetDescendants(long ancestor, int ancestorLevel);
       public abstract IEnumerator<IVoxel> GetEnumerator();
        internal abstract long GetFullVoxelID(long item);
        internal abstract IVoxel GetVoxel(long item);
        internal abstract bool Remove(IVoxel item);
        internal abstract bool Remove(long item);
        internal abstract int RemoveDescendants(long ancestor, int ancestorLevel);
        internal abstract int RemoveWhere(Predicate<IVoxel> match);
       // internal abstract IVoxelSet Copy(VoxelizedSolid solid);



        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class VoxelBinSet : IVoxelSet
    {
        private readonly VoxelBinClass[,,] voxelBins;
        private readonly int numBitsInID;

        internal VoxelBinSet(int[] v, int numBitsInID)
        {
            voxelBins =new VoxelBinClass[v[0],v[1],v[2]]; 
            this.numBitsInID = numBitsInID;
        }


        internal override int Count => throw new NotImplementedException();

        public override IEnumerator<IVoxel> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal override bool AddOrReplace(IVoxel newVoxel)
        {
            throw new NotImplementedException();
        }

        internal override void AddRange(ICollection<IVoxel> voxels)
        {
            throw new NotImplementedException();
        }

        internal override bool Contains(IVoxel item)
        {
            throw new NotImplementedException();
        }

        internal override bool Contains(long item)
        {
            throw new NotImplementedException();
        }

        internal override int CountDescendants(long ancestor, int ancestorLevel)
        {
            throw new NotImplementedException();
        }

        internal override int CountDescendants(long ancestor, int ancestorLevel, VoxelRoleTypes role)
        {
            throw new NotImplementedException();
        }

        internal override List<IVoxel> GetDescendants(long ancestor, int ancestorLevel)
        {
            throw new NotImplementedException();
        }

        internal override long GetFullVoxelID(long item)
        {
            throw new NotImplementedException();
        }

        internal override IVoxel GetVoxel(long item)
        {
            throw new NotImplementedException();
        }

        internal override bool Remove(IVoxel item)
        {
            throw new NotImplementedException();
        }

        internal override bool Remove(long item)
        {
            throw new NotImplementedException();
        }

        internal override int RemoveDescendants(long ancestor, int ancestorLevel)
        {
            throw new NotImplementedException();
        }

        internal override int RemoveWhere(Predicate<IVoxel> match)
        {
            throw new NotImplementedException();
        }
    }
}