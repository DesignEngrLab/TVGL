// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonUnion : PolygonBooleanBase
    {
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonEdge currentEdge,
            out bool startAgain)
        {
            startAgain = false;
            if (intersectionData.Relationship == SegmentRelationship.NoOverlap)
            {
                if (intersectionData.CollinearityType == CollinearityTypes.ABeforeBAfter && !intersectionData.VisitedA && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    return true;
                }
                if (intersectionData.CollinearityType == CollinearityTypes.AAfterBBefore && !intersectionData.VisitedB && !intersectionData.VisitedA)
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
            if (intersectionData.Relationship == SegmentRelationship.AEnclosesB && !intersectionData.VisitedB && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeA;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.BEnclosesA && !intersectionData.VisitedB && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeB;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.CrossOver_AOutsideAfter && !intersectionData.VisitedB)
            {
                currentEdge = intersectionData.EdgeB;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.CrossOver_BOutsideAfter && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeA;
                return true;
            }
            currentEdge = null;
            return false;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection intersectionData, bool currentEdgeIsFromPolygonA)
        {
            if (intersectionData.Relationship == SegmentRelationship.NoOverlap) return true;
            if (currentEdgeIsFromPolygonA)
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
            if (startingIntersection.Relationship == SegmentRelationship.NoOverlap &&
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
            // otherwise if the copy is inverted then the two cancel each other out and neither is explicitly needed in the result.
            // a hole is effectively removed
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            using var enumerator = relationships.GetEnumerator();
            enumerator.MoveNext();
            var rel = enumerator.Current.Item1;
            if (rel < PolyRelInternal.AInsideB ||  //separated
                (!partOfPolygonB && (rel & PolyRelInternal.BInsideA) != 0b0) || //subPolygon is part of A and it encompasses the B (BInsideA)
                (partOfPolygonB && (rel & PolyRelInternal.AInsideB) != 0b0)) //subPolygon is part of B and it encompasses the A (AInsideB)
                //  whether positive or negative - it is included
                newPolygons.Add(subPolygon.Copy(false, false));
            else
            { //failing the above if means that it is included in the outer. So it can only be included in the result if it is inside a hole
                while (enumerator.MoveNext())
                {
                    rel = enumerator.Current.Item1;
                    if ((!partOfPolygonB && (rel & PolyRelInternal.AInsideB) != 0b0) ||
                        (partOfPolygonB && (rel & PolyRelInternal.BInsideA) != 0b0))
                    {
                        newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negatie as a negative
                        return;                                                               // otherwise, the polygon has no effect since it is a subset of the other
                    }
                }
            }
        }
    }
}