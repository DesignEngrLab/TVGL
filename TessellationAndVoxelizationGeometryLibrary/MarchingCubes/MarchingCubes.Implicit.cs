// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)

namespace TVGL
{
    internal class MarchingCubesImplicit : MarchingCubes<ImplicitSolid, double>
    {
        private readonly double surfaceLevel;

        internal MarchingCubesImplicit(ImplicitSolid solid, double discretization)
            : base(solid, discretization)
        {
            surfaceLevel = solid.SurfaceLevel;
        }

        protected override double GetValueFromSolid(int x, int y, int z)
        {
            return solid[
                  _xMin + x * gridToCoordinateFactor,
                            _yMin + y * gridToCoordinateFactor,
                            _zMin + z * gridToCoordinateFactor
                ];
        }

        protected override bool IsInside(double v)
        {
            return v <= surfaceLevel;
        }

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