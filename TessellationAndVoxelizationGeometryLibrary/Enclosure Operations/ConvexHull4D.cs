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
        /// The volume of the Convex Hull.
        /// </summary>
        public double tolerance { get; init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<Vertex4D> Vertices = new List<Vertex4D>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<ConvexHullFace4D> Tetrahedra = new List<ConvexHullFace4D>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public readonly List<Edge4D> Faces = new List<Edge4D>();
        /// <summary>
        /// Gets the vertex pairs.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public readonly List<VertexPair> VertexPairs = new List<VertexPair>();


    }
    public class VertexPair
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
    public class Vertex4D
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
    }

    public class ConvexHullFace4D
    {
        static int counter = 0;
        public ConvexHullFace4D(Vertex4D vertex1, Vertex4D vertex2, Vertex4D vertex3, Vertex4D vertex4, Vector4 knownUnderPoint)
        {
            ID = counter++;
            A = vertex1;
            B = vertex2;
            C = vertex3;
            D = vertex4;
            InteriorVertices = new List<Vertex4D>();
            Normal = DetermineNormal(vertex1.Coordinates, vertex2.Coordinates, vertex3.Coordinates, vertex4.Coordinates, knownUnderPoint);
        }

        private Vector4 DetermineNormal(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, Vector4 knownUnderPoint)
        {
            // following the approach at https://www.mathwizurd.com/linalg/2018/11/15/find-a-normal-vector-to-a-hyperplane
            // f is the vector that should have a positive dot product with the normal. It's used to flip the normal if necessary.
            var f = p0 - knownUnderPoint;
            var d1 = p1 - p0;
            var d2 = p2 - p1;
            var d3 = p3 - p2;

            // This was generated using Mathematica
            var nx = d1.W * (d2.Z * d3.Y - d2.Y * d3.Z)
                 + d1.Z * (d2.Y * d3.W - d2.W * d3.Y)
                 + d1.Y * (d2.W * d3.Z - d2.Z * d3.W);
            var ny = d1.W * (d2.X * d3.Z - d2.Z * d3.X)
                     + d1.Z * (d2.W * d3.X - d2.X * d3.W)
                     + d1.X * (d2.Z * d3.W - d2.W * d3.Z);
            var nz = d1.W * (d2.Y * d3.X - d2.X * d3.Y)
                     + d1.Y * (d2.X * d3.W - d2.W * d3.X)
                     + d1.X * (d2.W * d3.Y - d2.Y * d3.W);
            var nw = d1.Z * (d2.X * d3.Y - d2.Y * d3.X)
                     + d1.Y * (d2.Z * d3.X - d2.X * d3.Z)
                     + d1.X * (d2.Y * d3.Z - d2.Z * d3.Y);
            var normal = new Vector4(nx, ny, nz, nw).Normalize();
            if (f.Dot(normal) < 0) normal = -normal;
            //Console.WriteLine(normal.Dot(d1) + " " + normal.Dot(d2) + " " + normal.Dot(d3));
            if (normal.IsNull()) return f.Normalize();
            return normal;
        }

        public int ID;
        public Vertex4D A { get; set; }
        public Vertex4D B { get; set; }
        public Vertex4D C { get; set; }
        public Vertex4D D { get; set; }
        public Edge4D ABC { get; set; }
        public Edge4D ABD { get; set; }
        public Edge4D ACD { get; set; }
        public Edge4D BCD { get; set; }
        public Vertex4D peakVertex { get; set; }
        public double peakDistance { get; set; }
        public Vector4 Normal { get; private set; }

        /// <summary>
        /// Gets the collection of vertices that are on the boundary of the convex hull but are not actively effecting the boundary representation
        /// of the convex hull
        /// </summary>
        public List<Vertex4D> InteriorVertices { get; set; }
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
