using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Class IntersectionData.
    /// </summary>
    public class IntersectionData
    {
        /// <summary>
        /// Gets Polygon Edge A.
        /// </summary>
        /// <value>The edge a.</value>
        public PolygonSegment EdgeA { get; }
        /// <summary>
        /// Gets Polygon Edge B.
        /// </summary>
        /// <value>The edge b.</value>
        public PolygonSegment EdgeB { get; }
        /// <summary>
        /// Gets the intersection coordinates.
        /// </summary>
        /// <value>The intersect coordinates.</value>
        public Vector2 IntersectCoordinates { get; }
        /// <summary>
        /// Gets the relationship.
        /// </summary>
        /// <value>The relationship.</value>
        public PolygonSegmentRelationship Relationship { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [the intersection has already been visited before].
        /// starting from EdgeB. This is used internally in polygon operations.
        /// </summary>
        /// <value><c>true</c> if [entered a]; otherwise, <c>false</c>.</value>
        internal bool VisitedA { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [the intersection has already been visited before].
        /// starting from EdgeB. This is used internally in polygon operations.
        /// </summary>
        /// <value><c>true</c> if [entered a]; otherwise, <c>false</c>.</value>
        internal bool VisitedB { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="IntersectionData"/> class.
        /// </summary>
        /// <param name="edgeA">The edge a.</param>
        /// <param name="edgeB">The edge b.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="relationship">The relationship.</param>
        internal IntersectionData(PolygonSegment edgeA, PolygonSegment edgeB, Vector2 intersectionPoint, PolygonSegmentRelationship relationship)
        {
            this.EdgeA = edgeA;
            this.EdgeB = edgeB;
            this.IntersectCoordinates = intersectionPoint;
            this.Relationship = relationship;
            VisitedA = false;
            VisitedB = false;
        }

    }
}
