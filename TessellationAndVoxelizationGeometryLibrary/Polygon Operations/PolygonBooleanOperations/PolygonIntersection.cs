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
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData,
            out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA || intersectionData.VisitedB ||
                (intersectionData.Relationship & PolygonSegmentRelationship.Interfaces) == PolygonSegmentRelationship.NoOverlap)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            switchPolygon = (intersectionData.Relationship & PolygonSegmentRelationship.Interfaces) != PolygonSegmentRelationship.Enclose;
            var AMovesInside = (intersectionData.Relationship & PolygonSegmentRelationship.AMovesInside) != 0b0;
            currentEdge = AMovesInside == switchPolygon ? intersectionData.EdgeB : intersectionData.EdgeA;
            return true;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA)
        {
            if (!base.SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA)) return false;
            PolygonSegmentRelationship whichInterface = newIntersection.Relationship & PolygonSegmentRelationship.Interfaces;
            return whichInterface == PolygonSegmentRelationship.Crossover || whichInterface == PolygonSegmentRelationship.DoubleOverlap;
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

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons, IEnumerable<(PolygonRelationship, bool)> relationships, bool partOfPolygonB)
        {
            var enumerator = relationships.GetEnumerator();
            enumerator.MoveNext();
            var rel = enumerator.Current.Item1;
            if (rel < PolygonRelationship.AInsideB) return; // for speed sake, just return if the two are separated (<AInsideB)
            do
            {
                rel = enumerator.Current.Item1;
                var otherIsPositive = enumerator.Current.Item2;
                if ((partOfPolygonB && (rel & PolygonRelationship.AInsideB) != 0b0 == otherIsPositive) ||   //part of B and either 1)A is inside of it and A is positive or 2) it is inside A and A is negative
                    (!partOfPolygonB && (rel & PolygonRelationship.BInsideA) != 0b0 == otherIsPositive))   //part of A and either 1) B is inside of it and B is positive or 2) it is inside B and B is negative
                    return;
            } while (enumerator.MoveNext());
            newPolygons.Add(subPolygon.Copy(false, false));
        }
    }
}
