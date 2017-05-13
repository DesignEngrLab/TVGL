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
        public List<PolygonalFace> Faces { get; protected set; }

        /// <summary>
        ///     Gets the inner edges.
        /// </summary>
        /// <value>The inner edges.</value>
        public List<Edge> InnerEdges
        {
            get
            {
                if (_innerEdges == null) DefineInnerOuterEdges();
                return _innerEdges;
            }
        }
        /// <summary>
        ///     Gets the outer edges.
        /// </summary>
        /// <value>The outer edges.</value>
        public List<Edge> OuterEdges
        {
            get
            {
                if (_outerEdges == null) DefineInnerOuterEdges();
                return _outerEdges;
            }
        }
        private List<Edge> _innerEdges;
        private List<Edge> _outerEdges;



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
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<Vertex> Vertices { get; protected set; }


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
    }
}