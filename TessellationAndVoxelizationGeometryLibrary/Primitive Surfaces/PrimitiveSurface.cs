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

            var outerEdges = new HashSet<Edge>();
            var innerEdges = new HashSet<Edge>();
            foreach (var face in Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (innerEdges.Contains(edge)) continue;
                    if (!outerEdges.Contains(edge)) outerEdges.Add(edge);
                    else
                    {
                        innerEdges.Add(edge);
                        outerEdges.Remove(edge);
                    }
                }
            }
            OuterEdges = new List<Edge>(outerEdges);
            InnerEdges = new List<Edge>(innerEdges);
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
        public PrimitiveSurfaceType Type { get; internal set; }

        /// <summary>
        ///     Gets the area.
        /// </summary>
        /// <value>The area.</value>
        public double Area { get; internal set; }

        /// <summary>
        ///     Gets or sets the polygonal faces.
        /// </summary>
        /// <value>The polygonal faces.</value>
        public List<PolygonalFace> Faces { get; internal set; }

        /// <summary>
        ///     Gets or sets the transformation.
        /// </summary>
        /// <value>The transformation.</value>
        public double[,] Transformation { get; internal set; }

        /// <summary>
        ///     Gets the inner edges.
        /// </summary>
        /// <value>The inner edges.</value>
        public List<Edge> InnerEdges { get; internal set; }


        /// <summary>
        ///     Gets the outer edges.
        /// </summary>
        /// <value>The outer edges.</value>
        public List<Edge> OuterEdges { get; internal set; }

        /// <summary>
        ///     Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        public List<Vertex> Vertices { get; internal set; }

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
                if (OuterEdges.Contains(e))
                {
                    OuterEdges.Remove(e);
                    InnerEdges.Add(e);
                }
                else OuterEdges.Add(e);
            }
            Faces.Add(face);
        }
    }
}