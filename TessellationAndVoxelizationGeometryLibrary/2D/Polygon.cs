using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL._2D
{
    /// <summary>
    /// A list of 2D points
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public readonly IList<Point> Path;
        /// <summary>
        /// A list of the polygons inside this polygon.
        /// </summary>
        public List<Polygon> Childern;
        /// <summary>
        /// The polygon that this polygon is inside of.
        /// </summary>
        public Polygon Parent;
        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index;
        /// <summary>
        /// Gets whether the polygon has an open path.
        /// </summary>
        public readonly bool IsOpen;

        /// <summary>
        /// Gets whether the path is CCW positive == not a hole.
        /// </summary>
        public bool IsPositive { get; set; }

        internal Polygon()
        {
        }

        internal Polygon(IEnumerable<Point> points, bool isOpen = false)
        {
            Path = new List<Point>(points);
            IsOpen = isOpen;
        }

        //Gets whether this polygon is a hole, based on its position
        //In the polygon tree. 
        //ToDo: Confirm this function, since mine is opposite from Clipper for some reason.
        internal bool IsHole()
        {
            var result = false;
            var parent = Parent;
            while (parent != null)
            {
                result = !result;
                parent = parent.Parent;
            }
            //If it has no parent, then it must be NOT be a hole
            return result;
        }

        internal void AddChild(Polygon child)
        {
            var count = Childern.Count;
            Childern.Add(child);
            child.Parent = this;
            child.Index = count;
        }
    }
}


