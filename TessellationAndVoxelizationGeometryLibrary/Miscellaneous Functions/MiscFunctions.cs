using System;
using System.Collections.Generic;
using System.Linq;
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
                var rotYAngle = -Math.Atan(xDir / zDir);
                rotateY = StarMath.RotationY(rotYAngle, true);
                backRotateY = StarMath.RotationY(-rotYAngle, true);
                var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
                var rotXAngle = Math.Sign(zDir)*Math.Atan(yDir / baseLength);
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
        //Gets the angle between edges that are ordered in a CCW list. 
        //NOTE: This is opposite from getting the CCW angle from v0 and v1.
        internal static double AngleBetweenEdgesCCW(double[] v0, double[] v1)
        {
            #region Law of Cosines Approach (Commented Out)
            ////This is an alternative approach to the one that is not commented out
            ////Use law of cosines to find smaller angle between two vectors
            //var aSq = v0[0] * v0[0] + v0[1] * v0[1];
            //var bSq = v1[0] * v1[0] + v1[1] * v1[1];
            //var cSq = (v0[0] + v1[0]) * (v0[0] + v1[0]) + (v0[1] + v1[1]) * (v0[1] + v1[1]);
            //var angle = Math.Acos((aSq + bSq - cSq) / (2 * Math.Sqrt(aSq) * Math.Sqrt(bSq)));
            ////Use cross product sign to determine if smaller angle is CCW from v0
            //var cross = v0[0] * v1[1] - v0[1] * v1[0];
            //if (Math.Sign(cross) < 0) angle = 2 * Math.PI - angle;
            #endregion

            var angleV0 = Math.Atan2(v0[1], v0[0]);
            var angleV1 = Math.Atan2(v1[1], v1[0]);
            var angleChange = Math.PI - (angleV1 - angleV0);
            if (angleChange > 2 * Math.PI) return angleChange - 2 * Math.PI;
            if (angleChange < 0) return angleChange + 2 * Math.PI;
            //if (Math.Abs(angle - angleChange) > 0.001) throw new Exception();
            return angleChange;
        }
        #endregion

        #region Intersection Method (between lines, planes, solids, etc.)
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

        internal static bool ArcArcIntersection(double[][] arc1Vectors, double[][] arc2Vectors, out double[][] intercepts)
        {
            intercepts = null;
            var tolerance = 0.0001;
            //Create two planes given arc1 and arc2
            var norm1 = arc1Vectors[0].crossProduct(arc1Vectors[1]).normalize(); //unit normal
            var norm2 = arc2Vectors[0].crossProduct(arc2Vectors[1]).normalize(); //unit normal

            //Check whether the planes are the same. 
            if (Math.Abs(norm1[0] - norm2[0]) < tolerance && Math.Abs(norm1[1] - norm2[1]) < tolerance
                && Math.Abs(norm1[2] - norm2[2]) < tolerance) return true; //All points intersect
            if (Math.Abs(norm1[0] + norm2[0]) < tolerance && Math.Abs(norm1[1] + norm2[1]) < tolerance
                && Math.Abs(norm1[2] + norm2[2]) < tolerance) return true; //All points intersect
            //ToDo: determine what to do with the above cases

            var position1 = norm1.crossProduct(norm2).normalize();
            var position2 = new[] { -position1[0], -position1[1], -position1[2] };
            var vertices = new[] { position1, position2 };
            //Check to see if the intersections are on the arcs
            for (var i = 0; i < 2; i++)
            {
                var l1 = Math.Acos(arc1Vectors[0].dotProduct(arc1Vectors[1]));
                var l2 = Math.Acos(arc1Vectors[0].dotProduct(vertices[i]));
                var l3 = Math.Acos(arc1Vectors[1].dotProduct(vertices[i]));
                var total1 = l1 - l2 - l3;
                l1 = Math.Acos(arc2Vectors[0].dotProduct(arc2Vectors[1]));
                l2 = Math.Acos(arc2Vectors[0].dotProduct(vertices[i]));
                l3 = Math.Acos(arc2Vectors[1].dotProduct(vertices[i]));
                var total2 = l1 - l2 - l3;
                if (!total1.IsNegligible() || !total2.IsNegligible()) continue;
                intercepts[0] = vertices[i];
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Returns lists of vertices that are inside vs. outside of each solid.
        /// </summary>
        /// <returns></returns>
        public static void FindSolidIntersections(TessellatedSolid solid1, TessellatedSolid solid2,
            out List<Vertex> verticesFromSolid1InsideSolid2, out List<Vertex> verticesFromSolid1OutsideSolid2,
            out List<Vertex> verticesFromSolid2InsideSolid1, out List<Vertex> verticesFromSolid2OutsideSolid1,
            bool onBoundaryIsInside = true)
        {
            //Set reference indices to keep track of which vertex belong to which point
            //NOTE: if the two solids are in fact the same solid, this will fail, but it
            //will work as long as it is a copy of the solid.
            var insideVertices = new List<Vertex>(solid1.Vertices);
            foreach (var vertex in insideVertices) vertex.ReferenceIndex = 1;
            var outsideVertices = new List<Vertex>(solid2.Vertices);
            foreach (var vertex in outsideVertices) vertex.ReferenceIndex = 2;
            
            //Set directions, where dir2 is perpendicular to dir1 and dir3 is perpendicular to both dir1 and dir2.
            var rnd = new Random();
            var direction1 = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() }.normalize();
            var direction2 = new[] { direction1[1] - direction1[2], -direction1[0], direction1[0] }.normalize(); //one of many
            var direction3 = direction1.crossProduct(direction2).normalize();
            var directions = new[] {direction1, direction2, direction3 };
            var allVertices = new List<Vertex>(insideVertices);
            allVertices.AddRange(outsideVertices);
            List<Vertex> sortedVertices;
            List<int[]> duplicateIndexRanges;
            SortAlongDirection(directions, allVertices, out sortedVertices, out duplicateIndexRanges);
            //if (onBoundaryIsInside && duplicateIndexRanges.Count > 1) return false;
            //Remove all duplicate vertices
            var offset = 0;
            foreach (var duplicateRange in duplicateIndexRanges)
            {
                sortedVertices.RemoveRange(duplicateRange[0] - offset, duplicateRange[1]);
                offset = offset + duplicateRange[1];
            }
            //The solids share all the same vertices (onBoundaryIsInside = false) was considered above
            //if (sortedVertices.Count < 1) return true; 
            //If the first or last vertex along the direction vectors was in the inside solid, then it is not inside
            //if (sortedVertices.First().ReferenceIndex == 1 || sortedVertices.Last().ReferenceIndex == 1) return false;

            //Perform a search along direction 1 looking for plane intercepts along direction 2.
            //This method assumes TRIANGLE FACES ONLY.
            var insideFaceList1 = new List<PolygonalFace>();
            var insideFaceList2 = new List<PolygonalFace>();
            var outsideFaceList1 = new List<PolygonalFace>();
            var outsideFaceList2 = new List<PolygonalFace>();
            verticesFromSolid1InsideSolid2 = new List<Vertex>();
            verticesFromSolid1OutsideSolid2 = new List<Vertex>();
            verticesFromSolid2InsideSolid1 = new List<Vertex>();
            verticesFromSolid2OutsideSolid1 = new List<Vertex>();
            foreach (var vertex in sortedVertices)
            {
                if (vertex.ReferenceIndex == 2)
                {
                    foreach (var triangle in vertex.Faces)
                    {
                        if (outsideFaceList1.Contains(triangle))
                        {
                            if (outsideFaceList2.Contains(triangle))
                            {
                                outsideFaceList1.Remove(triangle);
                                outsideFaceList2.Remove(triangle);
                            }
                            else outsideFaceList2.Add(triangle);
                        }
                        else outsideFaceList1.Add(triangle);
                    }
                    var faceCountBelow = 0;
                    var faceCountAbove = 0;
                    var inside = true;
                    foreach (var triangle in insideFaceList1)
                    {
                        double signedDistance;
                        //The following funtion returns null if the vertex does not intersect the triangle
                        var position = PointOnTriangleFromLine(triangle, vertex, direction2, out signedDistance);
                        if (position != null)
                        {
                            if (signedDistance.IsNegligible())
                            {
                                inside = onBoundaryIsInside;
                                //Set face counts to an odd number, and add vertex to list of inside vertices
                                faceCountAbove = 1;
                                faceCountBelow = 1;
                                break;
                            }
                            if (signedDistance < 0.0)
                            {
                                faceCountBelow++;
                            }
                            else
                            {
                                faceCountAbove++;
                            }
                        }
                    }
                    if (faceCountAbove == 0 || faceCountBelow == 0) inside = false;
                    if (faceCountAbove % 2 == 0 || faceCountBelow % 2 == 0) inside = false; //Even number of intercepts, means the vertex is outside
                    if (inside) verticesFromSolid2InsideSolid1.Add(vertex);
                    else verticesFromSolid2OutsideSolid1.Add(vertex);
                }
                else
                {
                    foreach (var triangle in vertex.Faces)
                    {
                        if (insideFaceList1.Contains(triangle))
                        {
                            if (insideFaceList2.Contains(triangle))
                            {
                                insideFaceList1.Remove(triangle);
                                insideFaceList2.Remove(triangle);
                            }
                            else insideFaceList2.Add(triangle);
                        }
                        else insideFaceList1.Add(triangle);
                    }
                    var faceCountBelow = 0;
                    var faceCountAbove = 0;
                    var inside = true;
                    foreach (var triangle in outsideFaceList1)
                    {
                        double signedDistance;
                        //The following funtion returns null if the vertex does not intersect the triangle
                        var position = PointOnTriangleFromLine(triangle, vertex, direction2, out signedDistance);
                        if (position != null)
                        {
                            if (signedDistance.IsNegligible())
                            {
                                inside = onBoundaryIsInside;
                                //Set face counts to an odd number, and add vertex to list of inside vertices
                                faceCountAbove = 1;
                                faceCountBelow = 1;
                                break;
                            }
                            if (signedDistance < 0.0)
                            {
                                faceCountBelow++;
                            }
                            else
                            {
                                faceCountAbove++;
                            }
                        }
                    }
                    if (faceCountAbove == 0 || faceCountBelow == 0) inside = false;
                    if (faceCountAbove % 2 == 0 || faceCountBelow % 2 == 0) inside = false; //Even number of intercepts, means the vertex is outside
                    if (inside) verticesFromSolid1InsideSolid2.Add(vertex);
                    else verticesFromSolid1OutsideSolid2.Add(vertex);
                }
            }
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

        /// <summary>
        /// Finds the point on the triangle made by a line. If that line is not going to pass through the 
        /// that triangle, then null is returned. The signed distance is positive if the vertex points to 
        /// the triangle along the direction (ray). User can also specify whether the edges of the triangle 
        /// are considered "inside."
        /// </summary>
        /// <param name="face"></param>
        /// <param name="vertex"></param>
        /// <param name="direction"></param>
        /// <param name="signedDistance"></param>
        /// <param name="onBoundaryIsInside"></param>
        /// <returns>Vertex.</returns>
        public static double[] PointOnTriangleFromLine(PolygonalFace face, Vertex vertex, double[] direction, out double signedDistance, bool onBoundaryIsInside = true)
        {
            var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position);
            signedDistance = -(vertex.Position.dotProduct(face.Normal) - distanceToOrigin) / (direction.dotProduct(face.Normal));
            //Note that if t == 0, then it is on the plane
            //else, find the intersection point and determine if it is inside the polygon (face)
            var newPoint = signedDistance.IsNegligible() ? vertex : new Vertex(vertex.Position.add(direction.multiply(signedDistance)));
            return IsPointInsideTriangle(face, newPoint, onBoundaryIsInside) ? newPoint.Position : null;
        }
        #endregion

        #region Sort Along Direction
        /// <summary>
        /// Returns a list of sorted vertices along a set direction. Ties are broken by direction[1] then direction[2] if available.
        /// </summary>
        /// <param name="directions"></param>
        /// <param name="vertices"></param>
        /// <param name="sortedVertices"></param>
        /// <param name="duplicateRanges"></param>
        /// <returns></returns>
        public static void SortAlongDirection(double[][] directions, List<Vertex> vertices, out List<Vertex> sortedVertices,
            out List<int[]> duplicateRanges)
        {
            //Get integer values for every vertex as distance along direction
            //Split positive and negative numbers into seperate lists. 0 is 
            //considered positive.
            //This is an O(n) preprocessing step
            duplicateRanges = new List<int[]>();
            sortedVertices = new List<Vertex>();
            var points = new List<Point>();
            foreach (var vertex in vertices)
            {
                //Get distance along 3 directions (2 & 3 to break ties) with accuracy to the 15th decimal place
                switch (directions.Count())
                {
                    case 1:
                    {
                        var dot1 = Math.Round(directions[0].dotProduct(vertex.Position)*1E+15);//Accuracy to the 15th decimal place
                        var point = new Point(vertex, dot1, 0.0, 0.0);
                        points.Add(point);
                    }
                        break;
                    case 2:
                    {
                        var dot1 = Math.Round(directions[0].dotProduct(vertex.Position)*1E+15);
                        var dot2 = Math.Round(directions[1].dotProduct(vertex.Position)*1E+15);
                        var point = new Point(vertex, dot1, dot2, 0.0);
                        points.Add(point);
                    }
                        break;
                    case 3:
                    {
                        var dot1 = Math.Round(directions[0].dotProduct(vertex.Position) * 1E+15);
                        var dot2 = Math.Round(directions[1].dotProduct(vertex.Position) * 1E+15);
                        var dot3 = Math.Round(directions[2].dotProduct(vertex.Position) * 1E+15);
                        var point = new Point(vertex, dot1, dot2, dot3);
                        points.Add(point); 
                    }
                        break;
                    default:
                        throw new System.ArgumentException("Must provide between 1 to 3 direction vectors");
                }
            }
            //Unsure what time domain this sort function uses. Note, however, rounding allows using the same
            //tolerance as the "isNeglible" star math function 
            var sortedPoints =
                points.OrderBy(point => point.X).ThenBy(point => point.Y).ThenBy(point => point.Z).ToList<Point>();


            //Linear operation to locate duplicates and convert back to a list of vertices
            var previousDuplicate = false;
            var startIndex = 0;
            sortedVertices.Add(sortedPoints[0].References[0]);
            var counter=0;
            int[] intRange;
            switch (directions.Count())
            {
                case 1:
                {
                    for (var i = 1; i < sortedPoints.Count; i++)
                    {
                        sortedVertices.Add(sortedPoints[i].References[0]);
                        if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X))
                        {
                            counter++;
                            if (previousDuplicate) continue;
                            startIndex = i - 1;
                            previousDuplicate = true;
                            counter++;
                        }
                        else if (previousDuplicate)
                        {
                            intRange = new[] { startIndex, counter };
                            duplicateRanges.Add(intRange);
                            previousDuplicate = false;
                            counter = 0;
                        }
                    }
                    //Add last duplicate group if necessary
                    if (!previousDuplicate) return;
                    intRange = new[] { startIndex, counter };
                    duplicateRanges.Add(intRange);
                }
                    break;
                case 2:
                {
                    for (var i = 1; i < sortedPoints.Count; i++)
                    {
                        sortedVertices.Add(sortedPoints[i].References[0]);
                        if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X) &&
                            sortedPoints[i - 1].Y.IsPracticallySame(sortedPoints[i].Y))
                        {
                            counter++;
                            if (previousDuplicate) continue;
                            startIndex = i - 1;
                            previousDuplicate = true;
                            counter++;
                        }
                        else if (previousDuplicate)
                        {
                            intRange = new[] { startIndex, counter };
                            duplicateRanges.Add(intRange);
                            previousDuplicate = false;
                            counter = 0;
                        }
                    }
                    //Add last duplicate group if necessary
                    if (!previousDuplicate) return;
                    intRange = new[] { startIndex, counter };
                    duplicateRanges.Add(intRange);
                }
                    break;
                case 3:
                {
                    for (var i = 1; i < sortedPoints.Count; i++)
                    {
                        sortedVertices.Add(sortedPoints[i].References[0]);
                        if (sortedPoints[i - 1].X.IsPracticallySame(sortedPoints[i].X) &&
                            sortedPoints[i - 1].Y.IsPracticallySame(sortedPoints[i].Y) &&
                            sortedPoints[i - 1].Z.IsPracticallySame(sortedPoints[i].Z))
                        {
                            counter++;
                            if (previousDuplicate) continue;
                            startIndex = i - 1;
                            previousDuplicate = true;
                            counter++;
                        }
                        else if (previousDuplicate)
                        {
                            intRange = new[] { startIndex, counter };
                            duplicateRanges.Add(intRange);
                            previousDuplicate = false;
                            counter = 0;
                        }
                    }
                    //Add last duplicate group if necessary
                    if (!previousDuplicate) return;
                    intRange = new[] { startIndex, counter };
                    duplicateRanges.Add(intRange);
                }
                    break;
                default:
                    throw new System.ArgumentException("Must provide between 1 to 3 direction vectors");
            }
        }
        #endregion

        #region isInside Methods (is 2D point inside polygon, vertex inside solid, ect.)
        /// <summary>
        /// Returns whether a vertex lies on a triangle. User can specify whether the edges of the 
        /// triangle are considered "inside."
        /// </summary>
        public static bool IsPointInsideTriangle(PolygonalFace triangle, Vertex vertexInQuestion, bool onBoundaryIsInside = true)
        {
            return IsPointInsideTriangle(triangle.Vertices, vertexInQuestion, onBoundaryIsInside);
        }

        /// <summary>
        /// Returns whether a vertex lies on a triangle. User can specify whether the edges of the 
        /// triangle are considered "inside."
        /// </summary>
        /// <references>
        /// http://www.blackpawn.com/texts/pointinpoly/
        /// </references>
        /// <param name="vertices"></param>
        /// <param name="vertexInQuestion"></param>
        /// <param name="onBoundaryIsInside"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static bool IsPointInsideTriangle(IList<Vertex> vertices, Vertex vertexInQuestion, bool onBoundaryIsInside = true)
        {
            if (vertices.Count != 3) throw new System.ArgumentException("Incorrect number of points in traingle");
            var p = vertexInQuestion.Position;
            var a = vertices[0].Position;
            var b = vertices[1].Position;
            var c = vertices[2].Position;
            return (SameSide(p, a, b, c) && SameSide(p, b, a, c) && SameSide(p, c, a, b));
        }
        internal static bool SameSide(double[] p1, double[] p2, double[] a, double[] b, bool onBoundaryIsInside = true)
        {
            var cp1 = b.subtract(a).crossProduct(p1.subtract(a));
            var cp2 = b.subtract(a).crossProduct(p2.subtract(a));
            var dot = cp1.dotProduct(cp2);
            if (dot.IsNegligible()) return onBoundaryIsInside;
            if (Math.Abs(dot) < 1E-10) return onBoundaryIsInside;
            return (dot > 0.0);
        }

        /// <summary>
        /// Determines if a point is inside a tesselated solid (polyhedron).
        /// And the polygon is not self-intersecting
        /// http://www.cescg.org/CESCG-2012/papers/Horvat-Ray-casting_point-in-polyhedron_test.pdf
        /// </summary>
        /// <returns></returns>
        public static bool IsVertexInsideSolid(TessellatedSolid ts, Vertex vertexInQuestion, bool onBoundaryIsInside = true)
        {
            //ToDo: Rewrite function to use plane list as in SolidIntersectionFunction
            var facesAbove = new List<PolygonalFace>();
            var facesBelow = new List<PolygonalFace>();
            var inconclusive = true;
            var rnd = new Random();
            //Added while inconclusive and random direction because there are some special cases that look the  
            //same. For instance, consider a vertex sitting at the center of a half moon. Along the z axis, 
            //It will go through 1 edge or vertex (special cases) above and one below. Then consider a box
            //centered on the origin. A point on the origin would point to an edge (of the two triangles
            //forming the face) above and one below. Therefore, it was decided that special cases (through
            //edges or vertices, will yeild inconclusive results. 
            while (inconclusive)
            {
                inconclusive = false;
                var direction = new[] { rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() }.normalize();
                foreach (var face in ts.Faces)
                {
                    if (face.Vertices.Any(vertex => vertexInQuestion.X.IsPracticallySame(vertex.X) &&
                                                    vertexInQuestion.Y.IsPracticallySame(vertex.Y) &&
                                                    vertexInQuestion.Z.IsPracticallySame(vertex.Z)))
                    {
                        return onBoundaryIsInside;
                    }

                    var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position);
                    var t = -(vertexInQuestion.Position.dotProduct(face.Normal) - distanceToOrigin) /
                            (direction.dotProduct(face.Normal));
                    //Note that if t == 0, then it is on the face
                    //else, find the intersection point and determine if it is inside the polygon (face)
                    var newVertex = t.IsNegligible() ? vertexInQuestion : new Vertex(vertexInQuestion.Position.add(direction.multiply(t)));
                    if (MiscFunctions.IsPointInsideTriangle(face, newVertex, true))
                    {
                        //If the distance between the vertex and a plane is neglible and the vertex is inside that face
                        if (t.IsNegligible())
                        {
                            return onBoundaryIsInside;
                        }
                        if (t > 0.0) //Face is higher on Z axis than vertex.
                        {
                            //Check to make sure no adjacent faces were already added to list (e.g., the projected vertex goes 
                            //through an edge).
                            var onAdjacentFace = face.AdjacentFaces.Any(adjacentFace => facesAbove.Contains(adjacentFace));
                            //Else, inconclusive (e.g., corners of cresent moon) 
                            if (!onAdjacentFace) facesAbove.Add(face);
                            else
                            {
                                inconclusive = true;
                                break;
                            }
                        }
                        else //Face is lower on Z axis than vertex.
                        {
                            //Check to make sure no adjacent faces were already added to list (e.g., the projected vertex goes 
                            //through an edge).
                            var onAdjacentFace = face.AdjacentFaces.Any(adjacentFace => facesBelow.Contains(adjacentFace));
                            if (!onAdjacentFace) facesBelow.Add(face);
                            else //Else, inconclusive (e.g., corners of cresent moon) 
                            {
                                inconclusive = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (facesAbove.Count == 0 || facesBelow.Count == 0) return false;
            return facesAbove.Count % 2 != 0 && facesBelow.Count % 2 != 0;//Even number of intercepts, means the vertex is inside
        }


        /// <summary>
        /// Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        /// And the polygon is not self-intersecting
        /// </summary>
        /// <returns></returns>
        public static bool IsPointInsidePolygon(List<Point> points, Point pointInQuestion, bool onBoundaryIsInside = true)
        {
            //If the point in question is == a point in points, then it is inside the polygon
            if (points.Any(point => point.X.IsPracticallySame(pointInQuestion.X) && point.Y.IsPracticallySame(pointInQuestion.Y)))
            {
                return true;
            }
            //Create nodes and add them to a list
            var nodes = points.Select(point => new Node(point, 0, 0)).ToList();

            //Add first line to list and update nodes with information
            var lines = new List<Line>();
            var line = new Line(nodes.Last(), nodes[0]);
            lines.Add(line);
            nodes.Last().StartLine = line;
            nodes[0].EndLine = line;
            //Create all other lines and add them to the list
            for (var i = 1; i < points.Count; i++)
            {
                line = new Line(nodes[i - 1], nodes[i]);
                lines.Add(line);
                nodes[i - 1].StartLine = line;
                nodes[i].EndLine = line;
            }

            //sort points by descending y, then descending x
            nodes.Add(new Node(pointInQuestion, 0, 0));
            var sortedNodes = nodes.OrderByDescending(node => node.Y).ThenByDescending(node => node.X).ToList<Node>();
            var lineList = new List<Line>();

            //Use red-black tree sweep to determine which lines should be tested for intersection
            foreach (var node in sortedNodes)
            {
                //Add to or remove from Red-Black Tree
                //If reached the point in question, then find intercepts on the lineList
                if (node.StartLine == null)
                {
                    if (lineList.Count % 2 != 0 || lineList.Count < 1) return false;
                    Line leftLine, rightLine;
                    bool isOnLine;
                    var numberOfLinesToLeft = TriangulatePolygon.LinesToLeft(node, lineList, out leftLine, out isOnLine);
                    //Check if the point is on the left line or right line (note that one direction search is sufficient).
                    if (isOnLine) return onBoundaryIsInside;
                    //Else, not on a boundary, so check to see that it is in between an odd number of lines to left and right
                    if (numberOfLinesToLeft % 2 == 0) return false;
                    return TriangulatePolygon.LinesToRight(node, lineList, out rightLine, out isOnLine) % 2 != 0;
                }
                if (lineList.Contains(node.StartLine))
                {
                    lineList.Remove(node.StartLine);
                }
                else
                {
                    lineList.Add(node.StartLine);
                }
                if (lineList.Contains(node.EndLine))
                {
                    lineList.Remove(node.EndLine);
                }
                else
                {
                    lineList.Add(node.EndLine);
                }
            }
            //If not returned, throw error
            throw new System.ArgumentException("Failed to return intercept information");
        }
        #endregion

        #region DEBUG: Is the convex hull correct?
        public static bool IsConvexHullCorrect(TessellatedSolid ts)
        {
            //Check if the vertices of an edge belong to the two faces it is supposed to belong to
            foreach (var edge in ts.ConvexHullEdges)
            {
                if (!edge.OwnedFace.Vertices.Contains(edge.To) || !edge.OwnedFace.Vertices.Contains(edge.From)) return false;
                if (!edge.OtherFace.Vertices.Contains(edge.To) || !edge.OtherFace.Vertices.Contains(edge.From)) return false;
                if (edge.Curvature == CurvatureType.Concave) return false;
            }
            return true;


        }
        #endregion
    }
}
