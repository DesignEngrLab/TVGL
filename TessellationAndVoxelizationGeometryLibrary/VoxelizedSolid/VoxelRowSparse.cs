// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelRowSparse.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// VoxelRowSparse represents a sparse array of bits for this line of voxels
    /// </summary>
    /// <seealso cref="TVGL.Voxelization.IVoxelRow" />
    internal readonly struct VoxelRowSparse : IVoxelRow
    {
        /// <summary>
        /// The indices are pairs of ranges of on-voxels, where the lo value is the position
        /// of the first voxel in a row. These are always at the even positions in the List.
        /// The odd positions are the ends of each range and these values are excluded from
        /// the range. They are the first off-voxel.
        /// </summary>
        internal readonly List<ushort> indices;

        /// <summary>
        /// Gets the number of voxels in this row.
        /// </summary>
        /// <value>The count.</value>
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
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelRowSparse" /> struct.
        /// because "structs cannot contain explicit parameterless constructors",
        /// We are forced to add the dummy input.
        /// </summary>
        /// <param name="length">The length.</param>
        internal VoxelRowSparse(int length)
        {
            indices = new List<ushort>();
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int index]
        {
            get
            {
                //lock (indices)
                    return GetValue(index);
            }
            set
            {
                if (value)
                    //lock (indices)
                        TurnOn((ushort)index);
                else
                    //lock (indices)
                        TurnOff((ushort)index);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        private bool GetValue(int index)
        {
            var count = indices.Count;
            if (count == 0 || index < indices[0] || index > indices[count - 1]) return false;
            BinarySearch(indices, count, index, out var _, out var voxelIsOn);
            return voxelIsOn;
        }

        /// <summary>
        /// Gets the lower-x neighbor and the upper-x neighbor for the one at xCoord.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <returns>System.ValueTuple&lt;System.Boolean, System.Boolean&gt;.</returns>
        public (bool, bool) GetNeighbors(int xCoord, ushort upperLimit)
        {
            var count = indices.Count;
            if (count == 0) return (false, false);
            if (xCoord == 0)
                return (false, GetValue(xCoord + 1));
            if (xCoord == upperLimit - 1)
                return (GetValue(xCoord - 1), false);

            var i = BinarySearch(indices, count, xCoord, out var valueExists, out var voxelIsOn);
            if (voxelIsOn) //then index is a value in this list - either a lower or upper range
            {
                if (valueExists) //then must be at the beginning of the range and the previous voxel is off, but this could
                                 //a lone voxel, so need to check next.
                    return (false, xCoord + 1 < indices[i + 1]);
                else //the current is on and not the beginning of the range, but this next could be off
                    return (true, xCoord + 1 < indices[i]);
            }
            else //the current voxel is off
            {
                if (valueExists) //then current is end of range which means previous is on,
                    //current is off, but next could be start of new range
                    return (true, i + 1 < count && xCoord + 1 == indices[i + 1]);
                else  //but neighbors could be inside
                    return (false, i < count && xCoord + 1 == indices[i]);
            }
        }

        // This binary search is modified/simplified from Array.BinarySearch
        // (https://referencesource.microsoft.com/mscorlib/a.html#b92d187c91d4c9a9),
        // but because our lists are pairs of ranges a bunch of extra conditions have been added
        /// <summary>
        /// Binaries the search.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="length">The length.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueExists">if set to <c>true</c> [value exists].</param>
        /// <param name="voxelIsOn">if set to <c>true</c> [voxel is on].</param>
        /// <param name="lowerBound">The lower bound.</param>
        /// <returns>System.Int32.</returns>
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

        /// <summary>
        /// Turns the on.
        /// </summary>
        /// <param name="value">The value.</param>
        private void TurnOn(ushort value)
        {
            int index;
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
            if (valueExists)
            {
                if (index + 1 < count && value + 1 == indices[index + 1])
                {  //check to see if this new voxel perfectly fill a gap between two ranges
                    indices.RemoveAt(index);
                    indices.RemoveAt(index);
                }
                else indices[index]++; //otherwise increment the upper range of these on-voxels
            }
            else //in the range of off-voxels
            {
                if (index < count && value + 1 == indices[index])
                    //this was the last off-voxel in a range, so need to pull back the next on-voxel
                    //range to include this
                    indices[index]--;
                else
                {  //add this lone voxel in a range of off-voxels
                    indices.Insert(index, (ushort)(value + 1));
                    indices.Insert(index, value);
                }
            }
        }

        /// <summary>
        /// Turns the off.
        /// </summary>
        /// <param name="value">The value.</param>
        private void TurnOff(ushort value)
        {
            var count = indices.Count;
            if (count == 0)
            {
                return; //nothing to do. there are no voxels anyway
            }
            var index = BinarySearch(indices, count, value, out var valueExists, out var voxelIsOn);
            if (!voxelIsOn) return; //it's already off
            if (valueExists)
            {
                if (value + 1 == indices[index + 1])
                {  //check to see if this is deleting a lone voxel
                    indices.RemoveAt(index);
                    indices.RemoveAt(index);
                }
                else indices[index]++;
                //otherwise increase the lower bound of this range of on-voxels
                //(or, to say it another way, increase the upper bound of these off-voxels
            }
            else  // in the range of on-voxels
            {
                if (value + 1 == indices[index])
                    //this was the last on-voxel in a range, so need to decrement the top of the range
                    indices[index]--;
                else
                {   // make a one in the range of on-voxels
                    indices.Insert(index, (ushort)(value + 1));
                    indices.Insert(index, value);
                }
            }
        }

        /// <summary>
        /// Unions the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        public void Union(IVoxelRow[] others, int offset = 0)
        {
            for (int i = 0; i < others.Length; i++)
            {
                IVoxelRow other = others[i];
                if (other is VoxelRowDense) other = VoxelizedSolid.CopyToSparse(other);
                var otherIndices = ((VoxelRowSparse)other).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                    TurnOnRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
            }
        }

        /// <summary>
        /// Intersects the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        public void Intersect(IVoxelRow[] others, int offset = 0)
        {
            for (int i = 0; i < others.Length; i++)
            {
                IVoxelRow other = others[i];
                if (other is VoxelRowDense) other = VoxelizedSolid.CopyToSparse(other);
                var otherIndices = ((VoxelRowSparse)other).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                if (otherLength == 0) indices.Clear();
                else
                {
                    if (otherIndices[0] != 0)
                        TurnOffRange(0, otherIndices[0], ref indexLowerBound);
                    for (int j = 1; j < otherLength - 1; j += 2)
                        TurnOffRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
                    TurnOffRange(otherIndices[otherLength - 1], ushort.MaxValue, ref indexLowerBound);
                }
            }
        }

        /// <summary>
        /// Subtracts the specified subtrahend rows from this row.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <param name="offset">The offset.</param>
        public void Subtract(IVoxelRow[] subtrahends, int offset = 0)
        {
            for (int i = 0; i < subtrahends.Length; i++)
            {
                IVoxelRow subtrahend = subtrahends[i];
                if (subtrahend is VoxelRowDense) subtrahend = VoxelizedSolid.CopyToSparse(subtrahend);
                var otherIndices = ((VoxelRowSparse)subtrahend).indices;
                var otherLength = otherIndices.Count;
                var indexLowerBound = 0;
                for (int j = 0; j < otherLength; j += 2)
                    TurnOffRange(otherIndices[j], otherIndices[j + 1], ref indexLowerBound);
            }
        }

        /// <summary>
        /// Turns all the voxels within the range to on/true.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        public void TurnOnRange(ushort lo, ushort hi)
        {
            var dummy = 0;
            TurnOnRange(lo, hi, ref dummy);
        }

        /// <summary>
        /// Turns the on range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
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
            TurnOnRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn, ref indexLowerBound);
        }

        /// <summary>
        /// Turns the on range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="loIndex">Index of the lo.</param>
        /// <param name="loValueExists">if set to <c>true</c> [lo value exists].</param>
        /// <param name="loVoxelIsOn">if set to <c>true</c> [lo voxel is on].</param>
        /// <param name="hi">The hi.</param>
        /// <param name="hiIndex">Index of the hi.</param>
        /// <param name="hiValueExists">if set to <c>true</c> [hi value exists].</param>
        /// <param name="hiVoxelIsOn">if set to <c>true</c> [hi voxel is on].</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
        private void TurnOnRange(ushort lo, int loIndex, bool loValueExists, bool loVoxelIsOn, ushort hi, int hiIndex,
            bool hiValueExists, bool hiVoxelIsOn, ref int indexLowerBound)
        {
            if (loValueExists && loVoxelIsOn) loIndex++; //if the lo value already lines up with the beginning of a current range
            // then don't delete it
            if (hiValueExists && hiVoxelIsOn)
            {
                hiIndex++; //if the new range ends right where an old one picks up then, be sure not
                           // to include this value in the new ranges as it is in the middle of a good range of on's
                indexLowerBound++;
            }
            for (int i = loIndex; i < hiIndex; i++)
            {
                indices.RemoveAt(loIndex);
                indexLowerBound--;
            }
            if (!hiVoxelIsOn && !hiValueExists)
            {
                indices.Insert(loIndex, hi);
                indexLowerBound++;
            }
            if (!loVoxelIsOn && !loValueExists)
            {
                indices.Insert(loIndex, lo);
                indexLowerBound++;
            }
        }

        /// <summary>
        /// Turns all the voxels within the range to off/false.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        public void TurnOffRange(ushort lo, ushort hi)
        {
            var dummy = 0;
            TurnOffRange(lo, hi, ref dummy);
        }

        /// <summary>
        /// Turns the off range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
        private void TurnOffRange(ushort lo, ushort hi, ref int indexLowerBound)
        {
            var count = indices.Count;
            if (count == 0) return;
            var loIndex = BinarySearch(indices, indices.Count, lo, out var loValueExists, out var loVoxelIsOn, indexLowerBound);
            indexLowerBound = loIndex;
            var hiIndex = BinarySearch(indices, indices.Count, hi, out var hiValueExists, out var hiVoxelIsOn, indexLowerBound);
            indexLowerBound = hiIndex;
            TurnOffRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn, ref indexLowerBound);
        }

        /// <summary>
        /// Turns the off range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="loIndex">Index of the lo.</param>
        /// <param name="loValueExists">if set to <c>true</c> [lo value exists].</param>
        /// <param name="loVoxelIsOn">if set to <c>true</c> [lo voxel is on].</param>
        /// <param name="hi">The hi.</param>
        /// <param name="hiIndex">Index of the hi.</param>
        /// <param name="hiValueExists">if set to <c>true</c> [hi value exists].</param>
        /// <param name="hiVoxelIsOn">if set to <c>true</c> [hi voxel is on].</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
        private void TurnOffRange(ushort lo, int loIndex, bool loValueExists, bool loVoxelIsOn, ushort hi, int hiIndex,
            bool hiValueExists, bool hiVoxelIsOn, ref int indexLowerBound)
        {
            if (loValueExists && !loVoxelIsOn) loIndex++; //if the lo value already lines up with the beginning of a current range
            // then don't delete it
            if (hiValueExists && !hiVoxelIsOn)
            {
                hiIndex++; //if the new range ends right where an old one picks up then, be sure not
                           // to include this value in the new ranges as it is in the middle of a good range of on's
                indexLowerBound++;
            }
            for (int i = loIndex; i < hiIndex; i++)
            {
                indices.RemoveAt(loIndex);
                indexLowerBound--;
            }
            if (hiVoxelIsOn && !hiValueExists)
            {
                indices.Insert(loIndex, hi);
                indexLowerBound++;
            }
            if (loVoxelIsOn && !loValueExists)
            {
                indices.Insert(loIndex, lo);
                indexLowerBound++;
            }
        }

        /// <summary>
        /// Inverts this row - making all on voxels off and vice-versa.
        /// </summary>
        public void Invert()
        {
            if (!indices.Any())
            {
                indices.Add(0);
                indices.Add(ushort.MaxValue);
            }
            else
            {
                if (indices[0] == 0) indices.RemoveAt(0);
                else indices.Insert(0, 0);
                var lastIndex = indices.Count - 1;
                if (indices[lastIndex] == ushort.MaxValue)
                    indices.RemoveAt(lastIndex);
                else indices.Add(ushort.MaxValue);
            }
        }

        /// <summary>
        /// Clears this row of all on voxels.
        /// </summary>
        public void Clear()
        {
            indices.Clear();
        }

        /// <summary>
        /// Averages the positions of the on voxels. This is used in finding center of mass.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int TotalXPosition()
        {
            var rowTotal = 0;
            var numIndices = indices.Count;
            if (numIndices == 0) return 0;
            for (int i = 0; i < numIndices; i += 2)
            {
                var num = indices[i + 1] - indices[i];
                rowTotal += num * (indices[i] + indices[i + 1]);
            }
            return rowTotal;
        }
    }
}