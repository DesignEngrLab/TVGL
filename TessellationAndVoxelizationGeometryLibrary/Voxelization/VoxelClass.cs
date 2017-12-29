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
        bool BtmCoordIsInside { get; }
    }

    public struct Voxel : IVoxel
    {
        public long ID { get; internal set; }
        // public int[] CoordinateIndices { get; internal set; }
        public double SideLength { get; internal set; }
        public VoxelRoleTypes Role { get; internal set; }
        public int Level { get; internal set; }
        public double[] BottomCoordinate { get; internal set; }
        public bool BtmCoordIsInside { get; internal set; }

        internal Voxel(long ID, VoxelizedSolid solid)
        {
            this.ID = ID;
            Constants.GetRoleFlags(ID, out var level, out var role, out var btmIsInside);
            Role = role;
            Level = level;
            BtmCoordIsInside = btmIsInside;
            SideLength = solid.VoxelSideLengths[Level];
            BottomCoordinate = solid.GetRealCoordinates(ID, Level);
        }
        internal Voxel(long ID, int level)
        {
            this.ID = ID;
            Constants.GetRoleFlags(ID, out var leveldummy, out var role, out var btmIsInside);
            Role = role;
            Level = level;
            BtmCoordIsInside = btmIsInside;
            SideLength = double.NaN;
            BottomCoordinate = null;
        }
    }
    public abstract class VoxelWithTessellationLinks : IVoxel
    {
        public abstract int Level { get; }
        public long ID { get; internal set; }

        public byte[] CoordinateIndices { get; internal set; }
        public double[] BottomCoordinate { get; internal set; }
        public double SideLength { get; internal set; }
        public bool BtmCoordIsInside { get; internal set; }
        public VoxelRoleTypes Role { get; internal set; }
        internal HashSet<TessellationBaseClass> TessellationElements;

        internal List<PolygonalFace> Faces => TessellationElements.Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
        internal List<Edge> Edges => TessellationElements.Where(te => te is Edge).Cast<Edge>().ToList();
        internal List<Vertex> Vertices => TessellationElements.Where(te => te is Vertex).Cast<Vertex>().ToList();
    }

    public class Voxel_Level0_Class : VoxelWithTessellationLinks
    {
        public override int Level => 0;

        public Voxel_Level0_Class(long ID, VoxelRoleTypes voxelRole, VoxelizedSolid solid)
        {
            this.ID = ID;
            Role = voxelRole;
            CoordinateIndices = Constants.GetCoordinateIndicesByte(ID, 0);
            byte x = (byte)((ID >> 16) & 240);
            byte y = (byte)((ID >> 36) & 240);
            byte z = (byte)((ID >> 56) & 240);
            CoordinateIndices = new[] { x, y, z };
            SideLength = solid.VoxelSideLengths[0];
            BottomCoordinate = solid.GetRealCoordinates(0, x, y, z);
            if (Role == VoxelRoleTypes.Partial)
            {
                NextLevelVoxels = new VoxelHashSet(new VoxelComparerCoarse(), solid);
                HighLevelVoxels = new VoxelHashSet(new VoxelComparerFine(), solid);
            }
        }

        //internal Voxel_Level0_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole)
        //{
        //    var coords = new[] { x & 240, y & 240, z & 240 };
        //    CoordinateIndices = new[] { (byte)coords[0], (byte)coords[1], (byte)coords[2] };
        //    Role = voxelRole;
        //    SideLength = voxelSideLengths[0];
        //    ID = Constants.MakeVoxelID0(x, y, z);
        //    BottomCoordinate = coords.multiply(voxelSideLengths[1]).add(offset);
        //    if (voxelRole == VoxelRoleTypes.Partial)
        //    {
        //        NextLevelVoxels = new VoxelHashSet(new VoxelComparerCoarse(), solid);
        //        HighLevelVoxels = new VoxelHashSet(new VoxelComparerFine(), solid);
        //    }
        //}
        internal VoxelHashSet HighLevelVoxels;
        internal VoxelHashSet NextLevelVoxels;
    }


    public class Voxel_Level1_Class : VoxelWithTessellationLinks
    {
        public Voxel_Level1_Class(long ID, VoxelRoleTypes voxelRole, VoxelizedSolid solid)
        {
            this.ID = ID;
            Role = voxelRole;
            CoordinateIndices = Constants.GetCoordinateIndicesByte(ID, 1);
            SideLength = solid.VoxelSideLengths[1];
            BottomCoordinate = solid.GetRealCoordinates(1, CoordinateIndices[0], CoordinateIndices[1], CoordinateIndices[2]);
        }

        //internal Voxel_Level1_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole, VoxelizedSolid solid)
        //{
        //    CoordinateIndices = new[] { x, y, z };
        //    Role = voxelRole;
        //    ID = Constants.MakeVoxelID1(x, y, z) + Constants.SetRoleFlags(1, Role);
        //    SideLength = solid.VoxelSideLengths[1];
        //    BottomCoordinate = solid.GetRealCoordinates(1, x, y, z);
        //}
        public override int Level => 1;
    }
}
