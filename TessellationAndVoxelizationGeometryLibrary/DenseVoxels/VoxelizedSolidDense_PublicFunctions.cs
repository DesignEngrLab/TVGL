using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StarMathLib;
using TVGL.Voxelization;

namespace TVGL.DenseVoxels
{
    /// <summary>
    /// Class VoxelizedSolidDense.
    /// </summary>
    public partial class VoxelizedSolidDense
    {
        #region Getting Neighbors
        public int[][] GetNeighbors(int i, int j, int k)
        {
            var neighbors = new int[][] { null, null, null, null, null, null };
            if (i + 1 != VoxelsPerSide[0] && Voxels[i + 1, j, k] != 0) neighbors[1] = new[] { i + 1, j, k };
            if (j + 1 != VoxelsPerSide[1] && Voxels[i, j + 1, k] != 0) neighbors[3] = new[] { i, j + 1, k };
            if (k + 1 != VoxelsPerSide[2] && Voxels[i, j, k + 1] != 0) neighbors[5] = new[] { i, j, k + 1 };
            if (i != 0 && Voxels[i - 1, j, k] != 0) neighbors[0] = new[] { i - 1, j, k };
            if (j != 0 && Voxels[i, j - 1, k] != 0) neighbors[2] = new[] { i, j - 1, k };
            if (k != 0 && Voxels[i, j, k - 1] != 0) neighbors[4] = new[] { i, j, k - 1 };
            return neighbors;
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
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                var counter = 0;
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 1; k < VoxelsPerSide[2]; k++)
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
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                var neighborCount = 0;
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 1; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] != 0)
                        neighborCount += NumNeighbors(i, j, k);
                neighbors.TryAdd(i, neighborCount);
            });
            SurfaceArea = (Count * 6 - neighbors.Values.Sum()) * Math.Pow(VoxelSideLength, 2);
        }
        #endregion

        #region Copy
        public VoxelizedSolidDense Copy()
        {
            return new VoxelizedSolidDense(this);
        }

        public static VoxelizedSolidDense Copy(VoxelizedSolidDense vs)
        {
            return new VoxelizedSolidDense(vs);
        }
        #endregion

        #region Boolean functions
        public VoxelizedSolidDense CreateBoundingSolid()
        {
            return new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds, 1);
        }

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

        public VoxelizedSolidDense UnionToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = Copy();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                {
                    if (solids.Any(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 1;
                }
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense IntersectToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = new VoxelizedSolidDense(VoxelsPerSide, Discretization, VoxelSideLength, Bounds);
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (Voxels[i, j, k] == 1 && solids.All(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 1;
            });
            vs.UpdateProperties();
            return vs;
        }

        public VoxelizedSolidDense SubtractToNewSolid(params VoxelizedSolidDense[] solids)
        {
            var vs = Copy();
            Parallel.For(0, VoxelsPerSide[0], i =>
            {
                for (var j = 0; j < VoxelsPerSide[1]; j++)
                for (var k = 0; k < VoxelsPerSide[2]; k++)
                    if (solids.Any(solid => solid.Voxels[i, j, k] != 0))
                        vs.Voxels[i, j, k] = 0;
            });
            vs.UpdateProperties();
            return vs;
        }
        #endregion

        #region Draft in VoxelDirection
        public VoxelizedSolidDense DraftToNewSolid(VoxelDirections vd)
        {
            var vs = Copy();

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

            Parallel.For(0, VoxelsPerSide[planeIndices[0]], m =>
            {
                for (var n = 0; n < VoxelsPerSide[planeIndices[1]]; n++)
                {
                    var fillAll = false;
                    for (var p = 0; p < VoxelsPerSide[draftIndex]; p++)
                    {
                        var q = draftDir > 0 ? p : VoxelsPerSide[draftIndex] - 1 - p;
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
            var copy = Copy();
            copy.ErodeVoxelSolid(designedSolid, dir.normalize(3), tLimit, toolDia, toolOptions);
            copy.UpdateProperties();
            return copy;
        }

        public VoxelizedSolidDense ErodeToNewSolid(VoxelizedSolidDense designedSolid, VoxelDirections dir,
            double tLimit = 0, double toolDia = 0, params string[] toolOptions)
        {
            var copy = Copy();

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
            var xLim = VoxelsPerSide[0];
            var yLim = VoxelsPerSide[1];
            var zLim = VoxelsPerSide[2];

            tLimit = tLimit <= 0 ? VoxelsPerSide.norm2() : tLimit / VoxelSideLength;
            var mLimit = tLimit + VoxelsPerSide.norm2();
            var mask = CreateProjectionMask(dir, mLimit);
            var starts = GetAllVoxelsOnBoundingSurfaces(dirX, dirY, dirZ, toolDia);
            var sliceMask = ThickenMask(mask[0], dir, toolDia, toolOptions);

            Parallel.ForEach(starts, vox =>
                ErodeMask(designedSolid, mask, dirX, dirY, dirZ, xLim, yLim, zLim, sliceMask, vox));
            //foreach (var vox in starts)
            //    ErodeMask(designedSolid, mask, tLimit, stopAtPartial, dir, sliceMask, vox);
        }

        private static IEnumerable<VoxelDirections> GetVoxelDirections(double dirX, double dirY, double dirZ)
        {
            var dirs = new VoxelDirections[3];
            var signedDir = new[] { Math.Sign(dirX), Math.Sign(dirY), Math.Sign(dirZ) };
            for (var i = 0; i < 3; i++)
            {
                if (signedDir[i] == 0) continue;
                dirs[i] = ((VoxelDirections)((i + 1) * -1 * signedDir[i]));
            }
            return dirs;
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

        private void ErodeMask(VoxelizedSolidDense designedSolid, IReadOnlyList<int[]> mask, double dirX, double dirY,
            double dirZ, int xLim, int yLim, int zLim, int[][] sliceMask = null, int[] start = null)
        {
            start = start ?? mask[0].ToArray();
            sliceMask = sliceMask ?? new[] {mask[0].ToArray()};
            var sliceMaskCount = sliceMask.Length;
            var xMask = mask[0][0];
            var yMask = mask[0][1];
            var zMask = mask[0][2];
            var xShift = start[0] - xMask;
            var yShift = start[1] - yMask;
            var zShift = start[2] - zMask;

            //foreach depth or timestep
            foreach (var initCoord in mask)
            {
                var xStartCoord = initCoord[0] + xShift;
                var yStartCoord = initCoord[1] + yShift;
                var zStartCoord = initCoord[2] + zShift;

                var xTShift = xStartCoord - xMask;
                var yTShift = yStartCoord - yMask;
                var zTShift = zStartCoord - zMask;

                var voxelsToRemove = new int[sliceMaskCount][];
                var i = 0;

                //Iterate over the template of the slice mask
                //to move them to the appropriate location but 
                //need to be sure that we are in the space (not negative)
                var succeedCounter = 0;

                foreach (var voxCoord in sliceMask)
                {
                    var coordX = voxCoord[0] + xTShift;
                    var coordY = voxCoord[1] + yTShift;
                    var coordZ = voxCoord[2] + zTShift;
                    if (PrecedesBounds(coordX, coordY, coordZ, dirX, dirY, dirZ, xLim, yLim, zLim)) continue;
                    
                    if (SucceedsBounds(coordX, coordY, coordZ, dirX, dirY, dirZ, xLim, yLim, zLim))
                    {
                        succeedCounter++;
                        // Return if you've left the part
                        if (succeedCounter == sliceMaskCount)
                        {
                            foreach (var vox in voxelsToRemove)
                            {
                                if (vox is null) return;
                                Voxels[vox[0], vox[1], vox[2]] = 0;
                            }
                            return;
                        }
                        continue;
                    }

                    //Return if you've hit the as-designed part
                    if (designedSolid.Voxels[coordX, coordY, coordZ] != 0)
                        return;

                    voxelsToRemove[i] = new[] { coordX, coordY, coordZ };
                    i++;
                }

                foreach (var vox in voxelsToRemove)
                {
                    if (vox is null) break;
                    Voxels[vox[0], vox[1], vox[2]] = 0;
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

        private static bool SucceedsBounds(int i, int j, int k, double dirX, double dirY, double dirZ, int xLim, int yLim,
            int zLim)
        {
            if (dirX < 0 && i < 0) return true;
            if (dirX > 0 && i >= xLim) return true;
            if (dirX == 0 && (i < 0 || i >= xLim)) return true;

            if (dirY < 0 && j < 0) return true;
            if (dirY > 0 && j >= yLim) return true;
            if (dirY == 0 && (j < 0 || j >= yLim)) return true;

            if (dirZ < 0 && k < 0) return true;
            if (dirZ > 0 && k >= zLim) return true;
            return dirZ == 0 && (k < 0 || k >= zLim);
        }

        private static bool PrecedesBounds(int i, int j, int k, double dirX, double dirY, double dirZ, int xLim, int yLim,
            int zLim)
        {
            if (dirX < 0 && i >= xLim) return true;
            if (dirX > 0 && i < 0) return true;
            if (dirX == 0 && (i >= xLim || i < 0)) return true;

            if (dirY < 0 && j >= yLim) return true;
            if (dirY > 0 && j < 0) return true;
            if (dirY == 0 && (j >= yLim || j < 0)) return true;

            if (dirZ < 0 && k >= zLim) return true;
            if (dirZ > 0 && k < 0) return true;
            return dirZ == 0 && (k >= zLim || k < 0);
        }

        private static bool OutsideBounds(int i, int j, int k, int xLim, int yLim, int zLim)
        {
            return i < 0 || i >= xLim ||
                   j < 0 || j >= yLim ||
                   k < 0 || k >= zLim;
        }

        private static int[][] GetVoxelsWithinCircle(IReadOnlyList<double> center, IList<double> dir, double radius,
            bool edge = false)
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

        private static int[][] GetVoxelsOnCone(IReadOnlyList<int> center, IList<double> dir, double radius,
            double angle)
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

        private static int[][] GetVoxelsOnHemisphere(IReadOnlyList<int> center, IList<double> dir, double radius)
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

        private int[][] ThickenMask(IReadOnlyList<int> vox, IList<double> dir, double toolDia,
            params string[] toolOptions)
        {
            if (toolDia <= 0) return new[] {vox.ToArray()};

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

        private List<int[]> CreateProjectionMask(double[] dir, double tLimit)
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
            return voxels;
        }

        //Exclusive by default (i.e. if line passes through vertex/edge it ony includes two voxels that are actually passed through)
        private static int[] GetNextVoxelCoord(IReadOnlyList<double> cInt, IReadOnlyList<double> direction)
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
        private IEnumerable<double> FindIntersectionDistances(IReadOnlyList<double> firstVoxel,
            IReadOnlyList<double> direction, double tLimit)
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

            var sortedIntersections = new SortedSet<double>(intersections);
            return sortedIntersections;
        }
        #endregion
    }
}
