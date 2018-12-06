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
        public readonly int LevelAtWhichLinkToTessellation;
        public readonly int[][] voxelsPerDimension;
        internal int numberOfLevels;
        internal int lastLevel => numberOfLevels - 1;
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


        /// <summary>
        /// Gets the number of levels.
        /// </summary>
        /// <value>The number of levels.</value>
        public int NumberOfLevels => bitLevelDistribution.Length;
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
        public VoxelizedSolid(int bitsPerVoxelCoordinate, double[][] bounds, UnitType units = UnitType.unspecified, string name = "",
            string filename = "", List<string> comments = null, string language = "") : base(units, name, filename,
            comments, language)
        {
            Discretization = bitsPerVoxelCoordinate;
            bitLevelDistribution = Constants.DefaultBitLevelDistribution[Discretization];
            voxelsPerSide = bitLevelDistribution.Select(b => (int)Math.Pow(2, b)).ToArray();
            voxelsInParent = voxelsPerSide.Select(s => s * s * s).ToArray();
            defineMaskAndShifts(bitLevelDistribution);
            numberOfLevels = bitLevelDistribution.Length;

            double longestSide;
            Bounds = new double[2][];
            Bounds[0] = (double[])bounds[0].Clone();
            Bounds[1] = (double[])bounds[1].Clone();
            dimensions = new double[3];
            for (int i = 0; i < 3; i++)
                dimensions[i] = Bounds[1][i] - Bounds[0][i];
            longestSide = dimensions.Max();
            longestDimensionIndex = dimensions.FindIndex(d => d == longestSide);
            longestSide = dimensions[longestDimensionIndex];
            VoxelSideLengths = new double[numberOfLevels];
            VoxelSideLengths[0] = longestSide / voxelsPerSide[0];
            for (int i = 1; i < numberOfLevels; i++)
                VoxelSideLengths[i] = VoxelSideLengths[i - 1] / voxelsPerSide[i];
            voxelDictionaryLevel0 = new VoxelBinSet(dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[0])).ToArray(), bitLevelDistribution[0]);
            voxelsPerDimension = new int[NumberOfLevels][];
            for (var i = 0; i < numberOfLevels; i++)
            {
                var voxelNum = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[i])).ToArray();
                voxelsPerDimension[i] = voxelNum;
            }
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
            voxelDictionaryLevel0 = new VoxelBinSet(dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[0])).ToArray(), bitLevelDistribution[0]);
            voxelsPerDimension = new int[NumberOfLevels][];
            for (var i = 0; i < numberOfLevels; i++)
                voxelsPerDimension[i] = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[i])).ToArray();
            byte[] bytes = Convert.FromBase64String(fileData.Voxels[0]);
            for (int i = 0; i < bytes.Length; i += 8)
            {
                var ID = BitConverter.ToInt64(bytes, i);
                Constants.GetAllFlags(ID, out var level, out VoxelRoleTypes role, out bool btmInside);
                voxelDictionaryLevel0.AddOrReplace(new VoxelBinClass(ID, role, this, btmInside));
            }

            for (int i = 1; i < bitLevelDistribution.Length; i++)
            {
                bytes = Convert.FromBase64String(fileData.Voxels[i]);
                for (int j = 0; j < bytes.Length; j += 8)
                {
                    var ID = BitConverter.ToInt64(bytes, j);
                    ((VoxelBinClass)voxelDictionaryLevel0.GetVoxel(ID)).InnerVoxels[i - 1]
                        .AddOrReplace(ID);
                }
            }

            UpdateProperties();

            if (fileData.Primitives != null && fileData.Primitives.Any())
                Primitives = fileData.Primitives;
        }
        #endregion

        #region Private Fields
        private readonly double[] dimensions;
        private readonly int longestDimensionIndex;
        private readonly VoxelBinSet voxelDictionaryLevel0;
        #endregion


        /// <summary>
        /// Makes the parent voxel identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="discretization">The discretization.</param>
        /// <param name="level">The level of the parent.</param>
        /// <returns>System.Int64.</returns>
        /// <exception cref="ArgumentOutOfRangeException">containing level must be 0, 1, 2, or 3</exception>
        public long MakeParentVoxelID(long id, int parentLevel)
        {
            long singleCoordMask = singleCoordinateMasks[parentLevel];
            var mask = (singleCoordMask << 4) + (singleCoordMask << 24) + (singleCoordMask << 44);
            return id & mask;
        }

    }
}