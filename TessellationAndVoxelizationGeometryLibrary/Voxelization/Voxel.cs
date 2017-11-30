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
    public class VoxelClass
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelClass"/> struct.
        /// </summary>
        /// <param name="voxelID">The voxel identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        /// <param name="level">The level.</param>
        /// <param name="tsObject">The ts object.</param>
        public VoxelClass(int x, int y, int z, VoxelRoleTypes voxelRole, int level, TessellationBaseClass tsObject = null)
        {
            Coordinates = new[] {(byte)x, (byte)y, (byte)z };
            VoxelRole = voxelRole;
            Level = level;
            if (VoxelRole == VoxelRoleTypes.Partial && level == 0)
            {
                NextLevelVoxels = new VoxelHashSet<long>(new VoxelComparerCoarse(), level);
                HighLevelVoxels = new VoxelHashSet<long>(new VoxelComparerFine(), level);
            }
            if (tsObject != null)
            {
                TessellationElements = new List<TessellationBaseClass> { tsObject };
                tsObject.AddVoxel(this);
            }
            else TessellationElements = null;
        }
        #endregion
        

        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole { get; internal set; }

        public int Level { get; internal set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
       // public long ID { get; internal set; } //is this ever used?

        internal byte[] Coordinates;

        #region TessellatedElements propoerties
        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        internal List<TessellationBaseClass> TessellationElements;

        internal List<PolygonalFace> Faces => TessellationElements.Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
        internal List<Edge> Edges => TessellationElements.Where(te => te is Edge).Cast<Edge>().ToList();
        internal List<Vertex> Vertices => TessellationElements.Where(te => te is Vertex).Cast<Vertex>().ToList();
        #endregion
        internal VoxelHashSet<long> HighLevelVoxels;
        internal VoxelHashSet<long> NextLevelVoxels;
        

        

    }
}
