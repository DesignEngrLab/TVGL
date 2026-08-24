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

        /// <summary>
        /// Displays a solid and invokes <paramref name="onSelection"/> for every viewer triangle selection.
        /// </summary>
        /// <remarks>
        /// Backends that do not support interactive picking throw <see cref="NotSupportedException"/>.
        /// </remarks>
        void ShowAndHang(Solid solid, Action<(TriangleFace face, Vector3 point)> onSelection, string heading = "", string title = "", string subtitle = "")
            => throw new NotSupportedException("This presenter does not support interactive triangle selection.");

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

        /// <summary>Displays a sequence of transformed paths and solids as an interactive progression.</summary>
        /// <param name="paths">The path groups for each step.</param>
        /// <param name="pathTransforms">The transforms applied to each path group.</param>
        /// <param name="solids">The solid groups for each step.</param>
        /// <param name="solidTransforms">The transforms applied to each solid group.</param>
        /// <param name="closePaths">Whether paths should be closed.</param>
        /// <param name="lineThicknesses">Optional path line thicknesses.</param>
        /// <param name="colors">Optional path colors.</param>
        void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<Solid>> solids, IEnumerable<IEnumerable<Matrix4x4>> solidTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null);

        /// <summary>Displays a sequence of transformed paths and triangle-face groups as an interactive progression.</summary>
        /// <param name="paths">The path groups for each step.</param>
        /// <param name="pathTransforms">The transforms applied to each path group.</param>
        /// <param name="faceGroups">The face groups for each step.</param>
        /// <param name="fGTransforms">The transforms applied to each face group.</param>
        /// <param name="closePaths">Whether paths should be closed.</param>
        /// <param name="lineThicknesses">Optional path line thicknesses.</param>
        /// <param name="pathColors">Optional path colors.</param>
        void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IEnumerable<IEnumerable<Matrix4x4>> fGTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> pathColors = null);
        #endregion
    }
}
