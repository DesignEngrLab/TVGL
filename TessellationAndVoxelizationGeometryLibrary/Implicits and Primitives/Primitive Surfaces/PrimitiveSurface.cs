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

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public abstract class PrimitiveSurface : ICloneable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="connectFacesToPrimitive">if set to <c>true</c> [connect faces to primitive].</param>
        protected PrimitiveSurface(IEnumerable<TriangleFace> faces, bool connectFacesToPrimitive = true)
        {
            if (faces == null) return;
            SetFacesAndVertices(faces, connectFacesToPrimitive);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }

        #endregion Constructors

        /// <summary>
        /// Sets the faces and vertices.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="connectFacesToPrimitive">if set to <c>true</c> [connect faces to primitive].</param>
        public void SetFacesAndVertices(IEnumerable<TriangleFace> faces, bool connectFacesToPrimitive = true)
        {
            _area = double.NaN;
            Faces = new HashSet<TriangleFace>(faces);
            FaceIndices = Faces.Select(f => f.IndexInList).ToArray();
            if (connectFacesToPrimitive)
                foreach (var face in Faces)
                    face.BelongsToPrimitive = this;
            SetVerticesFromFaces();
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
        private void CalculateBothErrors()
        {
            _maxError = 0.0;
            _meanSquaredError = 0.0;
            foreach (var c in Vertices.Select(v => v.Coordinates))
            {
                var d = Math.Abs(PointMembership(c));
                _meanSquaredError += d * d;
                if (_maxError < d)
                    _maxError = d;
            }
            foreach (var c in InnerEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates))
                    .Concat(OuterEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates))))
            {
                var d = PointMembership(c);
                _meanSquaredError += d * d;
            }
            _meanSquaredError /= Vertices.Count + InnerEdges.Count + OuterEdges.Count;
        }

        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public virtual double CalculateMeanSquareError(IEnumerable<Vector3> vertices)// = null)
        {
            //if (vertices == null)
            //{
            //    vertices = Vertices.Select(v => v.Coordinates)
            //        .Concat(InnerEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates)))
            //        .Concat(OuterEdges.Select(edge => 0.5 * (edge.To.Coordinates + edge.From.Coordinates)));
            //}
            var mse = 0.0;
            var n = 0;
            foreach (var c in vertices)
            {
                var d = PointMembership(c);
                mse += d * d;
                n++;
            }
            return mse / n;
        }
        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public virtual double CalculateMaxError(IEnumerable<Vector3> vertices)
        {
            var maxError = 0.0;
            foreach (var c in vertices)
            {
                var d = Math.Abs(PointMembership(c));
                if (maxError < d)
                    maxError = d;
            }
            return maxError;
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
        public abstract double PointMembership(Vector3 point);

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
        double _meanSquaredError = double.NaN;
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
        double _maxError = double.NaN;

        /// <summary>
        /// Gets or sets the triangle faces.
        /// </summary>
        /// <value>The triangle faces.</value>
        [JsonIgnore]
        public HashSet<TriangleFace> Faces { get; set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public HashSet<Vertex> Vertices { get; set; }

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
        private void DefineInnerOuterEdges()
        {
            MiscFunctions.DefineInnerOuterEdges(Faces, out _innerEdges, out _outerEdges);
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public virtual void Transform(Matrix4x4 transformMatrix)
        {
            _meanSquaredError = double.NaN;
            _maxError = double.NaN;
        }

        /// <summary>
        /// Updates surface by adding face
        /// </summary>
        /// <param name="face">The face.</param>
        public void AddFace(TriangleFace face)
        {
            _meanSquaredError = double.NaN;
            _maxError = double.NaN;
            if (Faces.Contains(face)) return;
            _area = Area + face.Area;
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
        /// <returns>IEnumerable&lt;PrimitiveSurface&gt;.</returns>
        public IEnumerable<PrimitiveSurface> AdjacentPrimitives()
        {
            foreach (var border in Borders)
                foreach (var prim in border.AdjacentPrimitives())
                    yield return prim;
        }

        /// <summary>
        /// Gets or sets the borders. PrimitiveBorders is typically one, unless
        /// there is a hole. A border is made up of border segments where each
        /// border segment delineates between a pair of primitives. Since a single
        /// primitive may border numerous other primitives, there are likely to be
        /// many border segments in the border. 
        /// </summary>
        /// <value>The borders.</value>
        public List<PrimitiveBorder> Borders { get; set; }

        /// <summary>
        /// Gets or sets the border segments.
        /// </summary>
        /// <value>The border segments.</value>
        public List<BorderSegment> BorderSegments { get; set; } = new List<BorderSegment>();//initialize to an empty list

        /// <summary>
        /// Borderses the encircling axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>IEnumerable&lt;PrimitiveBorder&gt;.</returns>
        public IEnumerable<PrimitiveBorder> BordersEncirclingAxis(Vector3 axis, Vector3 anchor)
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
        /// Borders the encircles axis.
        /// </summary>
        /// <param name="edgepath">The edgepath.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(EdgePath edgepath, Vector3 axis, Vector3 anchor)
        {
            if (axis.IsNull() || anchor.IsNull() || edgepath.NumPoints <= 2) return false;
            var transform = axis.TransformToXYPlane(out _);
            var coords = edgepath.GetVertices().Select(v => v.ConvertTo2DCoordinates(transform));
            var borderPolygon = new Polygon(coords);
            var center3d = anchor.ConvertTo2DCoordinates(transform);
            return borderPolygon.IsPointInsidePolygon(true, center3d);
        }
        /// <summary>
        /// Borders the encircles axis.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(IEnumerable<Vector3> path, Vector3 axis, Vector3 anchor)
        {
            var angle = Math.Abs(FindWindingAroundAxis(path, axis, anchor, out _, out _));
            return angle > 1.67 * Math.PI;
            // 1.67 is 5/3, which is 5/6 the way around. so the border would be at least a hexagon.
        }

        /// <summary>
        /// Finds the total winding angle around the axis and provides the starting angle.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="startingAngle">The starting angle.</param>
        /// <returns>A double.</returns>
        public static double FindWindingAroundAxis(IEnumerable<Vector3> path, Matrix4x4 transform, Vector3 anchor,
            out double minAngle, out double maxAngle)
        {
            var coords = path.Select(v => v.ConvertTo2DCoordinates(transform));
            var center = anchor.ConvertTo2DCoordinates(transform);
            var startPoint = coords.First();
            var prevVector = startPoint - center;
            var startingAngle = Math.Atan2(prevVector.Y, prevVector.X);
            var angleSum = 0.0;
            minAngle = double.PositiveInfinity;
            maxAngle = double.NegativeInfinity;
            foreach (var coord in coords.Skip(1))
            {
                var nextVector = coord - center;
                angleSum += Math.Atan2(prevVector.Cross(nextVector), prevVector.Dot(nextVector));
                if (minAngle > angleSum) minAngle = angleSum;
                if (maxAngle < angleSum) maxAngle = angleSum;
                prevVector = nextVector;
            }
            minAngle += startingAngle;
            maxAngle += startingAngle;
            return angleSum;
        }
        /// <summary>
        /// Finds the total winding angle around the axis and provides the starting angle.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="startingAngle">The starting angle.</param>
        /// <returns>A double.</returns>
        public static double FindWindingAroundAxis(IEnumerable<Vector3> path, Vector3 axis, Vector3 anchor,
            out double minAngle, out double maxAngle)
        {
            var transform = axis.TransformToXYPlane(out _);
            return FindWindingAroundAxis(path, transform, anchor, out minAngle, out maxAngle);
        }


        public static Vector3 GetCenterOfMass(IEnumerable<TriangleFace> faces)
        {
            var totalArea = 0.0;
            var totalCenter = Vector3.Zero;
            foreach (var face in faces)
            {
                var area = face.Area;
                totalArea += area;
                totalCenter += face.Center * area;
            }
            return totalCenter / totalArea;
        }

        /// <summary>
        /// Gets or sets the maximum x.
        /// </summary>
        /// <value>The maximum x.</value>
        [JsonIgnore]
        public double MaxX { get; protected set; } = double.NaN;
        /// <summary>
        /// Gets or sets the minimum x.
        /// </summary>
        /// <value>The minimum x.</value>
        [JsonIgnore]
        public double MinX { get; protected set; } = double.NaN;
        /// <summary>
        /// Gets or sets the maximum y.
        /// </summary>
        /// <value>The maximum y.</value>
        [JsonIgnore]
        public double MaxY { get; protected set; } = double.NaN;
        /// <summary>
        /// Gets or sets the minimum y.
        /// </summary>
        /// <value>The minimum y.</value>
        [JsonIgnore]
        public double MinY { get; protected set; } = double.NaN;
        /// <summary>
        /// Gets or sets the maximum z.
        /// </summary>
        /// <value>The maximum z.</value>
        [JsonIgnore]
        public double MaxZ { get; protected set; } = double.NaN;
        /// <summary>
        /// Gets or sets the minimum z.
        /// </summary>
        /// <value>The minimum z.</value>
        [JsonIgnore]
        public double MinZ { get; protected set; } = double.NaN;

        /// <summary>
        /// Sets the bounds.
        /// </summary>
        /// <param name="ignoreIfAlreadySet">if set to <c>true</c> [ignore if already set].</param>
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
        /// Returns whether a point is within the X,Y,Z bounds of the primitive.
        /// This is a fast, crude first step to determining interference.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool WithinBounds(Vector3 v)
        {
            SetBounds();//ignores if already set.
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
                SetBounds(true);
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
                    if (segment.GetSecondPrimitive(this) == other)
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


        public Vector3 GetAxis()
        {
            if (this is Plane plane) return plane.Normal;
            else if (this is Cylinder cylinder) return cylinder.Axis;
            else if (this is Cone cone) return cone.Axis;
            else if (this is Torus torus) return torus.Axis;
            else if (this is Prismatic prismatic) return prismatic.Axis;
            else return Vector3.Null;
        }

        public Vector3 GetAnchor()
        {
            if (this is Cylinder cylinder) return cylinder.Anchor;
            else if (this is Cone cone) return cone.Apex;
            else if (this is Torus torus) return torus.Center;
            else if (this is Sphere sphere) return sphere.Center;
            else return GetCenterOfMass(Faces);
        }

        public double GetRadius(bool max = false)
        {
            if (this is Cylinder cylinder) return cylinder.Radius;
            if (this is Sphere sphere) return sphere.Radius;
            if (this is Torus torus)
            {
                if (max) return Math.Max(torus.MajorRadius, torus.MinorRadius);
                return torus.MajorRadius;
            }
            if (this.Borders == null) return double.NaN;

            var circleBorders = this.Borders.Where(b => b.Curve is Circle);
            if (!circleBorders.Any()) return 0.0;
            else if (max) return circleBorders.Max(b => ((Circle)b.Curve).Radius);
            else return circleBorders.Average(b => ((Circle)b.Curve).Radius);
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

        public PrimitiveSurface Copy(IEnumerable<TriangleFace> faces)
        {
            var copy = (PrimitiveSurface)this.Clone();
            copy.SetFacesAndVertices(faces, true);
            return copy;
        }
    }
}