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
        private void ChangeVoxelToEmpty(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Empty) return;
            if (voxel.Level == 0)
                // no need to do anything with the HigherLevelVoxels as deleting this Voxel, deletes
                // the only reference to these higher voxels.
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Remove(voxel.ID);
            else
            {
                var thisIDwoFlags = Constants.ClearFlagsFromID(voxel.ID, false);
                var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(Constants.MakeParentVoxelID(thisIDwoFlags, 0));
                lock (voxel0.InnerVoxels[voxel.Level - 1])
                    voxel0.InnerVoxels[voxel.Level - 1].Remove(thisIDwoFlags);
                if (voxel0.InnerVoxels[voxel.Level - 1].Count == 0) ChangeVoxelToEmpty(GetParentVoxel(voxel));
                else if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    for (int i = voxel.Level; i < discretizationLevel; i++)
                    {
                        lock (voxel0.InnerVoxels[i])
                            voxel0.InnerVoxels[i]
                                .RemoveWhere(vx => Constants.MakeParentVoxelID(vx.ID, voxel.Level) == thisIDwoFlags);
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
            voxel = level == 1
                ? (IVoxel)new Voxel_Level1_Class(ID, VoxelRoleTypes.Full, this)
                : (IVoxel)new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full));
            lock (voxel0.InnerVoxels[level - 1]) voxel0.InnerVoxels[level - 1].Add(voxel);
            if (level == 1)
            {
                lock (voxel0.InnerVoxels[0])
                {
                    if (voxel0.InnerVoxels[0].Count == 4096
                        && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full))
                        ChangePartialVoxelToFull(voxel0);
                }
            }
            else if (level > 1 && voxel0.InnerVoxels[voxel.Level - 1].Count >= 4096)
            {
                var parentID = Constants.MakeParentVoxelID(voxel.ID, level - 1);

                lock (voxel0.InnerVoxels[level - 1])
                {
                    if (voxel0.InnerVoxels[voxel.Level - 1].Count(vx =>
                            Constants.MakeParentVoxelID(vx.ID, level - 1) == parentID
                            && vx.Role == VoxelRoleTypes.Full) == 4096)
                        ChangePartialVoxelToFull(voxel0.InnerVoxels[level - 2].GetVoxel(parentID));
                }
            }
            return voxel;
        }


        private IVoxel ChangePartialVoxelToFull(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Full)
                throw new ArgumentException("input voxel is already full.");
            if (voxel.Level == 0)
            {
                lock (voxel)
                    ((Voxel_Level0_Class)voxel).InnerVoxels = null;
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
                return voxel;
            }
            var level = voxel.Level;
            var thisIDwoFlags = Constants.ClearFlagsFromID(voxel.ID);
            var id0 = Constants.MakeParentVoxelID(thisIDwoFlags, 0);
            var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            if (level == 1) ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Full;
            else
            {
                lock (voxel0.InnerVoxels[level - 1])
                {
                    voxel0.InnerVoxels[level - 1].Remove(voxel);
                    voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full));
                    voxel0.InnerVoxels[level - 1].Add(voxel);
                }
            }
            for (int i = level; i < discretizationLevel; i++)
            {
                lock (voxel0.InnerVoxels[i])
                    voxel0.InnerVoxels[i]
                        .RemoveWhere(vx =>
                            Constants.MakeParentVoxelID(vx.ID, level) == thisIDwoFlags);
            }
            if (level == 1)
            {
                lock (voxel0.InnerVoxels[0])
                {
                    if (voxel0.InnerVoxels[0].Count == 4096
                               && voxel0.InnerVoxels[0].All(v => v.Role == VoxelRoleTypes.Full))
                        ChangePartialVoxelToFull(voxel0);
                }
            }
            else if (level > 1 && voxel0.InnerVoxels[voxel.Level - 1].Count >= 4096)
            {
                var parentID = Constants.MakeParentVoxelID(voxel.ID, level - 1);

                lock (voxel0.InnerVoxels[level - 1])
                {
                    if (voxel0.InnerVoxels[voxel.Level - 1].Count(vx =>
                            Constants.MakeParentVoxelID(vx.ID, level - 1) == parentID
                            && vx.Role == VoxelRoleTypes.Full) == 4096)
                        ChangePartialVoxelToFull(voxel0.InnerVoxels[level - 2].GetVoxel(parentID));
                }
            }
            return voxel;
        }

        private IVoxel ChangeEmptyVoxelToPartial(long ID, int level)
        {
            IVoxel voxel;
            if (level == 0)
            {
                voxel = new Voxel_Level0_Class(ID, VoxelRoleTypes.Partial, this);
                if (discretizationLevel >= 1)
                    ((Voxel_Level0_Class)voxel).InnerVoxels[0] = new VoxelHashSet(new VoxelComparerCoarse(), this);
                for (int i = 2; i < discretizationLevel; i++)
                    ((Voxel_Level0_Class)voxel).InnerVoxels[i] = new VoxelHashSet(new VoxelComparerFine(), this);
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
            voxel = level == 1
                ? (IVoxel)new Voxel_Level1_Class(ID, VoxelRoleTypes.Partial, this)
                : (IVoxel)new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial));
            lock (voxel0.InnerVoxels[level - 1]) voxel0.InnerVoxels[level - 1].Add(voxel);
            return voxel;
        }
        private IVoxel ChangeFullVoxelToPartial(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Partial)
                throw new ArgumentException("input voxel is already partial.");
            if (voxel.Level == 0)
            {
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Partial;
                if (discretizationLevel > 0)
                {
                    var level1Voxels = new Voxel_Level1_Class[4096];
                    Parallel.For(0, 16, i =>
                    {
                        for (int j = 0; j < 16; j++)
                            for (int k = 0; k < 16; k++)
                                level1Voxels[256 * i + 16 * j + k] = new Voxel_Level1_Class(voxel.ID + (i * 65536L) +
                                    (j * 68719476736) +
                                    (k * 72057594037927936L),
                                    VoxelRoleTypes.Full, this);
                    });
                    ((Voxel_Level0_Class)voxel).InnerVoxels = new VoxelHashSet[discretizationLevel];
                    ((Voxel_Level0_Class)voxel).InnerVoxels[0] = new VoxelHashSet(new VoxelComparerCoarse(), this, level1Voxels);
                }
                return voxel;
            }
            var level = voxel.Level;
            var thisIDwoFlags = Constants.ClearFlagsFromID(voxel.ID);
            var id0 = Constants.MakeParentVoxelID(thisIDwoFlags, 0);
            var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            if (voxel0.Role == VoxelRoleTypes.Full) ChangeFullVoxelToPartial(voxel0);
            if (level == 1) ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Partial;
            else
            {
                lock (voxel0.InnerVoxels[level - 1])
                {
                    voxel0.InnerVoxels[level - 1].Remove(voxel);
                    voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial));
                    voxel0.InnerVoxels[level - 1].Add(voxel);
                }
            }
            var lowerLevelVoxels = new List<IVoxel>();
            var xShift = 1L << 4 + 4 * (4 - level);
            var yShift = xShift << 20;
            var zShift = yShift << 20;
            for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                    for (int k = 0; k < 16; k++)
                        lowerLevelVoxels.Add(new Voxel(thisIDwoFlags
                            + (i * xShift) + (j * yShift) + (k * zShift) + Constants.SetRoleFlags(level + 1, VoxelRoleTypes.Full, true)));
            if (this.discretizationLevel > level)
                lock (voxel0.InnerVoxels[level + 1])
                    voxel0.InnerVoxels[level + 1].AddRange(lowerLevelVoxels);
            return voxel;
        }

        private IVoxel ChangeFullVoxelToPartialNEW(IVoxel voxel, bool populateSubVoxels = true)
        {
            if (voxel.Role == VoxelRoleTypes.Partial)
                throw new ArgumentException("input voxel is already partial.");
            if (voxel.Level == 0)
            {
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Partial;
                if (discretizationLevel > 0)
                {
                    ((Voxel_Level0_Class)voxel).InnerVoxels = new VoxelHashSet[discretizationLevel];
                    if (populateSubVoxels)
                    {
                        var level1Voxels = new Voxel_Level1_Class[4096];
                        Parallel.For(0, 16, i =>
                        {
                            for (int j = 0; j < 16; j++)
                                for (int k = 0; k < 16; k++)
                                    level1Voxels[256 * i + 16 * j + k] = new Voxel_Level1_Class(voxel.ID + (i * 65536L) +
                                                                                            (j * 68719476736) +
                                                                                            (k * 72057594037927936L),
                                    VoxelRoleTypes.Full, this);
                        });
                        ((Voxel_Level0_Class)voxel).InnerVoxels[0] =
                            new VoxelHashSet(new VoxelComparerCoarse(), this, level1Voxels);
                    }
                    else ((Voxel_Level0_Class)voxel).InnerVoxels[0] = new VoxelHashSet(new VoxelComparerCoarse(), this);
                }
                return voxel;
            }
            var level = voxel.Level;
            var thisIDwoFlags = Constants.ClearFlagsFromID(voxel.ID);
            var id0 = Constants.MakeParentVoxelID(thisIDwoFlags, 0);
            var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(id0);
            if (voxel0.Role == VoxelRoleTypes.Full) ChangeFullVoxelToPartial(voxel0);
            if (level == 1) ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Partial;
            else
            {
                lock (voxel0.InnerVoxels[level - 1])
                {
                    voxel0.InnerVoxels[level - 1].Remove(voxel);
                    voxel = new Voxel(thisIDwoFlags + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial));
                    voxel0.InnerVoxels[level - 1].Add(voxel);
                }
            }
            if (this.discretizationLevel <= level) return voxel;
            if (voxel0.InnerVoxels[level] == null)
                voxel0.InnerVoxels[level] = new VoxelHashSet(new VoxelComparerFine(), this);
            if (populateSubVoxels)
            {
                var lowerLevelVoxels = new List<IVoxel>();
                var xShift = 1L << 4 + 4 * (4 - level);
                var yShift = xShift << 20;
                var zShift = yShift << 20;
                for (int i = 0; i < 16; i++)
                    for (int j = 0; j < 16; j++)
                        for (int k = 0; k < 16; k++)
                            lowerLevelVoxels.Add(new Voxel(thisIDwoFlags
                                                           + (i * xShift) + (j * yShift) + (k * zShift)
                                                           + Constants.SetRoleFlags(level + 1, VoxelRoleTypes.Full, true)));
                lock (voxel0.InnerVoxels[level])
                    voxel0.InnerVoxels[level].AddRange(lowerLevelVoxels);
            }
            return voxel;
        }
        #endregion

        private void UpdateProperties(int level = -1)
        {
            _totals = new[]
                        {
                            voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Full),
                            voxelDictionaryLevel0.Count(v => v.Role == VoxelRoleTypes.Partial),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,1,VoxelRoleTypes.Full)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,1,VoxelRoleTypes.Partial)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,2,VoxelRoleTypes.Full)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,2,VoxelRoleTypes.Partial)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,3,VoxelRoleTypes.Full)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,4,VoxelRoleTypes.Partial)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,4,VoxelRoleTypes.Full)),
                            voxelDictionaryLevel0.Sum(dict => CountVoxels(dict,4,VoxelRoleTypes.Partial))
                        };
            Volume = 0.0;
            for (int i = 0; i <= discretizationLevel; i++)
                Volume += Math.Pow(VoxelSideLengths[i], 3) * _totals[2 * i];
            Volume += Math.Pow(VoxelSideLengths[discretizationLevel], 3) * _totals[2 * discretizationLevel + 1];
            _count = _totals.Sum();
        }

        private long CountVoxels(IVoxel dict, int level, VoxelRoleTypes role)
        {
            var innerVoxels = ((Voxel_Level0_Class)dict).InnerVoxels;
            if (innerVoxels == null || innerVoxels.Length < level || innerVoxels[level - 1] == null) return 0L;
            return innerVoxels[level - 1].Count(v => v.Role == role);
        }

        internal double[] GetRealCoordinates(long ID, int level)
        {
            return GetRealCoordinates(level, Constants.GetCoordinateIndices(ID, level));
            //var indices = Constants.GetCoordinateIndices(ID, level);
            //return indices.multiply(VoxelSideLengths[level]).add(Offset);
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
        internal static bool isLevel1(long ID)
        {
            var id = ID & 31;
            return id < 20 && id >= 16;
        }
        internal static bool isLevel0(long ID)
        {
            var id = ID & 31;
            return id < 4;
        }
        internal static bool isFullLevel2(long ID)
        {
            return isLevel2(ID) && isFull(ID);
        }
        internal static bool isPartialLevel2(long ID)
        {
            return isLevel2(ID) && isPartial(ID);
        }
        internal static bool isFullLevel3(long ID)
        {
            return isLevel3(ID) && isFull(ID);
        }
        internal static bool isPartialLevel3(long ID)
        {
            return isLevel3(ID) && isPartial(ID);
        }
        internal static bool isFullLevel4(long ID)
        {
            return isLevel4(ID) && isFull(ID);
        }
        internal static bool isPartialLevel4(long ID)
        {
            return isLevel4(ID) && isPartial(ID);
        }
        #endregion
    }
}