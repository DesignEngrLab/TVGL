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
using StarMathLib;

namespace TVGL.Voxelization
{
    public interface IVoxel
    {
        long ID { get; }
        // int[] CoordinateIndices { get; }
        double[] BottomCoordinate { get; }
        double SideLength { get; }
        VoxelRoleTypes Role { get; }
        int Level { get; }
    }

    public struct Voxel : IVoxel
    {
        public long ID { get; internal set; }
        // public int[] CoordinateIndices { get; internal set; }
        public double SideLength { get; internal set; }
        public VoxelRoleTypes Role { get; internal set; }
        public int Level { get; internal set; }
        public double[] BottomCoordinate { get; internal set; }

        internal Voxel(long ID, double[] voxelSideLengths, double[] offset)
        {
            this.ID = ID;
            var roleFlags = VoxelizedSolid.GetRoleFlags(ID);
            Role = roleFlags.Last();
            Level = roleFlags.Length - 1;
            SideLength = voxelSideLengths[Level];
            BottomCoordinate = new[]
             {
                (double) ((ID & Constants.maskAllButX) >> 40),
                (double) ((ID & Constants.maskAllButX) >> 20),
                (double) (ID & Constants.maskAllButX)
            };
            BottomCoordinate = BottomCoordinate.multiply(voxelSideLengths[4]).add(offset);
        }

    }
    public abstract class VoxelWithTessellationLinks : IVoxel
    {
        protected VoxelWithTessellationLinks(byte x, byte y, byte z, VoxelRoleTypes voxelRole, double[] sideLengths, int level, double[] offset)
        {
            var coords = (level == 0) ? new[] { x & 240, y & 240, z & 240 } : new[] { (int)x, (int)y, (int)z };
            CoordinateIndices = (level == 0) ? new[] { (byte)coords[0], (byte)coords[1], (byte)coords[2] } : new[] { x, y, z };
            Role = voxelRole;
            SideLength = sideLengths[level];
            BottomCoordinate = coords.multiply(sideLengths[1]).add(offset);
        }
        public abstract int Level { get; }
        public long ID { get; internal set; }

        public byte[] CoordinateIndices { get; internal set; }
        public double[] BottomCoordinate { get; internal set; }
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

        internal Voxel_Level0_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
            : base(x, y, z, voxelRole, voxelSideLengths, 0, offset)
        {
            if (voxelRole == VoxelRoleTypes.Partial)
            {
                NextLevelVoxels = new VoxelHashSet(new VoxelComparerCoarse(), voxelSideLengths, offset);
                HighLevelVoxels = new VoxelHashSet(new VoxelComparerFine(), voxelSideLengths, offset);
            }
        }
        internal VoxelHashSet HighLevelVoxels;
        internal VoxelHashSet NextLevelVoxels;
    }


    public class Voxel_Level1_Class : VoxelWithTessellationLinks
    {
        internal Voxel_Level1_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
        : base(x, y, z, voxelRole, voxelSideLengths, 1, offset) { }
        public override int Level => 1;
    }
}
