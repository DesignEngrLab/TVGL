using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    /// Interface for the Presenter class containing all public methods and properties
    /// </summary>
    public interface IPresenter3D
    {
        void ShowAndHang(Solid solid, string heading = "", string title = "", string subtitle = "");

        void ShowAndHang(IEnumerable<Solid> solids, string heading = "", string title = "", string subtitle = "");

        #region ShowPaths with or without Solid(s)

        void ShowPointsAndHang(IEnumerable<Vector3> points, double radius = 0, Color color = null);

        void ShowPointsAndHang(IEnumerable<IEnumerable<Vector3>> pointSets, double radius = 0, IEnumerable<Color> colors = null);

        void ShowAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, params Solid[] solids);

        void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, bool otherwiseRandomPathColors = false, params Solid[] solids);


        void ShowAndHang(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, IEnumerable<TriangleFace> faces = null);

        void ShowAndHang(IEnumerable<Vector3> path, bool closePaths = false, double lineThickness = -1, Color color = null, params Solid[] solids);

        void ShowAndHang(IEnumerable<TriangleFace> faces, string heading = "", string title = "", string subtitle = "");


        #endregion

        #region Additional Methods

        void ShowAndHangTransparentsAndSolids(IEnumerable<TessellatedSolid> transparentSolids, IEnumerable<TessellatedSolid> solids);

        /// <summary>
        /// Shows the gauss sphere with intensity.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="colors">The colors.</param>
        /// <param name="solid">The ts.</param>
        void ShowGaussSphereWithIntensity(IEnumerable<Vertex> vertices, IList<Color> colors, Solid solid);

        void Show(Solid solid, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);

        void Show(ICollection<Solid> solids, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1);
        void Show(IEnumerable<IEnumerable<Vector3>> paths, IEnumerable<bool> closePaths = null,
           IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null, string title = "",
           HoldType holdType = HoldType.Immediate, int timetoShow = -1, int id = -1, params Solid[] solids);

        void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<Solid>> solids, IEnumerable<IEnumerable<Matrix4x4>> solidTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> colors = null);

        void ShowStepsAndHang(IEnumerable<IEnumerable<IEnumerable<Vector3>>> paths, IEnumerable<IEnumerable<Matrix4x4>> pathTransforms,
            IEnumerable<IEnumerable<IEnumerable<TriangleFace>>> faceGroups, IEnumerable<IEnumerable<Matrix4x4>> fGTransforms, IEnumerable<bool> closePaths = null,
            IEnumerable<double> lineThicknesses = null, IEnumerable<Color> pathColors = null);
        #endregion
    }
}
