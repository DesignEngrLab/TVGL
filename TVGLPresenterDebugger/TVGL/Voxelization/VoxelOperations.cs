using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL;
using TVGL.Voxelization;

namespace TVGLPresenterDebugger.TVGL.Voxelization
{
    public class VoxelOperations
    {


        public static HashSet<long> GetSphereCenteredOnVoxel(long voxelID, VoxelizedSolid voxelizedSolid, int r, bool solid = false)
        {
            var sphereOffsets = solid ? GetSolidSphereOffsets(r) : GetHollowSphereOffsets(r);
            return GetSphereCenteredOnVoxel(voxelID, voxelizedSolid, sphereOffsets);
        }

        public static HashSet<long> GetSphereCenteredOnVoxel(long voxelID, VoxelizedSolid voxelizedSolid, List<int[]> sphereOffsets)
        {
            var voxels = new HashSet<long>();
            foreach (var offset in sphereOffsets)
            {
                voxels.Add(voxelizedSolid.IndicesToVoxelID(AddIntArrays(voxelizedSolid.VoxelIDToIndices(voxelID), offset)));
            }
            return voxels;
        }

        public static List<int[]> GetSolidSphereOffsets(int r)
        {
            var voxelOffsets = new List<int[]>();
            var rSqaured = r * r;

            //Do all the square operations before we start.
            //These could be calculated even fuir
            // Generate a sequence of integers from -r to r 
            var offsets = Enumerable.Range(-r, 2 * r + 1).ToArray();
            // and then generate their squares.
            var squares = offsets.Select(val => val * val).ToArray();
            var xi = -1;
            foreach (var xOffset in offsets)
            {
                xi++;
                var yi = -1;
                foreach (var yOffset in offsets)
                {
                    yi++;
                    var zi = -1;
                    foreach (var zOffset in offsets)
                    {
                        //Count at start rather than at end so that if we continue, zi is correct.
                        zi++; 
                        //Euclidean distance sqrt(x^2 + y^2 + z^2) must be less than r. Square both sides to get the following.
                        if (squares[xi] + squares[yi] + squares[zi] > rSqaured) continue; //Not within the sphere.
                        voxelOffsets.Add(new []{ xOffset, yOffset, zOffset });
                    }     
                }
            }
            return voxelOffsets;
        }

        public static List<int[]> GetHollowSphereOffsets(int r)
        {
            var voxelOffsets = new List<int[]>();
            var rSqaured = r * r;

            //Do all the square operations before we start.
            //These could be calculated even fuir
            // Generate a sequence of integers from -r to r 
            var offsets = Enumerable.Range(-r, 2 * r + 1).ToArray();
            // and then generate their squares.
            var squares = offsets.Select(val => val * val).ToArray();
            var xi = -1;
            foreach (var xOffset in offsets)
            {
                xi++;
                var yi = -1;
                foreach (var yOffset in offsets)
                {
                    yi++;
                    var zi = -1;
                    foreach (var zOffset in offsets)
                    {
                        //Count at start rather than at end so that if we continue, zi is correct.
                        zi++;
                        //Euclidean distance sqrt(x^2 + y^2 + z^2) must be exactly equal to r. Square both sides to get the following.
                        if (squares[xi] + squares[yi] + squares[zi] != rSqaured) continue; //Not within the sphere.
                        voxelOffsets.Add(new[] { xOffset, yOffset, zOffset });
                    }
                }
            }
            return voxelOffsets;
        }

        public static void GetSolidCubeCenteredOnVoxel(Voxel voxel, ref VoxelizedSolid voxelizedSolid, int length)
        {
            var x = voxel.Index[0];
            var y = voxel.Index[1];
            var z = voxel.Index[2];
            var voxels = voxelizedSolid.VoxelIDHashSet;
            var r = length/2;
            for (var xOffset = -r; xOffset < r; xOffset++)
            {
                for (var yOffset = -r; yOffset < r; yOffset++)
                {
                    for (var zOffset = -r; zOffset < r; zOffset++)
                    {
                        voxels.Add(voxelizedSolid.IndicesToVoxelID(x + xOffset, y + yOffset, z + zOffset));
                    }
                }
            }
        }

        private static int[] AddIntArrays(IReadOnlyList<int> ints1, IReadOnlyList<int> ints2)
        {
            if (ints1.Count != ints2.Count) throw new Exception("This add function is only for arrays of the same size");
            var retInts = new int[ints1.Count];
            for (var i = 0; i < ints1.Count; i++)
            {
                retInts[i] = ints1[i] + ints2[i];
            }
            return retInts;
        }
    }
}
