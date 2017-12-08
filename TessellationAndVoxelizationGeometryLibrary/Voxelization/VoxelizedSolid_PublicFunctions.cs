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

        public IVoxel this[long ID]
        {
            get
            {
                if (voxelDictionaryLevel0.ContainsKey(ID)) return voxelDictionaryLevel0[ID];
                if (voxelDictionaryLevel1.ContainsKey(ID)) return voxelDictionaryLevel1[ID];
                foreach (var voxelLevel0 in voxelDictionaryLevel0.Values)
                {
                    var voxel = voxelLevel0.HighLevelVoxels.GetVoxel(ID);
                    if (voxel != null) return voxel;
                }
                return null;
            }
        }

        bool isFullLevel0(long ID) { return ID >= 2305843009213693952 && ID <= 3458764513820540927; }
        bool isPartialLevel0(long ID) { return ID >= 3458764513820540928 && ID <= 4611686018427387903; }
        bool isFullLevel1(long ID) { return ID >= 5764607523034234880 && ID <= 6917529027641081855; }
        bool isPartialLevel1(long ID) { return ID >= 6917529027641081856 && ID <= 8070450532247928831; }
        bool isFullLevel2(long ID) { return ID <= -8070450532247928833; } //no greater than since that's Long.MinValue and is always true
        bool isPartialLevel2(long ID) { return ID >= -8070450532247928832 && ID <= -6917529027641081857; }
        bool isFullLevel3(long ID) { return ID >= -5764607523034234880 && ID <= -4611686018427387905; }
        bool isPartialLevel3(long ID) { return ID >= -4611686018427387904 && ID <= -3458764513820540929; }
        bool isFullLevel4(long ID) { return ID >= -2305843009213693952 && ID <= -1152921504606846977; }
        bool isPartialLevel4(long ID) { return ID >= -1152921504606846976 && ID <= -1; }

        #region Public Enumerations

        #endregion

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
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -8070450532247928832)))
                    yield return v;
            if (level == 3)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880, -4611686018427387904)))
                    yield return v;
            if (level == 4)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880, -2305843009213693952, -1152921504606846976)))
                    yield return v;
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
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, -8070450532247928832)))
                        yield return v;
            }
            if (level == 3)
            {
                if (role == VoxelRoleTypes.Full)
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, -5764607523034234880)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, - -4611686018427387904)))
                        yield return v;
            }
            if (level == 4)
            {
                if (role == VoxelRoleTypes.Full)
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, -2305843009213693952, -1152921504606846976)))
                        yield return v;
                else
                    foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                        EnumerateHighLevelVoxelsFromLevel0(voxDict, -1152921504606846976)))
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
            var flags = new VoxelRoleTypes[level];
            for (int i = 0; i < level - 1; i++)
                flags[i] = VoxelRoleTypes.Partial;
            flags[level - 1] = role;
            var targetFlags = SetRoleFlags(flags);
            return voxelDictionaryLevel0.Values.SelectMany(voxDict => EnumerateHighLevelVoxelsFromLevel0(voxDict, targetFlags));
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
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -8070450532247928832));
            if (level == 3)
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -5764607523034234880, -4611686018427387904));
            else //level==4
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -2305843009213693952, -1152921504606846976));
        }

        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel, params long[] targetFlags)
        {
            foreach (var vx in voxel.HighLevelVoxels)
            {
                var flags = vx & -1152921504606846976; //get rid of every but the flags
                if (targetFlags.Contains(flags))
                    yield return new Voxel(vx, VoxelSideLengths, Offset);
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
            long coordValue = (voxel.ID >> (20 * (2 - dimension))) & Constants.maskAllButZ;
            //hmm, this 2 minus is clunky. why did I put x last again?
            coordValue = coordValue >> (4 * (4 - voxel.Level));
            var maxValue = Constants.maskAllButZ >> (4 * (4 - voxel.Level));
            if ((coordValue == 0 && !positiveStep) || (positiveStep && coordValue == maxValue))
            {  //then stepping outside of entire bounds!
                neighborHasDifferentParent = true;
                return null;
            }
            var justThisLevelCoordValue = coordValue & 15;
            neighborHasDifferentParent = ((justThisLevelCoordValue == 0 && !positiveStep) || (justThisLevelCoordValue == 15 && positiveStep));
            #endregion
            var delta = 1L;
            delta = delta << ((20 * (2 - dimension)) + 4 * (4 - voxel.Level));
            var newID = (positiveStep) ? voxel.ID + delta : voxel.ID - delta;
            if (voxel.Level == 0)
            {
                if (voxelDictionaryLevel0.ContainsKey(newID)) return voxelDictionaryLevel0[newID];
                return new Voxel(newID + SetRoleFlags(VoxelRoleTypes.Empty), VoxelSideLengths, Offset);
            }
            if (voxel.Level == 1)
            {
                if (voxelDictionaryLevel1.ContainsKey(newID)) return voxelDictionaryLevel1[newID];
                return new Voxel(newID + SetRoleFlags(VoxelRoleTypes.Partial, VoxelRoleTypes.Empty), VoxelSideLengths, Offset);
            }
            var level0ParentID = MakeContainingVoxelID(newID, 0);
            if (voxelDictionaryLevel0.ContainsKey(level0ParentID) &&
                voxelDictionaryLevel0[level0ParentID].HighLevelVoxels.Contains(newID))
                return voxelDictionaryLevel0[level0ParentID].HighLevelVoxels.GetVoxel(newID);
            var flags = new List<VoxelRoleTypes>();
            for (int i = 0; i < voxel.Level; i++) flags.Add(VoxelRoleTypes.Partial);
            flags.Add(VoxelRoleTypes.Empty);
            return new Voxel(newID + SetRoleFlags(flags), VoxelSideLengths, Offset);
        }

        public IVoxel[] GetNeighbors(IVoxel voxel)
        {
            return GetNeighbors(voxel, out bool[] neighborsHaveDifferentParent);
        }

        public IVoxel[] GetNeighbors(IVoxel voxel, out bool[] neighborsHaveDifferentParent)
        {
            var neighbors = new IVoxel[6];
            neighborsHaveDifferentParent = new bool[6];
            var i = 0;
            foreach (var direction in Enum.GetValues(typeof(VoxelDirections)))
                neighbors[i++] = GetNeighbor(voxel, (VoxelDirections)direction, out neighborsHaveDifferentParent[i]);
            return neighbors;
        }

        public IVoxel GetParentVoxel(IVoxel child)
        {
            switch (child.Level)
            {
                case 1: return voxelDictionaryLevel0[MakeContainingVoxelID(child.ID, 0)];
                case 2: return voxelDictionaryLevel1[MakeContainingVoxelID(child.ID, 1)];
                case 3:
                    var roles3 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(MakeContainingVoxelID(child.ID, 2) + SetRoleFlags(roles3), VoxelSideLengths, Offset);
                case 4:
                    var roles4 = new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
                    return new Voxel(MakeContainingVoxelID(child.ID, 3) + SetRoleFlags(roles4), VoxelSideLengths, Offset);
                default: return null;
            }

        }

        public List<IVoxel> GetChildVoxels(IVoxel parent)
        {
            var voxels = new List<IVoxel>();
            if (parent == null)
                voxels.AddRange(voxelDictionaryLevel0.Values);
            else if (parent is Voxel_Level0_Class)
            {
                var IDs = ((Voxel_Level0_Class)parent).NextLevelVoxels;
                voxels.AddRange(IDs.Select(id => voxelDictionaryLevel1[id]));
            }
            else if (parent is Voxel_Level1_Class)
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                voxels.AddRange(IDs.Where(v => isPartialLevel2(v) || isFullLevel2(v))
                    .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset)));
            }
            else
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                if (parent.Level == 2)
                    voxels.AddRange(IDs.Where(v => isPartialLevel3(v) || isFullLevel3(v))
                        .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset)));
                else
                    voxels.AddRange(IDs.Where(v => isPartialLevel4(v) || isFullLevel4(v))
                        .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset)));
            }
            return voxels;
        }
        /// <summary>
        /// Gets the sibling voxels.
        /// </summary>
        /// <param name="siblingVoxel">The sibling voxel.</param>
        /// <returns>List&lt;IVoxel&gt;.</returns>
        public List<IVoxel> GetSiblingVoxels(IVoxel siblingVoxel)
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
            var copy = new VoxelizedSolid(this.Discretization, this.Bounds, this.Units, this.Name, this.FileName, this.Comments);
            foreach (var voxelLevel0Class in this.voxelDictionaryLevel0)
            {
                copy.voxelDictionaryLevel0.Add(voxelLevel0Class.Key, new Voxel_Level0_Class(voxelLevel0Class.Value.CoordinateIndices[0],
                    voxelLevel0Class.Value.CoordinateIndices[1], voxelLevel0Class.Value.CoordinateIndices[2], voxelLevel0Class.Value.Role,
                    this.VoxelSideLengths, this.Offset));
            }
            foreach (var voxelLevel1Class in this.voxelDictionaryLevel1)
            {
                copy.voxelDictionaryLevel1.Add(voxelLevel1Class.Key, new Voxel_Level1_Class(voxelLevel1Class.Value.CoordinateIndices[0],
                    voxelLevel1Class.Value.CoordinateIndices[1], voxelLevel1Class.Value.CoordinateIndices[2], voxelLevel1Class.Value.Role,
                    this.VoxelSideLengths, this.Offset));
            }
            foreach (var voxelLevel0Class in this.voxelDictionaryLevel0)
            {
                var copyVoxel = copy.voxelDictionaryLevel0[voxelLevel0Class.Key];
                copyVoxel.NextLevelVoxels = new VoxelHashSet(voxelLevel0Class.Value.NextLevelVoxels.ToArray(),
                    new VoxelComparerCoarse());
                copyVoxel.HighLevelVoxels = new VoxelHashSet(voxelLevel0Class.Value.HighLevelVoxels.ToArray(),
                    new VoxelComparerFine());
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

        public void Draft(VoxelDirections direction, IVoxel parent = null)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var voxels = GetChildVoxels(parent);
            var layerOfVoxels = new List<IVoxel>[16];

            Parallel.ForEach(voxels, v =>
            //foreach (var voxel0 in voxelDictionaryLevel0.Values)
            {
                var layer = (v.ID >> ((20 * (2 - dimension)) + 4 * (4 - v.Level))) & 15;
                if (positiveStep) layer = 15 - layer;
                lock (layerOfVoxels[layer])
                    layerOfVoxels[layer].Add(v);
            });
            for (int i = 0; i < 16; i++)
            {
                Parallel.ForEach(layerOfVoxels[i], voxel =>
                {
                    if (voxel.Role == VoxelRoleTypes.Full ||
                        (voxel.Role == VoxelRoleTypes.Partial && voxel.Level == discretizationLevel))
                    {
                        var neighbor = GetNeighbor(voxel, direction, out var neighborHasDifferentParent);
                        while (neighbor?.Role != VoxelRoleTypes.Full)
                        {
                            MakeVoxelFull(neighbor);
                            if (neighborHasDifferentParent) break;
                            neighbor = GetNeighbor(neighbor, direction, out neighborHasDifferentParent);
                        }
                    }
                    else if (voxel.Role == VoxelRoleTypes.Partial)
                        Draft(direction, voxel);
                });
            }
        }

        #endregion
        #region Boolean Function (e.g. union, intersect, etc.)

        /// <exclude />
        public VoxelizedSolid IntersectToNewSolid(VoxelizedSolid target, params VoxelizedSolid[] references)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.Intersect(target, references);
            return copy;
        }

        public void Intersect(VoxelizedSolid target, params VoxelizedSolid[] references)
        {
            Parallel.ForEach(voxelDictionaryLevel0, keyValue =>
            {
                var ID = keyValue.Key;
                var targetVoxel = keyValue.Value;
                //if (voxelDictionaryLevel0[ID].Role == VoxelRoleTypes.Empty) return; //I don't think this'll be called
                var referenceLowestRole = GetLowestRole(ID, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty)
                    MakeVoxelEmpty(targetVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(targetVoxel);
                else
                {
                    MakeVoxelPartial(targetVoxel);
                    if (discretizationLevel >= 1)
                        IntersectLevel1(targetVoxel.NextLevelVoxels.ToList(), references);
                }
            });
            UpdateProperties();
        }
        void IntersectLevel1(IList<long> level1Keys, VoxelizedSolid[] references)
        {
            Parallel.ForEach(level1Keys, ID =>
            {
                var targetVoxel = voxelDictionaryLevel1[ID];
                //if (voxelDictionaryLevel1[ID].Role == VoxelRoleTypes.Empty) return;
                var referenceLowestRole = GetLowestRole(ID, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(targetVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(targetVoxel);
                else
                {
                    MakeVoxelPartial(targetVoxel);
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)GetParentVoxel(targetVoxel);
                        IntersectHigherLevels(v0Parent.HighLevelVoxels, references,
                             new List<VoxelRoleTypes> { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                    }
                }
            });
        }

       void IntersectHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid[] references, List<VoxelRoleTypes> flags)
        {
            var flagsPlusPartial = flags.ToList(); // this just copies the list 
            flagsPlusPartial.Add(VoxelRoleTypes.Partial);
            var flagsPlusPartialLong = SetRoleFlags(flagsPlusPartial);
            var flagsPlusFull = flags.ToList(); // this just copies the list
            flagsPlusFull.Add(VoxelRoleTypes.Full);
            var flagsPlusFullLong = SetRoleFlags(flagsPlusFull);
            var listOfIDs = highLevelHashSet.Where(id => (id & Constants.maskAllButFlags) == flagsPlusFullLong ||
                                                       (id & Constants.maskAllButFlags) == flagsPlusPartialLong).ToList();

            Parallel.ForEach(listOfIDs, id =>
            {
                var referenceLowestRole = GetLowestRole(id, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(id);
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(id);
                else
                {
                    MakeVoxelPartial(id);
                    if (discretizationLevel >= flagsPlusPartial.Count)
                        IntersectHigherLevels(highLevelHashSet, references, flagsPlusPartial);
                }
            });
        }

        VoxelRoleTypes GetLowestRole(long ID, params VoxelizedSolid[] solids)
        {
            var argumentLowest = VoxelRoleTypes.Full;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid[ID];
                if (argVoxel == null || argVoxel.Role == VoxelRoleTypes.Empty)
                    return VoxelRoleTypes.Empty;
                if (argVoxel.Role == VoxelRoleTypes.Partial)
                    argumentLowest = VoxelRoleTypes.Partial;
            }
            return argumentLowest;
        }
        VoxelRoleTypes GetHighestRole(long ID, params VoxelizedSolid[] solids)
        {
            var argumentHighest = VoxelRoleTypes.Empty;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid[ID];
                if (argVoxel == null) continue;
                if (argVoxel.Role == VoxelRoleTypes.Full)
                    return VoxelRoleTypes.Full;
                if (argVoxel.Role == VoxelRoleTypes.Partial)
                    argumentHighest = VoxelRoleTypes.Partial;
            }
            return argumentHighest;
        }
        #endregion
    }
}