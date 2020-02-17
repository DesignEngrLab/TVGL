// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 04-18-2016
// ***********************************************************************
// <copyright file="PrimitiveSurface.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TVGL.Numerics;
using TVGL.Voxelization;

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
            Type = PrimitiveSurfaceType.Unknown;
            Faces = new HashSet<PolygonalFace>(faces);
            foreach (var face in faces)
                face.BelongsToPrimitive = this;
            Area = Faces.Sum(f => f.Area);
            Vertices = Faces.SelectMany(f => f.Vertices).Distinct().ToList();
        }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="PrimitiveSurface" /> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }

        /// <summary>
        ///     Gets the Type of primitive surface
        /// </summary>
        /// <value>The type.</value>
        public PrimitiveSurfaceType Type { get; protected set; }

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
            set { _faceIndices = value; }
        }


        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public List<Vertex> Vertices { get; protected set; }

        public int[] VertexIndices
        {
            get
            {
                if (Vertices != null)
                    return Vertices.Select(v => v.IndexInList).ToArray();
                return Array.Empty<int>();
            }
            set { _vertexIndices = value; }
        }


        /// <summary>
        ///     Gets the inner edges.
        /// </summary>
        /// <value>The inner edges.</value>
        [JsonIgnore]
        public List<Edge> InnerEdges
        {
            get
            {
                if (_innerEdges == null) DefineInnerOuterEdges();
                return _innerEdges;
            }
        }


        public int[] InnerEdgeIndices
        {
            get
            {
                if (Faces != null)
                    return InnerEdges.Select(e => e.IndexInList).ToArray();
                return Array.Empty<int>();
            }
            set { _innerEdgeIndices = value; }
        }

        /// <summary>
        ///     Gets the outer edges.
        /// </summary>
        /// <value>The outer edges.</value>
        [JsonIgnore]
        public List<Edge> OuterEdges
        {
            get
            {
                if (_outerEdges == null) DefineInnerOuterEdges();
                return _outerEdges;
            }
        }

        public int[] OuterEdgeIndices
        {
            get
            {
                if (Faces != null)
                    return OuterEdges.Select(e => e.IndexInList).ToArray();
                return Array.Empty<int>();
            }
            set { _outerEdgeIndices = value; }
        }
        private List<Edge> _innerEdges;
        private List<Edge> _outerEdges;
        private int[] _faceIndices;
        private int[] _innerEdgeIndices;
        private int[] _outerEdgeIndices;
        private int[] _vertexIndices;

        private void DefineInnerOuterEdges()
        {
            var outerEdgeHash = new HashSet<Edge>();
            var innerEdgeHash = new HashSet<Edge>();
            if (Faces!=null)
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
            _outerEdges = outerEdgeHash.ToList();
            _innerEdges = innerEdgeHash.ToList();
        }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public abstract void Transform(Matrix4x4 transformMatrix);

        /// <summary>
        ///     Checks if face should be a member of this surface
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns><c>true</c> if [is new member of] [the specified face]; otherwise, <c>false</c>.</returns>
        public abstract bool IsNewMemberOf(PolygonalFace face);

        /// <summary>
        ///     Updates surface by adding face
        /// </summary>
        /// <param name="face">The face.</param>
        public virtual void UpdateWith(PolygonalFace face)
        {
            Area += face.Area;
            foreach (var v in face.Vertices.Where(v => !Vertices.Contains(v)))
                Vertices.Add(v);
            foreach (var e in face.Edges.Where(e => !InnerEdges.Contains(e)))
            {
                if (_outerEdges.Contains(e))
                {
                    _outerEdges.Remove(e);
                    _innerEdges.Add(e);
                }
                else _outerEdges.Add(e);
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
            Vertices = new List<Vertex>();
            foreach (var i in _vertexIndices)
                Vertices.Add(ts.Vertices[i]);

            _innerEdges = new List<Edge>();
            foreach (var i in _innerEdgeIndices)
                _innerEdges.Add(ts.Edges[i]);

            _outerEdges = new List<Edge>();
            foreach (var i in _outerEdgeIndices)
                _outerEdges.Add(ts.Edges[i]);
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

        /// <summary>
        /// Takes in a list of edges and returns their list of loops for edges and vertices 
        /// The order of the output loops are not considered (i.e., they may be "reversed"),
        /// since no face normal information is used.
        /// </summary>
        /// <param name="edges"></param>
        /// <returns></returns>
        public static (bool allLoopsClosed, List<List<Edge>> edgeLoops, List<List<Vertex>> vertexLoops) GetLoops(HashSet<Edge> outerEdges, bool canModifyTheInput)
        {
            //Use a boolean canModifyTheInput, so that we can save time creating a hashset if the user allows it to be mutated. 
            var edges = canModifyTheInput ? outerEdges : new HashSet<Edge>(outerEdges);

            //loop through the edges to form loops 
            var allLoopsClosed = true;
            var loops = new List<List<Vertex>>();
            var edgeLoops = new List<List<Edge>>();
            while (edges.Any())
            {
                var currentEdge = edges.First();
                edges.Remove(currentEdge);
                var startVertex = currentEdge.From;
                var loop = new List<Vertex> { startVertex };
                var edgeLoop = new List<Edge> { currentEdge };
                bool isClosed = false;
                var previousVertex = startVertex;
                while (!isClosed)
                {
                    //The To/From order cannot be used, since it is only correct for the face the edge belongs to.
                    //So, add whichever vertex of the edge has no already been added
                    var currentVertex = previousVertex == currentEdge.From ? currentEdge.To : currentEdge.From;
                    //Check if we have wrapped around to close the loop
                    isClosed = currentVertex == startVertex;
                    if (isClosed) continue; //Break the while loop.

                    //Otherwise, add the new vertex and find the next edge
                    loop.Add(currentVertex);
                    var possibleEdges = currentVertex.Edges;
                    Edge nextEdge = null;
                    foreach (var edge in possibleEdges)
                    {
                        if (!edges.Contains(edge)) continue;
                        edges.Remove(edge);
                        nextEdge = edge;
                        break;
                    }
                    if (nextEdge == null)
                    {
                        allLoopsClosed = false; //This loop does not close
                        break;
                    }
                    edgeLoop.Add(nextEdge);
                    previousVertex = currentVertex;
                    currentEdge = nextEdge;
                }
                edgeLoops.Add(edgeLoop);
                loops.Add(loop);
            }
            return (allLoopsClosed, edgeLoops, loops);
        }
    }
}