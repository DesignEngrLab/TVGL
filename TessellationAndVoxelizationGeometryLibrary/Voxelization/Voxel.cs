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
    /// <summary>
    /// Indicates the role of the voxel in the solid.
    /// it inside (interior)?
    /// </summary>
    public enum VoxelRoleTypes
    {
        /// <summary>
        /// The eoxel is empty or is completely outside the part
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
        ExtraCoarse = 16,
        /// <summary>
        /// The coarse discretization is up to 256 voxels on a side.
        /// </summary>
        Coarse = 256,
        /// <summary>
        /// The medium discretization is up to 4096 voxels on a side.
        /// </summary>
        Medium = 4096,
        /// <summary>
        /// The fine discretization is up to 65,536 voxels on a side (2^16)
        /// </summary>
        Fine = 65536,
        /// <summary>
        /// The extra fine is up to 2^20 (~1million) voxels on a side.
        /// </summary>
        ExtraFine = 1048576
    };
    /// <summary>
    /// Class Voxel.
    /// </summary>
    public class Voxel
    {
        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long ID;

        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        public List<TessellationBaseClass> TessellationElements { get; internal set; }
        /// <summary>
        /// Gets the voxels.
        /// </summary>
        /// <value>
        /// The voxels.
        /// </value>
        public HashSet<long> Voxels { get; internal set; }

        public Voxel(long voxelID, VoxelRoleTypes voxelRole, int level, TessellationBaseClass tsObject = null)
        {
            ID = voxelID;
            VoxelRole = voxelRole;
            if (VoxelRole == VoxelRoleTypes.Partial && level == 0)
                Voxels = new HashSet<long>();
            if (tsObject != null)
                TessellationElements = new List<TessellationBaseClass> { tsObject };
        }
    }
}
