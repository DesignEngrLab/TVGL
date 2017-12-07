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
    public partial class VoxelizedSolid : Solid
    {
        public long Count => _count;
        private long _count;
        public new double Volume => _volume;
        double _volume;

        public long[] GetTotals => _totals;
        long[] _totals;


        bool isFullLevel0(long ID) { return ID >= 2305843009213693952 && ID <= 3458764513820540927; }
        bool isPartialLevel0(long ID) { return ID >= 3458764513820540928 && ID <= 4611686018427387903; }
        bool isFullLevel1(long ID) { return ID >= 5764607523034234880 && ID <= 6917529027641081855; }
        bool isPartialLevel1(long ID) { return ID >= 6917529027641081856 && ID <= 8070450532247928831; }
        bool isFullLevel2(long ID) { return ID <= -8070450532247928833; } //no greater than since that's Long.MinValue and is always true
        bool isPartialLevel2(long ID) { return ID >= -8070450532247928832 && ID <= -6917529027641081857; }
        bool isFullLevel3(long ID) { return ID >= -5764607523034234880 && ID <= -4611686018427387905; }
        bool isPartialLevel3(long ID) { return ID >= -4611686018427387904 && ID <= -3458764513820540929; }
        bool isFullLevel4(long ID) { return ID >= -2305843009213693952 && ID <= -1152921504606846977; }
        bool isPartialLevel4(long ID) { return ID >= -1152921504606846976 && ID <= -1; }

        #region Public Enumerations

        #endregion


        #region Solid Method Overrides (Transforms & Copy)
        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid Copy()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Draft

        public VoxelizedSolid DraftToNewSolid(VoxelDirections direction)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Draft(direction);
            return copy;
        }

        public void Draft(VoxelDirections direction, IVoxel parent = null)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var voxels = GetChildVoxels(parent);
            var layerOfVoxels = new List<IVoxel>[16];

            Parallel.ForEach(voxels, v =>
            //foreach (var voxel0 in voxelDictionaryLevel0.Values)
            {
                var layer = (v.ID >> ((20*(2-dimension))+4*(4 - v.Level))) & 15;
                if (positiveStep) layer = 15 - layer;
                lock (layerOfVoxels[layer])
                    layerOfVoxels[layer].Add(v);
            });
            for (int i = 0; i < 16; i++)
            {
                Parallel.ForEach(layerOfVoxels[i], voxel =>
                {
                    if (voxel.Role == VoxelRoleTypes.Full ||
                        (voxel.Role == VoxelRoleTypes.Partial && voxel.Level == discretizationLevel))
                    {
                        var neighbor = GetNeighbor(voxel, direction, out var neighborHasDifferentParent);
                        while (neighbor?.Role != VoxelRoleTypes.Full)
                        {
                            MakeVoxelFull(neighbor);
                            if (neighborHasDifferentParent) break;
                            neighbor = GetNeighbor(neighbor, direction, out neighborHasDifferentParent);
                        }
                    }
                    else if (voxel.Role == VoxelRoleTypes.Partial)
                        Draft(direction, voxel);
                });
            }
        }

        #endregion
        #region Get Functions

        public IEnumerable<IVoxel> Voxels(VoxelDiscretization upToAndIncludingLevel = VoxelDiscretization.ExtraFine)
        {
            var level = (int)upToAndIncludingLevel;
            if (level > discretizationLevel) level = discretizationLevel;
            foreach (var v in voxelDictionaryLevel0.Values.Where(v => v.Role == VoxelRoleTypes.Full))
                yield return v;
            if (level == 0)
                foreach (var v in voxelDictionaryLevel0.Values.Where(v => v.Role == VoxelRoleTypes.Partial))
                    yield return v;
            if (level >= 1)
                foreach (var v in voxelDictionaryLevel1.Values.Where(v => v.Role == VoxelRoleTypes.Full))
                    yield return v;
            if (level == 1)
                foreach (var v in voxelDictionaryLevel1.Values.Where(v => v.Role == VoxelRoleTypes.Partial))
                    yield return v;
            if (level == 2)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    GetHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -8070450532247928832)))
                    yield return v;
            if (level == 3)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    GetHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880, -4611686018427387904)))
                    yield return v;
            if (level == 4)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    GetHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880, -2305843009213693952, -1152921504606846976)))
                    yield return v;
        }

        public IEnumerable<IVoxel> GetVoxels(VoxelRoleTypes role, int level)
        {
            if (level == 0)
                return voxelDictionaryLevel0.Values.Where(v => v.Role == role);
            if (level == 1)
                return voxelDictionaryLevel1.Values.Where(v => v.Role == role);
            if (level > discretizationLevel) level = discretizationLevel;
            var flags = new VoxelRoleTypes[level];
            for (int i = 0; i < level - 1; i++)
                flags[i] = VoxelRoleTypes.Partial;
            flags[level - 1] = role;
            var targetFlags = SetRoleFlags(flags);
            return voxelDictionaryLevel0.Values.SelectMany(voxDict => GetHighLevelVoxelsFromLevel0(voxDict, targetFlags));
        }

        internal IEnumerable<IVoxel> GetHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel, params long[] targetFlags)
        {
            foreach (var vx in voxel.HighLevelVoxels)
            {
                var flags = vx & -1152921504606846976; //get rid of every but the flags
                if (targetFlags.Contains(flags))
                    yield return new Voxel(vx, discretizationLevel, VoxelSideLengths, Offset);
            }
        }
        public IVoxel GetNeighbor(IVoxel voxel, VoxelDirections direction, out bool neighborHasDifferentParent)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var coordValue =  (voxel.ID >> ((20 * (2 - dimension)) + 4 * (4 - voxel.Level))) & 15;
            neighborHasDifferentParent = ((coordValue == 0 && !positiveStep) || (coordValue == 15 && positiveStep));
            if (voxel.Level == 0 & neighborHasDifferentParent) return null;
            var delta = 1L;
            delta = delta << ((20 *(2- dimension)) + 4*(4 - voxel.Level));
            var newID = (positiveStep) ? voxel.ID + delta : voxel.ID - delta;
            if (voxel.Level == 0)
                return voxelDictionaryLevel0.ContainsKey(newID) ? voxelDictionaryLevel0[newID] : null;
            if (voxel.Level == 1)
                return voxelDictionaryLevel1.ContainsKey(newID) ? voxelDictionaryLevel1[newID] : null;
            var level0ParentID=MakeContainingVoxelID(newID, 0);
            if (!voxelDictionaryLevel0.ContainsKey(level0ParentID)) return null;
            var level0Parent= voxelDictionaryLevel0[level0ParentID];
            if (level0Parent.HighLevelVoxels == null || !level0Parent.HighLevelVoxels.Contains(newID)) return null;
            return level0Parent.HighLevelVoxels.GetVoxel(newID);
        }
        public IVoxel[] GetNeighbors(IVoxel voxel, out bool[] neighborsHaveDifferentParent)
        {
            var neighbors = new IVoxel[6];
            neighborsHaveDifferentParent = new bool[6];
            var i = 0;
            foreach (var direction in Enum.GetValues(typeof(VoxelDirections)))
                neighbors[i++] = GetNeighbor(voxel, (VoxelDirections)direction, out neighborsHaveDifferentParent[i]);
            return neighbors;
        }

        public IVoxel GetParentVoxel(IVoxel child)
        {
            switch (child.Level)
            {
                case 1: return voxelDictionaryLevel0[MakeContainingVoxelID(child.ID, 0)];
                case 2: return voxelDictionaryLevel1[MakeContainingVoxelID(child.ID, 1)];
                case 3:
                    var roles3 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(MakeContainingVoxelID(child.ID, 2) + SetRoleFlags(roles3), this.discretizationLevel, VoxelSideLengths, Offset);
                case 4:
                    var roles4 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(MakeContainingVoxelID(child.ID, 3) + SetRoleFlags(roles4), this.discretizationLevel, VoxelSideLengths, Offset);
                default: return null;
            }

        }
        
        public List<IVoxel> GetChildVoxels(IVoxel parent)
        {
            var voxels = new List<IVoxel>();
            if (parent == null)
                voxels.AddRange(voxelDictionaryLevel0.Values);
            else if (parent is Voxel_Level0_Class)
            {
                var IDs = ((Voxel_Level0_Class)parent).NextLevelVoxels;
                voxels.AddRange(IDs.Select(id => voxelDictionaryLevel1[id]));
            }
            else if (parent is Voxel_Level1_Class)
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                voxels.AddRange(IDs.Where(v => isPartialLevel2(v) || isFullLevel2(v))
                    .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel, VoxelSideLengths, Offset)));
            }
            else
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                if (parent.Level == 2)
                    voxels.AddRange(IDs.Where(v => isPartialLevel3(v) || isFullLevel3(v))
                        .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel, VoxelSideLengths, Offset)));
                else
                    voxels.AddRange(IDs.Where(v => isPartialLevel4(v) || isFullLevel4(v))
                        .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel, VoxelSideLengths, Offset)));
            }
            return voxels;
        }
        /// <summary>
        /// Gets the sibling voxels.
        /// </summary>
        /// <param name="siblingVoxel">The sibling voxel.</param>
        /// <returns>List&lt;IVoxel&gt;.</returns>
        public List<IVoxel> GetSiblingVoxels(IVoxel siblingVoxel)
        {
            return GetChildVoxels(GetParentVoxel(siblingVoxel));
        }
        #endregion
    }
}