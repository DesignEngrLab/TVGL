using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Gets the polygon relationship of PolygonA to PolygonB and the intersections between them.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>PolygonRelationship.</returns>
        public static PolygonRelationship GetPolygonRelationshipAndIntersections(this Polygon polygonA, Polygon polygonB,
    out List<IntersectionData> intersections)
        {
            intersections = new List<IntersectionData>();
            //As a first check, determine if the axis aligned bounding boxes overlap. If not, then we can
            // safely return that the polygons are separated.
            if (polygonA.MinX > polygonB.MaxX ||
                polygonA.MaxX < polygonB.MinX ||
                polygonA.MinY > polygonB.MaxY ||
                polygonA.MaxY < polygonB.MinY) return PolygonRelationship.Separated;
            //Else, we need to check for intersections between all lines of the two
            // To avoid an n-squared check (all of A's lines with all of B's), we sort the lines by their XMin
            // value. Instead of directly sorting the Lines, which will have many repeat XMin values (since every
            // pair of lines meet at the a given point), the points are sorted first. These sorted points are
            // used in the ArePointsInsidePolygon function as well.
            // the prev. 3 lines a simpler solution would be the following line.
            var orderedAPoints = polygonA.AllPolygons.SelectMany(poly => poly.Vertices).OrderBy(p => p.X).ToList();
            if (polygonA.Lines.Count == polygonB.Lines.Count) ; //this silly little line is simply to ensure that the Lines
            // property has been invoked before the next function (GetOrderedLines), which requires that lines and vertices be properly connected
            var aLines = GetOrderedLines(orderedAPoints);
            //repeat for the lines in B
            var orderedBPoints = (polygonB.AllPolygons.SelectMany(poly => poly.Vertices)).OrderBy(p => p.X).ToList();
            var bLines = GetOrderedLines(orderedBPoints);

            var aIndex = 0;
            var bIndex = 0;
            while (aIndex < aLines.Length && bIndex < bLines.Length) // this while loop increments both B lines and A lines
            {
                if (aLines[aIndex].XMin < bLines[bIndex].XMin) // if the next A-line is lower compare it to all B-lines
                {
                    var aLine = aLines[aIndex++];
                    var localBIndex = bIndex; //the localBIndex is incremented in the following loop, but we
                                              //need to come back to the main bIndex above
                    while (localBIndex < bLines.Length && aLine.XMax > bLines[localBIndex].XMin)
                    {  // the real savings comes from the second condition in the while loop. We do not need to check bLines
                        // that have higher XMin than the current aLine's xMax. In this way, the number of comparisons is greatly limited
                        var bLine = bLines[localBIndex++];
                        var segmentRelationship = aLine.PolygonSegmentIntersection(bLine, out var intersection);
                        if (segmentRelationship >= 0) //the separated PolygonSegmentRelationship has a value of -1 all meaningful
                                                      //interactions are 0 or greate
                            intersections.Add(new IntersectionData(aLine, bLine, intersection,
                                segmentRelationship));
                    }
                }          // I hate that there is duplicate code here, but I don't know if there is a better way
                else       // (I tried several). Other than the name, the blocking difference is in the last
                           // IntersectionData constructor where the order of A then B is used in defining segmentA 
                {          // and EdgeB
                    var bLine = bLines[bIndex++];
                    var localAIndex = aIndex;
                    while (localAIndex < aLines.Length && bLine.XMax > aLines[localAIndex].XMin)
                    {
                        var aLine = aLines[localAIndex++];
                        var segmentRelationship = bLine.PolygonSegmentIntersection(aLine, out var intersection);
                        if (segmentRelationship >= 0)
                            intersections.Add(new IntersectionData(aLine, bLine, intersection,
                                segmentRelationship));
                    }
                }
            }
            if (intersections.Any(intersect => intersect.Relationship == PolygonSegmentRelationship.IntersectNominal))
            {
                if (ArePointsInsidePolygonLines(aLines, aLines.Length, orderedBPoints, out _, false))
                    return PolygonRelationship.BVerticesInsideAButLinesIntersect;
                if (ArePointsInsidePolygonLines(bLines, bLines.Length, orderedAPoints, out _, false))
                    return PolygonRelationship.AVerticesInsideBButLinesIntersect;
                return PolygonRelationship.Intersect;
            }

            bool onBoundary;
            if (BoundingBoxEncompasses(polygonA, polygonB))
            {
                foreach (var hole in polygonA.Holes)
                {
                    if (hole.IsNonIntersectingPolygonInside(polygonB, out onBoundary))
                    {
                        if (onBoundary) return PolygonRelationship.BInsideAButBordersTouch;
                        return PolygonRelationship.BIsInsideHoleOfA;
                    }
                }
                if (polygonA.IsNonIntersectingPolygonInside(polygonB, out onBoundary))
                {
                    if (onBoundary) return PolygonRelationship.BInsideAButBordersTouch;
                    return PolygonRelationship.BIsCompletelyInsideA;
                }
            }
            else if (BoundingBoxEncompasses(polygonB, polygonA))
            {
                foreach (var hole in polygonB.Holes)
                {
                    if (hole.IsNonIntersectingPolygonInside(polygonA, out onBoundary))
                    {
                        if (onBoundary) return PolygonRelationship.AInsideBButBordersTouch;
                        return PolygonRelationship.AIsInsideHoleOfB;
                    }
                }
                if (polygonB.IsNonIntersectingPolygonInside(polygonA, out onBoundary))
                {
                    if (onBoundary) return PolygonRelationship.AInsideBButBordersTouch;
                    return PolygonRelationship.AIsCompletelyInsideB;
                }
            }
            return PolygonRelationship.Separated;
        }

        private static bool BoundingBoxEncompasses(this Polygon polygonA, Polygon polygonB)
        {
            return (polygonA.MaxX >= polygonB.MaxX
                && polygonA.MaxY >= polygonB.MaxY
                && polygonA.MinX <= polygonB.MinX
                && polygonA.MinY <= polygonB.MinY);
        }


        private static PolygonSegment[] GetOrderedLines(List<Vertex2D> orderedPoints)
        {
            var length = orderedPoints.Count;
            var result = new PolygonSegment[length];
            var smallHashOfLinesOfEqualX = new HashSet<PolygonSegment>();
            var k = 0;
            for (int i = 0; i < length; i++)
            {
                var point = orderedPoints[i];
                if (point.EndLine.OtherPoint(point).X > point.X)
                    result[k++] = point.EndLine;
                else if (point.EndLine.OtherPoint(point).X == point.X &&
                         !smallHashOfLinesOfEqualX.Contains(point.EndLine))
                {
                    result[k++] = point.EndLine;
                    smallHashOfLinesOfEqualX.Add(point.EndLine);
                }
                if (point.StartLine.OtherPoint(point).X > point.X)
                    result[k++] = point.StartLine;
                else if (point.StartLine.OtherPoint(point).X == point.X &&
                         !smallHashOfLinesOfEqualX.Contains(point.StartLine))
                {
                    result[k++] = point.StartLine;
                    smallHashOfLinesOfEqualX.Add(point.StartLine);
                }

                if (k >= length) break;
            }
            return result;
        }

        private static List<IntersectionData> GetSelfIntersections(this Polygon polygonA)
        {
            var intersections = new List<IntersectionData>();
            var numLines = polygonA.Lines.Count;
            var orderedLines = polygonA.Lines.OrderBy(line => line.XMin).ToList();
            for (int i = 0; i < numLines - 1; i++)
            {
                var current = orderedLines[i];
                for (int j = i + 1; j < numLines; j++)
                {
                    var other = orderedLines[j];
                    if (current.XMax < orderedLines[j].XMin) break;
                    if (current.IsAdjacentTo(other)) continue;
                    var segmentRelationship = current.PolygonSegmentIntersection(other, out var intersection);
                    if (segmentRelationship >= 0)
                        intersections.Add(new IntersectionData(current, other, intersection,
                            segmentRelationship));
                }
            }
            return intersections;
        }

        #region Boolean Operations
        public static Polygon RemoveSelfIntersections(this Polygon polygon, double minAllowableArea = Constants.BaseTolerance)
        {
            return RemoveSelfIntersections(polygon, polygon.GetSelfIntersections(), minAllowableArea);
        }
        public static Polygon RemoveSelfIntersections(this Polygon polygon, List<IntersectionData> intersections,
            double minAllowableArea = Constants.BaseTolerance)
        {
            if (intersections.Count == 0) return polygon.Copy();
            var intersectionLookup = MakeIntersectionLookupList(polygon.Lines.Count, intersections);
            var positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store positive polygons in increasing area
            var negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersectionLookup, intersections, -1, out var startingIntersection,
                out var startEdge))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, false).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates, false));
                else positivePolygons.Add(area, new Polygon(polyCoordinates, false));
            }
            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values)[0];
        }

        /// <summary>
        /// Gets the next intersection by looking through the intersectionLookupList. It'll return false, when there are none left.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="onlyVisitOnce">if set to <c>true</c> [only visit once].</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="intersectionIndex">Index of the intersection.</param>
        /// <param name="currentEdgeIndex">Index of the current edge.</param>
        /// <returns><c>true</c> if a new starting intersection was found, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool GetNextStartingIntersection(List<int>[] intersectionLookup, List<IntersectionData> intersections,
         int crossProductSign, out IntersectionData nextStartingIntersection, out PolygonSegment currentEdge)
        {
            for (int edgeIndex = 0; edgeIndex < intersectionLookup.Length; edgeIndex++)
            {
                if (intersectionLookup[edgeIndex] == null) continue;
                foreach (var index in intersectionLookup[edgeIndex])
                {
                    var intersectionData = intersections[index];
                    if (intersectionData.Visited) continue;
                    var enteringEdgeA = edgeIndex == intersectionData.EdgeA.IndexInList;
                    var cross = (enteringEdgeA ? 1 : -1)
                                // cross product is from the entering edge to the other. We use the "enteringEdgeA" boolean to flip the sign if we are really entering B
                                * intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);

                    if (crossProductSign * cross < 0) continue; //cross product does not have expected sign. Instead, the intersection will have
                    // to be entered from the other edge

                    // what about when crossProduct is zero - like in a line Intersection.Relationship will be in line
                    currentEdge = enteringEdgeA ? intersectionData.EdgeA : intersectionData.EdgeB;
                    nextStartingIntersection = intersectionData;
                    return true;
                }
            }

            nextStartingIntersection = null;
            currentEdge = null;
            return false;
        }

        /// <summary>
        /// Makes the polygon through intersections.
        /// </summary>
        /// <param name="intersectionLookup">The readonly intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="intersectionIndex">The index of new intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="onlyVisitOnce">if set to <c>true</c> [only visit once].</param>
        /// <param name="switchDirections">if set to <c>true</c> [switch directions].</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup,
            List<IntersectionData> intersections, IntersectionData intersectionData, PolygonSegment currentEdge, bool switchDirections)
        {
            var newPath = new List<Vector2>();
            var forward = true; // as in following the edges in the forward direction (from...to). If false, then traverse backwards
            while (!intersectionData.Visited)
            {
                currentEdge = currentEdge == intersectionData.EdgeA ? intersectionData.EdgeB : intersectionData.EdgeA;
                intersectionData.Visited = true;
                if (intersectionData.Relationship == PolygonSegmentRelationship.CollinearAndOverlapping
                    || intersectionData.Relationship == PolygonSegmentRelationship.ConnectInT
                    || intersectionData.Relationship == PolygonSegmentRelationship.EndPointsTouch)
                    throw new NotImplementedException();
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                newPath.Add(intersectionCoordinates);
                int intersectionIndex;
                if (switchDirections) forward = !forward;
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                   intersectionCoordinates, forward, out intersectionIndex))
                {
                    if (forward)
                    {
                        newPath.Add(currentEdge.ToPoint.Coordinates);
                        currentEdge = currentEdge.ToPoint.StartLine;
                    }
                    else
                    {
                        newPath.Add(currentEdge.FromPoint.Coordinates);
                        currentEdge = currentEdge.FromPoint.EndLine;
                    }
                    intersectionCoordinates = Vector2.Null;
                }
                intersectionData = intersections[intersectionIndex];
            }

            return newPath;
        }

        private static bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<IntersectionData> allIntersections,
        Vector2 formerIntersectCoords, bool forward, out int indexOfIntersection)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            indexOfIntersection = -1;
            if (intersectionIndices == null)
                return false;
            var minDistanceToIntersection = double.PositiveInfinity;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                double distance;
                if (thisIntersectData.Relationship == PolygonSegmentRelationship.CollinearAndOverlapping)
                {
                    var otherLine = (thisIntersectData.EdgeA == currentEdge) ? thisIntersectData.EdgeB : thisIntersectData.EdgeA;
                    var fromDist = currentEdge.Vector.Dot(otherLine.FromPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var toDist = currentEdge.Vector.Dot(otherLine.ToPoint.Coordinates - currentEdge.FromPoint.Coordinates);
                    var thisLength = currentEdge.Vector.LengthSquared();
                    throw new NotImplementedException();
                }

                var vector = forward ? currentEdge.Vector : -currentEdge.Vector;
                var datum = !formerIntersectCoords.IsNull() ? formerIntersectCoords :
                    forward ? currentEdge.FromPoint.Coordinates : currentEdge.ToPoint.Coordinates;
                distance = vector.Dot(thisIntersectData.IntersectCoordinates - datum);
                if (distance > 0 && minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    indexOfIntersection = index;
                }
            }
            return indexOfIntersection >= 0;
        }

        private static List<int>[] MakeIntersectionLookupList(int numLines, List<IntersectionData> intersections)
        {
            var result = new List<int>[numLines];
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                intersection.Visited = false;
                var index = intersection.EdgeA.IndexInList;
                result[index] ??= new List<int>();
                result[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                result[index] ??= new List<int>();
                result[index].Add(i);
            }
            return result;
        }

        #endregion


        #region New Boolean Operations

        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Union(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship, List<IntersectionData> intersections,
            double minAllowableArea = Constants.BaseTolerance)
        {
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy = polygonA.Copy();
                    if (!polygonB.IsPositive)
                        polygonACopy.AddHole(polygonB.Copy());
                    return new List<Polygon> { polygonACopy };
                case PolygonRelationship.AIsCompletelyInsideB:
                    var polygonBCopy = polygonB.Copy();
                    if (!polygonA.IsPositive)
                        polygonBCopy.AddHole(polygonA.Copy());
                    return new List<Polygon> { polygonBCopy };

                //case PolygonRelationship.Intersect:
                //case PolygonRelationship.SeparatedButBordersTouch:
                //case PolygonRelationship.BVerticesInsideAButLinesIntersect:
                //case PolygonRelationship.BInsideAButBordersTouch:
                //case PolygonRelationship.AVerticesInsideBButLinesIntersect:
                //case PolygonRelationship.AInsideBButBordersTouch:
                default:
                    return BooleanOperation(polygonA, polygonB, intersections, false, -1, minAllowableArea);
            }
        }
        public static List<Polygon> BooleanOperation(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, bool switchDirection,
            int crossProductSign, double minAllowableArea = Constants.BaseTolerance)
        {
            var id = 0;
            foreach (var polygon in polygonA.AllPolygons)
            {
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = id++;
            }
            // temporarily number the vertices so that each has a unique number. this is important for the Intersection Lookup List
            foreach (var polygon in polygonB.AllPolygons)
            {
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = id++;
            }
            var intersectionLookup = MakeIntersectionLookupList(id, intersections);
            var positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store positive polygons in increasing area
            var negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersectionLookup, intersections, crossProductSign, out var startIndex,
                out var startEdge))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startIndex,
                    startEdge, switchDirection).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates, false));
                else positivePolygons.Add(area, new Polygon(polyCoordinates, false));
            }
            // reset ids for polygon B
            id = 0;
            foreach (var vertex in polygonB.Vertices)
                vertex.IndexInList = id++;

            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values);
        }
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Intersect(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            return polygonRelationship switch
            {
                PolygonRelationship.Separated => new List<Polygon>(),
                PolygonRelationship.BIsCompletelyInsideA => new List<Polygon> { polygonB.Copy() },
                PolygonRelationship.AIsCompletelyInsideB => new List<Polygon> { polygonA.Copy() },
                //case PolygonRelationship.Intersect:
                //case PolygonRelationship.SeparatedButBordersTouch:
                //case PolygonRelationship.BVerticesInsideAButLinesIntersect:
                //case PolygonRelationship.BInsideAButBordersTouch:
                //case PolygonRelationship.AVerticesInsideBButLinesIntersect:
                //case PolygonRelationship.AInsideBButBordersTouch:
                _ => BooleanOperation(polygonA, polygonB, intersections, false, +1, minAllowableArea)
            };
        }

        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Subtract(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }

        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
            PolygonRelationship polygonRelationship, List<IntersectionData> intersections,
            double minAllowableArea = Constants.BaseTolerance)
        {
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButBordersTouch:
                    return new List<Polygon> { polygonA.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy = polygonA.Copy();
                    if (polygonB.IsPositive)
                    {
                        var polygonBCopy = polygonB.Copy();
                        polygonBCopy.Reverse();
                        polygonACopy.AddHole(polygonBCopy);
                    }
                    return new List<Polygon> { polygonACopy };
                case PolygonRelationship.AIsCompletelyInsideB:
                    return new List<Polygon>();
                default:
                    return BooleanOperation(polygonA, polygonB, intersections, true, -1, minAllowableArea);
            }
        }

        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return ExclusiveOr(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }

        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship,
            List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButBordersTouch:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy1 = polygonA.Copy();
                    if (polygonB.IsPositive)
                    {
                        var polygonBCopy1 = polygonB.Copy();
                        polygonBCopy1.Reverse();
                        polygonACopy1.AddHole(polygonBCopy1);
                    }
                    return new List<Polygon> { polygonACopy1 };
                case PolygonRelationship.AIsCompletelyInsideB:
                    var polygonBCopy2 = polygonB.Copy();
                    if (polygonA.IsPositive)
                    {
                        var polygonACopy2 = polygonA.Copy();
                        polygonACopy2.Reverse();
                        polygonBCopy2.AddHole(polygonACopy2);
                    }
                    return new List<Polygon> { polygonBCopy2 };
                default:
                    var firstSubtraction = BooleanOperation(polygonA, polygonB, intersections,
                        true, -1, minAllowableArea);
                    var secondSubtraction = BooleanOperation(polygonB, polygonA, intersections,
                        true, -1, minAllowableArea);
                    firstSubtraction.AddRange(secondSubtraction);
                    return firstSubtraction;
            }
        }
        #endregion

        #region Clipper Boolean Functions
        #region union
        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, bool simplifyPriorToUnion = false,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, (IEnumerable<List<Vector2>>)subject, null, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToUnion = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, clip, simplifyPriorToUnion);
        }


        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, new[] { subject }, new[] { clip }, simplifyPriorToUnion);
        }

        /// <summary>
        /// Union. Joins paths that are touching into merged larger subject.
        /// Use CreatePolygons to correctly order the polygons inside one another.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToUnion"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Union(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToUnion = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctUnion, subject, new[] { clip }, simplifyPriorToUnion);
        }

        #endregion

        #region Difference
        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctDifference, subject, clip, simplifyPriorToDifference);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<Vector2> clip, bool simplifyPriorToDifference = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(subject, new[] { clip }, simplifyPriorToDifference, polyFill);
        }

        /// <summary>
        /// Difference. Gets the difference between two sets of polygons. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToDifference"></param>
        /// <param name="polyFill"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Difference(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToDifference = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Difference(new[] { subject }, clip, simplifyPriorToDifference, polyFill);
        }
        #endregion

        #region Intersection
        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip, bool simplifyPriorToIntersection = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(subjects, new[] { clip }, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToIntersection = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Intersection(new[] { subject }, clips, simplifyPriorToIntersection, polyFill);
        }

        /// <summary>
        /// Intersection. Gets the areas covered by both the subjects and the clips.
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToIntersection"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Intersection(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToIntersection = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctIntersection, subject, clip, simplifyPriorToIntersection);
        }
        #endregion

        #region Xor

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subject, IEnumerable<IEnumerable<Vector2>> clip,
            bool simplifyPriorToXor = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return BooleanOperation(polyFill, ClipType.ctXor, subject, clip, simplifyPriorToXor);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<Vector2> clip, bool simplifyPriorToXor = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips. 
        /// </summary>
        /// <param name="subjects"></param>
        /// <param name="clip"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<IEnumerable<Vector2>> subjects, IEnumerable<Vector2> clip,
            bool simplifyPriorToXor = false, PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(subjects, new[] { clip }, simplifyPriorToXor, polyFill);
        }

        /// <summary>
        /// XOR. Opposite of Intersection. Gets the areas covered by only either subjects or clips.  
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="clips"></param>
        /// <param name="simplifyPriorToXor"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<List<Vector2>> Xor(this IEnumerable<Vector2> subject, IEnumerable<IEnumerable<Vector2>> clips, bool simplifyPriorToXor = false,
            PolygonFillType polyFill = PolygonFillType.Positive)
        {
            return Xor(new[] { subject }, clips, simplifyPriorToXor, polyFill);
        }

        #endregion

        private static List<List<Vector2>> BooleanOperation(PolygonFillType fillMethod, ClipType clipType,
            IEnumerable<IEnumerable<Vector2>> subject,
           IEnumerable<IEnumerable<Vector2>> clip, bool simplifyPriorToBooleanOperation = false)
        {
            var fillType = fillMethod switch
            {
                PolygonFillType.Positive => PolyFillType.pftPositive,
                PolygonFillType.Negative => PolyFillType.pftNegative,
                PolygonFillType.NonZero => PolyFillType.pftNonZero,
                PolygonFillType.EvenOdd => PolyFillType.pftEvenOdd,
                _ => throw new NotImplementedException(),
            };

            if (simplifyPriorToBooleanOperation)
            {
                subject = subject.Select(path => Simplify(path));
                //If not null
                clip = clip?.Select(path => Simplify(path));
            }

            if (!subject.Any())
            {
                if (clip == null || !clip.Any())
                {
                    return new List<List<Vector2>>();
                }
                //Use the clip as the subject if this is a union operation and the clip is not null.
                if (clipType == ClipType.ctUnion)
                {
                    subject = clip;
                    clip = null;
                }
            }

            //Setup Clipper
            var clipper = new Clipper() { StrictlySimple = true };

            //Convert Points (TVGL) to IntPoints (Clipper)
            var subjectIntLoops = new List<List<IntPoint>>();
            foreach (var loop in subject)
            {
                var intLoop = loop.Select(point
                    => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                if (intLoop.Count > 2) subjectIntLoops.Add(intLoop);
            }
            clipper.AddPaths(subjectIntLoops, PolyType.ptSubject, true);

            if (clip != null)
            {
                var clipIntLoops = new List<List<IntPoint>>();
                foreach (var loop in clip)
                {
                    var intLoop = loop.Select(point
                        => new IntPoint(point.X * Constants.DoubleToIntPointMultipler, point.Y * Constants.DoubleToIntPointMultipler)).ToList();
                    if (intLoop.Count > 2) clipIntLoops.Add(intLoop);
                }
                clipper.AddPaths(clipIntLoops, PolyType.ptClip, true);
            }

            //Begin an evaluation
            var clipperSolution = new List<List<IntPoint>>();
            var result = clipper.Execute(clipType, clipperSolution, fillType, fillType);
            if (!result) throw new Exception("Clipper Union Failed");

            //Convert back to points
            var solution = clipperSolution.Select(clipperPath => clipperPath.Select(point
                => new Vector2(point.X * Constants.IntPointToDoubleMultipler, point.Y * Constants.IntPointToDoubleMultipler))
            .ToList()).ToList();
            return solution;
        }
        #endregion

    }
}