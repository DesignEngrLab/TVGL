// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)


namespace TVGL
{
    internal class MarchingCubesDenseVoxels : MarchingCubes<VoxelizedSolid, bool>
    {
        private readonly int numVoxelsPerGrid;
        private readonly double oneOverNumVoxelsPerGrid;
        private readonly double coordToVoxelIndex;


        internal MarchingCubesDenseVoxels(VoxelizedSolid solid, int numVoxelsPerGrid)
            : base(solid, solid.VoxelSideLength * numVoxelsPerGrid)

        {
            this.numVoxelsPerGrid = numVoxelsPerGrid;
            this.oneOverNumVoxelsPerGrid = 1.0 / numVoxelsPerGrid;
            coordToVoxelIndex = 1 / solid.VoxelSideLength;
        }

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