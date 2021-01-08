// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : campmatt
// Created          : 01-04-2021
//
// Last Modified By : campmatt
// Last Modified On : 01-04-2021
// ***********************************************************************
// <copyright file="PrimitiveSurfaceBorder.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurfaceBorder.
    /// </summary>
    public class SurfaceBorder
    {
        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public I2DCurve Curve { get; set; }
        /// <summary>
        /// Gets or sets the plane.
        /// </summary>
        /// <value>The plane.</value>
        public Plane Plane { get; set; }
        /// <summary>
        /// Gets the edges and direction.
        /// </summary>
        /// <value>The edges and direction.</value>
        public List<(Edge edge, bool dir)> EdgesAndDirection { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [encircles axis].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool EncirclesAxis { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [border is closed].
        /// </summary>
        /// <value><c>true</c> if [border is closed]; otherwise, <c>false</c>.</value>
        public bool IsClosed { get; set; }
        /// <summary>
        /// Gets the number points.
        /// </summary>
        /// <value>The number points.</value>
        public int NumPoints
        {
            get
            {
                if (IsClosed) return EdgesAndDirection.Count;
                return EdgesAndDirection.Count + 1;
            }
        }
        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        /// <value>The first vertex.</value>
        public Vertex FirstVertex
        {
            get
            {
                if (EdgesAndDirection[0].dir) return EdgesAndDirection[0].edge.From;
                else return EdgesAndDirection[0].edge.To;
            }
        }
        /// <summary>
        /// Gets the last vertex.
        /// </summary>
        /// <value>The last vertex.</value>
        public Vertex LastVertex
        {
            get
            {
                var lastEdgeAndDir = EdgesAndDirection[^1];
                var firstVertex = FirstVertex;
                // the following condition uses the edge direction of course, but it also checks
                // to see if it is closed because - if it is closed then the last and the first would
                // be repeated. To prevent this, we quickly check that if direction is true use To
                // unless it's closed, then use From (True-False), go through the four cases in your mind
                // and you see that this checks out.
                if (lastEdgeAndDir.dir != IsClosed) return lastEdgeAndDir.edge.To;
                else return lastEdgeAndDir.edge.From;
            }

        }

        public Polygon AsPolygon
        {
            get
            {
                if (_polygon == null)
                    _polygon = new Polygon(GetVertices().ProjectTo2DCoordinates(Plane.Normal, out _));
                return _polygon;
            }
        }
        private Polygon _polygon;
        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <returns>IEnumerable&lt;Vertex&gt;.</returns>
        public IEnumerable<Vertex> GetVertices()
        {
            if (!EdgesAndDirection.Any()) yield break;
            foreach (var edgeAndDir in EdgesAndDirection)
            {
                if (edgeAndDir.Item2) yield return edgeAndDir.Item1.From;
                else yield return edgeAndDir.Item1.To;
            }
            if (!IsClosed) //only add the last one if not a closed loop since it would otherwise
                           // repeat the first point
            {
                var lastEdgeAndDir = EdgesAndDirection[^1];
                if (lastEdgeAndDir.Item2) yield return lastEdgeAndDir.Item1.To;
                else yield return lastEdgeAndDir.Item1.From;
            }
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="SurfaceBorder"/> class from being created.
        /// </summary>
        public SurfaceBorder()
        {
            EdgesAndDirection = new List<(Edge, bool)>();
        }

        public void AddEnd(Edge edge, bool dir)
        {
            EdgesAndDirection.Add((edge, dir));
        }
        public void AddBegin(Edge edge, bool dir)
        {
            EdgesAndDirection.Insert(0, (edge, dir));
        }


        public SurfaceBorder Copy(bool reverse = false, TessellatedSolid copiedTessellatedSolid = null)
        {
            var copy = new SurfaceBorder();
            if (Plane != null)
                copy.Plane = new Plane(Plane.DistanceToOrigin, Plane.Normal);
            copy.Curve = Curve;
            copy.IsClosed = IsClosed;
            copy.EncirclesAxis = EncirclesAxis;
            if (copiedTessellatedSolid == null)
            {
                foreach (var eAndA in EdgesAndDirection)
                    if (reverse)
                        copy.EdgesAndDirection.Insert(0, (eAndA.edge, !eAndA.dir));
                    else
                        copy.EdgesAndDirection.Add((eAndA.edge, eAndA.dir));
            }
            else
            {
                foreach (var eAndA in EdgesAndDirection)
                    if (reverse)
                        copy.EdgesAndDirection.Insert(0, (copiedTessellatedSolid.Edges[eAndA.edge.IndexInList], !eAndA.dir));
                    else
                        copy.EdgesAndDirection.Add((copiedTessellatedSolid.Edges[eAndA.edge.IndexInList], eAndA.dir));
            }
            return copy;
        }
    }
}
