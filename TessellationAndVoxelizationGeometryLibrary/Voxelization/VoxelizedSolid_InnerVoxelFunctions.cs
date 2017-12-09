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
            if (!(voxelbase is VoxelWithTessellationLinks)) return;
            var voxel = (VoxelWithTessellationLinks)voxelbase;
            if (voxel.TessellationElements == null) voxel.TessellationElements = new List<TessellationBaseClass>();
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
                        //voxel = new Voxel_Level0_Class(???);
                        throw new NotImplementedException("This was not implemented because"
                                                          + " I wasn't sure it'd ever happen and the input arguments needed for the" +
                                                          "Voxel_Level0_Class are not readily available here.");
                    if (!voxelDictionaryLevel0.ContainsKey(voxel.ID))
                        voxelDictionaryLevel0.Add(voxel.ID, (Voxel_Level0_Class)voxel);
                }
                else
                {
                    ((Voxel_Level0_Class)voxel).HighLevelVoxels.Clear();
                    foreach (var nextLevelVoxel in ((Voxel_Level0_Class)voxel).NextLevelVoxels)
                        voxelDictionaryLevel1.Remove(nextLevelVoxel);
                    ((Voxel_Level0_Class)voxel).NextLevelVoxels.Clear();
                }
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
            }
            else if (voxel.Level == 1)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;
                if (voxel.Role == VoxelRoleTypes.Empty)
                {
                    if (!(voxel is Voxel_Level1_Class))
                        //voxel = new Voxel_Level1_Class(???);
                        throw new NotImplementedException("This was not implemented because"
                                                          + " I wasn't sure it'd ever happen and the input arguments needed for the" +
                                                          "Voxel_Level1_Class are not readily available here.");
                    if (!voxelDictionaryLevel1.ContainsKey(voxel.ID))
                    {
                        voxelDictionaryLevel1.Add(voxel.ID, (Voxel_Level1_Class)voxel);
                        voxel0.NextLevelVoxels.Add(voxel.ID);
                    }
                }
                else  //then it was partial
                    voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Full;
                if (voxel0.NextLevelVoxels.Count(v => voxelDictionaryLevel1[v].Role == VoxelRoleTypes.Full) == 4096)
                    MakeVoxelFull(voxel0);
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;

                voxel0.HighLevelVoxels.Remove(voxel.ID);
                if (voxel.Role == VoxelRoleTypes.Partial)
                    voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Full));
                if (voxel0.HighLevelVoxels.Count(isFullLevel2) == 4096)
                    MakeVoxelFull(GetParentVoxel(voxel));
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isFullLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2))) return;
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                        MakeVoxelPartial(new Voxel(id2));

                voxel0.HighLevelVoxels.Remove(voxel.ID);
                if (voxel.Role == VoxelRoleTypes.Partial)
                    voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskLevel4) == thisIDwoFlags);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial, VoxelRoleTypes.Full));
                if (GetSiblingVoxels(voxel).Count(v => isFullLevel3(v.ID)) == 4096)
                    MakeVoxelFull(GetParentVoxel(voxel));
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) return;

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) return;

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isFullLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2))) return;
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2));

                var id3 = MakeContainingVoxelID(thisIDwoFlags, 3);
                if (!isFullLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3))) return;
                if (!isPartialLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3)))
                    MakeVoxelPartial(new Voxel(id3));


                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                                               VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full));
                if (GetSiblingVoxels(voxel).Count(v => isFullLevel4(v.ID)) == 4096)
                    MakeVoxelFull(GetParentVoxel(voxel));
            }
        }

        private void MakeVoxelEmpty(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Empty) return;
            if (voxel.Level == 0)
            {
                if (voxel.Role == VoxelRoleTypes.Partial)
                    foreach (var nextLevelVoxel in ((Voxel_Level0_Class)voxel).NextLevelVoxels)
                        voxelDictionaryLevel1.Remove(nextLevelVoxel);
                voxelDictionaryLevel0.Remove(voxel.ID);
            }
            else if (voxel.Level == 1)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.NextLevelVoxels.Remove(thisIDwoFlags);
                if (!voxel0.NextLevelVoxels.Any()) MakeVoxelEmpty(voxel0);
                else if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                    voxelDictionaryLevel1.Remove(voxel.ID);
                }
            }
            else
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
                if (!GetSiblingVoxels(voxel).Any()) MakeVoxelEmpty(GetParentVoxel(voxel));
                if (voxel.Role == VoxelRoleTypes.Partial)
                {
                    if (voxel.Level == 2)
                        voxel0.HighLevelVoxels.RemoveWhere(
                            id => (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
                    else if (voxel.Level == 3)
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
                    //voxel = new Voxel_Level0_Class(???);
                    throw new NotImplementedException("This was not implemented because"
                                                      + " I wasn't sure it'd ever happen and the input arguments needed for the" +
                                                      "Voxel_Level0_Class are not readily available here.");
                if (!voxelDictionaryLevel0.ContainsKey(voxel.ID))
                    voxelDictionaryLevel0.Add(voxel.ID, (Voxel_Level0_Class)voxel);
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Partial;
            }
            else if (voxel.Level == 1)
            {
                var id0 = MakeContainingVoxelID(voxel.ID, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);
                if (!(voxel is Voxel_Level1_Class))
                    //voxel = new Voxel_Level1_Class(???);
                    throw new NotImplementedException("This was not implemented because"
                                                      + " I wasn't sure it'd ever happen and the input arguments needed for the" +
                                                      "Voxel_Level1_Class are not readily available here.");
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
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);

                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial, VoxelRoleTypes.Partial));
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2));

                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial,
                                               VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial));
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var id0 = MakeContainingVoxelID(thisIDwoFlags, 0);
                if (!voxelDictionaryLevel0.ContainsKey(id0)) MakeVoxelPartial(new Voxel(id0));
                var voxel0 = voxelDictionaryLevel0[id0];
                if (voxel0.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel0);

                var id1 = MakeContainingVoxelID(thisIDwoFlags, 1);
                if (!voxelDictionaryLevel1.ContainsKey(id1)) MakeVoxelPartial(new Voxel(id1));
                var voxel1 = voxelDictionaryLevel1[id0];
                if (voxel1.Role == VoxelRoleTypes.Full) MakeVoxelPartial(voxel1);

                var id2 = MakeContainingVoxelID(thisIDwoFlags, 2);
                if (!isPartialLevel2(voxel0.HighLevelVoxels.GetFullVoxelID(id2)))
                    MakeVoxelPartial(new Voxel(id2));

                var id3 = MakeContainingVoxelID(thisIDwoFlags, 3);
                if (!isPartialLevel3(voxel0.HighLevelVoxels.GetFullVoxelID(id3)))
                    MakeVoxelPartial(new Voxel(id3));

                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(VoxelRoleTypes.Partial,
                                               VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial));
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
    }
}