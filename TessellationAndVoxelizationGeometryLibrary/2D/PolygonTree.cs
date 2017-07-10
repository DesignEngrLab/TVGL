using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A list of one positive (Outer) polygon and all the negative (Inner) polygons directly inside it.
    /// Use GetShallowPolygonTrees() method to create a list of ShallowPolygonTrees for a cross section.
    /// </summary>
    public class ShallowPolygonTree
    {
        /// <summary>
        /// The list of all the negative polygons inside the positive=outer polygon.
        /// There can be NO positive polygons inside this class, since this is a SHALLOW Polygon Tree
        /// </summary>
        public IList<Polygon> InnerPolygons;

        /// <summary>
        /// The outer most polygon, which is always positive. THe negative polygons are inside it.
        /// </summary>
        public readonly Polygon OuterPolygon;

        /// <summary>
        /// A list of all the polygons in this tree.
        /// </summary>
        public IList<Polygon> AllPolygons => new List<Polygon>(InnerPolygons) {OuterPolygon};

        /// <summary>
        /// Gets the area of the shallow polygon tree (OuterPolygon - InnerPolygons)
        /// </summary>
        public double Area => AllPolygons.Sum(p => p.Area);

        /// <summary>
        /// Create an empty ShallowPolygonTree
        /// </summary>
        public ShallowPolygonTree() { }
        
        /// <summary>
        /// Create an ShallowPolygonTree with just a positive polygon
        /// </summary>
        public ShallowPolygonTree(Polygon positivePolygon)
        {
            if (!positivePolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            InnerPolygons = new List<Polygon>();
            OuterPolygon = positivePolygon;
        }

        /// <summary>
        /// Create an ShallowPolygonTree with the positive (outer) polygon and negative (inner) polygons
        /// </summary>
        public ShallowPolygonTree(Polygon positivePolygon, ICollection<Polygon> negativePolygons)
        {
            if (!positivePolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = positivePolygon;
            if (negativePolygons.Any(negativePolygon => negativePolygon.IsPositive))
            {
                throw new Exception("The inner polygons must be negative");
            }
            foreach (var negativePolygon in negativePolygons)
            {
                negativePolygon.Parent = OuterPolygon;
                OuterPolygon.Childern.Add(negativePolygon);
            }
            InnerPolygons = new List<Polygon>(negativePolygons);
        }

        /// <summary>
        /// Gets the Shallow Polygon Trees for a given set of paths. If the paths are already ordered correctly, 
        /// it will return shallow trees using their current order. Else, it will use Clipper's UnionEvenOdd.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="alreadyInOrder"></param>
        /// <returns></returns>
        public static List<ShallowPolygonTree> GetShallowPolygonTrees(List<List<Point>> paths, bool alreadyInOrder = false)
        {
            //The correct order for shallow polygon trees is as follows.
            //The first polygon in the list is always positive. The next positive polygon signals the start of a new 
            //shallow tree. Any polygons in-between those belong to the earlier shallow tree.
            var result = !alreadyInOrder ? PolygonOperations.Union(paths, false, PolygonFillType.EvenOdd) : paths;

            var shallowPolygonTrees = new List<ShallowPolygonTree>();
            foreach (var path in result)
            {
                var newPolygon = new Polygon(path);
                if (newPolygon.IsPositive)
                {
                    shallowPolygonTrees.Add(new ShallowPolygonTree(newPolygon));
                }
                else
                {
                    var shallowTree = shallowPolygonTrees.Last();
                    shallowTree.InnerPolygons.Add(newPolygon);
                    newPolygon.Parent = shallowTree.OuterPolygon;
                    shallowTree.OuterPolygon.Childern.Add(newPolygon);
                }
            }

            return shallowPolygonTrees;
        }
    }
}
