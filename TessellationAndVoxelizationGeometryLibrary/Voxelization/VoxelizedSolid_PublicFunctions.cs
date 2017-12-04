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
        public int Count => voxelDictionaryLevel0.Count + (voxelDictionaryLevel1?.Count ?? 0) +
                            voxelDictionaryLevel0.Sum(dict => dict.Value.HighLevelVoxels?.Count ?? 0);
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

        public int[] GetTotals()
        {
            return new[]
            {
                voxelDictionaryLevel0.Values.Count(v => v.Role == VoxelRoleTypes.Full),
                voxelDictionaryLevel0.Values.Count(v => v.Role == VoxelRoleTypes.Partial),
                voxelDictionaryLevel1.Values.Count(v => v.Role == VoxelRoleTypes.Full),
                voxelDictionaryLevel1.Values.Count(v => v.Role == VoxelRoleTypes.Partial),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -8070450532247928833 && vx >= -9223372036854775808)),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -6917529027641081857 && vx >= -8070450532247928832)),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -4611686018427387905 && vx >= -5764607523034234880)),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -3458764513820540929 && vx >= -4611686018427387904)),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -1152921504606846977 && vx >= -2305843009213693952)),
                voxelDictionaryLevel0.Values.Sum(dict => dict.HighLevelVoxels
                .Count(vx => vx <= -1 && vx >= -1152921504606846976))
            };
        }
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



        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            throw new NotImplementedException();
        }



    }
}