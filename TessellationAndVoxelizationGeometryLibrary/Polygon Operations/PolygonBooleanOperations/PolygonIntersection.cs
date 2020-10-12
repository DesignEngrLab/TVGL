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
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons,
            bool identicalPolygonIsInverted)
        {
            if (!identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }
        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            using var enumerator = relationships.GetEnumerator();
            enumerator.MoveNext();
            while (enumerator.MoveNext())
            {
                var rel = enumerator.Current.Item1;
                var isPos = enumerator.Current.Item2;
                // these  conditions follow from the Table-Handling-Non-Contacting-Polygons-in-Intersection.png
                if ((subPolygon.IsPositive &&
                    ((isPos && rel < PolyRelInternal.AInsideB) || // Separeated
                        (!partOfPolygonB && isPos && (rel & PolyRelInternal.BInsideA) != 0b0) ||  //both are positive and B is inside A
                        (!partOfPolygonB && !isPos && (rel & PolyRelInternal.AInsideB) != 0b0) ||  //A is inside a hole of B
                        (partOfPolygonB && isPos && (rel & PolyRelInternal.AInsideB) != 0b0) ||  // B is inside a hole of A
                        (partOfPolygonB && !isPos && (rel & PolyRelInternal.BInsideA) != 0b0)))  // B is inside a hole of A
                    ||
                    (!subPolygon.IsPositive &&
                    ((!partOfPolygonB && !isPos && (rel & PolyRelInternal.AInsideB) != 0b0) || //A and B are neg, but B encloses A
                     (!partOfPolygonB && isPos && rel < PolyRelInternal.AInsideB) || //A is negative, B is positive but they are separate
                     (!partOfPolygonB && isPos && (rel & PolyRelInternal.BInsideA) != 0b0) || //A is negative, B is positive but B is inside A
                    (partOfPolygonB && !isPos && (rel & PolyRelInternal.BInsideA) != 0b0) || //A and B are neg, but A encloses B
                     (partOfPolygonB && isPos && rel < PolyRelInternal.AInsideB) || //B is negative, A is positive but they are separate
                     (partOfPolygonB && isPos && (rel & PolyRelInternal.AInsideB) != 0b0)))) //B is negative, A is positive but A is inside B
                    return; // these follow the 3 "Neither (empty result) from Table-Handling-Non-Contacting-Polygons-in-Intersection.png
            }
            newPolygons.Add(subPolygon.Copy(false, false));
        }
    }
}