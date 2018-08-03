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
        public void ChangeVoxelToEmpty(IVoxel voxel, bool removeDescendants, bool checkParentEmpty)
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
                {
                    voxel0.InnerVoxels[voxel.Level - 1].Remove(voxel.ID);
                    var parent = GetParentVoxel(voxel);
                    // then check to see if the parent should be empty as well
                    if (checkParentEmpty)
                        if (voxel0.InnerVoxels[voxel.Level - 1].Count == 0 ||
                            voxel0.InnerVoxels[voxel.Level - 1].CountDescendants(parent.ID, voxel.Level - 1) == 0)
                            ChangeVoxelToEmpty(parent, false, true);
                }
                // finally, any descendants of voxel need to be removed
                if (voxel.Role == VoxelRoleTypes.Partial && removeDescendants)
                {
                    for (int i = voxel.Level; i < numberOfLevels - 1; i++)
                    {
                        // by starting at voxel.Level (and not doing an i-1), we are starting at the next lower level
                        lock (voxel0.InnerVoxels[i]) voxel0.InnerVoxels[i].RemoveDescendants(voxel.ID, voxel.Level);
                    }
                }
            }
        }

        private IVoxel ChangeEmptyVoxelToFull(long ID, int level, bool checkParentFull)
        {
            IVoxel voxel;
            if (level == 0)
            {   //adding a new level-0 voxel is fairly straightforward
                voxel = new Voxel_Level0_Class(ID, VoxelRoleTypes.Full, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.AddOrReplace(voxel);
                return voxel;
            }
            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = MakeParentVoxelID(thisIDwoFlags, 0);
            Voxel_Level0_Class voxel0;
            lock (voxelDictionaryLevel0)
                if (!voxelDictionaryLevel0.Contains(id0))
                    voxel0 = (Voxel_Level0_Class)ChangeEmptyVoxelToPartial(id0, 0);
                else
                    voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            // make the new Voxel, and add it to the proper hashset
            voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
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
                                    && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full));
                if (changeToFull) ChangeVoxelToFull(voxel0, checkParentFull);
            }
            else if (level > 1)
            {
                // for the remaining voxellevels, we also need to check if the parent has been created
                var parentID = MakeParentVoxelID(voxel.ID, level - 1);
                IVoxel parentVoxel;
                var mightBeFull = false;
                lock (voxel0.InnerVoxels[level - 2]) parentVoxel = voxel0.InnerVoxels[level - 2].GetVoxel(parentID);
                if (parentVoxel == null) ChangeEmptyVoxelToPartial(parentID, level - 1);
                else mightBeFull = true;
                if (checkParentFull && mightBeFull)
                {
                    bool makeParentFull = false;
                    lock (voxel0.InnerVoxels[voxel.Level - 1])
                    {
                        // since the hashsets are combined we need to count
                        // what is indeed the immediate descendant of the the parent and see if they are all full
                        makeParentFull = voxel0.InnerVoxels[voxel.Level - 1].Count >= voxelsInParent[level];
                        if (makeParentFull)
                            makeParentFull = (voxel0.InnerVoxels[voxel.Level - 1]
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
        /// <returns>IVoxel.</returns>
        public IVoxel ChangeVoxelToFull(IVoxel voxel, bool checkParentFull)
        {
            // if (voxel == null) Debug.WriteLine("");
            if (voxel.Role == VoxelRoleTypes.Full)
            {
                // Debug.WriteLine("Call to ChangeVoxelToFull but voxel is already full.");
                return voxel;
            }

            if (voxel.Role == VoxelRoleTypes.Empty) return ChangeEmptyVoxelToFull(voxel.ID, voxel.Level, checkParentFull);
            // so the rest of this function is doing the work to change a partial voxel to a full
            if (voxel.Level == 0)
            {
                // level-0 is easy since it has no parent voxels to check and the children are easily removed
                // by deleting the InnerVoxels object.

                lock (voxel)
                {
                    ((Voxel_Level0_Class)voxel).InnerVoxels = new VoxelHashSet[numberOfLevels - 1];

                    for (int i = 1; i < numberOfLevels; i++)
                        ((Voxel_Level0_Class)voxel).InnerVoxels[i - 1] = new VoxelHashSet(i, this);

                    ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
                    return voxel;
                }
            }
            var level = voxel.Level;
            var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(voxel.ID);

            if (voxel is Voxel_ClassWithLinksToTSElements)
                ((Voxel_ClassWithLinksToTSElements)voxel).Role = VoxelRoleTypes.Full;
            else //if it's a class then we can change with the above statement
            {
                //if it's the voxel struct then we have to delete it and make a new one
                voxel = new Voxel(
                    Constants.ClearFlagsFromID(voxel.ID) + Constants.SetRoleFlags(level, VoxelRoleTypes.Full),
                    this);
                lock (voxel0.InnerVoxels[level - 1])
                    voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            }

            // now, remove all the descendants - for all lower levels. This is just like in changing partial to empty
            for (int i = level; i < numberOfLevels - 1; i++)
                lock (voxel0.InnerVoxels[i])
                    voxel0.InnerVoxels[i].RemoveDescendants(voxel.ID, level);
            if (checkParentFull)
            {
                if (level == 1)
                {
                    //What if this is the last voxel to be added that makes the parent full?
                    // This is checked differently for the level-1 than the others. The code could be
                    // combined but it would be less efficient.
                    var makeVoxelFull =
                    (voxel0.InnerVoxels[0].Count ==
                     voxelsInParent[level] //the following All statement is slow. First, just
                                           // check if its worth counting to see if all are Full
                     && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full));
                    if (makeVoxelFull) ChangeVoxelToFull(voxel0, checkParentFull);
                }
                else
                {
                    // for the remaining voxellevels, since the hashsets are combined we need to count
                    // what is indeed an immediate descendant of the the parent and see if they are all full
                    var parentID = MakeParentVoxelID(voxel.ID, level - 1);
                    var makeVoxelFull = false;
                    lock (voxel0.InnerVoxels[voxel.Level - 1])
                        makeVoxelFull = voxel0.InnerVoxels[voxel.Level - 1].Count >= voxelsInParent[level];
                    if (makeVoxelFull)
                        lock (voxel0.InnerVoxels[voxel.Level - 1])
                            makeVoxelFull =
                                (voxel0.InnerVoxels[voxel.Level - 1]
                                     .CountDescendants(parentID, level - 1, VoxelRoleTypes.Full) == voxelsInParent[level]);
                    if (makeVoxelFull) ChangeVoxelToFull(voxel0.InnerVoxels[level - 1 - 1].GetVoxel(parentID), checkParentFull);
                }
            }
            return voxel;
        }

        private IVoxel ChangeEmptyVoxelToPartial(long ID, int level)
        {
            IVoxel voxel;
            if (level == 0)
            {
                //adding a new level-0 voxel is fairly straightforward
                voxel = new Voxel_Level0_Class(ID, VoxelRoleTypes.Partial, this);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.AddOrReplace(voxel);
                return voxel;
            }

            // for the lower levels, first get or make the level-0 voxel (next7 lines)
            var thisIDwoFlags = Constants.ClearFlagsFromID(ID);
            var id0 = MakeParentVoxelID(thisIDwoFlags, 0);
            Voxel_Level0_Class voxel0;
            lock (voxelDictionaryLevel0)
                if (!voxelDictionaryLevel0.Contains(id0))
                    voxel0 = (Voxel_Level0_Class)ChangeEmptyVoxelToPartial(id0, 0);
                else
                    voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            // make the new Voxel, and add it to the proper hashset
            voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), this);
            lock (voxel0.InnerVoxels[level - 1])
                voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            // well, we don't need to check if parent is full but we may need to create the parent
            if (level > 1)
            {
                // for the remaining voxellevels, we also need to check if the parent has been created
                var parentID = MakeParentVoxelID(voxel.ID, level - 1);
                IVoxel parentVoxel = null;
                lock (voxel0.InnerVoxels[level - 2])
                    parentVoxel = voxel0.InnerVoxels[level - 2].GetVoxel(parentID);
                if (parentVoxel == null) ChangeEmptyVoxelToPartial(parentID, level - 1);
            }
            return voxel;
        }

        private IVoxel ChangeVoxelToPartial(IVoxel voxel, bool addAllDescendants)
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
                lock (voxel0)
                {
                    voxel0.Role = VoxelRoleTypes.Partial;
                    if (addAllDescendants) AddAllDescendants(Constants.ClearFlagsFromID(voxel0.ID), level, voxel0);
                }
                return voxel0;
            }
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
                voxel = new Voxel(Constants.ClearFlagsFromID(voxel.ID) +
                                  Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), this);
                lock (voxel0.InnerVoxels[level - 1])
                    voxel0.InnerVoxels[level - 1].AddOrReplace(voxel);
            }
            if (addAllDescendants && this.numberOfLevels - 1 != level)
                AddAllDescendants(Constants.ClearFlagsFromID(voxel.ID), level, voxel0);
            return voxel;  //if at the lowest level or
        }

        List<IVoxel> AddAllDescendants(long startingID, int level, int shortDimension = -1, int numShortLayers = -1)
        {
            var limits = new[] { voxelsPerSide[level + 1], voxelsPerSide[level + 1], voxelsPerSide[level + 1] };
            if (shortDimension >= 0) limits[shortDimension] = numShortLayers;
            var descendants = new List<IVoxel>();
            var xShift = 1L << (4 + singleCoordinateShifts[level + 1]); //finding the correct multiplier requires adding up all the bits used in current levels
            var yShift = xShift << 20; //once the xShift is known, the y and z shifts are just 20 bits over
            var zShift = yShift << 20;
            for (int i = 0; i < limits[0]; i++)
                for (int j = 0; j < limits[1]; j++)
                    for (int k = 0; k < limits[2]; k++)
                        descendants.Add(new Voxel(startingID
                                                       + (i * xShift) + (j * yShift) + (k * zShift)
                                                       + Constants.SetRoleFlags(level + 1, VoxelRoleTypes.Full, true), this));
            return descendants;
        }
        void AddAllDescendants(long startingID, int level, Voxel_Level0_Class voxel0, int shortDimension = -1, int numShortLayers = -1)
        {
            var lowerLevelVoxels = AddAllDescendants(startingID, level, shortDimension, numShortLayers);
            lock (voxel0.InnerVoxels[level])
                voxel0.InnerVoxels[level].AddRange(lowerLevelVoxels);
        }
        internal IEnumerable<IVoxel> GetChildVoxelsInner(IVoxel parent)
        {
            if (parent == null) return voxelDictionaryLevel0;
            if (parent.Level == numberOfLevels - 1) return null;
            Voxel_Level0_Class level0Parent;
            if (parent is Voxel_Level0_Class)
            {
                level0Parent = (Voxel_Level0_Class)parent;
                return level0Parent.InnerVoxels[0];
            }
            // else the parent is level 1, 2, or 3
            level0Parent = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(parent.ID);
            IEnumerable<IVoxel> voxels;
            lock (level0Parent.InnerVoxels[parent.Level])
                voxels = level0Parent.InnerVoxels[parent.Level].GetDescendants(parent.ID, parent.Level);
            return voxels;
        }
        #endregion

        public void UpdateProperties(int level = -1)
        {
            _totals = new long[2 * numberOfLevels];
            _totals[0] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Full);
            _totals[1] = voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Partial);
            for (int i = 1; i < numberOfLevels; i++)
            {
                _totals[2 * i] = voxelDictionaryLevel0.Sum(dict => CountVoxels((Voxel_Level0_Class)dict, i, VoxelRoleTypes.Full));
                _totals[2 * i + 1] = voxelDictionaryLevel0.Sum(dict => CountVoxels((Voxel_Level0_Class)dict, i, VoxelRoleTypes.Partial));
            };
            Volume = 0.0;
            for (int i = 0; i < numberOfLevels; i++)
                Volume += Math.Pow(VoxelSideLengths[i], 3) * _totals[2 * i];
            Volume += Math.Pow(VoxelSideLengths[numberOfLevels - 1], 3) * _totals[2 * (numberOfLevels - 1) + 1];
            _count = _totals.Sum();
        }

        private long CountVoxels(Voxel_Level0_Class voxel0, int level, VoxelRoleTypes role)
        {
            return voxel0.InnerVoxels[level - 1].Count(v => v.Role == role);
        }

        internal double[] GetRealCoordinates(int level, params int[] indices)
        {
            return indices.multiply(VoxelSideLengths[level]).add(Offset);
        }
    }
}
