// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="EdgePath.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TVGL
{
    /// <summary>
    /// Class EdgePath.
    /// </summary>
    [JsonObject]
    public class EdgePath : IList<(Edge edge, bool dir)>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EdgePath"/> class.
        /// </summary>
        public EdgePath()
        {
            EdgeList = new List<Edge>();
            DirectionList = new List<bool>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EdgePath"/> class.
        /// </summary>
        /// <param name="edge">The edge.</param>
        public EdgePath(Edge edge) : this()
        {
            AddBegin(edge, true);
        }

        /// <summary>
        /// Gets the edges and direction.
        /// </summary>
        /// <value>The edges and direction.</value>
        [JsonIgnore]
        public List<Edge> EdgeList { get; protected set; }

        /// <summary>
        /// Gets the edges and direction.
        /// </summary>
        /// <value>The edges and direction.</value>
        [JsonIgnore]
        public List<bool> DirectionList { get; protected set; }

        /// <summary>
        /// The length
        /// </summary>
        private double _length = -1.0;
        /// <summary>
        /// Gets the length.
        /// </summary>
        /// <value>The length.</value>
        [JsonIgnore]
        public double Length
        {
            get
            {
                if (_length.Equals(-1))
                    _length = EdgeList.Sum(e => e.Length);
                return _length;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [border is closed].
        /// </summary>
        /// <value><c>true</c> if [border is closed]; otherwise, <c>false</c>.</value>

        public bool IsClosed { get; set; }

        /// <summary>
        /// Updates the is closed.
        /// </summary>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool UpdateIsClosed()
        {
            if (EdgeList == null || EdgeList.Count < 3)
                IsClosed = false;
            else
            {
                var lastVertex = DirectionList[^1] ? EdgeList[^1].To : EdgeList[^1].From;
                IsClosed = (FirstVertex == lastVertex);
            }
            return IsClosed;
        }

        /// <summary>
        /// Gets the number points.
        /// </summary>
        /// <value>The number points.</value>
        [JsonIgnore]
        public int NumPoints
        {
            get
            {
                if (IsClosed) return EdgeList.Count;
                return EdgeList.Count + 1;
            }
        }

        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        /// <value>The first vertex.</value>
        [JsonIgnore]
        public Vertex FirstVertex
        {
            get
            {
                if (DirectionList == null || DirectionList.Count == 0) return null;
                return DirectionList[0] ? EdgeList[0].From : EdgeList[0].To;
            }
        }

        /// <summary>
        /// Gets the last vertex.
        /// </summary>
        /// <value>The last vertex.</value>
        [JsonIgnore]
        public Vertex LastVertex
        {
            get
            {
                if (DirectionList == null || DirectionList.Count == 0) return null;
                // the following condition uses the edge direction of course, but it also checks
                // to see if it is closed because - if it is closed then the last and the first would
                // be repeated. To prevent this, we quickly check that if direction is true use To
                // unless it's closed, then use From (True-False), go through the four cases in your mind
                // and you see that this checks out.
                if (DirectionList[^1] != IsClosed) return EdgeList[^1].To;
                else return EdgeList[^1].From;
            }
        }

        /// <summary>
        /// Reports if the given border the encircles the given axis and anchor.
        /// </summary>
        /// <param name="border">The border.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>A bool.</returns>
        public bool EncirclesAxis(Vector3 axis, Vector3 anchor)
        {
            return GetVectors().BorderEncirclesAxis(axis, anchor);
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1" />.
        /// </summary>
        /// <value>The count.</value>
        [JsonIgnore]
        public int Count => EdgeList.Count;

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1" /> is read-only.
        /// </summary>
        /// <value><c>true</c> if this instance is read only; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsReadOnly => true;

        /// <summary>
        /// Gets or sets the <see cref="System.ValueTuple{Edge, System.Boolean}"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>System.ValueTuple&lt;Edge, System.Boolean&gt;.</returns>
        [JsonIgnore]
        public (Edge edge, bool dir) this[int index]
        {
            get => (EdgeList[index], DirectionList[index]);
            set
            {
                EdgeList[index] = value.edge;
                DirectionList[index] = value.dir;
            }
        }

        /// <summary>
        /// Gets the vertices.
        /// </summary>
        /// <returns>IEnumerable&lt;Vertex&gt;.</returns>
        public IEnumerable<Vertex> GetVertices()
        {
            if (EdgeList.Count == 0) yield break;
            for (int i = 0; i < EdgeList.Count; i++)
            {
                if (DirectionList[i]) yield return EdgeList[i].From;
                else yield return EdgeList[i].To;
            }
            //only add the last one if not a closed loop since it would otherwise repeat the first point
            if (!IsClosed) 
            {
                if (DirectionList[^1]) yield return EdgeList[^1].To;
                else yield return EdgeList[^1].From;
            }
        }

        /// <summary>
        /// Gets the vectors.
        /// </summary>
        /// <returns>IEnumerable&lt;Vector3&gt;.</returns>
        public IEnumerable<Vector3> GetVectors()
        {
            foreach(var vertex in GetVertices())
                yield return vertex.Coordinates;
        }

        /// <summary>
        /// Gets the centers.
        /// </summary>
        /// <returns>IEnumerable&lt;Vector3&gt;.</returns>
        public IEnumerable<Vector3> GetCenters()
        {
            foreach (var edge in EdgeList)
                yield return edge.Center();
        }

        /// <summary>
        /// Adds the end.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="dir">if set to <c>true</c> [dir].</param>
        public void AddEnd(Edge edge, bool dir)
        {
            EdgeList.Add(edge);
            DirectionList.Add(dir);
        }
        /// <summary>
        /// Adds the end.
        /// </summary>
        /// <param name="edge">The edge.</param>
        public void AddEnd(Edge edge)
        {
            // lastVertex is a local variable that breaks the rule of the LastVertex property. It should not
            // depend on IsClosed
            var lastVertex = !DirectionList.Any() ? null : DirectionList[^1] ? EdgeList[^1].To : EdgeList[^1].From;

            if (lastVertex == null) DirectionList.Add(true);
            else DirectionList.Add(edge.From == lastVertex);
            EdgeList.Add(edge);
        }
        /// <summary>
        /// Adds the begin.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <param name="dir">if set to <c>true</c> [dir].</param>
        public void AddBegin(Edge edge, bool dir)
        {
            EdgeList.Insert(0, edge);
            DirectionList.Insert(0, dir);
        }

        /// <summary>
        /// Adds the begin.
        /// </summary>
        /// <param name="edge">The edge.</param>
        public void AddBegin(Edge edge)
        {
            if (LastVertex == null) DirectionList.Add(true);
            DirectionList.Insert(0, edge.To == FirstVertex);
            EdgeList.Insert(0, edge);
        }


        internal void AddRange(EdgePath ep2)
        {
            if (FirstVertex == ep2.FirstVertex)
                foreach (var (edge, dir) in ep2)
                    AddBegin(edge, !dir);
            else if (FirstVertex == ep2.LastVertex)
                foreach (var (edge, dir) in ep2.Reverse())
                    AddBegin(edge, dir);
            else if (LastVertex == ep2.FirstVertex)
                foreach (var (edge, dir) in ep2)
                    AddEnd(edge, dir);
            else if (LastVertex == ep2.LastVertex)
                foreach (var (edge, dir) in ep2.Reverse())
                    AddEnd(edge, !dir);
            else throw new ArgumentException("The two edge paths do not share a common vertex");
        }
        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1" />.
        /// </summary>
        /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <returns>The index of <paramref name="item" /> if found in the list; otherwise, -1.</returns>
        public int IndexOf((Edge edge, bool dir) item)
        {
            var i = EdgeList.IndexOf(item.edge);
            if (DirectionList[i] != item.dir) return -1;
            return i;
        }
        /// <summary>
        /// Indexes the of.
        /// </summary>
        /// <param name="edge">The edge.</param>
        /// <returns>System.Int32.</returns>
        public int IndexOf(Edge edge)
        {
            return EdgeList.IndexOf(edge);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item" /> should be inserted.</param>
        /// <param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1" />.</param>
        /// <exception cref="System.NotSupportedException">Inserting into arbitrary positions in not allowed. Use either AddBegin or AddEnd</exception>
        public void Insert(int index, (Edge edge, bool dir) item)
        {
            throw new NotSupportedException("Inserting into arbitrary positions in not allowed. Use either AddBegin or AddEnd");
            //EdgeList.Insert(index, item.edge);
            //DirectionList.Insert(index, item.dir);
        }

    /// <summary>
    /// Removes the <see cref="T:System.Collections.Generic.IList`1" /> item at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the item to remove.</param>
    public void RemoveAt(int index)
    {
        EdgeList.RemoveAt(index);
        DirectionList.RemoveAt(index);
    }

    /// <summary>
    /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
    /// <exception cref="System.NotSupportedException">Add is ambiguous. Use either AddBegin or AddEnd</exception>
    public void Add((Edge edge, bool dir) item)
    {
        throw new NotSupportedException("Add is ambiguous. Use either AddBegin or AddEnd");
        //EdgeList.Add(item.edge);
        //DirectionList.Add(item.dir);
    }

    /// <summary>
    /// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1" />.
    /// </summary>
    public void Clear()
    {
        EdgeList.Clear();
        DirectionList.Clear();
    }

    /// <summary>
    /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1" /> contains a specific value.
    /// </summary>
    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
    /// <returns><see langword="true" /> if <paramref name="item" /> is found in the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />.</returns>
    public bool Contains((Edge edge, bool dir) item)
    {
        return IndexOf(item) != -1;
    }

    /// <summary>
    /// Determines whether this instance contains the object.
    /// </summary>
    /// <param name="edge">The edge.</param>
    /// <returns><c>true</c> if [contains] [the specified edge]; otherwise, <c>false</c>.</returns>
    internal bool Contains(Edge edge)
    {
        return EdgeList.Contains(edge);
    }

    /// <summary>
    /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
    /// </summary>
    /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1" />. The <see cref="T:System.Array" /> must have zero-based indexing.</param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array" /> at which copying begins.</param>
    public void CopyTo((Edge edge, bool dir)[] array, int arrayIndex)
    {
        for (int i = arrayIndex; i < array.Length; i++)
            array[i] = (EdgeList[i], DirectionList[i]);
    }

    /// <summary>
    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1" />.
    /// </summary>
    /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
    /// <returns><see langword="true" /> if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, <see langword="false" />. This method also returns <see langword="false" /> if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
    public bool Remove((Edge edge, bool dir) item)
    {
        var i = IndexOf(item);
        if (i == -1) return false;
        RemoveAt(i);
        return true;
    }

    /// <summary>
    /// Copies the specified EdgePath.
    /// </summary>
    /// <param name="reverse">if set to <c>true</c> [reverse].</param>
    /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    /// <returns>EdgePath.</returns>
    public EdgePath Copy(bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
        int startIndex = 0, int endIndex = -1)
    {
        var copy = new EdgePath();
        this.CopyEdgesPathData(copy, reverse, copiedTessellatedSolid, startIndex, endIndex);
        return copy;
    }
    /// <summary>
    /// Copies the data (properties) from this EdgePath over to another.
    /// </summary>
    /// <param name="copy">The copy.</param>
    /// <param name="reverse">if set to <c>true</c> [reverse].</param>
    /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
    /// <param name="startIndex">The start index.</param>
    /// <param name="endIndex">The end index.</param>
    protected void CopyEdgesPathData(EdgePath copy, bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
        int startIndex = 0, int endIndex = -1)
    {
        copy.IsClosed = this.IsClosed && startIndex == 0 && (endIndex == -1 || endIndex >= EdgeList.Count);
        if (endIndex == -1) endIndex = EdgeList.Count;
        if (copiedTessellatedSolid == null)
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                if (reverse)
                    copy.AddBegin(EdgeList[i], !DirectionList[i]);
                else
                    copy.AddEnd(EdgeList[i], DirectionList[i]);
            }
        }
        else
        {
            for (int i = startIndex; i < endIndex; i++)
            {
                if (reverse)
                    copy.AddBegin(copiedTessellatedSolid.Edges[EdgeList[i].IndexInList], !DirectionList[i]);
                else
                    copy.AddEnd(copiedTessellatedSolid.Edges[EdgeList[i].IndexInList], DirectionList[i]);
            }
        }
    }

    /// <summary>
    /// The serialization data
    /// </summary>
    [JsonExtensionData]
    protected IDictionary<string, JToken> serializationData;

    /// <summary>
    /// Called when [serializing method].
    /// </summary>
    /// <param name="context">The context.</param>
    [OnSerializing]
    protected void OnSerializingMethod(StreamingContext context)
    {
        serializationData = new Dictionary<string, JToken>();
        serializationData.Add("EdgeIndices", JToken.FromObject(EdgeList.Select(e => e.IndexInList)));
        serializationData.Add("Dirs", string.Join(null, DirectionList.Select(dir => dir ? "1" : "0")));
    }

    /// <summary>
    /// Completes the post serialization.
    /// </summary>
    /// <param name="ts">The ts.</param>
    internal void CompletePostSerialization(TessellatedSolid ts)
    {
        foreach (var edgeIndex in serializationData["EdgeIndices"].ToObject<IEnumerable<int>>())
            EdgeList.Add(ts.Edges[edgeIndex]);
        foreach (var s in serializationData["Dirs"].ToObject<string>())
            DirectionList.Add(s == '1');
    }

    /// <summary>
    /// Gets the range.
    /// </summary>
    /// <param name="lb">The lb.</param>
    /// <param name="ub">The ub.</param>
    /// <returns>IEnumerable&lt;System.ValueTuple&lt;Edge, System.Boolean&gt;&gt;.</returns>
    internal IEnumerable<(Edge edge, bool dir)> GetRange(int lb, int ub)
    {
        for (int i = lb; i < ub; i++)
            yield return (EdgeList[i], DirectionList[i]);
    }
    /// <summary>
    /// Removes the range.
    /// </summary>
    /// <param name="lb">The lb.</param>
    /// <param name="ub">The ub.</param>
    internal void RemoveRange(int lb, int ub)
    {
        var numberToRemove = ub - lb;
        EdgeList.RemoveRange(lb, numberToRemove);
        DirectionList.RemoveRange(lb, numberToRemove);
    }
    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<(Edge edge, bool dir)> GetEnumerator()
    {
        for (int i = 0; i < EdgeList.Count; i++)
        {
            yield return (EdgeList[i], DirectionList[i]);
        }
    }

    /// <summary>
    /// Gets the enumerator.
    /// </summary>
    /// <returns>IEnumerator.</returns>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
}
