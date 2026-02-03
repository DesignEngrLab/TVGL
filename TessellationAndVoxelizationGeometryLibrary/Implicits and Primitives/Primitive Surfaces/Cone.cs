// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Cone.cs" company="Design Engineering Lab">
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
    /// The class for Cone primitives.
    /// </summary>
    public class Cone : PrimitiveSurface
    {
        public const double PracticalMinAperture = 0.001; // 0.06 degrees
        public const double PracticalMaxAperture = 1000; // 89.94 degrees

        /// <summary>
        /// Initializes a new instance of the <see cref="Cone"/> class.
        /// </summary>
        public Cone() { }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, bool isPositive)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            this.isPositive = isPositive;
        }
        /// <summary>
        /// Cone
        /// </summary>
        /// <param name="apex">The apex.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="aperture">The aperture.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces all.</param>
        public Cone(Vector3 apex, Vector3 axis, double aperture, IEnumerable<TriangleFace> faces)
        {
            Apex = apex;
            Axis = axis;
            Aperture = aperture;
            SetFacesAndVertices(faces);
        }

        /// <summary>
        /// Gets the aperture. This is a slope, like m, not an angle. It is dimensionless and NOT radians.
        /// like y = mx + b. aperture = tan(cone_angle) where cone_angle is measure from the axis to the cone
        /// if m is zero, then cone is a line(spike).
        /// if m is infinity, then cone is a plane.
        /// </summary>
        /// <value>The aperture.</value>
        public double Aperture
        {
            get { return aperture; }
            set
            {
                aperture = value;
                cosAperture = Math.Sqrt(1 / (1 + value * value));
                sinAperture = value * cosAperture;
            }
        }


        /// <summary>
        /// Gets or sets the maximum distance along the axis.
        /// </summary>
        /// <value>The maximum distance along axis.</value>
        public double Length { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// The aperture
        /// </summary>
        private double aperture;
        /// <summary>
        /// The cos aperture
        /// </summary>
        private double cosAperture;
        /// <summary>
        /// The sin aperture
        /// </summary>
        private double sinAperture;

        /// <summary>
        /// Gets the apex.
        /// </summary>
        /// <value>The apex.</value>
        public Vector3 Apex { get; set; }

        /// <summary>
        /// Gets the axis, which is a unit vector and points from the apex towards the 
        /// meaningful (triangles of the) surface - not away from.
        /// </summary>
        /// <value>The axis.</value>
        public Vector3 Axis { get; set; }


        public override string KeyString => "Cone|" + Axis.ToString() +
            "|" + Apex.ToString() + "|" + Aperture.ToString("F5") + GetCommonKeyDetails();


        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix, bool transformFacesAndVertices)
        {
            base.Transform(transformMatrix, transformFacesAndVertices);
            Apex = Apex.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = Aperture * Axis.Cross(rVector1);
            rVector1 *= Aperture;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            Aperture = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            // this is the same procedure for how Radius is determined in the cylinder
            // transform. Its like we've moved done the cone by 1 unit and the aperture 
            // is the radius at that cross-section
        }

        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 faceXDir = Vector3.Null;
        /// <summary>
        /// The face y dir
        /// </summary>
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
             * since we take the cosine (and sine) of an Inverse tangent, we can reduce the computation
            var angle = Math.Atan2(y, x) * betaFactor;
            return new Vector2(distanceDownCone * Math.Cos(angle), distanceDownCone * Math.Sin(angle));
        */
            var hypotenuse = Math.Sqrt(x * x + y * y);
            var cosAngle = sinAperture * x / hypotenuse;
            var sinAngle = sinAperture * y / hypotenuse;
            return new Vector2(distanceDownCone * cosAngle, distanceDownCone * sinAngle);
            // you know how you can make a cone by cutting an arc from flat stock
            // (paper, sheet metal, etc) and rolling it up? well what angle is that
            // flattened sheet of the full 360-degrees?
            // I call this angle, beta.
            // Visualize or draw out both the 3d cone and the flattened arc. 
            // The bottom of the cone (some perpendicular cut through the cone),
            // the base circle is the same as the outside of the arc and has a
            // length, c. This is equal to both:
            // c = beta * distAtCommonDepth (where distAtCommonDepth is the distance down the outside of the cone; 
            // note that distAtCommonDepth is Sqrt(h^2 + r^2) ),
            // c = 2*pi*r e.g. circumference of the circle at the bottom of the cone
            // here r is also aperture*height.
            // Equate the c equations and solve for beta. 
            // which reduces to h*sqrt(1+a^2).

            //it turns out that betaFactor is the same as the sin(aperture angle)
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = Math.Atan2(point.Y, point.X) / sinAperture;
            var distanceDownCone = point.Length();
            var radius = sinAperture * distanceDownCone;
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
            if (pathIsClosed && points.BorderEncirclesAxis(Axis, Apex))
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
            var halfRepeatAngle = sinAperture * Constants.TwoPi;
            // the first point is called the prevPoint, just to set up the following loop - so that the previous
            // visited point is always known when processing each subsequent point.
            var prevAngle = double.NaN;
            foreach (var point in points)
            {
                var v = new Vector3(point.X, point.Y, point.Z) - Apex;
                var distanceDownCone = v.Length();
                var x = faceXDir.Dot(v);
                var y = faceYDir.Dot(v);
                var angle = Math.Atan2(y, x) * sinAperture;
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



        /// <summary>
        /// Gets the normal at point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>A Vector3.</returns>
        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var a = (point - Apex);
            var b = a.Cross(Axis);
            var c = Axis.Cross(b).Normalize();  // outward from the axis to the point
            var outwardVector = (c - (Axis * Aperture)) / Math.Sqrt(1 + Aperture * Aperture);
            if (IsPositive.HasValue && !IsPositive.Value) outwardVector *= -1;
            return outwardVector;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var v = point - Apex;
            var distAtCommonDepth = v.Cross(Axis).Length() - Aperture * v.Dot(Axis);
            var d = distAtCommonDepth * cosAperture;
            if (IsPositive.HasValue && !IsPositive.Value) d = -d;
            return d;
        }

        public override Vector3 ClosestPointOnSurfaceToPoint(Vector3 point)
        {
            var v = point - Apex;
            var outOfPlane = v.Cross(Axis).Normalize();
            var vertDir = Axis.Cross(outOfPlane).Normalize();
            var onSurfDir = (Axis + Aperture * vertDir).Normalize();
            return MiscFunctions.ClosestPointOnLineSegmentToPoint(Apex, Apex + onSurfDir, point, out _);
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any() || Area.IsNegligible()) return;
            if ((LargestFace.Center - Apex).Dot(Axis) < 0)
                Axis *= -1;
            var innerRefPoint = Apex + (LargestFace.Center - Apex).Dot(Axis) * Axis;
            isPositive = (LargestFace.Center - innerRefPoint).Dot(LargestFace.Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            if (double.IsFinite(Length))
            {
                var top = Apex;
                var bottom = Apex + Length * Axis;
                var radius = Length * Aperture;
                var xFactor = Math.Sqrt(1 - Axis.X * Axis.X);
                var yFactor = Math.Sqrt(1 - Axis.Y * Axis.Y);
                var zFactor = Math.Sqrt(1 - Axis.Z * Axis.Z);

                MinX = Math.Min(top.X, bottom.X - xFactor * radius);
                MaxX = Math.Max(top.X, bottom.X + xFactor * radius);
                MinY = Math.Min(top.Y, bottom.Y - yFactor * radius);
                MaxY = Math.Max(top.Y, bottom.Y + yFactor * radius);
                MinZ = Math.Min(top.Z, bottom.Z - zFactor * radius);
                MaxZ = Math.Max(top.Z, bottom.Z + zFactor * radius);
            }
            else
            {
                MinX = MinY = MinZ = double.NegativeInfinity;
                MaxX = MaxY = MaxZ = double.PositiveInfinity;
            }
        }
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 p, Vector3 d)
        {
            d = d.Normalize();

            //var u = Axis; // expected to be unit length
            var pDa = p - Apex;

            // Handle the special case where the line passes through the apex.
            if (pDa.IsAlignedOrReverse(d, 1 - Constants.BaseTolerance))
            {
                yield return (Apex, -pDa.Dot(d));
                yield break;
            }

            var mSqd = Aperture * Aperture;
            var onePlusMSqd = 1.0 + mSqd;

            var dDotB = d.Dot(Axis); //using b instead of A (since 'a' is used for Apex)
            var pDaDotB = pDa.Dot(Axis);

            var a = 1 - onePlusMSqd * dDotB * dDotB;
            var b = 2.0 * (d.Dot(pDa) - onePlusMSqd * dDotB * pDaDotB);
            var c = pDa.LengthSquared() - onePlusMSqd * pDaDotB * pDaDotB;

            (var root1, var root2) = PolynomialSolve.Quadratic(a, b, c);

            if (!root1.IsRealNumber)
                yield break; // no need to check root2 - both are either real or imaginary

            if (!root1.Real.IsPracticallySame(root2.Real))
            {
                var iPt = p + root1.Real * d;
                if ((iPt - Apex).Dot(Axis) >= 0)
                    yield return (iPt, root1.Real);
                iPt = p + root2.Real * d;
                if ((iPt - Apex).Dot(Axis) >= 0)
                    yield return (iPt, root2.Real);
            }
            else
            {
                var iPt = p + root1.Real * d;
                if ((iPt - Apex).Dot(Axis) >= 0)
                    yield return (iPt, root1.Real);
            }

        }

    }
}