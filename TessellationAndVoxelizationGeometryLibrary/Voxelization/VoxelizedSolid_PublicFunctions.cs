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
        /// <param name="newID">The new identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetVoxel(long newID, int level = -1)
        {
            if (level == -1)
            {
                Constants.GetRoleFlags(newID, out var levelFromID, out var role, out var btmIsInside);
                level = levelFromID;
            }

            var newIDwoTags = Constants.ClearFlagsFromID(newID);
            if (level == 0)
            {
                return voxelDictionaryLevel0.GetVoxel(newIDwoTags)
                       ??
                       new Voxel(newIDwoTags + Constants.SetRoleFlags(0, VoxelRoleTypes.Empty), this);
            }
            var parentID = Constants.MakeParentVoxelID(newIDwoTags, singleCoordinateMasks[0]);
            var parent = voxelDictionaryLevel0.GetVoxel(parentID);
            if (parent == null)
                return new Voxel(newIDwoTags + Constants.SetRoleFlags(level, VoxelRoleTypes.Empty), this);
            if (parent.Role == VoxelRoleTypes.Full)
                return new Voxel(newIDwoTags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);

            var innerVoxels = ((Voxel_Level0_Class)parent).InnerVoxels;
            if (innerVoxels.Length >= level)
            {
                var voxel = innerVoxels[level - 1]?.GetVoxel(newIDwoTags);
                if (voxel == null) return new Voxel(newIDwoTags + Constants.SetRoleFlags(level, VoxelRoleTypes.Empty), this);
                return voxel;
            }

            for (int i = 1; i < level; i++)
            {
                parentID = Constants.MakeParentVoxelID(newIDwoTags, singleCoordinateMasks[i]);
                if (innerVoxels.Length >= i)
                    parent = innerVoxels[i - 1].GetVoxel(parentID);
                if (parent == null || parent.Role == VoxelRoleTypes.Empty)
                    return new Voxel(newIDwoTags + Constants.SetRoleFlags(level, VoxelRoleTypes.Empty), this);
                if (parent.Role == VoxelRoleTypes.Full)
                    return new Voxel(newIDwoTags + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
            }
            throw new Exception("it should not be possible to get here.");
        }


        #region Get Functions

        /// <summary>
        /// Gets the Voxels with a specified role.
        /// </summary>
        /// <param name="role">The role.</param>
        /// <param name="voxelLevel">The voxel level.</param>
        /// <param name="onlyThisLevel">if set to <c>true</c> [only this level].</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        /// <exception cref="ArgumentException">Specifying voxels at a level that is finer than created.</exception>
        public IEnumerable<IVoxel> Voxels(VoxelRoleTypes role, VoxelDiscretization voxelLevel = VoxelDiscretization.ExtraFine,
            bool onlyThisLevel = false)
        {
            var level = (int)voxelLevel;
            if (level > numberOfLevels)
            {
                if (onlyThisLevel) throw new ArgumentException("Specifying voxels at a level that is finer than created.");
                level = numberOfLevels;
            }
            if ((onlyThisLevel && level == 0) || (!onlyThisLevel && level >= 0))
                foreach (var v in voxelDictionaryLevel0.Where(v => v.Role == role)) yield return v;
            if ((onlyThisLevel && level == 1) || (!onlyThisLevel && level >= 1))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 1)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 1, role)))
                    yield return v;
            if ((onlyThisLevel && level == 2) || (!onlyThisLevel && level >= 2))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 2)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 2, role)))
                    yield return v;
            if ((onlyThisLevel && level == 3) || (!onlyThisLevel && level >= 3))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 3)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 3, role)))
                    yield return v;
            if ((onlyThisLevel && level == 4) || (!onlyThisLevel && level >= 4))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 4)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 4, role)))
                    yield return v;
        }

        /// <summary>
        /// Gets the Voxels with a specified voxel level.
        /// </summary>
        /// <param name="voxelLevel">The voxel level.</param>
        /// <param name="onlyThisLevel">if set to <c>true</c> [only this level].</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        /// <exception cref="ArgumentException">Specifying voxels at a level that is finer than created.</exception>
        public IEnumerable<IVoxel> Voxels(VoxelDiscretization voxelLevel = VoxelDiscretization.ExtraFine, bool onlyThisLevel = false)
        {
            var level = (int)voxelLevel;
            if (level > numberOfLevels)
            {
                if (onlyThisLevel) throw new ArgumentException("Specifying voxels at a level that is finer than created.");
                level = numberOfLevels;
            }
            if ((onlyThisLevel && level == 0) || (!onlyThisLevel && level >= 0))
                foreach (var v in voxelDictionaryLevel0) yield return v;
            if ((onlyThisLevel && level == 1) || (!onlyThisLevel && level >= 1))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 1)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 1)))
                    yield return v;
            if ((onlyThisLevel && level == 2) || (!onlyThisLevel && level >= 2))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 2)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 2)))
                    yield return v;
            if ((onlyThisLevel && level == 3) || (!onlyThisLevel && level >= 3))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 3)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 3)))
                    yield return v;
            if ((onlyThisLevel && level == 4) || (!onlyThisLevel && level >= 4))
                foreach (var v in voxelDictionaryLevel0
                    .Where(vb => ((Voxel_Level0_Class)vb).InnerVoxels != null &&
                                 ((Voxel_Level0_Class)vb).InnerVoxels.Length >= 4)
                    .SelectMany(vb => EnumerateHighLevelVoxelsFromLevel0((Voxel_Level0_Class)vb, 4)))
                    yield return v;
        }

        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel,
            int level)
        {
            if (voxel.InnerVoxels[level - 1] != null)
                foreach (var vx in voxel.InnerVoxels[level - 1])
                    yield return vx;
        }
        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel,
            int level, VoxelRoleTypes role)
        {
            foreach (var vx in voxel.InnerVoxels[level - 1])
                if (vx.Role == role)
                    yield return vx;
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
            // can't this section shift be combined with first? No, when rightshifting bits, the newest
            // bits entering from the left will be 1's if the MSB is 1. Thus, we do it after the mask
            var maxValue = Constants.MaxForSingleCoordinate >> singleCoordinateShifts[level];
            if ((coordValue == 0 && !positiveStep) || (positiveStep && coordValue == maxValue))
            {
                //then stepping outside of entire bounds!
                neighborHasDifferentParent = true;
                return null;
            }
            var maxForThisLevel = voxelsPerSide[level] - 1;
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
            var parentID = Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[0]);
            var level0Parent = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(parentID);
            if (level0Parent == null)
                return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                 + Constants.SetRoleFlags(0, VoxelRoleTypes.Empty), this);
            if (level0Parent.Role == VoxelRoleTypes.Full)
                return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                 + Constants.SetRoleFlags(0, VoxelRoleTypes.Full), this);
            if (parentLevel == 0) return level0Parent;
            //now for childlevels 2,3, 4 or parent levels 1, 2, 3
            parentID = Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel]);
            var parent = level0Parent.InnerVoxels[parentLevel - 1].GetVoxel(parentID);
            if (parent != null) return parent;
            // so the rest of this should be either fulls or empties as there is no immediate partial parent
            if (parentLevel == 1)
                return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                 + Constants.SetRoleFlags(parentLevel, VoxelRoleTypes.Empty), this);
            //now for childlevels 3, 4 or parent levels 2, 3
            parentID = Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel - 1]); // which would be either 1, or 2 - the grandparent
            parent = level0Parent.InnerVoxels[parentLevel - 2].GetVoxel(parentID);
            if (parent != null || parentLevel == 2)
            {
                if (parent?.Role == VoxelRoleTypes.Full)
                    return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                     + Constants.SetRoleFlags(parentLevel, VoxelRoleTypes.Full), this);
                else return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                                + Constants.SetRoleFlags(parentLevel, VoxelRoleTypes.Empty), this);
            }
            parentID = Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel - 2]); // which would be 1 - the great-grandparent of a voxels at 4
            parent = level0Parent.InnerVoxels[parentLevel - 3].GetVoxel(parentID);
            if (parent?.Role == VoxelRoleTypes.Full)
                return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                 + Constants.SetRoleFlags(parentLevel, VoxelRoleTypes.Full), this);
            else return new Voxel(Constants.MakeParentVoxelID(child.ID, singleCoordinateMasks[parentLevel])
                                  + Constants.SetRoleFlags(parentLevel, VoxelRoleTypes.Empty), this);
        }

        /// <summary>
        /// Gets the child voxels.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>IEnumerable&lt;IVoxel&gt;.</returns>
        public IEnumerable<IVoxel> GetChildVoxels(IVoxel parent)
        {
            if (parent == null) return voxelDictionaryLevel0;
            Voxel_Level0_Class level0Parent;
            if (parent is Voxel_Level0_Class)
            {
                level0Parent = (Voxel_Level0_Class)parent;
                if (level0Parent.InnerVoxels != null && level0Parent.InnerVoxels.Length > 0)
                    return level0Parent.InnerVoxels[0];
                return new List<IVoxel>();
            }
            // else the parent is level 1, 2, or 3
            level0Parent = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(Constants.MakeParentVoxelID(parent.ID, singleCoordinateMasks[0]));
            if (level0Parent.InnerVoxels == null || level0Parent.InnerVoxels.Length <= parent.Level)
                return new List<IVoxel>();
            var parentIDwithoutFlags = Constants.ClearFlagsFromID(parent.ID);
            return level0Parent.InnerVoxels[parent.Level].Where(v =>
                Constants.MakeParentVoxelID(v.ID, singleCoordinateMasks[parent.Level]) == parentIDwithoutFlags);
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
                copy.voxelDictionaryLevel0.Add(new Voxel_Level0_Class(voxel.ID, voxel.Role, this));
            foreach (var v in this.voxelDictionaryLevel0.Where(v => v.Role == VoxelRoleTypes.Partial))
            {
                var thisVoxel = (Voxel_Level0_Class)v;
                var copyVoxel = (Voxel_Level0_Class)copy.voxelDictionaryLevel0.GetVoxel(thisVoxel.ID);
                for (int i = 1; i < numberOfLevels; i++)
                {
                    if (thisVoxel.InnerVoxels[i - 1] == null) break;
                    copyVoxel.InnerVoxels[i - 1] = new VoxelHashSet(i, copy);
                    foreach (var innerVoxel in thisVoxel.InnerVoxels[i - 1])
                        copyVoxel.InnerVoxels[i - 1].Add(new Voxel(innerVoxel.ID, copy));
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
                var maxVoxels = (int)Math.Ceiling(dimensions[(int)direction - 1] / VoxelSideLengths[numberOfLevels - 1]);
                Extrude(direction, null, maxVoxels, 0);
            }
            else Extrude(direction, null, int.MaxValue, 0);
            UpdateProperties();
        }

        private bool Extrude(VoxelDirections direction, IVoxel parent, int remainingVoxelLayers,
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
            var voxelsPerLayer = 1;
            for (int i = level + 1; i < numberOfLevels; i++)
                voxelsPerLayer *= voxelsPerSide[i];
            var limit = Math.Min((int)Math.Ceiling(remainingVoxelLayers / (double)voxelsPerLayer), voxelsPerSide[level]);
            /* limit will often be the max. The only time it is not is for positive extrudes that meet the bounding box. */
            var layerOfVoxels = new HashSet<IVoxel>[limit]; /* the voxels are organized into layers */
            for (int i = 0; i < limit; i++)
                layerOfVoxels[i] = new HashSet<IVoxel>();
            Parallel.ForEach(voxels, v =>
            //foreach (var v in voxels)
            {
                var layerIndex = getLayerIndex(v, dimension, level, positiveDir);
                lock (layerOfVoxels[layerIndex])
                    layerOfVoxels[layerIndex].Add(v);
            });
            /* now, for the main loop */
            var innerLimit = limit < voxelsPerSide[level] ? limit : voxelsPerSide[level] + 1;
            var nextLayerCount = 0; /* this is used to count how many in a layer/slice/cross-section are filled up.
                                     * if it hits the max, then the parent voxel below this one should be filled up. */
            for (int i = 0; i < limit; i++)
            { /* cycle over each layer, note that voxels are being removed from subsequent layers so the process should
               * speed up. The loop may not reach ?...wait a second redundant code?  */
              //   if (remainingVoxelLayers < voxelsPerLayer) continue; //return;
                Parallel.ForEach(layerOfVoxels[i], voxel =>
                //  foreach (var voxel in layerOfVoxels[i])
                {
                    #region fill up the layers below this one
                    if (voxel.Role == VoxelRoleTypes.Full
                        || (voxel.Role == VoxelRoleTypes.Partial && level == numberOfLevels - 1))
                    { //if at the lowest level - then treat partial voxel as if it were full
                        nextLayerCount++;
                        bool neighborHasDifferentParent;
                        var neighbor = voxel;
                        var neighborLayer = i;
                        do
                        {
                            neighbor = GetNeighbor(neighbor, direction, out neighborHasDifferentParent);
                            if (neighbor == null) break; // null happens when you go outside of bounds (of coarsest voxels)
                            if (++neighborLayer < innerLimit)
                                neighbor = ChangeVoxelToFull(neighbor);
                            if (!neighborHasDifferentParent && neighborLayer < layerOfVoxels.Length)
                                lock (layerOfVoxels[neighborLayer])
                                    layerOfVoxels[neighborLayer].Remove(neighbor);
                        } while (!neighborHasDifferentParent);
                    }
                    #endregion
                    #region this voxel is partial, so recurse down to fill up sublayer 
                    else if (voxel.Role == VoxelRoleTypes.Partial)
                    {
                        var filledUpNextLayer = Extrude(direction, voxel, remainingVoxelLayers, level + 1);
                        var neighbor = GetNeighbor(voxel, direction, out var neighborHasDifferentParent);
                        if (neighbor == null || layerOfVoxels.Length <= i + 1)  return;  // null happens when you go outside of bounds (of coarsest voxels)
                        if (filledUpNextLayer) neighbor = ChangeVoxelToFull(neighbor);
                        else if (neighbor.Role == VoxelRoleTypes.Empty) neighbor = ChangeVoxelToPartial(neighbor);
                        layerOfVoxels[i + 1].Add(neighbor);
                    }
                    #endregion
                } );
                remainingVoxelLayers -= (int)voxelsPerLayer;
            }
            return nextLayerCount == 256;
        }
        int getLayerIndex(IVoxel v, int dimension, int level, bool positiveStep)
        {
            var layer = (int)((v.ID >> (20 * dimension + 4 + singleCoordinateShifts[level])) & singleCoordinateMasks[level]);
            if (positiveStep) return layer;
            else return (int)singleCoordinateMasks[level] - layer;
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
        public VoxelizedSolid IntersectToNewSolidOLD(params VoxelizedSolid[] references)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.IntersectOLD(references);
            return copy;
        }
        /// <summary>
        /// Intersects this solid with the specified references.
        /// </summary>
        /// <param name="references">The references.</param>
        public void Intersect(params VoxelizedSolid[] references)
        {
            Intersect(null, 0, references, false);
            UpdateProperties();
        }
        public void IntersectOLD(params VoxelizedSolid[] references)
        {
            IntersectOLD(null, 0, references);
            UpdateProperties();
        }

        private void IntersectOLD(IVoxel parent, int level, VoxelizedSolid[] references)
        {
            var voxels = GetChildVoxels(parent);
            Parallel.ForEach(voxels, thisVoxel =>
            //foreach (var thisVoxel in voxels)
            {
                var referenceLowestRole = GetLowestRole(thisVoxel.ID, level, references);
                if (referenceLowestRole == VoxelRoleTypes.Full) return;
                if (referenceLowestRole == VoxelRoleTypes.Empty) ChangeVoxelToEmpty(thisVoxel);
                else
                {
                    if (thisVoxel.Role == VoxelRoleTypes.Full) ChangeVoxelToPartial(thisVoxel);
                    if (numberOfLevels > level)
                        IntersectOLD(thisVoxel, level + 1, references);
                }
            });
        }
        private void Intersect(IVoxel parent, int level, VoxelizedSolid[] references, bool parentWasFull)
        {
            if (parentWasFull)
            {
                // the time-savings here is that creating all sub-voxels is expensive - and then this function will
                // simply delete many of them, so if parent is full, we can look to the first reference
                // to guide us in which sub-voxels to keep.
                var voxel0 = (Voxel_Level0_Class)voxelDictionaryLevel0.GetVoxel(parent.ID);

                var refVoxels = GetChildVoxels(references[0].GetVoxel(parent.ID, parent.Level));
                // cycle over the reference voxels from references[0]
                //Parallel.ForEach(refVoxels, refVoxel =>
                foreach (var refVoxel in refVoxels)
                {
                    var referenceLowestRole = GetLowestRole(refVoxel.ID, level, references);
                    if (referenceLowestRole == VoxelRoleTypes.Full)
                    {
                        var newVoxel = new Voxel(Constants.ClearFlagsFromID(refVoxel.ID)
                                           + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
                        lock (voxel0.InnerVoxels[level - 1])
                            voxel0.InnerVoxels[level - 1].Add(newVoxel);
                    }
                    else if (referenceLowestRole == VoxelRoleTypes.Partial)
                    {
                        var newVoxel = new Voxel(Constants.ClearFlagsFromID(refVoxel.ID)
                                                    + Constants.SetRoleFlags(level, VoxelRoleTypes.Partial), this);
                        lock (voxel0.InnerVoxels[level - 1])
                            voxel0.InnerVoxels[level - 1].Add(newVoxel);
                        if (numberOfLevels > level)
                            Intersect(newVoxel, level + 1, references, true);
                    }
                }//);
            }
            else
            {
                var voxels = GetChildVoxels(parent);
                //Parallel.ForEach(voxels, thisVoxel =>
                foreach (var thisVoxel in voxels)
                {
                    var referenceLowestRole = GetLowestRole(thisVoxel.ID, level, references);
                    if (referenceLowestRole == VoxelRoleTypes.Full) return;
                    if (referenceLowestRole == VoxelRoleTypes.Empty) ChangeVoxelToEmpty(thisVoxel);
                    else
                    {
                        var thisVoxelWasFull = thisVoxel.Role == VoxelRoleTypes.Full;
                        if (thisVoxelWasFull) ChangeVoxelToPartial(thisVoxel, false);
                        if (numberOfLevels > level)
                            Intersect(thisVoxel, level + 1, references, thisVoxelWasFull);
                    }
                } //);
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
            Subtract(null, 0, subtrahends);
            UpdateProperties();
        }

        private void Subtract(IVoxel parent, int level, VoxelizedSolid[] subtrahends)
        {
            var voxels = GetChildVoxels(parent);
            Parallel.ForEach(voxels, thisVoxel =>
            {
                var referenceHighestRole = GetHighestRole(thisVoxel.ID, level, subtrahends);
                if (referenceHighestRole == VoxelRoleTypes.Empty) return;
                if (referenceHighestRole == VoxelRoleTypes.Full) ChangeVoxelToEmpty(thisVoxel);
                else if (numberOfLevels > level)
                {
                    if (thisVoxel.Role == VoxelRoleTypes.Full) ChangeVoxelToPartial(thisVoxel);
                    Subtract(thisVoxel, level + 1, subtrahends);
                }
                else ChangeVoxelToEmpty(thisVoxel);
            });
        }

        #endregion
        #region Union
        /// <exclude />
        public VoxelizedSolid UnionToNewSolid(VoxelizedSolid reference)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Union(null, 0, reference);
            return copy;
        }

        private void Union(IVoxel parent, int level, VoxelizedSolid reference)
        {
            var voxels = reference.GetChildVoxels(parent);
            Parallel.ForEach(voxels, refVoxel =>
            {
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(refVoxel.ID, level);
                if (thisVoxel.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel.Role == VoxelRoleTypes.Empty) ChangeEmptyVoxelToFull(refVoxel.ID, level);
                    else ChangeVoxelToFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel.Role == VoxelRoleTypes.Empty) ChangeEmptyVoxelToPartial(refVoxel.ID, level);
                    if (numberOfLevels > level)
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
            copy.ExclusiveOr(null, 0, reference);
            return copy;
        }

        /// <summary>
        /// Performs the exclusive-or function with the reference and creates a new solid.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="level">The level.</param>
        /// <param name="reference">The reference.</param>
        private void ExclusiveOr(IVoxel parent, int level, VoxelizedSolid reference)
        {
            IEnumerable<IVoxel> voxels;
            if (parent.Role == VoxelRoleTypes.Full)
            {
                var thisIDwoFlags = Constants.ClearFlagsFromID(parent.ID);
                voxels = new List<IVoxel>();
                var xShift = 1L << 4 + 4 * (4 - level);
                var yShift = xShift << 20;
                var zShift = yShift << 20;
                for (int i = 0; i < 16; i++) //todo fix 16
                    for (int j = 0; j < 16; j++)//todo fix 16
                        for (int k = 0; k < 16; k++)//todo fix 16
                            ((List<IVoxel>)voxels).Add(new Voxel(thisIDwoFlags
                                                              + (i * xShift) + (j * yShift) + (k * zShift) + Constants.SetRoleFlags(level + 1, VoxelRoleTypes.Full, true), this));
            }
            else voxels = reference.GetChildVoxels(parent);
            Parallel.ForEach(voxels, refVoxel =>
            {
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(refVoxel.ID, level);
                if (thisVoxel.Role == VoxelRoleTypes.Empty && refVoxel.Role == VoxelRoleTypes.Full)
                    ChangeEmptyVoxelToFull(refVoxel.ID, level);
                else if (thisVoxel.Role == VoxelRoleTypes.Full && refVoxel.Role == VoxelRoleTypes.Full)
                    ChangeVoxelToEmpty(thisVoxel);
                else
                {
                    if (thisVoxel.Role == VoxelRoleTypes.Empty) ChangeEmptyVoxelToPartial(refVoxel.ID, level);
                    else if (thisVoxel.Role == VoxelRoleTypes.Full) ChangeVoxelToPartial(thisVoxel);
                    if (numberOfLevels > level) ExclusiveOr(refVoxel, level + 1, reference);
                }
            });
        }

        #endregion


        VoxelRoleTypes GetLowestRole(long ID, int level, params VoxelizedSolid[] solids)
        {
            var argumentLowest = VoxelRoleTypes.Full;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid.GetVoxel(ID, level);
                if (argVoxel == null || argVoxel.Role == VoxelRoleTypes.Empty)
                    return VoxelRoleTypes.Empty;
                if (argVoxel.Role == VoxelRoleTypes.Partial)
                    argumentLowest = VoxelRoleTypes.Partial;
            }
            return argumentLowest;
        }
        VoxelRoleTypes GetHighestRole(long ID, int level, params VoxelizedSolid[] solids)
        {
            var argumentHighest = VoxelRoleTypes.Empty;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid.GetVoxel(ID, level);
                if (argVoxel == null) continue;
                if (argVoxel.Role == VoxelRoleTypes.Full)
                    return VoxelRoleTypes.Full;
                if (argVoxel.Role == VoxelRoleTypes.Partial)
                    argumentHighest = VoxelRoleTypes.Partial;
            }
            return argumentHighest;
        }

        #endregion

        #region Offset

        /// <summary>
        /// Get the partial voxels ordered along X, Y, or Z, with a dictionary of their distance. X == 0, Y == 1, Z == 2. 
        /// This function does not sort along negative axis.
        /// </summary>
        /// <param name="directionIndex"></param>
        /// <param name="voxelLevel"></param>
        /// <returns></returns>
        private SortedDictionary<long, HashSet<IVoxel>> GetPartialVoxelsOrderedAlongDirection(int directionIndex, int voxelLevel)
        {
            var sortedDict = new SortedDictionary<long, HashSet<IVoxel>>(); //Key = SweepDim Value, value = voxel ID
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
                    sortedDict.Add(coordValue, new HashSet<IVoxel> { voxel });
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
                        if (voxelItem.Value) voxelSolid.ChangeVoxelToPartial(new Voxel(voxelItem.Key, this));
                        else voxelSolid.ChangeEmptyVoxelToFull(voxelItem.Key, voxelLevel);
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
        private static Dictionary<long, bool> GetSphereCenteredOnVoxel(IVoxel iVoxel, List<Tuple<int[], bool>> sphereOffsets)
        {
            var voxel = (Voxel)iVoxel;
            return sphereOffsets.ToDictionary(offsetTuple => AddDeltaToID(voxel.ID, offsetTuple.Item1), offsetTuple => offsetTuple.Item2);
        }

        private Dictionary<long, bool> GetVoxelOffsetsBasedOnNeighbors(IVoxel iVoxel,
            Dictionary<int, List<Tuple<int[], bool>>> offsetsByDirectionCombinations, int voxelLevel)
        {
            //Initialize
            IVoxel voxel;
            if (voxelLevel == 1)
            {
                voxel = (Voxel_ClassWithLinksToTSElements)iVoxel;
            }
            else
            {
                throw new NotImplementedException();

            }

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

        private List<VoxelDirections> GetExposedFaces(IVoxel voxel)
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
        #endregion
    }
}