// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using MIConvexHull;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL.Primitives
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
            if (faces == null) return;
            SetFacesAndVertices(faces);
        }

        protected PrimitiveSurface(PrimitiveSurface originalToBeCopied, TessellatedSolid copiedTessellatedSolid)
        {
            _area = originalToBeCopied._area;
            var length = originalToBeCopied.FaceIndices.Length;
            _faceIndices = new int[length];
            for (int i = 0; i < length; i++)
                _faceIndices[i] = originalToBeCopied.FaceIndices[i];
            length = originalToBeCopied.VertexIndices.Length;
            _vertexIndices = new int[length];
            for (int i = 0; i < length; i++)
                _vertexIndices[i] = originalToBeCopied.VertexIndices[i];
            length = originalToBeCopied.InnerEdgeIndices.Length;
            _innerEdgeIndices = new int[length];
            for (int i = 0; i < length; i++)
                _innerEdgeIndices[i] = originalToBeCopied.InnerEdgeIndices[i];
            length = originalToBeCopied.OuterEdgeIndices.Length;
            _outerEdgeIndices = new int[length];
            for (int i = 0; i < length; i++)
                _outerEdgeIndices[i] = originalToBeCopied.OuterEdgeIndices[i];
            if (originalToBeCopied.Borders != null && originalToBeCopied.Borders.Any())
            {
                _borders = new List<SurfaceBorder>();
                foreach (var origBorder in originalToBeCopied.Borders)
                    _borders.Add(origBorder.Copy(this, false, copiedTessellatedSolid));
            }
            if (copiedTessellatedSolid != null)
                CompletePostSerialization(copiedTessellatedSolid);
        }
        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }

        #endregion Constructors

        public void SetFacesAndVertices(IEnumerable<PolygonalFace> faces)
        {
            Faces = new HashSet<PolygonalFace>(faces);
            foreach (var face in Faces)
                face.BelongsToPrimitive = this;
            Vertices = new HashSet<Vertex>(Faces.SelectMany(f => f.Vertices).Distinct());
        }

        public abstract double CalculateError(IEnumerable<Vector3> vertices = null);
        public abstract IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<IVertex3D> points);
        public abstract Vector2 TransformFrom3DTo2D(IVertex3D point);
        public abstract Vector3 TransformFrom2DTo3D(IVertex2D point);
        public int Index { get; set; }

        /// <summary>
        ///     Gets the area.
        /// </summary>
        /// <value>The area.</value>
        public double Area
        {
            get
            {
                if (double.IsNaN(_area) && Faces != null)
                    _area = Faces.Sum(f => f.Area);
                return _area;
            }
        }
        double _area = double.NaN;
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
            set => _faceIndices = value;
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

        /// <summary>
        ///     Gets IsPositive by using the inner edges
        /// </summary>
        /// <value>The inner edges.</value>
        public bool PositiveByEdges()
        {
            var concave = 0;
            var convex = 0;
            foreach (var edge in InnerEdges)
            {
                if (edge.Curvature == CurvatureType.Concave) concave++;
                else if (edge.Curvature == CurvatureType.Convex) convex++;
            }
            return convex > concave;
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
            MiscFunctions.DefineInnerOuterEdges(Faces, out _innerEdges, out _outerEdges);
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public abstract void Transform(Matrix4x4 transformMatrix);

        /// <summary>
        ///     Updates surface by adding face
        /// </summary>
        /// <param name="face">The face.</param>
        public void AddFace(PolygonalFace face)
        {
            _area = Area + face.Area;
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
                    var vertexIndex1 = outerEdge.From.IndexInList;
                    var vertexIndex2 = outerEdge.To.IndexInList;
                    if (faceVertexIndices.Contains(vertexIndex1) && faceVertexIndices.Contains(vertexIndex2))
                    {
                        faceVertexIndices.Remove(vertexIndex1);
                        faceVertexIndices.Remove(vertexIndex2);
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
            face.BelongsToPrimitive = this;
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
                if (_borders == null || _borders.Count == 0)
                    DefineBorders();
                return _borders;
            }
        }

        public IEnumerable<SurfaceBorder> BordersEncirclingAxis(Vector3 axis, Vector3 anchor)
        {
            var transform = axis.TransformToXYPlane(out _);
            foreach (var border in Borders)
            {
                var polygon = new Polygon(border.GetVertices().Select(v => v.ConvertTo2DCoordinates(transform)));
                if (anchor != Vector3.Null && polygon.IsPointInsidePolygon(true, anchor.ConvertTo2DCoordinates(transform)))
                    yield return border;
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
            var currentSurfaceError = CalculateError();
            if (currentSurfaceError > maxErrorInCurveFit) maxErrorInCurveFit = Math.Max(currentSurfaceError, Constants.ErrorForFaceInSurface * 100);
            _borders = new List<SurfaceBorder>();
            var edges = new HashSet<Edge>(OuterEdges);
            foreach (var border in edges.GetLoops(Faces))
            {
                _borders.Add(border);
                var curve = MiscFunctions.FindBestPlanarCurve(border.GetVertices().Select(v => v.Coordinates),
                    out var bestFitPlane, out var planeResidual, out var curveResidual);
                //if (planeResidual < maxErrorInCurveFit)
                border.Surface = bestFitPlane;
                border.SurfaceError = planeResidual;
                border.CurveError = curveResidual;
                SetBorderConvexity(border);
                if (curveResidual < maxErrorInCurveFit)
                {
                    //Check curve residual with center points to avoid fitting curves to non-curved features.
                    //Do not do this when defining the curve, because these center points will always have increased error.
                    var vertices = border.EdgeList.Select(p => p.Center());
                    foreach (var p in vertices)
                        curveResidual += curve.SquaredErrorOfNewPoint(border.Surface.TransformFrom3DTo2D(p));
                    if (curveResidual < maxErrorInCurveFit)
                        border.Curve = curve;
                }
                if (border.IsClosed)
                {
                    var axis = Vector3.Null;
                    var anchor = Vector3.Null;
                    if (this is Cylinder cylinder)
                    {
                        axis = cylinder.Axis;
                        anchor = cylinder.Anchor;
                    }
                    else if (this is Cone cone)
                    {
                        axis = cone.Axis;
                        anchor = cone.Apex;
                    }
                    else if (this is Torus torus)
                    {
                        axis = torus.Axis;
                        anchor = torus.Center;
                    }
                    else if (this is Plane plane)
                    {
                        axis = plane.Normal;
                    }
                    else continue;
                    var transform = axis.TransformToXYPlane(out _);
                    var polygon = new Polygon(border.GetVertices().Select(v => v.ConvertTo2DCoordinates(transform)));
                    if (anchor != Vector3.Null)
                    {
                        border.EncirclesAxis = polygon.IsPointInsidePolygon(true, anchor.ConvertTo2DCoordinates(transform));
                    }
                }
            }
        }

        private static void SetBorderConvexity(SurfaceBorder border)
        {
            var concave = 0;
            var convex = 0;
            var flat = 0;
            foreach (var (edge, _) in border)
            {
                var p1 = edge.OwnedFace.OtherVertex(edge);
                var p2 = edge.OtherFace.OtherVertex(edge);
                var v1 = p2.Coordinates - p1.Coordinates;
                var dot1 = v1.Dot(edge.OwnedFace.Normal);
                var v2 = p1.Coordinates - p2.Coordinates;
                var dot2 = v2.Dot(edge.OtherFace.Normal);
                if (Math.Sign(dot1) != Math.Sign(dot2)) { }
                if (dot1 < 0 && edge.Curvature == CurvatureType.Concave) { }
                if (dot1 > 0 && edge.Curvature == CurvatureType.Convex && !edge.InternalAngle.IsNegligible(0.001)) { }
                if (edge.InternalAngle.IsPracticallySame(Math.PI, Constants.SameFaceNormalDotTolerance)) flat++;
                else if (edge.InternalAngle > Math.PI) concave++;
                else convex++;
            }
            border.FullyFlush = flat > 0 && convex == 0 && concave == 0;
            border.FullyConcave = concave > 0 && flat == 0 && convex == 0;
            border.FullyConvex = convex > 0 && flat == 0 && concave == 0;
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

        /// <summary>
        /// Gets the center of the bounding box.
        /// </summary>
        /// <value>The center of the bounding box.</value>
        public Vector3 CenterOfBoundingBox
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