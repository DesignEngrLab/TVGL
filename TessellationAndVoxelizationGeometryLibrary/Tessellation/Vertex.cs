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
using MIConvexHull;
using System.Collections.Generic;


namespace TVGL.Tessellation
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
        public CurvatureType PointCurvature { get; internal set; }
        /// <summary>
        /// Gets the curvature by considering the connecting edges.
        /// </summary>
        /// <value>The global curve.</value>
        public CurvatureType EdgeCurvature { get; internal set; }
        /// <summary>
        /// Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value><c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.</value>
        public Boolean PartofConvexHull { get; internal set; }

        /// <summary>
        /// Gets the index in list.
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; internal set; }
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
                EdgeCurvature = EdgeCurvature,
                PartofConvexHull = PartofConvexHull,
                PointCurvature = PointCurvature,
                Edges = new List<Edge>(),
                Faces = new List<PolygonalFace>(),
                Position = (double[])Position.Clone(),
                IndexInList = IndexInList
            };
        }
    }

}
