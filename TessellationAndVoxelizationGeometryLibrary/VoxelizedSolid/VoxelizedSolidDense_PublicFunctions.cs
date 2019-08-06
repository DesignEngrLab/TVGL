using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using StarMathLib;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid
    {
        #region Getting Neighbors
        //Neighbors functions use VoxelsPerSide
        public int[][] GetNeighborsDense(int xCoord, int yCoord, int zCoord)
        {
            return GetNeighborsDense(xCoord, yCoord, zCoord, VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]);
        }

        public int NumNeighborsDense(int xCoord, int yCoord, int zCoord)
        {
            return NumNeighborsDense(xCoord, yCoord, zCoord, VoxelsPerSide[0], VoxelsPerSide[1], VoxelsPerSide[2]);
        }

        public int[][] GetNeighborsDense(int xCoord, int yCoord, int zCoord, int xLim, int yLim, int zLim)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            var xByteCoord = xCoord >> 3;
            var bitPosition = xCoord & 7;

            if (xCoord != 0)
            {
                var neighBit = bitPosition == 0 ? 7 : bitPosition - 1;
                var neighByte = bitPosition == 0 ? xByteCoord - 1 : xByteCoord;
                if (GetVoxelDense(neighByte, yCoord, zCoord, neighBit))
                    neighbors[0] = new[] { xCoord - 1, yCoord, zCoord };
            }
            if (xCoord + 1 != xLim)
            {
                var neighBit = bitPosition == 7 ? 0 : bitPosition + 1;
                var neighByte = bitPosition == 7 ? xByteCoord + 1 : xByteCoord;
                if (GetVoxelDense(neighByte, yCoord, zCoord, neighBit))
                    neighbors[1] = new[] { xCoord + 1, yCoord, zCoord };
            }

            if (yCoord != 0 && GetVoxelDense(xByteCoord, yCoord - 1, zCoord, bitPosition)) neighbors[2] = new[] { xCoord, yCoord - 1, zCoord };
            if (yCoord + 1 != yLim && GetVoxelDense(xByteCoord, yCoord + 1, zCoord, bitPosition)) neighbors[3] = new[] { xCoord, yCoord + 1, zCoord };
            if (zCoord != 0 && GetVoxelDense(xByteCoord, yCoord, zCoord - 1, bitPosition)) neighbors[4] = new[] { xCoord, yCoord, zCoord - 1 };
            if (zCoord + 1 != zLim && GetVoxelDense(xByteCoord, yCoord, zCoord + 1, bitPosition)) neighbors[5] = new[] { xCoord, yCoord, zCoord + 1 };

            return neighbors;
        }
        public int NumNeighborsDense(int xCoord, int yCoord, int zCoord, int xLim, int yLim, int zLim)
        {
            var neighbors = 0;
            var xByteCoord = xCoord >> 3;
            var bitPosition = xCoord & 7;

            if (xCoord != 0)
            {
                var neighBit = bitPosition == 0 ? 7 : bitPosition - 1;
                var neighByte = bitPosition == 0 ? xByteCoord - 1 : xByteCoord;
                if (GetVoxelDense(neighByte, yCoord, zCoord, neighBit))
                    neighbors++;
            }
            if (xCoord + 1 != xLim)
            {
                var neighBit = bitPosition == 7 ? 0 : bitPosition + 1;
                var neighByte = bitPosition == 7 ? xByteCoord + 1 : xByteCoord;
                if (GetVoxelDense(neighByte, yCoord, zCoord, neighBit))
                    neighbors++;
            }

            if (yCoord != 0 && GetVoxelDense(xByteCoord, yCoord - 1, zCoord, bitPosition)) neighbors++;
            if (yCoord + 1 != yLim && GetVoxelDense(xByteCoord, yCoord + 1, zCoord, bitPosition)) neighbors++;
            if (zCoord != 0 && GetVoxelDense(xByteCoord, yCoord, zCoord - 1, bitPosition)) neighbors++;
            if (zCoord + 1 != zLim && GetVoxelDense(xByteCoord, yCoord, zCoord + 1, bitPosition)) neighbors++;

            return neighbors;
        }
        #endregion

        #region Set/update properties
        public void UpdatePropertiesDense()
        {
            var count = new ConcurrentDictionary<int, int>();
            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                var counter = 0;
                for (var j = 0; j < yLim; j++)
                    for (var k = 0; k < zLim; k++)
                    {
                        var val = Dense[i, j, k];
                        if (val != 0)
                            counter += CountSetBitsDense(val);
                    }
                count.TryAdd(i, counter);
            });
            Count = 0;
            foreach (var v in count.Values)
                Count += v;
            Volume = Count * Math.Pow(VoxelSideLength, 3);

            var neighbors = new ConcurrentDictionary<int, int>();
            xLim = VoxelsPerSide[0];
            //Parallel.For(0, xLim, i =>
            for (int i = 0; i < xLim; i++)
            {
                var iB = i >> 3;
                var iS = i & 7;
                var neighborCount = 0;
                for (var j = 0; j < yLim; j++)
                {
                    var jB = j >> 3;
                    var jS = j & 7;
                    for (var k = 0; k < zLim; k++)
                    {
                        if (!GetVoxelDense(iB, j, k, iS)) continue;
                        var num = NumNeighborsDense(i, j, k, xLim, yLim, zLim);
                        neighborCount += num;
                    }
                }
                neighbors.TryAdd(i, neighborCount);
            } //);
            long totalNeighbors = 0;
            foreach (var v in neighbors.Values)
                totalNeighbors += v;
            SurfaceArea = 6 * (Count - totalNeighbors / 6) * Math.Pow(VoxelSideLength, 2);
        }
        #endregion

        #region Boolean functions
        //Boolean functions use BytesPerSide
        public VoxelizedSolid CreateBoundingSolid()
        {
            return new VoxelizedSolid(VoxelsPerSide, VoxelSideLength, Bounds);
        }

        // NOT A
        public VoxelizedSolid InvertToNewSolid()
        {
            var vs = new VoxelizedSolid(VoxelsPerSide, VoxelSideLength, Bounds, true);
            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                    for (var k = 0; k < zLim; k++)
                        if (Dense[i, j, k] != byte.MaxValue)
                            vs.Dense[i, j, k] = (byte)~Dense[i, j, k];
            });
            vs.UpdatePropertiesDense();
            return vs;
        }

        // A OR B
        public VoxelizedSolid UnionToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = (VoxelizedSolid)Copy();
            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                    for (var k = 0; k < zLim; k++)
                    {
                        var voxel = Dense[i, j, k];
                        if (voxel == byte.MaxValue) continue;
                        foreach (var vox in solids)
                        {
                            voxel = (byte)(voxel | vox.Dense[i, j, k]);
                            if (voxel == byte.MaxValue) break;
                        }
                        vs.Dense[i, j, k] = voxel;
                    }
            });
            vs.UpdatePropertiesDense();
            return vs;
        }

        // A AND B
        public VoxelizedSolid IntersectToNewSolid(params VoxelizedSolid[] solids)
        {
            var vs = new VoxelizedSolid(VoxelsPerSide, VoxelSideLength, Bounds, true);
            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                    for (var k = 0; k < zLim; k++)
                    {
                        var voxel = Dense[i, j, k];
                        if (voxel == 0) continue;
                        foreach (var vox in solids)
                        {
                            voxel = (byte)(voxel & vox.Dense[i, j, k]);
                            if (voxel == 0) break;
                        }

                        if (voxel == 0) continue;
                        vs.Dense[i, j, k] = voxel;
                    }
            });
            vs.UpdatePropertiesDense();
            return vs;
        }

        // A AND (NOT B)
        public VoxelizedSolid SubtractToNewSolid(params VoxelizedSolid[] subtrahends)
        {
            var vs = (VoxelizedSolid)Copy();
            var xLim = BytesPerSide[0];
            var yLim = BytesPerSide[1];
            var zLim = BytesPerSide[2];
            Parallel.For(0, xLim, i =>
            {
                for (var j = 0; j < yLim; j++)
                    for (var k = 0; k < zLim; k++)
                    {
                        var voxel = Dense[i, j, k];
                        if (voxel == 0b0) continue;
                        foreach (var vox in subtrahends)
                        {
                            voxel = (byte)(voxel & (byte)~vox.Dense[i, j, k]);
                            if (voxel == 0) break;
                        }

                        vs.Dense[i, j, k] = voxel;
                    }
            });
            vs.UpdatePropertiesDense();
            return vs;
        }
        #endregion

        #region Slice Voxel Solids
        // If vd is negative, the negative side solid is in position one of return tuple
        // If vd is positive, the positive side solid is in position one of return tuple
        // cutBefore is the zero-based index of voxel-plane to cut before
        // i.e. cutBefore = 8, would yield one solid with voxels 0 to 7, and one with 8 to end
        // 0 < cutBefore < VoxelsPerSide[cut direction]
        public (VoxelizedSolid, VoxelizedSolid) SliceOnFlat(CartesianDirections vd, int cutBefore)
        {
            var cutDir = Math.Abs((int)vd) - 1;
            if (cutBefore >= VoxelsPerSide[cutDir] || cutBefore < 1)
                throw new ArgumentOutOfRangeException();

            var cutSign = Math.Sign((int)vd);
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();
            if (cutSign == 1)
                switch (cutDir)
                {
                    case 0:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (i < cutBefore)
                                        vs1.SetVoxelDense(false, i, j, k);
                                    else
                                        vs2.SetVoxelDense(false, i, j, k);
                        });
                        break;
                    case 1:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (j < cutBefore)
                                        vs1.SetVoxelDense(false, i, j, k);
                                    else
                                        vs2.SetVoxelDense(false, i, j, k);
                        });
                        break;
                    case 2:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (k < cutBefore)
                                        vs1.SetVoxelDense(false, i, j, k);
                                    else
                                        vs2.SetVoxelDense(false, i, j, k);
                        });
                        break;
                }
            else
                switch (cutDir)
                {
                    case 0:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (i < cutBefore)
                                        vs2.SetVoxelDense(false, i, j, k);
                                    else
                                        vs1.SetVoxelDense(false, i, j, k);
                        });
                        break;
                    case 1:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (j < cutBefore)
                                        vs2.SetVoxelDense(false, i, j, k);
                                    else
                                        vs1.SetVoxelDense(false, i, j, k);
                        });
                        break;
                    case 2:
                        Parallel.For(0, VoxelsPerSide[0], i =>
                        {
                            for (var j = 0; j < VoxelsPerSide[1]; j++)
                                for (var k = 0; k < VoxelsPerSide[2]; k++)
                                    if (k < cutBefore)
                                        vs2.SetVoxelDense(false, i, j, k);
                                    else
                                        vs1.SetVoxelDense(false, i, j, k);
                        });
                        break;
                }
            vs1.UpdatePropertiesDense();
            vs2.UpdatePropertiesDense();
            return (vs1, vs2);
        }

        // Solid on positive side of flat is in position one of return tuple
        // Voxels exactly on the plane are assigned to the positive side
        public (VoxelizedSolid, VoxelizedSolid) SliceOnFlat(Flat plane)
        {
            var vs1 = (VoxelizedSolid)Copy();
            var vs2 = (VoxelizedSolid)Copy();

            var pn = plane.Normal;
            var pp = plane.ClosestPointToOrigin;

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
                        var d = MiscFunctions.DistancePointToPlane(new[] { x, y, z }, pn, pp);
                        if (d < 0)
                            vs1.SetVoxelDense(false, i, j, k);
                        else
                            vs2.SetVoxelDense(false, i, j, k);
                    }
            });

            vs1.UpdatePropertiesDense();
            vs2.UpdatePropertiesDense();
            return (vs1, vs2);
        }

        //// Solid on positive side of flat is in position one of return tuple
        //// Voxels exactly on the plane are assigned to the positive side
        //public (VoxelizedSolidDense, VoxelizedSolidDense) SliceOnFlatAndResizeVoxelArrays(Flat plane)
        //{
        //    if (!GetPlaneBoundsInSolid(Bounds, plane, out var inters))
        //        throw new ArgumentOutOfRangeException();

        //    var mins = new []
        //    {
        //        new[] {Bounds[1][0], Bounds[1][1], Bounds[1][2]},
        //        new[] {Bounds[0][0], Bounds[0][1], Bounds[0][2]}
        //    };

        //    foreach (var intersection in inters)
        //    {
        //        mins[0][0] = Math.Min(mins[0][0], intersection[0]);
        //        mins[1][0] = Math.Max(mins[1][0], intersection[0]);
        //        mins[0][1] = Math.Min(mins[0][1], intersection[1]);
        //        mins[1][1] = Math.Max(mins[1][1], intersection[1]);
        //        mins[0][2] = Math.Min(mins[0][2], intersection[2]);
        //        mins[1][2] = Math.Max(mins[1][2], intersection[2]);
        //    }

        //    var pn = plane.Normal;
        //    var pp = plane.ClosestPointToOrigin;

        //    var bounds1 = new[]
        //    {
        //        Bounds[0].ToArray(),
        //        Bounds[1].ToArray()
        //    };

        //    var bounds2 = new[]
        //    {
        //        Bounds[0].ToArray(),
        //        Bounds[1].ToArray()
        //    };

        //    for (var i = 0; i < 3; i++)
        //    {
        //        switch (Math.Sign(pn[i]))
        //        {
        //            case 1:
        //            {
        //                bounds1[0][i] = mins[0][i];
        //                bounds1[1][i] = Bounds[1][i];
        //                bounds2[0][i] = Bounds[0][i];
        //                bounds2[1][i] = mins[1][i];
        //                break;
        //            }
        //            case -1:
        //            {
        //                bounds1[0][i] = Bounds[0][i];
        //                bounds1[1][i] = mins[1][i];
        //                bounds2[0][i] = mins[0][i];
        //                bounds2[1][i] = Bounds[1][i];
        //                break;
        //            }
        //            default:
        //            {
        //                bounds1[0][i] = Bounds[0][i];
        //                bounds1[1][i] = Bounds[1][i];
        //                bounds2[0][i] = Bounds[0][i];
        //                bounds2[1][i] = Bounds[1][i];
        //                break;
        //            }
        //        }
        //    }

        //    var voxelsPerSide1 = VoxelsPerSide.ToArray();
        //    var voxelsPerSide2 = VoxelsPerSide.ToArray();

        //    for (var i = 0; i < 3; i++)
        //    {
        //        voxelsPerSide1[0] = (int) Math.Round((bounds1[1][0] - bounds1[0][0]) / VoxelSideLength);
        //        voxelsPerSide1[1] = (int) Math.Round((bounds1[1][1] - bounds1[0][1]) / VoxelSideLength);
        //        voxelsPerSide1[2] = (int) Math.Round((bounds1[1][2] - bounds1[0][2]) / VoxelSideLength);

        //        voxelsPerSide2[0] = (int) Math.Ceiling((bounds2[1][0] - bounds2[0][0]) / VoxelSideLength);
        //        voxelsPerSide2[1] = (int) Math.Ceiling((bounds2[1][1] - bounds2[0][1]) / VoxelSideLength);
        //        voxelsPerSide2[2] = (int) Math.Ceiling((bounds2[1][2] - bounds2[0][2]) / VoxelSideLength);
        //    }

        //    var xOff1 = Math.Sign(pn[0]) == 1 ? Math.Max(VoxelsPerSide[0] - voxelsPerSide1[0], 0) : 0;
        //    var yOff1 = Math.Sign(pn[1]) == 1 ? Math.Max(VoxelsPerSide[1] - voxelsPerSide1[1], 0) : 0;
        //    var zOff1 = Math.Sign(pn[2]) == 1 ? Math.Max(VoxelsPerSide[2] - voxelsPerSide1[2], 0) : 0;

        //    var xOff2 = Math.Sign(pn[0]) == -1 ? Math.Max(VoxelsPerSide[0] - voxelsPerSide2[0], 0) : 0;
        //    var yOff2 = Math.Sign(pn[1]) == -1 ? Math.Max(VoxelsPerSide[1] - voxelsPerSide2[1], 0) : 0;
        //    var zOff2 = Math.Sign(pn[2]) == -1 ? Math.Max(VoxelsPerSide[2] - voxelsPerSide2[2], 0) : 0;

        //    var voxels1 = new byte[voxelsPerSide1[0], voxelsPerSide1[1], voxelsPerSide1[2]];
        //    var voxels2 = new byte[voxelsPerSide2[0], voxelsPerSide2[1], voxelsPerSide2[2]];

        //    var xOff = Offset[0];
        //    var yOff = Offset[1];
        //    var zOff = Offset[2];

        //    Parallel.For(0, VoxelsPerSide[0], i =>
        //    {
        //        for (var j = 0; j < VoxelsPerSide[1]; j++)
        //        for (var k = 0; k < VoxelsPerSide[2]; k++)
        //        {
        //            var x = xOff + (i + .5) * VoxelSideLength;
        //            var y = yOff + (j + .5) * VoxelSideLength;
        //            var z = zOff + (k + .5) * VoxelSideLength;
        //            var d = MiscFunctions.DistancePointToPlane(new[] {x, y, z}, pn, pp);
        //            if (d < 0)
        //                voxels2[i - xOff2, j - yOff2, k - zOff2] = Voxels[i, j, k];
        //            else
        //                voxels1[i - xOff1, j - yOff1, k - zOff1] = Voxels[i, j, k];
        //        }
        //    });

        //    var vs1 = new VoxelizedSolidDense(voxels1, Discretization, voxelsPerSide1, VoxelSideLength, bounds1);
        //    var vs2 = new VoxelizedSolidDense(voxels2, Discretization, voxelsPerSide2, VoxelSideLength, bounds2);
        //    return (vs1, vs2);
        //}

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

        #region Draft in VoxelDirection
        public VoxelizedSolid DraftToNewSolid(CartesianDirections vd)
        {

            var draftDir = (int)vd;
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

            if (draftIndex == 0)
                return DraftOnXDimension(draftDir, mLim, nLim, pLim);
            var vs = (VoxelizedSolid)Copy();

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
                        int shift;

                        switch (draftIndex)
                        {
                            case 0:
                                i = q;
                                j = m;
                                k = n >> 3;
                                shift = n & 7;
                                break;
                            case 1:
                                j = q;
                                i = m >> 3;
                                k = n;
                                shift = m & 7;
                                break;
                            case 2:
                                k = q;
                                i = m >> 3;
                                j = n;
                                shift = m & 7;
                                break;
                            default:
                                continue;
                        }

                        if (fillAll)
                        {
                            if (!GetVoxelDense(i, j, k, shift))   //if the voxel is off, turn it on
                                TurnVoxelOnDense(i, j, k, shift);
                            continue;
                        }
                        if (!fillAll && GetVoxelDense(i, j, k, shift)) fillAll = true;
                    }

                }
            });

            vs.UpdatePropertiesDense();
            return vs;
        }

        public VoxelizedSolid DraftOnXDimension(int draftDir, int mLim, int nLim, int pLim)
        {
            var vs = (VoxelizedSolid)Copy();

            Parallel.For(0, mLim, m =>
            {
                for (var n = 0; n < nLim; n++)
                {
                    var fillAll = false;
                    var fillByte = false;

                    for (var p = 0; p < pLim; p++)
                    {
                        var q = draftDir > 0 ? p : pLim - 1 - p;

                        var qB = q >> 3;
                        var qS = q & 7;
                        if (!fillByte && fillAll)
                        {
                            var fillByteComparator = draftDir > 0 ? 0 : 7;
                            if (qS == fillByteComparator)
                                fillByte = true;
                        }
                        if (fillByte)
                            vs.Dense[qB, m, n] = byte.MaxValue;
                        else if (fillAll)
                        {
                            if (!GetVoxelDense(qB, m, n, qS))
                                TurnVoxelOnDense(qB, m, n, qS);
                        }
                        else if (!fillAll && GetVoxelDense(qB, m, n, qS))
                            fillAll = true;
                    }
                }
            });

            vs.UpdatePropertiesDense();
            return vs;
        }

        #endregion

        #region Voxel erosion
        public VoxelizedSolid ErodeToNewSolid(VoxelizedSolid designedSolid, double[] dir,
            double tLimit = 0, double toolDia = 0, params string[] toolOptions)
        {
            var copy = (VoxelizedSolid)Copy();
            copy.ErodeVoxelSolid(designedSolid, dir.normalize(3), tLimit, toolDia, toolOptions);
            copy.UpdatePropertiesDense();
            return copy;
        }

        public VoxelizedSolid ErodeToNewSolid(VoxelizedSolid designedSolid, CartesianDirections dir,
            double tLimit = 0, double toolDia = 0, params string[] toolOptions)
        {
            var copy = (VoxelizedSolid)Copy();

            var tDir = new[] { .0, .0, .0 };
            tDir[Math.Abs((int)dir) - 1] = Math.Sign((int)dir);

            copy.ErodeVoxelSolid(designedSolid, tDir.normalize(3), tLimit, toolDia, toolOptions);
            copy.UpdatePropertiesDense();
            return copy;
        }

        private void ErodeVoxelSolid(VoxelizedSolid designedSolid, double[] dir,
            double tLimit, double toolDia, params string[] toolOptions)
        {
            var dirX = dir[0];
            var dirY = dir[1];
            var dirZ = dir[2];
            var signX = (byte)(Math.Sign(dirX) + 1);
            var signY = (byte)(Math.Sign(dirY) + 1);
            var signZ = (byte)(Math.Sign(dirZ) + 1);
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];

            tLimit = tLimit <= 0 ? VoxelsPerSide.norm2() : tLimit / VoxelSideLength;
            var mLimit = tLimit + VoxelsPerSide.norm2();
            var mask = CreateProjectionMask(dir, mLimit);
            var starts = GetAllVoxelsOnBoundingSurfaces(dirX, dirY, dirZ, toolDia);
            var sliceMask = ThickenMask(mask[0], dir, toolDia, toolOptions);

            Parallel.ForEach(starts, vox =>
                ErodeMask(designedSolid, mask, signX, signY, signZ, xLim, yLim, zLim, sliceMask, vox));
            //foreach (var vox in starts)
            //    ErodeMask(designedSolid, mask, tLimit, stopAtPartial, dir, sliceMask, vox);
        }

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

        private void ErodeMask(VoxelizedSolid designedSolid, int[][] mask, byte signX, byte signY,
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
                    if (designedSolid.GetVoxelDense(coordX, coordY, coordZ)) return;
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
                    SetVoxelDense(false, coordX, coordY, coordZ);
                }
            }
        }
        #endregion

        #region Basic Voxel Flipping/Reading Functions

        /// <summary>
        /// Sets the voxel to true or false. If you already know it's state,
        /// then it is better to use TurnVoxelOn or TurnVoxelOff.
        /// </summary>
        /// <param name="value">if set to <c>true</c> [value].</param>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        public void SetVoxelDense(bool value, int xCoord, int yCoord, int zCoord)
        {
            var bitPosition = xCoord & 7;
            var xByteCoord = xCoord >> 3;
            var oldValue = GetVoxelDense(xByteCoord, yCoord, zCoord, bitPosition); // ((byte)(Dense[coord, yCoord, zCoord] << shift) >> 7 != 0);
            if (oldValue == value) return;
            if (value) Dense[xByteCoord, yCoord, zCoord] += (byte)(1 << (7 - bitPosition));
            else Dense[xByteCoord, yCoord, zCoord] -= (byte)(1 << (7 - bitPosition));

        }
        /// <summary>
        /// Turns the voxel on ONLY if you know that it is already OFF/
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        public void TurnVoxelOnDense(int xCoord, int yCoord, int zCoord)
        {
            TurnVoxelOnDense(xCoord >> 3, yCoord, zCoord, xCoord & 7);
        }

        /// <summary>
        /// Turns the voxel on ONLY if you know that it is already OFF/
        /// </summary>
        /// <param name="xByteCoord">The x byte coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        public void TurnVoxelOnDense(int xByteCoord, int yCoord, int zCoord, int bitPosition)
        {
            Dense[xByteCoord, yCoord, zCoord] += (byte)(1 << (7 - bitPosition));
        }
        /// <summary>
        /// Turns the voxel off ONLY if you know that it is already ON.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        public void TurnVoxelOffDense(int xCoord, int yCoord, int zCoord)
        {
            TurnVoxelOffDense(xCoord >> 3, yCoord, zCoord, xCoord & 7);
        }

        /// <summary>
        /// Turns the voxel off ONLY if you know that it is already ON.
        /// </summary>
        /// <param name="xByteCoord">The x byte coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        public void TurnVoxelOffDense(int xByteCoord, int yCoord, int zCoord, int bitPosition)
        {
            Dense[xByteCoord, yCoord, zCoord] -= (byte)(1 << (7 - bitPosition));
        }

        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="xCoord">The x coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool GetVoxelDense(int xCoord, int yCoord, int zCoord)
        {
            var shift = xCoord & 7;
            var xByteCoord = xCoord >> 3;
            return GetVoxelDense(Dense[xByteCoord, yCoord, zCoord], shift);
        }
        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="xByteCoord">The x byte coord.</param>
        /// <param name="yCoord">The y coord.</param>
        /// <param name="zCoord">The z coord.</param>
        /// <param name="bitPosition">The bit position.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool GetVoxelDense(int xByteCoord, int yCoord, int zCoord, int bitPosition)
        {
            return GetVoxelDense(Dense[xByteCoord, yCoord, zCoord], bitPosition);
        }
        /// <summary>
        /// Gets the voxel.
        /// </summary>
        /// <param name="b">The b.</param>
        /// <param name="bitPosition">The bit position.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool GetVoxelDense(byte b, int bitPosition)
        {
            return (byte)(b << bitPosition) >> 7 != 0;
            // this previous line looks hacky but it is faster than the following conditional
            // if guess the reason is that simplicity of execution even though shifting would 
            // seem to do more constructing.
            //if (bitPosition == 0) return (b & 0b1) != 0;
            //else if (bitPosition == 1) return (b & 0b01) != 0;
            //else if (bitPosition == 2) return (b & 0b001) != 0;
            //else if (bitPosition == 3) return (b & 0b0001) != 0;
            //else if (bitPosition == 4) return (b & 0b00001) != 0;
            //else if (bitPosition == 5) return (b & 0b000001) != 0;
            //else if (bitPosition == 6) return (b & 0b0000001) != 0;
            //return (b & 0b00000001) != 0;
        }

        private static int CountSetBitsDense(byte val)
        {
            var bits = 0;
            while (val > 0)
            {
                bits += val & 1;
                val = (byte)(val >> 1);
            }
            return bits;
        }
        #endregion

        #region Functions for Dilation (3D Offsetting)
        private static int[][] GetVoxelsWithinCircle(double[] center, double[] dir, double radius, bool edge = false)
        {
            var voxels = new HashSet<int[]>(new SameCoordinates());

            var radii = new List<double>();
            if (!edge)
                for (var i = .0; i < radius; i += 0.5)
                    radii.Add(i);
            radii.Add(radius);
            var a = Math.Abs(dir[0]) < 1e-5
                ? new[] { .0, -dir[2], dir[1] }.normalize(3)
                : new[] { dir[1], -dir[0], 0 }.normalize(3);
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
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

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
            var voxels = new HashSet<int[]>(new[] { center.ToArray() }, new SameCoordinates());

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
            if (toolDia <= 0) return new[] { vox };

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
