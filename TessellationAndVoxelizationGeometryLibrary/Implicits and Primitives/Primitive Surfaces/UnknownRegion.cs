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
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// Class DenseRegion.
    /// </summary>
    public class UnknownRegion : PrimitiveSurface
    {
        public override string KeyString => "Unknown" + GetCommonKeyDetails();

        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownRegion" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        public UnknownRegion(IEnumerable<TriangleFace> faces)
        {
            SetFacesAndVertices(faces);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="UnknownRegion"/> class.
        /// </summary>
        public UnknownRegion() { }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix, bool transformFacesAndVertices)
        {
            base.Transform(transformMatrix, transformFacesAndVertices);
        }
        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateMeanSquareError(IEnumerable<Vector3> vertices = null)
        {
            return double.PositiveInfinity;
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
        /// Calculates the both errors.
        /// </summary>
       internal protected override void CalculateBothErrors()
        {
            _maxError = double.PositiveInfinity;
            _meanSquaredError = double.PositiveInfinity;
        }

        /// <summary>
        /// Calculates the mean square error.
        /// </summary>
        /// <param name="points">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateMaxError(IEnumerable<Vector3> points)
        {
            return double.PositiveInfinity;
        }

        /// <summary>
        /// Returns whether the given point is inside the primitive.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public override bool PointIsInside(Vector3 x)
        {
            return false;
        }
        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var (distance, _) = GetClosest(point);
            return distance;
        }

        public override Vector3 ClosestPointOnSurfaceToPoint(Vector3 point)
        {
            var (_, closestElt) = GetClosest(point);
            if (closestElt is TriangleFace face)
                            return
                MiscFunctions.PointOnTriangleFromRay(face, point, face.Normal, out _);
            if (closestElt is Edge edge)
            {
                var lineVector = edge.Vector.Normalize();
                MiscFunctions.DistancePointToLine(point, edge.From.Coordinates, lineVector, out var pointOnLine);
                return pointOnLine;
            }
            return ((Vertex)closestElt).Coordinates;
        }

        private (double distance, TessellationBaseClass closestElt) GetClosest(Vector3 point)
        {
            var shortestDistance = double.PositiveInfinity;
            TessellationBaseClass closestElt = null;
            foreach (var face in Faces)
            {
                var ptOnTri =
                MiscFunctions.PointOnTriangleFromRay(face, point, face.Normal, out var signedDistance);
                if (ptOnTri.IsNull()) continue;
                if (Math.Abs(signedDistance) < shortestDistance)
                {
                    shortestDistance = Math.Abs(signedDistance);
                    closestElt = face;
                }
            }
            foreach (var v in Vertices)
            {
                var distance = v.Coordinates.Distance(point);
                if (Math.Abs(distance) < shortestDistance)
                {
                    shortestDistance = Math.Abs(distance);
                    closestElt = v;
                }
            }
            foreach (var edge in InnerEdges.Concat(OuterEdges))
            {
                var lineVector = edge.Vector.Normalize();
                var distance = MiscFunctions.DistancePointToLine(point, edge.From.Coordinates, lineVector, out var pointOnLine);
                var t = (pointOnLine - edge.From.Coordinates).Dot(lineVector);
                if (t < 0 || t > edge.Length) continue;
                if (Math.Abs(distance) < shortestDistance)
                {
                    shortestDistance = Math.Abs(distance);
                    closestElt = edge;
                }
            }
            return (shortestDistance, closestElt);
        }

        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var (distance, closestElt) = GetClosest(point);
            if (closestElt is TriangleFace face) return face.Normal;

            if (closestElt is Edge edge)
            {
                var vecToEdgeCenter = (edge.Center() - point).Normalize();
                var ownDot = Faces.Contains(edge.OwnedFace) ?
                    Math.Abs(vecToEdgeCenter.Dot(edge.OwnedFace.Normal))
                    : double.PositiveInfinity;
                var othDot = Faces.Contains(edge.OtherFace)
                    ? Math.Abs(vecToEdgeCenter.Dot(edge.OtherFace.Normal))
                    : double.PositiveInfinity;
                return ownDot > othDot ? edge.OwnedFace.Normal : edge.OtherFace.Normal;
            }
            var vertex = (Vertex)closestElt;
            var vecToVertex = (vertex.Coordinates - point).Normalize();
            var closestFace = vertex.Faces.Where(f => Faces.Contains(f))
                .MaxBy(f => Math.Abs(vecToVertex.Dot(f.Normal)));
            return closestFace.Normal;
        }
        protected override void CalculateIsPositive()
        {
            // todo: do we want this to be true=convex and false=concave?
            // it's not exactly the same. 
            //throw new NotImplementedException();
        }

        protected override void SetPrimitiveLimits()
        {
            MinX = MinY = MinZ = double.NegativeInfinity;
            MaxX = MaxY = MaxZ = double.PositiveInfinity;
        }

        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            if (Faces == null || Faces.Count == 0) yield break;
            foreach (var face in Faces)
            {
                var intersectPoint = MiscFunctions.PointOnTriangleFromRay(face, anchor, direction, out var t);
                if (!intersectPoint.IsNull()) yield return (intersectPoint, t);
            }
        }
    }
}