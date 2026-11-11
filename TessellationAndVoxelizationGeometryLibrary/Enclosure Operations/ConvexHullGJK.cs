using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>Distance and penetration queries between the convex hulls of two point lists.</summary>
    public static partial class ConvexHullGJK
    {
        // Geometry predicates use a roundoff threshold independently of the requested distance accuracy.
        private const double RoundoffTolerance = 1.5e-14;

        /// <summary>
        /// Returns the shortest separation distance, zero for touching, or the negative minimum
        /// translation distance to touching for overlapping hulls (without rotating either hull).
        /// </summary>
        /// <param name="cvxHullPoints1">Points defining the first convex hull; their order is unchanged.</param>
        /// <param name="cvxHullPoints2">Points defining the second convex hull; their order is unchanged.</param>
        /// <param name="v">
        /// The second witness point minus the first. For separation this points from the first hull
        /// to the second. During penetration, translating the second hull by -v brings the witnesses
        /// together. Its length is the absolute signed distance, within numerical tolerance.
        /// </param>
        /// <returns>The signed distance, using the default tolerances of <see cref="ConvexHullDistanceOptions"/>.</returns>
        /// <exception cref="InvalidOperationException">The numerical solver did not converge.</exception>
        /// <remarks>
        /// Use <see cref="DistanceBetweenDetailed{T1, T2}"/> for vertex indices, interpolation weights,
        /// custom tolerances and an explicit convergence status. The input lists need not contain
        /// faces, be sorted, or contain only extreme vertices.
        /// </remarks>
        public static double DistanceBetween<T1, T2>(this IList<T1> cvxHullPoints1,
            IList<T2> cvxHullPoints2, out Vector3 v)
            where T1 : IVector3D where T2 : IVector3D
        {
            var result = cvxHullPoints1.DistanceBetweenDetailed(cvxHullPoints2);
            if (!result.Converged) throw new InvalidOperationException(result.FailureReason);
            v = result.Second.Point - result.First.Point;
            return result.SignedDistance;
        }

        /// <summary>
        /// Computes signed distance and reconstructable surface witnesses using GJK for separation
        /// and EPA for penetration. A negative distance is the negative minimum translation depth.
        /// </summary>
        /// <param name="cvxHullPoints1">A nonempty list of finite coordinates defining the first convex hull.</param>
        /// <param name="cvxHullPoints2">A nonempty list of finite coordinates defining the second convex hull.</param>
        /// <param name="options">Distance accuracy and iteration limits; null uses the defaults.</param>
        /// <returns>
        /// Witness indices refer to positions in the supplied lists, rather than
        /// <see cref="TessellationBaseClass.IndexInList"/>. Failed results have no witnesses and a NaN distance.
        /// </returns>
        /// <remarks>
        /// Second.Point - First.Point approximately equals SignedDistance * Normal.
        /// For penetration, translating the second hull by -SignedDistance * Normal brings it to touching.
        /// Touching is determined within tolerance, so its witnesses may differ slightly.
        /// Equally good witnesses and normals need not be unique. Points, segments and planar hulls
        /// are supported; an intersection admitting arbitrarily small separating translations in
        /// three dimensions has zero depth. No input coordinates, indices or topology are modified.
        /// </remarks>
        public static ConvexHullDistanceResult DistanceBetweenDetailed<T1, T2>(this IList<T1> cvxHullPoints1,
            IList<T2> cvxHullPoints2, ConvexHullDistanceOptions options = null)
            where T1 : IVector3D where T2 : IVector3D
        {
            options ??= new ConvexHullDistanceOptions();
            options.Validate();
            var query = new DistanceQuery(Snapshot(cvxHullPoints1, nameof(cvxHullPoints1)),
                Snapshot(cvxHullPoints2, nameof(cvxHullPoints2)), options);
            Span<SupportVertex> simplex = stackalloc SupportVertex[4];
            Span<double> weights = stackalloc double[4];
            simplex[0] = query.Support(Vector3.UnitX);
            weights[0] = 1;
            var count = 1;
            var closest = simplex[0].Difference;
            for (var iteration = 1; iteration <= options.MaxGjkIterations; iteration++)
            {
                var distance = closest.Length();
                if (distance <= query.Tolerance)
                    return Penetration(query, simplex[..count], weights[..count], iteration);

                var support = query.Support(-closest / distance);
                // Both sides have units of distance in the normalized coordinate system.
                var improvement = distance - closest.Dot(support.Difference) / distance;
                if (improvement <= query.Tolerance)
                    return query.Result(simplex[..count], weights[..count], false, iteration, 0);
                if (Contains(simplex[..count], support))
                    return Failure("GJK stalled before reaching the requested accuracy.", iteration, 0);

                simplex[count++] = support;
                closest = ClosestPoint(simplex[..count], weights);
                var reducedCount = 0;
                for (var i = 0; i < count; i++)
                    if (weights[i] > 0)
                    {
                        simplex[reducedCount] = simplex[i];
                        weights[reducedCount++] = weights[i];
                    }
                count = reducedCount;
            }
            return Failure("GJK reached its iteration limit.", options.MaxGjkIterations, 0);
        }

        private static Vector3[] Snapshot<T>(IList<T> points, string name) where T : IVector3D
        {
            if (points == null) throw new ArgumentNullException(name);
            if (points.Count == 0) throw new ArgumentException("A convex hull needs at least one point.", name);
            var snapshot = new Vector3[points.Count];
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p is null || !double.IsFinite(p.X) || !double.IsFinite(p.Y) || !double.IsFinite(p.Z))
                    throw new ArgumentException("All hull points must have finite coordinates.", name);
                snapshot[i] = new Vector3(p.X, p.Y, p.Z);
            }
            return snapshot;
        }

        private readonly struct SupportVertex
        {
            internal readonly Vector3 Difference;
            internal readonly int First, Second;
            internal SupportVertex(Vector3 difference, int first, int second)
            { Difference = difference; First = first; Second = second; }
        }

        private static bool Contains(ReadOnlySpan<SupportVertex> points, SupportVertex point)
        {
            foreach (var p in points)
                if ((p.First == point.First && p.Second == point.Second)
                    || (p.Difference - point.Difference).LengthSquared() <= RoundoffTolerance * RoundoffTolerance)
                    return true;
            return false;
        }

        // Enumerate at most 15 subsets so reduction and barycentric bookkeeping share one contract.
        // Singular subsets are skipped; their edges and vertices are considered separately.
        private static Vector3 ClosestPoint(ReadOnlySpan<SupportVertex> simplex, Span<double> weights)
        {
            var best = Vector3.Zero;
            var bestSquared = double.PositiveInfinity;
            Span<int> indices = stackalloc int[4];
            Span<double> candidate = stackalloc double[4];
            weights.Clear();
            for (var mask = 1; mask < (1 << simplex.Length); mask++)
            {
                var count = 0;
                for (var i = 0; i < simplex.Length; i++)
                    if ((mask & (1 << i)) != 0) indices[count++] = i;
                candidate.Clear();
                if (!ProjectOrigin(simplex, indices[..count], candidate)) continue;
                var sum = 0.0;
                var valid = true;
                for (var i = 0; i < count; i++)
                {
                    if (candidate[i] < -RoundoffTolerance || !double.IsFinite(candidate[i])) { valid = false; break; }
                    candidate[i] = Math.Max(0, candidate[i]);
                    sum += candidate[i];
                }
                if (!valid || sum == 0) continue;
                var point = Vector3.Zero;
                for (var i = 0; i < count; i++)
                {
                    candidate[i] /= sum;
                    point += candidate[i] * simplex[indices[i]].Difference;
                }
                // The triangle's normal gives a projection without cancellation of large
                // tangential coordinates. Its barycentric reconstruction still defines witnesses.
                if (count == 3 && candidate[0] > 0 && candidate[1] > 0 && candidate[2] > 0)
                {
                    var a = simplex[indices[0]].Difference;
                    var n = (simplex[indices[1]].Difference - a).Cross(simplex[indices[2]].Difference - a);
                    point = n * (a.Dot(n) / n.LengthSquared());
                }
                else if (count == 4 && candidate[0] > 0 && candidate[1] > 0 && candidate[2] > 0 && candidate[3] > 0)
                    point = Vector3.Zero;
                var squared = point.LengthSquared();
                if (squared >= bestSquared) continue;
                best = point;
                bestSquared = squared;
                weights.Clear();
                for (var i = 0; i < count; i++) weights[indices[i]] = candidate[i];
            }
            return best;
        }

        private static bool ProjectOrigin(ReadOnlySpan<SupportVertex> simplex, ReadOnlySpan<int> indices,
            Span<double> weights)
        {
            var a = simplex[indices[0]].Difference;
            weights[0] = 1;
            if (indices.Length == 1) return true;
            var ab = simplex[indices[1]].Difference - a;
            var abSquared = ab.LengthSquared();
            if (abSquared == 0) return false;
            if (indices.Length == 2)
            {
                weights[1] = -a.Dot(ab) / abSquared;
                weights[0] = 1 - weights[1];
                return true;
            }
            var ac = simplex[indices[2]].Difference - a;
            var normal = ab.Cross(ac);
            var normalSquared = normal.LengthSquared();
            if (normalSquared <= RoundoffTolerance * RoundoffTolerance * abSquared * ac.LengthSquared()) return false;
            if (indices.Length == 3)
            {
                weights[1] = (-a).Cross(ac).Dot(normal) / normalSquared;
                weights[2] = ab.Cross(-a).Dot(normal) / normalSquared;
                weights[0] = 1 - weights[1] - weights[2];
                return true;
            }
            var ad = simplex[indices[3]].Difference - a;
            var determinant = normal.Dot(ad);
            if (Math.Abs(determinant) <= RoundoffTolerance * Math.Sqrt(normalSquared * ad.LengthSquared())) return false;
            weights[1] = (-a).Dot(ac.Cross(ad)) / determinant;
            weights[2] = ab.Dot((-a).Cross(ad)) / determinant;
            weights[3] = normal.Dot(-a) / determinant;
            weights[0] = 1 - weights[1] - weights[2] - weights[3];
            return true;
        }

        private static ConvexHullDistanceResult Failure(string reason, int gjkIterations, int epaIterations)
            => new ConvexHullDistanceResult(double.NaN, Vector3.Zero, null, null,
                ConvexHullDistanceStatus.Failed, gjkIterations, epaIterations, reason);

        private sealed class DistanceQuery
        {
            internal readonly ConvexHullDistanceOptions Options;
            internal readonly double Scale, Tolerance;
            private readonly Vector3[] first, second, normalizedFirst, normalizedSecond;

            internal DistanceQuery(Vector3[] first, Vector3[] second, ConvexHullDistanceOptions options)
            {
                this.first = first;
                this.second = second;
                Options = options;
                var origin = first[0];
                normalizedFirst = new Vector3[first.Length];
                normalizedSecond = new Vector3[second.Length];
                Scale = 0;
                foreach (var p in first) Scale = Math.Max(Scale, MaxCoordinate(p - origin));
                foreach (var p in second) Scale = Math.Max(Scale, MaxCoordinate(p - origin));
                if (!double.IsFinite(Scale))
                    throw new ArgumentException("The coordinate range is too large for a distance query.");
                if (Scale == 0) Scale = 1;
                for (var i = 0; i < first.Length; i++) normalizedFirst[i] = (first[i] - origin) / Scale;
                for (var i = 0; i < second.Length; i++) normalizedSecond[i] = (second[i] - origin) / Scale;
                Tolerance = Math.Max(RoundoffTolerance, options.AbsoluteTolerance / Scale + options.RelativeTolerance);
            }

            private static double MaxCoordinate(Vector3 v) => Math.Max(Math.Abs(v.X), Math.Max(Math.Abs(v.Y), Math.Abs(v.Z)));

            internal SupportVertex Support(Vector3 direction)
            {
                var i = SupportIndex(normalizedFirst, direction);
                var j = SupportIndex(normalizedSecond, -direction);
                return new SupportVertex(normalizedFirst[i] - normalizedSecond[j], i, j);
            }

            private static int SupportIndex(Vector3[] points, Vector3 direction)
            {
                var best = 0;
                var max = points[0].Dot(direction);
                for (var i = 1; i < points.Length; i++)
                {
                    var dot = points[i].Dot(direction);
                    if (dot > max) { max = dot; best = i; }
                }
                return best;
            }

            internal ConvexHullDistanceResult Result(ReadOnlySpan<SupportVertex> simplex, ReadOnlySpan<double> weights,
                bool penetration, int gjkIterations, int epaIterations, Vector3 surfaceNormal = default)
            {
                // A numerically flat tetrahedron can contain the origin even though its height is
                // below the roundoff floor. Return its closest boundary triangle, never four source
                // vertices for an interior witness. This path is only used for tolerance contact.
                if (simplex.Length == 4)
                {
                    Span<SupportVertex> triangle = stackalloc SupportVertex[3];
                    Span<SupportVertex> bestTriangle = stackalloc SupportVertex[3];
                    Span<double> triangleWeights = stackalloc double[3];
                    Span<double> bestWeights = stackalloc double[3];
                    var bestSquared = double.PositiveInfinity;
                    for (var omitted = 0; omitted < 4; omitted++)
                    {
                        var count = 0;
                        for (var i = 0; i < 4; i++)
                            if (i != omitted) triangle[count++] = simplex[i];
                        var closest = ClosestPoint(triangle, triangleWeights);
                        var squared = closest.LengthSquared();
                        if (squared >= bestSquared) continue;
                        bestSquared = squared;
                        triangle.CopyTo(bestTriangle);
                        triangleWeights.CopyTo(bestWeights);
                    }
                    if (bestSquared > Tolerance * Tolerance)
                        return Failure("A contact simplex could not be reduced to a surface witness.", gjkIterations, epaIterations);
                    return Result(bestTriangle, bestWeights, penetration, gjkIterations, epaIterations, surfaceNormal);
                }
                var difference = Vector3.Zero;
                for (var i = 0; i < simplex.Length; i++) difference += weights[i] * simplex[i].Difference;
                var distance = difference.Length();
                var touching = distance <= Tolerance;
                var normal = surfaceNormal.LengthSquared() > 0 ? surfaceNormal
                    : touching ? Vector3.Zero : (penetration ? difference / distance : -difference / distance);
                return new ConvexHullDistanceResult(touching ? 0 : (penetration ? -distance : distance) * Scale,
                    normal, Witness(first, simplex, weights, true), Witness(second, simplex, weights, false),
                    touching ? ConvexHullDistanceStatus.Touching : penetration ? ConvexHullDistanceStatus.Penetrating
                    : ConvexHullDistanceStatus.Separated, gjkIterations, epaIterations, null);
            }

            private static ConvexHullWitness Witness(Vector3[] points, ReadOnlySpan<SupportVertex> simplex,
                ReadOnlySpan<double> weights, bool firstHull)
            {
                var indices = new List<int>(3);
                var combinedWeights = new List<double>(3);
                for (var i = 0; i < simplex.Length; i++)
                {
                    if (weights[i] <= 0) continue;
                    var index = firstHull ? simplex[i].First : simplex[i].Second;
                    var existing = indices.IndexOf(index);
                    if (existing < 0) { indices.Add(index); combinedWeights.Add(weights[i]); }
                    else combinedWeights[existing] += weights[i];
                }
                // Reconstruct relative to a source vertex to avoid summing large world coordinates.
                var origin = points[indices[0]];
                var point = origin;
                for (var i = 1; i < indices.Count; i++) point += combinedWeights[i] * (points[indices[i]] - origin);
                return new ConvexHullWitness(point, indices.ToArray(), combinedWeights.ToArray());
            }
        }
    }
}
