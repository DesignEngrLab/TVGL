// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{

    /// <summary>
    /// Class Polygon.
    /// </summary>
    public class Polygon
    {
        #region Constructors and Copy

        // the following argument-less constructor is only used in the copy function
        // and in deserialization
        /// <summary>
        /// Prevents a default instance of the <see cref="Polygon"/> class from being created.
        /// </summary>
        public Polygon() { }

        public Polygon(IEnumerable<Vector2> coordinates, int index = -1)
        {
            Index = index;
            _path = coordinates as List<Vector2> ?? coordinates.ToList();
        }


        public Polygon(IEnumerable<IList<Vector2>> loops) : this(loops.First())
        {
            foreach (var innerLoop in loops.Skip(1))
                AddInnerPolygon(new Polygon(innerLoop));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="index">The index.</param>
        public Polygon(IEnumerable<Vertex2D> vertices, int index = -1)
        {
            _vertices = vertices as List<Vertex2D> ?? vertices.ToList();
            Index = index;
            //SetBounds();

            /*
            var prevX = Math.Round(_vertices[0].X, NumSigDigits);
            var prevY = Math.Round(_vertices[0].Y, NumSigDigits);

            for (int i = _vertices.Count - 1; i >= 0; i--)
            {
                var x = Math.Round(_vertices[i].X, NumSigDigits);
                var y = Math.Round(_vertices[i].Y, NumSigDigits);
                if (x != prevX || y != prevY)
                {
                    _vertices[i].Coordinates = new Vector2(x, y);
                    prevX = x;
                    prevY = y;
                }
                else
                    _vertices.RemoveAt(i);
            }
            */
            _path = _vertices.Select(v => v.Coordinates).ToList();
        }

        /// <summary>
        /// Copies the specified copy inner polygons.
        /// </summary>
        /// <param name="copyInnerPolygons">The copy inner polygons.</param>
        /// <param name="invert">The invert.</param>
        /// <returns>TVGL.TwoDimensional.Polygon.</returns>
        public Polygon Copy(bool copyInnerPolygons, bool invert)
        {
            List<Vector2> thisPath = null;
            if (invert)
            {
                thisPath = new List<Vector2>(Path);
                thisPath.Reverse();
                // now the following three lines are to aid with mapping old polygon data to new polygon data.
                // we are simply moving the first element to the end - the polygon doesn't change but not the 
                // original first line will be the last flipped line. The second original line will be the second
                // to last flipped line.
                var front = thisPath[0];
                thisPath.RemoveAt(0);
                thisPath.Add(front);
            }
            else thisPath = Path;
            var thisInnerPolygons = _innerPolygons != null && copyInnerPolygons ?
                _innerPolygons.Where(p => p.Vertices.Count > 0)
                .Select(p => p.Copy(true, invert)).ToList() : null;
            var copiedArea = copyInnerPolygons ? this.area : this.pathArea;
            if (invert) copiedArea *= -1;
            var copiedPolygon = new Polygon(thisPath, this.index)
            {
                area = copiedArea,
                maxX = this.maxX,
                maxY = this.maxY,
                minX = this.minX,
                minY = this.minY,
                _innerPolygons = thisInnerPolygons
            };
            return copiedPolygon;
        }


        #endregion

        #region Fields and Properties
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
        public List<Vector2> Path => _path;

        /// <summary>
        /// The list of 2D points that make up a polygon and its inner polygons
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
        public IEnumerable<List<Vector2>> AllPaths
        {
            get
            {
                return AllPolygons.Select(p => p.Path);
            }
        }

        /// <summary>
        /// The path
        /// </summary>
        List<Vector2> _path;


        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <value>The vertices.</value>
        [JsonIgnore]
        public List<Vertex2D> Vertices
        {
            get
            {
                if (_vertices == null && _path != null) MakeVerticesFromPath();
                return _vertices;
            }
        }
        /// <summary>
        /// The vertices
        /// </summary>
        List<Vertex2D> _vertices;

        internal int NumSigDigits
        {
            get
            {
                if (_numSigDigits < 0) SetBounds();
                return _numSigDigits;
            }
        }
        int _numSigDigits = int.MinValue;

        /// <summary>
        /// Gets the ordered x vertices.
        /// </summary>
        /// <value>The ordered x vertices.</value>
        [JsonIgnore]
        internal Vertex2D[] OrderedXVertices
        {
            get
            {
                if (Vertices != null && (_orderedXVertices == null || _orderedXVertices.Length != Vertices.Count))
                    _orderedXVertices = Vertices.OrderBy(v => v, new VertexSortedByXFirst()).ToArray();
                return _orderedXVertices;
            }
        }
        /// <summary>
        /// The ordered x vertices
        /// </summary>
        Vertex2D[] _orderedXVertices;

        /// <summary>
        /// Gets the list of lines that make up a polygon. This is not set by default.
        /// </summary>
        /// <value>The lines.</value>
        [JsonIgnore]
        public PolygonEdge[] Edges
        {
            get
            {
                MakePolygonEdgesIfNonExistent();
                return _edges;
            }
        }
        #endregion

        public void MakePolygonEdgesIfNonExistent()
        {
            if (_edges != null && _edges.Length == Vertices.Count) return;
            foreach (var poly in AllPolygons)
                poly.MakeThisPolygonsEdges();
        }

        private void MakeThisPolygonsEdges()
        {
            var numPoints = (Vertices != null) ? Vertices.Count : 0;
            _edges = new PolygonEdge[numPoints];
            for (int i = 0, j = numPoints - 1; i < numPoints; j = i++)
            // note this compact approach to setting i and j. 
            {
                var fromNode = Vertices[j];
                var toNode = Vertices[i];
                var polySegment = new PolygonEdge(fromNode, toNode);
                fromNode.StartLine = polySegment;
                toNode.EndLine = polySegment;
                _edges[i] = polySegment;
            }
        }

        /// <summary>
        /// The lines
        /// </summary>
        private PolygonEdge[] _edges;


        /// <summary>
        /// Removes all inner polygon.
        /// </summary>
        internal void RemoveAllInnerPolygon()
        {
            _innerPolygons = null;
        }


        /// <summary>
        /// Adds the hole to the polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public void AddInnerPolygon(Polygon polygon)
        {
            _innerPolygons ??= new List<Polygon>();
            _innerPolygons.Add(polygon);
            perimeter = double.NaN;
            area = double.NaN;
        }

        /// <summary>
        /// Removes the hole from the polygon.
        /// </summary>
        /// <param name="polygon">The polygon.</param>
        public void RemoveHole(Polygon polygon)
        {
            _innerPolygons.Remove(polygon);
        }
        /// <summary>
        /// Gets the inner polygons.
        /// </summary>
        /// <value>The inner polygons.</value>
        [JsonIgnore]
        public IEnumerable<Polygon> InnerPolygons
        {
            get
            {
                if (_innerPolygons is null) yield break;
                foreach (var hole in _innerPolygons)
                    yield return hole;
            }
        }
        /// <summary>
        /// Gets the number of inner polygons.
        /// </summary>
        /// <value>The number of inner polygons.</value>
        [JsonIgnore]
        public int NumberOfInnerPolygons => (_innerPolygons?.Count) ?? 0;

        /// <summary>
        /// The inner polygons
        /// </summary>
        [JsonProperty("Inners")]
        List<Polygon> _innerPolygons;

        /// <summary>
        /// Gets all polygons.
        /// </summary>
        /// <value>All polygons.</value>
        [JsonIgnore]
        public IEnumerable<Polygon> AllPolygons
        {
            get
            {
                yield return this;
                if (_innerPolygons is null) yield break;
                foreach (var polygon in _innerPolygons)
                    // yield return polygon;
                    //if we want to allow deep polygon trees, then the  code below would allow this (but would need to 
                    //comment the previous line ("yield return polygon;").
                    foreach (var innerPolygon in polygon.AllPolygons)
                        yield return innerPolygon;
            }
        }

        /// <summary>
        /// The index of this child in its parent's child list.
        /// </summary>
        /// <value>The index.</value>
        [JsonProperty]
        public int Index
        {
            get => index;
            set =>
                //if (index == value) return;
                //if (value < 0)
                //    throw new ArgumentException("The ID or Index of a polygon must be a non-negative integer.");
                index = value;
            //if (_vertices != null)
            //    foreach (var v in Vertices)
            //    {
            //        v.LoopID = index;
            //    }
        }

        /// <summary>
        /// Gets or sets whether the path is CCW positive. This will reverse the path if it was ordered CW.
        /// </summary>
        /// <value><c>true</c> if this instance is positive; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsPositive
        {
            get => Area >= 0;
        }


        /// <summary>
        /// This reverses the polygon, including updates to area and the point path.
        /// </summary>
        /// <param name="reverseInnerPolygons">if set to <c>true</c> [reverse inner polygons].</param>
        public void Reverse(bool reverseInnerPolygons = false)
        {
            if (_path == null) return;
            _path.Reverse();
            if (_vertices != null)
                _vertices.Reverse();
            Reset();
        }


        /// <summary>
        /// Gets the net area of the polygon - meaning any holes will be subtracted from the total area.
        /// </summary>
        /// <value>The area.</value>
        [JsonIgnore]
        public double Area
        {
            get
            {
                if (_path == null || _path.Count == 0)
                {
                    if (_innerPolygons.Count > 0) area = double.PositiveInfinity;
                    else area = 0.0;
                }
                else
                {
                    lock (_path)
                        if (double.IsNaN(area))
                            area = PathArea + InnerPolygons.Sum(p => p.Area);
                }
                return area;
            }
        }
        /// <summary>
        /// The area
        /// </summary>
        private double area = double.NaN;



        /// <summary>
        /// Gets the area of the top polygon. This area does not include the effect of inner polygons.
        /// </summary>
        /// <value>The path area.</value>
        [JsonIgnore]
        public double PathArea
        {
            get
            {
                if (_path == null || _path.Count == 0) pathArea = 0.0;
                else
                {
                    lock (_path)
                        if (double.IsNaN(pathArea))
                            pathArea = Path.Area();
                }
                return pathArea;
            }
        }
        /// <summary>
        /// The path area
        /// </summary>
        private double pathArea = double.NaN;



        /// <summary>
        /// Gets the area of the polygon. Negative Area for holes.
        /// </summary>
        /// <value>The perimeter.</value>
        [JsonIgnore]
        public double Perimeter
        {
            get
            {
                lock (_path)
                    if (double.IsNaN(perimeter))
                        perimeter = Path.Perimeter();
                return perimeter + InnerPolygons.Sum(p => p.Perimeter);
            }
        }

        /// <summary>
        /// The perimeter
        /// </summary>
        private double perimeter = double.NaN;

        /// <summary>
        /// Maxiumum X value
        /// </summary>
        /// <value>The maximum x.</value>
        [JsonIgnore]
        public double MaxX
        {
            get
            {
                if (double.IsNegativeInfinity(maxX))
                    SetBounds();
                return maxX;
            }
        }

        /// <summary>
        /// The maximum x
        /// </summary>
        private double maxX = double.NegativeInfinity;

        /// <summary>
        /// Miniumum X value
        /// </summary>
        /// <value>The minimum x.</value>
        [JsonIgnore]
        public double MinX
        {
            get
            {
                if (double.IsPositiveInfinity(minX))
                    SetBounds();
                return minX;
            }
        }

        /// <summary>
        /// The minimum x
        /// </summary>
        private double minX = double.PositiveInfinity;

        /// <summary>
        /// Maxiumum Y value
        /// </summary>
        /// <value>The maximum y.</value>
        [JsonIgnore]
        public double MaxY
        {
            get
            {
                if (double.IsNegativeInfinity(maxY))
                    SetBounds();
                return maxY;
            }
        }

        /// <summary>
        /// The maximum y
        /// </summary>
        private double maxY = double.NegativeInfinity;

        /// <summary>
        /// Gets the minimum y.
        /// </summary>
        /// <value>The minimum y.</value>
        [JsonIgnore]
        public double MinY
        {
            get
            {
                if (double.IsPositiveInfinity(minY))
                    SetBounds();
                return minY;
            }
        }

        /// <summary>
        /// Minimum Y value
        /// </summary>
        private double minY = double.PositiveInfinity;

        /// <summary>
        /// The index
        /// </summary>
        private int index = -1;


        public Vector2 Centroid
        {
            get
            {
                if (_centroid.IsNull() && Vertices != null && Vertices.Count > 0)
                    CalculateCentroid();
                return _centroid;
            }
        }
        private Vector2 _centroid = Vector2.Null;

        private void CalculateCentroid()
        {
            var xCenter = 0.0;
            var yCenter = 0.0;
            foreach (var p in AllPolygons)
            {
                for (int i = 0, j = Vertices.Count - 1; i < Vertices.Count; j = i++)
                {
                    var pj = Vertices[j];
                    var pi = Vertices[i];
                    var a = pj.X * pi.Y - pi.X * pj.Y;
                    xCenter += (pj.X + pi.X) * a;
                    yCenter += (pj.Y + pi.Y) * a;
                }
            }
            _centroid = new Vector2(xCenter, yCenter) / (6 * Area);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="index">The index.</param>


        private void MakeVerticesFromPath()
        {
            _vertices = new List<Vertex2D>();
            var prevX = Math.Round(_path[^1].X, NumSigDigits);
            var prevY = Math.Round(_path[^1].Y, NumSigDigits);

            for (int i = 0; i < _path.Count; i++)
            {
                var x = Math.Round(_path[i].X, NumSigDigits);
                var y = Math.Round(_path[i].Y, NumSigDigits);
                if (x != prevX || y != prevY)
                {
                    var coord = new Vector2(x, y);
                    _path[i] = coord;
                    _vertices.Add(new Vertex2D(coord, i, Index));
                    prevX = x;
                    prevY = y;
                }
                else
                {
                    _path.RemoveAt(i);
                    i--;
                }
            }
        }


        internal void RecreateVertices(bool topOnly = true)
        {
            var index = 0;
            // first, remove any vertices from the front of the list, by simply finding a value of 'index'
            // to properly start from
            while (_vertices[index].EndLine == null || _vertices[index].StartLine == null)
            {
                index++;
                // if you end up going through all the vertices then the polygon is nil
                if (_vertices.Count == index)
                {
                    _vertices.Clear();
                    Reset();
                    return;
                }
            }
            var firstVertex = _vertices[index];
            // now that the first vertex is found, walk from here to create the list
            var current = firstVertex;
            _vertices.Clear();
            index = 0;
            do
            {
                current.IndexInList = index++;
                current.LoopID = this.Index;
                _vertices.Add(current);
                current = current.StartLine.ToPoint;
            } while (current != firstVertex);
            _path = _vertices.Select(v => v.Coordinates).ToList();
            Reset();
            if (!topOnly)
            {
                foreach (var innerP in InnerPolygons)
                    innerP.RecreateVertices();
            }
        }


        /// <summary>
        /// Determines whether this instance is convex.
        /// </summary>
        /// <returns><c>true</c> if this instance is convex; otherwise, <c>false</c>.</returns>
        public bool IsConvex()
        {
            if (Area < 0) return false; //It must have an area greater than zero
            var firstLine = Edges.Last();
            foreach (var secondLine in Edges)
            {
                var cross = firstLine.Vector.Cross(secondLine.Vector);
                if (secondLine.Length.IsNegligible(Constants.PolygonSameTolerance)) continue; // without updating the first line             
                if (cross < 0)
                    return false;
                firstLine = secondLine;
            }
            return true;
        }

        /// <summary>
        /// Sets the bounds.
        /// </summary>
        private void SetBounds()
        {
            if (_path != null && _path.Count > 0)
            {
                foreach (var point in _path)
                {
                    if (point.X > maxX) maxX = point.X;
                    if (point.X < minX) minX = point.X;
                    if (point.Y > maxY) maxY = point.Y;
                    if (point.Y < minY) minY = point.Y;
                }
                var tolerance = (maxX - minX + maxY - minY) * Constants.PolygonSameTolerance / 2;
                _numSigDigits = 0;
                while (tolerance < 1 && _numSigDigits < 15)
                {
                    _numSigDigits++;
                    tolerance *= 10;
                }
            }
            else if (_innerPolygons.Count > 0 && !_innerPolygons[0].IsPositive)
            {
                maxX = double.PositiveInfinity;
                minX = double.NegativeInfinity;
                maxY = double.PositiveInfinity;
                minY = double.NegativeInfinity;
                _numSigDigits = 0;
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
                foreach (var v in polygon.Vertices)
                {
                    v.Transform(transformMatrix);
                    if (minX > v.X) minX = v.X;
                    if (minY > v.Y) minY = v.Y;
                    if (maxX < v.X) maxX = v.X;
                    if (maxY < v.Y) maxY = v.Y;
                }
                polygon.Reset();
            }
        }

        public void Reset()
        {
            _orderedXVertices = null;
            area = double.NaN;
            pathArea = double.NaN;
            perimeter = double.NaN;
            _centroid = Vector2.Null;
        }

        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;

        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("Coordinates", JToken.FromObject(Path.ConvertTo1DDoublesCollection()));
        }

        [OnDeserialized]
        protected void OnDeserializedMethod(StreamingContext context)
        {
            JArray jArray = (JArray)serializationData["Coordinates"];
            _path = PolygonOperations.ConvertToVector2s(jArray.ToObject<IEnumerable<double>>()).ToList();
        }
    }

    internal class VertexSortedByXFirst : IComparer<Vertex2D>
    {

        public int Compare(Vertex2D v1, Vertex2D v2)
        {
            if (v1.X.IsPracticallySame(v2.X))
                return (v1.Y < v2.Y) ? -1 : 1;
            return (v1.X < v2.X) ? -1 : 1;
        }
    }

    internal class VertexSortedByYFirst : IComparer<Vertex2D>
    {

        public int Compare(Vertex2D v1, Vertex2D v2)
        {
            if (v1.Y.IsPracticallySame(v2.Y))
                return (v1.X < v2.X) ? -1 : 1;
            return (v1.Y < v2.Y) ? -1 : 1;
        }
    }

    internal class VertexSortedByDirection : IComparer<Vertex2D>
    {
        private readonly Vector2 sweepDirection;
        private readonly Vector2 alongDirection;

        internal VertexSortedByDirection(Vector2 sweepDirection)
        {
            this.sweepDirection = sweepDirection;
            alongDirection = new Vector2(-sweepDirection.Y, sweepDirection.X);

        }
        public int Compare(Vertex2D v1, Vertex2D v2)
        {
            var d1 = v1.Coordinates.Dot(sweepDirection);
            var d2 = v2.Coordinates.Dot(sweepDirection);
            if (d1.IsPracticallySame(d2))
                return (v1.Coordinates.Dot(alongDirection) < v2.Coordinates.Dot(alongDirection)) ? -1 : 1;
            return (d1 < d2) ? -1 : 1;
        }
    }
}


