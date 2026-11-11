using System;
using System.Collections.Generic;

namespace TVGL
{
    public static partial class ConvexHullGJK
    {
        private sealed class PolytopeFace
        {
            internal readonly int A, B, C;
            internal readonly Vector3 Normal, Closest;
            internal readonly double PlaneDistance;
            internal readonly double[] Weights;

            internal PolytopeFace(int a, int b, int c, Vector3 normal, double planeDistance,
                Vector3 closest, double[] weights)
            { A = a; B = b; C = c; Normal = normal; PlaneDistance = planeDistance; Closest = closest; Weights = weights; }
        }

        private static ConvexHullDistanceResult Penetration(DistanceQuery query,
            ReadOnlySpan<SupportVertex> simplex, ReadOnlySpan<double> weights, int gjkIterations)
        {
            var vertices = new List<SupportVertex>(16);
            foreach (var vertex in simplex) AddSupport(vertices, vertex);
            foreach (var axis in new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
            {
                AddSupport(vertices, query.Support(axis));
                AddSupport(vertices, query.Support(-axis));
            }

            // A GJK segment through the origin may lie deep inside a full-dimensional difference.
            // Probe its orthogonal complement instead of equating a small simplex with touching.
            var basis = FindBasis(vertices);
            if (basis.Count == 2)
            {
                var direction = vertices[basis[1]].Difference - vertices[basis[0]].Difference;
                var axis = Math.Abs(direction.X) < Math.Abs(direction.Y) ? Vector3.UnitX : Vector3.UnitY;
                var perpendicular = direction.Cross(axis);
                perpendicular /= perpendicular.Length();
                var other = direction.Cross(perpendicular);
                other /= other.Length();
                AddSupport(vertices, query.Support(perpendicular));
                AddSupport(vertices, query.Support(-perpendicular));
                AddSupport(vertices, query.Support(other));
                AddSupport(vertices, query.Support(-other));
                basis = FindBasis(vertices);
            }
            if (basis.Count == 3)
            {
                var normal = (vertices[basis[1]].Difference - vertices[basis[0]].Difference)
                    .Cross(vertices[basis[2]].Difference - vertices[basis[0]].Difference);
                normal /= normal.Length();
                AddSupport(vertices, query.Support(normal));
                AddSupport(vertices, query.Support(-normal));
                basis = FindBasis(vertices);
            }
            if (basis.Count < 4)
                return query.Result(simplex, weights, true, gjkIterations, 0);

            // The seed tetrahedron need not enclose the origin. Incorporating every original GJK
            // vertex preserves its near-zero convex combination, including symmetric octahedra
            // where no tetrahedron made of extreme vertices strictly encloses the origin.
            var interior = Vector3.Zero;
            foreach (var index in basis) interior += vertices[index].Difference / 4;
            var faces = new List<PolytopeFace>();
            foreach (var triangle in new[] { (0, 1, 2), (0, 3, 1), (0, 2, 3), (1, 3, 2) })
            {
                if (!TryMakeFace(vertices, basis[triangle.Item1], basis[triangle.Item2], basis[triangle.Item3], interior, out var face))
                    return Failure("EPA could not construct a nondegenerate initial polytope.", gjkIterations, 0);
                faces.Add(face);
            }
            for (var i = 0; i < vertices.Count; i++)
                if (!basis.Contains(i) && !TryExpand(vertices, faces, i, interior, out _))
                    return Failure("EPA could not construct a closed initial polytope.", gjkIterations, 0);

            Span<SupportVertex> triangleVertices = stackalloc SupportVertex[3];
            for (var iteration = 1; iteration <= query.Options.MaxEpaIterations; iteration++)
            {
                PolytopeFace outside = null;
                PolytopeFace nearest = null;
                var nearestSquared = double.PositiveInfinity;
                foreach (var face in faces)
                {
                    if (face.PlaneDistance < -RoundoffTolerance
                        && (outside == null || face.PlaneDistance < outside.PlaneDistance)) outside = face;
                    var squared = face.Closest.LengthSquared();
                    if (squared < nearestSquared) { nearestSquared = squared; nearest = face; }
                }
                if (nearest == null) return Failure("EPA lost its polytope boundary.", gjkIterations, iteration);

                // A small positive separation can enter EPA through the distance tolerance. Certify
                // contact against a supporting plane, or expand until the origin is enclosed.
                // Use the geometric face normal. Normalizing a reconstructed witness difference
                // magnifies its tangential roundoff when the penetration depth is very small.
                var direction = outside != null ? outside.Normal : nearest.Normal;
                var support = query.Support(direction);
                var supportDistance = direction.Dot(support.Difference);
                if (outside != null && supportDistance <= query.Tolerance)
                    return query.Result(simplex, weights, false, gjkIterations, iteration, direction);
                if (outside == null && supportDistance - nearest.PlaneDistance <= query.Tolerance)
                {
                    triangleVertices[0] = vertices[nearest.A];
                    triangleVertices[1] = vertices[nearest.B];
                    triangleVertices[2] = vertices[nearest.C];
                    return query.Result(triangleVertices, nearest.Weights, true, gjkIterations, iteration, direction);
                }

                if (!AddSupport(vertices, support))
                    return Failure("EPA repeated a support point before reaching the requested accuracy.", gjkIterations, iteration);
                if (!TryExpand(vertices, faces, vertices.Count - 1, interior, out var expanded) || !expanded)
                    return Failure("EPA could not expand a closed polytope at the requested accuracy.", gjkIterations, iteration);
            }
            return Failure("EPA reached its iteration limit.", gjkIterations, query.Options.MaxEpaIterations);
        }

        private static bool AddSupport(List<SupportVertex> vertices, SupportVertex support)
        {
            foreach (var vertex in vertices)
                if ((vertex.First == support.First && vertex.Second == support.Second)
                    || (vertex.Difference - support.Difference).LengthSquared() <= RoundoffTolerance * RoundoffTolerance)
                    return false;
            vertices.Add(support);
            return true;
        }

        private static List<int> FindBasis(List<SupportVertex> vertices)
        {
            var basis = new List<int>(4) { 0 };
            var origin = vertices[0].Difference;
            var farthest = 0;
            var maxSquared = RoundoffTolerance * RoundoffTolerance;
            for (var i = 1; i < vertices.Count; i++)
            {
                var squared = (vertices[i].Difference - origin).LengthSquared();
                if (squared > maxSquared) { maxSquared = squared; farthest = i; }
            }
            if (farthest == 0) return basis;
            basis.Add(farthest);
            var line = vertices[farthest].Difference - origin;
            var lineSquared = line.LengthSquared();
            farthest = 0;
            maxSquared = RoundoffTolerance * RoundoffTolerance;
            for (var i = 1; i < vertices.Count; i++)
            {
                var squared = line.Cross(vertices[i].Difference - origin).LengthSquared() / lineSquared;
                if (squared > maxSquared) { maxSquared = squared; farthest = i; }
            }
            if (farthest == 0) return basis;
            basis.Add(farthest);
            var normal = line.Cross(vertices[farthest].Difference - origin);
            normal /= normal.Length();
            farthest = 0;
            var maxDistance = RoundoffTolerance;
            for (var i = 1; i < vertices.Count; i++)
            {
                var distance = Math.Abs(normal.Dot(vertices[i].Difference - origin));
                if (distance > maxDistance) { maxDistance = distance; farthest = i; }
            }
            if (farthest != 0) basis.Add(farthest);
            return basis;
        }

        private static bool TryMakeFace(List<SupportVertex> vertices, int a, int b, int c,
            Vector3 interior, out PolytopeFace face)
        {
            face = null;
            var origin = vertices[a].Difference;
            var ab = vertices[b].Difference - origin;
            var ac = vertices[c].Difference - origin;
            var normal = ab.Cross(ac);
            var length = normal.Length();
            if (length <= RoundoffTolerance * Math.Sqrt(ab.LengthSquared() * ac.LengthSquared())) return false;
            normal /= length;
            if (normal.Dot(origin - interior) < 0) { (b, c) = (c, b); normal = -normal; }
            Span<SupportVertex> triangle = stackalloc SupportVertex[3];
            triangle[0] = vertices[a]; triangle[1] = vertices[b]; triangle[2] = vertices[c];
            var weights = new double[3];
            var closest = ClosestPoint(triangle, weights);
            face = new PolytopeFace(a, b, c, normal, normal.Dot(origin), closest, weights);
            return true;
        }

        private static bool TryExpand(List<SupportVertex> vertices, List<PolytopeFace> faces,
            int vertexIndex, Vector3 interior, out bool expanded)
        {
            expanded = false;
            var visible = new List<int>();
            var horizon = new Dictionary<(int, int), (int A, int B)>();
            var point = vertices[vertexIndex].Difference;
            for (var i = 0; i < faces.Count; i++)
            {
                var face = faces[i];
                if (face.Normal.Dot(point - vertices[face.A].Difference) <= RoundoffTolerance) continue;
                visible.Add(i);
                if (!AddEdge(face.A, face.B) || !AddEdge(face.B, face.C) || !AddEdge(face.C, face.A)) return false;
            }
            if (visible.Count == 0) return true; // An interior or coplanar seed point adds no boundary.
            if (horizon.Count < 3) return false;
            var starts = new HashSet<int>();
            var ends = new HashSet<int>();
            var additions = new List<PolytopeFace>(horizon.Count);
            foreach (var edge in horizon.Values)
            {
                if (!starts.Add(edge.A) || !ends.Add(edge.B)
                    || !TryMakeFace(vertices, edge.A, edge.B, vertexIndex, interior, out var face)) return false;
                additions.Add(face);
            }
            if (!starts.SetEquals(ends)) return false;
            // Publish only a complete expansion, so a failed predicate cannot leave an open hull.
            for (var i = visible.Count - 1; i >= 0; i--) faces.RemoveAt(visible[i]);
            faces.AddRange(additions);
            expanded = true;
            return true;

            bool AddEdge(int a, int b)
            {
                if (horizon.Remove((b, a))) return true;
                return horizon.TryAdd((a, b), (a, b));
            }
        }
    }
}
