using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL.SparseVoxelization
{
    public class InternalNode
    {
        public bool IsUniform { get; set; }

        public int Level;

        public InternalNode(int level)
        {
            Level = level;
        }
    }
}
