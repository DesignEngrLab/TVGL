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
        bool onLayers = false;
        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid)
            : this(solid, (solid.StepDistances[^1] - solid.StepDistances[0]) / (solid.NumLayers - 1))
        { }
        internal MarchingCubesCrossSectionSolid(CrossSectionSolid solid, double discretization)
            : base(solid, discretization)
        {
            var distanceBetweenLayers = (solid.StepDistances[^1] - solid.StepDistances[0]) / (solid.NumLayers - 1);
            onLayers = distanceBetweenLayers.IsPracticallySame(discretization);
            var closestFacingDir = 0;
            if (Math.Abs(solid.Direction[1]) > Math.Abs(solid.Direction[closestFacingDir])) closestFacingDir = 1;
            if (Math.Abs(solid.Direction[2]) > Math.Abs(solid.Direction[closestFacingDir])) closestFacingDir = 2;
            var newDirection = new double[3];
            newDirection[closestFacingDir] = Math.Sign(solid.Direction[closestFacingDir]);

            numGridX = (int)Math.Ceiling((solid.XMax - solid.XMin) / discretization);
            numGridY = (int)Math.Ceiling((solid.YMax - solid.ZMin) / discretization);
            numGridZ = (int)Math.Ceiling((solid.ZMax - solid.ZMin) / discretization);
            var gridLayersToStore =
            (distanceBetweenLayers <= discretization) ? 2
            : (2 * distanceBetweenLayers <= discretization) ? 3 : 4;
            gridLayers = new double[gridLayersToStore][,];

        }

        internal override TessellatedSolid Generate()
        {
            //Calculate new grid
            //interpolate points for grid
            //call marching cubes function for this z-layer
            for (var i = 0; i < numGridX - 1; i++)
                for (var j = 0; j < numGridY - 1; j++)
                    for (var k = 0; k < numGridZ - 1; k++)
                        MakeFacesInCube(i, j, k);
            var comments = new List<string>(solid.Comments);
            comments.Add("tessellation (via marching cubes) of the voxelized solid, " + solid.Name);
            return new TessellatedSolid(faces);
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
            if (to.Value.IsNegligible()) return gridToCoordinateFactor;
            return -gridToCoordinateFactor * from.Value / (to.Value - from.Value);
        }
    }
}
