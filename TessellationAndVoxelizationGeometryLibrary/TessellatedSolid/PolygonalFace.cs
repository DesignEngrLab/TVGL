// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    ///     This class defines a flat polygonal face. The implementation began with triangular faces in mind.
    ///     It should be double-checked for higher polygons.   It inherits from the ConvexFace class in
    ///     MIConvexHull
    /// </summary>
    public class PolygonalFace : TessellationBaseClass
    {
        /// <summary>
        ///     Copies this instance. Does not include reference lists.
        /// </summary>
        /// <returns>PolygonalFace.</returns>
        public PolygonalFace Copy()
        {
            return new PolygonalFace
            {
                _area = _area,
                _center = _center,
                _curvature = _curvature,
                Color = Color,
                PartOfConvexHull = PartOfConvexHull,
                Edges = new List<Edge>(),
                _normal = _normal,
                Vertices = new List<Vertex>()
            };
        }

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
        ///     Updates normal, vertex order, and area
        /// </summary>
        public void Update()
        {
            _center = Vector3.Null;
            _normal = Vector3.Null;
            _area = double.NaN;
        }

        /// <summary>
        ///     Transforms this face's normal and center. Vertices and edges are transformed seperately
        /// </summary>
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
        internal void AddEdge(Edge edge)
        {
            var vertFromIndex = Vertices.IndexOf(edge.From);
            var vertToIndex = Vertices.IndexOf(edge.To);
            int index;
            var lastIndex = Vertices.Count - 1;
            if ((vertFromIndex == 0 && vertToIndex == lastIndex)
                || (vertFromIndex == lastIndex && vertToIndex == 0))
                index = lastIndex;
            else index = Math.Min(vertFromIndex, vertToIndex);
            while (Edges.Count <= index) Edges.Add(null);
            if (index < 0) return;
            Edges[index] = edge;
        }

        /// <summary>
        ///     Others the edge.
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
        ///     Others the vertex.
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
        ///     Others the vertex.
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
        ///     Nexts the vertex CCW.
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <returns>Vertex.</returns>
        public Vertex NextVertexCCW(Vertex v1)
        {
            var index = Vertices.IndexOf(v1);
            if (index < 0) return null; //Vertex is not part of this face
            return index == Vertices.Count - 1 ? Vertices[0] : Vertices[index + 1];
        }

        internal void AdoptNeighborsNormal(PolygonalFace ownedFace)
        {
            _normal = ownedFace.Normal;
        }

        /// <summary>
        /// This is a T-Edge. Set the face normal to be that of the two smaller edges other face.
        /// </summary>
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
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        private PolygonalFace()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(IEnumerable<Vertex> vertices, bool connectVerticesBackToFace = true) : this()
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                if (connectVerticesBackToFace)
                    v.Faces.Add(this);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(Vertex A, Vertex B, Vertex C, bool connectVerticesBackToFace = true) : this()
        {
            Vertices.Add(A);
            Vertices.Add(B);
            Vertices.Add(C);
            if (connectVerticesBackToFace)
            {
                A.Faces.Add(this);
                B.Faces.Add(this);
                C.Faces.Add(this);
            }          
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="suggestedNormal">A guess for the normal vector.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(IEnumerable<Vertex> vertices, Vector3 suggestedNormal, bool connectVerticesBackToFace = true)
            : this(vertices, connectVerticesBackToFace)
        {
            _normal = MiscFunctions.DetermineNormalForA3DPolygon(Vertices, Vertices.Count, out var reverseVertexOrder, suggestedNormal, out _);
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
                    _normal = ((Vertices[1].Coordinates - Vertices[0].Coordinates)
                        .Cross(Vertices[2].Coordinates - Vertices[0].Coordinates))
                        .Normalize();
                return _normal;
            }
        }

        private Vector3 _normal = Vector3.Null;

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<Vertex> Vertices { get; internal set; }

        /// <summary>
        ///     Gets the first vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex A => Vertices[0];

        /// <summary>
        ///     Gets the second vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex B => Vertices[1];

        /// <summary>
        ///     Gets the third vertex
        /// </summary>
        /// <value>The vertices.</value>
        public Vertex C => Vertices[2];

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public List<Edge> Edges { get; internal set; }

        /// <summary>
        ///     Gets the center.
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

        private Vector3 _center = Vector3.Null;

        /// <summary>
        ///     Gets the area.
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

        private double _area = double.NaN;

        /// <summary>
        ///     Determines the area.
        /// </summary>
        /// <returns>System.Double.</returns>
        internal double DetermineArea()
        {
            var area = 0.0;
            for (var i = 2; i < Vertices.Count; i++)
            {
                var edge1 = Vertices[1].Coordinates.Subtract(Vertices[0].Coordinates);
                var edge2 = Vertices[2].Coordinates.Subtract(Vertices[0].Coordinates);
                // the area of each triangle in the face is the area is half the magnitude of the cross product of two of the edges
                area += 0.5 * Math.Abs(edge1.Cross(edge2).Dot(Normal));
            }
            //If not a number, the triangle is actually a straight line. Set the area = 0, and let repair function fix this.
            return double.IsNaN(area) ? 0.0 : area;
        }

        /// <summary>
        ///     Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color Color { get; set; }

        public PrimitiveSurface BelongsToPrimitive { get; internal set; }

        /// <summary>
        ///     Gets the adjacent faces.
        /// </summary>
        /// <value>The adjacent faces.</value>
        public List<PolygonalFace> AdjacentFaces
        {
            get
            {
                var adjacentFaces = new List<PolygonalFace>();
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

        private CurvatureType _curvature = CurvatureType.Undefined;

        /// <summary>
        ///     Defines the face curvature. Depends on DefineEdgeAngle
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

        #endregion Properties
    }
}