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
using System.IO;
using System.Linq;
using System.Threading;



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
        public override void Transform(Matrix4x4 transformMatrix, bool transformFacesAndVertices)
        {
            var transformedPerpXDir = perpXDir.IsNull()
                ? Vector3.Null
                : perpXDir.TransformNoTranslate(transformMatrix);
            base.Transform(transformMatrix, transformFacesAndVertices);
            Anchor = Anchor.Transform(transformMatrix);
            Axis = Axis.TransformNoTranslate(transformMatrix);
            Axis = Axis.Normalize();
            var rVector1 = Axis.GetPerpendicularDirection();
            var rVector2 = Radius * Axis.Cross(rVector1);
            rVector1 *= Radius;
            rVector1 = rVector1.TransformNoTranslate(transformMatrix);
            rVector2 = rVector2.TransformNoTranslate(transformMatrix);
            Radius = Math.Sqrt((rVector1.LengthSquared() + rVector2.LengthSquared()) / 2);
            perpXDir = Vector3.Null;
            perpYDir = Vector3.Null;
            if (!transformedPerpXDir.IsNull())
                PerpXDir = transformedPerpXDir;
            // we currently don't allow the cylinder to be squished into an elliptical cylinder
            // so the radius is the average of the two radius component vectors after the 
            // transform. Earlier, we were doing 
            //Radius*=transformMatrix.M11;
            // but this is not correct since M11 is often non-unity during rotation
        }

        /// <summary>
        /// Gets or sets the radial direction represented by both joined boundaries of the developed cylinder.
        /// </summary>
        public Vector3 PerpXDir
        {
            get
            {
                if (perpXDir.IsNull())
                    perpXDir = Axis.GetPerpendicularDirection();
                else
                {
                    var axisDirection = Axis.Normalize();
                    perpXDir -= perpXDir.Dot(axisDirection) * axisDirection;
                    perpXDir = perpXDir.LengthSquared().IsNegligible()
                        ? Axis.GetPerpendicularDirection()
                        : perpXDir.Normalize();
                }
                return perpXDir;
            }
            set
            {
                if (Axis.LengthSquared().IsNegligible())
                {
                    perpXDir = value;
                    perpYDir = Vector3.Null;
                    return;
                }
                var axisDirection = Axis.Normalize();
                var radialDirection = value - value.Dot(axisDirection) * axisDirection;
                if (radialDirection.LengthSquared().IsNegligible())
                    throw new ArgumentException("The cylinder perpendicular X direction must be perpendicular to the cylinder axis.",
                        nameof(value));
                perpXDir = radialDirection.Normalize();
                perpYDir = Vector3.Null;
            }
        }
        /// <summary>
        /// The face x dir
        /// </summary>
        private Vector3 perpXDir = Vector3.Null;

        private Vector3 PerpYDir
        {
            get
            {
                if (perpYDir.IsNull())
                    perpYDir = Axis.Cross(PerpXDir);
                return perpYDir;
            }
        }
        /// <summary>
        /// The face y dir
        /// </summary>
        private Vector3 perpYDir = Vector3.Null;


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

        private Circle _circle;
        /// <summary>
        /// Gets the circle.
        /// </summary>
        /// <value>The circle.</value>
        public Circle Circle
        {
            get
            {
                if (_circle.Area == 0)
                {
                    var transform = MiscFunctions.TransformToXYPlane(Axis, out var _backTransform);
                    var center = Anchor.ConvertTo2DCoordinates(transform);
                    _circle = new Circle(center, Radius * Radius);
                }
                return _circle;
            }
            set
            {
                _circle = value;
            }
        }

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


        public override string KeyString => "Cylinder|" + Axis.ToString() +
            "|" + Anchor.ToString() + "|" + Radius.ToString("F5") + "|" + MinDistanceAlongAxis.ToString("F5")
            + "|" + MaxDistanceAlongAxis.ToString("F5") + GetCommonKeyDetails();
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
        public Cylinder(Vector3 axis, Vector3 anchor, double radius, IEnumerable<TriangleFace> faces)
        {
            Axis = axis;
            Anchor = anchor;
            Radius = radius;
            SetFacesAndVertices(faces);
            if (faces != null)
            {
                var (min, max) = MinimumEnclosure.GetDistanceToExtremeVertex(Vertices, axis, out _, out _);//vertices are set in base constructor
                MinDistanceAlongAxis = min;
                MaxDistanceAlongAxis = max;
            }
        }


        public override Vector3 GetNormalAtPoint(Vector3 point)
        {
            var a = point - Anchor;
            var b = Axis.Cross(a);
            var outwardVector = b.Cross(Axis).Normalize();
            if (IsPositive.GetValueOrDefault(true)) return outwardVector;
            else return -outwardVector;
        }

        /// <summary>
        /// Points the membership.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>System.Double.</returns>
        public override double DistanceToPoint(Vector3 point)
        {
            var d = (point - Anchor).Cross(Axis).Length() - Radius;
            // if d is positive, then the point is outside the cylinder
            // if d is negative, then the point is inside the cylinder
            if (IsPositive.GetValueOrDefault(true)) return d;
            else return -d;
        }

        /// <summary>
        /// Returns the point on the object that is closest to the specified position in 3D space.
        /// </summary>
        /// <param name="point">The position in world coordinates for which to find the nearest point on the object.</param>
        /// <returns>A <see cref="Vector3"/> representing the closest point on the object to the specified position.</returns>
        /// <exception cref="NotImplementedException">The method is not implemented.</exception>
        public override Vector3 ClosestPointOnSurfaceToPoint(Vector3 point)
        {
            var d = (point - Anchor).Dot(Axis);
            var axisPoint = Anchor + d * Axis;
            var radialDir = (point - axisPoint).Normalize();
            return axisPoint + Radius * radialDir;
        }

        protected override void CalculateIsPositive()
        {
            if (Faces == null || !Faces.Any() || Area.IsNegligible()) return;
            var axisPointUnderFace = Anchor + (LargestFace.Center - Anchor).Dot(Axis) * Axis;
            isPositive = (LargestFace.Center - axisPointUnderFace).Dot(LargestFace.Normal) > 0;
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
        => LineIntersection(Axis, Radius, Anchor, anchorLine, direction).ToList();

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

        #region Flattening/Unrolling/Developing the Cylinder

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public override Vector2 TransformFrom3DTo2D(Vector3 point)
        => TransformPoint(point, out _);

        /// <summary>
        /// Transforms the from 3d to 2d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector2.</returns>
        public Vector2 TransformPoint(Vector3 point, out double angle)
        {
            var v = point - Anchor;
            var x = PerpXDir.Dot(v);
            var y = PerpYDir.Dot(v);
            angle = Math.Atan2(y, x);
            return new Vector2(angle * Radius, v.Dot(Axis));
        }

        /// <summary>
        /// Transforms the from 2d to 3d.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 TransformFrom2DTo3D(Vector2 point)
        {
            var angle = (point.X / Radius) % Math.Tau;
            var result = Anchor + Radius * Math.Cos(angle) * PerpXDir;
            result += Radius * Math.Sin(angle) * PerpYDir;
            result += point.Y * Axis;
            return result;
        }

        /// <summary>
        /// To fulfill the necessary override, we call a much more involved method that takes in
        /// 3D polygons and returns dictionaries and polygons. Here it is assumed that the input
        /// points are not a closed path.
        /// <param name="points">The ordered points on the cone.</param>
        /// <returns>The points on the developed (flattened) cone.</returns>
        public override IEnumerable<Vector2> TransformFrom3DTo2D(IEnumerable<Vector3> points)
          => TransformFrom3DTo2D([new Polygon3D(points, false)], out _, out _).First().Path;

        /// <summary>
        /// Creates the polygons that are drawn on the cylinder.
        /// </summary>
        /// <param name="polyon3Ds"></param>
        /// <param name="defineSeam"></param>
        /// <param name="VertexMapping"></param>
        /// <param name="newVertexSpacing"></param>
        /// <returns></returns>
        public List<Polygon> TransformFrom3DTo2D(IEnumerable<Polygon3D> polyon3Ds,
            out Dictionary<Vertex2D, Vertex2D> MatchingSeamVertices,
            out Dictionary<Vector3, Vertex2D> VertexMapping, double newVertexSpacing = -1.0)
        {
            var listOf3DPolygons = polyon3Ds as IList<Polygon3D> ?? polyon3Ds.ToList();
            var allResults = new List<Polygon>();
            var properPolygonTrees = new List<Polygon>();
            var sortedStraddleHoles = new SortedList<double, (List<Vertex2D> vertex2Ds, int loopID)>();
            VertexMapping = new Dictionary<Vector3, Vertex2D>();
            MatchingSeamVertices = new Dictionary<Vertex2D, Vertex2D>();
            var encirclingLoops = new SortedList<double, (List<Vertex2D> vertex2Ds, bool windsPositive, int loopID)>();
            var circumference = Math.Tau * Radius;
            var transform2D = Axis.TransformToXYPlane(out var backTransform);
            var loopID = 0;
            foreach (var poly3 in listOf3DPolygons)
            {
                bool? isClosed = null;
                foreach (var path in poly3.AllPaths())
                {
                    if (!isClosed.HasValue) isClosed = poly3.IsClosed;
                    // given that second, thirc, etc. paths in a polygon3D are holes, then
                    // they are necessarily closed. So, this is a little logic puzzle making
                    // isClosed start as null, then subsequent paths are all marked as true
                    else isClosed = true;
                    var windingAngle = MiscFunctions.FindWindingAroundAxis(path, transform2D, Anchor, out var minAngle,
                        out var maxAngle, isClosed.Value);
                    // even if there is a crazy helical shaped hole that first wraps around a dozen times. The winding angle
                    // should be 0 if it comes back the other way.
                    var encirclesAxis = isClosed.Value && Math.Abs(windingAngle) > 1.67 * Math.PI;

                    // Use the path overload so angular coordinates are unwrapped continuously across the
                    // Atan2 branch cut. Transforming each point independently folds a full encircling loop
                    // back into the principal interval and prevents its two boundaries from being paired.
                    var transformedPoints = TransformPoints(path, circumference);
                    var pointList2D = new Vertex2D[path.Count].ToList();
                    for (int i = 0; i < pointList2D.Count; i++)
                    {
                        var point3D = path[i];
                        var vertex2D = new Vertex2D(transformedPoints[i], i, loopID);
                        pointList2D[i] = vertex2D;
                        VertexMapping.Add(point3D, vertex2D);
                    }
                    // there are 3 types of results:
                    // 1. a closed path that does not encircle the axis. Imagine drawing a loop on the side of the cylinder
                    // 2. a closed path that does encircle the axis - this will be an open polygon where the
                    //    ends are offset by 2π*Radius in the x-direction
                    // 3. an open path - this will be an open polygon
                    if (encirclesAxis)
                        // store for later in a sorted list by the lower of the endpoint y-values
                        encirclingLoops.Add(Math.Min(pointList2D[0].Y, pointList2D[^1].Y), (pointList2D, windingAngle > 0, loopID));
                    else if (isClosed.Value)
                    {   // closed but not encircling => these are holes.
                        // maxAngle cannot be greater than Pi (or less than -Pi), but minAngle can be less than -Pi. If it is,
                        // then so, then the polygon straddles the seam
                        if (minAngle < -Math.PI)
                            sortedStraddleHoles.Add(pointList2D.Min(p => p.Y), (pointList2D, loopID));
                        else
                            properPolygonTrees.Add(new Polygon(pointList2D, loopID, true));
                    }
                    else // hopeless open path, so we will return it now
                        allResults.Add(new Polygon(pointList2D, loopID, false));
                    loopID++;
                }
            }

            // now to handle the encircling loops.
            // if the first one is going the wrong way, then it is not a proper enclosure of the cylinder, and we will consider it an open polygon
            if (encirclingLoops.Count > 0 && !encirclingLoops.Values[0].windsPositive)
            {
                allResults.Add((new Polygon(encirclingLoops.Values[0].vertex2Ds, encirclingLoops.Values[0].loopID, true)));
                encirclingLoops.RemoveAt(0);
            }
            // now for the rest of the encircling loops, we will combine them into pairs, and each pair will be a closed polygon
            var straddleHoleWorkingIndex = 0;
            for (int i = 1; i < encirclingLoops.Count; i += 2)
            {
                var straddleHoles = GetCylinderStraddlingHoles(sortedStraddleHoles, ref straddleHoleWorkingIndex,
                    encirclingLoops.Keys[i - 1], encirclingLoops.Keys[i]);
                // because the straddle holes are between the two loops and there are no intersections,
                // there order is useful in splitting and adding to the sides of the seam.
                var btmLoop = encirclingLoops.Values[i - 1];
                loopID = btmLoop.vertex2Ds[0].LoopID;
                if (!btmLoop.windsPositive)
                    throw new ArgumentException("The bottom loop of a pair must wind positively around the cylinder.");
                MoveSingleTurnLoopIntoCylinderStrip(btmLoop.vertex2Ds, circumference, increasing: true);

                var topLoop = encirclingLoops.Values[i];
                if (topLoop.windsPositive)
                    throw new ArgumentException("The top loop of a pair must wind negatively around the cylinder.");
                MoveSingleTurnLoopIntoCylinderStrip(topLoop.vertex2Ds, circumference, increasing: false);

                // Intersect each loop's closing edge with the seam once, then use the same height on both
                // developed copies. The paired seam vertices must roll back to one physical 3D point.
                var lowerSeamY = GetCylinderSeamHeight(btmLoop.vertex2Ds, circumference, true);
                var upperSeamY = GetCylinderSeamHeight(topLoop.vertex2Ds, circumference, false);
                // todo:need to plan for the straddle holes starting here.
                var seamVertexSpacing = double.IsNaN(newVertexSpacing)
                    ? double.PositiveInfinity
                    : newVertexSpacing > 0
                        ? newVertexSpacing
                        : GetRootMeanSquareEdgeLength(btmLoop.vertex2Ds, topLoop.vertex2Ds);
                var seamHeight = upperSeamY - lowerSeamY;
                var lastIndexOnSeam = 1 + (int)(seamHeight / seamVertexSpacing);
                var stepSize = seamHeight / lastIndexOnSeam;

                // build the new polygon starting with the upperLeft
                var polygonPoints = new List<Vertex2D>();
                var k = 0;
                for (int j = 0; j < lastIndexOnSeam; j++)
                {
                    var y = upperSeamY - j * stepSize;
                    polygonPoints.Add(new Vertex2D(new Vector2(0, y), k++, loopID));
                }
                polygonPoints.Add(new Vertex2D(new Vector2(0, lowerSeamY), k++, loopID));

                polygonPoints.AddRange(btmLoop.vertex2Ds);
                var indexMatchedToStart = polygonPoints.Count;
                for (int j = 0; j < lastIndexOnSeam; j++)
                {
                    var y = lowerSeamY + j * stepSize;
                    var vRight = new Vertex2D(new Vector2(circumference, y), k++, loopID);
                    var vLeft = polygonPoints[lastIndexOnSeam - j];
                    polygonPoints.Add(vRight);
                    MatchingSeamVertices.Add(vRight, vLeft);
                    MatchingSeamVertices.Add(vLeft, vRight);
                }
                // todo: end of straddle holes new code
                var vUpperRight = new Vertex2D(new Vector2(circumference, upperSeamY), k++, loopID);
                MatchingSeamVertices.Add(vUpperRight, polygonPoints[0]);
                MatchingSeamVertices.Add(polygonPoints[0], vUpperRight);
                polygonPoints.AddRange(topLoop.vertex2Ds);
                foreach (var v in topLoop.vertex2Ds)
                    v.LoopID = loopID;

                // Now we can create the polygon
                properPolygonTrees.Add(new Polygon(polygonPoints, loopID, true));
            }
            // finally, if there is an odd number of encircling loops, then the last one will be an open polygon
            if (int.IsOddInteger(encirclingLoops.Count))
                allResults.Add(new Polygon(encirclingLoops.Values[^1].vertex2Ds, encirclingLoops.Values[^1].vertex2Ds[0].LoopID, true));
            properPolygonTrees = properPolygonTrees.CreateShallowPolygonTrees(false);
            // now we need to resolve the straddling holes. They are holes that straddle the seam, so they need to be split into
            // two pieces, one on each side of the seam. The two pieces will be connected by a seam edge, and the two pieces will
            // be added to the properPolygonTrees list.
            allResults.AddRange(properPolygonTrees);
            return allResults;
        }

        private Vector2[] TransformPoints(IList<Vector3> points, double circumference)
        {
            // the cylinder will be unrolled, and the tangential angle around the cylinder will be transformed
            // into the x (horizontal coordinate). The first point provides a reference for the additional points
            var result = new Vector2[points.Count];
            var prevAngle = 0.0;
            for (int i = 0; i < points.Count; i++)
            {
                var this2DPoint = TransformPoint(points[i], out var thisAngle);
                // if next point is off by more than 180 degrees,then it is likely the angle is wrong and needs to be adjusted
                var angleAltered = false;
                while (thisAngle - prevAngle > Math.PI)
                {
                    angleAltered = true;
                    thisAngle -= Math.Tau;
                }
                while (prevAngle - thisAngle > Math.PI)
                {
                    angleAltered = true;
                    thisAngle += Math.Tau;
                }
                if (angleAltered)
                    result[i] = new Vector2(thisAngle * Radius, this2DPoint.Y);
                else result[i] = this2DPoint;
                prevAngle = thisAngle;
            }
            return result;
        }



        internal static double GetRootMeanSquareEdgeLength<T>(params IEnumerable<IList<T>> loops)
            where T : IVector2D
        {
            var totalLengthSqdSum = 0.0;
            var numLengths = 0;
            foreach (var loop in loops)
            {
                numLengths += Math.Max(0, loop.Count - 1);
                for (int j = 1; j < loop.Count; j++)
                    totalLengthSqdSum += (loop[j - 1].X - loop[j].X) * (loop[j - 1].X - loop[j].X) +
                        (loop[j - 1].Y - loop[j].Y) * (loop[j - 1].Y - loop[j].Y);
            }
            return numLengths == 0 ? double.NaN : Math.Sqrt(totalLengthSqdSum / numLengths);
        }

        private static List<(List<Vertex2D> vertex2Ds, int loopID)> GetCylinderStraddlingHoles(SortedList<double, (List<Vertex2D> vertex2Ds, int loopID)> straddlingHoles,
            ref int workingIndex, double lowY, double highY)
        {
            throw new NotImplementedException();
        }

        private static void MoveSingleTurnLoopIntoCylinderStrip(List<Vertex2D> loop, double circumference, bool increasing)
        {
            if (loop.Count < 2)
                return;

            // Every X + k*circumference represents the same physical point on
            // the cylinder. First choose the representative in [0, circumference).
            foreach (var vertex in loop)
            {
                var wrappedX = vertex.X
                    - circumference * Math.Floor(vertex.X / circumference);

                vertex.Coordinates = new Vector2(wrappedX, vertex.Y);
            }

            // Wrapping creates one large jump where the path crosses the seam.
            // Move that jump to the list boundary so the bottom runs 0 -> C
            // and the top runs C -> 0.
            var cutIndex = 0;
            var largestWrapJump = 0.5 * circumference;

            for (var j = 1; j < loop.Count; j++)
            {
                var wrapJump = increasing
                    ? loop[j - 1].X - loop[j].X   // large downward jump
                    : loop[j].X - loop[j - 1].X;  // large upward jump

                if (wrapJump > largestWrapJump)
                {
                    largestWrapJump = wrapJump;
                    cutIndex = j;
                }
            }

            if (cutIndex == 0)
                return;

            var beforeSeam = loop.GetRange(0, cutIndex);
            loop.RemoveRange(0, cutIndex);
            loop.AddRange(beforeSeam);
        }

        private static double GetCylinderSeamHeight(IList<Vertex2D> loop, double circumference,
            bool isIncreasing)
        {
            (var startX, var endX) = isIncreasing
                ? (loop[0].X, loop[^1].X)
                : (loop[^1].X, loop[0].X);
            (var startY, var endY) = isIncreasing
                ? (loop[0].Y, loop[^1].Y)
                : (loop[^1].Y, loop[0].Y);
            while (startX > circumference) startX -= circumference;
            // in this way, the endX is now negative, and we simply have to find the crossing at 0.0
            while (endX > 0) endX -= circumference;
            //return endY + (startY - endY) * (0.0 - endX) / (startX - endX);
            // this is correct, but writing more simply (less intuitively) as:
            return endY + endX * (endY - startY) / (startX - endX);
        }

        #endregion
    }
}
