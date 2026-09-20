// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="GeodesicDistanceEnums.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>Enums and options for geodesic distance calculation.</summary>
// ***********************************************************************

namespace TVGL
{
    /// <summary>
    /// Specifies the mode used to compute geodesic distances with the Heat Method.
    /// </summary>
    public enum GeodesicDistanceMode
    {
        /// <summary>
        /// Computes geodesic distances directly on the input 3D mesh geometry.
        /// Requires that the mesh does not contain degenerate faces.
        /// </summary>
        Direct,

        /// <summary>
        /// Constructs an intrinsic Delaunay triangulation (iDT) internally by performing
        /// intrinsic edge flips without modifying the 3D vertex positions.
        /// Provides numerical stability and works on meshes with poor aspect ratio or degenerate faces.
        /// This is the recommended default mode.
        /// </summary>
        IntrinsicDelaunay
    }
}

