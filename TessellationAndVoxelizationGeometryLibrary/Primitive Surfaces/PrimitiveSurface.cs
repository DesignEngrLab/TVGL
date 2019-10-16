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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using TVGL.Voxelization;

namespace TVGL
{
    /// <summary>
    ///     Class PrimitiveSurface.
    /// </summary>
    [XmlInclude(typeof(Flat))]
    [XmlInclude(typeof(Cone))]
    [XmlInclude(typeof(Cylinder))]
    [XmlInclude(typeof(Sphere))]
    [XmlInclude(typeof(Torus))]
    [XmlInclude(typeof(DenseRegion))]
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
        [XmlIgnore]
        public PrimitiveSurfaceType Type { get; protected set; }

        /// <summary>
        ///     Gets the area.
        /// </summary>
        /// <value>The area.</value>
        [XmlIgnore]
        public double Area { get; protected set; }

        /// <summary>
        ///     Gets or sets the polygonal faces.
        /// </summary>
        /// <value>The polygonal faces.</value>
        [XmlIgnore]
        public HashSet<PolygonalFace> Faces { get; protected set; }

        public string FaceIndices
        {
            get { return string.Join(",", Faces.Select(f => f.IndexInList)); }
            set { _faceIndices = value; }
        }


        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [XmlIgnore]
        public List<Vertex> Vertices { get; protected set; }

        public string VertexIndices
        {
            get { return string.Join(",", Vertices.Select(v => v.IndexInList)); }
            set { _vertexIndices = value; }
        }


        /// <summary>
        ///     Gets the inner edges.
        /// </summary>
        /// <value>The inner edges.</value>
        [XmlIgnore]
        public List<Edge> InnerEdges
        {
            get
            {
                if (_innerEdges == null) DefineInnerOuterEdges();
                return _innerEdges;
            }
        }


        public string InnerEdgeIndices
        {
            get { return string.Join(",", InnerEdges.Select(e => e.IndexInList)); }
            set { _innerEdgeIndices = value; }
        }

        /// <summary>
        ///     Gets the outer edges.
        /// </summary>
        /// <value>The outer edges.</value>
        [XmlIgnore]
        public List<Edge> OuterEdges
        {
            get
            {
                if (_outerEdges == null) DefineInnerOuterEdges();
                return _outerEdges;
            }
        }

        public string OuterEdgeIndices
        {
            get { return string.Join(",", OuterEdges.Select(e => e.IndexInList)); }
            set { _outerEdgeIndices = value; }
        }
        private List<Edge> _innerEdges;
        private List<Edge> _outerEdges;
        private string _faceIndices;
        private string _innerEdgeIndices;
        private string _outerEdgeIndices;
        private string _vertexIndices;

        private void DefineInnerOuterEdges()
        {
            var outerEdgeHash = new HashSet<Edge>();
            var innerEdgeHash = new HashSet<Edge>();
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
        public abstract void Transform(double[,] transformMatrix);

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
            var stringList = _faceIndices.Split(',');
            var listLength = stringList.Length;
            for (int i = 0; i < listLength; i++)
            {
                var face = ts.Faces[int.Parse(stringList[i])];
                Faces.Add(face);
                face.BelongsToPrimitive = this;
            }

            Vertices = new List<Vertex>();
            stringList = _vertexIndices.Split(',');
            listLength = stringList.Length;
            for (int i = 0; i < listLength; i++)
                Vertices.Add(ts.Vertices[int.Parse(stringList[i])]);

            if (!string.IsNullOrWhiteSpace(_innerEdgeIndices))
            {
                _innerEdges = new List<Edge>();
                stringList = _innerEdgeIndices.Split(',');
                listLength = stringList.Length;
                for (int i = 0; i < listLength; i++)
                    _innerEdges.Add(ts.Edges[int.Parse(stringList[i])]);
            }
            if (!string.IsNullOrWhiteSpace(_outerEdgeIndices))
            {
                _outerEdges = new List<Edge>();
                stringList = _outerEdgeIndices.Split(',');
                listLength = stringList.Length;
                for (int i = 0; i < listLength; i++)
                    _outerEdges.Add(ts.Edges[int.Parse(stringList[i])]);
            }
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