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
            return Constants.ClearFlagsFromID(x) == Constants.ClearFlagsFromID(y);
        }

        public int GetHashCode(long obj)
        {
            // 0000 0000 1111 1111 1111 0000 0000 1111 1111 1111 0000 0000 1111 1111 1111 0000
            //           z-3  z-4  z-5            y-3  y-4  y-5            x-3  x-4  x-4  flags
            var xValuesLevels234 = (obj >> 4) & 4095;
            var yValuesLevels234 = (obj >> 24) & 4095; 
            var zValuesLevels234 = (obj >> 44) & 4095;
            return (int)(xValuesLevels234 + (yValuesLevels234 <<10) + (zValuesLevels234 << 19));
        }
    }
    public class VoxelComparerCoarse : IEqualityComparer<long>
    {
        public bool Equals(long x, long y)
        {
            return Constants.ClearFlagsFromID(x) == Constants.ClearFlagsFromID(y);
        }

        public int GetHashCode(long obj)
        {
            //long x = obj & Constants.maskAllButLevel0and1;
            long x = (obj >> 16) & 255;
            long y = (obj >> 36) & 255;
            long z = (obj >> 56) & 255;
            //#FF000,FF000,FF000,0
            return (int)(x+ (y<<10) + (z << 20));
            // this moves the x levels into the first position and then
            // z's value for levels 1 and 2 between y and x
            // converting to int remove the higher values 
            // 000 zzzz zzzz 00 yyyy yyyyy 00 xxxx xxxx
        }
    }
    public class VoxelComparerTwoDimension : IEqualityComparer<long>
    {
        private long mask;
        private int shift1, shift2;

        internal VoxelComparerTwoDimension(int dimensionToSkip)
        {
            switch (dimensionToSkip)
            {
                case 0:
                    mask = Constants.maskOutX;
                    shift1 = 24;
                    shift2 = 44;
                    break;
                case 1:
                    mask = Constants.maskOutY;
                    shift1 = 4;
                    shift2 = 44;
                    break;
                default:
                    mask = Constants.maskOutZ;
                    shift1 = 4;
                    shift2 = 24;
                    break;
            }
        }
        public bool Equals(long x, long y)
        {
            return (x & mask) == (y & mask);
        }

        public int GetHashCode(long obj)
        {
            var x = obj & mask;
            var comp1 = (x >> shift1) & Constants.MaxForSingleCoordinate;
            var comp2 = (x >> shift2) & Constants.MaxForSingleCoordinate;
            return (int)(comp1 + (comp2 << 11));
        }
    }
}