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

#region Trapezoid Class

    #endregion

    #region Partial Trapezoid Class

    /// <summary>
    ///     Partial Trapezoid Class. Used to hold information to create Trapezoids.
    /// </summary>
    internal class PartialTrapezoid
    {
        /// <summary>
        ///     Constructs a partial trapezoid
        /// </summary>
        /// <param name="topNode">The top node.</param>
        /// <param name="leftLine">The left line.</param>
        /// <param name="rightLine">The right line.</param>
        internal PartialTrapezoid(Vertex2D topNode, PolygonSegment leftLine, PolygonSegment rightLine)
        {
            TopNode = topNode;
            LeftLine = leftLine;
            RightLine = rightLine;
        }

        /// <summary>
        ///     Gets the TopNode. Set is through constructor.
        /// </summary>
        /// <value>The top node.</value>
        internal Vertex2D TopNode { get; private set; }

        /// <summary>
        ///     Gets the left vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The left line.</value>
        internal PolygonSegment LeftLine { get; }

        /// <summary>
        ///     Gets the right vertical line of the trapezoid. Set is through constructor.
        /// </summary>
        /// <value>The right line.</value>
        internal PolygonSegment RightLine { get; }

        /// <summary>
        ///     Checks whether the partial trapezoid contains the two lines.
        /// </summary>
        /// <param name="line1">The line1.</param>
        /// <param name="line2">The line2.</param>
        /// <returns><c>true</c> if [contains] [the specified line1]; otherwise, <c>false</c>.</returns>
        internal bool Contains(PolygonSegment line1, PolygonSegment line2)
        {
            if (LeftLine != line1 && LeftLine != line2) return false;
            return RightLine == line1 || RightLine == line2;
        }
    }

#endregion
#region MonotonePolygon class

#endregion
#region NodeLine Class

    #endregion
}