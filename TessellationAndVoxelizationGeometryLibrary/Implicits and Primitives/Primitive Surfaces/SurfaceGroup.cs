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
        public HashSet<PrimitiveSurface> Surfaces { get; set; }

        public void AddPrimitiveSurface(PrimitiveSurface surface, bool resetBorders = true)
        {
            if (surface is SurfaceGroup)
                throw new Exception("Use Combine");
            Surfaces.Add(surface);
            surface.BelongsToGroup = this;
            if(resetBorders)
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

        public new IEnumerable<TriangleFace> Faces()
        {
            //Okay to return IEnumerable, since no duplicates will occur.
            foreach(var surface in Surfaces)
                foreach(var face in surface.Faces) 
                    yield return face;
        }

        public new IEnumerable<Vertex> Vertices()
        {
            //Duplicates will occur.
            var vertices = new HashSet<Vertex>();   
            foreach (var surface in Surfaces)
                foreach (var vertex in surface.Vertices)
                    vertices.Add(vertex);
            return vertices;
        }

        public IEnumerable<SurfaceGroup> AdjacentGroups()
        {
            //use a hashset to avoid duplicating feautes in the list
            var adjacentFeatures = new HashSet<SurfaceGroup>();
            foreach(var segment in BorderSegments)
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
            foreach(var face in Faces())
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
            throw new NotImplementedException();
        }

        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            throw new NotImplementedException();
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            throw new NotImplementedException();
        }

        protected override void CalculateIsPositive()
        {
            throw new NotImplementedException();
        }

        protected override void SetPrimitiveLimits()
        {
            throw new NotImplementedException();
        }
    }
}
