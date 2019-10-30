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
                    sum += 1 + indices[i + 1] - indices[i];
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
                            //if (currentVal) //then just started a new row of on voxels
                                indices.Add(i);
                            //else //then just started a new row of off voxels.
                                 //note that since the limits are inclusive, we record the previous one
                                //indices.Add((ushort)(i - 1));
                        }
                        currentByte <<= 1;
                        i++;
                    }
                }
                if (denseRow[i]) indices.Add(i);
            }
        }
        public bool this[int index]
        {
            get => GetValue(index);
            set { if (value) TurnOn((ushort)index); else TurnOff((ushort)index); }
        }


        bool GetValue(int index)
        {
            var count = indices.Count;
            if (count == 0 || index < indices[0] || index > indices[count - 1]) return false;
            var i = BinarySearch(indices, count, index, out var valueExists);
            if (valueExists) //then index is a value in this list - either a lower or upper range
                             //but since ranges are inclusive, then it is clearly 'on'/true
                return true;
            else return (i & 0b1) != 0; //if i is odd, then that corresponds to the top of the range
                                        //which means index is less than a top and greater than a bottom. 
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
            var i = BinarySearch(indices, count, index, out var valueExists);
            if (valueExists) //then index is a value in this list - either a lower or upper range
            {
                if ((i & 0b1) != 0) //odd means top of range, but bottom? need to check if a lone voxel
                    return (index - 1 >= indices[i - 1], false);
                else //even bottom of the range, check upper neighbor is in range or is it a lone voxel
                    return (false, index + 1 <= indices[i + 1]);
            }
            else
            {
                if ((i & 0b1) == 0) //even means bottom of range and it's less so index is outside, 
                                    //but neighbors could be inside
                    return (index - 1 <= indices[i - 1], index + 1 >= indices[i]);
                else //odd so comfortably in a range. there is only one result
                    return (true, true);
            }
        }
        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearch(IList<ushort> array, int length, double value, out bool valueExists, int lowerBound = 0)
        {
            valueExists = true;
            var lo = lowerBound;
            var hi = length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                var c = array[i];
                if (c == value) return i; //note the '=='. the value exists in the list, so return
                if (c < value) lo = i + 1;
                else hi = i - 1;
            }
            // the value was not found on this, so set that bool then return lo, which is actually
            // the array value just larger than it
            valueExists = false;
            return lo;
        }
        void TurnOn(ushort value)
        {
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(value);
                indices.Add(value);
                return;
            }
            var i = BinarySearch(indices, count, value, out var valueExists);
            if (valueExists) return; //if value exists there is nothing to do
            if ((i & 0b1) == 0) //even means bottom of range and it's less 
                                //so index is outside. we need to add it twice since it 
                                //is both the beginning and end of its range - however it
                                //could be adding to the previous or next range.
            {
                if (value + 1 == indices[i])
                    indices[i]--;
                else if (value - 1 == indices[i - 1])
                    indices[i - 1]++;
                else
                {
                    indices.Insert(i, value);
                    indices.Insert(i, value);
                }
            }
        }
        void TurnOff(ushort value)
        {
            var count = indices.Count;
            if (count == 0) return; //nothing to do. there are no voxels anyway
            var i = BinarySearch(indices, count, value, out var valueExists);
            if (valueExists) // i is positive when the value is on the list
            {
                if ((i & 0b1) == 0) // even means bottom of a range
                {
                    if (indices[i] == indices[i + 1]) //if its a lone voxel, then need to remove range
                    {
                        indices.RemoveAt(i);
                        indices.RemoveAt(i);
                    }
                    else indices[i]++; //otherwise move up bottom by one
                }
                else //top of the range
                {
                    if (indices[i] == indices[i - 1]) //if its a lone voxel, then need to remove range
                    {
                        indices.RemoveAt(i - 1);
                        indices.RemoveAt(i - 1);
                    }
                    else indices[i]--; //otherwise move down the top of range by one
                }
            }
            else if ((i & 0b1) != 0) //odd means top of range so comfortable within a string of on voxels 
                                     //need to make a hole...given inclusive range, this one is a little unintuitive
            {
                indices.Insert(i, (ushort)(value + 1));
                indices.Insert(i, (ushort)(value - 1));
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
                var lowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                {
                    var lo = otherIndices[j];
                    var hi = otherIndices[j + 1];
                    var loIndex = BinarySearch(indices, indices.Count, lo, out _, lowerBound);
                    lowerBound = loIndex;
                    var hiIndex = BinarySearch(indices, indices.Count, hi, out _, lowerBound);
                    lowerBound = hiIndex;
                    TurnOnRange(lo, loIndex, hi, hiIndex);
                }
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
                var lowerBound = 0;
                var upperBound = indices.Count - 1;
                for (int j = 0; j < otherLength; j += 2)
                {
                    var lo = otherIndices[j];
                    var hi = otherIndices[j + 1];
                    var loIndex = BinarySearch(indices, upperBound, lo, out _, lowerBound);
                    lowerBound = loIndex;
                    var hiIndex = BinarySearch(indices, upperBound, hi, out _, lowerBound);
                    lowerBound = hiIndex;
                    TurnOnRange(lo, loIndex, hi, hiIndex);
                }
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
                var lowerBound = 0;
                var upperBound = indices.Count - 1;
                for (int j = 0; j < otherLength; j += 2)
                {
                    var lo = otherIndices[j];
                    var hi = otherIndices[j + 1];
                    var loIndex = BinarySearch(indices, upperBound, lo, out _, lowerBound);
                    lowerBound = loIndex;
                    var hiIndex = BinarySearch(indices, upperBound, hi, out _, lowerBound);
                    lowerBound = hiIndex;
                    TurnOffRange(lo, loIndex, hi, hiIndex);
                }
            }
        }
        public void TurnOnRange(ushort lo, ushort hi)
        {
            if (lo == hi)
            {
                TurnOn(lo);
                return;
            }
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(lo);
                indices.Add(hi);
                return;
            }
            var loIndex = BinarySearch(indices, count, lo, out _);
            var hiIndex = BinarySearch(indices, count, hi, out _);
            TurnOnRange(lo, loIndex, hi, hiIndex);
        }

        private void TurnOnRange(ushort lo, int loIndex, ushort hi, int hiIndex)
        {
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            if ((hiIndex & 0b1) == 0) indices.Insert(loIndex, hi);
            if ((loIndex & 0b1) == 0)
                indices.Insert(loIndex, lo);
        }

        public void TurnOffRange(ushort lo, ushort hi)
        {
            var count = indices.Count;
            if (count == 0) return;
            if (lo == hi)
            {
                TurnOff(lo);
                return;
            }
            var loIndex = BinarySearch(indices, count, lo--, out _);
            var hiIndex = BinarySearch(indices, count, hi++, out _);
            TurnOffRange(lo, loIndex, hi, hiIndex);
        }

        private void TurnOffRange(ushort lo, int loIndex, ushort hi, int hiIndex)
        {
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            if ((hiIndex & 0b1) == 0) indices.Insert(loIndex, hi);
            if ((loIndex & 0b1) != 0)
                indices.Insert(loIndex, lo);
        }
        public void IntersectRange(ushort lo, ushort hi)
        {
            if (lo == hi)
            {
                TurnOn(lo);
                return;
            }
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(lo);
                indices.Add(hi);
                return;
            }
            var loIndex = BinarySearch(indices, count, lo, out _);
            var hiIndex = BinarySearch(indices, count, hi, out _);
            IntersectRange(lo, loIndex, hi, hiIndex);
        }

        private void IntersectRange(ushort lo, int loIndex, ushort hi, int hiIndex)
        {
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            if ((hiIndex & 0b1) == 0) indices.Insert(loIndex, hi);
            if ((loIndex & 0b1) == 0) indices.Insert(loIndex, lo);
        }
    }
}
