// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Cylinder.cs" company="Design Engineering Lab">
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
    /// The class for Cylinder primitives.
    /// </summary>
    public class Cylinder : PrimitiveSurface
    {
        /// <summary>
        /// Transforms the shape by the provided transformation matrix.
        /// </summary>
        /// <param name="transformMatrix">The transform matrix.</param>
        public override void Transform(Matrix4x4 transformMatrix)
        {
            base.Transform(transformMatrix);
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
        /// The face x dir
        /// </summary>
        private Vector3 faceXDir = Vector3.Null;
        /// <summary>
        /// The face y dir
        /// </summary>
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
            if (pathIsClosed && points.BorderEncirclesAxis(Axis, Anchor))
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
        /// Gets the anchor.
        /// </summary>
        /// <value>The anchor.</value>
        public Vector3 Anchor { get; set; }
        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>The direction.</value>
        public Vector3 Axis { get; set; }

        /// <summary>
        /// Gets the circle.
        /// </summary>
        /// <value>The circle.</value>
        public Circle Circle => new Circle(TransformFrom3DTo2D(Axis), Radius * Radius);

        /// <summary>
        /// Gets the radius.
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
        public double Height
        {
            get
            {
                if (double.IsNaN(height) && !double.IsInfinity(MinDistanceAlongAxis)
                    && !double.IsInfinity(MaxDistanceAlongAxis))
                    height = MaxDistanceAlongAxis - MinDistanceAlongAxis;
                return height;
            }
            set
            {
                height = value;
            }
        }
        double height = double.NaN;
        /// <summary>
        /// Gets the volume.
        /// </summary>
        /// <value>The volume.</value>
        public double Volume => Height * Math.PI * Radius * Radius;

        /// <summary>
        /// Gets or sets the total internal angle.
        /// </summary>
        /// <value>The total internal angle.</value>
        public double TotalInternalAngle { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder"/> class.
        /// </summary>
        public Cylinder() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Cylinder" /> class.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="isPositive">if set to <c>true</c> [is positive].</param>
        /// <param name="faces">The faces.</param>
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, IEnumerable<TriangleFace> faces) : base(faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            if (faces != null)
            {
                var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
                MinDistanceAlongAxis = min;
                MaxDistanceAlongAxis = max;
            }
        }


        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var pointDot = point.Dot(Axis);
            var a = (point - Anchor);
            var b = Axis.Cross(a);
            Vector3 outwardVector;
            // if you're within half a percent of the min or max distance along the axis, 
            // and you're not closer to the radius than the end-caps, then you're on the end-cap
            if (Math.Abs(pointDot - MinDistanceAlongAxis) < Math.Abs(b.Length() - Radius))
                outwardVector = -Axis;
            else if (Math.Abs(pointDot - MaxDistanceAlongAxis) < Math.Abs(b.Length() - Radius))
                outwardVector = Axis;
            else outwardVector = b.Cross(Axis).Normalize();
            if (isPositive.HasValue && !isPositive.Value) outwardVector *= -1;
            return outwardVector;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var dxAlong = point.Dot(Axis);
            if (dxAlong < MinDistanceAlongAxis)
            {
                dxAlong = MinDistanceAlongAxis - dxAlong;
                var outward = (point - Anchor).Cross(Axis).Length() - Radius;
                return Math.Sqrt(dxAlong * dxAlong + outward * outward);
            }
            if (dxAlong > MaxDistanceAlongAxis)
            {
                dxAlong = MaxDistanceAlongAxis - dxAlong;
                var outward = (point - Anchor).Cross(Axis).Length() - Radius;
                return Math.Sqrt(dxAlong * dxAlong + outward * outward);
            }
            var d = (point - Anchor).Cross(Axis).Length() - Radius;
            // if d is positive, then the point is outside the cylinder
            // if d is negative, then the point is inside the cylinder
            if (isPositive.HasValue && !isPositive.Value) d = -d;
            return d;
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any()) return;
            var firstFace = Faces.First();
            var axisPointUnderFace = Anchor + (firstFace.Center - Anchor).Dot(Axis) * Axis;
            isPositive = (firstFace.Center - axisPointUnderFace).Dot(firstFace.Normal) > 0;
        }

        protected override void SetPrimitiveLimits()
        {
            if (double.IsFinite(MaxDistanceAlongAxis)
             && double.IsFinite(MinDistanceAlongAxis))
            {
                var offset = Anchor.Dot(Axis);
                var top = Anchor + (MaxDistanceAlongAxis - offset) * Axis;
                var bottom = Anchor + (MinDistanceAlongAxis - offset) * Axis;
                var xFactor = Math.Sqrt(1 - Axis.X * Axis.X);
                if (double.IsNaN(xFactor)) xFactor = 0; // occasionally get NaN due to rounding errors when Axis.X is 1 or -1
                var yFactor = Math.Sqrt(1 - Axis.Y * Axis.Y);
                if (double.IsNaN(yFactor)) yFactor = 0; // occasionally get NaN due to rounding errors ""
                var zFactor = Math.Sqrt(1 - Axis.Z * Axis.Z);
                if (double.IsNaN(zFactor)) zFactor = 0; // occasionally get NaN due to rounding errors ::
                MinX = Math.Min(top.X, bottom.X) - xFactor * Radius;
                MaxX = Math.Max(top.X, bottom.X) + xFactor * Radius;
                MinY = Math.Min(top.Y, bottom.Y) - yFactor * Radius;
                MaxY = Math.Max(top.Y, bottom.Y) + yFactor * Radius;
                MinZ = Math.Min(top.Z, bottom.Z) - zFactor * Radius;
                MaxZ = Math.Max(top.Z, bottom.Z) + zFactor * Radius;
            }
            else
            {
                MinX = MinY = MinZ = double.NegativeInfinity;
                MaxX = MaxY = MaxZ = double.PositiveInfinity;
            }
        }

        /// <summary>
        /// Finds the intersection between this cylinder and the specified line.
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public override IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 anchorLine, Vector3 direction)
        {
            var firstNeedMaxCapPoint = false;
            var firstNeedMinCapPoint = false;
            var cylIntersects = LineIntersection(Axis, Radius, Anchor, anchorLine, direction).ToList();
            if (cylIntersects.Count > 0)
            {
                var dot = cylIntersects[0].intersection.Dot(Axis);
                firstNeedMinCapPoint = double.IsFinite(MinDistanceAlongAxis) && MinDistanceAlongAxis > dot;
                firstNeedMaxCapPoint = double.IsFinite(MaxDistanceAlongAxis) && MaxDistanceAlongAxis < dot;
                if (!firstNeedMinCapPoint && !firstNeedMaxCapPoint)
                    yield return cylIntersects[0];
            }
            var secondNeedMaxCapPoint = false;
            var secondNeedMinCapPoint = false;
            if (cylIntersects.Count > 1)
            {
                var dot = cylIntersects[1].intersection.Dot(Axis);
                secondNeedMinCapPoint = double.IsFinite(MinDistanceAlongAxis) && MinDistanceAlongAxis > dot;
                secondNeedMaxCapPoint = double.IsFinite(MaxDistanceAlongAxis) && MaxDistanceAlongAxis < dot;
                if (!secondNeedMinCapPoint && !secondNeedMaxCapPoint)
                    yield return cylIntersects[1];
            }
            if ((firstNeedMinCapPoint && secondNeedMinCapPoint) || (firstNeedMaxCapPoint && secondNeedMaxCapPoint))
                // if both intersections are below the min plane or above the max plane then no intersection
                yield break;
            if (firstNeedMinCapPoint || secondNeedMinCapPoint)
            {
                var minPlaneIntersect = Plane.LineIntersection(Axis, MinDistanceAlongAxis, anchorLine, direction);
                if (!minPlaneIntersect.intersection.IsNull()) yield return minPlaneIntersect;
            }
            if (firstNeedMaxCapPoint || secondNeedMaxCapPoint)
            {
                var maxPlaneIntersect = Plane.LineIntersection(Axis, MaxDistanceAlongAxis, anchorLine, direction);
                if (!maxPlaneIntersect.intersection.IsNull()) yield return maxPlaneIntersect;
            }
        }

        /// <summary>
        /// Finds the intersection between a cylinder and a line. Returns true if intersecting.
        /// </summary>
        /// <param name="axis">The axis of the cylinder.</param>
        /// <param name="radius">The radius of the cylinder.</param>
        /// <param name="anchorCyl">The anchor of the cylinder..</param>
        /// <param name="anchorLine">The anchor of the line.</param>
        /// <param name="direction">The direction of the line.</param>
        /// <param name="point1">One of the intersecting points.</param>
        /// <param name="point2">The other of the intersecting points.</param>
        /// <param name="t1">The parametric distance from the anchor along the line to point1.</param>
        /// <param name="t2">The parametric distance from the anchor along the line to point2.</param>
        /// <returns>A bool where true is intersecting.</returns>
        public static IEnumerable<(Vector3 intersection, double lineT)> LineIntersection(Vector3 axis, double radius, Vector3 anchorCyl, Vector3 anchorLine, Vector3 direction)
        {
            direction = direction.Normalize();
            var minDistance = MiscFunctions.SkewedLineIntersection(anchorCyl, axis, anchorLine, direction, out _, out var cylAxisPoint, out var linePoint, out _,
                out var tChordCenter);

            if (minDistance.IsPracticallySame(radius)) // one intersection
                yield return (linePoint, tChordCenter);
            if (minDistance > radius) // no intersection
                yield break;
            // here, the halfChoordLength is the distance from the chordCenter to where it would intersect the circle of the cylinder
            var halfChordLength = Math.Sqrt(radius * radius - minDistance * minDistance);
            var sinAngleLineCylinder = axis.Cross(direction).Length();
            var distanceToCylinder = halfChordLength / sinAngleLineCylinder;
            yield return (linePoint - distanceToCylinder * direction, tChordCenter - distanceToCylinder);
            yield return (linePoint + distanceToCylinder * direction, tChordCenter + distanceToCylinder);
        }


        //public TessellatedSolid AsTessellatedSolid()
        //{
        //    var faces = new List<TriangleFace>();
        //    foreach(var face in Faces)
        //    {
        //        var vertices = new Vertex[] { face.C, face.B, face.A }; //reverse the vertices
        //        faces.Add(new TriangleFace(vertices, face.Normal * -1)));
        //    }
        //    //Add the top and bottom faces
        //    //Build the cylinder along the axis
        //    //First, get the planes on the top and bottom.
        //    //Second, determine which plane is further along the axis. The faces on this plane will have a normal == axis
        //    //The bottom plane will have faces in the reverse of the axis.
        //    var plane1 = MiscFunctions.GetPlaneFromThreePoints(Loop1[0].Coordinates, Loop1[1].Coordinates, Loop1[2].Coordinates);
        //    var plane
        //}
        #endregion
    }
}