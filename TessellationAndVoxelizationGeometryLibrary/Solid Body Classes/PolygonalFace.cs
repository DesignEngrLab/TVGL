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
        /// Initializes a new instance of the <see cref="PolygonalFace" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="normal">A guess for the normal vector.</param>
        /// <param name="ConnectVerticesBackToFace">if set to <c>true</c> [connect vertices back to face].</param>
        public PolygonalFace(IList<Vertex> vertices, double[] normal = null, bool ConnectVerticesBackToFace = true)
            : this()
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                if (ConnectVerticesBackToFace)
                    v.Faces.Add(this);
            }
            var centerX = Vertices.Average(v => v.X);
            var centerY = Vertices.Average(v => v.Y);
            var centerZ = Vertices.Average(v => v.Z);
            Center = new[] { centerX, centerY, centerZ };
            bool reverseVertexOrder;
            Normal = DetermineNormal(out reverseVertexOrder, normal);
            if (reverseVertexOrder) Vertices.Reverse();
            Area = DetermineArea();
        }

        internal double DetermineArea()
        {
            var area = 0.0;
            for (int i = 2; i < Vertices.Count; i++)
            {
            var edge1 = Vertices[1].Position.subtract(Vertices[0].Position);
            var edge2 = Vertices[2].Position.subtract(Vertices[0].Position);
            // the area of each triangle in the face is the area is half the magnitude of the cross product of two of the edges
            area+= Math.Abs(edge1.crossProduct(edge2).dotProduct(this.Normal)) / 2;
            }
            return area;
        }
        private double[] DetermineNormal(out bool reverseVertexOrder, double[] normal = null) //Assuming CCW order of vertices
        {
            reverseVertexOrder = false;
            var n = Vertices.Count;
            if (normal != null && normal.Contains(double.NaN)) normal = null;
            else if (normal != null) normal.normalizeInPlace();
            var edgeVectors = new double[n][];
            var normals = new List<double[]>();
            edgeVectors[0] = Vertices[0].Position.subtract(Vertices[n - 1].Position);
            for (var i = 1; i < n; i++)
            {
                edgeVectors[i] = Vertices[i].Position.subtract(Vertices[i - 1].Position);
                var tempCross = edgeVectors[i - 1].crossProduct(edgeVectors[i]).normalize();
                if (!tempCross.Any(double.IsNaN))
                {
                    if (!normals.Any())
                    {   // a guess at the normal (usually from an STL file) may be passed
                        // in to this function. If we find that the guess matches this first one
                        // (it's first because normals is empty), then we simply exit with the provided
                        // value.
                        if (normal != null)
                        {
                            if (tempCross.IsPracticallySame(normal, Constants.SameFaceNormalDotTolerance)) return normal;
                            if (tempCross.multiply(-1).IsPracticallySame(normal, Constants.SameFaceNormalDotTolerance))
                            {
                                reverseVertexOrder = true;
                                return normal;
                            }
                        }

                    }
                    normals.Add(tempCross);
                }
            }
            var lastCross = edgeVectors[n - 1].crossProduct(edgeVectors[0]).normalize();
            if (!lastCross.Any(double.IsNaN)) normals.Add(lastCross);

            n = normals.Count;
            if (n == 0) // this would happen if the face collapse to a line.
                return normal.normalize();
            // before we just average these normals, let's check that they agree.
            // the dotProductsOfNormals simply takes the dot product of adjacent
            // normals. If they're all close to one, then we can average and return.
            var dotProductsOfNormals = new List<double>();
            dotProductsOfNormals.Add(normals[0].dotProduct(normals[n - 1]));
            for (var i = 1; i < n; i++) dotProductsOfNormals.Add(normals[i].dotProduct(normals[i - 1]));
            // if all are close to one (or at least positive), then the face is a convex polygon. Now,
            // we can simply average and return the answer.
            IsConvex = (dotProductsOfNormals.All(x => x > 0));
            if (IsConvex)
            {
                var newNormal = normals.Aggregate((current, c) => current.add(c)).normalize();
                // even though the normal provide was wrong above (or nonexistent)
                // we still check it to see if this is the correct direction.
                if (normal == null || newNormal.dotProduct(normal) >= 0) return newNormal;
                // else reverse the order 
                reverseVertexOrder = true;
                return newNormal.multiply(-1);
            }
            // now, the rare case in which the polygon face is not convex, the only .
            if (normal != null)
            {
                //
                // well, here the guess may be useful. We'll insert it into the list of dotProducts
                // and then do a tally
                dotProductsOfNormals[0] = normal.dotProduct(normals[0]);
                dotProductsOfNormals.Insert(0, normal.dotProduct(normals[n - 1]));
            }
            var likeFirstNormal = true;
            var numLikeFirstNormal = 1;
            foreach (var d in dotProductsOfNormals)
            {   // this tricky little function keeps track of how many are in the same direction
                // as the first one.
                if (d < 0) likeFirstNormal = !likeFirstNormal;
                if (likeFirstNormal) numLikeFirstNormal++;
            }
            // if the majority are like the first one, then use that one (which may have been the guess).
            if (2 * numLikeFirstNormal >= normals.Count) return normals[0].normalize();
            // otherwise, go with the opposite (so long as there isn't an original guess)
            if (normal == null) return normals[0].normalize().multiply(-1);
            //finally, assume the original guess is right, and reverse the order
            reverseVertexOrder = true;
            return normals[0].normalize();

        }
        private double[] DetermineNormal() //Assuming CCW order of vertices
        {
            var n = Vertices.Count;
            var edgeVectors = new double[n][];
            var normals = new List<double[]>();
            edgeVectors[0] = Vertices[0].Position.subtract(Vertices[n - 1].Position);
            for (var i = 1; i < n; i++)
            {
                edgeVectors[i] = Vertices[i].Position.subtract(Vertices[i - 1].Position);
                var tempCross = edgeVectors[i - 1].crossProduct(edgeVectors[i]).normalize();
                if (!tempCross.Any(double.IsNaN))
                    normals.Add(tempCross);
            }
            var lastCross = edgeVectors[n - 1].crossProduct(edgeVectors[0]).normalize();
            if (!lastCross.Any(double.IsNaN)) normals.Add(lastCross);

            n = normals.Count;
            if (n == 0) // this would happen if the face collapse to a line.
                return new[] { 0.0, 0.0, 0.0 };
            // before we just average these normals, let's check that they agree.
            // the dotProductsOfNormals simply takes the dot product of adjacent
            // normals. If they're all close to one, then we can average and return.
            var dotProductsOfNormals = new List<double>();
            dotProductsOfNormals.Add(normals[0].dotProduct(normals[n - 1]));
            for (var i = 1; i < n; i++) dotProductsOfNormals.Add(normals[i].dotProduct(normals[i - 1]));
            // if all are close to one (or at least positive), then the face is a convex polygon. Now,
            // we can simply average and return the answer.
            IsConvex = (dotProductsOfNormals.All(x => x > 0));
            var normal = new[] { 0.0, 0.0, 0.0 };
            if (IsConvex)
            {
                // it's okay to overwrite the guess. If it didn't work above, no reason it
                // should make sense now. 
                normal = normals.Aggregate((current, c) => current.add(c));
                return normal.normalize();
            }
            // now, the rare case in which the polygon face is not convex...
            if (normal != null)
            {
                // well, here the guess may be useful. We'll insert it into the list of dotProducts
                // and then do a tally
                dotProductsOfNormals[0] = normal.dotProduct(normals[0]);
                dotProductsOfNormals.Insert(0, normal.dotProduct(normals[n - 1]));
            }
            var likeFirstNormal = true;
            var numLikeFirstNormal = 1;
            foreach (var d in dotProductsOfNormals)
            {   // this tricky little function keeps track of how many are in the same direction
                // as the first one.
                if (d < 0) likeFirstNormal = !likeFirstNormal;
                if (likeFirstNormal) numLikeFirstNormal++;
            }
            // if the majority are like the first one, then use that one (which may have been the guess).
            if (2 * numLikeFirstNormal >= normals.Count) return normals[0].normalize();
            // otherwise, go with the opposite.
            return normals[0].normalize().multiply(-1);
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
        public double Area { get; internal set; }

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
        /// Gets the adjacent faces.
        /// </summary>
        /// <value>The adjacent faces.</value>
        public List<PolygonalFace> AdjacentFaces
        {
            get
            {
                var adjacentFaces = new List<PolygonalFace>();
                foreach (var e in Edges)
                {
                    if (e == null) adjacentFaces.Add(null);
                    else adjacentFaces.Add((this == e.OwnedFace) ? e.OtherFace : e.OwnedFace);
                }
                return adjacentFaces;
            }
        }

        public int IndexInList { get; internal set; }

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
            Normal = DetermineNormal();
            Area = DetermineArea();
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

    }
}
