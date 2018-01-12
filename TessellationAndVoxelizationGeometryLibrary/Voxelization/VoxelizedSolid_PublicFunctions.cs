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
        public long Count => _count;
        private long _count;
        public new double Volume => _volume;
        double _volume;

        public long[] GetTotals => _totals;
        long[] _totals;

        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="newID">The new identifier.</param>
        /// <param name="level">The level.</param>
        /// <returns>IVoxel.</returns>
        public IVoxel GetVoxel(long newID, int level = -1)
        {
            Constants.GetRoleFlags(newID, out var levelFromID, out var role, out var btmIsInside);
            if (level == -1) level = levelFromID;
            if (level == 0)
            {
                if (voxelDictionaryLevel0.ContainsKey(newID)) return voxelDictionaryLevel0[newID];
                return new Voxel(newID + Constants.SetRoleFlags(0, VoxelRoleTypes.Empty), this);
            }
            if (level == 1)
            {
                if (voxelDictionaryLevel1.ContainsKey(newID)) return voxelDictionaryLevel1[newID];
                var parentID = Constants.MakeContainingVoxelID(newID, 0);
                var neighborsParent = voxelDictionaryLevel0.ContainsKey(parentID)
                    ? voxelDictionaryLevel0[parentID] : null;
                if (neighborsParent != null && neighborsParent.Role == VoxelRoleTypes.Full)
                    return new Voxel(newID + Constants.SetRoleFlags(1, VoxelRoleTypes.Full), this);
                return new Voxel(newID + Constants.SetRoleFlags(1, VoxelRoleTypes.Empty), this);
            }
            var level0ParentID = Constants.MakeContainingVoxelID(newID, 0);
            var neighborsLevel0Parent = voxelDictionaryLevel0.ContainsKey(level0ParentID)
                ? voxelDictionaryLevel0[level0ParentID] : null;
            if (neighborsLevel0Parent != null)
            {
                var neighbor = neighborsLevel0Parent.HighLevelVoxels.GetVoxel(newID);
                if (neighbor != null) return neighbor;
                var neighborsParent = GetParentVoxel(new Voxel(newID, level));
                if (neighborsParent.Role == VoxelRoleTypes.Full)
                    return new Voxel(newID + Constants.SetRoleFlags(level, VoxelRoleTypes.Full), this);
            }
            return new Voxel(newID + Constants.SetRoleFlags(level, VoxelRoleTypes.Empty), this);
        }



        #region Get Functions

        public IEnumerable<IVoxel> Voxels(VoxelDiscretization voxelLevel = VoxelDiscretization.ExtraFine)
        {
            var level = (int)voxelLevel;
            if (level > discretizationLevel) level = discretizationLevel;
            foreach (var v in voxelDictionaryLevel0.Values.Where(v => v.Role == VoxelRoleTypes.Full))
                yield return v;
            if (level == 0)
                foreach (var v in voxelDictionaryLevel0.Values.Where(v => v.Role == VoxelRoleTypes.Partial))
                    yield return v;
            if (level >= 1)
                foreach (var v in voxelDictionaryLevel1.Values.Where(v => v.Role == VoxelRoleTypes.Full))
                    yield return v;
            if (level == 1)
                foreach (var v in voxelDictionaryLevel1.Values.Where(v => v.Role == VoxelRoleTypes.Partial))
                    yield return v;
            if (level == 2)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 5, 6, 7)))
                    yield return v;
            if (level == 3)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 7, 9, 10, 11)))
                    yield return v;
            if (level == 4)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 7, 11, 13, 14, 15)))
                    yield return v;
        }

        public IEnumerable<IVoxel> Voxels(VoxelRoleTypes role, VoxelDiscretization voxelLevel = VoxelDiscretization.ExtraFine)
        {
            return Voxels(voxelLevel, role);
        }

        public IEnumerable<IVoxel> Voxels(VoxelDiscretization voxelLevel, VoxelRoleTypes role)
        {
            var level = (int)voxelLevel;
            if (level > discretizationLevel) level = discretizationLevel;
            foreach (var v in voxelDictionaryLevel0.Values.Where(v => v.Role == role))
                yield return v;
            if (level >= 1)
                foreach (var v in voxelDictionaryLevel1.Values.Where(v => v.Role == role))
                    yield return v;
            if (level == 2)
            {
                if (role == VoxelRoleTypes.Full)
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 7)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 5, 6)))
                        yield return v;
            }
            if (level == 3)
            {
                if (role == VoxelRoleTypes.Full)
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 11)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 9, 10)))
                        yield return v;
            }
            if (level == 4)
            {
                if (role == VoxelRoleTypes.Full)
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 15)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, 13, 14)))
                        yield return v;
            }
        }

        public IEnumerable<IVoxel> Voxels(VoxelDiscretization voxelLevel, VoxelRoleTypes role, bool onlyThisLevel)
        {
            if (!onlyThisLevel) return Voxels(voxelLevel, role);
            int level = (int)voxelLevel;
            if (level == 0)
                return voxelDictionaryLevel0.Values.Where(v => v.Role == role);
            if (level == 1)
                return voxelDictionaryLevel1.Values.Where(v => v.Role == role);
            if (level > discretizationLevel) level = discretizationLevel;
            var targetFlags = Constants.SetRoleFlags(level, role);
            return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                EnumerateHighLevelVoxelsFromLevel0(voxDict, targetFlags));
        }

        public IEnumerable<IVoxel> Voxels(VoxelDiscretization voxelLevel, bool onlyThisLevel)
        {
            if (!onlyThisLevel) return Voxels(voxelLevel);
            int level = (int)voxelLevel;
            if (level > discretizationLevel) level = discretizationLevel;
            if (level == 0)
                return voxelDictionaryLevel0.Values;
            if (level == 1)
                return voxelDictionaryLevel1.Values;
            if (level > discretizationLevel) level = discretizationLevel;
            if (level == 2)
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 5, 6, 7));
            if (level == 3)
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 9, 10, 11));
            else //level==4
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, 13, 14, 15));
        }

        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel,
            params long[] targetFlags)
        {
            foreach (var vx in voxel.HighLevelVoxels)
            {
                var flags = vx & 15; //get rid of every but the flags
                if (targetFlags.Contains(flags))
                    yield return new Voxel(vx, this);
            }
        }

        public IVoxel GetNeighbor(IVoxel voxel, VoxelDirections direction)
        {
            return GetNeighbor(voxel, direction, out bool dummy);
        }

        public IVoxel GetNeighbor(IVoxel voxel, VoxelDirections direction, out bool neighborHasDifferentParent)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;

            #region Check if steps outside or neighbor has different parent
            long coordValue = Constants.GetCoordinateIndex(voxel.ID, voxel.Level, dimension);
            // can't this section shift be combined with first? No, when rightshifting bits, the newest
            // bits entering from the left will be 1's if the MSB is 1. Thus, we do it after the mask
            var maxValue = Constants.MaxForSingleCoordinate >> (4 * (4 - voxel.Level));
            if ((coordValue == 0 && !positiveStep) || (positiveStep && coordValue == maxValue))
            {
                //then stepping outside of entire bounds!
                neighborHasDifferentParent = true;
                return null;
            }
            var justThisLevelCoordValue = coordValue & 15;
            neighborHasDifferentParent = ((justThisLevelCoordValue == 0 && !positiveStep) ||
                                          (justThisLevelCoordValue == 15 && positiveStep));
            #endregion

            var delta = 1L;
            var shift = 20 * dimension + 4 * (4 - voxel.Level) + 4;
            delta = delta << shift;
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
            var i = 0;
            foreach (var direction in Enum.GetValues(typeof(VoxelDirections)))
                neighbors[i] = GetNeighbor(voxel, (VoxelDirections)direction, out neighborsHaveDifferentParent[i++]);
            return neighbors;
        }

        public IVoxel GetParentVoxel(IVoxel child)
        {
            long parentID;
            IVoxel parent;
            switch (child.Level)
            {
                case 1:
                    parentID = Constants.MakeContainingVoxelID(child.ID, 0);
                    if (voxelDictionaryLevel0.ContainsKey(parentID))
                        return voxelDictionaryLevel0[parentID];
                    return new Voxel(parentID + Constants.SetRoleFlags(0, VoxelRoleTypes.Empty), this);
                case 2:
                    parentID = Constants.MakeContainingVoxelID(child.ID, 1);
                    if (voxelDictionaryLevel1.ContainsKey(parentID))
                        return voxelDictionaryLevel1[parentID];
                    return GetParentVoxel(new Voxel(parentID, 1));
                case 3:
                    parentID = Constants.MakeContainingVoxelID(child.ID, 0);
                    if (voxelDictionaryLevel0.ContainsKey(parentID))
                    {
                        var level0Voxel = voxelDictionaryLevel0[parentID];
                        parentID = Constants.MakeContainingVoxelID(child.ID, 2);
                        parent = level0Voxel.HighLevelVoxels.GetVoxel(parentID);
                        if (parent != null) return parent;
                    }
                    return GetParentVoxel(new Voxel(parentID, 2));
                case 4:
                    parentID = Constants.MakeContainingVoxelID(child.ID, 0);
                    if (voxelDictionaryLevel0.ContainsKey(parentID))
                    {
                        var level0Voxel = voxelDictionaryLevel0[parentID];
                        parentID = Constants.MakeContainingVoxelID(child.ID, 3);
                        parent = level0Voxel.HighLevelVoxels.GetVoxel(parentID);
                        if (parent != null) return parent;
                    }
                    return GetParentVoxel(new Voxel(parentID, 3));
                default: return null;
            }
        }

        public IEnumerable<IVoxel> GetChildVoxels(IVoxel parent)
        {
            if (parent == null) return voxelDictionaryLevel0.Values;
            if (parent is Voxel_Level0_Class)
            {
                var IDs = ((Voxel_Level0_Class)parent).NextLevelVoxels;
                return IDs.Select(id => voxelDictionaryLevel1[id]);
            }
            if (parent is Voxel_Level1_Class)
            {
                var level0 = voxelDictionaryLevel0[Constants.MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                return IDs.Where(isLevel2)
                    .Select(id => (IVoxel)new Voxel(id, this));
            }
            else
            {
                var level0 = voxelDictionaryLevel0[Constants.MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                var parentIDwithoutFlags = Constants.ClearFlagsFromID(parent.ID);
                if (parent.Level == 2)
                    return IDs.Where(v => isLevel3(v) &&
                                          Constants.MakeContainingVoxelID(v, 2) == parentIDwithoutFlags)
                        .Select(id => (IVoxel)new Voxel(id, this));
                return IDs.Where(v => isLevel4(v) &&
                                          Constants.MakeContainingVoxelID(v, 3) == parentIDwithoutFlags)
                        .Select(id => (IVoxel)new Voxel(id, this));
            }
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
            throw new NotImplementedException();
        }

        public override Solid Copy()
        {
            var copy = new VoxelizedSolid(this.Discretization, this.Bounds, this.Units, this.Name, this.FileName,
                this.Comments);
            foreach (var voxelLevel0Class in this.voxelDictionaryLevel0)
            {
                copy.voxelDictionaryLevel0.Add(voxelLevel0Class.Key, new Voxel_Level0_Class(voxelLevel0Class.Key,
                    voxelLevel0Class.Value.Role, this));
            }
            foreach (var voxelLevel1Class in this.voxelDictionaryLevel1)
            {
                copy.voxelDictionaryLevel1.Add(voxelLevel1Class.Key, new Voxel_Level1_Class(voxelLevel1Class.Key,
                    voxelLevel1Class.Value.Role, this));
            }
            foreach (var voxelLevel0Class in this.voxelDictionaryLevel0)
            {
                var copyVoxel = copy.voxelDictionaryLevel0[voxelLevel0Class.Key];
                if (voxelLevel0Class.Value.NextLevelVoxels != null)
                    copyVoxel.NextLevelVoxels = voxelLevel0Class.Value.NextLevelVoxels.Copy(copy);
                if (voxelLevel0Class.Value.HighLevelVoxels != null)
                    copyVoxel.HighLevelVoxels = voxelLevel0Class.Value.HighLevelVoxels.Copy(copy);
            }
            copy.UpdateProperties();
            return copy;
        }

        #endregion

        #region Draft

        public VoxelizedSolid DraftToNewSolid(VoxelDirections direction)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Draft(direction);
            return copy;
        }

        /// <summary>
        /// Drafts the solid in the specified direction.
        /// </summary>
        /// <param name="direction">The direction in which to draft.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public void Draft(VoxelDirections direction)
        {
            Draft(direction, null, direction > 0 ?
                numVoxels[Math.Abs((int)direction) - 1] : int.MaxValue, 0);
        }

        private bool Draft(VoxelDirections direction, IVoxel parent, int remainingVoxelLayers,
            int level)
        {
            var positiveDir = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var voxels = GetChildVoxels(parent);
            var voxelsPerLayer = Math.Pow(16, discretizationLevel - level);
            var limit = Math.Min((int)Math.Ceiling(remainingVoxelLayers / voxelsPerLayer), 16);
            var layerOfVoxels = new HashSet<IVoxel>[limit];
            for (int i = 0; i < limit; i++)
                layerOfVoxels[i] = new HashSet<IVoxel>();
            Parallel.ForEach(voxels, v =>
            {
                var layerIndex = getLayerIndex(v, dimension, level, positiveDir);
                lock (layerOfVoxels[layerIndex])
                    layerOfVoxels[layerIndex].Add(v);
            });
            var innerLimit = limit < 16 ? limit : 17;
            var nextLayerCount = 0;
            for (int i = 0; i < limit; i++)
            {
                Parallel.ForEach(layerOfVoxels[i], voxel =>
                //foreach (var voxel in layerOfVoxels[i])
                {
                    if (remainingVoxelLayers >= voxelsPerLayer && (voxel.Role == VoxelRoleTypes.Full
                        || (voxel.Role == VoxelRoleTypes.Partial && level == discretizationLevel)))
                    {
                        nextLayerCount++;
                        bool neighborHasDifferentParent;
                        var neighbor = voxel;
                        var neighborLayer = i;
                        do
                        {
                            neighbor = GetNeighbor(neighbor, direction, out neighborHasDifferentParent);
                            if (neighbor == null) break; // null happens when you go outside of bounds (of coarsest voxels)
                            if (++neighborLayer < innerLimit) neighbor = MakeVoxelFull(neighbor);
                            if (!neighborHasDifferentParent && neighborLayer < layerOfVoxels.Length)
                                layerOfVoxels[neighborLayer].Remove(neighbor);
                        } while (!neighborHasDifferentParent);
                    }
                    else if (voxel.Role == VoxelRoleTypes.Partial)
                    {
                        var filledUpNextLayer = Draft(direction, voxel, remainingVoxelLayers, level + 1);
                        var neighbor = GetNeighbor(voxel, direction, out var neighborHasDifferentParent);
                        if (neighbor == null || layerOfVoxels.Length <= i + 1) return; // null happens when you go outside of bounds (of coarsest voxels)
                        if (filledUpNextLayer) neighbor = MakeVoxelFull(neighbor);
                        else neighbor = MakeVoxelPartial(neighbor);
                        layerOfVoxels[i + 1].Add(neighbor);
                    }
                });
                remainingVoxelLayers -= (int)voxelsPerLayer;
            }
            return nextLayerCount == 256;
        }
        int getLayerIndex(IVoxel v, int dimension, int level, bool positiveStep)
        {
            var layer = (int)((v.ID >> (20 * dimension + 4 * (4 - level) + 4)) & 15);
            if (positiveStep) return layer;
            else return 15 - layer;
        }

        #endregion
        #region Boolean Function (e.g. union, intersect, etc.)
        #region Intersect
        /// <exclude />
        public VoxelizedSolid IntersectToNewSolid(params VoxelizedSolid[] references)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Intersect(references);
            return copy;
        }

        public void Intersect(params VoxelizedSolid[] references)
        {
            var v0Keys = voxelDictionaryLevel0.Keys.ToList();
            Parallel.ForEach(v0Keys, ID =>
            //foreach (var ID in v0Keys)
            {
                var thisVoxel = voxelDictionaryLevel0[ID];
                //if (voxelDictionaryLevel0[ID].Role == VoxelRoleTypes.Empty) return; //I don't think this'll be called
                var referenceLowestRole = GetLowestRole(thisVoxel.ID, 0, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty)
                    MakeVoxelEmpty(thisVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 1)
                        IntersectLevel1(thisVoxel.NextLevelVoxels.ToList(), references);
                }
                // Well, no need to call the following (make voxel full) since intersect and 
                // subtract only make smaller models
                //else if (referenceLowestRole == VoxelRoleTypes.Full &&
                //         thisVoxel.Role == VoxelRoleTypes.Full) MakeVoxelFull(thisVoxel);
            }  );
            UpdateProperties();
        }
        void IntersectLevel1(IList<long> level1Keys, VoxelizedSolid[] references)
        {
            Parallel.ForEach(level1Keys, ID =>
            //foreach (var ID in level1Keys)
            {
                var thisVoxel = voxelDictionaryLevel1[ID];
                //if (voxelDictionaryLevel1[ID].Role == VoxelRoleTypes.Empty) return;
                var referenceLowestRole = GetLowestRole(thisVoxel.ID, 1, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(thisVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Partial)
                {
                    thisVoxel = (Voxel_Level1_Class)MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)GetParentVoxel(thisVoxel);
                        IntersectHigherLevels(v0Parent.HighLevelVoxels, references, 2);
                    }
                }
                // Well, no need to call the following (make voxel full) since intersect and 
                // subtract only make smaller models
                //else if (referenceLowestRole == VoxelRoleTypes.Full &&
                //         thisVoxel.Role == VoxelRoleTypes.Full) MakeVoxelFull(thisVoxel);
            }  );
        }

        void IntersectHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid[] references, int level)  //List<VoxelRoleTypes> flags)
        {
            List<long> listOfIDs;
            if (level == 2) listOfIDs = highLevelHashSet.Where(isLevel2).ToList();
            else if (level == 3) listOfIDs = highLevelHashSet.Where(isLevel3).ToList();
            else if (level == 4) listOfIDs = highLevelHashSet.Where(isLevel4).ToList();
            else listOfIDs = null;
            level++;
            Parallel.ForEach(listOfIDs, id =>
            {
                var referenceLowestRole = GetLowestRole(id, level, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(new Voxel(id, level));
                else if (referenceLowestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(new Voxel(id, level));
                    if (discretizationLevel >= level)
                        IntersectHigherLevels(highLevelHashSet, references, level);
                }
            });
        }

        #endregion
        #region Subtract
        /// <exclude />
        public VoxelizedSolid SubtractToNewSolid(params VoxelizedSolid[] subtrahends)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Subtract(subtrahends);
            return copy;
        }

        public void Subtract(params VoxelizedSolid[] subtrahends)
        {
            var v0Keys = voxelDictionaryLevel0.Keys.ToList();
            Parallel.ForEach(v0Keys, ID =>
            //foreach (var ID in v0Keys)
            {
                var thisVoxel = voxelDictionaryLevel0[ID];
                //if (voxelDictionaryLevel0[ID].Role == VoxelRoleTypes.Empty) return; //I don't think this'll be called
                var referenceHighestRole = GetHighestRole(ID, 0, subtrahends);
                if (referenceHighestRole == VoxelRoleTypes.Full)
                    MakeVoxelEmpty(thisVoxel);
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 1)
                        SubtractLevel1(thisVoxel.NextLevelVoxels.ToList(), subtrahends);
                }
            }  );
            UpdateProperties();
        }
        void SubtractLevel1(IList<long> level1Keys, VoxelizedSolid[] references)
        {
            Parallel.ForEach(level1Keys, ID =>
            //foreach (var ID in level1Keys)
            {
                var thisVoxel = voxelDictionaryLevel1[ID];
                //if (voxelDictionaryLevel1[ID].Role == VoxelRoleTypes.Empty) return;
                var referenceHighestRole = GetHighestRole(ID, 1, references);
                if (referenceHighestRole == VoxelRoleTypes.Full) MakeVoxelEmpty(thisVoxel);
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    thisVoxel = (Voxel_Level1_Class)MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)GetParentVoxel(thisVoxel);
                        SubtractHigherLevels(v0Parent.HighLevelVoxels, references, 2);
                    }
                }
            } );
        }

        void SubtractHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid[] references, int level)
        {
            List<long> listOfIDs;
            if (level == 2) listOfIDs = highLevelHashSet.Where(isLevel2).ToList();
            else if (level == 3) listOfIDs = highLevelHashSet.Where(isLevel3).ToList();
            else if (level == 4) listOfIDs = highLevelHashSet.Where(isLevel4).ToList();
            else listOfIDs = null;
            level++;
            Parallel.ForEach(listOfIDs, id =>
            {
                var referenceHighestRole = GetHighestRole(id, level, references);
                if (referenceHighestRole == VoxelRoleTypes.Full) MakeVoxelEmpty(new Voxel(id, level));
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(new Voxel(id, level));
                    if (discretizationLevel >= level)
                        SubtractHigherLevels(highLevelHashSet, references, level);
                }
            });
        }

        #endregion
        #region Union
        /// <exclude />
        public VoxelizedSolid UnionToNewSolid(VoxelizedSolid reference)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Union(reference);
            return copy;
        }

        public void Union(VoxelizedSolid reference)
        {
            Parallel.ForEach(reference.voxelDictionaryLevel0, keyValue =>
            {
                var ID = keyValue.Key;
                var refVoxel = keyValue.Value;
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(ID, 0);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel0)
                            voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(ID, VoxelRoleTypes.Full, this));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel0)
                            voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(ID, VoxelRoleTypes.Partial, this));
                    if (discretizationLevel >= 1)
                        UnionLevel1(refVoxel.NextLevelVoxels.ToList(), reference);
                }
            });
            UpdateProperties();
        }
        void UnionLevel1(IList<long> level1Keys, VoxelizedSolid reference)
        {
            Parallel.ForEach(level1Keys, ID =>
            {
                var refVoxel = reference.voxelDictionaryLevel1[ID];
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(ID, 1);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel1)
                            voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(ID, VoxelRoleTypes.Full, this));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                    {
                        lock (voxelDictionaryLevel1)
                            voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(ID, VoxelRoleTypes.Partial, this));
                    }
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)reference.GetParentVoxel(refVoxel);
                        UnionHigherLevels(v0Parent.HighLevelVoxels, reference, 2);
                    }
                }
            });
        }

        void UnionHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid reference, int level)
        {
            List<long> listOfIDs;
            if (level == 2) listOfIDs = highLevelHashSet.Where(isLevel2).ToList();
            else if (level == 3) listOfIDs = highLevelHashSet.Where(isLevel3).ToList();
            else if (level == 4) listOfIDs = highLevelHashSet.Where(isLevel4).ToList();
            else listOfIDs = null;
            level++;
            Parallel.ForEach(listOfIDs, id =>
            {
                var refVoxel = reference.GetVoxel(id, level);
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(id, level);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null) MakeVoxelFull(new Voxel(id, level));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null) MakeVoxelPartial(new Voxel(id, level));
                    else MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= level)
                        UnionHigherLevels(highLevelHashSet, reference, level);
                }
            });
        }

        #endregion
        #region ExclusiveOr
        /// <exclude />
        public VoxelizedSolid ExclusiveOrToNewSolid(VoxelizedSolid reference)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.ExclusiveOr(reference);
            return copy;
        }

        public void ExclusiveOr(VoxelizedSolid reference)
        {
            Parallel.ForEach(reference.voxelDictionaryLevel0, keyValue =>
            {
                var ID = keyValue.Key;
                var refVoxel = keyValue.Value;
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(ID, 0);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel0)
                            voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(ID, VoxelRoleTypes.Full, this));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel0)
                            voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(ID, VoxelRoleTypes.Partial, this));
                    if (discretizationLevel >= 1)
                        ExclusiveOrLevel1(refVoxel.NextLevelVoxels.ToList(), reference);
                }
            });
            UpdateProperties();
        }
        void ExclusiveOrLevel1(IList<long> level1Keys, VoxelizedSolid reference)
        {
            Parallel.ForEach(level1Keys, ID =>
            {
                var refVoxel = reference.voxelDictionaryLevel1[ID];
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(ID, 1);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel1)
                            voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(ID, VoxelRoleTypes.Full, this));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        lock (voxelDictionaryLevel1)
                            voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(ID, VoxelRoleTypes.Partial, this));
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)reference.GetParentVoxel(refVoxel);
                        ExclusiveOrHigherLevels(v0Parent.HighLevelVoxels, reference, 2);
                    }
                }
            });
        }

        void ExclusiveOrHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid reference, int level)
        {
            List<long> listOfIDs;
            if (level == 2) listOfIDs = highLevelHashSet.Where(isLevel2).ToList();
            else if (level == 3) listOfIDs = highLevelHashSet.Where(isLevel3).ToList();
            else if (level == 4) listOfIDs = highLevelHashSet.Where(isLevel4).ToList();
            else listOfIDs = null;
            level++;
            Parallel.ForEach(listOfIDs, id =>
            {
                var refVoxel = reference.GetVoxel(id, level);
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(id, level);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null) MakeVoxelFull(new Voxel(id, level));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null) MakeVoxelPartial(new Voxel(id, level));
                    else MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= level)
                        ExclusiveOrHigherLevels(highLevelHashSet, reference, level);
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
                //ToDo: double check this next line / convert this into a Constants function if useful
                long coordValue = (voxel.ID >> (20 * (directionIndex) + 4 * (4 - voxelLevel) + 4))
                                  & (Constants.MaxForSingleCoordinate >> 4 * (4 - voxelLevel));
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
            var offsetValues = GetPartialSolidSphereOffsets(r);

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
                        long coordValue = Constants.GetCoordinateIndex(voxel.ID, voxelLevel, directionIndex);
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
                        if (voxelItem.Value) voxelSolid.MakeVoxelPartial(new Voxel(voxelItem.Key, voxelLevel));
                        else voxelSolid.MakeVoxelFull(new Voxel(voxelItem.Key, voxelLevel));
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
                voxel = (Voxel_Level1_Class)iVoxel;
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
        private static Dictionary<int, List<Tuple<int[], bool>>> GetPartialSolidSphereOffsets(int r)
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

        public static List<int[]> GetHollowSphereOffsets(int r)
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

        public static void GetSolidCubeCenteredOnVoxel(Voxel voxel, ref VoxelizedSolid voxelizedSolid, int length)
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