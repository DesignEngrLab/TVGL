using BugViewer;
using TVGL;
using Color = TVGL.Color;

namespace WebGPUPresenter;

/// <summary>Displays TVGL three-dimensional geometry in the WebGPU presenter.</summary>
public sealed class Presenter3D : IPresenter3D
{
    private static readonly Lazy<LocalPresenterHost> SharedHostInstance = new(() => new LocalPresenterHost());
    internal static LocalPresenterHost SharedHost => SharedHostInstance.Value;
    private readonly LocalPresenterHost presenterHost;

    public Presenter3D()
    {
        presenterHost = SharedHostInstance.Value;
        presenterHost.WaitReady();
    }

    #region Show and Hang

    /// <summary>Displays a solid and waits until the browser presenter releases it.</summary>
    public void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "")
        => ShowAndHang([solid], heading, title, subtitle);

    public void ShowAndHang(Solid solid, Action<(TriangleFace face, Vector3 point)> onSelection, string heading = "", string title = "", string subtitle = "")
    {
        ArgumentNullException.ThrowIfNull(onSelection);
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Heading = heading,
            Title = title,
            Subtitle = subtitle,
            OnSelection = onSelection,
            ShowSurfacesAs = MeshFaceDisplay.Triangles
        };
        AddSolid(scene, solid);
        presenterHost.Show(scene);
    }

    public void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Heading = heading,
            Title = title,
            Subtitle = subtitle,
        };
        var primitivesExist = true;
        foreach (var solid in solids)
        {
            AddSolid(scene, solid);
            if (solid.Primitives is null || solid.Primitives.Count == 0)
                primitivesExist = false;
        }
        if (!primitivesExist)
            scene = new SceneRequest
            {
                RequestId = Guid.NewGuid(),
                Meshes = scene.Meshes,
                Paths = scene.Paths,
                PathBatches = scene.PathBatches,
                PointSets = scene.PointSets,
                Heading = heading,
                Title = title,
                Subtitle = subtitle,
                ShowSurfacesAs = MeshFaceDisplay.Triangles
            };
        presenterHost.Show(scene);
    }

    public void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Heading = heading,
            Title = title,
            Subtitle = subtitle,
            ShowMeshBorders = false,
            ShowSurfacesAs = MeshFaceDisplay.Triangles
        };
        scene.Meshes.Add(CreateMesh(faces, ColorRgba.LightGray, false, "faces"));
        presenterHost.Show(scene);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null,
        IEnumerable<Color>? colors = null,
        bool otherwiseRandomPathColors = false,
        params Solid[] solids)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
        };
        AddPaths(scene, paths, closePaths, lineThicknesses, colors, expandMissingColors: true);

        var primitivesExist = true;
        foreach (var solid in solids)
        {
            AddSolid(scene, solid);
            if (solid.Primitives is null || solid.Primitives.Count == 0)
                primitivesExist = false;
        }
        if (!primitivesExist)
            scene = new SceneRequest
            {
                Meshes = scene.Meshes,
                Paths = scene.Paths,
                PathBatches = scene.PathBatches,
                PointSets = scene.PointSets,
                RequestId = scene.RequestId,
                ShowSurfacesAs = MeshFaceDisplay.Triangles
            };
        presenterHost.Show(scene);
    }

    public void ShowAndHang(
        IEnumerable<Vector3> path,
        bool closePaths = false,
        double lineThickness = -1,
        Color? color = null,
        params Solid[] solids)
    {
        ShowAndHang(
            [path],
            [closePaths],
            [lineThickness],
            [color ?? new Color(KnownColors.Black)],
            false,
            solids);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths,
        IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null,
        IEnumerable<Color>? colors = null,
        params Solid[] solids)
    {
        var pathGroups = paths.Select(pathSet => pathSet.ToList()).ToList();
        var flattenedPaths = pathGroups.SelectMany(pathSet => pathSet);
        IList<Color>? colorList = colors?.ToList();
        if (colorList is null)
            colorList = Color.Distinct64Colors;
        var expandedColors =
             pathGroups.SelectMany((pathSet, setIndex) =>
                Enumerable.Repeat(
                    setIndex < colorList.Count ? colorList[setIndex]
                    : Color.GetRandomColors().First(),  // new Color(KnownColors.Black),
                    pathSet.Count))
                .ToList();

        ShowAndHang(flattenedPaths, closePaths, lineThicknesses, expandedColors, true, solids);
    }

    public void ShowAndHang(
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths,
        IEnumerable<double>? lineThicknesses,
        IEnumerable<Color>? colors,
        IEnumerable<TriangleFace>? faces)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            ShowMeshBorders = false,
            ShowSurfacesAs = MeshFaceDisplay.Triangles
        };
        AddPaths(scene, paths, closePaths, lineThicknesses, colors);
        if (faces is not null)
            scene.Meshes.Add(CreateMesh(faces, ColorRgba.LightGray, false, "faces"));
        presenterHost.Show(scene);
    }

    public void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color? color = null)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
        };
        scene.PointSets.Add(CreatePoints(points, radius, color ?? new Color(KnownColors.Red)));
        presenterHost.Show(scene);
    }

    public void ShowPointsAndHang(
        IEnumerable<IEnumerable<Vector3>> pointSets,
        double radius = 0,
        IEnumerable<Color>? colors = null)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
        };
        var palette = colors?.ToList() ?? [];
        var setIndex = 0;
        foreach (var pointSet in pointSets)
        {
            var color = setIndex < palette.Count ? palette[setIndex] : RandomColor(setIndex);
            scene.PointSets.Add(CreatePoints(pointSet, radius, color));
            setIndex++;
        }
        presenterHost.Show(scene);
    }

    public void ShowAndHangTransparentsAndSolids(
        IEnumerable<TessellatedSolid> a,
        IEnumerable<TessellatedSolid> b)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
        };
        foreach (var solid in b)
            AddSolid(scene, solid);
        foreach (var solid in a)
        {
            scene.Meshes.Add(CreateMesh(
                solid.Faces,
                new ColorRgba(solid.SolidColor.R, solid.SolidColor.G, solid.SolidColor.B, 89),
                solid.HasUniformColor,
                "transparent"));
        }
        presenterHost.Show(scene);
    }

    public void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
        };
        AddSolid(scene, solid);
        var radius = Math.Max(
            solid.XMax - solid.XMin,
            Math.Max(solid.YMax - solid.YMin, solid.ZMax - solid.ZMin)) / 2;
        var center = solid.Center;
        var vertexList = vertices.ToList();
        for (var index = 0; index < vertexList.Count; index++)
        {
            var path = new[] { center, center + vertexList[index].Coordinates * radius };
            var color = index < colors.Count ? colors[index] : new Color(KnownColors.Red);
            AddPaths(scene, [path], [false], [5.0], [color]);
        }
        presenterHost.Show(scene);
    }

    #endregion

    #region Publish

    /// <summary>Publishes a solid without blocking the calling thread.</summary>
    public void Show(
        Solid solid,
        string title = "",
        HoldType holdType = HoldType.Immediate,
        int timeToShow = -1,
        int id = -1)
    {
        Show([solid], title, holdType, timeToShow, id);
    }

    public void Show(
        ICollection<Solid> solids,
        string title = "",
        HoldType holdType = HoldType.Immediate,
        int timeToShow = -1,
        int id = -1)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Title = title,

            IsBlocking = false,
            PersistentId = id,
            HoldType = holdType,
            DisplayIntervalMilliseconds = timeToShow
        };
        foreach (var solid in solids)
            AddSolid(scene, solid);
        presenterHost.Publish(scene);
    }

    public void Show(
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths = null,
        IEnumerable<double>? lineThicknesses = null,
        IEnumerable<Color>? colors = null,
        string title = "",
        HoldType holdType = HoldType.Immediate,
        int timeToShow = -1,
        int id = -1,
        params Solid[] solids)
    {
        var scene = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            Title = title,
            IsBlocking = false,
            PersistentId = id,
            HoldType = holdType,
            DisplayIntervalMilliseconds = timeToShow
        };
        AddPaths(scene, paths, closePaths, lineThicknesses, colors, expandMissingColors: true);
        foreach (var solid in solids)
            AddSolid(scene, solid);

        presenterHost.Publish(scene);
    }


    public void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>>? paths,
        IList<IEnumerable<Matrix4x4>>? pathTransforms, IList<IEnumerable<Solid>>? solids,
        IList<IEnumerable<Matrix4x4>>? solidTransforms, IList<IEnumerable<bool>>? closePaths = null,
        IList<IEnumerable<double>>? lineThicknesses = null, IList<IEnumerable<Color>>? colors = null)
        => ShowStepsAndHang(paths, pathTransforms, solids, solidTransforms, closePaths,
            lineThicknesses, colors, null);

    public void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>>? paths,
        IList<IEnumerable<Matrix4x4>>? pathTransforms, IList<IEnumerable<Solid>>? solids,
        IList<IEnumerable<Matrix4x4>>? solidTransforms, IList<IEnumerable<bool>>? closePaths,
        IList<IEnumerable<double>>? lineThicknesses, IList<IEnumerable<Color>>? colors,
        SteppedPresentationOptions? options)
    {
        var faceGroups = new List<IEnumerable<IEnumerable<TriangleFace>>>();
        foreach (var solidGroup in solids ?? [])
        {
            var facesInGroup = new List<IEnumerable<TriangleFace>>();
            foreach (var solid in solidGroup ?? [])
            {
                IEnumerable<TriangleFace> faces = solid switch
                {
                    TessellatedSolid tessellatedSolid => tessellatedSolid.Faces,
                    ImplicitSolid implicitSolid => implicitSolid.ConvertToTessellatedSolid(1).Faces,
                    VoxelizedSolid voxelizedSolid => voxelizedSolid.ConvertToTessellatedSolidRectilinear().Faces,
                    _ => []
                };
                facesInGroup.Add(faces);
            }
            faceGroups.Add(facesInGroup);
        }
        ShowStepsAndHang(paths, pathTransforms, faceGroups, solidTransforms, closePaths,
            lineThicknesses, colors, options);
    }

    public void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>>? paths,
        IList<IEnumerable<Matrix4x4>>? pathTransformGroups,
        IList<IEnumerable<IEnumerable<TriangleFace>>>? faceGroups, IList<IEnumerable<Matrix4x4>>? fGTransforms,
        IList<IEnumerable<bool>>? closePaths = null, IList<IEnumerable<double>>? lineThicknesses = null,
        IList<IEnumerable<Color>>? pathColors = null)
        => ShowStepsAndHang(paths, pathTransformGroups, faceGroups, fGTransforms, closePaths,
            lineThicknesses, pathColors, null);

    public void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>>? paths,
        IList<IEnumerable<Matrix4x4>>? pathTransformGroups,
        IList<IEnumerable<IEnumerable<TriangleFace>>>? faceGroups, IList<IEnumerable<Matrix4x4>>? fGTransforms,
        IList<IEnumerable<bool>>? closePaths, IList<IEnumerable<double>>? lineThicknesses,
        IList<IEnumerable<Color>>? pathColors, SteppedPresentationOptions? options)
    {
        options?.Validate();
        var request = new SceneRequest
        {
            RequestId = Guid.NewGuid(),
            AutoResetCamera = UpdateTypes.Never,
            ShowMeshBorders = false
        };
        var actualPathGroups = paths ?? [];
        for (var groupIndex = 0; groupIndex < actualPathGroups.Count; groupIndex++)
        {
            var pathGroup = AsReadOnlyList(actualPathGroups[groupIndex]) ?? [];
            var closeValues = closePaths != null && groupIndex < closePaths.Count ? closePaths[groupIndex] : null;
            var thicknessValues = lineThicknesses != null && groupIndex < lineThicknesses.Count
                ? lineThicknesses[groupIndex]
                : null;
            var colorValues = pathColors != null && groupIndex < pathColors.Count ? pathColors[groupIndex] : null;
            var group = new SceneStepGroup
            {
                PathSource = new SceneStepPathSource
                {
                    Id = $"step-path-group-{request.RequestId:N}-{groupIndex}",
                    Paths = pathGroup,
                    ClosePaths = AsReadOnlyList(closeValues) ?? [],
                    Thicknesses = AsReadOnlyList(thicknessValues) ?? [],
                    Colors = AsReadOnlyList(colorValues) ?? [],
                    DefaultColor = Color.GetRandomColors().First(),
                    HistoryStepLimit = options?.PathHistoryStepLimit
                },
                Transforms = pathTransformGroups != null && groupIndex < pathTransformGroups.Count
                    ? AsReadOnlyList(pathTransformGroups[groupIndex])
                    : null
            };
            request.StepGroups.Add(group);
        }

        var actualFaceGroups = faceGroups ?? [];
        for (var groupIndex = 0; groupIndex < actualFaceGroups.Count; groupIndex++)
        {
            var faceGroup = actualFaceGroups[groupIndex] ?? [];
            var group = new SceneStepGroup
            {
                Meshes = faceGroup.Select(CreateStepMesh).ToList(),
                Transforms = fGTransforms != null && groupIndex < fGTransforms.Count
                    ? AsReadOnlyList(fGTransforms[groupIndex])
                    : null
            };
            request.StepGroups.Add(group);
        }
        presenterHost.Show(request);
    }

    #endregion

    private static void AddSolid(SceneRequest scene, Solid solid)
    {
        if (solid is TessellatedSolid tessellatedSolid)
        {
            var primitives = tessellatedSolid.Primitives?.Where(primitive => primitive.Faces?.Count > 0).ToList() ?? [];
            if (primitives.Count == 0)
            {
                scene.Meshes.Add(CreateMesh(tessellatedSolid.Faces,
                    new ColorRgba(tessellatedSolid.SolidColor.R, tessellatedSolid.SolidColor.G,
                        tessellatedSolid.SolidColor.B, tessellatedSolid.SolidColor.A),
                    tessellatedSolid.HasUniformColor, "solid"));
                return;
            }

            var primitiveFaces = primitives.SelectMany(primitive => primitive.Faces).ToHashSet();
            foreach (var primitive in primitives)
                scene.Meshes.Add(CreateMesh(primitive.Faces,
                    new ColorRgba(tessellatedSolid.SolidColor.R, tessellatedSolid.SolidColor.G,
                        tessellatedSolid.SolidColor.B, tessellatedSolid.SolidColor.A),
                    tessellatedSolid.HasUniformColor, "primitive", primitive));

            var unassignedFaces = tessellatedSolid.Faces.Where(face => !primitiveFaces.Contains(face)).ToList();
            if (unassignedFaces.Count > 0)
                scene.Meshes.Add(CreateMesh(unassignedFaces,
                    new ColorRgba(tessellatedSolid.SolidColor.R, tessellatedSolid.SolidColor.G,
                        tessellatedSolid.SolidColor.B, tessellatedSolid.SolidColor.A),
                    tessellatedSolid.HasUniformColor, "unclassified"));
        }
        else if (solid is CrossSectionSolid crossSectionSolid)
        {
            AddPaths(scene, crossSectionSolid.GetCrossSectionsAs3DLoops().SelectMany(loop => loop), null, null, null);
        }
        else if (solid is VoxelizedSolid voxelizedSolid)
        {
            var points = voxelizedSolid.GetExposedVoxels().Select(voxel => new[]
            {
                (float)(voxel.xIndex * voxelizedSolid.VoxelSideLength + voxelizedSolid.Offset.X),
                (float)(voxel.yIndex * voxelizedSolid.VoxelSideLength + voxelizedSolid.Offset.Y),
                (float)(voxel.zIndex * voxelizedSolid.VoxelSideLength + voxelizedSolid.Offset.Z)
            }).ToList();
            scene.PointSets.Add(new ScenePointSet
            {
                Id = $"voxels-{Guid.NewGuid():N}",
                Radius = Math.Max(1, voxelizedSolid.VoxelSideLength),
                Color = new ColorRgba(voxelizedSolid.SolidColor.R, voxelizedSolid.SolidColor.G,
                    voxelizedSolid.SolidColor.B, voxelizedSolid.SolidColor.A),
                Points = points
            });
        }
    }

    private static SceneMesh CreateMesh(IEnumerable<TriangleFace> faces, ColorRgba defaultColor,
        bool hasUniformColor, string idPrefix, PrimitiveSurface? primitiveOverride = null)
    {
        var faceList = faces.ToList();
        var vertices = new List<Vertex>();
        var primitiveSurfaceNormals = new List<float[]>();
        var triangles = new List<int[]>(faceList.Count);
        var indicesByVertexAndPrimitive = new Dictionary<(Vertex Vertex, PrimitiveSurface? Primitive), int>();
        var hasPrimitiveSurfaces = false;

        foreach (var face in faceList)
        {
            var primitive = primitiveOverride ?? face.BelongsToPrimitive;
            hasPrimitiveSurfaces |= primitive is not null;
            var faceVertices = face.Vertices.ToList();
            var triangle = new int[faceVertices.Count];
            for (var vertexIndex = 0; vertexIndex < faceVertices.Count; vertexIndex++)
            {
                var vertex = faceVertices[vertexIndex];
                var key = (vertex, primitive);
                if (!indicesByVertexAndPrimitive.TryGetValue(key, out var index))
                {
                    index = vertices.Count;
                    indicesByVertexAndPrimitive.Add(key, index);
                    vertices.Add(vertex);
                    primitiveSurfaceNormals.Add(GetPrimitiveSurfaceNormal(primitive, vertex.Coordinates));
                }
                triangle[vertexIndex] = index;
            }
            triangles.Add(triangle);
        }

        return new SceneMesh
        {
            Id = $"{idPrefix}-{Guid.NewGuid():N}",
            Vertices = vertices
                .Select(v => new[] { (float)v.X, (float)v.Y, (float)v.Z })
                .ToList(),
            Triangles = triangles,
            PrimitiveSurfaceNormals = primitiveSurfaceNormals,
            HasPrimitiveSurfaces = hasPrimitiveSurfaces,
            Colors = hasUniformColor
                ? [defaultColor]
                : faceList.Select(face => face.Color is null ? defaultColor
                : new ColorRgba(face.Color.R, face.Color.G, face.Color.B, face.Color.A)).ToList(),
            HasUniformColor = hasUniformColor,
            SourceFaces = faceList
        };
    }

    private static float[] GetPrimitiveSurfaceNormal(PrimitiveSurface? primitive, Vector3 point)
    {
        if (primitive is null)
            return [0f, 0f, 0f];

        var normal = primitive.GetNormalAtPoint(point);
        if (normal.IsNull() || normal.LengthSquared() <= 1e-20)
            return [0f, 0f, 0f];

        normal = normal.Normalize();
        return [(float)normal.X, (float)normal.Y, (float)normal.Z];
    }

    private static void AddPaths(
        SceneRequest scene,
        IEnumerable<IEnumerable<Vector3>> paths,
        IEnumerable<bool>? closePaths,
        IEnumerable<double>? lineThicknesses,
        IEnumerable<Color>? colors,
        bool expandMissingColors = false)
    {
        var closePathList = AsReadOnlyList(closePaths) ?? [];
        var lineThicknessList = AsReadOnlyList(lineThicknesses) ?? [];
        var colorList = AsReadOnlyList(colors) ?? [];
        var fallbackColors = expandMissingColors ? Color.Distinct64Colors.ToList() : [];
        using var randomColors = expandMissingColors ? Color.GetRandomColors().GetEnumerator() : null;

        scene.PathBatches.AddRange(ScenePathBatchBuilder.Build(scene.RequestId, EnumeratePaths()));

        IEnumerable<ScenePath> EnumeratePaths()
        {
            var pathIndex = 0;
            foreach (var path in paths)
            {
                var vertices = path.Where(vertex => !vertex.IsNull())
                    .Select(vertex => new[] { (float)vertex.X, (float)vertex.Y, (float)vertex.Z })
                    .ToList();
                if (vertices.Count < 2)
                {
                    pathIndex++;
                    continue;
                }
                if (pathIndex < closePathList.Count && closePathList[pathIndex])
                    vertices.Add(vertices[0]);
                Color color;
                if (pathIndex < colorList.Count)
                    color = colorList[pathIndex];
                else if (!expandMissingColors)
                    color = new Color(KnownColors.Black);
                else if (pathIndex < fallbackColors.Count)
                    color = fallbackColors[pathIndex];
                else
                {
                    randomColors!.MoveNext();
                    color = randomColors.Current;
                }
                yield return new ScenePath
                {
                    // These source paths are consumed immediately into request-scoped batches;
                    // individual IDs would create hundreds of thousands of short-lived strings.
                    Id = "presenter-path",
                    Vertices = vertices,
                    Thickness = pathIndex < lineThicknessList.Count ? lineThicknessList[pathIndex] : -1,
                    Color = new ColorRgba(color.R, color.G, color.B, color.A)
                };
                pathIndex++;
            }
        }
    }

    private static IReadOnlyList<T>? AsReadOnlyList<T>(IEnumerable<T>? values)
        => values switch
        {
            null => null,
            IReadOnlyList<T> list => list,
            _ => values.ToList()
        };

    private static SceneMesh? CreateStepMesh(IEnumerable<TriangleFace>? faces)
    {
        if (faces is null)
            return null;
        var faceList = faces.ToList();
        return faceList.Count == 0
            ? null
            : CreateMesh(faceList, ColorRgba.LightGray, false, "step");
    }

    private static ScenePointSet CreatePoints(IEnumerable<Vector3> points, double radius, Color color)
        => new()
        {
            Id = $"points-{Guid.NewGuid():N}",
            Radius = radius <= 0 ? -1 : radius,
            Color = new ColorRgba(color.R, color.G, color.B, color.A),
            Points = points.Select(p => new[] { (float)p.X, (float)p.Y, (float)p.Z }).ToList()
        };

    private static Color RandomColor(int i)
        => new(
            (byte)((i * 97 + 40) % 220),
            (byte)((i * 57 + 90) % 220),
            (byte)((i * 131 + 20) % 220));

}
