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

        public BorderSegment(EdgePath edgePath)
        {
            EdgeList = edgePath.EdgeList;
            DirectionList = edgePath.DirectionList;
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
        public PrimitiveSurface AdjacentPrimitive(PrimitiveSurface prim)
        {
            if (prim == OwnedPrimitive) return OtherPrimitive;
            if (prim == OtherPrimitive) return OwnedPrimitive;
            return null;
        }

        /// <summary>
        /// Need to use a bool to determine if the curve has been defined, since ICurve can be null 
        /// for more complex edge segments (i.e., those that are not either circular or straight). 
        /// </summary>
        private bool isCurveDefined = false;

        private ICurve _curve;

        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public ICurve Curve
        {
            get
            {
                if (!isCurveDefined) SetCurve();
                return _curve;
            }
        }

        public double Radius
        {
            get
            {
                if (!isCurveDefined) SetCurve();
                if (IsStraight) return double.MaxValue;
                return ((Circle)_curve).Radius;
            }
        }

        /// <summary>
        /// Gets or sets the curve error.
        /// </summary>
        /// <value>The curve error.</value>
        public double CurveError { get; set; }

        /// <summary>
        /// Gets whether the [edge path is a straight line].
        /// </summary>
        /// <value><c>true</c> if this instance is circular; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsStraight
        {
            get
            {
                if (Curve == null) return false;
                return Curve is StraightLine3D;
            }
        }

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
        public Vector3 CircleCenter = Vector3.Null;

        /// <summary>
        /// Gets or sets the best-fit plane normal
        /// </summary>
        /// <value>The plane error.</value>
        public Vector3 PlaneNormal { get; set; }

        /// <summary>
        /// Gets or sets the best-fit plane error
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneError { get; set; }

        /// <summary>
        /// Gets or sets the best-fit plane distance
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneDistance { get; set; }

        public void SetCurve()
        {
            CurveError = double.MaxValue;
            //If either primitive is a torus or sphere, the border segment is almost certainly circular.
            //It is definately not a straight line.
            //var onlyCircles = OwnedPrimitive is Torus || OwnedPrimitive is Sphere || OtherPrimitive is Torus || OtherPrimitive is Sphere;
            //var onlyLines = OwnedPrimitive is Plane && OtherPrimitive is Plane;

            var coordinates = GetCoordinates();
            PlaneError = double.MaxValue;
            PlaneDistance = double.NaN;
            PlaneNormal = Vector3.Null;

            //Set the border segment as a straight line, a curve, or leave it null for something more complex
            if (StraightLine3D.CreateFromPoints(coordinates, out var curve, out var error))
            {
                if (error < Constants.DefaultTessellationError)
                {
                    _curve = curve;
                    CurveError = error;
                }
            }

            if(coordinates.Count() > 2) //!//onlyLines)
            {
                var plane = Plane.FitToVertices(coordinates, Vector3.Null, out var planeError);
                //Get the circle too and compare the error to straight line.
                if (plane != null && Circle.CreateFromPoints(coordinates.ProjectTo2DCoordinates(plane.Normal, out var backTransform), out var circle, out var circleError))
                {
                    if (circleError < Constants.DefaultTessellationError && circleError < CurveError)
                    {
                        CircleCenter = plane.TransformFrom2DTo3D(((Circle)circle).Center);
                        //MiscFunctions.PointOnPlaneFromLine(PlaneNormal, PlaneDistance, )
                        _curve = circle;
                        CurveError = circleError;
                    }
                    PlaneError = planeError;
                    PlaneDistance = plane.DistanceToOrigin;
                    PlaneNormal = plane.Normal;
                }
            }
            isCurveDefined = true;
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
                if (edge.Curvature == CurvatureType.Concave) concave++;
                else if (edge.Curvature == CurvatureType.Convex) convex++;
                else flat++;
            }
            if (concave >= flat + convex)
                _curvature = CurvatureType.Concave;
            else if (convex >= flat + concave)
                _curvature = CurvatureType.Convex;
            else if (flat >= convex + concave)
                _curvature = CurvatureType.SaddleOrFlat;
            else
                _curvature = CurvatureType.Undefined;
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
            copy._curve = Curve;
            copy.CurveError = CurveError;
            if (reverse)
            {
                copy.OwnedPrimitive = OtherPrimitive;
                copy.OtherPrimitive = OwnedPrimitive;
            }
            else
            {
                copy.OwnedPrimitive = OwnedPrimitive;
                copy.OtherPrimitive = OtherPrimitive;
            }
            this.CopyEdgesPathData(copy, reverse, copiedTessellatedSolid, startIndex, endIndex);
            return copy;
        }

        public BorderSegment CopyToNewPrimitive(PrimitiveSurface owned, PrimitiveSurface other)
        {
            var copy = new BorderSegment();
            copy._curvature = _curvature;
            copy._internalAngle = _internalAngle;
            copy._curve = Curve;
            copy.CurveError = CurveError;
            copy.OwnedPrimitive = owned;
            copy.OtherPrimitive = other;
            copy.IsClosed = IsClosed;
            for (int i = 0; i < EdgeList.Count; i++)
                copy.AddEnd(EdgeList[i], DirectionList[i]);
            return copy;
        }
    }
}
