// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Vertex.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using TVGL.ConvexHullDetails;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// The 3D vertex can connect to any number of faces and edges. It inherits from the
    /// MIConvexhull IPoint interface.
    /// </summary>
    public sealed class Vertex : TessellationBaseClass, IPoint3D, IPoint
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="Vertex" /> class from being created.
        /// </summary>
        private Vertex()
        {
        }

        /// <summary>
        /// Copies this instance. Does not include reference lists.
        /// </summary>
        /// <returns>Vertex.</returns>
        public Vertex Copy()
        {
            return new Vertex
            {
                _curvature = Curvature,
                PartOfConvexHull = PartOfConvexHull,
                Edges = new List<Edge>(),
                Faces = new List<TriangleFace>(),
                Coordinates = new Vector3(Coordinates.X, Coordinates.Y, Coordinates.Z),
                IndexInList = IndexInList
            };
        }

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <param name="indexInListOfVertices">The index in list of vertices.</param>
        public Vertex(Vector3 position, int indexInListOfVertices)
            : this(position)
        {
            IndexInList = indexInListOfVertices;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vertex" /> class.
        /// </summary>
        /// <param name="position">The position.</param>
        public Vertex(Vector3 position)
        {
            Coordinates = position;
            Edges = new List<Edge>();
            Faces = new List<TriangleFace>();
            IndexInList = -1;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>The position.</value>
        public Vector3 Coordinates { get; set; }

        /// <summary>
        /// Gets the x.
        /// </summary>
        /// <value>The x.</value>
        [JsonIgnore]
        public double X => Coordinates.X;

        /// <summary>
        /// Gets the y.
        /// </summary>
        /// <value>The y.</value>
        [JsonIgnore]
        public double Y => Coordinates.Y;

        /// <summary>
        /// Gets the z.
        /// </summary>
        /// <value>The z.</value>
        [JsonIgnore]
        public double Z => Coordinates.Z;

        /// <summary>
        /// Gets the edges.
        /// </summary>
        /// <value>The edges.</value>
        [JsonIgnore]
        public List<Edge> Edges { get; private set; }

        /// <summary>
        /// Gets the faces.
        /// </summary>
        /// <value>The faces.</value>
        [JsonIgnore]
        public List<TriangleFace> Faces { get; private set; }

        /// <summary>
        /// Gets the normal.
        /// </summary>
        /// <value>The normal.</value>
        [JsonIgnore]
        public override Vector3 Normal
        {
            get
            {
                if (_normal.IsNull()) DetermineNormal();
                return _normal;
            }
        }

        /// <summary>
        /// The normal
        /// </summary>
        [JsonIgnore] //cannot serialize null values.
        private Vector3 _normal = Vector3.Null;

        /// <summary>
        /// Determines the normal.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private void DetermineNormal()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        public override CurvatureType Curvature
        {
            get
            {
                if (_curvature == CurvatureType.Undefined) DefineCurvature();
                return _curvature;
            }
        }

        public double this[int i] => Coordinates[i];

        /// <summary>
        /// The curvature
        /// </summary>
        private CurvatureType _curvature = CurvatureType.Undefined;

        /// <summary>
        /// Defines vertex curvature
        /// </summary>
        private void DefineCurvature()
        {
            if (Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                _curvature = CurvatureType.Undefined;
            else if (Edges.All(e => e.Curvature == CurvatureType.SaddleOrFlat))
                _curvature = CurvatureType.SaddleOrFlat;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Convex))
                _curvature = CurvatureType.Concave;
            else if (Edges.Any(e => e.Curvature != CurvatureType.Concave))
                _curvature = CurvatureType.Convex;
            else _curvature = CurvatureType.SaddleOrFlat;
        }

        public bool IsNull() => double.IsNaN(X) || double.IsNaN(Y);
        #endregion Properties
    }
}