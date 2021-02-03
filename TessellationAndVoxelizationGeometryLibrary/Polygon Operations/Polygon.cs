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
        /// <summary>
        /// The list of 2D points that make up a polygon.
        /// </summary>
        /// <value>The path.</value>
        [JsonIgnore]
        public List<Vector2> Path
        {
            get
            {
                lock (_vertices)
                {
                    if (_path == null)
                    {
                        _path = new List<Vector2>();
                        foreach (var point in _vertices)
                        {
                            _path.Add(new Vector2(point.X, point.Y));
                        }
                    }
                }
                return _path;
            }
            internal set { _path = value; }
        }

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
        public List<Vertex2D> Vertices => _vertices;
        /// <summary>
        /// The vertices
        /// </summary>
        List<Vertex2D> _vertices;

        /// <summary>
        /// Gets the ordered x vertices.
        /// </summary>
        /// <value>The ordered x vertices.</value>
        [JsonIgnore]
        internal Vertex2D[] OrderedXVertices
        {
            get
            {
                lock (_vertices)
                    if (_orderedXVertices == null || _orderedXVertices.Length != Vertices.Count)
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

        public void MakePolygonEdgesIfNonExistent()
        {
            lock (_vertices)
            {
                if (_edges != null && _edges.Length == Vertices.Count) return;
                foreach (var poly in AllPolygons)
                    poly.MakeThisPolygonsEdges();
            }
        }

        private void MakeThisPolygonsEdges()
        {
            var numPoints = Vertices.Count;
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
        /// Makes the vertices.
        /// </summary>
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
        public bool AddInnerPolygon(Polygon polygon)
        {
            if (polygon is null || (polygon._path is null && polygon._vertices is null)) return false;
            //if (this.IsNonIntersectingPolygonInside(polygon, false, out _) == false) return false;
            //if (polygon.IsPositive) polygon.Reverse();
            _innerPolygons ??= new List<Polygon>();
            //for (int i = _holes.Count - 1; i >= 0; i--)
            //{
            //    if (polygon.IsNonIntersectingPolygonInside(_holes[i], true, out _) == true)
            //        _holes.RemoveAt(i);
            //}
            // this text was removed from the method description since this code was commented out above
            // This method assumes that there are no intersections between the hole polygon and the host polygon. However, 
            // it does check and remove holes in the host that are fully inside of the  new hole.


            _innerPolygons.Add(polygon);
            perimeter = double.NaN;
            area = double.NaN;

            return true;
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
            get => PathArea > 0;
            set
            {
                if (value != (PathArea > 0))
                    Reverse();
            }
        }


        /// <summary>
        /// This reverses the polygon, including updates to area and the point path.
        /// </summary>
        /// <param name="reverseInnerPolygons">if set to <c>true</c> [reverse inner polygons].</param>
        public void Reverse(bool reverseInnerPolygons = false)
        {
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
                lock (_vertices)
                    if (double.IsNaN(area))
                        area = PathArea + InnerPolygons.Sum(p => p.Area);
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
                lock (_vertices)
                    if (double.IsNaN(pathArea))
                    pathArea = Path.Area();
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
                lock (_vertices)
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
                lock (_vertices)
                    if (double.IsInfinity(maxX))
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
                lock (_vertices)
                    if (double.IsInfinity(minX))
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
                lock (_vertices)
                    if (double.IsInfinity(maxY))
                    SetBounds();
                return maxY;
            }
        }

        /// <summary>
        /// The maximum y
        /// </summary>
        private double maxY = double.NegativeInfinity;

        /// <summary>
        /// Minimum Y value
        /// </summary>
        private double minY = double.PositiveInfinity;

        /// <summary>
        /// The index
        /// </summary>
        private int index = -1;

        /// <summary>
        /// Gets the minimum y.
        /// </summary>
        /// <value>The minimum y.</value>
        [JsonIgnore]
        public double MinY
        {
            get
            {
                lock (_vertices)
                    if (double.IsInfinity(minY))
                    SetBounds();
                return minY;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// Assumes path is closed and not self-intersecting.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        /// <param name="index">The index.</param>


        public Polygon(IEnumerable<Vector2> coordinates, int index = -1)
        {
            //_path = coordinates.ToList();
            _path = new List<Vector2>();
            Vector2 prevCoordinate = Vector2.Null;
            foreach (var p in coordinates)
            {
                if (p.IsPracticallySame(prevCoordinate)) continue;
                prevCoordinate = p;
                _path.Add(p);
            }
            if (_path.Count > 1 && _path[0].IsPracticallySame(_path[^1])) _path.RemoveAt(_path.Count - 1);
            Index = index;
            MakeVertices();
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
                _innerPolygons.Select(p => p.Copy(true, invert)).ToList() : null;
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
            copiedPolygon.MakeVertices();
            return copiedPolygon;
        }

        // the following argument-less constructor is only used in the copy function
        // and in deserialization
        /// <summary>
        /// Prevents a default instance of the <see cref="Polygon"/> class from being created.
        /// </summary>
        public Polygon()
        {
        }


        /// <summary>
        /// Determines whether this instance is convex.
        /// </summary>
        /// <returns><c>true</c> if this instance is convex; otherwise, <c>false</c>.</returns>
        public bool IsConvex()
        {
            var tolerance = this.GetToleranceForPolygon(); ;
            if (!Area.IsPositiveNonNegligible(tolerance)) return false; //It must have an area greater than zero
            var firstLine = Edges.Last();
            foreach (var secondLine in Edges)
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

        /// <summary>
        /// Sets the bounds.
        /// </summary>
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
            lock (_vertices)
            {
                _path = null;
                _edges = null;
                _orderedXVertices = null;
                area = double.NaN;
                pathArea = double.NaN;
                perimeter = double.NaN;
            }
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
            MakeVertices();
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
}


