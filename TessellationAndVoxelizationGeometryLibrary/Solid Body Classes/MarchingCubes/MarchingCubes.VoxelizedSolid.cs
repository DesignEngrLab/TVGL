using System;
using TVGL.Voxelization;

namespace TVGL
{
    internal class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, bool>
    {
        internal MarchingCubesDenseVoxels(VoxelizedSolid solid, double discretization)
            : base(solid, discretization)
        {
            numGridX = (int)Math.Ceiling(solid.VoxelsPerSide[0] / discretization);
            numGridY = (int)Math.Ceiling(solid.VoxelsPerSide[1] / discretization);
            numGridZ = (int)Math.Ceiling(solid.VoxelsPerSide[2] / discretization);
            yMultiplier = numGridX;
            zMultiplier = numGridX * numGridY;
            solidOffset = new double[3];
        }

        protected override bool GetValueFromSolid(double x, double y, double z)
        {
            return solid[(int)x, (int)y, (int)z];
        }

        protected override bool IsInside(bool v)
        {
            return v;
        }

        protected override double GetOffset(StoredValue<bool> from, StoredValue<bool> to,
            int direction, int sign)
        {
            switch (direction)
            {
                case 0:
                    var maxX = (int)(to.X - from.X);
                    for (int i = 0; i < maxX; i++)
                        if (!solid[(int)(from.X + sign * i), (int)from.Y, (int)from.Z])
                            return gridToCoordinateSpacing * (i + 0.5) / maxX;
                    break;
                case 1:
                    var maxY = (int)(to.Y - from.Y);
                    for (int i = 0; i < maxY; i++)
                        if (!solid[(int)from.X, (int)(from.Y + sign * i), (int)from.Z])
                            return gridToCoordinateSpacing * (i + 0.5) / maxY;
                    break;
                case 2:
                    var maxZ = (int)(to.X - from.X);
                    for (int i = 0; i < maxZ; i++)
                        if (!solid[(int)from.X, (int)from.Y, (int)(from.Z + sign * i)])
                            return gridToCoordinateSpacing * (i + 0.5) / maxZ;
                    break;
            }
            return gridToCoordinateSpacing;
        }
    }
}