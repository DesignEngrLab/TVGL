// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="ConvexHull3D.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public static partial class ConvexHullAlgorithm
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public static bool Create(this TessellatedSolid ts, out ConvexHull3D<Vertex> convexHull)
            => Create(ts.Vertices, ts.SameTolerance, out convexHull);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// Optionally can choose to create faces and edges. Cannot make edges without faces.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="createFaces">if set to <c>true</c> [create faces].</param>
        /// <param name="createEdges">if set to <c>true</c> [create edges].</param>
        public static bool Create(this IList<Vertex> vertices, double tolerance, out ConvexHull3D<Vertex> convexHull, bool isMaximal = false)
        {
            bool success = false;
            List<int> vertexIndices;
            ConvexHull3D<Vector3> convexHullPoints;
            if (isMaximal)
                success = CreateMaximal(vertices.Select(v => v.Coordinates).ToList(), tolerance, out convexHullPoints, out vertexIndices);
            else success = Create(vertices.Select(v => v.Coordinates).ToList(), tolerance, out convexHullPoints, out vertexIndices);
            if (!success)
            {
                convexHull = null;
                return false;
            }
            convexHull = new ConvexHull3D<Vertex> { tolerance = tolerance };
            convexHull.Vertices.AddRange(vertexIndices.Select(ind => vertices[ind]));
            convexHull.cHFaces.AddRange(convexHullPoints.cHFaces.Select(chp => new CHFace
            {
                BorderVertices = chp.BorderVertices.ToList(),
                InteriorVertices = chp.InteriorVertices.ToList(),
                D = chp.D,
                Normal = chp.Normal,
            }));
            foreach (var vert in convexHull.Vertices)
                vert.PartOfConvexHull = true;
            return true;
        }
        public static bool CreateMaximal(this IList<Vector3> vertices, double tolerance, out ConvexHull3D<Vector3> convexHull,
            out List<int> vertexIndices)
        {
            var success = Create(vertices, tolerance, out convexHull, out vertexIndices);
            if (!success) return false;
            //
            //TODO: Add the rest of the vertices
            //
            return true;
        }
        public static bool Create(this IList<Vector3> vertices, double tolerance, out ConvexHull3D<Vector3> convexHull,
            out List<int> vertexIndices)
        {
            var n = vertices.Count;
            if (SolveAs2D(vertices, tolerance, out convexHull, out vertexIndices)) return true;
            var points = new (Vector3, int)[n];

            for (int i = 0; i < n; i++)
                points[i] = (vertices[i], i);

            /* The first step is to quickly identify the three to eight vertices based on the
             * Akl-Toussaint heuristic. */
            List<(Vector3 point, int index)> extremePoints = GetExtremaOnAABB(n, points, out var numExtrema);

            if (numExtrema < 4)
            {
                SolveAs2D(vertices, tolerance, out convexHull, out vertexIndices);
                return true;
            }
            convexHull = BuildInitialSimplex(tolerance, out vertexIndices, extremePoints, numExtrema);

            var indicesUsed = vertexIndices.OrderBy(x => x).ToList();

            var indexOfUsedIndices = 0;
            var nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
            for (int i = 0; i < n; i++)
            {
                if (indexOfUsedIndices < indicesUsed.Count && i == nextUsedIndex)
                    //in order to avoid a contains function call, we know to only check with next usedIndex in order
                    nextUsedIndex = indicesUsed[indexOfUsedIndices++]; //Note: it increments after getting the current index
                else
                {
                    var point = points[i];
                    var maxDot = 0.0;
                    CHFace maxCHFace = null;
                    foreach (var face in convexHull.cHFaces)
                    {
                        var dot = (point.Item1 - face.Anchor).Dot(face.Normal);
                        if (dot > maxDot)
                        {
                            maxDot = dot;
                            maxCHFace = face;
                        }
                    }
                    if (maxCHFace != null)
                        maxCHFace.SortedNew.Add(maxDot, point.Item2);
                }
            }
            return true;
        }

        private static bool SolveAs2D(IList<Vector3> vertices, double tolerance, out ConvexHull3D<Vector3> convexHull,
            out List<int> vertexIndices)
        {
            Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
            var plane = new Plane(distance, planeNormal);
            if (plane.CalculateMaxError(vertices) > Constants.DefaultPlaneDistanceTolerance)
            {
                convexHull = null;
                vertexIndices = null;
                return false;
            }
            var coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out var backTransform).ToList();
            if (coords2D.Area() < 0)
            {
                planeNormal *= -1;
                distance *= -1;
                coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out backTransform).ToList();
            }
            var cvxHull2D = coords2D.CreateConvexHull(out vertexIndices);
            var cvxHull3DPoints = cvxHull2D.ConvertTo3DLocations(backTransform).ToList();
            convexHull = new ConvexHull3D<Vector3> { tolerance = tolerance };
            convexHull.Vertices.AddRange(vertexIndices.Select(ind => vertices[ind]));
            convexHull.cHFaces.Add(new CHFace
            {
                BorderVertices = Enumerable.Range(0, vertexIndices.Count).ToList(), //yes, this is 0 to n because
                                                                                    // they indicate - not the original vertices - but the vertices in the convex hull
                InteriorVertices = Enumerable.Range(vertexIndices.Count, vertices.Count).ToList(),
                D = distance,
                Normal = planeNormal,
            });
            return true;
        }


        private static ConvexHull3D<Vector3> BuildInitialSimplex(double tolerance, out List<int> vertexIndices, List<(Vector3 point, int index)> extremePoints, int numExtrema)
        {
            var maxVol = 0.0;
            int maxI1 = -1, maxI2 = -1, maxI3 = -1, maxI4 = -1;
            var invertBest = false;
            for (int i1 = 0; i1 < numExtrema - 3; i1++)
            {
                var basePoint = extremePoints[i1].point;
                for (int i2 = i1 + 1; i2 < numExtrema - 2; i2++)
                {
                    for (int i3 = i2 + 1; i3 < numExtrema - 1; i3++)
                    {
                        var baseTriangleArea = (extremePoints[i2].point - basePoint).Cross(extremePoints[i3].point - basePoint);
                        for (int i4 = i3 + 1; i4 < numExtrema; i4++)
                        {
                            var projectedHeight = extremePoints[i4].point - basePoint;
                            var volume = projectedHeight.Dot(baseTriangleArea);
                            if (Math.Abs(volume) > maxVol)
                            {
                                maxVol = Math.Abs(volume);
                                invertBest = volume < 0;
                                maxI1 = i1; maxI2 = i2; maxI3 = i3; maxI4 = i4;
                            }
                        }
                    }
                }
            }
            var convexHull = new ConvexHull3D<Vector3> { tolerance = tolerance };
            convexHull.Vertices.Add(extremePoints[maxI1].point);
            vertexIndices = new List<int> { extremePoints[maxI1].index };
            if (invertBest)
            {  // based on the cross product, the order of the points is wrong
                convexHull.Vertices.Add(extremePoints[maxI3].point);
                vertexIndices.Add(extremePoints[maxI3].index);
                convexHull.Vertices.Add(extremePoints[maxI2].point);
                vertexIndices.Add(extremePoints[maxI2].index);
            }
            else
            {
                convexHull.Vertices.Add(extremePoints[maxI2].point);
                vertexIndices.Add(extremePoints[maxI2].index);
                convexHull.Vertices.Add(extremePoints[maxI3].point);
                vertexIndices.Add(extremePoints[maxI3].index);
            }
            convexHull.Vertices.Add(extremePoints[maxI4].point);
            vertexIndices.Add(extremePoints[maxI4].index);
            convexHull.cHFaces.Add(convexHull.MakeCHFace(0, 1, 2));
            convexHull.cHFaces.Add(convexHull.MakeCHFace(0, 1, 3));
            convexHull.cHFaces.Add(convexHull.MakeCHFace(1, 2, 3));
            convexHull.cHFaces.Add(convexHull.MakeCHFace(2, 0, 3));
            return convexHull;
        }

        private static List<(Vector3 point, int index)> GetExtremaOnAABB(int n, (Vector3, int)[] points, out int numExtrema)
        {
            var extremePoints = new List<(Vector3 point, int index)>(6);
            for (int i = 0; i < n; i += 2)
            {
                if (points[i].Item1.X < extremePoints[0].Item1.X ||
                    points[i].Item1.X == extremePoints[0].Item1.X && points[i].Item1.Y < extremePoints[0].Item1.Y)
                    extremePoints[0] = points[i];
                if (points[i].Item1.X > extremePoints[1].Item1.X ||
                    points[i].Item1.X == extremePoints[1].Item1.X && points[i].Item1.Z > extremePoints[1].Item1.Z)
                    extremePoints[1] = points[i];
                if (points[i].Item1.Y < extremePoints[2].Item1.Y ||
                    points[i].Item1.Y == extremePoints[2].Item1.Y && points[i].Item1.Z < extremePoints[2].Item1.Z)
                    extremePoints[2] = points[i];
                if (points[i].Item1.Y > extremePoints[3].Item1.Y ||
                    points[i].Item1.Y == extremePoints[3].Item1.Y && points[i].Item1.X > extremePoints[3].Item1.X)
                    extremePoints[3] = points[i];
                if (points[i].Item1.Z < extremePoints[4].Item1.Z ||
                    points[i].Item1.Z == extremePoints[4].Item1.Z && points[i].Item1.X < extremePoints[4].Item1.X)
                    extremePoints[4] = points[i];
                if (points[i].Item1.Z > extremePoints[5].Item1.Z ||
                    points[i].Item1.Z == extremePoints[5].Item1.Z && points[i].Item1.Y > extremePoints[5].Item1.Y)
                    extremePoints[5] = points[i];
            }
            var j = 0;
            numExtrema = 6;
            for (int i = numExtrema - 1; i >= 0; i--)
            {
                var thisExtreme = extremePoints[i];
                var nextExtreme = extremePoints[j];
                if (thisExtreme.index == nextExtreme.index)
                {
                    numExtrema--;
                    extremePoints.RemoveAt(i);
                }
                else j = i;
            }
            return extremePoints;
        }
    }
}