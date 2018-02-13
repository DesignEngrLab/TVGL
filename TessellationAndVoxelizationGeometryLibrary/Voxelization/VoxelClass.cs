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
        int[] CoordinateIndices { get; }
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

        public int[] CoordinateIndices => Constants.GetCoordinateIndices(ID, Level);

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
        //todo: get rid of level as inputarg - maybe remove above constructor
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
        //internal Voxel(int x, int y, int z, int level,int inputCoordLevel, VoxelRoleTypes role, bool btmIsInside)
        //{
        //    this.ID = Constants.MakeIDFromCoordinates(level,x, y, z, inputCoordLevel);
        //    Role = role;
        //    Level = level;
        //    BtmCoordIsInside = btmIsInside;
        //    SideLength = double.NaN;
        //    BottomCoordinate = null;
        //}
    }
    public abstract class VoxelWithTessellationLinks : IVoxel
    {
        public abstract int Level { get; }
        public long ID { get; internal set; }

        public double[] BottomCoordinate { get; internal set; }
        public double SideLength { get; internal set; }
        public bool BtmCoordIsInside { get; internal set; }
        public VoxelRoleTypes Role { get; internal set; }
        internal HashSet<TessellationBaseClass> TessellationElements;
        public int[] CoordinateIndices => Constants.GetCoordinateIndices(ID, Level);

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
            if (Role == VoxelRoleTypes.Partial) this.ID += 1;
            else if (Role == VoxelRoleTypes.Partial) this.ID += 3;
            SideLength = solid.VoxelSideLengths[0];
            var coordinateIndices = Constants.GetCoordinateIndices(ID, 0);
            BottomCoordinate = solid.GetRealCoordinates(0, coordinateIndices[0], coordinateIndices[1], coordinateIndices[2]);
            InnerVoxels = new VoxelHashSet[solid.discretizationLevel];
        }

        internal VoxelHashSet[] InnerVoxels;
    }


    public class Voxel_Level1_Class : VoxelWithTessellationLinks
    {
        public override int Level => 1;

        public Voxel_Level1_Class(long ID, VoxelRoleTypes voxelRole, VoxelizedSolid solid)
        {
            this.ID = Constants.ClearFlagsFromID(ID) + 16; //adding 10000 which indicates level1 although it cuts in on the 4th level of
            // the x-position.
            Role = voxelRole;
            if (Role == VoxelRoleTypes.Partial) this.ID += 1;
            else if (Role == VoxelRoleTypes.Partial) this.ID += 3;
            SideLength = solid.VoxelSideLengths[1];
            var coordinateIndices = Constants.GetCoordinateIndices(ID, 1);
            BottomCoordinate = solid.GetRealCoordinates(1, coordinateIndices[0], coordinateIndices[1], coordinateIndices[2]);
        }
    }
}
