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
        //public readonly List<CHFace> cHFaces = new List<CHFace>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<ConvexHullFace4D> Faces = new List<ConvexHullFace4D>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public readonly List<Edge4D> Edges = new List<Edge4D>();


    }

    public class Edge4D
    {
        public Edge4D(Vertex4D vertexA, Vertex4D vertexB, Vertex4D vertexC, ConvexHullFace4D ownedFace, ConvexHullFace4D otherFace)
        {
            A = vertexA;
            B = vertexB;
            C = vertexC;
            OwnedFace = ownedFace;
            OtherFace = otherFace;
        }

        public Vertex4D A { get; set; }
        public Vertex4D B { get; set; }
        public Vertex4D C { get; set; }
        public ConvexHullFace4D OwnedFace { get; set; }
        public ConvexHullFace4D OtherFace { get; set; }

        internal ConvexHullFace4D AdjacentFace(ConvexHullFace4D face)
        {
            if (face == OwnedFace) return OtherFace;
            if (face == OtherFace) return OwnedFace;
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
        public ConvexHullFace4D(Vertex4D vertex1, Vertex4D vertex2, Vertex4D vertex3, Vertex4D vertex4, Vector4 knownUnderPoint)
        {
            InteriorVertices = new List<Vertex4D>();
            Normal = DetermineNormal(vertex1.Coordinates, vertex2.Coordinates, vertex3.Coordinates, vertex4.Coordinates, knownUnderPoint);
        }

        private Vector4 DetermineNormal(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, Vector4 knownUnderPoint)
        {
            // following the approach at https://www.mathwizurd.com/linalg/2018/11/15/find-a-normal-vector-to-a-hyperplane
            // f is the vector that should have a positive dot product with the normal. It's used to flip the normal if necessary.
            var f = p0 - knownUnderPoint;
            var d1 = (p1 - p0);
            var d2 = (p2 - p0);
            var d3 = (p3 - p0);

            if (d1.X.IsNegligible())
            {
                if (d2.X.IsNegligible())
                {
                    if (d3.X.IsNegligible())
                    {  // all vectors are parallel to the x-axis so the normal is the x-axis
                        if (f.X < 0) return -Vector4.UnitX;
                        return Vector4.UnitX;
                    }
                    // move d1 to the bottom and d3 to the top
                    (d1, d3) = (d3, d1);
                }
                // swap d1 and d2
                (d1, d2) = (d2, d1);
            }
            // d1.X is not negligible so we can use it to eliminate the x component of d2 and d3
            d2 = d1.X * d2 - d2.X * d1;
            d3 = d1.X * d3 - d3.X * d1;

            if (d2.Y.IsNegligible())
            {
                if (d3.Y.IsNegligible())
                {
                    //throw new Exception("Two vectors are parallel to the y-axis. this is reparable by considering w, but...");
                    if (d2.Z.IsNegligible())
                    {
                        if (d3.Z.IsNegligible())
                            throw new Exception("There is a linear dependence with two of the four points defining this hyperplane.");
                        //(d2, d3) = (d3, d2);
                        // d2.Y is negligible, but d2.Z is not, so we can use it to eliminate the z component of d3
                        // setting y to -1, z = 0 and w = 0
                        var normalXY = new Vector4(d1.Y / d1.X, -1, 0, 0);
                        if (f.Dot(normalXY) > 0) return normalXY.Normalize();
                        else return -normalXY.Normalize();
                    }
                    // swap d2 and d3 so that d2.Y is not negligible
                    (d2, d3) = (d3, d2);
                }
            }
            // d2.Y is not negligible so we can use it to eliminate the y component of d3
            d3 = d2.Y * d3 - d3.Y * d2;
            // setting w to -1
            var z = d3.W / d3.Z;
            var y = (d2.W - d2.Z * z) / d2.Y;
            var x = (d1.W - d1.Y * y - d1.Z * z) / d1.X;
            var normal = new Vector4(x, y, z, -1);
            if (f.Dot(normal) > 0) return normal.Normalize();
            else return -normal.Normalize();
        }


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

        internal Vertex4D VertexOppositeEdge(Edge4D edge)
        {
            if (edge == ABC) return D;
            if (edge == ABD) return C;
            if (edge == ACD) return B;
            if (edge == BCD) return A;
            throw new Exception("The edge is not part of this face.");
        }
    }
}
