// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Curves;
using TVGL.Numerics;

namespace TVGL
{
    /// <summary>
    /// Class Primitive_Classification.
    /// </summary>
    public static class PrimitiveClassification
    {
        const int lowMedVertexCutoff = 4;
        const int medHighVertexCutoff = 7;
        const int highUltraVertexCutoff = 13;
        const double errorInitialEdgePairing = 0.05;
        const double minCircleEdgeAngle = 2.0;
        const double tsToleranceMultiplier = 10000;
        static TessellatedSolid debugSolid;
        //this corresponds to 114.6 degrees. That seems about right. I was thinking 120 but that seemed
        // on the high side


        public static List<PrimitiveSurface> ClassifyPrimitiveSurfaces(this TessellatedSolid tessellatedSolid, bool AddToInputSolid = true)
        {
#if PRESENT
            debugSolid = tessellatedSolid;
#endif
            tessellatedSolid.MakeEdgesIfNonExistent();
            // first "order" the vertices by the number of edges that connect. Sometime in graph theory this is referred to as degree or valence
            var verticesCategorized = CategorizeVerticesByValence(tessellatedSolid.Vertices);
            // start with a hashset of all the edges. I believe it is more efficent to the innards of hashset to remove than to add new.
            // so here we keep track of what is available as opposed to what has already been used.
            var availableEdgeHash = tessellatedSolid.Edges.ToHashSet();
            #region Step 1: Find all Conics in TS
            var conics = new List<ConicSection>();
            foreach (var v in verticesCategorized)
                MakeConics(availableEdgeHash, v, conics, tsToleranceMultiplier * tessellatedSolid.SameTolerance);

            #region Debugging
#if PRESENT
            TVGL.Presenter.ShowVertexPathsWithSolid(conics.Select(cs => cs.Points
                                        .Select(p => p.ConvertTo3DLocation(cs.Plane.AsTransformFromXYPlane))), new[] { debugSolid }, false);


#endif
            #endregion
            #endregion

            // like was done above with edges, here we start with all faces as available faces
            var availableFaces = new HashSet<PolygonalFace>(tessellatedSolid.Faces);
            var primitives = new List<PrimitiveSurface>();
            // consider both sides of the conic as a possibilitiy for defining a primitive shape
            var conicHash = new HashSet<ConicSection>(conics);
            var edgesInConics = new Dictionary<Edge, ConicSection>();
            foreach (var conic in conics)
                foreach (var edgeAndDir in conic.EdgesAndDirection)
                    edgesInConics.Add(edgeAndDir.Item1, conic);
            // or should we use priority queue here
            while (conicHash.Any())
            {
                MakePrimitiveSurfaces(primitives, availableFaces, edgesInConics, conicHash);
            }
#if PRESENT
            PaintSurfaces(primitives, tessellatedSolid);
            Presenter.ShowAndHang(tessellatedSolid);
            ReportStats(primitives);
#endif
            if (AddToInputSolid && primitives.Any())
                tessellatedSolid.Primitives = primitives;
            return primitives;
        }

        private static void MakePrimitiveSurfaces(List<PrimitiveSurface> primitiveSurfaces, HashSet<PolygonalFace> availableFaces,
            Dictionary<Edge, ConicSection> edgesInConics, HashSet<ConicSection> conicHash)
        {
            var conic = conicHash.First();
            var side = !conic.PositiveSideVisited;
            if (side) conic.PositiveSideVisited = true;
            else conic.NegativeSideVisited = true;
            if (conic.PositiveSideVisited && conic.NegativeSideVisited)
                conicHash.Remove(conic);
            var primalFaces = new List<PolygonalFace>(conic.EdgesAndDirection
                .Select(eAD => eAD.Item2 == side ? eAD.Item1.OwnedFace : eAD.Item1.OtherFace)
                .Where(f => availableFaces.Contains(f)));
            if (primalFaces.Count < 3) return; // it would normally be impossible but given previous passes. perhaps one or more
            // faces have already been incorporated?
            var dotsWithConicPlane = primalFaces.Select(pF => Math.Abs(pF.Normal.Dot(conic.Plane.Normal))).ToList();
            PrimitiveSurface primitiveSurface = null;
            if (conic.ConicType == ConicSectionType.Circle && dotsWithConicPlane.Min() > 1 - Constants.SameFaceNormalDotTolerance) // must be a flat
                primitiveSurface = new Plane(primalFaces);
            else if (conic.ConicType == ConicSectionType.Circle && dotsWithConicPlane.Max() < Constants.SameFaceNormalDotTolerance) // must be a cylinder
                primitiveSurface = new Cylinder(primalFaces, conic.Plane.Normal);
            else // the faces are not all the same direction as the curve nor are the perpendicular, so cone, sphere or torus
            {
                Debug.WriteLine("Cone, sphere, or torus");
                primitiveSurface = new Cone(primalFaces, conic.Plane.Normal,Math.Acos(dotsWithConicPlane.Average()));
            }
            if (ExpandToFindPrimitiveSurface(conic, primitiveSurface, availableFaces, edgesInConics, out List<ConicSection> neighborConics))
            {
                primitiveSurfaces.Add(primitiveSurface);
                foreach (var face in primitiveSurface.Faces)
                    availableFaces.Remove(face);
                foreach (var neightbor in neighborConics)
                    if (!conic.PositiveSideVisited && !conic.NegativeSideVisited)
                        conicHash.Remove(neightbor);
            }
        }

