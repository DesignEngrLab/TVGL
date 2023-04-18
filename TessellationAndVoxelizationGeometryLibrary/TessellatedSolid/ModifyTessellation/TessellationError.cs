// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="TessellationError.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Stores errors in the tessellated solid
    /// </summary>
    public class TessellationError
    {
        /// <summary>
        /// Edges that are used by more than two faces
        /// </summary>
        /// <value>The overused edges.</value>
        public List<(Edge, List<TriangleFace>)> OverusedEdges { get; internal set; }

        /// <summary>
        /// Edges that only have one face
        /// </summary>
        /// <value>The singled sided edges.</value>
        public List<Edge> SingledSidedEdges { get; internal set; }

        /// <summary>
        /// Faces with errors
        /// </summary>
        /// <value>The degenerate faces.</value>
        public List<int[]> DegenerateFaces { get; internal set; }

        /// <summary>
        /// Duplicate Faces
        /// </summary>
        /// <value>The duplicate faces.</value>
        public List<int[]> DuplicateFaces { get; internal set; }

        /// <summary>
        /// Faces with only one vertex
        /// </summary>
        /// <value>The faces with one vertex.</value>
        public List<TriangleFace> FacesWithOneVertex { get; internal set; }

        /// <summary>
        /// Faces with only one edge
        /// </summary>
        /// <value>The faces with one edge.</value>
        public List<TriangleFace> FacesWithOneEdge { get; internal set; }

        /// <summary>
        /// Faces with only two vertices
        /// </summary>
        /// <value>The faces with two vertices.</value>
        public List<TriangleFace> FacesWithTwoVertices { get; internal set; }

        /// <summary>
        /// Faces with only two edges
        /// </summary>
        /// <value>The faces with two edges.</value>
        public List<TriangleFace> FacesWithTwoEdges { get; internal set; }

        /// <summary>
        /// Faces with negligible area (which is not necessarily an error)
        /// </summary>
        /// <value>The faces with negligible area.</value>
        public List<TriangleFace> FacesWithNegligibleArea { get; internal set; }

        /// <summary>
        /// Edges that do not link back to faces that link to them
        /// </summary>
        /// <value>The edges that do not link back to face.</value>
        public List<(TriangleFace, Edge)> EdgesThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        /// Edges that do not link back to vertices that link to them
        /// </summary>
        /// <value>The edges that do not link back to vertex.</value>
        public List<(Vertex, Edge)> EdgesThatDoNotLinkBackToVertex { get; internal set; }

        /// <summary>
        /// Vertices that do not link back to faces that link to them
        /// </summary>
        /// <value>The verts that do not link back to face.</value>
        public List<(TriangleFace, Vertex)> VertsThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        /// Vertices that do not link back to edges that link to them
        /// </summary>
        /// <value>The verts that do not link back to edge.</value>
        public List<(Edge, Vertex)> VertsThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        /// Faces that do not link back to edges that link to them
        /// </summary>
        /// <value>The faces that do not link back to edge.</value>
        public List<(Edge, TriangleFace)> FacesThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        /// Faces that do not link back to vertices that link to them
        /// </summary>
        /// <value>The faces that do not link back to vertex.</value>
        public List<(Vertex, TriangleFace)> FacesThatDoNotLinkBackToVertex { get; internal set; }

        /// <summary>
        /// Edges with bad angles
        /// </summary>
        /// <value>The edges with bad angle.</value>
        public List<Edge> EdgesWithBadAngle { get; internal set; }

        /// <summary>
        /// Edges to face ratio
        /// </summary>
        /// <value>The edge face ratio.</value>
        public double EdgeFaceRatio { get; internal set; } = double.NaN;

        /// <summary>
        /// Whether ts.Errors contains any errors that need to be resolved
        /// </summary>
        /// <value><c>true</c> if [no errors]; otherwise, <c>false</c>.</value>
        internal bool NoErrors { get; set; }

        /// <summary>
        /// Gets a value indicating whether [model is inside out].
        /// </summary>
        /// <value><c>true</c> if [model is inside out]; otherwise, <c>false</c>.</value>
        public bool ModelIsInsideOut { get; internal set; }

        /// <summary>
        /// Counts this instance.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public int Count()
        {
            int count = 0;
            if (DegenerateFaces != null) count += DegenerateFaces.Count;
            if (DuplicateFaces != null) count += DuplicateFaces.Count;
            if (EdgesThatDoNotLinkBackToFace != null) count += EdgesThatDoNotLinkBackToFace.Count;
            if (EdgesThatDoNotLinkBackToVertex != null) count += EdgesThatDoNotLinkBackToVertex.Count;
            if (EdgesWithBadAngle != null) count += EdgesWithBadAngle.Count;
            if (FacesThatDoNotLinkBackToEdge != null) count += FacesThatDoNotLinkBackToEdge.Count;
            if (FacesThatDoNotLinkBackToVertex != null) count += FacesThatDoNotLinkBackToVertex.Count;
            if (FacesWithNegligibleArea != null) count += FacesWithNegligibleArea.Count;
            if (FacesWithOneEdge != null) count += FacesWithOneEdge.Count;
            if (FacesWithOneVertex != null) count += FacesWithOneVertex.Count;
            if (FacesWithTwoEdges != null) count += FacesWithTwoEdges.Count;
            if (FacesWithTwoVertices != null) count += FacesWithTwoVertices.Count;
            if (OverusedEdges != null) count += OverusedEdges.Count;
            if (SingledSidedEdges != null) count += SingledSidedEdges.Count;
            if (VertsThatDoNotLinkBackToEdge != null) count += VertsThatDoNotLinkBackToEdge.Count;
            if (VertsThatDoNotLinkBackToFace != null) count += VertsThatDoNotLinkBackToFace.Count;
            return count;
        }

        /// <summary>
        /// Reports this instance.
        /// </summary>
        /// <returns>System.String.</returns>
        public string Report()
        {
            //if (NoErrors) return "No Errors";
            string report = "";
            if (DegenerateFaces != null) report += "Degenerate faces: " + DegenerateFaces.Count + "\n";
            if (DuplicateFaces != null) report += "Duplicate faces: " + DuplicateFaces.Count + "\n";
            if (EdgesThatDoNotLinkBackToFace != null) report += "Edges that do not link back to face: " + EdgesThatDoNotLinkBackToFace.Count + "\n";
            if (EdgesThatDoNotLinkBackToVertex != null) report += "Edges that do not link back to vertex: " + EdgesThatDoNotLinkBackToVertex.Count + "\n";
            if (EdgesWithBadAngle != null) report += "Edges with bad angles: " + EdgesWithBadAngle.Count + "\n";
            if (FacesThatDoNotLinkBackToEdge != null) report += "Faces that do not link back to edges: " + FacesThatDoNotLinkBackToEdge.Count + "\n";
            if (FacesThatDoNotLinkBackToVertex != null) report += "Faces that do not link back to vertices: " + FacesThatDoNotLinkBackToVertex.Count + "\n";
            if (FacesWithNegligibleArea != null) report += "Faces with negligible area: " + FacesWithNegligibleArea.Count + "\n";
            if (FacesWithOneEdge != null) report += "Faces with one edge: " + FacesWithOneEdge.Count + "\n";
            if (FacesWithOneVertex != null) report += "Faces with one vertex: " + FacesWithOneVertex.Count + "\n";
            if (FacesWithTwoEdges != null) report += "FacesWithTwoEdge: " + FacesWithTwoEdges.Count + "\n";
            if (FacesWithTwoVertices != null) report += "FacesWithTwoVertices: " + FacesWithTwoVertices.Count + "\n";
            if (ModelIsInsideOut) report += "Model is inside-out: " + ModelIsInsideOut + "\n";
            if (OverusedEdges != null) report += "Overused edges: " + OverusedEdges.Count + "\n";
            if (SingledSidedEdges != null) report += "Singled sided edges: " + SingledSidedEdges.Count + "\n";
            if (VertsThatDoNotLinkBackToEdge != null) report += "Vertices that do not link back to edges: " + VertsThatDoNotLinkBackToEdge.Count + "\n";
            if (VertsThatDoNotLinkBackToFace != null) report += "Vertices that do not link back to faces: " + VertsThatDoNotLinkBackToFace.Count;
            return report;
        }
    }
}