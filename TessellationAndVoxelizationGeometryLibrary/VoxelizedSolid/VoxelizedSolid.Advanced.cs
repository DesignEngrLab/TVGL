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
using System.Threading.Tasks;

namespace TVGL
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid : Solid, IEnumerable<(int xIndex, int yIndex, int zIndex)>
    {
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

        public void Subtract(PrimitiveSurface surface)
        {
            var minIndices = ConvertCoordinatesToIndices(new Vector3(surface.MinX, surface.MinY, surface.MinZ));
            var maxIndices = ConvertCoordinatesToIndices(new Vector3(surface.MaxX, surface.MaxY, surface.MaxZ));
            var minJ = Math.Max(0, minIndices[1]);
            var maxJ = Math.Min(numVoxelsY, maxIndices[1]);
            var minK = Math.Max(0, minIndices[2]);
            var maxK = Math.Min(numVoxelsZ, maxIndices[2]);


            Parallel.For(minK, maxK, k =>
            //for (var k = minK; k < maxK; k++)
            {
                var zCoord = ConvertZIndexToCoord(k);
                for (int j = minJ; j < maxJ; j++)
                {
                    var yCoord = ConvertYIndexToCoord(j);
                    var voxRow = (VoxelRowSparse)voxels[k * zMultiplier + j];
                    var crossings = new PriorityQueue<(bool, double), double>();
                    foreach (var q in surface.LineIntersection(new Vector3(XMin, yCoord, zCoord), Vector3.UnitX))
                        crossings.Enqueue((surface.GetNormalAtPoint(q.intersection).X < 0, q.lineT), q.lineT);
                    var start = (ushort)0;
                    if (crossings.Count == 0) continue;
                    var startDefined = !crossings.Peek().Item1;
                    while (crossings.Count > 0)
                    {
                        var next = crossings.Dequeue();
                        var xIndex = (ushort)ConvertXCoordToIndex(next.Item2);
                        var breakAfterThis = false;
                        if (xIndex >= numVoxelsX)
                        {
                            if (startDefined) voxRow.TurnOffRange(start, numVoxelsX);
                            breakAfterThis = true;
                        }
                        else if (startDefined)
                        {
                            voxRow.TurnOffRange(start, xIndex);
                            startDefined = false;
                        }
                        else
                        {
                            start = xIndex;
                            startDefined = true;
                        }
                        if (breakAfterThis) break;
                    }
                }
            } );
        }
        #endregion
    }
}
