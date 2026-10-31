using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>An indexed line segment with optional source-entity provenance.</summary>
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class WireframeEdge
    {
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string EntityId { get; set; }
        public string NativeType { get; set; }
        public string Layer { get; set; }
        public string Role { get; set; }
        public bool IsRoleAuthoritative { get; set; }

        public WireframeEdge() { }

        public WireframeEdge(int startIndex, int endIndex, string entityId = "",
            string nativeType = "", string layer = "", string role = "",
            bool isRoleAuthoritative = false)
        {
            StartIndex = startIndex;
            EndIndex = endIndex;
            EntityId = entityId ?? string.Empty;
            NativeType = nativeType ?? string.Empty;
            Layer = layer ?? string.Empty;
            Role = role ?? string.Empty;
            IsRoleAuthoritative = isRoleAuthoritative;
        }
    }

    /// <summary>
    /// A source-preserving collection of three-dimensional vertices and edges. Unlike a
    /// <see cref="TessellatedSolid"/>, a wireframe does not imply a closed volume or surface.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public sealed class Wireframe : Solid
    {
        public List<Vector3> Vertices { get; set; } = [];
        public List<WireframeEdge> Edges { get; set; } = [];
        public Dictionary<string, string> Metadata { get; set; } = new(StringComparer.Ordinal);

        public Wireframe() : base() { }

        public Wireframe(IEnumerable<Vector3> vertices, IEnumerable<WireframeEdge> edges,
            UnitType units = UnitType.unspecified, string name = "", string filename = "",
            IDictionary<string, string> metadata = null)
            : base(units, name, filename)
        {
            Vertices = vertices?.ToList() ?? throw new ArgumentNullException(nameof(vertices));
            Edges = edges?.ToList() ?? throw new ArgumentNullException(nameof(edges));
            if (metadata != null) Metadata = new Dictionary<string, string>(metadata, StringComparer.Ordinal);
            Validate();
            CalculateBounds();
        }

        private Wireframe(Wireframe source) : base(source)
        {
            Vertices = source.Vertices.ToList();
            Edges = source.Edges.Select(edge => new WireframeEdge(
                edge.StartIndex, edge.EndIndex, edge.EntityId, edge.NativeType,
                edge.Layer, edge.Role, edge.IsRoleAuthoritative)).ToList();
            Metadata = new Dictionary<string, string>(source.Metadata, StringComparer.Ordinal);
            CalculateBounds();
        }

        public void Validate()
        {
            for (var index = 0; index < Edges.Count; index++)
            {
                var edge = Edges[index];
                if (edge.StartIndex < 0 || edge.StartIndex >= Vertices.Count ||
                    edge.EndIndex < 0 || edge.EndIndex >= Vertices.Count)
                    throw new ArgumentOutOfRangeException(nameof(Edges),
                        $"Wireframe edge {index} refers to a vertex outside the vertex collection.");
            }
        }

        public override void Transform(Matrix4x4 transformMatrix)
        {
            for (var index = 0; index < Vertices.Count; index++)
                Vertices[index] = Vertices[index].Transform(transformMatrix);
            ResetDerivedValues();
            CalculateBounds();
        }

        public override Solid TransformToNewSolid(Matrix4x4 transformationMatrix)
        {
            var copy = new Wireframe(this);
            copy.Transform(transformationMatrix);
            return copy;
        }

        protected override void CalculateCenter()
        {
            if (Vertices.Count == 0) { _center = Vector3.Null; return; }
            _center = new Vector3(
                Vertices.Average(vertex => vertex.X),
                Vertices.Average(vertex => vertex.Y),
                Vertices.Average(vertex => vertex.Z));
        }

        protected override void CalculateVolume() => _volume = 0;
        protected override void CalculateSurfaceArea() => _surfaceArea = 0;
        protected override void CalculateInertiaTensor() => _inertiaTensor = new Matrix3x3();

        private void CalculateBounds()
        {
            if (Vertices.Count == 0)
            {
                Bounds = [Vector3.Zero, Vector3.Zero];
                return;
            }
            Bounds =
            [
                new Vector3(Vertices.Min(vertex => vertex.X), Vertices.Min(vertex => vertex.Y),
                    Vertices.Min(vertex => vertex.Z)),
                new Vector3(Vertices.Max(vertex => vertex.X), Vertices.Max(vertex => vertex.Y),
                    Vertices.Max(vertex => vertex.Z)),
            ];
        }

        private void ResetDerivedValues()
        {
            _center = Vector3.Null;
            _volume = double.NaN;
            _surfaceArea = double.NaN;
            _inertiaTensor = Matrix3x3.Null;
        }
    }
}
