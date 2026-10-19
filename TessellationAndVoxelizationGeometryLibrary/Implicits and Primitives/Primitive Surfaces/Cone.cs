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
        internal double sinAperture;

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
            var transformedPerpXDir = perpXDir.IsNull()
                ? Vector3.Null
                : perpXDir.TransformNoTranslate(transformMatrix);
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
            perpXDir = Vector3.Null;
            if (!transformedPerpXDir.IsNull())
                PerpXDir = transformedPerpXDir;
            // this is the same procedure for how Radius is determined in the cylinder
            // transform. Its like we've moved done the cone by 1 unit and the aperture 
            // is the radius at that cross-section
        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
            => TransformFrom3DTo2D([point]).First();

        /// <summary>
        /// Rolls a point from a developed cone back onto the cone using the same seam direction that was used
        /// to develop it.
        /// </summary>
        /// <param name="point">The point in the developed cone.</param>
        /// A direction perpendicular to <see cref="Axis"/> that points from the axis toward the seam.
        /// </param>
        /// <returns>The corresponding point on the cone.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = Math.Atan2(point.Y, point.X) / sinAperture;
            var distanceDownCone = point.Length();
            var radius = sinAperture * distanceDownCone;
            var height = radius / Aperture;
            var result = Apex + height * Axis;
            result += radius * Math.Cos(angle) * SectorCenterDirection;
            result += radius * Math.Sin(angle) * SectorYDirection;
            return result;
        }


        /// <summary>
        /// Gets or sets the radial direction at the center of the physical seam in the developed cone.
        /// The occupied sector is centered in the opposite direction.
        /// </summary>
        public Vector3 PerpXDir
        {
            get
            {
                if (perpXDir.IsNull())
                    perpXDir = Axis.GetPerpendicularDirection();
                return perpXDir;
            }
            set
            {
                perpXDir = value.Normalize();
            }
        }
        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 perpXDir = Vector3.Null;

        private Vector3 SectorCenterDirection => -PerpXDir;

        private Vector3 SectorYDirection => Axis.Cross(SectorCenterDirection).Normalize();


        /// <summary>
        /// Develops an ordered path on the cone onto a plane without breaking the path when it crosses the
        /// <see cref="Math.Atan2(double, double)"/> branch cut. Successive points are used to determine which
        /// revolution of the developed cone each point belongs to, so the result can span any number of turns.
        /// </summary>
        /// <param name="points">The ordered points on the cone.</param>
        /// <returns>The points on the developed (flattened) cone.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
        {
            // Cut the cone to its center and lay it flat. A full 2*pi turn around the 3D
            // cone occupies only 2*pi*sin(apertureAngle) radians in that flat sector. Instead of
            // repeating every 2*pi like a cylinder, the sector repeats every 2*pi*sin(apertureAngle).
            var repeatAngle = Math.Abs(sinAperture) * Math.Tau;
            var halfRepeatAngle = 0.5 * repeatAngle;

            // Atan2 returns only its principal angle, so the raw developed angle below always lies on the first
            // copy of the sector. previousUnwrappedAngle remembers which repeated copy the path actually reached.
            // It starts as NaN because the first point establishes the arbitrary starting revolution.
            var previousUnwrappedAngle = double.NaN;
            foreach (var pt in points)
            {
                // In the developed cone, distance from the apex is the slant distance. Developing the cone is an
                // isometry, so this radial coordinate is simply the length of the 3D apex-to-point vector.
                var pointOnCone = ClosestPointOnSurfaceToPoint(pt);
                var v = pointOnCone - Apex;
                var distanceDownCone = v.Length();
                // Find the point's azimuth around the 3D cone. Multiplying that azimuth by sinAperture compresses
                // one full 3D revolution into the angular width of the developed sector described above.
                var x = SectorCenterDirection.Dot(v);
                var y = SectorYDirection.Dot(v);
                var unwrappedAngle = Math.Atan2(y, x) * sinAperture; // recall that Atan2 returns -pi to +pi,
                                                                     // sinAperture attenuates this so that a gap
                                                                     // or seam is left...centered at 180 degrees
                if (!double.IsNaN(previousUnwrappedAngle))
                {
                    // Select the equivalent angle on the repeated sector that is closest to the preceding point.
                    // Crossing the Atan2 seam makes the raw angle jump by one repeatAngle; these loops remove that
                    // artificial jump. They deliberately use "while", rather than "if": after several turns the
                    // previous angle can be several repeated sectors away from the principal Atan2 result.
                    while (unwrappedAngle - previousUnwrappedAngle > halfRepeatAngle)
                        unwrappedAngle -= repeatAngle;
                    while (previousUnwrappedAngle - unwrappedAngle > halfRepeatAngle)
                        unwrappedAngle += repeatAngle;
                }
                // Convert the unwrapped polar coordinates to ordinary Cartesian coordinates in the flat plane.
                // As with any sampled angular path, this assumes adjacent points are less than half a revolution
                // apart; otherwise the intended direction between those two samples is inherently ambiguous.
                yield return new Vector2(distanceDownCone * Math.Cos(unwrappedAngle),
                    distanceDownCone * Math.Sin(unwrappedAngle));
                previousUnwrappedAngle = unwrappedAngle;
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
            if (IsPositive.GetValueOrDefault(true)) return outwardVector;
            else return -outwardVector;
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
            if (IsPositive.GetValueOrDefault(true)) return d;
            else return -d;
        }

        public override Vector3 ClosestPointOnSurfaceToPoint(Vector3 point)
        {
            var v = point - Apex;
            var outOfPlane = v.Cross(Axis).Normalize();
            var vertDir = Axis.Cross(outOfPlane).Normalize();
            var onSurfDir = (Axis + Aperture * vertDir).Normalize();
            // onSurfDir is now the vector on the cone that is in the same plane as the point.
            // find the closest point on the line defined by this vector (and the Apex as anchor)
            // to the point
            return Apex + ((point - Apex).Dot(onSurfDir)) * onSurfDir;
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
