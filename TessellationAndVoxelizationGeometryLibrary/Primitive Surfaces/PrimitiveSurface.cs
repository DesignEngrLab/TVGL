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
            Faces = faces.ToList();
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
        public List<PolygonalFace> Faces { get; protected set; }

        public int[] FaceIndices
        {
            get
            {
                if (Faces != null)
                    return Faces.Select(f => f.IndexInList).ToArray();
                return new int[0];
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
                return new int[0];
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
                return new int[0];
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
                return new int[0];
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
            Faces = new List<PolygonalFace>();
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
    }
}