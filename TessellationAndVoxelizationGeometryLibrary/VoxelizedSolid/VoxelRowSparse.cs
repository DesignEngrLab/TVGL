using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL.Voxelization
{
    public struct VoxelRowSparse : IVoxelRow
    {
        public readonly List<ushort> indices;

        public int Count
        {
            get
            {
                var sum = 0;
                for (int i = 0; i < indices.Count - 1; i += 2)
                    sum += indices[i + 1] - indices[i];
                return sum;
            }
        }

        //because "structs cannot contain explicit parameterless constructors", I'm forced
        //to add the dummy input
        public VoxelRowSparse(bool dummy)
        {
            indices = new List<ushort>();
        }
        public VoxelRowSparse(IVoxelRow row)
        {
            if (row is VoxelRowSparse)
            {
                indices = new List<ushort>(((VoxelRowSparse)row).indices);
            }
            else
            {
                indices = new List<ushort>();
                var denseRow = ((VoxelRowDense)row);
                var lastVal = false;
                ushort i = 0;
                foreach (var thisByte in denseRow.values)
                {
                    var currentByte = thisByte;
                    for (int j = 0; j < 8; j++)
                    {
                        var currentVal = (currentByte & 0b10000000) != 0;
                        if (currentVal != lastVal)
                        {
                            lastVal = currentVal;
                            indices.Add(i);
                        }
                        currentByte <<= 1;
                        i++;
                    }
                }
                if (lastVal) indices.Add(i);
            }
        }
        public bool this[int index]
        {
            get => GetValue(index);
            set
            {
                var dummy = 0;
                if (value) TurnOn((ushort)index, ref dummy);
                else TurnOff((ushort)index, ref dummy);
            }
        }


        bool GetValue(int index)
        {
            var count = indices.Count;
            if (count == 0 || index < indices[0] || index > indices[count - 1]) return false;
            BinarySearch(indices, count, index, out var _, out var voxelIsOn);
            return voxelIsOn;
        }
        public (bool, bool) GetNeighbors(int index)
        {
            var count = indices.Count;
            if (count == 0) return (false, false);
            if (index == 0)
            {
                var upperNeighber = GetValue(index + 1);
                return (false, upperNeighber);
            }
            var i = BinarySearch(indices, count, index, out var valueExists, out var voxelIsOn);
            if (voxelIsOn) //then index is a value in this list - either a lower or upper range
            {
                if (valueExists) //then must be at the beginning of the range and the previous voxel is off, but this could
                                 //a lone voxel, so need to check next.
                    return (false, index + 1 < indices[i + 1]);
                else //the current is on and not the beginning of the range, but this next could be off
                    return (true, index + 1 < indices[i]);
            }
            else //the current voxel is off
            {
                if (valueExists) //then current is end of range which means previous is on, current is off, but next could be 
                    //start of new range 
                    return (true, index + 1 == indices[i + 1]);
                else  //but neighbors could be inside
                    return (false, index + 1 == indices[i]);
            }
        }
        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearch(IList<ushort> array, int length, double value, out bool valueExists, out bool voxelIsOn, int lowerBound = 0)
        {
            valueExists = true;
            var lo = lowerBound;
            var hi = length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                var c = array[i];
                if (c == value)
                {
                    voxelIsOn = (i & 0b1) == 0;
                    return i; //note the '=='. the value exists in the list, so return
                }
                if (c < value) lo = i + 1;
                else hi = i - 1;
            }
            // the value was not found on this, so set that bool then return lo, which is actually
            // the array value just larger than it
            valueExists = false;
            voxelIsOn = (lo & 0b1) != 0;
            return lo;
        }

        void TurnOn(ushort value, ref int index)
        {
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(value);
                indices.Add((ushort)(value + 1));
                index = 1;
                return;
            }
            index = BinarySearch(indices, count, value, out var valueExists, out var voxelIsOn);
            if (voxelIsOn) return; //it's already on
            if (valueExists) indices[index]++;
            else
            {
                indices.Insert(index, (ushort)(value + 1));
                indices.Insert(index, value);
                index++;
            }
        }

        void TurnOff(ushort value, ref int index)
        {
            var count = indices.Count;
            if (count == 0)
            {
                index = 0;
                return; //nothing to do. there are no voxels anyway
            }
            index = BinarySearch(indices, count, value, out var valueExists, out var voxelIsOn);
            if (!voxelIsOn) return; //it's already off
            if (valueExists) indices[index]++;
            else
            {
                indices.Insert(index, (ushort)(value + 1));
                indices.Insert(index, value);
                index++;
            }
        }

        public void Union(IVoxelRow[] others, int offset = 0)
        {
            for (int i = 0; i < others.Length; i++)
            {
                IVoxelRow other = others[i];
                if (other is VoxelRowDense) other = new VoxelRowSparse(other);
                var otherIndices = ((VoxelRowSparse)other).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                    TurnOnRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
            }
        }
        public void Intersect(IVoxelRow[] others, int offset = 0)
        {
            for (int i = 0; i < others.Length; i++)
            {
                IVoxelRow other = others[i];
                if (other is VoxelRowDense) other = new VoxelRowSparse(other);
                var otherIndices = ((VoxelRowSparse)other).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                    TurnOnRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
            }
        }

        public void Subtract(IVoxelRow[] subtrahends, int offset = 0)
        {
            for (int i = 0; i < subtrahends.Length; i++)
            {
                IVoxelRow subtrahend = subtrahends[i];
                if (subtrahend is VoxelRowDense) subtrahend = new VoxelRowSparse(subtrahend);
                var otherIndices = ((VoxelRowSparse)subtrahend).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                    TurnOffRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
            }
        }
        public void TurnOnRange(ushort lo, ushort hi)
        {
            var dummy = 0;
            TurnOnRange(lo, hi, ref dummy);
        }
        private void TurnOnRange(ushort lo, ushort hi, ref int indexLowerBound)
        {
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(lo);
                indices.Add(hi);
                indexLowerBound = 1;
                return;
            }
            var loIndex = BinarySearch(indices, indices.Count, lo, out var loValueExists, out var loVoxelIsOn, indexLowerBound);
            indexLowerBound = loIndex;
            var hiIndex = BinarySearch(indices, indices.Count, hi, out var hiValueExists, out var hiVoxelIsOn, indexLowerBound);
            indexLowerBound = hiIndex;
            TurnOnRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn);
        }
        private void TurnOnRange(ushort lo, int loIndex, bool loValueExists, bool loVoxelIsOn, ushort hi, int hiIndex, bool hiValueExists, bool hiVoxelIsOn)
        {
            //if (loValueExists && loVoxelIsOn) loIndex++;
            if (hiValueExists && !hiVoxelIsOn) hiIndex++;
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            if (!hiVoxelIsOn) indices.Insert(loIndex, hi);
            if (!loVoxelIsOn) indices.Insert(loIndex, lo);
        }

        public void TurnOffRange(ushort lo, ushort hi)
        {
            var dummy = 0;
            TurnOffRange(lo, hi, ref dummy);
        }

        private void TurnOffRange(ushort lo, ushort hi, ref int indexLowerBound)
        {
            var count = indices.Count;
            if (count == 0) return;
            var loIndex = BinarySearch(indices, indices.Count, lo, out var loValueExists, out var loVoxelIsOn, indexLowerBound);
            indexLowerBound = loIndex;
            var hiIndex = BinarySearch(indices, indices.Count, hi, out var hiValueExists, out var hiVoxelIsOn, indexLowerBound);
            indexLowerBound = hiIndex;
            TurnOffRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn);
        }

        private void TurnOffRange(ushort lo, int loIndex, bool loValueExists, bool loVoxelIsOn, ushort hi, int hiIndex, bool hiValueExists, bool hiVoxelIsOn)
        {
            if (loValueExists && !loVoxelIsOn) loIndex++;
            if (hiValueExists && hiVoxelIsOn) hiIndex--;
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            if (hiVoxelIsOn) indices.Insert(loIndex, hi);
            if (loVoxelIsOn) indices.Insert(loIndex, lo);
        }
    }
}
