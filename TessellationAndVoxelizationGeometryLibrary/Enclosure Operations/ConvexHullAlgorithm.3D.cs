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
        {
            /// debug
            debugSolid = ts;
            ///
            if (Create(ts.Vertices, out convexHull, false, ts.SameTolerance))
            {
                ts.ConvexHull = convexHull;
                foreach (var face in ts.Faces.Where(face => face.Vertices.All(v => v.PartOfConvexHull)))
                {
                    face.PartOfConvexHull = true;
                    foreach (var e in face.Edges)
                        if (e != null) e.PartOfConvexHull = true;
                }
                return true;
            }
            else
            {
                convexHull = null;
                return false;
            }
        }

        /// debug
        private static TessellatedSolid debugSolid;
        ///

        public static bool Create(IList<Vertex> vertices, out ConvexHull3D convexHull,
            bool connectVerticesToCvxHullFaces, double tolerance = double.NaN)
        {
            var n = vertices.Count;
            if (double.IsNaN(tolerance)) tolerance = Constants.BaseTolerance;
            if (SolveAs2D(vertices, out convexHull, tolerance)) return true;

            /* The first step is to quickly identify the two to six vertices based on the
             * Akl-Toussaint heuristic. */
            var extremePoints = GetExtremaOnAABB(n, vertices, out var numExtrema);
            if (numExtrema == 1)
            {
                convexHull = new ConvexHull3D { tolerance = tolerance };
                //if (isMaximal) convexHull.Vertices.AddRange(vertices);
                convexHull.Vertices.Add(extremePoints[0]);
                return true;
            }
            if (numExtrema == 2)
            {
                var thirdPoint = Find3rdStartingPoint(extremePoints, vertices);
                if (thirdPoint == extremePoints[0] || thirdPoint == extremePoints[1])
                {
                    convexHull = new ConvexHull3D { tolerance = tolerance };
                    convexHull.Vertices.Add(extremePoints[0]);
                    convexHull.Vertices.Add(extremePoints[1]);
                    return true;
                }
                extremePoints = new List<Vertex> { extremePoints[0], extremePoints[1], thirdPoint };
            }
            else if (numExtrema > 4) FindBestExtremaSubset(extremePoints);
            var simplexFaces = MakeSimplexFaces(extremePoints);
            /// debug
            ////var simplexSolid = new TessellatedSolid(simplexFaces.Cast<TriangleFace>().ToList(), 
            ////    buildOptions: new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor=true, DefineConvexHull=false});
            ////Presenter.ShowAndHang(simplexSolid);
            /// end debug
            // now add all the other vertices to the simplex faces. AddVertexToProperFace will add the vertex to the face that it is "farthest" from
            var extremePointsHash = extremePoints.ToHashSet();
            foreach (var v in vertices)
            {
                if (extremePointsHash.Contains(v)) continue;
                AddVertexToProperFace(simplexFaces, v, tolerance);
            }
            var faceQueue = new UpdatablePriorityQueue<ConvexHullFace, double>(simplexFaces.Select(f => (f, f.peakDistance)), new NoEqualSort(false));
            var newFaces = new List<ConvexHullFace>();
            var oldFaces = new List<ConvexHullFace>();
            var verticesToReassign = new List<Vertex>();
            while (true)
            {
                var face = faceQueue.Dequeue();
                face.Color = new Color(KnownColors.LightCyan);
                Presenter.ShowAndHang(faceQueue.UnorderedItems.Select(fq => fq.Element).Concat([face]));
                if (face.peakVertex == null)
                {
                    faceQueue.Enqueue(face, face.peakDistance);
                    break;
                }
                CreateNewFaceCone(face, newFaces, oldFaces, verticesToReassign);
                foreach (var iv in verticesToReassign)
                    AddVertexToProperFace(newFaces, iv, tolerance);
                foreach (var f in oldFaces)
                    faceQueue.Remove(f);
                foreach (var newFace in newFaces)
                    faceQueue.Enqueue(newFace, newFace.peakDistance);
                foreach (var nf in newFaces)
                    nf.Color = new Color(KnownColors.LightPink);
                Presenter.ShowAndHang(faceQueue.UnorderedItems.Select(ue => ue.Element));
                // debug
                foreach (var f in newFaces)
                    f.Color = new Color(100, 0, 100, 0);
                foreach (var f in oldFaces)
                    f.Color = new Color(100, 100, 0, 0);
                //Presenter.ShowAndHang(faceQueue.UnorderedItems.Select(fq => fq.Element).Concat(oldFaces));
                foreach (var f in newFaces)
                    f.Color = new Color(KnownColors.Gray);
                //// end debug
            }
            convexHull = MakeConvexHullWithFaces(tolerance, connectVerticesToCvxHullFaces,
                faceQueue.UnorderedItems.Select(fq => fq.Element));
            return true;
        }

        private static Vertex Find3rdStartingPoint(List<Vertex> extremePoints, IList<Vertex> vertices)
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
            return thirdPoint;
        }

        /// <summary>
        /// This is based on the method described in https://algolist.ru/maths/geom/convhull/qhull3d.php as CALCULATE_HORIZON
        /// It's subtle and complicated. One difference is that our borders are populated in the clockwise direction since the
        /// children on the stack are added in the CCW direction.
        /// </summary>
        /// <param name="startingFace"></param>
        /// <param name="newFaces"></param>
        /// <param name="oldFaces"></param>
        /// <param name="verticesToReassign"></param>
        /// <returns></returns>
        private static void CreateNewFaceCone(ConvexHullFace startingFace, List<ConvexHullFace> newFaces,
            List<ConvexHullFace> oldFaces, List<Vertex> verticesToReassign)
        {
            // clearing the lists for now - in the future make it more performant by overwriting
            newFaces.Clear();
            oldFaces.Clear();
            verticesToReassign.Clear();
            //
            //var borderFaces = new List<ConvexHullFace>();
            Edge inConeEdge = null;
            Edge firstInConeEdge = null;
            var peakVertex = startingFace.peakVertex;
            var peakCoord = peakVertex.Coordinates;
            var stack = new Stack<(ConvexHullFace, Edge)>();
            stack.Push((startingFace, null));
            while (stack.Count > 0)
            {
                var (current, connectingEdge) = stack.Pop();
                if (current.Visited) continue;
                if ((current.Center - peakCoord).Dot(current.Normal) > 0)
                { // current is part of the convex hull that is to be kept. it is beyond the horizon
                    // so we stop here but before we move down the stack we need to create a new face
                    // this border face is stored in the borderFaces list so that at the end we can clear the Visited flags
                    //borderFaces.Add(current);
                    ConvexHullFace newFace;
                    if (connectingEdge.OwnedFace == current)
                    {   // if the current owns the face then the new face will follow the edge backwards
                        newFace = new ConvexHullFace(connectingEdge.To, connectingEdge.From, peakVertex);
                        connectingEdge.OtherFace = newFace;
                        newFace.AddEdge(connectingEdge);
                        if (firstInConeEdge == null) // this is the first time through so we get to own the two in-cone edges as well
                        {
                            inConeEdge = new Edge(peakVertex, connectingEdge.To, newFace, null, false);
                            inConeEdge.OwnedFace = newFace;
                            newFace.AddEdge(inConeEdge);
                            firstInConeEdge = new Edge(connectingEdge.From, peakVertex, newFace, null, false);
                            newFace.AddEdge(firstInConeEdge);
                            firstInConeEdge.OwnedFace = newFace;
                        }
                        else // then it's not the first time we've been here, so firstEdge AND prevEdge should already be set
                        {
                            newFace.AddEdge(inConeEdge);
                            inConeEdge.OtherFace = newFace;
                            if (firstInConeEdge.To == connectingEdge.To || firstInConeEdge.From == connectingEdge.To)
                            {
                                newFace.AddEdge(firstInConeEdge);
                                firstInConeEdge.OtherFace = newFace;
                            }
                            else
                            {
                                inConeEdge = new Edge(peakVertex, connectingEdge.To, newFace, null, false);
                                newFace.AddEdge(inConeEdge);
                                inConeEdge.OwnedFace = newFace;
                            }
                        }
                    }
                    else // the newFace will own the edge
                    {
                        newFace = new ConvexHullFace(connectingEdge.From, connectingEdge.To, peakVertex);
                        connectingEdge.OwnedFace = newFace;
                        newFace.AddEdge(connectingEdge);
                        if (firstInConeEdge == null) // this is the first time through so we get to own the two in-cone edges as well
                        {
                            inConeEdge = new Edge(peakVertex, connectingEdge.From, newFace, null, false);
                            inConeEdge.OwnedFace = newFace;
                            newFace.AddEdge(inConeEdge);
                            firstInConeEdge = new Edge(connectingEdge.To, peakVertex, newFace, null, false);
                            newFace.AddEdge(firstInConeEdge);
                            firstInConeEdge.OwnedFace = newFace;
                        }
                        else // then it's not the first time we've been here, so firstEdge AND prevEdge should already be set
                        {
                            newFace.AddEdge(inConeEdge);
                            inConeEdge.OtherFace = newFace;
                            if (firstInConeEdge.To == connectingEdge.From || firstInConeEdge.From == connectingEdge.From)
                            {
                                newFace.AddEdge(firstInConeEdge);
                                firstInConeEdge.OtherFace = newFace;
                            }
                            else
                            {
                                inConeEdge = new Edge(peakVertex, connectingEdge.From, newFace, null, false);
                                newFace.AddEdge(inConeEdge);
                                inConeEdge.OwnedFace = newFace;
                            }
                        }
                    }
                    newFaces.Add(newFace);
                }
                else
                {
                    current.Visited = true;
                    oldFaces.Add(current);
                    if (current.peakVertex != null && current.peakVertex != peakVertex)
                        verticesToReassign.Add(current.peakVertex);
                    verticesToReassign.AddRange(current.InteriorVertices);
                    var connectingIndex = current.AB == connectingEdge ? 0 : current.BC == connectingEdge ? 1 : current.CA == connectingEdge ? 2 : -1;
                    var i = 0;
                    var neighborCount = connectingIndex >= 0 ? 2 : 3;
                    foreach (var edge in current.Edges.Concat(current.Edges))
                    {
                        if (i++ > connectingIndex)
                        {
                            if (edge == connectingEdge) continue; //this can  be removed once the code is debugged
                            stack.Push(((ConvexHullFace)edge.GetMatingFace(current), edge));
                            if (--neighborCount == 0) break;
                        }
                    }
                }
            }
            //foreach (var f in borderFaces)
            //    f.Visited = false;
        }

        private static ConvexHull3D MakeConvexHullWithFaces(double tolerance,
            bool connectVerticesToCvxHullFaces, IEnumerable<ConvexHullFace> cvxFaces)
        {
            var cvxHull = new ConvexHull3D { tolerance = tolerance };
            cvxHull.Faces.AddRange(cvxFaces);

            var cvxVertexHash = new HashSet<Vertex>();
            var cvxEdgeHash = new HashSet<Edge>();
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
                foreach (var e in f.Edges)
                {
                    e.PartOfConvexHull = true;
                    cvxEdgeHash.Add(e);
                    if (connectVerticesToCvxHullFaces)
                    {
                        e.From.Edges.Add(e);
                        e.To.Edges.Add(e);
                    }
                }
            }
            cvxHull.Vertices.AddRange(cvxVertexHash);
            cvxHull.Edges.AddRange(cvxEdgeHash);
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


        private static void FindBestExtremaSubset(List<Vertex> extremePoints)
        {
            var maxVol = 0.0;
            var numExtrema = extremePoints.Count;
            int maxI1 = -1, maxI2 = -1, maxI3 = -1, maxI4 = -1;
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
                            var projectedHeight = basePoint.Coordinates - extremePoints[i4].Coordinates;
                            var volume = Math.Abs(projectedHeight.Dot(baseTriangleArea));
                            if (volume > maxVol)
                            {
                                maxVol = volume;
                                maxI1 = i1; maxI2 = i2; maxI3 = i3; maxI4 = i4;
                            }
                        }
                    }
                }
            }
            for (int i = numExtrema - 1; i > 0; i--)
            {
                if (i == maxI1 || i == maxI2 || i == maxI3 || i == maxI4) continue;
                extremePoints.RemoveAt(i);
            }
        }
        private static List<ConvexHullFace> MakeSimplexFaces(List<Vertex> vertices)
        {
            if (vertices.Count == 3)
            {
                var face012 = new ConvexHullFace(vertices[0], vertices[1], vertices[2]);
                var face210 = new ConvexHullFace(vertices[2], vertices[1], vertices[0]);
                var edge01 = new Edge(vertices[0], vertices[1], face012, face210, false);
                edge01.OwnedFace = face012;
                edge01.OtherFace = face210;
                var edge12 = new Edge(vertices[1], vertices[2], face012, face210, false);
                edge12.OwnedFace = face012;
                edge12.OtherFace = face210;
                var edge20 = new Edge(vertices[2], vertices[0], face012, face210, false);
                edge20.OwnedFace = face012;
                edge20.OtherFace = face210;
                return [face012, face210];
            }
            else
            {
                var basePoint = vertices[0].Coordinates;
                var volume = (vertices[1].Coordinates - basePoint).Cross(vertices[2].Coordinates - basePoint).Dot(basePoint - vertices[3].Coordinates);
                if (volume < 0) Constants.SwapItemsInList(1, 2, vertices);

                var face012 = new ConvexHullFace(vertices[0], vertices[1], vertices[2]);
                var face031 = new ConvexHullFace(vertices[0], vertices[3], vertices[1]);
                var face132 = new ConvexHullFace(vertices[1], vertices[3], vertices[2]);
                var face230 = new ConvexHullFace(vertices[2], vertices[3], vertices[0]);
                var edge01 = new Edge(vertices[0], vertices[1], face012, face031, false);
                edge01.OwnedFace = face012;
                edge01.OtherFace = face031;
                var edge12 = new Edge(vertices[1], vertices[2], face012, face132, false);
                edge12.OwnedFace = face012;
                edge12.OtherFace = face132;
                var edge20 = new Edge(vertices[2], vertices[0], face012, face230, false);
                edge20.OwnedFace = face012;
                edge20.OtherFace = face230;
                var edge03 = new Edge(vertices[0], vertices[3], face031, face230, false);
                edge03.OwnedFace = face031;
                edge03.OtherFace = face230;
                var edge13 = new Edge(vertices[1], vertices[3], face132, face031, false);
                edge13.OwnedFace = face132;
                edge13.OtherFace = face031;
                var edge23 = new Edge(vertices[2], vertices[3], face230, face132, false);
                edge23.OwnedFace = face230;
                edge23.OtherFace = face132;
                return [face012, face031, face132, face230];
            }
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