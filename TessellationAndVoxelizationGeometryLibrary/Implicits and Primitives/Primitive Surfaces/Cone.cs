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
        /// <summary>
        /// To fulfill the necessary override, we call a much more involved method that takes in
        /// 3D polygons and returns dictionaries and polygons. Here it is assumed that the input
        /// points are not a closed path.
        /// <param name="points">The ordered points on the cone.</param>
        /// <returns>The points on the developed (flattened) cone.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
          => TransformFrom3DTo2D([new Polygon3D(points, false)], out _, out _).First().Path;

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
            => TransformPoint(point, Math.Abs(sinAperture) * Math.Tau, Math.Abs(sinAperture) * Math.PI, out _, out _);

        /// <summary>
        /// Create polygons that are unrolled versions of the 3D polygons on the cone and link each source 3D point
        /// to the actual <see cref="Vertex2D"/> retained by the resulting polygon.
        /// </summary>
        /// <param name="cone">The cone on which the point sets lie.</param>
        /// <param name="pointSets">The ordered 3D paths to develop.</param>
        /// <param name="IsClosedSet">Whether each corresponding point set is closed.</param>
        /// <param name="pointToVertexMap">
        /// Maps each source 3D point to its resulting polygon vertex. If polygon tolerance combines source points,
        /// each combined source point maps to the same retained vertex.
        /// </param>
        /// <param name="newVertexSpacing">Spacing for vertices added along the cone seam.</param>
        /// <returns>The developed polygons and their enclosure/seam metadata.</returns>
        public List<Polygon> TransformFrom3DTo2D(IEnumerable<Polygon3D> polygon3Ds,
            out Dictionary<Vertex2D, Vertex2D> MatchingSeamVertices,
            out Dictionary<Vector3, Vertex2D> VertexMapping, double newVertexSpacing = -1.0)
        {
            var repeatAngle = Math.Abs(sinAperture) * Math.Tau;
            var halfRepeatAngle = 0.5 * repeatAngle;
            var allResults = new List<Polygon>();
            var properPolygonTrees = new List<Polygon>();
            var sortedStraddleHoles = new SortedList<double, (List<Vertex2D> vertex2Ds, int loopID)>();
            VertexMapping = new Dictionary<Vector3, Vertex2D>();
            MatchingSeamVertices = new Dictionary<Vertex2D, Vertex2D>();
            var encirclingLoops = new SortedList<double, (List<Vertex2D> vertex2Ds, bool windsPositive, int loopID)>();
            var transform2D = Axis.TransformToXYPlane(out var backTransform);
            var loopID = 0;
            foreach (var poly3 in polygon3Ds)
            {
                bool? isClosed = null;
                foreach (var path in poly3.AllPaths())
                {
                    if (!isClosed.HasValue) isClosed = poly3.IsClosed;
                    else isClosed = true;
                    var windingAngle = MiscFunctions.FindWindingAroundAxis(path, transform2D, Apex, out var minAngle,
                        out var maxAngle, isClosed.Value);
                    var encirclesAxis = isClosed.Value && Math.Abs(windingAngle) > 1.67 * Math.PI;

                    // there are 3 types of results:
                    // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                    // 2. a closed path that does encircle the axis - this will be an open polygon where the
                    //    ends are offset by 2π*Radius in the x-direction
                    // 3. an open path - this will be an open polygon
                    var transformedPoints = TransformPoints(path, repeatAngle, halfRepeatAngle, out minAngle, out maxAngle);
                    var pointList2D = new Vertex2D[path.Count].ToList();
                    for (int i = 0; i < pointList2D.Count; i++)
                    {
                        var point3D = path[i];
                        var vertex2D = new Vertex2D(transformedPoints[i], i, loopID);
                        pointList2D[i] = vertex2D;
                        VertexMapping.Add(point3D, vertex2D);
                    }
                    // there are 3 types of results:
                    // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                    // 2. a closed path that does encircle the axis - this will be an open polygon where the
                    //    ends are offset by 2π*Radius in the x-direction
                    // 3. an open path - this will be an open polygon
                    if (encirclesAxis)
                        // store for later in a sorted list by the lower of the endpoint y-values
                        encirclingLoops.Add(Math.Min(pointList2D[0].Coordinates.LengthSquared(),
                            pointList2D[^1].Coordinates.LengthSquared()), (pointList2D, windingAngle > 0, loopID));
                    else if (isClosed.Value)
                    {   // closed but not encircling => these are holes.
                        // maxAngle cannot be greater than Pi (or less than -Pi), but minAngle can be less than -Pi. If it is,
                        // then so, then the polygon straddles the seam
                        if (minAngle < -halfRepeatAngle)
                        {
                            var rot2D = Matrix3x3.CreateRotation(repeatAngle);
                            for (int i = 0; i < pointList2D.Count; i++)
                                pointList2D[i].Coordinates = pointList2D[i].Coordinates.Transform(rot2D);

                            sortedStraddleHoles.Add(pointList2D.Min(p => p.Coordinates.LengthSquared()),
                                (pointList2D, loopID));
                        }
                        if (maxAngle > halfRepeatAngle)
                        {
                            sortedStraddleHoles.Add(pointList2D.Min(p => p.Coordinates.LengthSquared()),
                                (pointList2D, loopID));
                        }
                        else
                            properPolygonTrees.Add(new Polygon(pointList2D, loopID, true));
                    }
                    else // hopeless open path, so we will return it now
                        allResults.Add(new Polygon(pointList2D, loopID, false));
                    loopID++;
                }
            }

            // now to handle the encircling loops. They are sorted by distance from the apex, so that the innermost loop is first.
            if (encirclingLoops.Count > 0)
            {
                var straddleHoleWorkingIndex = 0;
                if (!encirclingLoops.Values[0].windsPositive)
                {   // loop encloses infinite fat end of the cone. Not great, but throw it in the allResults list and move on. 
                    allResults.Add(new Polygon(encirclingLoops.Values[0].vertex2Ds, encirclingLoops.Values[0].loopID, true));
                    encirclingLoops.RemoveAt(0);
                }
                if (encirclingLoops.Count == 1) // && windsPositive is true
                {   // then loop is enclosing the apex of the cone 
                    var straddleHoles = GetStraddlingHoles(sortedStraddleHoles,
                        ref straddleHoleWorkingIndex, 0, encirclingLoops.Keys[0]);
                    Polygon p = MakeConePolygon(Array.Empty<Vertex2D>(),encirclingLoops.Values[0].loopID,
                        encirclingLoops.Values[0].vertex2Ds, repeatAngle,
                        halfRepeatAngle, newVertexSpacing, straddleHoles, MatchingSeamVertices, VertexMapping);
                    properPolygonTrees.Add(p);
                    encirclingLoops.RemoveAt(0);
                }
                // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
                for (int i = 1; i < encirclingLoops.Count; i += 2)
                {
                    var innerLoop = encirclingLoops.Values[i - 1];
                    var outerLoop = encirclingLoops.Values[i];
                    var straddleHoles = GetStraddlingHoles(sortedStraddleHoles,
                        ref straddleHoleWorkingIndex, encirclingLoops.Keys[i - 1], encirclingLoops.Keys[i]);
                    Polygon p = MakeConePolygon(innerLoop.vertex2Ds,innerLoop.loopID, outerLoop.vertex2Ds,
                        repeatAngle, halfRepeatAngle, newVertexSpacing, straddleHoles, MatchingSeamVertices, VertexMapping);
                    properPolygonTrees.Add(p);
                }
                // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
                if (int.IsOddInteger(encirclingLoops.Count))
                    allResults.Add(new Polygon(encirclingLoops.Values[^1].vertex2Ds, encirclingLoops.Values[^1].vertex2Ds[0].LoopID, false));
            }
            properPolygonTrees = properPolygonTrees.CreateShallowPolygonTrees(false);
            // now we need to resolve the straddling holes. They are holes that straddle the seam, so they need to be split into
            // two pieces, one on each side of the seam. The two pieces will be connected by a seam edge, and the two pieces will
            // be added to the properPolygonTrees list.
            allResults.AddRange(properPolygonTrees);
            return allResults;
        }

        private Vector2 TransformPoint(Vector3 pt, double repeatAngle,
            double halfRepeatAngle, out double radius, out double angle)
        {
            // In the developed cone, distance from the apex is the slant distance. Developing the cone is an
            // isometry, so this radial coordinate is simply the length of the 3D apex-to-point vector.
            var pointOnCone = ClosestPointOnSurfaceToPoint(pt);
            var v = pointOnCone - Apex;
            radius = v.Length();
            // Find the point's azimuth around the 3D cone. Multiplying that azimuth by sinAperture compresses
            // one full 3D revolution into the angular width of the developed sector described above.
            var x = SectorCenterDirection.Dot(v);
            var y = SectorYDirection.Dot(v);
            angle = Math.Atan2(y, x) * sinAperture; // recall that Atan2 returns -pi to +pi,
                                                    // sinAperture attenuates this so that a gap
                                                    // or seam is left...centered at 180 degrees
            return new Vector2(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }
        private Vector2[] TransformPoints(IList<Vector3> points, double repeatAngle,
            double halfRepeatAngle, out double minAngle, out double maxAngle)
        {
            minAngle = double.PositiveInfinity;
            maxAngle = double.NegativeInfinity;
            var result = new Vector2[points.Count];
            var prevAngle = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                var this2DPoint = TransformPoint(points[i], repeatAngle, halfRepeatAngle,
                    out var radius, out var angle);
                var angleAltered = false;
                while (angle - prevAngle > halfRepeatAngle)
                {
                    angle -= repeatAngle;
                    angleAltered = true;
                }
                while (prevAngle - angle > halfRepeatAngle)
                {
                    angle += repeatAngle;
                    angleAltered = true;
                }
                if (minAngle > angle) minAngle = angle;
                if (maxAngle < angle) maxAngle = angle;
                if (angleAltered)
                    result[i] = new Vector2(angle * radius, this2DPoint.Y);
                else result[i] = this2DPoint;
                prevAngle = angle;
            }
            return result;
        }




        private static Polygon MakeConePolygon(IList<Vertex2D> innerLoop,int loopID IList<Vertex2D> outerLoop, double repeatAngle,
            double halfRepeatAngle, double newVertexSpacing, List<(List<Vertex2D> vertex2Ds, int loopID)> straddleHoles,
            Dictionary<Vertex2D, Vertex2D> matchingSeamVertices, Dictionary<Vector3, Vertex2D> vertexMapping)
        {
            var cosGap = Math.Cos(halfRepeatAngle);
            var sinGap = Math.Sin(halfRepeatAngle);
            double rmsLength = (newVertexSpacing > 0) ? newVertexSpacing :
                Cylinder.GetRootMeanSquareEdgeLength(innerLoop, outerLoop);
            if (double.IsNaN(newVertexSpacing))
                newVertexSpacing = double.MaxValue;
            else if (newVertexSpacing <= 0)
                newVertexSpacing = rmsLength;
            var innerLoopWasEmpty = innerLoop.Count == 0;
            if (innerLoopWasEmpty)
                innerLoop = [new Vertex2D(Vector2.Zero, 0, loopID)];
            else
                ArrangeConeLoopAtSeam(innerLoop, shouldIncrease: false);

            ArrangeConeLoopAtSeam(outerLoop, shouldIncrease: true);
            // The closing edge of each arranged loop is the edge cut by the seam. Intersect that edge with one
            // sector border and use the same radius for both copies. This creates true duplicate 3D points instead
            // of projecting the two neighboring samples independently onto the two borders.
            var outerSeamRadius = GetConeSeamRadius(outerLoop, repeatAngle, halfRepeatAngle, isIncreasing: true);
            var outerTop = new Vector2(outerSeamRadius * cosGap, outerSeamRadius * sinGap);
            var outerBtm = new Vector2(outerSeamRadius * cosGap, -outerSeamRadius * sinGap);
            outerLoop.Insert(0, outerBtm);

            Vector2 innerTop;
            Vector2 innerBtm;
            if (innerLoopWasEmpty)
            {
                innerTop = innerLoop[0];
                innerBtm = innerLoop[^1];
            }
            else
            {
                var innerSeamRadius = GetConeSeamRadius(innerLoop, repeatAngle, halfRepeatAngle,
                    isIncreasing: false);
                innerTop = new Vector2(innerSeamRadius * cosGap, innerSeamRadius * sinGap);
                innerBtm = new Vector2(innerSeamRadius * cosGap, -innerSeamRadius * sinGap);
                innerLoop.Insert(0, innerTop);
                innerLoop.Add(innerBtm);
            }

            var polygonPoints = new List<Vertex2D> { new Vertex2D(outerTop, -1, outerLoopID) };
            var topDistance = outerTop.Distance(innerTop);
            var btmDistance = outerBtm.Distance(innerBtm);
            var numPointsToAdd = Math.Max(1,
                (int)Math.Ceiling(Math.Max(topDistance, btmDistance) / newVertexSpacing));
            for (int j = 1; j < numPointsToAdd; j++)
            {
                var fraction = (double)j / numPointsToAdd;
                polygonPoints.Add(new Vertex2D(outerTop + fraction * (innerTop - outerTop), -1,
                    outerLoopID));
            }
            numRepeats = polygonPoints.Count + 1;
            polygonPoints.AddRange(innerLoop.Select(p => new Vertex2D(p, -1, innerLoopID)));
            indexMatchedToStart = polygonPoints.Count;
            for (int j = 1; j < numPointsToAdd; j++)
            {
                var fraction = (double)j / numPointsToAdd;
                polygonPoints.Add(new Vertex2D(innerBtm + fraction * (outerBtm - innerBtm), -1,
                    innerLoopID));
            }
            polygonPoints.AddRange(outerLoop.Select(p => new Vertex2D(p, -1, outerLoopID)));
            //Presenter.ShowAndHang(new Polygon(polygonPoints, isClosed: true));
            return new Polygon(polygonPoints, outerLoopID, true);
        }
        private static List<(List<Vertex2D> vertex2Ds, int loopID)> GetStraddlingHoles(SortedList<double, (List<Vertex2D> vertex2Ds, int loopID)> straddlingHoles,
            ref int workingIndex, double lowR, double highR)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
