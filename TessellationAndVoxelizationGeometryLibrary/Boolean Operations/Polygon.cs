﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// 2D Polygon
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public readonly IEnumerable<Point> Path;
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

        internal PolygonTree(Polygon outerPolygon, IEnumerable<Polygon> innerPolygons)
        {
            if (!outerPolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = outerPolygon;
            InnerPolygons = new List<Polygon>(innerPolygons);
        }
    }
}
