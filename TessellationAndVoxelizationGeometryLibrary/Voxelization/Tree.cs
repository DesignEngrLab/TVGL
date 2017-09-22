using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.Voxelization
{
    public class VoxelTreeBase
    {
        /// A tree with only a root node and leaf nodes has depth 2, for example.
        public int TreeDepth = 0;
        /// Return the number of leaf nodes.
        public int LeafCount= 0;
        /// Return the number of non-leaf nodes.
        public int NonLeafCount = 0;
        /// Return the number of active voxels stored in leaf nodes.
        public long ActiveLeafVoxelCount = 0;
        /// Return the number of inactive voxels stored in leaf nodes.
        public long InactiveLeafVoxelCount = 0;
        /// Return the total number of active voxels.
        public long ActiveVoxelCount = 0;
        /// Return the number of inactive voxels within the bounding box of all active voxels.
        public long InactiveVoxelCount = 0;
        /// Return the total number of active tiles.
        public long ActiveTileCount = 0;
        /// Return the total amount of memory in bytes occupied by this tree.
        public long MemUsage = 0;

//        /// A tree with only a root node and leaf nodes has depth 2, for example.
//        Index treeDepth() const override { return DEPTH; }
//    /// Return the number of leaf nodes.
//    Index32 leafCount() const override { return mRoot.leafCount(); }
///// Return the number of non-leaf nodes.
//Index32 nonLeafCount() const override { return mRoot.nonLeafCount(); }
//    /// Return the number of active voxels stored in leaf nodes.
//    Index64 activeLeafVoxelCount() const override { return mRoot.onLeafVoxelCount(); }
//    /// Return the number of inactive voxels stored in leaf nodes.
//    Index64 inactiveLeafVoxelCount() const override { return mRoot.offLeafVoxelCount(); }
//    /// Return the total number of active voxels.
//    Index64 activeVoxelCount() const override { return mRoot.onVoxelCount(); }
//    /// Return the number of inactive voxels within the bounding box of all active voxels.
//    Index64 inactiveVoxelCount() const override;
//    /// Return the total number of active tiles.
//    Index64 activeTileCount() const override { return mRoot.onTileCount(); }

//    /// Return the minimum and maximum active values in this tree.
//    void evalMinMax(ValueType &min, ValueType &max) const;

//Index64 memUsage() const override { return sizeof(* this) + mRoot.memUsage(); }


public VoxelTreeBase()
        {
        }



    }
}
