// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Linq;



namespace TVGL
{
    /// <summary>
    ///     The class for Cylinder primitives.
    /// </summary>
    public class Cylinder : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            Anchor = Anchor.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = Radius * Axis.Cross(rVector1);
            rVector1 *= Radius;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            Radius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            // we currently don't allow the cylinder to be squished into an elliptical cylinder
            // so the radius is the average of the two radius component vectors after the 
            // transform. Earlier, we were doing 
            //Radius*=transformMatrix.M11;
            // but this is not correct since M11 is often non-unity during rotation
        }

        /// <summary>
        /// Calculates the error.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <returns>System.Double.</returns>
        public override double CalculateError(IEnumerable<Vector3> vertices = null)
        {
            if (Axis.IsNull()) return double.MaxValue;
            if (vertices == null)
            {
                vertices = new List<Vector3>();
                vertices = Vertices.Select(v => v.Coordinates).ToList();
                ((List<Vector3>)vertices).AddRange(InnerEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
                ((List<Vector3>)vertices).AddRange(OuterEdges.Select(edge => (edge.To.Coordinates + edge.From.Coordinates) / 2));
            }
            var maxError = 0.0;
            foreach (var c in vertices)
            {
                var d = Math.Abs((c - Anchor).Cross(Axis).Length() - Radius);
                if (d > maxError)
                    maxError = d;
            }
            return maxError;
        }

        private Vector3 faceXDir = Vector3.Null;
        private Vector3 faceYDir = Vector3.Null;

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        {
            var v = point - Anchor;
            if (faceXDir.IsNull())
            {
                faceXDir = Axis.GetPerpendicularDirection();
                faceYDir = Axis.Cross(faceXDir);
            }
            var x = faceXDir.Dot(v);
            var y = faceYDir.Dot(v);
            var angle = Math.Atan2(y, x);

            return new Vector2(angle * Radius, v.Dot(Axis));
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = (point.X / Radius) % Constants.TwoPi;
            var result = Anchor + Radius * Math.Cos(angle) * faceXDir;
            result += Radius * Math.Sin(angle) * faceYDir;
            result += point.Y * Axis;
            return result;
        }

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pathIsClosed">if set to <c>true</c> [path is closed].</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points, bool pathIsClosed)
        {
            // when the points are a closed path and they encircle the axis, basically we see the simplest resulting
            // polygon as a circle. Perhaps this doesn't capture what was intended but it is the best choice given
            // alternatives
            if (pathIsClosed && BorderEncirclesAxis(points, Axis, Anchor))
            {
                var transform = Axis.TransformToXYPlane(out _);
                foreach (var point in points)
                    yield return point.ConvertTo2DCoordinates(transform);
                yield break;
            }
            // the cylinder will be unrolled, and the tangential angle around the cylinder will be transformed
            // into the x (horizontal coordinate). The first point is provides a reference for the additional points
            var horizRepeat = Radius * Constants.TwoPi;
            // the first point is called the prevPoint, just to set up the following loop - so that the previous
            // visited point is always known when processing each subsequent point.
            var prevPoint = points.First();
            var prev2DVertex = TransformFrom3DTo2D(prevPoint);
            yield return prev2DVertex;
            foreach (var point in points.Skip(1))
            {
                // the next 5 lines are to determine how to advance the x-value if the shape wraps around more than
                // 360-degrees of the cylinder
                var vector = point - prevPoint;
                var rightIsOutward = vector.Cross(Axis);
                var step = rightIsOutward.Dot(point - Anchor) > 0 ? 1 : -1;
                // step will be +1 if the move from the prevPoint to this point is CCW about the axis - thus it should
                // have a higher value of x than the prevPoint
                var coord2D = TransformFrom3DTo2D(point);
                var coord2Dx = coord2D.X;
                while (coord2Dx * step < prev2DVertex.X * step)
                    coord2Dx += step * horizRepeat;
                coord2D = new Vector2(coord2Dx, coord2D.Y);
                yield return coord2D;
                prevPoint = point;
                prev2DVertex = coord2D;
            }
        }

        #region Properties

        /// <summary>
        ///     Is the cylinder positive? (false is negative)
        /// </summary>
        public bool IsPositive { get; set; }


        /// <summary>
        ///     Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public Vector3 Anchor { get; set; }
        /// <summary>
        ///     Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        public Circle Circle => new Circle(TransformFrom3DTo2D(Axis), Radius * Radius);

        /// <summary>
        ///     Gets the radius.
        /// </summary>
        /// <value>The radius.</value>
        public double Radius { get; set; }


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

        public double Volume => Height * Math.PI * Radius * Radius;

        #endregion

        #region Constructors

        public Cylinder() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="dxOfBottomPlane">The dx of bottom plane.</param>
        /// <param name="dxOfTopPlane">The dx of top plane.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, double minDistanceAlongAxis,
            double maxDistanceAlongAxis, bool isPositive = true, IEnumerable<PolygonalFace> faces = null)
            : base(faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            IsPositive = isPositive;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, bool isPositive, IEnumerable<PolygonalFace> faces) : base(faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            IsPositive = isPositive;
            var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
            MinDistanceAlongAxis = min;
            MaxDistanceAlongAxis = max;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, bool isPositive)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            IsPositive = isPositive;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cylinder(Cylinder originalToBeCopied, TessellatedSolid copiedTessellatedSolid = null)
            : base(originalToBeCopied, copiedTessellatedSolid)
        {
            Axis = originalToBeCopied.Axis;
            Anchor = originalToBeCopied.Anchor;
            Radius = originalToBeCopied.Radius;
            IsPositive = originalToBeCopied.IsPositive;
            MinDistanceAlongAxis = originalToBeCopied.MinDistanceAlongAxis;
            MaxDistanceAlongAxis = originalToBeCopied.MaxDistanceAlongAxis;
            Height = originalToBeCopied.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="originalToBeCopied">The original to be copied.</param>
        public Cylinder(Cylinder originalToBeCopied, int[] newFaceIndices, TessellatedSolid copiedTessellatedSolid)
            : base(newFaceIndices, copiedTessellatedSolid)
        {
            Axis = originalToBeCopied.Axis;
            Anchor = originalToBeCopied.Anchor;
            Radius = originalToBeCopied.Radius;
            IsPositive = originalToBeCopied.IsPositive;
            MinDistanceAlongAxis = originalToBeCopied.MinDistanceAlongAxis;
            MaxDistanceAlongAxis = originalToBeCopied.MaxDistanceAlongAxis;
            Height = originalToBeCopied.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="circle">The circle.</param>
        /// <param name="minDistanceAlongAxis">The minimum distance along axis.</param>
        /// <param name="maxDistanceAlongAxis">The maximum distance along axis.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, Circle circle, double minDistanceAlongAxis,
            double maxDistanceAlongAxis)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = circle.Radius;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="circle">The circle.</param>
        /// <param name="minDistanceAlongAxis">The minimum distance along axis.</param>
        /// <param name="maxDistanceAlongAxis">The maximum distance along axis.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, double minDistanceAlongAxis,
            double maxDistanceAlongAxis)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            MinDistanceAlongAxis = minDistanceAlongAxis;
            MaxDistanceAlongAxis = maxDistanceAlongAxis;
            Height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
        }


        /// <summary>
        /// Returns where the given point is inside the cylinder.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool PointIsInside(Vector3 x)
        {
            var dxAlong = x.Dot(Axis);
            if (dxAlong < MinDistanceAlongAxis) return false;
            if (dxAlong > MaxDistanceAlongAxis) return false;
            var rSqd = (x - Anchor).Cross(Axis).LengthSquared();
            return rSqd < Radius * Radius;
        }

        public override double PointMembership(Vector3 point)
        {
            var dxAlong = point.Dot(Axis);
            if (dxAlong < MinDistanceAlongAxis) return MinDistanceAlongAxis - dxAlong;
            if (dxAlong > MaxDistanceAlongAxis) return dxAlong - MaxDistanceAlongAxis;
            return (point - Anchor).Cross(Axis).Length() - Radius;
        }



        //public TessellatedSolid AsTessellatedSolid()
        //{
        //    var faces = new List<PolygonalFace>();
        //    foreach(var face in Faces)
        //    {
        //        var vertices = new Vertex[] { face.C, face.B, face.A }; //reverse the vertices
        //        faces.Add(new PolygonalFace(vertices, face.Normal * -1)));
        //    }
        //    //Add the top and bottom faces
        //    //Build the cylinder along the axis
        //    //First, get the planes on the top and bottom.
        //    //Second, determine which plane is further along the axis. The faces on this plane will have a normal == axis
        //    //The bottom plane will have faces in the reverse of the axis.
        //    var plane1 = MiscFunctions.GetPlaneFromThreePoints(Loop1[0].Position, Loop1[1].Position, Loop1[2].Position);
        //    var plane
        //}
        #endregion
    }
}