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
        /// The voxel side length for each voxel level. It's a square, so all sides are the same length.
        /// </summary>
        public double[] VoxelSideLengths { get; private set; }

        /// <summary>
        /// Gets the offset that moves the model s.t. the lowest elements are at 0,0,0.
        /// </summary>
        /// <value>
        /// The offset.
        /// </value>
        public double[] Offset => Bounds[0];
        #endregion

        #region Constructor (from another voxelized solid, or maybe from a file)

        public VoxelizedSolid(VoxelDiscretization voxelDiscretization, double[][] bounds, UnitType units = UnitType.unspecified, string name = "",
            string filename = "", List<string> comments = null, string language = "") : base(units, name, filename,
            comments, language)
        {
            Discretization = voxelDiscretization;
            Bounds = new double[2][];
            Bounds[0] = (double[])bounds[0].Clone();
            Bounds[1] = (double[])bounds[1].Clone();
            var dimensions = new double[3];
            for (int i = 0; i < 3; i++)
                dimensions[i] = Bounds[1][i] - Bounds[0][i];
            var longestSide = dimensions.Max();
            longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            longestSide = Bounds[1][longestDimensionIndex] - Bounds[0][longestDimensionIndex];
            VoxelSideLengths = new[] { longestSide / 16, longestSide / 256, longestSide / 4096, longestSide / 65536, longestSide / 1048576 };
            voxelDictionaryLevel0 = new Dictionary<long, Voxel_Level0_Class>(new VoxelComparerCoarse());
            voxelDictionaryLevel1 = new Dictionary<long, Voxel_Level1_Class>(new VoxelComparerCoarse());
            UpdateProperties();
        }
        #endregion

        #region Private Fields
        private readonly double[][] transformedCoordinates;
        private readonly double[] dimensions;
        private readonly int longestDimensionIndex;
        private readonly Dictionary<long, Voxel_Level0_Class> voxelDictionaryLevel0;
        private readonly Dictionary<long, Voxel_Level1_Class> voxelDictionaryLevel1;
        #endregion

        #region converting IDs and back again

        internal static long MakeVoxelID1(byte x, byte y, byte z)
        {
            var xLong = (long)x << 16;
            var yLong = (long)y << 36;
            var zLong = (long)z << 56;
            //   z0   z1    z2   z3    z4   y0   y1    y2   y3    y4    x0   x1    x2   x3    x4   flags
            // ||----|----||----|----||----|----||----|----||----|----||----|----||----|----||----|----|
            // 64   60    56    52   48    44   40    36   32    28   24    20   16    12    8    4
            return xLong + yLong + zLong;
        }

        internal static long MakeVoxelID0(byte x, byte y, byte z)
        {
            var xLong = (long)(x & 240) << 16; 
            var yLong = (long)(y & 240) << 36;
            var zLong = (long)(z & 240) << 56;
            return xLong + yLong + zLong;
        }

        internal static long SetRoleFlags(int level, VoxelRoleTypes role, bool btmIsInside = false)
        {
            var result = 0L;
            if (level == 1) result = 16;
            else if (level == 2) result = 4;
            else if (level == 3) result = 8;
            else if (level == 4) result = 12;
            if (role == VoxelRoleTypes.Partial)
            {
                if (btmIsInside) result += 2;
                else result += 1;
            }
            else if (role == VoxelRoleTypes.Full) result += 3;
            return result;
        }

        internal static void GetRoleFlags(long ID, out int level, out VoxelRoleTypes role, out bool btmIsInside)
        {
            level = 0;
            btmIsInside = false;
            role = VoxelRoleTypes.Empty;
            var flags = ID & Constants.maskAllButFlags;
            if (flags >= 16 && flags < 20) level = 1;
            flags = flags & 15;
            if (flags == 0) return;
            if (flags == 1) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 2)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true;
                return;
            }
            if (flags == 3) { role = VoxelRoleTypes.Full; return; }

            level = 2;
            if (flags == 4) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 5) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 6)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 7) { role = VoxelRoleTypes.Full; return; }

            level = 3;
            if (flags == 8) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 9) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 10)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 11) { role = VoxelRoleTypes.Full; return; }

            level = 4;
            if (flags == 12) { role = VoxelRoleTypes.Empty; return; }
            if (flags == 13) { role = VoxelRoleTypes.Partial; return; }
            if (flags == 14)
            {
                role = VoxelRoleTypes.Partial;
                btmIsInside = true; return;
            }
            if (flags == 15) role = VoxelRoleTypes.Full;
        }


        internal static long MakeCoordinateZero(long id, int dimension)
        {
            if (dimension == 0)
                return id & Constants.maskOutX;
            if (dimension == 1)
                return id & Constants.maskOutY;
            return id & Constants.maskOutZ;
        }

        internal static long ChangeCoordinate(long id, long newValue, int dimension, int level, int startDiscretizationLevel)
        {
            var shift = 4 + 4 * (4 - startDiscretizationLevel) - 4 * (startDiscretizationLevel - level);
            shift += 20 * dimension;
            newValue = newValue << shift;
            return newValue + MakeCoordinateZero(id, dimension);
        }

        internal static long MakeContainingVoxelID(long id, int level)
        {
            switch (level)
            {
                case 0: return id & Constants.maskAllButLevel0;
                case 1:
                    return id & Constants.maskAllButLevel0and1;
                case 2:
                    return id & Constants.maskAllButLevel01and2;
                case 3:
                    return id & Constants.maskLevel4;
            }
            throw new ArgumentOutOfRangeException("containing level must be 0, 1, 2, or 3");
        }

        private long MaskAllBut(long ID, int directionIndex)
        {
            switch (directionIndex)
            {
                case 0:
                    return ID & Constants.maskAllButX;
                case 1:
                    return ID & Constants.maskAllButY;
                default:
                    return ID & Constants.maskAllButZ;
            }
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