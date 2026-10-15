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
        private Vector3 perpXDir = Vector3.Null;
        /// <summary>
        /// The face y dir
        /// </summary>
        private Vector3 perpYDir = Vector3.Null;

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = new Vector3(point.X, point.Y, point.Z) - Apex;
            if (perpXDir.IsNull())
            {
                perpXDir = Axis.GetPerpendicularDirection();
                perpYDir = perpXDir.Cross(Axis);
            }
            var distanceDownCone = v.Length();
            var x = perpXDir.Dot(v);
            var y = perpYDir.Dot(v);
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
            if (perpXDir.IsNull())
            {
                perpXDir = Axis.GetPerpendicularDirection();
                perpYDir = perpXDir.Cross(Axis);
            }
            var result = Apex + height * Axis;
            result += radius * Math.Sin(angle) * perpYDir;
            result += radius * Math.Sin(angle) * perpYDir;
            return result;
        }



        /// <summary>
        /// Develops an ordered path on the cone onto a plane without breaking the path when it crosses the
        /// <see cref="Math.Atan2(double, double)"/> branch cut. Successive points are used to determine which
        /// revolution of the developed cone each point belongs to, so the result can span any number of turns.
        /// </summary>
        /// <param name="points">The ordered points on the cone.</param>
        /// <returns>The points on the developed (flattened) cone.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
        {
            // perpXDir and perpYDir form the two-dimensional frame used to measure a point's angle around the
            // cone axis. They are fixed for this Cone instance, so every call uses the same angular seam.
            if (perpXDir.IsNull())
            {
                perpXDir = Axis.GetPerpendicularDirection();
                perpYDir = perpXDir.Cross(Axis);
            }

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
                var x = perpXDir.Dot(v);
                var y = perpYDir.Dot(v);
                var unwrappedAngle = Math.Atan2(y, x) * sinAperture;

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

        public List<Polygon> GetUnrolledPolygons(IEnumerable<IList<Vector3>> pointSets, IEnumerable<bool> IsClosedSet,
            out List<bool> open)
        {
            var result = new List<Polygon>();
            var polygons = new List<Polygon>();
            var encirclingLoops = new List<(double distance, List<Vector2> points, bool isPositive)>();
            open = new List<bool>();
            var isClosedEnumerator = IsClosedSet.GetEnumerator();
            var repeatAngle = Math.Abs(sinAperture) * Math.Tau;

            foreach (var pointList in pointSets)
            {
                var isClosed = isClosedEnumerator.MoveNext() ? isClosedEnumerator.Current : false;
                // there are 3 types of results:
                // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cone
                // 2. a closed path that does encircle the axis - this will be an open path where the
                //    ends are offset by the cone's repeat angle (2π*sin(apertureAngle)) in polar angle around the apex
                // 3. an open path - this will be an open polygon
                var pointList2D = TransformFrom3DTo2D(pointList).ToList();
                if (pointList2D.Count == 0) continue;

                // when the points are a closed path and they encircle the axis
                var windingAngle = Math.Abs(MiscFunctions.FindWindingAroundAxis(pointList, Axis, Apex, out _, out _, isClosed));
                var encirclesAxis = isClosed && windingAngle > 1.67 * Math.PI;
                if (encirclesAxis)
                {
                    // if the shape encircles the axis, then the first and last points should be the same in 3D, so we
                    // yield the first point again to close the path, BUT rotated by ±repeatAngle around the apex (the origin)
                    // so that the shape can be unrolled into a flat sector.
                    var totalAngle = 0.0;
                    var prevAngle = Math.Atan2(pointList2D[0].Y, pointList2D[0].X);
                    for (int i = 1; i < pointList2D.Count; i++)
                    {
                        var currentAngle = Math.Atan2(pointList2D[i].Y, pointList2D[i].X);
                        var dTheta = currentAngle - prevAngle;
                        while (dTheta > Math.PI) dTheta -= Math.Tau;
                        while (dTheta < -Math.PI) dTheta += Math.Tau;
                        totalAngle += dTheta;
                        prevAngle = currentAngle;
                    }
                    var isPositive = totalAngle >= 0;
                    var deltaAngle = isPositive ? repeatAngle : -repeatAngle;
                    var cos = Math.Cos(deltaAngle);
                    var sin = Math.Sin(deltaAngle);
                    var p0 = pointList2D[0];
                    pointList2D.Add(new Vector2(p0.X * cos - p0.Y * sin, p0.X * sin + p0.Y * cos));

                    // Use distance from the apex (length in 2D) to sort loops along the cone
                    encirclingLoops.Add((pointList2D[0].Length(), pointList2D, isPositive));
                }
                else if (isClosed)
                    polygons.Add(new Polygon(pointList2D, isClosed: true));
                else
                {
                    result.Add(new Polygon(pointList2D, isClosed: false));
                    open.Add(true);
                }
            }

            // now to handle the encircling loops
            var sortedLoops = encirclingLoops.OrderBy(l => l.distance).ToList();
            if (sortedLoops.Count > 0 && !sortedLoops[0].isPositive) // then loop is enclosing the apex of the cone and we consider it open
            {
                result.Add(new Polygon(sortedLoops[0].points, isClosed: true));
                open.Add(true);
                sortedLoops.RemoveAt(0);
            }
            for (int i = 1; i < sortedLoops.Count; i += 2)
            {
                var loopA = sortedLoops[i - 1].points;
                var loopB = sortedLoops[i].points;
                if (sortedLoops[i].isPositive == sortedLoops[i - 1].isPositive)
                    loopB = loopB.AsEnumerable().Reverse().ToList();

                var angleAEnd = Math.Atan2(loopA[^1].Y, loopA[^1].X);
                var angleBStart = Math.Atan2(loopB[0].Y, loopB[0].X);
                var angleDiff = angleAEnd - angleBStart;
                var k = (int)Math.Round(angleDiff / repeatAngle);
                if (k != 0)
                {
                    var rot = k * repeatAngle;
                    var c = Math.Cos(rot);
                    var s = Math.Sin(rot);
                    loopB = loopB.Select(p => new Vector2(p.X * c - p.Y * s, p.X * s + p.Y * c)).ToList();
                }
                polygons.Add(new Polygon(loopA.Concat(loopB), isClosed: true));
            }
            if (int.IsOddInteger(sortedLoops.Count))
            {
                result.Add(new Polygon(sortedLoops[^1].points, isClosed: true));
                open.Add(true);
            }
            foreach (var poly in polygons.CreateShallowPolygonTrees(false))
            {
                result.Add(poly);
                open.Add(false);
            }
            return result;
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
            return MiscFunctions.ClosestPointOnLineSegmentToPoint(Apex, Apex + onSurfDir, point);
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
