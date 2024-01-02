// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="VoxelizedSolid.PublicFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;

namespace TVGL
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid : Solid, IEnumerable<(int xIndex, int yIndex, int zIndex)>
    {
        #region Boolean functions
        // NOT A
        /// <summary>
        /// Inverts to new solid. This is a boolean function when all empty voxels in the bounds
        /// of the solid are made full, and all full are made empty. It is essentially a negation operation.
        /// </summary>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid InvertToNewSolid()
        {
            var vs = Copy();
            vs.Invert();
            return vs;
        }
        // NOT A
        /// <summary>
        /// Inverts this instance. This is a boolean function when all empty voxels in the bounds
        /// of the solid are made full, and all full are made empty. It is essentially a negation operation.
        /// </summary>
        public void Invert()
        {
            UpdateToAllSparse();
            Parallel.ForEach(voxels, vx => vx.Invert());
            UpdateProperties();
        }

        // A OR B
        /// <summary>
        /// Unions to new solid. This is a boolean function that returns a union or OR operation on all
        /// the voxels of the presented solids. Note, that solids should have same bounds for correctness.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid UnionToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = Copy();
            vs.Union(solids);
            return vs;
        }
        // A OR B
        /// <summary>
        /// Unions the specified solids. This is a boolean function that returns a union or OR operation on all
        /// the voxels of the presented solids. Note, that solids should have same bounds for correctness.
        /// </summary>
        /// <param name="solids">The solids.</param>
        public void Union(params VoxelizedSolid[] solids)
        {
            UpdateToAllSparse();
            foreach (var solid in solids)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            {
                voxels[i].Union(solids.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }

        // A AND B
        /// <summary>
        /// Intersects to new solid. This is a boolean function that returns an intersection or AND operation on all
        /// the voxels of the presented solids. Note, that solids should have same bounds for correctness.
        /// </summary>
        /// <param name="solids">The solids.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid IntersectToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = Copy();
            vs.Intersect(solids);
            return vs;
        }
        // A AND B
        /// <summary>
        /// Intersects the specified solids. This is a boolean function that returns an intersection or AND operation on all
        /// the voxels of the presented solids. Note, that solids should have same bounds for correctness.
        /// </summary>
        /// <param name="solids">The solids.</param>
        public void Intersect(params VoxelizedSolid[] solids)
        {
            UpdateToAllSparse();
            foreach (var solid in solids)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            {
                voxels[i].Intersect(solids.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }

        // A AND (NOT B)
        /// <summary>
        /// Subtracts to new solid. This is a boolean function that returns a new solid treating "this" solid as the
        /// minuend and all arguments as subtracted from it (or subtrahends).
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid SubtractToNewSolid(params VoxelizedSolid[] subtrahends)
        {
            var vs = Copy();
            vs.Subtract(subtrahends);
            return vs;
        }

        // A AND (NOT B)
        /// <summary>
        /// Subtracts the specified subtrahends. This is a boolean function that removes voxels from "this" solid
        /// (which is treated as the minuend) that are present in any of the arguments (or subtrahends).
        /// </summary>
        /// <param name="subtrahends">The subtrahends.</param>
        public void Subtract(params VoxelizedSolid[] subtrahends)
        {
            UpdateToAllSparse();
            foreach (var solid in subtrahends)
                solid.UpdateToAllSparse();
            Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
            //for (var i = 0; i < numVoxelsY * numVoxelsZ; i++)
            {
                voxels[i].Subtract(subtrahends.Select(s => s.voxels[i]).ToArray());
            });
            UpdateProperties();
        }
        #endregion

        #region Slice Voxel Solids
        // If direction is negative, the negative side solid is in position one of return tuple
        // If direction is positive, the positive side solid is in position one of return tuple
        // distance is the zero-based index of voxel-plane to cut before
        // i.e. distance = 8, would yield one solid with voxels 0 to 7, and one with 8 to end
        // 0 < distance < VoxelsPerSide[cut direction]
        /// <summary>
        /// Slices this solid into two voxelized solids given the plane defined as aligning with
        /// the cartesian axis of the voxelized solid.
        /// </summary>
        /// <param name="vd">The vd.</param>
        /// <param name="distance">The distance.</param>
        /// <returns>System.ValueTuple&lt;VoxelizedSolid, VoxelizedSolid&gt;.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        public (VoxelizedSolid, VoxelizedSolid) SliceOnPlane(CartesianDirections vd, int distance)
        {
            if (distance >= VoxelsPerSide[Math.Abs((int)vd) - 1] || distance < 1)
                throw new ArgumentOutOfRangeException();
            ushort uCutBefore = (ushort)distance;
            ushort top = numVoxelsX;
            var vs1 = Copy();
            var vs2 = Copy();
            switch (vd)
            {
                case CartesianDirections.XPositive:
                    Parallel.ForEach(vs1.voxels, row => row.TurnOffRange(0, uCutBefore));
                    Parallel.ForEach(vs2.voxels, row => row.TurnOffRange(uCutBefore, (ushort)numVoxelsX));
                    break;
                case CartesianDirections.YPositive:
                    Parallel.For(0, numVoxelsZ, k =>
                    {
                        for (var j = 0; j < distance; j++)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        for (var j = distance; j < numVoxelsY; j++)
                            vs2.voxels[j + zMultiplier * k].Clear();
                    });
                    break;
                case CartesianDirections.ZPositive:
                    Parallel.For(0, VoxelsPerSide[1] * distance, i => vs1.voxels[i].Clear());
                    Parallel.For(VoxelsPerSide[1] * distance, numVoxelsY * numVoxelsZ,
                        i => vs2.voxels[i].Clear());
                    break;
                case CartesianDirections.XNegative:
                    Parallel.ForEach(vs2.voxels, row => row.TurnOffRange(0, uCutBefore));
                    Parallel.ForEach(vs1.voxels, row => row.TurnOffRange(uCutBefore, (ushort)numVoxelsX));
                    break;
                case CartesianDirections.YNegative:
                    Parallel.For(0, numVoxelsZ, k =>
                    {
                        for (var j = 0; j < distance; j++)
                            vs2.voxels[j + zMultiplier * k].Clear();
                        for (var j = distance; j < numVoxelsY; j++)
                            vs1.voxels[j + zMultiplier * k].Clear();
                    });
                    break;
                case CartesianDirections.ZNegative:
                    Parallel.For(0, zMultiplier * distance, i => vs2.voxels[i].Clear());
                    Parallel.For(zMultiplier * distance, numVoxelsY * numVoxelsZ,
                        i => vs1.voxels[i].Clear());
                    break;
            }
            vs1.UpdateProperties();
            vs2.UpdateProperties();
            return (vs1, vs2);
        }

        // Solid on positive side of flat is in position one of return tuple
        // Voxels exactly on the plane are assigned to the positive side
        /// <summary>
        /// Slices this solid into two voxelized solids given any provided plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <returns>System.ValueTuple&lt;VoxelizedSolid, VoxelizedSolid&gt;.</returns>
        public (VoxelizedSolid, VoxelizedSolid) SliceOnPlane(Plane plane)
        {
            var vs1 = Copy();
            var vs2 = Copy();

            var normalOfPlane = plane.Normal;
            var distOfPlane = plane.DistanceToOrigin;

            var xOff = Offset[0];
            var yOff = Offset[1];
            var zOff = Offset[2];
            if (normalOfPlane[0].IsNegligible()) //since no x component. we simply clear rows
                Parallel.For(0, numVoxelsZ, k =>
                {
                    for (var j = 0; j < VoxelsPerSide[1]; j++)
                    {
                        var y = yOff + (j + .5) * VoxelSideLength;
                        var z = zOff + (k + .5) * VoxelSideLength;
                        var d = MiscFunctions.DistancePointToPlane(new Vector3(0, y, z), normalOfPlane, distOfPlane);
                        if (d < 0)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        else vs2.voxels[j + zMultiplier * k].Clear();
                    }
                });
            else
            {
                Parallel.For(0, numVoxelsZ, k =>
                //for (int k = 0; k < numVoxelsZ; k++)
                {
                    var z = zOff + (k + .5) * VoxelSideLength;
                    var zComponent = distOfPlane - z * normalOfPlane[2];
                    for (var j = 0; j < VoxelsPerSide[1]; j++)
                    {
                        var y = yOff + (j + .5) * VoxelSideLength;
                        var x = (zComponent - y * normalOfPlane[1]) / normalOfPlane[0];
                        var xIndex = (x - xOff) / VoxelSideLength - 0.5;
                        if (xIndex < 0)
                            vs2.voxels[j + zMultiplier * k].Clear();
                        else if (xIndex > numVoxelsX)
                            vs1.voxels[j + zMultiplier * k].Clear();
                        else
                        {
                            vs1.voxels[j + zMultiplier * k].TurnOffRange(0, (ushort)xIndex);
                            vs2.voxels[j + zMultiplier * k].TurnOffRange((ushort)xIndex,
                                numVoxelsX);
                        }
                    }
                });
            }
            vs1.UpdateProperties();
            vs2.UpdateProperties();
            return (vs1, vs2);
        }
        #endregion

        #region Draft in VoxelDirection
        /// <summary>
        /// Drafts or extrudes the solid in specified direction. This means that the side of the
        /// part opposite the direction will be like the origial and the side facing the direction
        /// will be flat - as if extrude (playdoh style) in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid DraftToNewSolid(CartesianDirections direction)
        {
            var vs = Copy();
            vs.Draft(direction);
            return vs;
        }
        /// <summary>
        /// Drafts or extrudes the solid in specified direction. This means that the side of the
        /// part opposite the direction will be like the origial and the side facing the direction
        /// will be flat - as if extrude (playdoh style) in the specified direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        public void Draft(CartesianDirections direction)
        {
            UpdateToAllSparse();
            if (direction == CartesianDirections.XPositive)
                Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
                {
                    var rowIndices = ((VoxelRowSparse)voxels[i]).indices;
                    if (rowIndices.Any())
                    {
                        var start = rowIndices[0];
                        rowIndices.Clear();
                        rowIndices.Add(start);
                        rowIndices.Add((ushort)(numVoxelsX));
                    }
                });
            else if (direction == CartesianDirections.XNegative)
                Parallel.For(0, numVoxelsY * numVoxelsZ, i =>
                {
                    var rowIndices = ((VoxelRowSparse)voxels[i]).indices;
                    if (rowIndices.Any())
                    {
                        var end = rowIndices.Last();
                        rowIndices.Clear();
                        rowIndices.Add(0);
                        rowIndices.Add(end);
                    }
                });
            else if (direction == CartesianDirections.YPositive)
                Parallel.For(0, numVoxelsZ, k =>
                //for(int k = 0; k < numVoxelsZ; k++)
                {
                    for (int i = 0; i < numVoxelsX; i++)
                    {
                        var j = 0;
                        while (j < numVoxelsY && !this[i, j, k]) j++;
                        for (; j < numVoxelsY; j++)
                            this[i, j, k] = true;
                    }
                });
            else if (direction == CartesianDirections.YNegative)
                Parallel.For(0, numVoxelsZ, k =>
                {
                    for (int i = 0; i < numVoxelsX; i++)
                    {
                        var j = numVoxelsY - 1;
                        while (j >= 0 && !this[i, j, k]) j--;
                        for (; j >= 0; j--)
                            this[i, j, k] = true;
                    }
                });
            else if (direction == CartesianDirections.ZPositive)
                Parallel.For(0, numVoxelsY, j =>
                {
                    for (int i = 0; i < numVoxelsX; i++)
                    {
                        var k = 0;
                        while (k < numVoxelsZ && !this[i, j, k]) k++;
                        for (; k < numVoxelsZ; k++)
                            this[i, j, k] = true;
                    }
                });
            else // if (direction == CartesianDirections.ZNegative)
                Parallel.For(0, numVoxelsY, j =>
                //for (int j = 0; j < numVoxelsY; j++)
          {
              for (int i = 0; i < numVoxelsX; i++)
              {
                  var k = numVoxelsZ - 1;
                  while (k >= 0 && !this[i, j, k]) k--;
                  for (; k >= 0; k--)
                      this[i, j, k] = true;
              }
          });
            UpdateProperties();
        }
        #endregion
    }
}
