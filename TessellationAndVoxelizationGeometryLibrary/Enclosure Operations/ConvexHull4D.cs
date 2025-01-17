﻿using System;
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
    public class VertexPair4D
    {
        public required Vertex4D Vertex1 { get; init; }
        public required Vertex4D Vertex2 { get; init; }
        public List<ConvexHullFace4D> Tetrahedra { get; } = new List<ConvexHullFace4D>();
    }


    public class Edge4D
    {
        public Edge4D(Vertex4D vertexA, Vertex4D vertexB, Vertex4D vertexC, ConvexHullFace4D ownedFace, ConvexHullFace4D otherFace)
        {
            A = vertexA;
            B = vertexB;
            C = vertexC;
            OwnedTetra = ownedFace;
            OtherTetra = otherFace;
        }

        public Vertex4D A { get; set; }
        public Vertex4D B { get; set; }
        public Vertex4D C { get; set; }
        public ConvexHullFace4D OwnedTetra { get; set; }
        public ConvexHullFace4D OtherTetra { get; set; }

        internal ConvexHullFace4D AdjacentTetra(ConvexHullFace4D face)
        {
            if (face == OwnedTetra) return OtherTetra;
            if (face == OtherTetra) return OwnedTetra;
            throw new Exception("The face is not adjacent to this edge.");
        }
    }
    public class Vertex4D: TessellationBaseClass
    {
        public Vertex4D(Vector4 vector4, int i)
        {
            Coordinates = vector4;
            IndexInList = i;
        }

        public Vector4 Coordinates { get; }
        public int IndexInList { get; }
        public double X => Coordinates[0];
        public double Y => Coordinates[1];
        public double Z => Coordinates[2];
        public double W => Coordinates[3];

        public override CurvatureType Curvature { get => throw new NotImplementedException(); internal set => throw new NotImplementedException(); }

        public override Vector3 Normal => throw new NotImplementedException();
    }

    public class ConvexHullFace4D
    {
        public required Vertex4D A { get; init; }
        public required Vertex4D B { get; init; }
        public required Vertex4D C { get; init; }
        public required Vertex4D D { get; init; }
        public Edge4D ABC { get; set; }
        public Edge4D ABD { get; set; }
        public Edge4D ACD { get; set; }
        public Edge4D BCD { get; set; }
        public Vertex4D peakVertex { get; set; }
        public double peakDistance { get; set; }
        public required Vector4 Normal { get; init; }

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
                if (other.Normal.IsNull()) continue;
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
