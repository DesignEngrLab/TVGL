// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Prismatic.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    /// The class for Prismatic primitives.
    /// </summary>
    public class Prismatic : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = BoundingRadius * Axis.Cross(rVector1);
            rVector1 *= BoundingRadius;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            BoundingRadius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            // we currently don't allow the Prismatic to be squished into an elliptical Prismatic
            // so the radius is the average of the two radius component vectors after the 
            // transform. Earlier, we were doing 
            //Radius*=transformMatrix.M11;
            // but this is not correct since M11 is often non-unity during rotation
        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            var transform = Axis.TransformToXYPlane(out _);
            foreach (var point in points)
                yield return point.ConvertTo2DCoordinates(transform);
            yield break;
        }

        #region Properties
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        /// Gets or sets the curve2 d.
        /// </summary>
        /// <value>The curve2 d.</value>
        public ICurve Curve2D { get; set; }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double BoundingRadius { get; set; }


        /// <summary>
        /// Gets or sets the maximum distance along axis.
        /// </summary>
        /// <value>The maximum distance along axis.</value>
        public double MaxDistanceAlongAxis { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// Gets or sets the minimum distance along axis.
        /// </summary>
        /// <value>The minimum distance along axis.</value>
        public double MinDistanceAlongAxis { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; set; } = double.PositiveInfinity;

        Matrix4x4 transformToXYPlane
        {
            get
            {
                if (_transformToXYPlane.IsNull())
                    _transformToXYPlane = Axis.TransformToXYPlane(out _transformBackFromXYPlane);
                return _transformToXYPlane;
            }
        }
        Matrix4x4 _transformToXYPlane = Matrix4x4.Null;
        Matrix4x4 transformBackFromXYPlane
        {
            get
            {
                if (_transformToXYPlane.IsNull())
                    _transformToXYPlane = Axis.TransformToXYPlane(out _transformBackFromXYPlane);
                return _transformBackFromXYPlane;
            }
        }
        Matrix4x4 _transformBackFromXYPlane = Matrix4x4.Null;

        public override string KeyString => "Primsatic|" + Axis.ToString() + "|" + GetCommonKeyDetails();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic"/> class.
        /// </summary>
        public Prismatic() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="minDistanceAlongAxis">The minimum distance along axis.</param>
        /// <param name="maxDistanceAlongAxis">The maximum distance along axis.</param>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(Vector3 axis, double minDistanceAlongAxis,
            double maxDistanceAlongAxis, IEnumerable<TriangleFace> faces = null, bool? isPositive = null)
            : base(faces)
        {
            Axis = axis;
            this.isPositive = isPositive;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(Vector3 axis, IEnumerable<TriangleFace> faces = null, bool? isPositive = null) : base(faces)
        {
            Axis = axis;
            this.isPositive = isPositive;
            var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
            MinDistanceAlongAxis = min;
            MaxDistanceAlongAxis = max;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Prismatic" /> class.
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Prismatic(IEnumerable<TriangleFace> faces = null, bool? isPositive = null) : base(faces)
        {
            Axis = MiscFunctions.FindMostOrthogonalVector(Faces.Select(face =>
                 (face.B.Coordinates - face.A.Coordinates).Cross(face.C.Coordinates - face.A.Coordinates)));
            this.isPositive = isPositive;
            var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, Axis, out _, out _);//vertices are set in base constructor
            MinDistanceAlongAxis = min;
            MaxDistanceAlongAxis = max;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        #endregion


        public List<Vector2> PolyLine
        {
            get
            {
                if (polyLine == null)
                    polyLine = CreatePolyLine();
                return polyLine;
            }
        }
        List<Vector2> polyLine;

        private List<Vector2> CreatePolyLine()
        {
            var points = new List<Vector2>();
            var edges = new Dictionary<Vector2, List<Vector2>>();
            var vertex2DDict = Vertices.ToDictionary(v => v, v => v.ConvertTo2DCoordinates(transformToXYPlane));

            var vector2VertexDict = new Dictionary<Vector2, Vertex>();
            foreach (var (key, value) in vertex2DDict)
            {
                if (!vector2VertexDict.ContainsKey(value))
                    vector2VertexDict.Add(value, key);
            }

            foreach (var v in vector2VertexDict.Values)
            {
                var neighbors = new List<Vector2>();
                var v2D = vertex2DDict[v];
                foreach (var e in v.Edges)
                {
                    if (!InnerEdges.Contains(e) && !OuterEdges.Contains(e)) continue;
                    neighbors.Add(vertex2DDict[e.OtherVertex(v)]);
                }
                points.Add(v2D);
                edges.Add(v2D, neighbors);
            }
#if PRESENT
            var edgePlots = new List<Vector2[]>();
            foreach ((var from, var tos) in edges)
            {
                foreach (var to in tos)
                    edgePlots.Add([from, to]);
            }
            Presenter.ShowAndHang(edgePlots);
#endif
                var axesDictionary = new Dictionary<Vector2, (Vector2 X, Vector2 Y)>();
            foreach (var p in points)
            {
                var neighbors = edges[p];
                var xDir = Vector2.Zero;
                foreach (var edge in neighbors)
                {
                    var vector = edge - p;
                    if (vector.X < 0) vector = -vector;
                    xDir += vector;
                }
                xDir = xDir.Normalize();
                var yDir = new Vector2(-xDir.Y, xDir.X);
                axesDictionary.Add(p, (xDir, yDir));
            }
            var endPoints = points.ToHashSet();
            var success = false;
            do
            {
                foreach (var point in endPoints)
                {
                    var posXNeighbors = 0;
                    var negXNeighbors = 0;
                    var neighbors = edges[point];
                    var axis = axesDictionary[point];
                    foreach (var neighbor in neighbors)
                    {
                        var vector = neighbor - point;
                        if (vector.Dot(axis.X) >= 0) posXNeighbors++;
                        else negXNeighbors++;
                    }
                    if (posXNeighbors != 0 && negXNeighbors != 0) endPoints.Remove(point);
                }
                if (endPoints.Count == 2)
                {
                    success = true;
                    break;
                }
                //if (endPoints.Count == 0 || endPoints.Count == 1) break;
                break;
            }
            while (true);
            if (!success)
            {
                return new List<Vector2>();
                //throw new NotImplementedException("Need to figure out how" +
                //    "to handle cases when it starts with less than 2 endpoints.");
            }
            var start = endPoints.First();
            var end = endPoints.Last();
            var startVertex = vector2VertexDict[start];
            var startFace = startVertex.Faces.First(Faces.Contains);
            var cross = startFace.Normal.Cross(startFace.Center - startVertex.Coordinates);
            if (cross.Dot(Axis) < 0) (start, end) = (end, start);
            // now run a Dijkstra's algorithm to find the shortest path from start to end
            var visited = new HashSet<Vector2>();
            var pq = new PriorityQueue<DijkstraNode, double>();
            pq.Enqueue(new DijkstraNode(start, 0, null), 0);
            while (pq.Count > 0)
            {
                var node = pq.Dequeue();
                if (!visited.Add(node.Point)) continue;
                if (node.Point == end) return node.Path;
                var neighbors = edges[node.Point];
                foreach (var neighbor in neighbors)
                {
                    if (visited.Contains(neighbor)) continue;
                    var vector = neighbor - node.Point;
                    var newDistance = node.Distance + vector.LengthSquared();
                    pq.Enqueue(new DijkstraNode(neighbor, newDistance, node.Path), newDistance);
                }
            }
            return new List<Vector2>();
        }
        class DijkstraNode
        {
            public Vector2 Point;
            public List<Vector2> Path;
            public double Distance;
            public DijkstraNode(Vector2 point, double distance, List<Vector2> prevPath)
            {
                Point = point;
                Distance = distance;
                if (prevPath == null)
                    Path = new List<Vector2> { point };
                else
                {
                    Path = new List<Vector2>(prevPath);
                    Path.Add(point);
                }
            }
        }
        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var point2D = point.ConvertTo2DCoordinates(transformToXYPlane);
            var minDistance = double.PositiveInfinity;
            var minI = -1;
            for (var i = 1; i < PolyLine.Count; i++)
            {
                var from = PolyLine[i - 1];
                var to = PolyLine[i];
                var closest = MiscFunctions.ClosestPointOnLineSegmentToPoint(from, to, point2D);
                var distance = (closest - point2D).LengthSquared();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minI = i;
                }
            }
            if (minI==-1) return double.PositiveInfinity;
            if ((point2D - PolyLine[minI - 1]).Cross(PolyLine[minI] - PolyLine[minI - 1]) < 0)
                return -Math.Sqrt(minDistance);
            return Math.Sqrt(minDistance);
        }
        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var point2D = point.ConvertTo2DCoordinates(transformToXYPlane);
            var minDistance = double.PositiveInfinity;
            var minI = -1;
            for (var i = 1; i < PolyLine.Count; i++)
            {
                var from = PolyLine[i - 1];
                var to = PolyLine[i];
                var closest = MiscFunctions.ClosestPointOnLineSegmentToPoint(from, to, point2D);
                var distance = (closest - point2D).LengthSquared();
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minI = i;
                }
            }
            var normal = (point2D - PolyLine[minI]).Normalize().ConvertTo3DLocation(transformBackFromXYPlane);
            if ((point2D - PolyLine[minI - 1]).Cross(PolyLine[minI] - PolyLine[minI - 1]) < 0)
                return -normal;
            return normal;
        }

        protected override void CalculateIsPositive()
        {
            IsPositive = null;
            return;
            var coords = Vertices.Select(v => v.ConvertTo2DCoordinates(transformToXYPlane)).ToList();
            var numConcave = 0;
            var numConvex = 0;
            var numFlat = 0;
            foreach (var face in Faces)
            {
                var normal2D = face.Normal.ConvertTo2DCoordinates(transformToXYPlane);
                var center2D = face.Center.ConvertTo2DCoordinates(transformToXYPlane);
                foreach (var coord in coords)
                {
                    var d = (coord - center2D).Dot(normal2D);
                    if (d.IsPositiveNonNegligible()) numConcave++;
                    else if (d.IsNegativeNonNegligible()) numConvex++;
                    else numFlat++;
                }
            }
            var percentConcave = numConcave / (double)(numConcave + numConvex + numFlat);
            if (numConcave > numConvex + numFlat) IsPositive = false;
            else if (numConvex > numConcave + numFlat) IsPositive = true;
            else IsPositive = null;
        }

        protected override void SetPrimitiveLimits()
        {
            MinX = MinY = MinZ = double.NegativeInfinity;
            MaxX = MaxY = MaxZ = double.PositiveInfinity;
        }

        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchor, Vector3 direction)
        {
            if (Faces == null || Faces.Count == 0) yield break;
            foreach (var face in Faces)
            {
                var intersectPoint = MiscFunctions.PointOnTriangleFromRay(face, anchor, direction, out var t);
                if (!intersectPoint.IsNull()) yield return (intersectPoint, t);
            }
        }

    }
}