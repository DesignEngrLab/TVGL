using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class Develop3Dto2D
    {
        /// <summary>Develops one closed cone or cylinder region using its existing`\ angular frame.</summary>
        public static List<Polygon> DevelopPeriodicRegion(this PrimitiveSurface surface, Polygon3D polygon3D,
           out Dictionary<Vertex2D, int> SeamIds, out Dictionary<Vector3, Vertex2D> VertexMapping,
           double newVertexSpacing = -1)
        {
            var (axis, anchor, period, start) = surface switch
            {
                Cylinder c => (c.Axis, c.Anchor, Math.Tau * c.Radius, 0.0),
                Cone c => (c.Axis, c.Apex, Math.Tau * Math.Abs(c.sinAperture), -Math.PI * Math.Abs(c.sinAperture)),
                _ => throw new ArgumentException("Expected a cone or cylinder.", nameof(surface))
            };
            var isCone = surface is Cone;
            var transform = axis.TransformToXYPlane(out _);
            var holes = polygon3D.Holes ?? Array.Empty<IList<Vector3>>();
            var sourcePoints = new List<(Vector3 source, Vector2 developed)>();
            var polygons = new List<Polygon>();
            if (Encircles(polygon3D.Vertices) || holes.Any(Encircles))
            {
                // The strip/sector builders already form the correct physical band from winding loops.
                var band = new Polygon3D(polygon3D.Vertices, true) { Holes = holes.Where(Encircles).ToArray() };
                Dictionary<Vector3, Vertex2D> bandSources;
                var pieces = surface is Cone cone
                    ? cone.GetUnrolledPolygons([band], out bandSources, newVertexSpacing)
                    : ((Cylinder)surface).GetUnrolledPolygons([band], out bandSources, newVertexSpacing);
                polygons.AddRange(pieces.Select(piece => piece.polygon));
                sourcePoints.AddRange(bandSources.Select(pair => (pair.Key, pair.Value.Coordinates)));
            }
            else
                polygons.AddRange(CutContour(polygon3D.Vertices, 0));

            var preClipVertices = polygons.SelectMany(p => p.AllPolygons).SelectMany(p => p.Path).ToArray();
            // Push a notch cutter past the seam by more than the polygon coordinate precision so
            // independent rounding of the two polygons cannot leave a sliver of material along the seam.
            var cutOverlap = Math.Max(1e-6, 4 * Constants.PolygonSameTolerance * polygons
                .Select(p => p.MaxX - p.MinX + p.MaxY - p.MinY).DefaultIfEmpty().Max());
            var cutters = holes.Where(hole => !Encircles(hole))
                .SelectMany(hole => CutContour(hole, cutOverlap)).ToList();
            if (cutters.Count != 0)
                polygons = polygons.Subtract(cutters);

            var tolerance = Math.Max(1e-7, polygons.Select(p =>
                (p.MaxX - p.MinX + p.MaxY - p.MinY) * Constants.PolygonSameTolerance)
                .DefaultIfEmpty().Max());
            var end = start + period;
            var lowerRay = FromPolar(start, 1.0);
            var upperRay = FromPolar(end, 1.0);

            var sides = new[] { new List<(double low, double high)>(), new List<(double low, double high)>() };
            foreach (var loop in polygons.SelectMany(p => p.AllPolygons))
                foreach (var edge in loop.Path.Zip(loop.Path.Skip(1).Append(loop.Path[0])))
                    for (var side = 0; side < 2; side++)
                        if (OnSide(edge.First, side) && OnSide(edge.Second, side))
                        {
                            var a = SeamDistance(edge.First, side);
                            var b = SeamDistance(edge.Second, side);
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

            var candidates = shared.SelectMany(span => new[] { span.low, span.high }).ToList();
            foreach (var point in preClipVertices.Concat(sourcePoints.Select(pair => pair.developed)))
                for (var side = 0; side < 2; side++)
                    if (OnSide(point, side))
                    {
                        var distance = SeamDistance(point, side);
                        if (shared.Any(span => distance > span.low + tolerance && distance < span.high - tolerance))
                            candidates.Add(distance);
                    }
            if (newVertexSpacing > 0 && double.IsFinite(newVertexSpacing))
                foreach (var (low, high) in shared)
                {
                    var count = (int)Math.Ceiling((high - low) / newVertexSpacing);
                    for (var i = 1; i < count; i++)
                        candidates.Add(low + (high - low) * i / count);
                }
            var samples = new List<double>();
            foreach (var position in candidates.Order())
                if (samples.Count == 0 || position - samples[^1] > tolerance)
                    samples.Add(position);

            var seamIds = new Dictionary<Vertex2D, int>();
            var boundarySamples = sourcePoints.Select(pair => pair.developed).Distinct().ToArray();
            polygons = polygons.Select(RebuildBoundary).ToList();
            VertexMapping = new Dictionary<Vector3, Vertex2D>();
            foreach (var loop in polygons.SelectMany(p => p.AllPolygons))
            {
                var byCoordinate = loop.Vertices.ToLookup(vertex => vertex.Coordinates);
                foreach (var (source, point) in sourcePoints)
                {
                    if (VertexMapping.ContainsKey(source) || point.X < loop.MinX - tolerance ||
                        point.X > loop.MaxX + tolerance || point.Y < loop.MinY - tolerance ||
                        point.Y > loop.MaxY + tolerance)
                        continue;
                    var rounded = new Vector2(Math.Round(point.X, loop.NumSigDigits),
                        Math.Round(point.Y, loop.NumSigDigits));
                    var vertex = byCoordinate[rounded].FirstOrDefault() ??
                        loop.Vertices.MinBy(candidate => candidate.Coordinates.DistanceSquared(point));
                    if (vertex != null && vertex.Coordinates.Distance(point) <= tolerance)
                        VertexMapping.Add(source, vertex);
                }
            }
            SeamIds = seamIds;
            return polygons;

            #region Local Methods (I'm learning from the AIs that this is a good way to avoid WET code)
            Vector2 SeamPoint(int side, double distance) => isCone
                ? FromPolar(side == 0 ? start : end, distance)
                : new Vector2(side == 0 ? start : end, distance);
            double SeamDistance(Vector2 point, int side) =>
                isCone ? point.Dot(side == 0 ? lowerRay : upperRay) : point.Y;
            bool OnSide(Vector2 point, int side)
            {
                var distance = SeamDistance(point, side);
                return (!isCone || distance >= -tolerance) &&
                    point.Distance(SeamPoint(side, distance)) <= tolerance;
            }
            bool Encircles(IList<Vector3> loop) =>
                // if the loop winds around the axis more than 5π/3 (1.67 * π, which is a little less than 2π),
                // it is considered a closed band.
                Math.Abs(loop.FindWindingAroundAxis(transform, anchor, out _, out _, true)) > 1.67 * Math.PI;
            List<Polygon> CutContour(IList<Vector3> contour, double overlap)
            {
                var lifted = new List<Vector2>(contour.Count);
                foreach (var source in contour)
                {
                    var point = surface.TransformFrom3DTo2D(source);
                    var x = isCone ? Math.Atan2(point.Y, point.X) : point.X;
                    if (lifted.Count > 0)
                        x -= period * Math.Round((x - lifted[^1].X) / period);
                    lifted.Add(new Vector2(x, isCone ? point.Length() : point.Y));
                }
                if (lifted.Count < 3)
                    return [];

                var min = lifted.Min(p => p.X);
                var max = lifted.Max(p => p.X);
                var first = (int)Math.Floor((min - start) / period);
                var last = (int)Math.Floor((max - start) / period);
                var bottom = lifted.Min(p => p.Y);
                var top = lifted.Max(p => p.Y);
                var margin = Math.Max(1, top - bottom);
                Vector2 ToDeveloped(Vector2 p, double shift, double overlapAtCut = 0)
                {
                    var x = p.X - shift;
                    var side = Math.Abs(x - start) <= 1e-6 ? -1 :
                        Math.Abs(x - start - period) <= 1e-6 ? 1 : 0;
                    if (side != 0) x = side < 0 ? start : start + period;
                    return isCone
                        ? FromPolar(x, p.Y) + side * FromPolar(x + Math.PI / 2, overlapAtCut)
                        : new Vector2(x + side * overlapAtCut, p.Y);
                }
                for (var i = 0; i < lifted.Count; i++)
                {
                    var position = lifted[i];
                    var turn = Math.Floor((position.X - start) / period);
                    sourcePoints.Add((contour[i], ToDeveloped(position, turn * period)));
                }
                if (first == last)
                    return [new Polygon(lifted.Select(p => ToDeveloped(p, first * period, overlap)), isClosed: true)];

                var subject = new Polygon(lifted, isClosed: true);
                var result = new List<Polygon>();
                for (var copy = first; copy <= last; copy++)
                {
                    var shift = copy * period;
                    var left = start + shift;
                    var right = left + period;
                    var window = new Polygon([
                        new Vector2(left, bottom - margin), new Vector2(right, bottom - margin),
                        new Vector2(right, top + margin), new Vector2(left, top + margin)], 
                        isClosed: true);
                    foreach (var piece in subject.Intersect(window))
                    {
                        var path = piece.Path.Select(point =>
                        {
                            // Chart clipping gives the topology. Intersect the original developed chord with
                            // the cut ray to recover its slant distance rather than interpolating polar radius.
                            if (isCone && Math.Min(Math.Abs(point.X - left), Math.Abs(point.X - right)) <= 1e-6)
                            {
                                var angle = Math.Abs(point.X - left) < Math.Abs(point.X - right) ? left : right;
                                point = new Vector2(angle, ConeCutRadius(lifted, angle, point.Y));
                            }
                            return ToDeveloped(point, shift, overlap);
                        });
                        result.Add(new Polygon(path, isClosed: true));
                    }
                }
                return result;
            }

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
                        var from = SeamDistance(a, side);
                        var to = SeamDistance(b, side);
                        var interior = samples.Where(s => s > Math.Min(from, to) + tolerance &&
                            s < Math.Max(from, to) - tolerance);
                        foreach (var sample in from <= to ? interior : interior.Reverse())
                            points.Add(SeamPoint(side, sample));
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
                        var distance = SeamDistance(vertex.Coordinates, side);
                        var index = samples.BinarySearch(distance);
                        if (index < 0)
                        {
                            index = ~index;
                            if (index > 0 && distance - samples[index - 1] <= tolerance)
                                index--;
                        }
                        if (index < samples.Count && Math.Abs(samples[index] - distance) <= tolerance)
                            seamIds.TryAdd(vertex, index);
                    }
                return result;
            }
            #endregion
        }

        private static Vector2 FromPolar(double angle, double radius) =>
            new(radius * Math.Cos(angle), radius * Math.Sin(angle));


        private static double ConeCutRadius(List<Vector2> loop, double angle, double interpolatedRadius)
        {
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
    }
}
