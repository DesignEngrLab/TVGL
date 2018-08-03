// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// ***********************************************************************
// <copyright file="Voxel.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;
using System.Linq;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Interface IVoxel
    /// </summary>
    public interface IVoxel
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        long ID { get; }
        /// <summary>
        /// Gets the bottom coordinate.
        /// </summary>
        /// <value>The bottom coordinate.</value>
        double[] BottomCoordinate { get; }
        /// <summary>
        /// Gets the length of the side.
        /// </summary>
        /// <value>The length of the side.</value>
        double SideLength { get; }
        /// <summary>
        /// Gets the role.
        /// </summary>
        /// <value>The role.</value>
        VoxelRoleTypes Role { get; }
        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <value>The level.</value>
        byte Level { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [BTM coord is inside].
        /// </summary>
        /// <value><c>true</c> if [BTM coord is inside]; otherwise, <c>false</c>.</value>
        bool BtmCoordIsInside { get; set; }
        /// <summary>
        /// Gets the coordinate indices.
        /// </summary>
        /// <value>The coordinate indices.</value>
        int[] CoordinateIndices { get; }
    }

    /// <summary>
    /// Struct Voxel
    /// </summary>
    /// <seealso cref="TVGL.Voxelization.IVoxel" />
    public struct Voxel : IVoxel
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long ID { get; internal set; }
        /// <summary>
        /// Gets the length of the side.
        /// </summary>
        /// <value>The length of the side.</value>
        public double SideLength { get; internal set; }
        /// <summary>
        /// Gets the role.
        /// </summary>
        /// <value>The role.</value>
        public VoxelRoleTypes Role { get; internal set; }
        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <value>The level.</value>
        public byte Level { get; internal set; }
        /// <summary>
        /// Gets the bottom coordinate.
        /// </summary>
        /// <value>The bottom coordinate.</value>
        public double[] BottomCoordinate { get; internal set; }
        /// <summary>
        /// Gets or sets a value indicating whether [BTM coord is inside].
        /// </summary>
        /// <value><c>true</c> if [BTM coord is inside]; otherwise, <c>false</c>.</value>
        public bool BtmCoordIsInside { get; set; }

        /// <summary>
        /// Gets the coordinate indices.
        /// </summary>
        /// <value>The coordinate indices.</value>
        public int[] CoordinateIndices { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel"/> struct.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="solid">The solid.</param>
        internal Voxel(long ID, VoxelizedSolid solid) // = null)
        {
            this.ID = ID;
            Constants.GetRoleFlags(ID, out var level, out var role, out var btmIsInside);
            Role = role;
            Level = level;
            BtmCoordIsInside = btmIsInside;
            SideLength = solid.VoxelSideLengths[Level];
            CoordinateIndices = Constants.GetCoordinateIndices(ID, solid.singleCoordinateShifts[level]);
            BottomCoordinate =
                solid.GetRealCoordinates(Level, CoordinateIndices[0], CoordinateIndices[1], CoordinateIndices[2]);
        }
    }

    public class Voxel_Level0_Class : IVoxel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Voxel_Level0_Class"/> class.
        /// </summary>
        /// <param name="ID">The identifier.</param>
        /// <param name="voxelRole">The voxel role.</param>
        /// <param name="solid">The solid.</param>
        public Voxel_Level0_Class(long ID, VoxelRoleTypes voxelRole, VoxelizedSolid solid,
            bool btmCoordIsInside = false)
        {
            InnerVoxels = new VoxelHashSet[solid.numberOfLevels - 1];
            for (int i = 1; i < solid.numberOfLevels; i++)
                InnerVoxels[i - 1] = new VoxelHashSet(i, solid);
            Role = voxelRole;
            Level = 0;
            this.ID = Constants.ClearFlagsFromID(ID) +
                      Constants.SetRoleFlags(Level, Role, Role == VoxelRoleTypes.Full || btmCoordIsInside);
            BtmCoordIsInside = btmCoordIsInside;
            SideLength = solid.VoxelSideLengths[Level];
            CoordinateIndices = Constants.GetCoordinateIndices(ID, solid.singleCoordinateShifts[0]);
            BottomCoordinate =
                solid.GetRealCoordinates(Level, CoordinateIndices[0], CoordinateIndices[1], CoordinateIndices[2]);
        }

        public VoxelHashSet[] InnerVoxels { get;internal set; }


        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>The identifier.</value>
        public long ID { get; internal set; }
        // public int[] CoordinateIndices { get; internal set; }
        /// <summary>
        /// Gets the length of the side.
        /// </summary>
        /// <value>The length of the side.</value>
        public double SideLength { get; internal set; }
        /// <summary>
        /// Gets the role.
        /// </summary>
        /// <value>The role.</value>
        public VoxelRoleTypes Role { get; internal set; }
        /// <summary>
        /// Gets the level.
        /// </summary>
        /// <value>The level.</value>
        public byte Level { get; internal set; }

        /// <summary>
        /// Gets the bottom coordinate.
        /// </summary>
        /// <value>The bottom coordinate.</value>
        public double[] BottomCoordinate { get; internal set; }


        /// <summary>
        /// Gets or sets a value indicating whether [BTM coord is inside].
        /// </summary>
        /// <value><c>true</c> if [BTM coord is inside]; otherwise, <c>false</c>.</value>
        public bool BtmCoordIsInside { get; set; }



        internal Dictionary<long, HashSet<TessellationBaseClass>> tsElementsForChildVoxels;
        /// <summary>
        /// Gets the coordinate indices.
        /// </summary>
        /// <value>The coordinate indices.</value>
        public int[] CoordinateIndices { get; internal set; }

    }


}
