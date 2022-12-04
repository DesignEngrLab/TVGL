// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;

namespace TVGL
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

        protected override bool SwitchAtThisIntersection(SegmentIntersection intersectionData, bool currentEdgeIsFromPolygonA, bool shapeIsOnlyNegative)
        {
            if (intersectionData.Relationship == SegmentRelationship.DoubleOverlap) return true;
            if ((currentEdgeIsFromPolygonA && (intersectionData.Relationship == SegmentRelationship.BEnclosesA ||
                intersectionData.Relationship == SegmentRelationship.CrossOver_BOutsideAfter)) ||
                (!currentEdgeIsFromPolygonA && (intersectionData.Relationship == SegmentRelationship.AEnclosesB ||
                intersectionData.Relationship == SegmentRelationship.CrossOver_AOutsideAfter)))
                return shapeIsOnlyNegative;
            return true;
        }

        protected override bool? PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection, PolygonEdge currentEdge, PolygonEdge startingEdge)
        {
            //if (currentIntersection.Relationship == SegmentRelationship.NoOverlap) return null;
            // can't reach a NoOverlap in Intersection, but this happens sometimes when following a collinear edge
            if ((currentEdge == currentIntersection.EdgeA && currentIntersection.VisitedA) ||
             (currentEdge == currentIntersection.EdgeB && currentIntersection.VisitedB))
                return true;
            if (startingIntersection != currentIntersection) return false;
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
        protected override bool HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            var firstPass = true;
            var otherIsPos = false;
            var isInsideOther = false;
            foreach (var relData in relationships)
            {
                var rel = relData.Item1;
                otherIsPos = relData.Item2;
                isInsideOther = (!partOfPolygonB && (rel & PolyRelInternal.AInsideB) != 0b0) ||
                    (partOfPolygonB && (rel & PolyRelInternal.BInsideA) != 0b0);

                if (firstPass)
                {
                    if (!isInsideOther) return !otherIsPos; //separated or enclosesOther is false. only 
                                                            // isInsideOther being true is the only way forward
                                                            //if (!otherIsPos && isInsideOther) return false; //A is inside a hole of B
                    firstPass = false;
                }
                else if (isInsideOther) return otherIsPos;
            }
            if (isInsideOther) return otherIsPos;
            return !otherIsPos;
        }
    }
}