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

        public IVoxel GetVoxel(long ID)
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

        bool isFullLevel0(long ID)
        {
            return ID >= 2305843009213693952 && ID <= 3458764513820540927;
        }

        bool isPartialLevel0(long ID)
        {
            return ID >= 3458764513820540928 && ID <= 4611686018427387903;
        }

        bool isFullLevel1(long ID)
        {
            return ID >= 5764607523034234880 && ID <= 6917529027641081855;
        }

        bool isPartialLevel1(long ID)
        {
            return ID >= 6917529027641081856 && ID <= 8070450532247928831;
        }

        bool isFullLevel2(long ID)
        {
            return ID <= -8070450532247928833;
        } //no greater than since that's Long.MinValue and is always true

        bool isPartialLevel2(long ID)
        {
            return ID >= -8070450532247928832 && ID <= -6917529027641081857;
        }

        bool isFullLevel3(long ID)
        {
            return ID >= -5764607523034234880 && ID <= -4611686018427387905;
        }

        bool isPartialLevel3(long ID)
        {
            return ID >= -4611686018427387904 && ID <= -3458764513820540929;
        }

        bool isFullLevel4(long ID)
        {
            return ID >= -2305843009213693952 && ID <= -1152921504606846977;
        }

        bool isPartialLevel4(long ID)
        {
            return ID >= -1152921504606846976 && ID <= -1;
        }

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
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880,
                        -4611686018427387904)))
                    yield return v;
            if (level == 4)
                foreach (var v in voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -5764607523034234880,
                        -2305843009213693952, -1152921504606846976)))
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
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -9223372036854775808, -8070450532247928832));
            if (level == 3)
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -5764607523034234880, -4611686018427387904));
            else //level==4
                return voxelDictionaryLevel0.Values.SelectMany(voxDict =>
                    EnumerateHighLevelVoxelsFromLevel0(voxDict, -2305843009213693952, -1152921504606846976));
        }

        internal IEnumerable<IVoxel> EnumerateHighLevelVoxelsFromLevel0(Voxel_Level0_Class voxel,
            params long[] targetFlags)
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
            delta = delta << ((20 * (2 - dimension)) + 4 * (4 - voxel.Level));
            var newID = (positiveStep)
                ? (voxel.ID & Constants.maskOutFlags) + delta
                : (voxel.ID & Constants.maskOutFlags) - delta;
            if (voxel.Level == 0)
            {
                if (voxelDictionaryLevel0.ContainsKey(newID)) return voxelDictionaryLevel0[newID];
                return new Voxel(newID + SetRoleFlags(VoxelRoleTypes.Empty));
            }
            if (voxel.Level == 1)
            {
                if (voxelDictionaryLevel1.ContainsKey(newID)) return voxelDictionaryLevel1[newID];
                return new Voxel(newID + SetRoleFlags(VoxelRoleTypes.Partial, VoxelRoleTypes.Empty));
            }
            var level0ParentID = MakeContainingVoxelID(newID, 0);
            if (voxelDictionaryLevel0.ContainsKey(level0ParentID) &&
                voxelDictionaryLevel0[level0ParentID].HighLevelVoxels.Contains(newID))
                return voxelDictionaryLevel0[level0ParentID].HighLevelVoxels.GetVoxel(newID);
            var flags = new List<VoxelRoleTypes>();
            for (int i = 0; i < voxel.Level; i++) flags.Add(VoxelRoleTypes.Partial);
            flags.Add(VoxelRoleTypes.Empty);
            return new Voxel(newID + SetRoleFlags(flags));
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
                    return new Voxel(MakeContainingVoxelID(child.ID, 2) + SetRoleFlags(roles3), VoxelSideLengths,
                        Offset);
                case 4:
                    var roles4 = new[]
                    {
                        VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial
                    };
                    return new Voxel(MakeContainingVoxelID(child.ID, 3) + SetRoleFlags(roles4), VoxelSideLengths,
                        Offset);
                default: return null;
            }

        }

        public List<IVoxel> GetChildVoxelslist(IVoxel parent)
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

        public IEnumerable<IVoxel> GetChildVoxels(IVoxel parent)
        {
            var voxels = new List<IVoxel>();
            if (parent == null) return voxelDictionaryLevel0.Values;
            else if (parent is Voxel_Level0_Class)
            {
                var IDs = ((Voxel_Level0_Class)parent).NextLevelVoxels;
                return IDs.Select(id => voxelDictionaryLevel1[id]);
            }
            else if (parent is Voxel_Level1_Class)
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                return IDs.Where(v => isPartialLevel2(v) || isFullLevel2(v))
                    .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset));
            }
            else
            {
                var level0 = voxelDictionaryLevel0[MakeContainingVoxelID(parent.ID, 0)];
                var IDs = level0.HighLevelVoxels;
                var parentIDwithoutFlags = parent.ID & Constants.maskOutFlags;
                if (parent.Level == 2)
                    return IDs.Where(v => (isPartialLevel3(v) || isFullLevel3(v)) &&
                                          (v & Constants.maskAllButLevel01and2) == parentIDwithoutFlags)
                        .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset));
                else
                    return IDs.Where(v => (isPartialLevel4(v) || isFullLevel4(v)) &&
                                          (v & Constants.maskLevel4) == parentIDwithoutFlags)
                        .Select(id => (IVoxel)new Voxel(id, VoxelSideLengths, Offset));
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
                copy.voxelDictionaryLevel0.Add(voxelLevel0Class.Key, new Voxel_Level0_Class(
                    voxelLevel0Class.Value.CoordinateIndices[0],
                    voxelLevel0Class.Value.CoordinateIndices[1], voxelLevel0Class.Value.CoordinateIndices[2],
                    voxelLevel0Class.Value.Role,
                    this.VoxelSideLengths, this.Offset));
            }
            foreach (var voxelLevel1Class in this.voxelDictionaryLevel1)
            {
                copy.voxelDictionaryLevel1.Add(voxelLevel1Class.Key, new Voxel_Level1_Class(
                    voxelLevel1Class.Value.CoordinateIndices[0],
                    voxelLevel1Class.Value.CoordinateIndices[1], voxelLevel1Class.Value.CoordinateIndices[2],
                    voxelLevel1Class.Value.Role,
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

        /*
        public void Draft(VoxelDirections direction)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var profileDict = new Dictionary<long, int[]>(new VoxelComparerTwoDimension(dimension));
            Parallel.ForEach(voxelDictionaryLevel1.Values, voxel =>
            //foreach (var voxelKeyValuePair in voxelDictionaryLevel1)
            {
                int sweepDimValue = int.MaxValue;
                var newProfileVoxel = false;
                lock (profileDict)
                    if (!profileDict.ContainsKey(voxel.ID))
                    {
                        newProfileVoxel = true;
                        profileDict.Add(voxel.ID, );

                    }
                var sortedSets = dict[id];
                var negativeFaceVoxels = sortedSets.Item1;
                var positiveFaceVoxels = sortedSets.Item2;
                var faces = voxel.Faces;
                if (faces.Any(f => f.Normal[sweepDim] >= 0))
                    lock (positiveFaceVoxels) positiveFaceVoxels.Add(voxel);
                if (faces.Any(f => f.Normal[sweepDim] <= 0))
                    lock (negativeFaceVoxels) negativeFaceVoxels.Add(voxel);
            });
            // Parallel.ForEach(dict.Values.Where(v => v.Item1.Any() && v.Item2.Any()), v =>
            foreach (var v in dict.Values.Where(v => v.Item1.Any() && v.Item2.Any()))
                MakeInteriorVoxelsAlongLine(v.Item1, v.Item2, sweepDim);
        }
        */

        public bool Draft(VoxelDirections direction, IVoxel parent = null)
        {
            var positiveStep = direction > 0;
            var dimension = Math.Abs((int)direction) - 1;
            var voxels = GetChildVoxels(parent);
            var layerOfVoxels = new List<IVoxel>[16];
            for (int i = 0; i < 16; i++)
                layerOfVoxels[i] = new List<IVoxel>();
            Parallel.ForEach(voxels, v =>
            //foreach (var v in voxels)
            {
                var layer = draftLayer(v, dimension, positiveStep);
                lock (layerOfVoxels[layer])
                    layerOfVoxels[layer].Add(v);
            });
            var nextLayerCount = 0;
            for (int i = 0; i < 16; i++)
            {
                if (parent == null)
                    Console.WriteLine("layer: {0}", i);
                //Parallel.ForEach(layerOfVoxels[i], voxel =>
                foreach (var voxel in layerOfVoxels[i])
                {
                    if (voxel.Role == VoxelRoleTypes.Full ||
                        (voxel.Role == VoxelRoleTypes.Partial && voxel.Level == discretizationLevel))
                    {
                        nextLayerCount++;
                        bool neighborHasDifferentParent;
                        var neighbor = voxel;
                        do
                        {
                            neighbor = GetNeighbor(neighbor, direction, out neighborHasDifferentParent);
                            if (neighbor == null) break; // null happens when you go outside of bounds (of coarsest voxels)
                            var neighborlayer = draftLayer(neighbor, dimension, positiveStep);
                            layerOfVoxels[neighborlayer].Remove(neighbor);
                            MakeVoxelFull(neighbor);
                        } while (!neighborHasDifferentParent);
                    }
                    else if (voxel.Role == VoxelRoleTypes.Partial)
                        if (Draft(direction, voxel))
                        {
                            var neighbor = GetNeighbor(voxel, direction, out var neighborHasDifferentParent);
                            if (neighbor == null) break; // null happens when you go outside of bounds (of coarsest voxels)
                            if (neighbor.Role == VoxelRoleTypes.Empty)
                                layerOfVoxels[draftLayer(neighbor, dimension, positiveStep)].Add(neighbor);
                            MakeVoxelFull(neighbor);
                        }
                } //);
            }
            return nextLayerCount == 256;
        }
        int draftLayer(IVoxel v, int dimension, bool positiveStep)
        {
            var layer = (int)((v.ID >> ((20 * (2 - dimension)) + 4 * (4 - v.Level))) & 15);
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
            //Parallel.ForEach(voxelDictionaryLevel0, keyValue =>
            foreach (var keyValue in voxelDictionaryLevel0)
            {
                var ID = keyValue.Key;
                var thisVoxel = keyValue.Value;
                //if (voxelDictionaryLevel0[ID].Role == VoxelRoleTypes.Empty) return; //I don't think this'll be called
                var referenceLowestRole = GetLowestRole(ID, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty)
                    MakeVoxelEmpty(thisVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(thisVoxel);
                else
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 1)
                        IntersectLevel1(thisVoxel.NextLevelVoxels.ToList(), references);
                }
            } //);
            UpdateProperties();
        }
        void IntersectLevel1(IList<long> level1Keys, VoxelizedSolid[] references)
        {
            //Parallel.ForEach(level1Keys, ID =>
            foreach (var ID in level1Keys)
            {
                var thisVoxel = voxelDictionaryLevel1[ID];
                //if (voxelDictionaryLevel1[ID].Role == VoxelRoleTypes.Empty) return;
                var referenceLowestRole = GetLowestRole(ID, references);
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(thisVoxel);
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(thisVoxel);
                else
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)GetParentVoxel(thisVoxel);
                        IntersectHigherLevels(v0Parent.HighLevelVoxels, references,
                             new List<VoxelRoleTypes> { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                    }
                }
            }  //);
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
                if (referenceLowestRole == VoxelRoleTypes.Empty) MakeVoxelEmpty(new Voxel(id));
                else if (referenceLowestRole == VoxelRoleTypes.Full) MakeVoxelFull(new Voxel(id));
                else
                {
                    MakeVoxelPartial(new Voxel(id));
                    if (discretizationLevel >= flagsPlusPartial.Count)
                        IntersectHigherLevels(highLevelHashSet, references, flagsPlusPartial);
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
            Parallel.ForEach(voxelDictionaryLevel0, keyValue =>
            {
                var ID = keyValue.Key;
                var thisVoxel = keyValue.Value;
                //if (voxelDictionaryLevel0[ID].Role == VoxelRoleTypes.Empty) return; //I don't think this'll be called
                var referenceHighestRole = GetHighestRole(ID, subtrahends);
                if (referenceHighestRole == VoxelRoleTypes.Full)
                    MakeVoxelEmpty(thisVoxel);
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 1)
                        SubtractLevel1(thisVoxel.NextLevelVoxels.ToList(), subtrahends);
                }
            });
            UpdateProperties();
        }
        void SubtractLevel1(IList<long> level1Keys, VoxelizedSolid[] references)
        {
            Parallel.ForEach(level1Keys, ID =>
            {
                var thisVoxel = voxelDictionaryLevel1[ID];
                //if (voxelDictionaryLevel1[ID].Role == VoxelRoleTypes.Empty) return;
                var referenceHighestRole = GetHighestRole(ID, references);
                if (referenceHighestRole == VoxelRoleTypes.Full) MakeVoxelEmpty(thisVoxel);
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)GetParentVoxel(thisVoxel);
                        SubtractHigherLevels(v0Parent.HighLevelVoxels, references,
                             new List<VoxelRoleTypes> { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                    }
                }
            });
        }

        void SubtractHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid[] references, List<VoxelRoleTypes> flags)
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
                var referenceHighestRole = GetHighestRole(id, references);
                if (referenceHighestRole == VoxelRoleTypes.Full) MakeVoxelEmpty(new Voxel(id));
                else if (referenceHighestRole == VoxelRoleTypes.Partial)
                {
                    MakeVoxelPartial(new Voxel(id));
                    if (discretizationLevel >= flagsPlusPartial.Count)
                        SubtractHigherLevels(highLevelHashSet, references, flagsPlusPartial);
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
                var thisVoxel = GetVoxel(ID);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(
                      refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                      VoxelRoleTypes.Full, VoxelSideLengths, Offset));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Partial, VoxelSideLengths, Offset));
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
                var thisVoxel = GetVoxel(ID);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Full, VoxelSideLengths, Offset));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Partial, VoxelSideLengths, Offset));
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)reference.GetParentVoxel(refVoxel);
                        UnionHigherLevels(v0Parent.HighLevelVoxels, reference,
                             new List<VoxelRoleTypes> { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                    }
                }
            });
        }

        void UnionHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid reference, List<VoxelRoleTypes> flags)
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
                var refVoxel = reference.GetVoxel(id);
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(id);
                if (thisVoxel?.Role == VoxelRoleTypes.Full) return;
                if (refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null) MakeVoxelFull(new Voxel(id));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null) MakeVoxelPartial(new Voxel(id));
                    else MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= flagsPlusPartial.Count)
                        UnionHigherLevels(highLevelHashSet, reference, flagsPlusPartial);
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
                var thisVoxel = GetVoxel(ID);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(
                      refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                      VoxelRoleTypes.Full, VoxelSideLengths, Offset));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel0.Add(ID, new Voxel_Level0_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Partial, VoxelSideLengths, Offset));
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
                var thisVoxel = GetVoxel(ID);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Full, VoxelSideLengths, Offset));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null)
                        voxelDictionaryLevel1.Add(ID, new Voxel_Level1_Class(
                            refVoxel.CoordinateIndices[0], refVoxel.CoordinateIndices[1], refVoxel.CoordinateIndices[2],
                            VoxelRoleTypes.Partial, VoxelSideLengths, Offset));
                    if (discretizationLevel >= 2)
                    {
                        var v0Parent = (Voxel_Level0_Class)reference.GetParentVoxel(refVoxel);
                        ExclusiveOrHigherLevels(v0Parent.HighLevelVoxels, reference,
                             new List<VoxelRoleTypes> { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial });
                    }
                }
            });
        }

        void ExclusiveOrHigherLevels(VoxelHashSet highLevelHashSet, VoxelizedSolid reference, List<VoxelRoleTypes> flags)
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
                var refVoxel = reference.GetVoxel(id);
                if (refVoxel.Role == VoxelRoleTypes.Empty) return;
                var thisVoxel = GetVoxel(id);
                if ((thisVoxel == null || thisVoxel.Role == VoxelRoleTypes.Empty) && refVoxel.Role == VoxelRoleTypes.Full)
                {
                    if (thisVoxel == null) MakeVoxelFull(new Voxel(id));
                    else MakeVoxelFull(thisVoxel);
                }
                else
                {
                    if (thisVoxel == null) MakeVoxelPartial(new Voxel(id));
                    else MakeVoxelPartial(thisVoxel);
                    if (discretizationLevel >= flagsPlusPartial.Count)
                        ExclusiveOrHigherLevels(highLevelHashSet, reference, flagsPlusPartial);
                }
            });
        }

        #endregion


        VoxelRoleTypes GetLowestRole(long ID, params VoxelizedSolid[] solids)
        {
            var argumentLowest = VoxelRoleTypes.Full;
            foreach (var voxelizedSolid in solids)
            {
                var argVoxel = voxelizedSolid.GetVoxel(ID);
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
                var argVoxel = voxelizedSolid.GetVoxel(ID);
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