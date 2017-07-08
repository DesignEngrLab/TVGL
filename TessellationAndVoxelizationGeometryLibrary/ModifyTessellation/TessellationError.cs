// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="TessellationError.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    ///     Stores errors in the tessellated solid
    /// </summary>
    public class TessellationError
    {
        /// <summary>
        ///     Edges that are used by more than two faces
        /// </summary>
        /// <value>The overused edges.</value>
        public List<Tuple<Edge, List<PolygonalFace>>> OverusedEdges { get; internal set; }

        /// <summary>
        ///     Edges that only have one face
        /// </summary>
        /// <value>The singled sided edges.</value>
        public List<Edge> SingledSidedEdges { get; internal set; }

        /// <summary>
        ///     Faces with errors
        /// </summary>
        /// <value>The degenerate faces.</value>
        public List<int[]> DegenerateFaces { get; internal set; }

        /// <summary>
        ///     Duplicate Faces
        /// </summary>
        /// <value>The duplicate faces.</value>
        public List<int[]> DuplicateFaces { get; internal set; }

        /// <summary>
        ///     Faces with only one vertex
        /// </summary>
        /// <value>The faces with one vertex.</value>
        public List<PolygonalFace> FacesWithOneVertex { get; internal set; }

        /// <summary>
        ///     Faces with only one edge
        /// </summary>
        /// <value>The faces with one edge.</value>
        public List<PolygonalFace> FacesWithOneEdge { get; internal set; }

        /// <summary>
        ///     Faces with only two vertices
        /// </summary>
        /// <value>The faces with two vertices.</value>
        public List<PolygonalFace> FacesWithTwoVertices { get; internal set; }

        /// <summary>
        ///     Faces with only two edges
        /// </summary>
        /// <value>The faces with two edges.</value>
        public List<PolygonalFace> FacesWithTwoEdges { get; internal set; }

        /// <summary>
        ///     Faces with negligible area (which is not necessarily an error)
        /// </summary>
        /// <value>The faces with negligible area.</value>
        public List<PolygonalFace> FacesWithNegligibleArea { get; internal set; }

        /// <summary>
        ///     Edges that do not link back to faces that link to them
        /// </summary>
        /// <value>The edges that do not link back to face.</value>
        public List<Tuple<PolygonalFace, Edge>> EdgesThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        ///     Edges that do not link back to vertices that link to them
        /// </summary>
        /// <value>The edges that do not link back to vertex.</value>
        public List<Tuple<Vertex, Edge>> EdgesThatDoNotLinkBackToVertex { get; internal set; }

        /// <summary>
        ///     Vertices that do not link back to faces that link to them
        /// </summary>
        /// <value>The verts that do not link back to face.</value>
        public List<Tuple<PolygonalFace, Vertex>> VertsThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        ///     Vertices that do not link back to edges that link to them
        /// </summary>
        /// <value>The verts that do not link back to edge.</value>
        public List<Tuple<Edge, Vertex>> VertsThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        ///     Faces that do not link back to edges that link to them
        /// </summary>
        /// <value>The faces that do not link back to edge.</value>
        public List<Tuple<Edge, PolygonalFace>> FacesThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        ///     Faces that do not link back to vertices that link to them
        /// </summary>
        /// <value>The faces that do not link back to vertex.</value>
        public List<Tuple<Vertex, PolygonalFace>> FacesThatDoNotLinkBackToVertex { get; internal set; }

        /// <summary>
        ///     Edges with bad angles
        /// </summary>
        /// <value>The edges with bad angle.</value>
        public List<Edge> EdgesWithBadAngle { get; internal set; }

        /// <summary>
        ///     Edges to face ratio
        /// </summary>
        /// <value>The edge face ratio.</value>
        public double EdgeFaceRatio { get; internal set; } = double.NaN;

        /// <summary>
        ///     Whether ts.Errors contains any errors that need to be resolved
        /// </summary>
        /// <value><c>true</c> if [no errors]; otherwise, <c>false</c>.</value>
        internal bool NoErrors { get; set; }
        /// <summary>
        /// Gets a value indicating whether [model is inside out].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [model is inside out]; otherwise, <c>false</c>.
        /// </value>
        public bool ModelIsInsideOut { get; internal set; }
    }
}