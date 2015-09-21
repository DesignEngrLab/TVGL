using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// This class defines a flat polygonal face. The implementation began with triangular faces in mind. 
    /// It should be double-checked for higher polygons.   It inherits from the ConvexFace class in 
    /// MIConvexHull
    /// </summary>
    public class PolygonalFace
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="normal">The normal.</param>
        /// <param name="color">The color.</param>
        public PolygonalFace(double[] normal, Color color)
            : this(normal)
        {
            this.color = color;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalFace"/> class.
        /// </summary>
        /// <param name="normal">The normal.</param>
        public PolygonalFace(double[] normal)
            : this()
        {
            Normal = normal;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalFace"/> class.
        /// </summary>
        public PolygonalFace()
        {
            Vertices = new List<Vertex>();
            Edges = new List<Edge>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalFace"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        public PolygonalFace(IList<Vertex> vertices, double[] normal,  bool ConnectVerticesBackToFace = true, 
            bool normalIsGuess = false, long faceReference = 0)
            : this()
        {
            if (normalIsGuess) 
            {
                foreach (var v in vertices)
                {
                    Vertices.Add(v);
                    if (ConnectVerticesBackToFace)
                        v.Faces.Add(this);
                }
                SetNormal(normal);
            }
            else
            {
                Normal = normal;
                var edge1 = vertices[1].Position.subtract(vertices[0].Position);
                var edge2 = vertices[2].Position.subtract(vertices[1].Position);
                if (Normal.dotProduct(edge1.crossProduct(edge2)) <= 0)
                    Vertices = new List<Vertex>(new[] { vertices[0], vertices[2], vertices[1] });
                else Vertices = new List<Vertex>(vertices);
                if (ConnectVerticesBackToFace)
                    foreach (var v in Vertices)
                        v.Faces.Add(this);
            }
            SetArea();
            SetCenter();
            if (faceReference == 0) SetFaceReference();
            else FaceReference = faceReference;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PolygonalFace"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        public PolygonalFace(IList<Vertex> vertices, bool ConnectVerticesBackToFace = true, long faceReference = 0)
            : this()
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                if (ConnectVerticesBackToFace)
                    v.Faces.Add(this);
            }
            SetArea();
            SetNormal();
            SetCenter();
            if (faceReference == 0) SetFaceReference();
            else FaceReference = faceReference;
        }

        public void SetArea()
        {
            // assuming triangular faces: the area is half the magnitude of the cross product of two of the edges
            if (Vertices.Count == 3)
            {
                var edge1 = Vertices[1].Position.subtract(Vertices[0].Position);
                var edge2 = Vertices[2].Position.subtract(Vertices[0].Position);
                Area = Math.Abs(edge1.crossProduct(edge2).norm2()) / 2;
                //if (Area.IsNegligible()) throw new Exception();
            }
            else throw new Exception("Not Implemented");
        }

        public void SetCenter()
        {
            var centerX = Vertices.Average(v => v.X);
            var centerY = Vertices.Average(v => v.Y);
            var centerZ = Vertices.Average(v => v.Z);
            Center = new[] { centerX, centerY, centerZ };
        }

        /// <summary>
        /// Gets the is convex.
        /// </summary>
        /// <value>The is convex.</value>
        public bool IsConvex { get; private set; }

        #endregion

        #region Properties
        /// <summary>
        /// Gets the normal.
        /// </summary>
        /// <value>
        /// The normal.
        /// </value>
        public double[] Normal { get; set; }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>
        /// The vertices.
        /// </value>
        public List<Vertex> Vertices { get; internal set; }
        /// <summary>
        /// Gets the edges.
        /// 
        /// </summary>
        /// <value>
        /// The edges.
        /// </value>
        public List<Edge> Edges { get; set; }
 
        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>
        /// The center.
        /// </value>
        public double[] Center { get; internal set; }
        /// <summary>
        /// Gets the area.
        /// </summary>
        /// <value>
        /// The area.
        /// </value>
        public double Area { get;  internal set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color color { get; set; }
        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>
        /// The curvature.
        /// </value>
        public CurvatureType Curvature { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether [it is part of the convex hull].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [it is part of the convex hull]; otherwise, <c>false</c>.
        /// </value>
        public bool PartofConvexHull { get; internal set; }

        /// <summary>
        /// Gets or sets the reference index, which is made from the vertex.IndexValue's 
        /// in a sorted list (not CCW). This value is unique for each face.
        /// </summary>
        public long FaceReference { get; internal set; }
        
        /// <summary>
        /// Gets the adjacent faces.
        /// </summary>
        /// <value>The adjacent faces.</value>
        public PolygonalFace[] AdjacentFaces
        {
            get
            {
                var adjacentFaces = new PolygonalFace[3];
                var i = 0;
                foreach (var e in Edges)
                {
                    if (e == null) adjacentFaces[i++] = null;
                    else adjacentFaces[i++] = (this == e.OwnedFace) ? e.OtherFace : e.OwnedFace;
                }
                return adjacentFaces;
            }
        }

        #endregion
        /// <summary>
        ///     Defines the face curvature. Depends on DefineEdgeAngle
        /// </summary>
        public void DefineFaceCurvature()
        {
            if (Edges.Any(e => e.Curvature == CurvatureType.Undefined))
                Curvature = CurvatureType.Undefined;
            else if (Edges.All(e => e.Curvature != CurvatureType.Concave))
                Curvature = CurvatureType.Convex;
            else if (Edges.All(e => e.Curvature != CurvatureType.Convex))
                Curvature = CurvatureType.Concave;
            else Curvature = CurvatureType.SaddleOrFlat;
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>PolygonalFace.</returns>
        public PolygonalFace Copy()
        {
            return new PolygonalFace
            {
                FaceReference = FaceReference,
                Area = Area,
                Center = (double[])Center.Clone(),
                Curvature = Curvature,
                color = new Color(color.A, color.R, color.G, color.B),
                PartofConvexHull = PartofConvexHull,
                Edges = new List<Edge>(),
                Normal = (double[])Normal.Clone(),
                Vertices = new List<Vertex>()
            };
        }

        //Set new normal and area. 
        //References are assumed to be the same.
        public void Update()
        {
            SetNormal();
            SetArea();
        }

        public void SetFaceReference()
        {
            var checkSumMultipliers = new long[Constants.MaxNumberEdgesPerFace];
            for (var i = 0; i < Constants.MaxNumberEdgesPerFace; i++)
                checkSumMultipliers[i] = (long)Math.Pow(TessellatedSolid.CheckSumMultiplier, i);
            double[] normal = null;
            long checksum = 0;
            var orderedIndices = new List<int>();
            foreach (var vertex in Vertices)
            {
                orderedIndices.Add(vertex.IndexInList);
            }
            orderedIndices.Sort();
            if (orderedIndices[0] == -1) //A vertex index has not been set.
            {
                 FaceReference = -1; //set an impossible face reference value.
            }
            else
            {
                if (orderedIndices.Count != Constants.MaxNumberEdgesPerFace) throw new Exception();
                for (var j = 0; j < orderedIndices.Count; j++)
                    checksum += orderedIndices[j] * checkSumMultipliers[j];
                FaceReference = checksum;
            }
        }

        internal Edge OtherEdge(Vertex thisVertex, bool willAcceptNullAnswer = false)
        {
            if (willAcceptNullAnswer)
                return Edges.FirstOrDefault(e => e.To != thisVertex && e.From != thisVertex);
            return Edges.First(e => e.To != thisVertex && e.From != thisVertex);
        }

        internal Vertex OtherVertex(Edge thisEdge, bool willAcceptNullAnswer = false)
        {
            return willAcceptNullAnswer ? Vertices.FirstOrDefault(v => v != thisEdge.To && 
                v != thisEdge.From) : Vertices.First(v => v != thisEdge.To && v != thisEdge.From);
        }

        internal Vertex OtherVertex(Vertex v1, Vertex v2, bool willAcceptNullAnswer = false)
        {
            return willAcceptNullAnswer ? Vertices.FirstOrDefault(v => v != v1 && v != v2) : 
                Vertices.First(v => v != v1 && v != v2);
        }

        internal void SetNormal(double[] guess = null)
        {
            var n = Vertices.Count;
            var edgeVectors = new double[n][];
            edgeVectors[0] = Vertices[0].Position.subtract(Vertices[n - 1].Position);
            for (var i = 1; i < n; i++)
                edgeVectors[i] = Vertices[i].Position.subtract(Vertices[i - 1].Position);

            var normals = new List<double[]>();
            var tempCross = edgeVectors[n - 1].crossProduct(edgeVectors[0]).normalize();
            if (!tempCross.Any(double.IsNaN)) normals.Add(tempCross);
            for (var i = 1; i < n; i++)
            {
                tempCross = edgeVectors[i - 1].crossProduct(edgeVectors[i]).normalize();
                if (!tempCross.Any(double.IsNaN))
                    normals.Add(tempCross);
            }
            n = normals.Count;
            if (n == 0)  // this would happen if the face collapse to a line.
                Normal = new[] { double.NaN, double.NaN, double.NaN };
            else
            {
                var dotProduct = new double[n];
                dotProduct[0] = normals[0].dotProduct(normals[n - 1]);
                for (var i = 1; i < n; i++) dotProduct[i] = normals[i].dotProduct(normals[i - 1]);
                IsConvex = (dotProduct.All(x => x > 0));
                Normal = new double[3];
                if (IsConvex)
                {
                    Normal = normals.Aggregate(Normal, (current, c) => current.add(c));
                    Normal = Normal.divide(normals.Count);
                }
                else
                {
                    if (guess == null)
                    {
                        var likeFirstNormal = true;
                        var numLikeFirstNormal = 1;
                        foreach (var d in dotProduct)
                        {
                            if (d < 0) likeFirstNormal = !likeFirstNormal;
                            if (likeFirstNormal) numLikeFirstNormal++;
                        }
                        if (2 * numLikeFirstNormal >= normals.Count) Normal = normals[0];
                        else Normal = normals[0].multiply(-1);
                    }
                    else
                    {
                        if (Normal.dotProduct(guess) < 0.0) Normal = normals[0].multiply(-1);
                    }
                }
            }
        }
    }
}
