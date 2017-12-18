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

        internal static readonly long maskAllButLevel0 = Int64.Parse("0F0000F0000F0000",
            NumberStyles.HexNumber);  // clears out X since = #0,F0000,F0000,F0000
        internal static readonly long maskAllButLevel0and1 = Int64.Parse("0FF000FF000FF000",
            NumberStyles.HexNumber);  // clears out X since = #0,FF000,FF000,FF000
        internal static readonly long maskAllButLevel01and2 = Int64.Parse("0FFF00FFF00FFF00",
            NumberStyles.HexNumber);  // clears out X since = #0,FFF00,FFF00,FFF00
        internal static readonly long maskLevel4 = Int64.Parse("0FFFF0FFFF0FFFF0",
            NumberStyles.HexNumber);  // clears out X since = #0,FFFF0,FFFF0,FFFF0
        internal static readonly long maskOutX = Int64.Parse("000000FFFFFFFFFF",
            NumberStyles.HexNumber);  // clears out X since = #0,00000,FFFFF,FFFFF
        internal static readonly long maskOutY = Int64.Parse("0FFFFF00000FFFFF",
            NumberStyles.HexNumber); // clears out Y since = #0,FFFFF,00000,FFFFF
        internal static readonly long maskOutZ = Int64.Parse("0FFFFFFFFFF00000",
            NumberStyles.HexNumber); // clears out Z since = #0,FFFFF,FFFFF,00000
        internal static readonly long maskAllButX = Int64.Parse("FFFFF0000000000",
            NumberStyles.HexNumber); // clears all but X
        internal static readonly long maskAllButY = Int64.Parse("FFFFF00000",
            NumberStyles.HexNumber); // clears all but Y
        internal static readonly long maskAllButZ = Int64.Parse("FFFFF",
            NumberStyles.HexNumber); // clears all but Z


        internal static long maskOutCoarse = Int64.Parse("000FFF00FFF00FFF",
            System.Globalization.NumberStyles.HexNumber);   // re move the flags, and levels 1 and 2 and four 

        // of the highest values 1111 1111 1111 0000 0000 1111 1111 1111 0000 0000 1111 1111 1111
        //                        x-3  x-4  x-5            y-3  y-4  y-5            z-3  z-4  z-4

        internal static long maskOutFlags = Int64.Parse("0FFFFFFFFFFFFFFF",
            System.Globalization.NumberStyles.HexNumber);   // remove the flags with # 0,FFFFF,FFFFF,FFFFF
        internal static long maskAllButFlags = Int64.Parse("F000000000000000",
            System.Globalization.NumberStyles.HexNumber);   // remove the flags with # F,00000,00000,00000
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
    /// <summary>
    /// Class Voxel.
    /// </summary>

}