        private static bool ExpandToFindPrimitiveSurface(ConicSection startingConic, PrimitiveSurface primitiveSurface,
            HashSet<PolygonalFace> availableFaces, Dictionary<Edge, ConicSection> edgesInConics, 
            out List<ConicSection> neighborConics)
        {
            var borderEdges = new HashSet<Edge>(startingConic.EdgesAndDirection.Select(eAD => eAD.Item1));
            neighborConics = new List<ConicSection>();
            var faceQueue = new Queue<PolygonalFace>(primitiveSurface.Faces.SelectMany(f => f.AdjacentFaces));
            while (faceQueue.Any())
            {
                var face = faceQueue.Dequeue();
                if (!primitiveSurface.IsNewMemberOf(face)) continue;
                primitiveSurface.UpdateWith(face);
                foreach (var edge in face.Edges)
                {
                    if (borderEdges.Contains(edge)) continue;
                    var faceOwnsEdge = face == edge.OwnedFace;
                    if (edgesInConics.ContainsKey(edge))
                    { // if edge is not in borderEdges, but it is still in here, then this is a new conic
                        var newNeighborConic = edgesInConics[edge];
                        var onPositiveSide = newNeighborConic.EdgesAndDirection.First(eAD => eAD.Item1 == edge)
                            .Item2 == faceOwnsEdge;
                        if ((onPositiveSide && newNeighborConic.PositiveSideVisited) ||
                        (!onPositiveSide && newNeighborConic.NegativeSideVisited)) continue;
                        if (!neighborConics.Contains(newNeighborConic)) neighborConics.Add(newNeighborConic);
                        foreach (var neighborEdgeAndDir in newNeighborConic.EdgesAndDirection)
                        {
                            borderEdges.Add(neighborEdgeAndDir.Item1);
                            faceQueue.Enqueue(neighborEdgeAndDir.Item2 == onPositiveSide ?
                                neighborEdgeAndDir.Item1.OwnedFace : neighborEdgeAndDir.Item1.OtherFace);
                        }
                        if (onPositiveSide)
                            newNeighborConic.PositiveSideVisited = true;
                        else newNeighborConic.NegativeSideVisited = true;
                    }
                    var adjacentFace = faceOwnsEdge ? edge.OtherFace : edge.OwnedFace;
                    if (!availableFaces.Contains(adjacentFace)) continue;
                    faceQueue.Enqueue(adjacentFace);
                }
            }
            // for a contiguous patch - even with holes, the number of outer edges should not greatly exceed
            // the number of faces. this is because the only valid cases where a single triangle can have two
            // outeredges is at a corner.
            //PaintSurfaces(new[] { primitiveSurface }, debugSolid);
            //Presenter.ShowAndHang(debugSolid);
            return primitiveSurface.OuterEdges.Count < 1.3 * primitiveSurface.Faces.Count;
        }

