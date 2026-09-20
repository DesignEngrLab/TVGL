// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// ***********************************************************************
// <copyright file="GeodesicDistanceExtensions.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary>Extension methods for computing geodesic distances on TessellatedSolid.</summary>
// ***********************************************************************

using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Provides extension methods on <see cref="TessellatedSolid"/> for geodesic distance computation.
    /// </summary>
    public static class GeodesicDistanceExtensions
    {
        /// <summary>
        /// Estimates geodesic distances from a single source vertex to all vertices in the solid mesh.
        /// </summary>
        /// <param name="solid">The triangulated solid mesh.</param>
        /// <param name="source">The source vertex.</param>
        /// <param name="mode">The computation mode (defaults to IntrinsicDelaunay).</param>
        /// <returns>An array of geodesic distances corresponding to each vertex in the mesh.</returns>
        public static double[] EstimateGeodesicDistances(this TessellatedSolid solid, Vertex source,
            GeodesicDistanceMode mode = GeodesicDistanceMode.IntrinsicDelaunay)
        {
            var hm = new HeatMethod(solid, mode);
            hm.AddSource(source);
            return hm.EstimateGeodesicDistances();
        }

        /// <summary>
        /// Estimates geodesic distances from a single source vertex index to all vertices in the solid mesh.
        /// </summary>
        /// <param name="solid">The triangulated solid mesh.</param>
        /// <param name="sourceIndex">The source vertex index.</param>
        /// <param name="mode">The computation mode (defaults to IntrinsicDelaunay).</param>
        /// <returns>An array of geodesic distances corresponding to each vertex in the mesh.</returns>
        public static double[] EstimateGeodesicDistances(this TessellatedSolid solid, int sourceIndex,
            GeodesicDistanceMode mode = GeodesicDistanceMode.IntrinsicDelaunay)
        {
            var hm = new HeatMethod(solid, mode);
            hm.AddSource(sourceIndex);
            return hm.EstimateGeodesicDistances();
        }

        /// <summary>
        /// Estimates geodesic distances from multiple source vertices to all vertices in the solid mesh.
        /// </summary>
        /// <param name="solid">The triangulated solid mesh.</param>
        /// <param name="sources">The collection of source vertices.</param>
        /// <param name="mode">The computation mode (defaults to IntrinsicDelaunay).</param>
        /// <returns>An array of geodesic distances corresponding to each vertex in the mesh.</returns>
        public static double[] EstimateGeodesicDistances(this TessellatedSolid solid, IEnumerable<Vertex> sources,
            GeodesicDistanceMode mode = GeodesicDistanceMode.IntrinsicDelaunay)
        {
            var hm = new HeatMethod(solid, mode);
            hm.AddSources(sources);
            return hm.EstimateGeodesicDistances();
        }

        /// <summary>
        /// Estimates geodesic distances from multiple source vertex indices to all vertices in the solid mesh.
        /// </summary>
        /// <param name="solid">The triangulated solid mesh.</param>
        /// <param name="sourceIndices">The collection of source vertex indices.</param>
        /// <param name="mode">The computation mode (defaults to IntrinsicDelaunay).</param>
        /// <returns>An array of geodesic distances corresponding to each vertex in the mesh.</returns>
        public static double[] EstimateGeodesicDistances(this TessellatedSolid solid, IEnumerable<int> sourceIndices,
            GeodesicDistanceMode mode = GeodesicDistanceMode.IntrinsicDelaunay)
        {
            var hm = new HeatMethod(solid, mode);
            hm.AddSources(sourceIndices);
            return hm.EstimateGeodesicDistances();
        }
    }
}

