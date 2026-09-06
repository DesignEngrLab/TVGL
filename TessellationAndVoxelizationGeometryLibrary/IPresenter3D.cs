using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Defines the rendering operations available for three-dimensional TVGL geometry.
    /// </summary>
    public interface IPresenter3D
    {
        /// <summary>Displays a solid and waits until the presentation is closed.</summary>
        /// <param name="solid">The solid to render.</param>
        /// <param name="heading">The optional heading shown above the presentation.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="subtitle">The optional subtitle.</param>
        void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "");

        /// <summary>Displays multiple solids and waits until the presentation is closed.</summary>
        /// <param name="solids">The solids to render.</param>
        /// <param name="heading">The optional heading shown above the presentation.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="subtitle">The optional subtitle.</param>
        void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "");

        #region ShowPaths with or without Solid(s)

        /// <summary>Displays a set of points as three-dimensional markers.</summary>
        /// <param name="points">The points to render.</param>
        /// <param name="radius">The marker radius, or zero for the backend default.</param>
        /// <param name="color">The optional marker color.</param>
        void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null);

        /// <summary>Displays multiple point sets as three-dimensional markers.</summary>
        /// <param name="pointSets">The point sets to render.</param>
        /// <param name="radius">The marker radius, or zero for the backend default.</param>
        /// <param name="colors">Optional colors corresponding to the point sets.</param>
        void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null);

        /// <summary>Displays paths together with optional solids.</summary>
        /// <param name="paths">The path collections to render.</param>
        /// <param name="closePaths">Whether each path should be closed.</param>
        /// <param name="lineThicknesses">Optional line thicknesses corresponding to the paths.</param>
        /// <param name="colors">Optional colors corresponding to the paths.</param>
        /// <param name="solids">Optional solids to display with the paths.</param>
        void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids);

        /// <summary>Displays paths and optional solids, assigning random colors when requested colors are absent.</summary>
        /// <param name="paths">The path collections to render.</param>
        /// <param name="closePaths">Whether each path should be closed.</param>
        /// <param name="lineThicknesses">Optional line thicknesses corresponding to the paths.</param>
        /// <param name="colors">Optional path colors.</param>
        /// <param name="otherwiseRandomPathColors">Whether to choose random path colors when colors are not supplied.</param>
        /// <param name="solids">Optional solids to display with the paths.</param>
        void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, bool otherwiseRandomPathColors = false, params Solid[] solids);


        /// <summary>Displays paths together with optional triangle faces.</summary>
        /// <param name="paths">The path collections to render.</param>
        /// <param name="closePaths">Whether each path should be closed.</param>
        /// <param name="lineThicknesses">Optional line thicknesses corresponding to the paths.</param>
        /// <param name="colors">Optional path colors.</param>
        /// <param name="faces">Optional triangle faces to render.</param>
        void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, IEnumerable<TriangleFace> faces = null);

        /// <summary>Displays one path together with optional solids.</summary>
        /// <param name="path">The path points in traversal order.</param>
        /// <param name="closePaths">Whether the path should be closed.</param>
        /// <param name="lineThickness">The line thickness, or a negative value for the backend default.</param>
        /// <param name="color">The optional path color.</param>
        /// <param name="solids">Optional solids to display with the path.</param>
        void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids);

        /// <summary>Displays triangle faces and waits until the presentation is closed.</summary>
        /// <param name="faces">The faces to render.</param>
        /// <param name="heading">The optional heading.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="subtitle">The optional subtitle.</param>
        void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "");


        #endregion

        #region Additional Methods

        /// <summary>Displays transparent solids together with opaque solids.</summary>
        /// <param name="transparentSolids">The solids to render transparently.</param>
        /// <param name="solids">The solids to render normally.</param>
        void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparentSolids, IEnumerable<TessellatedSolid> solids);

        /// <summary>
        /// Shows the gauss sphere with intensity.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="solid">The ts.</param>
        void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid);

        /// <summary>Displays a solid without blocking the calling code.</summary>
        /// <param name="solid">The solid to render.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="holdType">Whether to queue or immediately display the solid.</param>
        /// <param name="timetoShow">The display duration in milliseconds, or -1 to leave it open.</param>
        /// <param name="id">An optional identifier used by the presentation backend.</param>
        void Show(Solid solid, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        /// <summary>Displays multiple solids without blocking the calling code.</summary>
        /// <param name="solids">The solids to render.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="holdType">Whether to queue or immediately display the solids.</param>
        /// <param name="timetoShow">The display duration in milliseconds, or -1 to leave it open.</param>
        /// <param name="id">An optional identifier used by the presentation backend.</param>
        void Show(ICollection<Solid> solids, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);
        /// <summary>Displays paths and optional solids without blocking the calling code.</summary>
        /// <param name="paths">The path collections to render.</param>
        /// <param name="closePaths">Whether each path should be closed.</param>
        /// <param name="lineThicknesses">Optional line thicknesses corresponding to the paths.</param>
        /// <param name="colors">Optional path colors.</param>
        /// <param name="title">The optional title.</param>
        /// <param name="holdType">Whether to queue or immediately display the content.</param>
        /// <param name="timetoShow">The display duration in milliseconds, or -1 to leave it open.</param>
        /// <param name="id">An optional identifier used by the presentation backend.</param>
        /// <param name="solids">Optional solids to display with the paths.</param>
        void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids);

        /// <summary>Displays groups of paths and solids over a shared interactive timestep progression.</summary>
        /// <param name="paths">Path groups, where each inner path occupies its corresponding timestep.</param>
        /// <param name="pathTransforms">One transform timeline per path group.</param>
        /// <param name="solids">Solid groups, where each inner solid occupies its corresponding timestep.</param>
        /// <param name="solidTransforms">One transform timeline per solid group.</param>
        /// <param name="closePaths">Optional per-path closed flags for each path group. The last value is repeated when needed.</param>
        /// <param name="lineThicknesses">Optional per-path world-space widths for each path group. The last value is repeated when needed.</param>
        /// <param name="colors">Optional per-path colors for each path group. The last value is repeated when needed.</param>
        void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IList<IEnumerable<Matrix4x4>> pathTransforms,
           IList<IEnumerable<Solid>> solids, IList<IEnumerable<Matrix4x4>> solidTransforms, IList<IEnumerable<bool>> closePaths = null,
           IList<IEnumerable<double>> lineThicknesses = null, IList<IEnumerable<Color>> colors = null);

        /// <summary>Displays transformed path and solid groups using explicit stepped-presentation options.</summary>
        void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IList<IEnumerable<Matrix4x4>> pathTransforms,
            IList<IEnumerable<Solid>> solids, IList<IEnumerable<Matrix4x4>> solidTransforms,
            IList<IEnumerable<bool>> closePaths, IList<IEnumerable<double>> lineThicknesses,
            IList<IEnumerable<Color>> colors, SteppedPresentationOptions options)
            => ShowStepsAndHang(paths, pathTransforms, solids, solidTransforms, closePaths, lineThicknesses, colors);

        /// <summary>Displays groups of paths and triangle faces over a shared interactive timestep progression.</summary>
        /// <param name="paths">Path groups, where each inner path occupies its corresponding timestep.</param>
        /// <param name="pathTransforms">One transform timeline per path group.</param>
        /// <param name="faceGroups">Face groups, where each inner face collection occupies its corresponding timestep.</param>
        /// <param name="fGTransforms">One transform timeline per face group.</param>
        /// <param name="closePaths">Optional per-path closed flags for each path group. The last value is repeated when needed.</param>
        /// <param name="lineThicknesses">Optional per-path world-space widths for each path group. The last value is repeated when needed.</param>
        /// <param name="pathColors">Optional per-path colors for each path group. The last value is repeated when needed.</param>
        void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IList<IEnumerable<Matrix4x4>> pathTransforms,
            IList<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IList<IEnumerable<Matrix4x4>> fGTransforms,
            IList<IEnumerable<bool>> closePaths = null, IList<IEnumerable<double>> lineThicknesses = null,
            IList<IEnumerable<Color>> pathColors = null);

        /// <summary>Displays transformed path and face groups using explicit stepped-presentation options.</summary>
        void ShowStepsAndHang(IList<IEnumerable<IEnumerable<Vector3>>> paths, IList<IEnumerable<Matrix4x4>> pathTransforms,
            IList<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IList<IEnumerable<Matrix4x4>> fGTransforms,
            IList<IEnumerable<bool>> closePaths, IList<IEnumerable<double>> lineThicknesses,
            IList<IEnumerable<Color>> pathColors, SteppedPresentationOptions options)
            => ShowStepsAndHang(paths, pathTransforms, faceGroups, fGTransforms, closePaths, lineThicknesses, pathColors);
        #endregion
    }
}
