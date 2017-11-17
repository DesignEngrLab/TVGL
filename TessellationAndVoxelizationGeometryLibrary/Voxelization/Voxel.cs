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
    /// Indicates the role of the voxel in the solids. Is it on the surface (exterior) or is
    /// it inside (interior)?
    /// </summary>
    public enum VoxelRoleTypes
    {
        Full,
        Partial
    };
    /// <summary>
    /// Class Voxel.
    /// </summary>
    public class VoxelBin
    {
        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole;

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public readonly long ID;

        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        public List<TessellationBaseClass> TessellationElements { get; internal set; }
        public HashSet<long> Voxels { get; internal set; }

        public VoxelBin(long voxelID, VoxelRoleTypes voxelRole, TessellationBaseClass tsObject = null)
        {
            ID = voxelID;
            VoxelRole = voxelRole;
            if (VoxelRole == VoxelRoleTypes.Partial)
                Voxels = new HashSet<long>();
            if (tsObject != null)
            {
                TessellationElements = new List<TessellationBaseClass>();
                TessellationElements.Add(tsObject);

            }
        }
    }
}
