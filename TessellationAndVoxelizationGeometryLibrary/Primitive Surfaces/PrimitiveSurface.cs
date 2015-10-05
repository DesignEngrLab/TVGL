using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{

    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    public abstract class PrimitiveSurface
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitiveSurface"/> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        protected PrimitiveSurface(IEnumerable<PolygonalFace> faces)
        {
            Faces = faces.ToList();
            Area = Faces.Sum(f => f.Area);

            var outerEdges = new HashSet<Edge>();
            var innerEdges = new HashSet<Edge>();
            foreach (var face in Faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (innerEdges.Contains(edge)) continue;
                    else if (!outerEdges.Contains(edge)) outerEdges.Add(edge);
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
        /// Initializes a new instance of the <see cref="PrimitiveSurface"/> class.
        /// </summary>
        protected PrimitiveSurface()
        {
        }

        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>
        /// The area.
        /// </value>
        public double Area { get; internal set; }

        /// <summary>
        /// Gets or sets the polygonal faces.
        /// </summary>
        /// <value>
        /// The polygonal faces.
        /// </value>
        public List<PolygonalFace> Faces { get; internal set; }

        /// <summary>
        /// Gets or sets the transformation.
        /// </summary>
        /// <value>
        /// The transformation.
        /// </value>
        public double[,] Transformation { get; internal set; }

        /// <summary>
        /// Gets the inner edges.
        /// </summary>
        /// <value>
        /// The inner edges.
        /// </value>
        public List<Edge> InnerEdges { get; internal set; }


        /// <summary>
        /// Gets the outer edges.
        /// </summary>
        /// <value>
        /// The outer edges.
        /// </value>
        public List<Edge> OuterEdges { get; internal set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>
        /// The vertices.
        /// </value>
        public List<Vertex> Vertices { get; internal set; }

        public abstract Boolean IsNewMemberOf(PolygonalFace face);

        public virtual void UpdateWith(PolygonalFace face)
        {
            Area += face.Area;
            foreach (var v in face.Vertices)
                if (!Vertices.Contains(v))
                    Vertices.Add(v);
            foreach (var e in face.Edges)
            {
                if (InnerEdges.Contains(e)) continue; //throw new Exception("edge of new face is already an inner edge of surface, WTF?!");
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
