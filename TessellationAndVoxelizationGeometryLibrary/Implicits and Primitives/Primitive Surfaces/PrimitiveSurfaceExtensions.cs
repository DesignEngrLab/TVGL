// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="PrimitiveSurface.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    /// Class PrimitiveSurface.
    /// </summary>
    [JsonObject(MemberSerialization.OptOut)]
    public static class PrimitiveSurfaceExtensions
    {

        /// <summary>
        /// Returns the Borders of the surface that encircle the surface's axis (e.g. the endcaps of a cylinder).
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A list of PrimitiveBorders.</returns>
        public static IEnumerable<BorderLoop> BordersEncirclingAxis(this PrimitiveSurface surface)
        {
            var axis = surface.GetAxis();
            if (axis.IsNull()) yield break;
            var anchor = surface.GetAnchor();
            if (anchor.IsNull()) yield break;
            foreach (var result in surface.BordersEncirclingAxis(axis, anchor))
                yield return result;
        }

        /// <summary>
        /// Returns the Borders of the surface that encircle the given axis and anchor point.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A list of PrimitiveBorders.</returns>
        public static IEnumerable<BorderLoop> BordersEncirclingAxis(this PrimitiveSurface surface, Vector3 axis, Vector3 anchor)
        {
            foreach (var border in surface.Borders)
                if (border.BorderEncirclesAxis(axis, anchor))
                    yield return border;
        }

        /// <summary>
        /// Reports if the given border the encircles axis of the given primitive surface.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="border">The border.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this PrimitiveSurface surface, BorderLoop border)
        {
            var axis = surface.GetAxis();
            if (axis.IsNull()) return false;
            var anchor = surface.GetAnchor();
            if (anchor.IsNull()) return false;
            return border.BorderEncirclesAxis(axis, anchor);
        }


        /// <summary>
        /// Reports if the given border the encircles the given axis and anchor.
        /// </summary>
        /// <param name="border">The border.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this BorderLoop border, Vector3 axis, Vector3 anchor)
        {
            return border.GetCoordinates().BorderEncirclesAxis(axis, anchor);
        }


        /// <summary>
        /// Reports if the given border the encircles the given axis and anchor.
        /// </summary>
        /// <param name="border">The border.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns>A bool.</returns>
        public static bool BorderEncirclesAxis(this BorderLoop border, Matrix4x4 transform, Vector3 anchor)
        {
            return border.GetCoordinates().BorderEncirclesAxis(transform, anchor);
        }
        /// <param name="transform">The transform.</param>


        /// <summary>
        /// Returns true if the given path encircles the axis (by more than 5/6 around).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="axis">The axis.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(this IEnumerable<Vector3> path, Vector3 axis, Vector3 anchor)
        {
            var angle = Math.Abs(MiscFunctions.FindWindingAroundAxis(path, axis, anchor, out _, out _));
            return angle > 1.67 * Math.PI;
            // 1.67 is 5/3, which is 5/6 the way around. so the border would be at least a hexagon.
        }

        /// <summary>
        /// Returns true if the given path encircles the axis (by more than 5/6 around).
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="transform">The transform.</param>
        /// <param name="anchor">The anchor.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool BorderEncirclesAxis(this IEnumerable<Vector3> path, Matrix4x4 transform, Vector3 anchor)
        {
            var angle = Math.Abs(MiscFunctions.FindWindingAroundAxis(path, transform, anchor, out _, out _));
            return angle > 1.67 * Math.PI;
            // 1.67 is 5/3, which is 5/6 the way around. so the border would be at least a hexagon.
        }



        /// <summary>
        /// Finds the largest encompassing angle for primitive about its axis. The resulting vectors
        /// (<paramref name="vectorAtMinAngle"/> & <paramref name="vectorAtMaxAngle"/>) emanate from the axis outward
        /// (they are orthogonal to the axis).
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <param name="vectorAtMinAngle">The vector at min angle.</param>
        /// <param name="vectorAtMaxAngle">The vector at max angle.</param>
        /// <returns>A double.</returns>
        public static double FindLargestEncompassingAngleForAxis(this PrimitiveSurface surface, out Vector3 vectorAtMinAngle,
            out Vector3 vectorAtMaxAngle, out double minAngle, out double maxAngle)
        => MiscFunctions.FindLargestEncompassingAngleForAxis(surface.Borders, surface.GetAxis(), surface.GetAnchor(), out vectorAtMinAngle, out vectorAtMaxAngle, out minAngle, out maxAngle);


        /// <summary>
        /// Gets the center of mass for primitive surface. This is a weighted sum using the face area,
        /// which would be the proper way to find the center of mass for a collection of triangles in a 2D plane
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetCenterOfMass(this PrimitiveSurface surface)
        { return surface.Faces.GetCenterOfMass(); }
        /// <summary>
        /// Gets the center of mass for the collection of faces. This is a weighted sum using the face area,
        /// which would be the proper way to find the center of mass for a collection of triangles in a 2D plane
        /// </summary>
        /// <param name="faces">The faces.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetCenterOfMass(this IEnumerable<TriangleFace> faces)
        {
            var totalArea = 0.0;
            var totalCenter = Vector3.Zero;
            foreach (var face in faces)
            {
                var area = face.Area;
                totalArea += area;
                totalCenter += face.Center * area;
            }
            return totalCenter / totalArea;
        }


        /// <summary>
        /// Gets the axis for the primitive surface. This is straightforward for cylinders, cones, tori,
        /// and prismatic surfaces. For planes, it returns the normal, and for spheres, it returns null.
        /// </summary>
        /// <param name="surface">The surface.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetAxis(this PrimitiveSurface surface)
        {
            if (surface is Plane plane) return plane.Normal;
            else if (surface is Cylinder cylinder) return cylinder.Axis;
            else if (surface is Cone cone) return cone.Axis;
            else if (surface is Torus torus) return torus.Axis;
            else if (surface is Prismatic prismatic) return prismatic.Axis;
            else if (surface is Capsule capsule) return (capsule.Anchor2 - capsule.Anchor1).Normalize();
            else if (surface is GeneralQuadric gq) return gq.Axis1;
            else return Vector3.Null;
        }

        /// <summary>
        /// Gets the anchor for the primitive surface. This is straightforward for cylinders, cones, tori,
        /// and spheres. For all others, it returns the center of mass.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>A Vector3.</returns>
        public static Vector3 GetAnchor(this PrimitiveSurface s)
        {
            if (s is Cylinder cylinder) return cylinder.Anchor;
            else if (s is Cone cone) return cone.Apex;
            else if (s is Torus torus) return torus.Center;
            else if (s is Sphere sphere) return sphere.Center;
            else if (s is Capsule capsule) return 0.5 * (capsule.Anchor1 + capsule.Anchor2);
            else if (s is GeneralQuadric gq) return gq.Center;
            else return GetCenterOfMass(s.Faces);
        }

        /// <summary>
        /// Gets the radius for the primitive surface. This is straightforward for cylinders, & spheres.
        /// For tori, it returns the major radius unless max is true. In which case it returns the larger
        /// of the minor and major radii. For all others, it returns the average radius of borders that
        /// are circle shaped. Unless max is true. In which case it returns the max of the radii of circles.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="max">If true, max.</param>
        /// <returns>A double.</returns>
        public static double GetRadius(this PrimitiveSurface s, bool max = false)
        {
            if (s is Cylinder cylinder) return cylinder.Radius;
            else if (s is Capsule capsule) return 0.5 * (capsule.Radius1 + capsule.Radius2);
            if (s is Sphere sphere) return sphere.Radius;
            if (s is Torus torus)
            {
                if (max) return Math.Max(torus.MajorRadius, torus.MinorRadius);
                return torus.MajorRadius;
            }
            if (s.Borders == null) return double.NaN;

            var circleBorders = s.Borders.Where(b => b.Curve is Circle);
            if (!circleBorders.Any()) return 0.0;
            else if (max) return circleBorders.Max(b => ((Circle)b.Curve).Radius);
            else return circleBorders.Average(b => ((Circle)b.Curve).Radius);
        }

        /// <summary>
        /// Given a set of 2D points in arbitrary order that are known to be on a curve, this function
        /// finds the extremes
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static (Vector2 start, Vector2 startDirj, Vector2 end, Vector2 endDir) FindExtremesAlong2DCurve(this PrimitiveSurface surface,
            IEnumerable<Vector2> points)
        {
            var int2PointDict = new Dictionary<int, (Vector2, Vertex)>();
            var pointsEnumerator = points.GetEnumerator();
            foreach (var v in surface.Vertices)
            {
                pointsEnumerator.MoveNext();
                var point = pointsEnumerator.Current;
                int2PointDict.Add(v.IndexInList, (point, v));
            }
            var possibleExtremes = new List<(Vector2, Vector2, Vector2)>();
            foreach ((Vector2 location, Vertex vertex) in int2PointDict.Values)
            {
                var outVectors = new List<Vector2>();
                var outer1 = Vector2.Null;
                var outer2 = Vector2.Null;
                foreach (var edge in vertex.Edges)
                {
                    var otherVertex = edge.OtherVertex(vertex);
                    if (!surface.OuterEdges.Contains(edge) && !surface.Vertices.Contains(otherVertex)) continue;
                    var otherLocation = int2PointDict[otherVertex.IndexInList].Item1;
                    var vector = otherLocation - location;
                    if (vector.LengthSquared().IsNegligible()) continue;
                    vector = vector.Normalize();
                    if (outer1.IsNull())
                        outer1 = vector;
                    else
                    {
                        outer2 = vector;
                        break;
                    }
                }
                if (outer1.IsNull() && possibleExtremes.Count > 0)
                    // this is subtle. if not outers then don't bother - unless you still haven't saved at least one. then might
                    // as well keep this one
                    continue;
                if (!outer2.IsNull() && outer1.Dot(outer2) < 0)
                    // if you have 2 outers they can't be in opposite (dot < 0) directions 
                    continue;
                possibleExtremes.Add((location, outer1, outer2));
            }
            if (possibleExtremes.Count == 2) return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                    possibleExtremes[1].Item1, (possibleExtremes[1].Item2 + possibleExtremes[1].Item3).Normalize());
            var numExtremes = possibleExtremes.Count;
            while (possibleExtremes.Count > 2)
            {
                for (int i = possibleExtremes.Count - 1; i > 0; i--)
                {
                    (var point, var v1, var v2) = possibleExtremes[i];
                    for (int j = i - 1; j >= 0; j--)
                    {
                        (var otherPoint, var w1, var w2) = possibleExtremes[j];
                        if (point.IsPracticallySame(otherPoint))
                        {
                            possibleExtremes.RemoveAt(i);
                            break;
                        }
                        var iSeesJ = PointASeesPointB(point, v1, v2, otherPoint);
                        var jSeesI = PointASeesPointB(otherPoint, w1, w2, point);
                        if (iSeesJ == jSeesI) continue;
                        if (jSeesI)
                        {
                            possibleExtremes.RemoveAt(i);
                            break;
                        }
                        else // if (iSeesJ)
                        {
                            i--;
                            possibleExtremes.RemoveAt(j);
                        }
                        //if (possibleExtremes.Count == 2) break;
                    }
                    //if (possibleExtremes.Count == 2) break;
                }
                if (possibleExtremes.Count <= 2) break;
                // more than 2 - then rework the possibleExtremes with connections to the nearest neighbors
                // (here, were are assuming the number is super small like 3 to 6)
                for (int i = possibleExtremes.Count - 1; i >= 0; i--)
                {
                    (var point, _, _) = possibleExtremes[i];
                    var closestVector1 = Vector2.Null;
                    var closestVector2 = Vector2.Null;
                    var closestDistance1 = double.PositiveInfinity;
                    var closestDistance2 = double.PositiveInfinity;
                    for (int j = 0; j < possibleExtremes.Count; j++)
                    {
                        if (i == j) continue;
                        (var otherPoint, _, _) = possibleExtremes[j];
                        var v = otherPoint - point;
                        var distance = v.LengthSquared();
                        if (distance < closestDistance1)
                        {
                            closestDistance2 = closestDistance1;
                            closestVector2 = closestVector1;
                            closestDistance1 = distance;
                            closestVector1 = v;
                        }
                        else if (distance < closestDistance2)
                        {
                            closestDistance2 = distance;
                            closestVector2 = v;
                        }
                    }
                    if (closestVector1.Dot(closestVector2) > 0)
                        possibleExtremes[i] = (point, closestVector1, closestVector2);
                    else possibleExtremes.RemoveAt(i);
                    if (possibleExtremes.Count <= 2) break;
                }
                if (numExtremes > 2 && numExtremes == possibleExtremes.Count)
                {   // This means that we've tried twice to reduce from n (some number g.t. 2) but failed.
                    // As a last ditch effort just remove all points but two which are farthest from the COM.
                    while (possibleExtremes.Count > 2)
                    {
                        var com = Vector2.Zero;
                        foreach (var (point, _, _) in possibleExtremes)
                            com += point;
                        com /= possibleExtremes.Count;
                        var minDistanceSqd = double.PositiveInfinity;
                        var indexToRemove = -1;
                        for (int i = 0; i < possibleExtremes.Count; i++)
                        {
                            var dSqd = (possibleExtremes[i].Item1 - com).LengthSquared();
                            if (dSqd < minDistanceSqd)
                            {
                                minDistanceSqd = dSqd;
                                indexToRemove = i;
                            }
                        }
                        possibleExtremes.RemoveAt(indexToRemove);
                    }
                }
                numExtremes = possibleExtremes.Count;
            }
            if (possibleExtremes.Count == 0)
                throw new Exception("No points found at the extremes of the curve.");
            if (possibleExtremes.Count == 1)
                return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                   Vector2.Null, Vector2.Null);
            else
                return (possibleExtremes[0].Item1, (possibleExtremes[0].Item2 + possibleExtremes[0].Item3).Normalize(),
                       possibleExtremes[1].Item1, (possibleExtremes[1].Item2 + possibleExtremes[1].Item3).Normalize());
        }

        /// <summary>
        /// small helper function to the above. by "sees" we mean that the point B is within 90-degrees of one of the
        /// two vector edges leaving A
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="vA1"></param>
        /// <param name="vA2"></param>
        /// <param name="pointB"></param>
        /// <returns></returns>
        private static bool PointASeesPointB(Vector2 pointA, Vector2 vA1, Vector2 vA2, Vector2 pointB)
        {
            var vNew = pointB - pointA;
            if (vNew.LengthSquared() < Constants.BaseTolerance * Constants.BaseTolerance) return false;
            return (vA1.IsNull() || vNew.Dot(vA1) >= 0) && (vA2.IsNull() || vNew.Dot(vA2) >= 0);
        }

        /// <summary>
        /// Trims the tessellation of the surface by removing faces and edges associated with vertices that have
        /// negative distance values, retaining only those with positive or zero distances. New vertices, edges,
        /// and faces are made at the boundarys where edges cross from negative to non-negative distances.
        /// </summary>
        /// <remarks>This method modifies the input surface in place. After execution, only the portions
        /// of the surface associated with non-negative vertex distances remain. The method is typically used to clip or
        /// trim a mesh based on a scalar field or distance function.</remarks>
        /// <param name="surface">The primitive surface whose tessellation will be modified to exclude faces and edges with negative vertex
        /// distances.</param>
        /// <param name="vertexDistances">A mapping of vertices to their corresponding distance values. Vertices with negative values are considered
        /// for removal from the tessellation.</param>
        /// <exception cref="Exception">Thrown if an unexpected case occurs where two edges are removed from a triangle, which indicates an invalid
        /// tessellation state.</exception>
        public static void TrimTessellationToPositiveVertices(this PrimitiveSurface surface,
            IDictionary<Vertex, double> vertexDistances)
        {
            var edgesToRemove = new HashSet<Edge>(); // we keep track of edges to remove to remove their
                                                     // reference from the vertices later
            var alreadyModifiedEdges = new HashSet<Edge>(); // this is to avoid modifying the same edge twice
                                                            // these are the edges that are crossing the zero distance
            var facesToRemove = new HashSet<TriangleFace>(); // these are the faces that have at least one negative vertex
                                                             // most are replaced by facesToAdd below (unless they are all negative)
                                                             // we keep track of these to remove their references from the vertices later
            var facesToAdd = new List<TriangleFace>();  // added to the primitive surface with the existing in SetFacesAndVertices
            foreach (var face in surface.Faces)
            {
                if (!vertexDistances.TryGetValue(face.A, out var aDist))
                    aDist = double.MinValue;
                if (!vertexDistances.TryGetValue(face.B, out var bDist))
                    bDist = double.MinValue;
                if (!vertexDistances.TryGetValue(face.C, out var cDist))
                    cDist = double.MinValue;
                if (aDist >= 0 && bDist >= 0 && cDist >= 0)
                    continue; // all positive, so keep face as is
                facesToRemove.Add(face);
                var thisPatchRemovedEdges = new List<Edge>();
                if (aDist < 0)
                {
                    if (bDist < 0) // then edge AB is below and is to be removed
                    {
                        edgesToRemove.Add(face.AB);
                        thisPatchRemovedEdges.Add(face.AB);
                    }
                    else
                    {  // edge AB is crossing, and B is above, so replace
                        if (alreadyModifiedEdges.Add(face.AB))
                            ReplaceNegativeVertexOnEdge(face.AB, face.A, face.B, aDist, bDist);
                    }
                    if (cDist < 0)
                    {
                        edgesToRemove.Add(face.CA);
                        thisPatchRemovedEdges.Add(face.CA);
                    }
                    else
                    {  // edge CA is crossing, and C is above, so replace
                        if (alreadyModifiedEdges.Add(face.CA))
                            ReplaceNegativeVertexOnEdge(face.CA, face.A, face.C, aDist, cDist);
                    }
                }
                if (bDist < 0)
                {
                    // edge AB already handled above if both are negative, just need to check
                    // if A is positive and the edge is to be modified
                    if (aDist >= 0 && alreadyModifiedEdges.Add(face.AB))
                        ReplaceNegativeVertexOnEdge(face.AB, face.B, face.A, bDist, aDist);
                    if (cDist < 0)
                    {
                        edgesToRemove.Add(face.BC);
                        thisPatchRemovedEdges.Add(face.BC);
                    }
                    else
                    {  // edge BC is crossing, and C is above, so replace
                        if (alreadyModifiedEdges.Add(face.BC))
                            ReplaceNegativeVertexOnEdge(face.BC, face.B, face.C, bDist, cDist);
                    }
                }
                if (cDist < 0)
                {
                    // now the three edge removals would have happened in the above
                    // conditions, so here just need to check if A or B are positive
                    // and the edge is to be modified
                    if (aDist >= 0 && alreadyModifiedEdges.Add(face.CA))
                        ReplaceNegativeVertexOnEdge(face.CA, face.C, face.A, cDist, aDist);
                    if (bDist >= 0 && alreadyModifiedEdges.Add(face.BC))
                        ReplaceNegativeVertexOnEdge(face.BC, face.C, face.B, cDist, bDist);
                }
                if (thisPatchRemovedEdges.Count == 3)
                    continue; // entire face is removed
                if (thisPatchRemovedEdges.Count == 2)
                    throw new Exception("This case should not happen - two edges removed from a triangle implies all three vertices are negative.");
                if (thisPatchRemovedEdges.Count == 1)
                {  // one edge removed, so create one new face
                    var orderedFaceVerts = GetVerticesFromModifyEdges(face, thisPatchRemovedEdges[0], out var oldVertIndex);
                    var newFace = new TriangleFace(orderedFaceVerts.Take(3), true);
                    facesToAdd.Add(newFace);
                    if (oldVertIndex == 1)
                        newFace.AddEdge(new Edge(newFace.C, newFace.A, newFace, null, true));
                    else
                        newFace.AddEdge(new Edge(newFace.B, newFace.C, newFace, null, true));
                    if (face.AB != thisPatchRemovedEdges[0])
                        newFace.AddEdge(face.AB);
                    if (face.BC != thisPatchRemovedEdges[0])
                        newFace.AddEdge(face.BC);
                    if (face.CA != thisPatchRemovedEdges[0])
                        newFace.AddEdge(face.CA);
                }
                else //if (thisPatchRemovedEdges.Count==0)
                {
                    // no edges removed, so must be two crossing edges, so create two new faces
                    // find the one edge that is kept
                    var orderedFaceVerts = GetVerticesFromModifyEdges(face, null, out var oldVertIndex);
                    var newFace = new TriangleFace(orderedFaceVerts.Take(3), true);
                    newFace.AddEdge(face.AB);
                    facesToAdd.Add(newFace);
                    var innerEdge = new Edge(newFace.C, newFace.A, newFace, null, true);
                    newFace.AddEdge(innerEdge);
                    if (oldVertIndex == 0)
                    {
                        newFace.AddEdge(new Edge(newFace.B, newFace.C, newFace, null, true));
                        newFace = new TriangleFace(orderedFaceVerts.Skip(2), true);
                        facesToAdd.Add(newFace);
                        newFace.AddEdge(face.BC);
                    }
                    else if (oldVertIndex == 1)
                    {
                        newFace.AddEdge(face.BC);
                        newFace = new TriangleFace(orderedFaceVerts.Skip(2), true);
                        facesToAdd.Add(newFace);
                        newFace.AddEdge(new Edge(newFace.A, newFace.B, newFace, null, true));
                    }
                    else // oldVertIndex ==2
                    {
                        newFace.AddEdge(face.BC);
                        newFace = new TriangleFace(orderedFaceVerts.Skip(2).Concat([orderedFaceVerts[0]]), true);
                        facesToAdd.Add(newFace);
                        newFace.AddEdge(new Edge(newFace.B, newFace.C, newFace, null, true));
                    }
                    newFace.AddEdge(face.CA);
                    newFace.AddEdge(innerEdge);
                }
                foreach (var f in facesToAdd)
                    if (f.BC == null) ;
            }
            // edgesToRemove ,facesToRemove,facesToAdd
            surface.Faces.ExceptWith(facesToRemove);
            surface.SetFacesAndVertices(surface.Faces.Concat(facesToAdd), true);
            surface.DefineInnerOuterEdges();
            foreach (var removeFace in facesToRemove)
                foreach (var v in surface.Vertices)
                    v.Faces.Remove(removeFace);
            foreach (var removeEdge in edgesToRemove)
                foreach (var v in surface.Vertices)
                    v.Edges.Remove(removeEdge);
        }

        private static List<Vertex> GetVerticesFromModifyEdges(TriangleFace face, Edge removedEdge, out int oldVertIndex)
        {
            var result = new List<Vertex>();
            oldVertIndex = 0; // start with the old vertex (as an assumption)
            foreach (var edge in face.Edges)
            {
                if (edge == removedEdge) continue;
                if (edge.OwnedFace == face)
                {
                    if (result.Count > 0 && result[^1] == edge.From)
                        oldVertIndex = (result.Count - 1) % 3;
                    else result.Add(edge.From);
                    result.Add(edge.To);
                }
                else
                {
                    if (result.Count > 0 && result[^1] == edge.To)
                        oldVertIndex = (result.Count - 1) % 3;
                    else result.Add(edge.To);
                    result.Add(edge.From);
                }
            }
            return result;
        }

        private static Vertex ReplaceNegativeVertexOnEdge(Edge edge, Vertex negV, Vertex posV, double negDist, double posDist)
        {
            var newVertex = new Vertex(posV.Coordinates + (negV.Coordinates - posV.Coordinates) *
                (posDist / (posDist - negDist)));
            if (edge.To == negV) edge.To = newVertex;
            else edge.From = newVertex;
            return newVertex;
        }

        #region Tessellation of PrimitiveSurfaces
        /// <summary>
        /// A generic tessellation of a primitive surface using marching cubes.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <param name="zMin"></param>
        /// <param name="zMax"></param>
        /// <param name="maxEdgeLength"></param>
        public static TessellatedSolid Tessellate(this PrimitiveSurface surface, double xMin, double xMax, double yMin, double yMax, double zMin, double zMax, double maxEdgeLength)
        {
            var tessellatedSolid = TessellateToNewSolid(surface, xMin, xMax, yMin, yMax, zMin, zMax, maxEdgeLength);
            surface.Faces = tessellatedSolid.Primitives[0].Faces;
            surface.Vertices = tessellatedSolid.Primitives[0].Vertices;
            tessellatedSolid.MakeEdgesIfNonExistent();
            return tessellatedSolid;
        }
        /// <summary>
        /// A generic tessellation of a primitive surface using marching cubes.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="xMin"></param>
        /// <param name="xMax"></param>
        /// <param name="yMin"></param>
        /// <param name="yMax"></param>
        /// <param name="zMin"></param>
        /// <param name="zMax"></param>
        /// <param name="maxEdgeLength"></param>
        public static TessellatedSolid TessellateToNewSolid(this PrimitiveSurface surface, double xMin, double xMax, double yMin, double yMax, double zMin, double zMax, double maxEdgeLength)
        {
            var meshSize = maxEdgeLength / Math.Sqrt(3);
            var surfaceCopy = surface.Copy(null);
            var solid = new ImplicitSolid(surfaceCopy);
            solid.Bounds = [new Vector3(xMin, yMin, zMin), new Vector3(xMax, yMax, zMax)];
            var tessellatedSolid = solid.ConvertToTessellatedSolid(meshSize);
            surfaceCopy.SetFacesAndVertices(tessellatedSolid.Faces, true);
            tessellatedSolid.AddPrimitive(surfaceCopy);
            return tessellatedSolid;
        }
        /// <summary>
        /// A generic tessellation of a primitive surface using marching cubes.
        /// </summary>
        /// <param name="surface"></param>
        /// <param name="maxEdgeLength"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static TessellatedSolid Tessellate(this PrimitiveSurface surface, double maxEdgeLength = double.NaN)
        {
            //if (surface.Vertices != null && surface.Vertices.Count > 0) return null;
            surface.SetBounds();
            if (double.IsFinite(surface.MaxX) && double.IsFinite(surface.MaxY) && double.IsFinite(surface.MaxZ) &&
                 double.IsFinite(surface.MinX) && double.IsFinite(surface.MinY) && double.IsFinite(surface.MinZ))
            {
                if (double.IsNaN(maxEdgeLength))
                {
                    var diagonal = new Vector3(surface.MaxX - surface.MinX, surface.MaxY - surface.MinY, surface.MaxZ - surface.MinZ);
                    maxEdgeLength = 0.033 * diagonal.Length();
                }
                return Tessellate(surface, surface.MinX, surface.MaxX, surface.MinY, surface.MaxY, surface.MinZ, surface.MaxZ, maxEdgeLength);
            }
            else throw new ArgumentOutOfRangeException("The provided primitive is" +
                "unbounded in size. Please invoke the overload of this method that accepts coordinate limits");
        }


        public static VoxelizedSolid Voxelize(this PrimitiveSurface surface, VoxelizedSolid environment, bool treatAsSolid = false)
        {
            var result = VoxelizedSolid.CreateEmpty(environment);
            var minIndices = result.ConvertCoordinatesToIndices(new Vector3(surface.MinX, surface.MinY, surface.MinZ));
            var maxIndices = result.ConvertCoordinatesToIndices(new Vector3(surface.MaxX, surface.MaxY, surface.MaxZ));
            var minJ = minIndices[1];
            var maxJ = Math.Min(result.numVoxelsY, maxIndices[1]);
            var minK = minIndices[2];
            var maxK = Math.Min(result.numVoxelsZ, maxIndices[2]);


            //Parallel.For(minK, maxK, k =>
            for (var k = minK; k < maxK; k++)
            {
                var zCoord = result.ConvertZIndexToCoord(k);
                for (int j = minJ; j < maxJ; j++)
                {
                    var yCoord = result.ConvertYIndexToCoord(j);
                    foreach (var intersection in GetPrimitiveAndLineIntersections(surface, result.XMin, yCoord, zCoord))
                    {
                        if (intersection.lineT < 0 || intersection.lineT > result.XMax) continue;
                        //if (treatAsSolid)
                        var indices = result.ConvertCoordinatesToIndices(intersection.intersection);
                        result[indices] = true;
                    }
                }
            } //);
            return result;
        }

        /// <summary>
        /// Creates a the faces and vertices for a circle positioned in 3D space at circleCenter with normalDirection as the outward normal
        /// and the given radius. The number of points in the circle is determined by numPoints.
        /// </summary>
        /// <param name="vertices"></param>
        /// <param name="faces"></param>
        /// <param name="circleCenter"></param>
        /// <param name="normalDirection"></param>
        /// <param name="radius"></param>
        /// <param name="numPoints"></param>
        internal static void GetCircleTessellation(out Vertex[] vertices, out TriangleFace[] faces,
            Vector3 circleCenter, Vector3 normalDirection, double radius, int numPoints, bool reverseFaces)
        {
            var cosAxis = normalDirection.GetPerpendicularDirection();
            var sinAxis = normalDirection.Cross(cosAxis);
            vertices = new Vertex[numPoints + 1];
            var centerVertex = new Vertex(circleCenter);
            vertices[numPoints] = centerVertex;

            for (int i = 0; i < numPoints; i++)
            {
                var angle = i * 2 * Math.PI / numPoints;
                var coord = circleCenter + radius * (cosAxis * Math.Cos(angle) + sinAxis * Math.Sin(angle));
                vertices[i] = new Vertex(coord);
            }

            faces = new TriangleFace[numPoints];
            var j = numPoints - 1;
            if (reverseFaces)
                for (int i = 0; i < numPoints; i++)
                {
                    faces[i] = new TriangleFace(centerVertex, vertices[i], vertices[j]);
                    j = i;
                }
            else
                for (int i = 0; i < numPoints; i++)
                {
                    faces[i] = new TriangleFace(centerVertex, vertices[j], vertices[i]);
                    j = i;
                }
            for (int i = 0; i < faces.Length; i++)
                faces[i].IndexInList = i;
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cone cone, double maxEdgeLength, bool keepOpen = false)
        {
            if (double.IsInfinity(cone.Length))
                throw new ArgumentException("The cone must have finite a finite length.");
            var baseCircleRadius = cone.Length * cone.Aperture;
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * baseCircleRadius * baseCircleRadius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return Tessellate(cone, numPoints, keepOpen);
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="numPoints"></param>
        /// <param name="keepOpen">if true then no triangles are added for the base circle</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cone cone, int numPoints = 30, bool keepOpen = false)
        {
            if (double.IsInfinity(cone.Length))
                throw new ArgumentException("The cone must have a finite length.");
            var axis = cone.Axis.Normalize();
            var baseCircleRadius = cone.Length * cone.Aperture;
            var baseCircleCenter = cone.Apex + cone.Length * axis;

            GetCircleTessellation(out var btmVertices, out var btmFaces, baseCircleCenter, axis, baseCircleRadius, numPoints, false);
            List<Vertex> vertices;
            List<TriangleFace> faces;
            Plane btmPlane = null;
            var apexVertex = new Vertex(cone.Apex);
            if (keepOpen)
            {
                vertices = btmVertices.SkipLast(1).Concat([apexVertex]).ToList();
                faces = new List<TriangleFace>();
            }
            else
            {
                vertices = btmVertices.Concat([apexVertex]).ToList();
                faces = btmFaces.ToList();
                btmPlane = new Plane(cone.Apex + axis * cone.Length, axis);
                btmPlane.SetFacesAndVertices(btmFaces, true, true);
            }

            var j = numPoints - 1;
            var coneFaces = new List<TriangleFace>();
            for (int i = 0; i < numPoints; i++)
            {
                coneFaces.Add(new TriangleFace(apexVertex, btmVertices[i], btmVertices[j]));
                j = i;
            }
            cone.SetFacesAndVertices(coneFaces);
            faces.AddRange(coneFaces);
            for (int i = 0; i < faces.Count; i++)
                faces[i].IndexInList = i;
            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor = false };
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = keepOpen ? [cone] : [btmPlane, cone]
            };
        }

        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cylinder cylinder, double maxEdgeLength, bool keepOpen = false)
        {
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * cylinder.Radius * cylinder.Radius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return Tessellate(cylinder, numPoints, keepOpen);
        }

        /// <summary>
        /// Create a tessellated solid for a cylinder. Long skinny triangles are created from one end to the other.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <param name="numPoints"></param>
        /// <param name="keepOpen">if true then no triangles are added for the circles on the ends</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid Tessellate(this Cylinder cylinder, int numPoints = 30, bool keepOpen = false)
        {
            if (double.IsInfinity(cylinder.MinDistanceAlongAxis) || double.IsInfinity(cylinder.MaxDistanceAlongAxis))
                throw new ArgumentException("The cylinder must have finite min and max distances along the axis.");
            var axis = cylinder.Axis.Normalize();
            var anchorDist = cylinder.Anchor.Dot(axis);
            var cylinderMinAxisPoint = cylinder.Anchor + (cylinder.MinDistanceAlongAxis - anchorDist) * axis;
            var cylinderMaxAxisPoint = cylinderMinAxisPoint + (cylinder.MaxDistanceAlongAxis - cylinder.MinDistanceAlongAxis) * axis;

            GetCircleTessellation(out var btmVertices, out var btmFaces, cylinderMinAxisPoint, axis, cylinder.Radius, numPoints, true);
            GetCircleTessellation(out var topVertices, out var topFaces, cylinderMaxAxisPoint, axis, cylinder.Radius, numPoints, false);
            List<Vertex> vertices;
            List<TriangleFace> faces;
            if (keepOpen)
            {
                vertices = btmVertices.SkipLast(1).Concat(topVertices.SkipLast(1)).ToList();
                faces = new List<TriangleFace>();
            }
            else
            {
                vertices = btmVertices.Concat(topVertices).ToList();
                faces = btmFaces.Concat(topFaces).ToList();
            }

            var sideFaces = new List<TriangleFace>();
            var j = numPoints - 1;
            for (int i = 0; i < numPoints; i++)
            {
                sideFaces.Add(new TriangleFace(btmVertices[i], topVertices[i], topVertices[j]));
                sideFaces.Add(new TriangleFace(btmVertices[i], topVertices[j], btmVertices[j]));
                j = i;
            }
            cylinder.SetFacesAndVertices(sideFaces, true, true);
            faces.AddRange(sideFaces);
            for (int i = 0; i < faces.Count; i++)
                faces[i].IndexInList = i;
            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor = false };
            if (keepOpen)
            {
                return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
                {
                    Primitives = [cylinder]
                };
            }
            var btmPlane = new Plane(cylinderMinAxisPoint, -axis);
            btmPlane.SetFacesAndVertices(btmFaces, true, true);
            var topPlane = new Plane(cylinderMaxAxisPoint, axis);
            topPlane.SetFacesAndVertices(topFaces, true, true);
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [btmPlane, topPlane, cylinder]
            };
        }


        /// <summary>
        /// Create a tessellated solid for a cone. Long skinny triangles are created from the apex to the base circle.
        /// </summary>
        /// <param name="cone"></param>
        /// <param name="maxEdgeLength"></param>
        /// <param name="keepOpen"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid TessellateHollowCylinder(this Cylinder cylinder, double innerRadius, double maxEdgeLength)
        {
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * innerRadius * innerRadius));
            var numPoints = (int)(2 * Math.PI / maxAngle);
            return TessellateHollowCylinder(cylinder, innerRadius, numPoints);
        }

        /// <summary>
        /// Creates a tessellated solid for a tube or hollow cylinder.
        /// </summary>
        /// <param name="cylinder"></param>
        /// <param name="innerRadius"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static TessellatedSolid TessellateHollowCylinder(this Cylinder cylinder, double innerRadius, int numPoints = 30)
        {
            if (double.IsInfinity(cylinder.MinDistanceAlongAxis) || double.IsInfinity(cylinder.MaxDistanceAlongAxis))
                throw new ArgumentException("The cylinder must have finite min and max distances along the axis.");
            if (innerRadius >= cylinder.Radius)
                throw new ArgumentException("The inner radius must be smaller than the cylinder's given (outer) radius.");
            var centerDot = cylinder.Axis.Dot(cylinder.Anchor);
            var bottomCenter = cylinder.Anchor + (centerDot - cylinder.MinDistanceAlongAxis) * cylinder.Axis;
            var topCenter = cylinder.Anchor + (centerDot - cylinder.MaxDistanceAlongAxis) * cylinder.Axis;
            var cosAxis = cylinder.Axis.GetPerpendicularDirection();
            var sinAxis = cylinder.Axis.Cross(cosAxis);
            var outerRadius = cylinder.Radius;
            var btmInnerVertices = new Vertex[numPoints];
            var btmOuterVertices = new Vertex[numPoints];
            var topInnerVertices = new Vertex[numPoints];
            var topOuterVertices = new Vertex[numPoints];
            for (int i = 0; i < numPoints; i++)
            {
                var angle = i * 2 * Math.PI / numPoints;
                var cosAngle = Math.Cos(angle);
                var sinAngle = Math.Sin(angle);
                var radialVector = cosAxis * cosAngle + sinAxis * sinAngle;
                var innerVector = innerRadius * radialVector;
                var outerVector = outerRadius * radialVector;
                btmInnerVertices[i] = new Vertex(bottomCenter + innerVector);
                btmOuterVertices[i] = new Vertex(bottomCenter + outerVector);
                topInnerVertices[i] = new Vertex(topCenter + innerVector);
                topOuterVertices[i] = new Vertex(topCenter + outerVector);
            }
            var bottomFaces = new List<TriangleFace>();
            var topFaces = new List<TriangleFace>();
            var innerFaces = new List<TriangleFace>();
            var outerFaces = new List<TriangleFace>();
            var j = numPoints - 1;
            for (int i = 0; i < numPoints; i++)
            {
                bottomFaces.Add(new TriangleFace(btmInnerVertices[j], btmOuterVertices[j], btmOuterVertices[i]));
                bottomFaces.Add(new TriangleFace(btmInnerVertices[j], btmOuterVertices[i], btmInnerVertices[i]));
                topFaces.Add(new TriangleFace(topOuterVertices[j], topInnerVertices[j], topInnerVertices[i]));
                topFaces.Add(new TriangleFace(topOuterVertices[j], topInnerVertices[i], topOuterVertices[i]));
                outerFaces.Add(new TriangleFace(btmOuterVertices[j], topOuterVertices[j], topOuterVertices[i]));
                outerFaces.Add(new TriangleFace(btmOuterVertices[j], topOuterVertices[i], btmOuterVertices[i]));
                innerFaces.Add(new TriangleFace(btmInnerVertices[i], topInnerVertices[i], topInnerVertices[j]));
                innerFaces.Add(new TriangleFace(btmInnerVertices[i], topInnerVertices[j], btmInnerVertices[j]));
                j = i;
            }

            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions();
            tessellatedSolidBuildOptions.CopyElementsPassedToConstructor = false;
            var faces = bottomFaces.Concat(topFaces).Concat(outerFaces).ToArray();
            var vertices = btmInnerVertices.Concat(btmOuterVertices).Concat(
                topInnerVertices).Concat(topOuterVertices).ToArray();
            var btmPlane = new Plane(cylinder.MinDistanceAlongAxis, cylinder.Axis);
            btmPlane.SetFacesAndVertices(bottomFaces, true, true);
            var topPlane = new Plane(cylinder.MaxDistanceAlongAxis, cylinder.Axis);
            topPlane.SetFacesAndVertices(topFaces, true, true);
            cylinder.SetFacesAndVertices(outerFaces, true, true);
            var innerCylinder = new Cylinder
            {
                Axis = cylinder.Axis,
                Anchor = cylinder.Anchor,
                Radius = innerRadius,
                MinDistanceAlongAxis = cylinder.MinDistanceAlongAxis,
                MaxDistanceAlongAxis = cylinder.MaxDistanceAlongAxis,
                IsPositive = false
            };
            innerCylinder.SetFacesAndVertices(innerFaces, true, true);

            for (int i = 0; i < faces.Length; i++)
                faces[i].IndexInList = i;
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [btmPlane, topPlane, cylinder, innerCylinder]
            };
        }


        /// <summary>
        /// Creates a tessellated solid for a sphere by attempting to equally space the points.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TessellatedSolid Tessellate(this Sphere sphere, double maxEdgeLength)
        {
            // assuming equilateral triangles are spread over sphere (which is a conservative assumption, since
            // equilateral triangles will have the most area)
            var equiTriArea = 0.25 * maxEdgeLength * maxEdgeLength * Math.Sqrt(3);
            var numTriangles = (int)(Math.PI * sphere.Radius * sphere.Radius / equiTriArea); // wait! isn't there a 4 in 
                                                                                             // the formula for the area of a sphere ? yes, but after some (brief) testing, it seems that this 
                                                                                             // overall heuristic is off by a factor of 4. So, we'll just leave it out.
            return Tessellate(sphere, numTriangles - 2);
        }

        /// <summary>
        /// Creates a tessellated solid for a sphere by attempting to equally space the points.
        /// </summary>
        /// <param name="sphere"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TessellatedSolid Tessellate(this Sphere sphere, int numPoints = 100)
        {
            var vertices = MiscFunctions.NEquidistantSpherePointsKogan(numPoints, sphere.Radius).Select(p => new Vertex(p + sphere.Center)).ToList();
            if (!ConvexHull3D.Create(vertices, out var convexHull, false, false))
                throw new Exception("Convex hull could not be created for sphere.");
            var faces = convexHull.Faces.Select(cf => new TriangleFace(cf.A, cf.B, cf.C)).ToList();
            for (int i = 0; i < faces.Count; i++)
                faces[i].IndexInList = i;

            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions();
            tessellatedSolidBuildOptions.CopyElementsPassedToConstructor = false;
            sphere.SetFacesAndVertices(faces, true, true);
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [sphere]
            };
        }


        /// <summary>
        /// Creates a tessellated solid for a torus which is determined by rings of points.
        /// </summary>
        /// <param name="torus"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TessellatedSolid Tessellate(this Torus torus, double maxEdgeLength)
        {
            var r = torus.MajorRadius + torus.MinorRadius;
            // using law of cosines
            var maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * r * r));
            var numPointsInMajor = (int)(2 * Math.PI / maxAngle);
            r = torus.MinorRadius;
            maxAngle = Math.Acos(1 - maxEdgeLength * maxEdgeLength / (2 * r * r));
            var numPointsInMinor = (int)(2 * Math.PI / maxAngle);
            return Tessellate(torus, numPointsInMajor, numPointsInMinor);
        }

        /// <summary>
        /// Creates a tessellated solid for a torus which is determined by rings of points.
        /// </summary>
        /// <param name="torus"></param>
        /// <param name="numPoints"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static TessellatedSolid Tessellate(this Torus torus, int numPointsInMajor = 30, int numPointsInMinor = 30)
        {
            var vertices = new Vertex[numPointsInMinor * numPointsInMajor];
            var cosMajorAxis = torus.Axis.GetPerpendicularDirection();
            var sinMajorAxis = torus.Axis.Cross(cosMajorAxis);
            var centerPoints = new Vector3[numPointsInMajor];
            for (int i = 0; i < numPointsInMajor; i++)
            {
                var angle = i * 2 * Math.PI / numPointsInMajor;
                var centerPoint = torus.Center + torus.MajorRadius * (cosMajorAxis * Math.Cos(angle) + sinMajorAxis * Math.Sin(angle));
                var sinMinorAxis = torus.Axis;
                var cosMinorAxis = (centerPoint - torus.Center).Normalize();

                for (int j = 0; j < numPointsInMinor; j++)
                {
                    angle = j * 2 * Math.PI / numPointsInMinor;
                    vertices[i * numPointsInMinor + j] = new Vertex(centerPoint + torus.MinorRadius
                        * (cosMinorAxis * Math.Cos(angle) + sinMinorAxis * Math.Sin(angle)));
                }
            }
            var faces = new TriangleFace[2 * numPointsInMinor * numPointsInMajor];
            var prevI = numPointsInMajor - 1;
            var k = 0;
            for (int i = 0; i < numPointsInMajor; i++)
            {
                var prevJ = numPointsInMinor - 1;
                for (int j = 0; j < numPointsInMinor; j++)
                {
                    faces[k++] = new TriangleFace(vertices[i * numPointsInMinor + j],
                        vertices[prevI * numPointsInMinor + j],
                        vertices[prevI * numPointsInMinor + prevJ]);
                    faces[k++] = new TriangleFace(vertices[i * numPointsInMinor + j],
                        vertices[prevI * numPointsInMinor + prevJ],
                        vertices[i * numPointsInMinor + prevJ]);
                    prevJ = j;
                }
                prevI = i;
            }
            for (int i = 0; i < faces.Length; i++)
                faces[i].IndexInList = i;
            var tessellatedSolidBuildOptions = new TessellatedSolidBuildOptions { CopyElementsPassedToConstructor = false };
            torus.SetFacesAndVertices(faces, true, true);
            return new TessellatedSolid(faces, vertices, tessellatedSolidBuildOptions)
            {
                Primitives = [torus]
            };
        }



        #endregion

        private static IEnumerable<(Vector3 intersection, double lineT)> GetPrimitiveAndLineIntersections(PrimitiveSurface surface, double xCoord,
         double yCoord, double zCoord)
        {
            if (surface.Faces == null || surface.Faces.Count == 0)
                foreach (var result in surface.LineIntersection(new Vector3(xCoord, yCoord, zCoord), Vector3.UnitX))
                    yield return result;
            else
            {
                foreach (var face in surface.Faces)
                {
                    var intersectPoint = MiscFunctions.PointOnTriangleFromRay(face, new Vector3(0, yCoord, zCoord),
                        Vector3.UnitX, out var t);
                    if (!intersectPoint.IsNull())
                        yield return (intersectPoint, t);
                }
            }
        }
    }
}