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


        public List<Vertex2D> Vertices
        {
            get
            {
                if (_points == null) MakeVertices();
                return _points;
            }
        }

        List<Vertex2D> _points;

        /// <summary>
        /// Gets the list of lines that make up a polygon. This is not set by default.
        /// </summary>
        /// <value>The lines.</value>
        public List<PolygonSegment> Lines
        {
            get
            {
                if (_lines == null)
                {
                    if (_points == null) MakeVertices();
                    MakeLineSegments();
                }
                return _lines;
            }
        }
        List<PolygonSegment> _lines;
        private void MakeVertices()
        {
            var numPoints = _path.Count;
            var _pointsArray = new Vertex2D[numPoints];
            for (int i = 0; i < numPoints; i++)
                _pointsArray[i] = new Vertex2D(_path[i], i, Index);
            _points = _pointsArray.ToList();
        }
        private void MakeLineSegments()
        {
            var numPoints = _points.Count;
            _lines = new List<PolygonSegment>();
            for (int i = 1; i <= numPoints; i++)
            {
                var fromNode = _points[i - 1];
                var toNode = _points[i % numPoints]; // note the mod operator and the fact that the for loop 
                // goes to and including numPoints. this allows for the last line to connect the last point 
                // back to the first. it is intended to avoid rewriting the following four lines of code.
                var polySegment = new PolygonSegment(fromNode, toNode);
                fromNode.StartLine = polySegment;
                toNode.EndLine = polySegment;
                _lines.Add(polySegment);
            }
        }



        public List<Polygon> InnerPolygons
        {
            get
            {
                if (_innerPolygons == null)
                    _innerPolygons = new List<Polygon>();
                return _innerPolygons;
            }
        }
        List<Polygon> _innerPolygons;

        public IEnumerable<Polygon> AllPolygons
        {
            get
            {
                yield return this;
                foreach (var innerPolygon in InnerPolygons)
                    foreach (var polygon in innerPolygon.AllPolygons)
                        yield return polygon;
            }
        }

        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets whether the path is CCW positive. This will reverse the path if it was ordered CW.
        /// </summary>
        public bool IsPositive
        {
            get { return Area >= 0; }
            set
            {
                if (value != (Area >= 0))
                    Reverse();
            }
        }


        /// <summary>
        /// This reverses the polygon, including updates to area and the point path.
        /// </summary>
        public void Reverse()
        {
            area = -Area;
            Path.Reverse();

            //Only reverse the lines if they have been generated
            if (_lines == null) return;
            var lines = _lines;
            _lines = new List<PolygonSegment>();
            var n = lines.Count;
            for (var i = 0; i < n; i++)
                _lines[i] = lines[n - i - 1].Reverse();

        }


        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Area
        {
            get
            {
                if (double.IsNaN(area))
                    area = Path.Area();
                return area + InnerPolygons.Sum(p => p.Area);
            }
        }

        private double area = double.NaN;

        /// <summary>
        /// Maxiumum X value
        /// </summary>
        public double MaxX
        {
            get
            {
                if (double.IsInfinity(maxX))
                    SetBounds();
                return maxX;
            }
        }

        private double maxX = double.NegativeInfinity;

        /// <summary>
        /// Miniumum X value
        /// </summary>
        public double MinX
        {
            get
            {
                if (double.IsInfinity(minX))
                    SetBounds();
                return minX;
            }
        }

        private double minX = double.PositiveInfinity;

        /// <summary>
        /// Maxiumum Y value
        /// </summary>
        public double MaxY
        {
            get
            {
                if (double.IsInfinity(maxY))
                    SetBounds();
                return maxY;
            }
        }

        private double maxY = double.NegativeInfinity;

        /// <summary>
        /// Minimum Y value
        /// </summary>
        private double minY = double.PositiveInfinity;
        public double MinY
        {
            get
            {
                if (double.IsInfinity(minY))
                    SetBounds();
                return minY;
            }
        }

        /// <summary>
        /// Polygon Constructor. Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="setLines"></param>
        /// <param name="index"></param>
        public Polygon(IEnumerable<Vector2> coordinates, int index = -1)
        {
            _path = coordinates.ToList();
            Index = index;
        }

        public Polygon(List<Vertex2D> points, List<PolygonSegment> lines, int index = -1)
        {
            _points = points;
            _lines = lines;
            Index = index;
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

        private void SetBounds()
        {
            if (_path != null)
            {
                foreach (var point in _path)
                {
                    if (point.X > maxX) maxX = point.X;
                    if (point.X < minX) minX = point.X;
                    if (point.Y > maxY) maxY = point.Y;
                    if (point.Y < minY) minY = point.Y;
                }
            }
            else
            {
                foreach (var point in _points)
                {
                    if (point.X > maxX) maxX = point.X;
                    if (point.X < minX) minX = point.X;
                    if (point.Y > maxY) maxY = point.Y;
                    if (point.Y < minY) minY = point.Y;
                }
            }
        }
    }
}


