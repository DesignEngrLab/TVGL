using HelixToolkit.SharpDX.Core;
using HelixToolkit.Wpf.SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL;
using Color = TVGL.Color;
using MediaColor = System.Windows.Media.Color;

namespace WindowsDesktopPresenter
{
    public class Presenter3D : IPresenter3D
    {
        List<Window3DHeldPlot> plot3DHeldWindows = new List<Window3DHeldPlot>();

        #region Show and Hang Solids
        public void ShowAndHang(Solid solid, string heading = "", string title = "",
            string subtitle = "")
        {
            if (solid is CrossSectionSolid css)
                ShowAndHang(css.GetCrossSectionsAs3DLoops());
            else
                ShowAndHang([solid], heading, title, subtitle);
        }

        public void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "")
        {
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(ConvertSolidsToModel3D(solids));
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        #endregion


        /// <summary>
        /// Shows the gauss sphere with intensity.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="solid">The ts.</param>
        public void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid)
        {

            var vm = new Window3DPlotViewModel();
            var window = new Window3DPlot(vm);
            var pt0 = new System.Windows.Media.Media3D.Point3D(solid.Center[0], solid.Center[1], solid.Center[2]);
            var x = solid.XMax - solid.XMin;
            var y = solid.YMax - solid.YMin;
            var z = solid.ZMax - solid.ZMin;
            var radius = System.Math.Max(System.Math.Max(x, y), z) / 2;

            //Add the solid to the visual
            var model = ConvertSolidsToModel3D(new[] { solid });
            vm.Add(model);

            //Add a transparent unit sphere to the visual...doesn't seem to be one in SharpDX
            //var sphere = new HelixToolkit.Wpf.SharpDX.
            //sphere.Radius = radius;
            //sphere.Center = pt0;
            //sphere.Material = MaterialHelper.CreateMaterial(new MediaColor { A = 15, R = 200, G = 200, B = 200 });
            //window.view1.Children.Add(sphere);

            var i = 0;
            foreach (var point in vertices)
            {
                var positions = new Vector3Collection(new[] {
                    new SharpDX.Vector3((float)solid.Center.X, (float)solid.Center.Y, (float)solid.Center.Z),
                    new SharpDX.Vector3((float)(pt0.X + point.X * radius),
                                        (float)(pt0.Y + point.Y * radius), (float)(pt0.Z + point.Z * radius))
                });
                var lineIndices = new IntCollection(new[] { 0, 1 });

                var color = colors[i];
                var lines = new LineGeometryModel3D
                {
                    Geometry = new LineGeometry3D
                    {
                        Positions = positions,
                        Indices = lineIndices
                    },
                    IsRendering = true,
                    Smoothness = 2,
                    Thickness = 5,
                    Color = new MediaColor { A = 255, R = color.R, G = color.G, B = color.B }
                };
            }
            window.ShowDialog();
        }


