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

        internal Voxel(long ID, double[] voxelSideLengths = null, double[] offset = null)
        {
            this.ID = ID;
            var roleFlags = VoxelizedSolid.GetRoleFlags(ID);
            Role = roleFlags.Last();
            Level = roleFlags.Length - 1;
            if (voxelSideLengths != null)
            {
                SideLength = voxelSideLengths[Level];
                BottomCoordinate = new[]
                {
                    (double) ((ID & Constants.maskAllButX) >> 40),
                    (double) ((ID & Constants.maskAllButY) >> 20),
                    (double) (ID & Constants.maskAllButZ)
                };
                BottomCoordinate = BottomCoordinate.multiply(voxelSideLengths[4]).add(offset);
            }
            else
            {
                SideLength = double.NaN; BottomCoordinate = null;
            }
        }
        internal Voxel(long ID, int level)
        {
            this.ID = ID;
            Role = VoxelRoleTypes.Empty;
            Level = level;
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

        public VoxelRoleTypes Role { get; internal set; }
        internal HashSet<TessellationBaseClass> TessellationElements;

        internal List<PolygonalFace> Faces => TessellationElements.Where(te => te is PolygonalFace).Cast<PolygonalFace>().ToList();
        internal List<Edge> Edges => TessellationElements.Where(te => te is Edge).Cast<Edge>().ToList();
        internal List<Vertex> Vertices => TessellationElements.Where(te => te is Vertex).Cast<Vertex>().ToList();
    }

    public class Voxel_Level0_Class : VoxelWithTessellationLinks
    {
        public override int Level => 0;

        public Voxel_Level0_Class(long ID, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
        {
            this.ID = ID;
            Role = voxelRole;
            byte x = (byte)((ID & Constants.maskAllButX) >> 52);
            byte y = (byte)((ID & Constants.maskAllButX) >> 32);
            byte z = (byte)((ID & Constants.maskAllButX) >> 12);
            var coords = new[] { x & 240, y & 240, z & 240 };
            CoordinateIndices = new[] { (byte)coords[0], (byte)coords[1], (byte)coords[2] };
            SideLength = voxelSideLengths[0];
            BottomCoordinate = coords.multiply(voxelSideLengths[1]).add(offset);
            if (Role == VoxelRoleTypes.Partial)
            {
                NextLevelVoxels = new VoxelHashSet(new VoxelComparerCoarse(), voxelSideLengths, offset);
                HighLevelVoxels = new VoxelHashSet(new VoxelComparerFine(), voxelSideLengths, offset);
            }
        }

        internal Voxel_Level0_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
        {
            var coords = new[] { x & 240, y & 240, z & 240 };
            CoordinateIndices = new[] { (byte)coords[0], (byte)coords[1], (byte)coords[2] };
            Role = voxelRole;
            SideLength = voxelSideLengths[0];
            ID = VoxelizedSolid.MakeVoxelID0(x, y, z);
            BottomCoordinate = coords.multiply(voxelSideLengths[1]).add(offset);
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
        public Voxel_Level1_Class(long ID, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
        {
            this.ID = ID;
            Role = voxelRole;
            byte x = (byte)((ID & Constants.maskAllButX) >> 52);
            byte y = (byte)((ID & Constants.maskAllButX) >> 32);
            byte z = (byte)((ID & Constants.maskAllButX) >> 12);
            CoordinateIndices = new[] { x, y, z };
            SideLength = voxelSideLengths[1];
            var coords = new[] { (int)x, (int)y, (int)z };
            BottomCoordinate = coords.multiply(voxelSideLengths[1]).add(offset);
        }

        internal Voxel_Level1_Class(byte x, byte y, byte z, VoxelRoleTypes voxelRole, double[] voxelSideLengths, double[] offset)
        {
            CoordinateIndices = new[] { x, y, z };
            Role = voxelRole;
            ID = VoxelizedSolid.MakeVoxelID1(x, y, z);
            SideLength = voxelSideLengths[1];
            var coords = new[] { (int)x, (int)y, (int)z };
            BottomCoordinate = coords.multiply(voxelSideLengths[1]).add(offset);
        }
        public override int Level => 1;
    }
}
