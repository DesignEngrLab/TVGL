using System.Globalization;
using System.Text;

namespace TVGL.STEPImportExport;

/// <summary>
/// Writes the managed AP214 hybrid/faceted subset of STEP without a platform-specific dependency.
/// </summary>
internal static class FacetedStepWriter
{
    internal abstract record AnalyticFace(
        int[] OuterLoop,
        IReadOnlyList<int[]> InnerLoops,
        IReadOnlySet<int> TriangleIndices,
        bool SameSense);

    internal sealed record PlanarFace(
        int[] OuterLoop,
        IReadOnlyList<int[]> InnerLoops,
        IReadOnlySet<int> TriangleIndices,
        (double X, double Y, double Z) Normal,
        IReadOnlyList<CircularBoundary?>? CircularBoundaries = null)
        : AnalyticFace(OuterLoop, InnerLoops, TriangleIndices, true);

    internal enum CurvedSurfaceKind
    {
        Cylinder,
        Cone,
        Sphere,
        Torus
    }

    internal sealed record CircularBoundary(
        int VertexIndex,
        (double X, double Y, double Z) Center,
        (double X, double Y, double Z) Normal,
        double Radius);

    internal sealed record CurvedFace(
        int[] OuterLoop,
        IReadOnlyList<int[]> InnerLoops,
        IReadOnlySet<int> TriangleIndices,
        bool SameSense,
        CurvedSurfaceKind Kind,
        (double X, double Y, double Z) Origin,
        (double X, double Y, double Z) Axis,
        double FirstRadius,
        double SecondRadius = 0,
        double SemiAngle = 0,
        IReadOnlyList<CircularBoundary>? CircularBoundaries = null)
        : AnalyticFace(OuterLoop, InnerLoops, TriangleIndices, SameSense);

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static bool Write(
        string filePath,
        double[] vertices,
        int[] triangles,
        IReadOnlyList<AnalyticFace>? analyticFaces = null,
        string? headerFileName = null)
    {
        if (vertices.Length < 9 || vertices.Length % 3 != 0 ||
            triangles.Length < 3 || triangles.Length % 3 != 0)
            return false;

        var entities = new List<string>();
        int Add(string value)
        {
            entities.Add(value);
            return entities.Count;
        }

        var applicationContext = Add("APPLICATION_CONTEXT('core data for automotive mechanical design processes')");
        Add($"APPLICATION_PROTOCOL_DEFINITION('international standard','automotive_design',2000,#{applicationContext})");
        var productContext = Add($"PRODUCT_CONTEXT('',#{applicationContext},'mechanical')");
        var product = Add($"PRODUCT('TVGL faceted model','TVGL faceted model','',(#{productContext}))");
        var formation = Add($"PRODUCT_DEFINITION_FORMATION('','',#{product})");
        var definitionContext = Add($"PRODUCT_DEFINITION_CONTEXT('part definition',#{applicationContext},'design')");
        var definition = Add($"PRODUCT_DEFINITION('design','',#{formation},#{definitionContext})");
        var definitionShape = Add($"PRODUCT_DEFINITION_SHAPE('','',#{definition})");

        var pointIds = new int[vertices.Length / 3];
        for (var i = 0; i < pointIds.Length; i++)
        {
            pointIds[i] = Add(
                $"CARTESIAN_POINT('',({Number(vertices[i * 3])},{Number(vertices[i * 3 + 1])},{Number(vertices[i * 3 + 2])}))");
        }

        var topology = BuildTopology(vertices, triangles);
        var faceIdsByComponent = new Dictionary<int, List<int>>();
        var circularEdges = new Dictionary<string, (int EdgeId, int VertexId, (double X, double Y, double Z) Point)>();
        var consolidatedTriangleIndices = analyticFaces == null
            ? new HashSet<int>()
            : analyticFaces.SelectMany(face => face.TriangleIndices).ToHashSet();
        var componentsWithCurvedFaces = new HashSet<int>();
        var surfaceIds = new Dictionary<AnalyticFace, int>();
        var planarFrames = new Dictionary<PlanarFace, (
            (double X, double Y, double Z) Origin,
            (double X, double Y, double Z) XAxis,
            (double X, double Y, double Z) YAxis)>();

        if (analyticFaces != null)
        {
            // Create every underlying surface first so shared edge curves can
            // carry parameter-space definitions for both neighboring surfaces.
            foreach (var analyticFace in analyticFaces)
            {
                if (analyticFace.OuterLoop.Length < 3)
                    continue;

                int surfaceId;
                if (analyticFace is PlanarFace planarFace)
                {
                    var originIndex = planarFace.OuterLoop[0];
                    var referenceIndex = planarFace.OuterLoop[1];
                    var reference = DirectionBetween(vertices, originIndex, referenceIndex);
                    if (reference == null)
                        continue;
                    var normalId = Add($"DIRECTION('',({Number(planarFace.Normal.X)},{Number(planarFace.Normal.Y)},{Number(planarFace.Normal.Z)}))");
                    var referenceId = Add($"DIRECTION('',({Number(reference.Value.X)},{Number(reference.Value.Y)},{Number(reference.Value.Z)}))");
                    var placementId = Add($"AXIS2_PLACEMENT_3D('',#{pointIds[originIndex]},#{normalId},#{referenceId})");
                    surfaceId = Add($"PLANE('',#{placementId})");
                    var origin = (
                        X: vertices[originIndex * 3],
                        Y: vertices[originIndex * 3 + 1],
                        Z: vertices[originIndex * 3 + 2]);
                    var normal = planarFace.Normal;
                    var yAxis = (
                        X: normal.Y * reference.Value.Z - normal.Z * reference.Value.Y,
                        Y: normal.Z * reference.Value.X - normal.X * reference.Value.Z,
                        Z: normal.X * reference.Value.Y - normal.Y * reference.Value.X);
                    TryNormalize(yAxis, out yAxis);
                    planarFrames[planarFace] = (origin, reference.Value, yAxis);
                }
                else if (analyticFace is CurvedFace curvedFace && TryNormalize(curvedFace.Axis, out var axis))
                {
                    var reference = Perpendicular(axis);
                    var surfaceOriginId = Add($"CARTESIAN_POINT('',({Number(curvedFace.Origin.X)},{Number(curvedFace.Origin.Y)},{Number(curvedFace.Origin.Z)}))");
                    var surfaceAxisId = Add($"DIRECTION('',({Number(axis.X)},{Number(axis.Y)},{Number(axis.Z)}))");
                    var referenceId = Add($"DIRECTION('',({Number(reference.X)},{Number(reference.Y)},{Number(reference.Z)}))");
                    var placementId = Add($"AXIS2_PLACEMENT_3D('',#{surfaceOriginId},#{surfaceAxisId},#{referenceId})");
                    surfaceId = curvedFace.Kind switch
                    {
                        CurvedSurfaceKind.Cylinder => Add($"CYLINDRICAL_SURFACE('',#{placementId},{Number(curvedFace.FirstRadius)})"),
                        CurvedSurfaceKind.Cone => Add($"CONICAL_SURFACE('',#{placementId},{Number(curvedFace.FirstRadius)},{Number(curvedFace.SemiAngle)})"),
                        CurvedSurfaceKind.Sphere => Add($"SPHERICAL_SURFACE('',#{placementId},{Number(curvedFace.FirstRadius)})"),
                        CurvedSurfaceKind.Torus => Add($"TOROIDAL_SURFACE('',#{placementId},{Number(curvedFace.FirstRadius)},{Number(curvedFace.SecondRadius)})"),
                        _ => 0
                    };
                    if (surfaceId == 0)
                        continue;
                    componentsWithCurvedFaces.Add(topology.ComponentByTriangle[curvedFace.TriangleIndices.First()]);
                }
                else
                    continue;
                surfaceIds[analyticFace] = surfaceId;
            }

            // Curved faces establish shared analytic edge curves first. Planar
            // neighbors then reuse those exact topological edges.
            foreach (var analyticFace in analyticFaces
                         .Where(surfaceIds.ContainsKey)
                         .OrderBy(face => face is CurvedFace ? 0 : 1))
            {
                var surfaceId = surfaceIds[analyticFace];
                var component = topology.ComponentByTriangle[analyticFace.TriangleIndices.First()];
                if (analyticFace is CurvedFace { Kind: CurvedSurfaceKind.Sphere } sphericalFace)
                {
                    foreach (var sphericalFaceId in AddSphericalFaces(sphericalFace, surfaceId))
                        AddFaceToComponent(sphericalFace.TriangleIndices.First(), sphericalFaceId);
                    continue;
                }
                if (analyticFace is CurvedFace { Kind: CurvedSurfaceKind.Torus } toroidalFace)
                {
                    foreach (var toroidalFaceId in AddToroidalFaces(toroidalFace, surfaceId))
                        AddFaceToComponent(toroidalFace.TriangleIndices.First(), toroidalFaceId);
                    continue;
                }
                var bounds = new List<int>();
                if (analyticFace is CurvedFace
                    {
                        Kind: CurvedSurfaceKind.Cylinder,
                        CircularBoundaries.Count: 2
                    } cylindricalFace)
                {
                    bounds.Add(AddCylindricalLoop(cylindricalFace, surfaceId, component));
                }
                else if (analyticFace is CurvedFace curvedWithBoundaries &&
                    curvedWithBoundaries.CircularBoundaries?.Count == 1 + analyticFace.InnerLoops.Count)
                {
                    bounds.Add(AddCircularLoop(curvedWithBoundaries.CircularBoundaries[0], isOuter: true, component));
                    foreach (var circularBoundary in curvedWithBoundaries.CircularBoundaries.Skip(1))
                        bounds.Add(AddCircularLoop(circularBoundary, isOuter: false, component));
                }
                else if (analyticFace is PlanarFace planarWithBoundaries &&
                         planarWithBoundaries.CircularBoundaries?.Count == 1 + analyticFace.InnerLoops.Count)
                {
                    var orderedLoops = new[] { analyticFace.OuterLoop }.Concat(analyticFace.InnerLoops).ToArray();
                    for (var loopIndex = 0; loopIndex < orderedLoops.Length; loopIndex++)
                    {
                        var circularBoundary = planarWithBoundaries.CircularBoundaries[loopIndex];
                        bounds.Add(circularBoundary == null
                            ? AddLoop(orderedLoops[loopIndex], isOuter: loopIndex == 0)
                            : AddCircularLoop(circularBoundary, isOuter: loopIndex == 0, component));
                    }
                }
                else
                {
                    bounds.Add(AddLoop(analyticFace.OuterLoop, isOuter: true));
                    foreach (var innerLoop in analyticFace.InnerLoops)
                        bounds.Add(AddLoop(innerLoop, isOuter: false));
                }
                var faceEntity = analyticFace is CurvedFace ? "ADVANCED_FACE" : "FACE_SURFACE";
                var faceId = Add($"{faceEntity}('',({References(bounds)}),#{surfaceId},{(analyticFace.SameSense ? ".T." : ".F.")})");
                AddFaceToComponent(analyticFace.TriangleIndices.First(), faceId);
            }
        }

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var triangleIndex = i / 3;
            var a = triangles[i];
            var b = triangles[i + 1];
            var c = triangles[i + 2];
            if (!TryGetFrame(vertices, a, b, c, out var normal, out var reference))
                continue;
            if (consolidatedTriangleIndices.Contains(triangleIndex))
                continue;

            var normalId = Add($"DIRECTION('',({Number(normal.X)},{Number(normal.Y)},{Number(normal.Z)}))");
            var referenceId = Add($"DIRECTION('',({Number(reference.X)},{Number(reference.Y)},{Number(reference.Z)}))");
            var placementId = Add($"AXIS2_PLACEMENT_3D('',#{pointIds[a]},#{normalId},#{referenceId})");
            var planeId = Add($"PLANE('',#{placementId})");
            var loopId = Add($"POLY_LOOP('',(#{pointIds[a]},#{pointIds[b]},#{pointIds[c]}))");
            var boundId = Add($"FACE_OUTER_BOUND('',#{loopId},.T.)");
            var faceId = Add($"FACE_SURFACE('',(#{boundId}),#{planeId},.T.)");
            AddFaceToComponent(triangleIndex, faceId);
        }

