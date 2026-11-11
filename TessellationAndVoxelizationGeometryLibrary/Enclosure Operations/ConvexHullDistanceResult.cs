using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>The hull relationship, or failure to establish it at the requested accuracy.</summary>
    public enum ConvexHullDistanceStatus
    {
        /// <summary>The hulls are farther apart than the numerical tolerance.</summary>
        Separated,
        /// <summary>The signed distance is zero within the numerical tolerance.</summary>
        Touching,
        /// <summary>The hulls have positive minimum translation depth.</summary>
        Penetrating,
        /// <summary>The solver exhausted its iteration limit or encountered numerical degeneracy.</summary>
        Failed
    }

    /// <summary>Accuracy and resource limits for a GJK/EPA distance query.</summary>
    /// <remarks>
    /// Absolute and relative tolerances are added. A roundoff floor of approximately 1.5e-14 times
    /// the coordinate range applies even when both tolerances are zero.
    /// </remarks>
    public sealed class ConvexHullDistanceOptions
    {
        /// <summary>Absolute distance tolerance in input units. Defaults to 1e-9.</summary>
        public double AbsoluteTolerance { get; init; } = Constants.BaseTolerance;
        /// <summary>
        /// Relative distance tolerance, multiplied by the maximum coordinate range relative to the
        /// first input point (including the separation of the hulls). Defaults to 1e-9.
        /// </summary>
        public double RelativeTolerance { get; init; } = Constants.BaseTolerance;
        /// <summary>Maximum GJK iterations. Defaults to 128.</summary>
        public int MaxGjkIterations { get; init; } = 128;
        /// <summary>Maximum EPA iterations, including enclosure refinement. Defaults to 256.</summary>
        public int MaxEpaIterations { get; init; } = 256;

        internal void Validate()
        {
            if (!double.IsFinite(AbsoluteTolerance) || AbsoluteTolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(AbsoluteTolerance));
            if (!double.IsFinite(RelativeTolerance) || RelativeTolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(RelativeTolerance));
            if (MaxGjkIterations < 1) throw new ArgumentOutOfRangeException(nameof(MaxGjkIterations));
            if (MaxEpaIterations < 1) throw new ArgumentOutOfRangeException(nameof(MaxEpaIterations));
        }
    }

    /// <summary>A surface point expressed as a convex combination of source vertices.</summary>
    /// <remarks>
    /// Point = sum(Weights[k] * input[VertexIndices[k]]), with nonnegative weights summing to one.
    /// One, two or three entries give vertex, segment or triangle interpolation. These describe a
    /// surface representation, not necessarily an edge or triangle in the input mesh topology:
    /// two vertices can span a diagonal of a planar facet. Indices are list positions; for Vertex
    /// inputs, input[index].IndexInList retrieves the associated original mesh index.
    /// </remarks>
    public sealed class ConvexHullWitness
    {
        /// <summary>The reconstructed point in the input coordinate system.</summary>
        public Vector3 Point { get; }
        /// <summary>Positions in the corresponding input list, paired with <see cref="Weights"/>.</summary>
        public IReadOnlyList<int> VertexIndices { get; }
        /// <summary>Barycentric weights; for a segment, the second weight is its t parameter.</summary>
        public IReadOnlyList<double> Weights { get; }

        internal ConvexHullWitness(Vector3 point, int[] vertexIndices, double[] weights)
        {
            Point = point;
            VertexIndices = Array.AsReadOnly(vertexIndices);
            Weights = Array.AsReadOnly(weights);
        }
    }

    /// <summary>A signed hull distance with interpolation data for both surface witnesses.</summary>
    public sealed class ConvexHullDistanceResult
    {
        /// <summary>Positive separation, zero touching, negative translation depth, or NaN on failure.</summary>
        public double SignedDistance { get; }
        /// <summary>
        /// Unit direction satisfying Second.Point - First.Point = SignedDistance * Normal within tolerance.
        /// For penetration, moving the second hull by -SignedDistance * Normal brings it to touching.
        /// At touching, the direction can be nonunique or zero.
        /// </summary>
        public Vector3 Normal { get; }
        /// <summary>Witness on the first hull, or null on failure.</summary>
        public ConvexHullWitness First { get; }
        /// <summary>Witness on the second hull, or null on failure.</summary>
        public ConvexHullWitness Second { get; }
        /// <summary>The geometric relationship or failure status.</summary>
        public ConvexHullDistanceStatus Status { get; }
        /// <summary>Whether the distance and witnesses reached the requested numerical accuracy.</summary>
        public bool Converged => Status != ConvexHullDistanceStatus.Failed;
        /// <summary>The number of GJK iterations.</summary>
        public int GjkIterations { get; }
        /// <summary>The number of EPA iterations, including enclosure refinement.</summary>
        public int EpaIterations { get; }
        /// <summary>Explanation of a failed query, or null for a converged query.</summary>
        public string FailureReason { get; }

        internal ConvexHullDistanceResult(double signedDistance, Vector3 normal, ConvexHullWitness first,
            ConvexHullWitness second, ConvexHullDistanceStatus status, int gjkIterations, int epaIterations, string failureReason)
        {
            SignedDistance = signedDistance;
            Normal = normal;
            First = first;
            Second = second;
            Status = status;
            GjkIterations = gjkIterations;
            EpaIterations = epaIterations;
            FailureReason = failureReason;
        }
    }
}
