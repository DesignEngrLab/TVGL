// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="MarchingCubes.Implicit.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace TVGL
{
    /// <summary>
    /// Class MarchingCubesImplicit.
    /// Implements the <see cref="TVGL.MarchingCubes{TVGL.ImplicitSolid, System.Double}" />
    /// </summary>
    /// <seealso cref="TVGL.MarchingCubes{TVGL.ImplicitSolid, System.Double}" />
    internal class MarchingCubesImplicit : MarchingCubes<ImplicitSolid, double>
    {
        /// <summary>
        /// The surface level
        /// </summary>
        private readonly double surfaceLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarchingCubesImplicit"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="discretization">The discretization.</param>
        internal MarchingCubesImplicit(ImplicitSolid solid, double discretization)
            : base(solid, discretization)
        {
            surfaceLevel = solid.SurfaceLevel;
        }

        /// <summary>
        /// Gets the value from solid.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="z">The z.</param>
        /// <returns>ValueT.</returns>
        protected override double GetValueFromSolid(int x, int y, int z)
        {
            return solid[
                  _xMin + x * gridToCoordinateFactor,
                            _yMin + y * gridToCoordinateFactor,
                            _zMin + z * gridToCoordinateFactor
                ];
        }

        /// <summary>
        /// Determines whether the specified v is inside.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if the specified v is inside; otherwise, <c>false</c>.</returns>
        protected override bool IsInside(double v)
        {
            return v <= surfaceLevel;
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="sign">The sign.</param>
        /// <returns>System.Double.</returns>
        protected override double GetOffset(StoredValue<double> from, StoredValue<double> to,
            int direction, int sign)
        {
            if (from.Value.IsPracticallySame(surfaceLevel)) return 0.0;
            if (to.Value.IsPracticallySame(surfaceLevel)) return gridToCoordinateFactor;
            if (to.Value.IsPracticallySame(from.Value)) return gridToCoordinateFactor / 2;
            return gridToCoordinateFactor * (surfaceLevel - from.Value) / (to.Value - from.Value);
        }
    }
}