        if (faceIdsByComponent.Count == 0)
            return false;

        var shapeItemIds = new List<int>();
        var allClosed = true;
        foreach (var (component, faceIds) in faceIdsByComponent.OrderBy(item => item.Key))
        {
            var isClosed = topology.ClosedComponents.Contains(component);
            allClosed &= isClosed;
            var shellId = Add($"{(isClosed ? "CLOSED_SHELL" : "OPEN_SHELL")}('',({References(faceIds)}))");
            shapeItemIds.Add(Add(isClosed
                ? componentsWithCurvedFaces.Contains(component)
                    ? $"MANIFOLD_SOLID_BREP('',#{shellId})"
                    : $"FACETED_BREP('',#{shellId})"
                : $"SHELL_BASED_SURFACE_MODEL('',(#{shellId}))"));
        }
        var originId = Add("CARTESIAN_POINT('',(0.,0.,0.))");
        var zId = Add("DIRECTION('',(0.,0.,1.))");
        var xId = Add("DIRECTION('',(1.,0.,0.))");
        var axisId = Add($"AXIS2_PLACEMENT_3D('',#{originId},#{zId},#{xId})");
        var lengthUnitId = Add("(LENGTH_UNIT() NAMED_UNIT(*) SI_UNIT(.MILLI.,.METRE.))");
        var angleUnitId = Add("(NAMED_UNIT(*) PLANE_ANGLE_UNIT() SI_UNIT($,.RADIAN.))");
        var solidAngleUnitId = Add("(NAMED_UNIT(*) SI_UNIT($,.STERADIAN.) SOLID_ANGLE_UNIT())");
        var uncertaintyId = Add($"UNCERTAINTY_MEASURE_WITH_UNIT(LENGTH_MEASURE(1.E-07),#{lengthUnitId},'distance_accuracy_value','confusion accuracy')");
        var representationContextId = Add(
            $"(GEOMETRIC_REPRESENTATION_CONTEXT(3) GLOBAL_UNCERTAINTY_ASSIGNED_CONTEXT((#{uncertaintyId})) " +
            $"GLOBAL_UNIT_ASSIGNED_CONTEXT((#{lengthUnitId},#{angleUnitId},#{solidAngleUnitId})) " +
            "REPRESENTATION_CONTEXT('Context #1','3D Context with UNIT and UNCERTAINTY'))");
        // An ADVANCED_BREP_SHAPE_REPRESENTATION describes one advanced B-rep.
        // Assemblies may contain multiple independent B-reps (and may mix
        // analytic and faceted items), so use the general container there.
        var representationType = shapeItemIds.Count > 1
            ? "SHAPE_REPRESENTATION"
            : allClosed
                ? "ADVANCED_BREP_SHAPE_REPRESENTATION"
                : "MANIFOLD_SURFACE_SHAPE_REPRESENTATION";
        var representationItems = new[] { axisId }.Concat(shapeItemIds);
        var representationId = Add($"{representationType}('',({References(representationItems)}),#{representationContextId})");
        Add($"SHAPE_DEFINITION_REPRESENTATION(#{definitionShape},#{representationId})");
        Add($"PRODUCT_RELATED_PRODUCT_CATEGORY('part',$,(#{product}))");

