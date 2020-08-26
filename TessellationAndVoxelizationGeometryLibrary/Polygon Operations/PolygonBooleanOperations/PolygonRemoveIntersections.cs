using System;
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
        internal List<Polygon> Run(Polygon polygon, List<SegmentIntersection> intersections, bool noHoles, double minAllowableArea,
            out List<Polygon> strayHoles)
        {
            var interaction = new PolygonInteractionRecord(PolygonRelationship.Separated, intersections, null, null, 0, 0);
            // new Dictionary<Polygon, int> { { polygon, 0 } }, 1, 0);
            var delimiters = polygon.NumberVertiesAndGetPolygonVertexDelimiter();
            var intersectionLookup = interaction.MakeIntersectionLookupList(delimiters[^1]);
            var newPolygons = new List<Polygon>();

            while (GetNextStartingIntersection(intersections, out var startingIntersection,
                out var startEdge, out var switchPolygon))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, switchPolygon).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (noHoles) polyCoordinates.Reverse();
                newPolygons.Add(new Polygon(polyCoordinates));
            }
            return newPolygons.CreateShallowPolygonTrees(true, out _, out strayHoles);
        }

        protected override bool ValidStartingIntersection(SegmentIntersection intersectionData, out PolygonSegment currentEdge, out bool switchPolygon)
        {
            if (intersectionData.VisitedA || intersectionData.VisitedB ||
                (intersectionData.Relationship & PolygonSegmentRelationship.Interfaces) != PolygonSegmentRelationship.Crossover)
            {
                currentEdge = null;
                switchPolygon = false;
                return false;
            }
            switchPolygon = true;
            var AMovesInside = (intersectionData.Relationship & PolygonSegmentRelationship.AMovesInside) != 0b0;
            currentEdge = AMovesInside ? intersectionData.EdgeB : intersectionData.EdgeA;
            return true;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA)
        {
            if (!base.SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA)) return false;
            PolygonSegmentRelationship whichInterface = newIntersection.Relationship & PolygonSegmentRelationship.Interfaces;
            return whichInterface == PolygonSegmentRelationship.Crossover;
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

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons, IEnumerable<(PolygonRelationship, bool)> relationships, bool partOfPolygonB)
        {
            newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }
    }
}
