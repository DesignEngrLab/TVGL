// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MarchingCubes.VoxelizedSolid.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************


namespace TVGL
{
    /// <summary>
    /// Class MarchingCubesDenseVoxels.
    /// Implements the <see cref="TVGL.MarchingCubes{TVGL.VoxelizedSolid, System.Boolean}" />
    /// </summary>
    /// <seealso cref="TVGL.MarchingCubes{TVGL.VoxelizedSolid, System.Boolean}" />
    internal class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, bool>
    {
        /// <summary>
        /// The number voxels per grid
        /// </summary>
        private readonly int numVoxelsPerGrid;
        /// <summary>
        /// The one over number voxels per grid
        /// </summary>
        private readonly double oneOverNumVoxelsPerGrid;
        /// <summary>
        /// The coord to voxel index
        /// </summary>
        private readonly double coordToVoxelIndex;


        /// <summary>
        /// Initializes a new instance of the <see cref="MarchingCubesDenseVoxels"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="numVoxelsPerGrid">The number voxels per grid.</param>
        internal MarchingCubesDenseVoxels(VoxelizedSolid solid, int numVoxelsPerGrid)
            : base(solid, solid.VoxelSideLength * numVoxelsPerGrid)

        {
            this.numVoxelsPerGrid = numVoxelsPerGrid;
            this.oneOverNumVoxelsPerGrid = 1.0 / numVoxelsPerGrid;
            coordToVoxelIndex = 1 / solid.VoxelSideLength;
        }

        /// <summary>
        /// Gets the value from solid.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>ValueT.</returns>
        protected override bool GetValueFromSolid(int x, int y, int z)
        {
            var xActual = _xMin + x * gridToCoordinateFactor;
            var yActual = _yMin + y * gridToCoordinateFactor;
            var zActual = _zMin + z * gridToCoordinateFactor;

            var i = (int)(coordToVoxelIndex * (xActual - solid.Offset.X));
            if (i < 0 || i >= solid.numVoxelsX) return false;
            var j = (int)(coordToVoxelIndex * (yActual - solid.Offset.Y));
            if (j < 0 || j >= solid.numVoxelsY) return false;
            var k = (int)(coordToVoxelIndex * (zActual - solid.Offset.Z));
            if (k < 0 || k >= solid.numVoxelsZ) return false;
            return solid[i, j, k];
        }

        /// <summary>
        /// Determines whether the specified v is inside.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if the specified v is inside; otherwise, <c>false</c>.</returns>
        protected override bool IsInside(bool v)
        {
            return v;
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sign">The sign.</param>
        /// <returns>System.Double.</returns>
        protected override double GetOffset(StoredValue<bool> from, StoredValue<bool> to,
            int direction, int sign)
        {
            var iFrom = (int)(coordToVoxelIndex * ((_xMin + from.X * gridToCoordinateFactor) - solid.Offset.X));
            var jFrom = (int)(coordToVoxelIndex * ((_yMin + from.Y * gridToCoordinateFactor) - solid.Offset.Y));
            var kFrom = (int)(coordToVoxelIndex * ((_zMin + from.Z * gridToCoordinateFactor) - solid.Offset.Z));
            switch (direction)
            {
                case 0:
                    for (int i = 0; i < numVoxelsPerGrid; i++)
                    {
                        if (iFrom + i + 1 <= 0) continue;
                        if (iFrom + i + 1 >= solid.numVoxelsX || (iFrom + i < 0 && solid[iFrom + i + 1, jFrom, kFrom]) ||
                        solid[iFrom + i, jFrom, kFrom] != solid[iFrom + i + 1, jFrom, kFrom])
                            return gridToCoordinateFactor * oneOverNumVoxelsPerGrid * i;
                    }
                    break;
                case 1:
                    for (int j = 0; j < numVoxelsPerGrid; j++)
                    {
                        if (jFrom + j + 1 <= 0) continue;
                        if (jFrom + j + 1 >= solid.numVoxelsY || (jFrom + j < 0 && solid[iFrom, jFrom + j + 1, kFrom]) ||
                         (solid[iFrom, jFrom + j, kFrom] != solid[iFrom, jFrom + j + 1, kFrom]))
                            return gridToCoordinateFactor * oneOverNumVoxelsPerGrid * j;
                    }
                    break;
                case 2:
                    for (int k = 0; k < numVoxelsPerGrid; k++)
                    {
                        if (kFrom + k + 1 <= 0) continue;
                        if (kFrom + k + 1 >= solid.numVoxelsZ || (kFrom + k < 0 && solid[iFrom, jFrom, kFrom + k + 1]) ||
                            (solid[iFrom, jFrom, kFrom + k] != solid[iFrom, jFrom, kFrom + k + 1]))
                            return gridToCoordinateFactor * oneOverNumVoxelsPerGrid * k;
                    }
                    break;
            }
            return gridToCoordinateFactor;
        }
    }
}