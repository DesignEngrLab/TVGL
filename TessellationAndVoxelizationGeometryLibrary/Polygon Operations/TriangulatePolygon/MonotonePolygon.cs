// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="SpecialClasses.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System.Collections.Generic;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Monotone Polygon, which consists of two ordered chains
    ///     The chains start and end at the same nodes
    /// </summary>
    internal struct MonotonePolygon
    {
        #region Constructor
        /// <summary>
        ///     Constructs a MonotonePolygon based on a list of nodes.
        /// </summary>
        /// <param name="leftChain">The left chain.</param>
        /// <param name="rightChain">The right chain.</param>
        /// <param name="sortedNodes">The sorted nodes.</param>
        internal MonotonePolygon(List<Node> leftChain, List<Node> rightChain, List<Node> sortedNodes)
        {
            LeftChain = leftChain;
            RightChain = rightChain;
            SortedNodes = sortedNodes;
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets Monochain1. Set is through the constructor.
        /// </summary>
        /// <value>The left chain.</value>
        internal List<Node> LeftChain { get; }

        /// <summary>
        ///     Gets Monochain2. Set is through the constructor.
        /// </summary>
        /// <value>The right chain.</value>
        internal List<Node> RightChain { get; }

        /// <summary>
        ///     Gets Monochain2. Set is through the constructor.
        /// </summary>
        /// <value>The sorted nodes.</value>
        internal List<Node> SortedNodes { get; }

        #endregion
    }
}