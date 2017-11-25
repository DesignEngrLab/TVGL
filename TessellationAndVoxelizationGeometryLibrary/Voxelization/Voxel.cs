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
        /// <summary>
        /// The voxel role (interior or exterior)
        /// </summary>
        public VoxelRoleTypes VoxelRole { get; internal set; }

        public byte Level { get; internal set; }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long ID { get; internal set; } //is this ever used?

        #region TessellatedElements functions
        /// <summary>
        /// Gets the tessellation elements that areoverlapping with this voxel.
        /// </summary>
        /// <value>The tessellation elements.</value>
        private List<TessellationBaseClass> TessellationElements;

        internal void Add(TessellationBaseClass tsObject)
        {
            if (TessellationElements == null) TessellationElements = new List<TessellationBaseClass>();
            else if (TessellationElements.Contains(tsObject)) return;
            TessellationElements.Add(tsObject);
            tsObject.AddVoxel(this);
        }

        internal bool Remove(TessellationBaseClass tsObject)
        {
            if (TessellationElements == null) return false;
            if (TessellationElements.Count == 1 && TessellationElements.Contains(tsObject))
            {
                TessellationElements = null;
                return true;
            }
            return TessellationElements.Remove(tsObject);
        }

        internal bool Contains(TessellationBaseClass tsObject)
        {
            if (TessellationElements == null) return false;
            return TessellationElements.Contains(tsObject);
        }
        internal List<PolygonalFace> Faces => TessellationElements.Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
        internal List<Edge> Edges => TessellationElements.Where(te => te is Edge).Cast<Edge>().ToList();
        internal List<Vertex> Vertices => TessellationElements.Where(te => te is Vertex).Cast<Vertex>().ToList();

        #endregion
        #region sub-voxel functions
        /// <summary>
        /// Gets the voxels.
        /// </summary>
        /// <value>
        /// The voxels.
        /// </value>
        internal HashSet<long> Voxels;


        internal void Add(long voxelID)
        {
            if (Voxels.Contains(voxelID)) return;
            if (Voxels.Count == 4095)
            {
                VoxelRole = VoxelRoleTypes.Full;
                Voxels.Clear();
            }
            Voxels.Add(voxelID);
        }

        internal bool Remove(long voxelID)
        {
            if (Voxels.Any())
                return Voxels.Remove(voxelID);
            if (Voxels.Count == 1 && Voxels.Contains(voxelID))
            { //then this is the last subvoxel, so this goes empty
                Voxels = null;
                VoxelRole = VoxelRoleTypes.Empty;
                //change ID? is it necessary
                return true;
            }
            throw new NotImplementedException("removing a voxel from a full means having to create all the sub-voxels minus 1.");
        }

        internal bool Contains(long voxelID)
        {
            if (Voxels == null) return false;
            return Voxels.Contains(voxelID);
        }

        internal int Count()
        {
            if (Voxels == null) return 0;
            return Voxels.Count;
        }


        internal IEnumerable<double[]> GetVoxels(long targetFlags, VoxelizedSolid voxelizedSolid, int level)
        {
            foreach (var voxel in Voxels)
            {
                var flags = voxel & 15; //get rid of every but the flags
                if (flags == targetFlags)
                    yield return voxelizedSolid.GetBottomAndWidth(voxel, level);
            }
        }



        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelClass"/> struct.
        /// </summary>
        /// <param name="voxelID">The voxel identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        /// <param name="level">The level.</param>
        /// <param name="tsObject">The ts object.</param>
        public VoxelClass(long voxelID, VoxelRoleTypes voxelRole, int level, TessellationBaseClass tsObject = null)
        {
            ID = voxelID;
            VoxelRole = voxelRole;
            Level = (byte)level;
            if (VoxelRole == VoxelRoleTypes.Partial && level == 0)
                Voxels = new HashSet<long>(new VoxelComparerFine()) { voxelID };
            else Voxels = null;
            if (tsObject != null)
            {
                TessellationElements = new List<TessellationBaseClass> { tsObject };
                tsObject.AddVoxel(this);
            }
            else TessellationElements = null;
        }

    }
}
