using System;
using System.Collections.Generic;
using StarMathLib;

namespace TVGL
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
        /// <param name="doublyLinkedFaces"></param>
        /// <param name="doublyLinkedVertices"></param>
        public Edge(Vertex fromVertex, Vertex toVertex, PolygonalFace ownedFace, PolygonalFace otherFace,
            Boolean doublyLinkedVertices = true)
        {
            From = fromVertex;
            To = toVertex;
            _ownedFace = ownedFace;
            _otherFace = otherFace;
            if (_ownedFace != null) _ownedFace.Edges.Add(this);
            if (_otherFace != null) _otherFace.Edges.Add(this);
            if (doublyLinkedVertices)
            {
                fromVertex.Edges.Add(this);
                toVertex.Edges.Add(this);
            }
            Vector = new[]
            {
                (To.Position[0] - From.Position[0]),
                (To.Position[1] - From.Position[1]),
                (To.Position[2] - From.Position[2])
            };
            Length =
                Math.Sqrt(Vector[0] * Vector[0] + Vector[1] * Vector[1] + Vector[2] * Vector[2]);
            DefineInternalEdgeAngle();
        }
        /// <summary>
        ///     Initializes a new instance of the <see cref="Edge" /> class.
        /// </summary>
        /// <param name="fromVertex">From vertex.</param>
        /// <param name="toVertex">To vertex.</param>
        /// <param name="ownedFace">The face.</param>
        /// <param name="otherFace">The other face.</param>
        /// <param name="doublyLinkedFaces"></param>
        /// <param name="doublyLinkedVertices"></param>
        public Edge(Vertex fromVertex, Vertex toVertex, Boolean doublyLinkedVertices)
        {
            From = fromVertex;
            To = toVertex;
            if (doublyLinkedVertices)
            {
                fromVertex.Edges.Add(this);
                toVertex.Edges.Add(this);
            }
            Vector = new[]
            {
                (To.Position[0] - From.Position[0]),
                (To.Position[1] - From.Position[1]),
                (To.Position[2] - From.Position[2])
            };
            Length =
                Math.Sqrt(Vector[0] * Vector[0] + Vector[1] * Vector[1] + Vector[2] * Vector[2]);
            DefineInternalEdgeAngle();
        }

        #endregion

        private Edge()
        {
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
        public readonly double Length;

        /// <summary>
        ///     Gets the vector.
        /// </summary>
        /// <value>
        ///     The vector.
        /// </value>
        public readonly double[] Vector;

        private PolygonalFace _otherFace;
        private PolygonalFace _ownedFace;

        /// <summary>
        ///     Gets the owned face (the face in which the from-to direction makes sense
        ///     - that is, produces the proper cross-product normal).
        /// </summary>
        /// <value>
        ///     The owned face.
        /// </value>
        public PolygonalFace OwnedFace
        {
            get { return _ownedFace; }
            internal set
            {
                if (_ownedFace == value) return;
                _ownedFace = value;
                DefineInternalEdgeAngle();
            }
        }

        /// <summary>
        ///     Gets the other face (the face in which the from-to direction doesn not
        ///     make sense- that is, produces the negative cross-product normal).
        /// </summary>
        /// <value>
        ///     The other face.
        /// </value>
        public PolygonalFace OtherFace
        {
            get { return _otherFace; }
            internal set
            {
                if (_otherFace == value) return;
                _otherFace = value;
                DefineInternalEdgeAngle();
            }
        }

        /// <summary>
        ///     Gets the internal angle in radians.
        /// </summary>
        /// <value>
        ///     The internal angle.
        /// </value>
        public double InternalAngle { get; private set; }

        /// <summary>
        ///     Gets the curvature of the surface.
        /// </summary>
        /// <value>
        ///     The curvature of the surface.
        /// </value>
        public CurvatureType Curvature { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether [is part of the convex hull].
        /// </summary>
        /// <value>
        ///     <c>true</c> if [is part of the convex hull]; otherwise, <c>false</c>.
        /// </value>
        public Boolean PartofConvexHull { get; internal set; }


        /// <summary>
        ///     Defines the edge angle.
        /// </summary>
        /// <param name="edges">The edges.</param>
        private void DefineInternalEdgeAngle()
        {
            /* this is a tricky function. What we need to do is take the dot-product of the normals.
             * which will give the cos(theta). Calling inverse cosine will result in a value from 0 to
             * pi, but is the edge convex or concave? It is convex if the crossproduct of the normals is 
             * in the same direction as the edge vector (dot product is positive). But we need to know 
             * which face-normal goes first in the cross product calculation as this will change the 
             * resulting direction. The one to go first is the one that "owns" the edge. What I mean by
             * own is that the from-to of the edge makes sense in the counter-clockwise prediction of 
             * the face normal. For one face the from-to will be incorrect (normal facing inwards) - 
             * in some geometry approaches this is solved by the concept of half-edges. Here we will 
             * just re-order the two faces referenced in the edge so that the first is the one that 
             * owns the edge...the face for which the direction makes sense, and the second face will 
             * need to reverse the edge vector to make it work out in a proper counter-clockwise loop 
             * for that face. */
            if (_ownedFace == _otherFace || _ownedFace == null || _otherFace == null)
            {
                InternalAngle = double.NaN;
                Curvature = CurvatureType.Undefined;
                return;
            }
            var ownedFaceToIndex = _ownedFace.Vertices.IndexOf(To);
            var ownedFaceNextIndex = (ownedFaceToIndex + 1 == _ownedFace.Vertices.Count) ? 0 : ownedFaceToIndex + 1;

            var nextOwnedFaceVertex = _ownedFace.Vertices[ownedFaceNextIndex];
            var nextEdgeVector = nextOwnedFaceVertex.Position.subtract(To.Position);

            if (Vector.crossProduct(nextEdgeVector).dotProduct(_ownedFace.Normal) < 0)
            {
                /* then switch owned face and opposite face since the predicted normal
                 * is in the wrong direction. When OwnedFace and OppositeFace were defined
                 * it was arbitrary anyway - so this is another by-product of this method - 
                 * correct the owned and opposite faces. */
                var temp = _ownedFace;
                _ownedFace = _otherFace;
                _otherFace = temp;
            }
            var dot = _ownedFace.Normal.dotProduct(_otherFace.Normal, 3);
            if (dot > 1.0 || dot.IsPracticallySame(1.0))
            {
                InternalAngle = Math.PI;
                Curvature = CurvatureType.SaddleOrFlat;
            }
            else
            {
                var cross = _ownedFace.Normal.crossProduct(_otherFace.Normal);
                if (cross.dotProduct(Vector) < 0)
                {
                    InternalAngle = Math.PI + Math.Acos(dot);
                    Curvature = CurvatureType.Concave;
                }
                else
                {
                    InternalAngle = Math.PI - Math.Acos(dot);
                    Curvature = CurvatureType.Convex;
                }
            }
            //Debug.WriteLine("angle = " + (InternalAngle * (180 / Math.PI)).ToString() + "; " + SurfaceIs.ToString());
        }
        #endregion
    }
}