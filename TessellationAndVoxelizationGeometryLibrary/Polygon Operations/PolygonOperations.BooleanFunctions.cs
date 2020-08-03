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

        #region Union Public Methods
        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Union(polygonA, polygonB, relationship, intersections);
        }
        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonRelationship">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship, List<IntersectionData> intersections)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", "polygonA");
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", "polygonB");
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButVerticesTouch:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.AIsInsideHoleOfBButVerticesTouch:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.BIsInsideHoleOfABButVerticesTouch:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                case PolygonRelationship.BIsInsideAButEdgesTouch:
                case PolygonRelationship.BIsInsideAButVerticesTouch:
                    return new List<Polygon> { polygonA.Copy() };
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                    return new List<Polygon> { polygonB.Copy() };
                default:
                    //case PolygonRelationship.SeparatedButEdgesTouch:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.AIsInsideHoleOfBButEdgesTouch:
                    //case PolygonRelationship.BIsInsideHoleOfABButEdgesTouch:
                    return BooleanOperation(polygonA, polygonB, intersections, false, true, false);
            }
        }
        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this IEnumerable<Polygon> polygons)
        {
            var polygonList = polygons.ToList();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var polygonRelationship = GetPolygonRelationshipAndIntersections(polygonList[i],
                        polygonList[j], out var intersections);
                    if (polygonRelationship == PolygonRelationship.BIsCompletelyInsideA
                        || polygonRelationship == PolygonRelationship.BIsInsideAButEdgesTouch
                        || polygonRelationship == PolygonRelationship.BIsInsideAButVerticesTouch)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (polygonRelationship == PolygonRelationship.AIsCompletelyInsideB
                        || polygonRelationship == PolygonRelationship.AIsInsideBButEdgesTouch
                        || polygonRelationship == PolygonRelationship.AIsInsideBButVerticesTouch)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (polygonRelationship == PolygonRelationship.SeparatedButEdgesTouch
                      || polygonRelationship == PolygonRelationship.Intersection
                      || polygonRelationship == PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                      || polygonRelationship == PolygonRelationship.BIsInsideHoleOfABButEdgesTouch)
                    {
                        //if (i == 7 && j == 3)
                        //    Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], polygonRelationship, intersections);
                        //if (i == 7 && j == 3)
                        //    Presenter.ShowAndHang(newPolygons);
                        polygonList.RemoveAt(i);
                        polygonList.RemoveAt(j);
                        polygonList.AddRange(newPolygons);
                        i = polygonList.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            return polygonList;
        }
        #endregion

        #region Intersect Public Methods
        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B. 
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Intersect(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B. By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonRelationship">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship, List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButVerticesTouch:
                case PolygonRelationship.SeparatedButEdgesTouch:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.AIsInsideHoleOfBButVerticesTouch:
                case PolygonRelationship.AIsInsideHoleOfBButEdgesTouch:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.BIsInsideHoleOfABButVerticesTouch:
                case PolygonRelationship.BIsInsideHoleOfABButEdgesTouch:
                    return new List<Polygon>();
                case PolygonRelationship.BIsCompletelyInsideA:
                case PolygonRelationship.BIsInsideAButVerticesTouch:
                case PolygonRelationship.BIsInsideAButEdgesTouch:
                    if (polygonB.IsPositive) return new List<Polygon> { polygonB.Copy() };
                    else
                    {
                        var polygonACopy = polygonA.Copy();
                        polygonACopy.AddHole(polygonB.Copy());
                        return new List<Polygon> { polygonACopy };
                    }
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                    if (polygonA.IsPositive) return new List<Polygon> { polygonA.Copy() };
                    else
                    {
                        var polygonBCopy = polygonB.Copy();
                        polygonBCopy.AddHole(polygonA.Copy());
                        return new List<Polygon> { polygonBCopy };
                    }
                default:
                    //case PolygonRelationship.Intersect:
                    return BooleanOperation(polygonA, polygonB, intersections, false, false, false, minAllowableArea);
            }
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ALL of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this IEnumerable<Polygon> polygons)
        {
            var polygonList = polygons.ToList();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var polygonRelationship = GetPolygonRelationshipAndIntersections(polygonList[i],
                        polygonList[j], out var intersections);
                    if (polygonRelationship == PolygonRelationship.Separated
                        || polygonRelationship == PolygonRelationship.SeparatedButVerticesTouch
                        || polygonRelationship == PolygonRelationship.SeparatedButEdgesTouch
                        || polygonRelationship == PolygonRelationship.AIsInsideHoleOfB
                        || polygonRelationship == PolygonRelationship.AIsInsideHoleOfBButVerticesTouch
                        || polygonRelationship == PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                        || polygonRelationship == PolygonRelationship.BIsInsideHoleOfA
                        || polygonRelationship == PolygonRelationship.BIsInsideHoleOfABButVerticesTouch
                        || polygonRelationship == PolygonRelationship.BIsInsideHoleOfABButEdgesTouch)
                        return new List<Polygon>();
                    else if (polygonRelationship == PolygonRelationship.BIsCompletelyInsideA
                        || polygonRelationship == PolygonRelationship.BIsInsideAButVerticesTouch
                        || polygonRelationship == PolygonRelationship.BIsInsideAButEdgesTouch)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (polygonRelationship == PolygonRelationship.AIsCompletelyInsideB
                                         || polygonRelationship == PolygonRelationship.AIsInsideBButVerticesTouch
                                            || polygonRelationship == PolygonRelationship.AIsInsideBButEdgesTouch)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else
                    {
                        var newPolygons = Intersect(polygonList[i], polygonList[j], polygonRelationship, intersections);
                        polygonList.RemoveAt(i);
                        polygonList.RemoveAt(j);
                        polygonList.AddRange(newPolygons);
                        i = polygonList.Count; // to restart the outer loop
                        break; // to stop the inner loop
                    }
                }
            }
            return polygonList;
        }

        #endregion

        #region Subtract Public Methods
        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return Subtract(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A). By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonRelationship">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        ///                 +" to accopmlish this function, like Intersect. - polygonA</exception>
        /// <exception cref="ArgumentException">The subtrahend is already negative polygon (i.e. hole).Consider another operation"
        ///                 + " to accopmlish this function, like Intersect. - polygonB</exception>
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
                    PolygonRelationship polygonRelationship, List<IntersectionData> intersections,
                    double minAllowableArea = Constants.BaseTolerance)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("The minuend is already a negative polygon (i.e. hole). Consider another operation"
                + " to accopmlish this function, like Intersect.", "polygonA");
            if (!polygonB.IsPositive) throw new ArgumentException("The subtrahend is already a negative polygon (i.e. hole). Consider another operation"
                + " to accopmlish this function, like Intersect.", "polygonB");
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButVerticesTouch:
                case PolygonRelationship.SeparatedButEdgesTouch:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.BIsInsideHoleOfABButVerticesTouch:
                case PolygonRelationship.BIsInsideHoleOfABButEdgesTouch:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.AIsInsideHoleOfBButVerticesTouch:
                case PolygonRelationship.AIsInsideHoleOfBButEdgesTouch:
                    return new List<Polygon> { polygonA.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy = polygonA.Copy();
                    var polygonBCopy = polygonB.Copy();
                    polygonBCopy.Reverse();
                    polygonACopy.AddHole(polygonBCopy);
                    return new List<Polygon> { polygonACopy };
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                    return new List<Polygon>();
                default:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.BInsideAButVerticesTouch:
                    //case PolygonRelationship.BInsideAButEdgesTouch:
                    return BooleanOperation(polygonA, polygonB, intersections, true, false, false, minAllowableArea);
            }
        }

        #endregion

        #region Exclusive-OR Public Methods
        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, double minAllowableArea = Constants.BaseTolerance)
        {
            var relationship = GetPolygonRelationshipAndIntersections(polygonA, polygonB, out var intersections);
            return ExclusiveOr(polygonA, polygonB, relationship, intersections, minAllowableArea);
        }

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both. By providing the intersections between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonRelationship">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonRelationship polygonRelationship,
                    List<IntersectionData> intersections, double minAllowableArea = Constants.BaseTolerance)
        {
            switch (polygonRelationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButVerticesTouch:
                case PolygonRelationship.SeparatedButEdgesTouch:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.BIsInsideHoleOfABButVerticesTouch:
                case PolygonRelationship.BIsInsideHoleOfABButEdgesTouch:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.AIsInsideHoleOfBButVerticesTouch:
                case PolygonRelationship.AIsInsideHoleOfBButEdgesTouch:
                    return new List<Polygon> { polygonA.Copy(), polygonB.Copy() };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy1 = polygonA.Copy();
                    var polygonBCopy1 = polygonB.Copy();
                    polygonBCopy1.Reverse();
                    polygonACopy1.AddHole(polygonBCopy1);
                    return new List<Polygon> { polygonACopy1 };
                case PolygonRelationship.AIsCompletelyInsideB:
                    var polygonBCopy2 = polygonB.Copy();
                    var polygonACopy2 = polygonA.Copy();
                    polygonACopy2.Reverse();
                    polygonBCopy2.AddHole(polygonACopy2);
                    return new List<Polygon> { polygonBCopy2 };
                //case PolygonRelationship.Intersect:
                //case PolygonRelationship.AIsInsideBButVerticesTouch:
                //case PolygonRelationship.AIsInsideBButEdgesTouch:
                //case PolygonRelationship.BIsInsideAButVerticesTouch:
                //case PolygonRelationship.BIsInsideAButEdgesTouch:
                default:
                    return BooleanOperation(polygonA, polygonB, intersections, true, false, true, minAllowableArea);
            }
        }
        #endregion

        #region RemoveSelfIntersections Public Method
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, bool noHoles, out List<Polygon> strayHoles,
            double minAllowableArea = Constants.BaseTolerance)
        {
            polygon = polygon.Simplify(Constants.BaseTolerance);
            var intersections = polygon.GetSelfIntersections();
            var isSubtract = false;
            var isUnion = true;
            var bothApproachDirections = true;
            var intersectionLookup = MakeIntersectionLookupList(intersections, polygon, null, out var positivePolygons,
                out var negativePolygons, isSubtract, isUnion, bothApproachDirections); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersections, isSubtract, isUnion, bothApproachDirections, out var startingIntersection,
                out var startEdge))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, isSubtract, isUnion).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0 && noHoles)
                {
                    polyCoordinates.Reverse();
                    positivePolygons.Add(area, new Polygon(polyCoordinates));
                }
                else if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates));
                else positivePolygons.Add(area, new Polygon(polyCoordinates));
            }
            //var startIndex = 0;
            //foreach (var poly in polygon.AllPolygons)
            //{
            //    if (startIndex >= newPolygonStartIndices.Count) break;
            //    if (poly.Vertices[0].IndexInList == newPolygonStartIndices[startIndex])
            //    {
            //        if (poly.IsPositive)
            //            positivePolygons.Add(poly.Area, poly.Copy());
            //        else negativePolygons.Add(poly.Area, poly.Copy());
            //        startIndex += 2;
            //    }
            //}
            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values,
                out strayHoles, false, true);
        }
        #endregion

        #region Private Functions used by the above public methods
        /// <summary>
        /// All of the previous boolean operations are accomplished by this function. Note that the function RemoveSelfIntersections is also
        /// very simliar to this function.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="isSubtract">The switch direction.</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        private static List<Polygon> BooleanOperation(this Polygon polygonA, Polygon polygonB, List<IntersectionData> intersections, bool isSubtract,
                    bool isUnion, bool bothApproachDirections, double minAllowableArea = Constants.BaseTolerance)
        {
            var intersectionLookup = MakeIntersectionLookupList(intersections, polygonA, polygonB, out var positivePolygons,
                out var negativePolygons, isSubtract, isUnion, bothApproachDirections); //store negative in increasing (from -inf to 0) area
            while (GetNextStartingIntersection(intersections, isSubtract, isUnion, bothApproachDirections, out var startingIntersection,
                out var startEdge))
            {
                var polyCoordinates = MakePolygonThroughIntersections(intersectionLookup, intersections, startingIntersection,
                    startEdge, isSubtract, isUnion).ToList();
                var area = polyCoordinates.Area();
                if (area.IsNegligible(minAllowableArea)) continue;
                if (area < 0) negativePolygons.Add(area, new Polygon(polyCoordinates));
                else positivePolygons.Add(area, new Polygon(polyCoordinates));
            }
            // for holes that were not participating in any intersection, we need to restore them to the result
            return CreateShallowPolygonTreesPostBooleanOperation(positivePolygons.Values.ToList(), negativePolygons.Values,
                out _, isSubtract, isUnion);
        }

        /// <summary>
        /// Gets the next intersection by looking through the intersectionLookupList. It'll return false, when there are none left.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="crossProductSign">The cross product sign.</param>
        /// <param name="nextStartingIntersection">The next starting intersection.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <returns><c>true</c> if a new starting intersection was found, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool GetNextStartingIntersection(List<IntersectionData> intersections, bool isSubtract, bool isUnion, bool bothApproachDirections,
            out IntersectionData nextStartingIntersection, out PolygonSegment currentEdge)
        {
            foreach (var intersectionData in intersections)
            {
                #region first some conditions that tell us to skip this intersection
                if (intersectionData.Visited) continue;
                if ((intersectionData.Relationship & PolygonSegmentRelationship.CoincidentLines) != 0b0)
                {  // this addresses the special cases where lines are coincident
                    if ((intersectionData.Relationship & (PolygonSegmentRelationship.SameLineBeforePoint | PolygonSegmentRelationship.SameLineAfterPoint))
                        == (PolygonSegmentRelationship.SameLineBeforePoint | PolygonSegmentRelationship.SameLineAfterPoint))
                        continue;
                    if (!isSubtract && (intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) == 0b0)
                        // if it is in the same direction (OppositeDirections bit is 0), then that won't work unless it's subtract 
                        continue;
                    if (isSubtract && (((intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0)
                        || ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0)))
                        // if it's subtract then they should be in the same direction, but we're only going to set current as the minuend
                        // so we can also remove cases where the intersection is the same point before the point (that's a bit of a confusingness)
                        continue;
                }

                #endregion

                // now look at 00 ("glance"), 11 ("overlapping"), 10 ("A encompasses B"), and 01 ("B encompasses A")
                #region "Glance" A and B touch but are only abutting one another. no overlap in regions
                if (isUnion && (intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0 &&
                    (intersectionData.Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0 &&
                    (intersectionData.Relationship & PolygonSegmentRelationship.CoincidentLines) != 0b0)
                { //the only time non-overlapping intersections are intereseting is when we are doing union and lines are coincident
                  // otherwise you simply stay on the same polygon you enter with
                    if (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                         (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0)
                        ||
                        ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                          (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0))
                    {
                        currentEdge = intersectionData.EdgeB;
                        nextStartingIntersection = intersectionData;
                        return true;
                    }
                    else if (((intersectionData.Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0 &&
                              (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)
                             ||
                             ((intersectionData.Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0 &&
                              (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0))
                    {
                        currentEdge = intersectionData.EdgeA;
                        nextStartingIntersection = intersectionData;
                        return true;
                    }
                }
                #endregion
                #region Overlapping. The conventional case where A and B cross into one another
                else if ((intersectionData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.Overlapping)
                {
                    var cross = intersectionData.EdgeA.Vector.Cross(intersectionData.EdgeB.Vector);
                    var switchSign = (isSubtract || isUnion) ? 1 : -1;
                    if (switchSign * cross < 0 && (!isSubtract || bothApproachDirections ||
                        intersectionData.EdgeA.IndexInList < intersectionData.EdgeB.IndexInList))
                    {
                        currentEdge = intersectionData.EdgeA;
                        nextStartingIntersection = intersectionData;
                        return true;
                    }
                    if (switchSign * cross > 0 && (!isSubtract || bothApproachDirections ||
                        intersectionData.EdgeB.IndexInList < intersectionData.EdgeA.IndexInList))
                    {
                        currentEdge = intersectionData.EdgeB;
                        nextStartingIntersection = intersectionData;
                        return true;
                    }
                }
                #endregion
                #region Polygon A encompasses all of polygon B at this intersection 
                else if (isSubtract && (bothApproachDirections || intersectionData.EdgeA.IndexInList < intersectionData.EdgeB.IndexInList) &&
                    (intersectionData.Relationship & PolygonSegmentRelationship.AEncompassesB) != 0b0)
                {
                    currentEdge = intersectionData.EdgeA;
                    nextStartingIntersection = intersectionData;
                    return true;

                }
                #endregion
                #region Polygon B encompasses all of polygon A at this intersection

                else if (isSubtract && (bothApproachDirections || intersectionData.EdgeB.IndexInList < intersectionData.EdgeA.IndexInList) &&
                    (intersectionData.Relationship & PolygonSegmentRelationship.BEncompassesA) != 0b0)
                {
                    currentEdge = intersectionData.EdgeB;
                    nextStartingIntersection = intersectionData;
                    return true;
                }
                #endregion
            }
            nextStartingIntersection = null;
            currentEdge = null;
            return false;
        }

        /// <summary>
        /// Makes the polygon through intersections. This is actually the heart of the matter here. The method is the main
        /// while loop that switches between segments everytime a new intersection is encountered. It is universal to all
        /// the boolean operations
        /// </summary>
        /// <param name="intersectionLookup">The readonly intersection lookup.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="intersectionData">The intersection data.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="isSubtract">if set to <c>true</c> [switch directions].</param>
        /// <returns>Polygon.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static List<Vector2> MakePolygonThroughIntersections(List<int>[] intersectionLookup,
            List<IntersectionData> intersections, IntersectionData startingIntersection, PolygonSegment startingEdge, bool isSubtract, bool isUnion)
        {
            var newPath = new List<Vector2>();
            var intersectionData = startingIntersection;
            var currentEdge = startingEdge;
            var forward = true; // as in following the edges in the forward direction (from...to). If false, then traverse backwards
            var currentEdgeIsFromPolygonA = currentEdge == intersectionData.EdgeA;
            do
            {
                intersectionData.Visited = true;
                var intersectionCoordinates = intersectionData.IntersectCoordinates;
                // only add the point to the path if it wasn't added below in the while loop. i.e. it is an intermediate point to the 
                // current polygon edge
                if (!forward || (currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) == 0b0)
                 || (!currentEdgeIsFromPolygonA && (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) == 0b0))
                    newPath.Add(intersectionCoordinates);
                currentEdgeIsFromPolygonA = !currentEdgeIsFromPolygonA;
                currentEdge = currentEdgeIsFromPolygonA ? intersectionData.EdgeA : intersectionData.EdgeB;
                if (isSubtract) forward = !forward;
                if (!forward && ((currentEdgeIsFromPolygonA &&
                                  (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfA) != 0b0) ||
                                 (!currentEdgeIsFromPolygonA &&
                                  (intersectionData.Relationship & PolygonSegmentRelationship.AtStartOfB) != 0b0)))
                    currentEdge = currentEdge.FromPoint.EndLine;

                // the following while loop add all the points along the subpath until the next intersection is encountered
                while (!ClosestNextIntersectionOnThisEdge(intersectionLookup, currentEdge, intersections,
                        intersectionCoordinates, forward, isSubtract, isUnion, out intersectionData, ref currentEdgeIsFromPolygonA))
                // when this returns true (a valid intersection is found - even if previously visited), then we break
                // out of the loop. The intersection is identified here, but processed above
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
                    intersectionCoordinates = Vector2.Null; // this is set to null because its value is used in ClosestNextIntersectionOnThisEdge
                                                            // when multiple intersections cross the edge. If we got through the first pass then there are no previous intersections on 
                                                            // the edge that concern us. We want that function to report the first one for the edge
                }
            } while (currentEdge != startingEdge && intersectionData != startingIntersection);
            return newPath;
        }

        /// <summary>
        /// This is invoked by the previous function, . It is possible that there are multiple intersections crossing the currentEdge. Based on the
        /// direction (forward?), the next closest one is identified.
        /// </summary>
        /// <param name="intersectionLookup">The intersection lookup.</param>
        /// <param name="currentEdge">The current edge.</param>
        /// <param name="allIntersections">All intersections.</param>
        /// <param name="formerIntersectCoords">The former intersect coords.</param>
        /// <param name="forward">if set to <c>true</c> [forward].</param>
        /// <param name="newIntersection">The index of intersection.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <exception cref="NotImplementedException"></exception>
        private static bool ClosestNextIntersectionOnThisEdge(List<int>[] intersectionLookup, PolygonSegment currentEdge, List<IntersectionData> allIntersections,
        Vector2 formerIntersectCoords, bool forward, bool isSubtract, bool isUnion, out IntersectionData newIntersection, ref bool currentEdgeIsFromPolygonA)
        {
            var intersectionIndices = intersectionLookup[currentEdge.IndexInList];
            newIntersection = null;
            if (intersectionIndices == null)
                return false;
            var minDistanceToIntersection = double.PositiveInfinity;
            var vector = forward ? currentEdge.Vector : -currentEdge.Vector;
            var datum = !formerIntersectCoords.IsNull() ? formerIntersectCoords :
                forward ? currentEdge.FromPoint.Coordinates : currentEdge.ToPoint.Coordinates;
            foreach (var index in intersectionIndices)
            {
                var thisIntersectData = allIntersections[index];
                if (formerIntersectCoords.Equals(thisIntersectData.IntersectCoordinates)) continue;
                // if the intersection is a point that both share but the lines are the same (and in same direction)
                if ((thisIntersectData.Relationship == (PolygonSegmentRelationship.BothLinesStartAtPoint | PolygonSegmentRelationship.CoincidentLines |
                    PolygonSegmentRelationship.SameLineAfterPoint | PolygonSegmentRelationship.SameLineBeforePoint))
                // if the two polygons just "glance" off of one another at this intersection, then don't consider this as a valid place to switch
                || (!isUnion && (thisIntersectData.Relationship & PolygonSegmentRelationship.Overlapping) == 0b0)
                // if union and current edge is on the outer polygon, then don't consider this as a valid place to switch
                || (isUnion && ((currentEdge == thisIntersectData.EdgeA && (thisIntersectData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.AEncompassesB)
                    || (currentEdge == thisIntersectData.EdgeB && (thisIntersectData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.BEncompassesA)))
                // if intersect and current edge is on the inner polygon, then don't consider this as a valid place to switch
                || (!isSubtract && !isUnion && ((currentEdge == thisIntersectData.EdgeA && (thisIntersectData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.BEncompassesA)
                    || (currentEdge == thisIntersectData.EdgeB && (thisIntersectData.Relationship & PolygonSegmentRelationship.Overlapping) == PolygonSegmentRelationship.AEncompassesB))))
                {
                    // well, even though the intersection is a valid place to switch, we need to mark that it is visited so that we don't start here next time
                    // GetNextStartingIntersection gets called
                    thisIntersectData.Visited = true;
                    continue;
                }

                var distance = vector.Dot(thisIntersectData.IntersectCoordinates - datum);
                if (distance < 0) continue;
                if (minDistanceToIntersection > distance)
                {
                    minDistanceToIntersection = distance;
                    newIntersection = thisIntersectData;
                }
            }
            currentEdgeIsFromPolygonA = newIntersection?.EdgeA == currentEdge;
            return newIntersection != null;
        }

        /// <summary>
        /// Makes the intersection lookup table that allows us to quickly find the intersections for a given edge.
        /// </summary>
        /// <param name="numLines">The number lines.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>System.Collections.Generic.List&lt;System.Int32&gt;[].</returns>
        private static List<int>[] MakeIntersectionLookupList(List<IntersectionData> intersections, Polygon polygonA,
            Polygon polygonB, out SortedDictionary<double, Polygon> positivePolygons, out SortedDictionary<double, Polygon> negativePolygons,
            bool isSubtract, bool isUnion, bool doubleApproach)
        {
            // first off, number all the vertices with a unique index between 0 and n. These are used in the lookupList to connect the 
            // edges to the intersections that they participate in.
            var index = 0;
            var polygonStartIndices = new List<int>();
            // in addition, keep track of the vertex index that is the beginning of each polygon. Recall that there could be numerous
            // hole-polygons that need to be accounted for.
            var allPolygons = polygonA.AllPolygons.ToList();
            foreach (var polygon in allPolygons)
            {
                polygonStartIndices.Add(index);
                foreach (var vertex in polygon.Vertices)
                    vertex.IndexInList = index++;
            }
            var startOfBVertices = index; //yeah, also keep track of when the second polygon tree argument starts
            if (polygonB != null)
                foreach (var polygon in polygonB.AllPolygons)
                {
                    allPolygons.Add(polygon);
                    polygonStartIndices.Add(index);
                    foreach (var vertex in polygon.Vertices)
                        vertex.IndexInList = index++;
                }
            polygonStartIndices.Add(index); // add a final exclusive top of the range for the for-loop below (not the next one, the one after)

            // now make the lookupList. One list per vertex. If the vertex does not intersect, then it is left as null.
            // this is potentially memory intensive but speeds up the matching in when creating new polygons
            var lookupList = new List<int>[index];
            for (int i = 0; i < intersections.Count; i++)
            {
                var intersection = intersections[i];
                intersection.Visited = false;
                index = intersection.EdgeA.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
                index = intersection.EdgeB.IndexInList;
                lookupList[index] ??= new List<int>();
                lookupList[index].Add(i);
            }

            positivePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store positive polygons in increasing area
            negativePolygons = new SortedDictionary<double, Polygon>(new NoEqualSort()); //store negative in increasing (from -inf to 0) area
            // now we want to find the sub-polygons that are not intersecting anything and decide whether to keep them or not
            index = 0;
            foreach (var poly in allPolygons)
            {
                var isNonIntersecting = true;
                var isIdentical = true;
                var identicalPolygonIsInverted = false;
                for (int j = polygonStartIndices[index]; j < polygonStartIndices[index + 1]; j++)
                {
                    if (lookupList[j] == null)
                        isIdentical = false;
                    else
                    {
                        if (!isIdentical)
                        {
                            isNonIntersecting = false;
                            break;
                        }
                        var intersectionIndex = lookupList[j].FindIndex(k => (intersections[k].Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0
                        && (intersections[k].Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0);
                        if (intersectionIndex == -1)
                            isIdentical = false;
                        else if ((intersections[lookupList[j][intersectionIndex]].Relationship & PolygonSegmentRelationship.OppositeDirections) != 0b0)
                            identicalPolygonIsInverted = true;
                    }
                }
                if (isIdentical)
                {   // go back through the same indices and remove references to the intersections. Also, set the intersections to "visited"
                    // which is easier than deleting since the other references would collapse down
                    for (int j = polygonStartIndices[index]; j < polygonStartIndices[index + 1]; j++)
                    {
                        var intersectionIndex = lookupList[j].First(k => (intersections[k].Relationship & PolygonSegmentRelationship.SameLineAfterPoint) != 0b0
                     && (intersections[k].Relationship & PolygonSegmentRelationship.SameLineBeforePoint) != 0b0);
                        lookupList[j].Remove(intersectionIndex);
                        if (lookupList[j].Count == 0) lookupList[j] = null;
                        intersections[intersectionIndex].Visited = true;
                        // note, in the next line - this has to be EdgeB since searching in order the A polygon will detect the duplicate - B will skip over
                        var otherLookupEntry = lookupList[intersections[intersectionIndex].EdgeB.IndexInList];
                        otherLookupEntry.Remove(intersectionIndex);
                        //if (otherLookupEntry.Count == 0) lookupList[intersections[intersectionIndex].EdgeB.IndexInList] = null;
                    }
                    if ((!identicalPolygonIsInverted && !isSubtract) ||
                        (identicalPolygonIsInverted && isSubtract && !isUnion && !doubleApproach))
                        if (poly.IsPositive)
                            positivePolygons.Add(poly.Area, poly.Copy());  //add the positive as a positive
                        else negativePolygons.Add(poly.Area, poly.Copy()); //add the negative as a negative
                }

                else if (isNonIntersecting)
                {
                    var partOfPolygonB = polygonStartIndices[index] >= startOfBVertices;
                    var otherPolygon = partOfPolygonB ? polygonA : polygonB != null ? polygonB : null;
                    var insideOther = otherPolygon?.IsNonIntersectingPolygonInside(poly, out _) == true;
                    if (poly.IsPositive)
                    {
                        if (isUnion != insideOther || (isSubtract && (!partOfPolygonB || doubleApproach)))
                            positivePolygons.Add(poly.Area, poly.Copy());  //add the positive as a positive
                        else if (insideOther && isSubtract && (partOfPolygonB || doubleApproach))
                            negativePolygons.Add(-poly.Area, poly.Copy(true)); // add the positive as a negative
                    }
                    else if (!insideOther && // then it's a hole, but it is not inside the other
                    (isUnion || (isSubtract && (!partOfPolygonB || doubleApproach))))
                        negativePolygons.Add(poly.Area, poly.Copy()); //add the negative as a negative
                    else // it's a hole in the other polygon 
                    {
                        //first need to check if it is inside a hole of the other
                        var holeIsInsideHole = otherPolygon.Holes.Any(h => h.IsNonIntersectingPolygonInside(poly, out _) == true);
                        if (holeIsInsideHole && (isUnion || (isSubtract && (!partOfPolygonB || doubleApproach))))
                            negativePolygons.Add(poly.Area, poly.Copy()); //add the negatie as a negative
                        else if (!holeIsInsideHole)
                        {
                            if (!isUnion && !isSubtract)
                                negativePolygons.Add(poly.Area, poly.Copy()); //add the negatie as a negative
                            else if (isSubtract && (!partOfPolygonB || doubleApproach))
                                positivePolygons.Add(-poly.Area, poly.Copy(true)); //add the negative as a positive
                        }
                    }
                }
                index++;
            }
            return lookupList;
        }
        #endregion
    }
}