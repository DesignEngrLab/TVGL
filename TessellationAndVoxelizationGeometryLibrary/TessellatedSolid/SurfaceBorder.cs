// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : campmatt
// Created          : 01-04-2021
//
// Last Modified By : campmatt
// Last Modified On : 01-04-2021
// ***********************************************************************
// <copyright file="PrimitiveSurfaceBorder.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurfaceBorder.
    /// </summary>
    [JsonObject]
    public class SurfaceBorder : EdgePath
    {
        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public ICurve Curve { get; set; }
        /// <summary>
        /// Gets or sets the plane.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public PrimitiveSurface Surface { get; set; }

        /// <summary>
        /// Gets or sets the plane error.
        /// </summary>
        /// <value>The plane error.</value>
        public double SurfaceError { get; set; }

        /// <summary>
        /// Gets or sets the curve error.
        /// </summary>
        /// <value>The curve error.</value>
        public double CurveError { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [encircles axis].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool EncirclesAxis { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [border is fully concave].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyConcave { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [border is fully concave].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyConvex { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [border is flush/flat - not concave or convex].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        public bool FullyFlush { get; set; }

        /// <summary>
        /// Gets whether the [border is circular].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsCircular
        {
            get
            {
                if (Curve == null) return false;
                return Curve is Circle;
            }
        }

        /// <summary>
        /// Gets the center of the circle if the border is a circle.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public Vector3 CircleCenter
        {
            get
            {
                if (Curve is Circle circle)
                    return Surface.TransformFrom2DTo3D(circle.Center);
                return Vector3.Null;

            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [both sides same primitive].
        /// </summary>
        /// <value><c>true</c> if [both sides same primitive]; otherwise, <c>false</c>.</value>
        public bool BothSidesSamePrimitive { get; set; }

        public SurfaceBorder(ICurve curve, PrimitiveSurface surface, EdgePath path, double curveError,
            double surfError) : this(curve, surface, path.EdgeList, path.DirectionList, curveError, surfError)
        {
            EdgeList = path.EdgeList;
            DirectionList = path.DirectionList;
            Curve = curve;
            Surface = surface;
            CurveError = curveError;
            SurfaceError = surfError;
        }
        public SurfaceBorder(ICurve curve, PrimitiveSurface surface, List<Edge> edges, List<bool> directions,
            double curveError, double surfError)
        {
            EdgeList = edges;
            DirectionList = directions;
            Curve = curve;
            Surface = surface;
            CurveError = curveError;
            SurfaceError = surfError;
        }

        public double PlaneResidualRatio(Vector3 coordinates, double tolerance)
        {
            var denominator = Math.Max(SurfaceError, tolerance);
            return CalcPlaneError(coordinates) / denominator;
        }

        private double CalcPlaneError(Vector3 point)
        {
            return Surface.CalculateError(new[] { point });
            //var d = Surface.Normal.Dot(point);
            //return (d - Surface.DistanceToOrigin) * (d - Surface.DistanceToOrigin);
        }

        public double CurveResidualRatio(Vector3 coordinates, double tolerance)
        {
            var denominator = Math.Max(CurveError, tolerance);
            return CalcError(coordinates) / denominator;
        }

        private double CalcError(Vector3 point)
        {
            return Curve.SquaredErrorOfNewPoint(Surface.TransformFrom3DTo2D(point));
        }


        [JsonIgnore]
        public Polygon AsPolygon
        {
            get
            {
                if (_polygon == null)
                    _polygon = new Polygon(Surface.TransformFrom3DTo2D(GetVertices().Select(v => v.Coordinates), IsClosed));
                return _polygon;
            }
        }
        private Polygon _polygon;

        /// <summary>
        /// Prevents a default instance of the <see cref="SurfaceBorder"/> class from being created.
        /// </summary>
        public SurfaceBorder() { }


        public SurfaceBorder Copy(PrimitiveSurface copiedSurface, bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
            int startIndex = 0, int endIndex = -1)
        {
            var copy = new SurfaceBorder();
            copy.Surface = copiedSurface;
            copy.Curve = Curve;
            copy.EncirclesAxis = EncirclesAxis;
            copy.FullyFlush = FullyFlush;
            copy.FullyConcave = FullyConcave;
            copy.FullyConvex = FullyConvex;
            CopyEdgesPathData(copy, reverse, copiedTessellatedSolid, startIndex, endIndex);
            return copy;
        }
    }
}
