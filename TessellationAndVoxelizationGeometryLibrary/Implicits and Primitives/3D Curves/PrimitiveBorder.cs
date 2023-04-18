// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : campmatt
// Created          : 01-04-2021
//
// Last Modified By : campmatt
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PrimitiveBorder.cs" company="Design Engineering Lab">
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
    /// Class PrimitiveSurfaceBorder.
    /// </summary>
    [JsonObject]
    public class PrimitiveBorder : EdgePath
    {
        /// <summary>
        /// Default instance of the <see cref="PrimitiveBorder" />.
        /// </summary>
        public PrimitiveBorder() : base()
        {
            Segments = new List<BorderSegment>();
            SegmentDirections = new List<bool>();
        }

        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
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
        public IEnumerable<PrimitiveSurface> AdjacentPrimitives()
        {
            foreach (var segment in Segments)
            {
                yield return segment.OwnedPrimitive == OwnedPrimitive ? segment.OtherPrimitive : segment.OwnedPrimitive;
            }
        }

        /// <summary>
        /// Returns all primitives that share a vertex with this border
        /// </summary>
        /// <returns>IEnumerable&lt;PrimitiveSurface&gt;.</returns>
        public IEnumerable<PrimitiveSurface> AdjacentPrimitivesByVertex()
        {
            var adjacents = new HashSet<PrimitiveSurface>();
            foreach (var segment in Segments)
                foreach (var vertex in segment.GetVertices())
                    foreach (var face in vertex.Faces)
                        if (face.BelongsToPrimitive != OwnedPrimitive)
                            adjacents.Add(face.BelongsToPrimitive);
            return adjacents;
        }

        /// <summary>
        /// Gets or sets the plane error.
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [encircles axis].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool EncirclesAxis { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [border is fully concave].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyConcave { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [border is fully concave].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyConvex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [border is flush/flat - not concave or convex].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyFlush { get; set; }

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
        /// Initializes a new instance of the <see cref="PrimitiveBorder"/> class.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="surface">The surface.</param>
        /// <param name="path">The path.</param>
        /// <param name="curveError">The curve error.</param>
        /// <param name="surfError">The surf error.</param>
        public PrimitiveBorder(ICurve curve, PrimitiveSurface surface, EdgePath path, double curveError,
            double surfError) : this(curve, surface, path.EdgeList, path.DirectionList, curveError, surfError)
        {
            EdgeList = path.EdgeList;
            DirectionList = path.DirectionList;
            Curve = curve;
            OwnedPrimitive = surface;
            CurveError = curveError;
            PlaneError = surfError;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveBorder"/> class.
        /// </summary>
        /// <param name="curve">The curve.</param>
        /// <param name="surface">The surface.</param>
        /// <param name="edges">The edges.</param>
        /// <param name="directions">The directions.</param>
        /// <param name="curveError">The curve error.</param>
        /// <param name="surfError">The surf error.</param>
        public PrimitiveBorder(ICurve curve, PrimitiveSurface surface, List<Edge> edges, List<bool> directions,
            double curveError, double surfError)
        {
            EdgeList = edges;
            DirectionList = directions;
            Curve = curve;
            OwnedPrimitive = surface;
            CurveError = curveError;
            PlaneError = surfError;
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
        /// Copies the specified copied surface.
        /// </summary>
        /// <param name="copiedSurface">The copied surface.</param>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>PrimitiveBorder.</returns>
        public PrimitiveBorder Copy(PrimitiveSurface copiedSurface, bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
            int startIndex = 0, int endIndex = -1)
        {
            var copy = new PrimitiveBorder();
            copy.OwnedPrimitive = copiedSurface;
            copy.Curve = Curve;
            copy.EncirclesAxis = EncirclesAxis;
            copy.FullyFlush = FullyFlush;
            copy.FullyConcave = FullyConcave;
            copy.FullyConvex = FullyConvex;
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
        private void AddEnd(BorderSegment segment, bool dir)
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
        private void AddBegin(BorderSegment segment, bool dir)
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