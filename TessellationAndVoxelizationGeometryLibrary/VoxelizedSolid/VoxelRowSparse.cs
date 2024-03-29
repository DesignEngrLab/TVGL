﻿// ***********************************************************************
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
    internal class VoxelRowSparse : VoxelRowBase
    {
        /// <summary>
        /// The indices are pairs of ranges of on-voxels, where the lo value is the position
        /// of the first voxel in a row. These are always at the even positions in the List.
        /// The odd positions are the ends of each range and these values are excluded from
        /// the range. They are the first off-voxel.
        /// </summary>
        internal readonly List<ushort> indices;

        // to save time in the search for the proper row, we
        int lastIndex = -1;
        /// <summary>
        /// Gets the number of voxels in this row.
        /// </summary>
        /// <value>The count.</value>
        internal override int Count
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
        internal VoxelRowSparse()
        {
            indices = new List<ushort>();
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><current>true</current> if XXXX, <current>false</current> otherwise.</returns>
        internal override bool this[int index]
        {
            get
            {
                //lock (indices)
                    return GetValue(index);
            }
            set
            {
                if (value)
                    lock (indices)
                        TurnOn((ushort)index);
                else
                    lock (indices)
                        TurnOff((ushort)index);
            }
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns><current>true</current> if XXXX, <current>false</current> otherwise.</returns>
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
        internal override (bool, bool) GetNeighbors(int xCoord, ushort upperLimit)
        {
            lock (indices)
            {
                var count = indices.Count;
                if (count == 0) return (false, false);
                if (xCoord == 0)
                    return (false, GetValue(xCoord + 1));
                if (xCoord == upperLimit - 1)
                    return (GetValue(xCoord - 1), false);

                var i = BinarySearch(indices, count, xCoord, out var valueExists, out var voxelIsOn);
                return (PreviousVoxelInRangeIsOn(voxelIsOn, valueExists),
                    NextVoxelInRangeIsOn(xCoord, i, count, voxelIsOn));
            }
        }

        private bool PreviousVoxelInRangeIsOn(bool currentVoxelIsOn, bool currentVoxelIsMentionedInRange)
        {
            // if the voxel is on, then the only possibility for the previous voxel to be off is
            // if valueExists is true (result should be false).
            // if the voxel is off, then the only possibility for the previous voxel to be on is
            // if valueExists is true. This is because the exclusive end-of-range matches with the voxel
            // so the previous voxel is on when current is on and not mentioned in range OR
            // when current is off and current is the end of a range
            return currentVoxelIsOn != currentVoxelIsMentionedInRange;
        }

        private bool NextVoxelInRangeIsOn(int xCoord, int xIndex, int count, bool currentVoxelIsOn)
        {
            if (currentVoxelIsOn)
                // since current is on, then the next voxel is true if the value at the next
                // (xCoord+1) is less than the number at the end of the range
                return xCoord + 1 < indices[xIndex + 1];
            // since current is off, the next voxel is true if it is the start of the next range,
            // but first need to check that we are not at the end of the ranges
            return xIndex + 2 < count && xCoord + 1 == indices[xIndex + 2];
        }


        /// <summary>
        /// This is used in the BinarySearch below to force the index (which is an int) to be an even number
        /// </summary>
        const int intForceEven = (int.MaxValue - 1);

        /// <summary>
        /// Binary search for the range that the queried value falls in
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="length">The length.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueExists">if set to <current>true</current> [value exists].</param>
        /// <param name="voxelIsOn">if set to <current>true</current> [voxel is on].</param>
        /// <param name="startGuessIndex">The lower bound.</param>
        /// <returns>System.Int32.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int BinarySearch(IList<ushort> array, int length, double value, out bool valueExists,
            out bool voxelIsOn)
        {
            var lo = 0;
            var hi = length; // note that entries in the array are pairs of start and end of a 
                             // present voxel range. length should always be an even number so the
                             // last valid range starts at length-2

            // set the index to the middle of the range of lo and hi or to the startGuess
            int index = (lastIndex < 0 || lastIndex >= hi) ? (hi >> 1) : lastIndex;
            index &= intForceEven;
            // note that when startGuess is invalid, we use half of the hi value (bit-shift by 1 ">> 1"), but this
            // could be an odd number but we only want to search over the even numbers. So, intForceEven forces the
            // index down one if it is odd
            while (lo <= hi)
            {
                var currentStart = array[index];
                var currentEnd = array[index + 1];

                if (currentStart <= value && value < currentEnd)
                {
                    valueExists = (currentStart == value);
                    voxelIsOn = true;
                    lastIndex = index;
                    return index;
                }
                if (currentEnd <= value && (index + 2 == length || value < array[index + 2]))
                {
                    valueExists = (currentEnd == value);
                    voxelIsOn = false;
                    lastIndex = index;
                    return index;
                }
                if (value < currentStart) hi = index - 2;
                else lo = index + 2;
                index = lo + (((hi - lo) >> 1) & intForceEven);
            }
            // the value was not found in these ranges, so set that bool then return lo, which is actually
            // the array value just larger than it
            valueExists = false;
            voxelIsOn = false;
            lastIndex = hi;
            return hi;
        }

        /// <summary>
        /// Turns the on.
        /// </summary>
        /// <param name="value">The value.</param>
        private void TurnOn(ushort value)
        {
            var count = indices.Count;
            if (count == 0)
            {   //since there are no voxels add this one a lone lower and upper range.
                indices.Add(value);
                indices.Add((ushort)(value + 1));
                return;
            }
            var index = BinarySearch(indices, count, value, out var valueExists, out var voxelIsOn);
            if (voxelIsOn) return; //it's already on
            var prevIsOn = PreviousVoxelInRangeIsOn(false, valueExists);
            var nextIsOn = NextVoxelInRangeIsOn(value, index, count, false);
            if (prevIsOn && nextIsOn)
            {  // this new voxel perfectly fill a gap between two ranges
                indices.RemoveAt(index + 1);
                indices.RemoveAt(index + 1);
            }
            else if (prevIsOn)
                indices[index + 1]++; //increment the upper range of these on-voxels

            else if (nextIsOn)
                //this was the last off-voxel in a range, so need to pull back the next on-voxel
                //range to include this
                indices[index + 2]--;
            else // neither neighbor is on
            {  //add this lone voxel in a range of off-voxels
                index += 2;
                //if (index < 0) index = 0;
                indices.Insert(index, (ushort)(value + 1));
                indices.Insert(index, value);
            }
        }

        /// <summary>
        /// Turns the off.
        /// </summary>
        /// <param name="value">The value.</param>
        private void TurnOff(ushort value)
        {
            var count = indices.Count;
            if (count == 0) return; //nothing to do. there are no voxels anyway

            var index = BinarySearch(indices, count, value, out var valueExists, out var voxelIsOn);
            if (!voxelIsOn) return; //it's already off
            var prevIsOn = PreviousVoxelInRangeIsOn(true, valueExists);
            var nextIsOn = NextVoxelInRangeIsOn(value, index, count, true);
            if (prevIsOn && nextIsOn)
            {   // break the current range into two by adding one off voxel in the range of on-voxels
                indices.Insert(index + 1, (ushort)(value + 1));
                indices.Insert(index + 1, value);
            }
            else if (nextIsOn)
                //otherwise increase the lower bound of this range of on-voxels
                //(or, to say it another way, increase the upper bound of these off-voxels
                indices[index]++;
            else if (prevIsOn)
                //this was the last on-voxel in a range, so need to decrement the top of the range
                indices[index + 1]--;
            else // if (!prevIsOn && !nextIsOn)
            {  // deleting a lone voxel
                indices.RemoveAt(index);
                indices.RemoveAt(index);
            }
        }

        /// <summary>
        /// Unions the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        internal override void Union(VoxelRowBase[] others, int offset = 0)
        {
            lock (indices)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    VoxelRowBase other = others[i];
                    if (other is VoxelRowDense) other = VoxelizedSolid.CopyToSparse(other);
                    var otherIndices = ((VoxelRowSparse)other).indices;
                    var otherLength = otherIndices.Count;
                    for (int j = 0; j < otherLength; j += 2)
                        TurnOnRange(otherIndices[j], otherIndices[j + 1]);
                }
            }
        }

        /// <summary>
        /// Intersects the specified other rows with this row.
        /// </summary>
        /// <param name="others">The others.</param>
        /// <param name="offset">The offset.</param>
        internal override void Intersect(VoxelRowBase[] others, int offset = 0)
        {
            lock (indices)
            {
                for (int i = 0; i < others.Length; i++)
                {
                    VoxelRowBase other = others[i];
                    if (other is VoxelRowDense) other = VoxelizedSolid.CopyToSparse(other);
                    var otherIndices = ((VoxelRowSparse)other).indices;
                    var otherLength = otherIndices.Count;
                    if (otherLength == 0) indices.Clear();
                    else
                    {
                        if (otherIndices[0] != 0)
                            TurnOffRange(0, otherIndices[0]);
                        for (int j = 1; j < otherLength - 1; j += 2)
                            TurnOffRange(otherIndices[j], otherIndices[j + 1]);
                        TurnOffRange(otherIndices[otherLength - 1], ushort.MaxValue);
                    }
                }
            }
        }

        /// <summary>
        /// Subtracts the specified subtrahend rows from this row.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <param name="offset">The offset.</param>
        internal override void Subtract(VoxelRowBase[] subtrahends, int offset = 0)
        {
            lock (indices)
            {
                for (int i = 0; i < subtrahends.Length; i++)
                {
                    VoxelRowBase subtrahend = subtrahends[i];
                    if (subtrahend is VoxelRowDense) subtrahend = VoxelizedSolid.CopyToSparse(subtrahend);
                    var otherIndices = ((VoxelRowSparse)subtrahend).indices;
                    var otherLength = otherIndices.Count;
                    for (int j = 0; j < otherLength; j += 2)
                        TurnOffRange(otherIndices[j], otherIndices[j + 1]);
                }
            }
        }

        /// <summary>
        /// Turns all the voxels within the range to on/true.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        internal override void TurnOnRange(ushort lo, ushort hi)
        {
            lock (indices)
            {
                var count = indices.Count;
                if (count == 0)
                {   //since there are no voxels add this one a a lone lower and upper range.
                    indices.Add(lo);
                    indices.Add(hi);
                    return;
                }
                var loIndex = BinarySearch(indices, indices.Count, lo, out var loValueExists, out var loVoxelIsOn);
                var hiIndex = BinarySearch(indices, indices.Count, hi, out var hiValueExists, out var hiVoxelIsOn);
                TurnOnRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn);
            }
        }

        /// <summary>
        /// Turns the on range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="loIndex">Index of the lo.</param>
        /// <param name="loAtRangeValue">if set to <current>true</current> [lo value exists].</param>
        /// <param name="loVoxelIsOn">if set to <current>true</current> [lo voxel is on].</param>
        /// <param name="hi">The hi.</param>
        /// <param name="hiIndex">Index of the hi.</param>
        /// <param name="hiAtRangeValue">if set to <current>true</current> [hi value exists].</param>
        /// <param name="hiVoxelIsOn">if set to <current>true</current> [hi voxel is on].</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
        private void TurnOnRange(ushort lo, int loIndex, bool loAtRangeValue, bool loVoxelIsOn,
                                 ushort hi, int hiIndex, bool hiAtRangeValue, bool hiVoxelIsOn)
        {
            loIndex++;
            if (!loVoxelIsOn && !loAtRangeValue)
            {
                loIndex++;
                indices.Insert(loIndex, lo);
                loIndex++;
                hiIndex++;
            }
            hiIndex++;
            if (!hiVoxelIsOn && !hiAtRangeValue)
            {
                hiIndex++;
                indices.Insert(hiIndex, hi);
            }
            var numToRemove = hiIndex - loIndex;
            indices.RemoveRange(loIndex, numToRemove);
        }

        /// <summary>
        /// Turns all the voxels within the range to off/false.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="hi">The hi.</param>
        internal override void TurnOffRange(ushort lo, ushort hi)
        {
            lock (indices)
            {
                var count = indices.Count;
                if (count == 0) return;
                var loIndex = BinarySearch(indices, indices.Count, lo, out var loValueExists, out var loVoxelIsOn);
                var hiIndex = BinarySearch(indices, indices.Count, hi, out var hiValueExists, out var hiVoxelIsOn);
                TurnOffRange(lo, loIndex, loValueExists, loVoxelIsOn, hi, hiIndex, hiValueExists, hiVoxelIsOn);
            }
        }

        /// <summary>
        /// Turns the off range.
        /// </summary>
        /// <param name="lo">The lo.</param>
        /// <param name="loIndex">Index of the lo.</param>
        /// <param name="loAtRangeValue">if set to <current>true</current> [lo value exists].</param>
        /// <param name="loVoxelIsOn">if set to <current>true</current> [lo voxel is on].</param>
        /// <param name="hi">The hi.</param>
        /// <param name="hiIndex">Index of the hi.</param>
        /// <param name="hiAtRangeValue">if set to <current>true</current> [hi value exists].</param>
        /// <param name="hiVoxelIsOn">if set to <current>true</current> [hi voxel is on].</param>
        /// <param name="indexLowerBound">The index lower bound.</param>
        private void TurnOffRange(ushort lo, int loIndex, bool loAtRangeValue, bool loVoxelIsOn,
                                  ushort hi, int hiIndex, bool hiAtRangeValue, bool hiVoxelIsOn)
        {
            if (loVoxelIsOn)
            {  // if at start range, do nothing. the removerange will take care of it
                if (!loAtRangeValue)
                {
                    loIndex++;
                    indices.Insert(loIndex, lo);
                    loIndex++;
                    hiIndex++; //because of the Insert
                }
            }
            else loIndex += 2;
            if (hiVoxelIsOn)
            {
                if (!hiAtRangeValue) // if hi is on but it's not the first (hiAtRangeValue), 
                                     // because it's okay for the last exclusive upper range to be on (off in normal range; on in subtracted range)
                {
                    hiIndex++; // go to the next spot and add this as the new start for the next on-range1
                    indices.Insert(hiIndex, hi);
                }
            }
            else 
                hiIndex += 2;
            var numToRemove = hiIndex - loIndex;
            if (numToRemove < 0) numToRemove = 0;
            indices.RemoveRange(loIndex, numToRemove);
        }

        /// <summary>
        /// Inverts this row - making all on voxels off and vice-versa.
        /// </summary>
        internal override void Invert(ushort numVoxelsX)
        {
            lock (indices)
            {
                if (!indices.Any())
                {
                    indices.Add(0);
                    indices.Add(numVoxelsX);
                }
                else
                {
                    if (indices[0] == 0) indices.RemoveAt(0);
                    else indices.Insert(0, 0);
                    var lastIndex = indices.Count - 1;
                    if (indices[lastIndex] >= numVoxelsX)
                        indices.RemoveAt(lastIndex);
                    else indices.Add(numVoxelsX);
                }
            }
        }

        /// <summary>
        /// Clears this row of all on voxels.
        /// </summary>
        internal override void Clear()
        {
            lock (indices)
            {
                indices.Clear();
            }
        }

        /// <summary>
        /// Averages the positions of the on voxels. This is used in finding center of mass.
        /// </summary>
        /// <returns>System.Int32.</returns>
        internal override int AverageXPosition()
        {
            lock (indices)
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

        /// <summary>
        /// Returns all the on-voxels for this row within the given range
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal override IEnumerable<ushort> XIndices(ushort start = 0, ushort end = ushort.MaxValue)
        {
            lock (indices)
            {
                if (indices.Count == 0) yield break;
                var startIndex = 1;
                while (indices[startIndex] < start) startIndex += 2;
                startIndex--;
                for (int i = startIndex; i < indices.Count; i += 2)
                {
                    ushort upLim;
                    bool lastOne;
                    if (indices[i + 1] > end)
                    {
                        upLim = end;
                        lastOne = true;
                    }
                    else
                    {
                        upLim = indices[i + 1];
                        lastOne = false;
                    }
                    for (var j = indices[i]; j < upLim; j++)
                    {
                        yield return j;
                    }
                    if (lastOne) yield break;
                }
            }
        }
    }
}