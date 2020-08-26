using System;
using TVGL.Voxelization;

namespace TVGL
{
    internal class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, bool>
    {
        private readonly double coordToVoxelIndex;
        internal MarchingCubesDenseVoxels(VoxelizedSolid solid, int numVoxelsPerGrid)
            : base(solid, solid.VoxelSideLength * numVoxelsPerGrid)

        { coordToVoxelIndex = 1 / solid.VoxelSideLength; }

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

        protected override bool IsInside(bool v)
        {
            return !v;
        }

        protected override double GetOffset(StoredValue<bool> from, StoredValue<bool> to,
            int direction, int sign)
        {
            switch (direction)
            {
                case 0:
                    var maxX = (int)(to.X - from.X);
                    for (int i = 1; i < maxX; i++)
                        if (GetValueFromSolid((from.X + sign * i), from.Y, from.Z) != GetValueFromSolid((from.X + sign * (i - 1)), from.Y, from.Z))
                            return gridToCoordinateFactor * (i + 0.5) / maxX;
                    break;
                case 1:
                    var maxY = (int)(to.Y - from.Y);
                    for (int i = 1; i < maxY; i++)
                        if (GetValueFromSolid(from.X, (from.Y + sign * i), from.Z) != GetValueFromSolid(from.X, (from.Y + sign * (i - 1)), from.Z))
                            return gridToCoordinateFactor * (i + 0.5) / maxY;
                    break;
                case 2:
                    var maxZ = (int)(to.X - from.X);
                    for (int i = 1; i < maxZ; i++)
                        if (GetValueFromSolid(from.X, from.Y, (from.Z + sign * i)) != GetValueFromSolid(from.X, from.Y, (from.Z + sign * (i - 1))))
                            return gridToCoordinateFactor * (i + 0.5) / maxZ;
                    break;
            }
            return gridToCoordinateFactor;
        }
    }
}