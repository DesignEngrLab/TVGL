using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL.DenseVoxels
{
    /// <inheritdoc />
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense
    {
        #region Getting Neighbors
        public bool GetNeighbors(int i, int j, int k, out int[][] neighbors)
        {
            neighbors = new int[][] { null, null, null, null, null, null };
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] != 0) neighbors[1] = new[] { i + 1, j, k };
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] != 0) neighbors[3] = new[] { i, j + 1, k };
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] != 0) neighbors[5] = new[] { i, j, k + 1 };
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors[0] = new[] { i - 1, j, k };
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors[2] = new[] { i, j - 1, k };
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors[4] = new[] { i, j, k - 1 };
            return neighbors.Any(a => a is null);
        }

        public bool GetNeighbors(int i, int j, int k, int xLim, int yLim, int zLim, out int[][] neighbors)
        {
            neighbors = new int[][] { null, null, null, null, null, null };
            if (i + 1 != xLim && Voxels[i + 1, j, k] != 0) neighbors[1] = new[] { i + 1, j, k };
            if (j + 1 != yLim && Voxels[i, j + 1, k] != 0) neighbors[3] = new[] { i, j + 1, k };
            if (k + 1 != zLim && Voxels[i, j, k + 1] != 0) neighbors[5] = new[] { i, j, k + 1 };
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors[0] = new[] { i - 1, j, k };
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors[2] = new[] { i, j - 1, k };
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors[4] = new[] { i, j, k - 1 };
            return neighbors.Any(a => a is null);
        }

        public int NumNeighbors(int i, int j, int k)
        {
            var neighbors = 0;
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] != 0) neighbors++;
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] != 0) neighbors++;
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] != 0) neighbors++;
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors++;
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors++;
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors++;
            return neighbors;
        }

        public int NumNeighbors(int i, int j, int k, int xLim, int yLim, int zLim)
        {
            var neighbors = 0;
            if (i + 1 != xLim && Voxels[i + 1, j, k] != 0) neighbors++;
            if (j + 1 != yLim && Voxels[i, j + 1, k] != 0) neighbors++;
            if (k + 1 != zLim && Voxels[i, j, k + 1] != 0) neighbors++;
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors++;
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors++;
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors++;
            return neighbors;
        }
        #endregion

        #region Set/update properties
        public void UpdateProperties()
        {
            SetCount();
            SetVolume();
            SetSurfaceArea();
        }

        private void SetCount()
        {
            var count = new ConcurrentDictionary<int, int>();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                var counter = 0;
                for (var j = 0; j < yLim; j++)
                for (var k = 0; k < zLim; k++)
                    if (Voxels[i, j, k] != 0)
                        counter++;
                count.TryAdd(i, counter);
            });
            Count = count.Values.Sum();
        }

        private void SetVolume()
        {
            Volume = Count * Math.Pow(VoxelSideLength, 3);
        }

        private void SetSurfaceArea()
        {
            var neighbors = new ConcurrentDictionary<int, int>();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                var neighborCount = 0;
                for (var j = 0; j < yLim; j++)
                for (var k = 0; k < zLim; k++)
                    if (Voxels[i, j, k] != 0)
                        neighborCount += NumNeighbors(i, j, k, xLim, yLim, zLim);
                neighbors.TryAdd(i, neighborCount);
            });
            SurfaceArea = 6 * (Count - neighbors.Values.Sum(x => x / 6)) * Math.Pow(VoxelSideLength, 2);
        }
        #endregion

        #region Solid Method Overrides (Transforms & Copy)
        public override void Transform(double[,] transformMatrix)
        {
            if (TS is null)
                throw new NotImplementedException();
            TS = (TessellatedSolid)TS.TransformToNewSolid(transformMatrix);

            var voxelsOnLongSide = Math.Pow(2, Discretization);

            Bounds[0] = TS.Bounds[0];
            Bounds[1] = TS.Bounds[1];
            for (var i = 0; i < 3; i++)
                Dimensions[i] = Bounds[1][i] - Bounds[0][i];

            //var longestSide = Dimensions.Max();
            VoxelSideLength = Dimensions.Max() / voxelsOnLongSide;
            VoxelsPerSide = Dimensions.Select(d => (int)Math.Round(d / VoxelSideLength)).ToArray();

            Voxels = new byte[VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]];

            VoxelizeSolid(TS);
            UpdateProperties();
        }

        public override Solid TransformToNewSolid(double[,] transformationMatrix)
        {
            if (TS is null)
                throw new NotImplementedException();
            var ts = (TessellatedSolid)TS.TransformToNewSolid(transformationMatrix);
            return new VoxelizedSolidDense(ts, Discretization);

            #region Doesn't work
            //var xLim = VoxelsPerSide[0] - 1;
            //var yLim = VoxelsPerSide[1] - 1;
            //var zLim = VoxelsPerSide[2] - 1;

            //var maxDim = new[] { 0, 0, 0 };
            //var minDim = new[] { xLim, yLim, zLim };

            //for (var i = 0; i <= xLim; i += xLim)
            //    for (var j = 0; j <= yLim; j += yLim)
            //        for (var k = 0; k <= zLim; k += zLim)
            //        {
            //            var newIJK = transformationMatrix.multiply(new[] { i, j, k, 1 }, 4, 4)
            //                .Select(a => (int)Math.Round(a)).ToArray();
            //            for (var m = 0; m < 3; m++)
            //            {
            //                if (newIJK[m] > maxDim[m])
            //                    maxDim[m] = newIJK[m];
            //                if (newIJK[m] < minDim[m])
            //                    minDim[m] = newIJK[m];
            //            }
            //        }

            //var newVoxPerSide = maxDim.subtract(minDim, 3).add(new[] { 1, 1, 1 }, 3);
            //var minBound = transformationMatrix.multiply(Bounds[0].Append(1).ToArray(), 4, 4);
            //var maxBound = transformationMatrix.multiply(Bounds[1].Append(1).ToArray(), 4, 4);
            //var newBound = new[] { minBound, maxBound };
            //var vs = new VoxelizedSolidDense(newVoxPerSide, Discretization, VoxelSideLength, newBound);

            //var xOff = -minDim[0];
            //var yOff = -minDim[1];
            //var zOff = -minDim[2];

            //var xMax = xOff + maxDim[0];
            //var yMax = yOff + maxDim[1];
            //var zMax = zOff + maxDim[2];

            //// This leaves void space if the solid increases in size
            //// Voxels are transformed 1 to 1, and some new voxels are added when the solid
            //// is stretched larger
            //Parallel.For(0, xLim + 1, i =>
            //{
            //    for (var j = 0; j <= yLim; j++)
            //        for (var k = 0; k <= zLim; k++)
            //        {
            //            if (Voxels[i, j, k] == 0) continue;
            //
            //            var newIJK = transformationMatrix.multiply(new[] { i, j, k, 1 }, 4, 4)
            //                .Select(a => (int)Math.Ceiling(a)).ToArray();
            //            if (newIJK[0] > xMax || newIJK[1] > yMax || newIJK[2] > zMax)
            //                throw new NotImplementedException();
            //
            //            if (!GetNeighbors(i, j, k, xLim + 1, yLim + 1, zLim + 1, out var neighbors))
            //            {
            //                vs.Voxels[newIJK[0] + xOff, newIJK[1] + yOff, newIJK[2] + zOff] = 1;
            //                continue;
            //            }
            //
            //            var voxels = new HashSet<int[]>(new SameCoordinates()) {newIJK};
            //
            //            for (var n = 1; n < 6; n += 2)
            //            //foreach (var neighbor in neighbors)
            //            {
            //                var neighbor = neighbors[n];
            //                if (neighbor is null) continue;
            //                var nIJK = transformationMatrix
            //                    .multiply(new[] {neighbor[0], neighbor[1], neighbor[2], 1}, 4, 4)
            //                    .Select(a => (int) Math.Ceiling(a)).ToArray();
            //                voxels.Add(nIJK);
            //
            //                ///////////////////////////////////////////////////////
            //                var direction = nIJK.subtract(newIJK, 4);
            //
            //                ///////////////////////////////////
            //                var intersections = new ConcurrentBag<double>();
            //
            //                var searchDirs = new List<int>();
            //                for (var m = 0; m < 3; m++)
            //                    if (Math.Abs(direction[m]) > 0.001) searchDirs.Add(m);
            //
            //                var searchSigns = new[] { 0, 0, 0 };
            //                foreach (var dir in searchDirs)
            //                    searchSigns[dir] = Math.Sign(direction[dir]);
            //
            //                //foreach (var dir in searchDirs)
            //                foreach (var dir in searchDirs)
            //                {
            //                    var toInt = Math.Max(nIJK[dir], newIJK[dir]);
            //                    var fromInt = Math.Min(nIJK[dir], newIJK[dir]) + 1;
            //                    for (var m = fromInt; m < toInt; m++)
            //                        intersections.Add((m - (newIJK[dir] + .5)) / direction[dir]);
            //                }
            //
            //                var ts = new SortedSet<double>(intersections).ToArray();
            //                ///////////////////////////////////
            //                foreach (var t in ts)
            //                {
            //                    var cInt = newIJK.add(new[] {0.5, 0.5, 0.5 ,0}).add(direction.multiply(t, 4), 4);
            //                    for (var m = 0; m < 3; m++) cInt[m] = Math.Round(cInt[m], 5);
            //                    //voxels.Add(GetNextVoxelCoord(cInt, direction));
            //                    ////////////////
            //                    var voxel = new int[4];
            //                    for (var m = 0; m < 3; m++)
            //                        if (Math.Sign(direction[m]) == -1)
            //                            voxel[m] = (int)Math.Ceiling(cInt[m] - 1);
            //                        else voxel[m] = (int)Math.Floor(cInt[m]);
            //                    voxels.Add(voxel);
            //                    ////////////////
            //                }
            //                ///////////////////////////////////////////////////////
            //            }
            //
            //            foreach (var vox in voxels)
            //                vs.Voxels[vox[0] + xOff, vox[1] + yOff, vox[2] + zOff] = 1;
            //        }
            //});

            //// This leaves a lot of void space if the solid increases in size
            //// Voxels are transformed 1 to 1, and new voxels are not added when the solid
            //// is stretched larger
            //var xLim = VoxelsPerSide[0] - 1;
            //var yLim = VoxelsPerSide[1] - 1;
            //var zLim = VoxelsPerSide[2] - 1;
            //
            //var maxDim = new[] { 0, 0, 0 };
            //var minDim = new[] { xLim, yLim, zLim };
            //
            //for (var i = 0; i <= xLim; i += xLim)
            //    for (var j = 0; j <= yLim; j += yLim)
            //        for (var k = 0; k <= zLim; k += zLim)
            //        {
            //            var newIJK = transformationMatrix.multiply(new[] {i, j, k, 1}, 4, 4)
            //                .Select(a => (int) Math.Round(a)).ToArray();
            //            for (var m = 0; m < 3; m++)
            //            {
            //                if (newIJK[m] > maxDim[m])
            //                    maxDim[m] = newIJK[m];
            //                if (newIJK[m] < minDim[m])
            //                    minDim[m] = newIJK[m];
            //            }
            //        }
            //
            //var newVoxPerSide = maxDim.subtract(minDim, 3).add(new[] { 1, 1, 1 }, 3);
            //var minBound = transformationMatrix.multiply(Bounds[0].Append(1).ToArray(), 4, 4);
            //var maxBound = transformationMatrix.multiply(Bounds[1].Append(1).ToArray(), 4, 4);
            //var newBound = new[] { minBound, maxBound };
            //var vs = new VoxelizedSolidDense(newVoxPerSide, Discretization, VoxelSideLength, newBound);
            //
            //var xOff = -minDim[0];
            //var yOff = -minDim[1];
            //var zOff = -minDim[2];
            //
            //var xMax = xOff + maxDim[0];
            //var yMax = yOff + maxDim[1];
            //var zMax = zOff + maxDim[2];
            //
            //Parallel.For(0, xLim + 1, i =>
            //{
            //    for (var j = 0; j <= yLim; j++)
            //    for (var k = 0; k <= zLim; k++)
            //    {
            //        if (Voxels[i, j, k] == 0) continue;
            //
            //        var newIJK = transformationMatrix.multiply(new[] {i, j, k, 1}, 4, 4)
            //            .Select(a => (int) Math.Ceiling(a)).ToArray();
            //        if (newIJK[0] > xMax || newIJK[1] > yMax || newIJK[2] > zMax)
            //            throw new NotImplementedException();
            //
            //        vs.Voxels[newIJK[0] + xOff, newIJK[1] + yOff, newIJK[2] + zOff] = 1;
            //    }
            //});

            //vs.UpdateProperties();
            //return vs;
            #endregion
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Solid.</returns>
        public override Solid Copy()
        {
            return new VoxelizedSolidDense(this);
        }

        public static VoxelizedSolidDense Copy(VoxelizedSolidDense vs)
        {
            return new VoxelizedSolidDense(vs);
        }
        #endregion

        #region Cut Voxel Solids
        // If vd is negative, the negative side solid is in position one of return tuple
        // If vd is positive, the positive side solid is in position one of return tuple
        // cutBefore is the zero-based index of voxel-plane to cut before
        // i.e. cutBefore = 8, would yield one solid with voxels 0 to 7, and one with 8 to end
        // 0 < cutBefore < VoxelsPerSide[cut direction]
        public (VoxelizedSolidDense, VoxelizedSolidDense) CutSolid(VoxelDirections vd, int cutBefore)
        {
            var cutDir = Math.Abs((int) vd) - 1;
            if (cutBefore >= VoxelsPerSide[cutDir] || cutBefore < 1)
                throw new ArgumentOutOfRangeException();

            var voxelsPerSide1 = VoxelsPerSide.ToArray();
            voxelsPerSide1[cutDir] = cutBefore;
            var voxels1 = new byte[voxelsPerSide1[0], voxelsPerSide1[1], voxelsPerSide1[2]];

            Parallel.For(0, voxelsPerSide1[0], i =>
            {
                for (var j = 0; j < voxelsPerSide1[1]; j++)
                for (var k = 0; k < voxelsPerSide1[2]; k++)
                {
                    voxels1[i, j, k] = Voxels[i, j, k];
                }
            });

            var bounds1 = new double[2][];
            bounds1[0] = (double[]) Bounds[0].Clone();
            bounds1[1] = (double[]) Bounds[1].Clone();
            bounds1[1][cutDir] = bounds1[0][cutDir] + voxelsPerSide1[cutDir] * VoxelSideLength;
            var vs1 = new VoxelizedSolidDense(voxels1, Discretization, voxelsPerSide1, VoxelSideLength, bounds1);

            var voxelsPerSide2 = VoxelsPerSide.ToArray();
            voxelsPerSide2[cutDir] = VoxelsPerSide[cutDir] - cutBefore;
            var voxels2 = new byte[voxelsPerSide2[0], voxelsPerSide2[1], voxelsPerSide2[2]];

            Parallel.For(0, voxelsPerSide2[0], i =>
            {
                for (var j = 0; j < voxelsPerSide2[1]; j++)
                for (var k = 0; k < voxelsPerSide2[2]; k++)
                {
                    if (cutDir == 0)
                        voxels2[i, j, k] = Voxels[i + cutBefore, j, k];
                    else if (cutDir == 1)
                        voxels2[i, j, k] = Voxels[i, j + cutBefore, k];
                    else
                        voxels2[i, j, k] = Voxels[i, j, k + cutBefore];
                    }
            });

            var bounds2 = new double[2][];
            bounds2[0] = (double[])Bounds[0].Clone();
            bounds2[1] = (double[])Bounds[1].Clone();
            bounds2[0][cutDir] = bounds1[1][cutDir];
            var vs2 = new VoxelizedSolidDense(voxels2, Discretization, voxelsPerSide2, VoxelSideLength, bounds2);

            var cutSign = Math.Sign((int)vd);
            var voxelSolids = cutSign == -1 ? (vs1, vs2) : (vs2, vs1);
            return voxelSolids;
        }

        // Solid on positive side of flat is in position one of return tuple
        // Voxels exactly on the plane are assigned to the positive side
        public (VoxelizedSolidDense, VoxelizedSolidDense) CutSolid(Flat plane)
        {
            if (!GetPlaneBoundsInSolid(Bounds, plane, out var inters))
                throw new ArgumentOutOfRangeException();

            var mins = new []
            {
                new[] {Bounds[1][0], Bounds[1][1], Bounds[1][2]},
                new[] {Bounds[0][0], Bounds[0][1], Bounds[0][2]}
            };

            foreach (var intersection in inters)
            {
                mins[0][0] = Math.Min(mins[0][0], intersection[0]);
                mins[1][0] = Math.Max(mins[1][0], intersection[0]);
                mins[0][1] = Math.Min(mins[0][1], intersection[1]);
                mins[1][1] = Math.Max(mins[1][1], intersection[1]);
                mins[0][2] = Math.Min(mins[0][2], intersection[2]);
                mins[1][2] = Math.Max(mins[1][2], intersection[2]);
            }
            
            var pn = plane.Normal;
            var pp = plane.ClosestPointToOrigin;

            var bounds1 = new[]
            {
                Bounds[0].ToArray(),
                Bounds[1].ToArray()
            };

            var bounds2 = new[]
            {
                Bounds[0].ToArray(),
                Bounds[1].ToArray()
            };

            for (var i = 0; i < 3; i++)
            {
                switch (Math.Sign(pn[i]))
                {
                    case 1:
                    {
                        bounds1[0][i] = mins[0][i];
                        bounds1[1][i] = Bounds[1][i];
                        bounds2[0][i] = Bounds[0][i];
                        bounds2[1][i] = mins[1][i];
                        break;
                    }
                    case -1:
                    {
                        bounds1[0][i] = Bounds[0][i];
                        bounds1[1][i] = mins[1][i];
                        bounds2[0][i] = mins[0][i];
                        bounds2[1][i] = Bounds[1][i];
                        break;
                    }
                    default:
                    {
                        bounds1[0][i] = Bounds[0][i];
                        bounds1[1][i] = Bounds[1][i];
                        bounds2[0][i] = Bounds[0][i];
                        bounds2[1][i] = Bounds[1][i];
                        break;
                    }
                }
            }

            var voxelsPerSide1 = VoxelsPerSide.ToArray();
            var voxelsPerSide2 = VoxelsPerSide.ToArray();

            for (var i = 0; i < 3; i++)
            {
                voxelsPerSide1[0] = (int) Math.Round((bounds1[1][0] - bounds1[0][0]) / VoxelSideLength);
                voxelsPerSide1[1] = (int) Math.Round((bounds1[1][1] - bounds1[0][1]) / VoxelSideLength);
                voxelsPerSide1[2] = (int) Math.Round((bounds1[1][2] - bounds1[0][2]) / VoxelSideLength);

                voxelsPerSide2[0] = (int) Math.Ceiling((bounds2[1][0] - bounds2[0][0]) / VoxelSideLength);
                voxelsPerSide2[1] = (int) Math.Ceiling((bounds2[1][1] - bounds2[0][1]) / VoxelSideLength);
                voxelsPerSide2[2] = (int) Math.Ceiling((bounds2[1][2] - bounds2[0][2]) / VoxelSideLength);
            }

            var xOff1 = Math.Sign(pn[0]) == 1 ? Math.Max(VoxelsPerSide[0] - voxelsPerSide1[0], 0) : 0;
            var yOff1 = Math.Sign(pn[1]) == 1 ? Math.Max(VoxelsPerSide[1] - voxelsPerSide1[1], 0) : 0;
            var zOff1 = Math.Sign(pn[2]) == 1 ? Math.Max(VoxelsPerSide[2] - voxelsPerSide1[2], 0) : 0;

            var xOff2 = Math.Sign(pn[0]) == -1 ? Math.Max(VoxelsPerSide[0] - voxelsPerSide2[0], 0) : 0;
            var yOff2 = Math.Sign(pn[1]) == -1 ? Math.Max(VoxelsPerSide[1] - voxelsPerSide2[1], 0) : 0;
            var zOff2 = Math.Sign(pn[2]) == -1 ? Math.Max(VoxelsPerSide[2] - voxelsPerSide2[2], 0) : 0;

            var voxels1 = new byte[voxelsPerSide1[0], voxelsPerSide1[1], voxelsPerSide1[2]];
            var voxels2 = new byte[voxelsPerSide2[0], voxelsPerSide2[1], voxelsPerSide2[2]];

            var xOff = Offset[0];
            var yOff = Offset[1];
            var zOff = Offset[2];

            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                {
                    var x = xOff + (i + .5) * VoxelSideLength;
                    var y = yOff + (j + .5) * VoxelSideLength;
                    var z = zOff + (k + .5) * VoxelSideLength;
                    var d = MiscFunctions.DistancePointToPlane(new[] {x, y, z}, pn, pp);
                    if (d < 0)
                        voxels2[i - xOff2, j - yOff2, k - zOff2] = Voxels[i, j, k];
                    else
                        voxels1[i - xOff1, j - yOff1, k - zOff1] = Voxels[i, j, k];
                }
            });

            var vs1 = new VoxelizedSolidDense(voxels1, Discretization, voxelsPerSide1, VoxelSideLength, bounds1);
            var vs2 = new VoxelizedSolidDense(voxels2, Discretization, voxelsPerSide2, VoxelSideLength, bounds2);
            return (vs1, vs2);
        }

        private static bool GetPlaneBoundsInSolid(double[][] bds, Flat plane, out List<double[]> intersections)
        {
            var pn = plane.Normal;
            var pd = plane.DistanceToOrigin;

            var vertices = new List<double[]>
            {
                new[] {bds[0][0], bds[0][1], bds[0][2]},
                new[] {bds[1][0], bds[0][1], bds[0][2]},
                new[] {bds[0][0], bds[1][1], bds[0][2]},
                new[] {bds[0][0], bds[0][1], bds[1][2]},
                new[] {bds[1][0], bds[1][1], bds[0][2]},
                new[] {bds[1][0], bds[0][1], bds[1][2]},
                new[] {bds[0][0], bds[1][1], bds[1][2]},
                new[] {bds[1][0], bds[1][1], bds[1][2]}
            };

            var dirs = new List<double[]>
            {
                new[] {1.0, 0, 0},
                new[] {0, 1.0, 0},
                new[] {0, 0, 1.0},
            };

            var rays = new List<int[]>
            {
                new[] {0, 0},
                new[] {0, 1},
                new[] {0, 2},
                new[] {1, 1},
                new[] {1, 2},
                new[] {2, 0},
                new[] {2, 2},
                new[] {3, 0},
                new[] {3, 1},
                new[] {4, 2},
                new[] {5, 1},
                new[] {6, 0},
            };

            intersections = new List<double[]>();

            foreach (var ray in rays)
            {
                var inter = MiscFunctions.PointOnPlaneFromRay(pn, pd, vertices[ray[0]], dirs[ray[1]], out var dist);
                if (!(inter is null) && dist >= 0 && inter[0] <= bds[1][0] && inter[1] <= bds[1][1] &&
                    inter[2] <= bds[1][2])
                    intersections.Add(inter);
            }

            return intersections.Count > 2;
        }
        #endregion

        #region Boolean functions
        public VoxelizedSolidDense CreateBoundingSolid()
        {
            return new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds, 1);
        }

        // NOT A
        public VoxelizedSolidDense InvertToNewSolid()
        {
            var vs = new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds);
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] == 0)
                        vs.Voxels[i, j, k] = 1;
            });
            vs.UpdateProperties();
            return vs;
        }

        // A OR B
        public VoxelizedSolidDense UnionToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = (VoxelizedSolidDense)Copy();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                for (var k = 0; k < zLim; k++)
                {
                    var voxel = Voxels[i, j, k];
                    if (voxel != 0) continue;
                    foreach (var vox in solids)
                    {
                        voxel = (byte)(voxel | vox.Voxels[i, j, k]);
                        if (voxel != 0) break;
                    }
                    vs.Voxels[i, j, k] = voxel;
                }
            });
            vs.UpdateProperties();
            return vs;
        }

        // A AND B
        public VoxelizedSolidDense IntersectToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds);
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                for (var k = 0; k < zLim; k++)
                {
                    var voxel = Voxels[i, j, k];
                    if (voxel == 0) continue;
                    foreach (var vox in solids)
                    voxel = (byte) (voxel & vox.Voxels[i, j, k]);
                    vs.Voxels[i, j, k] = voxel;
                }
            });
            vs.UpdateProperties();
            return vs;
        }

        // A AND (NOT B)
        public VoxelizedSolidDense SubtractToNewSolid(params VoxelizedSolidDense[] subtrahends)
        {
            var vs = (VoxelizedSolidDense)Copy();
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                for (var k = 0; k < zLim; k++)
                    if (subtrahends.Any(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 0;
            });
            vs.UpdateProperties();
            return vs;
        }
        #endregion

        #region Draft in VoxelDirection
        public VoxelizedSolidDense DraftToNewSolid(VoxelDirections vd)
        {
            var vs = (VoxelizedSolidDense)Copy();

            var draftDir = (int) vd;
            var draftIndex = Math.Abs(draftDir) - 1;
            var planeIndices = new int[2];
            var ii = 0;
            for (var i = 0; i < 3; i++)
                if (i != draftIndex)
                {
                    planeIndices[ii] = i;
                    ii++;
                }

            var mLim = VoxelsPerSide[planeIndices[0]];
            var nLim = VoxelsPerSide[planeIndices[1]];
            var pLim = VoxelsPerSide[draftIndex];

            Parallel.For(0, mLim, m =>
            {
                for (var n = 0; n < nLim; n++)
                {
                    var fillAll = false;
                    for (var p = 0; p < pLim; p++)
                    {
                        var q = draftDir > 0 ? p : pLim - 1 - p;
                        int i;
                        int j;
                        int k;

                        switch (draftIndex)
                        {
                            case 0:
                                i = q;
                                j = m;
                                k = n;
                                break;
                            case 1:
                                i = m;
                                j = q;
                                k = n;
                                break;
                            case 2:
                                i = m;
                                j = n;
                                k = q;
                                break;
                            default:
                                continue;
                        }

                        if (fillAll)
                        {
                            vs.Voxels[i, j, k] = 1;
                            continue;
                        }
                        if (vs.Voxels[i, j, k] != 0)
                            fillAll = true;
                    }

                }
            });

            vs.UpdateProperties();
            return vs;
        }
        #endregion

        #region Voxel erosion
        public VoxelizedSolidDense ErodeToNewSolid(VoxelizedSolidDense designedSolid, double[] dir,
            double tLimit = 0, double toolDia = 0, params string[] toolOptions)
        {
            var copy = (VoxelizedSolidDense) Copy();
            copy.ErodeVoxelSolid(designedSolid, dir.normalize(3), tLimit, toolDia, toolOptions);
            copy.UpdateProperties();
            return copy;
        }

        public VoxelizedSolidDense ErodeToNewSolid(VoxelizedSolidDense designedSolid, VoxelDirections dir,
            double tLimit = 0, double toolDia = 0, params string[] toolOptions)
        {
            var copy = (VoxelizedSolidDense)Copy();

            var tDir = new[] {.0, .0, .0};
            tDir[Math.Abs((int) dir) - 1] = Math.Sign((int) dir);

            copy.ErodeVoxelSolid(designedSolid, tDir.normalize(3), tLimit, toolDia, toolOptions);
            copy.UpdateProperties();
            return copy;
        }

        private void ErodeVoxelSolid(VoxelizedSolidDense designedSolid, double[] dir,
            double tLimit, double toolDia, params string[] toolOptions)
        {
            var dirX = dir[0];
            var dirY = dir[1];
            var dirZ = dir[2];
            var signX = (byte) (Math.Sign(dirX) + 1);
            var signY = (byte) (Math.Sign(dirY) + 1);
            var signZ = (byte) (Math.Sign(dirZ) + 1);
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];

            tLimit = tLimit <= 0 ? VoxelsPerSide.norm2() : tLimit / VoxelSideLength;
            toolDia = toolDia <= 0 ? 0 : toolDia;
            var mLimit = tLimit + VoxelsPerSide.norm2();
            var mask = CreateProjectionMask(dir, mLimit);
            var starts = GetAllVoxelsOnBoundingSurfaces(dirX, dirY, dirZ, toolDia);
            var sliceMask = ThickenMask(mask[0], dir, toolDia, toolOptions);

            Parallel.ForEach(starts, vox =>
                ErodeMask(designedSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox));
            //foreach (var vox in starts)
            //    ErodeMask(designedSolid, mask, tLimit, stopAtPartial, dir, sliceMask, vox);
        }

        private static IEnumerable<VoxelDirections> GetVoxelDirections(double dirX, double dirY, double dirZ)
        {
            var dirs = new List<VoxelDirections>();
            var signedDir = new[] { Math.Sign(dirX), Math.Sign(dirY), Math.Sign(dirZ) };
            for (var i = 0; i < 3; i++)
            {
                if (signedDir[i] == 0) continue;
                dirs.Add((VoxelDirections)((i + 1) * -1 * signedDir[i]));
            }
            return dirs.ToArray();
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurfaces(double dirX, double dirY, double dirZ,
            double toolDia)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());
            var directions = GetVoxelDirections(dirX, dirY, dirZ);
            foreach (var direction in directions)
            {
                var voxel = GetAllVoxelsOnBoundingSurface(direction, toolDia);
                foreach (var vox in voxel)
                    voxels.Add(vox);
            }
            return voxels;
        }

        private IEnumerable<int[]> GetAllVoxelsOnBoundingSurface(VoxelDirections dir, double toolDia)
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
            var voxels = new int[arraySize][];
            var m = 0;

            for (var i = limit[0][0]; i < limit[0][1]; i++)
            for (var j = limit[1][0]; j < limit[1][1]; j++)
            for (var k = limit[2][0]; k < limit[2][1]; k++)
            {
                voxels[m] = new[] { i, j, k };
                m++;
            }

            return voxels;
        }

        private void ErodeMask(VoxelizedSolidDense designedSolid, int[][] mask, byte signX, byte signY,
            byte signZ, int xLim, int yLim, int zLim, int[][] sliceMask = null, int[] start = null)
        {
            start = start ?? mask[0].ToArray();
            sliceMask = sliceMask ?? new[] {mask[0].ToArray()};
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

                    //Return if you've hit the as-designed part
                    if (designedSolid.Voxels[coordX, coordY, coordZ] != 0)
                        return;
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
                    Voxels[coordX, coordY, coordZ] = 0;
                }
            }
        }

        private class SameCoordinates : EqualityComparer<int[]>
        {
            public override bool Equals(int[] a1, int[] a2)
            {
                if (a1 == null && a2 == null)
                    return true;
                if (a1 == null || a2 == null)
                    return false;
                return (a1[0] == a2[0] &&
                        a1[1] == a2[1] &&
                        a1[2] == a2[2]);
            }
            public override int GetHashCode(int[] ax)
            {
                if (ax is null) return 0;
                var hCode = ax[0] + (ax[1] << 10) + (ax[2] << 20);
                return hCode.GetHashCode();
            }
        }

        #region Functions to check if within bounds 
        //private static bool SucceedsBounds(int i, int j, int k, double dirX, double dirY, double dirZ, int xLim, int yLim,
        //    int zLim)
        //{
        //    if (dirX < 0 && i < 0) return true;
        //    if (dirX > 0 && i >= xLim) return true;
        //    if (dirX == 0 && (i < 0 || i >= xLim)) return true;

        //    if (dirY < 0 && j < 0) return true;
        //    if (dirY > 0 && j >= yLim) return true;
        //    if (dirY == 0 && (j < 0 || j >= yLim)) return true;

        //    if (dirZ < 0 && k < 0) return true;
        //    if (dirZ > 0 && k >= zLim) return true;
        //    return dirZ == 0 && (k < 0 || k >= zLim);
        //}

        //private static bool PrecedesBounds(int i, int j, int k, double dirX, double dirY, double dirZ, int xLim, int yLim,
        //    int zLim)
        //{
        //    if (dirX < 0 && i >= xLim) return true;
        //    if (dirX > 0 && i < 0) return true;
        //    if (dirX == 0 && (i >= xLim || i < 0)) return true;

        //    if (dirY < 0 && j >= yLim) return true;
        //    if (dirY > 0 && j < 0) return true;
        //    if (dirY == 0 && (j >= yLim || j < 0)) return true;

        //    if (dirZ < 0 && k >= zLim) return true;
        //    if (dirZ > 0 && k < 0) return true;
        //    return dirZ == 0 && (k >= zLim || k < 0);
        //}

        //private static bool OutsideBounds(int i, int j, int k, int xLim, int yLim, int zLim)
        //{
        //    return i < 0 || i >= xLim ||
        //           j < 0 || j >= yLim ||
        //           k < 0 || k >= zLim;
        //}
        #endregion
        
        private static int[][] GetVoxelsWithinCircle(double[] center, double[] dir, double radius, bool edge = false)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());

            var radii = new List<double>();
            if (!edge)
                for (var i = .0; i < radius; i += 0.5)
                    radii.Add(i);
            radii.Add(radius);
            var a = Math.Abs(dir[0]) < 1e-5
                ? new[] {.0, -dir[2], dir[1]}.normalize(3)
                : new[] {dir[1], -dir[0], 0}.normalize(3);
            var b = a.crossProduct(dir);

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

        private static int[][] GetVoxelsOnCone(int[] center, double[] dir, double radius, double angle)
        {
            var voxels = new HashSet<int[]>(new[] {center.ToArray()}, new SameCoordinates());

            var a = angle * (Math.PI / 180) / 2;
            var l = radius / Math.Sin(a);
            var numSteps = (int)Math.Ceiling(l / 0.5);
            var lStep = l / numSteps;
            var tStep = lStep * Math.Cos(a);
            var rStep = lStep * Math.Sin(a);

            var centerDouble = new double[] { center[0], center[1], center[2] };
            var c = centerDouble.ToArray();
            var cStep = dir.multiply(tStep, 3);

            for (var i = 1; i <= numSteps; i++)
            {
                var r = rStep * i;
                c = c.subtract(cStep, 3);
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        private static int[][] GetVoxelsOnHemisphere(int[] center, double[] dir, double radius)
        {
            var voxels = new HashSet<int[]>(new[] {center.ToArray()}, new SameCoordinates());

            var centerDouble = new double[] { center[0], center[1], center[2] };

            var numSteps = (int)Math.Ceiling(Math.PI * radius / 2 / 0.5);
            var aStep = Math.PI / 2 / numSteps;

            for (var i = 1; i <= numSteps; i++)
            {
                var a = aStep * i;
                var r = radius * Math.Sin(a);
                var tStep = radius * (1 - Math.Cos(a));
                var c = centerDouble.subtract(dir.multiply(tStep, 3), 3);
                var voxelsOnCircle = GetVoxelsWithinCircle(c, dir, r, true);
                foreach (var voxel in voxelsOnCircle)
                    voxels.Add(voxel);
            }

            return voxels.ToArray();
        }

        private int[][] ThickenMask(int[] vox, double[] dir, double toolDia, params string[] toolOptions)
        {
            if (toolDia <= 0) return new[] {vox};

            var radius = 0.5 * toolDia / VoxelSideLength;
            toolOptions = toolOptions.Length == 0 ? new[] { "flat" } : toolOptions;

            switch (toolOptions[0])
            {
                case "ball":
                    return GetVoxelsOnHemisphere(vox, dir, radius);
                case "cone":
                    double angle;
                    if (toolOptions.Length < 2) angle = 118;
                    else if (!double.TryParse(toolOptions[1], out angle))
                        angle = 118;
                    return GetVoxelsOnCone(vox, dir, radius, angle);
                default:
                    var voxDouble = new double[] { vox[0], vox[1], vox[2] };
                    return GetVoxelsWithinCircle(voxDouble, dir, radius);
            }
        }

        private int[][] CreateProjectionMask(double[] dir, double tLimit)
        {
            var initCoord = new[] { 0, 0, 0 };
            for (var i = 0; i < 3; i++)
                if (dir[i] < 0) initCoord[i] = VoxelsPerSide[i] - 1;
            var voxels = new List<int[]>(new[] { initCoord });
            var c = initCoord.add(new[] { 0.5, 0.5, 0.5 }, 3);
            var ts = FindIntersectionDistances(c, dir, tLimit);
            foreach (var t in ts)
            {
                var cInt = c.add(dir.multiply(t, 3), 3);
                for (var i = 0; i < 3; i++) cInt[i] = Math.Round(cInt[i], 5);
                voxels.Add(GetNextVoxelCoord(cInt, dir));
            }
            return voxels.ToArray();
        }

        //Exclusive by default (i.e. if line passes through vertex/edge it ony includes two voxels that are actually passed through)
        private static int[] GetNextVoxelCoord(double[] cInt, double[] direction)
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
        private double[] FindIntersectionDistances(double[] firstVoxel, double[] direction, double tLimit)
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

            //foreach (var dir in searchDirs)
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
        #endregion
    }
}
