// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="BorderSegment.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class BorderSegment.
    /// Implements the <see cref="TVGL.EdgePath" />
    /// </summary>
    /// <seealso cref="TVGL.EdgePath" />
    [JsonObject]
    public class BorderSegment : EdgePath
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BorderSegment"/> class.
        /// </summary>
        public BorderSegment() : base()
        {
        }

        //First primitive connected to this border segment. There is no logic to determine owned/other; it is arbitrary (currently).
        /// <summary>
        /// Gets or sets the owned primitive.
        /// </summary>
        /// <value>The owned primitive.</value>
        public PrimitiveSurface OwnedPrimitive { get; set; }

        //Second primitive connected to this border segment. There is no logic to determine owned/other; it is arbitrary (currently).
        /// <summary>
        /// Gets or sets the other primitive.
        /// </summary>
        /// <value>The other primitive.</value>
        public PrimitiveSurface OtherPrimitive { get; set; }

        /// <summary>
        /// Gets the second primitive.
        /// </summary>
        /// <param name="prim">The prim.</param>
        /// <returns>PrimitiveSurface.</returns>
        public PrimitiveSurface GetSecondPrimitive(PrimitiveSurface prim)
        {
            if (prim == OwnedPrimitive) return OtherPrimitive;
            if (prim == OtherPrimitive) return OwnedPrimitive;
            return null;
        }

        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public ICurve Curve { get; set; }

        /// <summary>
        /// Gets or sets the curve error.
        /// </summary>
        /// <value>The curve error.</value>
        public double CurveError { get; set; }

        /// <summary>
        /// Gets whether the [edge path is circular].
        /// </summary>
        /// <value><c>true</c> if this instance is circular; otherwise, <c>false</c>.</value>
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
                if (IsCircular)
                    return OwnedPrimitive.TransformFrom2DTo3D(((Circle)Curve).Center);
                return Vector3.Null;
            }
        }

        /// <summary>
        /// The internal angle
        /// </summary>
        private double _internalAngle = double.NaN;
        /// <summary>
        /// Gets the internal angle.
        /// </summary>
        /// <value>The internal angle.</value>
        [JsonIgnore]
        public double InternalAngle
        {
            get
            {
                if (double.IsNaN(_internalAngle))
                    _internalAngle = EdgeList.Average(e => e.InternalAngle);
                return _internalAngle;
            }
        }

        /// <summary>
        /// The curvature
        /// </summary>
        private CurvatureType _curvature = CurvatureType.Undefined;
        /// <summary>
        /// Gets the curvature.
        /// </summary>
        /// <value>The curvature.</value>
        [JsonIgnore]
        public CurvatureType Curvature
        {
            get
            {
                if (_curvature == CurvatureType.Undefined)
                    SetCurvature();
                return _curvature;
            }
        }

        /// <summary>
        /// Sets the curvature.
        /// </summary>
        private void SetCurvature()
        {
            var concave = 0;
            var convex = 0;
            var flat = 0;
            foreach (var (edge, _) in this)
            {
                if (edge.InternalAngle.IsPracticallySame(Math.PI, Constants.SameFaceNormalDotTolerance)) flat++;
                else if (edge.Curvature == CurvatureType.Concave) concave++;
                else if (edge.Curvature == CurvatureType.Convex) convex++;
            }
            var flush = flat > 0 && convex == 0 && concave == 0;
            var fullyConcave = concave > 0 && flat == 0 && convex == 0;
            var fullyConvex = convex > 0 && flat == 0 && concave == 0;
            if (flush)
                _curvature = CurvatureType.SaddleOrFlat;
            else if (fullyConcave)
                _curvature = CurvatureType.Concave;
            else if (fullyConvex)
                _curvature = CurvatureType.Convex;
            else
                _curvature = CurvatureType.SaddleOrFlat;
        }

        /// <summary>
        /// Copies the specified reverse.
        /// </summary>
        /// <param name="reverse">if set to <c>true</c> [reverse].</param>
        /// <param name="copiedTessellatedSolid">The copied tessellated solid.</param>
        /// <param name="startIndex">The start index.</param>
        /// <param name="endIndex">The end index.</param>
        /// <returns>EdgePath.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public new EdgePath Copy(bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
            int startIndex = 0, int endIndex = -1)
        {
            var copy = new BorderSegment();
            copy._curvature = _curvature;
            copy._internalAngle = _internalAngle;
            copy.Curve = Curve;
            copy.CurveError = CurveError;
            if (reverse)
            {
                copy.OwnedPrimitive = OtherPrimitive;
                copy.OtherPrimitive= OwnedPrimitive;
            }
            else
            {
                  copy.OwnedPrimitive = OwnedPrimitive;
                copy.OtherPrimitive = OtherPrimitive;
            }
            this.CopyEdgesPathData(copy, reverse, copiedTessellatedSolid, startIndex, endIndex);
            return copy;
        }
    }
}
