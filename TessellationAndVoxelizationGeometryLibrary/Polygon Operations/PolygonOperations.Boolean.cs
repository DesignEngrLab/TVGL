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
        static PolygonUnion polygonUnion;
        static PolygonIntersection polygonIntersection;
        static PolygonSubtraction polygonSubtraction;
        static PolygonRemoveIntersections polygonRemoveIntersections;

        #region Union Public Methods
        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, double minAllowableArea = double.NaN)
        {
            Presenter.ShowAndHang(new[] { polygonA, polygonA.InnerPolygons.First(), polygonB, polygonB.InnerPolygons.First() });
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return Union(polygonA, polygonB, relationship, minAllowableArea);
        }
        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonInteraction">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord polygonInteraction,
             double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", "polygonA");
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", "polygonB");
            switch (polygonInteraction.Relationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.SeparatedButVerticesTouch:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.AIsInsideHoleOfBButVerticesTouch:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.BIsInsideHoleOfABButVerticesTouch:
                    return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
                case PolygonRelationship.BIsCompletelyInsideA:
                case PolygonRelationship.BIsInsideAButEdgesTouch:
                case PolygonRelationship.BIsInsideAButVerticesTouch:
                case PolygonRelationship.Equal:
                    return new List<Polygon> { polygonA.Copy(true, false) };
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                    return new List<Polygon> { polygonB.Copy(true, false) };
                default:
                    //case PolygonRelationship.SeparatedButEdgesTouch:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.AIsInsideHoleOfBButEdgesTouch:
                    //case PolygonRelationship.BIsInsideHoleOfABButEdgesTouch:
                    if (polygonUnion == null) polygonUnion = new PolygonUnion();
                    return polygonUnion.Run(polygonA, polygonB, polygonInteraction, minAllowableArea);
            }
        }
        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this IEnumerable<Polygon> polygons, double minAllowableArea = double.NaN)
        {
            var polygonList = polygons.ToList();
            if (double.IsNaN(minAllowableArea))
                minAllowableArea = polygonList.Sum(p => p.Area) * Constants.BaseTolerance / polygonList.Count;
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i],
                        polygonList[j]);
                    if (interaction.Relationship == PolygonRelationship.BIsCompletelyInsideA
                        || interaction.Relationship == PolygonRelationship.BIsInsideAButEdgesTouch
                        || interaction.Relationship == PolygonRelationship.BIsInsideAButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (interaction.Relationship == PolygonRelationship.AIsCompletelyInsideB
                        || interaction.Relationship == PolygonRelationship.AIsInsideBButEdgesTouch
                        || interaction.Relationship == PolygonRelationship.AIsInsideBButVerticesTouch)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.Relationship == PolygonRelationship.SeparatedButEdgesTouch
                             || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                             || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfABButEdgesTouch
                             || (interaction.Relationship & PolygonRelationship.Intersection) == PolygonRelationship.Intersection)
                    {
                        //if (i == 22 && j == 18)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], interaction, minAllowableArea);
                        //Console.WriteLine("i = {0}, j = {1}", i, j);
                        //if (i == 239 && j == 238)
                        //Presenter.ShowAndHang(newPolygons);
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
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return Intersect(polygonA, polygonB, relationship, minAllowableArea);
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
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
             double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            switch (interaction.Relationship)
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
                    if (polygonB.IsPositive) return new List<Polygon> { polygonB.Copy(true, false) };
                    else
                    {
                        var polygonACopy = polygonA.Copy(true, false);
                        polygonACopy.AddInnerPolygon(polygonB.Copy(true, false));
                        return new List<Polygon> { polygonACopy };
                    }
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                    if (polygonA.IsPositive) return new List<Polygon> { polygonA.Copy(true, false) };
                    else
                    {
                        var polygonBCopy = polygonB.Copy(true, false);
                        polygonBCopy.AddInnerPolygon(polygonA.Copy(true, false));
                        return new List<Polygon> { polygonBCopy };
                    }
                default:
                    //case PolygonRelationship.Intersect:
                    if (polygonIntersection == null) polygonIntersection = new PolygonIntersection();
                    return polygonIntersection.Run(polygonA, polygonB, interaction, minAllowableArea);
            }
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ALL of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this IEnumerable<Polygon> polygons, double minAllowableArea = double.NaN)
        {
            var polygonList = polygons.ToList();
            if (double.IsNaN(minAllowableArea))
                minAllowableArea = polygonList.Sum(p => p.Area) * Constants.BaseTolerance / polygonList.Count;
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i],
                        polygonList[j]);
                    if (interaction.Relationship == PolygonRelationship.Separated
                        || interaction.Relationship == PolygonRelationship.SeparatedButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.SeparatedButEdgesTouch
                        || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfB
                        || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfBButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                        || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfA
                        || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfABButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfABButEdgesTouch)
                        return new List<Polygon>();
                    else if (interaction.Relationship == PolygonRelationship.BIsCompletelyInsideA
                        || interaction.Relationship == PolygonRelationship.BIsInsideAButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.BIsInsideAButEdgesTouch)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.Relationship == PolygonRelationship.AIsCompletelyInsideB
                        || interaction.Relationship == PolygonRelationship.AIsInsideBButVerticesTouch
                        || interaction.Relationship == PolygonRelationship.AIsInsideBButEdgesTouch)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else
                    {
                        var newPolygons = Intersect(polygonList[i], polygonList[j], interaction, minAllowableArea);
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
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            var polygonBInverted = polygonB.Copy(true, true);
            var relationship = GetPolygonInteraction(polygonA, polygonBInverted);
            return Subtract(polygonA, polygonBInverted, relationship, minAllowableArea, true);
        }

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A). By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The polygon relationship.</param>
        /// <param name="intersections">The intersections.</param>
        /// <param name="minAllowableArea">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        ///                 +" to accopmlish this function, like Intersect. - polygonA</exception>
        /// <exception cref="ArgumentException">The subtrahend is already negative polygon (i.e. hole).Consider another operation"
        ///                 + " to accopmlish this function, like Intersect. - polygonB</exception>
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
                    PolygonInteractionRecord interaction, double minAllowableArea = double.NaN)
        {
            return Subtract(polygonA, polygonB, interaction, minAllowableArea, false);
        }
        private static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
           PolygonInteractionRecord interaction, double minAllowableArea, bool interactionAlreadyInverted)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            if (!polygonA.IsPositive) throw new ArgumentException("The minuend is already a negative polygon (i.e. hole). Consider another operation"
                + " to accopmlish this function, like Intersect.", "polygonA");
            if (polygonB.IsPositive == interactionAlreadyInverted) throw new ArgumentException("The subtrahend is already a negative polygon (i.e. hole). Consider another operation"
                  + " to accopmlish this function, like Intersect.", "polygonB");
            if (!interactionAlreadyInverted)
                interaction = interaction.InvertPolygonInRecord(ref polygonB, minAllowableArea);
            switch (interaction.Relationship)
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
                    return new List<Polygon> { polygonA.Copy(true, false) };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy = polygonA.Copy(true, false);
                    polygonACopy.AddInnerPolygon(polygonB.Copy(true, true));
                    return new List<Polygon> { polygonACopy };
                case PolygonRelationship.AIsCompletelyInsideB:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                    return new List<Polygon>();
                default:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.BInsideAButVerticesTouch:
                    //case PolygonRelationship.BInsideAButEdgesTouch:
                    if (polygonSubtraction == null) polygonSubtraction = new PolygonSubtraction();
                    return polygonSubtraction.Run(polygonA, polygonB, interaction, minAllowableArea);
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
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            var relationship = GetPolygonInteraction(polygonA, polygonB);
            return ExclusiveOr(polygonA, polygonB, relationship, minAllowableArea);
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
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
            double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = 0.5 * (polygonA.Area + polygonB.Area) * Constants.BaseTolerance;
            switch (interactionRecord.Relationship)
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
                    return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
                case PolygonRelationship.BIsCompletelyInsideA:
                    var polygonACopy1 = polygonA.Copy(true, false);
                    polygonACopy1.AddInnerPolygon(polygonB.Copy(true, true));
                    return new List<Polygon> { polygonACopy1 };
                case PolygonRelationship.AIsCompletelyInsideB:
                    var polygonBCopy2 = polygonB.Copy(true, false);
                    polygonBCopy2.AddInnerPolygon(polygonA.Copy(true, true));
                    return new List<Polygon> { polygonBCopy2 };
                //case PolygonRelationship.Intersect:
                //case PolygonRelationship.AIsInsideBButVerticesTouch:
                //case PolygonRelationship.AIsInsideBButEdgesTouch:
                //case PolygonRelationship.BIsInsideAButVerticesTouch:
                //case PolygonRelationship.BIsInsideAButEdgesTouch:
                default:
                    if (polygonSubtraction == null) polygonSubtraction = new PolygonSubtraction();
                    var result = polygonA.Subtract(polygonB, interactionRecord, minAllowableArea, false);
                    result.AddRange(polygonB.Subtract(polygonA, interactionRecord, minAllowableArea, false));
                    return result;
            }
        }

        internal static List<int> NumberVertiesAndGetPolygonVertexDelimiter(this Polygon polygon, int startIndex = 0)
        {
            var polygonStartIndices = new List<int>();
            // in addition, keep track of the vertex index that is the beginning of each polygon. Recall that there could be numerous
            // hole-polygons that need to be accounted for.
            var index = startIndex;
            foreach (var poly in polygon.AllPolygons)
            {
                polygonStartIndices.Add(index);
                foreach (var vertex in poly.Vertices)
                    vertex.IndexInList = index++;
            }
            polygonStartIndices.Add(index); // add a final exclusive top of the range for the for-loop below (not the next one, the one after)
            return polygonStartIndices;
        }

        #endregion

        #region RemoveSelfIntersections Public Method
        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, bool noHoles, out List<Polygon> strayHoles, double minAllowableArea = double.NaN)
        {
            if (double.IsNaN(minAllowableArea)) minAllowableArea = Math.Abs(polygon.Area * Constants.BaseTolerance);
            polygon = polygon.Simplify(minAllowableArea);
            var intersections = polygon.GetSelfIntersections();
            if (intersections.Count == 0)
            {
                strayHoles = null;
                return new List<Polygon> { polygon };
            }
            if (polygonRemoveIntersections == null) polygonRemoveIntersections = new PolygonRemoveIntersections();
            // if (intersections.Any(n => (n.Relationship & PolygonRemoveIntersections.alignedIntersection) == PolygonRemoveIntersections.alignedIntersection)) 
            return polygonRemoveIntersections.Run(polygon, intersections, noHoles, minAllowableArea, out strayHoles);
        }
        #endregion

    }
}