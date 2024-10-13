// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="SegmentIntersection.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************


namespace TVGL
{
    /// <summary>
    /// Class SegmentIntersection. Is return as part of the Polygon-to-Polygon Interaction Record.
    /// A collection of these define all the ways that polygon line segment intersect.
    /// </summary>
    public class SegmentIntersection
    {
        /// <summary>
        /// Gets Polygon Edge A.
        /// </summary>
        /// <value>The edge a.</value>
        public PolygonEdge EdgeA { get; }
        /// <summary>
        /// Gets Polygon Edge B.
        /// </summary>
        /// <value>The edge b.</value>
        public PolygonEdge EdgeB { get; }
        /// <summary>
        /// Gets the intersection coordinates.
        /// </summary>
        /// <value>The intersect coordinates.</value>
        public Vector2 IntersectCoordinates { get; }
        /// <summary>
        /// Gets the relationship.
        /// </summary>
        /// <value>The relationship.</value>
        public SegmentRelationship Relationship { get; }
        /// <summary>
        /// Gets the where intersection.
        /// </summary>
        /// <value>The where intersection.</value>
        public WhereIsIntersection WhereIntersection { get; }
        /// <summary>
        /// Gets the type of the collinearity.
        /// </summary>
        /// <value>The type of the collinearity.</value>
        public CollinearityTypes CollinearityType { get; }
        /// <summary>
        /// Gets or sets a value indicating whether [the intersection has already been visited before].
        /// starting from EdgeB. This is used internally in polygon operations.
        /// </summary>
        /// <value><c>true</c> if [entered a]; otherwise, <c>false</c>.</value>
        public bool VisitedA { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [the intersection has already been visited before].
        /// starting from EdgeB. This is used internally in polygon operations.
        /// </summary>
        /// <value><c>true</c> if [entered a]; otherwise, <c>false</c>.</value>
        public bool VisitedB { get; set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="SegmentIntersection" /> class.
        /// </summary>
        /// <param name="edgeA">The edge a.</param>
        /// <param name="edgeB">The edge b.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="relationship">The relationship.</param>
        /// <param name="whereIsIntersection">The where is intersection.</param>
        /// <param name="collinearity">The collinearity.</param>
        internal SegmentIntersection(PolygonEdge edgeA, PolygonEdge edgeB, Vector2 intersectionPoint, SegmentRelationship relationship,
            WhereIsIntersection whereIsIntersection, CollinearityTypes collinearity)
        {
            this.EdgeA = edgeA;
            this.EdgeB = edgeB;
            this.IntersectCoordinates = intersectionPoint;
            this.Relationship = relationship;
            this.WhereIntersection = whereIsIntersection;
            this.CollinearityType = collinearity;
            VisitedA = false;
            VisitedB = false;
        }

    }
}
