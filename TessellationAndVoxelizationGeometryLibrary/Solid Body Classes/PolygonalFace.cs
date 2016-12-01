// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="PolygonalFace.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

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
        ///     Defines the face curvature. Depends on DefineEdgeAngle
        /// </summary>
        public void DefineFaceCurvature()
        {
            if (Edges.Any(e => e == null || e.Curvature == CurvatureType.Undefined))
                Curvature = CurvatureType.Undefined;
            else if (Edges.All(e => e.Curvature != CurvatureType.Concave))
                Curvature = CurvatureType.Convex;
            else if (Edges.All(e => e.Curvature != CurvatureType.Convex))
                Curvature = CurvatureType.Concave;
            else Curvature = CurvatureType.SaddleOrFlat;
        }

        /// <summary>
        ///     Copies this instance. Does not include reference lists.
        /// </summary>
        /// <returns>PolygonalFace.</returns>
        public PolygonalFace Copy()
        {
            return new PolygonalFace
            {
                Area = Area,
                Center = (double[])Center.Clone(),
                Curvature = Curvature,
                Color = Color,
                PartOfConvexHull = PartOfConvexHull,
                Edges = new List<Edge>(),
                Normal = (double[])Normal.Clone(),
                Vertices = new List<Vertex>()
            };
        }

        //Set new normal and area. 
        //References are assumed to be the same.
        /// <summary>
        ///     Updates normal, vertex order, and area
        /// </summary>
        public void Update()
        {
            bool reverseVertexOrder;
            Normal = DetermineNormal(this.Vertices, out reverseVertexOrder, Normal);
            if (reverseVertexOrder) Vertices.Reverse();
            Area = DetermineArea();
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
            Edges[index] = edge;
        }

        /// <summary>
        ///     Others the edge.
        /// </summary>
        /// <param name="thisVertex">The this vertex.</param>
        /// <param name="willAcceptNullAnswer">if set to <c>true</c> [will accept null answer].</param>
        /// <returns>Edge.</returns>
        internal Edge OtherEdge(Vertex thisVertex, bool willAcceptNullAnswer = false)
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
        internal Vertex OtherVertex(Edge thisEdge, bool willAcceptNullAnswer = false)
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
        internal Vertex OtherVertex(Vertex v1, Vertex v2, bool willAcceptNullAnswer = false)
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
        internal Vertex NextVertexCCW(Vertex v1)
        {
            var index = Vertices.IndexOf(v1);
            if (index < 0) return null; //Vertex is not part of this face
            return index == Vertices.Count - 1 ? Vertices[0] : Vertices[index + 1];
        }

        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="color">The color.</param>
        public PolygonalFace(double[] normal, Color color)
            : this(normal)
        {
            Color = color;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="normal">The normal.</param>
        public PolygonalFace(double[] normal)
            : this()
        {
            Normal = normal;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        public PolygonalFace()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }


        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(IEnumerable<Vertex> vertices, bool connectVerticesBackToFace = true)
            : this(vertices, null, connectVerticesBackToFace)
        {           
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">A guess for the normal vector.</param>
        /// <param name="connectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(IEnumerable<Vertex> vertices, double[] normal, bool connectVerticesBackToFace = true)
            : this()
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                if (connectVerticesBackToFace)
                    v.Faces.Add(this);
            }
            var centerX = Vertices.Average(v => v.X);
            var centerY = Vertices.Average(v => v.Y);
            var centerZ = Vertices.Average(v => v.Z);
            Center = new[] { centerX, centerY, centerZ };
            bool reverseVertexOrder;
            Normal = DetermineNormal(Vertices, out reverseVertexOrder, normal);
            if (reverseVertexOrder) Vertices.Reverse();
            Area = DetermineArea();
        }

        /// <summary>
        ///     Determines the area.
        /// </summary>
        /// <returns>System.Double.</returns>
        internal double DetermineArea()
        {
            var area = 0.0;
            for (var i = 2; i < Vertices.Count; i++)
            {
                var edge1 = Vertices[1].Position.subtract(Vertices[0].Position);
                var edge2 = Vertices[2].Position.subtract(Vertices[0].Position);
                // the area of each triangle in the face is the area is half the magnitude of the cross product of two of the edges
                area += Math.Abs(edge1.crossProduct(edge2).dotProduct(Normal)) / 2;
            }
            //If not a number, the triangle is actually a straight line. Set the area = 0, and let repair function fix this.
            return double.IsNaN(area) ? 0.0 : area;
        }

        /// <summary>
        /// Determines the normal.
        /// </summary>
        /// <param name="reverseVertexOrder">if set to <c>true</c> [reverse vertex order].</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">The normal.</param>
        /// <returns>System.Double[].</returns>
        internal static double[] DetermineNormal(List<Vertex> vertices, out bool reverseVertexOrder, double[] normal = null)
        {
            reverseVertexOrder = false;
            var n = vertices.Count;
            if (normal != null && normal.Contains(double.NaN)) normal = null;
            else if (normal != null) normal.normalizeInPlace();
            var edgeVectors = new double[n][];
            var normals = new List<double[]>();
            edgeVectors[0] = vertices[0].Position.subtract(vertices[n - 1].Position);
            for (var i = 1; i < n; i++)
            {
                edgeVectors[i] = vertices[i].Position.subtract(vertices[i - 1].Position);
                var tempCross = edgeVectors[i - 1].crossProduct(edgeVectors[i]).normalize();
                if (!tempCross.Any(double.IsNaN))
                {
                    if (!normals.Any())
                    {
                        // a guess at the normal (usually from an STL file) may be passed
                        // in to this function. If we find that the guess matches this first one
                        // (it's first because normals is empty), then we simply exit with the provided
                        // value.
                        if (normal != null)
                        {
                            if (tempCross.IsPracticallySame(normal, Constants.SameFaceNormalDotTolerance))
                                return normal;
                            if (tempCross.multiply(-1).IsPracticallySame(normal, Constants.SameFaceNormalDotTolerance))
                            {
                                reverseVertexOrder = true;
                                return normal;
                            }
                        }
                    }
                    normals.Add(tempCross);
                }
            }
            var lastCross = edgeVectors[n - 1].crossProduct(edgeVectors[0]).normalize();
            if (!lastCross.Any(double.IsNaN)) normals.Add(lastCross);

            n = normals.Count;
            if (n == 0) // this would happen if the face collapse to a line.
                return new[] { double.NaN, double.NaN, double.NaN };
            // before we just average these normals, let's check that they agree.
            // the dotProductsOfNormals simply takes the dot product of adjacent
            // normals. If they're all close to one, then we can average and return.
            var dotProductsOfNormals = new List<double>();
            dotProductsOfNormals.Add(normals[0].dotProduct(normals[n - 1]));
            for (var i = 1; i < n; i++) dotProductsOfNormals.Add(normals[i].dotProduct(normals[i - 1]));
            // if all are close to one (or at least positive), then the face is a convex polygon. Now,
            // we can simply average and return the answer.
            var isConvex = dotProductsOfNormals.All(x => x > 0);
            if (isConvex)
            {
                var newNormal = normals.Aggregate((current, c) => current.add(c)).normalize();
                // even though the normal provide was wrong above (or nonexistent)
                // we still check it to see if this is the correct direction.
                if (normal == null || newNormal.dotProduct(normal) >= 0) return newNormal;
                // else reverse the order 
                reverseVertexOrder = true;
                return newNormal.multiply(-1);
            }
            // now, the rare case in which the polygon face is not convex, the only .
            if (normal != null)
            {
                //
                // well, here the guess may be useful. We'll insert it into the list of dotProducts
                // and then do a tally
                dotProductsOfNormals[0] = normal.dotProduct(normals[0]);
                dotProductsOfNormals.Insert(0, normal.dotProduct(normals[n - 1]));
            }
            var likeFirstNormal = true;
            var numLikeFirstNormal = 1;
            foreach (var d in dotProductsOfNormals)
            {
                // this tricky little function keeps track of how many are in the same direction
                // as the first one.
                if (d < 0) likeFirstNormal = !likeFirstNormal;
                if (likeFirstNormal) numLikeFirstNormal++;
            }
            // if the majority are like the first one, then use that one (which may have been the guess).
            if (2 * numLikeFirstNormal >= normals.Count) return normals[0].normalize();
            // otherwise, go with the opposite (so long as there isn't an original guess)
            if (normal == null) return normals[0].normalize().multiply(-1);
            //finally, assume the original guess is right, and reverse the order
            reverseVertexOrder = true;
            return normals[0].normalize();
        }
        #endregion

        #region Properties

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<Vertex> Vertices { get; internal set; }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public List<Edge> Edges { get; internal set; }

        /// <summary>
        ///     Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public double[] Center { get; internal set; }

        /// <summary>
        ///     Gets the area.
        /// </summary>
        /// <value>The area.</value>
        public double Area { get; internal set; }

        /// <summary>
        ///     Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color Color { get; set; }

        ///     Gets or sets the unique ID.
        /// </summary>
        /// <value>The ID.</value>
        public string ID { get; set; }

        /// <summary>
        ///     Gets or sets the created in function.
        /// </summary>
        /// <value>The created in function.</value>
        internal string CreatedInFunction { get; set; }

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
        #endregion
    }
}