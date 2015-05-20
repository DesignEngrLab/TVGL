using System;
using System.Collections.Generic;

namespace TVGL.Tessellation
{
    /// <summary>
    ///     The straight-line edge class. It connects to two nodes and lies between two faces.
    /// </summary>
    public class Edge
    {
        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Edge" /> class.
        /// </summary>
        /// <param name="fromVertex">From vertex.</param>
        /// <param name="toVertex">To vertex.</param>
        /// <param name="ownedFace">The face.</param>
        /// <param name="otherFace">The other face.</param>
        public Edge(Vertex fromVertex, Vertex toVertex, PolygonalFace ownedFace, PolygonalFace otherFace)
        {
            From = fromVertex;
            fromVertex.Edges.Add(this);
            To = toVertex;
            toVertex.Edges.Add(this);
            OwnedFace = ownedFace;
            OtherFace = otherFace;
        }

        #endregion

        private Edge()
        {
        }
        internal void DefineVectorAndLength()
        {
            Vector = new[]
            {
                (To.Position[0] - From.Position[0]),
                (To.Position[1] - From.Position[1]),
                (To.Position[2] - From.Position[2])
            };
            Length =
                Math.Sqrt(Vector[0] * Vector[0] + Vector[1] * Vector[1] + Vector[2] * Vector[2]);
        }

        /// <summary>
        ///     Others the vertex.
        /// </summary>
        /// <param name="v">The v.</param>
        /// <returns>Vertex.</returns>
        /// <exception cref="System.Exception">OtherVertex: Vertex thought to connect to edge, but it doesn't.</exception>
        public Vertex OtherVertex(Vertex v)
        {
            if (v == To) return From;
            if (v == From) return To;
            throw new Exception("OtherVertex: Vertex thought to connect to edge, but it doesn't.");
        }

        #region Properties

        /// <summary>
        ///     Gets the From Vertex.
        /// </summary>
        /// <value>
        ///     From.
        /// </value>
        public Vertex From { get; internal set; }

        /// <summary>
        ///     Gets the To Vertex.
        /// </summary>
        /// <value>
        ///     To.
        /// </value>
        public Vertex To { get; internal set; }

        /// <summary>
        ///     Gets the length.
        /// </summary>
        /// <value>
        ///     The length.
        /// </value>
        public double Length { get; internal set; }

        /// <summary>
        ///     Gets the vector.
        /// </summary>
        /// <value>
        ///     The vector.
        /// </value>
        public double[] Vector { get; internal set; }

        /// <summary>
        ///     Gets the owned face (the face in which the from-to direction makes sense
        ///     - that is, produces the proper cross-product normal).
        /// </summary>
        /// <value>
        ///     The owned face.
        /// </value>
        public PolygonalFace OwnedFace { get; internal set; }

        /// <summary>
        ///     Gets the other face (the face in which the from-to direction doesn not
        ///     make sense- that is, produces the negative cross-product normal).
        /// </summary>
        /// <value>
        ///     The other face.
        /// </value>
        public PolygonalFace OtherFace { get; internal set; }

        /// <summary>
        ///     Gets the internal angle in radians.
        /// </summary>
        /// <value>
        ///     The internal angle.
        /// </value>
        public double InternalAngle { get; internal set; }

        /// <summary>
        ///     Gets the curvature of the surface.
        /// </summary>
        /// <value>
        ///     The curvature of the surface.
        /// </value>
        public CurvatureType Curvature { get; internal set; }

        /// <summary>
        ///     Gets a value indicating whether [is part of the convex hull].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [is part of the convex hull]; otherwise, <c>false</c>.
        /// </value>
        public Boolean PartofConvexHull { get; internal set; }


        /// <summary>
        ///     A dictionary with 5 groups of Flat, Cylinder, Sphere, Flat to Curve and Sharp Edge and their
        ///     probabilities.
        /// </summary>
        /// <value>
        ///     Group Probability
        /// </value>
        internal Dictionary<int, double> CatProb { get; set; }

        #endregion
    }
}