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
            if (intersectionData.VisitedA && intersectionData.VisitedB)
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
                if (lineBIsInsideA && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
                if (!intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
            }
            // Overlapping. The conventional case where A and B cross into one another
            if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping)
            {
                var cross = intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);
                if (cross < 0 && !intersectionData.VisitedA)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
                if (cross > 0 && !intersectionData.VisitedB)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
            }
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) == 0b0 //same direction
              && (intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0)  //splits after this point
            {
                if (!intersectionData.VisitedB && (intersectionData.Relationship & PolygonSegmentRelationship.BEncompassesA) != 0b0)
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = false;
                    return true;
                }
                if (!intersectionData.VisitedA && (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = false;
                    return true;
                }
            }
            // "Glance" A and B touch but are only abutting one another. no overlap in regions
            else if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0 &&
                 (intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0 &&
                 (intersectionData.Relationship & PolygonSegmentRelationship.CoincidentLines) != 0b0)
            { //the only time non-overlapping intersections are intereseting is when we are doing union and lines are coincident
              // otherwise you simply stay on the same polygon you enter with
                if (!intersectionData.VisitedB && (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                     (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0)
                    ||
                    ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                      (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)))
                {
                    currentEdge = intersectionData.EdgeB;
                    switchPolygon = true;
                    return true;
                }
                else if (!intersectionData.VisitedA &&
                    (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                          (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)
                         ||
                         ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                          (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0)))
                {
                    currentEdge = intersectionData.EdgeA;
                    switchPolygon = true;
                    return true;
                }
            }
            else if (intersectionData.Relationship == PolygonSegmentRelationship.BothLinesStartAtPoint)
            {
                currentEdge =!intersectionData.VisitedA ? intersectionData.EdgeA : intersectionData.EdgeB;
                switchPolygon = true;
                return true;

            }
            currentEdge = null;
            switchPolygon = false;
            return false;
        }

        protected override bool SwitchAtThisIntersection(SegmentIntersection newIntersection, bool currentEdgeIsFromPolygonA)
        {
            if (!base.SwitchAtThisIntersection(newIntersection, currentEdgeIsFromPolygonA)) return false;
            return (newIntersection.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping;
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
