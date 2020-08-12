using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonDifference : PolygonBooleanBase
    {
        internal PolygonDifference() : base(true) { }

        protected override bool ValidStartingIntersection(IntersectionData intersectionData,
            out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA || intersectionData.VisitedB)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            //Overlapping. The conventional case where A and B cross into one another
            if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping)
            {
                var cross = intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);
                if (cross < 0 && !intersectionData.VisitedA && intersectionData.EdgeA.IndexInList < intersectionData.EdgeB.IndexInList)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                if (cross > 0 && !intersectionData.VisitedB && intersectionData.EdgeB.IndexInList < intersectionData.EdgeA.IndexInList)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
            }
            // merging into same line
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) == 0b0
              && (intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0)
            {
                if (!intersectionData.VisitedA && intersectionData.EdgeA.IndexInList < intersectionData.EdgeB.IndexInList &&
                                (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                // Polygon B encompasses all of polygon A at this intersection
                else if (!intersectionData.VisitedB && intersectionData.EdgeB.IndexInList < intersectionData.EdgeA.IndexInList &&
                    (intersectionData.Relationship & PolygonSegmentRelationship.BEncompassesA) != 0b0)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
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
            return (newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) != 0b0;
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
            if (identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or negative as a negative
            //else do not add it.
            // clearly is both positive then the subtraction should yield zero, but for two negative polygons (i.e. holes)
            // it is harder to see. The surrounding material will be removed and the hole will be outside of the positive
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, List<Polygon> newPolygons, bool partOfPolygonB)
        {
            var insideOther = subPolygon.Vertices[0].Type == NodeType.Inside;
            if (partOfPolygonB) // part of the subtrahend, the B in A-B
            {
                if (insideOther)
                    newPolygons.Add(subPolygon.Copy(false, true)); // add the positive as a negative or add the negative as a positive
            }
            else if (!insideOther) // then part of the minuend, the A in A-B
                                   // then on the outside of the other, but could be inside a hole
                newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negatie as a negative
        }
    }
}
