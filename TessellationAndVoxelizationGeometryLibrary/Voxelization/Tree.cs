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

        //This tree is currently set up to work on 1000+ vertices along a direction. For smaller spaces, 
        //internalNode2 should probably be ignored?
        public VoxelTreeBase(VoxelizedSolid space)
        {
            //(4,3,2) => ~1.5^n 

            //We could insted do a 3x3x3, 4x4x4, 5x5x5, = 60 voxels to a side, so for 1000, 
            //you would have 17 SecondInternalNodes along one direction.
            var branchingFactors = new int[] {2, 3, 4};//4, 8, 16
            var leafNodeVoxelsAlongSide = Math.Pow(2, branchingFactors[0]);  // 4 along side, 64 total voxels.
            var internalNode1VoxelsAlongSide = Math.Pow(2, branchingFactors[1]) * leafNodeVoxelsAlongSide; //32 along side, 32,768 total
            var internalNode2VoxelsAlongSide = Math.Pow(2, branchingFactors[2]) * internalNode1VoxelsAlongSide;  //512 along side.  
            var scaleToLeafNode2 = space.ScaleToIntSpace*8;

            //Determine the number of second internal nodes, given the size of the space.
            //This could result in a 1*3*1, 2*2*2, or similar arrangment. It is not explicitly restricted in size or shape.

            //Perform a dense fill of the space??


            //If a leaf contains no border voxels, then it must be either dense or empty.
            //Check one voxel index, and if it is contained in the dense fill, set leaf node to dense, 
            //else set it to empty. 

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


        public HashSet<ulong> DenseFill(VoxelizedSolid space)
        {
            //First make some lookup tables of the voxel indices
            //Dictionary(x, HashSet(yzValue), where yzValue could be the full Morton code. 
            //Dictionary(y, HashSet(xzValue), where yzValue could be the full Morton code. 
            //zSpace = Dictionary(z, HashSet(xyValue), where yzValue could be the full Morton code. 

            //Start at min(x, y, z)
            //Check for fill along the X direction, then shift +Y. When done with the row, 
            //Shift up +Z and set Y back to the start

            //Get all the Z and Y voxels at this level. 
            //var voxelsAtZ = new HashSet(zSpace[z]);
            //var voxelsAtY = new HashSet(ySpace[y]);

            //If the voxel is contained in both voxelsAtZ && voxelsAtY, 
            //then it is part of the current ray. sort them by X value.
            //Assume no knife edges (there is always negative space in the shell)
            //Use a bool turning on and off every time a cavity is reached. 
            //If the bool is on, then store all the voxel indices in that region.

            throw new NotImplementedException();
        }
    }
}
