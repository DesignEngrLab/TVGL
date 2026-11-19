// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Cylinder.cs" company="Design Engineering Lab">
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
    /// The class for Cylinder primitives.
    /// </summary>
    public class Cylinder : PrimitiveSurface
    {
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
            Anchor = Anchor.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = Radius * Axis.Cross(rVector1);
            rVector1 *= Radius;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            Radius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            perpXDir = Vector3.Null;
            perpYDir = Vector3.Null;
            if (!transformedPerpXDir.IsNull())
                PerpXDir = transformedPerpXDir;
            // we currently don't allow the cylinder to be squished into an elliptical cylinder
            // so the radius is the average of the two radius component vectors after the 
            // transform. Earlier, we were doing 
            //Radius*=transformMatrix.M11;
            // but this is not correct since M11 is often non-unity during rotation
        }

        /// <summary>
        /// Gets or sets the radial direction represented by both joined boundaries of the developed cylinder.
        /// </summary>
        public Vector3 PerpXDir
        {
            get
            {
                if (perpXDir.IsNull())
                    perpXDir = Axis.GetPerpendicularDirection();
                else
                {
                    var axisDirection = Axis.Normalize();
                    perpXDir -= perpXDir.Dot(axisDirection) * axisDirection;
                    perpXDir = perpXDir.LengthSquared().IsNegligible()
                        ? Axis.GetPerpendicularDirection()
                        : perpXDir.Normalize();
                }
                return perpXDir;
            }
            set
            {
                if (Axis.LengthSquared().IsNegligible())
                {
                    perpXDir = value;
                    perpYDir = Vector3.Null;
                    return;
                }
                var axisDirection = Axis.Normalize();
                var radialDirection = value - value.Dot(axisDirection) * axisDirection;
                if (radialDirection.LengthSquared().IsNegligible())
                    throw new ArgumentException("The cylinder perpendicular X direction must be perpendicular to the cylinder axis.",
                        nameof(value));
                perpXDir = radialDirection.Normalize();
                perpYDir = Vector3.Null;
            }
        }
        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 perpXDir = Vector3.Null;

        private Vector3 PerpYDir
        {
            get
            {
                if (perpYDir.IsNull())
                    perpYDir = Axis.Cross(PerpXDir);
                return perpYDir;
            }
        }
        /// <summary>
        /// The face y dir
        /// </summary>
        private Vector3 perpYDir = Vector3.Null;


        #region Properties


        /// <summary>
        /// Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public Vector3 Anchor { get; set; }
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        private Circle _circle;
        /// <summary>
        /// Gets the circle.
        /// </summary>
        /// <value>The circle.</value>
        public Circle Circle
        {
            get
            {
                if (_circle.Area == 0)
                {
                    var transform = MiscFunctions.TransformToXYPlane(Axis, out var _backTransform);
                    var center = Anchor.ConvertTo2DCoordinates(transform);
                    _circle = new Circle(center, Radius * Radius);
                }
                return _circle;
            }
            set
            {
                _circle = value;
            }
        }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }


        /// <summary>
        /// Gets or sets the maximum distance along axis.
        /// </summary>
        /// <value>The maximum distance along axis.</value>
        public double MaxDistanceAlongAxis { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// Gets or sets the minimum distance along axis.
        /// </summary>
        /// <value>The minimum distance along axis.</value>
        public double MinDistanceAlongAxis { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height
        {
            get
            {
                if (double.IsNaN(height) && !double.IsInfinity(MinDistanceAlongAxis)
                    && !double.IsInfinity(MaxDistanceAlongAxis))
                    height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
                return height;
            }
            set
            {
                height = value;
            }
        }
        double height = double.NaN;
        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume => Height * Math.PI * Radius * Radius;

        /// <summary>
        /// Gets or sets the total internal angle.
        /// </summary>
        /// <value>The total internal angle.</value>
        public double TotalInternalAngle { get; set; }


        public override string KeyString => "Cylinder|" + Axis.ToString() +
            "|" + Anchor.ToString() + "|" + Radius.ToString("F5") + "|" + MinDistanceAlongAxis.ToString("F5")
            + "|" + MaxDistanceAlongAxis.ToString("F5") + GetCommonKeyDetails();
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        public Cylinder() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, IEnumerable<TriangleFace> faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            SetFacesAndVertices(faces);
            if (faces != null)
            {
                var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
                MinDistanceAlongAxis = min;
                MaxDistanceAlongAxis = max;
            }
        }


        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var a = point - Anchor;
            var b = Axis.Cross(a);
            var outwardVector = b.Cross(Axis).Normalize();
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
            var d = (point - Anchor).Cross(Axis).Length() - Radius;
            // if d is positive, then the point is outside the cylinder
            // if d is negative, then the point is inside the cylinder
            if (IsPositive.GetValueOrDefault(true)) return d;
            else return -d;
        }

        /// <summary>
        /// Returns the point on the object that is closest to the specified position in 3D space.
        /// </summary>
        /// <param name="point">The position in world coordinates for which to find the nearest point on the object.</param>
        /// <returns>A <see cref="Vector3"/> representing the closest point on the object to the specified position.</returns>
        /// <exception cref="NotImplementedException">The method is not implemented.</exception>
        public override Vector3 ClosestPointOnSurfaceToPoint(Vector3 point)
        {
            var d = (point - Anchor).Dot(Axis);
            var axisPoint = Anchor + d * Axis;
            var radialDir = (point - axisPoint).Normalize();
            return axisPoint + Radius * radialDir;
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any() || Area.IsNegligible()) return;
            var axisPointUnderFace = Anchor + (LargestFace.Center - Anchor).Dot(Axis) * Axis;
            isPositive = (LargestFace.Center - axisPointUnderFace).Dot(LargestFace.Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            if (double.IsFinite(MaxDistanceAlongAxis)
             && double.IsFinite(MinDistanceAlongAxis))
            {
                var offset = Anchor.Dot(Axis);
                var top = Anchor + (MaxDistanceAlongAxis - offset) * Axis;
                var bottom = Anchor + (MinDistanceAlongAxis - offset) * Axis;
                var xFactor = Math.Sqrt(1 - Axis.X * Axis.X);
                if (double.IsNaN(xFactor)) xFactor = 0; // occasionally get NaN due to rounding errors when Axis.X is 1 or -1
                var yFactor = Math.Sqrt(1 - Axis.Y * Axis.Y);
                if (double.IsNaN(yFactor)) yFactor = 0; // occasionally get NaN due to rounding errors ""
                var zFactor = Math.Sqrt(1 - Axis.Z * Axis.Z);
                if (double.IsNaN(zFactor)) zFactor = 0; // occasionally get NaN due to rounding errors ::
                MinX = Math.Min(top.X, bottom.X) - xFactor * Radius;
                MaxX = Math.Max(top.X, bottom.X) + xFactor * Radius;
                MinY = Math.Min(top.Y, bottom.Y) - yFactor * Radius;
                MaxY = Math.Max(top.Y, bottom.Y) + yFactor * Radius;
                MinZ = Math.Min(top.Z, bottom.Z) - zFactor * Radius;
                MaxZ = Math.Max(top.Z, bottom.Z) + zFactor * Radius;
            }
            else
            {
                MinX = MinY = MinZ = double.NegativeInfinity;
                MaxX = MaxY = MaxZ = double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Finds the intersection between this cylinder and the specified line.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchorLine, Vector3 direction)
        => LineIntersection(Axis, Radius, Anchor, anchorLine, direction).ToList();

        /// <summary>
        /// Finds the intersection between a cylinder and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="axis">The axis of the cylinder.</param>
        /// <param name="radius">The radius of the cylinder.</param>
        /// <param name="anchorCyl">The anchor of the cylinder..</param>
        /// <param name="anchorLine">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public static IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 axis, double radius, Vector3 anchorCyl, Vector3 anchorLine, Vector3 direction)
        {
            direction = direction.Normalize();
            var minDistance = MiscFunctions.SkewedLineIntersection(anchorCyl, axis, anchorLine, direction, out _, out var cylAxisPoint, out var linePoint, out _,
                out var tChordCenter);

            if (minDistance.IsPracticallySame(radius)) // one intersection
                yield return (linePoint, tChordCenter);
            if (minDistance > radius) // no intersection
                yield break;
            // here, the halfChoordLength is the distance from the chordCenter to where it would intersect the circle of the cylinder
            var halfChordLength = Math.Sqrt(radius * radius - minDistance * minDistance);
            var sinAngleLineCylinder = axis.Cross(direction).Length();
            var distanceToCylinder = halfChordLength / sinAngleLineCylinder;
            yield return (linePoint - distanceToCylinder * direction, tChordCenter - distanceToCylinder);
            yield return (linePoint + distanceToCylinder * direction, tChordCenter + distanceToCylinder);
        }


        //public TessellatedSolid AsTessellatedSolid()
        //{
        //    var faces = new List<TriangleFace>();
        //    foreach(var face in Faces)
        //    {
        //        var vertices = new Vertex[] { face.C, face.B, face.A }; //reverse the vertices
        //        faces.Add(new TriangleFace(vertices, face.Normal * -1)));
        //    }
        //    //Add the top and bottom faces
        //    //Build the cylinder along the axis
        //    //First, get the planes on the top and bottom.
        //    //Second, determine which plane is further along the axis. The faces on this plane will have a normal == axis
        //    //The bottom plane will have faces in the reverse of the axis.
        //    var plane1 = MiscFunctions.GetPlaneFromThreePoints(Loop1[0].Coordinates, Loop1[1].Coordinates, Loop1[2].Coordinates);
        //    var plane
        //}
        #endregion

        #region Flattening/Unrolling/Developing the Cylinder

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        => TransformPoint(point, out _);

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public Vector2 TransformPoint(Vector3 point, out double angle)
        {
            var v = point - Anchor;
            var x = PerpXDir.Dot(v);
            var y = PerpYDir.Dot(v);
            angle = Math.Atan2(y, x);
            return new Vector2(angle * Radius, v.Dot(Axis));
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = (point.X / Radius) % Math.Tau;
            var result = Anchor + Radius * Math.Cos(angle) * PerpXDir;
            result += Radius * Math.Sin(angle) * PerpYDir;
            result += point.Y * Axis;
            return result;
        }

        /// <summary>Develops an open path continuously around the cylinder.</summary>
        /// <param name="points">The 3D points in traversal order.</param>
        /// <returns>Developed points with successive angles unwrapped as the path crosses the seam.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
            => TransformPoints(points as IList<Vector3> ?? points.ToList());

        /// <summary>Develops cylinder outlines and their holes into polygons that can be triangulated in 2D.</summary>
        /// <param name="polygon3Ds">The input outlines, including any holes grouped with each outline.</param>
        /// <param name="MatchingSeamVertices">
        /// A two-way map between distinct retained vertices that represent the same 3D point on the
        /// artificial cut. The two vertices can belong to different output polygons.
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
        /// True to cut each standalone closed outline into the fixed strip. False to let independent
        /// contours form bands or stay continuous. Outlines with explicit holes are handled together.
        /// </param>
        /// <returns>
        /// Developed polygon pieces with any enclosed holes attached, together with continuously developed open paths.
        /// A hole crossing the seam becomes part of the cut boundary.
        /// </returns>
        /// <remarks>
        /// The strip runs from X = 0 to X = one circumference. Both edges represent the same line
        /// on the cylinder in the radial direction of <see cref="PerpXDir"/>. Y measures signed
        /// axial distance from <see cref="Anchor"/>.
        /// </remarks>
        public List<Polygon> TransformFrom3DTo2D(IEnumerable<Polygon3D> polygon3Ds,
            out Dictionary<Vertex2D, Vertex2D> MatchingSeamVertices,
            out Dictionary<Vector3, Vertex2D> VertexMapping, double newVertexSpacing = -1.0,
            bool cutStandaloneOutlines = false)
        {
            MatchingSeamVertices = new();
            VertexMapping = new();
            var result = new List<Polygon>();
            var circumference = Math.Tau * Radius;
            var frame = Axis.TransformToXYPlane(out _);
            var loopID = 0;
            foreach (var (batch, grouped) in DevelopmentBatches(polygon3Ds, cutStandaloneOutlines))
            {
                var rings = new List<List<Vertex2D>>();
                var contours = new List<(int index, Vector2[] path, IList<Vector3> source, bool hole)>();
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
                        var points = TransformPoints(path);
                        if (!closed)
                        {
                            AddOpenPath(path, points, index, result, VertexMapping);
                        }
                        else if (Math.Abs(path.FindWindingAroundAxis(frame, Anchor, out _, out _)) > 1.67 * Math.PI)
                        {
                            var ring = points.Select((point, i) => new Vertex2D(point, i, index)).ToList();
                            ringSources.AddRange(path.Zip(ring));
                            rings.Add(ring);
                        }
                        else
                            contours.Add((index, points, path, hole));
                        hole = true;
                    }
                }

                // Each pair of winding rings bounds a band. The lower and upper paths run in
                // opposite directions so the strip boundary can join them at each seam edge.
                rings = rings.OrderBy(ring => ring.Average(vertex => vertex.Y)).ToList();
                for (var i = 1; i < rings.Count; i += 2)
                {
                    var bottom = rings[i - 1];
                    var top = rings[i];
                    if (bottom[^1].X < bottom[0].X) bottom.Reverse();
                    if (top[^1].X > top[0].X) top.Reverse();
                    MoveSingleTurnLoopIntoCylinderStrip(bottom, circumference, true);
                    MoveSingleTurnLoopIntoCylinderStrip(top, circumference, false);
                    var lower = GetCylinderSeamHeight(bottom, circumference, true);
                    var upper = GetCylinderSeamHeight(top, circumference, false);
                    var loop = new List<Vector2> { new(0, upper), new(0, lower) };
                    loop.AddRange(bottom.Where(v => !OnCorner(v, lower)).Select(v => v.Coordinates));
                    loop.Add(new Vector2(circumference, lower));
                    loop.Add(new Vector2(circumference, upper));
                    loop.AddRange(top.Where(v => !OnCorner(v, upper)).Select(v => v.Coordinates));
                    pieces.Add(new Polygon(loop, bottom[0].LoopID) { IsPositive = true });
                }
                if (rings.Count % 2 != 0)
                {
                    var ring = rings[^1];
                    loose.Add(new Polygon(ring, ring[0].LoopID));
                }
                // The ring vertices now hold strip coordinates, which the output polygons will use.
                sourcePoints.AddRange(ringSources.Select(pair => (pair.source, pair.vertex.Coordinates)));

                var overlap = SeamTolerance(pieces) * 4;
                foreach (var (index, path, source, hole) in contours)
                {
                    if (!grouped && pieces.Count == 0)
                    {
                        loose.Add(new Polygon(path, index));
                        sourcePoints.AddRange(source.Zip(path));
                        continue;
                    }
                    // Clip for placement and containment. A hole is then subtracted using
                    // the full translated cutter so it reaches the edge of the band.
                    var fragments = CutCylinderContour(path, circumference, index, 0);
                    if (grouped && !hole)
                        pieces.AddRange(fragments);
                    else if (grouped || IsInsideBand(pieces, fragments))
                        cutters.AddRange(CutCylinderContour(path, circumference, index, overlap));
                    else
                    {
                        loose.Add(new Polygon(path, index));
                        sourcePoints.AddRange(source.Zip(path));
                        continue;
                    }
                    // Record both representatives of a source sample on the seam. Either
                    // one can survive clipping, and the source map only needs one.
                    for (var i = 0; i < path.Length; i++)
                    {
                        var x = path[i].X - circumference * Math.Floor(path[i].X / circumference);
                        sourcePoints.Add((source[i], new Vector2(x, path[i].Y)));
                        if (Math.Abs(x) <= overlap || Math.Abs(x - circumference) <= overlap)
                            sourcePoints.Add((source[i], new Vector2(x < circumference / 2 ? circumference : 0, path[i].Y)));
                    }
                }
                var spacing = newVertexSpacing <= 0 && rings.Count > 0
                    ? GetRootMeanSquareEdgeLength<Vertex2D>(rings) : newVertexSpacing;
                result.AddRange(CompleteSeamCuts(pieces, cutters, sourcePoints, spacing,
                    (side, height) => new Vector2(side * circumference, height), (point, _) => point.Y,
                    MatchingSeamVertices, VertexMapping));
                var looseTrees = loose.CreateShallowPolygonTrees(false);
                AddRetainedSourceVertices(looseTrees, sourcePoints, SeamTolerance(looseTrees), VertexMapping);
                result.AddRange(looseTrees);

                bool OnCorner(Vertex2D vertex, double height) =>
                    Math.Abs(vertex.X) <= Constants.BaseTolerance && Math.Abs(vertex.Y - height) <= Constants.BaseTolerance;
            }
            return result;
        }

        private static List<Polygon> CutCylinderContour(Vector2[] path, double period, int index, double overlap)
        {
            if (path.Length < 3) return [];
            var min = path.Min(point => point.X);
            var max = path.Max(point => point.X);
            var low = path.Min(point => point.Y);
            var high = path.Max(point => point.Y);
            var margin = Math.Max(1, high - low);
            var first = (int)Math.Floor(min / period);
            var last = (int)Math.Floor(max / period);
            var window = new Polygon(new Vector2[] { new(0, low - margin), new(period, low - margin),
                new(period, high + margin), new(0, high + margin) });
            var pieces = new List<Polygon>();
            for (var turn = first; turn <= last; turn++)
            {
                var shifted = new Polygon(path.Select(point => new Vector2(point.X - turn * period, point.Y)), index)
                { IsPositive = true };
                // The band's own boundary clips the cutter. Keeping its full translated contour avoids slivers.
                if (overlap > 0 || first == last)
                    pieces.Add(shifted);
                else
                    pieces.AddRange(shifted.Intersect(window).Select(piece => { piece.Index = index; return piece; }));
            }
            return pieces;
        }

        /// <summary>
        /// Keeps each closed region with known holes in its own development batch. Independent contours
        /// share a batch so their winding rings can form bands, unless each outline must be cut separately.
        /// </summary>
        internal static IEnumerable<(IList<Polygon3D> batch, bool grouped)> DevelopmentBatches(
            IEnumerable<Polygon3D> polygons, bool cutStandaloneOutlines)
        {
            var loose = new List<Polygon3D>();
            foreach (var polygon in polygons)
                if (polygon.IsClosed && (cutStandaloneOutlines || polygon.Holes?.Count > 0))
                    yield return (new[] { polygon }, true);
                else
                    loose.Add(polygon);
            if (loose.Count > 0) yield return (loose, false);
        }

        internal static void AddOpenPath(IList<Vector3> path, Vector2[] points, int index,
            List<Polygon> result, Dictionary<Vector3, Vertex2D> sourceVertices)
        {
            var vertices = points.Select((point, i) => new Vertex2D(point, i, index)).ToArray();
            var polygon = new Polygon(vertices, index, false);
            var retained = polygon.Vertices.ToHashSet();
            for (var i = 0; i < vertices.Length; i++)
                if (retained.Contains(vertices[i]))
                    sourceVertices.TryAdd(path[i], vertices[i]);
            result.Add(polygon);
        }

        internal static bool IsInsideBand(List<Polygon> bands, List<Polygon> fragments)
        {
            var area = fragments.Sum(p => p.Area);
            return area > 0 && bands.Any(band =>
                fragments.Sum(fragment => band.Intersect(fragment).Sum(p => p.Area)) >= area * (1 - 1e-5));
        }

        internal static double SeamTolerance(IEnumerable<Polygon> polygons) => Math.Max(1e-7,
            polygons.Select(p => (p.MaxX - p.MinX + p.MaxY - p.MinY) * Constants.PolygonSameTolerance)
                .DefaultIfEmpty().Max());

        /// <summary>
        /// Resolves candidate source coordinates to retained vertex instances after polygon construction
        /// and clipping. Matches within the rounding tolerance preserve the original 3D coordinates.
        /// </summary>
        internal static void AddRetainedSourceVertices(IEnumerable<Polygon> polygons,
            List<(Vector3 source, Vector2 point)> sourcePoints, double tolerance,
            Dictionary<Vector3, Vertex2D> sourceVertices)
        {
            foreach (var loop in polygons.SelectMany(p => p.AllPolygons))
            {
                var byCoordinate = loop.Vertices.ToLookup(vertex => vertex.Coordinates);
                foreach (var (source, point) in sourcePoints)
                {
                    if (sourceVertices.ContainsKey(source) || point.X < loop.MinX - tolerance ||
                        point.X > loop.MaxX + tolerance || point.Y < loop.MinY - tolerance ||
                        point.Y > loop.MaxY + tolerance)
                        continue;
                    var rounded = new Vector2(Math.Round(point.X, loop.NumSigDigits),
                        Math.Round(point.Y, loop.NumSigDigits));
                    var vertex = byCoordinate[rounded].FirstOrDefault() ??
                        loop.Vertices.MinBy(candidate => candidate.Coordinates.DistanceSquared(point));
                    if (vertex != null && vertex.Coordinates.Distance(point) <= tolerance)
                        sourceVertices.Add(source, vertex);
                }
            }
        }

        /// <summary>
        /// Subtracts holes and rebuilds both copies of the cut with matching subdivisions wherever
        /// the resulting region reaches both sides. Records pairs using the rebuilt polygon vertices.
        /// </summary>
        /// <param name="polygons">The developed region pieces before hole subtraction.</param>
        /// <param name="cutters">The developed hole contours to subtract.</param>
        /// <param name="sourcePoints">Original 3D samples and their candidate developed positions.</param>
        /// <param name="spacing">Maximum length of a new seam subdivision when positive and finite.</param>
        /// <param name="seamPoint">Converts a side index and seam coordinate to a developed 2D point.</param>
        /// <param name="seamDistance">
        /// Reads the seam coordinate on the specified side. It is axial height for a cylinder and slant
        /// radius for a cone, so equal values on opposite sides refer to the same 3D point.
        /// </param>
        /// <param name="matchingSeamVertices">Receives both directions of every retained seam pair.</param>
        /// <param name="sourceVertices">Receives mappings to the final retained vertices.</param>
        /// <returns>The rebuilt pieces, including their modified hole and outer boundaries.</returns>
        internal static List<Polygon> CompleteSeamCuts(List<Polygon> polygons, List<Polygon> cutters,
            List<(Vector3 source, Vector2 point)> sourcePoints, double spacing,
            Func<int, double, Vector2> seamPoint, Func<Vector2, int, double> seamDistance,
            Dictionary<Vertex2D, Vertex2D> matchingSeamVertices, Dictionary<Vector3, Vertex2D> sourceVertices)
        {
            var preClipVertices = polygons.SelectMany(p => p.AllPolygons).SelectMany(p => p.Path).ToArray();
            if (cutters.Count != 0)
                polygons = polygons.Subtract(cutters);
            var tolerance = SeamTolerance(polygons);
            bool OnSide(Vector2 point, int side) => point.Distance(seamPoint(side, seamDistance(point, side))) <= tolerance;
            var sides = new[] { new List<(double low, double high)>(), new List<(double low, double high)>() };
            foreach (var loop in polygons.SelectMany(p => p.AllPolygons))
                foreach (var edge in loop.Path.Zip(loop.Path.Skip(1).Append(loop.Path[0])))
                    for (var side = 0; side < 2; side++)
                        if (OnSide(edge.First, side) && OnSide(edge.Second, side))
                        {
                            var a = seamDistance(edge.First, side);
                            var b = seamDistance(edge.Second, side);
                            if (Math.Abs(a - b) > tolerance)
                                sides[side].Add((Math.Min(a, b), Math.Max(a, b)));
                        }

            // A physical boundary can lie on the cut. Only intervals with material on both copies are welded.
            var shared = new List<(double low, double high)>();
            sides[0].Sort();
            sides[1].Sort();
            for (int i = 0, j = 0; i < sides[0].Count && j < sides[1].Count;)
            {
                var left = sides[0][i];
                var right = sides[1][j];
                var low = Math.Max(left.low, right.low);
                var high = Math.Min(left.high, right.high);
                if (high - low > tolerance) shared.Add((low, high));
                if (left.high <= right.high) i++;
                if (right.high <= left.high) j++;
            }

            // Use the same seam samples on both sides, including overlap endpoints and any
            // original breakpoints that Boolean clipping may have discarded.
            var candidates = shared.SelectMany(span => new[] { span.low, span.high }).ToList();
            foreach (var point in preClipVertices.Concat(sourcePoints.Select(pair => pair.point)))
                for (var side = 0; side < 2; side++)
                    if (OnSide(point, side))
                    {
                        var distance = seamDistance(point, side);
                        if (shared.Any(span => distance > span.low + tolerance && distance < span.high - tolerance))
                            candidates.Add(distance);
                    }
            if (spacing > 0 && double.IsFinite(spacing))
                foreach (var (low, high) in shared)
                {
                    var count = (int)Math.Ceiling((high - low) / spacing);
                    for (var i = 1; i < count; i++)
                        candidates.Add(low + (high - low) * i / count);
                }
            var samples = new List<double>();
            foreach (var position in candidates.Order())
                if (samples.Count == 0 || position - samples[^1] > tolerance)
                    samples.Add(position);
            // Pair by sample index after construction. A vertex shared by both sides,
            // such as the cone apex, already has a single identity.
            var seamVertices = new[] { new Dictionary<int, Vertex2D>(), new Dictionary<int, Vertex2D>() };
            var boundarySamples = sourcePoints.Select(pair => pair.point).Distinct().ToArray();
            polygons = polygons.Select(RebuildBoundary).ToList();
            foreach (var (index, left) in seamVertices[0])
                if (seamVertices[1].TryGetValue(index, out var right) && !ReferenceEquals(left, right))
                {
                    matchingSeamVertices.Add(left, right);
                    matchingSeamVertices.Add(right, left);
                }
            AddRetainedSourceVertices(polygons, sourcePoints, tolerance, sourceVertices);
            return polygons;

            Polygon RebuildBoundary(Polygon loop)
            {
                var points = new List<Vector2>();
                for (var i = 0; i < loop.Vertices.Count; i++)
                {
                    var a = loop.Vertices[i].Coordinates;
                    var b = loop.Vertices[(i + 1) % loop.Vertices.Count].Coordinates;
                    points.Add(a);
                    var isCutLine = false;
                    for (var side = 0; side < 2; side++)
                    {
                        if (!OnSide(a, side) || !OnSide(b, side)) continue;
                        isCutLine = true;
                        var from = seamDistance(a, side);
                        var to = seamDistance(b, side);
                        var interior = samples.Where(s => s > Math.Min(from, to) + tolerance &&
                            s < Math.Max(from, to) - tolerance);
                        foreach (var sample in from <= to ? interior : interior.Reverse())
                            points.Add(seamPoint(side, sample));
                        break;
                    }
                    if (isCutLine) continue;
                    // Boolean operations can discard collinear samples. On a cylinder a straight developed
                    // ring edge still needs those vertices to follow the circumference when it rolls back up.
                    var v = b - a;
                    var lengthSquared = v.LengthSquared();
                    if (lengthSquared <= tolerance * tolerance) continue;
                    var fractionTolerance = tolerance / Math.Sqrt(lengthSquared);
                    var positions = new List<double>();
                    foreach (var sample in boundarySamples)
                    {
                        if (sample.X < Math.Min(a.X, b.X) - tolerance || sample.X > Math.Max(a.X, b.X) + tolerance ||
                            sample.Y < Math.Min(a.Y, b.Y) - tolerance || sample.Y > Math.Max(a.Y, b.Y) + tolerance)
                            continue;
                        var fraction = (sample - a).Dot(v) / lengthSquared;
                        if (fraction > fractionTolerance && fraction < 1 - fractionTolerance &&
                            sample.Distance(a + fraction * v) <= tolerance)
                            positions.Add(fraction);
                    }
                    var previous = 0.0;
                    foreach (var fraction in positions.Order())
                        if (fraction - previous > fractionTolerance)
                        {
                            points.Add(a + fraction * v);
                            previous = fraction;
                        }
                }
                var result = new Polygon(points, loop.Index);
                foreach (var hole in loop.InnerPolygons)
                    result.AddInnerPolygon(RebuildBoundary(hole));
                foreach (var vertex in result.Vertices)
                    for (var side = 0; side < 2; side++)
                    {
                        if (!OnSide(vertex.Coordinates, side)) continue;
                        var distance = seamDistance(vertex.Coordinates, side);
                        var index = samples.BinarySearch(distance);
                        if (index < 0)
                        {
                            index = ~index;
                            if (index > 0 && distance - samples[index - 1] <= tolerance)
                                index--;
                        }
                        if (index < samples.Count && Math.Abs(samples[index] - distance) <= tolerance)
                            seamVertices[side].TryAdd(index, vertex);
                    }
                return result;
            }
        }

        private Vector2[] TransformPoints(IList<Vector3> points)
        {
            // the cylinder will be unrolled, and the tangential angle around the cylinder will be transformed
            // into the x (horizontal coordinate). The first point provides a reference for the additional points
            var result = new Vector2[points.Count];
            var prevAngle = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                var this2DPoint = TransformPoint(points[i], out var thisAngle);
                // if next point is off by more than 180 degrees,then it is likely the angle is wrong and needs to be adjusted
                var angleAltered = false;
                while (thisAngle - prevAngle > Math.PI)
                {
                    angleAltered = true;
                    thisAngle -= Math.Tau;
                }
                while (prevAngle - thisAngle > Math.PI)
                {
                    angleAltered = true;
                    thisAngle += Math.Tau;
                }
                if (angleAltered)
                    result[i] = new Vector2(thisAngle * Radius, this2DPoint.Y);
                else result[i] = this2DPoint;
                prevAngle = thisAngle;
            }
            return result;
        }



        internal static double GetRootMeanSquareEdgeLength<T>(params IEnumerable<IList<T>> loops)
            where T : IVector2D
        {
            var totalLengthSqdSum = 0.0;
            var numLengths = 0;
            foreach (var loop in loops)
            {
                numLengths += Math.Max(0, loop.Count - 1);
                for (int j = 1; j < loop.Count; j++)
                    totalLengthSqdSum += (loop[j - 1].X - loop[j].X) * (loop[j - 1].X - loop[j].X) +
                        (loop[j - 1].Y - loop[j].Y) * (loop[j - 1].Y - loop[j].Y);
            }
            return numLengths == 0 ? double.NaN : Math.Sqrt(totalLengthSqdSum / numLengths);
        }

        private static void MoveSingleTurnLoopIntoCylinderStrip(List<Vertex2D> loop, double circumference, bool increasing)
        {
            if (loop.Count < 2)
                return;

            // Every X + k*circumference represents the same physical point on
            // the cylinder. First choose the representative in [0, circumference).
            foreach (var vertex in loop)
            {
                var wrappedX = vertex.X
                    - circumference * Math.Floor(vertex.X / circumference);

                vertex.Coordinates = new Vector2(wrappedX, vertex.Y);
            }

            // Wrapping creates one large jump where the path crosses the seam.
            // Move that jump to the list boundary so the bottom runs 0 -> C
            // and the top runs C -> 0.
            var cutIndex = 0;
            var largestWrapJump = 0.5 * circumference;

            for (var j = 1; j < loop.Count; j++)
            {
                var wrapJump = increasing
                    ? loop[j - 1].X - loop[j].X   // large downward jump
                    : loop[j].X - loop[j - 1].X;  // large upward jump

                if (wrapJump > largestWrapJump)
                {
                    largestWrapJump = wrapJump;
                    cutIndex = j;
                }
            }

            if (cutIndex == 0)
                return;

            var beforeSeam = loop.GetRange(0, cutIndex);
            loop.RemoveRange(0, cutIndex);
            loop.AddRange(beforeSeam);
        }

        private static double GetCylinderSeamHeight(IList<Vertex2D> loop, double circumference,
            bool isIncreasing)
        {
            (var startX, var endX) = isIncreasing
                ? (loop[0].X, loop[^1].X)
                : (loop[^1].X, loop[0].X);
            (var startY, var endY) = isIncreasing
                ? (loop[0].Y, loop[^1].Y)
                : (loop[^1].Y, loop[0].Y);
            while (startX > circumference) startX -= circumference;
            // in this way, the endX is now negative, and we simply have to find the crossing at 0.0
            while (endX > 0) endX -= circumference;
            //return endY + (startY - endY) * (0.0 - endX) / (startX - endX);
            // this is correct, but writing more simply (less intuitively) as:
            return endY + endX * (endY - startY) / (startX - endX);
        }

        #endregion
    }
}
