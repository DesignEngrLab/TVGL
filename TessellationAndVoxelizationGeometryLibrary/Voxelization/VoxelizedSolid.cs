// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 09-21-2017
// ***********************************************************************
// <copyright file="VoxelizedSolid.cs" company="Design Engineering Lab">
//     Copyright ©  2017
// </copyright>
// <summary></summary>
// ***********************************************************************

using StarMathLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace TVGL.Voxelization
{
    /// <summary>
    /// Class VoxelizedSolid.
    /// </summary>
    public partial class VoxelizedSolid
    {
        #region Properties
        /// <summary>
        /// The discretization level
        /// </summary>
        public VoxelDiscretization Discretization
        {
            get => (VoxelDiscretization)discretizationLevel;
            private set => discretizationLevel = (int)value;
        }
        private int discretizationLevel;

        /// <summary>
        /// The voxel side length. It's a square, so all sides are the same length.
        /// </summary>
        public double[] VoxelSideLength { get; private set; }

        /// <summary>
        /// Gets the offset that moves the model s.t. the lowest elements are at 0,0,0.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public double[] Offset => Bounds[0];
        private double[][] Bounds;

        #endregion

        #region Private Fields

        private readonly double[][] transformedCoordinates;
        private readonly double[] dimensions;
        private readonly int longestDimensionIndex;
        private readonly Dictionary<long, VoxelClass> voxelDictionaryLevel0;
        private readonly Dictionary<long, VoxelClass> voxelDictionaryLevel1;
        #endregion
        
        #region converting IDs and back again

        private static long MakeVoxelID(int x, int y, int z, int level, int startDiscretizationLevel, params VoxelRoleTypes[] levels)
        {
            var shift = 4 * (startDiscretizationLevel - level);
            var xLong = (long)x >> shift;
            var yLong = (long)y >> shift;
            var zLong = (long)z >> shift;
            shift = 4 * (4 - level);
            xLong = xLong << (40 + shift); //can't you combine with the above? no. The shift is doing both division
            yLong = yLong << (20 + shift); // and remainder. What I mean to say is that e.g. 7>>2<<2 = 4
            zLong = zLong << (shift);
            //  flags  x0   x1    x2   x3    x4   y0   y1    y2   y3    y4    z0   z1    z2   z3    z4 
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            return xLong + yLong + zLong + SetRoleFlags(levels);
        }
        private static long MakeVoxelID1(int x, int y, int z)
        {
            var xLong = (long)x << 52;
            var yLong = (long)y << 32;
            var zLong = (long)z << 12;
            //  flags  x0   x1    x2   x3    x4   y0   y1    y2   y3    y4    z0   z1    z2   z3    z4 
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            return xLong + yLong + zLong;
        }

        private static long MakeVoxelID0(int x, int y, int z)
        {
            var xLong = (long)x >> 4; //this shift is to clear out the level-1 values
            var yLong = (long)y >> 4;
            var zLong = (long)z >> 4;
            xLong = xLong << 56;
            yLong = yLong << 36;
            zLong = zLong << 16;
            //  flags  x0   x1    x2   x3    x4   y0   y1    y2   y3    y4    z0   z1    z2   z3    z4 
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            return xLong + yLong + zLong;
        }

        internal static long SetRoleFlags(VoxelRoleTypes[] levels)
        {
            if (levels == null || !levels.Any()) return 0L << 60; //no role is specified
            if (levels[0] == VoxelRoleTypes.Empty) return 1L << 60; //the rest of the levels would also be empty
            if (levels[0] == VoxelRoleTypes.Full) return 2L << 60; // the rest of the levels would also be full
            if (levels[0] == VoxelRoleTypes.Partial && levels.Length == 1) return 3L << 60;
            // level 0 is partial but the smaller voxels could be full, empty of partial. 
            // they are not specified if the length is only one. If the length is more
            // than 1, then go to next level
            if (levels[1] == VoxelRoleTypes.Empty) return 4L << 60; //the rest are empty
            if (levels[1] == VoxelRoleTypes.Full) return 5L << 60; // the rest are full
            if (levels[1] == VoxelRoleTypes.Partial && levels.Length == 2) return 6L << 60;
            if (levels[2] == VoxelRoleTypes.Empty) return 7L << 60; //the rest are empty
            if (levels[2] == VoxelRoleTypes.Full) return 8L << 60; // the rest are full
            if (levels[2] == VoxelRoleTypes.Partial && levels.Length == 3) return 9L << 60;
            if (levels[3] == VoxelRoleTypes.Empty) return 10L << 60; //the rest are empty
            if (levels[3] == VoxelRoleTypes.Full) return 11L << 60; // the rest are full
            if (levels[3] == VoxelRoleTypes.Partial && levels.Length == 4) return 12L << 60;
            if (levels[3] == VoxelRoleTypes.Empty) return 13L << 60;
            if (levels[4] == VoxelRoleTypes.Full) return 14L << 60;
            return 15L << 60;
        }

        internal static VoxelRoleTypes[] GetRoleFlags(object flags)
        {
            return GetRoleFlags((long)flags);
        }

        internal static VoxelRoleTypes[] GetRoleFlags(long flags)
        {
            flags = flags >> 60;
            if (flags == 0) return new VoxelRoleTypes[0]; //no role is specified
            if (flags == 1) return new[] { VoxelRoleTypes.Empty }; // could add a bunch more empties. is this necessary?
            if (flags == 2) return new[] { VoxelRoleTypes.Full };
            if (flags == 3) return new[] { VoxelRoleTypes.Partial };
            if (flags == 4) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Empty }; //the rest are empty
            if (flags == 5) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Full }; // the rest are full
            if (flags == 6) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
            if (flags == 7) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Empty };
            if (flags == 8) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full };
            if (flags == 9) return new[] { VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial };
            if (flags == 10)
                return new[]
                    {VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Empty};
            if (flags == 11)
                return new[]
                    {VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Full};
            if (flags == 12)
                return new[]
                    {VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial};
            if (flags == 13)
                return new[]
                {
                    VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Empty
                };
            if (flags == 14)
                return new[]
                {
                    VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                    VoxelRoleTypes.Full
                };
            return new[]
            {
                VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial,
                VoxelRoleTypes.Partial
            };
        }

        internal static int[] GetCoordinatesFromID(long ID, int level, int startDiscretizationLevel)
        {
            return new[]
            {
                GetCoordinateFromID(ID, 0, level,startDiscretizationLevel),
                GetCoordinateFromID(ID, 1, level,startDiscretizationLevel),
                GetCoordinateFromID(ID, 2, level,startDiscretizationLevel)
            };
        }

        internal static int GetCoordinateFromID(long id, int dimension, int level, int startDiscretizationLevel)
        {
            var shift = 4 * (4 - startDiscretizationLevel) - 4 * (startDiscretizationLevel - level);
            shift += 20 * (2 - dimension);
            if (dimension == 0) //x starts at 40 and goes to the end,60
            {
                var xCoord = id & Constants.maskAllButX;
                xCoord = xCoord >> shift;
                return (int)xCoord; //the & is to clear out the flags
            }
            if (dimension == 1) // y starts at 20 and goes to 40
            {
                var yCoord = id & Constants.maskAllButY;
                yCoord = yCoord >> shift;
                return (int)yCoord; // the & is to clear out the x value and the flags
            }
            var zCoord = id & Constants.maskAllButZ;
            zCoord = zCoord >> shift;
            return (int)zCoord; // the & is to clear out the x and y values and the flags
        }


        internal static long MakeCoordinateZero(long id, int dimension)
        {
            if (dimension == 0)
            {
                var idwoX = id & Constants.maskOutX;
                return idwoX;
            }
            if (dimension == 1)
            {
                var idwoY = id & Constants.maskOutY;
                return idwoY;
            }
            var idwoZ = id & Constants.maskOutZ;
            return idwoZ;
        }

        internal static long ChangeCoordinate(long id, long newValue, int dimension, int level, int startDiscretizationLevel)
        {
            var shift = 4 * (4 - startDiscretizationLevel) - 4 * (startDiscretizationLevel - level);
            shift += 20 * (2 - dimension);
            newValue = newValue << shift;
            return newValue + MakeCoordinateZero(id, dimension);
        }

        internal static long GetContainingVoxel(long id, int level)
        {
            switch (level)
            {
                case 0: return id & Constants.maskAllButLevel0;
                case 1:
                    return id & Constants.maskAllButLevel0and1;
                case 2:
                    return (id & Constants.maskAllButLevel01and2) + SetRoleFlags(new[]
                               {VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial});
                case 3:
                    return (id & Constants.maskLevel4) + SetRoleFlags(new[]
                    {
                        VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial, VoxelRoleTypes.Partial
                    });
            }
            throw new ArgumentOutOfRangeException("containing level must be 0, 1, 2, or 3");
        }

        #endregion


        /// <summary>
        /// Is the double currently at an integer value?
        /// </summary>
        /// <param name="d">The d.</param>
        /// <returns></returns>
        private static bool atIntegerValue(double d)
        {
            return Math.Ceiling(d) == d;
        }

    }
}