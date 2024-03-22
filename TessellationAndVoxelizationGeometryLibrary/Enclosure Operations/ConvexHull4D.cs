
using System;
using System.Collections.Generic;
using System.Linq;

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
        public double tolerance { get; public init; }

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
        public ConvexHullFace4D OwnedFace { get; set; }
        public ConvexHullFace4D OtherFace { get; set; }
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
    }

    public class ConvexHullFace4D
    {
        //public ConvexHullFace4D(Vertex4D vertex1, Vertex4D vertex2, Vertex4D vertex3, Vertex4D vertex4, Vector4 planeNormal)
        //{
        //    Normal = planeNormal;
        //    InteriorVertices = new List<Vertex4D>();
        //}
        public ConvexHullFace4D(Vertex4D vertex1, Vertex4D vertex2, Vertex4D vertex3, Vertex4D vertex4, Vector4 knownUnderPoint)
        {
            InteriorVertices = new List<Vertex4D>();
            Normal = DetermineNormal(vertex1.Coordinates, vertex2.Coordinates, vertex3.Coordinates, vertex4.Coordinates, knownUnderPoint);
        }

        private Vector4 DetermineNormal(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3, Vector4 f)
        {
            // 1. get two in-plane vectors that are not parallel
            var (v1, v2) = GetInPlaneBasis(p0, p1, p2, p3);
            // 2. get the normal
            var q = f - p0;
            var v1Dx = v1.Dot(q) * v1;
            var v2Dx = v2.Dot(q) * v2;

            var normal = -f + 2 * p0 + v1Dx + v2Dx;
            return normal.Normalize();
        }

        private static (Vector4, Vector4) GetInPlaneBasis(Vector4 p0, Vector4 p1, Vector4 p2, Vector4 p3)
        {
            var d1 = (p1 - p0).Normalize();
            var d2 = (p2 - p0).Normalize();
            var d3 = (p3 - p0).Normalize();
            var dot_12 = d1.Dot(d2);
            var dot_13 = d1.Dot(d3);
            var dot_23 = d2.Dot(d3);
            if (dot_12 < dot_13 && dot_12 < dot_23)
                return (d1.Normalize(), d2.Normalize());
            else if (dot_13 < dot_12 && dot_13 < dot_23)
                return (d1.Normalize(), d3.Normalize());
            return (d2.Normalize(), d3.Normalize());
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

        internal void AddEdge(Edge4D connectingEdge)
        {
            throw new NotImplementedException();
        }
    }
}
