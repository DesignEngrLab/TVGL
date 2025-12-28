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
        /// Creates a new solid that is the boolean inversion (NOT operation) of the current solid.
        /// All "on" voxels become "off", and all "off" voxels within the bounding box become "on".
        /// </summary>
        /// <returns>A new VoxelizedSolid representing the inversion.</returns>
        /// <remarks>
        /// This operation is equivalent to taking the bounding box of the solid and subtracting the solid from it.
        /// Common search terms: "invert solid", "negate voxels", "boolean not".
        /// </remarks>
        public VoxelizedSolid InvertToNewSolid()
        {
            var vs = Copy();
            vs.Invert();
            return vs;
        }
        // NOT A
        /// <summary>
        /// Performs an in-place boolean inversion (NOT operation) of the current solid.
        /// All "on" voxels become "off", and all "off" voxels within the bounding box become "on".
        /// </summary>
        /// <remarks>
        /// This is a destructive operation that modifies the current solid.
        /// Common search terms: "invert solid", "negate voxels", "boolean not".
        /// </remarks>
        public void Invert()
        {
            UpdateToAllSparse();
            Parallel.ForEach(voxels, vx => vx.Invert(numVoxelsX));
            UpdateProperties();
        }

        // A OR B
        /// <summary>
        /// Creates a new solid that is the boolean union (OR operation) of this solid and one or more other solids.
        /// A voxel will be "on" in the new solid if it is "on" in *any* of the input solids.
        /// </summary>
        /// <param name="solids">An array of VoxelizedSolids to union with this one.</param>
        /// <returns>A new VoxelizedSolid representing the union.</returns>
        /// <remarks>
        /// For a correct union, all solids should have the same voxel side length and be aligned (i.e., have the same transform and bounds).
        /// Common search terms: "voxel union", "combine solids", "boolean OR".
        /// </remarks>
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
        /// Creates a new solid that is the boolean intersection (AND operation) of this solid and one or more other solids.
        /// A voxel will be "on" in the new solid only if it is "on" in *all* of the input solids.
        /// </summary>
        /// <param name="solids">An array of VoxelizedSolids to intersect with this one.</param>
        /// <returns>A new VoxelizedSolid representing the intersection.</returns>
        /// <remarks>
        /// For a correct intersection, all solids should have the same voxel side length and be aligned.
        /// Common search terms: "voxel intersection", "common volume", "boolean AND".
        /// </remarks>
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
        /// <summary>
        /// Slices the solid into two new solids along a plane aligned with one of the Cartesian axes.
        /// </summary>
        /// <param name="vd">The Cartesian direction indicating the slicing plane's normal (e.g., XPositive for a plane with normal +X).</param>
        /// <param name="distance">The zero-based index of the voxel plane to cut before. For example, a distance of 8 will split the solid into voxels 0-7 and voxels 8 to the end.</param>
        /// <returns>A tuple containing two new VoxelizedSolids. The order depends on the direction: for a positive direction, the solid on the positive side of the plane is first.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Thrown if the distance is outside the valid range of voxel indices.</exception>
        /// <remarks>
        /// This is a highly efficient way to cut a voxelized solid, as it operates directly on the voxel rows and indices.
        /// Common search terms: "cut voxel solid", "split solid", "axis-aligned slice".
        /// </remarks>
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

        /// <summary>
        /// Slices the solid into two new solids along an arbitrary plane.
        /// </summary>
        /// <param name="plane">The plane to slice the solid with.</param>
        /// <returns>A tuple containing two new VoxelizedSolids. The first solid is on the positive side of the plane (the side the normal points to).</returns>
        /// <remarks>
        /// This method iterates through the voxel grid and determines which side of the plane each voxel's center lies on. Voxels lying exactly on the plane are assigned to the positive side.
        /// This is more computationally intensive than an axis-aligned slice.
        /// Common search terms: "cut solid with plane", "arbitrary slice", "planar cut".
        /// </remarks>
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
        /// Creates a new solid by drafting (extruding) the current solid along a Cartesian direction.
        /// All voxels are projected onto the plane perpendicular to the draft direction.
        /// </summary>
        /// <param name="direction">The Cartesian direction to draft along.</param>
        /// <returns>A new, drafted VoxelizedSolid.</returns>
        /// <remarks>
        /// This is like shining a flashlight from the specified direction and filling in all the shadows. The resulting solid will have a flat face on the side it was drafted towards.
        /// This is useful for creating mold patterns or checking for undercuts in manufacturing.
        /// Common search terms: "extrude voxels", "draft analysis", "projected volume".
        /// </remarks>
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
