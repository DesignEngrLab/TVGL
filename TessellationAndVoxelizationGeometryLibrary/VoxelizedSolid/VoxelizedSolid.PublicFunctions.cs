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
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid : Solid, IEnumerable<(int xIndex, int yIndex, int zIndex)>
    {
        #region Public Methods that Branch
        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int xCoord, int yCoord, int zCoord]
        {
            get
            {
                if (xCoord >= numVoxelsX)
                    // this is needed because the end voxel index in sparse is sometimes
                    // set to ushort.MaxValue
                    return false;
                return voxels[yCoord + zMultiplier * zCoord][xCoord];
            }
            set
            {
                voxels[yCoord + zMultiplier * zCoord][xCoord] = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="System.Boolean" /> at the specified coordinate.
        /// true corresponds to "on" and false to "off".
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool this[int[] coordinates]
        {
            get => this[coordinates[0], coordinates[1], coordinates[2]];
            set => this[coordinates[0], coordinates[1], coordinates[2]] = value;
        }

        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is true if there are neighbors and false if there are none.
        /// the neighbors array is the coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="neighbors">The neighbors.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool GetNeighbors(int xCoord, int yCoord, int zCoord, out int[][] neighbors)
        {
            neighbors = GetNeighbors(xCoord, yCoord, zCoord);
            return neighbors.Any(n => n != null);
        }

        /// <summary>
        /// Gets the neighbors of the specified voxel position (even if specified is an off-voxel).
        /// The result is an array of coordinates or nulls. Where the null represents off-voxels.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns>System.Int32[][].</returns>
        public int[][] GetNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (xNeighbors.Item1)
                neighbors[0] = new[] { xCoord - 1, yCoord, zCoord };
            if (xNeighbors.Item2)
                neighbors[1] = new[] { xCoord + 1, yCoord, zCoord };

            if (yCoord > 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord])
                neighbors[2] = new[] { xCoord, yCoord - 1, zCoord };
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord])
                neighbors[3] = new[] { xCoord, yCoord + 1, zCoord };

            if (zCoord > 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord])
                neighbors[4] = new[] { xCoord, yCoord, zCoord - 1 };
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord])
                neighbors[5] = new[] { xCoord, yCoord, zCoord + 1 };

            return neighbors;
        }


        /// <summary>
        /// Returns the number of adjacent voxels (0 to 6)
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns>System.Int32.</returns>
        public int NumNeighbors(int xCoord, int yCoord, int zCoord)
        {
            var neighbors = 0;

            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (xNeighbors.Item1) neighbors++;
            if (xNeighbors.Item2) neighbors++;

            if (yCoord != 0 && voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]) neighbors++;
            if (yCoord + 1 < numVoxelsY && voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]) neighbors++;

            if (zCoord != 0 && voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]) neighbors++;
            if (zCoord + 1 < numVoxelsZ && voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord]) neighbors++;

            return neighbors;
        }

        public bool IsExposed(int xCoord, int yCoord, int zCoord)
        {
            if (!this[xCoord, yCoord, zCoord]) return false;
            var xNeighbors = voxels[yCoord + zMultiplier * zCoord].GetNeighbors(xCoord, numVoxelsX);
            if (!xNeighbors.Item1) return true;
            if (!xNeighbors.Item2) return true;
            if (yCoord == 0 || yCoord + 1 >= numVoxelsY || zCoord == 0 || zCoord + 1 >= numVoxelsZ)
                return true;
            if (!voxels[yCoord - 1 + zMultiplier * zCoord][xCoord]) return true;
            if (!voxels[yCoord + 1 + zMultiplier * zCoord][xCoord]) return true;
            if (!voxels[yCoord + zMultiplier * (zCoord - 1)][xCoord]) return true;
            if (!voxels[yCoord + zMultiplier * (zCoord + 1)][xCoord]) return true;
            return false;
        }
        #endregion

        #region Set/update properties
        /// <summary>
        /// Updates the properties.
        /// </summary>
        /// <font color="red">Badly formed XML comment.</font>
        private void UpdateProperties()
        {
            for (int k = 0; k < voxels.Length; k++)
            {
                VoxelRowBase vx = voxels[k];
                if (vx == null) continue;
                var sparse = ((VoxelRowSparse)vx);
                if (sparse.indices.Count % 2 == 1)
                    Console.WriteLine("--------------------------------------------bad ");
                for (int i = 1; i < sparse.indices.Count; i++)
                    if (sparse.indices[i] <= sparse.indices[i - 1])
                        Console.WriteLine("-----------------------------------------------------bad ");
            }
            CalculateCenter();
            CalculateVolume();
        }


        #endregion

        #region Boolean functions
        // NOT A
        /// <summary>
        /// Inverts to new solid. This is a boolean function when all empty voxels in the bounds
        /// of the solid are made full, and all full are made empty. It is essentially a negation operation.
        /// </summary>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid InvertToNewSolid()
        {
            var vs = (VoxelizedSolid)Copy();
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
            var vs = (VoxelizedSolid)Copy();
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
            var vs = (VoxelizedSolid)Copy();
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
            var vs = (VoxelizedSolid)Copy();
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
            ushort top = (ushort)numVoxelsX;
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();
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
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();

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
                                (ushort)(numVoxelsX));
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
            var vs = (VoxelizedSolid)Copy();
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

        #region Voxel erosion
        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// This creates a new solid. The original is unaltered.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">Size of the mask.</param>
        /// <param name="maskOptions">The mask options.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid DirectionalErodeToConstraintToNewSolid(in VoxelizedSolid constraintSolid, Vector3 dir,
            double tLimit = 0, double maskSize = 0, params string[] maskOptions)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.DirectionalErodeToConstraint(constraintSolid, dir.Normalize(), tLimit, maskSize, maskOptions);
            return copy;
        }

        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// This creates a new solid. The orinal is unaltered.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">Size of the mask.</param>
        /// <param name="maskOptions">The mask options.</param>
        /// <returns>VoxelizedSolid.</returns>
        public VoxelizedSolid DirectionalErodeToConstraintToNewSolid(in VoxelizedSolid constraintSolid, CartesianDirections dir,
            double tLimit = 0, double maskSize = 0, params string[] maskOptions)
        {
            var copy = (VoxelizedSolid)Copy();
            var tDir = Vector3.UnitVector(dir);
            copy.DirectionalErodeToConstraint(constraintSolid, tDir, tLimit, maskSize, maskOptions);
            return copy;
        }

        /// <summary>
        /// Erodes the solid in the supplied direction until the mask contacts the constraint solid.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <param name="maskSize">The tool dia.</param>
        /// <param name="maskOptions">The mask options.</param>
        private void DirectionalErodeToConstraint(VoxelizedSolid constraintSolid, Vector3 dir,
            double tLimit, double maskSize, params string[] maskOptions)
        {
            var dirX = dir[0];
            var dirY = dir[1];
            var dirZ = dir[2];
            var signX = (byte)(Math.Sign(dirX) + 1);
            var signY = (byte)(Math.Sign(dirY) + 1);
            var signZ = (byte)(Math.Sign(dirZ) + 1);
            var xLim = numVoxelsX;
            var yLim = numVoxelsY;
            var zLim = numVoxelsZ;

            tLimit = tLimit <= 0 ? Math.Sqrt(VoxelsPerSide.Sum(i => i * i)) : tLimit / VoxelSideLength;
            var mLimit = tLimit + Math.Sqrt(VoxelsPerSide.Sum(i => i * i));
            var mask = CreateProjectionMask(dir, mLimit);
            var starts = GetAllVoxelsOnBoundingSurfaces(dirX, dirY, dirZ, maskSize);
            var sliceMask = ThickenMask(mask[0], dir, maskSize, maskOptions);

            Parallel.ForEach(starts, vox =>
                ErodeMask(constraintSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox));
            //foreach (var vox in starts)
            //    ErodeMask(constraintSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox);
            UpdateProperties();
        }

        /// <summary>
        /// Gets the voxel directions.
        /// </summary>
        /// <param name="dirX">The dir x.</param>
        /// <param name="dirY">The dir y.</param>
        /// <param name="dirZ">The dir z.</param>
        /// <returns>IEnumerable&lt;CartesianDirections&gt;.</returns>
        private static IEnumerable<CartesianDirections> GetVoxelDirections(double dirX, double dirY, double dirZ)
        {
            var dirs = new List<CartesianDirections>();
            var signedDir = new[] { Math.Sign(dirX), Math.Sign(dirY), Math.Sign(dirZ) };
            for (var i = 0; i < 3; i++)
            {
                if (signedDir[i] == 0) continue;
                dirs.Add((CartesianDirections)((i + 1) * -1 * signedDir[i]));
            }
            return dirs.ToArray();
        }

        /// <summary>
        /// Gets all voxels on bounding surfaces.
        /// </summary>
        /// <param name="dirX">The dir x.</param>
        /// <param name="dirY">The dir y.</param>
        /// <param name="dirZ">The dir z.</param>
        /// <param name="toolDia">The tool dia.</param>
        /// <returns>IEnumerable&lt;System.Int32[]&gt;.</returns>
        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurfaces(double dirX, double dirY, double dirZ,
            double toolDia)
        {
            var surfaceVoxels = new HashSet<int[]>(new SameCoordinates());
            foreach (var direction in GetVoxelDirections(dirX, dirY, dirZ))
            {
                foreach (var vox in GetAllVoxelsOnBoundingSurface(direction, toolDia))
                    surfaceVoxels.Add(vox);
            }
            return surfaceVoxels;
        }

        /// <summary>
        /// Gets all voxels on bounding surface.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="toolDia">The tool dia.</param>
        /// <returns>IEnumerable&lt;System.Int32[]&gt;.</returns>
        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurface(CartesianDirections dir, double toolDia)
        {
            var limit = new int[3][];
            var offset = (int)Math.Ceiling(0.5 * toolDia / VoxelSideLength);

            limit[0] = new[] { 0 - offset, VoxelsPerSide[0] + offset };
            limit[1] = new[] { 0 - offset, VoxelsPerSide[1] + offset };
            limit[2] = new[] { 0 - offset, VoxelsPerSide[2] + offset };

            var ind = Math.Abs((int)dir) - 1;
            if (Math.Sign((int)dir) == 1)
                limit[ind][0] = limit[ind][1] - 1;
            else
                limit[ind][1] = limit[ind][0] + 1;

            var arraySize = (limit[0][1] - limit[0][0]) * (limit[1][1] - limit[1][0]) * (limit[2][1] - limit[2][0]);
            var surfaceVoxels = new int[arraySize][];
            var m = 0;

            for (var i = limit[0][0]; i < limit[0][1]; i++)
                for (var j = limit[1][0]; j < limit[1][1]; j++)
                    for (var k = limit[2][0]; k < limit[2][1]; k++)
                    {
                        surfaceVoxels[m] = new[] { i, j, k };
                        m++;
                    }

            return surfaceVoxels;
        }

        /// <summary>
        /// Erodes the mask.
        /// </summary>
        /// <param name="constraintSolid">The constraint solid.</param>
        /// <param name="mask">The mask.</param>
        /// <param name="signX">The sign x.</param>
        /// <param name="signY">The sign y.</param>
        /// <param name="signZ">The sign z.</param>
        /// <param name="xLim">The x lim.</param>
        /// <param name="yLim">The y lim.</param>
        /// <param name="zLim">The z lim.</param>
        /// <param name="sliceMask">The slice mask.</param>
        /// <param name="start">The start.</param>
        private void ErodeMask(VoxelizedSolid constraintSolid, int[][] mask, byte signX, byte signY,
            byte signZ, int xLim, int yLim, int zLim, int[][] sliceMask, int[] start)
        {
            //var sliceMaskCount = sliceMask.Length;
            var xMask = mask[0][0];
            var yMask = mask[0][1];
            var zMask = mask[0][2];
            var xShift = start[0] - xMask;
            var yShift = start[1] - yMask;
            var zShift = start[2] - zMask;

            var insidePart = false;

            //foreach depth or timestep
            foreach (var initCoord in mask)
            {
                var xStartCoord = initCoord[0] + xShift;
                var yStartCoord = initCoord[1] + yShift;
                var zStartCoord = initCoord[2] + zShift;

                var xTShift = xStartCoord - xMask;
                var yTShift = yStartCoord - yMask;
                var zTShift = zStartCoord - zMask;

                //Iterate over the template of the slice mask
                //to move them to the appropriate location but 
                //need to be sure that we are in the space (not negative)
                //var succeedCounter = 0;
                //var precedeCounter = 0;
                var succeeds = true;
                var precedes = true;
                var outOfBounds = false;

                foreach (var voxCoord in sliceMask)
                {
                    var coordX = voxCoord[0] + xTShift;
                    var coordY = voxCoord[1] + yTShift;
                    var coordZ = voxCoord[2] + zTShift;

                    // 0 is negative dir, 1 is zero, and 2 is positive. E.g. for signX:
                    // 0: [-0.577  0.577  0.577]
                    // 1: [ 0      0.707  0.707]
                    // 2: [ 0.577  0.577  0.577]
                    if (!insidePart && ((signX == 0 && coordX >= xLim) || (signX == 2 && coordX < 0) ||
                                        (signX == 1 && (coordX >= xLim || coordX < 0)) ||
                                        (signY == 0 && coordY >= yLim) || (signY == 2 && coordY < 0) ||
                                        (signY == 1 && (coordY >= yLim || coordY < 0)) ||
                                        (signZ == 0 && coordZ >= zLim) || (signZ == 2 && coordZ < 0) ||
                                        (signZ == 1 && (coordZ >= zLim || coordZ < 0))))
                    {
                        outOfBounds = true;
                        continue;
                    }
                    precedes = false;

                    if (coordX < 0 || coordY < 0 || coordZ < 0 || coordX >= xLim || coordY >= yLim || coordZ >= zLim)
                    {
                        outOfBounds = true;
                        // Return if you've left the part
                        continue;
                    }
                    succeeds = false;
                    if (constraintSolid[coordX, coordY, coordZ]) return;
                }

                if (!insidePart && precedes) continue;
                if (succeeds) return;
                if (!insidePart)
                    insidePart = true;

                foreach (var voxCoord in sliceMask)
                {
                    var coordX = voxCoord[0] + xTShift;
                    var coordY = voxCoord[1] + yTShift;
                    var coordZ = voxCoord[2] + zTShift;
                    if (outOfBounds && (coordX < 0 || coordY < 0 || coordZ < 0 || coordX >= xLim || coordY >= yLim ||
                                        coordZ >= zLim)) continue;
                    this[coordX, coordY, coordZ] = false;
                }
            }
        }
        #endregion


        #region Functions for Dilation (3D Offsetting)
        /// <summary>
        /// Gets the voxels within circle.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="edge">if set to <c>true</c> [edge].</param>
        /// <returns>System.Int32[][].</returns>
        private static int[][] GetVoxelsWithinCircle(Vector3 center, Vector3 dir, double radius, bool edge = false)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());

            var radii = new List<double>();
            if (!edge)
                for (var i = .0; i < radius; i += 0.5)
                    radii.Add(i);
            radii.Add(radius);
            var a = Math.Abs(dir[0]) < 1e-5
                ? new Vector3(0, -dir[2], dir[1]).Normalize()
                : new Vector3(dir[1], -dir[0], 0).Normalize();
            var b = a.Cross(dir);

            foreach (var r in radii)
            {
                var step = 2 * Math.PI / Math.Ceiling(Math.PI * 2 * r / 0.5);
                for (var t = .0; t < 2 * Math.PI; t += step)
                {
                    var x = (int)Math.Floor(center[0] + 0.5 + r * Math.Cos(t) * a[0] + r * Math.Sin(t) * b[0]);
                    var y = (int)Math.Floor(center[1] + 0.5 + r * Math.Cos(t) * a[1] + r * Math.Sin(t) * b[1]);
                    var z = (int)Math.Floor(center[2] + 0.5 + r * Math.Cos(t) * a[2] + r * Math.Sin(t) * b[2]);
                    voxels.Add(new[] { x, y, z });
                }
            }

            return voxels.ToArray();
        }

        /// <summary>
        /// Gets the voxels on cone.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="angle">The angle.</param>
        /// <returns>System.Int32[][].</returns>
        private static int[][] GetVoxelsOnCone(int[] center, Vector3 dir, double radius, double angle)
        {
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

            var a = angle * (Math.PI / 180) / 2;
            var l = radius / Math.Sin(a);
            var numSteps = (int)Math.Ceiling(l / 0.5);
            var lStep = l / numSteps;
            var tStep = lStep * Math.Cos(a);
            var rStep = lStep * Math.Sin(a);

            var c = new Vector3(center[0], center[1], center[2]);
            var cStep = dir * tStep;

            for (var i = 1; i <= numSteps; i++)
            {
                var r = rStep * i;
                c = c - cStep;
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        /// <summary>
        /// Gets the voxels on hemisphere.
        /// </summary>
        /// <param name="center">The center.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>System.Int32[][].</returns>
        private static int[][] GetVoxelsOnHemisphere(int[] center, Vector3 dir, double radius)
        {
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

            var centerDouble = new Vector3(center[0], center[1], center[2]);

            var numSteps = (int)Math.Ceiling(Math.PI * radius / 2 / 0.5);
            var aStep = Math.PI / 2 / numSteps;

            for (var i = 1; i <= numSteps; i++)
            {
                var a = aStep * i;
                var r = radius * Math.Sin(a);
                var tStep = radius * (1 - Math.Cos(a));
                var c = centerDouble.Subtract(dir * tStep);
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        /// <summary>
        /// Thickens the mask.
        /// </summary>
        /// <param name="vox">The vox.</param>
        /// <param name="dir">The dir.</param>
        /// <param name="toolDia">The tool dia.</param>
        /// <param name="maskOptions">The mask options.</param>
        /// <returns>System.Int32[][].</returns>
        private int[][] ThickenMask(int[] vox, Vector3 dir, double toolDia, params string[] maskOptions)
        {
            if (toolDia <= 0) return new[] { vox };

            var radius = 0.5 * toolDia / VoxelSideLength;
            maskOptions = maskOptions.Length == 0 ? new[] { "flat" } : maskOptions;

            switch (maskOptions[0])
            {
                case "ball":
                    return GetVoxelsOnHemisphere(vox, dir, radius);
                case "cone":
                    double angle;
                    if (maskOptions.Length < 2) angle = 118;
                    else if (!double.TryParse(maskOptions[1], out angle))
                        angle = 118;
                    return GetVoxelsOnCone(vox, dir, radius, angle);
                default:
                    var voxDouble = new Vector3(vox[0], vox[1], vox[2]);
                    return GetVoxelsWithinCircle(voxDouble, dir, radius);
            }
        }

        /// <summary>
        /// Creates the projection mask.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <returns>System.Int32[][].</returns>
        private int[][] CreateProjectionMask(Vector3 dir, double tLimit)
        {
            var initCoord = new[] { 0, 0, 0 };
            for (var i = 0; i < 3; i++)
                if (dir[i] < 0) initCoord[i] = VoxelsPerSide[i] - 1;
            var maskVoxels = new List<int[]>(new[] { initCoord });
            var c = new Vector3(initCoord[0] + 0.5, initCoord[1] + 0.5, initCoord[2] + 0.5);
            var ts = FindIntersectionDistances(c, dir, tLimit);
            foreach (var t in ts)
            {
                var cInt = c + (dir * t);
                cInt += new Vector3(
                   Math.Round(cInt.X, 5), Math.Round(cInt.Y, 5), Math.Round(cInt.Z, 5));
                maskVoxels.Add(GetNextVoxelCoord(cInt, dir));
            }
            return maskVoxels.ToArray();
        }

        //Exclusive by default (i.e. if line passes through vertex/edge it ony includes two voxels that are actually passed through)
        /// <summary>
        /// Gets the next voxel coord.
        /// </summary>
        /// <param name="cInt">The c int.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>System.Int32[].</returns>
        private static int[] GetNextVoxelCoord(Vector3 cInt, Vector3 direction)
        {
            var searchDirs = new List<int>();
            for (var i = 0; i < 3; i++)
                if (Math.Abs(direction[i]) > 0.001) searchDirs.Add(i);

            var searchSigns = new[] { 0, 0, 0 };
            foreach (var dir in searchDirs)
                searchSigns[dir] = Math.Sign(direction[dir]);

            var voxel = new int[3];
            for (var i = 0; i < 3; i++)
                if (Math.Sign(direction[i]) == -1)
                    voxel[i] = (int)Math.Ceiling(cInt[i] - 1);
                else voxel[i] = (int)Math.Floor(cInt[i]);

            return voxel;
        }

        //firstVoxel needs to be in voxel coordinates and represent the center of the voxel (i.e. {0.5, 0.5, 0.5})
        /// <summary>
        /// Finds the intersection distances.
        /// </summary>
        /// <param name="firstVoxel">The first voxel.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="tLimit">The t limit.</param>
        /// <returns>System.Double[].</returns>
        private double[] FindIntersectionDistances(Vector3 firstVoxel, Vector3 direction, double tLimit)
        {
            var intersections = new ConcurrentBag<double>();
            var searchDirs = new List<int>();

            for (var i = 0; i < 3; i++)
                if (Math.Abs(direction[i]) > 0.001) searchDirs.Add(i);

            var searchSigns = new[] { 0, 0, 0 };
            var firstInt = new[] { 0, 0, 0 };

            foreach (var dir in searchDirs)
            {
                searchSigns[dir] = Math.Sign(direction[dir]);
                firstInt[dir] = (int)(firstVoxel[dir] + 0.5 * searchSigns[dir]);
            }

            foreach (var dir in searchDirs)
                Parallel.ForEach(searchDirs, dir =>
                {
                    var c = firstVoxel[dir];
                    var d = direction[dir];
                    //var toValue = searchSigns[dir] == -1 ? 0 : voxelsPerDimension[lastLevel][dir];
                    var toValue = searchSigns[dir] == -1 ? VoxelsPerSide[dir] - Math.Ceiling(tLimit) : Math.Ceiling(tLimit);
                    var toInt = Math.Max(toValue, firstInt[dir]) + (searchSigns[dir] == -1 ? 1 : 0);
                    var fromInt = Math.Min(toValue, firstInt[dir]);
                    for (var i = fromInt; i < toInt; i++)
                    {
                        var t = (i - c) / d;
                        if (t <= tLimit) intersections.Add(t);
                    }
                });

            var sortedIntersections = new SortedSet<double>(intersections).ToArray();
            return sortedIntersections;
        }

        public int[] ConvertCoordinatesToIndices(Vector3 coordinates)
        {
            return new int[]
            {
                ConvertXCoordToIndex(coordinates.X),
                ConvertYCoordToIndex(coordinates.Y),
                ConvertZCoordToIndex(coordinates.Z)
            };
        }

        public int ConvertXCoordToIndex(double x) => (int)(inverseVoxelSideLength * (x - Offset.X));
        public int ConvertYCoordToIndex(double y) => (int)(inverseVoxelSideLength * (y - Offset.Y));
        public int ConvertZCoordToIndex(double z) => (int)(inverseVoxelSideLength * (z - Offset.Z));
        private double ConvertXIndexToCoord(int i) => Offset.X + (i + 0.5) * VoxelSideLength;
        public double ConvertYIndexToCoord(int j) => Offset.Y + (j + 0.5) * VoxelSideLength;
        public double ConvertZIndexToCoord(int k) => Offset.Z + (k + 0.5) * VoxelSideLength;

        public Vector3 ConvertIndicesToCoordinates(int[] indices) => new Vector3(ConvertXIndexToCoord(indices[0]),
            ConvertYIndexToCoord(indices[1]), ConvertZIndexToCoord(indices[2]));

        #endregion
    }
}
