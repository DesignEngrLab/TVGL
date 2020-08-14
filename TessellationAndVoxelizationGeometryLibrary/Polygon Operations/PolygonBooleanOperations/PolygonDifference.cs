using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// A set of general operation for points and paths
    /// </summary>
    internal class PolygonDifference : PolygonIntersection
    {
        protected override void HandleNonIntersectingSubPolygon(Polygon subPolygon, Polygon polygonA, Polygon polygonB, List<Polygon> newPolygons, bool partOfPolygonB)
        {
            var insideOther = subPolygon.Vertices[0].Type == NodeType.Inside;
            if (partOfPolygonB) // part of the subtrahend, the B in A-B
            {
                if (insideOther)
                    newPolygons.Add(subPolygon.Copy(false, true)); // add the positive as a negative or add the negative as a positive
            }
            else if (!insideOther) // then part of the minuend, the A in A-B
                                   // then on the outside of the other, but could be inside a hole
                newPolygons.Add(subPolygon.Copy(false, false));  //add the positive as a positive or add the negatie as a negative
        }
    }
}
