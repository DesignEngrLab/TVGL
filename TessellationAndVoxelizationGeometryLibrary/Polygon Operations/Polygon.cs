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
            foreach (var polygon in AllPolygons)
            {
                var numPoints = polygon._path.Count;
                var pointsArray = new Vertex2D[numPoints];
                for (int i = 0; i < numPoints; i++)
                    pointsArray[i] = new Vertex2D(polygon._path[i], i, Index);
                polygon._vertices = pointsArray.ToList();
            }
        }

        private void MakeLineSegments()
        {
            foreach (var polygon in AllPolygons)
            {
                var numPoints = polygon.Vertices.Count;
                polygon._lines = new List<PolygonSegment>();
                for (int i = 0, j = numPoints - 1; i < numPoints; j = i++)
                {
                    var fromNode = polygon.Vertices[j];
                    var toNode = polygon.Vertices[i]; // note the mod operator and the fact that the for loop 
                    // goes to and including numPoints. this allows for the last line to connect the last point 
                    // back to the first. it is intended to avoid rewriting the following four lines of code.
                    var polySegment = new PolygonSegment(fromNode, toNode);
                    fromNode.StartLine = polySegment;
                    toNode.EndLine = polySegment;
                    polygon._lines.Add(polySegment);
                }
            }
        }


        /// <summary>
        /// Adds the hole to the polygon. This method assume that there are no intersections between the hole polygon
        /// and the host polygon. However, it does check and remove holes in the host that are fully inside of the
        /// new hole.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        public void AddHole(Polygon polygon)
        {
            if (polygon is null || (polygon._path is null && polygon._vertices is null)) return;
            if (polygon.IsPositive) polygon.Reverse();
            _holes ??= new List<Polygon>();
            if (polygon._lines is null && _lines != null)
                polygon.MakeLineSegments();
            for (int i = _holes.Count-1; i >=0; i--)
                if (polygon.IsNonIntersectingPolygonInside(_holes[i], out _))
                    _holes.RemoveAt(i);
            _holes.Add(polygon);
        }

        /// <summary>
        /// Removes the hole from the polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        public void RemoveHole(Polygon polygon)
        {
            _holes.Remove(polygon);
        }
        public IEnumerable<Polygon> Holes
        {
            get
            {
                if (_holes is null) yield break;
                foreach (var hole in _holes)
                    yield return hole;
            }
        }

        List<Polygon> _holes;

        public IEnumerable<Polygon> AllPolygons
        {
            get
            {
                yield return this;
                if (_holes is null) yield break;
                foreach (var innerPolygon in _holes)
                    foreach (var polygon in innerPolygon.AllPolygons)
                        yield return polygon;
            }
        }

        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        public int Index
        {
            get => index;
            set
            {
                if (index == value) return;
                if (value < 0)
                    throw new ArgumentException("The ID or Index of a polygon must be a non-negative integer.");
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
            if (_holes != null)
            {
                foreach (var innerPolygon in _holes)
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
                return area + Holes.Sum(p => p.Area);
            }
        }

        private double area = double.NaN;

        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        public double Perimeter
        {
            get
            {
                if (double.IsNaN(perimeter))
                    perimeter = Path.Perimeter();
                return perimeter + Holes.Sum(p => p.Perimeter);
            }
        }

        private double perimeter = double.NaN;

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
            var thisInnerPolygons = _holes == null ? null : _holes.Select(p => p.Copy()).ToList();
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
                _holes = thisInnerPolygons
            };
        }

        // the following private argument-less constructor is only used in the copy function
        private Polygon()
        {
        }

        public bool IsConvex()
        {
            if (!Area.IsGreaterThanNonNegligible()) return false; //It must have an area greater than zero
            var firstLine = Lines.Last();
            foreach (var secondLine in Lines)
            {
                var cross = firstLine.Vector.Cross(secondLine.Vector);
                if (secondLine.Length.IsNegligible(0.0000001)) continue; // without updating the first line             
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

        /// <summary>
        /// Transforms the specified transform matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public void Transform(Matrix3x3 transformMatrix)
        {
            foreach (var polygon in AllPolygons)
            {
                polygon.minX = double.PositiveInfinity;
                polygon.minY = double.PositiveInfinity;
                polygon.maxX = double.NegativeInfinity;
                polygon.maxY = double.NegativeInfinity;
                for (var i = 0; i < polygon.Path.Count; i++)
                {
                    var v = polygon.Path[i];
                    polygon.Path[i] = v = v.Transform(transformMatrix);
                    if (minX > v.X) minX = v.X;
                    if (minY > v.Y) minY = v.Y;
                    if (maxX < v.X) maxX = v.X;
                    if (maxY < v.Y) maxY = v.Y;
                }
            }

            area = double.NaN;
            perimeter = double.NaN;

            MakeVertices();
            MakeLineSegments();
        }
    }
}


