// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellatedSolid.StaticFunctions.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Class TessellatedSolid.
    /// </summary>
    /// <tags>help</tags>
    /// <remarks>This partial class file is focused on static functions that relate to Tessellated Solid.</remarks>
    public partial class TessellatedSolid : Solid
    {
        /// <summary>
        /// Determines whether [contains duplicate indices] [the specified ordered indices].
        /// </summary>
        /// <param name="orderedIndices">The ordered indices.</param>
        /// <returns><c>true</c> if [contains duplicate indices] [the specified ordered indices]; otherwise, <c>false</c>.</returns>
        private static bool ContainsDuplicateIndices(List<int> orderedIndices)
        {
            for (var i = 0; i < orderedIndices.Count - 1; i++)
                if (orderedIndices[i] == orderedIndices[i + 1]) return true;
            return false;
        }

        /// <summary>
        /// Removes the references to vertex.
        /// </summary>
        /// <param name="vertex">The vertex.</param>
        private static void RemoveReferencesToVertex(Vertex vertex)
        {
            foreach (var face in vertex.Faces)
            {
                face.ReplaceVertex(vertex, null);
            }
            foreach (var edge in vertex.Edges)
            {
                if (vertex == edge.To) edge.To = null;
                if (vertex == edge.From) edge.From = null;
            }
        }
    }
}