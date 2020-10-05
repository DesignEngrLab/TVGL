// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using TVGL.Numerics;

namespace TVGL.TwoDimensional
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
        public WhereIsIntersection WhereIntersection { get; }
        public CollinearityTypes CollinearityType { get; }
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
        /// Initializes a new instance of the <see cref="SegmentIntersection"/> class.
        /// </summary>
        /// <param name="edgeA">The edge a.</param>
        /// <param name="edgeB">The edge b.</param>
        /// <param name="intersectionPoint">The intersection point.</param>
        /// <param name="relationship">The relationship.</param>
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
