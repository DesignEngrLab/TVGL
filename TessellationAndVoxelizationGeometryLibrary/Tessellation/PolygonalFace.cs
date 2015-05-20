using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;

namespace TVGL.Tessellation
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
        public PolygonalFace(List<Vertex> vertices)
            : this()
        {
            foreach (var v in vertices)
            {
                Vertices.Add(v);
                v.Faces.Add(this);
            }
            // now determine normal
            var n = vertices.Count;
            var edgeVectors = new double[n][];
            edgeVectors[0] = vertices[0].Position.subtract(vertices[n - 1].Position);
            for (var i = 1; i < n; i++)
                edgeVectors[i] = vertices[i].Position.subtract(vertices[i - 1].Position);

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
                Normal = new[] {double.NaN, double.NaN, double.NaN};
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
                    var likeFirstNormal = true;
                    var numLikeFirstNormal = 1;
                    foreach (var d in dotProduct)
                    {
                        if (d < 0) likeFirstNormal = !likeFirstNormal;
                        if (likeFirstNormal) numLikeFirstNormal++;
                    }
                    if (2*numLikeFirstNormal >= normals.Count) Normal = normals[0];
                    else Normal = normals[0].multiply(-1);
                }
            }
        }

        /// <summary>
        /// Gets the is convex.
        /// </summary>
        /// <value>The is convex.</value>
        public Boolean IsConvex { get; private set; }

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
        /// </summary>
        /// <value>
        /// The edges.
        /// </value>
        public List<Edge> Edges { get; internal set; }

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
        public double Area { get; set; }

        /// <summary>
        /// Gets or sets the color.
        /// </summary>
        /// <value>The color.</value>
        public Color color { get; set; }


        ////////////////////////////////////////////////////////////////////
        /// <summary>
        /// gets the Area * edge ratio of the face. Edge ratio is the the length of longest
        /// edge to the length of the shortest edge
        /// </summary>
        /// <value>
        /// AreaRatio
        /// </value>
        public double AreaRatio { get; set; }
        /// <summary>
        /// Dictionary with possible face category obtained from different 
        /// combinatons  of its edges' groups 
        /// </summary>
        /// <value>
        /// Face Category
        /// </value>
        internal Dictionary<PrimitiveSurfaceType, double> FaceCat { get; set; }
        /// <summary>
        /// Dictionary with faceCat on its key and the combinaton which makes the category on its value
        /// </summary>
        /// <value>
        /// Category to combination
        /// </value>
        internal Dictionary<PrimitiveSurfaceType, int[]> CatToCom { get; set; }
        /// <summary>
        /// Dictionary with edge combinations on key and edges obtained from face rules on its value
        /// </summary>
        /// <value>
        /// Combination to Edges
        /// </value>
        internal Dictionary<int[], Edge[]> ComToEdge { get; set; }
        /// <summary>
        /// Dictionary with faceCat on key and edges lead to the category on its value
        /// </summary>
        /// <value>
        /// Edges lead to desired category
        /// </value>
        internal Dictionary<PrimitiveSurfaceType, List<Edge>> CatToELDC { get; set; }
        ////////////////////////////////////////////////////////////////////

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
        public Boolean PartofConvexHull { get; internal set; }

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
                    adjacentFaces[i++] = (this == e.OwnedFace) ? e.OtherFace : e.OwnedFace;
                return adjacentFaces;
            }
        }

        #endregion

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>PolygonalFace.</returns>
        public PolygonalFace Copy()
        {
            return new PolygonalFace
            {
                Area = Area,
                AreaRatio = AreaRatio,
                Center = (double[])Center.Clone(),
                Curvature = Curvature,
                color = new Color(color.A, color.R, color.G, color.B),
                PartofConvexHull = PartofConvexHull,
                Edges = new List<Edge>(),
                Normal = (double[])Normal.Clone(),
                Vertices = new List<Vertex>()
            };
        }

        internal Edge OtherEdge(Vertex thisVertex)
        {
           return Edges.First(e => e.To != thisVertex && e.From!=thisVertex);
        }

        internal Vertex OtherVertex(Edge thisEdge)
        {
            return Vertices.First(v => v != thisEdge.To && v != thisEdge.From);
        }
    }
}
