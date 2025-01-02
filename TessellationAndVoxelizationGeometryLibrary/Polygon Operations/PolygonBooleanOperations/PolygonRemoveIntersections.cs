// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonRemoveIntersections.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonRemoveIntersections : PolygonBooleanBase
    {
        /// <summary>
        /// Runs the specified polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <param name="knownWrongPoints">The known wrong points.</param>
        /// <param name="maxNumberOfPolygons">The maximum number of polygons.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        internal List<Polygon> Run(Polygon polygon, List<SegmentIntersection> intersections, ResultType resultType,
            List<bool> knownWrongPoints, int maxNumberOfPolygons)
        {
            var interaction = new PolygonInteractionRecord(polygon, null);
            interaction.IntersectionData.AddRange(intersections);
            var delimiters = NumberVerticesAndGetPolygonVertexDelimiter(polygon);
            var intersectionLookup = interaction.MakeIntersectionLookupList(delimiters[^1]);
            var newPolygons = new List<Polygon>();
            var indexIntersectionStart = 0;
            while (GetNextStartingIntersection(intersections, out var startingIntersection,
                out var startEdge, out var switchPolygon, ref indexIntersectionStart))
            {
                var newPolygon = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, switchPolygon, out var includesWrongPoints, knownWrongPoints);
                if (includesWrongPoints) continue;
                var area = newPolygon.Area;
                if (area.IsNegligible(polygon.Area * Constants.PolygonSameTolerance)) continue;
                if (area * (int)resultType < 0) // note that the ResultType enum has assigned negative values that are used
                                                //in conjunction with the area of the sign. Only if the product is negative - do we do something 
                {
                    if (resultType == ResultType.OnlyKeepNegative || resultType == ResultType.OnlyKeepPositive) continue;
                    else newPolygon.Reverse();
                }
                newPolygon.SimplifyMinLength(polygon.Area * Constants.PolygonSameTolerance);
                newPolygons.Add(newPolygon);
            }
            return newPolygons.OrderByDescending(p => Math.Abs(p.Area))
                .Take(maxNumberOfPolygons).Reverse()
                .CreateShallowPolygonTrees(true, true);
        }

        /// <summary>
        /// Valids the starting intersection.
        /// </summary>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="startAgain">if set to <c>true</c> [start again].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonEdge currentEdge, out bool startAgain)
        {
            startAgain = false;
            if (intersectionData.VisitedB && intersectionData.VisitedA)
            {
                currentEdge = null;
                return false;
            }
            if (intersectionData.Relationship == SegmentRelationship.NoOverlap
                || intersectionData.Relationship == SegmentRelationship.Abutting)
            {
                currentEdge = null;
                return false;
            }
            startAgain = !(intersectionData.VisitedB || intersectionData.VisitedA);
            if (intersectionData.Relationship == SegmentRelationship.AEnclosesB && !intersectionData.VisitedA)
            {
                currentEdge = intersectionData.EdgeA;
                return true;
            }
            if (intersectionData.Relationship == SegmentRelationship.BEnclosesA && !intersectionData.VisitedB)
            {
                currentEdge = intersectionData.EdgeB;
                return true;
            }
            currentEdge = intersectionData.VisitedA ? intersectionData.EdgeB : intersectionData.EdgeA;
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
        protected override bool PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection,
            PolygonEdge currentEdge, PolygonEdge startingEdge)
        {
            return startingIntersection == currentIntersection && currentEdge == startingEdge;
        }

        //private bool lastSwitch = false;
        /// <summary>
        /// Switches at this intersection.
        /// </summary>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdgeIsFromPolygonA">if set to <c>true</c> [current edge is from polygon a].</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool SwitchAtThisIntersection(SegmentIntersection intersectionData, bool currentEdgeIsFromPolygonA)
        {
            if (intersectionData.Relationship == SegmentRelationship.CrossOver_AOutsideAfter ||
                intersectionData.Relationship == SegmentRelationship.CrossOver_BOutsideAfter ||
                intersectionData.Relationship == SegmentRelationship.DoubleOverlap)
                return true;
            //if (intersectionData.Relationship == SegmentRelationship.AEnclosesB)
            //    return !currentEdgeIsFromPolygonA;
            //if (intersectionData.Relationship == SegmentRelationship.BEnclosesA)
            //    return currentEdgeIsFromPolygonA;
            //if (intersectionData.Relationship == SegmentRelationship.NoOverlap)
            return false;
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
        protected override bool HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons, IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            return true;
            //newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }
    }
}