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
        public long Count => (long)voxelDictionaryLevel0.Count + (long)(voxelDictionaryLevel1?.Count ?? 0) +
                             (long)voxelDictionaryLevel0.Sum(dict => (long)(dict.Value.HighLevelVoxels?.Count ?? 0));
        public new double Volume
        {
            get
            {
                var totals = GetTotals();
                var volume = 0.0;
                for (int i = 0; i <= discretizationLevel; i++)
                {
                    volume += Math.Pow(VoxelSideLength[i], 3) * totals[2 * i];
                }
                return volume + Math.Pow(VoxelSideLength[discretizationLevel], 3) * totals[2 * discretizationLevel + 1];
            }
            internal set { _volume = value; }
        }
        double _volume;

        public long[] GetTotals()
        {
            return new[]
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
        }

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

        public IEnumerable<double[]> GetVoxelsAsAABBDoubles(VoxelRoleTypes role = VoxelRoleTypes.Partial, int level = 4)
        {
            if (level == 0)
                return voxelDictionaryLevel0.Values.Where(v => v.Role == role).Select(v => GetBottomAndWidth(v.Coordinates, 0));
            if (level == 1)
                return voxelDictionaryLevel1.Values.Where(v => v.Role == role).Select(v => GetBottomAndWidth(v.Coordinates, 1));
            if (level > discretizationLevel) level = discretizationLevel;
            var flags = new VoxelRoleTypes[level];
            for (int i = 0; i < level - 1; i++)
                flags[i] = VoxelRoleTypes.Partial;
            flags[level - 1] = role;
            var targetFlags = SetRoleFlags(flags);
            return voxelDictionaryLevel0.Values.SelectMany(voxDict => GetVoxels(voxDict, targetFlags, this, level));
        }

        private double[] GetBottomAndWidth(int[] coordinates, int level)
        {
            if (level == 0)
                coordinates = coordinates.Select(x => x >> 4 << 4).ToArray();

            var doubleCoords = coordinates.Select(Convert.ToDouble).ToArray();
            doubleCoords = doubleCoords.multiply(VoxelSideLength[1]).add(Offset);

            return new[] { doubleCoords[0], doubleCoords[1], doubleCoords[2], VoxelSideLength[level] };
        }

        internal double[] GetBottomAndWidth(long id, int level)
        {
            var bottomCoordinate = GetCoordinatesFromID(id, level, discretizationLevel).multiply(VoxelSideLength[level]).add(Offset);
            return new[] { bottomCoordinate[0], bottomCoordinate[1], bottomCoordinate[2], VoxelSideLength[level] };
        }

        #endregion


        public static IEnumerable<double[]> GetVoxels(Voxel_Level0_Class voxel, long targetFlags, VoxelizedSolid voxelizedSolid, int level)
        {
            foreach (var vx in voxel.HighLevelVoxels)
            {
                var flags = vx & -1152921504606846976; //get rid of every but the flags
                if (flags == targetFlags)
                    yield return voxelizedSolid.GetBottomAndWidth(vx, level);
            }
        }


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

        public void Draft(VoxelDirections direction, IVoxel parent = null, int level = 0)
        {
            var voxels = GetChildVoxels(parent);
            var sortedPartials = new List<IVoxel>[16];

            Parallel.ForEach(voxels, v =>
            //foreach (var voxel0 in voxelDictionaryLevel0.Values)
            {
                var index = 0; //todo: how to find the right index?
                sortedPartials[index].Add(v);
            });
            //sortedPartials.Add(voxel0));
            Parallel.ForEach(voxelDictionaryLevel0.Values, voxel0 =>
            //foreach (var voxel0 in voxelDictionaryLevel0.Values)
            {
                if (voxel0.Role == VoxelRoleTypes.Full)
                {
                    var neighbor = FindNeighbor(voxel0, direction);
                    while (neighbor?.Role != VoxelRoleTypes.Full)
                    {
                        MakeVoxelFull(neighbor);
                        neighbor = FindNeighbor(neighbor, direction);
                    }
                }

            });

        }

        #endregion

        public IVoxel FindNeighbor(IVoxel voxel, VoxelDirections direction)
        {
            throw new NotImplementedException();

        }

        public IVoxel GetParentVoxel(IVoxel child)
        {
            switch (child.Level)
            {
                case 1: return voxelDictionaryLevel0[GetContainingVoxel(child.ID, 0)];
                case 2: return voxelDictionaryLevel1[GetContainingVoxel(child.ID, 1)];
                case 3:
                    var roles3 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(GetContainingVoxel(child.ID, 2) + SetRoleFlags(roles3), this.discretizationLevel);
                case 4:
                    var roles4 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(GetContainingVoxel(child.ID, 3) + SetRoleFlags(roles4), this.discretizationLevel);
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
                var level0 = voxelDictionaryLevel0[GetContainingVoxel(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                voxels.AddRange(IDs.Where(v => isPartialLevel2(v) || isFullLevel2(v))
                    .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel)));
            }
            else
            {
                var level0 = voxelDictionaryLevel0[GetContainingVoxel(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                if (parent.Level == 2)
                    voxels.AddRange(IDs.Where(v => isPartialLevel3(v) || isFullLevel3(v))
                        .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel)));
                else
                    voxels.AddRange(IDs.Where(v => isPartialLevel4(v) || isFullLevel4(v))
                        .Select(id => (IVoxel)new Voxel(id, this.discretizationLevel)));
            }
            return voxels;
        }
        public List<IVoxel> GetSiblingVoxels(IVoxel siblingVoxel)
        {
            return GetChildVoxels(GetParentVoxel(siblingVoxel));
        }

    }
}