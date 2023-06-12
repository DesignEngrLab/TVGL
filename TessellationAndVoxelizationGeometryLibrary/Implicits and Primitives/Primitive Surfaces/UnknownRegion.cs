// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="UnknownRegion.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System.Collections.Generic;


namespace TVGL
{
    /// <summary>
    /// Class DenseRegion.
    /// </summary>
    public class UnknownRegion : PrimitiveSurface
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownRegion" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public UnknownRegion(IEnumerable<TriangleFace> faces) : base(faces) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownRegion"/> class.
        /// </summary>
        public UnknownRegion() { }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
        }
        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateMeanSquareError(IEnumerable<Vector3> vertices = null)
        {
            return 0.0;
        }

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            return Vector2.Null;
        }

        /// <summary>
        /// Transforms the from2 d to3 d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            return Vector3.Null;
        }

        /// <summary>
        /// Transforms the from3 d to2 d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            if (!Plane.DefineNormalAndDistanceFromVertices(points, out _, out var normal))
                yield break;
            var transform = normal.TransformToXYPlane(out _);
            foreach (var point in points)
            {
                yield return point.ConvertTo2DCoordinates(transform);
            }
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double PointMembership(Vector3 point)
        {
            return double.NaN;
        }

        protected override void CalculateIsPositive()
        {
            // todo: do we want this to be true=convex and false=concave?
            // it's not exactly the same. 
            //throw new NotImplementedException();
        }
    }
}