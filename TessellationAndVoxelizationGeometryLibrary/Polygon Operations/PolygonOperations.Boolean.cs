using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    public static partial class PolygonOperations
    {
        // while the main executing methods are provided in this file (all of which can be invoked as Extensions), the code that perform the new polygon creation
        // is provided in the following four non-Static classes. These are non-static because they all inherit from the BooleanBase class. Each of these only needs
        // to be instantiated once as no data is stored in the class objects. So, this is a sort of singleton model but it's too bad we can have static classes inherit from
        // other static classes.
        private static PolygonUnion polygonUnion;

        private static PolygonIntersection polygonIntersection;
        private static PolygonSubtraction polygonSubtraction;
        private static PolygonRemoveIntersections polygonRemoveIntersections;

        #region Union Public Methods

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var relationship = GetPolygonInteraction(polygonA, polygonB, tolerance);
            return Union(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that exist in either A OR B.By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="polygonInteraction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonA</exception>
        /// <exception cref="ArgumentException">A negative polygon (i.e. hole) is provided to Union which results in infinite shape. - polygonB</exception>
        public static List<Polygon> Union(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord polygonInteraction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonA));
            if (!polygonB.IsPositive) throw new ArgumentException("A negative polygon (i.e. hole) is provided to Union which results in infinite shape.", nameof(polygonB));
            if (!polygonInteraction.CoincidentEdges && (polygonInteraction.Relationship == PolygonRelationship.Separated ||
                polygonInteraction.Relationship == PolygonRelationship.AIsInsideHoleOfB ||
                polygonInteraction.Relationship == PolygonRelationship.BIsInsideHoleOfA))
                return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.BInsideA ||
               polygonInteraction.Relationship == PolygonRelationship.Equal)
                return new List<Polygon> { polygonA.Copy(true, false) };
            if (polygonInteraction.Relationship == PolygonRelationship.AInsideB)
                return new List<Polygon> { polygonB.Copy(true, false) };
            polygonUnion ??= new PolygonUnion();
            return polygonUnion.Run(polygonA, polygonB, polygonInteraction, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ANY of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Union(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles,
            double tolerance = double.NaN)
        {
            var polygonList = polygons.ToList();
            if (double.IsNaN(tolerance))
            {
                var xMin = polygonList.Min(p => p.MinX);
                var xMax = polygonList.Max(p => p.MaxX);
                var yMin = polygonList.Min(p => p.MinY);
                var yMax = polygonList.Max(p => p.MaxY);
                tolerance = Math.Min(xMax - xMin, yMax - yMin) * Constants.BaseTolerance;
            }
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i], polygonList[j], tolerance);
                    if (interaction.Relationship == PolygonRelationship.BInsideA
                        || interaction.Relationship == PolygonRelationship.Equal)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.Relationship == PolygonRelationship.SeparatedButEdgesTouch
                             || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfBButEdgesTouch
                             || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfABButEdgesTouch
                             || (interaction.Relationship & PolygonRelationship.Intersection) == PolygonRelationship.Intersection
                             || (int)interaction.Relationship >= 64)
                    {
                        //if (i == 1 && j == 0)
                        //Presenter.ShowAndHang(new[] { polygonList[i], polygonList[j] });
                        var newPolygons = Union(polygonList[i], polygonList[j], interaction, outputAsCollectionType, tolerance);
                        //Console.WriteLine("i = {0}, j = {1}", i, j);
                        //if (i == 1 && j == 0)
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

        #endregion Union Public Methods

        #region Intersect Public Methods

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var relationship = GetPolygonInteraction(polygonA, polygonB, tolerance);
            return Intersect(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that result from the subshapes common to both A and B. By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The interaction.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The minimum allowable area.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            switch (interaction.Relationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.AIsInsideHoleOfB:
                case PolygonRelationship.BIsInsideHoleOfA:
                    return new List<Polygon>();

                case PolygonRelationship.BInsideA:
                    if (polygonB.IsPositive) return new List<Polygon> { polygonB.Copy(true, false) };
                    else
                    {
                        var polygonACopy = polygonA.Copy(true, false);
                        polygonACopy.AddInnerPolygon(polygonB.Copy(true, false));
                        return new List<Polygon> { polygonACopy };
                    }
                case PolygonRelationship.AInsideB:
                    if (polygonA.IsPositive) return new List<Polygon> { polygonA.Copy(true, false) };
                    else
                    {
                        var polygonBCopy = polygonB.Copy(true, false);
                        polygonBCopy.AddInnerPolygon(polygonA.Copy(true, false));
                        return new List<Polygon> { polygonBCopy };
                    }
                default:
                    //case PolygonRelationship.Intersect:
                    polygonIntersection ??= new PolygonIntersection();
                    return polygonIntersection.Run(polygonA, polygonB, interaction, outputAsCollectionType, tolerance);
            }
        }

        /// <summary>
        /// Returns the list of polygons that are the subshapes of ALL of the provided polygons.
        /// </summary>
        /// <param name="polygons">The polygons.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Intersect(this IEnumerable<Polygon> polygons, PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles,
            double tolerance = double.NaN)
        {
            var polygonList = polygons.ToList();
            for (int i = polygonList.Count - 1; i > 0; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var interaction = GetPolygonInteraction(polygonList[i], polygonList[j], tolerance);
                    if (interaction.Relationship == PolygonRelationship.Separated
                        || interaction.Relationship == PolygonRelationship.AIsInsideHoleOfB
                        || interaction.Relationship == PolygonRelationship.BIsInsideHoleOfA)
                        return new List<Polygon>();
                    else if (interaction.Relationship == PolygonRelationship.BInsideA)
                    {                            // remove polygon A
                        polygonList.RemoveAt(i);
                        break; // to stop the inner loop
                    }
                    else if (interaction.Relationship == PolygonRelationship.AInsideB)
                    {  // remove polygon B
                        polygonList.RemoveAt(j);
                        i--;
                    }
                    else
                    {
                        var newPolygons = Intersect(polygonList[i], polygonList[j], interaction, outputAsCollectionType, tolerance);
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

        #endregion Intersect Public Methods

        #region Subtract Public Methods

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A).
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var polygonBInverted = polygonB.Copy(true, true);
            var relationship = GetPolygonInteraction(polygonA, polygonBInverted, tolerance);
            return Subtract(polygonA, polygonBInverted, relationship, outputAsCollectionType, true, tolerance);
        }

        /// <summary>
        /// Subtracts the specified polygons in b from the polygons in a.
        /// </summary>
        /// <param name="polygonsA">The polygons in a.</param>
        /// <param name="polygonsB">The polygons b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        public static List<Polygon> Subtract(this IEnumerable<Polygon> polygonsA, IEnumerable<Polygon> polygonsB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var allPolygons = new List<Polygon>();
            allPolygons.AddRange(polygonsA);
            allPolygons.AddRange(polygonsB.Select(p => p.Copy(true, true)));
            return Intersect(allPolygons, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that result from A-B (subtracting polygon B from polygon A). By providing the intersections
        /// between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The polygon relationship.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        /// +" to accopmlish this function, like Intersect. - polygonA</exception>
        public static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interaction,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            return Subtract(polygonA, polygonB, interaction, outputAsCollectionType, false, tolerance);
        }

        /// <summary>
        /// Subtracts the specified polygon b.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interaction">The interaction.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="interactionAlreadyInverted">if set to <c>true</c> [interaction already inverted].</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>List&lt;Polygon&gt;.</returns>
        /// <exception cref="ArgumentException">The minuend is already a negative polygon (i.e. hole). Consider another operation"
        ///                 + " to accopmlish this function, like Intersect. - polygonA</exception>
        private static List<Polygon> Subtract(this Polygon polygonA, Polygon polygonB,
           PolygonInteractionRecord interaction, PolygonCollection outputAsCollectionType, bool interactionAlreadyInverted, double tolerance = double.NaN)
        {
            if (!polygonA.IsPositive) throw new ArgumentException("The minuend is already a negative polygon (i.e. hole). Consider another operation"
                + " to accopmlish this function, like Intersect.", nameof(polygonA));
            if (polygonB.IsPositive == interactionAlreadyInverted) throw new ArgumentException("The subtrahend is already a negative polygon (i.e. hole). Consider another operation"
                  + " to accopmlish this function, like Intersect.", nameof(polygonB));
            if (!interactionAlreadyInverted)
                interaction = interaction.InvertPolygonInRecord(ref polygonB);
            switch (interaction.Relationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.AIsInsideHoleOfB:
                    return new List<Polygon> { polygonA.Copy(true, false) };

                case PolygonRelationship.BInsideA:
                    var polygonACopy = polygonA.Copy(true, false);
                    polygonACopy.AddInnerPolygon(polygonB.Copy(true, true));
                    return new List<Polygon> { polygonACopy };

                case PolygonRelationship.AInsideB:
                    return new List<Polygon>();

                default:
                    //case PolygonRelationship.Intersect:
                    //case PolygonRelationship.BInsideAButVerticesTouch:
                    //case PolygonRelationship.BInsideAButEdgesTouch:
                    polygonSubtraction ??= new PolygonSubtraction();
                    return polygonSubtraction.Run(polygonA, polygonB, interaction, outputAsCollectionType, tolerance);
            }
        }

        #endregion Subtract Public Methods

        #region Exclusive-OR Public Methods

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB,
            PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            var relationship = GetPolygonInteraction(polygonA, polygonB, tolerance);
            return ExclusiveOr(polygonA, polygonB, relationship, outputAsCollectionType, tolerance);
        }

        /// <summary>
        /// Returns the list of polygons that are the Exclusive-OR of the two input polygons. Exclusive-OR are the regions where one polgyon
        /// resides but not both. By providing the intersections between the two polygons, the operation will be performed with less time and memory.
        /// </summary>
        /// <param name="polygonA">The polygon a.</param>
        /// <param name="polygonB">The polygon b.</param>
        /// <param name="interactionRecord">The interaction record.</param>
        /// <param name="outputAsCollectionType">Type of the output as collection.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns>System.Collections.Generic.List&lt;TVGL.TwoDimensional.Polygon&gt;.</returns>
        public static List<Polygon> ExclusiveOr(this Polygon polygonA, Polygon polygonB, PolygonInteractionRecord interactionRecord,
           PolygonCollection outputAsCollectionType = PolygonCollection.PolygonWithHoles, double tolerance = double.NaN)
        {
            switch (interactionRecord.Relationship)
            {
                case PolygonRelationship.Separated:
                case PolygonRelationship.BIsInsideHoleOfA:
                case PolygonRelationship.AIsInsideHoleOfB:
                    return new List<Polygon> { polygonA.Copy(true, false), polygonB.Copy(true, false) };

                case PolygonRelationship.BInsideA:
                    var polygonACopy1 = polygonA.Copy(true, false);
                    polygonACopy1.AddInnerPolygon(polygonB.Copy(true, true));
                    return new List<Polygon> { polygonACopy1 };

                case PolygonRelationship.AInsideB:
                    var polygonBCopy2 = polygonB.Copy(true, false);
                    polygonBCopy2.AddInnerPolygon(polygonA.Copy(true, true));
                    return new List<Polygon> { polygonBCopy2 };
                //case PolygonRelationship.Intersect:
                case PolygonRelationship.AIsInsideBButVerticesTouch:
                case PolygonRelationship.AIsInsideBButEdgesTouch:
                case PolygonRelationship.BIsInsideAButVerticesTouch:
                case PolygonRelationship.BIsInsideAButEdgesTouch:
                default:
                    polygonSubtraction ??= new PolygonSubtraction();
                    var result = polygonA.Subtract(polygonB, interactionRecord, outputAsCollectionType, false, tolerance);
                    result.AddRange(polygonB.Subtract(polygonA, interactionRecord, outputAsCollectionType, false, tolerance));
                    return result;
            }
        }

        #endregion Exclusive-OR Public Methods

        #region RemoveSelfIntersections Public Method

        public static List<Polygon> RemoveSelfIntersections(this Polygon polygon, bool noHoles, out List<Polygon> strayHoles, double tolerance = double.NaN)
        {
            if (double.IsNaN(tolerance)) tolerance = Math.Min(polygon.MaxX - polygon.MinX, polygon.MaxY - polygon.MinY) * Constants.BaseTolerance;
            var intersections = polygon.GetSelfIntersections(tolerance);
            if (intersections.Count == 0)
            {
                strayHoles = null;
                return new List<Polygon> { polygon };
            }
            polygonRemoveIntersections ??= new PolygonRemoveIntersections();
            // if (intersections.Any(n => (n.Relationship & PolygonRemoveIntersections.alignedIntersection) == PolygonRemoveIntersections.alignedIntersection))
            return polygonRemoveIntersections.Run(polygon, intersections, noHoles, tolerance, out strayHoles);
        }

        #endregion RemoveSelfIntersections Public Method
    }
}