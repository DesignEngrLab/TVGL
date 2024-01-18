// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 01-18-2024
//
// Last Modified By : --
// Last Modified On : --
// ***********************************************************************
// <copyright file="ConvexHull3D.cs" company="Design Engineering Lab">
//     2024
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// The Convex Hull of a Tesselated Solid
    /// </summary>
    public class ConvexHull3D : Solid
    {
        public bool IsMaximal=>double.IsNaN(tolerance);
        /// <summary>
        /// The volume of the Convex Hull.
        /// </summary>
        public double tolerance { get; internal init; }

        /// <summary>
        /// The vertices of the ConvexHull
        /// </summary>
        public readonly List<Vertex> Vertices = new List<Vertex>();
        //internal readonly List<CHFace> cHFaces = new List<CHFace>();
        /// <summary>
        /// Gets the convex hull faces.
        /// </summary>
        /// <value>The convex hull faces.</value>
        public readonly List<ConvexHullFace> Faces= new List<ConvexHullFace>();

        /// <summary>
        /// Gets the convex hull edges.
        /// </summary>
        /// <value>The convex hull edges.</value>
        public Edge[] Edges
        {
            get
            {
                if (Vertices.Count > 0 && edges == null)
                    edges = MakeEdges(Faces);
                return edges;
            }
        }
        private Edge[] edges;
        private static Edge[] MakeEdges(IList<ConvexHullFace> faces)
        {
            var numVertices = (3 * faces.Count) >> 1;
            var edgeDictionary = new Dictionary<long, Edge>();
            foreach (var face in faces)
            {
                var fromVertex = face.C;
                foreach (var toVertex in face.Vertices)
                {
                    var fromVertexIndex = fromVertex.IndexInList;
                    var toVertexIndex = toVertex.IndexInList;
                    long checksum = fromVertexIndex < toVertexIndex
                        ? fromVertexIndex + numVertices * toVertexIndex
                        : toVertexIndex + numVertices * fromVertexIndex;

                    if (edgeDictionary.TryGetValue(checksum, out var edge))
                    {
                        edge.OtherFace = face;
                        face.AddEdge(edge);
                    }
                    else edgeDictionary.Add(checksum, new Edge(fromVertex, toVertex, face, null, false, checksum));
                    fromVertex = toVertex;
                }
            }
            Edge[] edgeArray = new Edge[edgeDictionary.Count];
            edgeDictionary.Values.CopyTo(edgeArray, 0);
            return edgeArray;
        }


        /// <summary>
        /// Calculates the center.
        /// </summary>
        protected override void CalculateCenter() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the volume.
        /// </summary>
        protected override void CalculateVolume() => Faces.CalculateVolumeAndCenter(SameTolerance, out _volume, out _center);

        /// <summary>
        /// Calculates the surface area.
        /// </summary>
        protected override void CalculateSurfaceArea() => _surfaceArea = Faces.Sum(face => face.Area);

        /// <summary>
        /// Calculates the inertia tensor.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override void CalculateInertiaTensor() => _inertiaTensor = Faces.CalculateInertiaTensor(Center);

        public override void Transform(Matrix4x4 transformMatrix)
        {
            var xMin = double.PositiveInfinity;
            var yMin = double.PositiveInfinity;
            var zMin = double.PositiveInfinity;
            var xMax = double.NegativeInfinity;
            var yMax = double.NegativeInfinity;
            var zMax = double.NegativeInfinity;
            foreach (var v in Vertices)
            {
                v.Coordinates = v.Coordinates.Transform(transformMatrix);
                if (xMin > v.Coordinates.X) xMin = v.Coordinates.X;
                if (yMin > v.Coordinates.Y) yMin = v.Coordinates.Y;
                if (zMin > v.Coordinates.Z) zMin = v.Coordinates.Z;
                if (xMax < v.Coordinates.X) xMax = v.Coordinates.X;
                if (yMax < v.Coordinates.Y) yMax = v.Coordinates.Y;
                if (zMax < v.Coordinates.Z) zMax = v.Coordinates.Z;
            }
            Bounds = [new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax)];

            //Update the faces
            foreach (var face in Faces)
            {
                face.Update();// Transform(transformMatrix);
            }
            //Update the edges
            if (edges!=null)
            {
                foreach (var edge in Edges)
                {
                    edge.Update(true);
                }
            }
            _center = _center.Transform(transformMatrix);
            // I'm not sure this is right, but I'm just using the 3x3 rotational submatrix to rotate the inertia tensor
            var rotMatrix = new Matrix3x3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13,
                    transformMatrix.M21, transformMatrix.M22, transformMatrix.M23,
                    transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
            _inertiaTensor *= rotMatrix;
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            var copy = this.Copy();
            copy.Transform(transformationMatrix);
            return copy;
        }

        private Solid Copy()
        {
            throw new System.NotImplementedException();
        }

        internal static ConvexHull3D Create(TessellatedSolid tessellatedSolid)
        {
            ConvexHullAlgorithm.Create(tessellatedSolid, out var convexHull);
            return convexHull;
        }

        internal static ConvexHull3D Create(List<Vertex> vertices, double sameTolerance)
        {
            ConvexHullAlgorithm.Create(vertices, out var convexHull, sameTolerance);
            return convexHull;
        }
    }
    public class ConvexHullFace : TriangleFace
    {
        public ConvexHullFace(Vertex vertex1, Vertex vertex2, Vertex vertex3, Vector3 planeNormal) : this(vertex1, vertex2, vertex3)
        {
            _normal = planeNormal;
        }
        public ConvexHullFace(Vertex vertex1, Vertex vertex2, Vertex vertex3) : base(vertex1, vertex2, vertex3,false)
        {
            peakVertex = null;
            InteriorVertices = new List<Vertex>();
        }

        internal Vertex peakVertex { get; set; }
        internal double peakDistance { get; set; }
        public List<Vertex> InteriorVertices { get; internal set; }
    }
}
