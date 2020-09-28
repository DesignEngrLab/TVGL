using System.Collections.Generic;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonIntersection : PolygonBooleanBase
    {
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData,
            out PolygonEdge currentEdge,
            out bool startAgain)
        {
            startAgain = false;
            if (intersectionData.Relationship == SegmentRelationship.DoubleOverlap)
            {
                if (intersectionData.CollinearityType == CollinearityTypes.ABeforeBAfter && !intersectionData.VisitedA && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    return true;
                }
                if (intersectionData.CollinearityType == CollinearityTypes.AAfterBBefore && !intersectionData.VisitedA && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeA;
                    return true;
                }
                if (intersectionData.CollinearityType == CollinearityTypes.None)
                {
                    startAgain = !(intersectionData.VisitedB || intersectionData.VisitedA);
                    if (!intersectionData.VisitedA)
                    {
                        currentEdge = intersectionData.EdgeA;
                        return true;
                    }
                    if (!intersectionData.VisitedB)
                    {
                        currentEdge = intersectionData.EdgeB;
                        return true;
                    }
                }
            }
            if (intersectionData.VisitedB || intersectionData.VisitedA)
            {
                currentEdge = null;
                return false;
            }
            if (intersectionData.Relationship == SegmentRelationship.AEnclosesB && !intersectionData.VisitedB)
            {
                currentEdge = intersectionData.EdgeB;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.BEnclosesA && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeA;
                return true;
            }

            if (intersectionData.Relationship == SegmentRelationship.CrossOver_AOutsideAfter && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeA;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.CrossOver_BOutsideAfter && !intersectionData.VisitedB)
            {
                currentEdge = intersectionData.EdgeB;
                return true;
            }
            currentEdge = null;
            return false;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection intersectionData, bool currentEdgeIsFromPolygonA)
        {
            if (intersectionData.Relationship == SegmentRelationship.DoubleOverlap) return true;
            if (!currentEdgeIsFromPolygonA)
            {
                return
                    intersectionData.Relationship == SegmentRelationship.CrossOver_BOutsideAfter ||
                    intersectionData.Relationship == SegmentRelationship.BEnclosesA;
            }
            return intersectionData.Relationship == SegmentRelationship.CrossOver_AOutsideAfter ||
                intersectionData.Relationship == SegmentRelationship.AEnclosesB;
        }

        protected override bool PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection, PolygonEdge currentEdge, PolygonEdge startingEdge)
        {
            if (startingIntersection != currentIntersection) return false;
            if (startingIntersection.Relationship == SegmentRelationship.DoubleOverlap &&
                startingIntersection.CollinearityType == CollinearityTypes.None)
                return currentEdge == startingEdge;
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