        var output = new StringBuilder(entities.Count * 80);
        output.AppendLine("ISO-10303-21;");
        output.AppendLine("HEADER;");
        output.AppendLine("FILE_DESCRIPTION(('TVGL faceted model'),'2;1');");
        output.Append("FILE_NAME('")
            .Append(Escape(string.IsNullOrWhiteSpace(headerFileName)
                ? Path.GetFileName(filePath)
                : Path.GetFileName(headerFileName)))
            .Append("','")
            .Append(DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss", Invariant))
            .AppendLine("',('TVGL'),('TVGL'),'TVGL STEP writer','TVGL','');");
        output.AppendLine("FILE_SCHEMA(('AUTOMOTIVE_DESIGN { 1 0 10303 214 1 1 1 1 }'));");
        output.AppendLine("ENDSEC;");
        output.AppendLine("DATA;");
        for (var i = 0; i < entities.Count; i++)
            output.Append('#').Append(i + 1).Append(" = ").Append(entities[i]).AppendLine(";");
        output.AppendLine("ENDSEC;");
        output.AppendLine("END-ISO-10303-21;");

        File.WriteAllText(filePath, output.ToString(), new UTF8Encoding(false));
        return true;

        void AddFaceToComponent(int triangleIndex, int faceId)
        {
            if (triangleIndex < 0 || triangleIndex >= topology.ComponentByTriangle.Length)
                return;
            var component = topology.ComponentByTriangle[triangleIndex];
            if (!faceIdsByComponent.TryGetValue(component, out var ids))
                faceIdsByComponent[component] = ids = new List<int>();
            ids.Add(faceId);
        }

        int AddLoop(IReadOnlyList<int> loop, bool isOuter)
        {
            var loopId = Add($"POLY_LOOP('',({string.Join(",", loop.Select(index => $"#{pointIds[index]}"))}))");
            return Add($"{(isOuter ? "FACE_OUTER_BOUND" : "FACE_BOUND")}('',#{loopId},.T.)");
        }


        int AddCircularLoop(CircularBoundary boundary, bool isOuter, int component)
        {
            var circle = GetOrAddCircleEdge(boundary, component);
            var orientedEdgeId = Add($"ORIENTED_EDGE('',*,*,#{circle.EdgeId},.T.)");
            var loopId = Add($"EDGE_LOOP('',(#{orientedEdgeId}))");
            return Add($"{(isOuter ? "FACE_OUTER_BOUND" : "FACE_BOUND")}('',#{loopId},.T.)");
        }

        int AddCylindricalLoop(CurvedFace face, int surfaceId, int component)
        {
            TryNormalize(face.Axis, out var axis);
            var first = face.CircularBoundaries![0];
            var second = face.CircularBoundaries[1];
            var firstHeight =
                (first.Center.X - face.Origin.X) * axis.X +
                (first.Center.Y - face.Origin.Y) * axis.Y +
                (first.Center.Z - face.Origin.Z) * axis.Z;
            var secondHeight =
                (second.Center.X - face.Origin.X) * axis.X +
                (second.Center.Y - face.Origin.Y) * axis.Y +
                (second.Center.Z - face.Origin.Z) * axis.Z;
            var firstCircle = GetOrAddCylinderCircleEdge(first, surfaceId, firstHeight, axis, component);
            var secondCircle = GetOrAddCylinderCircleEdge(second, surfaceId, secondHeight, axis, component);
            var firstPoint = firstCircle.Point;
            var secondPoint = secondCircle.Point;
            var firstPointId = Add($"CARTESIAN_POINT('',({Number(firstPoint.X)},{Number(firstPoint.Y)},{Number(firstPoint.Z)}))");
            var seamDirection = (
                X: secondPoint.X - firstPoint.X,
                Y: secondPoint.Y - firstPoint.Y,
                Z: secondPoint.Z - firstPoint.Z);
            var seamLength = Math.Sqrt(seamDirection.X * seamDirection.X + seamDirection.Y * seamDirection.Y + seamDirection.Z * seamDirection.Z);
            TryNormalize(seamDirection, out seamDirection);
            var seamDirectionId = Add($"DIRECTION('',({Number(seamDirection.X)},{Number(seamDirection.Y)},{Number(seamDirection.Z)}))");
            var seamVectorId = Add($"VECTOR('',#{seamDirectionId},{Number(seamLength)})");
            var seamLineId = Add($"LINE('',#{firstPointId},#{seamVectorId})");
            var seamParameterDirection = secondHeight >= firstHeight ? 1.0 : -1.0;
            var seamPcurve1Id = AddPcurveLine(surfaceId, 0, firstHeight, 0, seamParameterDirection);
            var seamPcurve2Id = AddPcurveLine(surfaceId, 2 * Math.PI, firstHeight, 0, seamParameterDirection);
            var seamCurveId = Add($"SEAM_CURVE('',#{seamLineId},(#{seamPcurve1Id},#{seamPcurve2Id}),.PCURVE_S1.)");
            var seamEdgeId = Add($"EDGE_CURVE('',#{firstCircle.VertexId},#{secondCircle.VertexId},#{seamCurveId},.T.)");
            var firstCircleOrientedId = Add($"ORIENTED_EDGE('',*,*,#{firstCircle.EdgeId},{(face.SameSense ? ".T." : ".F.")})");
            var seamForwardId = Add($"ORIENTED_EDGE('',*,*,#{seamEdgeId},.T.)");
            var secondCircleOrientedId = Add($"ORIENTED_EDGE('',*,*,#{secondCircle.EdgeId},{(face.SameSense ? ".F." : ".T.")})");
            var seamBackwardId = Add($"ORIENTED_EDGE('',*,*,#{seamEdgeId},.F.)");
            var loopId = Add($"EDGE_LOOP('',(#{firstCircleOrientedId},#{seamForwardId},#{secondCircleOrientedId},#{seamBackwardId}))");
            return Add($"FACE_BOUND('',#{loopId},{(face.SameSense ? ".T." : ".F.")})");
        }

        (int EdgeId, int VertexId, (double X, double Y, double Z) Point) GetOrAddCylinderCircleEdge(
            CircularBoundary boundary,
            int surfaceId,
            double height,
            (double X, double Y, double Z) surfaceAxis,
            int component)
        {
            var key = CircleKey(boundary, component);
            if (circularEdges.TryGetValue(key, out var existing))
                return existing;
            // Both end circles of a cylinder must use the same angular seam.
            // Picking a vertex independently from each tessellated ring can
            // select slightly different angles and create a diagonal seam.
            var geometry = AddCircleGeometry(
                boundary,
                surfaceAxis,
                Perpendicular(surfaceAxis));
            var cylinderPcurveId = AddPcurveLine(surfaceId, 0, height, 1, 0);
            var pcurveIds = new List<int>();
            var adjacentPlane = analyticFaces?.OfType<PlanarFace>().FirstOrDefault(plane =>
                topology.ComponentByTriangle[plane.TriangleIndices.First()] == component &&
                plane.CircularBoundaries?.Any(candidate =>
                    candidate != null && CircleKey(candidate, component) == key) == true);
            if (adjacentPlane != null && surfaceIds.TryGetValue(adjacentPlane, out var planeSurfaceId) &&
                planarFrames.TryGetValue(adjacentPlane, out var planeFrame))
            {
                var offset = (
                    X: boundary.Center.X - planeFrame.Origin.X,
                    Y: boundary.Center.Y - planeFrame.Origin.Y,
                    Z: boundary.Center.Z - planeFrame.Origin.Z);
                var centerX = offset.X * planeFrame.XAxis.X + offset.Y * planeFrame.XAxis.Y + offset.Z * planeFrame.XAxis.Z;
                var centerY = offset.X * planeFrame.YAxis.X + offset.Y * planeFrame.YAxis.Y + offset.Z * planeFrame.YAxis.Z;
                pcurveIds.Add(AddPcurveCircle(planeSurfaceId, centerX, centerY, boundary.Radius));
            }
            pcurveIds.Add(cylinderPcurveId);
            var surfaceCurveId = Add($"SURFACE_CURVE('',#{geometry.CircleId},({References(pcurveIds)}),.PCURVE_S1.)");
            var edgeId = Add($"EDGE_CURVE('',#{geometry.VertexId},#{geometry.VertexId},#{surfaceCurveId},.T.)");
            return circularEdges[key] = (edgeId, geometry.VertexId, geometry.Point);
        }

        (int EdgeId, int VertexId, (double X, double Y, double Z) Point) GetOrAddCircleEdge(
            CircularBoundary boundary,
            int component)
        {
            var key = CircleKey(boundary, component);
            if (circularEdges.TryGetValue(key, out var existing))
                return existing;
            var geometry = AddCircleGeometry(boundary);
            var edgeId = Add($"EDGE_CURVE('',#{geometry.VertexId},#{geometry.VertexId},#{geometry.CircleId},.T.)");
            return circularEdges[key] = (edgeId, geometry.VertexId, geometry.Point);
        }

        string CircleKey(CircularBoundary boundary, int component)
        {
            static long Quantize(double value) => (long)Math.Round(value * 1.0e7);
            return $"{component}:{Quantize(boundary.Center.X)}:{Quantize(boundary.Center.Y)}:{Quantize(boundary.Center.Z)}:{Quantize(boundary.Radius)}";
        }

        (int CircleId, int VertexId, (double X, double Y, double Z) Point) AddCircleGeometry(
            CircularBoundary boundary,
            (double X, double Y, double Z)? normalOverride = null,
            (double X, double Y, double Z)? referenceOverride = null)
        {
            var centerId = Add($"CARTESIAN_POINT('',({Number(boundary.Center.X)},{Number(boundary.Center.Y)},{Number(boundary.Center.Z)}))");
            var normal = normalOverride ?? boundary.Normal;
            var normalId = Add($"DIRECTION('',({Number(normal.X)},{Number(normal.Y)},{Number(normal.Z)}))");
            var reference = referenceOverride ??
                            DirectionBetweenPointAndVertex(vertices, boundary.Center, boundary.VertexIndex) ??
                            Perpendicular(boundary.Normal);
            TryNormalize(reference, out reference);
            var referenceId = Add($"DIRECTION('',({Number(reference.X)},{Number(reference.Y)},{Number(reference.Z)}))");
            var placementId = Add($"AXIS2_PLACEMENT_3D('',#{centerId},#{normalId},#{referenceId})");
            var circleId = Add($"CIRCLE('',#{placementId},{Number(boundary.Radius)})");
            (double X, double Y, double Z) point;
            int vertexPointId;
            if (referenceOverride != null)
            {
                point = (
                    X: boundary.Center.X + boundary.Radius * reference.X,
                    Y: boundary.Center.Y + boundary.Radius * reference.Y,
                    Z: boundary.Center.Z + boundary.Radius * reference.Z);
                vertexPointId = Add($"CARTESIAN_POINT('',({Number(point.X)},{Number(point.Y)},{Number(point.Z)}))");
            }
            else
            {
                point = (
                    X: vertices[boundary.VertexIndex * 3],
                    Y: vertices[boundary.VertexIndex * 3 + 1],
                    Z: vertices[boundary.VertexIndex * 3 + 2]);
                vertexPointId = pointIds[boundary.VertexIndex];
            }
            var vertexId = Add($"VERTEX_POINT('',#{vertexPointId})");
            return (circleId, vertexId, point);
        }

        int AddPcurveLine(int surfaceId, double x, double y, double directionX, double directionY)
        {
            var pointId = Add($"CARTESIAN_POINT('',({Number(x)},{Number(y)}))");
            var directionId = Add($"DIRECTION('',({Number(directionX)},{Number(directionY)}))");
            var vectorId = Add($"VECTOR('',#{directionId},1.)");
            var lineId = Add($"LINE('',#{pointId},#{vectorId})");
            var contextId = Add("(GEOMETRIC_REPRESENTATION_CONTEXT(2) PARAMETRIC_REPRESENTATION_CONTEXT() REPRESENTATION_CONTEXT('2D SPACE',''))");
            var representationId = Add($"DEFINITIONAL_REPRESENTATION('',(#{lineId}),#{contextId})");
            return Add($"PCURVE('',#{surfaceId},#{representationId})");
        }

        int AddPcurveCircle(int surfaceId, double centerX, double centerY, double radius)
        {
            var centerId = Add($"CARTESIAN_POINT('',({Number(centerX)},{Number(centerY)}))");
            var directionId = Add("DIRECTION('',(1.,0.))");
            var placementId = Add($"AXIS2_PLACEMENT_2D('',#{centerId},#{directionId})");
            var circleId = Add($"CIRCLE('',#{placementId},{Number(radius)})");
            var contextId = Add("(GEOMETRIC_REPRESENTATION_CONTEXT(2) PARAMETRIC_REPRESENTATION_CONTEXT() REPRESENTATION_CONTEXT('2D SPACE',''))");
            var representationId = Add($"DEFINITIONAL_REPRESENTATION('',(#{circleId}),#{contextId})");
            return Add($"PCURVE('',#{surfaceId},#{representationId})");
        }

        IReadOnlyList<int> AddSphericalFaces(CurvedFace face, int surfaceId)
        {
            var center = face.Origin;
            var radius = face.FirstRadius;
            var directions = new[]
            {
                (X: 1.0, Y: 0.0, Z: 0.0),
                (X: -1.0, Y: 0.0, Z: 0.0),
                (X: 0.0, Y: 1.0, Z: 0.0),
                (X: 0.0, Y: -1.0, Z: 0.0),
                (X: 0.0, Y: 0.0, Z: 1.0),
                (X: 0.0, Y: 0.0, Z: -1.0)
            };
            var vertexIds = new int[directions.Length];
            for (var index = 0; index < directions.Length; index++)
            {
                var direction = directions[index];
                var pointId = Add($"CARTESIAN_POINT('',({Number(center.X + radius * direction.X)},{Number(center.Y + radius * direction.Y)},{Number(center.Z + radius * direction.Z)}))");
                vertexIds[index] = Add($"VERTEX_POINT('',#{pointId})");
            }

            var edges = new Dictionary<(int Min, int Max), (int EdgeId, int Start)>();
            (int EdgeId, bool Forward) GetEdge(int from, int to)
            {
                var key = from < to ? (from, to) : (to, from);
                if (edges.TryGetValue(key, out var existing))
                    return (existing.EdgeId, existing.Start == from);
                var fromDirection = directions[from];
                var toDirection = directions[to];
                var normal = (
                    X: fromDirection.Y * toDirection.Z - fromDirection.Z * toDirection.Y,
                    Y: fromDirection.Z * toDirection.X - fromDirection.X * toDirection.Z,
                    Z: fromDirection.X * toDirection.Y - fromDirection.Y * toDirection.X);
                TryNormalize(normal, out normal);
                var centerId = Add($"CARTESIAN_POINT('',({Number(center.X)},{Number(center.Y)},{Number(center.Z)}))");
                var normalId = Add($"DIRECTION('',({Number(normal.X)},{Number(normal.Y)},{Number(normal.Z)}))");
                var referenceId = Add($"DIRECTION('',({Number(fromDirection.X)},{Number(fromDirection.Y)},{Number(fromDirection.Z)}))");
                var placementId = Add($"AXIS2_PLACEMENT_3D('',#{centerId},#{normalId},#{referenceId})");
                var circleId = Add($"CIRCLE('',#{placementId},{Number(radius)})");
                var edgeId = Add($"EDGE_CURVE('',#{vertexIds[from]},#{vertexIds[to]},#{circleId},.T.)");
                edges[key] = (edgeId, from);
                return (edgeId, true);
            }

            var result = new List<int>(8);
            foreach (var xPositive in new[] { false, true })
            foreach (var yPositive in new[] { false, true })
            foreach (var zPositive in new[] { false, true })
            {
                var x = xPositive ? 0 : 1;
                var y = yPositive ? 2 : 3;
                var z = zPositive ? 4 : 5;
                var positiveProduct = (xPositive ? 1 : -1) *
                                      (yPositive ? 1 : -1) *
                                      (zPositive ? 1 : -1) > 0;
                var loopVertices = positiveProduct
                    ? new[] { x, y, z }
                    : new[] { x, z, y };
                var orientedEdges = new List<int>(3);
                for (var edgeIndex = 0; edgeIndex < 3; edgeIndex++)
                {
                    var edge = GetEdge(loopVertices[edgeIndex], loopVertices[(edgeIndex + 1) % 3]);
                    orientedEdges.Add(Add($"ORIENTED_EDGE('',*,*,#{edge.EdgeId},{(edge.Forward ? ".T." : ".F.")})"));
                }
                var loopId = Add($"EDGE_LOOP('',({References(orientedEdges)}))");
                var boundId = Add($"FACE_OUTER_BOUND('',#{loopId},.T.)");
                result.Add(Add($"ADVANCED_FACE('',(#{boundId}),#{surfaceId},{(face.SameSense ? ".T." : ".F.")})"));
            }
            return result;
        }

        IReadOnlyList<int> AddToroidalFaces(CurvedFace face, int surfaceId)
        {
            TryNormalize(face.Axis, out var zAxis);
            var xAxis = Perpendicular(zAxis);
            var yAxis = (
                X: zAxis.Y * xAxis.Z - zAxis.Z * xAxis.Y,
                Y: zAxis.Z * xAxis.X - zAxis.X * xAxis.Z,
                Z: zAxis.X * xAxis.Y - zAxis.Y * xAxis.X);
            TryNormalize(yAxis, out yAxis);
            var center = face.Origin;
            var majorRadius = face.FirstRadius;
            var minorRadius = face.SecondRadius;
            const int divisions = 4;
            var vertexIds = new int[divisions, divisions];
            var points = new (double X, double Y, double Z)[divisions, divisions];
            for (var uIndex = 0; uIndex < divisions; uIndex++)
            {
                var u = uIndex * Math.PI / 2;
                var radial = (
                    X: Math.Cos(u) * xAxis.X + Math.Sin(u) * yAxis.X,
                    Y: Math.Cos(u) * xAxis.Y + Math.Sin(u) * yAxis.Y,
                    Z: Math.Cos(u) * xAxis.Z + Math.Sin(u) * yAxis.Z);
                for (var vIndex = 0; vIndex < divisions; vIndex++)
                {
                    var v = vIndex * Math.PI / 2;
                    var radialDistance = majorRadius + minorRadius * Math.Cos(v);
                    var point = (
                        X: center.X + radialDistance * radial.X + minorRadius * Math.Sin(v) * zAxis.X,
                        Y: center.Y + radialDistance * radial.Y + minorRadius * Math.Sin(v) * zAxis.Y,
                        Z: center.Z + radialDistance * radial.Z + minorRadius * Math.Sin(v) * zAxis.Z);
                    points[uIndex, vIndex] = point;
                    var pointId = Add($"CARTESIAN_POINT('',({Number(point.X)},{Number(point.Y)},{Number(point.Z)}))");
                    vertexIds[uIndex, vIndex] = Add($"VERTEX_POINT('',#{pointId})");
                }
            }

            var uEdges = new int[divisions, divisions];
            var vEdges = new int[divisions, divisions];
            for (var uIndex = 0; uIndex < divisions; uIndex++)
            for (var vIndex = 0; vIndex < divisions; vIndex++)
            {
                var nextU = (uIndex + 1) % divisions;
                var nextV = (vIndex + 1) % divisions;
                var v = vIndex * Math.PI / 2;
                var majorCircleCenter = (
                    X: center.X + minorRadius * Math.Sin(v) * zAxis.X,
                    Y: center.Y + minorRadius * Math.Sin(v) * zAxis.Y,
                    Z: center.Z + minorRadius * Math.Sin(v) * zAxis.Z);
                var majorCircleRadius = majorRadius + minorRadius * Math.Cos(v);
                uEdges[uIndex, vIndex] = AddCircularArc(
                    vertexIds[uIndex, vIndex],
                    vertexIds[nextU, vIndex],
                    majorCircleCenter,
                    zAxis,
                    points[uIndex, vIndex],
                    majorCircleRadius);

                var u = uIndex * Math.PI / 2;
                var radial = (
                    X: Math.Cos(u) * xAxis.X + Math.Sin(u) * yAxis.X,
                    Y: Math.Cos(u) * xAxis.Y + Math.Sin(u) * yAxis.Y,
                    Z: Math.Cos(u) * xAxis.Z + Math.Sin(u) * yAxis.Z);
                var minorCircleCenter = (
                    X: center.X + majorRadius * radial.X,
                    Y: center.Y + majorRadius * radial.Y,
                    Z: center.Z + majorRadius * radial.Z);
                var minorCircleNormal = (
                    X: radial.Y * zAxis.Z - radial.Z * zAxis.Y,
                    Y: radial.Z * zAxis.X - radial.X * zAxis.Z,
                    Z: radial.X * zAxis.Y - radial.Y * zAxis.X);
                vEdges[uIndex, vIndex] = AddCircularArc(
                    vertexIds[uIndex, vIndex],
                    vertexIds[uIndex, nextV],
                    minorCircleCenter,
                    minorCircleNormal,
                    points[uIndex, vIndex],
                    minorRadius);
            }

            var result = new List<int>(divisions * divisions);
            for (var uIndex = 0; uIndex < divisions; uIndex++)
            for (var vIndex = 0; vIndex < divisions; vIndex++)
            {
                var nextU = (uIndex + 1) % divisions;
                var nextV = (vIndex + 1) % divisions;
                var orientedEdges = new[]
                {
                    Add($"ORIENTED_EDGE('',*,*,#{uEdges[uIndex, vIndex]},.T.)"),
                    Add($"ORIENTED_EDGE('',*,*,#{vEdges[nextU, vIndex]},.T.)"),
                    Add($"ORIENTED_EDGE('',*,*,#{uEdges[uIndex, nextV]},.F.)"),
                    Add($"ORIENTED_EDGE('',*,*,#{vEdges[uIndex, vIndex]},.F.)")
                };
                var loopId = Add($"EDGE_LOOP('',({References(orientedEdges)}))");
                var boundId = Add($"FACE_OUTER_BOUND('',#{loopId},.T.)");
                result.Add(Add($"ADVANCED_FACE('',(#{boundId}),#{surfaceId},{(face.SameSense ? ".T." : ".F.")})"));
            }
            return result;

            int AddCircularArc(
                int startVertexId,
                int endVertexId,
                (double X, double Y, double Z) circleCenter,
                (double X, double Y, double Z) circleNormal,
                (double X, double Y, double Z) startPoint,
                double circleRadius)
            {
                TryNormalize(circleNormal, out circleNormal);
                var reference = (
                    X: (startPoint.X - circleCenter.X) / circleRadius,
                    Y: (startPoint.Y - circleCenter.Y) / circleRadius,
                    Z: (startPoint.Z - circleCenter.Z) / circleRadius);
                TryNormalize(reference, out reference);
                var centerId = Add($"CARTESIAN_POINT('',({Number(circleCenter.X)},{Number(circleCenter.Y)},{Number(circleCenter.Z)}))");
                var normalId = Add($"DIRECTION('',({Number(circleNormal.X)},{Number(circleNormal.Y)},{Number(circleNormal.Z)}))");
                var referenceId = Add($"DIRECTION('',({Number(reference.X)},{Number(reference.Y)},{Number(reference.Z)}))");
                var placementId = Add($"AXIS2_PLACEMENT_3D('',#{centerId},#{normalId},#{referenceId})");
                var circleId = Add($"CIRCLE('',#{placementId},{Number(circleRadius)})");
                return Add($"EDGE_CURVE('',#{startVertexId},#{endVertexId},#{circleId},.T.)");
            }
        }
    }

    private static bool TryNormalize(
        (double X, double Y, double Z) value,
        out (double X, double Y, double Z) normalized)
    {
        var length = Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z);
        if (length <= 1.0e-12)
        {
            normalized = default;
            return false;
        }
        normalized = (value.X / length, value.Y / length, value.Z / length);
        return true;
    }

