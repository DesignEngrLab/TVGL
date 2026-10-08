// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="Polygon3D.cs" company="Design Engineering Lab">
//     2026
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Represents a 3D polygon, consisting of vertices and optional holes.
    /// </summary>
    public class Polygon3D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon3D"/> class.
        /// </summary>
        public Polygon3D() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon3D"/> class with vertices, a closed flag, and an index.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="isClosed">if set to <c>true</c>, the polygon is closed.</param>
        /// <param name="index">The index.</param>
        public Polygon3D(IEnumerable<Vector3> vertices, bool isClosed, int index = -1)
        {
            Vertices = vertices as IList<Vector3> ?? vertices.ToList();
            Index = index;
            IsClosed = isClosed;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon3D"/> class with vertices, a closed flag, and holes.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="isClosed">if set to <c>true</c>, the polygon is closed.</param>
        /// <param name="holes">The holes in the polygon.</param>
        public Polygon3D(IEnumerable<Vector3> vertices, bool isClosed, IEnumerable<IEnumerable<Vector3>> holes)
            : this(vertices, isClosed)
        {
            if (holes is ICollection<IEnumerable<Vector3>> collection)
            {
                var numHoles = collection.Count;
                Holes = new IList<Vector3>[numHoles];
                var i = 0;
                foreach (var hole in holes)
                    Holes[i++] = hole as IList<Vector3> ?? hole.ToArray();
            }
            else
            {
                Holes = new List<IList<Vector3>>();
                foreach (var hole in holes)
                    Holes.Add(hole as IList<Vector3> ?? hole.ToArray());
            }
        }

        /// <summary>
        /// Gets the primary outer vertices of the polygon.
        /// </summary>
        [JsonIgnore]
        public IList<Vector3> Vertices { get; set; }

        /// <summary>
        /// Gets or sets the holes in the polygon.
        /// </summary>
        [JsonIgnore]
        public IList<IList<Vector3>> Holes { get; set; }

        [JsonProperty("Coordinates")]
        [JsonConverter(typeof(DoubleSequenceJsonConverter))]
        private IEnumerable<double> SerializedCoordinates
        {
            get => Vertices?.ConvertTo1DDoublesCollection();
            set => Vertices = ConvertToVector3s(value);
        }

        [JsonProperty("HoleCoordinates")]
        [JsonConverter(typeof(DoubleSequenceCollectionJsonConverter))]
        private IList<IList<double>> SerializedHoleCoordinates
        {
            get => Holes?.Select(hole => hole.ConvertTo1DDoublesCollection().ToList()).Cast<IList<double>>().ToList();
            set => Holes = value?.Select(ConvertToVector3s).Cast<IList<Vector3>>().ToList();
        }

        [JsonProperty("Vertices")]
        private IList<Vector3> LegacyVertices
        {
            set
            {
                if (Vertices == null || Vertices.Count == 0)
                    Vertices = value;
            }
        }

        [JsonProperty("Holes")]
        private IList<IList<Vector3>> LegacyHoles
        {
            set
            {
                if (Holes == null || Holes.Count == 0)
                    Holes = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Polygon3D"/> is closed.
        /// </summary>
        public bool IsClosed { get; init; }

        /// <summary>
        /// Iterates over all paths starting with the outer perimeter and 
        /// then any holes.
        /// </summary>
        /// <returns>An enumeration of all paths.</returns>
        public IEnumerable<IList<Vector3>> AllPaths()
        {
            yield return Vertices;
            if (Holes != null)
                foreach (var hole in Holes)
                    yield return hole;
        }

        /// <summary>
        /// Gets the booleans for all the paths in the 3D polygon 
        /// starting with the outer one. All holes are considerd closed anyway, 
        /// so the output is always [t/f t t t ...]
        /// </summary>
        /// <returns>An enumeration of boolean closed states.</returns>
        public IEnumerable<bool> AllIsClosed()
        {
            yield return IsClosed;
            if (Holes != null)
                foreach (var hole in Holes)
                    yield return true;
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="Polygon3D"/>.
        /// </summary>
        /// <returns>A new <see cref="Polygon3D"/> object.</returns>
        public Polygon3D Copy()
        {
            var copiedHoles = new Vector3[Holes.Count][];
            for (int i = 0; i < Holes.Count; i++)
                copiedHoles[i] = Holes[i].ToArray();
            return new Polygon3D(Vertices.ToArray(), IsClosed, copiedHoles);
        }

        /// <summary>
        /// Gets the total number of paths (1 for the outer perimeter + number of holes).
        /// </summary>
        public int NumPaths => 1 + (Holes?.Count ?? 0);

        /// <summary>
        /// Gets the total number of points across all paths.
        /// </summary>
        public int NumPoints => AllPaths().Sum(path => path.Count);

        /// <summary>
        /// Gets or sets the index of the polygon.
        /// </summary>
        public int Index { get;  set; }

        private static List<Vector3> ConvertToVector3s(IEnumerable<double> coordinates)
        {
            var result = new List<Vector3>();
            if (coordinates == null) return result;

            var values = coordinates.ToList();
            for (int i = 0; i + 2 < values.Count; i += 3)
                result.Add(new Vector3(values[i], values[i + 1], values[i + 2]));

            return result;
        }

        private static IList<double> ReadDoubleSequence(JToken token)
        {
            if (token.Type == JTokenType.Null) return null;

            var values = token as JArray;
            if (values == null && token is JObject wrapper)
                values = wrapper["$values"] as JArray;
            if (values == null)
                throw new JsonSerializationException("Polygon3D coordinates must be a JSON array.");

            return values.Values<double>().ToList();
        }

        private sealed class DoubleSequenceJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                typeof(IEnumerable<double>).IsAssignableFrom(objectType);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteStartArray();
                foreach (var coordinate in (IEnumerable<double>)value)
                    writer.WriteValue(coordinate);
                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer) => ReadDoubleSequence(JToken.Load(reader));
        }

        private sealed class DoubleSequenceCollectionJsonConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) =>
                typeof(IEnumerable<IEnumerable<double>>).IsAssignableFrom(objectType);

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value == null)
                {
                    writer.WriteNull();
                    return;
                }

                writer.WriteStartArray();
                foreach (var sequence in (IEnumerable<IEnumerable<double>>)value)
                {
                    writer.WriteStartArray();
                    foreach (var coordinate in sequence)
                        writer.WriteValue(coordinate);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
                JsonSerializer serializer)
            {
                var token = JToken.Load(reader);
                if (token.Type == JTokenType.Null) return null;

                var sequences = token as JArray;
                if (sequences == null && token is JObject wrapper)
                    sequences = wrapper["$values"] as JArray;
                if (sequences == null)
                    throw new JsonSerializationException("Polygon3D hole coordinates must be a JSON array.");

                return sequences.Select(ReadDoubleSequence).ToList();
            }
        }
    }
}
