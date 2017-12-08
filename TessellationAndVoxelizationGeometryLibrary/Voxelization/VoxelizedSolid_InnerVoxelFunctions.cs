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
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Full;
                ((Voxel_Level0_Class)voxel).HighLevelVoxels.Clear();
                foreach (var nextLevelVoxel in ((Voxel_Level0_Class)voxel).NextLevelVoxels)
                    voxelDictionaryLevel1.Remove(nextLevelVoxel);
                ((Voxel_Level0_Class)voxel).NextLevelVoxels.Clear();
            }
            else if (voxel.Level == 1)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Full;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.NextLevelVoxels.Remove(thisIDwoFlags);
                voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Full
                }));
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskLevel4) == thisIDwoFlags);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Full
                }));
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Full
                }));
            }
        }

        private void MakeVoxelFull(long id)
        {
            throw new NotImplementedException();
        }

        private void MakeVoxelPartial(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Partial) return;
            if (voxel.Level == 0)
            {
                ((Voxel_Level0_Class)voxel).Role = VoxelRoleTypes.Partial;
            }
            else if (voxel.Level == 1)
            {
                ((Voxel_Level1_Class)voxel).Role = VoxelRoleTypes.Partial;
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial
                }));
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial
                }));
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
                voxel0.HighLevelVoxels.Add(thisIDwoFlags + SetRoleFlags(new[]
                {
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Partial
                }));
            }
        }

        private void MakeVoxelPartial(long id)
        {
            throw new NotImplementedException();
        }



        //todo: this function and next. we are emptying a voxel and any children it has, but there are two more
        //cases that need to be included: 1) we empty the last voxel of the parent (actually, this is already 
        //done for level1), and 2)we are emptying a voxel but the parent is currently full. these functions can
        //and should recurse (again just like level 1)
        private void MakeVoxelEmpty(IVoxel voxel)
        {
            if (voxel.Role == VoxelRoleTypes.Empty) return;
            if (voxel.Level == 0)
            {
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
                else
                {
                    voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                    voxelDictionaryLevel1.Remove(voxel.ID);
                }
            }
            else if (voxel.Level == 2)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskAllButLevel01and2) == thisIDwoFlags);
            }
            else if (voxel.Level == 3)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(id => (id & Constants.maskLevel4) == thisIDwoFlags);
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = voxel.ID & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(voxel.ID);
            }
        }

        private void MakeVoxelEmpty(long id)
        {
            var flags = GetRoleFlags(id);
            var role = flags.Last();
            var level = flags.Length - 1;
            if (role == VoxelRoleTypes.Empty) return;
            if (level == 0)
            {
                var voxel = voxelDictionaryLevel0[id];
                foreach (var nextLevelVoxel in voxel.NextLevelVoxels)
                    voxelDictionaryLevel1.Remove(nextLevelVoxel);
                voxelDictionaryLevel0.Remove(id);
            }
            else if (level == 1)
            {
                var thisIDwoFlags = id & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.NextLevelVoxels.Remove(thisIDwoFlags);
                if (!voxel0.NextLevelVoxels.Any()) MakeVoxelEmpty(voxel0);
                else
                {
                    voxel0.HighLevelVoxels.RemoveWhere(vx => (vx & Constants.maskAllButLevel0and1) == thisIDwoFlags);
                    voxelDictionaryLevel1.Remove(id);
                }
            }
            else if (level == 2)
            {
                var thisIDwoFlags = id & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(vx => (vx & Constants.maskAllButLevel01and2) == thisIDwoFlags);
            }
            else if (level == 3)
            {
                var thisIDwoFlags = id & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.RemoveWhere(vx => (vx & Constants.maskLevel4) == thisIDwoFlags);
            }
            else //if (voxel.Level == 4)
            {
                var thisIDwoFlags = id & Constants.maskOutFlags;
                var voxel0 = voxelDictionaryLevel0[MakeContainingVoxelID(thisIDwoFlags, 0)];
                voxel0.HighLevelVoxels.Remove(id);
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