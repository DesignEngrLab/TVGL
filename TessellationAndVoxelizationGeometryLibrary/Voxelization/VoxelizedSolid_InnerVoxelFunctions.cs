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
        #region Tessellation References functions
        internal static void Add(IVoxel voxelbase, TessellationBaseClass tsObject)
        {
            if (tsObject == null) return;
            if (!(voxelbase is VoxelWithTessellationLinks)) return;
            var voxel = (VoxelWithTessellationLinks)voxelbase;
            if (voxel.TessellationElements == null) voxel.TessellationElements = new HashSet<TessellationBaseClass>();
            else if (voxel.TessellationElements.Contains(tsObject)) return;
            lock (voxel.TessellationElements) voxel.TessellationElements.Add(tsObject);
            tsObject.AddVoxel(voxel);
        }


        internal bool Remove(VoxelWithTessellationLinks voxel, TessellationBaseClass tsObject)
        {
            if (voxel.TessellationElements == null) return false;
            if (voxel.TessellationElements.Count == 1 && voxel.TessellationElements.Contains(tsObject))
            {
                voxel.TessellationElements = null;
                return true;
            }
            return voxel.TessellationElements.Remove(tsObject);
        }

        internal bool Contains(VoxelWithTessellationLinks voxel, TessellationBaseClass tsObject)
        {
            if (voxel.TessellationElements == null) return false;
            return voxel.TessellationElements.Contains(tsObject);
        }
        #endregion

        #region Functions to make voxel full, empty or partial
        private void MakeVoxelFull(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Full) return;
            if (voxel.Level == 0)
            {
                if (voxel.Role == VoxelRoleTypes.Empty) //then you may need to add it
                {
                    if (!(voxel is Voxel_Level0_Class))
                        voxel = new Voxel_Level0_Class(voxel.ID, VoxelRoleTypes.Full, VoxelSideLengths, Offset);
                    lock (voxelDictionaryLevel0)
                        if (!voxelDictionaryLevel0.ContainsKey(voxel.ID))
                            voxelDictionaryLevel0.Add(voxel.ID, (Voxel_Level0_Class)voxel);
                }
                else
                {
                    lock (((Voxel_Level0_Class)voxel).HighLevelVoxels)
                        ((Voxel_Level0_Class)voxel).HighLevelVoxels.Clear();
                    foreach (var nextLevelVoxel in ((Voxel_Level0_Class)voxel).NextLevelVoxels)
                        lock (voxelDictionaryLevel1)
                            voxelDictionaryLevel1.Remove(nextLevelVoxel);
                    lock (((Voxel_Level0_Class)voxel).NextLevelVoxels)
                        ((Voxel_Level0_Class)voxel).NextLevelVoxels.Clear();
                }
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
            }
            else if (voxel.Level == 1)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;
                if (voxel.Role == VoxelRoleTypes.Empty)
                {
                    if (!(voxel is Voxel_Level1_Class))
                        voxel = new Voxel_Level1_Class(voxel.ID, VoxelRoleTypes.Full, VoxelSideLengths, Offset);
                    lock (voxelDictionaryLevel1)
                        if (!voxelDictionaryLevel1.ContainsKey(voxel.ID))
                        {
                            voxelDictionaryLevel1.Add(voxel.ID, (Voxel_Level1_Class)voxel);
                            voxel0.NextLevelVoxels.Add(voxel.ID);
                        }
                }
                else  //then it was partial
                    lock (voxel0.HighLevelVoxels)
                        voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Full;
                if (voxel0.NextLevelVoxels.Count == 4096 
                    && voxel0.NextLevelVoxels.All(v => voxelDictionaryLevel1[v].Role == VoxelRoleTypes.Full))
                    MakeVoxelFull(voxel0);
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;
                var makeParentFull = false;
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    if (voxel.Role == VoxelRoleTypes.Partial)
                        voxel0.HighLevelVoxels.RemoveWhere(id =>
                            (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(2, VoxelRoleTypes.Full));
                    makeParentFull = (voxel0.HighLevelVoxels.Count(isFullLevel2) == 4096);
                }
                if (makeParentFull) MakeVoxelFull(GetParentVoxel(voxel));
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isFullLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2))) return;
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2, 2));
                var makeParentFull = false;
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    if (voxel.Role == VoxelRoleTypes.Partial)
                        voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskLevel4) == thisIDwoFlags);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(3, VoxelRoleTypes.Full));
                    makeParentFull = (GetSiblingVoxels(voxel).Count(v => isFullLevel3(v.ID)) == 4096);
                }
                if (makeParentFull) MakeVoxelFull(GetParentVoxel(voxel));
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isFullLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2))) return;
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2, 2));

                var id3 = MakeContainingVoxelID(thisIDwoFlags, 3);
                if (!isFullLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3))) return;
                if (!isPartialLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3)))
                    MakeVoxelPartial(new Voxel(id3, 3));

                var makeParentFull = false;
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(4, VoxelRoleTypes.Full));
                    makeParentFull = (GetSiblingVoxels(voxel).Count(v => isFullLevel4(v.ID)) == 4096);
                }
                if (makeParentFull) MakeVoxelFull(GetParentVoxel(voxel));
            }
        }

        private void MakeVoxelEmpty(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Empty) return;
            if (voxel.Level == 0)
            {
                if (voxel.Role == VoxelRoleTypes.Partial)
                    lock (voxelDictionaryLevel1)
                        foreach (var nextLevelVoxel in ((Voxel_Level0_Class)voxel).NextLevelVoxels)
                            voxelDictionaryLevel1.Remove(nextLevelVoxel);
                lock (voxelDictionaryLevel0)
                    voxelDictionaryLevel0.Remove(voxel.ID);
            }
            else if (voxel.Level == 1)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                lock (voxel0.NextLevelVoxels)
                    voxel0.NextLevelVoxels.Remove(thisIDwoFlags);
                if (voxel0.NextLevelVoxels.Count == 0) MakeVoxelEmpty(voxel0);
                else if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    lock (voxel0.HighLevelVoxels)
                        voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                    lock (voxelDictionaryLevel1) voxelDictionaryLevel1.Remove(voxel.ID);
                }
            }
            else
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                lock (voxel0.HighLevelVoxels)
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                if (!GetSiblingVoxels(voxel).Any()) MakeVoxelEmpty(GetParentVoxel(voxel));
                if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    if (voxel.Level == 2)
                        lock (voxel0.HighLevelVoxels)
                            voxel0.HighLevelVoxels.RemoveWhere(
                                id => (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
                    else if (voxel.Level == 3)
                        lock (voxel0.HighLevelVoxels)
                            voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskLevel4) == thisIDwoFlags);
                }
            }
        }


        private void MakeVoxelPartial(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Partial) return;
            if (voxel.Level == 0)
            {
                if (!(voxel is Voxel_Level0_Class))
                    voxel = new Voxel_Level0_Class(voxel.ID, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                lock (voxelDictionaryLevel0)
                    if (!voxelDictionaryLevel0.ContainsKey(voxel.ID))
                        voxelDictionaryLevel0.Add(voxel.ID, (Voxel_Level0_Class)voxel);
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Partial;
            }
            else if (voxel.Level == 1)
            {
                var id0 = MakeContainingVoxelID(voxel.ID, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);
                if (!(voxel is Voxel_Level1_Class))
                    voxel = new Voxel_Level1_Class(voxel.ID, VoxelRoleTypes.Partial, VoxelSideLengths, Offset);
                lock (voxelDictionaryLevel1)
                    if (!voxelDictionaryLevel1.ContainsKey(voxel.ID))
                    {
                        voxelDictionaryLevel1.Add(voxel.ID, (Voxel_Level1_Class)voxel);
                        voxel0.NextLevelVoxels.Add(voxel.ID);
                    }
                ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Partial;
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(2, VoxelRoleTypes.Partial));
                }
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2, 2));
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(3, VoxelRoleTypes.Partial));
                }
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0, 0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1, 1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2, 2));

                var id3 = MakeContainingVoxelID(thisIDwoFlags, 3);
                if (!isPartialLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3)))
                    MakeVoxelPartial(new Voxel(id3, 3));
                lock (voxel0.HighLevelVoxels)
                {
                    voxel0.HighLevelVoxels.Remove(voxel.ID);
                    voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(4, VoxelRoleTypes.Partial));
                }
            }
        }




        #endregion
        private void UpdateProperties(int level = -1)
        {
            _count = (long)voxelDictionaryLevel0.Count + (long)(voxelDictionaryLevel1?.Count ?? 0) +
            (long)voxelDictionaryLevel0.Sum(dict => (long)(dict.Value.HighLevelVoxels?.Count ?? 0));
            _totals = new[]
                        {
                            voxelDictionaryLevel0.Values.Count(v => v.Role == VoxelRoleTypes.Full),
                            voxelDictionaryLevel0.Values.Count(v => v.Role == VoxelRoleTypes.Partial),
                            voxelDictionaryLevel1.Values.Count(v => v.Role == VoxelRoleTypes.Full),
                            voxelDictionaryLevel1.Values.Count(v => v.Role == VoxelRoleTypes.Partial),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isFullLevel2)),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isPartialLevel2)),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isFullLevel3)),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isPartialLevel3)),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isFullLevel4)),
                            voxelDictionaryLevel0.Values.Sum(dict => (long)dict.HighLevelVoxels.Count(isPartialLevel4))
                        };
            _volume = 0.0;
            for (int i = 0; i <= discretizationLevel; i++)
                _volume += Math.Pow(VoxelSideLengths[i], 3) * _totals[2 * i];
            _volume += Math.Pow(VoxelSideLengths[discretizationLevel], 3) * _totals[2 * discretizationLevel + 1];
        }

        #region Quick Booleans for IDs
        bool isFull(long ID)
        {
            return (ID & 3) == 3;
        }
        bool isEmpty(long ID)
        {
            return (ID & 3) == 0;
        }
        bool isPartial(long ID)
        {
            var id = ID & 3;
            return id == 1 || id == 2;
        }

        bool isLevel4(long ID)
        {
            return (ID & 15) >= 12;
        }
        bool isLevel3(long ID)
        {
            var id = ID & 15;
            return id < 12 && id >= 8;
        }
        bool isLevel2(long ID)
        {
            var id = ID & 15;
            return id < 8 && id >= 4;
        }
        bool isLevel1(long ID)
        {
            var id = ID & 31;
            return id < 20 && id >= 16;
        }
        bool isLevel0(long ID)
        {
            var id = ID & 31;
            return id < 4;
        }
        bool isFullLevel2(long ID)
        {
            return isLevel2(ID) && isFull(ID);
        }
        bool isPartialLevel2(long ID)
        {
            return isLevel2(ID) && isPartial(ID);
        }
        bool isFullLevel3(long ID)
        {
            return isLevel3(ID) && isFull(ID);
        }
        bool isPartialLevel3(long ID)
        {
            return isLevel3(ID) && isPartial(ID);
        }
        bool isFullLevel4(long ID)
        {
            return isLevel4(ID) && isFull(ID);
        }
        bool isPartialLevel4(long ID)
        {
            return isLevel4(ID) && isPartial(ID);
        }
        #endregion
    }
}