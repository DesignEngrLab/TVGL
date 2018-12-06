// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using TVGL.IOFunctions.amfclasses;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid : Solid
    {
        /// <summary>
        /// Gets the sum of all voxels of all sizes.
        /// </summary>
        /// <value>The count.</value>
        public long Count => _count;
        private long _count;

        /// <summary>
        /// Gets an array of voxel totals in order full0, partial0, full1, etc.
        /// </summary>
        /// <value>The get totals.</value>
        public long[] GetTotals => _totals;
        long[] _totals;

        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetVoxel(int[] coordinates, int level)
        {
            var id = Constants.MakeIDFromCoordinates(coordinates, singleCoordinateShifts[level]);
            return GetVoxel(id, level);
        }
        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="ID">The new identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetVoxel(long ID, int level = -1)
        {
            if (level == -1)
            {
                Constants.GetAllFlags(ID, out var levelFromID, out var role, out var btmIsInside);
                level = levelFromID;
            }
            var voxel0 = voxelDictionaryLevel0.GetVoxel(ID);
            if (level == 0)
            {
                if (voxel0 != null) return voxel0;
                return new Voxel(Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(0, VoxelRoleTypes.Empty), this);
            }
            //var newIDwoTags = Constants.ClearFlagsFromID(ID);
            //var parentID = Constants.MakeParentVoxelID(newIDwoTags, singleCoordinateMasks[0]);
            //var parent = voxelDictionaryLevel0.GetVoxel(parentID);
            if (voxel0 == null || voxel0.Role == VoxelRoleTypes.Empty)
                return new Voxel(Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty), this);
            if (voxel0.Role == VoxelRoleTypes.Full)
                return new Voxel(Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Full), this);

            var vID = voxel0.InnerVoxels[level - 1].GetVoxel(ID);
            if (vID != 0) return new Voxel(vID, this);

            //so, now the voxel0 exists and is partial, but the ID is not registered with its expected level. This means that
            //one of the ancestors must be empty or full
            for (int i = level - 1; i >= 1; i--)
            {
                var parentID = MakeParentVoxelID(ID, i);
                parentID = voxel0.InnerVoxels[i - 1].GetVoxel(parentID);
                if (parentID != 0)
                {
                    if (Constants.GetRole(parentID) == VoxelRoleTypes.Full)
                        return new Voxel(
                            Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Full), this);
                    else
                        return new Voxel(
                            Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty), this);
                }
            }
            // else then it was null at even level-1, which means that it is empty
            return new Voxel(
                Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty), this);
        }


        #region Get Functions

        /// <summary>
        /// Gets the Voxels with a specified role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="level">The voxel level.</param>
        /// <param name="onlyThisLevel">if set to <c>true</c> [only this level].</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        /// <exception cref="ArgumentException">Specifying voxels at a level that is finer than created.</exception>
        public IEnumerable<IVoxel> Voxels(VoxelRoleTypes role, int level = 20, bool onlyThisLevel = false)
        {
            if (level >= numberOfLevels)
            {
                if (onlyThisLevel) throw new ArgumentException("Specifying voxels at a level that is finer than created.");
                level = numberOfLevels - 1;
            }
            if ((onlyThisLevel && level == 0) || (!onlyThisLevel && level >= 0))
                foreach (var v in voxelDictionaryLevel0.Where(v => v.Role == role)) yield return v;
            if ((onlyThisLevel && level == 1) || (!onlyThisLevel && level >= 1))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 1, role)))
                    yield return v;
            if ((onlyThisLevel && level == 2) || (!onlyThisLevel && level >= 2))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 2, role)))
                    yield return v;
            if ((onlyThisLevel && level == 3) || (!onlyThisLevel && level >= 3))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 3, role)))
                    yield return v;
            if ((onlyThisLevel && level == 4) || (!onlyThisLevel && level >= 4))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 4, role)))
                    yield return v;
        }

        /// <summary>
        /// Gets the Voxels with a specified voxel level.
        /// </summary>
        /// <param name="level">The voxel level.</param>
        /// <param name="onlyThisLevel">if set to <c>true</c> [only this level].</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        /// <exception cref="ArgumentException">Specifying voxels at a level that is finer than created.</exception>
        public IEnumerable<IVoxel> Voxels(int level = 20, bool onlyThisLevel = false)
        {
            if (level >= numberOfLevels)
            {
                if (onlyThisLevel) throw new ArgumentException("Specifying voxels at a level that is finer than created.");
                level = numberOfLevels - 1;
            }
            if ((onlyThisLevel && level == 0) || (!onlyThisLevel && level >= 0))
                foreach (var v in voxelDictionaryLevel0) yield return v;
            if ((onlyThisLevel && level == 1) || (!onlyThisLevel && level >= 1))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 1)))
                    yield return v;
            if ((onlyThisLevel && level == 2) || (!onlyThisLevel && level >= 2))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 2)))
                    yield return v;
            if ((onlyThisLevel && level == 3) || (!onlyThisLevel && level >= 3))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 3)))
                    yield return v;
            if ((onlyThisLevel && level == 4) || (!onlyThisLevel && level >= 4))
                foreach (var v in voxelDictionaryLevel0
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((VoxelBinClass)vb, 4)))
                    yield return v;
        }

        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(VoxelBinClass voxel,
            int level)
        {
            if (voxel.InnerVoxels[level - 1] != null)
                foreach (var vx in voxel.InnerVoxels[level - 1])
                    yield return new Voxel(vx, this);
        }
        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(VoxelBinClass voxel,
            int level, VoxelRoleTypes role)
        {
            foreach (var vx in voxel.InnerVoxels[level - 1])
                if (Constants.GetRole(vx) == role)
                    yield return new Voxel(vx, this);
        }

        /// <summary>
        /// Gets the neighbor.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetNeighbor(IVoxel voxel, VoxelDirections direction)
        {
            return GetNeighbor(voxel, direction, out bool dummy);
        }

        /// <summary>
        /// Gets the neighbor.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="neighborHasDifferentParent">if set to <c>true</c> [neighbor has different parent].</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetNeighbor(IVoxel voxel, VoxelDirections direction, out bool neighborHasDifferentParent)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var level = voxel.Level;

            #region Check if steps outside or neighbor has different parent
            var coordValue = Constants.GetCoordinateIndex(voxel.ID, dimension, singleCoordinateShifts[level]);
            var maxValue = Constants.MaxForSingleCoordinate >> singleCoordinateShifts[level];
            var maxForThisLevel = (level == 0) ? voxelDictionaryLevel0.size[dimension] - 1 : voxelsPerSide[level] - 1;
            if ((coordValue == 0 && !positiveStep) || (level == 0 && positiveStep && coordValue == maxForThisLevel)
                || (level > 0 && positiveStep && coordValue == maxValue))
            {
                //then stepping outside of entire bounds!
                neighborHasDifferentParent = true;
                return null;
            }
            var justThisLevelCoordValue = coordValue & maxForThisLevel;
            neighborHasDifferentParent = ((justThisLevelCoordValue == 0 && !positiveStep) ||
                                          (justThisLevelCoordValue == maxForThisLevel && positiveStep));
            #endregion

            var delta = 1L << (20 * dimension + singleCoordinateShifts[voxel.Level] + 4);
            var newID = (positiveStep)
                ? Constants.ClearFlagsFromID(voxel.ID) + delta
                : Constants.ClearFlagsFromID(voxel.ID) - delta;
            return GetVoxel(newID, voxel.Level);
        }

        /// <summary>
        /// Gets the neighbors of the given voxel. This returns an array in the order:
        /// { Xpositive, YPositive, ZPositive, XNegative, YNegative, ZNegative }.
        /// If no voxel existed at a neigboring spot, then an empty voxel is produced;
        /// however, these will be of type Voxel even if it is Level0 or Leve1. Unless,
        /// of course, an existing partial or full Level0 or Level1 voxel is present.
        /// A null can appear in the neighbor array if the would-be neighbor is outisde
        /// of the bounds of the voxelized solid.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>IVoxel[].</returns>
        public IVoxel[] GetNeighbors(IVoxel voxel)
        {
            return GetNeighbors(voxel, out bool[] neighborsHaveDifferentParent);
        }

        /// <summary>
        /// Gets the neighbors of the given voxel. This returns an array in the order:
        /// { Xpositive, YPositive, ZPositive, XNegative, YNegative, ZNegative }.
        /// If no voxel existed at a neigboring spot, then an empty voxel is produced;
        /// however, these will be of type Voxel even if it is Level0 or Leve1. Unless,
        /// of course, an existing partial or full Level0 or Level1 voxel is present.
        /// A null can appear in the neighbor array if the would-be neighbor is outisde
        /// of the bounds of the voxelized solid.
        /// The additional boolean array corresponding to each neighbor is produced to
        /// indicate whether or not the neighbor has a different parent.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="neighborsHaveDifferentParent">The neighbors have different parent.</param>
        /// <returns>IVoxel[].</returns>
        public IVoxel[] GetNeighbors(IVoxel voxel, out bool[] neighborsHaveDifferentParent)
        {
            var neighbors = new IVoxel[6];
            neighborsHaveDifferentParent = new bool[6];
            neighbors[0] = GetNeighbor(voxel, VoxelDirections.XNegative, out neighborsHaveDifferentParent[0]);
            neighbors[1] = GetNeighbor(voxel, VoxelDirections.XPositive, out neighborsHaveDifferentParent[1]);
            neighbors[2] = GetNeighbor(voxel, VoxelDirections.YNegative, out neighborsHaveDifferentParent[2]);
            neighbors[3] = GetNeighbor(voxel, VoxelDirections.YPositive, out neighborsHaveDifferentParent[3]);
            neighbors[4] = GetNeighbor(voxel, VoxelDirections.ZNegative, out neighborsHaveDifferentParent[4]);
            neighbors[5] = GetNeighbor(voxel, VoxelDirections.ZPositive, out neighborsHaveDifferentParent[5]);
            return neighbors;
        }

        /// <summary>
        /// Gets the parent voxel.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>IVoxel.</returns>
        /// <exception cref="ArgumentException">There are no parents for level-0 voxels.</exception>
        public IVoxel GetParentVoxel(IVoxel child)
        {
            var parentLevel = child.Level - 1;
            if (child.Level == 0) throw new ArgumentException("There are no parents for level-0 voxels.");
            // childlevels 1, 2, 3, 4 or parent levels 0, 1, 2, 3
            var parentID = MakeParentVoxelID(child.ID, 0);
            var level0Parent = (VoxelBinClass)voxelDictionaryLevel0.GetVoxel(parentID);
            if (level0Parent == null)
                return new Voxel(MakeParentVoxelID(child.ID, parentLevel)
                                 + Constants.MakeFlags(0, VoxelRoleTypes.Empty), this);
            if (level0Parent.Role == VoxelRoleTypes.Full)
                return new Voxel(MakeParentVoxelID(child.ID, parentLevel)
                                 + Constants.MakeFlags(0, VoxelRoleTypes.Full), this);
            if (parentLevel == 0) return level0Parent;
            //now for childlevels 2,3, 4 or parent levels 1, 2, 3
            parentID = MakeParentVoxelID(child.ID, parentLevel);
            parentID = level0Parent.InnerVoxels[parentLevel - 1].GetVoxel(parentID);
            if (parentID != 0) return new Voxel(parentID, this);
            // so the rest of this should be either fulls or empties as there is no immediate partial parent
            if (parentLevel == 1)
                return new Voxel(MakeParentVoxelID(child.ID, parentLevel)
                                 + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Empty), this);
            //now for childlevels 3, 4 or parent levels 2, 3
            parentID = MakeParentVoxelID(child.ID, parentLevel - 1); // which would be either 1, or 2 - the grandparent
            parentID = level0Parent.InnerVoxels[parentLevel - 2].GetVoxel(parentID);
            if (parentID != 0) return new Voxel(parentID, this);
            // so the rest of this should be either fulls or empties as there is no immediate partial parent
            if (parentLevel == 1)
                return new Voxel(MakeParentVoxelID(child.ID, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Empty),
                    this);
            //now for childlevels 3, 4 or parent levels 2, 3
            var i = 0;
            do
            {
                i++;
                parentID = MakeParentVoxelID(child.ID, parentLevel - i); // which would be either 1, or 2 - the grandparent
                parentID = level0Parent.InnerVoxels[parentLevel - i - 1].GetVoxel(parentID);
            } while (parentID == 0);
            if (Constants.GetRole(parentID) == VoxelRoleTypes.Full)
                return new Voxel(MakeParentVoxelID(child.ID, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Full), this);
            return new Voxel(MakeParentVoxelID(child.ID, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Empty), this);
        }
        
        /// <summary>
        /// Gets the child voxels.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        public IEnumerable<IVoxel> GetChildVoxels(IVoxel parent)
        {
            if (parent == null) return voxelDictionaryLevel0;
            if (parent.Level == numberOfLevels - 1) return null;
            VoxelBinClass level0Parent;
            if (parent is VoxelBinClass)
            {
                level0Parent = (VoxelBinClass)parent;
                return level0Parent.InnerVoxels[0].Select(v => new Voxel(v, this)).Cast<IVoxel>();
            }
            // else the parent is level 1, 2, or 3
            level0Parent = (VoxelBinClass)voxelDictionaryLevel0.GetVoxel(parent.ID);
            return level0Parent.InnerVoxels[parent.Level].GetDescendants(parent.ID, parent.Level)
                .Select(v => new Voxel(v, this)).Cast<IVoxel>();
        }

        /// <summary>
        /// Gets the sibling voxels.
        /// </summary>
        /// <param name="siblingVoxel">The sibling voxel.</param>
        /// <returns>List&lt;IVoxel&gt;.</returns>
        public IEnumerable<IVoxel> GetSiblingVoxels(IVoxel siblingVoxel)
        {
            return GetChildVoxels(GetParentVoxel(siblingVoxel));
        }
        #endregion
        #region Get longs

        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public long GetVoxelID(int[] coordinates, int level)
        {
            var id = Constants.MakeIDFromCoordinates(coordinates, singleCoordinateShifts[level]);
            return GetVoxelID(id, level);
        }
        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="ID">The new identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public long GetVoxelID(long ID, int level = -1)
        {
            if (level == -1)
            {
                Constants.GetAllFlags(ID, out var levelFromID, out var role, out var btmIsInside);
                level = levelFromID;
            }
            var voxel0 = voxelDictionaryLevel0.GetVoxel(ID);
            if (level == 0)
            {
                if (voxel0 != null) return voxel0.ID;
                return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(0, VoxelRoleTypes.Empty);
            }
            //var newIDwoTags = Constants.ClearFlagsFromID(ID);
            //var parentID = Constants.MakeParentVoxelID(newIDwoTags, singleCoordinateMasks[0]);
            //var parent = voxelDictionaryLevel0.GetVoxel(parentID);
            if (voxel0 == null || voxel0.Role == VoxelRoleTypes.Empty)
                return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty);
            if (voxel0.Role == VoxelRoleTypes.Full)
                return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Full);

            var vID = voxel0.InnerVoxels[level - 1].GetVoxel(ID);
            if (vID != 0) return vID;

            //so, now the voxel0 exists and is partial, but the ID is not registered with its expected level. This means that
            //one of the ancestors must be empty or full
            for (int i = level - 1; i >= 1; i--)
            {
                var parentID = MakeParentVoxelID(ID, i);
                parentID = voxel0.InnerVoxels[i - 1].GetVoxel(parentID);
                if (parentID != 0)
                {
                    if (Constants.GetRole(parentID) == VoxelRoleTypes.Full)
                        return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Full);
                    else
                        return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty);
                }
            }
            // else then it was null at even level-1, which means that it is empty
            return Constants.ClearFlagsFromID(ID) + Constants.MakeFlags(level, VoxelRoleTypes.Empty);
        }


        /// <summary>
        /// Gets the neighbor.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>IVoxel.</returns>
        public long GetNeighbor(long voxel, VoxelDirections direction)
        {
            return GetNeighbor(voxel, direction, out bool dummy);
        }

        /// <summary>
        /// Gets the neighbor.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="neighborHasDifferentParent">if set to <c>true</c> [neighbor has different parent].</param>
        /// <returns>IVoxel.</returns>
        public long GetNeighbor(long voxel, VoxelDirections direction, out bool neighborHasDifferentParent)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var level = Constants.GetLevel(voxel);

            #region Check if steps outside or neighbor has different parent
            var coordValue = Constants.GetCoordinateIndex(voxel, dimension, singleCoordinateShifts[level]);
            var maxValue = Constants.MaxForSingleCoordinate >> singleCoordinateShifts[level];
            var maxForThisLevel = (level == 0) ? voxelDictionaryLevel0.size[dimension] - 1 : voxelsPerSide[level] - 1;
            if ((coordValue == 0 && !positiveStep) || (level == 0 && positiveStep && coordValue == maxForThisLevel)
                || (level > 0 && positiveStep && coordValue == maxValue))
            {
                //then stepping outside of entire bounds!
                neighborHasDifferentParent = true;
                return 0L;
            }
            var justThisLevelCoordValue = coordValue & maxForThisLevel;
            neighborHasDifferentParent = ((justThisLevelCoordValue == 0 && !positiveStep) ||
                                          (justThisLevelCoordValue == maxForThisLevel && positiveStep));
            #endregion

            var delta = 1L << (20 * dimension + singleCoordinateShifts[level] + 4);
            var newID = (positiveStep)
                ? Constants.ClearFlagsFromID(voxel) + delta
                : Constants.ClearFlagsFromID(voxel) - delta;
            return GetVoxelID(newID, level);
        }

        /// <summary>
        /// Gets the neighbors of the given voxel. This returns an array in the order:
        /// { Xpositive, YPositive, ZPositive, XNegative, YNegative, ZNegative }.
        /// If no voxel existed at a neigboring spot, then an empty voxel is produced;
        /// however, these will be of type Voxel even if it is Level0 or Leve1. Unless,
        /// of course, an existing partial or full Level0 or Level1 voxel is present.
        /// A null can appear in the neighbor array if the would-be neighbor is outisde
        /// of the bounds of the voxelized solid.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <returns>IVoxel[].</returns>
        public long[] GetNeighbors(long voxel)
        {
            return GetNeighbors(voxel, out bool[] neighborsHaveDifferentParent);
        }

        /// <summary>
        /// Gets the neighbors of the given voxel. This returns an array in the order:
        /// { Xpositive, YPositive, ZPositive, XNegative, YNegative, ZNegative }.
        /// If no voxel existed at a neigboring spot, then an empty voxel is produced;
        /// however, these will be of type Voxel even if it is Level0 or Leve1. Unless,
        /// of course, an existing partial or full Level0 or Level1 voxel is present.
        /// A null can appear in the neighbor array if the would-be neighbor is outisde
        /// of the bounds of the voxelized solid.
        /// The additional boolean array corresponding to each neighbor is produced to
        /// indicate whether or not the neighbor has a different parent.
        /// </summary>
        /// <param name="voxel">The voxel.</param>
        /// <param name="neighborsHaveDifferentParent">The neighbors have different parent.</param>
        /// <returns>IVoxel[].</returns>
        public long[] GetNeighbors(long voxel, out bool[] neighborsHaveDifferentParent)
        {
            var neighbors = new long[6];
            neighborsHaveDifferentParent = new bool[6];
            neighbors[0] = GetNeighbor(voxel, VoxelDirections.XNegative, out neighborsHaveDifferentParent[0]);
            neighbors[1] = GetNeighbor(voxel, VoxelDirections.XPositive, out neighborsHaveDifferentParent[1]);
            neighbors[2] = GetNeighbor(voxel, VoxelDirections.YNegative, out neighborsHaveDifferentParent[2]);
            neighbors[3] = GetNeighbor(voxel, VoxelDirections.YPositive, out neighborsHaveDifferentParent[3]);
            neighbors[4] = GetNeighbor(voxel, VoxelDirections.ZNegative, out neighborsHaveDifferentParent[4]);
            neighbors[5] = GetNeighbor(voxel, VoxelDirections.ZPositive, out neighborsHaveDifferentParent[5]);
            return neighbors;
        }

        /// <summary>
        /// Gets the parent voxel.
        /// </summary>
        /// <param name="child">The child.</param>
        /// <returns>IVoxel.</returns>
        /// <exception cref="ArgumentException">There are no parents for level-0 voxels.</exception>
        public long GetParentVoxel(long child)
        {
            var childLevel = Constants.GetLevel(child);
            var parentLevel = childLevel - 1;
            if (childLevel == 0) throw new ArgumentException("There are no parents for level-0 voxels.");
            // childlevels 1, 2, 3, 4 or parent levels 0, 1, 2, 3
            var level0Parent = (VoxelBinClass)voxelDictionaryLevel0.GetVoxel(child);
            if (level0Parent == null)
                return MakeParentVoxelID(child, parentLevel) + Constants.MakeFlags(0, VoxelRoleTypes.Empty);
            if (level0Parent.Role == VoxelRoleTypes.Full)
                return MakeParentVoxelID(child, parentLevel) + Constants.MakeFlags(0, VoxelRoleTypes.Full);
            if (parentLevel == 0) return level0Parent.ID;
            //now for childlevels 2,3, 4 or parent levels 1, 2, 3
            var parentID = MakeParentVoxelID(child, parentLevel);
            parentID = level0Parent.InnerVoxels[parentLevel - 1].GetVoxel(parentID);
            if (parentID != 0) return parentID;
            // so the rest of this should be either fulls or empties as there is no immediate partial parent
            if (parentLevel == 1)
                return MakeParentVoxelID(child, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Empty);
            //now for childlevels 3, 4 or parent levels 2, 3
            var i = 0;
            do
            {
                i++;
                parentID = MakeParentVoxelID(child, parentLevel - i); // which would be either 1, or 2 - the grandparent
                parentID = level0Parent.InnerVoxels[parentLevel - i - 1].GetVoxel(parentID);
            } while (parentID == 0);
            if (Constants.GetRole(parentID) == VoxelRoleTypes.Full)
                return MakeParentVoxelID(child, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Full);
            else return MakeParentVoxelID(child, parentLevel) + Constants.MakeFlags(parentLevel, VoxelRoleTypes.Empty);
        }


        /// <summary>
        /// Gets the child voxels.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        public IEnumerable<long> GetChildVoxels(long parent)
        {
            if (parent == 0) return voxelDictionaryLevel0.Select(v => v.ID);
            var level = Constants.GetLevel(parent);
            if (level == numberOfLevels - 1) return null;
            VoxelBinClass level0Parent = voxelDictionaryLevel0.GetVoxel(parent);
            if (level == 0) return level0Parent.InnerVoxels[0];
            // else the parent is level 1, 2, or 3
            IEnumerable<long> voxels;
            //todo here's a place where you could speed up GetDescendents by querying
            //if children exist instead of going through a huge list of InnerVoxels
            lock (level0Parent.InnerVoxels[level])
                voxels = level0Parent.InnerVoxels[level].GetDescendants(parent, level);
            return voxels;
        }


        /// <summary>
        /// Gets the sibling voxels.
        /// </summary>
        /// <param name="siblingVoxel">The sibling voxel.</param>
        /// <returns>List&lt;long&gt;.</returns>
        public IEnumerable<long> GetSiblingVoxels(long siblingVoxel)
        {
            return GetChildVoxels(GetParentVoxel(siblingVoxel));
        }

        #endregion



        #region Solid Method Overrides (Transforms & Copy)

        public override void Transform(double[,] transformMatrix)
        {
            throw new NotImplementedException();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Transform(transformationMatrix);
            return copy;
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Solid.</returns>
        public override Solid Copy()
        {
            var copy = new VoxelizedSolid(this.Discretization, this.Bounds, this.Units, this.Name, this.FileName,
                this.Comments);
            foreach (var voxel in this.voxelDictionaryLevel0)
                copy.voxelDictionaryLevel0.AddOrReplace(new VoxelBinClass(voxel.ID, voxel.Role, this));
            foreach (var v in this.voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial))
            {
                var thisVoxel = v;
                var copyVoxel = copy.voxelDictionaryLevel0.GetVoxel(thisVoxel.ID);
                for (int i = 1; i < numberOfLevels; i++)
                {
                    if (thisVoxel.InnerVoxels[i - 1] == null) break;
                    copyVoxel.InnerVoxels[i - 1] = new VoxelHashSet(i, copy.bitLevelDistribution);
                    foreach (var innerVoxel in thisVoxel.InnerVoxels[i - 1])
                        copyVoxel.InnerVoxels[i - 1].AddOrReplace(innerVoxel);
                }
            }
            copy.UpdateProperties();
            return copy;
        }

        #endregion

        #region Extrude

        /// <summary>
        /// Extrudes to a new solid.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid ExtrudeToNewSolid(VoxelDirections direction)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Extrude(direction);
            return copy;
        }

        /// <summary>
        /// Drafts the solid in the specified direction.
        /// </summary>
        /// <param name="direction">The direction in which to draft.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public void Extrude(VoxelDirections direction)
        {
            if (direction > 0)
            {
                var maxVoxels =
                    (int)Math.Ceiling(dimensions[(int)direction - 1] / VoxelSideLengths[numberOfLevels - 1]);// + 1;
                Extrude(direction, 0, maxVoxels, 0);
            }
            else
                Extrude(direction, 0, int.MaxValue, 0);
            UpdateProperties();
        }

        private bool Extrude(VoxelDirections direction, long parent, int remainingVoxelLayers,
            int level)
        {
            var positiveDir = direction > 0; /* positive direction is harder, because the solid is
                                              * aligned with the local origin. This means that drafting
                                              * in the negative direction always stop at 0, but for
                                              * positive, we wouldn't want to go to the far end of the
                                              * cube - but rather to stop at the bounding box of the solid. */
            /* remainingVoxelLayers, innerLimit, limit, and voxelPerLayer are all about this positive extrude. */
            var dimension = Math.Abs((int)direction) - 1;
            var voxels = GetChildVoxels(parent);
            var voxelsPerLayer = 1; //this is the number of smallest voxels that are within one of the current voxels.
            // this is important because, when we go in the positive direction, we want to stop at the highest voxel length
            // and a layer at this level may jump this. This is capture by the line near the bottom
            for (var i = level + 1; i < numberOfLevels; i++)  // this for-loop finishes the calculation of voxelsPerLayer
                voxelsPerLayer *= voxelsPerSide[i];
            var numLayers = voxelsPerSide[level];
            var lastLayer = false;
            //This condition is never met for negative directions
            if (numLayers >= (int)Math.Ceiling(remainingVoxelLayers / (double)voxelsPerLayer))
            {
                numLayers = (int)Math.Ceiling(remainingVoxelLayers / (double)voxelsPerLayer);
                lastLayer = true;
            }
            /* limit will often be the max. The only time it is not is for positive extrudes that meet the bounding box. */
            var layerOfVoxels = new VoxelHashSet[numLayers]; /* the voxels are organized into layers */
            for (var i = 0; i < numLayers; i++)
                layerOfVoxels[i] = new VoxelHashSet(level, bitLevelDistribution);
            Parallel.ForEach(voxels, v =>
            //foreach (var v in voxels)
            {  //place all the voxels in this level into layers along the extrude direction
                var layerIndex = (int)((v >> (20 * dimension + 4 + singleCoordinateShifts[level])) & (voxelsPerSide[level] - 1));
                if (!positiveDir) layerIndex = numLayers - 1 - layerIndex;
                lock (layerOfVoxels[layerIndex])
                    layerOfVoxels[layerIndex].AddOrReplace(v);
            });
            /* now, for the main loop */
            var loopLimit = lastLayer ? numLayers - 1 : numLayers;
            // loopLimit is one more than the number of layers so that we can "inform" the set below this one.
            // it is the same if this is the last one of the part
            var numVoxelsOnXSection = 0; /* this is used to count how many in a layer/slice/cross-section are filled up.
                                     * if it hits the max, then the parent voxel below this one should be filled up. */
            for (var i = 0; i < numLayers; i++)
            { /* cycle over each layer, note that voxels are being removed from subsequent layers so the process should
               * speed up.  */
                //Parallel.ForEach(layerOfVoxels[i], voxel =>
                foreach (var voxel in layerOfVoxels[i])
                {
                    #region fill up the layers below this one
                    if (Constants.GetRole(voxel) == VoxelRoleTypes.Full
                        || (Constants.GetRole(voxel) == VoxelRoleTypes.Partial && level == numberOfLevels - 1))
                    { //if at the lowest level - then treat partial voxel as if it were full
                        numVoxelsOnXSection++;
                        var neighbor = voxel;
                        for (var neighborLayer = i + 1; neighborLayer <= loopLimit; neighborLayer++)
                        {
                            neighbor = GetNeighbor(neighbor, direction, out var neighborHasDifferentParent);
                            if (neighbor == 0) break; // null happens when you go outside of bounds
                            //lastLayer is always false for negative directions
                            if (lastLayer && neighborLayer == loopLimit)
                            {
                                neighbor = ChangeVoxelToPartial(neighbor, false);
                                if (level < numberOfLevels - 1)
                                    // todo: this should go down to the lowest level!
                                    AddAllDescendants(Constants.ClearFlagsFromID(neighbor), level,
                                        voxelDictionaryLevel0.GetVoxel(neighbor), dimension, 1);
                                lock (layerOfVoxels[neighborLayer])
                                    layerOfVoxels[neighborLayer].AddOrReplace(neighbor);
                            }
                            else
                            {
                                neighbor = ChangeVoxelToFull(neighbor, false);
                                if (!neighborHasDifferentParent)
                                    lock (layerOfVoxels[neighborLayer])
                                        layerOfVoxels[neighborLayer].Remove(neighbor);
                            }
                        }
                    }
                    #endregion
                    #region this voxel is partial, so recurse down to fill up sublayer 
                    else if (Constants.GetRole(voxel) == VoxelRoleTypes.Partial)
                    {
                        bool filledUpNextLayer;
                        filledUpNextLayer = Extrude(direction, voxel, remainingVoxelLayers, level + 1);
                        var neighbor = GetNeighbor(voxel, direction);//, out var neighborHasDifferentParent);
                        if (neighbor == 0 || layerOfVoxels.Length <= i + 1)
                            continue; //  return;  // null happens when you go outside of bounds (of coarsest voxels)
                        if (filledUpNextLayer)
                        {
                            //lastLayer is always false for negative directions
                            if (i + 1 == loopLimit && lastLayer) neighbor = ChangeVoxelToPartial(neighbor, false);
                            else neighbor = ChangeVoxelToFull(neighbor, false);
                        }
                        else if (Constants.GetRole(neighbor) == VoxelRoleTypes.Empty) neighbor = ChangeVoxelToPartial(neighbor, false);
                        lock (layerOfVoxels[i + 1])
                            layerOfVoxels[i + 1].AddOrReplace(neighbor);
                    }
                    #endregion
                } //);
                remainingVoxelLayers -= voxelsPerLayer;
            }
            return numVoxelsOnXSection == voxelsPerSide[level] * voxelsPerSide[level];
        }

        #endregion
        #region Boolean Function (e.g. union, intersect, etc.)
        #region Intersect
        /// <summary>
        /// Intersects this solid with the specified references.
        /// </summary>
        /// <param name="references">The references.</param>
        /// <returns>TVGL.Voxelization.VoxelizedSolid.</returns>
        public VoxelizedSolid IntersectToNewSolid(params VoxelizedSolid[] references)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Intersect(references);
            return copy;
        }
        /// <summary>
        /// Intersects this solid with the specified references.
        /// </summary>
        /// <param name="references">The references.</param>
        public void Intersect(params VoxelizedSolid[] references)
        {
            Intersect(0, 0, references, false);
            UpdateProperties();
        }

        private void Intersect(long parent, int level, VoxelizedSolid[] references, bool parentWasFull)
        {
            if (parentWasFull)
            {
                // the time-savings here is that creating all sub-voxels is expensive - and then this function will
                // simply delete many of them, so if parent is full, we can look to the first reference
                // to guide us in which sub-voxels to keep.
                var voxel0 = voxelDictionaryLevel0.GetVoxel(parent);
                //now one of the references must have stated that they were partial for this voxel, but we are not sure which.
                //if we choose one that is full, then we won't get a set of reference sub-voxel and this "shortcut"
                //wouldn't be worth it.
                var k = 0;
                var newReference = references[k];
                var refParent = newReference.GetVoxelID(parent, level - 1);
                var refRole = Constants.GetRole(refParent);
                while (refRole == VoxelRoleTypes.Full)
                {
                    newReference = references[++k];
                    refParent = newReference.GetVoxelID(parent, level - 1);
                    refRole = Constants.GetRole(refParent);
                }
                var refVoxels = newReference.GetChildVoxels(refParent);
                // cycle over the reference voxels from references[0]
                Parallel.ForEach(refVoxels, refVoxel =>
                //foreach (var refVoxel in refVoxels)
                {
                    var referenceLowestRole = GetLowestRole(refVoxel, level, references);
                    if (referenceLowestRole == VoxelRoleTypes.Full)
                    {
                        var newVoxel = Constants.ClearFlagsFromID(refVoxel)
                                           + Constants.MakeFlags(level, VoxelRoleTypes.Full);
                        lock (voxel0.InnerVoxels[level - 1])
                            voxel0.InnerVoxels[level - 1].AddOrReplace(newVoxel);
                    }
                    else if (referenceLowestRole == VoxelRoleTypes.Partial)
                    {
                        var newVoxel = Constants.ClearFlagsFromID(refVoxel)
                                                    + Constants.MakeFlags(level, VoxelRoleTypes.Partial);
                        lock (voxel0.InnerVoxels[level - 1])
                            voxel0.InnerVoxels[level - 1].AddOrReplace(newVoxel);
                        if (level < numberOfLevels - 1)
                            Intersect(newVoxel, level + 1, references, true);
                    }
                });
            }
            else
            {
                var voxels = GetChildVoxels(parent);
                Parallel.ForEach(voxels, thisVoxel =>
                //foreach (var thisVoxel in voxels)
                {
                    var referenceLowestRole = GetLowestRole(thisVoxel, level, references);
                    if (referenceLowestRole == VoxelRoleTypes.Full) return; //continue;
                    if (referenceLowestRole == VoxelRoleTypes.Empty) ChangeVoxelToEmpty(thisVoxel, true, false);
                    else
                    {
                        var thisVoxelWasFull = Constants.GetRole(thisVoxel) == VoxelRoleTypes.Full;
                        if (thisVoxelWasFull) ChangeVoxelToPartial(thisVoxel, false);
                        if (level < numberOfLevels - 1)
                            Intersect(thisVoxel, level + 1, references, thisVoxelWasFull);
                    }
                });
            }
        }
        #endregion
        #region Subtract
        /// <summary>
        /// Subtracts to new solid.
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid SubtractToNewSolid(params VoxelizedSolid[] subtrahends)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Subtract(subtrahends);
            return copy;
        }

        /// <summary>
        /// Subtracts the specified subtrahends from this solid
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        public void Subtract(params VoxelizedSolid[] subtrahends)
        {
            Subtract(0, 0, subtrahends);
            UpdateProperties();
        }
        // could Subtract be sped up like Intersect? It is doubtful. One would need
        // to check which subvoxels are absent in the references, which is not likely
        // to be quicker than the current approach. For now, it's best to keep it simple.
        private void Subtract(long parent, int level, VoxelizedSolid[] subtrahends)
        {
            var voxels = GetChildVoxels(parent);
            Parallel.ForEach(voxels, thisVoxel =>
            //foreach(var thisVoxel in voxels)
            {
                var referenceHighestRole = GetHighestRole(thisVoxel, level, subtrahends);
                if (referenceHighestRole == VoxelRoleTypes.Empty) return;
                if (referenceHighestRole == VoxelRoleTypes.Full) ChangeVoxelToEmpty(thisVoxel, true, true);
                else if (level < numberOfLevels - 1)
                {
                    if (Constants.GetRole(thisVoxel) == VoxelRoleTypes.Full) ChangeVoxelToPartial(thisVoxel, true);
                    Subtract(thisVoxel, level + 1, subtrahends);
                }
                else ChangeVoxelToEmpty(thisVoxel, false, true);
            });
        }

        #endregion
        #region Bounding Solid
        /// <summary>
        /// Negates to new solid.
        /// </summary>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid CreateBoundingSolid()
        {
            var copy = (VoxelizedSolid) Copy();
            copy.BoundingSolid(0, 0);
            copy.UpdateProperties();
            return copy;
        }
        private void BoundingSolid(long parent, int level)
        {
            var coords = GetChildVoxelCoords(parent, level);
            //foreach (var coord in coords)
            Parallel.ForEach(coords, coord =>
            {
                var vox = GetVoxelID(coord, level);
                if (OverSurface(vox, level + 1))
                {
                    ChangeVoxelToPartial(vox, false);
                    BoundingSolid(vox, level + 1);
                    return;
                }
                ChangeVoxelToFull(vox, false);
            });
        }
        #endregion
        #region Invert
        public VoxelizedSolid InvertToNewSolid()
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Invert(0, 0, false);
            copy.UpdateProperties();
            return copy;
        }
        private void Invert(long parent, int level, bool onSurface)
        {
            var descendants = level != numberOfLevels - 1;
            var level0 = level == 0;
            var coords = GetChildVoxelCoords(parent, level);
            //foreach (var coord in coords)
            Parallel.ForEach(coords, coord =>
            {
                // Starting at highest (largest) level, check each voxel's role and recurse down a level
                // if it is partial or empty and crosses the outer surface of the part (i.e. is also not
                // smallest level)
                var vox = GetVoxelID(coord, level);
                var overSurface = OverSurface(vox, level + 1);
                switch (Constants.GetRole(vox))
                {
                    case VoxelRoleTypes.Empty:
                        if (overSurface)
                        {
                            ChangeVoxelToPartial(vox, false);
                            Invert(vox, level + 1, true);
                            break;
                        }
                        ChangeVoxelToFull(vox, false);
                        break;
                    case VoxelRoleTypes.Full:
                        ChangeVoxelToEmpty(vox, descendants, !level0);
                        break;
                    case VoxelRoleTypes.Partial:
                        if (descendants) Invert(vox, level + 1, overSurface);
                        else ChangeVoxelToEmpty(vox, false, !level0);
                        break;
                }
            });
            // If partial voxel spanning outer surface of solid, check if it was made empty
            // i.e. only contains empty voxels
            if (!onSurface || Constants.GetRole(parent) != VoxelRoleTypes.Partial) return;
            var empty = true;
            Parallel.ForEach(coords, coord =>
            {
                if (!empty) return;
                var vox = GetVoxelID(coord, level);
                if (Constants.GetRole(vox) != VoxelRoleTypes.Empty) empty = false;
            });
            if (empty) ChangeVoxelToEmpty(parent, false, false);
        }
        // Get all child coordinate indices (within part bounds) for a given parent
        private IEnumerable<int[]> GetChildVoxelCoords(long parent, int level)
        {
            var coords = new ConcurrentBag<int[]>();
            var iS = new [] { 0, 0, 0 };
            var iE = voxelsPerDimension[level].ToArray();
            if (parent != 0)
            {
                // Find child coordinate indices which lie within parent voxel
                var coord = Constants.GetCoordinateIndices(parent, singleCoordinateShifts[level - 1]);
                var num = voxelsPerSide[level];
                for (var i = 0; i < 3; i++)
                {
                    // Only take voxels that lie within part bounds
                    iS[i] = Math.Max(coord[i] * num, iS[i]);
                    iE[i] = Math.Min(coord[i] * num + num, iE[i]);
                }
            }
            Parallel.For(iS[0], iE[0], i =>
            {
                for (var j = iS[1]; j < iE[1]; j++)
                for (var k = iS[2]; k < iE[2]; k++)
                    coords.Add(new [] { i, j, k });
            });
            return coords;
        }
        // Determine if Voxel at coarse level spans the part's bounds
        // This will occur when the number of finest voxels along a dimension within the bounds
        // is less than the number that should exist for the given number of coarse voxels
        // i.e. The part is 211 voxels wide, but at resolution 8 (2^5, 2^3), it needs 27 coarse
        // voxels in that dimension. This results in 216 "would-be" voxels
        private bool OverSurface(long parent, int level)
        {
            var nL = numberOfLevels - 1;
            if (level > nL) return false;
            var voxelMultiplier = 1;
            for (var i = level; i <= nL; i++) voxelMultiplier *= voxelsPerSide[i];
            var compare = Constants.GetCoordinateIndices(parent, singleCoordinateShifts[level]).
                add(new[] { 1, 1, 1 }).multiply(voxelMultiplier);
            var totalVoxels = voxelsPerDimension[nL];
            for (var i = 0; i < 3; i++)
            {
                if (compare[i] > totalVoxels[i]) return true;
            }
            return false;
        }
        #endregion
        #region Union
        /// <summary>
        /// Unions to new solid.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid UnionToNewSolid(VoxelizedSolid reference)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Union(reference);
            return copy;
        }

        /// <summary>
        /// Unions the specified reference.
        /// </summary>
        /// <param name="reference">The reference.</param>
        public void Union(VoxelizedSolid reference)
        {
            Union(0, 0, reference);
            UpdateProperties();
        }

        private void Union(long parent, int level, VoxelizedSolid reference)
        {
            var voxels = reference.GetChildVoxels(parent);
            Parallel.ForEach(voxels, refVoxel =>
            {
                var refRole = Constants.GetRole(refVoxel);
                if (refRole == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxelID(refVoxel, level);
                var thisRole = Constants.GetRole(thisVoxel);
                if (thisRole == VoxelRoleTypes.Full) return;
                if (refRole == VoxelRoleTypes.Full)
                    ChangeVoxelToFull(thisVoxel, false);
                else
                {
                    if (thisRole == VoxelRoleTypes.Empty) ChangeEmptyVoxelToPartial(refVoxel, level);
                    if (level < numberOfLevels - 1)
                        Union(refVoxel, level + 1, reference);
                }
            });
        }

        #endregion
        #region ExclusiveOr

        /// <summary>
        /// Performs the exclusive-or function with the reference and creates a new solid.
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid ExclusiveOrToNewSolid(VoxelizedSolid reference)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.ExclusiveOr(0, 0, reference);
            return copy;
        }

        /// <summary>
        /// Exclusives the or.
        /// </summary>
        /// <param name="reference">The reference.</param>
        public void ExclusiveOr(VoxelizedSolid reference)
        {
            ExclusiveOr(0, 0, reference);
            UpdateProperties();
        }

        /// <summary>
        /// Performs the exclusive-or function with the reference and creates a new solid.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="level">The level.</param>
        /// <param name="reference">The reference.</param>
        private void ExclusiveOr(long parent, int level, VoxelizedSolid reference)
        {
            IEnumerable<long> voxels;
            if (Constants.GetRole(parent) == VoxelRoleTypes.Full)
                voxels = AddAllDescendants(Constants.ClearFlagsFromID(parent), level);
            else voxels = reference.GetChildVoxels(parent);
            Parallel.ForEach(voxels, refVoxel =>
            {
                var refRole = Constants.GetRole(refVoxel);
                if (refRole == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxelID(refVoxel, level);
                var thisRole = Constants.GetRole(thisVoxel);
                if (thisRole == VoxelRoleTypes.Empty && refRole == VoxelRoleTypes.Full)
                    ChangeEmptyVoxelToFull(refVoxel, level, false);
                else if (thisRole == VoxelRoleTypes.Full && refRole == VoxelRoleTypes.Full)
                    ChangeVoxelToEmpty(thisVoxel, true, false);
                // these 3 conditions cover full-empty (as well as empty-empty), empty-full, and full-full
                // what's left then is something with partial, requiring recursion
                else
                {
                    ChangeVoxelToPartial(thisVoxel, true);
                    if (level < numberOfLevels - 1) ExclusiveOr(refVoxel, level + 1, reference);
                }
            });
        }

        #endregion


        VoxelRoleTypes GetLowestRole(long ID, int level, params VoxelizedSolid[] solids)
        {
            var argumentLowest = VoxelRoleTypes.Full;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid.GetVoxelID(ID, level);
                if (argVoxel == 0) return VoxelRoleTypes.Empty;
                var argRole = Constants.GetRole(argVoxel);
                if (argRole == VoxelRoleTypes.Empty)
                    return VoxelRoleTypes.Empty;
                if (argRole == VoxelRoleTypes.Partial)
                    argumentLowest = VoxelRoleTypes.Partial;
            }
            return argumentLowest;
        }
        VoxelRoleTypes GetHighestRole(long ID, int level, params VoxelizedSolid[] solids)
        {
            var argumentHighest = VoxelRoleTypes.Empty;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid.GetVoxelID(ID, level);
                if (argVoxel == 0) continue;
                var argRole = Constants.GetRole(argVoxel);
                if (argRole == VoxelRoleTypes.Full)
                    return VoxelRoleTypes.Full;
                if (argRole == VoxelRoleTypes.Partial)
                    argumentHighest = VoxelRoleTypes.Partial;
            }
            return argumentHighest;
        }

        #endregion
        #region Voxel Projection along line
        //Todo: these functions
        public VoxelizedSolid ErodeVoxelSolid(VoxelizedSolid designedSolid, double[] dir,
            double tLimit = 0, bool inclusive = false)
        {
            var copy = (VoxelizedSolid) Copy();
            copy.ErodeSolid(designedSolid, dir, tLimit, inclusive);
            return copy;
        }

        private void ErodeSolid(VoxelizedSolid designedSolid, double[] dir,
            double tLimit, bool inclusive)
        {
            if (tLimit <= 0)
                tLimit = voxelsPerDimension[NumberOfLevels - 1].norm2();
            var mask = CreateProjectionMask(dir, tLimit, inclusive);
            var dirs = GetVoxelDirections(dir);
            var voxels = GetAllVoxelsOnBoundingSurfaces(dirs);
            ErodeVoxels(designedSolid, mask, voxels);
        }

        private void ErodeVoxels(VoxelizedSolid designedSolid, IReadOnlyList<int[]> mask,
            IEnumerable<int[]> start)
        {
            foreach (var vox in start)
                ErodeMask(designedSolid, mask, vox);
        }

        private static IEnumerable<VoxelDirections> GetVoxelDirections(IReadOnlyList<double> dir)
        {
            var dirs = new List<VoxelDirections>();
            var signedDir = new[] { Math.Sign(dir[0]), Math.Sign(dir[1]), Math.Sign(dir[2]) };
            for (var i = 0; i < 3; i++)
            {
                if (signedDir[i] == 0) continue;
                dirs.Add((VoxelDirections)((i + 1) * -1 * signedDir[i]));
            }
            return dirs;
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurfaces(IEnumerable<VoxelDirections> directions)
        {
            var voxels = new List<int[]>();
            foreach (var dir in directions)
                voxels.AddRange(GetAllVoxelsOnBoundingSurface(dir));
            return voxels;
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurface(VoxelDirections dir)
        {
            var voxels = new List<int[]>();
            var level = NumberOfLevels - 1;
            var limit = new int[3][];

            limit[0] = new [] { 0, voxelsPerDimension[level][0] };
            limit[1] = new [] { 0, voxelsPerDimension[level][1] };
            limit[2] = new [] { 0, voxelsPerDimension[level][2] };

            var ind = Math.Abs((int) dir) - 1;
            if (Math.Sign((int) dir) == 1)
                limit[ind][0] = limit[ind][1] - 1;
            else
                limit[ind][1] = 1;

            for (var i = limit[0][0]; i < limit[0][1]; i++)
                for (var j = limit[1][0]; j < limit[1][1]; j++)
                    for (var k = limit[2][0]; k < limit[2][1]; k++)
                        voxels.Add(new [] { i, j, k });

            return voxels;
        }

        private void ErodeMask(VoxelizedSolid designedSolid, IReadOnlyList<int[]> mask,
            IList<int> start = null)
        {
            var shift = new [] { 0, 0, 0 };
            if (!(start is null))
                shift = start.subtract(mask.First());
            var level = numberOfLevels - 1;
            var scShift = singleCoordinateShifts[level];
            foreach (var coord in mask)
            {
                var coordinate = !(start is null) ? coord : coord.add(shift);
                if (ExceedsBounds(coordinate, level)) break;
                var eVox = Constants.MakeIDFromCoordinates(coordinate, scShift);
                var dVox = designedSolid.GetVoxelID(eVox, level);
                if (Constants.GetRole(dVox) == VoxelRoleTypes.Full)
                    break;
                ChangeVoxelToEmpty(eVox, false, true);
            }
        }

        private bool ExceedsBounds(IReadOnlyList<int> coord, int level)
        {
            var uL = voxelsPerDimension[level];
            for (var i = 0; i < 3; i++)
                if (coord[i] < 0 || coord[i] >= uL[i]) return true;
            return false;
        }

        private IEnumerable<long> GetVoxelsFromMask(IEnumerable<int[]> mask)
        {
            return mask.Select(coord => Constants.MakeIDFromCoordinates(coord, NumberOfLevels - 1)).ToList();
        }

        private List<int[]> CreateProjectionMask(double[] dir, double tLimit,
            bool inclusive)
        {
            var nL = NumberOfLevels - 1;
            var initCoord = new[] { 0, 0, 0 };
            for (var i = 0; i < 3; i++)
                if (dir[i] < 0) initCoord[i] = voxelsPerDimension[nL][i] - 1;
            var voxels = new List<int[]>(new [] {initCoord});
            var c = initCoord.add(new[] { 0.5, 0.5, 0.5 });
            var ts = FindIntersectionDistances(c, dir, tLimit);
            foreach (var t in ts)
            {
                var cInt = c.add(dir.multiply(t));
                for (var i = 0; i < 3; i++) cInt[i] = Math.Round(cInt[i], 5);
                voxels.Add(GetNextVoxelCoord(cInt, dir));
            }
            return voxels;
        }

        private static int[] GetNextVoxelCoord(double[] cInt, double[] direction)
        {
            var searchDirs = new List<int>();
            for (var i = 0; i < 3; i++)
                if (direction[i] != 0) searchDirs.Add(i);

            var searchSigns = new[] { 0, 0, 0 };
            foreach (var dir in searchDirs)
                searchSigns[dir] = Math.Sign(direction[dir]);

            var voxel = GetOppositeVoxel(cInt, direction);
            return voxel;
        }

        public static int[] GetOppositeVoxel(double[] cInt, double[] direction)
        {
            var voxel = new int[3];
            for (var i = 0; i < 3; i++)
                if (Math.Sign(direction[i]) == -1)
                    voxel[i] = (int)Math.Ceiling(cInt[i] - 1);
                else voxel[i] = (int)Math.Floor(cInt[i]);
            return voxel;
        }

        //firstVoxel needs to be in voxel coordinates and represent the center of the voxel (i.e. {0.5, 0.5, 0.5})
        private IEnumerable<double> FindIntersectionDistances(IReadOnlyList<double> firstVoxel,
            IReadOnlyList<double> direction, double tLimit)
        {
            var intersections = new ConcurrentBag<double>();
            var searchDirs = new List<int>();

            for (var i = 0; i < 3; i++)
                if (direction[i] != 0) searchDirs.Add(i);

            var searchSigns = new [] {0, 0, 0};
            var firstInt = new[] { 0, 0, 0 };

            foreach (var dir in searchDirs)
            {
                searchSigns[dir] = Math.Sign(direction[dir]);
                firstInt[dir] = (int) (firstVoxel[dir] + 0.5 * searchSigns[dir]);
            }

            foreach (var dir in searchDirs)
            {
                var c = firstVoxel[dir];
                var d = direction[dir];
                var toValue = searchSigns[dir] == -1 ? 0 : voxelsPerDimension[NumberOfLevels - 1][dir];
                var toInt = Math.Max(toValue, firstInt[dir]);
                var fromInt = Math.Min(toValue, firstInt[dir]);
                Parallel.For(fromInt, toInt, i =>
                {
                    var t = (i - c) / d;
                    if (t <= tLimit) intersections.Add(t);
                });
            }

            var sortedIntersections = new SortedSet<double>(intersections);
            return sortedIntersections;
        }
        //private static double[] NextPlane(IReadOnlyList<double> currentIntersection, IReadOnlyList<double> dir)
        //{
        //    var nextPlane = new [] { 0.0, 0.0, 0.0 };
        //    for (var i = 0; i < 3; i++)
        //    {
        //        var d = currentIntersection[i];
        //        switch (Math.Sign(dir[i]))
        //        {
        //            case -1:
        //                nextPlane[i] = Math.Ceiling(d) - 1;
        //                break;
        //            case 1:
        //                nextPlane[i] = Math.Floor(d) + 1;
        //                break;
        //        }
        //    }
        //    return nextPlane;
        //}
        #endregion

        #region Offset

        /*
        /// <summary>
        /// Get the partial voxels ordered along X, Y, or Z, with a dictionary of their distance. X == 0, Y == 1, Z == 2. 
        /// This function does not sort along negative axis.
        /// </summary>
        /// <param name="directionIndex"></param>
        /// <param name="voxelLevel"></param>
        /// <returns></returns>
        private SortedDictionary<long, HashSet<long>> GetPartialVoxelsOrderedAlongDirection(int directionIndex, int voxelLevel)
        {
            var sortedDict = new SortedDictionary<long, HashSet<long>>(); //Key = SweepDim Value, value = voxel ID
            foreach (var voxel in Voxels())
            {
                //Partial voxels will exist on every level (unlike full or empty), we just want those on the given level.
                if (voxel.Level != voxelLevel || voxel.Role != VoxelRoleTypes.Partial) continue;

                //Instead of findind the actual coordinate value, get the IDMask for the value because it is faster.
                //var coordinateMaskValue = MaskAllBut(voxel.ID, directionIndex);
                //The actual coordinate value
                long coordValue = (voxel.ID >> (20 * (directionIndex) + 4 + singleCoordinateShifts[voxelLevel]))
                                  & (Constants.MaxForSingleCoordinate >> singleCoordinateShifts[voxelLevel]);
                if (sortedDict.ContainsKey(coordValue))
                {
                    sortedDict[coordValue].Add(voxel);
                }
                else
                {
                    sortedDict.Add(coordValue, new HashSet<long> { voxel });
                }
            }
            return sortedDict;
        }

        /// <summary>
        /// Offsets all the surfaces by a given radius
        /// </summary>
        /// <param name="r"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public VoxelizedSolid OffsetByRadius(int r, int voxelLevel)
        {
            var voxelSolid = this;//(VoxelizedSolid) Copy();

            //First, to round all edges, apply spheres to the center of every voxel in the shell.
            //Second, get all the new outer voxels.
            var offsetValues = MakeMasks(r);

            //By using a concurrent dictionary rather than a bag, we can prevent duplicates. Use byte to only take 1 bit.
            var possibleShellVoxels = new ConcurrentDictionary<long, ConcurrentDictionary<long, bool>>(); //True = on shell. False = inner.
            var directionIndex = longestDimensionIndex;                                                        //{
            int counter = 0;

            var sortedVoxelLayers = GetPartialVoxelsOrderedAlongDirection(directionIndex, 1);
            var nextUpdateLayer = sortedVoxelLayers.Keys.Min() - r; //The first layer will be set back by the offset r.
            var maxLayerIndex = sortedVoxelLayers.Keys.Max();
            foreach (var layerIndex in sortedVoxelLayers.Keys)
            {
                Parallel.ForEach(sortedVoxelLayers[layerIndex], (voxel) =>
                //foreach (var voxel in sortedVoxelLayers[layerIndex])
                {
                    counter++;
                    if (voxel.Level != voxelLevel || voxel.Role != VoxelRoleTypes.Partial) return; // continue;//

                    //Use adjacency to determine open faces. Apply either a line, partial circle, or partial sphere.
                    var sphereVoxels = GetVoxelOffsetsBasedOnNeighbors(voxel, offsetValues, voxelLevel);
                    foreach (var sphereVoxel in sphereVoxels)
                    {
                        //Add the voxelID. If it is already in the list, update the value:
                        //If oldValue == true && newValue == true => true. If either is false, return false.
                        //ToDo: double check this next line / convert this into a Constants function if useful
                        var coordValue = Constants.GetCoordinateIndex(voxel.ID, voxelLevel, directionIndex);
                        if (possibleShellVoxels.ContainsKey(coordValue))
                        {
                            possibleShellVoxels[coordValue].AddOrUpdate(sphereVoxel.Key, sphereVoxel.Value,
                                (key, oldValue) => oldValue && sphereVoxel.Value);
                        }
                        else
                        {
                            possibleShellVoxels[coordValue] = new ConcurrentDictionary<long, bool>();
                            possibleShellVoxels[coordValue].AddOrUpdate(sphereVoxel.Key, sphereVoxel.Value,
                                (key, oldValue) => oldValue && sphereVoxel.Value);
                        }
                    }
                });
                if (layerIndex == maxLayerIndex)
                {
                    r = -r; //ensures all the final layers are updated
                }

                //To be able to update a layer, the current layerIndex must be at least r distance away.
                while (layerIndex > nextUpdateLayer + r)
                {
                    ConcurrentDictionary<long, bool> removedLayer;
                    possibleShellVoxels.TryRemove(nextUpdateLayer, out removedLayer);
                    nextUpdateLayer++;
                    if (removedLayer == null) continue;

                    //ToDo: Can this be parallelized???
                    foreach (var voxelItem in removedLayer)
                    {
                        if (voxelItem.Value) voxelSolid.ChangeEmptyVoxelToPartial(voxelItem.Key, voxelLevel);
                        else voxelSolid.ChangeEmptyVoxelToFull(voxelItem.Key, voxelLevel, true);
                        //todo: not sure about this last true to check if the parent is full
                    }
                }
            }

            return voxelSolid;
        }

        /// <summary>
        /// Get the voxel IDs for any voxel within a sphere offset from the given voxel. 
        /// Voxels on the shell of the sphere are returned with a true.
        /// </summary>
        /// <param name="iVoxel"></param>
        /// <param name="sphereOffsets"></param>
        /// <returns></returns>
        private static Dictionary<long, bool> GetSphereCenteredOnVoxel(long iVoxel, List<Tuple<int[], bool>> sphereOffsets)
        {
            var voxel = (Voxel)iVoxel;
            return sphereOffsets.ToDictionary(offsetTuple => AddDeltaToID(voxel.ID, offsetTuple.Item1), offsetTuple => offsetTuple.Item2);
        }

        private Dictionary<long, bool> GetVoxelOffsetsBasedOnNeighbors(long voxel,
            Dictionary<int, List<Tuple<int[], bool>>> offsetsByDirectionCombinations, int voxelLevel)
        {
            //Initialize
            var outputVoxels = new Dictionary<long, bool>();

            //Get all the directions that have a non-empty neighbor
            var exposedDirections = GetExposedFaces(voxel);
            if (!exposedDirections.Any()) return outputVoxels;

            //Get the direction combination value.
            //If not along a primary axis, add 0.
            //Otherwise, add 1 for negative and 2 for positive.
            //The unique int value will be x*100 + y*10 + z
            var numActiveAxis = 3;
            var xdirs = new List<int>();
            if (exposedDirections.Contains(VoxelDirections.XNegative)) xdirs.Add(1);
            if (exposedDirections.Contains(VoxelDirections.XPositive)) xdirs.Add(2);
            if (!xdirs.Any())
            {
                xdirs.Add(0);
                numActiveAxis--;
            }

            var ydirs = new List<int>();
            if (exposedDirections.Contains(VoxelDirections.YNegative)) ydirs.Add(1);
            if (exposedDirections.Contains(VoxelDirections.YPositive)) ydirs.Add(2);
            if (!ydirs.Any())
            {
                ydirs.Add(0);
                numActiveAxis--;
            }

            var zdirs = new List<int>();
            if (exposedDirections.Contains(VoxelDirections.ZNegative)) zdirs.Add(1);
            if (exposedDirections.Contains(VoxelDirections.ZPositive)) zdirs.Add(2);
            if (!zdirs.Any())
            {
                zdirs.Add(0);
                numActiveAxis--;
            }

            foreach (var xdir in xdirs)
            {
                foreach (var ydir in ydirs)
                {
                    foreach (var zdir in zdirs)
                    {
                        //If there are three active axis, we only want to consider the spherical combinations
                        if (numActiveAxis == 3 && xdir * ydir * zdir == 0) continue;
                        //If there are two active axis, we only want to consider the circular combination
                        if (numActiveAxis == 2 && xdir * ydir == 0 && xdir * zdir == 0 && ydir * zdir == 0) continue;
                        var combinationKey = 100 * xdir + 10 * ydir + zdir;
                        var offsetTuples = offsetsByDirectionCombinations[combinationKey];
                        foreach (var offsetTuple in offsetTuples)
                        {
                            var key = AddDeltaToID(voxel.ID, offsetTuple.Item1);
                            //Check if it is already in the dictionary, since the sphere octants
                            //will overlap if next to one another. (same for circle quadrants)
                            if (!outputVoxels.ContainsKey(key))
                            {
                                outputVoxels.Add(key, offsetTuple.Item2);
                            }
                        }
                    }
                }
            }

            return outputVoxels;
        }

        private List<VoxelDirections> GetExposedFaces(long voxel)
        {
            var directions = Enum.GetValues(typeof(VoxelDirections)).Cast<VoxelDirections>().ToList();
            var exposedDirections = new List<VoxelDirections>();
            foreach (var direction in directions)
            {
                var neighbor = GetNeighbor(voxel, direction);
                if (neighbor == null || neighbor.Role == VoxelRoleTypes.Empty)
                {
                    exposedDirections.Add(direction);
                }
            }
            return exposedDirections;
        }

        private static long AddDeltaToID(long ID, int[] delta)
        {
            return AddDeltaToID(ID, delta[0], delta[1], delta[2]);
        }

        private static long AddDeltaToID(long ID, int deltaX, int deltaY, int deltaZ)
        {
            var deltaXLong = Math.Sign(deltaX) * ((((long)Math.Abs(deltaX)) & Constants.MaxForSingleCoordinate) << 4);
            var deltaYLong = Math.Sign(deltaY) * ((((long)Math.Abs(deltaY)) & Constants.MaxForSingleCoordinate) << 24);
            var deltaZLong = Math.Sign(deltaZ) * ((((long)Math.Abs(deltaZ)) & Constants.MaxForSingleCoordinate) << 44);
            return ID + deltaXLong + deltaYLong + deltaZLong;
        }

        //Returns the offsets for a solid sphere with each offset's sqaured distance to the center
        //true if on shell. false if inside.
        private static List<Tuple<int[], bool>> GetSolidSphereOffsets(int r)
        {
            var voxelOffsets = new List<Tuple<int[], bool>>();
            var rSqaured = r * r;

            //Do all the square operations before we start.
            //These could be calculated even fuir
            // Generate a sequence of integers from -r to r 
            var offsets = Enumerable.Range(-r, 2 * r + 1).ToArray();
            // and then generate their squares.
            var squares = offsets.Select(val => (double)val * val).ToArray();
            var xi = -1;
            foreach (var xOffset in offsets)
            {
                xi++;
                var yi = -1;
                foreach (var yOffset in offsets)
                {
                    yi++;
                    var zi = -1;
                    foreach (var zOffset in offsets)
                    {
                        //Count at start rather than at end so that if we continue, zi is correct.
                        zi++;
                        //Euclidean distance sqrt(x^2 + y^2 + z^2) must be less than r. Square both sides to get the following.
                        //By using sqrt(int), the resulting values are rounded to the floor. This gaurantees than any voxel 
                        //intersecting the sphere is set to the outer shell, but creates a 2 voxel thick portion in certain
                        //sections. For this reason, we are using regular rounding. It does not completely encompass the circle
                        //but does gaurantee a thin shell.
                        var dSqaured = (int)Math.Round(squares[xi] + squares[yi] + squares[zi]);
                        if (dSqaured > rSqaured) continue; //Not within the sphere.
                        voxelOffsets.Add(new Tuple<int[], bool>(new[] { xOffset, yOffset, zOffset }, dSqaured == rSqaured));
                    }
                }
            }
            return voxelOffsets;
        }

        //This function pre-builds all the possible offsets for every direction combination
        //This function should only need to be called once during the offset function, so it
        //does not need to be that optimized.
        private static Dictionary<int, List<Tuple<int[], bool>>> MakeMasks(int r)
        {
            var partialSolidSphereOffsets = new Dictionary<int, List<Tuple<int[], bool>>>();

            // Generate a sequence of integers from -r to r 
            var zero = new[] { 0 };
            var negOffsets = Enumerable.Range(-r, r + 1).ToArray(); //-r to 0
            var posOffsets = Enumerable.Range(0, r + 1).ToArray(); //0 to r 

            var arrays = new List<int[]>
            {
                zero,
                negOffsets,
                posOffsets,
            };

            //Get all the combinations and set their int values.
            //Use the same criteria as GetVoxelOffsetsBasedOnNeighbors
            var combinations = new Dictionary<int, List<int[]>>();
            for (var xdir = 0; xdir < arrays.Count; xdir++)
            {
                for (var ydir = 0; ydir < arrays.Count; ydir++)
                {
                    for (var zdir = 0; zdir < arrays.Count; zdir++)
                    {
                        var combinationKey = 100 * xdir + 10 * ydir + zdir;
                        combinations.Add(combinationKey, new List<int[]> { arrays[xdir], arrays[ydir], arrays[zdir] });
                    }
                }
            }

            foreach (var combination in combinations)
            {
                var key = combination.Key;
                var xOffsets = combination.Value[0];
                var yOffsets = combination.Value[1];
                var zOffsets = combination.Value[2];
                //The offsets only do positive or negative at one time, 
                //so getting the unsigned max is preferred for checking if dominated
                var maxX = xOffsets.Max(Math.Abs);
                var maxY = yOffsets.Max(Math.Abs);
                var maxZ = zOffsets.Max(Math.Abs);

                var voxelOffsets = new List<Tuple<int[], bool>>();
                foreach (var xOffset in xOffsets)
                {

                    foreach (var yOffset in yOffsets)
                    {
                        foreach (var zOffset in zOffsets)
                        {
                            //Euclidean distance sqrt(x^2 + y^2 + z^2) must be less than r. Square both sides to get the following.
                            //By using sqrt(int), the resulting values are rounded to the floor. This gaurantees than any voxel 
                            //intersecting the sphere is set to the outer shell, but creates a 2 voxel thick portion in certain
                            //sections. Regular rounding does not completely encompass the circle and cannot gaurantee a thin shell.
                            //For this reason, we use case to int and then check for dominated voxels (larger x,y, and z value)
                            var onShell = false;
                            var r2 = (int)Math.Sqrt(xOffset * xOffset + yOffset * yOffset + zOffset * zOffset);
                            if (r2 > r) continue;
                            if (r2 == r)
                            {
                                //Check if adding +1 to the active X,Y,Z axis (active if max>0) results in radius also == r. 
                                //Do this one at a time for each of the active axis. If r3 == r for all the active axis, 
                                //then the then the voxel in question is dominated.
                                var x = Math.Abs(xOffset);
                                var y = Math.Abs(yOffset);
                                var z = Math.Abs(zOffset);
                                if (maxX > 0)
                                {
                                    x++;
                                    var r3 = (int)Math.Sqrt(x * x + y * y + z * z);
                                    if (r3 > r) onShell = true;
                                    x--;
                                }
                                //If we still don't know if the voxel is on the shell and Y is active
                                if (!onShell && maxY > 0)
                                {
                                    y++;
                                    var r3 = (int)Math.Sqrt(x * x + y * y + z * z);
                                    if (r3 > r) onShell = true;
                                    y--;
                                }
                                //If we still don't know if the voxel is on the shell and Z is active
                                if (!onShell && maxZ > 0)
                                {
                                    z++;
                                    var r3 = (int)Math.Sqrt(x * x + y * y + z * z);
                                    if (r3 > r) onShell = true;
                                }
                            }
                            voxelOffsets.Add(new Tuple<int[], bool>(new[] { xOffset, yOffset, zOffset }, onShell));
                        }
                    }
                }
                partialSolidSphereOffsets.Add(key, voxelOffsets);
            }

            return partialSolidSphereOffsets;
        }

        private static List<int[]> GetHollowSphereOffsets(int r)
        {
            var voxelOffsets = new List<int[]>();
            var rSqaured = r * r;

            //Do all the square operations before we start.
            //These could be calculated even fuir
            // Generate a sequence of integers from -r to r 
            var offsets = Enumerable.Range(-r, 2 * r + 1).ToArray();
            // and then generate their squares.
            var squares = offsets.Select(val => val * val).ToArray();
            var xi = -1;
            foreach (var xOffset in offsets)
            {
                xi++;
                var yi = -1;
                foreach (var yOffset in offsets)
                {
                    yi++;
                    var zi = -1;
                    foreach (var zOffset in offsets)
                    {
                        //Count at start rather than at end so that if we continue, zi is correct.
                        zi++;
                        //Euclidean distance sqrt(x^2 + y^2 + z^2) must be exactly equal to r. Square both sides to get the following.
                        if (squares[xi] + squares[yi] + squares[zi] != rSqaured) continue; //Not within the sphere.
                        voxelOffsets.Add(new[] { xOffset, yOffset, zOffset });
                    }
                }
            }
            return voxelOffsets;
        }

        private static void GetSolidCubeCenteredOnVoxel(Voxel voxel, VoxelizedSolid voxelizedSolid, int length)
        {
            //var x = voxel.Index[0];
            //var y = voxel.Index[1];
            //var z = voxel.Index[2];
            //var voxels = voxelizedSolid.VoxelIDHashSet;
            //var r = length / 2;
            //for (var xOffset = -r; xOffset < r; xOffset++)
            //{
            //    for (var yOffset = -r; yOffset < r; yOffset++)
            //    {
            //        for (var zOffset = -r; zOffset < r; zOffset++)
            //        {
            //            voxels.Add(voxelizedSolid.IndicesToVoxelID(x + xOffset, y + yOffset, z + zOffset));
            //        }
            //    }
            //}
        }

        private static int[] AddIntArrays(IList<int> ints1, IList<int> ints2)
        {
            if (ints1.Count != ints2.Count)
                throw new Exception("This add function is only for arrays of the same size");
            var retInts = new int[ints1.Count];
            for (var i = 0; i < ints1.Count; i++)
            {
                retInts[i] = ints1[i] + ints2[i];
            }
            return retInts;
        }
        */

        #endregion
    }
}