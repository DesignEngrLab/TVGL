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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using StarMathLib;

namespace TVGL.Voxelization
{
    internal static class Constants
    {
        /// <summary>
        /// The fraction of white space around the finest voxel (2^20 along longest side)
        /// </summary>
        internal const double fractionOfWhiteSpaceAroundFinestVoxel = 0.5;
        // we call this f for short
        // to find this delta, we have two equations & two unknowns
        // Eq1: length_of_model + 2*delta = length_of_box
        // where delta is the border added around the model, which is f of smallest voxel
        // Eq2: smallestVoxelLength = length_of_box / 1048576 = length_of_model / (1048576 - 2*f)
        // using the latter two expressions and isolating length_of_box yields
        // length_of_box = 1048576*length_of_model/(1048576-2f)
        // plugging this into Eq 1, and solving for delta
        // delta = 0.5*[(1048576*length_of_model/(1048576-2f))-length_of_model]
        // or more simply as
        // delta = ([(1048576/(1048576-2f))-1]/2)*length_of_model
        // this factor multiplying "length_of_model" is what is stored in the next constant
        internal const double fractionOfWhiteSpaceAroundFinestVoxelFactor =
           ((1048576 / (1048576 - 2 * fractionOfWhiteSpaceAroundFinestVoxel)) - 1) / 2;

        internal const int NumberOfInteriorAttempts = 7;
        internal const int NumberOfInteriorSuccesses = 4;


        internal static readonly long maskOutX = Int64.Parse("FFFFFFFFFF000000",
            NumberStyles.HexNumber);  // clears out X since = #0,00000,FFFFF,FFFFF
        internal static readonly long maskOutY = Int64.Parse("FFFFF00000FFFFF0",
            NumberStyles.HexNumber); // clears out Y since = #0,FFFFF,00000,FFFFF
        internal static readonly long maskOutZ = Int64.Parse("00000FFFFFFFFFF0",
            NumberStyles.HexNumber); // clears out Z since = #0,FFFFF,FFFFF,00000
        internal static readonly long MaxForSingleCoordinate = Int64.Parse("FFFFF",
            NumberStyles.HexNumber); // max value for a single coordinate

        #region converting IDs and back again
        #region Parents and Children
        public static long MakeVoxelID1(byte x, byte y, byte z)
        {
            var xLong = (long)x << 16; //add 16 bits to the right of binary(x)
            var yLong = (long)y << 36; //add 36 bits to the right of binary(y)
            var zLong = (long)z << 56; //add 56 bits to the right of binary(z)
            //   z0   z1    z2   z3    z4   y0   y1    y2   y3    y4    x0   x1    x2   x3    x4   flags
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            var all = xLong + yLong + zLong;
            //var x2 = GetLevel1X(all);
            //var y2 = GetLevel1Y(all);
            //var z2 = GetLevel1Z(all);
            return all;
        }

        public static long MakeVoxelID0(byte x, byte y, byte z)
        {
            var xLong = (long)(x & 240) << 16;
            var yLong = (long)(y & 240) << 36;
            var zLong = (long)(z & 240) << 56;
            return xLong + yLong + zLong;
        }

        private static readonly long maskAllButLevel0 = Int64.Parse("F0000F0000F00000",
            NumberStyles.HexNumber);  // clears out X since = #0,F0000,F0000,F0000
        private static readonly long maskAllButLevel0and1 = Int64.Parse("FF000FF000FF0000",
            NumberStyles.HexNumber);  // clears out X since = #0,FF000,FF000,FF000
        private static readonly long maskAllButLevel01and2 = Int64.Parse("FFF00FFF00FFF000",
            NumberStyles.HexNumber);  // clears out X since = #0,FFF00,FFF00,FFF00
        private static readonly long maskLevel4 = Int64.Parse("FFFF0FFFF0FFFF00",
            NumberStyles.HexNumber);  // clears out X since = #0,FFFF0,FFFF0,FFFF0
        public static long MakeContainingVoxelID(long id, int level)
        {
            switch (level)
            {
                case 0: return id & maskAllButLevel0;
                case 1:
                    return id & maskAllButLevel0and1;
                case 2:
                    return id & maskAllButLevel01and2;
                case 3:
                    return id & maskLevel4;
            }
            throw new ArgumentOutOfRangeException("containing level must be 0, 1, 2, or 3");
        }
        #endregion
        #region Flags
        public static long SetRoleFlags(int level, VoxelRoleTypes role, bool btmIsInside = false)
        {
            var result = 0L;
            if (level == 1) result = 16;
            else if (level == 2) result = 4;
            else if (level == 3) result = 8;
            else if (level == 4) result = 12;
            if (role == VoxelRoleTypes.Partial)
            {
                if (btmIsInside) result += 2;
                else result += 1;
            }
            else if (role == VoxelRoleTypes.Full) result += 3;
            return result;
        }

        public static void GetRoleFlags(long ID, out int level, out VoxelRoleTypes role, out bool btmIsInside)
        {
            level = 0;
            btmIsInside = false;
            role = VoxelRoleTypes.Empty;
            var flags = ID & 31; //31 since this is the bottom 5 1's 11111. If it is level 2, 3, 4 this and the fifth
            // spot wasn't actually a flag but part of the x-coordinate, this is solved by the next line.
            if (flags >= 16 && flags < 20) level = 1;
            flags = flags & 15;
            if (flags == 0) return;
            if (flags == 1) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 2)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true;
                return;
            }
            if (flags == 3) { role = VoxelRoleTypes.Full; return; }

