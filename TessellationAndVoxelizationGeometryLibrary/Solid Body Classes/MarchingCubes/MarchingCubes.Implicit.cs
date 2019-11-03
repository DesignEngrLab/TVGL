using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Implicit;

namespace TVGL
{
    internal class MarchingCubesImplicit : MarchingCubes<ImplicitSolid, double>
    {
        private double surfaceLevel;

        internal MarchingCubesImplicit(ImplicitSolid solid, double discretization)
            : base(solid, discretization)
        {
            surfaceLevel = solid.SurfaceLevel;
            numGridX = (int)Math.Ceiling((solid.XMax - solid.XMin) / discretization);
            numGridY = (int)Math.Ceiling((solid.YMax - solid.ZMin) / discretization);
            numGridZ = (int)Math.Ceiling((solid.ZMax - solid.ZMin) / discretization);
        }

        protected override double GetValueFromSolid(double x, double y, double z)
        {
            return solid[x, y, z];
        }

        protected override bool IsInside(double v)
        {
            return v <= surfaceLevel;
        }

        protected override double GetOffset(StoredValue<double> from, StoredValue<double> to,
            int direction, int sign)
        {
            if (from.Value.IsPracticallySame(surfaceLevel)) return 0.0;
            if (to.Value.IsPracticallySame(surfaceLevel)) return gridToCoordinateSpacing;
            if (to.Value.IsPracticallySame(from.Value)) return gridToCoordinateSpacing/2;
            return gridToCoordinateSpacing*(surfaceLevel - from.Value) / (to.Value - from.Value);
        }
    }
}
