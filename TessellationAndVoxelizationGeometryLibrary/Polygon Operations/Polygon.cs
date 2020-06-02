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
                    foreach (var point in _vertices)
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
                if (_vertices == null) MakeVertices();
                return _vertices;
            }
        }

        List<Vertex2D> _vertices;

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
                    if (_vertices == null) MakeVertices();
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
            _vertices = _pointsArray.ToList();
        }
        private void MakeLineSegments()
        {
            var numPoints = Vertices.Count;
            _lines = new List<PolygonSegment>();
            for (int i = 0, j = numPoints - 1; i < numPoints; j = i++)
            {
                var fromNode = Vertices[j];
                var toNode = Vertices[i]; // note the mod operator and the fact that the for loop 
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
        public int Index
        {
            get { return index; }
            set
            {
                if (index == value) return;
                if (value < 0) throw new ArgumentException("The ID or Index of a polygon must be a non-negative integer.");
                index = value;
                if (_vertices != null)
                    foreach (var v in Vertices)
                    {
                        v.LoopID = index;
                    }
            }
        }

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
            if (_vertices != null)
                _vertices.Reverse();
            //Only reverse the lines if they have been generated
            _lines = null;
            _vertices = null;
            if (_innerPolygons!=null)
            {
                foreach (var innerPolygon in _innerPolygons)
                    innerPolygon.Reverse();
            }
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
        private int index = -1;

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
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="createLines">if set to <c>true</c> [create lines].</param>
        /// <param name="index">The index.</param>
        public Polygon(IEnumerable<Vector2> coordinates, bool createLines, int index = -1)
        {
            _path = coordinates.ToList();
            Index = index;
            if (createLines) MakeLineSegments();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="createLines">if set to <c>true</c> [create lines].</param>
        /// <param name="index">The index.</param>
        public Polygon(List<Vertex2D> vertices, bool createLines, int index = -1)
        {
            _vertices = vertices;
            if (createLines) MakeLineSegments();
            Index = index;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon"/> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="lines">The lines.</param>
        /// <param name="index">The index.</param>
        public Polygon(List<Vertex2D> vertices, List<PolygonSegment> lines, int index = -1)
        {
            _vertices = vertices;
            _lines = lines;
            Index = index;
        }

        public Polygon Copy()
        {
            var thisPath = _path == null ? null : new List<Vector2>(_path);
            var thisVertices = _vertices == null ? null : _vertices.Select(v => v.Copy()).ToList();
            var thisInnerPolygons = _innerPolygons == null ? null : _innerPolygons.Select(p => p.Copy()).ToList();
            return new Polygon
            {
                index = this.index,
                area = this.area,
                maxX = this.maxX,
                maxY = this.maxY,
                minX = this.minX,
                minY = this.minY,
                _path = thisPath,
                _vertices = thisVertices,
                _innerPolygons = thisInnerPolygons
            };
        }
        // the following private argument-less constructor is only used in the copy function
        private Polygon() { }
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
                foreach (var point in _vertices)
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


