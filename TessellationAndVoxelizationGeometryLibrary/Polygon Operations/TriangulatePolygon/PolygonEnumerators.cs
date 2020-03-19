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
    /// <summary>
    ///     Enum NodeType
    /// </summary>
    internal enum NodeType
    {
        /// <summary>
        ///     The downward reflex
        /// </summary>
        DownwardReflex,
        UpwardReflex,
        Peak,
        Root,
        Left,
        Right,

        /// <summary>
        ///     The duplicate
        /// </summary>
        Duplicate
    }


    internal enum PolygonType
    {
        Subject,
        Clip
    };

}