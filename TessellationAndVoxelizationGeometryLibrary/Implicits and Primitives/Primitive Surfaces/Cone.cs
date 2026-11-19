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
        /// Rolls a point from a developed cone back onto the cone using the same seam direction that was used
        /// to develop it.
        /// </summary>
        /// <param name="point">The point in the developed cone.</param>
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


        #region Flattening/Unrolling/Developing the cone
        /// <summary>Develops an open path continuously across equivalent cone sectors.</summary>
        /// <param name="points">The 3D points in traversal order.</param>
        /// <returns>Developed Cartesian points with successive angles unwrapped across the sector boundary.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
            => DevelopPath(points, Math.Abs(sinAperture) * Math.Tau)
                .Select(point => FromPolar(point.X, point.Y));

        /// <summary>Projects a point onto the cone and develops it into the principal sector.</summary>
        /// <param name="point">The 3D point to develop.</param>
        /// <returns>Cartesian coordinates in the developed cone, with the apex at the origin.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var polar = GetDevelopedPolarPoint(point);
            return FromPolar(polar.X, polar.Y);
        }

        /// <summary>Develops cone outlines and their holes into polygons that can be triangulated in 2D.</summary>
        /// <param name="polygon3Ds">The input outlines, including any holes grouped with each outline.</param>
        /// <param name="MatchingSeamVertices">
        /// A two-way map between distinct retained vertices that represent the same 3D point on the
        /// artificial cut. The two vertices can belong to different output polygons. A single
        /// shared apex has no pair.
        /// </param>
        /// <param name="VertexMapping">
        /// Maps each retained original 3D point to one of its actual vertex instances in the output.
        /// Use <paramref name="MatchingSeamVertices"/> to find its other copy, if present.
        /// </param>
        /// <param name="newVertexSpacing">
        /// Maximum spacing for subdivisions along shared seam intervals. A nonpositive value uses
        /// the root mean square edge length of the winding rings, when available.
        /// Use <c>double.NaN</c> to keep seam breakpoints without adding spacing-based subdivisions.
        /// </param>
        /// <param name="cutStandaloneOutlines">
        /// True to cut each standalone closed outline into the fixed sector. False to let independent
        /// contours form bands or stay continuous. Outlines with explicit holes are handled together.
        /// </param>
        /// <returns>
        /// Developed polygon pieces with any enclosed holes attached, together with continuously developed open paths.
        /// A hole crossing the seam becomes part of the cut boundary.
        /// </returns>
        /// <remarks>
        /// The fixed sector spans one developed revolution centered about the positive 2D X axis.
        /// Its two rays meet at the apex and represent the same physical line in the direction of
        /// <see cref="PerpXDir"/> around the cone. Matching seam points have equal slant radius.
        /// </remarks>
        public List<Polygon> TransformFrom3DTo2D(IEnumerable<Polygon3D> polygon3Ds,
            out Dictionary<Vertex2D, Vertex2D> MatchingSeamVertices,
            out Dictionary<Vector3, Vertex2D> VertexMapping, double newVertexSpacing = -1.0,
            bool cutStandaloneOutlines = false)
        {
            MatchingSeamVertices = new();
            VertexMapping = new();
            var result = new List<Polygon>();
            // Scale physical azimuth by the sine of the cone's half-angle to get developed angle.
            var period = Math.Abs(sinAperture) * Math.Tau;
            var half = period / 2;
            var frame = Axis.TransformToXYPlane(out _);
            var loopID = 0;
            foreach (var (batch, grouped) in Cylinder.DevelopmentBatches(polygon3Ds, cutStandaloneOutlines))
            {
                var rings = new List<(List<Vertex2D> vertices, bool enclosesApex)>();
                var contours = new List<(int index, Vector2[] polar, IList<Vector3> source, bool hole)>();
                var sourcePoints = new List<(Vector3 source, Vector2 point)>();
                var ringSources = new List<(Vector3 source, Vertex2D vertex)>();
                var pieces = new List<Polygon>();
                var loose = new List<Polygon>();
                var cutters = new List<Polygon>();
                foreach (var outline in batch)
                {
                    var hole = false;
                    foreach (var path in outline.AllPaths())
                    {
                        var index = loopID++;
                        var closed = hole || outline.IsClosed;
                        if (path.Count == 0) { hole = true; continue; }
                        var polar = DevelopPath(path, period).ToArray();
                        var points = polar.Select(point => FromPolar(point.X, point.Y)).ToArray();
                        if (!closed)
                        {
                            Cylinder.AddOpenPath(path, points, index, result, VertexMapping);
                        }
                        else
                        {
                            var winding = path.FindWindingAroundAxis(frame, Apex, out _, out _);
                            if (Math.Abs(winding) > 1.67 * Math.PI)
                            {
                                var ring = polar.Select((point, i) =>
                                    new Vertex2D(FromPolar(WrapAngle(point.X, period), point.Y), i, index)).ToList();
                                ringSources.AddRange(path.Zip(ring));
                                rings.Add((ring, (winding > 0) == IsPositive.GetValueOrDefault(true)));
                            }
                            else
                                contours.Add((index, polar, path, hole));
                        }
                        hole = true;
                    }
                }

                // Winding rings bound radial bands. When the region includes the apex, its
                // innermost ring bounds a filled sector and the remaining rings pair as bands.
                rings = rings.OrderBy(ring => ring.vertices.Average(vertex => vertex.Coordinates.LengthSquared())).ToList();
                var firstRing = 0;
                if (rings.Count > 0 && rings.Count % 2 != 0 && (grouped || rings[0].enclosesApex))
                {
                    pieces.Add(MakeConePolygon([], rings[0].vertices, period));
                    firstRing = 1;
                }
                for (; firstRing + 1 < rings.Count; firstRing += 2)
                    pieces.Add(MakeConePolygon(rings[firstRing].vertices, rings[firstRing + 1].vertices, period));
                if (firstRing < rings.Count)
                {
                    var last = rings[firstRing].vertices;
                    loose.Add(new Polygon(last, last[0].LoopID));
                }
                sourcePoints.AddRange(ringSources.Select(pair => (pair.source, pair.vertex.Coordinates)));

                var overlap = Cylinder.SeamTolerance(pieces) * 4;
                foreach (var (index, polar, source, hole) in contours)
                {
                    if (!grouped && pieces.Count == 0)
                    {
                        var points = polar.Select(point => FromPolar(point.X, point.Y)).ToArray();
                        loose.Add(new Polygon(points, index));
                        sourcePoints.AddRange(source.Zip(points));
                        continue;
                    }
                    // Use exact sector fragments for placement. Expand cutters across the
                    // seam slightly so that polygon rounding cannot close off the hole there.
                    var fragments = CutConeContour(polar, period, index, 0);
                    if (grouped && !hole)
                    {
                        pieces.AddRange(fragments);
                        overlap = Cylinder.SeamTolerance(pieces) * 4;
                    }
                    else if (grouped || Cylinder.IsInsideBand(pieces, fragments))
                        cutters.AddRange(CutConeContour(polar, period, index, overlap));
                    else
                    {
                        var points = polar.Select(point => FromPolar(point.X, point.Y)).ToArray();
                        loose.Add(new Polygon(points, index));
                        sourcePoints.AddRange(source.Zip(points));
                        continue;
                    }
                    // Either copy of a source point on the seam may survive clipping.
                    for (var i = 0; i < polar.Length; i++)
                    {
                        var angle = WrapAngle(polar[i].X, period);
                        sourcePoints.Add((source[i], FromPolar(angle, polar[i].Y)));
                        if (Math.Abs(Math.Abs(angle) - half) * polar[i].Y <= overlap)
                            sourcePoints.Add((source[i], FromPolar(angle < 0 ? half : -half, polar[i].Y)));
                    }
                }
                var spacing = newVertexSpacing <= 0 && rings.Count > 0
                    ? Cylinder.GetRootMeanSquareEdgeLength<Vertex2D>(rings.Select(ring => ring.vertices))
                    : newVertexSpacing;
                var rays = new[] { FromPolar(-half, 1), FromPolar(half, 1) };
                result.AddRange(Cylinder.CompleteSeamCuts(pieces, cutters, sourcePoints, spacing,
                    (side, radius) => rays[side] * radius, (point, side) => Math.Max(0, point.Dot(rays[side])),
                    MatchingSeamVertices, VertexMapping));
                var looseTrees = loose.CreateShallowPolygonTrees(false);
                Cylinder.AddRetainedSourceVertices(looseTrees, sourcePoints, Cylinder.SeamTolerance(looseTrees), VertexMapping);
                result.AddRange(looseTrees);
            }
            return result;
        }

        private Vector2 GetDevelopedPolarPoint(Vector3 point)
        {
            var v = ClosestPointOnSurfaceToPoint(point) - Apex;
            var angle = Math.Atan2(SectorYDirection.Dot(v), SectorCenterDirection.Dot(v)) * sinAperture;
            return new Vector2(angle, v.Length());
        }

        private IEnumerable<Vector2> DevelopPath(IEnumerable<Vector3> points, double period)
        {
            var previous = double.NaN;
            foreach (var source in points)
            {
                var polar = GetDevelopedPolarPoint(source);
                var angle = polar.X;
                // Choose the equivalent angle nearest the previous unwrapped angle.
                if (double.IsFinite(previous) && period > 0)
                    angle -= period * Math.Round((angle - previous) / period);
                previous = angle;
                yield return new Vector2(angle, polar.Y);
            }
        }

        private static double WrapAngle(double angle, double period) =>
            angle - period * Math.Floor(angle / period + 0.5);

        /// <summary>Joins winding rings along the sector rays to form a band or a filled sector.</summary>
        /// <param name="inner">The inner ring, or an empty list if the sector reaches the apex.</param>
        /// <param name="outer">The outer ring. Both nonempty rings are reordered in place at the seam.</param>
        /// <param name="period">The developed angle for one revolution around the cone.</param>
        /// <returns>A positive polygon in the fixed sector.</returns>
        private static Polygon MakeConePolygon(List<Vertex2D> inner, List<Vertex2D> outer, double period)
        {
            var half = period / 2;
            ArrangeConeLoopAtSeam(outer, true);
            var outerRadius = GetConeSeamRadius(outer, period, true);
            var loop = new List<Vector2> { FromPolar(half, outerRadius) };
            if (inner.Count == 0)
                loop.Add(Vector2.Zero);
            else
            {
                ArrangeConeLoopAtSeam(inner, false);
                var innerRadius = GetConeSeamRadius(inner, period, false);
                loop.Add(FromPolar(half, innerRadius));
                loop.AddRange(inner.Select(vertex => vertex.Coordinates));
                loop.Add(FromPolar(-half, innerRadius));
            }
            loop.Add(FromPolar(-half, outerRadius));
            loop.AddRange(outer.Select(vertex => vertex.Coordinates));
            return new Polygon(loop, outer[0].LoopID) { IsPositive = true };
        }

        private static void ArrangeConeLoopAtSeam(List<Vertex2D> loop, bool increasing)
        {
            (int index, double delta) FindJump() => Enumerable.Range(0, loop.Count)
                .Select(i => (index: i, delta: Angle(loop[(i + 1) % loop.Count]) - Angle(loop[i])))
                .MaxBy(jump => Math.Abs(jump.delta));
            // Put the wrap jump on the closing edge so the rest of the ring can be
            // traversed in order between the two seam rays.
            var jump = FindJump();
            if ((jump.delta < 0) != increasing)
            {
                loop.Reverse();
                jump = FindJump();
            }
            var first = (jump.index + 1) % loop.Count;
            if (first == 0) return;
            var beforeSeam = loop.GetRange(0, first);
            loop.RemoveRange(0, first);
            loop.AddRange(beforeSeam);
        }

        private static double Angle(Vertex2D point) => Math.Atan2(point.Y, point.X);

        private static double GetConeSeamRadius(List<Vertex2D> loop, double period, bool increasing)
        {
            var sign = increasing ? 1 : -1;
            var ray = FromPolar(sign * period / 2, 1);
            var last = loop[^1].Coordinates;
            // Rejoin the closing edge in the same sector as its last point before intersecting the ray.
            var equivalentFirst = FromPolar(Angle(loop[0]) + sign * period, loop[0].Coordinates.Length());
            var edge = equivalentFirst - last;
            var divisor = edge.Cross(ray);
            return divisor.IsNegligible() ? last.Dot(ray) :
                (last - last.Cross(ray) / divisor * edge).Dot(ray);
        }

        private static List<Polygon> CutConeContour(Vector2[] polar, double period, int index, double overlap)
        {
            if (polar.Length < 3) return [];
            var half = period / 2;
            var first = (int)Math.Floor((polar.Min(point => point.X) + half) / period);
            var last = (int)Math.Floor((polar.Max(point => point.X) + half) / period);
            var high = Math.Max(1, polar.Max(point => point.Y) * 2);
            // Clip in unwrapped angle and slant radius. In Cartesian coordinates another turn can
            // overlap the fixed sector, especially when the developed angle is wider than half a circle.
            var chart = new Polygon(polar, index) { IsPositive = true };
            var pieces = new List<Polygon>();
            for (var turn = first; turn <= last; turn++)
            {
                var shift = turn * period;
                var lowAngle = shift - half;
                var highAngle = shift + half;
                IEnumerable<Polygon> fragments = first == last ? [chart] :
                    chart.Intersect(new Polygon(new Vector2[] { new(lowAngle, -1), new(highAngle, -1),
                        new(highAngle, high), new(lowAngle, high) }));
                foreach (var fragment in fragments)
                {
                    var points = fragment.Path.Select(point =>
                    {
                        var side = Math.Abs(point.X - lowAngle) <= 1e-6 ? -1 :
                            Math.Abs(point.X - highAngle) <= 1e-6 ? 1 : 0;
                        if (side == 0) return FromPolar(point.X - shift, point.Y);
                        var angle = side * half;
                        var radius = ConeCutRadius(polar, shift + angle, point.Y);
                        // Land past the seam so independent polygon rounding cannot leave a sliver.
                        return FromPolar(angle, radius) + side * FromPolar(angle + Math.PI / 2, overlap);
                    });
                    pieces.Add(new Polygon(points, index) { IsPositive = true });
                }
            }
            return pieces;
        }

        private static Vector2 FromPolar(double angle, double radius) =>
            new(radius * Math.Cos(angle), radius * Math.Sin(angle));


        private static double ConeCutRadius(IList<Vector2> loop, double angle, double interpolatedRadius)
        {
            // The polar-chart intersection identifies the crossing edge. Its true developed
            // crossing is the intersection of the Cartesian chord with the seam ray.
            var closest = double.PositiveInfinity;
            var radius = interpolatedRadius;
            for (var i = 0; i < loop.Count; i++)
            {
                var a = loop[i];
                var b = loop[(i + 1) % loop.Count];
                if (angle < Math.Min(a.X, b.X) || angle > Math.Max(a.X, b.X) || a.X == b.X)
                    continue;
                var error = Math.Abs(a.Y + (b.Y - a.Y) * (angle - a.X) / (b.X - a.X) - interpolatedRadius);
                if (error >= closest) continue;
                var p = FromPolar(a.X - angle, a.Y);
                var q = FromPolar(b.X - angle, b.Y);
                if (p.Y == q.Y) continue;
                radius = p.X + (q.X - p.X) * p.Y / (p.Y - q.Y);
                closest = error;
            }
            return radius;
        }
        #endregion
    }
}
