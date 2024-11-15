using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// TetraMeshEdge represents an edge in a tetrahedral mesh that can be shared by multiple tetrahedra.
    /// </summary>
    public readonly struct TetraMeshEdge
    {
        public TetraMeshEdge(Vertex to, Vertex from)
        {
            To = to;
            From = from;
            Tetrahedra = new List<Tetrahedron>();
        }

        /// <summary>
        /// The vertex at the start of the edge (although direction is arbitrary).
        /// </summary>
        public readonly Vertex From { get; init; }
        /// <summary>        
        /// The vertex at the end of the edge (although direction is arbitrary).
        /// </summary>
        public readonly Vertex To { get; init; }
        /// <summary>
        /// The list of tetrahedra that share this edge.
        /// </summary>
        public readonly List<Tetrahedron> Tetrahedra { get; }
        /// <summary>
        /// The vector from the From vertex to the To vertex.
        /// </summary>
        public Vector3 Vector => To.Coordinates - From.Coordinates;
    }

    /// <summary>
    /// TetraMeshFace is a triangular face in a tetrahedral mesh between two tetrahedra.
    /// </summary>
    public class TetraMeshFace
    {
        /// <summary>
        /// Vertex 1 of the face.
        /// </summary>
        public required Vertex Vertex1 { get; init; }
        /// <summary>
        /// Vertex 2 of the face.
        /// </summary>
        public required Vertex Vertex2 { get; init; }
        /// <summary>
        /// Vertex 2 of the face.
        /// </summary>
        public required Vertex Vertex3 { get; init; }
        /// <summary>
        /// The tetrahedron that owns this face. The face's normal points away from the owned tetrahedron.
        /// </summary>
        public Tetrahedron OwnedTetra { get; set; }
        /// <summary>
        /// The other tetrahedron of the face. The face's normal points into the other tetrahedron.
        /// </summary>
        public Tetrahedron OtherTetra { get; set; }


        /// <summary>
        /// Enumerates the vertices of the face.
        /// </summary>
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                yield return Vertex1;
                yield return Vertex2;
                yield return Vertex3;
            }
        }
    }

    /// <summary>
    /// Tetrahedron in a tetrahedral mesh where vertices, edges and faces are shared
    /// between neighbors.
    /// </summary>
    public readonly struct Tetrahedron
    {
        /// <summary>
        /// Vertex A of the tetrahedron.
        /// </summary>
        public readonly required Vertex A { get; init; }
        /// <summary>
        /// Vertex B of the tetrahedron.
        /// </summary>
        public readonly required Vertex B { get; init; }
        /// <summary>
        /// Vertex C of the tetrahedron.
        /// </summary>
        public readonly required Vertex C { get; init; }
        /// <summary>
        /// Vertex D of the tetrahedron.
        /// </summary>
        public readonly required Vertex D { get; init; }
        /// <summary>
        /// The face formed by vertices A, B, and C.
        /// </summary>
        public readonly required TetraMeshFace ABC { get; init; }
        /// <summary>
        /// The face formed by vertices A, B, and D.
        /// </summary>
        public readonly required TetraMeshFace ABD { get; init; }
        /// <summary>
        /// The face formed by vertices A, C, and D.
        /// </summary>
        public readonly required TetraMeshFace ACD { get; init; }
        /// <summary>
        /// The face formed by vertices B, C, and D.
        /// </summary>
        public readonly required TetraMeshFace BCD { get; init; }
        public readonly required TetraMeshEdge AB { get; init; }
        public readonly required TetraMeshEdge AC { get; init; }
        public readonly required TetraMeshEdge AD { get; init; }
        public readonly required TetraMeshEdge BC { get; init; }
        public readonly required TetraMeshEdge BD { get; init; }
        public readonly required TetraMeshEdge CD { get; init; }



        public TetraMeshEdge? GetOppositeEdge(TetraMeshEdge edge)
        {
            int code;
            if (edge.From == A)
                code = 0b1000;
            else if (edge.From == B)
                code = 0b0100;
            else if (edge.From == C)
                code = 0b0010;
            else if (edge.From == D)
                code = 0b0001;
            else
                return null;

            if (edge.To == A)
                code += 0b1000;
            else if (edge.To == B)
                code += 0b0100;
            else if (edge.To == C)
                code += 0b0010;
            else if (edge.To == D)
                code += 0b0001;
            else
                return null;

            switch (code)
            {
                case 0b1100:
                    return AB;
                case 0b1010:
                    return AC;
                case 0b1001:
                    return AD;
                case 0b0110:
                    return BC;
                case 0b0101:
                    return BD;
                case 0b0011:
                    return CD;
                default:
                    return null;
            }
        }


        public TetraMeshEdge? GetEdge(Vertex a, Vertex b)
        {
            if (a == A)
            {
                if (b == B) return AB;
                if (b == C) return AC;
                if (b == D) return AD;
            }
            if (a == B)
            {
                if (b == A) return AB;
                if (b == C) return BC;
                if (b == D) return BD;
            }
            if (a == C)
            {
                if (b == A) return AC;
                if (b == B) return BC;
                if (b == D) return CD;
            }
            if (a == D)
            {
                if (b == A) return AD;
                if (b == B) return BD;
                if (b == C) return CD;
            }
            return null;
        }

        /// <summary>
        /// Enumerates the vertices of the tetrahedron.
        /// </summary>
        public IEnumerable<Vertex> Vertices
        {
            get
            {
                yield return A;
                yield return B;
                yield return C;
                yield return D;
            }
        }

        /// <summary>
        /// Enumerates the faces of the tetrahedron.
        /// </summary>
        public IEnumerable<TetraMeshFace> Faces
        {
            get
            {
                yield return ABC;
                yield return ABD;
                yield return ACD;
                yield return BCD;
            }
        }

    }
    public class Delaunay3D
    {
        /// <summary>
        /// Gets the vertices of the Delaunay Tetrahedral Mesh
        /// </summary>
        public Vertex[] Vertices { get; private set; }
        /// <summary>
        /// Gets the tetrahedra of the Delaunay Tetrahedral Mesh
        /// </summary>
        public Tetrahedron[] Tetrahedra { get; private set; }

        /// <summary>       
        /// Gets the faces of the Delaunay Tetrahedral Mesh
        /// </summary>
        public TetraMeshFace[] Faces { get; private set; }
        /// <summary>        
        /// Gets the edges of the Delaunay Tetrahedral Mesh
        /// </summary>
        public TetraMeshEdge[] Edges { get; private set; }

        /// <summary>
        /// Create the Delaunay 3D mesh of tetrahedra from a list/array of vertices.
        /// The vertices will appear in the mesh in a separate list (Vertices) if reuseInputVertices is true.
        /// The vertices will be unaffected.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="delaunay3D"></param>
        /// <param name="reuseInputVertices"></param>
        /// <returns></returns>
        public static bool Create(IList<Vertex> points, out Delaunay3D delaunay3D, bool reuseInputVertices)
        {
            if (!reuseInputVertices) return Create(points, out delaunay3D);
            bool success = CreateInner(points, out var convexHull4D);
            if (!success)
            {
                delaunay3D = null;
                return false;
            }
            delaunay3D = new Delaunay3D();
            delaunay3D.Vertices = convexHull4D.Vertices.Select(v => points[v.IndexInList]).ToArray();
            delaunay3D.Vertices = new Vertex[convexHull4D.Vertices.Length];
            foreach (var v in convexHull4D.Vertices)
                delaunay3D.Vertices[v.IndexInList] = new Vertex(v.X, v.Y, v.Z, v.IndexInList);
            CreateRemainingMeshElementsFromCvxHull4D(delaunay3D, convexHull4D);
            return true;
        }

        /// <summary>
        /// Create the Delaunay 3D mesh of tetrahedra from the points.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="delaunay3D"></param>
        /// <returns></returns>
        public static bool Create(IEnumerable<IVector3D> points, out Delaunay3D delaunay3D)
        {
            bool success = CreateInner(points, out var convexHull4D);
            if (!success)
            {
                delaunay3D = null;
                return false;
            }
            delaunay3D = new Delaunay3D();
            delaunay3D.Vertices = new Vertex[convexHull4D.Vertices.Length];
            foreach (var v in convexHull4D.Vertices)
                delaunay3D.Vertices[v.IndexInList] = new Vertex(v.X, v.Y, v.Z, v.IndexInList);
            CreateRemainingMeshElementsFromCvxHull4D(delaunay3D, convexHull4D);
            return true;
        }

        private static void CreateRemainingMeshElementsFromCvxHull4D(Delaunay3D delaunay3D, 
            ConvexHull4D convexHull4D)
        {
            var baseFactor = delaunay3D.Vertices.Length;
            var baseSqdFactor = (long)baseFactor * (long)baseFactor;
            delaunay3D.Tetrahedra = new Tetrahedron[convexHull4D.Tetrahedra.Length];

            var faceDict = new Dictionary<long, TetraMeshFace>();
            var vpDict = new Dictionary<long, TetraMeshEdge>();
            var k = 0;
            foreach (var tetra in convexHull4D.Tetrahedra)
            {
                var vA = delaunay3D.Vertices[tetra.A.IndexInList];
                var vB = delaunay3D.Vertices[tetra.B.IndexInList];
                var vC = delaunay3D.Vertices[tetra.C.IndexInList];
                var vD = delaunay3D.Vertices[tetra.D.IndexInList];
                (var faceABC, var ownABC) = AddTetraMeshFace(faceDict, vA, vB, vC, vD, baseFactor, baseSqdFactor);
                (var faceABD, var ownABD) = AddTetraMeshFace(faceDict, vA, vB, vD, vC, baseFactor, baseSqdFactor);
                (var faceACD, var ownACD) = AddTetraMeshFace(faceDict, vA, vC, vD, vB, baseFactor, baseSqdFactor);
                (var faceBCD, var ownBCD) = AddTetraMeshFace(faceDict, vB, vC, vD, vA, baseFactor, baseSqdFactor);
                var edgeAB = AddVertexPair(vpDict, vA, vB, baseFactor);
                var edgeAC = AddVertexPair(vpDict, vA, vC, baseFactor);
                var edgeAD = AddVertexPair(vpDict, vA, vD, baseFactor);
                var edgeBC = AddVertexPair(vpDict, vB, vC, baseFactor);
                var edgeBD = AddVertexPair(vpDict, vB, vD, baseFactor);
                var edgeCD = AddVertexPair(vpDict, vC, vD, baseFactor);
                var newTetra = new Tetrahedron
                {
                    A = vA, B = vB, C = vC, D = vD,
                    ABC = faceABC, ABD = faceABD, ACD = faceACD, BCD = faceBCD,
                    AB = edgeAB, AC = edgeAC, AD = edgeAD, BC = edgeBC, BD = edgeBD, CD = edgeCD
                };
                if (ownABC) faceABC.OwnedTetra = newTetra;
                else faceABC.OtherTetra = newTetra;
                if (ownABD) faceABD.OwnedTetra = newTetra;
                else faceABD.OtherTetra = newTetra;
                if (ownACD) faceACD.OwnedTetra = newTetra;
                else faceACD.OtherTetra = newTetra;
                if (ownBCD) faceBCD.OwnedTetra = newTetra;
                else faceBCD.OtherTetra = newTetra;

                edgeAB.Tetrahedra.Add(newTetra);
                edgeAC.Tetrahedra.Add(newTetra);
                edgeAD.Tetrahedra.Add(newTetra);
                edgeBC.Tetrahedra.Add(newTetra);
                edgeBD.Tetrahedra.Add(newTetra);
                edgeCD.Tetrahedra.Add(newTetra);
                delaunay3D.Tetrahedra[k] = newTetra;
            }
            delaunay3D.Faces = faceDict.Values.ToArray();
            delaunay3D.Edges = vpDict.Values.ToArray();
        }

        private static (TetraMeshFace, bool) AddTetraMeshFace(Dictionary<long, TetraMeshFace> faceDict, Vertex a, Vertex b, Vertex c, Vertex other,
            int baseFactor, long baseSqdFactor)
        {
            var id = ConvexHull4D.GetIDForTriple(baseFactor, baseSqdFactor, a.IndexInList, b.IndexInList, c.IndexInList);
            if (faceDict.TryGetValue(id, out var tetraMeshFace))
                return (tetraMeshFace, false);
            var reverse = (b.Coordinates - a.Coordinates).Cross(c.Coordinates - a.Coordinates).Dot(a.Coordinates - other.Coordinates) < 0;
            var newFace = reverse ? new TetraMeshFace { Vertex1 = c, Vertex2 = b, Vertex3 = a } : new TetraMeshFace { Vertex1 = a, Vertex2 = b, Vertex3 = c };
            faceDict.Add(id, newFace);
            return (newFace, true);
        }

        private static TetraMeshEdge AddVertexPair(Dictionary<long, TetraMeshEdge> vertexPairs, Vertex a, Vertex b, long baseFactor)
        {
            var id = a.IndexInList > b.IndexInList ? baseFactor * a.IndexInList + b.IndexInList : baseFactor * b.IndexInList + a.IndexInList;
            if (!vertexPairs.TryGetValue(id, out TetraMeshEdge tetraMeshEdge))
            {
                tetraMeshEdge = a.IndexInList > b.IndexInList ? new TetraMeshEdge(a, b) : new TetraMeshEdge(b, a);
                vertexPairs.Add(id, tetraMeshEdge);
            }
            return tetraMeshEdge;
        }


        private static bool CreateInner(IEnumerable<IVector3D> points, out ConvexHull4D convexHull)
        {
            bool success = false;
            Vertex4D[] vertices;
            if (points is IList<Vector3> pointList)
            {
                vertices = new Vertex4D[pointList.Count];
                for (int i = 0; i < pointList.Count; i++)
                {
                    var p = pointList[i];
                    var w = p.X * p.X + p.Y * p.Y + p.Z * p.Z;
                    vertices[i] = new Vertex4D(new Vector4(p, w), i);
                }
            }
            else
            {
                var vertexList = new List<Vertex4D>();
                var i = 0;
                foreach (var p in points)
                {
                    var w = p.X * p.X + p.Y * p.Y + p.Z * p.Z;
                    vertexList.Add(new Vertex4D(new Vector4(p.X, p.Y, p.Z, w), i++));
                }
                vertices = vertexList.ToArray();
            }
            success = ConvexHull4D.Create(vertices, out convexHull);
            return success;
        }
    }
}