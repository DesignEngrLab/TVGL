// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : campmatt
// Created          : 01-04-2021
//
// Last Modified By : campmatt
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="BorderLoop.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class BorderLoop.
    /// </summary>
    [JsonObject]
    public class BorderLoop : EdgePath
    {
        /// <summary>
        /// Default instance of the <see cref="BorderLoop" />.
        /// </summary>
        public BorderLoop() : base()
        {
            Segments = new List<BorderSegment>();
            SegmentDirections = new List<bool>();
        }
        /// <summary>
        /// The list of BorderSegments that make up this BorderLoop. Note that a
        /// BorderLoop is unique to a primitive but the BorderSegments are defined between them.
        /// </summary>
        public List<BorderSegment> Segments { get; set; }

        /// <summary>
        /// Gets the edges and direction.
        /// </summary>
        /// <value>The edges and direction.</value>
        [JsonIgnore]
        public List<bool> SegmentDirections { get; protected set; }

        /// <summary>
        /// Gets or sets the plane.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public PrimitiveSurface OwnedPrimitive { get; set; }

        /// <summary>
        /// Returns all primitives that share an edge segment with this border
        /// </summary>
        /// <returns>IEnumerable&lt;PrimitiveSurface&gt;.</returns>
        public ISet<PrimitiveSurface> AdjacentPrimitives()
        {
            //Use a set to avoid duplicates. DO NOT USE IEnumerable.
            var set = new HashSet<PrimitiveSurface>();
            foreach (var segment in Segments)
                set.Add(segment.AdjacentPrimitive(OwnedPrimitive));
            return set;
        }

        /// <summary>
        /// Returns all primitives that share a vertex with this border
        /// </summary>
        /// <returns>IEnumerable&lt;PrimitiveSurface&gt;.</returns>
        public IEnumerable<PrimitiveSurface> AdjacentPrimitivesByVertex()
        {
            var adjacents = new HashSet<PrimitiveSurface>();
            foreach (var vertex in GetVertices())
                foreach (var face in vertex.Faces)
                    if (face.BelongsToPrimitive != OwnedPrimitive)
                        adjacents.Add(face.BelongsToPrimitive);
            return adjacents;
        }

        /// <summary>
        /// Gets or sets the best-fit plane normal
        /// </summary>
        /// <value>The plane error.</value>
        public Vector3 PlaneNormal { get; set; }

        /// <summary>
        /// Gets or sets the best-fit plane error
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneError { get; set; }

        /// <summary>
        /// Gets or sets the best-fit plane distance
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneDistance{ get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [encircles axis].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool EncirclesAxis { get; set; }

        private bool _curvatureIsSet { get; set; }

        /// <summary>
        /// The curvature
        /// </summary>
        private CurvatureType _curvature = CurvatureType.Undefined;

        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        [JsonIgnore]
        public CurvatureType Curvature
        {
            get
            {
                if (!_curvatureIsSet)
                    SetCurvature();
                return _curvature;
            }
            set => _curvature = value;
        }

        /// <summary>
        /// Sets the curvature.
        /// </summary>
        private void SetCurvature()
        {
            var concave = 0;
            var convex = 0;
            var flat = 0;
            foreach (var segment in Segments)
            {
                if (segment.Curvature == CurvatureType.Concave) concave++;
                else if (segment.Curvature == CurvatureType.Convex) convex++;
                else flat++;
            }
            if (flat > 0 && convex == 0 && concave == 0)
                _curvature = CurvatureType.SaddleOrFlat;
            else if (concave > 0 && flat == 0 && convex == 0)
                _curvature = CurvatureType.Concave;
            else if (convex > 0 && flat == 0 && concave == 0)
                _curvature = CurvatureType.Convex;
            else
                _curvature = CurvatureType.Undefined;
            _curvatureIsSet = true;
        }

        public void SetBorderPlane()
        {
            var vertices = GetVectors();
            var verticesPlusCenters = new List<Vector3>(vertices);
            foreach (var center in GetCenters())
                verticesPlusCenters.Add(center);
            var plane = Plane.FitToVertices(verticesPlusCenters, Vector3.Null, out var planeMaxError);
            if (plane == null)
            {
                PlaneError = planeMaxError;
                PlaneNormal = Vector3.Null;
                PlaneDistance = double.NaN;
                return;
            }
            PlaneError = planeMaxError;
            PlaneNormal = plane.Normal;
            PlaneDistance = plane.DistanceToOrigin;
        }

        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public ICurve Curve { get; set; }

        /// <summary>
        /// Gets or sets the curve error.
        /// </summary>
        /// <value>The curve error.</value>
        public double CurveError { get; set; }

        /// <summary>
        /// Gets whether the [edge path is circular].
        /// </summary>
        /// <value><c>true</c> if this instance is circular; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsCircular
        {
            get
            {
                if (Curve == null) return false;
                return Curve is Circle;
            }
        }

        /// <summary>
        /// Gets the center of the circle if the border is a circle.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public Vector3 CircleCenter
        {
            get
            {
                if (IsCircular)
                    return OwnedPrimitive.TransformFrom2DTo3D(((Circle)Curve).Center);
                return Vector3.Null;
            }
        }

        /// <summary>
        /// Gets as polygon.
        /// </summary>
        /// <value>As polygon.</value>
        [JsonIgnore]
        public Polygon AsPolygon
        {
            get
            {
                if (_polygon == null)
                    _polygon = new Polygon(OwnedPrimitive.TransformFrom3DTo2D(GetVectors(), IsClosed));
                return _polygon;
            }
        }
        /// <summary>
        /// The polygon
        /// </summary>
        private Polygon _polygon;

        /// <summary>
        /// Gets the area of the polygon according to the primitives 3D to 2D transform
        /// </summary>
        public double Area => AsPolygon.Area;

        /// <summary>
        /// Copies the specified copied surface.
        /// </summary>
        /// <param name="copiedSurface">The copied surface.</param>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>BorderLoop.</returns>
        public BorderLoop Copy(PrimitiveSurface copiedSurface, bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
            int startIndex = 0, int endIndex = -1)
        {
            var copy = new BorderLoop();
            copy.OwnedPrimitive = copiedSurface;
            copy.Curve = Curve;
            copy.EncirclesAxis = EncirclesAxis;
            copy.Curvature = Curvature;
            CopyEdgesPathData(copy, reverse, copiedTessellatedSolid, startIndex, endIndex);
            return copy;
        }

        /// <summary>
        /// Adds the specified segment.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="addToEnd">if set to <c>true</c> [add to end].</param>
        /// <param name="dir">if set to <c>true</c> [dir].</param>
        public void Add(BorderSegment segment, bool addToEnd, bool dir)
        {
            if (addToEnd)
                AddEnd(segment, dir);
            else
                AddBegin(segment, dir);
        }

        /// <summary>
        /// Adds the end.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="dir">if set to <c>true</c> [dir].</param>
        public void AddEnd(BorderSegment segment, bool dir)
        {
            Segments.Add(segment);
            SegmentDirections.Add(dir);
            //If addToEnd (true) == dir, iterate forward. Otherwise, add in reverse order.
            if (dir)
            {
                foreach (var (edge, dir2) in segment)
                {
                    EdgeList.Add(edge);
                    DirectionList.Add(dir2 == dir);
                }
            }
            else
            {
                for (var i = segment.Count - 1; i >= 0; i--)
                {
                    var (edge, dir2) = segment[i];
                    EdgeList.Add(edge);
                    DirectionList.Add(dir2 == dir);
                }
            }

        }

        /// <summary>
        /// Adds the begin.
        /// </summary>
        /// <param name="segment">The segment.</param>
        /// <param name="dir">if set to <c>true</c> [dir].</param>
        public void AddBegin(BorderSegment segment, bool dir)
        {
            Segments.Insert(0, segment);
            SegmentDirections.Insert(0, dir);
            //If addToEnd (false) == dir, iterate forward. Otherwise, insert in reverse order.
            if (!dir)
            {
                foreach (var (edge, dir2) in segment)
                {
                    EdgeList.Insert(0, edge);
                    DirectionList.Insert(0, dir2 == dir);
                }
            }
            else
            {
                for (var i = segment.Count - 1; i >= 0; i--)
                {
                    var (edge, dir2) = segment[i];
                    EdgeList.Insert(0, edge);
                    DirectionList.Insert(0, dir2 == dir);
                }
            }
        }

        /// <summary>
        /// Updates the is closed.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public new bool UpdateIsClosed()
        {
            if (Segments == null)
                IsClosed = false;
            else if (Segments.Count == 1 && Segments[0].IsClosed)
                IsClosed = true;
            else
            {
                var lastVertex = SegmentDirections[^1] ? Segments[^1].LastVertex : Segments[^1].FirstVertex;
                IsClosed = (FirstVertex == lastVertex);
            }
            return IsClosed;
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public new void Clear()
        {
            Segments.Clear();
            SegmentDirections.Clear();
            EdgeList.Clear();
            DirectionList.Clear();
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if [contains] [the specified path]; otherwise, <c>false</c>.</returns>
        internal bool Contains(EdgePath path)
        {
            return Segments.Contains(path);
        }

        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        /// <value>The first vertex.</value>
        [JsonIgnore]
        public new Vertex FirstVertex
        {
            get
            {
                if (SegmentDirections == null || SegmentDirections.Count == 0) return null;
                return SegmentDirections[0] ? Segments[0].FirstVertex : Segments[0].LastVertex;
            }
        }

        /// <summary>
        /// Gets the last vertex.
        /// </summary>
        /// <value>The last vertex.</value>
        [JsonIgnore]
        public new Vertex LastVertex
        {
            get
            {
                if (SegmentDirections == null || SegmentDirections.Count == 0) return null;
                // the following condition uses the edge direction of course, but it also checks
                // to see if it is closed because - if it is closed then the last and the first would
                // be repeated. To prevent this, we quickly check that if direction is true use To
                // unless it's closed, then use From (True-False), go through the four cases in your mind
                // and you see that this checks out.
                if (SegmentDirections[^1] != IsClosed) return Segments[^1].LastVertex;
                else return Segments[^1].FirstVertex;
            }
        }
    }
}