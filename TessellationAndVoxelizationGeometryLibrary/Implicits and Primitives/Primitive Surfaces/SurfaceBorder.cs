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
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurfaceBorder.
    /// </summary>
    public class SurfaceBorder
    {
        /// <summary>
        /// Gets or sets the curve.
        /// </summary>
        /// <value>The curve.</value>
        public I2DCurve Curve { get; set; }
        /// <summary>
        /// Gets or sets the plane.
        /// </summary>
        /// <value>The plane.</value>
        public Plane Plane { get; set; }

        /// <summary>
        /// Gets or sets the plane error.
        /// </summary>
        /// <value>The plane error.</value>
        public double PlaneError { get; set; }

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
        /// Gets whether the [border is circular].
        /// </summary>
        /// <value><c>true</c> if [encircles axis]; otherwise, <c>false</c>.</value>
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
        public Vector3 CircleCenter
        {
            get
            {
                if (!IsCircular) return Vector3.Null;
                return ((Circle)Curve).Center.ConvertTo3DLocation(Plane.AsTransformFromXYPlane);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [both sides same primitive].
        /// </summary>
        /// <value><c>true</c> if [both sides same primitive]; otherwise, <c>false</c>.</value>
        public bool BothSidesSamePrimitive { get; set; }

        public SurfaceBorder(IEnumerable<Vector3> points, Type curveType) : base()
        {
            var pointList = GetVertices().Select(v => v.Coordinates).ToList();
            if (points != null) pointList.AddRange(points);
            if (Plane.DefineNormalAndDistanceFromVertices(pointList, out var distanceToPlane, out var normal))
                Plane = new Plane(distanceToPlane, normal);
            else
            {
                var lineDir = Vector3.Zero;
                for (int i = 1; i < pointList.Count; i++)
                    lineDir += (pointList[i] - pointList[0]);
                normal = lineDir.Normalize().GetPerpendicularDirection();
                Plane = new Plane(pointList[0], normal);
            }
            UpdateTerms(pointList, curveType);
        }


        public SurfaceBorder(I2DCurve curve2D, Plane curvePlane, double curveError, double planeError) : this()
        {
            this.Curve = curve2D;
            this.Plane = curvePlane;
            this.CurveError = curveError;
            this.PlaneError = planeError;
        }

        public double PlaneResidualRatio(Vector3 coordinates, double tolerance)
        {
            var denominator = Math.Max(PlaneError, tolerance);
            return CalcPlaneError(coordinates) / denominator;
        }

        private double CalcPlaneError(Vector3 point)
        {
            var d = Plane.Normal.Dot(point);
            return (d - Plane.DistanceToOrigin) * (d - Plane.DistanceToOrigin);
        }

        public double CurveResidualRatio(Vector3 coordinates, double tolerance)
        {
            var denominator = Math.Max(CurveError, tolerance);
            return CalcError(coordinates) / denominator;
        }

        private double CalcError(Vector3 point)
        {
            return Curve.SquaredErrorOfNewPoint(point.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane));
        }


        public bool Upgrade(Vector3 newPoint)
        {
            return false;
            throw new NotImplementedException();
            // in the future - see if upgrading from straight to circle
            // then circle to parabola
            // or ellipse and hyperbola
            // make the new fit better.
        }

        public bool UpdateTerms()
        {
            return UpdateTerms(GetVertices().Select(v => v.Coordinates).ToList(), Curve.GetType());
        }

        internal bool UpdateTerms(IList<Vector3> pointList, Type curveType)
        {
            var sucess = UpdateTerms(pointList.Select(p => p.ConvertTo2DCoordinates(Plane.AsTransformToXYPlane)).ToList(), curveType);
            if (sucess)
            {
                PlaneError = pointList.Sum(p => CalcPlaneError(p)) / pointList.Count;
                return true;
            }
            return false;
        }
        internal bool UpdateTerms(IList<Vector2> point2D, Type curveType)
        {
            var arguments = new object[] { point2D, null, null };
            if ((bool)curveType.GetMethod("CreateFromPoints").Invoke(null, arguments))
            {
                Curve = (I2DCurve)arguments[1];
                CurveError = (double)arguments[2];
                return true;
            }
            return false;
        }


        public IEnumerable<Vertex> GetVertices() => Edges.GetVertices();

        public EdgePath Edges { get; set; }

        public Polygon AsPolygon
        {
            get
            {
                if (_polygon == null)
                    _polygon = new Polygon(Edges.GetVertices().ProjectTo2DCoordinates(Plane.Normal, out _));
                return _polygon;
            }
        }
        private Polygon _polygon;

        /// <summary>
        /// Prevents a default instance of the <see cref="SurfaceBorder"/> class from being created.
        /// </summary>
        public SurfaceBorder()
        {
            Edges = new EdgePath();
        }


        public SurfaceBorder Copy(bool reverse = false, TessellatedSolid copiedTessellatedSolid = null)
        {
            var copy = new SurfaceBorder();
            if (Plane != null)
                copy.Plane = new Plane(Plane.DistanceToOrigin, Plane.Normal);
            copy.Curve = Curve;
            copy.EncirclesAxis = EncirclesAxis;
            copy.FullyConcave = FullyConcave;
            copy.FullyConvex = FullyConvex;
            copy.Edges = new EdgePath { IsClosed = Edges.IsClosed };
            if (copiedTessellatedSolid == null)
            {
                foreach (var eAndA in Edges)
                    if (reverse)
                        copy.Edges.Insert(0, (eAndA.edge, !eAndA.dir));
                    else
                        copy.Edges.Add((eAndA.edge, eAndA.dir));
            }
            else
            {
                foreach (var eAndA in Edges)
                    if (reverse)
                        copy.Edges.Insert(0, (copiedTessellatedSolid.Edges[eAndA.edge.IndexInList], !eAndA.dir));
                    else
                        copy.Edges.Add((copiedTessellatedSolid.Edges[eAndA.edge.IndexInList], eAndA.dir));
            }
            return copy;
        }


        public static IEnumerable<HashSet<PolygonalFace>> GetFacePatchesBetweenSignificantEdges(HashSet<Edge> significantEdges, IEnumerable<PolygonalFace> faces)
        {
            var remainingFaces = new HashSet<PolygonalFace>(faces);
            //Pick a start edge, then collect all adjacent faces on one side of the face, without crossing over significant edges.
            //This collection of faces will be used to create a patch.
            while (remainingFaces.Any())
            {
                var startFace = remainingFaces.First();
                var patch = new HashSet<PolygonalFace> { startFace };
                remainingFaces.Remove(startFace);
                var stack = new Stack<PolygonalFace>();
                stack.Push(startFace);
                while (stack.Any())
                {
                    var face = stack.Pop();
                    foreach (var edge in face.Edges)
                    {
                        if (significantEdges.Contains(edge)) continue;//Don't cross over significant edges
                        var otherFace = face == edge.OwnedFace ? edge.OtherFace : edge.OwnedFace;
                        if (remainingFaces.Contains(otherFace))
                        {
                            stack.Push(otherFace);
                            patch.Add(otherFace);
                            remainingFaces.Remove(otherFace);
                        }
                    }
                }
                yield return patch;
            }
        }
    }
}
