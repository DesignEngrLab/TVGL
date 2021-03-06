﻿// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;

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
        public List<(Edge, List<PolygonalFace>)> OverusedEdges { get; internal set; }

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
        public List<(PolygonalFace, Edge)> EdgesThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        ///     Edges that do not link back to vertices that link to them
        /// </summary>
        /// <value>The edges that do not link back to vertex.</value>
        public List<(Vertex, Edge)> EdgesThatDoNotLinkBackToVertex { get; internal set; }

        /// <summary>
        ///     Vertices that do not link back to faces that link to them
        /// </summary>
        /// <value>The verts that do not link back to face.</value>
        public List<(PolygonalFace, Vertex)> VertsThatDoNotLinkBackToFace { get; internal set; }

        /// <summary>
        ///     Vertices that do not link back to edges that link to them
        /// </summary>
        /// <value>The verts that do not link back to edge.</value>
        public List<(Edge, Vertex)> VertsThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        ///     Faces that do not link back to edges that link to them
        /// </summary>
        /// <value>The faces that do not link back to edge.</value>
        public List<(Edge, PolygonalFace)> FacesThatDoNotLinkBackToEdge { get; internal set; }

        /// <summary>
        ///     Faces that do not link back to vertices that link to them
        /// </summary>
        /// <value>The faces that do not link back to vertex.</value>
        public List<(Vertex, PolygonalFace)> FacesThatDoNotLinkBackToVertex { get; internal set; }

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

        public int Count()
        {
            if (NoErrors) return 0;
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

        public string Report()
        {
            if (NoErrors) return "No Errors";
            string report = "";
            if (DegenerateFaces != null) report += "DegenerateFaces: " + DegenerateFaces.Count + "\n";
            if (DuplicateFaces != null) report += "DuplicateFaces: " + DuplicateFaces.Count + "\n";
            if (EdgesThatDoNotLinkBackToFace != null) report += "EdgesThatDoNotLinkBackToFace: " + EdgesThatDoNotLinkBackToFace.Count + "\n";
            if (EdgesThatDoNotLinkBackToVertex != null) report += "EdgesThatDoNotLinkBackToVertex: " + EdgesThatDoNotLinkBackToVertex.Count + "\n";
            if (EdgesWithBadAngle != null) report += "EdgesWithBadAngle: " + EdgesWithBadAngle.Count + "\n";
            if (FacesThatDoNotLinkBackToEdge != null) report += "FacesThatDoNotLinkBackToEdge: " + FacesThatDoNotLinkBackToEdge.Count + "\n";
            if (FacesThatDoNotLinkBackToVertex != null) report += "FacesThatDoNotLinkBackToVertex: " + FacesThatDoNotLinkBackToVertex.Count + "\n";
            if (FacesWithNegligibleArea != null) report += "FacesWithNegligibleArea: " + FacesWithNegligibleArea.Count + "\n";
            if (FacesWithOneEdge != null) report += "FacesWithOneEdge: " + FacesWithOneEdge.Count + "\n";
            if (FacesWithOneVertex != null) report += "FacesWithOneVertex: " + FacesWithOneVertex.Count + "\n";
            if (FacesWithTwoEdges != null) report += "FacesWithTwoEdge: " + FacesWithTwoEdges.Count + "\n";
            if (FacesWithTwoVertices != null) report += "FacesWithTwoVertices: " + FacesWithTwoVertices.Count + "\n";
            if (ModelIsInsideOut) report += "ModelIsInsideOut: " + ModelIsInsideOut + "\n";
            if (OverusedEdges != null) report += "OverusedEdges: " + OverusedEdges.Count + "\n";
            if (SingledSidedEdges != null) report += "SingledSidedEdges: " + SingledSidedEdges.Count + "\n";
            if (VertsThatDoNotLinkBackToEdge != null) report += "VertsThatDoNotLinkBackToEdge: " + VertsThatDoNotLinkBackToEdge.Count + "\n";
            if (VertsThatDoNotLinkBackToFace != null) report += "VertsThatDoNotLinkBackToFace: " + VertsThatDoNotLinkBackToFace.Count;
            return report;
        }
    }
}