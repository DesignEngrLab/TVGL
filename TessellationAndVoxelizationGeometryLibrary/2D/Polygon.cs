﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
{
    internal enum PolygonType
    {
        Subject,
        Clip
    };

    /// <summary>
    /// A list of 2D points
    /// </summary>
    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public IList<Point> Path;
        /// <summary>
        /// The list of lines that make up a polygon.
        /// </summary>
        public IList<Line> PathLines;
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
        public int Index
        {
            get { return _index; }
            set
            {
                _index = value;
                foreach (var point in Path)
                {
                    point.PolygonIndex = _index;
                }
            }
        }

        private int _index;

        /// <summary>
        /// Gets whether the polygon has an open path.
        /// </summary>
        public readonly bool IsOpen;

        /// <summary>
        /// Gets or sets whether the path is CCW positive. This will reverse the path if it was ordered CW.
        /// </summary>
        public bool IsPositive
        {
            get { return !(Area < 0); }
            set
            {
                if (value == true)
                {
                    SetToCCWPositive();
                }
                else SetToCWNegative();
            }
        }

        /// <summary>
        /// Gets the length of the polygon.
        /// </summary>
        public double Length;

        /// <summary>
        /// Gets whether the path is CCW positive == not a hole.
        /// </summary>
        public bool IsConvex;

        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Area;

        /// <summary>
        /// Gets whether the path is self intersecting.
        /// </summary>
        public bool IsSelfIntersecting;
        
        /// <summary>
        /// Maxiumum X value
        /// </summary>
        public double MaxX;

        /// <summary>
        /// Miniumum X value
        /// </summary>
        public double MinX;

        /// <summary>
        /// Maxiumum Y value
        /// </summary>
        public double MaxY;

        /// <summary>
        /// Minimum Y value
        /// </summary>
        public double MinY;

        /// <summary>
        /// Polygon Constructor
        /// </summary>
        /// <param name="points"></param>
        /// <param name="isOpen"></param>
        /// <param name="index"></param>
        public Polygon(IEnumerable<Point> points, bool isOpen = false, int index = -1)
        {
            Path = new List<Point>(points);
            //set index in path
            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;
            for (var i =0; i < Path.Count; i++)
            {
                var point = Path[i];
                if (point.X > MaxX) MaxX = point.X;
                else if (point.X < MinX) MinX = point.X;
                if (point.Y > MaxY) MaxY = point.Y;
                else if (point.Y < MinY) MinY = point.Y;
                point.IndexInPath = i;
                point.Lines = new List<Line>(); //erase any previous connection to lines.
            }
            Index = index;
            IsOpen = isOpen;
            IsConvex = IsThisConvex();
            PathLines = SetPathLines();
            IsSelfIntersecting = IsThisSelfIntersecting();
            Area = CalculateArea();
            Length = SetLength();
        }

        private double SetLength()
        {
            return PathLines.Sum(line => line.Length);
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

        internal void Simplify()
        {
            List<Point> outPath;
            var canSimplify = PolygonOperations.CanSimplifyToSinglePolygon(Path, out outPath);
            if (canSimplify)
            {
                Path = outPath;
                IsSelfIntersecting = false;
            }
            else
            {
                Debug.WriteLine("Could not simplify polygon to a single polygon.");
            }
        }

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        private void SetToCCWPositive()
        {
            //Check if already positive ccw.
            if (!(Area < 0)) return;

            //It is negative. Reverse the path and path lines.
            var path = new List<Point>(Path);
            var lines = new List<Line>(PathLines);
            path.Reverse();
            lines.Reverse();

            //Renumber the points and lines
            for (var i = 0; i < path.Count; i++)
            {
                path[i].IndexInPath = i;
            }
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInList = i;
            }

            //Invert the area.
            Area = -Area;
            Path = path;
            PathLines = lines;
        }

        private void SetToCWNegative()
        {
            //Check if already negative cw.
            if (Area < 0) return;

            //It is positive. Reverse the path and path lines.
            var path = new List<Point>(Path);
            var lines = new List<Line>(PathLines);
            path.Reverse();
            lines.Reverse();

            //Renumber the points and lines
            for (var i = 0; i < path.Count; i++)
            {
                path[i].IndexInPath = i;
            }
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInList = i;
            }

            //Invert the area.
            Area = -Area;
            Path = path;
            PathLines = lines;
        }


        private double CalculateArea()
        {
            return MiscFunctions.AreaOfPolygon(Path.ToArray());
        }

        /// <summary>
        /// Gets whether the polygon is convex.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <assumptions>
        /// 1. the polygon is closed
        /// 2. the last point is not repeated.
        /// 3. the polygon is simple (does not intersect itself or have holes)
        /// </assumptions>
        /// /// <source>
        /// http://debian.fmi.uni-sofia.bg/~sergei/cgsr/docs/clockwise.htm
        /// </source>
        private bool IsThisConvex()
        {
            var n = Path.Count;
            var flag = 0;

            if (n < 3) return false;

            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                var k = (i + 2) % n;
                var z = (Path[j].X - Path[i].X) * (Path[k].Y - Path[j].Y);
                z -= (Path[j].Y - Path[i].Y) * (Path[k].X - Path[j].X);
                if (z < 0)
                    flag |= 1;
                else if (z > 0)
                    flag |= 2;
                if (flag == 3)
                    return (true);
            }
            if (flag != 0)
            {
                return (false);
            }
            return false;
           // throw new Exception("Concavity could not be determined. May be due to colinear points. Add functionality to code to account for this.");
        }

        /// <summary>
        /// Returns a list of lines that make up the path of this polygon
        /// </summary>
        /// <returns></returns>
        private List<Line> SetPathLines()
        {
            var lines = new List<Line>();
            var n = Path.Count;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                lines.Add(new Line(Path[i], Path[j], true) { IndexInList = i });
            }
            return lines;
        }

        private bool IsThisSelfIntersecting()
        {
            var intersectionPoints = GetSelfIntersectionPoints();
            return intersectionPoints.Any();
        }

        /// <summary>
        /// Gets all the intersection points if the polygon is self intersecting.
        /// </summary>
        /// <returns></returns>
        public List<Point> GetSelfIntersectionPoints()
        {
            //Path Lines must be created first.
            if (PathLines == null || PathLines.Count == 0)
            {
                SetPathLines();
            }
            var orderedLoop = Path;
            const int precision = 15;
            var sortedPoints = orderedLoop.OrderBy(point => Math.Round(point.Y, precision)).ThenBy(point => Math.Round(point.X, precision)).ToList();
            var intersectionPoints = new Dictionary<int, Point>();
            //inititallize lineList 
            var lineList = new HashSet<Line>();
            foreach (var point1 in sortedPoints)
            {
                //Add to or remove lines from Red-Black Tree
                foreach (var line in point1.Lines)
                {
                    if (lineList.Contains(line))
                    {
                        lineList.Remove(line);
                    }
                    else
                    {
                        lineList.Add(line);
                    }
                }
                //Create a second lineList in a perpendicular direction
                var unsortedPointSet2 = new HashSet<Point>();
                foreach (var line in lineList)
                {
                    if (!unsortedPointSet2.Contains(line.ToPoint)) unsortedPointSet2.Add(line.ToPoint);
                    if (!unsortedPointSet2.Contains(line.FromPoint)) unsortedPointSet2.Add(line.FromPoint);
                }
                var sortedPointSet2 =
                    unsortedPointSet2.OrderBy(point => Math.Round(point.X, precision))
                        .ThenBy(point => Math.Round(point.Y, precision))
                        .ToList();
                var lineList2 = new HashSet<Line>();
                foreach (var point in sortedPointSet2)
                {
                    //Add to or remove lines from Red-Black Tree
                    foreach (var line in point.Lines.Where(line => lineList.Contains(line)))
                    {
                        if (lineList2.Contains(line))
                        {
                            lineList2.Remove(line);
                        }
                        else
                        {
                            lineList2.Add(line);
                        }
                    }

                    //Check if any of the lines are crossing 
                    foreach (var line1 in lineList2)
                    {
                        foreach (var line2 in lineList2)
                        {
                            if (line2 == line1) continue;
                            //Check if this intersection has already been found
                            var index = HashIndexOfIntersection(line1, line2);
                            if (intersectionPoints.ContainsKey(index)) continue;
                            //Only consider them, if they don't share any points.
                            if (line1.FromPoint == line2.FromPoint || line1.ToPoint == line2.FromPoint ||
                                line1.FromPoint == line2.ToPoint || line1.ToPoint == line2.ToPoint) continue;
                            Point intersectionPoint;
                            var linesIntersect = MiscFunctions.LineLineIntersection(line1, line2, out intersectionPoint);
                            if (linesIntersect) intersectionPoints.Add(index, intersectionPoint);
                        }
                    }
                }
            }
            //Debug.WriteLine("Number of intersections = " + intersectionPoints.Count);
            return intersectionPoints.Values.ToList();
        }

        private int HashIndexOfIntersection(Line line1, Line line2)
        {
            if (line1.IndexInList < line2.IndexInList)
            {
                return line2.IndexInList * Path.Count + line1.IndexInList;
            }
            return line1.IndexInList * Path.Count + line2.IndexInList;
        }

        
    }
}


