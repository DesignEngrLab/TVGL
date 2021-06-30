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

        internal bool BothSidesSamePrimitive;

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
    }
}