        /// <summary>
        /// Makes the conics.
        /// </summary>
        /// <param name="availableEdgeHash">The available edge hash.</param>
        /// <param name="v">The v.</param>
        /// <param name="conics">The conics.</param>
        /// <param name="tolerance">The tolerance.</param>
        private static void MakeConics(HashSet<Edge> availableEdgeHash, Vertex v, List<ConicSection> conics, double tolerance)
        {
            var sortedEdgesByLength = v.Edges.Where(e => availableEdgeHash.Contains(e)).OrderBy(e => e.Vector.LengthSquared()).ToList();
            for (int i = 0; i < sortedEdgesByLength.Count - 1; i++)
            {
                var inEdge = sortedEdgesByLength[i];
                if (!availableEdgeHash.Contains(inEdge)) continue; //it's necessary to check again since a previous
                                                                   //pass in this same for loop might find another curve     
                var inEdgeLength = inEdge.Vector.LengthSquared();
                for (int j = i + 1; j < sortedEdgesByLength.Count; j++)
                {
                    var outEdge = sortedEdgesByLength[j];
                    if (!availableEdgeHash.Contains(outEdge)) continue; //it's necessary to check again since a previous
                                                                        //pass in this same for loop might find another curve                        
                    var outEdgeLength = outEdge.Vector.LengthSquared();
                    var error = 2 * (outEdgeLength - inEdgeLength) / (inEdgeLength + outEdgeLength);
                    if (error > errorInitialEdgePairing) break; // if the error is too large then there is no reason to check 
                                                                // with remaining edges since these are sorted
                    if (ExpandToFindConicSection(v, inEdge, outEdge, out var conicSection, availableEdgeHash, tolerance))
                    {
                        foreach (var edge in conicSection.EdgesAndDirection)
                            availableEdgeHash.Remove(edge.Item1);
                        conics.Add(conicSection);
                        //#if PRESENT
                        //                        TVGL.Presenter.ShowVertexPathsWithSolid(new[]{conicSection.Points
                        //                                                    .Select(p => p.ConvertTo3DLocation(conicSection.Plane.AsTransformFromXYPlane)) }, new[] { debugSolid }, false);
                        //                        //TVGL.Presenter.ShowAndHang(conicSection.Points);
                        //#endif
                        break;
                    }
                }
            }
        }

        private static bool ExpandToFindConicSection(Vertex v, Edge inEdge, Edge outEdge, out ConicSection conicSection,
            HashSet<Edge> availableEdgeHash, double tolerance)
        {
            var inVector = inEdge.Vector;
            var inEdgeInCorrectDirection = true;
            if (inEdge.From == v)
            {
                inVector *= -1;
                inEdgeInCorrectDirection = false;
            }
            var outVector = outEdge.Vector;
            var outEdgeInCorrectDirection = true;
            if (outEdge.To == v)
            {
                outVector *= -1;
                outEdgeInCorrectDirection = false;
            }
            var angle = inVector.SmallerAngleBetweenVectors(outVector);
            if (angle < minCircleEdgeAngle)
            {   // if the angle is not very obtuse then this is likely to be a sharp edge and not a smooth quadric surface
                conicSection = null;
                return false;
            }
            if (angle.IsPracticallySame(Math.PI)) //then straightLine
                conicSection = ConicSection.DefineForLine(v.Coordinates, inVector.Normalize());
            else
            {
                var plane = new Plane(v.Coordinates, inVector.Cross(outVector).Normalize());
                conicSection = ConicSection.DefineForCircle(plane,
                    MinimumEnclosure.GetCircleFrom3Points(v.Coordinates, inEdge.OtherVertex(v).Coordinates, outEdge.OtherVertex(v).Coordinates, plane));
            }
            conicSection.Points.Add(v.ConvertTo2DCoordinates(conicSection.Plane.AsTransformToXYPlane));
            return FindEdgesInPlane(inEdge, inEdgeInCorrectDirection, outEdge, outEdgeInCorrectDirection, conicSection,
                availableEdgeHash, new HashSet<Vertex> { v }, tolerance);
        }

