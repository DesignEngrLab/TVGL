using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL.Voxelization
{
    public struct VoxelRowDense : IVoxelRow
    {
        internal readonly byte[] values;
        readonly int numBytes;
        internal VoxelRowDense(ushort numBytes)
        {
            this.numBytes = numBytes;
            values = new byte[numBytes];
        }
        public VoxelRowDense(IVoxelRow row, ushort numBytes) : this(numBytes)
        {
            if (row is VoxelRowSparse)
            {
                var sparse = (VoxelRowSparse)row;
                if (sparse.indices.Any())
                    for (int i = 0; i < sparse.indices.Count; i += 2)
                        TurnOnRange(sparse.indices[i], sparse.indices[i + 1]);
            }
            else
            {
                values = (byte[])((VoxelRowDense)row).values.Clone();
            }
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
                ? (bytePos < numBytes - 1)
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

        public void TurnOnRange(ushort lo, ushort hi)
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
        public void TurnOffRange(ushort lo, ushort hi)
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
            if (offset != 0) throw new ArgumentException("Intersect of Dense Voxels currently" +
                  " does not support an offset.");
            else Intersect(others);
        }
        public void Intersect(IVoxelRow[] others)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowDense)
                {
                    var otherValues = ((VoxelRowDense)item).values;
                    for (int i = 0; i < numBytes; i++)
                        values[i] &= otherValues[i];
                }
                else //item is VoxelRowSparse
                {
                    var indices = ((VoxelRowSparse)item).indices;
                    if (indices.Any())
                        for (int i = 0; i < indices.Count; i += 2)
                            IntersectRange(indices[i], indices[i + 1]);
                }
            }
        }
       public void IntersectRange(ushort lo, ushort hi)
        {
            var xByte = lo >> 3;
            var xByteEnd = hi >> 3;
            var bitPostion = lo & 7;
            switch (bitPostion)
            {
                case 0: values[xByte] &= 0b11111111; break;
                case 1: values[xByte] &= 0b01111111; break;
                case 2: values[xByte] &= 0b00111111; break;
                case 3: values[xByte] &= 0b00011111; break;
                case 4: values[xByte] &= 0b00001111; break;
                case 5: values[xByte] &= 0b00000111; break;
                case 6: values[xByte] &= 0b00000011; break;
                default: values[xByte] &= 0b00000001; break;
            }
            while (++xByte < xByteEnd)
                values[xByte] = 0b11111111;
            bitPostion = hi & 7;
            switch (bitPostion)
            {
                case 1: values[xByte] &= 0b10000000; break;
                case 2: values[xByte] &= 0b11000000; break;
                case 3: values[xByte] &= 0b11100000; break;
                case 4: values[xByte] &= 0b11110000; break;
                case 5: values[xByte] &= 0b11111000; break;
                case 6: values[xByte] &= 0b11111100; break;
                case 7: values[xByte] &= 0b11111110; break;
                default: break;
            }
        }
        public void Subtract(IVoxelRow[] subtrahends, int offset)
        {
            if (offset != 0) throw new ArgumentException("Subtract of Dense Voxels currently" +
                  " does not support an offset.");
            else Union(subtrahends);
        }
        public void Subtract(IVoxelRow[] subtrahends)
        {
            foreach (var item in subtrahends)
            {
                if (item is VoxelRowDense)
                {
                    var otherValues = ((VoxelRowDense)item).values;
                    for (int i = 0; i < numBytes; i++)
                        values[i] &= (byte)~otherValues[i];
                }
                else //item is VoxelRowSparse
                {
                    var indices = ((VoxelRowSparse)item).indices;
                    if (indices.Any())
                        for (int i = 0; i < indices.Count; i += 2)
                            TurnOffRange(indices[i], indices[i + 1]);
                }
            }
        }

        public void Union(IVoxelRow[] others, int offset)
        {
            if (offset != 0) throw new ArgumentException("Union of Dense Voxels currently" +
                  " does not support an offset.");
            else Union(others);
        }
        public void Union(IVoxelRow[] others)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowDense)
                {
                    var otherValues = ((VoxelRowDense)item).values;
                    for (int i = 0; i < numBytes; i++)
                        values[i] |= otherValues[i];
                }
                else //item is VoxelRowSparse
                {
                    var indices = ((VoxelRowSparse)item).indices;
                    if (indices.Any())
                        for (int i = 0; i < indices.Count; i += 2)
                            TurnOnRange(indices[i], indices[i + 1]);
                }
            }
        }
    }
}