    private static (double X, double Y, double Z) Perpendicular((double X, double Y, double Z) axis)
    {
        var candidate = Math.Abs(axis.X) < 0.9
            ? (X: 1.0, Y: 0.0, Z: 0.0)
            : (X: 0.0, Y: 1.0, Z: 0.0);
        var dot = candidate.X * axis.X + candidate.Y * axis.Y + candidate.Z * axis.Z;
        TryNormalize((candidate.X - dot * axis.X, candidate.Y - dot * axis.Y, candidate.Z - dot * axis.Z), out var result);
        return result;
    }

    private sealed record MeshTopology(int[] ComponentByTriangle, HashSet<int> ClosedComponents);

    private static MeshTopology BuildTopology(double[] vertices, int[] triangles)
    {
        var triangleCount = triangles.Length / 3;
        var parents = Enumerable.Range(0, triangleCount).ToArray();
        var edgeUses = new Dictionary<(int Min, int Max), (int FirstTriangle, int Count)>();

        int Find(int item)
        {
            while (parents[item] != item)
            {
                parents[item] = parents[parents[item]];
                item = parents[item];
            }
            return item;
        }
        void Union(int first, int second)
        {
            first = Find(first);
            second = Find(second);
            if (first != second)
                parents[second] = first;
        }
        void AddEdge(int first, int second, int triangleIndex)
        {
            var edge = first < second ? (first, second) : (second, first);
            if (edgeUses.TryGetValue(edge, out var use))
            {
                Union(triangleIndex, use.FirstTriangle);
                edgeUses[edge] = (use.FirstTriangle, use.Count + 1);
            }
            else
                edgeUses[edge] = (triangleIndex, 1);
        }

        for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            var offset = triangleIndex * 3;
            var a = triangles[offset];
            var b = triangles[offset + 1];
            var c = triangles[offset + 2];
            if (!TryGetFrame(vertices, a, b, c, out _, out _))
                continue;
            AddEdge(a, b, triangleIndex);
            AddEdge(b, c, triangleIndex);
            AddEdge(c, a, triangleIndex);
        }

