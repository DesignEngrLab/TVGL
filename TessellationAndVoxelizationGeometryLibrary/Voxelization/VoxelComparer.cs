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
    public class VoxelComparerFine : IEqualityComparer<long>
    {
        public bool Equals(long x, long y)
        {
            return (x & Constants.maskOutFlags) == (y & Constants.maskOutFlags);
        }

        public int GetHashCode(long obj)
        {
            long x = obj & Constants.maskOutCoarse;
            // 1111 1111 1111 0000 0000 1111 1111 1111 0000 0000 1111 1111 1111
            // x-3  x-4  x-5            y-3  y-4  y-5            z-3  z-4  z-4
            var xValuesToStart = x >> 40;
            var yValuesOnly = (x & Constants.maskOutZ) >> 1; // the very last bit in an int32
            // is not used in hash sets so shift to lower 31 bits
            var zValuesToMid = x << 10;
            return (int)(yValuesOnly + zValuesToMid + xValuesToStart);
            // as a result the last 3 y value bits overlap with the first 3 z bits
            // and the last 2 z bits overlap with the first 2 x bits. oh well, this is the 
            // best we can do in compressing 36 bits into 31. The overlap is quite minimal.
        }
    }
    public class VoxelComparerCoarse : IEqualityComparer<long>
    {
        public bool Equals(long x, long y)
        {
            return (x & Constants.maskOutFlags) == (y & Constants.maskOutFlags);
        }

        public int GetHashCode(long obj)
        {
            long x = obj & Constants.maskAllButLevel0and1;
            //#0,FF000,FF000,FF000
            return (int)((x >> 12) + (x >> 40));
            // this moves the z levels into the first position and then
            //x's value for levels 1 and 2 between y and z
            // converting to int remove the higher values
            // 0yyxx0zz
        }
    }
}