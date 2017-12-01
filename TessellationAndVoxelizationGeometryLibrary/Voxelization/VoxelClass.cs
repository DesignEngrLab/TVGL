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
    public interface IVoxel
    {
        long ID { get; }
        double X { get; }
        double Y { get; }
        double Z { get; }
        double SideLength { get; }
        VoxelRoleTypes Role { get; set; }
}

    public struct Voxel : IVoxel
    {
        public long ID { get; }
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double SideLength { get; }
        public VoxelRoleTypes Role { get; set; }
    }
    public class VoxelClass:IVoxel
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelClass"/> struct.
        /// </summary>
        /// <param name="voxelID">The voxel identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        /// <param name="level">The level.</param>
        /// <param name="tsObject">The ts object.</param>
        public VoxelClass(int x, int y, int z, VoxelRoleTypes voxelRole, int level) 
        {
            Coordinates = new[] {(byte)x, (byte)y, (byte)z };
            VoxelRole = voxelRole;
            Level = level;
            if (VoxelRole == VoxelRoleTypes.Partial && level == 0)
            {
                NextLevelVoxels = new VoxelHashSet<long>(new VoxelComparerCoarse(), level);
                HighLevelVoxels = new VoxelHashSet<long>(new VoxelComparerFine(), level);
            }
            //if (tsObject != null)
            //{
            //    TessellationElements = new List<TessellationBaseClass> { tsObject };
            //    tsObject.AddVoxel(this);
            //}
            //else TessellationElements = null;
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

        public long ID => throw new NotImplementedException();

        public double X => throw new NotImplementedException();

        public double Y => throw new NotImplementedException();

        public double Z => throw new NotImplementedException();

        public double SideLength => throw new NotImplementedException();

        public VoxelRoleTypes Role { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        #endregion
        internal VoxelHashSet<long> HighLevelVoxels;
        internal VoxelHashSet<long> NextLevelVoxels;
    }
}
