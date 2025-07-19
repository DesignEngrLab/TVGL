// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Vertex2D.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;

namespace TVGL
{
    /// <summary>
    /// Vertex2D class used in Triangulate Polygon
    /// Inherits position from point class
    /// </summary>
    public class Vertex2D : IVector2D
    {
        #region Properties

        /// <summary>
        /// Gets the loop ID that this node belongs to.
        /// </summary>
        /// <value>The loop identifier.</value>
        public int LoopID { get; set; }

        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X
        {
            get => RationalIP.AsDoubleValue(Coordinates.X, Coordinates.W);
            init => throw new System.NotImplementedException();
        }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y
        {
            get => RationalIP.AsDoubleValue(Coordinates.Y, Coordinates.W);
            init => throw new System.NotImplementedException();
        }

        public double this[int i]
        {
            get
            {
                if (i == 0) return X;
                else return Y;
            }
        }

        // Returns true if the vertex is convex in the polygon. If it is concave
        // then it is false. If the vertex is not attached at the StartLine or 
        // the Endline or one of the following methods in Polygon have 
        // NOT been invoked (SetVertexConvexities)
        // then it will be null.
        public bool? IsConvex { get; internal set; }

        /// <summary>
        /// Gets the line that starts at this node.
        /// </summary>
        /// <value>The start line.</value>
        public PolygonEdge StartLine { get; internal set; }

        /// <summary>
        /// Gets the line that ends at this node.
        /// </summary>
        /// <value>The end line.</value>
        public PolygonEdge EndLine { get; internal set; }

        /// <summary>
        /// Gets the base class, Point of this node.
        /// </summary>
        /// <value>The point.</value>
        internal Vector2IP Coordinates { get; set; }

        /// <summary>
        /// Gets the index in list.
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList { get; set; }
        #endregion Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="referenceID">The reference identifier.</param>
        /// <param name="loopID">The loop identifier.</param>
        public Vertex2D(Vector2 currentPoint, int referenceID, int loopID)
        {
            LoopID = loopID;
            Coordinates = currentPoint;
            IndexInList = referenceID;
        }

        /// <summary>
        /// Copies this instance.
        /// </summary>
        /// <returns>Vertex2D.</returns>
        public Vertex2D Copy()
        {
            return new Vertex2D
            {
                Coordinates = this.Coordinates,
                IndexInList = this.IndexInList,
                LoopID = this.LoopID,
            };
        }

        // the following private argument-less constructor is only used in the copy function
        /// <summary>
        /// Prevents a default instance of the <see cref="Vertex2D"/> class from being created.
        /// </summary>
        internal Vertex2D() { }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return "{" + X + "," + Y + "}";
        }

        /// <summary>
        /// Transforms the specified matrix.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        internal void Transform(Matrix3x3 matrix)
        {
            Coordinates = Coordinates.Transform(matrix);
        }

        /// <summary>
        /// Determines whether the current instance represents a null or uninitialized state.
        /// </summary>
        /// <remarks>This method delegates the null-check to the <see cref="Coordinates"/> instance.
        /// Ensure that the <see cref="Coordinates"/> property is properly initialized before calling this
        /// method.</remarks>
        /// <returns><see langword="true"/> if the current instance is considered null or uninitialized; otherwise, <see
        /// langword="false"/>.</returns>
        public bool IsNull()
        {
            return Coordinates.IsNull();
        }

        public bool IsSameXAs(Vertex2D that) => RationalIP.Equals(this.Coordinates.X, this.Coordinates.W, that.Coordinates.X, that.Coordinates.W);
        public bool IsSameYAs(Vertex2D that) => RationalIP.Equals(this.Coordinates.Y, this.Coordinates.W, that.Coordinates.Y, that.Coordinates.W);
        public bool HasLessXThan(Vertex2D that) => RationalIP.IsLessThanVectorX(this.Coordinates, that.Coordinates);
        public bool HasLessYThan(Vertex2D that) => RationalIP.IsLessThanVectorY(this.Coordinates, that.Coordinates);
        public bool HasGreaterXThan(Vertex2D that) => RationalIP.IsGreaterThanVectorX(this.Coordinates, that.Coordinates);
        public bool HasGreaterYThan(Vertex2D that) => RationalIP.IsGreaterThanVectorY(this.Coordinates, that.Coordinates);

        /// <summary>
        /// Calculates the internal angle at this vertex formed by the start and end lines. Result is a positive angle
        /// bewten 0 and 2π radians.
        /// </summary>
        /// <returns>The internal angle in radians between the two lines. Returns <see cref="double.NaN"/> if either <see
        /// cref="StartLine"/> or <see cref="EndLine"/> is <c>null</c>.</returns>
        public double GetInternalAngle()
        {
            if (StartLine == null || EndLine == null)
                return double.NaN;
            var vector1 = EndLine.Vector;
            var vector2 = StartLine.Vector;
            return Math.PI - Math.Atan2(vector1.Cross(vector2), vector1.Dot(vector2));
        }
        #endregion Constructor
    }
}