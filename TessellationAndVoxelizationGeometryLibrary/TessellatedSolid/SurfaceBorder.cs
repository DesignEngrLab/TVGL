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
using System.Collections;
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
        public List<EdgePath> EdgePaths { get; set; }

        /// <summary>
        /// Gets the edges and direction.
        /// </summary>
        /// <value>The edges and direction.</value>
        [JsonIgnore]
        public List<bool> PathDirectionList { get; protected set; }

        /// <summary>
        /// Gets or sets the plane.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public PrimitiveSurface Surface { get; set; }
        public new PrimitiveSurface OwnedPrimitive => throw new NotImplementedException();
        public new PrimitiveSurface OtherPrimitive => throw new NotImplementedException();

        public IEnumerable<PrimitiveSurface> AdjacentPrimitives()
        {
            foreach(var edgePath in EdgePaths)
            {
                yield return edgePath.OwnedPrimitive == Surface ? edgePath.OtherPrimitive : edgePath.OwnedPrimitive;
            }
        }

        /// <summary>
        /// Gets or sets the plane error.
        /// </summary>
        /// <value>The plane error.</value>
        public double SurfaceError { get; set; }

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
        /// Gets the center of the circle if the border is a circle.
        /// </summary>
        /// <value>The plane.</value>
        [JsonIgnore]
        public Vector3 CircleCenter
        {
            get
            {
                if (IsCircular)
                    return Surface.TransformFrom2DTo3D(((Circle)Curve).Center);
                return Vector3.Null;
            }
        }

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
                    _polygon = new Polygon(Surface.TransformFrom3DTo2D(AsVectors(), IsClosed));
                return _polygon;
            }
        }
        private Polygon _polygon;

        /// <summary>
        /// Prevents a default instance of the <see cref="SurfaceBorder"/> class from being created.
        /// </summary>
        public SurfaceBorder()
        {
            EdgePaths = new List<EdgePath>();
            PathDirectionList = new List<bool>();
        }

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

        public void Add(EdgePath edgePath, bool addToEnd, bool dir)
        {
            if (addToEnd)
                AddEnd(edgePath, dir);
            else 
                AddBegin(edgePath, dir);
        }

        private void AddEnd(EdgePath edgePath, bool dir)
        {
            EdgePaths.Add(edgePath);
            PathDirectionList.Add(dir);
            //If addToEnd (true) == dir, iterate forward. Otherwise, add in reverse order.
            if (dir)
            {
                foreach (var (edge, dir2) in edgePath)
                {
                    EdgeList.Add(edge);
                    DirectionList.Add(dir2 == dir);
                }
            }
            else
            {
                for (var i = edgePath.Count - 1; i >= 0; i--)
                {
                    var (edge, dir2) = edgePath[i];
                    EdgeList.Add(edge);
                    DirectionList.Add(dir2 == dir);
                }
            }

        }

        private void AddBegin(EdgePath edgePath, bool dir)
        {
            EdgePaths.Insert(0, edgePath);
            PathDirectionList.Insert(0, dir);
            //If addToEnd (false) == dir, iterate forward. Otherwise, insert in reverse order.
            if (!dir)
            {
                foreach(var (edge, dir2) in edgePath)
                {
                    EdgeList.Insert(0, edge);
                    DirectionList.Insert(0, dir2 == dir);
                }
            }
            else
            {
                for (var i = edgePath.Count - 1; i >= 0; i--)
                {
                    var (edge, dir2) = edgePath[i];
                    EdgeList.Insert(0, edge);
                    DirectionList.Insert(0, dir2 == dir);
                }
            }  
        }

        public new bool UpdateIsClosed()
        {
            if (EdgePaths == null)
                IsClosed = false;
            else if (EdgePaths.Count == 1 && EdgePaths[0].IsClosed)
                IsClosed = true;
            else
            {
                var lastVertex = PathDirectionList[^1] ? EdgePaths[^1].To : EdgePaths[^1].From;
                IsClosed = (FirstVertex == lastVertex);
            }
            return IsClosed;
        }

        public new void Clear()
        {
            EdgePaths.Clear();
            PathDirectionList.Clear();
            EdgeList.Clear();
            DirectionList.Clear();
        }

        internal bool Contains(EdgePath path)
        {
            return EdgePaths.Contains(path);
        }

        /// <summary>
        /// Gets the first vertex.
        /// </summary>
        /// <value>The first vertex.</value>
        [JsonIgnore]
        public new Vertex FirstVertex
        {
            get
            {
                if (PathDirectionList == null || PathDirectionList.Count == 0) return null;
                return PathDirectionList[0] ? EdgePaths[0].From : EdgePaths[0].To;
            }
        }

        /// <summary>
        /// Gets the last vertex.
        /// </summary>
        /// <value>The last vertex.</value>
        [JsonIgnore]
        public new Vertex LastVertex
        {
            get
            {
                if (PathDirectionList == null || PathDirectionList.Count == 0) return null;
                // the following condition uses the edge direction of course, but it also checks
                // to see if it is closed because - if it is closed then the last and the first would
                // be repeated. To prevent this, we quickly check that if direction is true use To
                // unless it's closed, then use From (True-False), go through the four cases in your mind
                // and you see that this checks out.
                if (PathDirectionList[^1] != IsClosed) return EdgePaths[^1].To;
                else return EdgePaths[^1].From;
            }
        }
    }
}
