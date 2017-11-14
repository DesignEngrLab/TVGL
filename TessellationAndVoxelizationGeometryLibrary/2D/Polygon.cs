using System;
using System.Collections.Generic;
using System.Linq;

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
        public List<Point> Path;

        /// <summary>
        /// The list of lines that make up a polygon. This is not set by default.
        /// </summary>
        public List<Line> PathLines;

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
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Area;
        
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
        /// Polygon Constructor. Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="setLines"></param>
        /// <param name="index"></param>
        public Polygon(IEnumerable<Point> points, bool setLines = false, int index = -1)
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
            Area = CalculateArea();
            Length = SetLength();
            PathLines = null;
            Parent = null;
            Childern = new List<Polygon>();

            if (setLines)
            {
                SetPathLines();
            }
        }

        private double SetLength()
        {
            return MiscFunctions.Perimeter(Path);
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
            path.Reverse();
            for (var i = 0; i < path.Count; i++)
            {
                path[i].IndexInPath = i;
            }
            Area = -Area;
            Path = path;

            //Only reverse the lines if they have been generated
            if (PathLines == null) return;
            var lines = new List<Line>(PathLines);
            lines.Reverse();
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInPath = i;
            }
            PathLines = lines;
        }

        private void SetToCWNegative()
        {
            //Check if already negative cw.
            if (Area < 0) return;

            //It is positive. Reverse the path and path lines.
            var path = new List<Point>(Path);
            path.Reverse();
            for (var i = 0; i < path.Count; i++)
            {
                path[i].IndexInPath = i;
            }
            Area = -Area;
            Path = path;

            //Only reverse the lines if they have been generated
            if (PathLines == null) return;
            var lines = new List<Line>(PathLines);
            lines.Reverse();
            for (var i = 0; i < lines.Count; i++)
            {
                lines[i].IndexInPath = i;
            }
            PathLines = lines;
        }

        private double CalculateArea()
        {
            return MiscFunctions.AreaOfPolygon(Path.ToArray());
        }

        /// <summary>
        /// Returns a list of lines that make up the path of this polygon
        /// </summary>
        /// <returns></returns>
        public void SetPathLines()
        {
            var lines = new List<Line>();
            var n = Path.Count;
            for (var i = 0; i < n; i++)
            {
                var j = (i + 1) % n;
                lines.Add(new Line(Path[i], Path[j], true) { IndexInPath = i });
            }
            PathLines = new List<Line>(lines);
        }

        /// <summary>
        /// Gets the next point in the path, given the current point index. 
        /// This function automatically wraps back to the first point.
        /// </summary>
        /// <param name="currentPointIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Point NextPoint(int currentPointIndex)
        {
            return Path[NextPointIndex(currentPointIndex)];
        }

        /// <summary>
        /// Gets the index of the next point in the path, given the current point index. 
        /// This function automatically wraps back to index 0.
        /// </summary>
        /// <param name="currentPointIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int NextPointIndex(int currentPointIndex)
        {
            if(Path[currentPointIndex].IndexInPath != currentPointIndex)
                throw new Exception("Path has been altered and the indices do not match up");
            if (Path.Count == currentPointIndex + 1)
            {
                return 0;
            }
            return currentPointIndex + 1;
        }

        /// <summary>
        /// Gets the next line in the path, given the current line index. 
        /// This function automatically wraps back to the first line.
        /// </summary>
        /// <param name="currentLineIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Line NextLine(int currentLineIndex)
        {
            return PathLines[NextLineIndex(currentLineIndex)];
        }

        /// <summary>
        /// Gets the index of the next line in the path, given the current line index. 
        /// This function automatically wraps back to index 0.
        /// </summary>
        /// <param name="currentLineIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int NextLineIndex(int currentLineIndex)
        {
            if (PathLines[currentLineIndex].IndexInPath != currentLineIndex)
                throw new Exception("Path has been altered and the indices do not match up");
            if (PathLines.Count == currentLineIndex + 1)
            {
                return 0;
            }
            return currentLineIndex + 1;
        }
    }
}


