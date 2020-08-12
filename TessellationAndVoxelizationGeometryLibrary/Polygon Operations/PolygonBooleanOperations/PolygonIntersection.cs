using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonIntersection : PolygonBooleanBase
    {
        internal PolygonIntersection() : base(false) { }
        protected override bool ValidStartingIntersection(IntersectionData intersectionData,
            out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA || intersectionData.VisitedB)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            // Overlapping. The conventional case where A and B cross into one another
            if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping)
            {
                var cross = intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);
                if (cross > 0 && !intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                if (cross < 0 && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
            }
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) == 0b0 //same direction
                 && (intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0)  //splits after this point
            {
                if (!intersectionData.VisitedB && (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = false;
                    return true;
                }
                if (!intersectionData.VisitedA && (intersectionData.Relationship & PolygonSegmentRelationship.BEncompassesA) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = false;
                    return true;
                }
            }
            currentEdge = null;
            switchPolygon = false;
            return false;
        }

        protected override bool SwitchAtThisIntersection(IntersectionData newIntersection, bool currentEdgeIsFromPolygonA)
        {
            if (!base.SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA)) return false;

            // if the two polygons just "glance" off of one another at this intersection, then don't consider this as a valid place to switch
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0) return false;
            // if current edge is on the inner polygon, then don't consider this as a valid place to switch
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.AEncompassesB &&
              !currentEdgeIsFromPolygonA)
                return false;
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.BEncompassesA &&
            currentEdgeIsFromPolygonA)
                return false;
            return true;
        }

        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool identicalPolygonIsInverted)
        {
            if (!identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, List<Polygon> newPolygons, bool partOfPolygonB)
        {
            var insideOther = subPolygon.Vertices[0].Type == NodeType.Inside;
            if (insideOther)
                newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }
    }
}