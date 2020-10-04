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

    internal enum BooleanOperationType
    {
        Union,
        Intersection
    };

    public enum PolygonFillType //http://www.angusj.com/delphi/clipper/documentation/Docs/Units/ClipperLib/Types/PolyFillType.htm
    {
        Positive, // (Most common if polygons are ordered correctly and not self-intersecting) All sub-regions with winding counts > 0 are filled.
        EvenOdd,  // (Most common when polygon directions are unknown) Odd numbered sub-regions are filled, while even numbered sub-regions are not.
        Negative, // (Rarely used) All sub-regions with winding counts < 0 are filled.
        NonZero //(Common if polygon directions are unknown) All non-zero sub-regions are filled (used in silhouette because we prefer filled regions).
    };
}