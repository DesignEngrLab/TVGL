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
using TVGL.Numerics;


namespace TVGL.TwoDimensional
{

#region Node Class

    #endregion

    #region Trapezoid Class

    /// <summary>
    ///     Trapezoid Class
    /// </summary>
    internal class Trapezoid
    {
        #region Constructor

        /// <summary>
        ///     Constructs a new trapezoid based on two nodes and two vertical lines.
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="bottomNode">The bottom node.</param>
        /// <param name="leftLine">The left line.</param>
        /// <param name="rightLine">The right line.</param>
        internal Trapezoid(Node topNode, Node bottomNode, Line leftLine, Line rightLine)
        {
            TopNode = topNode;
            BottomNode = bottomNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the TopNode. Set is through constructor.
        /// </summary>
        /// <value>The top node.</value>
        internal Node TopNode { get; private set; }

        /// <summary>
        ///     Gets the BottomNode. Set is through constructor.
        /// </summary>
        /// <value>The bottom node.</value>
        internal Node BottomNode { get; private set; }

        /// <summary>
        ///     Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The left line.</value>
        internal Line LeftLine { get; private set; }

        /// <summary>
        ///     Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The right line.</value>
        internal Line RightLine { get; private set; }

        #endregion
    }

#endregion
#region Partial Trapezoid Class
#endregion
#region MonotonePolygon class
#endregion
#region NodeLine Class

    #endregion
}