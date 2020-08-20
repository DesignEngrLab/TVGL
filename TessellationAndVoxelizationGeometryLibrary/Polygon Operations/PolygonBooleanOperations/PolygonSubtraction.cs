using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonSubtraction : PolygonIntersection
    {
        /// <summary>
        /// Handles identical polygons. In this case subPolygon is always on polygonA and the duplicated is in polygonB
        /// </summary>
        /// <param name="subPolygon">The sub polygon.</param>
        /// <param name="positivePolygons">The positive polygons.</param>
        /// <param name="negativePolygons">The negative polygons.</param>
        /// <param name="identicalPolygonIsInverted">The identical polygon is inverted.</param>
        protected override void HandleIdenticalPolygons(Polygon subPolygonA, List<Polygon> newPolygons, bool identicalPolygonIsInverted)
        {
            if (identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<(PolygonRelationship, bool)> relationships, bool partOfPolygonB)
        {
            // the possibilities are AInsideB, BInsideA, or Separated
            var enumerator = relationships.GetEnumerator();
            if (!partOfPolygonB) // then part of A or the minuend
            {
                while (enumerator.MoveNext())
                {
                    var rel = enumerator.Current.Item1;
                    var otherIsPositive = enumerator.Current.Item2;
                    if ((rel & PolygonRelationship.BInsideA) != 0b0 != otherIsPositive)   // either 1) B is inside of A and B is negative (well, originally positive) or 2) A is inside B and B is positive (originally negative)
                    {
                        newPolygons.Add(subPolygon.Copy(false, false));
                        return;
                    }
                }
            }
            else  // then part of the subtrahend
            {
                while (enumerator.MoveNext())  //it is inside the outer of the other, so it should likely be included unless it is in a hole of the other
                {
                    var rel = enumerator.Current.Item1;
                    var otherIsPositive = enumerator.Current.Item2;
                    if ((rel & PolygonRelationship.BInsideA) != 0b0 != otherIsPositive)  // either 1) B is inside of A and A is negative or 2) A is inside B and 
                        // A is positive then just return
                        return;
                }
                newPolygons.Add(subPolygon.Copy(false, false));
            }
        }
    }
}
