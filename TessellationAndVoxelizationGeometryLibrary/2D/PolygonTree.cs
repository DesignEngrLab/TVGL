using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL._2D
{
    /// <summary>
    /// A list of one outer polygon and all the polygons inside it.
    /// </summary>
    public class PolygonTree
    {
        /// <summary>
        /// The list of all the polygons inside the outer polygon. that make up a polygon.
        /// </summary>
        public readonly IEnumerable<Polygon> InnerPolygons;

        /// <summary>
        /// The outer most polygon. All other polygons are inside it.
        /// </summary>
        public readonly Polygon OuterPolygon;

        internal PolygonTree() { }

        internal PolygonTree(Polygon outerPolygon, IEnumerable<Polygon> innerPolygons)
        {
            if (!outerPolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = outerPolygon;
            InnerPolygons = new List<Polygon>(innerPolygons);
        }
    }
    
    /// <summary>
    /// A list of one positive polygon and all the negative polygons directly inside it.
    /// </summary>
    public class ShallowPolygonTree
    {
        /// <summary>
        /// The list of all the negative polygons inside the positive=outer polygon.
        /// There can be NO positive polygons inside this class, since this is a SHALLOW Polygon Tree
        /// </summary>
        public readonly IEnumerable<Polygon> InnerPolygons;

        /// <summary>
        /// The outer most polygon, which is always positive. THe negative polygons are inside it.
        /// </summary>
        public readonly Polygon OuterPolygon;

        internal ShallowPolygonTree() { }

        internal ShallowPolygonTree(Polygon positivePolygon, ICollection<Polygon> negativePolygons)
        {
            if (!positivePolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = positivePolygon;
            if (negativePolygons.Any(negativePolygon => negativePolygon.IsPositive))
            {
                throw new Exception("The inner polygons must be negative");
            }
            InnerPolygons = new List<Polygon>(negativePolygons);
        }
    }
}
