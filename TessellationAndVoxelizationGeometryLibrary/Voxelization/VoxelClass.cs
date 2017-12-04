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
        int[] Coordinates { get; }
        double SideLength { get; }
        VoxelRoleTypes Role { get; }
        int Level { get; }
    }

    public struct Voxel : IVoxel
    {
        public long ID { get; internal set; }
        public int[] Coordinates { get; internal set; }
        public double SideLength { get; internal set; }
        public VoxelRoleTypes Role { get; internal set; }
        public int Level { get; internal set; }

    }
    public abstract class VoxelWithTessellationLinks : IVoxel
    {
        protected VoxelWithTessellationLinks(int x, int y, int z, VoxelRoleTypes voxelRole)
        {
            Coordinates = new[] {x, y, z };
            Role = voxelRole;
        }
        public abstract int Level { get; }
        public long ID { get; internal set; }

        public int[] Coordinates { get; internal set; }
        public double SideLength { get; internal set; }

        public VoxelRoleTypes Role { get; internal set; }
        internal List<TessellationBaseClass> TessellationElements;

        internal List<PolygonalFace> Faces => TessellationElements.Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
        internal List<Edge> Edges => TessellationElements.Where(te => te is Edge).Cast<Edge>().ToList();
        internal List<Vertex> Vertices => TessellationElements.Where(te => te is Vertex).Cast<Vertex>().ToList();
    }

    public class Voxel_Level0_Class : VoxelWithTessellationLinks
    {
        public override int Level => 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel_Level0_Class"/> struct.
        /// </summary>
        /// <param name="voxelID">The voxel identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        internal Voxel_Level0_Class(int x, int y, int z, VoxelRoleTypes voxelRole) : base(x, y, z, voxelRole)
        {
            if (voxelRole == VoxelRoleTypes.Partial)
            {
                NextLevelVoxels = new VoxelHashSet<long>(new VoxelComparerCoarse(), 0);
                HighLevelVoxels = new VoxelHashSet<long>(new VoxelComparerFine(), 0);
            }
        }

        internal VoxelHashSet<long> HighLevelVoxels;
        internal VoxelHashSet<long> NextLevelVoxels;
    }


    public class Voxel_Level1_Class : VoxelWithTessellationLinks
    {
        internal Voxel_Level1_Class(int x, int y, int z, VoxelRoleTypes voxelRole) : base(x, y, z, voxelRole) { }
        public override int Level => 1;
    }
}
