// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    ///     Class PrimitiveSurface.
    /// </summary>
    public abstract class PrimitiveSurface
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        protected PrimitiveSurface(IEnumerable<PolygonalFace> faces)
        {
            Faces = new HashSet<PolygonalFace>(faces);
            foreach (var face in Faces)
                face.BelongsToPrimitive = this;
            Area = Faces.Sum(f => f.Area);
            Vertices = new HashSet<Vertex>(Faces.SelectMany(f => f.Vertices).Distinct());
        }

        #endregion Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }


        public abstract double CalculateError(IEnumerable<IVertex3D> vertices = null);



        /// <summary>
        ///     Gets the area.
        /// </summary>
        /// <value>The area.</value>
        public double Area { get; protected set; }

        /// <summary>
        ///     Gets or sets the polygonal faces.
        /// </summary>
        /// <value>The polygonal faces.</value>
        [JsonIgnore]
        public HashSet<PolygonalFace> Faces { get; protected set; }

        public int[] FaceIndices
        {
            get
            {
                if (Faces != null)
                    return Faces.Select(f => f.IndexInList).ToArray();
                return Array.Empty<int>();
            }
        }

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public HashSet<Vertex> Vertices { get; protected set; }

        public int[] VertexIndices
        {
            get
            {
                if (Vertices != null)
                    return Vertices.Select(v => v.IndexInList).ToArray();
                return Array.Empty<int>();
            }
            set => _vertexIndices = value;
        }

        /// <summary>
        ///     Gets the inner edges.
        /// </summary>
        /// <value>The inner edges.</value>
        [JsonIgnore]
        public HashSet<Edge> InnerEdges
        {
            get
            {
                if (_innerEdges == null) DefineInnerOuterEdges();
                return _innerEdges;
            }
            protected set => _innerEdges = value;
        }

        public int[] InnerEdgeIndices
        {
            get
            {
                if (Faces != null)
                    return InnerEdges.Select(e => e.IndexInList).ToArray();
                return Array.Empty<int>();
            }
        }

        /// <summary>
        ///     Gets the outer edges.
        /// </summary>
        /// <value>The outer edges.</value>
        [JsonIgnore]
        public HashSet<Edge> OuterEdges
        {
            get
            {
                if (_outerEdges == null) DefineInnerOuterEdges();
                return _outerEdges;
            }
            protected set => _outerEdges = value;
        }

        public int[] OuterEdgeIndices
        {
            get
            {
                if (Faces != null)
                    return OuterEdges.Select(e => e.IndexInList).ToArray();
                return Array.Empty<int>();
            }
        }

        private HashSet<Edge> _innerEdges;
        private HashSet<Edge> _outerEdges;
        private int[] _faceIndices;
        private int[] _innerEdgeIndices;
        private int[] _outerEdgeIndices;
        private int[] _vertexIndices;

        private void DefineInnerOuterEdges()
        {
            var outerEdgeHash = new HashSet<Edge>();
            var innerEdgeHash = new HashSet<Edge>();
            if (Faces != null)
                foreach (var face in Faces)
                {
                    foreach (var edge in face.Edges)
                    {
                        if (innerEdgeHash.Contains(edge)) continue;
                        if (!outerEdgeHash.Contains(edge)) outerEdgeHash.Add(edge);
                        else
                        {
                            innerEdgeHash.Add(edge);
                            outerEdgeHash.Remove(edge);
                        }
                    }
                }
            _outerEdges = outerEdgeHash;
            _innerEdges = innerEdgeHash;
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public virtual void Transform(Matrix4x4 transformMatrix)
        {
            foreach (var v in Vertices)
            {
                v.Coordinates = v.Coordinates.Transform(transformMatrix);
            }
        }

        /// <summary>
        ///     Updates surface by adding face
        /// </summary>
        /// <param name="face">The face.</param>
        public void AddFace(PolygonalFace face)
        {
            Area += face.Area;
            foreach (var v in face.Vertices.Where(v => !Vertices.Contains(v)))
                Vertices.Add(v);
            if (face.Edges.Count == face.Vertices.Count)
                foreach (var e in face.Edges.Where(e => !InnerEdges.Contains(e)))
                {
                    if (OuterEdges.Contains(e))
                    {
                        OuterEdges.Remove(e);
                        InnerEdges.Add(e);
                    }
                    else OuterEdges.Add(e);
                }
            else  //basically, this is for cases where edges are not yet defined.
            {
                var faceVertexIndices = face.Vertices.SelectMany(v => new[] { v.IndexInList, v.IndexInList }).ToList();
                var outerEdgesToRemove = new List<Edge>();
                foreach (var outerEdge in OuterEdges)
                {
                    var vertexIndices = Edge.GetVertexIndices(outerEdge.EdgeReference);
                    if (faceVertexIndices.Contains(vertexIndices.Item1) && faceVertexIndices.Contains(vertexIndices.Item2))
                    {
                        faceVertexIndices.Remove(vertexIndices.Item1);
                        faceVertexIndices.Remove(vertexIndices.Item2);
                        outerEdge.UpdateWithNewFace(face);
                        outerEdgesToRemove.Add(outerEdge);
                        InnerEdges.Add(outerEdge);
                        if (faceVertexIndices.Count == 0) break;
                    }
                }
                foreach (var edge in outerEdgesToRemove)
                    OuterEdges.Remove(edge);
                while (faceVertexIndices.Count > 0)
                {
                    var vIndex1 = faceVertexIndices[0];
                    faceVertexIndices.RemoveAt(0);
                    var vIndex2 = faceVertexIndices.First(index => index != vIndex1);
                    faceVertexIndices.Remove(vIndex2);
                    var v1 = face.Vertices.FindIndex(v => v.IndexInList == vIndex1);
                    var v2 = face.Vertices.FindIndex(v => v.IndexInList == vIndex2);
                    var step = v2 - v1;
                    if (step < 0) step += face.Vertices.Count;
                    if (step == 1)
                        OuterEdges.Add(new Edge(face.Vertices[v1], face.Vertices[v2], face, null, false));
                    else OuterEdges.Add(new Edge(face.Vertices[v2], face.Vertices[v1], face, null, false));
                }
            }
            Faces.Add(face);
        }

        public void CompletePostSerialization(TessellatedSolid ts)
        {
            Faces = new HashSet<PolygonalFace>();
            foreach (var i in _faceIndices)
            {
                var face = ts.Faces[i];
                Faces.Add(face);
                face.BelongsToPrimitive = this;
            }
            Vertices = new HashSet<Vertex>();
            foreach (var i in _vertexIndices)
                Vertices.Add(ts.Vertices[i]);

            _innerEdges = new HashSet<Edge>();
            foreach (var i in _innerEdgeIndices)
                InnerEdges.Add(ts.Edges[i]);

            _outerEdges = new HashSet<Edge>();
            foreach (var i in _outerEdgeIndices)
                OuterEdges.Add(ts.Edges[i]);
            Area = Faces.Sum(f => f.Area);
        }

        public HashSet<PolygonalFace> GetAdjacentFaces()
        {
            var adjacentFaces = new HashSet<PolygonalFace>(); //use a hash to avoid duplicates
            foreach (var edge in OuterEdges)
            {
                if (Faces.Contains(edge.OwnedFace)) adjacentFaces.Add(edge.OtherFace);
                else adjacentFaces.Add(edge.OwnedFace);
            }
            return adjacentFaces;
        }


        private List<SurfaceBorder> _borders;
        public List<SurfaceBorder> Borders
        {
            get
            {
                if (_borders == null)
                    DefineBorders();
                return _borders;
            }
        }
        /// <summary>
        /// Takes in a list of edges and returns their list of loops for edges and vertices
        /// The order of the output loops are not considered (i.e., they may be "reversed"),
        /// since no face normal information is used.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public void DefineBorders(double maxErrorInCurveFit = -1.0)
        {
            _borders = new List<SurfaceBorder>();
            var edges = new HashSet<Edge>(OuterEdges);
            while (edges.Any())
            {
                var currentEdge = edges.First();
                edges.Remove(currentEdge);
                var correctDirection = Faces.Contains(currentEdge.OwnedFace);
                var startVertex = correctDirection ? currentEdge.From : currentEdge.To;
                var currentVertex = correctDirection ? currentEdge.To : currentEdge.From;
                var border = new SurfaceBorder();
                border.AddEnd(currentEdge, correctDirection);
                foreach (var forwardDir in new[] { true, false })
                {
                    do
                    {
                        var possibleEdges = currentVertex.Edges.Where(e => e != currentEdge && edges.Contains(e)).ToList();
                        if (possibleEdges.Count == 0)
                        {
                            currentVertex = null;
                            currentEdge = null;
                            continue;
                        }
                        if (possibleEdges.Count == 1) currentEdge = possibleEdges[0];
                        else
                        {
                            var forwardVector = currentEdge.Vector.Normalize();
                            if (currentEdge.From == currentVertex) forwardVector *= -1;
                            var bestDot = double.NegativeInfinity;
                            Edge bestEdge = null;
                            foreach (var e in possibleEdges)
                            {
                                var candidateVector = e.Vector.Normalize();
                                if (e.To == currentVertex) candidateVector *= -1;
                                var dot = candidateVector.Dot(forwardVector);
                                if (bestDot < dot)
                                {
                                    bestDot = dot;
                                    bestEdge = e;
                                }
                            }
                            currentEdge = bestEdge;
                        }
                        correctDirection = (currentEdge.From == currentVertex) == forwardDir;
                        edges.Remove(currentEdge);
                        if (forwardDir) border.AddEnd(currentEdge, correctDirection);
                        else border.AddBegin(currentEdge, correctDirection);
                        currentVertex = currentEdge.OtherVertex(currentVertex);
                    } while (currentEdge != null && currentVertex != startVertex);
                    border.IsClosed = currentVertex == startVertex && border.NumPoints > 2;
#if PRESENT
                    //TVGL.Presenter.ShowVertexPathsWithSolid(new [] {border.GetVertices().Select(v => v.Coordinates) }, new[] { debugSolid }, false);
#endif
                    if (border.IsClosed) break;
                    var currentEdgeAndDir = border.EdgesAndDirection[0];
                    currentEdge = currentEdgeAndDir.edge;
                    currentVertex = currentEdgeAndDir.dir ? currentEdge.From : currentEdge.To;
                }
                if (maxErrorInCurveFit > 0)
                {
                    var curve = MiscFunctions.FindBestPlanarCurve(border.GetVertices().Select(v => v.Coordinates), out var plane, out var planeResidual,
                          out var curveResidual);
                    if (planeResidual < maxErrorInCurveFit && curveResidual < maxErrorInCurveFit)
                    {
                        border.Curve = curve;
                        border.Plane = plane;
                    }
                }
                if (border.IsClosed)
                {
                    var axis = Vector3.Null;
                    var anchor = Vector3.Null;
                    if (this is Cylinder)
                    {
                        axis = ((Cylinder)this).Axis;
                        anchor = ((Cylinder)this).Anchor;
                    }
                    else if (this is Cone)
                    {
                        axis = ((Cone)this).Axis;
                        anchor = ((Cone)this).Apex;
                    }
                    else if (this is Torus)
                    {
                        axis = ((Torus)this).Axis;
                        anchor = ((Torus)this).Center;
                    }
                    else continue;
                    var transform = axis.TransformToXYPlane(out _);
                    var polygon = new Polygon(border.GetVertices().Select(v => v.ConvertTo2DCoordinates(transform)));
                    border.EncirclesAxis = polygon.IsPointInsidePolygon(true, anchor.ConvertTo2DCoordinates(transform));
                }
                _borders.Add(border);
            }
        }

        public double MaxX { get; protected set; } = double.NaN;
        public double MinX { get; protected set; } = double.NaN;
        public double MaxY { get; protected set; } = double.NaN;
        public double MinY { get; protected set; } = double.NaN;
        public double MaxZ { get; protected set; } = double.NaN;
        public double MinZ { get; protected set; } = double.NaN;

        public void SetBounds(bool ignoreIfAlreadySet = true)
        {
            if (ignoreIfAlreadySet && !double.IsNaN(MaxX) && !double.IsNaN(MinX) &&
                !double.IsNaN(MaxY) && !double.IsNaN(MinY) &&
                !double.IsNaN(MaxZ) && !double.IsNaN(MinZ)) return;
            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;
            MaxZ = double.MinValue;
            MinZ = double.MaxValue;
            foreach (var v in Vertices)
            {
                var x = v.X;
                var y = v.Y;
                var z = v.Z;
                if (x > MaxX) MaxX = x;
                if (x < MinX) MinX = x;
                if (y > MaxY) MaxY = y;
                if (y < MinY) MinY = y;
                if (z > MaxZ) MaxZ = z;
                if (z < MinZ) MinZ = z;
            }
        }

        public Vector3 Center
        {
            get
            {
                SetBounds(true);
                return new Vector3(MaxX + MinX, MaxY + MinY, MaxZ + MinZ) / 2;
            }
        }

        public void SetColor(Color color)
        {
            foreach (var face in Faces) face.Color = color;
        }
    }
}