// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt
// Last Modified On : 03-18-2015
// ***********************************************************************
// <copyright file="Vertex.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using Newtonsoft.Json;
using OldTVGL.Voxelization;

namespace OldTVGL
{
    /// <summary>
    ///     The 3D vertex can connect to any number of faces and edges. It inherits from the
    ///     MIConvexhull IVertex interface.
    /// </summary>
    public class Vertex : TessellationBaseClass, IVertex
    {
        /// <summary>
        ///     Prevents a default instance of the <see cref="Vertex" /> class from being created.
        /// </summary>
        private Vertex()
        {
        }

        /// <summary>
        ///     Copies this instance. Does not include reference lists.
        /// </summary>
        /// <returns>Vertex.</returns>
        public Vertex Copy()
        {
            return new Vertex
            {
                Curvature = Curvature,
                PartOfConvexHull = PartOfConvexHull,
                Edges = new List<Edge>(),
                Faces = new List<PolygonalFace>(),
                Position = (double[])Position.Clone(),
                IndexInList = IndexInList
            };
        }

        /// <summary>
        ///     Defines vertex curvature
        /// </summary>
        public void DefineCurvature()
        {
            if (Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                Curvature = CurvatureType.Undefined;
            else if (Edges.All(e => e.Curvature == CurvatureType.SaddleOrFlat))
                Curvature = CurvatureType.SaddleOrFlat;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Convex))
                Curvature = CurvatureType.Concave;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Concave))
                Curvature = CurvatureType.Convex;
            else Curvature = CurvatureType.SaddleOrFlat;
        }

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="indexInListOfVertices">The index in list of vertices.</param>
        public Vertex(double[] position, int indexInListOfVertices)
            : this(position)
        {
            IndexInList = indexInListOfVertices;
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        public Vertex(double[] position)
        {
            if (position.Length == 3) Position = position;
            else if (position.Length > 3) Position = position.Take(3).ToArray();
            else
                throw new ArgumentException(
                    "Vertex constructor given a position array with fewer than 3 values (all vertices must be 3D).");
            Edges = new List<Edge>();
            Faces = new List<PolygonalFace>();
            IndexInList = -1;
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the position.
        /// </summary>
        /// <value>The position.</value>
        public double[] Position { get; set; }

        /// <summary>
        ///     Gets the x.
        /// </summary>
        /// <value>The x.</value>
        [JsonIgnore]
        public double X
        {
            get { return Position[0]; }
        }

        /// <summary>
        ///     Gets the y.
        /// </summary>
        /// <value>The y.</value>
        [JsonIgnore]
        public double Y
        {
            get { return Position[1]; }
        }

        /// <summary>
        ///     Gets the z.
        /// </summary>
        /// <value>The z.</value>
        [JsonIgnore]
        public double Z
        {
            get { return Position[2]; }
        }

        /// <summary>
        ///     Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        [JsonIgnore]
        public List<Edge> Edges { get; private set; }

        /// <summary>
        ///     Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        [JsonIgnore]
        public List<PolygonalFace> Faces { get; private set; }

        #endregion
    }
}