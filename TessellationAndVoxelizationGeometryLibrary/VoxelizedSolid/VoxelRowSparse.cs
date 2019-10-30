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
        internal VoxelRowSparse(IVoxelRow row)
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
                        var currentVal = (currentByte & 0b1) != 0;
                        if (currentVal == lastVal) continue;
                        lastVal = currentVal;
                        if (currentVal) //then just started a new row of on voxels
                            indices.Add(i);
                        else //then just started a new row of off voxels.
                             //note that since the limits are inclusive, we record the previous one
                            indices.Add((ushort)(i - 1));
                        currentByte >>= 1;
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
        private static int BinarySearch(IList<ushort> array, int length, double value, out bool valueExists)
        {
            valueExists = true;
            var lo = 0;
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
                if (!otherIndices.Any()) continue;
                var thisIndices = new List<ushort>(indices);
                var thisLength = thisIndices.Count;
                indices.Clear();
                var thisIndex = 0;
                var otherIndex = 0;
                bool thisIsLower = false;
                var otherIsLower = false;
                var updateThis = true;
                var updateOther = true;
                var thisEnded = false;
                var otherEnded = false;
                int lastAdded = 0;
                ushort otherValue = 0;
                ushort thisValue = 0;
                while (!thisEnded || !otherEnded)
                {
                    if (!thisEnded && (updateThis || thisValue < lastAdded))
                    {
                        thisIndex++;
                        if (thisIndex == thisLength)
                        {
                            thisEnded = true;
                            thisIndex--;
                        }
                        else thisValue = thisIndices[thisIndex];
                        thisIsLower = !thisIsLower;
                        updateThis = false;
                    }
                    else if (!otherEnded && (updateOther || otherValue < lastAdded))
                    {
                        otherIndex++;
                        if (otherIndex == otherLength)
                        {
                            otherEnded = true;
                            otherIndex--;
                        }
                        else otherValue = (ushort)(otherIndices[otherIndex] + offset);
                        updateOther = false;
                        otherIsLower = !otherIsLower;
                    }
                    else if (thisIsLower && otherIsLower)
                    {   // if both lists are at lower range values then simply take the lower
                        // being careful that this could extend the range
                        if (thisValue <= otherValue)
                        {
                            if (thisValue == lastAdded && lastAdded > 0)
                                indices.RemoveAt(indices.Count - 1);
                            else
                            {
                                indices.Add(thisValue);
                                lastAdded = thisValue;
                            }
                            updateThis = true;
                        }
                        else
                        {
                            if (otherValue == lastAdded && lastAdded > 0)
                                indices.RemoveAt(indices.Count - 1);
                            else
                            {
                                indices.Add(otherValue);
                                lastAdded = otherValue;
                            }
                            updateOther = true;
                        }
                    }
                    else if (!thisIsLower && otherIsLower)
                    {
                        if (thisValue < otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                            updateThis = true;
                        }
                        else updateOther = true; //the other value is less than, so it is ignored. move to next useful other value
                    }
                    else if (thisIsLower && !otherIsLower)
                    {
                        if (otherValue < thisValue)
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                            otherIndex++;
                            otherIsLower = true;
                        }
                        else updateThis = true;
                    }
                    else //then both are upper
                    {
                        if (thisValue >= otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                            updateThis = true;
                        }
                        else
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                            updateOther = true;
                        }
                    }
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
                if (!otherIndices.Any()) continue;
                var thisIndices = new List<ushort>(indices);
                var thisLength = thisIndices.Count;
                indices.Clear();
                var thisIndex = 0;
                var otherIndex = 0;
                var thisIsLower = true;
                var otherIsLower = true;
                int lastAdded = -1;
                while (thisIndex < thisLength && otherIndex < otherLength)
                {
                    var thisValue = indices[thisIndex];
                    var otherValue = (ushort)(otherIndices[otherIndex] + offset);
                    if (thisValue < lastAdded)
                    {
                        thisIndex++;
                        thisIsLower = !thisIsLower;
                    }
                    else if (otherValue < lastAdded)
                    {
                        otherIndex++;
                        otherIsLower = !otherIsLower;
                    }
                    else if (thisIsLower && otherIsLower)
                    {
                        if (thisValue >= otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                        }
                        else
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                        }
                    }
                    else if (!thisIsLower && otherIsLower)
                    {
                        if (thisValue >= otherValue)
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                        }
                    }
                    else if (thisIsLower && !otherIsLower)
                    {
                        if (otherValue >= thisValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                        }
                    }
                    else //then both are upper
                    {
                        if (thisValue <= otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                            thisIndex++;
                            thisIsLower = true;
                        }
                        else
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                            otherIndex++;
                            otherIsLower = true;
                        }
                    }
                }
            }
        }
        public void Subtract(IVoxelRow[] subtrahends, int offset = 0)
        {
            for (int i = 0; i < subtrahends.Length; i++)
            {
                IVoxelRow other = subtrahends[i];
                if (other is VoxelRowDense) other = new VoxelRowSparse(other);
                var otherIndices = ((VoxelRowSparse)other).indices;
                var otherLength = otherIndices.Count;
                if (!otherIndices.Any()) continue;
                var thisIndices = new List<ushort>(indices);
                var thisLength = thisIndices.Count;
                indices.Clear();
                var thisIndex = 0;
                var otherIndex = 0;
                var thisIsLower = true;
                var otherIsLower = true;
                int lastAdded = -1;
                while (thisIndex < thisLength && otherIndex < otherLength)
                {
                    var thisValue = indices[thisIndex];
                    var otherValue = (ushort)(otherIndices[otherIndex] + offset);
                    if (thisValue < lastAdded)
                    {
                        thisIndex++;
                        thisIsLower = !thisIsLower;
                    }
                    else if (otherValue < lastAdded)
                    {
                        otherIndex++;
                        otherIsLower = !otherIsLower;
                    }
                    else if (thisIsLower && otherIsLower)
                    {
                        if (thisValue >= otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                        }
                        else
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                        }
                    }
                    else if (!thisIsLower && otherIsLower)
                    {
                        if (thisValue >= otherValue)
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                        }
                    }
                    else if (thisIsLower && !otherIsLower)
                    {
                        if (otherValue >= thisValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                        }
                    }
                    else //then both are upper
                    {
                        if (thisValue <= otherValue)
                        {
                            indices.Add(thisValue);
                            lastAdded = thisValue;
                            thisIndex++;
                            thisIsLower = true;
                        }
                        else
                        {
                            indices.Add(otherValue);
                            lastAdded = otherValue;
                            otherIndex++;
                            otherIsLower = true;
                        }
                    }
                }
            }
        }

        public void TurnOnRange(int lo, int hi)
        {
            var ulo = (ushort)lo;
            var uhi = (ushort)hi;
            if (ulo == uhi)
            {
                TurnOn(ulo);
                return;
            }
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a a lone lower and upper range.
                indices.Add(ulo);
                indices.Add(uhi);
                return;
            }
            var loIndex = BinarySearch(indices, count, lo, out var loExists);
            var hiIndex = BinarySearch(indices, count, hi, out var hiExists);
            if (loIndex == hiIndex)
            {
                indices.Insert(loIndex, uhi);
                indices.Insert(loIndex, ulo);
                return;
            }
            for (int i = loIndex; i < hiIndex; i++)
                indices.RemoveAt(loIndex);
            /*** this is too hard to figure out right now and it's not being used. So, 
             * I'm stopping until a use case arrives.
            if (loExists && (i & 0b1) == 0) //even means bottom of range and it's less 
            {
                if 
            }
            */

        }
        public void TurnOffRange(int lo, int hi)
        {
            throw new NotImplementedException();
        }
    }
}
