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
        public void ChangeVoxelToEmpty(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Empty) return;
            if (voxel.Level == 0)
                // no need to do anything with the HigherLevelVoxels as deleting this Voxel, deletes
                // the only reference to these higher voxels.
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Remove(voxel.ID);
            else
            {
                var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(voxel.ID);
                lock (voxel0.InnerVoxels[voxel.Level - 1]) //remove the node here
                    voxel0.InnerVoxels[voxel.Level - 1].Remove(voxel.ID);
                var parent = GetParentVoxel(voxel);
                // then check to see if the parent should be empty as well
                if (voxel0.InnerVoxels[voxel.Level - 1].Count == 0 ||
                    voxel0.InnerVoxels[voxel.Level - 1].CountDescendants(parent.ID, voxel.Level - 1) == 0)
                    ChangeVoxelToEmpty(GetParentVoxel(voxel));
                // finally, any descendants of voxel need to be removed
                else if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    for (int i = voxel.Level; i < numberOfLevels; i++)
                    { // by starting at voxel.Level (and not doing an i-1), we are starting at the next lower level
                        lock (voxel0.InnerVoxels[i])
                            voxel0.InnerVoxels[i].RemoveDescendants(voxel.ID, voxel.Level);
                    }
                }

            }
        }

        private IVoxel ChangeEmptyVoxelToFull(long ID, int level)
        {
            IVoxel voxel;
            if (level == 0)
            {
                voxel = new Voxel_Level0_Class(ID, VoxelRoleTypes.Full, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Add(voxel);
                return voxel;
            }
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = Constants.MakeParentVoxelID(thisIDwoFlags, 0);
            Voxel_Level0_Class voxel0;
            lock (voxelDictionaryLevel0)
                if (!voxelDictionaryLevel0.Contains(id0))
                    voxel0 = (Voxel_Level0_Class)ChangeEmptyVoxelToPartial(id0, 0);
                else
                    voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
            lock (voxel0.InnerVoxels[level - 1]) voxel0.InnerVoxels[level - 1].Add(voxel);
            if (level == 1)
            {   //What if this is the last voxel to be added that makes the parent full?
                // This is checked differently for the level-1 than the others. The code could be
                // combined but it would be less efficient.
                lock (voxel0.InnerVoxels[0])
                {
                    if (voxel0.InnerVoxels[0].Count == voxelsInParent[level] //the following All statement is slow. First, just
                                                                             // check if its worth counting to see if all are Full
                        && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full))
                        ChangeVoxelToFull(voxel0);
                }
            }
            else if (level > 1 && voxel0.InnerVoxels[voxel.Level - 1].Count >= voxelsInParent[level])
            {   // for the remaining voxellevels, since the hashsets are combined we need to count
                // what is indeed an immediate descendant of the the parent and see if they are all full
                var parentID = Constants.MakeParentVoxelID(voxel.ID, level - 1);
                lock (voxel0.InnerVoxels[level - 1])
                {
                    if (voxel0.InnerVoxels[voxel.Level - 1].CountDescendants(parentID, level - 1, VoxelRoleTypes.Full) == voxelsInParent[level])
                        ChangeVoxelToFull(voxel0.InnerVoxels[level - 1 - 1].GetVoxel(parentID));
                }
            }
            return voxel;
        }


        /// <summary>
        /// Changes the voxel to full.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel ChangeVoxelToFull(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Full)
            {
                // Debug.WriteLine("Call to ChangeVoxelToFull but voxel is already full.");
                return voxel;
            }
            if (voxel.Role == VoxelRoleTypes.Empty) return ChangeEmptyVoxelToFull(voxel.ID, voxel.Level);
            // so the rest of this function is doing the work to change a partial voxel to a full
            if (voxel.Level == 0)
            {   // level-0 is easy since it has no parent voxels to check and the children are easily removed
                // by deleting the InnerVoxels object.
                lock (voxel)
                    ((Voxel_Level0_Class)voxel).InnerVoxels = null;
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
                return voxel;
            }
            var level = voxel.Level;
            var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(voxel.ID);
            if (voxel is Voxel_ClassWithLinksToTSElements) ((Voxel_ClassWithLinksToTSElements)voxel).Role = VoxelRoleTypes.Full;
            else //if it's a class then we can change with the above statement
            {    //if it's the voxel struct then we have to delete it and make a new one
                lock (voxel0.InnerVoxels[level - 1])
                {
                    voxel0.InnerVoxels[level - 1].Remove(voxel);
                    voxel = new Voxel(Constants.ClearFlagsFromID(voxel.ID) + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
                    voxel0.InnerVoxels[level - 1].Add(voxel);
                }
            }
            // now, remove all the descendants - for all lower levels. This is just like in changing partial to empty
            for (int i = level; i < numberOfLevels-1; i++)
            {
                lock (voxel0.InnerVoxels[i])
                    voxel0.InnerVoxels[i]
                        .RemoveDescendants(voxel.ID, level);
            }
            if (level == 1)
            {   //What if this is the last voxel to be added that makes the parent full?
                // This is checked differently for the level-1 than the others. The code could be
                // combined but it would be less efficient.
                lock (voxel0.InnerVoxels[0])
                {
                    if (voxel0.InnerVoxels[0].Count == voxelsInParent[level] //the following All statement is slow. First, just
                                                                             // check if its worth counting to see if all are Full
                               && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full))
                        ChangeVoxelToFull(voxel0);
                }
            }
            else if (level > 1 && voxel0.InnerVoxels[voxel.Level - 1].Count >= voxelsInParent[level])
            {   // for the remaining voxellevels, since the hashsets are combined we need to count
                // what is indeed an immediate descendant of the the parent and see if they are all full
                var parentID = Constants.MakeParentVoxelID(voxel.ID, level - 1);
                lock (voxel0.InnerVoxels[level - 1])
                {
                    if (voxel0.InnerVoxels[voxel.Level - 1].CountDescendants(parentID, level - 1, VoxelRoleTypes.Full) == voxelsInParent[level])
                        ChangeVoxelToFull(voxel0.InnerVoxels[level - 1 - 1].GetVoxel(parentID));
                }
            }
            return voxel;
        }

        private IVoxel ChangeEmptyVoxelToPartial(long ID, int level)
        {
            IVoxel voxel;
            if (level == 0)
            {  //adding a new level-0 voxel is fairly straightforward
                voxel = new Voxel_Level0_Class(ID, VoxelRoleTypes.Partial, this);
                for (int i = 1; i < numberOfLevels; i++)
                    ((Voxel_Level0_Class)voxel).InnerVoxels[i - 1] = new VoxelHashSet(i, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Add(voxel);
                return voxel;
            }
            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = Constants.MakeParentVoxelID(thisIDwoFlags, 0);
            Voxel_Level0_Class voxel0;
            lock (voxelDictionaryLevel0)
                if (!voxelDictionaryLevel0.Contains(id0))
                    voxel0 = (Voxel_Level0_Class)ChangeEmptyVoxelToPartial(id0, 0);
                else voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            // make the new Voxel, and add it to the proper hashset
            voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), this);
            lock (voxel0.InnerVoxels[level - 1]) voxel0.InnerVoxels[level - 1].Add(voxel);
            return voxel;
        }
        private IVoxel ChangeVoxelToPartial(IVoxel voxel, bool populateSubVoxels = true)
        {
            if (voxel.Role == VoxelRoleTypes.Partial)
            {
                //Debug.WriteLine("Call to ChangeVoxelToPartial but voxel is already partial.");
                return voxel;
            }

            if (voxel.Role == VoxelRoleTypes.Empty) return ChangeEmptyVoxelToPartial(voxel.ID, voxel.Level);
            // otherwise, we are changing a full to a partial
            var level = voxel.Level;
            Voxel_Level0_Class voxel0;
            if (level == 0)
            {
                //again, level-0 is easy. there's no parent, and the class allows us to change role directly.
                // also, we need to set up the innerVoxel hashsets.
                voxel0 = (Voxel_Level0_Class)voxel;
                voxel0.InnerVoxels = new VoxelHashSet[numberOfLevels - 1];
                voxel0.Role = VoxelRoleTypes.Partial;
            }
            else
            {
                voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(voxel.ID);
                // if the voxel at level-0 is full then we first must change it to Partial so that we can
                // add this voxel. In this way, populateSubVoxels must be set to true in the following recursion
                // so that we have an accurate representation of the parent. In the present, it may be false since
                // the calling function intends to fill it up. Actually, I'm not sure the recursion will ever happed
                // with current set of modification functions that work form level-0 on down.
                if (voxel0.Role == VoxelRoleTypes.Full) ChangeVoxelToPartial(voxel0, true);
                if (voxel is Voxel_ClassWithLinksToTSElements)
                    ((Voxel_ClassWithLinksToTSElements)voxel).Role = VoxelRoleTypes.Partial;
                else //if it's a class then we can change with the above statement
                {
                    //if it's the voxel struct then we have to delete it and make a new one
                    lock (voxel0.InnerVoxels[level - 1])
                    {
                        voxel0.InnerVoxels[level - 1].Remove(voxel);
                        voxel = new Voxel(Constants.ClearFlagsFromID(voxel.ID) +
                                          Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), this);
                        voxel0.InnerVoxels[level - 1].Add(voxel);
                    }
                }
            }
            if (voxel0.InnerVoxels[level - 1] == null) //ensure that InnerVoxels is setup. Actually, I'm pretty sure 
                //this code will never be hit. testing...
                voxel0.InnerVoxels[level - 1] = new VoxelHashSet(level, this);

            if (!populateSubVoxels || this.numberOfLevels == level) return voxel;  //if at the lowest level or
                                                                                   //if populateSubVoxels is false, then we can return 
            var startingID = Constants.ClearFlagsFromID(voxel.ID);  //this provides the base for adding all descendants
            var lowerLevelVoxels = new List<IVoxel>();
            var xShift = 1L << (4 + 20 - bitLevelDistribution.Take(level).Sum()); //finding the correct multiplier requires adding up all the bits used in current levels
            var yShift = xShift << 20; //once the xShift is known, the y and z shifts are just 20 bits over
            var zShift = yShift << 20;
            for (int i = 0; i < voxelsPerSide[level + 1]; i++)
                for (int j = 0; j < voxelsPerSide[level + 1]; j++)
                    for (int k = 0; k < voxelsPerSide[level + 1]; k++)
                        lowerLevelVoxels.Add(new Voxel(startingID
                                                       + (i * xShift) + (j * yShift) + (k * zShift)
                                                       + Constants.SetRoleFlags(level + 1, VoxelRoleTypes.Full, true), this));
            lock (voxel0.InnerVoxels[level])
                voxel0.InnerVoxels[level].AddRange(lowerLevelVoxels);
            return voxel;
        }
        #endregion

        public void UpdateProperties(int level = -1)
        {
            _totals = new long[2 * numberOfLevels];
            _totals[0] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Full);
            _totals[1] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Partial);
            for (int i = 1; i < numberOfLevels; i++)
            {
                _totals[2 * i] = voxelDictionaryLevel0.Sum(dict => CountVoxels(dict, 1, VoxelRoleTypes.Full));
                _totals[2 * i + 1] = voxelDictionaryLevel0.Sum(dict => CountVoxels(dict, 1, VoxelRoleTypes.Partial));
            };
            Volume = 0.0;
            for (int i = 0; i < numberOfLevels; i++)
                Volume += Math.Pow(VoxelSideLengths[i], 3) * _totals[2 * i];
            Volume += Math.Pow(VoxelSideLengths[numberOfLevels - 1], 3) * _totals[2 * (numberOfLevels - 1) + 1];
            _count = _totals.Sum();
        }

        private long CountVoxels(IVoxel dict, int level, VoxelRoleTypes role)
        {
            var innerVoxels = ((Voxel_Level0_Class)dict).InnerVoxels;
            if (innerVoxels == null || innerVoxels.Length < level || innerVoxels[level - 1] == null) return 0L;
            return innerVoxels[level - 1].Count(v => v.Role == role);
        }

        internal double[] GetRealCoordinates(int level, params int[] indices)
        {
            return indices.multiply(VoxelSideLengths[level]).add(Offset);
        }


        #region Quick Booleans for IDs
        internal static bool isFull(long ID)
        {
            return (ID & 3) == 3;
        }
        internal static bool isEmpty(long ID)
        {
            return (ID & 3) == 0;
        }
        internal static bool isPartial(long ID)
        {
            var id = ID & 3;
            return id == 1 || id == 2;
        }

        internal static bool isLevel4(long ID)
        {
            return (ID & 15) >= 12;
        }
        internal static bool isLevel3(long ID)
        {
            var id = ID & 15;
            return id < 12 && id >= 8;
        }
        internal static bool isLevel2(long ID)
        {
            var id = ID & 15;
            return id < 8 && id >= 4;
        }


        #endregion
    }
}