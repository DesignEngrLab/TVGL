// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonUnion.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonUnion : PolygonBooleanBase
    {
        /// <summary>
        /// Valids the starting intersection.
        /// </summary>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="startAgain">if set to <c>true</c> [start again].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData,
            out PolygonEdge currentEdge, out bool startAgain)
        {
            startAgain = false;
            if (intersectionData.Relationship == SegmentRelationship.NoOverlap)
            {
                if (intersectionData.CollinearityType == CollinearityTypes.ABeforeBAfter 
                    && !intersectionData.VisitedA && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    return true;
                }
                if (intersectionData.CollinearityType == CollinearityTypes.AAfterBBefore
                    && !intersectionData.VisitedB && !intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    return true;
                }
                if (intersectionData.CollinearityType == CollinearityTypes.None)
                {
                    startAgain = !(intersectionData.VisitedB || intersectionData.VisitedA); // true if both false
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
            // Double_Overlap or Abutting
            currentEdge = null;
            return false;
        }

        /// <summary>
        /// Switches at this intersection.
        /// </summary>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdgeIsFromPolygonA">if set to <c>true</c> [current edge is from polygon a].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool SwitchAtThisIntersection(SegmentIntersection intersectionData, bool currentEdgeIsFromPolygonA)
        {
            if ((currentEdgeIsFromPolygonA && intersectionData.Relationship == SegmentRelationship.AEnclosesB) ||
                (!currentEdgeIsFromPolygonA && intersectionData.Relationship == SegmentRelationship.BEnclosesA))
                return false;
            return true;
        }

        /// <summary>
        /// Polygons the completed.
        /// </summary>
        /// <param name="currentIntersection">The current intersection.</param>
        /// <param name="startingIntersection">The starting intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="startingEdge">The starting edge.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection, PolygonEdge currentEdge, PolygonEdge startingEdge)
        {
            if ((currentEdge == currentIntersection.EdgeA && currentIntersection.VisitedA) ||
             (currentEdge == currentIntersection.EdgeB && currentIntersection.VisitedB))
                return true;
            if (startingIntersection != currentIntersection) return false;
            if (currentIntersection.Relationship == SegmentRelationship.NoOverlap &&
                currentIntersection.CollinearityType == CollinearityTypes.None)
                return currentEdge == startingEdge;
            return true;
        }

        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygonA">The sub polygon a.</param>
        /// <param name="newPolygons">The new polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool identicalPolygonIsInverted)
        {
            if (!identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
            // otherwise if the copy is inverted then the two cancel each other out and neither is explicitly needed in the result.
            // a hole is effectively removed
        }

        /// <summary>
        /// Handles the non intersecting sub polygon.
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="newPolygons">The new polygons.</param>
        /// <param name="relationships">The relationships.</param>
        /// <param name="partOfPolygonB">if set to <c>true</c> [part of polygon b].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            foreach (var relData in relationships)
            {
                var rel = relData.Item1;
                var otherIsPos = relData.Item2;
                var isInsideOther = (!partOfPolygonB && (rel & PolyRelInternal.AInsideB) != 0b0) ||
                    (partOfPolygonB && (rel & PolyRelInternal.BInsideA) != 0b0);
                // these  conditions follow from the Table-Handling-Non-Contacting-Polygons-in-Union.png
                if (otherIsPos && !isInsideOther) return true; // B is positive, so keep this polygon so long as it 
                // isn't inside the other - that is either separated or enclosing the other. You still may end up 
                // keeping it if it is in a hole of the other.
                if (!otherIsPos && isInsideOther) return true; //A is inside a hole of B
            }
            return false;
        }
    }
}