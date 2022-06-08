using Newtonsoft.Json;
using System;
using System.Linq;

namespace TVGL
{
    [JsonObject]
    public class BorderSegment : EdgePath
    {
        public BorderSegment() : base()
        {
        }

        //First primitive connected to this border segment. There is no logic to determine owned/other; it is arbitrary (currently).
        public PrimitiveSurface OwnedPrimitive { get; set; }

        //Second primitive connected to this border segment. There is no logic to determine owned/other; it is arbitrary (currently).
        public PrimitiveSurface OtherPrimitive { get; set; }

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

        private double _internalAngle = -10.0;
        [JsonIgnore]
        public double InternalAngle
        {
            get
            {
                if (_internalAngle.Equals(-10.0))
                    _internalAngle = EdgeList.Average(e => e.InternalAngle);
                return _internalAngle;
            }
        }

        private CurvatureType _curvature = CurvatureType.Undefined;
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

        private void SetCurvature()
        {
            var concave = 0;
            var convex = 0;
            var flat = 0;
            foreach (var (edge, _) in this)
            {
                if (edge.InternalAngle.IsPracticallySame(Math.PI / 2, Constants.SameFaceNormalDotTolerance)) flat++;
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

        public new EdgePath Copy(bool reverse = false, TessellatedSolid copiedTessellatedSolid = null,
            int startIndex = 0, int endIndex = -1)
        {
            throw new NotImplementedException();
        }
    }
}
