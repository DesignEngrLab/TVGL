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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TVGL.IOFunctions;

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
        public readonly int Discretization;
        internal int numberOfLevels;
        internal int[] bitLevelDistribution;
        private int[] voxelsPerSide;
        private int[] voxelsInParent;
        internal int[] singleCoordinateShifts;
        private long[] singleCoordinateMasks;


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

        /// <summary>
        /// Initializes a new instance of the <see cref="VoxelizedSolid"/> class.
        /// </summary>
        /// <param name="voxelDiscretization">The voxel discretization.</param>
        /// <param name="bounds">The bounds.</param>
        /// <param name="units">The units.</param>
        /// <param name="name">The name.</param>
        /// <param name="filename">The filename.</param>
        /// <param name="comments">The comments.</param>
        /// <param name="language">The language.</param>
        public VoxelizedSolid(int voxelDiscretization, double[][] bounds, UnitType units = UnitType.unspecified, string name = "",
            string filename = "", List<string> comments = null, string language = "") : base(units, name, filename,
            comments, language)
        {
            Discretization = voxelDiscretization;
            bitLevelDistribution = Constants.DefaultBitLevelDistribution[Discretization];
            voxelsPerSide = bitLevelDistribution.Select(b => (int)Math.Pow(2, b)).ToArray();
            voxelsInParent = voxelsPerSide.Select(s => s * s * s).ToArray();
            defineMaskAndShifts(bitLevelDistribution);
            numberOfLevels = bitLevelDistribution.Length;
            Bounds = new double[2][];
            Bounds[0] = (double[])bounds[0].Clone();
            Bounds[1] = (double[])bounds[1].Clone();
            dimensions = new double[3];
            for (int i = 0; i < 3; i++)
                dimensions[i] = Bounds[1][i] - Bounds[0][i];
            var longestSide = dimensions.Max();
            longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            longestSide = Bounds[1][longestDimensionIndex] - Bounds[0][longestDimensionIndex];
            VoxelSideLengths = new double[numberOfLevels];
            VoxelSideLengths[0] = longestSide / voxelsPerSide[0];
            for (int i = 1; i < numberOfLevels; i++)
                VoxelSideLengths[i] = VoxelSideLengths[i - 1] / voxelsPerSide[i];
            voxelDictionaryLevel0 = new VoxelHashSet(0, this);
            UpdateProperties();
        }

        private void defineMaskAndShifts(int[] bits)
        {
            var n = bitLevelDistribution.Length;
            singleCoordinateMasks = new long[n];
            singleCoordinateShifts = new int[n];

            var shift = 20;
            var mask = 0L;
            for (int i = 0; i < n; i++)
            {
                shift -= bits[i];
                singleCoordinateShifts[i] = shift;
                mask += (long)(voxelsPerSide[i] - 1) << shift;
                singleCoordinateMasks[i] = mask;
            }
        }

        internal VoxelizedSolid(TVGLFileData fileData, string fileName) : base(fileData, fileName)
        {
            bitLevelDistribution = fileData.BitLevelDistribution;
            Discretization = bitLevelDistribution.Sum();
            voxelsPerSide = bitLevelDistribution.Select(b => (int)Math.Pow(2, b)).ToArray();
            voxelsInParent = voxelsPerSide.Select(s => s * s * s).ToArray();
            defineMaskAndShifts(bitLevelDistribution);
            numberOfLevels = bitLevelDistribution.Length;
            // the next 10 lines are common to the constructor above. They cannot be combines since these
            // flow down to different sub-constructors
            dimensions = new double[3];
            for (int i = 0; i < 3; i++)
                dimensions[i] = Bounds[1][i] - Bounds[0][i];
            var longestSide = dimensions.Max();
            longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            longestSide = Bounds[1][longestDimensionIndex] - Bounds[0][longestDimensionIndex];
            VoxelSideLengths = new double[numberOfLevels];
            VoxelSideLengths[0] = longestSide / voxelsPerSide[0];
            for (int i = 1; i < numberOfLevels; i++)
                VoxelSideLengths[i] = VoxelSideLengths[i - 1] / voxelsPerSide[i];
            voxelDictionaryLevel0 = new VoxelHashSet(0, this);

            byte[] bytes = Convert.FromBase64String(fileData.Voxels[0]);
            for (int i = 0; i < bytes.Length; i += 8)
            {
                var ID = BitConverter.ToInt64(bytes, i);
                Constants.GetRoleFlags(ID, out var level, out VoxelRoleTypes role, out bool btmInside);
                voxelDictionaryLevel0.AddOrReplace(new Voxel_Level0_Class(ID, role, this, btmInside));
            }

            for (int i = 1; i < bitLevelDistribution.Length; i++)
            {
                bytes = Convert.FromBase64String(fileData.Voxels[i]);
                for (int j = 0; j < bytes.Length; j += 8)
                {
                    var ID = BitConverter.ToInt64(bytes, j);
                    ((Voxel_Level0_Class) voxelDictionaryLevel0.GetVoxel(ID)).InnerVoxels[i - 1]
                        .AddOrReplace(new Voxel(ID, this));
                }
            }

            UpdateProperties();

                if (fileData.Primitives != null && fileData.Primitives.Any())
                    Primitives = fileData.Primitives;
            }
        #endregion

            #region Private Fields
        private readonly double[][] transformedCoordinates;
        private readonly double[] dimensions;
        private readonly int longestDimensionIndex;
        private readonly VoxelHashSet voxelDictionaryLevel0;
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