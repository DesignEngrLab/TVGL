// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;


namespace TVGL
{
    /// <summary>
    ///     The class for Cone primitives.
    /// </summary>
    public class Cone : PrimitiveSurface
    {
        public Cone() { }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive, IEnumerable<PolygonalFace> faces)
            : base(faces)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cone"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cone(Cone originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            IsPositive = originalToBeCopied.IsPositive;
            Aperture = originalToBeCopied.Aperture;
            Apex = originalToBeCopied.Apex;
            Axis = originalToBeCopied.Axis;
        }


        /// <summary>
        ///     Is the cone positive? (false is negative)
        /// </summary>
        public bool IsPositive;

        /// <summary>
        ///     Gets the aperture. This is a slope, like m, not an angle. It is dimensionless and NOT radians.
        ///     like y = mx + b
        /// </summary>
        /// <value>The aperture.</value>
        public double Aperture { get; set; }

        /// <summary>
        ///     Gets the apex.
        /// </summary>
        /// <value>The apex.</value>
        public Vector3 Apex { get; set; }

        /// <summary>
        ///     Gets the axis.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Axis = Axis.Multiply(transformMatrix);
            Apex = Apex.Multiply(transformMatrix);
        }

        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var sqDistanceSum = 0.0;
            var numVerts = 0;
            var cosApertureSquared = 1 / (1 + Aperture);
            foreach (var c in vertices)
            {
                var d = (c - Apex).Cross(Axis).Length()
                    - Math.Abs(Aperture * (c - Apex).Dot(Axis));
                sqDistanceSum += d * d * cosApertureSquared;
                numVerts++;
            }
            return sqDistanceSum / numVerts;
        }


        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = new Vector3(point.X, point.Y, point.Z) - Apex;
            if (faceXDir.IsNull())
            {
                faceXDir = Axis.GetPerpendicularDirection();
                faceYDir = faceXDir.Cross(Axis);
            }
            var x = faceXDir.Dot(v);
            var y = faceYDir.Dot(v);
            var angle = Math.Atan2(y, x);
            var dxAlong = v.Dot(Axis);
            var radius = dxAlong * Aperture;
            return new Vector2(angle * radius, v.Dot(Axis));
        }

        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var radius = point.Y * Aperture;
            var angle = (point.X / radius) % Constants.TwoPi;
            var result = Apex + radius * Math.Cos(angle) * faceXDir;
            result += radius * Math.Sin(angle) * faceYDir;
            result += point.Y * Axis;
            return result;
        }

        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            if (pathIsClosed && BorderEncirclesAxis(points, Axis, Apex))
            {
                var transform = Axis.TransformToXYPlane(out _);
                foreach (var point in points)
                    yield return point.ConvertTo2DCoordinates(transform);
                yield break;
            }
            var lastPoint = points.First();
            var last2DVertex = TransformFrom3DTo2D(lastPoint);
            yield return last2DVertex;
            foreach (var point in points.Skip(1))
            {
                var vector = point - lastPoint;
                var rightIsOutward = vector.Cross(Axis);
                var step = rightIsOutward.Dot(point - Apex) > 0 ? 1 : -1;
                var coord2D = TransformFrom3DTo2D(point);
                var coord2Dx = coord2D.X;
                var horizRepeat =Math.Abs(coord2D.Y * Aperture * Constants.TwoPi);
                 while (coord2Dx * step < last2DVertex.X * step)
                    coord2Dx += step * horizRepeat;
                coord2D = new Vector2(coord2Dx, coord2D.Y);
                yield return coord2D;
                lastPoint = point;
                last2DVertex = coord2D;
            }
        }
    }
}