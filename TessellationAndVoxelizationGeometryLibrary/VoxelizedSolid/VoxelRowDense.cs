// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelRowDense.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// VoxelRowDense represents the dense array of bits for this line of voxels
    /// </summary>
    /// <seealso cref="TVGL.Voxelization.IVoxelRow" />
    internal readonly struct VoxelRowDense : IVoxelRow
    {
        /// <summary>
        /// The values is the byte array where each bit corresponds to whether or not
        /// a voxel is on or off
        /// </summary>
        internal readonly byte[] values;
        /// <summary>
        /// The length of the row. This is the same as the number of voxels in x (numVoxelsX)
        /// for the participating solid.
        /// </summary>
        /// <value>The maximum number of voxels.</value>
        public ushort maxNumberOfVoxels { get; }
        /// <summary>
        /// The number bytes
        /// </summary>
        readonly int numBytes;
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelRowDense" /> struct.
        /// </summary>
        /// <param name="length">The length.</param>
        internal VoxelRowDense(int length)
        {
            maxNumberOfVoxels = (ushort)length;
            numBytes = length >> 3;
            if ((length & 7) != 0) numBytes++;
            values = new byte[numBytes];
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelRowDense" /> struct.
        /// This is typically used to copy an existing dense row, or convert from
        /// a sparse row.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="length">The length.</param>
        internal VoxelRowDense(IVoxelRow row, int length) : this(length)
        {
            if (row is VoxelRowSparse sparse)
            {
                if (sparse.indices.Any())
                    for (int i = 0; i < sparse.indices.Count; i += 2)
                        TurnOnRange(sparse.indices[i], sparse.indices[i + 1]);
            }
            else
            {
                values = (byte[])((VoxelRowDense)row).values.Clone();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified xCoord.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int xCoord]
        {
            get => GetValue(xCoord >> 3, xCoord & 7);
            set
            {
                if (value) TurnOn(xCoord >> 3, xCoord & 7);
                else TurnOff(xCoord >> 3, xCoord & 7);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="byteCoord">The byte coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool GetValue(int byteCoord, int bitPosition)
        {
            var shift = 7 - bitPosition;
            return (values[byteCoord] & (0b1 << shift)) != 0;
        }
        /// <summary>
        /// Gets the lower-x neighbor and the upper-x neighbor for the one at xCoord.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <returns>System.ValueTuple&lt;System.Boolean, System.Boolean&gt;.</returns>
        public (bool, bool) GetNeighbors(int xCoord)
        {
            var bytePos = xCoord >> 3;
            var bitPos = xCoord & 7;
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


        /// <summary>
        /// Turns the on.
        /// </summary>
        /// <param name="byteCoord">The byte coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        void TurnOn(int byteCoord, int bitPosition)
        {
            var shift = 7 - bitPosition;
            values[byteCoord] |= (byte)(0b1 << shift);
        }

        /// <summary>
        /// Turns the off.
        /// </summary>
        /// <param name="byteCoord">The byte coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        void TurnOff(int byteCoord, int bitPosition)
        {
            if (bitPosition == 7) values[byteCoord] &= 0b11111110;
            else if (bitPosition == 6) values[byteCoord] &= 0b11111101;
            else if (bitPosition == 5) values[byteCoord] &= 0b11111011;
            else if (bitPosition == 4) values[byteCoord] &= 0b11110111;
            else if (bitPosition == 3) values[byteCoord] &= 0b11101111;
            else if (bitPosition == 2) values[byteCoord] &= 0b11011111;
            else if (bitPosition == 1) values[byteCoord] &= 0b10111111;
            else values[byteCoord] &= 0b01111111;
        }
        /// <summary>
        /// Gets the number of voxels in this row.
        /// </summary>
        /// <value>The count.</value>
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

        /// <summary>
        /// Turns all the voxels within the range to on/true.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
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

        /// <summary>
        /// Turns all the voxels within the range to off/false.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
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

        /// <summary>
        /// Intersects the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="System.ArgumentException">Intersect of Dense Voxels currently" +
        ///                   " does not support an offset.</exception>
        public void Intersect(IVoxelRow[] others, int offset)
        {
            if (offset != 0) throw new ArgumentException("Intersect of Dense Voxels currently" +
                  " does not support an offset.");
            else Intersect(others);
        }
        /// <summary>
        /// Intersects the specified others.
        /// </summary>
        /// <param name="others">The others.</param>
        internal void Intersect(IVoxelRow[] others)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowDense dense)
                {
                    var otherValues = dense.values;
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

        /// <summary>
        /// Subtracts the specified subtrahend rows from this row.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="System.ArgumentException">Subtract of Dense Voxels currently" +
        ///                   " does not support an offset.</exception>
        public void Subtract(IVoxelRow[] subtrahends, int offset)
        {
            if (offset != 0) throw new ArgumentException("Subtract of Dense Voxels currently" +
                  " does not support an offset.");
            else Union(subtrahends);
        }
        /// <summary>
        /// Subtracts the specified subtrahends.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        internal void Subtract(IVoxelRow[] subtrahends)
        {
            foreach (var item in subtrahends)
            {
                if (item is VoxelRowDense dense)
                {
                    var otherValues = dense.values;
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

        /// <summary>
        /// Unions the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        /// <exception cref="System.ArgumentException">Union of Dense Voxels currently" +
        ///                   " does not support an offset.</exception>
        public void Union(IVoxelRow[] others, int offset)
        {
            if (offset != 0) throw new ArgumentException("Union of Dense Voxels currently" +
                  " does not support an offset.");
            else Union(others);
        }
        /// <summary>
        /// Unions the specified others.
        /// </summary>
        /// <param name="others">The others.</param>
        internal void Union(IVoxelRow[] others)
        {
            foreach (var item in others)
            {
                if (item is VoxelRowDense dense)
                {
                    var otherValues = dense.values;
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

        /// <summary>
        /// Inverts this row - making all on voxels off and vice-versa.
        /// </summary>
        public void Invert()
        {
            for (int i = 0; i < numBytes; i++)
                values[i] = (byte)~values[i];
        }

        /// <summary>
        /// Clears this row of all on voxels.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < numBytes; i++)
                values[i] = 0b0;
        }

        /// <summary>
        /// Averages the positions of the on voxels. This is used in finding center of mass.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int TotalXPosition()
        {
            var xTotal = 0;
            var byteOffset = 0;
            foreach (var b in values)
            {
                if (b == 0) continue;
                if ((b & 1) != 0) xTotal += byteOffset + 7;
                if ((b & 2) != 0) xTotal += byteOffset + 6;
                if ((b & 4) != 0) xTotal += byteOffset + 5;
                if ((b & 8) != 0) xTotal += byteOffset + 4;
                if ((b & 16) != 0) xTotal += byteOffset + 3;
                if ((b & 32) != 0) xTotal += byteOffset + 2;
                if ((b & 64) != 0) xTotal += byteOffset + 1;
                if (b > 127) xTotal += byteOffset;
                byteOffset += 8;
            }
            return xTotal;
        }
    }
}
