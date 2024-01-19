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
using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    public partial class ConvexHull3D : Solid
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// Optionally can choose to create faces and edges. Cannot make edges without faces.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <param name="createFaces">if set to <c>true</c> [create faces].</param>
        /// <param name="createEdges">if set to <c>true</c> [create edges].</param>
        public static bool Create(IList<Vector3> points, out ConvexHull3D convexHull,
            out List<int> vertexIndices, double tolerance = double.NaN)
        {
            bool success = false;
            var n = points.Count;
            var vertices = new Vertex[n];
            for (int i = 0; i < n; i++)
                vertices[i] = new Vertex(points[i], i);

            success = Create(vertices, out convexHull, true, tolerance);
            if (success)
            {
                vertexIndices = vertices.Select(v => v.IndexInList).ToList();
                return true;
            }
            else
            {
                vertexIndices = null;
                return false;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvexHull3D" /> class.
        /// </summary>
        /// <param name="ts">The tessellated solid that the convex hull is made from.</param>
        public static bool Create(TessellatedSolid ts, out ConvexHull3D convexHull)
           // => Create(ts.Vertices, out convexHull, false, ts.SameTolerance);
        {
        debugSolid = ts;
        return Create(ts.Vertices, out convexHull, false, ts.SameTolerance);
    }

        private static TessellatedSolid debugSolid;

        public static bool Create(IList<Vertex> vertices, out ConvexHull3D convexHull,
            bool connectVerticesToCvxHullFaces, double tolerance = double.NaN)
        {
            var n = vertices.Count;
            if (double.IsNaN(tolerance)) tolerance = Constants.BaseTolerance;
            if (SolveAs2D(vertices, out convexHull, tolerance)) return true;

            /* The first step is to quickly identify the two to six vertices based on the
             * Akl-Toussaint heuristic. */
            var extremePoints = GetExtremaOnAABB(n, vertices, out var numExtrema);
            List<ConvexHullFace> simplexFaces;
            List<Vertex> cvxVertices;
            if (numExtrema == 1)
            {
                convexHull = new ConvexHull3D { tolerance = tolerance };
                //if (isMaximal) convexHull.Vertices.AddRange(vertices);
                convexHull.Vertices.Add(extremePoints[0]);
                return true;
            }
            if (numExtrema <= 3)
            {
                if (numExtrema == 2)
                {
                    var axis = extremePoints[1].Coordinates - extremePoints[0].Coordinates;
                    var maxDistance = double.NegativeInfinity;
                    Vertex thirdPoint = null;
                    foreach (var v in vertices)
                    {
                        var distance = (v.Coordinates - extremePoints[0].Coordinates).Cross(axis).LengthSquared();
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                            thirdPoint = v;
                        }
                    }
                    if (thirdPoint == extremePoints[0] || thirdPoint == extremePoints[1])
                    {
                        convexHull = new ConvexHull3D { tolerance = tolerance };
                        convexHull.Vertices.Add(extremePoints[0]);
                        convexHull.Vertices.Add(extremePoints[1]);
                        return true;
                    }
                    cvxVertices = new List<Vertex> { extremePoints[0], extremePoints[1], thirdPoint };
                }
                else cvxVertices = extremePoints;
                simplexFaces = new List<ConvexHullFace>
                {
                    new ConvexHullFace(cvxVertices[0], cvxVertices[1], cvxVertices[2]),
                    new ConvexHullFace(cvxVertices[1], cvxVertices[0], cvxVertices[2])
                };
            }
            else cvxVertices = BuildInitialSimplex(extremePoints, numExtrema, out simplexFaces);
            var simplexSolid = new TessellatedSolid(simplexFaces.Cast<TriangleFace>().ToList(), buildOptions:TessellatedSolidBuildOptions.Minimal);
            
            Presenter.ShowAndHang(simplexSolid);
            var simplexVertices = cvxVertices.ToHashSet();
            foreach (var v in vertices)
            {
                if (simplexVertices.Contains(v)) continue;
                AddVertexToProperFace(simplexFaces, v, tolerance);
            }
            var faceQueue = new Queue<ConvexHullFace>(simplexFaces);
            var cvxFaces = new List<ConvexHullFace>();
            while (faceQueue.Count > 0)
            {
                var face = faceQueue.Dequeue();
                if (face.peakVertex == null)
                {
                    cvxFaces.Add(face);
            //Presenter.ShowAndHang(cvxFaces);
                }
                else
                {
                    var newFaces = new[] {new ConvexHullFace(face.A, face.B, face.peakVertex),
                     new ConvexHullFace(face.B, face.C, face.peakVertex), new ConvexHullFace(face.C, face.A, face.peakVertex)};
                    foreach (var iv in face.InteriorVertices)
                        AddVertexToProperFace(newFaces, iv, tolerance);
                    foreach (var newFace in newFaces)
                        faceQueue.Enqueue(newFace);
                    foreach (var newFace in newFaces)
newFace.Color = new Color(KnownColors.LightSteelBlue);
                        //Presenter.ShowAndHang(faceQueue);
                }
            }
            convexHull = MakeConvexHullWithFaces(tolerance, connectVerticesToCvxHullFaces, cvxFaces);
            return true;
        }

        private static ConvexHull3D MakeConvexHullWithFaces(double tolerance,
            bool connectVerticesToCvxHullFaces, List<ConvexHullFace> cvxFaces)
        {
            var cvxHull = new ConvexHull3D { tolerance = tolerance };
            cvxHull.Faces.AddRange(cvxFaces);
            var cvxVertexHash = new HashSet<Vertex>();
            foreach (var f in cvxFaces)
            {
                foreach (var v in f.Vertices)
                {
                    v.PartOfConvexHull = true;
                    cvxVertexHash.Add(v);
                    if (connectVerticesToCvxHullFaces)
                        v.Faces.Add(f);
                }
                foreach (var v in f.InteriorVertices)
                    v.PartOfConvexHull = true;
            }
            cvxHull.Vertices.AddRange(cvxVertexHash);
            return cvxHull;
        }

        private static void AddVertexToProperFace(IList<ConvexHullFace> faces, Vertex v, double tolerance)
        {
            var maxDot = double.NegativeInfinity;
            ConvexHullFace maxFace = null;
            foreach (var face in faces)
            {
                var dot = (v.Coordinates - face.Center).Dot(face.Normal);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    maxFace = face;
                }
            }
            if (maxDot >= -tolerance)
            {
                if ((maxFace.peakVertex == null && maxDot >= tolerance) || maxDot > maxFace.peakDistance)
                {
                    if (maxFace.peakVertex != null)
                        maxFace.InteriorVertices.Add(maxFace.peakVertex);
                    maxFace.peakVertex = v;
                    maxFace.peakDistance = maxDot;
                }
                else maxFace.InteriorVertices.Add(v);
            }
        }

        private static bool SolveAs2D(IList<Vertex> vertices, out ConvexHull3D convexHull, double tolerance = double.NaN)
        {
            Plane.DefineNormalAndDistanceFromVertices(vertices, out var distance, out var planeNormal);
            var plane = new Plane(distance, planeNormal);
            if (plane.CalculateMaxError(vertices.Select(v => v.Coordinates)) > Constants.DefaultPlaneDistanceTolerance)
            {
                convexHull = null;
                return false;
            }
            var coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out var backTransform).ToList();
            if (coords2D.Area() < 0)
            {
                planeNormal *= -1;
                distance *= -1;
                coords2D = vertices.ProjectTo2DCoordinates(planeNormal, out backTransform).ToList();
            }
            var cvxHull2D = coords2D.CreateConvexHull(out var vertexIndices);
            //var cvxHull3DPoints = cvxHull2D.ConvertTo3DLocations(backTransform).ToList();
            var indexHash = vertexIndices.ToHashSet();
            convexHull = new ConvexHull3D { tolerance = tolerance };
            //convexHull.Vertices.AddRange(vertexIndices.Select(ind => vertices[ind]));
            var interiorVertices = new List<Vertex>();
            for (var i = 0; i < vertices.Count; i++)
            {
                if (indexHash.Contains(i))
                {
                    convexHull.Vertices.Add(vertices[i]);
                    if (convexHull.Vertices.Count < 3) continue;
                    convexHull.Faces.Add(new ConvexHullFace(convexHull.Vertices[0], convexHull.Vertices[convexHull.Vertices.Count - 2],
                        convexHull.Vertices[convexHull.Vertices.Count - 1], planeNormal));
                    convexHull.Faces.Add(new ConvexHullFace(convexHull.Vertices[convexHull.Vertices.Count - 1],
                        convexHull.Vertices[convexHull.Vertices.Count - 2], convexHull.Vertices[0], -planeNormal));
                }
                else interiorVertices.Add(vertices[i]);
            }
            foreach (var v in interiorVertices)
            {
                for (var i = 0; i < convexHull.Faces.Count; i += 2)
                {
                    var face = convexHull.Faces[i];
                    if (MiscFunctions.IsVertexInsideTriangle(face, v.Coordinates))
                    {
                        face.InteriorVertices.Add(v);
                        convexHull.Faces[i + 1].InteriorVertices.Add(v);
                    }
                }
            }
            return true;
        }


        private static List<Vertex> BuildInitialSimplex(List<Vertex> extremePoints, int numExtrema, out List<ConvexHullFace> simplexFaces)
        {
            var maxVol = 0.0;
            int maxI1 = -1, maxI2 = -1, maxI3 = -1, maxI4 = -1;
            var invertBest = false;
            for (int i1 = 0; i1 < numExtrema - 3; i1++)
            {
                var basePoint = extremePoints[i1];
                for (int i2 = i1 + 1; i2 < numExtrema - 2; i2++)
                {
                    for (int i3 = i2 + 1; i3 < numExtrema - 1; i3++)
                    {
                        var baseTriangleArea = (extremePoints[i2].Coordinates - basePoint.Coordinates).Cross(extremePoints[i3].Coordinates - basePoint.Coordinates);
                        for (int i4 = i3 + 1; i4 < numExtrema; i4++)
                        {
                            var projectedHeight = basePoint.Coordinates- extremePoints[i4].Coordinates ;
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
            var simplexVertices = new List<Vertex>();
            simplexFaces = new List<ConvexHullFace>();
            simplexVertices.Add(extremePoints[maxI1]);
            if (invertBest)
            {  // based on the cross product, the order of the points is wrong
                simplexVertices.Add(extremePoints[maxI3]);
                simplexVertices.Add(extremePoints[maxI2]);
            }
            else
            {
                simplexVertices.Add(extremePoints[maxI2]);
                simplexVertices.Add(extremePoints[maxI3]);
            }
            simplexVertices.Add(extremePoints[maxI4]);

            simplexFaces.Add(new ConvexHullFace(simplexVertices[0], simplexVertices[1], simplexVertices[2]));
            simplexFaces.Add(new ConvexHullFace(simplexVertices[0], simplexVertices[3], simplexVertices[1]));
            simplexFaces.Add(new ConvexHullFace(simplexVertices[1], simplexVertices[3], simplexVertices[2]));
            simplexFaces.Add(new ConvexHullFace(simplexVertices[2], simplexVertices[3], simplexVertices[0]));
            return simplexVertices;
        }

        private static List<Vertex> GetExtremaOnAABB(int n, IList<Vertex> points, out int numExtrema)
        {
            var extremePoints = Enumerable.Repeat(points[0], 6).ToList();
            for (int i = 1; i < n; i += 2)
            {
                if (points[i].X < extremePoints[0].X ||
                    points[i].X == extremePoints[0].X && points[i].Y < extremePoints[0].Y)
                    extremePoints[0] = points[i];
                if (points[i].X > extremePoints[1].X ||
                    points[i].X == extremePoints[1].X && points[i].Z > extremePoints[1].Z)
                    extremePoints[1] = points[i];
                if (points[i].Y < extremePoints[2].Y ||
                    points[i].Y == extremePoints[2].Y && points[i].Z < extremePoints[2].Z)
                    extremePoints[2] = points[i];
                if (points[i].Y > extremePoints[3].Y ||
                    points[i].Y == extremePoints[3].Y && points[i].X > extremePoints[3].X)
                    extremePoints[3] = points[i];
                if (points[i].Z < extremePoints[4].Z ||
                    points[i].Z == extremePoints[4].Z && points[i].X < extremePoints[4].X)
                    extremePoints[4] = points[i];
                if (points[i].Z > extremePoints[5].Z ||
                    points[i].Z == extremePoints[5].Z && points[i].Y > extremePoints[5].Y)
                    extremePoints[5] = points[i];
            }
            numExtrema = 6;
            for (int i = numExtrema - 1; i > 0; i--)
            {
                var extremeI = extremePoints[i];
                for (int j = 0; j < i; j++)
                {
                    var extremeJ = extremePoints[j];
                    if (extremeI == extremeJ)
                    {
                        numExtrema--;
                        extremePoints.RemoveAt(i);
                        break;
                    }
                }
            }
            return extremePoints;
        }
    }
}