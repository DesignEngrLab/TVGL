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
            if (!identicalPolygonIsInverted)
                newPolygons.Add(subPolygonA.Copy(false, false));  //add the positive as a positive or add the negative as a negative
        }

        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, List<Polygon> newPolygons,
            IEnumerable<PolygonRelationship> relationships, bool partOfPolygonB)
        {
            var enumerator = relationships.GetEnumerator();
            enumerator.MoveNext();
            var relWithOuter = enumerator.Current;
            if (!partOfPolygonB && (relWithOuter < PolygonRelationship.AInsideB ||
                (relWithOuter & PolygonRelationship.Intersection) != PolygonRelationship.BInsideA))
            {
                newPolygons.Add(subPolygon.Copy(false, false));
                return;
            }
            while (enumerator.MoveNext())  //it is inside the outer of the other, so it should likely be included unless it is in a hole of the other
            {

                var relWithInner = enumerator.Current;
                if (!partOfPolygonB)
                    if ((!partOfPolygonB && (subPolygon.IsPositive != ((relWithInner & PolygonRelationship.AInsideB) != 0b0))) ||
                        (partOfPolygonB && (subPolygon.IsPositive != ((relWithInner & PolygonRelationship.BInsideA) != 0b0))))
                        return;
            }
            newPolygons.Add(subPolygon.Copy(false, false));
        }
    }
}
