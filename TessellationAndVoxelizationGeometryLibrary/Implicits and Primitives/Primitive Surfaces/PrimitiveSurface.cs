// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="PrimitiveSurface.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class PrimitiveSurface : ICloneable
    {
        [JsonIgnore]
        public SurfaceGroup BelongsToGroup { get; set; }

        /// <summary>
        /// Sets the faces and vertices.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="connectFacesToPrimitive">if set to <c>true</c> [connect faces to primitive].</param>
        public void SetFacesAndVertices(IEnumerable<TriangleFace> faces, bool connectFacesToPrimitive = true,
            bool doNotResetFaceDependentValues = false)
        {
            if (!doNotResetFaceDependentValues)
                ResetFaceDependentValues();
            ResetFaceDependentConnectivity();
            if (faces == null) Faces = new HashSet<TriangleFace>();
            else Faces = new HashSet<TriangleFace>(faces);
            FaceIndices = Faces.Select(f => f.IndexInList).ToArray();
            if (connectFacesToPrimitive)
                foreach (var face in Faces)
                    face.BelongsToPrimitive = this;
            if (faces == null) return;
            var firstFace = Faces.First();
            var faceNormal = firstFace.Normal;
            if (this is Plane plane)
            {
                if (faceNormal.Dot(plane.Normal) < 0)
                {
                    plane.Normal = -plane.Normal;
                    plane.DistanceToOrigin = -plane.DistanceToOrigin;
                }
            }
            else if (this is not UnknownRegion && this is not Prismatic)
            {   // can't check unknown or prismatic since these RELY on faces for determining normal
                var primNormalAtFirst = GetNormalAtPoint(firstFace.Center);
                if (faceNormal.Dot(primNormalAtFirst) < 0)
                {
                    if (IsPositive.HasValue) IsPositive = !IsPositive;
                    else IsPositive = false;
                }
            }
            SetVerticesFromFaces();
        }

        private void ResetFaceDependentValues()
        {
            _area = double.NaN;
            _maxError = double.NaN;
            _meanSquaredError = double.NaN;
            //isPositive = null;
            Borders = null;
            BorderSegments = null;
            MinX = double.NaN;
            MinY = double.NaN;
            MinZ = double.NaN;
            MaxX = double.NaN;
            MaxY = double.NaN;
            MaxZ = double.NaN;
        }

        private void ResetFaceDependentConnectivity()
        {
            _adjacentSurfaces = null;
            _innerEdges = null;
            _outerEdges = null;
            Borders = null;
            BorderSegments = null;
        }


        /// <summary>
        /// Sets the vertices from faces.
        /// </summary>
        public void SetVerticesFromFaces()
        {
            Vertices = new HashSet<Vertex>();
            //Don't use linq here, so we can avoid unnecessary intermediate lists.
            foreach (var face in Faces)
                foreach (var v in face.Vertices)
                    Vertices.Add(v);
        }


        /// <summary>
        /// Calculates the both errors.
        /// </summary>
        internal protected virtual void CalculateBothErrors()
        {
            _maxError = 0.0;
            _meanSquaredError = 0.0;
            if (Vertices == null || Vertices.Count == 0) return;
            foreach (var c in Vertices.Select(v => v.Coordinates)
                // also add midpoints of edges
                .Concat(InnerEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates))
                .Concat(OuterEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates)))))
            {
                var d = Math.Abs(DistanceToPoint(c));
                _meanSquaredError += d * d;
                if (_maxError < d)
                    _maxError = d;
            }
            _meanSquaredError /= Vertices.Count + InnerEdges.Count + OuterEdges.Count;
        }

        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="points">The vertices.</param>
        /// <returns>System.Double.</returns>
        public virtual double CalculateMeanSquareError(IEnumerable<Vector3> points)
        {
            var mse = 0.0;
            var n = 0;
            foreach (var c in points)
            {
                var d = DistanceToPoint(c);
                mse += d * d;
                n++;
            }
            return mse / n;
        }
        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="points">The vertices.</param>
        /// <returns>System.Double.</returns>
        public virtual double CalculateMaxError(IEnumerable<Vector3> points)
        {
            var maxError = 0.0;
            foreach (var c in points)
            {
                var d = Math.Abs(DistanceToPoint(c));
                if (maxError < d)
                    maxError = d;
            }
            return maxError;
        }

        /// <summary>
        /// Returns whether the given point is inside the primitive.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public virtual bool PointIsInside(Vector3 x)
        {
            return DistanceToPoint(x) < 0;
        }

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public abstract IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed);
        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public abstract Vector2 TransformFrom3DTo2D(Vector3 point);
        /// <summary>
        /// Transforms the from2 d to3 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public abstract Vector3 TransformFrom2DTo3D(Vector2 point);
        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public abstract double DistanceToPoint(Vector3 point);

        /// <summary>
        /// Returns all intersection of the given line with the primitive surface (which could be zero to four points).
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public abstract IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction);

        /// <summary>
        /// Gets the normal of the surface at the provided point.
        /// The method may return a 'valid' value even if not on the surface
        /// (e.g. a point inside a cylinder still has a meaningful normal).
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>A Vector3.</returns>
        public abstract Vector3 GetNormalAtPoint(Vector3 point);

        /// <summary>
        /// Gets or sets the triangle vertex indices.
        /// </summary>
        /// <value>The triangle vertex indices.</value>
        [JsonIgnore]
        //A tempory class used when importing primitives 
        public (int, int, int)[] TriangleVertexIndices { get; set; }

        /// <summary>
        /// Gets or sets the face indices.
        /// </summary>
        /// <value>The face indices.</value>
        public int[] FaceIndices
        {
            get
            {
                if (Faces == null && _faceIndices == null)
                    return Array.Empty<int>();
                if (Faces != null && (_faceIndices == null || _faceIndices.Length < Faces.Count))
                    _faceIndices = Faces.Select(f => f.IndexInList).ToArray();
                return _faceIndices;
            }
            set => _faceIndices = value;
        }
        /// <summary>
        /// The face indices
        /// </summary>
        int[] _faceIndices;

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>The index.</value>
        [JsonIgnore]
        public int Index { get; set; }

        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>The area.</value>
        [JsonIgnore]
        public double Area
        {
            get
            {
                if (double.IsNaN(_area) && Faces != null)
                    _area = Faces.Sum(f => f.Area);
                return _area;
            }
        }
        /// <summary>
        /// The area
        /// </summary>
        double _area = double.NaN;


        /// <summary>
        /// Is the primitive surface positive? (false is negative). Positive generally means
        /// that - if isolated - it would represent a finite solid. Whereas, negative is like a 
        /// hole or void in space. e.g. a positive cylinder is like a peg or shaft, and a
        /// negative cylinder is like a drilled hole.
        /// </summary>
        /// <value><c>true</c> if this instance is positive; otherwise, <c>false</c>.</value>
        public bool? IsPositive
        {
            get
            {
                if (!isPositive.HasValue) CalculateIsPositive();
                return isPositive;
            }
            set
            {
                isPositive = value;
            }
        }
        protected bool? isPositive;
        protected abstract void CalculateIsPositive();


        /// <summary>
        /// Gets the residual.
        /// </summary>
        /// <value>The residual.</value>
        [JsonIgnore]
        public double MeanSquaredError
        {
            get
            {
                if (double.IsNaN(_meanSquaredError))
                    CalculateBothErrors();
                return _meanSquaredError;
            }
        }
        /// <summary>
        /// The mean squared error
        /// </summary>
        internal protected double _meanSquaredError = double.NaN;
        /// <summary>
        /// Gets the maximum error.
        /// </summary>
        /// <value>The maximum error.</value>
        [JsonIgnore]
        public double MaxError
        {
            get
            {
                if (double.IsNaN(_maxError) && Faces != null)
                    CalculateBothErrors();
                return _maxError;
            }
        }
        /// <summary>
        /// The maximum error
        /// </summary>
        internal protected double _maxError = double.NaN;

        /// <summary>
        /// Gets or sets the triangle faces.
        /// </summary>
        /// <value>The triangle faces.</value>
        [JsonIgnore]
        public virtual HashSet<TriangleFace> Faces { get; set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public virtual HashSet<Vertex> Vertices { get; set; }

        /// <summary>
        /// Gets the inner edges.
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
        /// Gets the outer edges.
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

        /// <summary>
        /// The inner edges
        /// </summary>
        private HashSet<Edge> _innerEdges;
        /// <summary>
        /// The outer edges
        /// </summary>
        private HashSet<Edge> _outerEdges;

        /// <summary>
        /// Defines the inner outer edges.
        /// </summary>
        internal void DefineInnerOuterEdges()
        {
            MiscFunctions.DefineInnerOuterEdges(Faces, out _innerEdges, out _outerEdges);
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public virtual void Transform(Matrix4x4 transformMatrix, bool transformFacesAndVertices)
        {
            _meanSquaredError = double.NaN;
            _maxError = double.NaN;
            if (transformFacesAndVertices)
            {
                foreach (var v in Vertices)
                    v.Coordinates = v.Coordinates.Transform(transformMatrix);
                foreach (var f in Faces)
                    f.Update();
            }
        }

        /// <summary>
        /// Updates surface by adding face
        /// </summary>
        /// <param name="face">The face.</param>
        public void AddFace(TriangleFace face)
        {
            _meanSquaredError = double.NaN;
            _maxError = double.NaN;
            if (Faces == null) Faces = new HashSet<TriangleFace>();
            if (Faces.Contains(face)) return;
            _area = Area + face.Area;
            if (Vertices == null) Vertices = new HashSet<Vertex>();
            foreach (var v in face.Vertices.Where(v => !Vertices.Contains(v)))
                Vertices.Add(v);
            if (face.AB != null && face.BC != null && face.CA != null)
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
                var faceVertexIndices = new List<int> { face.A.IndexInList, face.B.IndexInList, face.B.IndexInList, face.C.IndexInList, face.C.IndexInList, face.A.IndexInList };
                //this is kind of hacky, but the faceVertexIndices don't need to be in order, so we can just add the same vertex twice. The if statement below will catch it.
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
                    if (step < 0) step += 3;
                    if (step == 1)
                        OuterEdges.Add(new Edge(face.A, face.B, face, null, false));
                    else OuterEdges.Add(new Edge(face.B, face.A, face, null, false));
                }
            }
            Faces.Add(face);
            face.BelongsToPrimitive = this;
        }

        /// <summary>
        /// Updates the belongs to primitive.
        /// </summary>
        public void UpdateBelongsToPrimitive()
        {
            foreach (var face in Faces)
                face.BelongsToPrimitive = this;
        }

        /// <summary>
        /// Completes the post serialization.
        /// </summary>
        /// <param name="ts">The ts.</param>
        public void CompletePostSerialization(TessellatedSolid ts)
        {
            Faces = new HashSet<TriangleFace>();
            Vertices = new HashSet<Vertex>();
            foreach (var i in FaceIndices)
            {
                var face = ts.Faces[i];
                Faces.Add(face);
                face.BelongsToPrimitive = this;
                foreach (var v in face.Vertices)
                    if (!Vertices.Contains(v)) Vertices.Add(v);
            }
            _outerEdges = null;
            _innerEdges = null;
            if (Borders != null)
                foreach (var border in Borders)
                    border.CompletePostSerialization(ts);
        }

        /// <summary>
        /// Gets the adjacent faces.
        /// </summary>
        /// <returns>HashSet&lt;TriangleFace&gt;.</returns>
        public HashSet<TriangleFace> GetAdjacentFaces()
        {
            var adjacentFaces = new HashSet<TriangleFace>(); //use a hash to avoid duplicates
            foreach (var edge in OuterEdges)
            {
                if (Faces.Contains(edge.OwnedFace)) adjacentFaces.Add(edge.OtherFace);
                else adjacentFaces.Add(edge.OwnedFace);
            }
            return adjacentFaces;
        }

        /// <summary>
        /// Adjacents the primitives.
        /// </summary>
        /// <returns>ISet&lt;PrimitiveSurface&gt;.</returns>
        public virtual HashSet<PrimitiveSurface> AdjacentPrimitives()
        {
            //Use a set to avoid duplicates. DO NOT USE IEnumerable.
            var set = new HashSet<PrimitiveSurface>();
            foreach (var border in Borders)
                foreach (var prim in border.AdjacentPrimitives())
                    set.Add(prim);
            return set;
        }

        /// <summary>
        /// Gets or sets the borders. PrimitiveBorders is typically one, unless
        /// there is a hole. A border is made up of border segments where each
        /// border segment delineates between a pair of primitives. Since a single
        /// primitive may border numerous other primitives, there are likely to be
        /// many border segments in the border. 
        /// </summary>
        /// <value>The borders.</value>
        [JsonIgnore]
        public List<BorderLoop> Borders { get; set; }

        /// <summary>
        /// Gets or sets the border segments.
        /// </summary>
        /// <value>The border segments.</value>
        [JsonIgnore]
        public List<BorderSegment> BorderSegments { get; set; } = new List<BorderSegment>();//initialize to an empty list


        /// <summary>
        /// Gets the maximum X value.
        /// </summary>
        [JsonIgnore]
        public double MaxX
        {
            get
            {
                if (double.IsNaN(maxX))
                    SetBounds();
                return maxX;
            }
            protected set => maxX = value;
        }
        private double maxX = double.NaN;

        /// <summary>
        /// Gets the minimum X value.
        /// </summary>
        [JsonIgnore]
        public double MinX
        {
            get
            {
                if (double.IsNaN(minX))
                    SetBounds();
                return minX;
            }
            protected set => minX = value;

        }
        private double minX = double.NaN;


        /// <summary>
        /// Gets the maximum Y value.
        /// </summary>
        [JsonIgnore]
        public double MaxY
        {
            get
            {
                if (double.IsNaN(maxY))
                    SetBounds();
                return maxY;
            }
            protected set => maxY = value;
        }
        private double maxY = double.NaN;

        /// <summary>
        /// Gets the minimum Y value.
        /// </summary>
        [JsonIgnore]
        public double MinY
        {
            get
            {
                if (double.IsNaN(minY))
                    SetBounds();
                return minY;
            }
            protected set => minY = value;

        }
        private double minY = double.NaN;
        /// <summary>
        /// Gets the maximum Z value.
        /// </summary>
        [JsonIgnore]
        public double MaxZ
        {
            get
            {
                if (double.IsNaN(maxZ))
                    SetBounds();
                return maxZ;
            }
            protected set => maxZ = value;
        }
        private double maxZ = double.NaN;

        /// <summary>
        /// Gets the minimum z value.
        /// </summary>
        [JsonIgnore]
        public double MinZ
        {
            get
            {
                if (double.IsNaN(minZ))
                    SetBounds();
                return minZ;
            }
            protected set => minZ = value;

        }
        private double minZ = double.NaN;

        /// <summary>
        /// Sets the bounds.
        /// </summary>
        /// <param name="ignoreIfAlreadySet">if set to <c>true</c> [ignore if already set].</param>
        public void SetBounds()
        {
            if (Vertices == null || Vertices.Count == 0) SetPrimitiveLimits();
            else
            {
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
        }

        protected abstract void SetPrimitiveLimits();

        /// <summary>
        /// Returns whether a point is within the X,Y,Z bounds of the primitive.
        /// This is a fast, crude first step to determining interference.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool WithinBounds(Vector3 v)
        {
            var x = v.X;
            if (x > MaxX) return false;
            if (x < MinX) return false;
            var y = v.Y;
            if (y > MaxY) return false;
            if (y < MinY) return false;
            var z = v.Z;
            if (z > MaxZ) return false;
            if (z < MinZ) return false;
            return true;
        }

        /// <summary>
        /// Gets the center of the bounding box.
        /// </summary>
        /// <value>The center of the bounding box.</value>
        [JsonIgnore]
        public Vector3 CenterOfBoundingBox
        {
            get
            {
                if (Vertices == null || Vertices.Count == 0) return Vector3.Null;
                SetBounds();
                return new Vector3(MaxX + MinX, MaxY + MinY, MaxZ + MinZ) / 2;
            }
        }

        /// <summary>
        /// Sets the color.
        /// </summary>
        /// <param name="color">The color.</param>
        public void SetColor(Color color)
        {
            foreach (var face in Faces) face.Color = color;
        }

        /// <summary>
        /// The adjacent surfaces
        /// </summary>
        private HashSet<PrimitiveSurface> _adjacentSurfaces;

        /// <summary>
        /// Gets the adjacent primitives.
        /// </summary>
        /// <param name="edgePrimitiveMap">The edge primitive map.</param>
        /// <param name="surfacesToConsider">The surfaces to consider.</param>
        /// <returns>HashSet&lt;PrimitiveSurface&gt;.</returns>
        public HashSet<PrimitiveSurface> GetAdjacentPrimitives(
             Dictionary<Edge, (PrimitiveSurface, PrimitiveSurface)> edgePrimitiveMap,
             HashSet<PrimitiveSurface> surfacesToConsider = null)
        {
            if (_adjacentSurfaces == null)
            {
                _adjacentSurfaces = new HashSet<PrimitiveSurface>();
                foreach (var edge in OuterEdges)
                    _adjacentSurfaces.Add(Other(edgePrimitiveMap[edge]));//Will not add duplicated as a hash set.
                _adjacentSurfaces.Remove(this); // just in case the edge map had an edge with both primitives as this prim, remove it.
            }
            //If given a subset of surface to consider, return those surfaces which are in both hashes. Don't permanently alter _adjacentSurfaces.
            if (surfacesToConsider != null)
                return new HashSet<PrimitiveSurface>(_adjacentSurfaces.Where(p => surfacesToConsider.Contains(p)));
            return _adjacentSurfaces;
        }

        /// <summary>
        /// Others the specified edge primitives.
        /// </summary>
        /// <param name="edgePrimitives">The edge primitives.</param>
        /// <returns>PrimitiveSurface.</returns>
        private PrimitiveSurface Other((PrimitiveSurface, PrimitiveSurface) edgePrimitives)
        {
            return this == edgePrimitives.Item1 ? edgePrimitives.Item2 : edgePrimitives.Item1;
        }

        /// <summary>
        /// Gets the shared edges.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>List&lt;Edge&gt;.</returns>
        public List<Edge> GetSharedEdges(PrimitiveSurface other)
        {
            var shared = new List<Edge>();
            foreach (var edge in OuterEdges)
            {
                if (other.OuterEdges.Contains(edge))
                    shared.Add(edge);
            }
            return shared;
        }


        /// <summary>
        /// Gets the shared border segment.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns>BorderSegment.</returns>
        public BorderSegment GetSharedBorderSegment(PrimitiveSurface other)
        {
            foreach (var border in Borders)
                foreach (var segment in border.Segments)
                    if (segment.AdjacentPrimitive(this) == other)
                        return segment;
            return null;
        }

        /// <summary>
        /// Gets the vectors.
        /// </summary>
        /// <returns>IEnumerable&lt;Vector3[]&gt;.</returns>
        public IEnumerable<Vector3[]> GetOuterEdgesAsVectors()
        {
            return OuterEdges.Select(e => new[] { e.From.Coordinates, e.To.Coordinates });
        }

        /// <summary>
        /// Clones the Primitive Solid and will work on inherited members, but note that this only copies
        /// the value types (i.e. ShallowCopy), which is all that inherited types should add.
        /// </summary>
        /// <returns>An object.</returns>
        public object Clone()
        {
            return this.MemberwiseClone();
        }

        public PrimitiveSurface Copy(IEnumerable<TriangleFace> faces, bool doNotResetFaceDependentValues = false)
        {
            var copy = (PrimitiveSurface)this.Clone();
            copy.SetFacesAndVertices(faces, true, doNotResetFaceDependentValues);
            return copy;
        }

        public PrimitiveSurface Copy()
        {
            var copy = (PrimitiveSurface)this.Clone();
            if (Faces != null && Faces.Count != 0)
            {
                var i = 0;
                //NumberOfVertices = vertices.Count;
                var newVertices = new Vertex[Vertices.Count];
                var oldNewVertexIndexDict = new Dictionary<int, int>();
                foreach (var vertex in Vertices)
                {
                    oldNewVertexIndexDict.Add(vertex.IndexInList, i);
                    newVertices[i] = new Vertex(vertex.Coordinates, i);
                    i++;
                }
                i = 0;
                var newFaces = new TriangleFace[Faces.Count];
                foreach (var oldFace in Faces)
                {
                    var newFace = new TriangleFace(newVertices[oldNewVertexIndexDict[oldFace.A.IndexInList]],
                        newVertices[oldNewVertexIndexDict[oldFace.B.IndexInList]], newVertices[oldNewVertexIndexDict[oldFace.C.IndexInList]]);
                    newFace.IndexInList = i;
                    newFace.Color = oldFace.Color;
                    newFaces[i] = newFace;
                    i++;
                }
                copy.SetFacesAndVertices(newFaces, true);
            }
            return copy;
        }

        public abstract string KeyString { get; }

        private protected string GetCommonKeyDetails()
        {
            var key = "|";
            if (IsPositive.HasValue)
            {
                if (IsPositive.Value) key += "P";
                else key += "N";
            }
            if (Faces != null && Faces.Any())
                key += "|" + Area.ToString("F5");
            if (OuterEdges != null && OuterEdges.Any())
                key += "|" + OuterEdges.Sum(f => f.Length).ToString("F5");
            return key;
        }


        public Polygon GetAsPolygon()
        {
            var polygons = new List<Polygon>();
            var vertexLoops = OuterEdges.MakeEdgePaths(true,
                new EdgePathLoopsAroundInputFaces(Faces)).Select(ep => ep.GetVertices().ToList());
            foreach (var loop in vertexLoops)
                polygons.Add(new Polygon(TransformFrom3DTo2D(loop.Select(v => v.Coordinates), true)));
            return polygons.CreateShallowPolygonTrees(false).First();
        }
    }
}