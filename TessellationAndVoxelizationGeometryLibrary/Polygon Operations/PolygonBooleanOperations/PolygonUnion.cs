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
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA || intersectionData.VisitedB ||
                (intersectionData.Relationship & PolygonSegmentRelationship.Interfaces) == PolygonSegmentRelationship.DoubleOverlap)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            switchPolygon = (intersectionData.Relationship & PolygonSegmentRelationship.Interfaces) != PolygonSegmentRelationship.Enclose;
            var AMovesInside = (intersectionData.Relationship & PolygonSegmentRelationship.AMovesInside) != 0b0;
            currentEdge = AMovesInside == switchPolygon ? intersectionData.EdgeA : intersectionData.EdgeB;
            return true;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA)
        {
            if (!base.SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA)) return false;
            PolygonSegmentRelationship whichInterface = newIntersection.Relationship & PolygonSegmentRelationship.Interfaces;
            return whichInterface == PolygonSegmentRelationship.Crossover || whichInterface == PolygonSegmentRelationship.NoOverlap;
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
            // otherwise if the copy is inverted then the two cancel each other out and neither is explicitly needed in the result. 
            // a hole is effectively removed
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons, IEnumerable<(PolygonRelationship, bool)> relationships, bool partOfPolygonB)
        {
            var enumerator = relationships.GetEnumerator();
            enumerator.MoveNext();
            var rel = enumerator.Current.Item1;
            if (rel < PolygonRelationship.AInsideB ||  //separated
                (!partOfPolygonB && (rel & PolygonRelationship.BInsideA) != 0b0) || //subPolygon is part of A and it encompasses the B (BInsideA)
                (partOfPolygonB && (rel & PolygonRelationship.AInsideB) != 0b0)) //subPolygon is part of B and it encompasses the A (AInsideB)
                //  whether positive or negative - it is included
                newPolygons.Add(subPolygon.Copy(false, false));
            else
            { //failing the above if means that it is included in the outer. So it can only be included in the result if it is inside a hole
                while (enumerator.MoveNext())
                {
                    rel = enumerator.Current.Item1;
                    if ((!partOfPolygonB && (rel & PolygonRelationship.AInsideB) != 0b0) ||
                        (partOfPolygonB && (rel & PolygonRelationship.BInsideA) != 0b0))
                    {
                        newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negatie as a negative
                        return;                                                               // otherwise, the polygon has no effect since it is a subset of the other
                    }
                }
            }
        }
    }
}
