// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)



using ClipperLib;
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    // Here are the set of misellaneous functions that optimize! find the closest point, the most
    // orthogonal direction, etc.
    public static partial class MiscFunctions
    {
        #region Closest Point/Vertex
        /// <summary>
        /// Finds the closest vertex (3D Point) on a triangle (a,b,c) to the given vertex (p).
        /// It may be one of the three given points (a,b,c), a point on the edge, 
        /// or a point on the face.
        /// </summary>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnTriangleToPoint 
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <param name="p"></param>
        /// <param name="uvw"></param>
        /// <returns></returns>
        public static Vector3 ClosestVertexOnTriangleToVertex(Vector3 a, Vector3 b, Vector3 c, Vector3 p,
            out Vector3 uvw)
        {
            //UVW is the vector of the point in question (p) to the nearest point on the triangle (a,b,c), I think.
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
        /// Gets the closest vertex (3D Point) on line segment (ab) from the given point (p). 
        /// It also returns the distance to the line segment.
        /// </summary>
        /// <source> OpenVDB 4.0.2 Proximity::closestPointOnSegmentToPoint
        /// Converted on 8.31.2017 by Brandon Massoni </source>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="p"></param>
        /// <param name="distanceToSegment"></param>
        /// <returns></returns>
        public static Vector3 ClosestVertexOnSegmentToVertex(Vector3 a, Vector3 b, Vector3 p, out double distanceToSegment)
        {
            var ab = b.Subtract(a);
            distanceToSegment = p.Subtract(a).Dot(ab);

            if (distanceToSegment <= 0.0)
            {
                // c projects outside the [a,b] interval, on the a side.
                distanceToSegment = 0.0;
                return a;
            }
            else
            {

                // always nonnegative since denom = ||ab||^2
                double denom = ab.Dot(ab);

                if (distanceToSegment >= denom)
                {
                    // c projects outside the [a,b] interval, on the b side.
                    distanceToSegment = 1.0;
                    return b;
                }
                else
                {
                    // c projects inside the [a,b] interval.
                    distanceToSegment = distanceToSegment / denom;
                    return a + (ab * distanceToSegment); // a + (ab * t);
                }
            }
        }

        /// <summary>
        /// Gets the closest point on the line segment from the given point (p). 
        /// </summary>
        /// <param name="line"></param>
        /// <param name="p"></param>
        /// <returns></returns>
        public static Vector2 ClosestPointOnLineSegmentToPoint(this PolygonEdge line, Vector2 p)
        {
            //First, project the point in question onto the infinite line, getting its distance on the line from 
            //the line.FromPoint
            //There are three possible results:
            //(1) If the distance is <= 0, the infinite line intersection is outside the line segment interval, on the FromPoint side.
            //(2) If the distance is >= the line.Length, the infinite line intersection is outside the line segment interval, on the ToPoint side.
            //(3) Otherwise, the infinite line intersection is inside the line segment interval.
            var fromPoint = line.FromPoint;
            var lineVector = line.ToPoint.Coordinates - line.FromPoint.Coordinates;
            var distanceToSegment = (p - fromPoint.Coordinates).Dot(lineVector) / line.Length;

            if (distanceToSegment <= 0.0)
            {
                return fromPoint.Coordinates;
            }
            if (distanceToSegment >= line.Length)
            {
                return line.ToPoint.Coordinates;
            }
            distanceToSegment = distanceToSegment / line.Length;
            return new Vector2(fromPoint.X + lineVector.X * distanceToSegment,
                fromPoint.Y + lineVector.Y * distanceToSegment);
        }

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
            // d1*x - g*y = b1
            // -g*x - d2*y = b2
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
        /// <returns></returns>
        public static CartesianDirections SnapDirectionToCartesian(this Vector3 direction, out bool withinTolerance, double tolerance = double.NaN)
        {
            var xDot = direction[0];
            var absXDot = Math.Abs(xDot);
            var yDot = direction[1];
            var absYDot = Math.Abs(yDot);
            var zDot = direction[2];
            var absZDot = Math.Abs(zDot);

            // X-direction
            if (absXDot > absYDot && absXDot > absZDot)
            {
                withinTolerance = !double.IsNaN(tolerance) && absXDot.IsPracticallySame(1.0, tolerance);
                return xDot > 0 ? CartesianDirections.XPositive : CartesianDirections.XNegative;
            }
            // Y-direction
            if (absYDot > absXDot && absYDot > absZDot)
            {
                withinTolerance = !double.IsNaN(tolerance) && absYDot.IsPracticallySame(1.0, tolerance);
                return yDot > 0 ? CartesianDirections.YPositive : CartesianDirections.YNegative;
            }
            // Z-direction
            withinTolerance = !double.IsNaN(tolerance) && absZDot.IsPracticallySame(1.0, tolerance);
            return zDot > 0 ? CartesianDirections.ZPositive : CartesianDirections.ZNegative;
        }

        /// <summary>
        /// Gets the perpendicular direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <param name="additionalRotation">An additional counterclockwise rotation (in radians) about the direction.</param>
        /// <returns>TVGL.Vector3.</returns>
        public static Vector3 GetPerpendicularDirection(this Vector3 direction, double additionalRotation = 0)
        {
            Vector3 dir;
            //If the vector is only in the y-direction, then return the x direction
            if (direction.X.IsNegligible() && direction.Z.IsNegligible())
                return Vector3.UnitX;
            // otherwise we will return something in the x-z plane, which is created by
            // taking the cross product of the Y-direction with this vector.
            // The thinking is that - since this is used in the function above (to translate
            // to the x-y plane) - the provided direction, is the new z-direction, so
            // we find something in the x-z plane through this cross-product, so that the
            // third direction has strong component in positive y-direction - like
            // camera up position.
            dir = Vector3.UnitY.Cross(direction).Normalize();
            if (additionalRotation == 0) return dir;
            return dir.Transform(Quaternion.CreateFromAxisAngle(direction, additionalRotation));
        }
        public static Vector3 FindAxisToMinimizeProjectedArea(IEnumerable<PolygonalFace> faces, int count)
        {
            var sums = Vector3.Zero;
            foreach (var face in faces)
                sums += (face.B.Coordinates - face.A.Coordinates).Cross(face.C.Coordinates - face.A.Coordinates);
            var Amatrix = new Matrix3x3(sums.X * sums.X, sums.X * sums.Y, sums.X * sums.Z,
                sums.X * sums.Y, sums.Y * sums.Y, sums.Y * sums.Z,
                sums.X * sums.Z, sums.Y * sums.Z, sums.Z * sums.Z);
            Amatrix.EigenRealsOnly(out var eigenValues, out var eigenVectors);
            if (eigenVectors.Length == 1)
            {
                var direction = eigenVectors[0];
                var inline = Math.Abs(direction.Dot(sums) / sums.Length());
                if (inline.IsPracticallySame(1, 0.1))
                // then this is a maximum, not a minimum. basically, the eigen analysis was overwhelmed
                // and no clear second best could be found. is this a plane?!
                {
                    var maxLength = 0.0;
                    var maxCross = Vector3.Null;
                    foreach (var f in faces)
                    {
                        var crossDir = f.Normal.Cross(direction);
                        var lengthSqd = crossDir.LengthSquared();
                        if (lengthSqd > maxLength)
                        {
                            maxCross = crossDir;
                            maxLength = lengthSqd;
                        }
                    }
                    return maxCross.Normalize();
                }
                else return direction;
            }
            if (eigenVectors.Length == 2)
            {
                if (Math.Abs(eigenValues[0]) < Math.Abs(eigenValues[1]))
                    return eigenVectors[0];
                return eigenVectors[1];
            }
            if (Math.Abs(eigenValues[0]) < Math.Abs(eigenValues[1])
                && Math.Abs(eigenValues[0]) < Math.Abs(eigenValues[2]))
                return eigenVectors[0];
            if (Math.Abs(eigenValues[1]) < Math.Abs(eigenValues[0])
                && Math.Abs(eigenValues[1]) < Math.Abs(eigenValues[2]))
                return eigenVectors[1];
            return eigenVectors[2];
        }

        public static Vector3 FindAxisToMaximizeProjectedArea(IEnumerable<PolygonalFace> faces, int count)
        {
            var sums = Vector3.Zero;
            foreach (var face in faces)
                sums += (face.B.Coordinates - face.A.Coordinates).Cross(face.C.Coordinates - face.A.Coordinates);
            var Amatrix = new Matrix3x3(sums.X * sums.X, sums.X * sums.Y, sums.X * sums.Z,
                sums.X * sums.Y, sums.Y * sums.Y, sums.Y * sums.Z,
                sums.X * sums.Z, sums.Y * sums.Z, sums.Z * sums.Z);
            Amatrix.EigenRealsOnly(out _, out var eigenVectors);
            var projectedArea0 = Math.Abs(eigenVectors[0].Dot(sums));
            var projectedArea1 = eigenVectors.Length > 1 ? Math.Abs(eigenVectors[1].Dot(sums)) : double.NegativeInfinity;
            var projectedArea2 = eigenVectors.Length > 2 ? Math.Abs(eigenVectors[2].Dot(sums)) : double.NegativeInfinity;
            if (projectedArea0 > projectedArea1 && projectedArea0 > projectedArea2)
                return eigenVectors[0];
            if (projectedArea0 < projectedArea1 && projectedArea1 > projectedArea2)
                return eigenVectors[1];
            return eigenVectors[2];
        }

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
        private static Vector3 FindAverageInnerEdgeVector(IEnumerable<PolygonalFace> faces, out HashSet<Edge> borderEdges)
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

        public static void RemoveDuplicates(this List<Vector3> directions, bool reverseIsDuplicate = false, double dotTolerance = Constants.SameFaceNormalDotTolerance)
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
                    if ((reverseIsDuplicate && temp[i].IsAlignedOrReverse(temp[j], dotTolerance)) ||
                        (!reverseIsDuplicate && temp[i].IsAligned(temp[j], dotTolerance)))
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


        public static IEnumerable<Vector3> ReduceDirections(List<Vector3> readOnlyDirs, 
            IEnumerable<Vector3> guessDirs, int targetNumberOfDirections)
        {
            if (guessDirs == null) return readOnlyDirs;
            var guessDirList = guessDirs.Distinct().ToList();
            var numReadOnly = readOnlyDirs.Count;
            foreach (var dir in readOnlyDirs)
                guessDirList.Insert(0, dir);
            var sortedProximities = new SortedList<double, (Vector3, Vector3)>(new NoEqualSort(true));
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

        public static IEnumerable<Vector3> GetDodecahedronDirs(Vector3 relativeZ)
        {
            var rotateMatrix = relativeZ.TransformToXYPlane(out _);
            foreach (var d in dodecDirs)
                yield return d.Transform(rotateMatrix);
        }

        readonly static double phi_CosSin = (1 + Math.Sqrt(5)) / (5 + Math.Sqrt(5)); //0.44721359549996...
        readonly static double phi_SinSqd = (3 + Math.Sqrt(5)) / (5 + Math.Sqrt(5)); //0.72360679774997...
        readonly static double phi_Sin = (1 + Math.Sqrt(5)) / (Math.Sqrt(10 + 2 * Math.Sqrt(5))); //0.85065080835204...
        readonly static double phi_CosSqd = 2 / (5 + Math.Sqrt(5)); //0.27639320225...
        readonly static double phi_Cos = 2 / Math.Sqrt(10 + 2 * Math.Sqrt(5)); //0.5257311121191336...

        readonly static Vector3 d1 = new Vector3(0, 2 * phi_CosSin, phi_CosSin);
        readonly static Vector3 d2 = new Vector3(phi_Cos, -phi_SinSqd, phi_CosSin);
        readonly static Vector3 d3 = new Vector3(-phi_Cos, -phi_SinSqd, phi_CosSin);
        readonly static Vector3 d4 = new Vector3(phi_Sin, phi_CosSqd, phi_CosSin);
        readonly static Vector3 d5 = new Vector3(-phi_Sin, phi_CosSqd, phi_CosSin);
        readonly static Vector3[] dodecDirs = new[] { d1, d2, d3, d4, d5 };
        #endregion



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
                planeResidual = thisPlane.CalculateError(pointList);
                return bestCurve;
            }
            else
            {
                var lineDir = Vector3.Zero;
                for (int i = 1; i < pointList.Count; i++)
                    lineDir += (pointList[i] - pointList[0]);
                normal = lineDir.Normalize().GetPerpendicularDirection();
                var thisPlane = new Plane(pointList[0], normal);
                if (StraightLine2D.CreateFromPoints(pointList.Select(p => (IVertex2D)p.ConvertTo2DCoordinates(thisPlane.AsTransformToXYPlane)),
                    out var straightLine, out var error))
                {
                    plane = thisPlane;
                    planeResidual = thisPlane.CalculateError(pointList);
                    curveResidual = error;
                    return straightLine;
                }
                plane = default;
                planeResidual = double.PositiveInfinity;
                curveResidual = double.PositiveInfinity;
                return new StraightLine2D();
            }
        }


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
        /// ns the equidistant sphere points kogan.
        /// https://scholar.rose-hulman.edu/cgi/viewcontent.cgi?article=1387&context=rhumj
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector2&gt;.</returns>
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
        /// ns the equidistant sphere points kogan.
        /// https://scholar.rose-hulman.edu/cgi/viewcontent.cgi?article=1387&context=rhumj
        /// </summary>
        /// <param name="n">The n.</param>
        /// <param name="radius">The radius.</param>
        /// <returns>System.Collections.Generic.IEnumerable&lt;TVGL.Vector3&gt;.</returns>
        public static IEnumerable<Vector3> NEquidistantSpherePointsKogan(int n, double radius)
        {
            foreach (var anglePair in NEquidistantSpherePointsKogan(n))
            {
                var x = anglePair.X;
                var y = anglePair.Y;
                yield return new Vector3(Math.Cos(x) * Math.Cos(y), Math.Sin(x) * Math.Cos(y), Math.Sin(y));
            }
        }
    }
}
