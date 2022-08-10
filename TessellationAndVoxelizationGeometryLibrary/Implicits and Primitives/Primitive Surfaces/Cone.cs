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
        /// Initializes a new instance of the <see cref="Cone"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cone(Cone originalToBeCopied, int[] newFaceIndices, TessellatedSolid copiedTessellatedSolid)
            : base(newFaceIndices, copiedTessellatedSolid)
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
        ///     like y = mx + b. aperture = tan(cone_angle)
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
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Axis = Axis.Multiply(transformMatrix);
            Apex = Apex.Multiply(transformMatrix);
            // we assume here that the scaling of the transform is the same
            // in all directions so that the circular cone is still
            // a circular cone and not an elliptical cone. Thus, 
            // we simply scale the radius by the ScaleX from the matrix
            Aperture *= transformMatrix.M11;
        }

        /// <summary>
        /// Calculates the error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var maxError = 0.0;
            var cosAperture = Math.Sqrt(1 / (1 + Aperture));
            foreach (var c in vertices)
            {
                var d = Math.Abs((c - Apex).Cross(Axis).Length()
                    - Math.Abs(Aperture * (c - Apex).Dot(Axis)));
                var error =  d * cosAperture;
                if (error > maxError)
                    maxError = error;
            }
            return maxError;
        }


        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = new Vector3(point.X, point.Y, point.Z) - Apex;
            if (faceXDir.IsNull())
            {
                faceXDir = Axis.GetPerpendicularDirection();
                faceYDir = faceXDir.Cross(Axis);
            }
            var distanceDownCone = v.Length();
            var x = faceXDir.Dot(v);
            var y = faceYDir.Dot(v);
            /* originally doing the following, which makes intuitive sense, but
             * since we take the cosine (and sine) of an inverse tangent, we can reduce the computation
            var angle = Math.Atan2(y, x) * betaFactor;
            return new Vector2(distanceDownCone * Math.Cos(angle), distanceDownCone * Math.Sin(angle));
        */
            var hypotenuse = Math.Sqrt(x * x + y * y);
            var cosAngle = betaFactor * x / hypotenuse;
            var sinAngle = betaFactor * y / hypotenuse;
            return new Vector2(distanceDownCone * cosAngle, distanceDownCone * sinAngle);
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = Math.Atan2(point.Y, point.X) / betaFactor;
            var distanceDownCone = point.Length();
            var radius = betaFactor * distanceDownCone;
            var height = radius / Aperture;
            if (faceXDir.IsNull())
            {
                faceXDir = Axis.GetPerpendicularDirection();
                faceYDir = faceXDir.Cross(Axis);
            }
            var result = Apex + height * Axis;
            result += radius * Math.Sin(angle) * faceYDir;
            result += radius * Math.Sin(angle) * faceYDir;
            return result;
        }

        private double betaFactor
        {
            get
            {
                if (double.IsNaN(_betaFactor))
                    _betaFactor = FlattenConeSpanningAngleFraction(Aperture);
                return _betaFactor;
            }
        }
        private double _betaFactor = double.NaN;
        private static double FlattenConeSpanningAngleFraction(double aperture)
        {
            // you know how you can make a cone by cutting an arc from flat stock
            // (paper, sheet metal, etc) and rolling it up? well what angle is that
            // flattened sheet of the full 360-degrees?
            // I call this angle, beta.
            // Visualize or draw out both the 3d cone and the flattened arc. 
            // The bottom of the cone (some perpendicular cut through the cone),
            // the base circle is the same as the outside of the arc and has a
            // length, c. This is equal to both:
            // c = beta * d (where d is the distance down the outside of the cone; 
            // note that d is Sqrt(h^2 + r^2) ),
            // c = 2*pi*r e.g. circumference of the circle at the bottom of the cone
            // here r is also aperture*height.
            // Equate the c equations and solve for beta. 
            // which reduces to h*sqrt(1+a^2).
            return aperture / Math.Sqrt(1 + aperture * aperture);
        }

        /// <summary>
        /// Transforms the from 3d points on the cone to a 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            // when the points are a closed path and they encircle the axis, then we define the resulting polygon
            // by looking down the axis of the cone
            if (pathIsClosed && BorderEncirclesAxis(points, Axis, Apex))
            {
                var transform = Axis.TransformToXYPlane(out _);
                foreach (var point in points)
                    yield return point.ConvertTo2DCoordinates(transform);
                yield break;
            }
            if (faceXDir.IsNull())
            {
                faceXDir = Axis.GetPerpendicularDirection();
                faceYDir = faceXDir.Cross(Axis);
            }
            // like a cylinder, we don't want to break the 2D shape just because it doesn't fit on our initial
            // 2D sheet. Therefore we need to continue the around the polar coordinates when you wrap
            // around the cone. This is done by keeping tack of the direction of movement from the previous point
            var halfRepeatAngle = betaFactor * Constants.TwoPi;
            // the first point is called the prevPoint, just to set up the following loop - so that the previous
            // visited point is always known when processing each subsequent point.
            var prevAngle = double.NaN;
            foreach (var point in points)
            {
                var v = new Vector3(point.X, point.Y, point.Z) - Apex;
                var distanceDownCone = v.Length();
                var x = faceXDir.Dot(v);
                var y = faceYDir.Dot(v);
                var angle = Math.Atan2(y, x) * betaFactor;
                if (!double.IsNaN(prevAngle))
                {
                    if (angle - prevAngle > halfRepeatAngle)
                        angle -= 2 * halfRepeatAngle;
                    else if (prevAngle - angle > halfRepeatAngle)
                        angle += halfRepeatAngle;
                }
                yield return new Vector2(distanceDownCone * Math.Cos(angle), distanceDownCone * Math.Sin(angle));
                prevAngle = angle;
            }
        }
    }
}