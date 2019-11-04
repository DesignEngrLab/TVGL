using System;
using System.Collections.Generic;
using System.Text;

namespace TVGL.Voxelization
{
    internal class SameCoordinates : EqualityComparer<int[]>
    {
        public override bool Equals(int[] a1, int[] a2)
        {
            if (a1 == null && a2 == null)
                return true;
            if (a1 == null || a2 == null)
                return false;
            return (a1[0] == a2[0] &&
                    a1[1] == a2[1] &&
                    a1[2] == a2[2]);
        }
        public override int GetHashCode(int[] ax)
        {
            if (ax is null) return 0;
            var hCode = ax[0] + (ax[1] << 10) + (ax[2] << 20);
            return hCode.GetHashCode();
        }
    }
    internal class VoxelEnumerator : IEnumerator<int[]>
    {
        VoxelizedSolid vs;
        int[] currentVoxelPosition = new int[3];
        int xIndex;
        int yIndex;
        int zIndex;
        int xLim;
        int yLim;
        int zLim;
        public VoxelEnumerator(VoxelizedSolid vs)
        {
            this.vs = vs;
            this.xLim = vs.VoxelsPerSide[0];
            this.yLim = vs.VoxelsPerSide[1];
            this.zLim = vs.VoxelsPerSide[2];
        }

        public object Current => currentVoxelPosition;

        int[] IEnumerator<int[]>.Current => currentVoxelPosition;


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public bool MoveNext()
        {
            do
            {
                xIndex++;
                if (xIndex == xLim)
                {
                    xIndex = 0;
                    yIndex++;
                    if (yIndex == yLim)
                    {
                        yIndex = 0;
                        zIndex++;
                        if (zIndex == zLim) return false;
                    }
                }
            } while (!vs[xIndex, yIndex, zIndex]);
            currentVoxelPosition[0] = xIndex;
            currentVoxelPosition[1] = yIndex;
            currentVoxelPosition[2] = zIndex;
            return true;
        }

        public void Reset()
        {
            xIndex = yIndex = zIndex = 0;
        }
    }
}
