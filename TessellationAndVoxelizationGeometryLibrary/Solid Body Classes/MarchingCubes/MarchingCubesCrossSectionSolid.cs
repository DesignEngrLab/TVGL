using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Implicit;

namespace TVGL
{
    internal class MarchingCubesCrossSectionSolid : MarchingCubes<CrossSectionSolid, double>
    {
        double[][,] gridLayers;
        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid, double discretization)
            : base(solid, discretization)
        {
            var closestFacingDir = 0;
            if (Math.Abs(solid.Direction[1]) > Math.Abs(solid.Direction[closestFacingDir])) closestFacingDir = 1;
            if (Math.Abs(solid.Direction[2]) > Math.Abs(solid.Direction[closestFacingDir])) closestFacingDir = 2;
            var newDirection = new double[3];
            newDirection[closestFacingDir] = Math.Sign(solid.Direction[closestFacingDir]);

            numGridX = (int)Math.Ceiling((solid.XMax - solid.XMin) / discretization);
            numGridY = (int)Math.Ceiling((solid.YMax - solid.ZMin) / discretization);
            numGridZ = (int)Math.Ceiling((solid.ZMax - solid.ZMin) / discretization);
        }

        protected override double GetValueFromSolid(double x, double y, double z)
        {
            return gridLayers[(int)z][(int)x, (int)y];
        }

        protected override bool IsInside(double v)
        {
            return v <= 0.0;
        }

        protected override double GetOffset(StoredValue<double> from, StoredValue<double> to,
            int direction, int sign)
        {
            if (from.Value.IsNegligible()) return 0.0;
            if (to.Value.IsNegligible()) return gridToCoordinateSpacing;
            return -gridToCoordinateSpacing * from.Value / (to.Value - from.Value);
        }
    }
}
