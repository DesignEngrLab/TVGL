// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="Voxel.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;

namespace TVGL.Voxelization
{
    internal class VoxelComparerFine : IEqualityComparer<long>
    {
        public bool Equals(long x, long y)
        {
            return (x & VoxelizedSolid.maskOutFlags) == (y & VoxelizedSolid.maskOutFlags);
        }

        public int GetHashCode(long obj)
        {
            long x = obj & VoxelizedSolid.maskOutCoarsePlusSomeSuperFine;
            // 1111 1111 1110 0000 0000 1111 1111 1100 0000 0000 1111 1111 1100
            // x-3  x-4  x-5            y-3  y-4  y-5            z-3  z-4  z-4
            var xValuesToStart = x >> 41;
            var yValuesOnly = (x & VoxelizedSolid.maskOutZ) >> 1;
            var zValuesToMid = x << 9;
            return (int)(yValuesOnly + zValuesToMid + xValuesToStart);
        }
    }
    internal class VoxelComparerCoarse : IEqualityComparer<long>
    {
        public bool Equals(long x, long y)
        {
            return (x & VoxelizedSolid.maskOutFlags) == (y & VoxelizedSolid.maskOutFlags);
        }

        public int GetHashCode(long obj)
        {
            long x = obj & VoxelizedSolid.maskAllButLevel1and2;
            //#0,FF000,FF000,FF000
            return (int)((x >> 12) + (x >> 40));
            // this moves the z levels into the first position and then
            //x's value for levels 1 and 2 between y and z
            // converting to int remove the higher values
            // 0yyxx0zz
        }
    }
}