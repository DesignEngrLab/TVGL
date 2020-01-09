using System;
using TVGL.Voxelization;

namespace TVGL
{
    internal class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, bool>
    {

        internal MarchingCubesDenseVoxels(VoxelizedSolid solid, int numVoxelsPerGrid)
            : base(solid, solid.VoxelSideLength*numVoxelsPerGrid)
        { }

        protected override bool GetValueFromSolid(int x, int y, int z)
        {
            return solid[x, y, z];
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
                    for (int i = 1; i < maxX; i++)
                        if (solid[(int)(from.X + sign * i), (int)from.Y, (int)from.Z]!= solid[(int)(from.X + sign * (i-1)), (int)from.Y, (int)from.Z])
                            return gridToCoordinateFactor * (i + 0.5) / maxX;
                    break;
                case 1:
                    var maxY = (int)(to.Y - from.Y);
                    for (int i = 1; i < maxY; i++)
                        if (solid[(int)from.X, (int)(from.Y + sign * i), (int)from.Z]!=solid[(int)from.X, (int)(from.Y + sign * (i-1)), (int)from.Z])
                           return gridToCoordinateFactor * (i + 0.5) / maxY;
                    break;
                case 2:
                    var maxZ = (int)(to.X - from.X);
                    for (int i = 1; i < maxZ; i++)
                        if (solid[(int)from.X, (int)from.Y, (int)(from.Z + sign * i)]!= solid[(int)from.X, (int)from.Y, (int)(from.Z + sign * (i-1))])
                            return gridToCoordinateFactor * (i + 0.5) / maxZ;
                    break;
            }
            return gridToCoordinateFactor;
        }
    }
}