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
        Interior,
        Exterior
    };
    /// <summary>
    /// Class Voxel.
    /// </summary>
    public class Voxel
    {
        /// <summary>
        /// The center
        /// </summary>
        public readonly double[] Center;
        /// <summary>
        /// The side length of the voxel cube
        /// </summary>
        public readonly double SideLength;

        /// <summary>
        /// Gets or sets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public readonly AABB Bounds;

        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole;
        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public readonly int[] Index;

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

        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel" /> class.
        /// </summary>
        /// <param name="Index">The index.</param>
        /// <param name="ID">The identifier.</param>
        /// <param name="SideLength">Length of the voxel.</param>
        /// <param name="VoxelRole">The voxel role.</param>
        /// <param name="tessellationObject">The tessellation object.</param>
        public Voxel(int[] Index, long ID, double SideLength, VoxelRoleTypes VoxelRole,
            TessellationBaseClass tessellationObject = null)
        {
            this.Index = (int[])Index.Clone();
            this.ID = ID;
            this.SideLength = SideLength;
            this.VoxelRole = VoxelRole;
            var minX = Index[0] * SideLength;
            var minY = Index[1] * SideLength;
            var minZ = Index[2] * SideLength;
            var halfLength = SideLength / 2;
            if (tessellationObject != null)
            {
                TessellationElements = new List<TessellationBaseClass>();
                TessellationElements.Add(tessellationObject);
            }
            Center = new[] { minX + halfLength, minY + halfLength, minZ + halfLength };
            //Bounds = new AABB
            //{
            //    MinX = minX,
            //    MinY = minY,
            //    MinZ = minZ,
            //    MaxX = minX + SideLength,
            //    MaxY = minY + SideLength,
            //    MaxZ = minZ + SideLength
            //};
        }
    }

}