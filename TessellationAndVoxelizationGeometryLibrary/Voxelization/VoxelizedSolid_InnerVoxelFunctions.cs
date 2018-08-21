// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid
    {
        #region Functions to make voxel full, empty or partial
        /// <summary>
        /// Changes the voxel to empty.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="removeDescendants">if set to <c>true</c> [remove descendants].</param>
        /// <param name="checkParentEmpty">if set to <c>true</c> [check parent empty].</param>
        public void ChangeVoxelToEmpty(long voxel, bool removeDescendants, bool checkParentEmpty)
        {
            Constants.GetAllFlags(voxel, out var level, out var role, out var btmIsInside);
            if (role == VoxelRoleTypes.Empty) return;
            if (level == 0)
                // no need to do anything with the HigherLevelVoxels as deleting this Voxel, deletes
                // the only reference to these higher voxels.
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Remove(voxel);
            else
            {
                var voxel0 = voxelDictionaryLevel0.GetVoxel(voxel);
                lock (voxel0.InnerVoxels[level - 1]) //remove the node here
                {
                    voxel0.InnerVoxels[level - 1].Remove(voxel);
                    var parent = GetParentVoxel(voxel);
                    // then check to see if the parent should be empty as well
                    if (checkParentEmpty)
                        if (voxel0.InnerVoxels[level - 1].Count == 0 ||
                            voxel0.InnerVoxels[level - 1].CountDescendants(parent, level - 1) == 0)
                            ChangeVoxelToEmpty(parent, false, true);
                }
                // finally, any descendants of voxel need to be removed
                if (role == VoxelRoleTypes.Partial && removeDescendants)
                {
                    for (int i = level; i < numberOfLevels - 1; i++)
                    {
                        // by starting at level (and not doing an i-1), we are starting at the next lower level
                        lock (voxel0.InnerVoxels[i]) voxel0.InnerVoxels[i].RemoveDescendants(voxel, level);
                    }
                }
            }
        }

        private long ChangeEmptyVoxelToFull(long ID, int level, bool checkParentFull)
        {
            if (level == 0)
            {   //adding a new level-0 voxel is fairly straightforward
                var voxelBin = new VoxelBinClass(ID, VoxelRoleTypes.Full, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.AddOrReplace(voxelBin);
                voxelBin.ID = Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Full);
                return voxelBin.ID;
            }
            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = MakeParentVoxelID(thisIDwoFlags, 0);
            VoxelBinClass voxel0;
            lock (voxelDictionaryLevel0)
                if (!voxelDictionaryLevel0.Contains(id0))
                {
                    ChangeEmptyVoxelToPartial(id0, 0);
                    voxel0 = voxelDictionaryLevel0.GetVoxel(id0);
                }
                else
                    voxel0 = (VoxelBinClass)voxelDictionaryLevel0.GetVoxel(id0);
            // make the new Voxel, and add it to the proper hashset
            var voxel = thisIDwoFlags + Constants.MakeFlags(level, VoxelRoleTypes.Full);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            if (level == 1 && checkParentFull)
            {
                //What if this is the last voxel to be added that makes the parent full?
                // This is checked differently for the level-1 than the others. The code could be
                // combined but it would be less efficient.
                var changeToFull = false;
                lock (voxel0.InnerVoxels[0])
                    changeToFull = (voxel0.InnerVoxels[0].Count == voxelsInParent[level]
                                    //the following All statement is slow. First, just
                                    // check if its worth counting to see if all are Full
                                    && voxel0.InnerVoxels[0].All(v => Constants.GetRole(v) == VoxelRoleTypes.Full));
                if (changeToFull) ChangeVoxelToFull(voxel0.ID, checkParentFull);
            }
            else if (level > 1)
            {
                // for the remaining voxellevels, we also need to check if the parent has been created
                var parentID = MakeParentVoxelID(voxel, level - 1);
                long parentVoxel;
                var mightBeFull = false;
                lock (voxel0.InnerVoxels[level - 2]) parentVoxel = voxel0.InnerVoxels[level - 2].GetVoxel(parentID);
                if (parentVoxel == 0) ChangeEmptyVoxelToPartial(parentID, level - 1);
                else mightBeFull = true;
                if (checkParentFull && mightBeFull)
                {
                    bool makeParentFull = false;
                    lock (voxel0.InnerVoxels[level - 1])
                    {
                        // since the hashsets are combined we need to count
                        // what is indeed the immediate descendant of the the parent and see if they are all full
                        makeParentFull = voxel0.InnerVoxels[level - 1].Count >= voxelsInParent[level];
                        if (makeParentFull)
                            makeParentFull = (voxel0.InnerVoxels[level - 1]
                                                  .CountDescendants(parentID, level - 1, VoxelRoleTypes.Full) ==
                                              voxelsInParent[level]);
                    }
                    if (makeParentFull) ChangeVoxelToFull(parentVoxel, checkParentFull);
                }
            }
            return voxel;
        }


        /// <summary>
        /// Changes the voxel to full.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="checkParentFull">if set to <c>true</c> [check parent full].</param>
        /// <returns>long.</returns>
        public long ChangeVoxelToFull(long voxel, bool checkParentFull)
        {
            Constants.GetAllFlags(voxel, out var level, out var role, out var btmIsInside);
            if (role == VoxelRoleTypes.Full)
            {
                // Debug.WriteLine("Call to ChangeVoxelToFull but voxel is already full.");
                return voxel;
            }

            if (role == VoxelRoleTypes.Empty) return ChangeEmptyVoxelToFull(voxel, level, checkParentFull);
            // so the rest of this function is doing the work to change a partial voxel to a full
            var voxel0 = voxelDictionaryLevel0.GetVoxel(voxel);
            if (level == 0)
            {
                // level-0 is easy since it has no parent voxels to check and the children are easily removed
                // by deleting the InnerVoxels object.
                lock (voxel0)
                {
                    voxel0.InnerVoxels = new VoxelHashSet[numberOfLevels - 1];
                    for (int i = 1; i < numberOfLevels; i++)
                        voxel0.InnerVoxels[i - 1] = new VoxelHashSet(i, bitLevelDistribution);
                    voxel0.BtmCoordIsInside = true;
                    voxel0.Role = VoxelRoleTypes.Full;
                    voxel0.ID = Constants.ClearFlagsFromID(voxel) + Constants.MakeFlags(level, VoxelRoleTypes.Full);
                    return voxel0.ID;
                }
            }
            voxel = Constants.ClearFlagsFromID(voxel) + Constants.MakeFlags(level, VoxelRoleTypes.Full);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            // now, remove all the descendants - for all lower levels. This is just like in changing partial to empty
            for (int i = level; i < numberOfLevels - 1; i++)
                lock (voxel0.InnerVoxels[i])
                    voxel0.InnerVoxels[i].RemoveDescendants(voxel, level);
            if (checkParentFull)
            {
                if (level == 1)
                {
                    //What if this is the last voxel to be added that makes the parent full?
                    // This is checked differently for the level-1 than the others. The code could be
                    // combined but it would be less efficient.
                    if (voxel0.InnerVoxels[0].Count == voxelsInParent[level]
                      //the following All statement is slow. First, just
                      // check if its worth counting to see if all are Full
                      && voxel0.InnerVoxels[0].All(v => Constants.GetRole(v) == VoxelRoleTypes.Full))
                        ChangeVoxelToFull(voxel0.ID, false);
                }
                else
                {
                    // for the remaining voxellevels, since the hashsets are combined we need to count
                    // what is indeed an immediate descendant of the the parent and see if they are all full
                    var parentID = MakeParentVoxelID(voxel, level - 1);
                    var makeVoxelFull = false;
                    lock (voxel0.InnerVoxels[level - 1])
                        makeVoxelFull = voxel0.InnerVoxels[level - 1].Count >= voxelsInParent[level];
                    if (makeVoxelFull)
                        lock (voxel0.InnerVoxels[level - 1])
                            makeVoxelFull =
                                (voxel0.InnerVoxels[level - 1]
                                     .CountDescendants(parentID, level - 1, VoxelRoleTypes.Full) == voxelsInParent[level]);
                    if (makeVoxelFull) ChangeVoxelToFull(voxel0.InnerVoxels[level - 1 - 1].GetVoxel(parentID), true);
                }
            }
            return voxel;
        }

        private long ChangeEmptyVoxelToPartial(long ID, int level)
        {
            long voxel;
            if (level == 0)
            {
                //adding a new level-0 voxel is fairly straightforward
                var voxelbin = new VoxelBinClass(ID, VoxelRoleTypes.Partial, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.AddOrReplace(voxelbin);
                voxelbin.ID = Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Partial);
                return voxelbin.ID;
            }
            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = MakeParentVoxelID(thisIDwoFlags, 0);
            VoxelBinClass voxel0;
            lock (voxelDictionaryLevel0)
            {
                if (!voxelDictionaryLevel0.Contains(id0))
                    ChangeEmptyVoxelToPartial(id0, 0);
                voxel0 = voxelDictionaryLevel0.GetVoxel(id0);
            }
            // make the new Voxel, and add it to the proper hashset
            voxel = thisIDwoFlags + Constants.MakeFlags(level, VoxelRoleTypes.Partial);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            // well, we don't need to check if parent is full but we may need to create the parent
            if (level > 1)
            {
                // for the remaining voxellevels, we also need to check if the parent has been created
                var parentID = MakeParentVoxelID(voxel, level - 1);
                lock (voxel0.InnerVoxels[level - 2])
                    parentID = voxel0.InnerVoxels[level - 2].GetVoxel(parentID);
                if (parentID == 0) ChangeEmptyVoxelToPartial(parentID, level - 1);
            }
            return voxel;
        }

        private long ChangeVoxelToPartial(long voxel, bool addAllDescendants)
        {
            Constants.GetAllFlags(voxel, out var level, out var role, out var btmIsInside);
            if (role == VoxelRoleTypes.Partial)
            {
                //Debug.WriteLine("Call to ChangeVoxelToPartial but voxel is already partial.");
                return voxel;
            }
            if (role == VoxelRoleTypes.Empty) return ChangeEmptyVoxelToPartial(voxel, level);
            // otherwise, we are changing a full to a partial
            var voxel0 = voxelDictionaryLevel0.GetVoxel(voxel);
            if (level == 0)
            {
                //again, level-0 is easy. there's no parent, and the class allows us to change role directly.
                // also, we need to set up the innerVoxel hashsets.
                lock (voxel0)
                {
                    voxel0.Role = VoxelRoleTypes.Partial;
                    if (addAllDescendants) AddAllDescendants(Constants.ClearFlagsFromID(voxel), level, voxel0);
                    voxel0.ID = Constants.ClearFlagsFromID(voxel) +
                                Constants.MakeFlags(level, VoxelRoleTypes.Partial, true);
                }
                return voxel0.ID;
            }
            // if the voxel at level-0 is full then we first must change it to Partial so that we can
            // add this voxel. In this way, populateSubVoxels must be set to true in the following recursion
            // so that we have an accurate representation of the parent. In the present, it may be false since
            // the calling function intends to fill it up. Actually, I'm not sure the recursion will ever happed
            // with current set of modification functions that work form level-0 on down.
            if (voxel0.Role == VoxelRoleTypes.Full) ChangeVoxelToPartial(voxel0.ID, true);
            voxel = Constants.ClearFlagsFromID(voxel) + Constants.MakeFlags(level, VoxelRoleTypes.Partial);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            if (addAllDescendants && this.numberOfLevels - 1 != level)
                AddAllDescendants(Constants.ClearFlagsFromID(voxel), level, voxel0);
            return voxel;  //if at the lowest level or
        }

        List<long> AddAllDescendants(long startingID, int level, int shortDimension = -1, int numShortLayers = -1)
        {
            var limits = new[] { voxelsPerSide[level + 1], voxelsPerSide[level + 1], voxelsPerSide[level + 1] };
            if (shortDimension >= 0) limits[shortDimension] = numShortLayers;
            var descendants = new List<long>();
            var xShift = 1L << (4 + singleCoordinateShifts[level + 1]); //finding the correct multiplier requires adding up all the bits used in current levels
            var yShift = xShift << 20; //once the xShift is known, the y and z shifts are just 20 bits over
            var zShift = yShift << 20;
            for (int i = 0; i < limits[0]; i++)
                for (int j = 0; j < limits[1]; j++)
                    for (int k = 0; k < limits[2]; k++)
                        descendants.Add(startingID + (i * xShift) + (j * yShift) + (k * zShift)
                                                       + Constants.MakeFlags(level + 1, VoxelRoleTypes.Full, true));
            return descendants;
        }

        void AddAllDescendants(long startingID, int level, VoxelBinClass voxel0, int shortDimension = -1,
            int numShortLayers = -1)
        {
            var lowerLevelVoxels = AddAllDescendants(startingID, level, shortDimension, numShortLayers);
            lock (voxel0.InnerVoxels[level])
                voxel0.InnerVoxels[level].AddRange(lowerLevelVoxels);
        }

        #endregion

        /// <summary>
        /// Updates the properties.
        /// </summary>
        /// <param name="level">The level.</param>
        public void UpdateProperties(int level = -1)
        {
            _totals = new long[2 * numberOfLevels];
            _totals[0] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Full);
            _totals[1] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Partial);
            for (int i = 1; i < numberOfLevels; i++)
            {
                _totals[2 * i] = voxelDictionaryLevel0.Sum(dict => CountVoxels((VoxelBinClass)dict, i, VoxelRoleTypes.Full));
                _totals[2 * i + 1] = voxelDictionaryLevel0.Sum(dict => CountVoxels((VoxelBinClass)dict, i, VoxelRoleTypes.Partial));
            };
            Volume = 0.0;
            for (int i = 0; i < numberOfLevels; i++)
                Volume += Math.Pow(VoxelSideLengths[i], 3) * _totals[2 * i];
            Volume += Math.Pow(VoxelSideLengths[numberOfLevels - 1], 3) * _totals[2 * (numberOfLevels - 1) + 1];
            _count = _totals.Sum();
        }

        private long CountVoxels(VoxelBinClass voxel0, int level, VoxelRoleTypes role)
        {
            return voxel0.InnerVoxels[level - 1].Count(v => Constants.GetRole(v) == role);
        }

        internal double[] GetRealCoordinates(int level, params int[] indices)
        {
            return indices.multiply(VoxelSideLengths[level]).add(Offset);
        }
    }
}
