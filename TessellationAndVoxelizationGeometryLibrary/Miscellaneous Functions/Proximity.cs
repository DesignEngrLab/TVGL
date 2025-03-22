// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="Proximity.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TVGL
{
    // Here are the set of misellaneous functions that optimize! find the closest point, the most
    // orthogonal direction, etc.
    /// <summary>
    /// Class MiscFunctions.
    /// </summary>
    public static partial class MiscFunctions
    {
        #region Closest Point/Vertex
        /// <summary>
        /// Finds the closest vertex (3D Point) on a triangle (a,b,c) to the given vertex (c).
        /// It may be one of the three given points (a,b,c), a point on the edge,
        /// or a point on the face.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="p">The c.</param>
        /// <param name="uvw">The uvw.</param>
        /// <returns>Vector3.</returns>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnTriangleToPoint
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        public static Vector3 ClosestVertexOnTriangleToVertex(Vector3 a, Vector3 b, Vector3 c, Vector3 p,
            out Vector3 uvw)
        {
            //UVW is the vector of the point in question (c) to the nearest point on the triangle (a,b,c), I think.
            double uvw1, uvw2;
            // degenerate triangle, singular
            if (a.Distance(b).IsNegligible() && a.Distance(c).IsNegligible())
            {
                uvw = new Vector3(1, 0, 0);
                return a;
            }

            var ab = b.Subtract(a);
            var ac = c.Subtract(a);
            var ap = p.Subtract(a);
            double d1 = ab.Dot(ap), d2 = ac.Dot(ap);

            // degenerate triangle edges
            if (a.Distance(b).IsNegligible())
            {
                var cps = ClosestVertexOnSegmentToVertex(a, c, p, out var t);
                uvw = new Vector3(1.0 - t, 0, t);
                return cps;

            }
            else if (a.Distance(c).IsNegligible() || b.Distance(c).IsNegligible())
            {
                var cps = ClosestVertexOnSegmentToVertex(a, b, p, out var t);
                uvw = new Vector3(1.0 - t, t, 0);
                return cps;
            }

            if (d1 <= 0.0 && d2 <= 0.0)
            {
                uvw = new Vector3(1, 0, 0);
                return a; // barycentric coordinates (1,0,0)
            }

            // Check if P in vertex region outside B
            var bp = p.Subtract(b);
            double d3 = ab.Dot(bp), d4 = ac.Dot(bp);
            if (d3 >= 0.0 && d4 <= d3)
            {
                uvw = new Vector3(0, 1, 0);
                return b; // barycentric coordinates (0,1,0)
            }

            // Check if P in edge region of AB, if so return projection of P onto AB
            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
            {
                uvw1 = d1 / (d1 - d3);
                uvw = new Vector3(1.0 - uvw1, uvw1, 0);
                return a + (ab * uvw1); // barycentric coordinates (1-v,v,0)
            }

            // Check if P in vertex region outside C
            var cp = p.Subtract(c);
            double d5 = ab.Dot(cp), d6 = ac.Dot(cp);
            if (d6 >= 0.0 && d5 <= d6)
            {
                uvw = new Vector3(0, 0, 1);
                return c; // barycentric coordinates (0,0,1)
            }

            // Check if P in edge region of AC, if so return projection of P onto AC
            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
            {
                uvw2 = d2 / (d2 - d6);
                uvw = new Vector3(1.0 - uvw2, 0, uvw2);
                return a + (ac * uvw2); // barycentric coordinates (1-w,0,w)
            }

            // Check if P in edge region of BC, if so return projection of P onto BC
            var va = d3 * d6 - d5 * d4;
            if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
            {
                uvw2 = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                uvw = new Vector3(0, 1.0 - uvw2, uvw2);
                return b + ((c - b) * uvw[2]); // b + uvw[2] * (c - b), barycentric coordinates (0,1-w,w)
            }

            // P inside face region. Compute Q through its barycentric coordinates (u,v,w)
            var denom = 1.0 / (va + vb + vc);
            uvw2 = vc * denom;
            uvw1 = vb * denom;
            uvw = new Vector3(1.0 - uvw1 - uvw2, uvw1, uvw2);

            return a + (ab * uvw[1]) + (ac * uvw[2]);
            //a + ab*uvw[1] + ac*uvw[2]; // = u*a + v*b + w*c , u= va*denom = 1.0-v-w
        }


        /// <summary>
        /// Gets the closest vertex (3D Point) on line segment (ab) from the given point (c).
        /// It also returns the t parameter of the line segment (a + t*(b-a)), so this is a
        /// number between 0 and 1. If the point is outside the line segment, the t value
        /// is set to 0 or 1, and the closest point is the corresponding end point.
        /// </summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <param name="c">The c.</param>
        /// <param name="tParameter">The distance to segment.</param>
        /// <returns>Vector3.</returns>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnSegmentToPoint
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        private static Vector3 ClosestVertexOnSegmentToVertex(Vector3 a, Vector3 b, Vector3 c, out double tParameter)
        {
            var ab = b.Subtract(a);
            tParameter = c.Subtract(a).Dot(ab);
            if (tParameter <= 0.0)
            {   // c projects outside the [a,b] interval, on the a side.
                tParameter = 0.0;
                return a;
            }
            // always nonnegative since denom = ||ab||^2
            double denom = ab.Dot(ab);
            if (tParameter >= denom)
            {   // c projects outside the [a,b] interval, on the b side.
                tParameter = 1.0;
                return b;
            }
            // c projects inside the [a,b] interval.
            tParameter = tParameter / denom;
            return a + (ab * tParameter); // a + (ab * t);
        }
        
        /// <summary>
        /// Gets the closest 3D Point on line segment (defined by fromPt-toPt) from the given point (vertex).
        /// It also returns the distance to the line segment.
        /// </summary>
        /// <param name="fromPt">fromPt.</param>
        /// <param name="toPoint">The toPoint.</param>
        /// <param name="vertex">The vertex.</param>
        /// <param name="distanceToSegment">The distance to segment.</param>
        /// <returns>Vector3.</returns>
        /// <source> This is based on the above method, but 
        /// rewritten for general use by MICampbell on 03.21.2025 </source>
        public static Vector3 ClosestPointOnLineSegmentToPoint(Vector3 fromPt, Vector3 toPoint, Vector3 vertex, out double distanceToSegment)
        {
            var v = toPoint - fromPt;
            var vLengthSqd = v.LengthSquared();
            //var vUnit = v / vLength;  // this would be the "costly" inverse square root, but let's put it off until we need it

            var distanceAlong = (vertex - fromPt).Dot(v); //really would need to divide by v so that unit length along vector

            if (distanceAlong <= 0.0)
            {   // point is "behind" the fromPt end of the segment, so the closest point is fromPt
                distanceToSegment = (vertex - fromPt).Length();
                return fromPt;
            }
            if (distanceAlong >= vLengthSqd)
            {   // algebraically, we think that the point is "beyond" the toPoint if the result of distanceAlong
                // is greater than the length of the segment. To avoid a square root, we multiple both sides by the
                // length of v, which makes the LHS what was found above and the RHS as simply vLengthSqd
                distanceToSegment = (vertex - toPoint).Length();
                return toPoint;
            }
            else
            {   // the point closest to the vertex is along the segment
                // this point is q = fromPt + t * v/||v||
                // note that distance along is t*||v||
                // so the second term in this RHS for q ("t * v/||v||") is the same as distanceAlong*v/vLengthSqd
                var q = fromPt + v * distanceAlong / vLengthSqd;
                distanceToSegment = (vertex - q).Length();
                return q;
            }
        }

        /// <summary>
        /// Gets the closest point on the line segment from the given point (c).
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="p">The c.</param>
        /// <returns>Vector2.</returns>
        public static Vector2 ClosestPointOnLineSegmentToPoint(this PolygonEdge line, Vector2 p)
            => ClosestPointOnLineSegmentToPoint(line.FromPoint.Coordinates, line.ToPoint.Coordinates, p);
        public static Vector2 ClosestPointOnLineSegmentToPoint(Vector2 fromPoint, Vector2 toPoint, Vector2 p)
        {
            //First, project the point in question onto the infinite line, getting its distance on the line from 
            //the line.FromPoint
            //There are three possible results:
            //(1) If the distance is <= 0, the infinite line intersection is outside the line segment interval, on the FromPoint side.
            //(2) If the distance is >= the line.Length, the infinite line intersection is outside the line segment interval, on the ToPoint side.
            //(3) Otherwise, the infinite line intersection is inside the line segment interval.
            var lineVector = toPoint - fromPoint;
            var lineLength = lineVector.Length();
            var distanceToSegment = (p - fromPoint).Dot(lineVector) / lineLength;

            if (distanceToSegment <= 0.0)
            {
                return fromPoint;
            }
            if (distanceToSegment >= lineLength)
            {
                return toPoint;
            }
            distanceToSegment = distanceToSegment / lineLength;
            return new Vector2(fromPoint.X + lineVector.X * distanceToSegment,
                fromPoint.Y + lineVector.Y * distanceToSegment);
        }

        /// <summary>
        /// Closest the point to lines.
        /// </summary>
        /// <param name="lines">The lines.</param>
        /// <returns>Vector2.</returns>
        public static Vector2 ClosestPointToLines(IEnumerable<(Vector2 anchor, Vector2 dir)> lines)
        {
            var n = 0;
            double dxdx = 0.0, dydy = 0.0, dxdy = 0.0;
            double pxdxdy = 0.0, pxdydy = 0.0, pydxdy = 0.0, pydxdx = 0.0;
            double pix = double.NaN, piy = double.NaN;
            foreach (var line in lines)
            {
                pix = line.anchor.X;
                piy = line.anchor.Y;
                var dix = line.dir.X;
                var diy = line.dir.Y;
                var dixdix = dix * dix;
                var diydiy = diy * diy;
                var dixdiy = dix * diy;
                dxdx += dixdix;
                dxdy += dixdiy;
                dydy += diydiy;
                pxdxdy += pix * dixdiy;
                pxdydy += pix * diydiy;
                pydxdy += piy * dixdiy;
                pydxdx += piy * dixdix;
                n++;
            }
            if (n == 0) return Vector2.Null;
            if (n == 1) return new Vector2(pix, piy);
            var d1 = dydy;
            var d2 = dxdx;
            var g = dxdy; //off-diagonal term
            var b1 = pxdydy - pydxdy;
            var b2 = pydxdx - pxdxdy;

            var cx = (b1 * d2 + b2 * g) / (d1 * d2 - g * g);
            var cy = (b2 + g * cx) / d2;
            return new Vector2(cx, cy);
        }
        #endregion

        #region Optimal Direction

        /// <summary>
        /// Snap Direction to Closest Cartesian Direction.
        /// </summary>
        /// <param name="direction">The direction to convert.</param>
        /// <param name="withinTolerance">To check if direction is within the optionally provided tolerance.</param>
        /// <param name="tolerance">The optionally provided tolerance for the previous boolean (does not effect the determined direction).</param>
        /// <returns>CartesianDirections.</returns>
        public static CartesianDirections SnapDirectionToCartesian(this Vector3 direction, out bool withinTolerance, double tolerance = double.NaN)
        {
            var xValue = direction[0];
            var absXValue = Math.Abs(xValue);
            var yValue = direction[1];
            var absYValue = Math.Abs(yValue);
            var zValue = direction[2];
            var absZValue = Math.Abs(zValue);

            // X-direction
            if (absXValue > absYValue && absXValue > absZValue)
            {
                withinTolerance = !double.IsNaN(tolerance) && absXValue.IsPracticallySame(1.0, tolerance);
                return xValue > 0 ? CartesianDirections.XPositive : CartesianDirections.XNegative;
            }
            // Y-direction
            if (absYValue > absXValue && absYValue > absZValue)
            {
                withinTolerance = !double.IsNaN(tolerance) && absYValue.IsPracticallySame(1.0, tolerance);
                return yValue > 0 ? CartesianDirections.YPositive : CartesianDirections.YNegative;
            }
            // Z-direction
            withinTolerance = !double.IsNaN(tolerance) && absZValue.IsPracticallySame(1.0, tolerance);
            return zValue > 0 ? CartesianDirections.ZPositive : CartesianDirections.ZNegative;
        }

        /// <summary>
        /// Gets the orthogonal/perpendicular direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="additionalRotation">An additional counterclockwise rotation (in radians) about the direction.</param>
        /// <returns>TVGL.Vector3.</returns>
        public static Vector3 GetOrthogonalDirection(this Vector3 direction, double additionalRotation = 0)
        => direction.GetPerpendicularDirection(additionalRotation);

        /// <summary>
        /// Gets the orthogonal/perpendicular direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="additionalRotation">An additional counterclockwise rotation (in radians) about the direction.</param>
        /// <returns>TVGL.Vector3.</returns>
        public static Vector3 GetPerpendicularDirection(this Vector3 direction, double additionalRotation = 0)
        {
            Vector3 dir;
            //If the vector is only in the y-direction, then return the x direction
            if (direction.Y.IsPracticallySame(1.0) || direction.Y.IsPracticallySame(-1.0))
            {
                if (additionalRotation == 0) return Vector3.UnitX;
                return Math.Cos(additionalRotation) * Vector3.UnitX - Math.Sin(additionalRotation) * Vector3.UnitZ;
            }
            // otherwise we will return something in the x-z plane, which is created by
            // taking the cross product of the Y-direction with this vector.
            // The thinking is that - since this is used in the function above (to translate
            // to the x-y plane) - the provided direction, is the new z-direction, so
            // we find something in the x-z plane through this cross-product, so that the
            // third direction has strong component in positive y-direction - like
            // camera up position.
            dir = Vector3.UnitY.Cross(direction).Normalize();
            if (additionalRotation == 0) return dir;
            var yDir = direction.Cross(dir).Normalize();
            return Math.Cos(additionalRotation) * dir + Math.Sin(additionalRotation) * yDir;
        }
        /// <summary>
        /// Finds the axis that is most orthogonal to the given set.
        /// </summary>
        /// <param name="vectors">The vectors.</param>
        /// <param name="count">The count.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 FindMostOrthogonalVector(IEnumerable<Vector3> vectors)
        {
            Plane.DefineNormalAndDistanceFromVertices(vectors, out _, out var normal);
            return normal;
        }

        /// <summary>
        /// Finds the axis from normals.
        /// </summary>
        /// <param name="normals">The normals.</param>
        /// <param name="count">The count.</param>
        /// <returns>Vector3.</returns>
        public static Vector3 FindAxisFromNormals(IEnumerable<Vector3> normals, int count)
        {
            var r = new Random(0);
            if (count > 10)
            {
                var normalsRandomized = normals.OrderBy(_ => r.NextDouble()).ToList();
                var sumVector = normalsRandomized[count - 1].Cross(normalsRandomized[0]);
                for (int i = 1; i < normalsRandomized.Count; i++)
                {
                    var crossVector = normalsRandomized[i - 1].Cross(normalsRandomized[i]);
                    if (crossVector.Dot(sumVector) >= 0) sumVector += crossVector;
                    else sumVector -= crossVector;
                }
                return sumVector.Normalize();
            }
            else
            {
                var normalList = normals as IList<Vector3> ?? normals.ToList();
                var sumVector = Vector3.Zero;
                for (int i = 0; i < normalList.Count - 1; i++)
                    for (int j = i + 1; j < normalList.Count; j++)
                    {
                        var crossVector = normalList[i].Cross(normalList[j]);
                        if (crossVector.Dot(sumVector) >= 0) sumVector += crossVector;
                        else sumVector -= crossVector;
                    }
                return sumVector.Normalize();
            }
        }

        //Gets the average inner edge direction. Add all together and then normalize
        //This won't be accurate, but it can find a good starting direction that the other methods miss.
        //Could be used on a cylinder and cone (though cone won't be good unless it is closed).
        /// <summary>
        /// Finds the average inner edge vector.
        /// </summary>
        /// <param name="faces">The vectors.</param>
        /// <param name="borderEdges">The border edges.</param>
        /// <returns>Vector3.</returns>
        private static Vector3 FindAverageInnerEdgeVector(IEnumerable<TriangleFace> faces, out HashSet<Edge> borderEdges)
        {
            borderEdges = new HashSet<Edge>();
            Vector3 innerEdgeVector = default;
            foreach (var face in faces)
            {
                foreach (var edge in face.Edges)
                {
                    if (borderEdges.Contains(edge))
                    {
                        //Add aligned or subtract reverse
                        var dot = innerEdgeVector.Dot(edge.UnitVector);
                        innerEdgeVector += edge.Vector * (dot >= 0 ? 1 : -1);
                        borderEdges.Remove(edge);
                    }
                    else
                    {
                        borderEdges.Add(edge);
                    }
                }
            }
            return innerEdgeVector.Normalize();
        }

        /// <summary>
        /// Removes the duplicates.
        /// </summary>
        /// <param name="directions">The directions.</param>
        /// <param name="reverseIsDuplicate">if set to <c>true</c> [reverse is duplicate].</param>
        /// <param name="dotTolerance">The dot tolerance.</param>
        public static void RemoveDuplicates(this List<Vector3> directions, bool reverseIsDuplicate = false)
        {
            if (!directions.Any()) return;

            //Set a tempory list, then clear directions and rebuild it only with unique directions
            var temp = new List<Vector3>(directions);
            directions.Clear();
            for (var i = 0; i < temp.Count - 1; i++)
            {
                //Offset j so that the other direction is always later in the list.
                var unique = true;
                for (var j = i + 1; j < temp.Count; j++)
                {
                    if ((reverseIsDuplicate && temp[i].IsAlignedOrReverse(temp[j])) ||
                        (!reverseIsDuplicate && temp[i].IsAligned(temp[j])))
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique) directions.Add(temp[i]);
            }

            //Add the last item, since it was skipped by index i and is gauranteed to be unique given the order of the loops above.
            directions.Add(temp.Last());
        }


        /// <summary>
        /// Reduces the directions.
        /// </summary>
        /// <param name="readOnlyDirs">The read only dirs.</param>
        /// <param name="guessDirs">The guess dirs.</param>
        /// <param name="targetNumberOfDirections">The target number of directions.</param>
        /// <returns>IEnumerable&lt;Vector3&gt;.</returns>
        public static IEnumerable<Vector3> ReduceDirections(List<Vector3> readOnlyDirs,
            IEnumerable<Vector3> guessDirs, int targetNumberOfDirections)
        {
            if (guessDirs == null) return readOnlyDirs;
            var guessDirList = guessDirs.Distinct().ToList();
            var numReadOnly = readOnlyDirs.Count;
            foreach (var dir in readOnlyDirs)
                guessDirList.Insert(0, dir);
            var sortedProximities = new SortedList<double, (Vector3, Vector3)>(new NoEqualSort());
            var totalProximities = new Dictionary<Vector3, double>();
            for (int i = guessDirList.Count - 1; i >= numReadOnly; i--)
            {
                for (int j = i - 1; j >= 0; j--)
                {
                    var gI = guessDirList[i];
                    var gJ = guessDirList[j];
                    var dot = Math.Abs(gI.Dot(gJ));
                    if (dot > Constants.Cos15)
                    {
                        guessDirList.RemoveAt(i);
                        totalProximities.Remove(gI);
                        break;
                    }
                    else
                    {
                        sortedProximities.Add(dot, (gI, gJ));
                        if (totalProximities.TryGetValue(gI, out var dist))
                            totalProximities[gI] = dist + dot;
                        else totalProximities.Add(gI, dot);
                        if (j >= numReadOnly)
                        {
                            if (totalProximities.TryGetValue(gJ, out dist))
                                totalProximities[gJ] = dist + dot;
                            else totalProximities.Add(gJ, dot);
                        }
                    }
                }
            }
            //if (!sortedProximities.Any()) yield break;
            var sortedIndex = sortedProximities.Count;
            while (guessDirList.Count > targetNumberOfDirections)
            {
                sortedIndex--;
                if (!sortedProximities.ContainsKey(sortedIndex))
                    return guessDirList;
                var dot = sortedProximities.Keys[sortedIndex];
                var value = sortedProximities.Values[sortedIndex];
                var iDirection = value.Item1;
                var iDirectionIsReadOnly = readOnlyDirs.Contains(iDirection);
                var iDirMayBeRemoved = false;
                var jDirection = value.Item2;
                var jDirectionIsReadOnly = readOnlyDirs.Contains(jDirection);
                var jDirMayBeRemoved = false;

                if (totalProximities.TryGetValue(iDirection, out var totalProximityI) || iDirectionIsReadOnly)
                {
                    if (!iDirectionIsReadOnly)
                    {
                        totalProximityI -= dot;
                        totalProximities[iDirection] = totalProximityI;
                        iDirMayBeRemoved = true;
                    }
                }
                if (totalProximities.TryGetValue(jDirection, out var totalProximityJ) || jDirectionIsReadOnly)
                {
                    if (!jDirectionIsReadOnly)
                    {
                        totalProximityJ -= dot;
                        totalProximities[jDirection] = totalProximityJ;
                        jDirMayBeRemoved = true;
                    }
                }
                if ((iDirMayBeRemoved && jDirectionIsReadOnly) ||
                    (iDirMayBeRemoved && jDirMayBeRemoved && totalProximityI > totalProximityJ))
                {
                    totalProximities.Remove(iDirection);
                    guessDirList.Remove(iDirection);
                }
                else if ((iDirMayBeRemoved && jDirectionIsReadOnly) ||
                (iDirMayBeRemoved && jDirMayBeRemoved && totalProximityJ > totalProximityI))
                {
                    totalProximities.Remove(jDirection);
                    guessDirList.Remove(jDirection);
                }
            }
            return guessDirList;
            //foreach (var dir in guessDirList) yield return dir;
        }
        #endregion

        /// <summary>
        /// Finds the best planar curve.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="planeResidual">The plane residual.</param>
        /// <param name="curveResidual">The curve residual.</param>
        /// <returns>ICurve.</returns>
        public static ICurve FindBestPlanarCurve(this IEnumerable<Vector3> points, out Plane plane, out double planeResidual,
            out double curveResidual)
        {
            var pointList = points as IList<Vector3> ?? points.ToList();
            if (Plane.DefineNormalAndDistanceFromVertices(pointList, out var distanceToPlane, out var normal))
            {
                var thisPlane = new Plane(distanceToPlane, normal);
                var minResidual = double.PositiveInfinity;
                ICurve bestCurve = null;
                var point2D = pointList.Select(p => p.ConvertTo2DCoordinates(thisPlane.AsTransformToXYPlane));
                foreach (var curveType in MiscFunctions.TypesImplementingICurve())
                {
                    var arguments = new object[] { point2D, null, null };
                    if ((bool)curveType.GetMethod("CreateFromPoints").Invoke(null, arguments))
                    {
                        curveResidual = (double)arguments[2];
                        if (minResidual > curveResidual)
                        {
                            minResidual = curveResidual;
                            bestCurve = (ICurve)arguments[1];
                        }
                    }
                }
                curveResidual = minResidual;
                plane = thisPlane;
                planeResidual = thisPlane.CalculateMeanSquareError(pointList);
                return bestCurve;
            }
            else
            {
                var lineDir = Vector3.Zero;
                for (int i = 1; i < pointList.Count; i++)
                    lineDir += (pointList[i] - pointList[0]);
                normal = lineDir.Normalize().GetPerpendicularDirection();
                var thisPlane = new Plane(pointList[0], normal);
                if (StraightLine2D.CreateFromPoints(pointList.Select(p => (IVector2D)p.ConvertTo2DCoordinates(thisPlane.AsTransformToXYPlane)),
                    out var straightLine, out var error))
                {
                    plane = thisPlane;
                    planeResidual = thisPlane.CalculateMeanSquareError(pointList);
                    curveResidual = error;
                    return straightLine;
                }
                plane = default;
                planeResidual = double.PositiveInfinity;
                curveResidual = double.PositiveInfinity;
                return new StraightLine2D();
            }
        }


        /// <summary>
        /// </summary>
        /// <param name="v1">The v1.</param>
        /// <param name="v2">The v2.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="chordError">The chord error.</param>
        /// <param name="v1Length">Length of the v1 (save a small amount of time if already known).</param>
        /// <param name="v2Length">Length of the v2 (save a small amount of time if already known).</param>
        /// <returns><c>true</c> if the specified v1 is discontinuous; otherwise, <c>false</c>.</returns>
        public static bool LineSegmentsAreC1Discontinuous(double dot, double crossLength, double v1Length, double v2Length, double chordError)
        {
            if (dot < 0)
                // if the dot is negative then angle is greater than 90-degrees also breaks C1 continuity
                return true;
            // ***now the rest of this function is some condensed math.***
            // what we want to check is that - if the three points were to define a curved feature
            // (circle, parabola, bezier etc) - the straight line segments would maximally be
            // separated from the curve near the center. like the chord of a circle. Draw two
            // vectors and the circle that connects all three. We can find the max distance of 
            // the chord to the true circle as r*(1-cos(2*phi)). where r is the circle radius and
            // phi is the half-angle of the arc that the chord creates. For the two vectors, v1 and v2
            // we need their corresponding phi's and we define the error from whichever is larger.
            // So, we have 3 unknowns: r, phi1, and phi2. Also, (shown next) theta happens to be the
            // deviation of v2 from v1's path and is easily found from the cross product (or dot product
            // equation) - this is the next 3 lines.
            var sinTheta = crossLength / (v1Length * v2Length);
            if (sinTheta > 1 || sinTheta < -1) return true;
            var theta = Math.Asin(sinTheta);

            //if (theta > Constants.MinSmoothAngle)
            // if theta is bigger than MinSmoothAngle, then it breaks C1 continuity
            //    return true;
            // with the equation of the chord-length and the fact that the phi's (chord arc angle) add
            // up to the theta (corner angle from vectors), we can do a little manipulation to find phi
            // without finding r and the center. This is presumably more accurate and faster than finding
            // the center of the circle made by the three points. To derive this arctan equation, you'll
            // need to review your trignometric identities (specifically the sin(a-b)).
            var phi2 = Math.Atan(crossLength / (v1Length * v1Length + dot));
            var phi1 = theta - phi2;
            double error, radius;
            if (phi1 < phi2)
            {
                radius = Math.Abs(v2Length / (2 * Math.Sin(phi2)));
                // radius does not need to be in the condition, but for improved accuracy we solve it
                // based on the larger phi. 
                error = radius * (1 - Math.Cos(phi2));
            }
            else
            {
                radius = Math.Abs(v1Length / (2 * Math.Sin(phi1)));
                error = radius * (1 - Math.Cos(phi1));
            }
            return error > chordError;
        }





        /// <summary>
        /// Chooses the tightest left turn.
        /// </summary>
        /// <param name="nextVertices">The next vertices.</param>
        /// <param name="current">The current.</param>
        /// <param name="previous">The previous.</param>
        /// <returns>Vertex2D.</returns>
        internal static Vertex2D ChooseTightestLeftTurn(List<Vertex2D> nextVertices, Vertex2D current, Vertex2D previous)
        {
            var lastVector = previous.Coordinates - current.Coordinates;
            var minAngle = double.PositiveInfinity;
            Vertex2D bestVertex = null;
            foreach (var vertex in nextVertices)
            {
                if (vertex == current || vertex == previous) continue;
                var currentVector = vertex.Coordinates - current.Coordinates;
                var angle = currentVector.AngleCWBetweenVectorAAndDatum(lastVector);
                if (minAngle > angle && !angle.IsNegligible())
                {
                    minAngle = angle;
                    bestVertex = vertex;
                }
            }
            return bestVertex;
        }

        /// <summary>
        /// Chooses the tightest left turn.
        /// </summary>
        /// <param name="nextVertices">The next vertices.</param>
        /// <param name="current">The current.</param>
        /// <param name="previous">The previous.</param>
        /// <returns>Vertex2D.</returns>
        internal static Vertex2D ChooseTightestLeftTurn(this IEnumerable<Vertex2D> nextVertices, Vertex2D current, Vertex2D previous)
        {
            var lastVector = previous.Coordinates - current.Coordinates;
            var minAngle = double.PositiveInfinity;
            Vertex2D bestVertex = null;
            foreach (var vertex in nextVertices)
            {
                if (vertex == current || vertex == previous) continue;
                var currentVector = vertex.Coordinates - current.Coordinates;
                var angle = currentVector.AngleCWBetweenVectorAAndDatum(lastVector);
                if (minAngle > angle)
                {
                    minAngle = angle;
                    bestVertex = vertex;
                }
            }
            return bestVertex;
        }

        /// <summary>
        /// Chooses the highest cosine similarity.
        /// </summary>
        /// <param name="possibleNextEdges">The possible next edges.</param>
        /// <param name="refEdge">The reference edge.</param>
        /// <param name="refEdgeDir">if set to <c>true</c> [reference edge dir].</param>
        /// <param name="edgeDirections">The edge directions.</param>
        /// <param name="minAcceptable">The minimum acceptable.</param>
        /// <returns>Edge.</returns>
        internal static Edge ChooseHighestCosineSimilarity(this IEnumerable<Edge> possibleNextEdges, Edge refEdge, bool refEdgeDir,
            IEnumerable<bool> edgeDirections = null, double minAcceptable = -1.0)
        {
            var maxCos = minAcceptable;
            Edge bestEdge = null;
            var refVector = refEdge.UnitVector;
            if (!refEdgeDir) refVector *= -1;
            var directionEnumerator = edgeDirections == null ? null : edgeDirections.GetEnumerator();
            foreach (var edge in possibleNextEdges)
            {
                var currentVector = edge.UnitVector;
                if (directionEnumerator != null && directionEnumerator.MoveNext() && !directionEnumerator.Current)
                    currentVector *= -1;
                var cos = refVector.Dot(currentVector);
                if (maxCos < cos)
                {
                    maxCos = cos;
                    bestEdge = edge;
                }
            }
            return bestEdge;
        }

        /// <summary>
        /// Generates n equidistant points on a sphere. Following the approach by
        /// https://scholar.rose-hulman.edu/rhumj/vol18/iss2/5 this may be slightly better than
        /// Fibonacci points.
        /// </summary>
        /// <param name="n">The number of points, n.</param>
        /// <returns>IEnumerable&lt;Vector2&gt;.</returns>
        public static IEnumerable<Vector2> NEquidistantSpherePointsKogan(int n)
        {
            var x = 0.1 + 1.2 * n;
            var nMinus1 = n - 1.0;
            var start = -1 + 1.0 / nMinus1;
            var increment = (2.0 - 2.0 / nMinus1) / nMinus1;
            for (int j = 0; j < n; j++)
            {
                var s = start + j * increment;
                yield return
                    new Vector2(s * x, Constants.HalfPi * Math.Sign(s) * (1 - Math.Sqrt(1 - Math.Abs(s))));
            }
        }
        /// <summary>
        /// Generates n equidistant points on a sphere. Following the approach by
        /// https://scholar.rose-hulman.edu/rhumj/vol18/iss2/5 this may be slightly better than
        /// Fibonacci points.
        /// </summary>
        /// <param name="n">The number of points, n.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>IEnumerable&lt;Vector3&gt;.</returns>
        /// <font color="red">Badly formed XML comment.</font>
        public static IEnumerable<Vector3> NEquidistantSpherePointsKogan(int n, double radius)
        {
            foreach (var anglePair in NEquidistantSpherePointsKogan(n))
            {
                var x = anglePair.X;
                var y = anglePair.Y;
                yield return new Vector3(radius * Math.Cos(x) * Math.Cos(y), radius * Math.Sin(x) * Math.Cos(y), radius * Math.Sin(y));
            }
        }


        /// <summary>
        /// Returns lines (an anchor and a direction) that are the best guesses for the axes of rotation for the part.
        /// It is possible that it will return as many lines as primitives, so it is up to the calling to decide how
        /// many to consider. They are ordered in descending order of the number of primitives, then by the total area
        /// of the primitives that agree with the line.
        /// </summary>
        /// <param name="solid"></param>
        /// <param name="distanceTolerance">The minimum straightline distance between unique anchors. Note anchors will all
        /// be on a plane cutting through the origin.</param>
        /// <param name="angleDegreesTolerance">The minimum angle between line directions - usually about 4 degrees.</param>
        /// <param name="maxDistanceFromCOM"></param>
        /// <returns>A tuple of the line anchor, the line direction, and the primitives centered about that line</returns>
        public static IEnumerable<(Vector3 anchor, Vector3 direction, List<PrimitiveSurface> surfaces, double area)> FindBestRotations(TessellatedSolid solid,
            double distanceTolerance = double.NaN, double angleDegreesTolerance = 4.0)
        {
            if (double.IsNaN(distanceTolerance)) distanceTolerance =
                    0.0001 * (solid.Bounds[1] - solid.Bounds[0]).Length();
            var uniqueLines = new Unique3DLineHashLikeCollection(true, distanceTolerance, angleDegreesTolerance);
            var dirToPrimsDictionary = new Dictionary<Vector4, List<PrimitiveSurface>>();
            var com = solid.Center;
            var otherPrimitives = new List<PrimitiveSurface>(); //these shouldn't define a rotation axis, but they should contribute to one
                                                                //if they are coincident with one. So, we will process these later.
            var dotTolerance = Math.Cos(angleDegreesTolerance * Constants.DegreesToRadiansFactor);
            foreach (var prim in solid.Primitives.OrderByDescending(p => p.Area))
            {
                if (prim is Plane || prim is Sphere || prim is Prismatic || prim is GeneralQuadric)
                    otherPrimitives.Add(prim);
                if (prim is Cylinder || prim is Cone || prim is Torus || prim is Capsule)
                {
                    var anchor = prim.GetAnchor();
                    var axis = prim.GetAxis();
                    var uniqueLine = Unique3DLine(anchor, axis);

                    if (uniqueLines.TryGet(uniqueLine, out var matchingDir))
                        dirToPrimsDictionary[matchingDir].Add(prim);
                    else
                    {
                        uniqueLines.Add(matchingDir, out _);
                        dirToPrimsDictionary.Add(matchingDir, new() { prim });
                    }
                }
            }
            // now attribute any other primitives to one of the lines if its close
            foreach (var prim in otherPrimitives)
            {
                var anchor = prim.GetAnchor();
                var axis = prim.GetAxis();
                if (anchor.IsNull() && axis.IsNull()) // if both are null, then it is not descriptive enough to add to any line
                    continue;

                // go through each of the unique lines and see if this primitive is close to it
                foreach (var uniqueLine in uniqueLines)
                {
                    (var lineAnchor, var lineDirection) = Get3DLineValuesFromUnique(uniqueLine);
                    if (!anchor.IsNull()  // if it has a center, but the center is off of the line, then skip
                        && (anchor - lineAnchor).Cross(lineDirection).Length() > distanceTolerance)
                        continue;
                    // if it has an axis, but the axis is not aligned with the line, then skip
                    if (!axis.IsNull() && !axis.IsAlignedOrReverse(lineDirection, dotTolerance))
                        continue;
                    // otherwise, you've found a match so add and then break to the next primitive
                    dirToPrimsDictionary[uniqueLine].Add(prim);
                    break;
                }
            }
            //Use area to choose the optimal direction rather than number of surfaces.
            //This is a better indication most of the time.
            var scoringList = dirToPrimsDictionary.Select(kvp => (kvp.Key, kvp.Value.Sum(p => p.Area))).ToList();
            foreach ((Vector4 lineDescriptor, double area) in scoringList.OrderByDescending(s => s.Item2))
            {
                var (anchor, direction) = MiscFunctions.Get3DLineValuesFromUnique(lineDescriptor);
                yield return (anchor, direction, dirToPrimsDictionary[lineDescriptor], area);
            }
        }
    }
}
