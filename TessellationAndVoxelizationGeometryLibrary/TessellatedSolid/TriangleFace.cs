﻿// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-10-2023
//
// Last Modified By : matth
// Last Modified On : 04-16-2023
// ***********************************************************************
// <copyright file="TriangleFace.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// This class defines a flat triangle face. The implementation began with triangular faces in mind.
    /// It should be double-checked for higher polygons.   It inherits from the ConvexFace class in
    /// MIConvexHull
    /// </summary>
    public class TriangleFace : TessellationBaseClass
    {
        /// <summary>
        /// Copies this instance. Does not include reference lists.
        /// </summary>
        /// <returns>TriangleFace.</returns>
        public TriangleFace Copy()
        {
            return new TriangleFace
            {
                _area = _area,
                _center = _center,
                _curvature = _curvature,
                Color = Color,
                PartOfConvexHull = PartOfConvexHull,
                _normal = _normal,
            };
        }

        /// <summary>
        /// Inverts this instance.
        /// </summary>
        internal void Invert()
        {
            _normal *= -1;
            //var firstVertex = face.Vertices[0];
            //face.Vertices.RemoveAt(0);
            //face.Vertices.Insert(1, firstVertex);
            Vertices.Reverse();
            Edges.Reverse();
            _curvature = (CurvatureType)(-1 * (int)_curvature);
        }

        //Set new normal and area.
        //References are assumed to be the same.
        /// <summary>
        /// Updates normal, vertex order, and area
        /// </summary>
        public void Update()
        {
            _center = Vector3.Null;
            _normal = Vector3.Null;
            _area = double.NaN;
        }

        /// <summary>
        /// Transforms this face's normal and center. Vertices and edges are transformed seperately
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        internal void Transform(Matrix4x4 transformMatrix)
        {
            _center = Center.Transform(transformMatrix);
            _normal = Normal.Transform(transformMatrix);
            //Area remains unchanged
        }

        /// <summary>
        /// Adds the edge.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <exception cref="System.Exception">Edge does not belong to this face.</exception>
        internal void AddEdge(Edge edge)
        {
            if ((A == edge.From && B == edge.To) || (A == edge.To && B == edge.From)) AB = edge;
            else if ((B == edge.From && C == edge.To)|| (B == edge.To && C == edge.From)) BC = edge;
            else if ((C == edge.From && A == edge.To)|| (C == edge.To && A == edge.From)) CA = edge;
            else throw new Exception("Edge does not belong to this face.");
        }

        /// <summary>
        /// Others the edge.
        /// </summary>
        /// <param name="thisVertex">The this vertex.</param>
        /// <param name="willAcceptNullAnswer">if set to <c>true</c> [will accept null answer].</param>
        /// <returns>Edge.</returns>
        public Edge OtherEdge(Vertex thisVertex, bool willAcceptNullAnswer = false)
        {
            if (willAcceptNullAnswer)
                return Edges.FirstOrDefault(e => e != null && e.To != thisVertex && e.From != thisVertex);
            return Edges.First(e => e != null && e.To != thisVertex && e.From != thisVertex);
        }

        /// <summary>
        /// Others the vertex.
        /// </summary>
        /// <param name="thisEdge">The this edge.</param>
        /// <param name="willAcceptNullAnswer">if set to <c>true</c> [will accept null answer].</param>
        /// <returns>Vertex.</returns>
        public Vertex OtherVertex(Edge thisEdge, bool willAcceptNullAnswer = false)
        {
            return willAcceptNullAnswer
                ? Vertices.FirstOrDefault(v => v != null && v != thisEdge.To &&
                                               v != thisEdge.From)
                : Vertices.First(v => v != null && v != thisEdge.To && v != thisEdge.From);
        }

        /// <summary>
        /// Others the vertex.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="willAcceptNullAnswer">if set to <c>true</c> [will accept null answer].</param>
        /// <returns>Vertex.</returns>
        public Vertex OtherVertex(Vertex v1, Vertex v2, bool willAcceptNullAnswer = false)
        {
            return willAcceptNullAnswer
                ? Vertices.FirstOrDefault(v => v != null && v != v1 && v != v2)
                : Vertices.First(v => v != null && v != v1 && v != v2);
        }

        /// <summary>
        /// Nexts the vertex CCW.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="System.ArgumentException">The given vertex is not part of this face.</exception>
        public Vertex NextVertexCCW(Vertex v1)
        {
            if (v1 == A) return B;
            if (v1 == B) return C;
            if (v1 == C) return A;
            throw new ArgumentException("The given vertex is not part of this face.");
        }

        /// <summary>
        /// Adopts the neighbors normal.
        /// </summary>
        /// <param name="ownedFace">The owned face.</param>
        internal void AdoptNeighborsNormal(TriangleFace ownedFace)
        {
            _normal = ownedFace.Normal;
        }

        /// <summary>
        /// This is a T-Edge. Set the face normal to be that of the two smaller edges other face.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        internal bool AdoptNeighborsNormal()
        {
            var edges = Edges.OrderBy(p => p.Length).ToList();
            var edge1 = edges[0];
            var edge2 = edges[1];
            var face1Normal = (this == edge1.OwnedFace ? edge1.OtherFace : edge1.OwnedFace).Normal;
            var face2Normal = (this == edge2.OwnedFace ? edge2.OtherFace : edge2.OwnedFace).Normal;
            var dot = face1Normal.Dot(face2Normal);
            if (dot.IsPracticallySame(1.0, Constants.SameFaceNormalDotTolerance))
            {
                _normal = (face1Normal + face2Normal).Normalize();
                return true;
            }
            return false;
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleFace" /> class.
        /// </summary>
        private TriangleFace() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public TriangleFace(IEnumerable<Vertex> vertices, bool connectVerticesBackToFace = true) : this()
        {
            var enumerator = vertices.GetEnumerator();
            if (enumerator.MoveNext())
            {
                A = enumerator.Current;
                if (connectVerticesBackToFace)
                    A.Faces.Add(this);
                if (enumerator.MoveNext())
                {
                    B = enumerator.Current;
                    if (connectVerticesBackToFace)
                        B.Faces.Add(this);
                    if (enumerator.MoveNext())
                    {
                        C = enumerator.Current;
                        if (connectVerticesBackToFace)
                            C.Faces.Add(this);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleFace" /> class.
        /// </summary>
        /// <param name="A">a.</param>
        /// <param name="B">The b.</param>
        /// <param name="C">The c.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public TriangleFace(Vertex A, Vertex B, Vertex C, bool connectVerticesBackToFace = true) : this()
        {
            this.A = A;
            this.B = B;
            this.C = C;
            if (connectVerticesBackToFace)
            {
                A.Faces.Add(this);
                B.Faces.Add(this);
                C.Faces.Add(this);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="suggestedNormal">A guess for the normal vector.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public TriangleFace(IEnumerable<Vertex> vertices, Vector3 suggestedNormal, bool connectVerticesBackToFace = true)
            : this(vertices, connectVerticesBackToFace)
        {
            _normal = MiscFunctions.DetermineNormalForA3DPolygon(Vertices, 3, out var reverseVertexOrder, suggestedNormal, out _);
            if (reverseVertexOrder) Vertices.Reverse();
        }

        /// <summary>
        /// Gets the normal.
        /// </summary>
        /// <value>The normal.</value>
        public override Vector3 Normal
        {
            get
            {
                if (_normal.IsNull())
                    _normal = ((B.Coordinates - A.Coordinates)
                        .Cross(C.Coordinates - A.Coordinates))
                        .Normalize();
                return _normal;
            }
        }

        /// <summary>
        /// The normal
        /// </summary>
        private Vector3 _normal = Vector3.Null;

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                yield return A;
                yield return B;
                yield return C;
            }
        }
        /// <summary>
        /// Gets the first vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex A;

        /// <summary>
        /// Gets the second vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex B;

        /// <summary>
        /// Gets the third vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex C;

        /// <summary>
        /// The ab
        /// </summary>
        public Edge AB;
        /// <summary>
        /// The bc
        /// </summary>
        public Edge BC;
        /// <summary>
        /// The ca
        /// </summary>
        public Edge CA;
        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public IEnumerable<Edge> Edges
        {
            get
            {
                yield return AB;
                yield return BC;
                yield return CA;
            }
        }

        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector3 Center
        {
            get
            {
                if (_center.IsNull())
                {
                    _center = Vector3.Zero;
                    foreach (var v in Vertices)
                        _center += v.Coordinates;
                    _center /= 3;
                }
                return _center;
            }
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector3 _center = Vector3.Null;

        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>The area.</value>
        public double Area
        {
            get
            {
                if (double.IsNaN(_area))
                    _area = DetermineArea();
                return _area;
            }
        }

        /// <summary>
        /// The area
        /// </summary>
        private double _area = double.NaN;

        /// <summary>
        /// Determines the area.
        /// </summary>
        /// <returns>System.Double.</returns>
        internal double DetermineArea()
        {
            var area = 0.5 * ((B.Coordinates - A.Coordinates)
                        .Cross(C.Coordinates - A.Coordinates)).Length();
            //If not a number, the triangle is actually a straight line. Set the area = 0, and let repair function fix this.
            return double.IsNaN(area) ? 0.0 : area;
        }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color Color { get; set; }

        /// <summary>
        /// Gets or sets the belongs to primitive.
        /// </summary>
        /// <value>The belongs to primitive.</value>
        public PrimitiveSurface BelongsToPrimitive { get; set; }

        /// <summary>
        /// Gets the adjacent faces.
        /// </summary>
        /// <value>The adjacent faces.</value>
        public List<TriangleFace> AdjacentFaces
        {
            get
            {
                var adjacentFaces = new List<TriangleFace>();
                foreach (var e in Edges)
                {
                    if (e == null) adjacentFaces.Add(null);
                    else adjacentFaces.Add(this == e.OwnedFace ? e.OtherFace : e.OwnedFace);
                }
                return adjacentFaces;
            }
        }

        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        public override CurvatureType Curvature
        {
            get
            {
                if (_curvature == CurvatureType.Undefined) DefineFaceCurvature();
                return _curvature;
            }
        }

        /// <summary>
        /// The curvature
        /// </summary>
        private CurvatureType _curvature = CurvatureType.Undefined;

        /// <summary>
        /// Defines the face curvature. Depends on DefineEdgeAngle
        /// </summary>
        private void DefineFaceCurvature()
        {
            if (Edges.Any(e => e == null || e.Curvature == CurvatureType.Undefined))
                _curvature = CurvatureType.Undefined;
            else if (Edges.All(e => e.Curvature != CurvatureType.Concave))
                _curvature = CurvatureType.Convex;
            else if (Edges.All(e => e.Curvature != CurvatureType.Convex))
                _curvature = CurvatureType.Concave;
            else _curvature = CurvatureType.SaddleOrFlat;
        }

        /// <summary>
        /// Replaces the vertex.
        /// </summary>
        /// <param name="oldVertex">The old vertex.</param>
        /// <param name="newVertex">The new vector.</param>
        /// <exception cref="System.Exception">Vertex not found in face</exception>
        internal void ReplaceVertex(Vertex oldVertex, Vertex newVertex)
        {
            if (oldVertex == A) A = newVertex;
            else if (oldVertex == B) B = newVertex;
            else if (oldVertex == C) C = newVertex;
            else throw new Exception("Vertex not found in face");
            oldVertex.Faces.Remove(this);
            newVertex.Faces.Add(this);
            Update();
        }

        /// <summary>
        /// Replaces the edge.
        /// </summary>
        /// <param name="oldEdge">The old edge.</param>
        /// <param name="newEdge">The new edge.</param>
        /// <exception cref="System.Exception">Vertex not found in face</exception>
        internal void ReplaceEdge(Edge oldEdge, Edge newEdge)
        {
            if (oldEdge == AB) AB = newEdge;
            else if (oldEdge == BC) BC = newEdge;
            else if (oldEdge == CA) CA = newEdge;
            else throw new Exception("Vertex not found in face");
            if (oldEdge.OwnedFace == this) oldEdge.OwnedFace = null;
            if (oldEdge.OtherFace == this) oldEdge.OtherFace = null;
            Update();
        }

        #endregion Properties
    }
}