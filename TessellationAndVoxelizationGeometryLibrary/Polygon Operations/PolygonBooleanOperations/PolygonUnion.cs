using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonUnion : PolygonBooleanBase
    {
        internal PolygonUnion() : base(false) { }
        protected override bool ValidStartingIntersection(IntersectionData intersectionData, out PolygonSegment currentEdge, out bool switchPolygon)
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
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) == 0b0 //same direction
              && (intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0)  //splits after this point
            {
                if (!intersectionData.VisitedB && (intersectionData.Relationship & PolygonSegmentRelationship.BEncompassesA) != 0b0)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = false;
                    return true;
                }
                if (!intersectionData.VisitedA && (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = false;
                    return true;
                }
            }
            // "Glance" A and B touch but are only abutting one another. no overlap in regions
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0 &&
                 (intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0 &&
                 (intersectionData.Relationship & PolygonSegmentRelationship.CoincidentLines) != 0b0)
            { //the only time non-overlapping intersections are intereseting is when we are doing union and lines are coincident
              // otherwise you simply stay on the same polygon you enter with
                if (!intersectionData.VisitedB && (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                     (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0)
                    ||
                    ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                      (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)))
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
                else if (!intersectionData.VisitedA &&
                    (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                          (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)
                         ||
                         ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                          (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0)))
                {
                    currentEdge = intersectionData.EdgeA;
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
            // if abutting but the lines are not coincident, then stay on the same
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0
                && (newIntersection.Relationship & PolygonSegmentRelationship.CoincidentLines) == 0b0) return false;
            // if union and current edge is on the outer polygon, then don't consider this as a valid place to switch
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.AEncompassesB
                && currentEdgeIsFromPolygonA) return false;
            if ((newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.BEncompassesA
                && !currentEdgeIsFromPolygonA) return false;
            return true;
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
            if (!identicalPolygonIsInverted)
            {
                if (subPolygonA.IsPositive)
                    positivePolygons.Add(subPolygonA.Area, subPolygonA.Copy());  //add the positive as a positive
                else negativePolygons.Add(subPolygonA.Area, subPolygonA.Copy()); //add the negative as a negative
            }
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, SortedDictionary<double, Polygon> positivePolygons, SortedDictionary<double, Polygon> negativePolygons, bool partOfPolygonB)
        {
            var otherPolygon = partOfPolygonB ? polygonA : polygonB;
            var insideOther = otherPolygon.IsNonIntersectingPolygonInside(subPolygon, out _);
            if (subPolygon.IsPositive)
            {
                if (isUnion != insideOther || (isSubtract && (!partOfPolygonB || doubleApproach)))
                    positivePolygons.Add(subPolygon.Area, subPolygon.Copy());  //add the positive as a positive
                else if (insideOther && isSubtract && (partOfPolygonB || doubleApproach))
                    negativePolygons.Add(-subPolygon.Area, subPolygon.Copy(true)); // add the positive as a negative
            }
            else if (!insideOther && // then it's a hole, but it is not inside the other
            (isUnion || (isSubtract && (!partOfPolygonB || doubleApproach))))
                negativePolygons.Add(subPolygon.Area, subPolygon.Copy()); //add the negative as a negative
            else // it's a hole in the other polygon 
            {
                //first need to check if it is inside a hole of the other
                var holeIsInsideHole = otherPolygon.Holes.Any(h => h.IsNonIntersectingPolygonInside(subPolygon, out _) == true);
                if (holeIsInsideHole && (isUnion || (isSubtract && (!partOfPolygonB || doubleApproach))))
                    negativePolygons.Add(subPolygon.Area, subPolygon.Copy()); //add the negatie as a negative
                else if (!holeIsInsideHole)
                {
                    if (!isUnion && !isSubtract)
                        negativePolygons.Add(subPolygon.Area, subPolygon.Copy()); //add the negatie as a negative
                    else if (isSubtract && (!partOfPolygonB || doubleApproach))
                        positivePolygons.Add(-subPolygon.Area, subPolygon.Copy(true)); //add the negative as a positive
                }
            }
        }
    }
}
