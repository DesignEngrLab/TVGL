using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TVGL.Numerics;

using TVGL.IOFunctions;

namespace TVGL.TwoDimensional
{

    public class Polygon
    {
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        public List<Vector2> Path
        {
            get
            {
                if (_path == null)
                {
                    _path = new List<Vector2>();
                    foreach (var point in _points)
                    {
                        _path.Add(new Vector2(point.X, point.Y));
                    }
                }
                return _path;
            }
        }
        List<Vector2> _path;


        public List<Node> Points
        {
            get
            {
                if (_points == null)
                {
                    _points = new List<Node>();
                    for (int i = 0; i < _path.Count; i++)
                    {
                        _points.Add(new Node(_path[i], i, Index));
                    }
                }
                return _points;
            }
        }
        List<Node> _points;

        /// The list of lines that make up a polygon. This is not set by default.
        /// </summary>
        public List<PolygonSegment> Lines
        {
            get
            {
                if (_lines == null)
                {
                    _lines = new List<PolygonSegment>();
                    var n = Path.Count - 1;
                    for (var i = 0; i < n; i++)
                        Lines.Add(new PolygonSegment(Points[i], Points[i + 1]));
                    Lines.Add(new PolygonSegment(Points[n], Points[0]));
                }
                return _lines;
            }
        }
        List<PolygonSegment> _lines;

        /// <summary>
        /// A list of the polygons inside this polygon.
        /// </summary>
        public List<Polygon> Children;

        /// <summary>
        /// The polygon that this polygon is inside of.
        /// </summary>
        public Polygon Parent;

        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index { get; set; }

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
        /// This reverses the polygon, including updates to area and the point path.
        /// </summary>
        public void Reverse()
        {
            if (IsPositive) SetToCWNegative();
            else SetToCCWPositive();
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
        /// <param name="coordinates"></param>
        /// <param name="setLines"></param>
        /// <param name="index"></param>
        public Polygon(IEnumerable<Vector2> coordinates, int index = -1)
        {
            _path = coordinates.ToList();
            //set index in path
            MaxX = double.MinValue;
            MinX = double.MaxValue;
            MaxY = double.MinValue;
            MinY = double.MaxValue;
            for (var i = 0; i < Path.Count; i++)
            {
                var point = Path[i];
                if (point.X > MaxX) MaxX = point.X;
                if (point.X < MinX) MinX = point.X;
                if (point.Y > MaxY) MaxY = point.Y;
                if (point.Y < MinY) MinY = point.Y;
                //point.Lines = new List<Line>(); //erase any previous connection to lines.
            }
            Index = index;
            Parent = null;
            Children = new List<Polygon>();
        }

        public Polygon(List<Node> points, List<PolygonSegment> lines, int index = -1)
        {
            _points = points;
            _lines = lines;
            Index = index;
        }

        /// <summary>
        /// Sets a polygon to counter clock wise positive
        /// </summary>
        private void SetToCCWPositive()
        {
            //Check if already positive ccw.
            if (Area >= 0) return;

            //It is negative. Reverse the path and path lines.
            Path.Reverse();
            Area = -Area;

            //Only reverse the lines if they have been generated
            if (_lines == null) return;
            var lines = _lines;
            _lines = new List<PolygonSegment>();
            var n = lines.Count;
            for (var i = 0; i < n; i++)
                _lines[i] = lines[n - i - 1].Reverse();
        }

        private void SetToCWNegative()
        {
            //Check if already negative cw.
            if (Area < 0) return;

            //It is positive. Reverse the path and path lines.
            Path.Reverse();
            Area = -Area;

            //Only reverse the lines if they have been generated
            if (_lines == null) return;
            var lines = _lines;
            _lines = new List<PolygonSegment>();
            var n = lines.Count;
            for (var i = 0; i < n; i++)
                _lines[i] = lines[n - i - 1].Reverse();
        }

        private double CalculateArea()
        {
            return Path.Area();
        }


        public bool IsConvex()
        {
            if (!Area.IsGreaterThanNonNegligible()) return false; //It must have an area greater than zero
            var firstLine = Lines.Last();
            foreach (var secondLine in Lines)
            {
                var cross = firstLine.Vector.Cross(secondLine.Vector);
                if (secondLine.Length.IsNegligible(0.0000001)) continue;// without updating the first line             
                if (cross < 0)
                {
                    return false;
                }
                firstLine = secondLine;
            }
            return true;
        }
    }
}


