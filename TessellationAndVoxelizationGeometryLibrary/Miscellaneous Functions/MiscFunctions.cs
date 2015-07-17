using System;
using System.Collections.Generic;
using StarMathLib;

namespace TVGL
{
    public static class MiscFunctions
    {
        #region Flatten to 2D
        /// <summary>
        /// Returns the positions (array of 3D arrays) of the vertices as that they would be represented in
        /// the x-y plane (although the z-values will be non-zero). This does not destructively alter
        /// the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="MergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[] direction,
            Boolean MergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction);
            return Get2DProjectionPoints(vertices, transform, MergeDuplicateReferences);
        }

        /// <summary>
        /// Returns the positions (array of 3D arrays) of the vertices as that they would be represented in
        /// the x-y plane (although the z-values will be non-zero). This does not destructively alter
        /// the vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="backTransform">The back transform.</param>
        /// <param name="MergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[] direction, out double[,] backTransform,
            Boolean MergeDuplicateReferences = false)
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            return Get2DProjectionPoints(vertices, transform , MergeDuplicateReferences);
        }

        /// <summary>
        /// Get2s the d projection points.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="MergeDuplicateReferences">The merge duplicate references.</param>
        /// <returns>Point[].</returns>
        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[,] transform,
            Boolean MergeDuplicateReferences = false)
        {
            var points = new List<Point>();
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            foreach (var vertex in vertices)
            {
                pointAs4[0] = vertex.Position[0];
                pointAs4[1] = vertex.Position[1];
                pointAs4[2] = vertex.Position[2];
                pointAs4 = transform.multiply(pointAs4);
                var point2D = new[] { pointAs4[0], pointAs4[1]};
                if (MergeDuplicateReferences)
                {
                    var sameIndex = points.FindIndex(p => p.Position2D.IsPracticallySame(point2D));
                    if (sameIndex >= 0)
                    {
                        //Add reference and move to the next vertex.
                        points[sameIndex].References.Add(vertex);
                        continue;
                    }
                }
                points.Add(new Point(vertex, pointAs4[0], pointAs4[1]));
            }
            return points.ToArray();
        }

        public static double[][] Get2DProjectionPoints(IList<double[]> vertices, double[] direction)
        {
            var transform = TransformToXYPlane(direction);
            var points = new double[vertices.Count][];
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            for (var i = 0; i < vertices.Count; i++)
            {
                pointAs4[0] = vertices[i][0];
                pointAs4[1] = vertices[i][1];
                pointAs4[2] = vertices[i][2];
                pointAs4 = transform.multiply(pointAs4);
                points[i] = new[] { pointAs4[0], pointAs4[1] };
            }
            return points;
        }
        private static double[,] TransformToXYPlane(double[] direction)
        {
            double[,] backTransformStandIn;
            return TransformToXYPlane(direction, out backTransformStandIn);
        }
        private static double[,] TransformToXYPlane(double[] direction, out double[,] backTransform)
        {
            var xDir = direction[0];
            var yDir = direction[1];
            var zDir = direction[2];

            double[,] rotateX, rotateY, backRotateX, backRotateY;
            if (xDir.IsNegligible() && zDir.IsNegligible())
            {
                rotateX = StarMath.RotationX(Math.Sign(yDir) * Math.PI / 2, true);
                backRotateX = StarMath.RotationX(-Math.Sign(yDir) * Math.PI / 2, true);
                backRotateY = rotateY = StarMath.makeIdentity(4);
            }
            else if (zDir.IsNegligible())
            {
                rotateY = StarMath.RotationY(-Math.Sign(xDir) * Math.PI / 2, true);
                backRotateY = StarMath.RotationY(Math.Sign(xDir) * Math.PI / 2, true);
                var rotXAngle = Math.Atan(yDir / Math.Abs(xDir));
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            else
            {
                var rotYAngle = Math.Atan(xDir / zDir);
                rotateY = StarMath.RotationY(-rotYAngle, true);
                backRotateY = StarMath.RotationY(rotYAngle, true);
                var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
                var rotXAngle = Math.Atan(yDir / baseLength);
                rotateX = StarMath.RotationX(rotXAngle, true);
                backRotateX = StarMath.RotationX(-rotXAngle, true);
            }
            backTransform = backRotateY.multiply(backRotateX);
            return rotateX.multiply(rotateY);
        }
        #endregion

        #region Angle between Edges/Lines
        internal static double SmallerAngleBetweenEdges(Edge edge1, Edge edge2)
        {
            var axis = edge1.Vector.crossProduct(edge2.Vector);
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return Math.Min(AngleBetweenEdgesCW(twoDEdges[0], twoDEdges[1]),
                AngleBetweenEdgesCCW(twoDEdges[0], twoDEdges[1]));
        }
        internal static double SmallerAngleBetweenEdges(double[] v0, double[] v1)
        {
            return Math.Min(AngleBetweenEdgesCW(v0, v1), AngleBetweenEdgesCCW(v0, v1));
        }

        internal static double AngleBetweenEdgesCW(Edge edge1, Edge edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return AngleBetweenEdgesCW(twoDEdges[0], twoDEdges[1]);
        }
        internal static double AngleBetweenEdgesCCW(Edge edge1, Edge edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1.Vector, edge2.Vector }, axis);
            return AngleBetweenEdgesCCW(twoDEdges[0], twoDEdges[1]);
        }
        internal static double AngleBetweenEdgesCW(double[] edge1, double[] edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1, edge2 }, axis);
            return AngleBetweenEdgesCW(twoDEdges[0], twoDEdges[1]);
        }
        internal static double AngleBetweenEdgesCCW(double[] edge1, double[] edge2, double[] axis)
        {
            var twoDEdges = Get2DProjectionPoints(new[] { edge1, edge2 }, axis);
            return AngleBetweenEdgesCCW(twoDEdges[0], twoDEdges[1]);
        }
        internal static double AngleBetweenEdgesCW(Point a, Point b, Point c)
        {
            return AngleBetweenEdgesCW(new[] { b.X - a.X, b.Y - a.Y }, new[] { c.X - b.X, c.Y - b.Y });
        }
        internal static double AngleBetweenEdgesCCW(Point a, Point b, Point c)
        {
            return AngleBetweenEdgesCCW(new[] { b.X - a.X, b.Y - a.Y }, new[] { c.X - b.X, c.Y - b.Y });
        }

        internal static double AngleBetweenEdgesCW(double[] v0, double[] v1)
        {
            return 2 * Math.PI - AngleBetweenEdgesCCW(v0, v1);
        }

        internal static double AngleBetweenEdgesCCW(double[] v0, double[] v1)
        {
            var angleV0 = Math.Atan2(v0[1], v0[0]);
            var angleV1 = Math.Atan2(v1[1], v1[0]);
            var angleChange = Math.PI - (angleV1 - angleV0);
            if (angleChange > 2 * Math.PI) return angleChange - 2 * Math.PI;
            if (angleChange < 0) return angleChange + 2 * Math.PI;
            return angleChange;
        }
        #endregion

        #region Intersection Method (between lines, between planes, etc.)
        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2, out double[] DirectionOfLine, out double[] PointOnLine)
        {
            DirectionOfLine = n1.crossProduct(n2).normalize();
            LineIntersectingTwoPlanes(n1, d1, n2, d2, DirectionOfLine, out PointOnLine);
        }
        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2, double[] DirectionOfLine, out double[] PointOnLine)
        {
            /* to find the point on the line...well a point on the line, it turns out that one has three unknowns (px, py, pz)
             * and only two equations. Let's put the point on the plane going through the origin. So this plane would have a normal 
             * of v (or DirectionOfLine). */
            var A = new double[3, 3];
            A.SetRow(0, n1);
            A.SetRow(1, n2);
            A.SetRow(2, DirectionOfLine);
            var b = new[] { d1, d2, 0 };
            PointOnLine = StarMath.solve(A, b);
        }
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2)
        {
            double[] center;
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out t1, out t2);
        }
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] center)
        {
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out t1, out t2);
        }

        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2, out double[] center,
            out double t1, out double t2)
        {
            var a00 = n1[0] * n1[0] + n1[1] * n1[1] + n1[2] * n1[2];
            var a01 = -n1[0] * n2[0] - n1[1] * n2[1] - n1[2] * n2[2];
            var a10 = n1[0] * n2[0] + n1[1] * n2[1] + n1[2] * n2[2];
            var a11 = -n2[0] * n2[0] - n2[1] * n2[1] - n2[2] * n2[2];
            var b0 = n1[0] * (p2[0] - p1[0]) + n1[1] * (p2[1] - p1[1]) + n1[2] * (p2[2] - p1[2]);
            var b1 = n2[0] * (p2[0] - p1[0]) + n2[1] * (p2[1] - p1[1]) + n2[2] * (p2[2] - p1[2]);
            var A = new[,] { { a00, a01 }, { a10, a11 } };
            var b = new[] { b0, b1 };
            var t = StarMath.solve(A, b);
            t1 = t[0];
            t2 = t[1];
            var interSect1 = new[] { p1[0] + n1[0] * t1, p1[1] + n1[1] * t1, p1[2] + n1[2] * t1 };
            var interSect2 = new[] { p2[0] + n2[0] * t2, p2[1] + n2[1] * t2, p2[2] + n2[2] * t2 };
            center = new[] { (interSect1[0] + interSect2[0]) / 2, (interSect1[1] + interSect2[1]) / 2, (interSect1[2] + interSect2[2]) / 2 };
            return DistancePointToPoint(interSect1, interSect2);
        }
        #endregion

        #region Distance Methods (between point, line, and plane)
        /// <summary>
        /// Returns the distance the point to line.
        /// </summary>
        /// <param name="qPoint">The q point that is off of the line.</param>
        /// <param name="lineRefPt">The line reference point on the line.</param>
        /// <param name="lineVector">The line direction vector.</param>
        /// <returns></returns>
        public static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector)
        {
            double[] dummy;
            return DistancePointToLine(qPoint, lineRefPt, lineVector, out dummy);
        }
        /// <summary>
        /// Distances the point to line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns></returns>
        public static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector, out double[] pointOnLine)
        {
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
             * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            var t = (lineVector[0] * (qPoint[0] - lineRefPt[0]) + lineVector[1] * (qPoint[1] - lineRefPt[1]) + lineVector[2] * (qPoint[2] - lineRefPt[2]))
                / (lineVector[0] * lineVector[0] + lineVector[1] * lineVector[1] + lineVector[2] * lineVector[2]);
            pointOnLine = new[] { lineRefPt[0] + lineVector[0] * t, lineRefPt[1] + lineVector[1] * t, lineRefPt[2] + lineVector[2] * t };
            return DistancePointToPoint(qPoint, pointOnLine);
        }

        /// <summary>
        /// Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPoint(double[] p1, double[] p2)
        {
            var dX = p1[0] - p2[0];
            var dY = p1[1] - p2[1];
            var dZ = p1[2] - p2[2];
            return Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
        }

        /// <summary>
        /// Returns the signed distance of the point to the plane.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="positionOnPlane">The position on plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(double[] point, double[] normalOfPlane, double[] positionOnPlane)
        {
            return DistancePointToPlane(point, normalOfPlane, positionOnPlane.dotProduct(normalOfPlane));
        }

        /// <summary>
        /// Returns the signed distance of the point to the plane. If the point is "above" the plane, then a positive
        /// distance is return - if "below" then negative. This "above" means that the point is on the side of the
        /// plane that the normal points towards.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="signedDistanceToPlane">The signed distance to plane.</param>
        /// <returns>the distance between the two 3D points.</returns>
        public static double DistancePointToPlane(double[] point, double[] normalOfPlane, double signedDistanceToPlane)
        {
            return normalOfPlane.dotProduct(point) - signedDistanceToPlane;
        }

        /// <summary>
        /// Finds the point on the plane made by a line (which is described by connecting point1 and point2) intersecting
        /// with that plane.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="point1">The point1.</param>
        /// <param name="point2">The point2.</param>
        /// <returns>Vertex.</returns>
        public static Vertex PointOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, Vertex point1, Vertex point2)
        {
            var d1 = normalOfPlane.dotProduct(point1.Position);
            var d2 = normalOfPlane.dotProduct(point2.Position);
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            var position = new double[3];
            for (var i = 0; i < 3; i++)
                position[i] = point2.Position[i] * fraction + point1.Position[i] * (1 - fraction);
            return new Vertex(position);
        }

        /// <summary>
        /// Finds the point on the plane made by a ray. If that ray is not going to pass through the 
        /// that plane, then null is returned.
        /// </summary>
        /// <param name="normalOfPlane">The normal of plane.</param>
        /// <param name="distOfPlane">The dist of plane.</param>
        /// <param name="rayPosition">The ray position.</param>
        /// <param name="rayDirection">The ray direction.</param>
        /// <returns>Vertex.</returns>
        public static double[] PointOnPlaneFromRay(double[] normalOfPlane, double distOfPlane, double[] rayPosition, double[] rayDirection)
        {
            var d1 = -DistancePointToPlane(rayDirection, normalOfPlane, distOfPlane);
            var angle = SmallerAngleBetweenEdges(normalOfPlane, rayDirection);
            var d2 = d1 / Math.Cos(angle);
            if (d2 < 0) return null;
            return rayPosition.add(rayDirection.multiply(d2));
        }
        #endregion
    }
}
