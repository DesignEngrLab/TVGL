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
            get { return Coordinates.X; }
            init { Coordinates = new Vector2(value, Coordinates.Y); }
        }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y
        {
            get { return Coordinates.Y; }
            init { Coordinates = new Vector2(Coordinates.X, value); }
        }

        public double this[int i]
        {
            get
            {
                if (i == 0) return X;
                else return Y;
            }
        }

        public bool? IsConvex
        {
            get
            {
                if (isConvex == null)
                {
                    if (StartLine != null && EndLine != null)
                        isConvex = EndLine.Vector.Cross(StartLine.Vector) > 0;
                }
                return isConvex;
            }
        }
        private bool? isConvex;
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
        public Vector2 Coordinates { get; set; }

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

        public bool IsNull()
        {
            return Coordinates.IsNull();
        }
        #endregion Constructor
    }
}