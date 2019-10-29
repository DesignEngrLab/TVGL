using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace TVGL.Voxelization
{
    internal interface IVoxelRow
    {
        bool this[int index] { get; set; }
        int Count { get; }
        void TurnOnRange(int lo, int hi);
        void TurnOffRange(int lo, int hi);
        (bool, bool) GetNeighbors(int index);
        void Union(IVoxelRow[] others, int offset = 0);
        void Intersect(IVoxelRow[] others, int offset = 0);
        void Subtract(IVoxelRow[] subtrahends, int offset = 0);
    }
}
