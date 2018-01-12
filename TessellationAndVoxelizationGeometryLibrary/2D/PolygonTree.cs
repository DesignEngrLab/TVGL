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
        /// A list of all the polygons in this tree.
        /// </summary>
        public IList<List<Point>> AllPaths => AllPolygons.Select(polygon => polygon.Path).ToList();

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
        /// Gets the Shallow Polygon Trees for a given set of polygons. 
        /// </summary>
        /// <param name="polygons"></param>
        /// <returns></returns>
        public static List<ShallowPolygonTree> GetShallowPolygonTrees(List<Polygon> polygons)
        {
            //Note: Clipper's UnionEvenOdd function does not order polygons correctly for a shallow tree.
            //The PolygonOperation.UnionEvenOdd calls this function to ensure they are ordered correctly

            //The correct order for shallow polygon trees is as follows.
            //The first polygon in the list is always positive. The next positive polygon signals the start of a new 
            //shallow tree. Any polygons in-between those belong to the earlier shallow tree.

            //Assumption: Ordered even-odd polygons. 
            //Example: A negative polygon must be between two concentric positive polygons.

            //By ordering the polygons, we are gauranteed to do the outermost positive polygons first.
            var orderedPolygons = polygons.OrderByDescending(p => p.Area);

            //1) Make a list of all the shallow polygon trees from the positive polygons
            var shallowPolygonTrees = new List<ShallowPolygonTree>();
            foreach (var polygon in orderedPolygons)
            {
                if (!polygon.IsPositive) break; //We reached the negative polygons
                shallowPolygonTrees.Add(new ShallowPolygonTree(polygon));
            }

            //This puts the smallest positive polygons first.
            shallowPolygonTrees.Reverse();

            //2) Find the positive polygon that this negative polygon is inside.
            //The negative polygon belongs to the smallest positive polygon that it fits inside.
            //The absolute area of the polygons (which is accounted for in the IsPolygonInsidePolygon function) 
            //and the reversed ordering, gaurantee that we get the correct shallow tree.
            foreach (var negativePolygon in orderedPolygons)
            {
                if (negativePolygon.IsPositive) continue;
                var isInside = false;

                //Start with the smallest positive polygon           
                foreach (var shallowTree in shallowPolygonTrees)
                {
                    if (MiscFunctions.IsPolygonInsidePolygon(shallowTree.OuterPolygon, negativePolygon))
                    {
                        shallowTree.InnerPolygons.Add(negativePolygon);
                        negativePolygon.Parent = shallowTree.OuterPolygon;
                        shallowTree.OuterPolygon.Childern.Add(negativePolygon);
                        //The negative polygon ONLY belongs to the smallest positive polygon that it fits inside.
                        isInside = true;
                        break;
                    }
                }

                if (!isInside) throw new Exception("Negative polygon was not inside any positive polygons");
            }

            //Set the polygon indices
            var polygonCount = 0;
            foreach (var shallowPolygonTree in shallowPolygonTrees)
            {
                foreach (var polygon in shallowPolygonTree.AllPolygons)
                {
                    polygon.Index = polygonCount;
                    polygonCount++;
                }
            }

            return shallowPolygonTrees;
        }

        /// <summary>
        /// Gets the Shallow Polygon Trees for a given set of paths. 
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static List<ShallowPolygonTree> GetShallowPolygonTrees(List<List<Point>> paths)
        {
            return GetShallowPolygonTrees(paths.Select(path => new Polygon(path)).ToList());
        }
    }
}
