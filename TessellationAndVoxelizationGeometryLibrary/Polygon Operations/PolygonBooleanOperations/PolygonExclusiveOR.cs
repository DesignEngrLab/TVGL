using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonExclusiveOR : PolygonBooleanBase
    {
        internal PolygonExclusiveOR() : base(true) { }



        protected override bool ValidStartingIntersection(IntersectionData intersectionData,
       out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA && intersectionData.VisitedB)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            //Overlapping. The conventional case where A and B cross into one another
            if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping)
            {
                var cross = intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);
                if (cross < 0 && !intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                if (cross > 0 && !intersectionData.VisitedB)
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
                if (!intersectionData.VisitedA &&
                                (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                // Polygon B encompasses all of polygon A at this intersection
                else if (!intersectionData.VisitedB && 
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
            return (newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) != 0b0;
        }

        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, SortedDictionary<double, Polygon> positivePolygons, SortedDictionary<double, Polygon> negativePolygons,
                    bool identicalPolygonIsInverted)
        {
            // If guess - given the nature of XOR - it's no surprise that duplicates are not captured. Xor is about capturing the uniqueness of both polygons.
            // I realize that sounds vague but when you go through the combinations, they are all turn out as nil: two positives, two negatives, one positive and one negative
        }


        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, SortedDictionary<double, Polygon> positivePolygons, SortedDictionary<double, Polygon> negativePolygons, bool partOfPolygonB)
        {
            var otherPolygon = partOfPolygonB ? polygonA : polygonB != null ? polygonB : null;
            var insideOther = otherPolygon?.IsNonIntersectingPolygonInside(subPolygon, out _) == true;

            if (insideOther)
            {
                if (subPolygon.IsPositive) negativePolygons.Add(-subPolygon.Area, subPolygon.Copy(false, true)); // add the positive as a negative
                else positivePolygons.Add(-subPolygon.Area, subPolygon.Copy(false, true)); //add the negative as a positive
            }
            else
            // then on the outside of the other, but could be inside a hole
            {
                if (subPolygon.IsPositive) positivePolygons.Add(subPolygon.Area, subPolygon.Copy(false, false));  //add the positive as a positive
                else negativePolygons.Add(subPolygon.Area, subPolygon.Copy(false, false)); //add the negatie as a negative
            }
        }

    }
}