        var components = Enumerable.Range(0, triangleCount).Select(Find).ToArray();
        var closed = components.ToHashSet();
        foreach (var use in edgeUses.Values)
            if (use.Count != 2)
                closed.Remove(components[use.FirstTriangle]);
        return new MeshTopology(components, closed);
    }

    private static (double X, double Y, double Z)? DirectionBetween(double[] vertices, int from, int to)
    {
        var vertexCount = vertices.Length / 3;
        if (from < 0 || from >= vertexCount || to < 0 || to >= vertexCount)
            return null;
        var direction = (X: vertices[to * 3] - vertices[from * 3],
            Y: vertices[to * 3 + 1] - vertices[from * 3 + 1],
            Z: vertices[to * 3 + 2] - vertices[from * 3 + 2]);
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y + direction.Z * direction.Z);
        return length <= 1.0e-12
            ? null
            : (direction.X / length, direction.Y / length, direction.Z / length);
    }

    private static (double X, double Y, double Z)? DirectionBetweenPointAndVertex(
        double[] vertices,
        (double X, double Y, double Z) point,
        int vertexIndex)
    {
        var vertexCount = vertices.Length / 3;
        if (vertexIndex < 0 || vertexIndex >= vertexCount)
            return null;
        return Normalize((
            vertices[vertexIndex * 3] - point.X,
            vertices[vertexIndex * 3 + 1] - point.Y,
            vertices[vertexIndex * 3 + 2] - point.Z));
    }

    private static (double X, double Y, double Z)? Normalize((double X, double Y, double Z) value)
        => TryNormalize(value, out var normalized) ? normalized : null;

    private static bool TryGetFrame(
        double[] vertices,
        int a,
        int b,
        int c,
        out (double X, double Y, double Z) normal,
        out (double X, double Y, double Z) reference)
    {
        normal = default;
        reference = default;
        var vertexCount = vertices.Length / 3;
        if (a < 0 || a >= vertexCount || b < 0 || b >= vertexCount || c < 0 || c >= vertexCount ||
            a == b || b == c || c == a)
            return false;

        var ab = (X: vertices[b * 3] - vertices[a * 3],
            Y: vertices[b * 3 + 1] - vertices[a * 3 + 1],
            Z: vertices[b * 3 + 2] - vertices[a * 3 + 2]);
        var ac = (X: vertices[c * 3] - vertices[a * 3],
            Y: vertices[c * 3 + 1] - vertices[a * 3 + 1],
            Z: vertices[c * 3 + 2] - vertices[a * 3 + 2]);
        var cross = (X: ab.Y * ac.Z - ab.Z * ac.Y,
            Y: ab.Z * ac.X - ab.X * ac.Z,
            Z: ab.X * ac.Y - ab.Y * ac.X);
        var normalLength = Math.Sqrt(cross.X * cross.X + cross.Y * cross.Y + cross.Z * cross.Z);
        var referenceLength = Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y + ab.Z * ab.Z);
        if (normalLength <= 1.0e-12 || referenceLength <= 1.0e-12)
            return false;

        normal = (cross.X / normalLength, cross.Y / normalLength, cross.Z / normalLength);
        reference = (ab.X / referenceLength, ab.Y / referenceLength, ab.Z / referenceLength);
        return true;
    }

    private static string Number(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentException("STEP coordinates must be finite.");
        var result = value.ToString("G17", Invariant);
        return result.Contains('.') || result.Contains('E') || result.Contains('e') ? result : result + ".";
    }

    private static string References(IEnumerable<int> ids) => string.Join(",", ids.Select(id => $"#{id}"));

    private static string Escape(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
