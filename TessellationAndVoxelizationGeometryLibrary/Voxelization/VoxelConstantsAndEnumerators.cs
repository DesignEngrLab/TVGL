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
using System.Linq.Expressions;

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
        internal const double fractionOfWhiteSpaceAroundFinestVoxel = 0.01;
        // we call this f for short
        // to find this delta, we have two equations & two unknowns
        // Eq1: length_of_model + 2*delta = length_of_box
        // where delta is the border added around the model, which is f of smallest voxel
        // Eq2: smallestVoxelLength = length_of_box / numVoxels = length_of_model / (numVoxels - 2*f)
        // using the latter two expressions and isolating length_of_box yields
        // length_of_box = numVoxels*length_of_model/(numVoxels-2f)
        // plugging this into Eq 1, and solving for delta
        // delta = 0.5*[(numVoxels*length_of_model/(numVoxels-2f))-length_of_model]
        // or more simply as
        // delta = ([(numVoxels/(numVoxels-2f))-1]/2)*length_of_model
        // this factor multiplying "length_of_model" is what is stored in the next constant

        /// <summary>
        /// The maximum for single coordinate
        /// </summary>
        internal static readonly long MaxForSingleCoordinate = Int64.Parse("FFFFF",
            NumberStyles.HexNumber); // max value for a single coordinate

        #region converting IDs and back again
        #region Flags

        /****** Flags ******
         * within the last 5 (LSB) bits of the long, the flags are encoded.
         * these result in a boolean, a VoxelRoleType and a int(byte):
         * bool btmIsInside: which is true if the bottom coordinate is inside the solid
         * VoxelRoleType: Empty, Partial, Full
         * these are not independent as Empty is always false for btmIsInside, and
         * Full is always true for btmIsInside
         * As a result, the first 2 bits correspond to:
         * 00: Empty (btmIsInside = false)
         * 01: Full (btmIsInside = true)
         * 10: Partial (btmIsInside = false)
         * 11: Partial (btmIsInside = true)
         *
         * Then bits 3, 4, & 5 encode the level. One big issue! Bit 5 is also
         * potentially used as the most detailed bit of the xCoord. It would only
         * be used at the highest level.
         * Level-0: 000xx
         * Level-1: 001xx
         * Level-2: 010xx
         * Level-3: 100xx
         * Level-4: 101xx
         * Level-5: 110xx
         * Level-6: ?11xx
         * This only allows us to encode up to 7 levels. Why not 8? Well, 011 and 111
         * both correspond to level 6. As in level-6, that fifth bit is used as part
         * of the x coordinate.          */
        /// <summary>
        /// Gets the role flags.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="role">The role.</param>
        /// <param name="btmIsInside">if set to <c>true</c> [BTM is inside].</param>
        internal static void GetAllFlags(long ID, out byte level, out VoxelRoleTypes role, out bool btmIsInside)
        {
            level = (byte)((ID & 12) >> 2); //12 is (1100)
            if (level == 3) level = 6; //level 3 (11) is actually 6 (See comment above)
            else if ((ID & 16) != 0) level += 3;

            if ((ID & 2) == 0) // 0_
            {
                if ((ID & 1) == 0)
                {
                    //00
                    btmIsInside = false;
                    role = VoxelRoleTypes.Empty;
                }
                else
                {
                    //01
                    btmIsInside = true;
                    role = VoxelRoleTypes.Full;
                }
            }
            else // 1_
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = (ID & 1) == 1; // 11
            }
        }

        internal static VoxelRoleTypes GetRole(long ID)
        {
            if ((ID & 2) == 0) // 0_
            {
                if ((ID & 1) == 0)
                    return VoxelRoleTypes.Empty;
                return VoxelRoleTypes.Full;
            }
            return VoxelRoleTypes.Partial;
        }

        internal static long SetRole(long id, VoxelRoleTypes value)
        {
            if ((id &2)>0) //then partial
            id -= 2;
            else if ((id & 1) > 0) // then full
            {
                if (value == VoxelRoleTypes.Partial)
                    id += 2;
                else if (value == VoxelRoleTypes.Empty)
                    id -= 1;
            }
            else //then must be empty
            {
                if (value == VoxelRoleTypes.Partial)
                    id += 2;
                else if (value == VoxelRoleTypes.Full)
                    id += 1;
            }
            return id;
        }
        internal static bool GetIfBtmIsInside(long ID)
        {
            if ((ID & 2) == 0) // 0_
            {
                return (ID & 1) != 0;
            }
            // 1_
            return (ID & 1) == 1; // 11
        }
        internal static long SetBtmCoordIsInside(long ID, bool btmIsInside)
        {
            if (btmIsInside)
            {
                if ((ID & 1) == 0) return ID + 1;
            }
            else
                if ((ID & 1) != 0) return ID - 1;

            return ID;
        }

        internal static byte GetLevel(long ID)
        {
            var level = (byte)((ID & 12) >> 2); //12 is (1100)
            if (level == 3) level = 6; //level 3 (11) is actually 6 (See comment above)
            else if ((ID & 16) != 0) level += 3;
            return level;
        }

        /// <summary>
        /// Sets the role flags.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="role">The role.</param>
        /// <param name="btmIsInside">if set to <c>true</c> [BTM is inside].</param>
        /// <returns>System.Int64.</returns>
        public static long MakeFlags(int level, VoxelRoleTypes role, bool btmIsInside = false)
        {
            var result = (btmIsInside || role == VoxelRoleTypes.Full) ? 1L : 0L;
            if (role == VoxelRoleTypes.Partial) result += 2;
            if (level == 6) return result + 12;
            result += (level % 3) << 2;
            if (level > 2) result += 16;
            return result;
        }

        /// <summary>
        /// Clears the flags from identifier.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <returns>System.Int64.</returns>
        internal static long ClearFlagsFromID(long ID)
        {
            if ((ID & 12) == 12) return ID & -16; // which is FFFFFFFFFFFFFFF0 or 1...10000
            else return ID & -32; // which is FFFFFFFFFFFFFFF0 or 1...100000
        }



        #endregion

        #region Coordinates

        /// <summary>
        /// Makes the identifier from coordinates.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns>System.Int64.</returns>
        internal static long MakeIDFromCoordinates(int[] coordinates, int singleCoordShift)
        {
            var shift = 4 + singleCoordShift;
            //   z0   z1    z2   z3    z4   y0   y1    y2   y3    y4    x0   x1    x2   x3    x4   flags
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            var result = (long)coordinates[0] << shift;
            shift += 20;
            result += (long)coordinates[1] << shift;
            shift += 20;
            result += (long)coordinates[2] << shift;
            return result;
        }


        /// <summary>
        /// Gets the coordinate indices.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>System.Int32[].</returns>
        internal static int[] GetCoordinateIndices(long ID, int singleShift)
        {
            return new[]
            {
                GetCoordinateIndex(ID, 0, singleShift),
                GetCoordinateIndex(ID, 1, singleShift),
                GetCoordinateIndex(ID, 2, singleShift)
            };
        }

        /// <summary>
        /// Gets the index of the coordinate.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="level">The level.</param>
        /// <param name="dimension">The dimension.</param>
        /// <returns>System.Int32.</returns>
        internal static int GetCoordinateIndex(long ID, int dimension, int singleShift)
        {
            var shift = 4 + 20 * dimension + singleShift;
            return (int)((ID >> shift) & (Constants.MaxForSingleCoordinate >> singleShift));
        }

        #endregion

        #endregion

        internal static Dictionary<int, int[]> DefaultBitLevelDistribution
            = new Dictionary<int, int[]>()
            {
                {5, new[] {3, 2}},
                {6, new[] {2, 2, 2}},
                {7, new[] {3, 2, 2}},
                {8, new[] {3, 3, 2}},
                {9, new[] {3, 2,2,2}},
                {10, new[] {4, 3, 3}},
                {11, new[] {4, 4, 3}},
                {12, new[] {4, 3, 3, 2}},
                {13, new[] {4, 3, 3, 3}},
                {14, new[] {4, 3, 4, 3}},  //what is attempted from this level on down 
                {15, new[] {5, 3, 4, 3}},  // is to try to get a certain number of levels after
                {16, new[] {5, 3, 3, 2, 1}}, //level-0 to sum to 10. This is because this allows
                {17, new[] {5, 3, 4, 3, 2}},  //the midLevel comparer in the VoxelHashSet to be used
                {18, new[] {5, 3, 4, 3, 3}},  //most effectively. Above this, the finecomparer is used
                {19, new[] {5, 3, 3, 2, 2, 2, 2}},
                {20, new[] {5, 3, 3, 2, 2, 3, 2}}
            };


        internal const int DefaultLevelAtWhichLinkToTessellation = 1;

        /// <summary>
        /// Is the double currently at an integer value?
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        internal static bool atIntegerValue(double d)
        {
            return Math.Ceiling(d) == d;
        }

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
}
