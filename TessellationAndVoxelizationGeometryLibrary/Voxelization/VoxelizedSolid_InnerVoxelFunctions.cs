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

        #region Add/Remove Functions
        internal void Add(Voxel_Level0_Class voxel, long voxelID, int level = -1)
        {
            if (voxelID < 0 || level >= 2)
            {
                if (voxel.HighLevelVoxels.Contains(voxelID)) return;
                lock (voxel.HighLevelVoxels) voxel.HighLevelVoxels.Add(voxelID);

                //todo: need to figure out how to check if a level 1, 2, or 3 parent gets full
            }
            else if ((voxelID < 4611686018427387904 && level == -1) || level == 0)
                throw new ArgumentException("Attempting to add a level 0 voxel to another level 0 voxel.");
            else
            {
                if (voxel.NextLevelVoxels.Contains(voxelID)) return;
                if (voxel.NextLevelVoxels.Count == 4095)
                {
                    voxel.Role = VoxelRoleTypes.Full;
                    // also have to change the key in the level-0 dictionary. to do this, we need to add and remove
                    voxel.HighLevelVoxels.Clear();
                    foreach (var nextLevelVoxel in voxel.NextLevelVoxels)
                        voxelDictionaryLevel1[nextLevelVoxel].Role = VoxelRoleTypes.Empty;
                    voxel.NextLevelVoxels.Clear();
                }
                else lock (voxel.NextLevelVoxels) voxel.NextLevelVoxels.Add(voxelID);
            }
        }

        internal bool Remove(Voxel_Level0_Class voxel, long voxelID)
        {
            if (voxelID < 4611686018427387904 && voxelID >= 0)
                throw new ArgumentException("Attempting to remove a level 0 voxel to another level 0 voxel.");
            if (voxel.Role == VoxelRoleTypes.Empty) return true;
            if (voxel.Role == VoxelRoleTypes.Full)
                throw new NotImplementedException(
                    "removing a voxel from a full means having to create all the sub-voxels minus 1.");
            if (voxelID < 0)
            {
                if (voxel.HighLevelVoxels.Count == 1 && voxel.HighLevelVoxels.Contains(voxelID))
                {
                    //then this is the last subvoxel, so this goes empty
                    voxel.HighLevelVoxels = null;
                    voxel.Role = VoxelRoleTypes.Empty;
                    return true;
                }
                if (voxel.HighLevelVoxels.Any())
                    return voxel.HighLevelVoxels.Remove(voxelID);
                //todo: need to figure out how to check if a level 1, 2, or 3 parent gets full

                throw new NotImplementedException("even though there are no high level voxels, we need to check next level, and create the subvoxels");
            }
            if (voxel.NextLevelVoxels.Count == 1 && voxel.NextLevelVoxels.Contains(voxelID))
            {
                //then this is the last subvoxel, so this goes empty
                voxel.NextLevelVoxels = null;
                voxel.HighLevelVoxels = null;
                voxel.Role = VoxelRoleTypes.Empty;
                return true;
            }
            if (voxel.NextLevelVoxels.Any())
                return voxel.NextLevelVoxels.Remove(voxelID);
            throw new NotImplementedException(
                "removing a voxel from a full means having to create all the sub-voxels minus 1.");
        }

        internal bool Contains(Voxel_Level0_Class voxel, long voxelID, int level = -1)
        {
            if (voxelID < 0 || level >= 2)
            {
                if (voxel.HighLevelVoxels == null) return false;
                return voxel.HighLevelVoxels.Contains(voxelID);
            }
            if ((voxelID < 4611686018427387904 && level == -1) || level == 0)
                return false;
            if (voxel.NextLevelVoxels == null) return false;
            return voxel.NextLevelVoxels.Contains(voxelID);
        }
        #endregion



        private void MakeVoxelFull(IVoxel neighbor)
        {
            throw new NotImplementedException();
        }

        private void FinalizeChange(int level = -1)
        {
            #region  Clear out empties

            var voxel0Keys = voxelDictionaryLevel0.Keys.ToList();
            foreach (var key in voxel0Keys)
            {
                var voxel0Value = voxelDictionaryLevel0[key];
                if (voxel0Value.Role == VoxelRoleTypes.Empty)
                {
                    foreach (var voxel1Key in voxel0Value.NextLevelVoxels)
                        voxelDictionaryLevel1.Remove(voxel1Key);
                    voxelDictionaryLevel0.Remove(key);
                }
                else
                {
                    if (voxel0Value.HighLevelVoxels != null) continue;
                    var highLevelEmptyFlags =
                        new List<long> { 0L << 60, 1L << 60, 4L << 60, 7L << 60, 10L << 60, 13L << 60 };
                    voxel0Value.HighLevelVoxels.RemoveWhere(vx =>
                        highLevelEmptyFlags.Contains(vx & Constants.maskAllButFlags));
                }
            }
            var voxel1Keys = voxelDictionaryLevel1.Keys.ToList();
            foreach (var key in voxel1Keys)
            {
                var voxel1Value = voxelDictionaryLevel1[key];
                if (voxel1Value.Role == VoxelRoleTypes.Empty)
                {
                    voxelDictionaryLevel1.Remove(key);
                    //todo:how to be sure to remove level 2-4 voxels
                }
             
            }



            #endregion

            #region update time consuming properties
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
            #endregion

        }
    }
}