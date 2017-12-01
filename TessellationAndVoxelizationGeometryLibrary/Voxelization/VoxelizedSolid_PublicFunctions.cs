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
    public partial class VoxelizedSolid:Solid
    {

        public override double Volume { get; internal set; }
        #region Public Enumerations

        public IEnumerable<double[]> GetVoxelsAsAABBDoubles(VoxelRoleTypes role = VoxelRoleTypes.Any, int level = -1)
        {
            if (level == -1|| level == 0)
                foreach (var voxel in voxelDictionaryLevel0.Values.Where(v => v.VoxelRole == role))
                    yield return GetBottomAndWidth(voxel.Coordinates, 0);
            if (level == -1 || level == 1)
                foreach(var voxel in voxelDictionaryLevel1.Values.Where(v => v.VoxelRole == role))
                    yield return GetBottomAndWidth(voxel.Coordinates, 1);
            var flags = new List<VoxelRoleTypes>;

            if (level==-1||level==2)
            if (level > discretizationLevel) level = discretizationLevel;
            var flags = new VoxelRoleTypes[level];
            for (int i = 0; i < level - 1; i++)
                flags[i] = VoxelRoleTypes.Partial;
            flags[level - 1] = role;
            var targetFlags = SetRoleFlags(flags);
            return voxelDictionaryLevel0.Values.SelectMany(voxDict => GetVoxels(voxDict, targetFlags, this, level));
        }

        private double[] GetBottomAndWidth(byte[] coordinates, int level)
        {
            double x, y, z;
            if (level == 0)
            {
                x = coordinates[0] >> 4;
                y = coordinates[1] >> 4;
                z = coordinates[2] >> 4;
            }
            else
            {
                x = coordinates[0];
                y = coordinates[1];
                z = coordinates[2];
            }
            return new[] { x, y, z, VoxelSideLength[level] };
        }

        internal double[] GetBottomAndWidth(long id, int level)
        {
            var bottomCoordinate = GetCoordinatesFromID(id, level, discretizationLevel).multiply(VoxelSideLength[level]).add(Offset);
            return new[] { bottomCoordinate[0], bottomCoordinate[1], bottomCoordinate[2], VoxelSideLength[level] };
        }

        #endregion

        public int Count(VoxelClass voxel)
        {
            return (voxel.HighLevelVoxels?.Count ?? 0)
                + (voxel.NextLevelVoxels?.Count ?? 0);
        }


        public static IEnumerable<double[]> GetVoxels(VoxelClass voxel, long targetFlags, VoxelizedSolid voxelizedSolid, int level)
        {
            foreach (var vx in voxel.HighLevelVoxels)
            {
                var flags = vx & -1152921504606846976; //get rid of every but the flags
                if (flags == targetFlags)
                    yield return voxelizedSolid.GetBottomAndWidth(vx, level);
            }
        }


        public override double Volume { get; internal set; }

        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToGetNewSolid(double[,] transformationMatrix)
        {
            throw new NotImplementedException();
        }
    }
}