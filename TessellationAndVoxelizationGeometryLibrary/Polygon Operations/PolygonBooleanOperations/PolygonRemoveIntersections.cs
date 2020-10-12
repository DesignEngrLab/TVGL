// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
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
        /// <param name="makeHolesPositive">if set to <c>true</c> [make holes positive].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="strayHoles">The stray holes.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        internal List<Polygon> Run(Polygon polygon, List<SegmentIntersection> intersections, bool makeHolesPositive, double tolerance,
            out List<Polygon> strayHoles)
        {
            var minAllowableArea = tolerance * tolerance; // / Constants.BaseTolerance;
            var interaction = new PolygonInteractionRecord(polygon, null);
            interaction.IntersectionData.AddRange(intersections);
            var delimiters = NumberVerticesAndGetPolygonVertexDelimiter(polygon);
            var intersectionLookup = interaction.MakeIntersectionLookupList(delimiters[^1]);
            var newPolygons = new List<Polygon>();
            var indexIntersectionStart = 0;
            while (GetNextStartingIntersection(intersections, out var startingIntersection,
                out var startEdge, out var switchPolygon, ref indexIntersectionStart))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, switchPolygon).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (makeHolesPositive && area < 0) polyCoordinates.Reverse();
                newPolygons.Add(new Polygon(polyCoordinates));
            }
            return newPolygons.CreateShallowPolygonTrees(true, out strayHoles);
        }

        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonEdge currentEdge, out bool startAgain)
        {
            startAgain = false;
            if (intersectionData.VisitedB && intersectionData.VisitedA)
            {
                currentEdge = null;
                return false;
            }
            if (intersectionData.Relationship == SegmentRelationship.NoOverlap)
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

        protected override bool PolygonCompleted(SegmentIntersection currentIntersection, SegmentIntersection startingIntersection,
            PolygonEdge currentEdge, PolygonEdge startingEdge)
        {
            return startingIntersection == currentIntersection && currentEdge == startingEdge;
        }

        //private bool lastSwitch = false;
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
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        ///
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool identicalPolygonIsInverted)
        {
            if (!identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
            // otherwise if the copy is inverted then the two cancel each other out and neither is explicitly needed in the result.
            // a hole is effectively removed
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons, IEnumerable<(PolyRelInternal, bool)> relationships, bool partOfPolygonB)
        {
            newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }
    }
}