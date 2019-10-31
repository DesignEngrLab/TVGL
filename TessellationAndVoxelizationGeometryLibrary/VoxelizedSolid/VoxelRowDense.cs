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
            return (values[byteCoord] & (0b1 << bitPosition)) != 0;
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
                    if ((b & 1) != 0) num++;
                    if ((b & 2) != 0) num++;
                    if ((b & 4) != 0) num++;
                    if ((b & 8) != 0) num++;
                    if ((b & 16) != 0) num++;
                    if ((b & 32) != 0) num++;
                    if ((b & 64) != 0) num++;
                    if (b > 127) num++;
                }
                return num;
            }
        }

        public void TurnOnRange(ushort lo, ushort hi)
        {
            var startByte = lo >> 3;
            var endByte = (hi - 1) >> 3;
            var startBitPostion = lo & 7;
            var endBitPostion = 7 - ((hi - 1) & 7);
            byte mask = 0b11111111;
            byte loMask = (byte)(mask >> startBitPostion);
            byte hiMask = (byte)(mask << endBitPostion);
            if (startByte == endByte)
            {
                mask = (byte)(loMask & hiMask);
                values[startByte] |= mask;
                return;
            }
            values[startByte] |= loMask;
            while (++startByte < endByte)
                values[startByte] = 0b11111111;
            values[startByte] |= hiMask;
        }

        public void TurnOffRange(ushort lo, ushort hi)
        {
            var startByte = lo >> 3;
            var endByte = (hi - 1) >> 3;
            var startBitPostion = lo & 7;
            var endBitPostion = 7 - ((hi - 1) & 7);
            byte mask = 0b11111111;
            byte loMask = (byte)(~(byte)(mask >> startBitPostion));
            byte hiMask = (byte)(~(byte)(mask << endBitPostion));
            if (startByte == endByte)
            {
                mask = (byte)(loMask & hiMask);
                values[startBitPostion] &= mask;
                return;
            }
            values[startByte] &= loMask;
            while (++startByte < endByte)
                values[startByte] = 0b11111111;
            values[startByte] &= hiMask;
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
                            ;//this doesn't work  IntersectRange(indices[i], indices[i + 1]);
                }
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
