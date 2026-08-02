using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public partial class ConvexHull4D
    {
        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public Vertex4D[] Vertices { get; private set; }
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public ConvexHullFace4D[] Tetrahedra { get; private set; }

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public Edge4D[] Faces { get; private set; }
        /// <summary>
        /// Gets the vertex pairs.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public VertexPair4D[] VertexPairs { get; private set; }


    }

    /// <summary>
    /// Represents a pair of vertices in 4D space, which is defined by two vertices and a list of convex hull faces (tetrahedra) that share the edge formed by the two vertices.
    /// </summary>
    public class VertexPair4D
    {
        /// <summary>
        /// The first vertex of the pair.
        /// </summary>
        public Vertex4D Vertex1 { get; internal init; }
        /// <summary>
        /// The second vertex of the pair.
        /// </summary>
        public Vertex4D Vertex2 { get; internal init; }

        /// <summary>
        /// Gets the list of convex hull faces (tetrahedra) that share the edge formed by the two vertices.
        /// </summary>
        public List<ConvexHullFace4D> Tetrahedra { get; } = new List<ConvexHullFace4D>();
    }

    /// <summary>
    /// Represents an edge in 4D space, which is defined by three vertices and is shared by two convex hull faces (tetrahedra).
    /// </summary>
    public class Edge4D
    {
        internal Edge4D(Vertex4D vertexA, Vertex4D vertexB, Vertex4D vertexC, ConvexHullFace4D ownedFace, ConvexHullFace4D otherFace)
        {
            A = vertexA;
            B = vertexB;
            C = vertexC;
            OwnedTetra = ownedFace;
            OtherTetra = otherFace;
        }

        /// <summary>
        /// Vertex A of the edge (in 4D edges will have 3 vertices)
        /// </summary>
        public Vertex4D A { get; internal set; }
        /// <summary>
        /// Vertex B of the edge (in 4D edges will have 3 vertices)
        /// </summary>
        public Vertex4D B { get; internal set; }
        /// <summary>
        /// Vertex C of the edge (in 4D edges will have 3 vertices)
        /// </summary>
        public Vertex4D C { get; internal set; }
        /// <summary>
        /// Gets the convex hull face (tetrahedron) that owns this edge. 
        /// In a convex hull, each edge is shared by two faces, and one of them is considered the owner of the edge.
        /// </summary>
        public ConvexHullFace4D OwnedTetra { get; internal set; }
        /// <summary>
        /// Gets the convex hull face (tetrahedron) that is adjacent to this edge but does not own it.
        /// </summary>
        public ConvexHullFace4D OtherTetra { get; internal set; }

        internal ConvexHullFace4D AdjacentTetra(ConvexHullFace4D face)
        {
            if (face == OwnedTetra) return OtherTetra;
            if (face == OtherTetra) return OwnedTetra;
            throw new Exception("The face is not adjacent to this edge.");
        }
    }

    /// <summary>
    /// Represents a vertex in 4D space, which is defined by its coordinates and an index in a list of vertices.
    /// </summary>
    public class Vertex4D: TessellationBaseClass
    {
        internal Vertex4D(Vector4 vector4, int i)
        {
            Coordinates = vector4;
            IndexInList = i;
        }

        /// <summary>
        /// Gets the coordinates of the vertex in 4D space.
        /// </summary>
        public Vector4 Coordinates { get; }

        /// <summary>
        /// Gets the X coordinate of the vertex in 4D space.
        /// </summary>
        public double X => Coordinates[0];
        /// <summary>
        /// Gets the Y coordinate of the vertex in 4D space.
        /// </summary>
        public double Y => Coordinates[1];
        /// <summary>
        /// Gets the Z coordinate of the vertex in 4D space.
        /// </summary>
        public double Z => Coordinates[2];
        /// <summary>
        /// Gets the W coordinate of the vertex in 4D space.
        /// </summary>
        public double W => Coordinates[3];

        /// <summary>
        /// The curvature type is not defined for the 4D edge
        /// </summary>
        public override CurvatureType Curvature { get => throw new NotImplementedException(); internal set => throw new NotImplementedException(); }

        /// <summary>
        /// The normal vector is not defined for the 4D edge
        /// </summary>
        public override Vector3 Normal => throw new NotImplementedException();
    }

    /// <summary>
    /// Represents a face of a convex hull in 4D space, which is defined by four vertices and four edges. Each face has a normal vector and may have a peak vertex that is not part of the face but is used to determine the orientation of the face.
    /// </summary>
    public class ConvexHullFace4D
    {
        /// <summary>
        /// Vertex A
        /// </summary>
        public required Vertex4D A { get; init; }

        /// <summary>
        /// Vertex B
        /// </summary>
        public required Vertex4D B { get; init; }

        /// <summary>
        /// Vertex C
        /// </summary>
        public required Vertex4D C { get; init; }

        /// <summary>
        /// Vertex D
        /// </summary>
        public required Vertex4D D { get; init; }

        /// <summary>
        /// Gets or sets the edge formed by vertices A, B, and C.
        /// </summary>
        public Edge4D ABC { get; set; }

        /// <summary>
        /// Gets or sets the edge formed by vertices A, B, and D.
        /// </summary>
        public Edge4D ABD { get; set; }

        /// <summary>
        /// Gets or sets the edge formed by vertices A, C, and D.
        /// </summary>
        public Edge4D ACD { get; set; }

        /// <summary>
        /// Gets or sets the edge formed by vertices B, C, and D.
        /// </summary>
        public Edge4D BCD { get; set; }
        internal Vertex4D peakVertex { get; set; }
        internal double peakDistance { get; set; }

        /// <summary>
        /// Gets or sets the normal vector of the face, which is used to determine the orientation of the face in 4D space.
        /// </summary>
        public required Vector4 Normal { get; init; }

        /// <summary>
        /// Gets the collection of edges that form the faces of the tetrahedron.
        /// </summary>
        public IEnumerable<Edge4D> Faces
        {
            get
            {
                yield return ABC;
                yield return ABD;
                yield return ACD;
                yield return BCD;
            }
        }

        internal Vector4 GetNormal(bool tryToRepair)
        {
            if (!tryToRepair || !Normal.IsNull())
                return Normal;
            var normal = Vector4.Zero;
            var validNeighborCount = 0;
            foreach (var face in Faces)
            {
                if (face == null) continue;
                var other = face.OwnedTetra == this ? face.OtherTetra : face.OwnedTetra;
                if (other == null) continue;
                if (other.Normal.IsNegligible()) continue;
                normal += other.Normal;
                validNeighborCount++;
            }
            return normal.Normalize();
        }

        /// <summary>
        /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
        /// of the convex hull
        /// </summary>
        public List<Vertex4D> InteriorVertices { get; } = new List<Vertex4D>();

        /// <summary>
        /// Gets or sets a value indicating whether this face has been visited during the convex hull construction process.
        /// </summary>
        public bool Visited { get; set; }

        internal void AddEdge(Edge4D edge)
        {
            var AIsAttached = (A == edge.A || A == edge.B || A == edge.C);
            var BIsAttached = (B == edge.A || B == edge.B || B == edge.C);
            var CIsAttached = (C == edge.A || C == edge.B || C == edge.C);
            var DIsAttached = (D == edge.A || D == edge.B || D == edge.C);
            if (!AIsAttached && BIsAttached && CIsAttached && DIsAttached)
                BCD = edge;
            else if (AIsAttached && !BIsAttached && CIsAttached && DIsAttached)
                ACD = edge;
            else if (AIsAttached && BIsAttached && !CIsAttached && DIsAttached)
                ABD = edge;
            else if (AIsAttached && BIsAttached && CIsAttached && !DIsAttached)
                ABC = edge;
            else throw new Exception("The edge is not part of this face.");
        }

        internal Vertex4D VertexOppositeFace(Edge4D edge)
        {
            if (edge == ABC) return D;
            if (edge == ABD) return C;
            if (edge == ACD) return B;
            if (edge == BCD) return A;
            throw new Exception("The edge is not part of this face.");
        }
    }
}
