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
        internal int discretizationLevel;

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
            numVoxels = dimensions.Select(d => (int)Math.Ceiling(d / VoxelSideLengths[discretizationLevel])).ToArray();
            voxelDictionaryLevel0 = new VoxelHashSet(new VoxelComparerCoarse(), this);
            UpdateProperties();
        }
        #endregion

        #region Private Fields
        private readonly double[][] transformedCoordinates;
        private readonly double[] dimensions;
        private readonly int[] numVoxels;
        private readonly int longestDimensionIndex;
        private readonly VoxelHashSet voxelDictionaryLevel0;
        //private readonly VoxelHashSet voxelDictionaryLevel1;
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