            level = 2;
            if (flags == 4) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 5) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 6)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 7) { role = VoxelRoleTypes.Full; return; }

            level = 3;
            if (flags == 8) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 9) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 10)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 11) { role = VoxelRoleTypes.Full; return; }

            level = 4;
            if (flags == 12) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 13) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 14)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 15) role = VoxelRoleTypes.Full;
        }

        private static readonly long maskOutFlags234 = Int64.Parse("FFFFFFFFFFFFFFF0",
            NumberStyles.HexNumber);   // remove the flags with #FFFFF,FFFFF,FFFFF,0
        private static readonly long maskOutFlags01 = Int64.Parse("FFFFFFFFFFFFFFE0",
            NumberStyles.HexNumber);   // remove the flags with #FFFFF,FFFFF,FFFFE,0
        internal static long ClearFlagsFromID(long ID)
        {
            return ClearFlagsFromID(ID, (ID & 12) > 0);
        }
        internal static long ClearFlagsFromID(long ID, bool level234)
        {
            if (level234) return ID & maskOutFlags234;
            else return ID & maskOutFlags01;
        }


        #endregion
        #region Coordinates
        public static long MakeCoordinateZero(long id, int dimension)
        {
            if (dimension == 0)
                return id & maskOutX;
            if (dimension == 1)
                return id & maskOutY;
            return id & maskOutZ;
        }

        internal static long ChangeCoordinate(long id, long newValue, int dimension, int level, int startDiscretizationLevel)
        {
            var shift = 4 + 20 * dimension + 4 * (4 - startDiscretizationLevel) - 4 * (startDiscretizationLevel - level);
            newValue = newValue << shift;
            return newValue + MakeCoordinateZero(id, dimension);
        }
        private static readonly long maskAllButX = Int64.Parse("000000000FFFFF0",
            NumberStyles.HexNumber); // clears all but X
        private static readonly long maskAllButY = Int64.Parse("00000FFFFF000000",
            NumberStyles.HexNumber); // clears all but Y
        private static readonly long maskAllButZ = Int64.Parse("FFFFF00000000000",
            NumberStyles.HexNumber); // clears all but Z
        internal static long MaskAllBut(long ID, int directionIndex)
        {
            switch (directionIndex)
            {
                case 0:
                    return ID & maskAllButX;
                case 1:
                    return ID & maskAllButY;
                default:
                    return ID & maskAllButZ;
            }
        }
        internal static byte[] GetCoordinateIndicesByte(long ID, int level)
        {
            if (level > 1) throw new ArgumentException("Level argument should be 0 or 1 if the return is only bytes.");
            return new[]
            {
                GetCoordinateIndexByte(ID, level, 0),
                GetCoordinateIndexByte(ID, level, 1),
                GetCoordinateIndexByte(ID, level, 2)
            };
        }
        internal static byte GetCoordinateIndexByte(long ID, int level, int dimension)
        {
            var shift = 4 + 20 * dimension + 4 * (4 - level);
            return (byte)((ID >> shift) & (Constants.MaxForSingleCoordinate >> 4 * (4 - level)));
        }
        internal static int[] GetCoordinateIndices(long ID, int level)
        {
            return new[]
            {
                GetCoordinateIndex(ID, level, 0),
                GetCoordinateIndex(ID, level, 1),
                GetCoordinateIndex(ID, level, 2)
            };
        }
        internal static int GetCoordinateIndex(long ID, int level, int dimension)
        {
            var shift = 4 + 20 * dimension + 4 * (4 - level);
            return (int)((ID >> shift) & (Constants.MaxForSingleCoordinate >> 4 * (4 - level)));
        }

        #endregion
        #endregion
    }

    /// <summary>
    /// VoxelDirections: just the six cardinal directions for the voxelized box around the solid
    /// </summary>
    public enum VoxelDirections
    {
        /// <summary>
        /// Negative X Direction
        /// </summary>
        XNegative = -1,
        /// <summary>
        /// Negative Y Direction
        /// </summary>
        YNegative = -2,
        /// <summary>
        /// Negative Z Direction
        /// </summary>
        ZNegative = -3,
        /// <summary>
        /// Positive X Direction
        /// </summary>
        XPositive = 1,
        /// <summary>
        /// Positive Y Direction
        /// </summary>
        YPositive = 2,
        /// <summary>
        /// Positive Z Direction
        /// </summary>
        ZPositive = 3
    }

    /// <summary>
    /// Indicates the role of the voxel in the solid.
    /// it inside (interior)?
    /// </summary>
    public enum VoxelRoleTypes
    {
        /// <summary>
        /// The voxel is empty or is completely outside the part
        /// </summary>
        Empty = -1,
        /// <summary>
        /// The voxel is fully within the material or is inside the part
        /// </summary>
        Full = 1,
        /// <summary>
        /// The partial fill or on the surface or exterior of the part
        /// </summary>
        Partial = 0
    };
    /// <summary>
    /// The discretization type for the voxelized solid. 
    /// </summary>
    public enum VoxelDiscretization
    {
        /// <summary>
        /// The extra coarse discretization is up to 16 voxels on a side.
        /// </summary>
        ExtraCoarse = 0, //= 16,
        /// <summary>
        /// The coarse discretization is up to 256 voxels on a side.
        /// </summary>
        Coarse = 1, // 256,
        /// <summary>
        /// The medium discretization is up to 4096 voxels on a side.
        /// </summary>
        Medium = 2,  //4096,
        /// <summary>
        /// The fine discretization is up to 65,536 voxels on a side (2^16)
        /// </summary>
        Fine = 3,  //65536,
        /// <summary>
        /// The extra fine is up to 2^20 (~1million) voxels on a side.
        /// </summary>
        ExtraFine = 4, // 1048576
    };

}
