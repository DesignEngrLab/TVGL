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
            if (intersectionData.VisitedA || intersectionData.VisitedB)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            // Overlapping. The conventional case where A and B cross into one another
            if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping &&
                (intersectionData.Relationship & PolygonSegmentRelationship.BothLinesStartAtPoint) == PolygonSegmentRelationship.BothLinesStartAtPoint)
            {
                var lineA = intersectionData.EdgeA;
                var lineB = intersectionData.EdgeB;
                var lineACrossLineB = lineA.Vector.Cross(lineB.Vector);
                var prevA = intersectionData.EdgeA.FromPoint.EndLine;
                var aCornerCross = prevA.Vector.Cross(lineA.Vector);
                var prevACrossLineB = prevA.Vector.Cross(lineB.Vector);
                var lineBIsInsideA = (aCornerCross >= 0 && lineACrossLineB > 0 && prevACrossLineB > 0) ||
                                     (aCornerCross < 0 && !(lineACrossLineB <= 0 && prevACrossLineB <= 0));
                if (lineBIsInsideA && !intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                if (!intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
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

        protected override bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA)
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
