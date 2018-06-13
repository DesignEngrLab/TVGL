// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// ***********************************************************************
// <copyright file="Voxel.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class Constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// The fraction of white space around the finest voxel (2^20 along longest side)
        /// </summary>
        internal const double fractionOfWhiteSpaceAroundFinestVoxel = 0.1;
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
        /// <summary>
        /// The fraction of white space around finest voxel factor
        /// </summary>
        internal const double fractionOfWhiteSpaceAroundFinestVoxelFactor =
           ((1048576 / (1048576 - 2 * fractionOfWhiteSpaceAroundFinestVoxel)) - 1) / 2;


        /// <summary>
        /// The mask out x
        /// </summary>
        internal static readonly long maskOutX = Int64.Parse("FFFFFFFFFF000000",
            NumberStyles.HexNumber);  // clears out X since = #0,00000,FFFFF,FFFFF
        /// <summary>
        /// The mask out y
        /// </summary>
        internal static readonly long maskOutY = Int64.Parse("FFFFF00000FFFFF0",
            NumberStyles.HexNumber); // clears out Y since = #0,FFFFF,00000,FFFFF
        /// <summary>
        /// The mask out z
        /// </summary>
        internal static readonly long maskOutZ = Int64.Parse("00000FFFFFFFFFF0",
            NumberStyles.HexNumber); // clears out Z since = #0,FFFFF,FFFFF,00000
        /// <summary>
        /// The maximum for single coordinate
        /// </summary>
        internal static readonly long MaxForSingleCoordinate = Int64.Parse("FFFFF",
            NumberStyles.HexNumber); // max value for a single coordinate

        #region converting IDs and back again
        #region Parents and Children


        /// <summary>
        /// The mask all but level0
        /// </summary>
        private static readonly long maskAllButLevel0 = Int64.Parse("F0000F0000F00000",
            NumberStyles.HexNumber);
        /// <summary>
        /// The mask all but level0and1
        /// </summary>
        private static readonly long maskAllButLevel0and1 = Int64.Parse("FF000FF000FF0000",
            NumberStyles.HexNumber);
        /// <summary>
        /// The mask all but level01and2
        /// </summary>
        private static readonly long maskAllButLevel01and2 = Int64.Parse("FFF00FFF00FFF000",
            NumberStyles.HexNumber);
        /// <summary>
        /// The mask level4
        /// </summary>
        private static readonly long maskLevel4 = Int64.Parse("FFFF0FFFF0FFFF00",
            NumberStyles.HexNumber);
        /// <summary>
        /// Makes the parent voxel identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>System.Int64.</returns>
        /// <exception cref="ArgumentOutOfRangeException">containing level must be 0, 1, 2, or 3</exception>
        public static long MakeParentVoxelID(long id, int level)
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
        /// <summary>
        /// Sets the role flags.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="role">The role.</param>
        /// <param name="btmIsInside">if set to <c>true</c> [BTM is inside].</param>
        /// <returns>System.Int64.</returns>
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

        /// <summary>
        /// Gets the role flags.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="role">The role.</param>
        /// <param name="btmIsInside">if set to <c>true</c> [BTM is inside].</param>
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

        /// <summary>
        /// The mask out flags234
        /// </summary>
        private static readonly long maskOutFlags234 = Int64.Parse("FFFFFFFFFFFFFFF0",
            NumberStyles.HexNumber);   // remove the flags with #FFFFF,FFFFF,FFFFF,0
        /// <summary>
        /// The mask out flags01
        /// </summary>
        private static readonly long maskOutFlags01 = Int64.Parse("FFFFFFFFFFFFFFE0",
            NumberStyles.HexNumber);   // remove the flags with #FFFFF,FFFFF,FFFFE,0
        /// <summary>
        /// Clears the flags from identifier.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>System.Int64.</returns>
        internal static long ClearFlagsFromID(long ID)
        {
            return ClearFlagsFromID(ID, (ID & 12) > 0);
        }
        /// <summary>
        /// Clears the flags from identifier.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level234">if set to <c>true</c> [level234].</param>
        /// <returns>System.Int64.</returns>
        internal static long ClearFlagsFromID(long ID, bool level234)
        {
            if (level234) return ID & maskOutFlags234;
            else return ID & maskOutFlags01;
        }


        #endregion
        #region Coordinates

        /// <summary>
        /// Makes the identifier from coordinates.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="inputCoordLevel">The input coord level.</param>
        /// <returns>System.Int64.</returns>
        internal static long MakeIDFromCoordinates(int level, int[] coordinates, int inputCoordLevel)
        {
            //   z0   z1    z2   z3    z4   y0   y1    y2   y3    y4    x0   x1    x2   x3    x4   flags
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            var xLong = (long)coordinates[0] << 4;
            var yLong = (long)coordinates[1] << 24;
            var zLong = (long)coordinates[2] << 44;

            xLong = xLong << 4 * (4 - inputCoordLevel);
            yLong = yLong << 4 * (4 - inputCoordLevel);
            zLong = zLong << 4 * (4 - inputCoordLevel);
            var id = zLong + yLong + xLong;
            switch (level)
            {
                case 0: return id & maskAllButLevel0;
                case 1:
                    return id & maskAllButLevel0and1;
                case 2:
                    return id & maskAllButLevel01and2;
                case 3:
                    return id & maskLevel4;
                default: return id;
            }

        }

        /// <summary>
        /// The mask all but x
        /// </summary>
        private static readonly long maskAllButX = Int64.Parse("0000000000FFFFF0",
            NumberStyles.HexNumber); // clears all but X
        /// <summary>
        /// The mask all but y
        /// </summary>
        private static readonly long maskAllButY = Int64.Parse("00000FFFFF000000",
            NumberStyles.HexNumber); // clears all but Y
        /// <summary>
        /// The mask all but z
        /// </summary>
        private static readonly long maskAllButZ = Int64.Parse("FFFFF00000000000",
            NumberStyles.HexNumber); // clears all but Z
        /// <summary>
        /// Masks all but.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="directionIndex">Index of the direction.</param>
        /// <returns>System.Int64.</returns>
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
        /// <summary>
        /// Gets the coordinate indices.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>System.Int32[].</returns>
        internal static int[] GetCoordinateIndices(long ID, int level)
        {
            return new[]
            {
                GetCoordinateIndex(ID, level, 0),
                GetCoordinateIndex(ID, level, 1),
                GetCoordinateIndex(ID, level, 2)
            };
        }
        /// <summary>
        /// Gets the index of the coordinate.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetCoordinateIndex(long ID, int level, int dimension)
        {
            var shift = 4 + 20 * dimension + 4 * (4 - level);  //todo: replace 4by4 with bit's take sum
            return (int)((ID >> shift) & (Constants.MaxForSingleCoordinate >> 4 * (4 - level)));
        }

        #endregion
        #endregion
        internal static Dictionary<VoxelDiscretization, int[]> DefaultBitLevelDistribution
        = new Dictionary<VoxelDiscretization, int[]>()
        {
              { VoxelDiscretization.ExtraCoarse, new[]{4}}, // 16 (2^4) voxels per side
               { VoxelDiscretization.Coarse, new[]{4,4}}, // 256 (2^8)  voxels per side
               { VoxelDiscretization.Medium, new[]{4,4,4}}, // 4096 (2^12)  voxels per side
               { VoxelDiscretization.Fine, new[]{4,4,4,4}}, // 65K (2^16)  voxels per side
               { VoxelDiscretization.ExtraFine, new[]{4,4,4,4,4}} //1million (2^20) voxels per side 
            /*   { VoxelDiscretization.ExtraCoarse, new[]{3,3}}, // 64 (2^6) voxels per side
               { VoxelDiscretization.Coarse, new[]{4,3,3}}, // 1024 (2^10)  voxels per side
               { VoxelDiscretization.Medium, new[]{4,3,3,2}}, // 4096 (2^12)  voxels per side
               { VoxelDiscretization.Fine, new[]{5,3,3,2,2}}, // 32K (2^15)  voxels per side
               { VoxelDiscretization.ExtraFine, new[]{5,4,3,3,3,2}} //1million (2^20) voxels per side */
        };
        internal const int LevelAtWhichComparerSwitchesToFine = 4;
        internal const int LevelAtWhichLinkToTessellation = 1;
    }

    /// <summary>
    /// VoxelDirections: just the six cardinal directions for the voxelized box around the solid
    /// </summary>
    public enum VoxelDirections
    {
        /// <summary>
        /// <summary>
        /// Enum VoxelDirections
        /// </summary>
        /// Negative X Direction
        /// </summary>
        /// <summary>
        /// The x negative
        /// </summary>
        XNegative = -1,
        /// <summary>
        /// Negative Y Direction
        /// <summary>
        /// The x negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y negative
        /// </summary>
        YNegative = -2,
        /// <summary>
        /// Negative Z Direction
        /// <summary>
        /// The y negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z negative
        /// </summary>
        ZNegative = -3,
        /// <summary>
        /// Positive X Direction
        /// <summary>
        /// The z negative
        /// </summary>
        /// </summary>
        /// <summary>
        /// The x positive
        /// </summary>
        XPositive = 1,
        /// <summary>
        /// Positive Y Direction
        /// <summary>
        /// The x positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The y positive
        /// </summary>
        YPositive = 2,
        /// <summary>
        /// Positive Z Direction
        /// <summary>
        /// The y positive
        /// </summary>
        /// </summary>
        /// <summary>
        /// The z positive
        /// </summary>
        ZPositive = 3
    }

    /// <summary>
    /// The z positive
    /// </summary>
    /// <summary>
    /// Indicates the role of the voxel in the solid.
    /// it inside (interior)?
    /// </summary>
    public enum VoxelRoleTypes
    {
        /// <summary>
        /// <summary>
        /// Enum VoxelRoleTypes
        /// </summary>
        /// The voxel is empty or is completely outside the part
        /// </summary>
        /// <summary>
        /// The empty
        /// </summary>
        Empty = -1,
        /// <summary>
        /// The voxel is fully within the material or is inside the part
        /// <summary>
        /// The empty
        /// </summary>
        /// </summary>
        /// <summary>
        /// The full
        /// </summary>
        Full = 1,
        /// <summary>
        /// The partial fill or on the surface or exterior of the part
        /// <summary>
        /// The full
        /// </summary>
        /// </summary>
        /// <summary>
        /// The partial
        /// </summary>
        Partial = 0
    };
    /// <summary>
    /// <summary>
    /// The partial
    /// </summary>
    /// The discretization type for the voxelized solid. 
    /// </summary>
    public enum VoxelDiscretization
    {
        /// <summary>
        /// <summary>
        /// Enum VoxelDiscretization
        /// </summary>
        /// The extra coarse discretization is up to 64 voxels on a side.
        /// </summary>
        /// <summary>
        /// The extra coarse
        /// </summary>
        ExtraCoarse = 0, //= 16,
        /// <summary>
        /// The coarse discretization is up to 512 voxels on a side.
        /// <summary>
        /// The extra coarse
        /// </summary>
        /// </summary>
        /// <summary>
        /// The coarse
        /// </summary>
        Coarse = 1, // 256,
        /// <summary>
        /// The medium discretization is up to 4096 voxels on a side.
        /// <summary>
        /// The coarse
        /// </summary>
        /// </summary>
        /// <summary>
        /// The medium
        /// </summary>
        Medium = 2,  //4096,
        /// <summary>
        /// The fine discretization is up to 65,536 voxels on a side (2^16)
        /// <summary>
        /// The medium
        /// </summary>
        /// </summary>
        /// <summary>
        /// The fine
        /// </summary>
        Fine = 3,  //65536,
        /// <summary>
        /// The extra fine is up to 2^20 (~1million) voxels on a side.
        /// <summary>
        /// The fine
        /// </summary>
        /// </summary>
        /// <summary>
        /// The extra fine
        /// </summary>
        ExtraFine = 4, // 1048576
    };
}
