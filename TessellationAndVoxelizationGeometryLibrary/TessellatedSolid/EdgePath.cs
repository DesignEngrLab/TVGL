using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace TVGL
{
    [JsonObject]
    public class EdgePath : IList<(Edge edge, bool dir)>
    {
        public EdgePath()
        {
            EdgeList = new List<Edge>();
            DirectionList = new List<bool>();
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
        /// Gets or sets a value indicating whether [border is closed].
        /// </summary>
        /// <value><c>true</c> if [border is closed]; otherwise, <c>false</c>.</value>

        public bool IsClosed { get; set; }

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
                if (DirectionList[0]) return EdgeList[0].From;
                else return EdgeList[0].To;
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

        [JsonIgnore]
        public int Count => EdgeList.Count;

        [JsonIgnore]
        public bool IsReadOnly => true;


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
            if (!IsClosed) //only add the last one if not a closed loop since it would otherwise
                           // repeat the first point
            {
                if (DirectionList[^1]) yield return EdgeList[^1].To;
                else yield return EdgeList[^1].From;
            }
        }

        public void AddEnd(Edge edge, bool dir)
        {
            EdgeList.Add(edge);
            DirectionList.Add(dir);
        }
        public void AddEnd(Edge edge)
        {
            if (LastVertex == null) DirectionList.Add(true);
            else DirectionList.Add(edge.From == LastVertex);
            EdgeList.Add(edge);
        }
        public void AddBegin(Edge edge, bool dir)
        {
            EdgeList.Insert(0, edge);
            DirectionList.Insert(0, dir);
        }

        public void AddBegin(Edge edge)
        {
            if (LastVertex == null) DirectionList.Add(true);
            DirectionList.Insert(0, edge.To == FirstVertex);
            EdgeList.Insert(0, edge);
        }


        public int IndexOf((Edge edge, bool dir) item)
        {
            var i = EdgeList.IndexOf(item.edge);
            if (DirectionList[i] != item.dir) return -1;
            return i;
        }
        public int IndexOf(Edge edge)
        {
            return EdgeList.IndexOf(edge);
        }
        [JsonIgnore]
        public double Length => EdgeList.Sum(edge => edge.Length);

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

        public void Clear()
        {
            EdgeList.Clear();
            DirectionList.Clear();
        }

        public bool Contains((Edge edge, bool dir) item)
        {
            return IndexOf(item) != -1;
        }

        internal bool Contains(Edge edge)
        {
            return EdgeList.Contains(edge);
        }

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
        /// Copies the data (properties) from this EdgePath over to another.
        /// </summary>
        /// <param name="copy">The copy.</param>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        public void CopyEdgesPathData(EdgePath copy, bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
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

        [JsonExtensionData]
        protected IDictionary<string, JToken> serializationData;

        [OnSerializing]
        protected void OnSerializingMethod(StreamingContext context)
        {
            serializationData = new Dictionary<string, JToken>();
            serializationData.Add("EdgeIndices", JToken.FromObject(EdgeList.Select(e => e.IndexInList)));
            serializationData.Add("Dirs", string.Join(null, DirectionList.Select(dir => dir ? "1" : "0")));
        }

        internal void CompletePostSerialization(TessellatedSolid ts)
        {
            foreach (var edgeIndex in serializationData["EdgeIndices"].ToObject<IEnumerable<int>>())
                EdgeList.Add(ts.Edges[edgeIndex]);
            foreach (var s in serializationData["Dirs"].ToObject<string>())
                DirectionList.Add(s == '1');
        }

        /// <summary>Gets the range.</summary>
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
        public IEnumerator<(Edge edge, bool dir)> GetEnumerator()
        {
            for (int i = 0; i < EdgeList.Count; i++)
            {
                yield return (EdgeList[i], DirectionList[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
