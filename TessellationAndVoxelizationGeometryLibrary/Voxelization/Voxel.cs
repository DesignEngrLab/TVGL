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
using System.Linq;

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
    public struct Voxel
    {
        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole;

        public readonly byte Level;
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public readonly long ID;

        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        private readonly List<TessellationBaseClass> TessellationElements;

        /// <summary>
        /// Gets the voxels.
        /// </summary>
        /// <value>
        /// The voxels.
        /// </value>
        private readonly HashSet<long> Voxels;

        internal void AddVoxel(long voxelID)
        {
            if (Voxels.Contains(voxelID)) return;
            if (Voxels.Count == 4095)
            {
                VoxelRole = VoxelRoleTypes.Full;
                Voxels.Clear();
            }
            Voxels.Add(voxelID);
        }

        internal bool RemoveVoxel(long voxelID)
        {
            if (Voxels.Any())
                return Voxels.Remove(voxelID);
        
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel"/> struct.
        /// </summary>
        /// <param name="voxelID">The voxel identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        /// <param name="level">The level.</param>
        /// <param name="tsObject">The ts object.</param>
        public Voxel(long voxelID, VoxelRoleTypes voxelRole, int level, TessellationBaseClass tsObject = null)
        {
            ID = voxelID;
            VoxelRole = voxelRole;
            Level = (byte)level;
            if (VoxelRole == VoxelRoleTypes.Partial && level == 0)
                Voxels = new HashSet<long>();
            else Voxels = null;
            if (tsObject != null)
            {
                TessellationElements = new List<TessellationBaseClass> {tsObject};
                tsObject.AddVoxel(this);
            }
            else TessellationElements = null;
        }
    }
}
