using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// A SurfaceGroup is a group of adjacent primitive surfaces that have border loops.
    /// This class inherits from primitive surface to be able to use border loops.
    /// It does not make use of border segments.
    /// </summary>
    public class SurfaceGroup : PrimitiveSurface
    {
        [JsonIgnore]
        public HashSet<PrimitiveSurface> Surfaces { get; set; }

        public override string KeyString
        {
            get
            {
                var key = "Group|";
                foreach (var surface in Surfaces)
                {
                    key += surface.GetType().Name + "|";
                }
                key += GetCommonKeyDetails();
                return key;
            }
        }

        public void AddPrimitiveSurface(PrimitiveSurface surface, bool resetBorders = true)
        {
            if (surface is SurfaceGroup)
                throw new Exception("Use Combine");
            Surfaces.Add(surface);
            surface.BelongsToGroup = this;
            if (resetBorders)
                SetBorders();
        }

        public void Combine(SurfaceGroup feature, bool resetBorders = true)
        {
            if (feature == this) return;
            foreach (var primitive in feature.Surfaces)
                AddPrimitiveSurface(primitive, false);
            if (resetBorders)
                SetBorders();
        }

        public SurfaceGroup(IEnumerable<PrimitiveSurface> surfaces)
        {
            Surfaces = surfaces.ToHashSet();
            foreach (var surface in Surfaces)
                surface.BelongsToGroup = this;
            SetBorders();
        }

        public SurfaceGroup() { }

        public SurfaceGroup(PrimitiveSurface surface)
        {
            Surfaces = new HashSet<PrimitiveSurface> { surface };
            surface.BelongsToGroup = this;
            SetBorders();
        }

        public override HashSet<TriangleFace> Faces
        {
            get
            {
                if (faces == null) GetFaces();
                return faces;
            }
        }
        HashSet<TriangleFace> faces;
        void GetFaces()
        {
            var surfWithMostFaces = Surfaces.MaxBy(s => s.Faces.Count);
            // to be slightly efficient we "copy" the hashset from the surface with the most faces
            faces = new HashSet<TriangleFace>(surfWithMostFaces.Faces, surfWithMostFaces.Faces.Comparer);
            foreach (var surface in Surfaces)
            {   // then add the remainding surfaces' faces to this hashset
                if (surface == surfWithMostFaces) continue;
                foreach (var face in surface.Faces)
                    faces.Add(face);
            }
        }

        public override HashSet<Vertex> Vertices
        {
            get
            {
                if (vertices == null) GetVertices();
                return vertices;
            }
        }
        HashSet<Vertex> vertices;
        void GetVertices()
        {
            var surfWithMostvertices = Surfaces.MaxBy(s => s.Vertices.Count);
            // to be slightly efficient we "copy" the hashset from the surface with the most vertices
            vertices = new HashSet<Vertex>(surfWithMostvertices.Vertices, surfWithMostvertices.Vertices.Comparer);
            foreach (var surface in Surfaces)
            {   // then add the remainding survertices' vertices to this hashset
                if (surface == surfWithMostvertices) continue;
                foreach (var vertice in surface.Vertices)
                    vertices.Add(vertice);
            }
        }

        public IEnumerable<SurfaceGroup> AdjacentGroups()
        {
            //use a hashset to avoid duplicating feautes in the list
            var adjacentFeatures = new HashSet<SurfaceGroup>();
            foreach (var segment in BorderSegments)
            {
                var adjacent = Surfaces.Contains(segment.OwnedPrimitive) ? segment.OtherPrimitive : segment.OwnedPrimitive;
                adjacentFeatures.Add(adjacent.BelongsToGroup);
            }
            return adjacentFeatures;
        }
        public override HashSet<PrimitiveSurface> AdjacentPrimitives()
        {
            //Use a hash to avoid returning duplicates.
            var allAdjacent = new HashSet<PrimitiveSurface>();
            foreach (var border in BorderSegments)
            {
                if (Surfaces.Contains(border.OwnedPrimitive))
                    allAdjacent.Add(border.OtherPrimitive);
                else
                    allAdjacent.Add(border.OwnedPrimitive);
            }
            return allAdjacent;
        }

        public void SetBorders()
        {
            //Get a list of the outer border segments.
            BorderSegments = new List<BorderSegment>();

            foreach (var surface in Surfaces)
            {
                foreach (var segment in surface.BorderSegments)
                {
                    //we want to skip border segments that are shared between surfaces of this feature
                    var adjacent = segment.AdjacentPrimitive(surface);
                    if (Surfaces.Contains(adjacent)) continue;
                    BorderSegments.Add(segment);// segment.CopyToNewPrimitive(owned, other));
                }
            }

            //Build a new set of border loop from the border segments.
            Borders = TessellationInspectAndRepair.DefineBorders(BorderSegments, this).ToList();

            foreach (var border in Borders)
                border.SetBorderPlane();
        }

        /// <summary>
        /// Sets the color.
        /// </summary>
        /// <param name="color">The color.</param>
        public new void SetColor(Color color)
        {
            foreach (var face in Faces)
                face.Color = color;
        }

        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            throw new NotImplementedException();
        }

        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            throw new NotImplementedException();
        }

        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            throw new NotImplementedException();
        }

        public override double DistanceToPoint(Vector3 point)
        {
           return Surfaces.Min(s => s.DistanceToPoint(point));
        }

        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            throw new NotImplementedException();
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var closestSurface= Surfaces.MinBy(s => s.DistanceToPoint(point));
            return closestSurface.GetNormalAtPoint(point);
        }

        protected override void CalculateIsPositive()
        {
           if (Surfaces.All(s=> s.IsPositive.HasValue && s.IsPositive.Value))
                isPositive = true;
            else if (Surfaces.All(s => s.IsPositive.HasValue && !s.IsPositive.Value))
                isPositive = false;
            else
                isPositive = null;
        }

        protected override void SetPrimitiveLimits()
        {
            throw new NotImplementedException();
        }
    }
}
