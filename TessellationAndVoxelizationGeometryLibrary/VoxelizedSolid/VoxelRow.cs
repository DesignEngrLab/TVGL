using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL.Voxelization
{
    internal interface IVoxelRow
    {
        bool this[int index] { get; set; }
        int Count { get; }
        void TurnOnRange(int lo, int hi);
        void TurnOffRange(int lo, int hi);
        (bool, bool) GetNeighbors(int index);
        void Union(IVoxelRow[] others, int offset = 0);
        void Intersect(IVoxelRow[] others, int offset = 0);
        void Subtract(IVoxelRow[] subtrahends, int offset = 0);
    }
    internal struct VoxelRowSparse : IVoxelRow
    {
        internal readonly List<ushort> indices;

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

        internal VoxelRowSparse(bool constructIndices = false)
        {
            indices = new List<ushort>();
        }
        internal VoxelRowSparse(VoxelRowDense dense)
        {
            indices = new List<ushort>();
            var lastVal = false;
            int i;
            for (i = 0; i < 8 * dense.values.Length; i++)
            {
                var currentVal = dense[i];
                if (currentVal == lastVal) continue;
                lastVal = currentVal;
                if (currentVal) //then just started a new row of on voxels
                    indices.Add((ushort)i);
                else //then just started a new row of off voxels.
                    //note that since the limits are inclusive, we record the previous one
                    indices.Add((ushort)(i - 1));
            }
            if (dense[i]) indices.Add((ushort)i);
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
            var i = BinarySearch(indices, count, index);
            if (i >= 0) //then index is a value in this list - either a lower or upper range
                return true;
            else return (~i & 0b1) != 0;
            //{
            //    i = ~i;
            //    if ((i & 0b1) == 0) //even means top of range in this case - and it's less so index is outside, but neighbors could be inside
            //        return (index - 1 <= indices[i - 1], index + 1 >= indices[i]);
            //    else //odd so comfortably in a range. there is only one result
            //        return (true, true);
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
            var i = BinarySearch(indices, count, index);
            if (i >= 0) //then index is a value in this list - either a lower or upper range
            {
                if ((i & 0b1) != 0) //odd means top of range
                    return (index - 1 >= indices[i - 1], false);
                else //even bottom of the range
                    return (false, index + 1 <= indices[i + 1]);
            }
            else
            {
                i = ~i;
                if ((i & 0b1) == 0) //even means top of range in this case - and it's less so index is outside, but neighbors could be inside
                    return (index - 1 <= indices[i - 1], index + 1 >= indices[i]);
                else //odd so comfortably in a range. there is only one result
                    return (true, true);
            }
        }
        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int BinarySearch(IList<ushort> array, int length, double value)
        {
            var lo = 0;
            var hi = length - 1;
            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                var c = array[i];
                if (c == value) return i;
                if (c < value) lo = i + 1;
                else hi = i - 1;
            }
            return ~lo;
        }
        void TurnOn(ushort value)
        {
            var lo = 0;
            var hi = indices.Count - 1;
            while (lo < hi)
            {
                if (value < indices[lo])
                {
                    if (value + 1 == indices[lo])
                    {
                        if (indices[lo - 1] + 1 == value) //then fill the hole by deleting adjacent ub and lo
                        {
                            indices.RemoveAt(lo - 1);
                            indices.RemoveAt(lo - 1);
                        }
                        else indices[lo]--;
                    }
                    else if (indices[lo - 1] + 1 == value)
                        indices[lo - 1]++;
                    else
                    {
                        indices.Insert(lo - 1, value);
                        indices.Insert(lo - 1, value);
                    }
                    return;
                }
                if (value > indices[hi])
                {
                    if (value - 1 == indices[hi])
                    {
                        if (indices[hi + 1] + 1 == value) //then fill the hole by deleting adjacent ub and lo
                        {
                            indices.RemoveAt(hi);
                            indices.RemoveAt(hi);
                        }
                        else indices[hi]++;
                    }
                    else if (indices[hi + 1] - 1 == value)
                        indices[hi + 1]--;
                    else
                    {
                        indices.Insert(hi + 1, value);
                        indices.Insert(hi + 1, value);
                    }
                    return;
                }
                var mid = lo + ((hi - lo) >> 1);
                if (value == indices[mid]) return; //that was lucky! the new lo is already on
                else if (value < indices[mid])
                    hi = mid - 1;
                else lo = mid;
            }
            return;
        }

        void TurnOff(ushort value)
        {
            var lo = 0;
            var hi = indices.Count - 1;
            var insideRange = false;
            while (lo < hi)
            {
                if (value < indices[lo] || value > indices[hi]) return; //already off
                if (value <= indices[lo + 1])
                {
                    hi = lo + 1;
                    insideRange = true;
                }
                else if (value >= indices[hi - 1])
                {
                    lo = hi - 1;
                    insideRange = true;
                }
                if (value == indices[lo])
                {
                    if (value == indices[lo + 1])
                    {
                        indices.RemoveAt(lo);
                        indices.RemoveAt(lo);
                    }
                    else indices[lo]++;
                    return;
                }
                if (value == indices[hi])
                {
                    if (value == indices[hi - 1])
                    {
                        indices.RemoveAt(hi - 1);
                        indices.RemoveAt(hi - 1);
                    }
                    else indices[hi]--;
                    return;
                }
                if (insideRange)
                {
                    indices.Insert(hi, value);
                    indices.Insert(hi, value);
                }
                else
                {
                    var mid = lo + ((hi - lo) >> 1);
                    if (value < indices[mid])
                        hi = mid - 1;
                    else lo = mid;
                }
            }
        }

        public void Union(IVoxelRow[] others, int offset = 0)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowSparse)
                {
                    var otherIndices = ((VoxelRowSparse)item).indices;
                    var thisWriteIndex = -1;
                    var thisReadIndex = 0;
                    var thisOtherIndex = 0;
                    var max = -1;
                    while (thisReadIndex < indices.Count || thisOtherIndex < otherIndices.Count)
                    {
                        if (thisReadIndex == indices.Count) thisReadIndex -= 2;
                        var thisLo = indices[thisReadIndex];
                        var thisHi = indices[thisReadIndex + 1];
                        if (thisOtherIndex == otherIndices.Count) thisOtherIndex -= 2;
                        var otherLo = (ushort)(otherIndices[thisOtherIndex] + offset);
                        var otherHi = (ushort)(otherIndices[thisOtherIndex + 1] + offset);
                        if (thisLo > max && thisLo <= otherLo)
                        {
                            max = indices[++thisWriteIndex] = thisLo;
                            continue;
                        }
                        if (otherLo > max && otherLo < thisLo)
                        {
                            max = indices[++thisWriteIndex] = otherLo;
                            continue;
                        }
                        if (thisHi > max && thisHi >= otherHi)
                            max = indices[thisWriteIndex] = thisHi;
                        else if (otherHi > max && otherHi > thisHi)
                            max = indices[thisWriteIndex] = otherHi;
                        thisReadIndex += 2;
                        thisOtherIndex += 2;
                    }
                    while (indices.Count > thisWriteIndex + 1) indices.RemoveAt(thisWriteIndex + 1);
                }
            }
        }


        public void Intersect(IVoxelRow[] others, int offset = 0)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowSparse)
                {
                    var otherIndices = ((VoxelRowSparse)item).indices;
                    var thisWriteIndex = -1;
                    var thisReadIndex = 0;
                    var thisOtherIndex = 0;
                    var max = -1;
                    while (thisReadIndex < indices.Count || thisOtherIndex < otherIndices.Count)
                    {
                        if (thisReadIndex == indices.Count) thisReadIndex -= 2;
                        var thisLo = indices[thisReadIndex];
                        var thisHi = indices[thisReadIndex + 1];
                        if (thisOtherIndex == otherIndices.Count) thisOtherIndex -= 2;
                        var otherLo = (ushort)(otherIndices[thisOtherIndex] + offset);
                        var otherHi = (ushort)(otherIndices[thisOtherIndex + 1] + offset);
                        if (thisLo > max && thisLo >= otherLo)
                        {
                            max = indices[++thisWriteIndex] = thisLo;
                            continue;
                        }
                        if (otherLo > max && otherLo > thisLo)
                        {
                            max = indices[++thisWriteIndex] = otherLo;
                            continue;
                        }
                        if (thisHi > max && thisHi <= otherHi)
                            max = indices[thisWriteIndex] = thisHi;
                        else if (otherHi > max && otherHi < thisHi)
                            max = indices[thisWriteIndex] = otherHi;
                        thisReadIndex += 2;
                        thisOtherIndex += 2;
                    }
                    while (indices.Count > thisWriteIndex + 1) indices.RemoveAt(thisWriteIndex + 1);
                }
            }
        }

        public void Subtract(IVoxelRow[] subtrahends, int offset = 0)
        {
            foreach (var subtrahend in subtrahends)
            {
                if (subtrahend is VoxelRowSparse)
                {
                    var subtrahendIndices = ((VoxelRowSparse)subtrahend).indices;
                    var thisWriteIndex = -1;
                    var thisReadIndex = 0;
                    var thisOtherIndex = 0;
                    var max = -1;
                    while (thisReadIndex < indices.Count || thisOtherIndex < subtrahendIndices.Count)
                    {
                        if (thisReadIndex == indices.Count) thisReadIndex -= 2;
                        var thisLo = indices[thisReadIndex];
                        var thisHi = indices[thisReadIndex + 1];
                        if (thisOtherIndex == subtrahendIndices.Count) thisOtherIndex -= 2;
                        var otherLo = (ushort)(subtrahendIndices[thisOtherIndex] + offset);
                        var otherHi = (ushort)(subtrahendIndices[thisOtherIndex + 1] + offset);
                        if (thisLo > max && thisLo < otherLo)
                        {
                            max = indices[++thisWriteIndex] = thisLo;
                            continue;
                        }
                        if (thisHi > max && thisHi < otherLo)
                            max = indices[thisWriteIndex] = thisHi;
                        else if (otherLo > max && otherHi < thisHi)
                            max = indices[thisWriteIndex] = otherHi;
                        thisReadIndex += 2;
                        thisOtherIndex += 2;
                    }
                    while (indices.Count > thisWriteIndex + 1) indices.RemoveAt(thisWriteIndex + 1);
                }
            }
        }

        public void TurnOnRange(int lo, int hi)
        {
            throw new NotImplementedException();
        }
        public void TurnOffRange(int lo, int hi)
        {
            throw new NotImplementedException();
        }
    }
    internal struct VoxelRowDense : IVoxelRow
    {
        internal readonly byte[] values;

        internal VoxelRowDense(ushort numBytes)
        {
            values = new byte[numBytes];
        }
        internal VoxelRowDense(VoxelRowSparse sparse, ushort numBytes) : this(numBytes)
        {
            if (sparse.indices.Any())
                for (int i = 0; i < sparse.indices.Count; i += 2)
                    TurnOnRange(sparse.indices[i], sparse.indices[i + 1]);
        }

        public bool this[int index]
        {
            get => GetValue(index);
            set { if (value) TurnOn(index); else TurnOff(index); }
        }

        bool GetValue(int xCoord)
        { return GetValue(xCoord >> 3, xCoord & 7); }
        bool GetValue(int byteCoord, int bitPosition)
        {
            return (byte)(byteCoord << bitPosition) >> 7 != 0;
            // this previous line looks hacky but it is faster than the following conditional
            // i guess the reason is that simplicity of execution even though shifting would 
            // seem to do more constructing.
            //if (bitPosition == 0) return (b & 0b1) != 0;
            //else if (bitPosition == 1) return (b & 0b01) != 0;
            //else if (bitPosition == 2) return (b & 0b001) != 0;
            //else if (bitPosition == 3) return (b & 0b0001) != 0;
            //else if (bitPosition == 4) return (b & 0b00001) != 0;
            //else if (bitPosition == 5) return (b & 0b000001) != 0;
            //else if (bitPosition == 6) return (b & 0b0000001) != 0;
            //return (b & 0b00000001) != 0;
        }
        public (bool, bool) GetNeighbors(int index)
        {
            var bytePos = index >> 3;
            var bitPos = index & 7;
            var lowerNeighbor = (bitPos == 0)
                 ? (bytePos > 0)
                     ? GetValue(bytePos - 1, 7) : false
                 : GetValue(bytePos, bitPos - 1);

            var upperNeighbor = (bitPos == 7)
                ? (bytePos < values.Length - 1)
                    ? GetValue(bytePos + 1, 0) : false
                : GetValue(bytePos, bitPos + 1);
            return (lowerNeighbor, upperNeighbor);
        }
        void TurnOn(int xCoord)
        { TurnOn(xCoord >> 3, xCoord & 7); }
        void TurnOn(int byteCoord, int bitPosition)
        {
            values[byteCoord] |= (byte)(0b1 << bitPosition);
        }
        void TurnOff(int xCoord)
        { TurnOff(xCoord >> 3, xCoord & 7); }
        void TurnOff(int byteCoord, int bitPosition)
        {
            if (bitPosition == 0) values[byteCoord] &= 0b11111110;
            else if (bitPosition == 1) values[byteCoord] &= 0b11111101;
            else if (bitPosition == 2) values[byteCoord] &= 0b11111011;
            else if (bitPosition == 3) values[byteCoord] &= 0b11110111;
            else if (bitPosition == 4) values[byteCoord] &= 0b11101111;
            else if (bitPosition == 5) values[byteCoord] &= 0b11011111;
            else if (bitPosition == 6) values[byteCoord] &= 0b10111111;
            else values[byteCoord] &= 0b01111111;
        }
        public int Count
        {
            get
            {
                var num = 0;
                foreach (var b in values)
                {
                    if (b == 0) continue;
                    if ((b & 1) > 0) num++;
                    if ((b & 2) > 0) num++;
                    if ((b & 4) > 0) num++;
                    if ((b & 8) > 0) num++;
                    if ((b & 16) > 0) num++;
                    if ((b & 32) > 0) num++;
                    if ((b & 64) > 0) num++;
                    if (b > 127) num++;
                }
                return num;
            }
        }

        public void TurnOnRange(int lo, int hi)
        {
            var xByte = lo >> 3;
            var xByteEnd = hi >> 3;
            var bitPostion = lo & 7;
            switch (bitPostion)
            {
                case 0: values[xByte] |= 0b11111111; break;
                case 1: values[xByte] |= 0b01111111; break;
                case 2: values[xByte] |= 0b00111111; break;
                case 3: values[xByte] |= 0b00011111; break;
                case 4: values[xByte] |= 0b00001111; break;
                case 5: values[xByte] |= 0b00000111; break;
                case 6: values[xByte] |= 0b00000011; break;
                default: values[xByte] |= 0b00000001; break;
            }
            while (++xByte < xByteEnd)
                values[xByte] = 0b11111111;
            bitPostion = hi & 7;
            switch (bitPostion)
            {
                case 1: values[xByte] |= 0b10000000; break;
                case 2: values[xByte] |= 0b11000000; break;
                case 3: values[xByte] |= 0b11100000; break;
                case 4: values[xByte] |= 0b11110000; break;
                case 5: values[xByte] |= 0b11111000; break;
                case 6: values[xByte] |= 0b11111100; break;
                case 7: values[xByte] |= 0b11111110; break;
                default: break;
            }
        }
        public void TurnOffRange(int lo, int hi)
        {
            var xByte = lo >> 3;
            var xByteEnd = hi >> 3;
            var bitPostion = lo & 7;
            switch (bitPostion)
            {
                case 0: values[xByte] &= 0b00000000; break;
                case 1: values[xByte] &= 0b10000000; break;
                case 2: values[xByte] &= 0b11000000; break;
                case 3: values[xByte] &= 0b11100000; break;
                case 4: values[xByte] &= 0b11110000; break;
                case 5: values[xByte] &= 0b11111000; break;
                case 6: values[xByte] &= 0b11111100; break;
                default: values[xByte] &= 0b11111110; break;
            }
            while (++xByte < xByteEnd)
                values[xByte] = 0b00000000;
            bitPostion = hi & 7;
            switch (bitPostion)
            {
                case 1: values[xByte] &= 0b01111111; break;
                case 2: values[xByte] &= 0b00111111; break;
                case 3: values[xByte] &= 0b00011111; break;
                case 4: values[xByte] &= 0b00001111; break;
                case 5: values[xByte] &= 0b00000111; break;
                case 6: values[xByte] &= 0b00000011; break;
                case 7: values[xByte] &= 0b00000001; break;
                default: break;
            }
        }

        public void Intersect(IVoxelRow[] others, int offset)
        {
            throw new NotImplementedException();
        }

        public void Subtract(IVoxelRow[] subtrahends, int offset)
        {
            throw new NotImplementedException();
        }

        public void Union(IVoxelRow[] others, int offset)
        {
            var offsetBytePosition = offset >> 3;
            var offsetBitPosition = offset & 7;
            /*
            foreach (var item in others)
            {
                if (item is VoxelRowDense)
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (i < offsetBytePosition) continue;
                        var otherByte = (i - offsetBytePosition)
                            }
            }
            */
        }

    }
}
