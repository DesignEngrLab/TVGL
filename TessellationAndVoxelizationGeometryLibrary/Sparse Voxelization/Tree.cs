using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;   


namespace TVGL.SparseVoxelization
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

        //This tree is currently set up to work on 1000+ vertices along a direction. For smaller spaces, 
        //internalNode2 should probably be ignored?
        public VoxelTreeBase(VoxelSpace space)
        {
            //(4,3,2) => ~1.5^n 
            var branchingFactors = new int[] {2, 3, 4};//4, 8, 16
            var leafNodeVoxelsAlongSide = Math.Pow(2, branchingFactors[0]);  // 4 along side, 64 total voxels.
            var internalNode1VoxelsAlongSide = Math.Pow(2, branchingFactors[1]) * leafNodeVoxelsAlongSide; //32 along side, 32,768 total
            var internalNode2VoxelsAlongSide = Math.Pow(2, branchingFactors[2]) * internalNode1VoxelsAlongSide;  //512 along side.  
            var scaleToLeafNode2 = space.ScaleToIntSpace*8;

            //Determine the number of second internal nodes, given the size of the space.
            //This could result in a 1*3*1, 2*2*2, or similar arrangment. It is not explicitly restricted in size or shape.
            
            //Perform a solid fill of the space??

            //Foreach second internal node, create all its first internal nodes by dividing its volume into the pre-specified sizes
            //Foreach (var internalNode2 )
            //internalNode2.GenerateChildren(space)

            //Foreach first internal nodes, create all its leaf nodes by dividing its volume into the pre-specified sizes.

            //Foreach leafNode, check all the possible voxel indices to see if they are in the VoxelSpace.
            //If none are active, set the leafNode to empty. If all are active, set the leafNode to filled.
            //Any calls to a leafNode that is empty or filled, won't actually visit the voxel level. 

            //If all the leafNodes were empty or filled, set the first internal node accordingly.
            //If all the first internalNodes were empty or filled, set the second internal node accordingly.
        }
    }
}