        private static bool FindEdgesInPlane(Edge inEdge, bool inEdgeInCorrectDirection, Edge outEdge,
            bool outEdgeInCorrectDirection, ConicSection conicSection, HashSet<Edge> availableEdgeHash,
            HashSet<Vertex> currentVertexHash, double tolerance)
        {
            Vertex newInVertex = inEdge == null ? null : inEdgeInCorrectDirection ? inEdge.From : inEdge.To;
            Vertex newOutVertex = outEdge == null ? null : outEdgeInCorrectDirection ? outEdge.To : outEdge.From;
            var sameVertex = newOutVertex == newInVertex;
            var inError = newInVertex == null ? double.PositiveInfinity : conicSection.CalcError(newInVertex.Coordinates);
            var outError = sameVertex || newOutVertex == null ? double.PositiveInfinity : conicSection.CalcError(newOutVertex.Coordinates);
            if (inError <= outError && inError < double.PositiveInfinity)
            {
                if (inError < tolerance || conicSection.Upgrade(newInVertex.Coordinates, tolerance))
                {
                    conicSection.AddStart(inEdge, inEdgeInCorrectDirection);
                    currentVertexHash.Add(inEdgeInCorrectDirection ? inEdge.From : inEdge.To);
                    if (sameVertex) return conicSection.Points.Count > 3;
                    var bestNextEdge = FindNextBest(newInVertex, inEdge, availableEdgeHash, currentVertexHash, conicSection, tolerance);
                    return FindEdgesInPlane(bestNextEdge, bestNextEdge?.To == newInVertex, outEdge, outEdgeInCorrectDirection,
                        conicSection, availableEdgeHash, currentVertexHash, tolerance);
                }
            }
            else if (outError < tolerance || (outError < double.PositiveInfinity && conicSection.Upgrade(newOutVertex.Coordinates, tolerance)))
            {
                conicSection.AddEnd(outEdge, outEdgeInCorrectDirection);
                currentVertexHash.Add(outEdgeInCorrectDirection ? outEdge.To : outEdge.From);
                var bestNextEdge = FindNextBest(newOutVertex, outEdge, availableEdgeHash, currentVertexHash, conicSection, tolerance);
                return FindEdgesInPlane(inEdge, inEdgeInCorrectDirection, bestNextEdge, bestNextEdge?.From == newOutVertex,
                    conicSection, availableEdgeHash, currentVertexHash, tolerance);
            }
            return conicSection.Points.Count > 3;
        }

        private static Edge FindNextBest(Vertex vertex, Edge fromEdge, HashSet<Edge> availableEdgeHash, HashSet<Vertex> currentVertexHash,
            ConicSection conicSection, double tolerance)
        {
            Edge bestEdge = null;
            var largestAngle = double.NegativeInfinity;
            foreach (var edge in vertex.Edges)
            {
                if (edge == fromEdge) continue;
                if (!availableEdgeHash.Contains(edge)) continue;
                var edgeIsIncoming = edge.To == vertex;
                var otherVertex = edgeIsIncoming ? edge.From : edge.To;
                if (currentVertexHash.Contains(otherVertex)) continue;
                if (Math.Abs(otherVertex.Dot(conicSection.Plane.Normal) - conicSection.Plane.DistanceToOrigin) > tolerance) continue;
                if (conicSection.CalcError(otherVertex.Coordinates) > tolerance) continue;
                var angle = fromEdge.Vector.SmallerAngleBetweenVectors(edge.Vector);
                if (largestAngle < angle)
                {
                    largestAngle = angle;
                    bestEdge = edge;
                }
            }
            return bestEdge;
        }

        private static List<Vertex> CategorizeVerticesByValence(IEnumerable<Vertex> vertices)
        //private static void CategorizeVerticesByValence(IEnumerable<Vertex> vertices, out List<Vertex> low, out List<Vertex> med, out List<Vertex> high, out List<Vertex> ultra)
        {
            var low = new List<Vertex>();
            var med = new List<Vertex>();
            var high = new List<Vertex>();
            var ultra = new List<Vertex>();
            foreach (var v in vertices)
            {
                int valence = v.Edges.Count;
                if (valence < lowMedVertexCutoff) low.Add(v);
                else if (valence < medHighVertexCutoff) med.Add(v);
                else if (valence < highUltraVertexCutoff) high.Add(v);
                else ultra.Add(v);
            }
            low.AddRange(med);
            low.AddRange(high);
            low.AddRange(ultra);
            return low;
        }
        #region Make Primitives

        private static bool IsReallyAFlat(IEnumerable<PolygonalFace> faces)
        {
            return (MiscFunctions.FacesWithDistinctNormals(faces.ToList()).Count == 1);
        }

