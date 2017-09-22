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
    /// Class Voxel.
    /// </summary>
    public class Voxel
    {
        /// <summary>
        /// The center
        /// </summary>
        public double[] Center;

        /// <summary>
        /// Gets or sets the bounds.
        /// </summary>
        /// <value>The bounds.</value>
        public AABB Bounds { get; set; }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        public int[] Index { get; set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long ID { get; set; }
        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        public List<TessellationBaseClass> TessellationElements { get; internal set; }

        /// <summary> Initializes a new instance of the <see cref="Voxel"/> class. </summary>
        /// <param name="index">The index.</param>
        /// <param name="ID">The identifier.</param>
        /// <param name="voxelLength">Length of the voxel.</param>
        /// <param name="tessellationObject">The tessellation object.</param>
        public Voxel(int[] index, long ID, double voxelLength, TessellationBaseClass tessellationObject = null)
        {
            Index = index;
            this.ID = ID;
            var minX = Index[0] * voxelLength;
            var minY = Index[1] * voxelLength;
            var minZ = Index[2] * voxelLength;
            var halfLength = voxelLength / 2;
            if (tessellationObject != null)
            {
                TessellationElements = new List<TessellationBaseClass>();
                TessellationElements.Add(tessellationObject);
            }
            Center = new[] { minX + halfLength, minY + halfLength, minZ + halfLength };
            Bounds = new AABB
            {
                MinX = minX,
                MinY = minY,
                MinZ = minZ,
                MaxX = minX + voxelLength,
                MaxY = minY + voxelLength,
                MaxZ = minZ + voxelLength
            };
        }
    }

}