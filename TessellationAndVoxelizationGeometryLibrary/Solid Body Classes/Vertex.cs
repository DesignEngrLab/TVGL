// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt
// Created          : 02-27-2015
//
// Last Modified By : Matt
// Last Modified On : 03-18-2015
// ***********************************************************************
// <copyright file="Vertex.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;


namespace TVGL
{
    /// <summary>
    /// The 3D vertex can connect to any number of faces and edges. It inherits from the
    /// MIConvexhull IVertex interface.
    /// </summary>
    public class Vertex : IVertex
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex"/> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="indexInListOfVertices">The index in list of vertices.</param>
        public Vertex(double[] position, int indexInListOfVertices)
            : this(position)
        {
            IndexInList = indexInListOfVertices;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        public Vertex(double[] position)
        {
            Position = position;
            Edges = new List<Edge>();
            Faces = new List<PolygonalFace>();
            IndexInList = -1;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>The position.</value>
        public double[] Position { get; set; }

        /// <summary>
        /// Gets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X { get { return Position[0]; } }
        /// <summary>
        /// Gets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y { get { return Position[1]; } }
        /// <summary>
        /// Gets the z.
        /// </summary>
        /// <value>The z.</value>
        public double Z { get { return Position[2]; } }
        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        public List<Edge> Edges { get; private set; }
        /// <summary>
        /// Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        public List<PolygonalFace> Faces { get; private set; }
        /// <summary>
        /// Gets the curvature at the point.
        /// </summary>
        /// <value>The point curvature.</value>
        public CurvatureType VertexCurvature { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value><c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.</value>
        public bool PartofConvexHull { get; internal set; }

        /// <summary>
        /// Gets the index in list.
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; internal set; }

        /// <summary>
        /// Gets or sets an arbitrary ReferenceIndex to track vertex
        /// </summary>
        /// <value>The reference index.</value>
        public int ReferenceIndex { get; set; }
        #endregion
        /// <summary>
        /// Prevents a default instance of the <see cref="Vertex"/> class from being created.
        /// </summary>
        private Vertex() { }
        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Vertex.</returns>
        public Vertex Copy()
        {
            return new Vertex
            {
                VertexCurvature = VertexCurvature,
                PartofConvexHull = PartofConvexHull,
                Edges = new List<Edge>(),
                Faces = new List<PolygonalFace>(),
                Position = (double[])Position.Clone(),
                IndexInList = IndexInList
            };
        }

        public void DefineVertexCurvature()
        {
            var edges = new List<Edge>(Edges);
            if (Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                VertexCurvature = CurvatureType.Undefined;
            else if (!Edges.Any(e => e.Curvature != CurvatureType.SaddleOrFlat))
                VertexCurvature = CurvatureType.SaddleOrFlat;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Convex))
                VertexCurvature = CurvatureType.Concave;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Concave))
                VertexCurvature = CurvatureType.Convex;
            else VertexCurvature = CurvatureType.SaddleOrFlat;
        } 
    }
}