        public static bool IsReallyACone(IEnumerable<PolygonalFace> facesAll, out Vector3 axis, out double coneAngle)
        {
            var faces = MiscFunctions.FacesWithDistinctNormals(facesAll.ToList());
            var n = faces.Count;
            if (faces.Count <= 1)
            {
                axis = Vector3.Null;
                coneAngle = double.NaN;
                return false;
            }
            if (faces.Count == 2)
            {
                axis = faces[0].Normal.Cross(faces[1].Normal).Normalize();
                coneAngle = 0.0;
                return false;
            }

            // a simpler approach: if the cross product of the normals are all parallel, it's a cylinder,
            // otherwise, cone.

            /*var r = new Random();
            var rndList = new List<int>();
            var crossProd = new List<Vector2>();
            var c = 0;
            while (c < 20)
            {
                var ne = r.Next(facesAll.Count);
                if (!rndList.Contains(ne)) rndList.Add(ne);
                c++;
            }
            for (var i = 0; i < rndList.Count-1; i++)
            {
                for (var j = i + 1; j < rndList.Count; j++)
                {
                    crossProd.Add(facesAll[i].Normal.Cross(facesAll[j].Normal).Normalize());
                }
            }
            axis = crossProd[0];
            coneAngle = 0.0;
            for (var i = 0; i < crossProd.Count - 1; i++)
            {
                for (var j = i + 1; j < crossProd.Count; j++)
                {
                    if (Math.Abs(crossProd[i].Dot(crossProd[j]) - 1) < 0.00000008) return true;
                }
            }
            return false;*/

            // find the plane that the normals live on in the Gauss sphere. If it is not
            // centered at 0 then you have a cone.
            // since the vectors that are the difference of two normals (v = n1 - n2) would
            // be in the plane, let's first figure out the average plane of this normal
            var inPlaneVectors = new Vector3[n];
            inPlaneVectors[0] = faces[0].Normal.Subtract(faces[n - 1].Normal);
            for (int i = 1; i < n; i++)
                inPlaneVectors[i] = faces[i].Normal.Subtract(faces[i - 1].Normal);

            var normalsOfGaussPlane = new List<Vector3>();
            var tempCross = inPlaneVectors[0].Cross(inPlaneVectors[n - 1]).Normalize();
            if (!tempCross.IsNull())
                normalsOfGaussPlane.Add(tempCross);
            for (int i = 1; i < n; i++)
            {
                tempCross = inPlaneVectors[i].Cross(inPlaneVectors[i - 1]).Normalize();
                if (!tempCross.IsNull())
                    if (tempCross.Dot(normalsOfGaussPlane[0]) >= 0)
                        normalsOfGaussPlane.Add(tempCross);
                    else normalsOfGaussPlane.Add(-1 * tempCross);
            }
            var normalOfGaussPlane = new Vector3();
            normalOfGaussPlane = normalsOfGaussPlane.Aggregate(normalOfGaussPlane, (current, c) => current + c);
            normalOfGaussPlane = normalOfGaussPlane.Divide(normalsOfGaussPlane.Count);

            var distance = faces.Sum(face => face.Normal.Dot(normalOfGaussPlane));
            if (distance < 0)
            {
                axis = normalOfGaussPlane * -1;
                distance = -distance / n;
            }
            else
            {
                distance /= n;
                axis = normalOfGaussPlane;
            }
            coneAngle = Math.Asin(distance);
            return Math.Abs(distance) >= Constants.MinConeGaussPlaneOffset;
        }

        private static bool IsReallyATorus(IEnumerable<PolygonalFace> faces)
        {
            return false;
            throw new NotImplementedException();
        }

        #endregion
        #region Show Results
        private static void PaintSurfaces(IEnumerable<PrimitiveSurface> primitives, TessellatedSolid ts)
        {
            ts.HasUniformColor = false;
            foreach (var f in ts.Faces)
            {
                f.Color = new Color(KnownColors.LightGray);
            }
            foreach (var primitiveSurface in primitives)
            {
                if (primitiveSurface is Cylinder)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Salmon);
                else if (primitiveSurface is Sphere)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Blue);
                else if (primitiveSurface is Plane)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Green);
                else if (primitiveSurface is UnknownRegion)
                    foreach (var f in primitiveSurface.Faces)
                        f.Color = new Color(KnownColors.Black);
            }
        }
        private static void ReportStats(List<PrimitiveSurface> primitives)
        {
            Debug.WriteLine("**************** RESULTS *******************");
            Debug.WriteLine("Number of Primitives = " + primitives.Count);
            Debug.WriteLine("Number of Flats = " + primitives.Count(p => p is Plane));
            Debug.WriteLine("Number of Cones = " + primitives.Count(p => p is Cone));
            Debug.WriteLine("Number of Cylinders = " + primitives.Count(p => p is Cylinder));
            Debug.WriteLine("Number of Spheres = " + primitives.Count(p => p is Sphere));
            Debug.WriteLine("Number of Torii = " + primitives.Count(p => p is Torus));
            Debug.WriteLine("Primitive Max Faces = " + primitives.Max(p => p.Faces.Count));
            var minFaces = primitives.Min(p => p.Faces.Count);
            Debug.WriteLine("Primitive Min Faces = " + minFaces);
            Debug.WriteLine("Number of with min faces = " + primitives.Count(p => p.Faces.Count == minFaces));
            Debug.WriteLine("Primitive Avg. Faces = " + primitives.Average(p => p.Faces.Count));
        }
        #endregion

    }
}