        #region ShowPaths with or without Solid(s)
        public void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null)
        {
            if (radius == 0) radius = 1;
            color ??= new Color(KnownColors.Red);
            var pointVisuals = GetPointModels(points, radius, color);
            var vm = new Window3DPlotViewModel();
            vm.Add(pointVisuals);
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null) //, bool randomColors = false)
        {
            var randomColors = true;
            if (radius == 0) radius = 1;
            //set the default color to be the first color in the list. If none was provided, use black.
            var colorEnumerator = colors != null ? colors.GetEnumerator() : randomColors ? Color.GetRandomColors().GetEnumerator()
                : new Repeater<Color>(new Color(KnownColors.Black));

            var vm = new Window3DPlotViewModel();
            foreach (var points in pointSets)
            {
                colorEnumerator.MoveNext();
                var color = colorEnumerator.Current;
                var pointVisuals = GetPointModels(points, radius, color);
                vm.Add(pointVisuals);
            }
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public static IEnumerable<GeometryModel3D> GetPointModels(IEnumerable<Vector3> points, double radius = 0, Color tvglColor = null)
        {
            var color = new MediaColor { R = tvglColor.R, G = tvglColor.G, B = tvglColor.B, A = tvglColor.A };
            yield return new PointGeometryModel3D
            {
                Geometry = new PointGeometry3D
                {
                    Positions = new Vector3Collection(points.Select(p => new SharpDX.Vector3((float)p.X, (float)p.Y, (float)p.Z)))
                },
                Size = new System.Windows.Size(radius, radius),
                FixedSize = true,
                Color = color
            };
        }

        public void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids)
        {
            var vm = new Window3DPlotViewModel();
            vm.Add(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors));
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, bool otherwiseRandomPathColors = false, params Solid[] solids)
        {
            var vm = new Window3DPlotViewModel();
            vm.Add(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors, otherwiseRandomPathColors));
            if (solids != null)
            {
                vm.Add(solids.Where(s => s != null).SelectMany(s => ConvertTessellatedSolidToMGM3D((TessellatedSolid)s)));
            }
            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, IEnumerable<TriangleFace> faces = null)
        {
            var vm = new Window3DPlotViewModel();
            vm.Add(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors, true));
            if (faces != null)
                vm.Add(ConvertTessellatedSolidToMGM3D(faces, new Color(KnownColors.LightGray), false));

            var window = new Window3DPlot(vm);

            window.ShowDialog();
        }

        public void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids)
            => ShowAndHang([path], [closePaths], [lineThickness == -1 ? 1 : lineThickness], [color == null ? new Color(KnownColors.Black) : color], false, solids);

        /// <summary>
        /// Show and hang a series of paths and solids at each step. Here, the outermost collection is the unique object, and the second
        /// is the time step. So if you have 3 paths and 5 time steps, you will have 3 collections each with 5 paths and each of which 
        /// is comprised of a certain number of points. These paths (3 in the example) can each be closed or not (the default is not closed), 
        /// and can have a certain thickness and color. So, in the example, these would be expected to be of length 3. Also keptEarlierPaths
        /// would be of length 3, indicating for each path whether to keep the earlier paths on the screen or not (the default is true). 
        /// If solids are provided, then they are kept on the screen, and at each time step, the transforms are applied to them. If a 
        /// transform collection is null, then the solid is assumed to be static and is kept in all time steps. If the transforms 
        /// collection is provided, then a Matrix4x4.Identity would keep it in the default position and a Matrix4x4.Null would remove 
        /// it from the display for that time step.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="solids"></param>
        /// <param name="solidTransforms"></param>
        /// <param name="keepEarlierPaths"></param>
        /// <param name="closePaths"></param>
        /// <param name="lineThicknesses"></param>
        /// <param name="colors"></param>
        public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<Solid>> solids, IEnumerable<IEnumerable<Matrix4x4>> solidTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null)
        => ShowStepsAndHang(paths, pathTransforms, solids?.Select(sgroup => sgroup.Select(sTimeStep => (sTimeStep is TessellatedSolid ts) ? ts.Faces :
        (sTimeStep is ImplicitSolid imp) ? imp.ConvertToTessellatedSolid(1).Faces : sTimeStep is VoxelizedSolid vs ?
        vs.ConvertToTessellatedSolidRectilinear().Faces : null)), solidTransforms, closePaths, lineThicknesses, colors);

        /// <summary>
        /// Steps through the various paths and face groups, applying transforms as provided. The outermost collection for both paths and solids 
        /// is a group of objects that have the same behavior. the second collection is the object at a particular timestep. Now, the transforms
        /// are aligned one-to-one with the groups. If they are null, then the shapes only appear in the one time step that they have been provided.
        /// Otherwise, the previous object groups survive and are transformed accordingly.
        /// </summary>
        /// <param name="paths"></param>
        /// <param name="pathTransforms"></param>
        /// <param name="faceGroups"></param>
        /// <param name="fGTransforms"></param>
        /// <param name="closePaths"></param>
        /// <param name="lineThicknesses"></param>
        /// <param name="pathColors"></param>
        /// <param name="faceGroupColors"></param>
        public void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IEnumerable<IEnumerable<Matrix4x4>> fGTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> pathColors = null)
        {
            var vm = new Stepped3DViewModel();
            var closedEnumerator = closePaths != null ? closePaths.GetEnumerator() : new Repeater<bool>(false);
            var thickEnumerator = lineThicknesses != null ? lineThicknesses.GetEnumerator() : new Repeater<double>(1);
            var colorEnumerator = pathColors != null ? pathColors.GetEnumerator() : new Repeater<Color>(new Color(KnownColors.Black));
            var outerTransformEnumerator = pathTransforms != null ? pathTransforms.GetEnumerator() : new Repeater<IEnumerable<Matrix4x4>>(null);

            var numPathTimeSteps = 0;
            foreach (var pathGroup in paths)
            {
                var closed = closedEnumerator.MoveNext() ? closedEnumerator.Current : false;
                var lineThickness = thickEnumerator.MoveNext() ? thickEnumerator.Current : 1;
                var pathColor = colorEnumerator.MoveNext() ? colorEnumerator.Current : new Color(KnownColors.Black);
                var innerTransformSteps = outerTransformEnumerator.MoveNext() ? outerTransformEnumerator.Current?.GetEnumerator() : null;
                var helixPathSteps = new List<GeometryModel3D>();
                var transformSteps = innerTransformSteps == null ? null : new List<System.Windows.Media.Media3D.Transform3D>();
                foreach (var pathStep in pathGroup)
                {
                    if (innerTransformSteps != null)
                        transformSteps.Add(innerTransformSteps.MoveNext() ? ConvertToWindowsTransform3D(innerTransformSteps.Current) : null);
                    helixPathSteps.Add(pathStep == null ? null : ConvertPathToLineModel(pathStep, lineThickness, pathColor, closed));
                }
                numPathTimeSteps = Math.Max(numPathTimeSteps, helixPathSteps.Count);
                vm.PathGroups.Add(helixPathSteps);
                vm.PathTransforms.Add(transformSteps);
            }

            var defColor = new Color(TVGL.Constants.DefaultColor);
            outerTransformEnumerator = fGTransforms != null ? fGTransforms.GetEnumerator() : new Repeater<IEnumerable<Matrix4x4>>(null);
            //var numSolidTimeSteps = 0;
            foreach (var solidGroup in faceGroups)
            {
                var numInGroup = 1;
                var subGroupSteps = new List<GeometryModel3D[]>();
                foreach (var solidStep in solidGroup)
                {
                    var geom3Ds = ConvertTessellatedSolidToMGM3D(solidStep, defColor, false).ToArray();
                    subGroupSteps.Add(geom3Ds);
                    numInGroup = Math.Max(numInGroup, geom3Ds.Length);
                }
                var innerMatrixSteps = outerTransformEnumerator.MoveNext() ? outerTransformEnumerator.Current : null;
                var innerTransformSteps = innerMatrixSteps == null ? null
                    : innerMatrixSteps.Select(ConvertToWindowsTransform3D).ToArray();
                for (int i = 0; i < numInGroup; i++)
                {
                    var helixsolidSteps = new List<GeometryModel3D>();
                    for (int j = 0; j < subGroupSteps.Count; j++)
                    {
                        if (i < subGroupSteps[j].Length)
                            helixsolidSteps.Add(subGroupSteps[j][i]);
                        else helixsolidSteps.Add(null);
                    }
                    //numSolidTimeSteps = Math.Max(numSolidTimeSteps, helixsolidSteps.Count);
                    while (helixsolidSteps.Count > 1 && helixsolidSteps[^1] == null)
                        helixsolidSteps.RemoveAt(helixsolidSteps.Count - 1);
                    vm.SolidGroups.Add(helixsolidSteps);
                    //if (innerTransformSteps.Length == helixsolidSteps.Count)
                    vm.SolidTransforms.Add(innerTransformSteps);
                    //else
                    //    vm.SolidTransforms.Add(innerTransformSteps.Take(helixsolidSteps.Count).ToArray());
                }
            }
            var window = new Window3DSteppedPlot(vm);
            window.ShowDialog();
        }

        private MeshGeometryModel3D CopyVizMesh(MeshGeometryModel3D origGeom)
        {
            var origMesh = (MeshGeometry3D)origGeom.Geometry;
            return new MeshGeometryModel3D
            {
                Geometry = new MeshGeometry3D
                {
                    Positions = new Vector3Collection(origMesh.Positions),
                    TriangleIndices = new IntCollection(origMesh.TriangleIndices),
                    Normals = new Vector3Collection(origMesh.Normals),
                },
                Material = new PhongMaterial() { DiffuseColor = ((PhongMaterial)origGeom.Material).DiffuseColor }
            };
        }

        private System.Windows.Media.Media3D.Transform3D ConvertToWindowsTransform3D(Matrix4x4 m)
        {
            if (m.IsNull()) return null;
            return new System.Windows.Media.Media3D.MatrixTransform3D(new System.Windows.Media.Media3D.Matrix3D(
                  m.M11, m.M12, m.M13, m.M14,
                  m.M21, m.M22, m.M23, m.M24,
                  m.M31, m.M32, m.M33, m.M34,
                  m.M41, m.M42, m.M43, m.M44
              ));
        }

        private IEnumerable<LineGeometryModel3D> ConvertPathsToLineModels(IEnumerable<IEnumerable<Vector3>> paths,
            IEnumerable<bool> closePaths, IEnumerable<double> lineThicknesses, IEnumerable<Color> colors, bool randomColors)
        {
            var closedEnumerator = closePaths != null ? closePaths.GetEnumerator() : new Repeater<bool>(false);
            var lineThickEnumerator = lineThicknesses != null ? lineThicknesses.GetEnumerator() : new Repeater<double>(1);
            var colorEnumerator = colors != null ? colors.GetEnumerator() : randomColors ? Color.GetRandomColors().GetEnumerator() :
                new Repeater<Color>(new Color(KnownColors.Black));

            var linesVisual = new List<LineGeometryModel3D>();
            foreach (var path in paths)
            {
                closedEnumerator.MoveNext();
                var isClosed = closedEnumerator.Current;
                lineThickEnumerator.MoveNext();
                var lineThick = lineThickEnumerator.Current;
                colorEnumerator.MoveNext();
                var color = colorEnumerator.Current;

                if (path != null && path.Any() && !path.Any(p => p.IsNull()))
                    yield return ConvertPathToLineModel(path, lineThick, color, isClosed);
            }
        }

        private List<LineGeometryModel3D> ConvertPathsToLineModels(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths,
            IEnumerable<bool> closePaths, IEnumerable<double> lineThicknesses, IEnumerable<Color> colors)
        {
            var lineVisuals = new List<LineGeometryModel3D>();
            closePaths = closePaths ?? paths.Select(x => true);
            var closedEnumerator = closePaths.GetEnumerator();
            lineThicknesses = lineThicknesses ?? paths.Select(x => 1.0);
            var lineThickEnumerator = lineThicknesses.GetEnumerator();
            //set the default mediaColor to be the first mediaColor in the list. If none was provided, use black.
            colors = colors ?? Color.GetRandomColors();
            var colorEnumerator = colors.GetEnumerator();

            var linesVisual = new List<LineGeometryModel3D>();
            foreach (var path in paths)
            {
                while (!closedEnumerator.MoveNext())
                    closedEnumerator = closePaths.GetEnumerator();
                var isClosed = closedEnumerator.Current;
                while (!lineThickEnumerator.MoveNext())
                    lineThickEnumerator = lineThicknesses.GetEnumerator();
                var lineThick = lineThickEnumerator.Current;
                while (!colorEnumerator.MoveNext())
                    colorEnumerator = colors.GetEnumerator();
                var color = colorEnumerator.Current;

                if (path == null || !path.Any()) continue;
                foreach (var p in path)
                    lineVisuals.Add(ConvertPathToLineModel(p, lineThick, color, isClosed));
            }

            return lineVisuals;
        }

        private static LineGeometryModel3D ConvertPathToLineModel(IEnumerable<Vector3> path, double thickness, Color color, bool closePath)
        {
            var contour = path.Select(point => new SharpDX.Vector3((float)point[0], (float)point[1], (float)point[2]));
            if (color == null) color = new Color();
            var mediaColor = new MediaColor { R = color.R, G = color.G, B = color.B, A = color.A };
            var positions = new Vector3Collection(contour);
            var lineIndices = new IntCollection();
            for (var i = 1; i < positions.Count; i++)
            {
                lineIndices.Add(i - 1);
                lineIndices.Add(i);
            }
            if (closePath)
            {
                lineIndices.Add(positions.Count - 1);
                lineIndices.Add(0);
            }

            return new LineGeometryModel3D
            {
                Geometry = new LineGeometry3D
                {
                    Positions = positions,
                    Indices = lineIndices
                },
                IsRendering = true,
                Smoothness = 2,
                FillMode = thickness == 0 ? SharpDX.Direct3D11.FillMode.Wireframe : SharpDX.Direct3D11.FillMode.Solid,
                Thickness = thickness,
                Color = mediaColor
            };
        }

        #endregion



        public void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "")
        {
            var geomModels = ConvertTessellatedSolidToMGM3D(faces, new Color(KnownColors.LightGray), false);
            var vm = new Window3DPlotViewModel(heading, title, subtitle);
            vm.Add(geomModels);
            var window = new Window3DPlot(vm);
            window.ShowDialog();
        }

        public static IEnumerable<GeometryModel3D> ConvertSolidsToModel3D(IEnumerable<Solid> solids)
        {
            foreach (var ts in solids.Where(ts => ts is TessellatedSolid))
                foreach (var m3d in ConvertTessellatedSolidToMGM3D((TessellatedSolid)ts))
                    yield return m3d;

            foreach (var vs in solids.Where(vs => vs is VoxelizedSolid))
                foreach (var m3d in ConvertVoxelsToPointModel3D((VoxelizedSolid)vs))
                    yield return m3d;
            foreach (var css in solids.Where(cs => cs is CrossSectionSolid))
                foreach (var layer in ((CrossSectionSolid)css).GetCrossSectionsAs3DLoops().SelectMany(v => v))
                    yield return ConvertPathToLineModel(layer, 1, null, true);
        }

        private static IEnumerable<GeometryModel3D> ConvertTessellatedSolidToMGM3D(TessellatedSolid ts)
        { return ConvertTessellatedSolidToMGM3D(ts.Faces, ts.SolidColor, ts.HasUniformColor); }
        private static IEnumerable<GeometryModel3D> ConvertTessellatedSolidToMGM3D(IEnumerable<TriangleFace> faces, Color defaultColor, bool hasUniformColor)
        {
            var faceList = faces as IList<TriangleFace> ?? faces.ToList();
            var numFaces = faceList.Count;
            var defaultSharpDXColor = new SharpDX.Color4(defaultColor.Rf, defaultColor.Gf, defaultColor.Bf, defaultColor.Af);
            var positions =
                faceList.SelectMany(
                    f => f.Vertices.Select(v =>
                        new SharpDX.Vector3((float)v.Coordinates[0], (float)v.Coordinates[1], (float)v.Coordinates[2])));
            var normals =
                           faceList.SelectMany(f =>
                               f.Vertices.Select(v =>
                                   new SharpDX.Vector3((float)f.Normal[0], (float)f.Normal[1], (float)f.Normal[2])));
            var indices = Enumerable.Range(0, numFaces * 3);
            if (hasUniformColor)
            {
                yield return new MeshGeometryModel3D
                {
                    Geometry = new MeshGeometry3D
                    {
                        Positions = new Vector3Collection(positions),
                        TriangleIndices = new IntCollection(indices),
                        Normals = new Vector3Collection(normals),
                    },
                    Material = new PhongMaterial() { DiffuseColor = defaultSharpDXColor }
                };
            }
            else
            {
                var vertexList = positions.ToList();  //to avoid re-enumeration
                var normalList = normals.ToList();    //these lists are defined
                var colorToFaceDict = new Dictionary<SharpDX.Color4, List<int>>();
                for (int i = 0; i < numFaces; i++)
                {
                    var f = faceList[i];
                    var faceColor = (f.Color == null) ? defaultSharpDXColor
                        : new SharpDX.Color4(f.Color.Rf, f.Color.Gf, f.Color.Bf, f.Color.Af);
                    if (colorToFaceDict.TryGetValue(faceColor, out var ints))
                        ints.Add(i);
                    else
                        colorToFaceDict.Add(faceColor, new List<int> { i });
                }
                foreach (var entry in colorToFaceDict)
                {
                    var material = new PhongMaterial { DiffuseColor = entry.Key };
                    var faceIndices = entry.Value;
                    yield return new MeshGeometryModel3D
                    {
                        Geometry = new MeshGeometry3D
                        {
                            Positions = new Vector3Collection(faceIndices
                        .SelectMany(f => new[] { vertexList[3 * f], vertexList[3 * f + 1], vertexList[3 * f + 2] })),
                            TriangleIndices = new IntCollection(Enumerable.Range(0, 3 * faceIndices.Count)),
                            Normals = new Vector3Collection(faceIndices
                        .SelectMany(f => new[] { normalList[3 * f], normalList[3 * f + 1], normalList[3 * f + 2] }))
                        },
                        Material = material
                    };
                }
            }
        }

        private static IEnumerable<GeometryModel3D> ConvertVoxelsToPointModel3D(VoxelizedSolid vs)
        {
            var sw = Stopwatch.StartNew();
            var s = (float)vs.VoxelSideLength;
            var xOffset = (float)vs.Offset[0];
            var yOffset = (float)vs.Offset[1];
            var zOffset = (float)vs.Offset[2];
            var radius = s;

            yield return new PointGeometryModel3D
            {
                FigureRatio = .2,
                Geometry = new PointGeometry3D
                {
                    Positions = new Vector3Collection(vs.GetExposedVoxels().Select(vox => new SharpDX.Vector3(vox.xIndex * s + xOffset,
                    vox.yIndex * s + yOffset, vox.zIndex * s + zOffset))),

                },
                Size = new System.Windows.Size(3 * Math.Sqrt(s), 3 * Math.Sqrt(s)),
                Color = new MediaColor { R = vs.SolidColor.R, G = vs.SolidColor.G, B = vs.SolidColor.B, A = vs.SolidColor.A }
            };
            Console.WriteLine(sw.Elapsed.ToString());
        }

        public void Show(Solid solid, string title = "",
    HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            if (solid is CrossSectionSolid css)
                throw new NotImplementedException();
            else
                Show([solid], title, holdType, timetoShow, id);
        }

        public void Show(ICollection<Solid> solids, string title = "",
            HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1)
        {
            var window = PresenterAsyncMethods.GetOrCreate3DWindow(id, plot3DHeldWindows);
            window.Dispatcher.Invoke(() =>
            {
                var vm = (Held3DViewModel)window.DataContext;
                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(ConvertSolidsToModel3D(solids));
                else vm.EnqueueNewSeries(ConvertSolidsToModel3D(solids));
                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }
        public void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "",
            HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids)
        {
            var window = PresenterAsyncMethods.GetOrCreate3DWindow(id, plot3DHeldWindows);
            window.Dispatcher.Invoke(() =>
            {
                var vm = (Held3DViewModel)window.DataContext;
                if (!string.IsNullOrEmpty(title)) vm.Title = title;

                if (timetoShow > 0)
                    vm.UpdateInterval = timetoShow;
                if (holdType == HoldType.Immediate)
                    vm.AddNewSeries(ConvertSolidsToModel3D(solids).Concat(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors, true)));
                else vm.EnqueueNewSeries(ConvertSolidsToModel3D(solids).Concat(ConvertPathsToLineModels(paths, closePaths, lineThicknesses, colors, true)));
                if (!window.IsVisible && !vm.HasClosed)
                    window.Show();
            });
        }

        public void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparentSolids, IEnumerable<TessellatedSolid> solids)
        {
            throw new NotImplementedException();
        }

        public static void NVEnable()
        {
            NVOptimusEnabler nvEnabler = new NVOptimusEnabler();

        }
    }

    public sealed class NVOptimusEnabler
    {
        static NVOptimusEnabler()
        {
            try
            {

                if (Environment.Is64BitProcess)
                    NativeMethods.LoadNvApi64();
                else
                    NativeMethods.LoadNvApi32();
            }
            catch { } // will always fail since 'fake' entry point doesn't exists
        }
    };

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi64();

        [System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi32();
    }

    internal class Repeater<T> : IEnumerator<T>
    {
        private readonly T item;

        internal Repeater(T item)
        {
            this.item = item;
        }
        public T Current => item;

        object IEnumerator.Current => Current;

        public void Dispose()
        => throw new NotImplementedException();

        public bool MoveNext()
        { return true; }
        //=> throw new NotImplementedException();

        public void Reset()
        => throw new NotImplementedException();
    }
}