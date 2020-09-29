// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="SpecialClasses.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     Vertex2D class used in Triangulate Polygon
    ///     Inherits position from point class
    /// </summary>
    public class Vertex2D
    {
        #region Properties

        /// <summary>
        ///     Gets the loop ID that this node belongs to.
        /// </summary>
        /// <value>The loop identifier.</value>
        public int LoopID { get; set; }

        /// <summary>
        ///     Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        public double X => Coordinates.X;

        /// <summary>
        ///     Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        public double Y => Coordinates.Y;

        /// <summary>
        ///     Gets the line that starts at this node.
        /// </summary>
        /// <value>The start line.</value>
        public PolygonEdge StartLine { get; internal set; }

        /// <summary>
        ///     Gets the line that ends at this node.
        /// </summary>
        /// <value>The end line.</value>
        public PolygonEdge EndLine { get; internal set; }

        /// <summary>
        ///     Gets the base class, Point of this node.
        /// </summary>
        /// <value>The point.</value>
        public Vector2 Coordinates { get; private set; }

        public int IndexInList { get; internal set; }

        #endregion Properties

        #region Constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="Node" /> class.
        /// </summary>
        /// <param name="currentPoint">The current point.</param>
        /// <param name="loopID">The loop identifier.</param>
        internal Vertex2D(Vector2 currentPoint, int referenceID, int loopID)
        {
            LoopID = loopID;
            Coordinates = currentPoint;
            IndexInList = referenceID;
        }

        internal Vertex2D Copy()
        {
            return new Vertex2D
            {
                Coordinates = this.Coordinates,
                IndexInList = this.IndexInList,
                LoopID = this.LoopID,
            };
        }

        // the following private argument-less constructor is only used in the copy function
        private Vertex2D() { }

        public override string ToString()
        {
            return "{" + X + "," + Y + "}";
        }

        #endregion Constructor
    }
}