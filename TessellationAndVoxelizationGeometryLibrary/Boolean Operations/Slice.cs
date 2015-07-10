// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 03-05-2015
// ***********************************************************************
// <copyright file="Slice.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// </summary>
    public static class Slice
    {
        /// <summary>
        /// Performs the slicing operation on the prescribed flat plane. This destructively alters
        /// the tessellated solid into one or more solids which are returned in the "out" parameter
        /// lists.
        /// </summary>
        /// <param name="oldSolid">The old solid.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolids">The solids that are on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolids">The solids on the negative side of the plane.</param>
        public static void OnFlat(TessellatedSolid ts, Flat plane,
            out List<TessellatedSolid> positiveSideSolids, out List<TessellatedSolid> negativeSideSolids)
        {
            var contactData = DefineContact(plane, ts, false);

            DivideUpContact(ts, contactData, plane);
            var loops =
                contactData.AllLoops;
            //.Where(loop => loop.All(ce => !(ce.ContactEdge.Curvature == CurvatureType.Convex && ce is CoincidentEdgeContactElement))).ToList();
            var allNegativeStartingFaces =
               loops.SelectMany(loop => loop.Select(ce => ce.SplitFaceNegative)).ToList();
            var allPositiveStartingFaces =
                loops.SelectMany(loop => loop.Select(ce => ce.SplitFacePositive)).ToList();

            var negativeSideFaceList = FindAllSolidsWithTheseFaces(allNegativeStartingFaces, allPositiveStartingFaces);
            var positiveSideFaceList = FindAllSolidsWithTheseFaces(allPositiveStartingFaces, allNegativeStartingFaces);

            negativeSideSolids = convertFaceListsToSolids(ts, negativeSideFaceList, loops, false, plane);
            positiveSideSolids = convertFaceListsToSolids(ts, positiveSideFaceList, loops, true, plane);
        }

        #region Define Contact at a Flat Plane

        /// <summary>
        /// When the tessellated solid is sliced at the specified plane, the contact surfaces are
        /// described by the returned ContactData object. This is a non-destructive function typically
        /// used to find the shape and size of 2D surface on the prescribed plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="ts">The ts.</param>
        /// <returns>ContactData.</returns>
        /// <exception cref="System.Exception">Contact Edges found that are not contained in loop.</exception>
        public static ContactData DefineContact(Flat plane, TessellatedSolid ts, Boolean artficiallyCloseOpenLoops = true)
        {
            // Contact elements are constructed and then later arranged into loops. Loops make up the returned object, ContactData.
            // Throughout the operations in this method, the distance a given vertex is from the plane is needed. In order to avoid 
            // calculating these distances multiple times, we first construct an array of distances.
            var distancesToPlane = new double[ts.NumberOfVertices];
            for (int i = 0; i < ts.NumberOfVertices; i++)
                distancesToPlane[i] = ts.Vertices[i].Position.dotProduct(plane.Normal) - plane.DistanceToOrigin;
            var contactElements = GetContactElements(plane, ts, distancesToPlane);

            var loops = new List<Loop>();
            var numberOfTries = 0;
            while (numberOfTries < contactElements.Count)
            {
                var loop = FindLoop(contactElements, plane, distancesToPlane, artficiallyCloseOpenLoops);
                if (loop != null)
                {
                    Debug.WriteLine(loops.Count + ": " + loop.MakeDebugContactString() + "  ");
                    loops.Add(loop);
                    numberOfTries = 0;
                }
                else
                {
                    var startingEdge = contactElements[0];
                    contactElements.RemoveAt(0);
                    contactElements.Add(startingEdge);
                    numberOfTries++;
                }
            }
            if (numberOfTries > 0) Debug.WriteLine("Contact Edges found that are not contained in loop.");
            return new ContactData(loops);
        }

        private static Loop FindLoop(List<ContactElement> contactElements, Flat plane, double[] vertexDistancesToPlane, bool artficiallyCloseOpenLoops)
        {
            var thisCE = contactElements[0];
            var loop = new List<ContactElement>();
            do
            {
                loop.Add(thisCE);
                var newStartVertex = thisCE.EndVertex;
                if (loop[0].StartVertex == newStartVertex) // then a loop is found!
                {
                    if (loop.All(
                        ce =>
                            ce.ContactType == ContactTypes.AlongEdge &&
                            (vertexDistancesToPlane[ce.SplitFacePositive.OtherVertex(ce.ContactEdge).IndexInList]
                                .IsNegligible()
                             ||
                             vertexDistancesToPlane[ce.SplitFaceNegative.OtherVertex(ce.ContactEdge).IndexInList]
                                 .IsNegligible())))
                    {
                        contactElements.RemoveAll(
                            ce => loop.Contains(ce) && ce.ContactEdge.Curvature == CurvatureType.Convex);
                        return new Loop(loop, plane.Normal, true, false, true);
                    }

                    contactElements.RemoveAll(ce => loop.Contains(ce));
                    return new Loop(loop, plane.Normal, true, false, false);
                }
                var possibleNextCEs = contactElements.Where(ce => ce.StartVertex == newStartVertex).ToList();
                if (!possibleNextCEs.Any())
                    possibleNextCEs = contactElements.Where(ce => ce.StartVertex.Position.IsPracticallySame(newStartVertex.Position)).ToList();
                if (possibleNextCEs.Count == 1) thisCE = possibleNextCEs[0];
                else if (possibleNextCEs.Count > 1)
                {
                    var thisAngle = Math.Atan2(thisCE.EndVertex.Y - thisCE.StartVertex.Y,
                        thisCE.EndVertex.X - thisCE.StartVertex.X);
                    var minIndex = -1;
                    var minAngle = double.PositiveInfinity;
                    for (int i = 0; i < possibleNextCEs.Count; i++)
                    {
                        var nextAngle = Math.Atan2(possibleNextCEs[i].EndVertex.Y - possibleNextCEs[i].StartVertex.Y,
                            possibleNextCEs[i].EndVertex.X - possibleNextCEs[i].StartVertex.X);
                        var angleChange = Math.PI - (nextAngle - thisAngle);
                        if (angleChange < 0) angleChange += Math.PI;
                        if (angleChange < minAngle)
                        {
                            minAngle = angleChange;
                            minIndex = i;
                        }
                    }
                    thisCE = possibleNextCEs[minIndex];
                }
                else if (artficiallyCloseOpenLoops)
                {
                    contactElements.RemoveAll(ce => loop.Contains(ce));
                    loop.Add(new ContactElement(newStartVertex, null, loop[0].StartVertex, null, null,
                        ContactTypes.Artificial));
                    return new Loop(loop, plane.Normal, true, true, false);
                }
                else return null;
            } while (true);
        }


        private static List<ContactElement> GetContactElements(Flat plane, TessellatedSolid ts, double[] distancesToPlane)
        {
            // the edges serve as the easiest way to identify where the solid is interacting with the plane, so we search over those
            // and organize the edges (or vertices into the following three categories: edges that straddle the plane (straddleEdges),
            // edges that in on the plane (inPlaneEdges), and edges endpoints (or rather just the vertex in question) that are in the
            // plane.
            var straddleEdges = new List<Edge>();
            var inPlaneEdges = new List<Edge>();
            var inPlaneVerticesHash = new HashSet<Vertex>();  //since these will be found multiple times, in the following loop, 
            // the hash-set allows us to quickly check if the v is already included
            foreach (var edge in ts.Edges)
            {
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                if (toDistance.IsNegligible() && fromDistance.IsNegligible())
                {   // both the to and from vertices are on the plane --> inPlaneEdge
                    inPlaneEdges.Add(edge);
                    if (!inPlaneVerticesHash.Contains(edge.From)) inPlaneVerticesHash.Add(edge.From);
                    if (!inPlaneVerticesHash.Contains(edge.To)) inPlaneVerticesHash.Add(edge.To);
                }
                else if (toDistance.IsNegligible())
                {   // both ends are not, but the head of the edge is --> inPlaneVertex
                    inPlaneVerticesHash.Add(edge.To);
                    if (!inPlaneVerticesHash.Contains(edge.To)) inPlaneVerticesHash.Add(edge.To);
                }
                else if (fromDistance.IsNegligible())
                {   // both ends are not, but the tail of the edge is --> inPlaneVertex
                    inPlaneVerticesHash.Add(edge.From);
                    if (!inPlaneVerticesHash.Contains(edge.From)) inPlaneVerticesHash.Add(edge.From);
                }
                else if ((toDistance > 0 && fromDistance < 0) || (toDistance < 0 && fromDistance > 0))
                    // the to and from are on either side --> straddle edge
                    straddleEdges.Add(edge);
            }
            // the following contactElements is what is returned by this method.
            List<ContactElement> contactElements = new List<ContactElement>();
            foreach (var inPlaneEdge in inPlaneEdges)
            {   //  inPlaneEdges are the easiest to make into ContactElements, but there are some
                // subtle issues related to inner edges and convexity of the edges (as occurs later on).   
                var ownedFaceOtherVertex = inPlaneEdge.OwnedFace.OtherVertex(inPlaneEdge);
                var planeDistOwnedFOV = distancesToPlane[ownedFaceOtherVertex.IndexInList];
                var otherFaceOtherVertex = inPlaneEdge.OtherFace.OtherVertex(inPlaneEdge);
                var planeDistOtherFOV = distancesToPlane[otherFaceOtherVertex.IndexInList];
                if (planeDistOwnedFOV.IsNegligible() && planeDistOtherFOV.IsNegligible()) continue;
                if (planeDistOwnedFOV * planeDistOtherFOV > 0) continue; //if both distances have the same sign, but 
                                                                         //this is "knife-edge" on the plane
                contactElements.Add(new ContactElement(inPlaneEdge, planeDistOwnedFOV > 0));
            }
            // now things get complicated. For each straddle each make a dictionary to ensure that newly
            // defined ContactElements use the same vertices. Well, specifically any new vertices that are
            // created when a straddleEdge is split.
            // in this splitEdgeDict, the straddleEdge is the Key and the Value is a Tuple of:
            // <new vertex on the straddle edge; the backward face; the forward face> .
            // These are the faces on either side of the edge that are in the backward or forward direction of the loop.
            var splitEdgeDict = straddleEdges.ToDictionary(edge => edge,
                edge => new Tuple<Vertex, PolygonalFace, PolygonalFace>(
                    LineFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.From, edge.To), null, null));
            // next add 0,1,or 2 ContactElements for the inPlane Vertices. Why is this not known? Because many of the vertices
            // are ends of inPlaneEdges, which are defined in the previous loop.
            foreach (var startingVertex in inPlaneVerticesHash)
            {
                Edge otherEdge;
                var straddleFace = FindForwardStraddleFace(plane, startingVertex, distancesToPlane, out otherEdge);
                if (straddleFace != null)
                {
                    var connectingData = splitEdgeDict[otherEdge];
                    contactElements.Add(new ContactElement(startingVertex, null, connectingData.Item1, otherEdge, straddleFace, ContactTypes.ThroughVertex));
                    // update the dictionary entry with the fact that the face on the backward side of this forward edge has been found. A "through vertex"
                    // contact element is created for this straddle vertex.
                    splitEdgeDict[otherEdge] = new Tuple<Vertex, PolygonalFace, PolygonalFace>(connectingData.Item1, straddleFace, connectingData.Item3);
                }
                straddleFace = FindBackwardStraddleFace(plane, startingVertex, distancesToPlane, out otherEdge);
                if (straddleFace != null)
                {
                    var connectingData = splitEdgeDict[otherEdge];
                    contactElements.Add(new ContactElement(connectingData.Item1, otherEdge, startingVertex, null, straddleFace, ContactTypes.ThroughVertex));
                    splitEdgeDict[otherEdge] = new Tuple<Vertex, PolygonalFace, PolygonalFace>(connectingData.Item1, connectingData.Item2, straddleFace);
                }
            }
            foreach (var keyValuePair in splitEdgeDict)
            {   // finally, we make ContactElements for the straddleEdges. This is the trickiest part.
                var edge = keyValuePair.Key;
                var newVertex = keyValuePair.Value.Item1;
                var backwardFace = edge.OwnedFace;
                var forwardFace = edge.OtherFace;
                if (distancesToPlane[edge.To.IndexInList] < 0)
                {   // whoops! the assignment should be reversed, given that the head of the arc
                    // is on the negative side of the plane and not the positive
                    backwardFace = edge.OtherFace;
                    forwardFace = edge.OwnedFace;
                }
                if (keyValuePair.Value.Item2 == null)
                {
                    var otherEdge =
                        backwardFace.Edges.First(
                            e =>
                                e != edge &&
                                ((distancesToPlane[e.To.IndexInList] < 0 &&
                                  distancesToPlane[e.From.IndexInList] > 0)
                                 ||
                                 (distancesToPlane[e.To.IndexInList] > 0 &&
                                  distancesToPlane[e.From.IndexInList] < 0)));
                    contactElements.Add(new ContactElement(splitEdgeDict[otherEdge].Item1, otherEdge, newVertex, edge, backwardFace, ContactTypes.ThroughFace));
                }
                if (keyValuePair.Value.Item3 == null)
                {
                    var otherEdge =
                        forwardFace.Edges.First(
                            e =>
                                e != edge &&
                                ((distancesToPlane[e.To.IndexInList] < 0 &&
                                  distancesToPlane[e.From.IndexInList] > 0)
                                 ||
                                 (distancesToPlane[e.To.IndexInList] > 0 &&
                                  distancesToPlane[e.From.IndexInList] < 0)));
                    contactElements.Add(new ContactElement(newVertex, edge, splitEdgeDict[otherEdge].Item1, otherEdge, forwardFace, ContactTypes.ThroughFace));
                }
            }
            return contactElements;
        }

        internal static PolygonalFace FindForwardStraddleFace(Flat plane, Vertex onPlaneVertex, double[] vertexDistancesToPlane, out Edge edge)
        {
            edge = null;
            foreach (var face in onPlaneVertex.Faces)
            {
                var otherEdge = face.OtherEdge(onPlaneVertex);
                var toDistance = vertexDistancesToPlane[otherEdge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[otherEdge.From.IndexInList];
                if ((toDistance > 0 && fromDistance < 0 && face == otherEdge.OwnedFace)
                    || (toDistance < 0 && fromDistance > 0 && face == otherEdge.OtherFace))
                {
                    edge = otherEdge;
                    return face;
                }
            }
            return null;
        }
        internal static PolygonalFace FindBackwardStraddleFace(Flat plane, Vertex onPlaneVertex, double[] vertexDistancesToPlane, out Edge edge)
        {
            edge = null;
            foreach (var face in onPlaneVertex.Faces)
            {
                var otherEdge = face.OtherEdge(onPlaneVertex);
                var toDistance = vertexDistancesToPlane[otherEdge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[otherEdge.From.IndexInList];
                if ((toDistance > 0 && fromDistance < 0 && face == otherEdge.OtherFace) ||
                    (toDistance < 0 && fromDistance > 0 && face == otherEdge.OwnedFace))
                {
                    edge = otherEdge;
                    return face;
                }
            }
            return null;
        }
        #endregion


        /// <summary>
        /// Divides up contact.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="contactData">The contact data.</param>
        /// <param name="plane">The plane.</param>
        /// <exception cref="System.Exception">face is supposed to be split at plane but lives only on one side</exception>
        private static void DivideUpContact(TessellatedSolid ts, ContactData contactData, Flat plane)
        {
            var edgesToAdd = new List<Edge>();
            var facesToAdd = new List<PolygonalFace>();
            var verticesToAdd = new List<Vertex>();
            var edgesToDelete = new List<Edge>();
            var facesToDelete = new List<PolygonalFace>();
            var edgesToModify = new List<Edge>();
            foreach (var loop in contactData.AllLoops)
            {
                for (int i = 0; i < loop.Count; i++)
                {
                    var ce = loop[i];
                    // in DefineContact the loop edges were not connected to the vertices as the desire
                    // was to leave the TS unaffected. But now that we are working these changes in, we need
                    // to ensure that the edges, vertices, and faces are all properly connected.                           
                    if (!ce.ContactEdge.From.Edges.Contains(ce.ContactEdge))
                        ce.ContactEdge.From.Edges.Add(ce.ContactEdge);
                    if (!ce.ContactEdge.To.Edges.Contains(ce.ContactEdge)) ce.ContactEdge.To.Edges.Add(ce.ContactEdge);
                    if (ce.ContactType == ContactTypes.AlongEdge)
                        // If the contact element is at a coincident edge, then there is nothing to do in this stage. When contact element was
                        // created, it properly defined SplitFacePositive and SplitFaceNegative.
                        continue;
                    edgesToAdd.Add(ce.ContactEdge); // the contact edge is a new edge for the solid
                    edgesToModify.Add(ce.ContactEdge);
                    // the contact edge will need to be linked to vertices and faces further down.
                    var faceToSplit = ce.SplitFacePositive; //faceToSplit will be removed, but before we do that, we use
                    facesToDelete.Add(faceToSplit); // use it to build the new 2 to 3 triangles

                    PolygonalFace positiveFace, negativeFace;
                    if (ce.ContactType == ContactTypes.ThroughVertex)
                    {
                        var vertPlaneDistances = //signed distances of faceToSplit's vertices from the plane
                            faceToSplit.Vertices.Select(
                                v => v.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin).ToArray();
                        var maxIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Max());
                        var maxVert = faceToSplit.Vertices[maxIndex];
                        var minIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Min());
                        var minVert = faceToSplit.Vertices[minIndex];
                        positiveFace = new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, maxVert },
                            faceToSplit.Normal);
                        facesToAdd.Add(positiveFace);
                        negativeFace = new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, minVert },
                            faceToSplit.Normal);
                        facesToAdd.Add(negativeFace);
                        ce.ContactType = ContactTypes.AlongEdge;
                        ce.SplitFacePositive = positiveFace;
                        ce.SplitFaceNegative = negativeFace;
                    } //#+1 add v to f           (both of these are done in the preceding PolygonalFace
                    //#+2 add f to v            constructors as well as the one for thirdFace below)
                    else if (ce.ContactType == ContactTypes.ThroughFace)
                    {
                        var tfce = ce; // ce is renamed and recast as tfce 
                        edgesToDelete.Add(tfce.StartEdge);
                        verticesToAdd.Add(tfce.StartVertex);
                        Vertex positiveVertex, negativeVertex;
                        if (tfce.StartEdge.To.Position.dotProduct(plane.Normal) > plane.DistanceToOrigin)
                        {
                            positiveVertex = tfce.StartEdge.To;
                            negativeVertex = tfce.StartEdge.From;
                        }
                        else
                        {
                            positiveVertex = tfce.StartEdge.From;
                            negativeVertex = tfce.StartEdge.To;
                        }
                        positiveFace =
                            new PolygonalFace(new[] { ce.ContactEdge.To, ce.ContactEdge.From, positiveVertex },
                                faceToSplit.Normal);
                        facesToAdd.Add(positiveFace);
                        negativeFace =
                            new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, negativeVertex },
                                faceToSplit.Normal);
                        facesToAdd.Add(negativeFace);
                        var positiveEdge = new Edge(positiveVertex, ce.ContactEdge.From, positiveFace, null);
                        edgesToAdd.Add(positiveEdge);
                        edgesToModify.Add(positiveEdge);
                        var negativeEdge = new Edge(ce.ContactEdge.From, negativeVertex, negativeFace, null);
                        edgesToAdd.Add(negativeEdge);
                        edgesToModify.Add(negativeEdge);

                        var otherVertex = faceToSplit.Vertices.First(v => v != positiveVertex && v != negativeVertex);
                        PolygonalFace thirdFace;
                        if (otherVertex.Position.dotProduct(plane.Normal) > plane.DistanceToOrigin)
                        {
                            thirdFace = new PolygonalFace(new[] { ce.ContactEdge.To, otherVertex, positiveVertex },
                                faceToSplit.Normal);
                            facesToAdd.Add(thirdFace);
                            edgesToAdd.Add(new Edge(ce.ContactEdge.To, positiveVertex, positiveFace, thirdFace));
                        }
                        else
                        {
                            thirdFace = new PolygonalFace(new[] { ce.ContactEdge.To, negativeVertex, otherVertex },
                                faceToSplit.Normal);
                            facesToAdd.Add(thirdFace);
                            edgesToAdd.Add(new Edge(negativeVertex, ce.ContactEdge.To, negativeFace, thirdFace));
                        }
                        ts.HasUniformColor = false;
                        thirdFace.color = new Color(KnownColors.Turquoise);
                        negativeFace.color = new Color(KnownColors.CornflowerBlue);
                        positiveFace.color = new Color(KnownColors.HotPink);
                        ce.ContactType = ContactTypes.AlongEdge;
                        ce.SplitFacePositive = positiveFace;
                        ce.SplitFaceNegative = negativeFace;
                        // for the new edges in a through face this line accomplishes: +3 add f to e; +4 add e to f; +5 add v to e; 
                        //    +6 add e to v 
                    }
                }
            }
            // -1 remove v from f - no need to do this as no v's are removed
            foreach (var face in facesToDelete)
            {
                foreach (var vertex in face.Vertices)
                    vertex.Faces.Remove(face); //-2 remove f from v
                foreach (var edge in face.Edges)
                {
                    if (edgesToDelete.Contains(edge)) continue;
                    edgesToModify.Add(edge);
                    if (edge.OwnedFace == face) edge.OwnedFace = null; //-3 remove f from e
                    else edge.OtherFace = null;
                }
            }
            //-4 remove e from f - no need to do as the only edges deleted are the ones between deleted faces
            ts.RemoveFaces(facesToDelete);
            // -5 remove v from e - not needed as no vertices are deleted (like -1 above)
            foreach (var edge in edgesToDelete)
            {
                edge.From.Edges.Remove(edge); //-6 remove e from v
                edge.To.Edges.Remove(edge);
            }
            ts.RemoveEdges(edgesToDelete);
            // now to add new faces to modified edges   
            ts.AddVertices(verticesToAdd);
            ts.AddFaces(facesToAdd);

            foreach (var edge in edgesToModify)
            {
                var facesToAttach = facesToAdd.Where(f => f.Vertices.Contains(edge.To) && f.Vertices.Contains(edge.From)
                    && !f.Edges.Contains(edge));
                if (facesToAttach.Count() > 2) throw new Exception();
                foreach (var face in facesToAttach)
                {
                    face.Edges.Add(edge); //+4 add e to f
                    var fromIndex = face.Vertices.IndexOf(edge.From);
                    if ((fromIndex == face.Vertices.Count - 1 && face.Vertices[0] == edge.To)
                        || (fromIndex < face.Vertices.Count - 1 && face.Vertices[fromIndex + 1] == edge.To))
                        edge.OwnedFace = face; //+3 add f to e
                    else edge.OtherFace = face;
                }
            }
            ts.AddEdges(edgesToAdd);
        }

        private static List<List<PolygonalFace>> FindAllSolidsWithTheseFaces(IEnumerable<PolygonalFace> frontierFacesEnumerable,
            IEnumerable<PolygonalFace> forbiddenFacesEnumerable)
        {
            var frontierFaces = new HashSet<PolygonalFace>(frontierFacesEnumerable);
            var forbiddenFaces = new HashSet<PolygonalFace>(forbiddenFacesEnumerable);
            var facesLists = new List<List<PolygonalFace>>();
            while (frontierFaces.Any())
            {
                var startFace = frontierFaces.First();
                var faceList = new List<PolygonalFace>();
                var stack = new Stack<PolygonalFace>(new[] { startFace });
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (forbiddenFaces.Contains(face)) continue;
                    if (frontierFaces.Contains(face)) frontierFaces.Remove(face);
                    forbiddenFaces.Add(face);
                    faceList.Add(face);
                    foreach (var adjacentFace in face.AdjacentFaces)
                        if (!forbiddenFaces.Contains(adjacentFace))
                            stack.Push(adjacentFace);
                }
                facesLists.Add(faceList);
            }
            return facesLists;
        }
        private static List<TessellatedSolid> convertFaceListsToSolids(TessellatedSolid ts, List<List<PolygonalFace>> facesLists,
            List<Loop> loops, Boolean onPositiveSide, Flat plane)
        {
            List<TessellatedSolid> solids = new List<TessellatedSolid>();
            foreach (var facesList in facesLists)
            {
                // get a list of the vertex indices from the original solid
                var vertIndices = facesList.SelectMany(f => f.Vertices.Select(v => v.IndexInList))
                    .Distinct().OrderBy(index => index).ToArray();
                var numVertices = vertIndices.Count();
                // get the set of connected loops for this list of faces. it could be one or it could be all
                var connectedLoops = loops.Where(loop =>
                    (onPositiveSide && loop.Any(ce => facesList.Contains(ce.SplitFacePositive)))
                    || (!onPositiveSide && loop.Any(ce => facesList.Contains(ce.SplitFaceNegative))))
                    .ToList();
                // put the vertices from vertIndices in subSolidVertices, except those that are on the loop.
                // you'll need to copy those.
                var subSolidVertices = new Vertex[numVertices];
                var indicesToCopy = connectedLoops.SelectMany(loop => loop.Select(ce => ce.StartVertex.IndexInList))
                    .OrderBy(index => index).ToArray();
                var numIndicesToCopy = indicesToCopy.GetLength(0);
                var newEdgeVertices = new Vertex[connectedLoops.Count][];
                for (int i = 0; i < connectedLoops.Count; i++)
                    newEdgeVertices[i] = new Vertex[connectedLoops[i].Count];
                var copyIndex = 0;
                for (int i = 0; i < numVertices; i++)
                {
                    Vertex vertexCopy;
                    if (copyIndex < numIndicesToCopy && vertIndices[i] == indicesToCopy[copyIndex])
                    {
                        var oldVertex = ts.Vertices[vertIndices[i]];
                        vertexCopy = oldVertex.Copy();
                        for (int j = 0; j < connectedLoops.Count; j++)
                        {
                            var k = connectedLoops[j].FindIndex(ce => ce.StartVertex == oldVertex);
                            if (k >= 0) newEdgeVertices[j][k] = vertexCopy;
                        }
                        foreach (var face in oldVertex.Faces.Where(face => facesList.Contains(face)))
                        {
                            face.Vertices[face.Vertices.IndexOf(oldVertex)] = vertexCopy;
                            vertexCopy.Faces.Add(face);
                        }
                        while (copyIndex < numIndicesToCopy && vertIndices[i] >= indicesToCopy[copyIndex])
                            copyIndex++;
                    }
                    else vertexCopy = ts.Vertices[vertIndices[i]];
                    vertexCopy.IndexInList = i;
                    subSolidVertices[i] = vertexCopy;
                }
                solids.Add(new TessellatedSolid(facesList, subSolidVertices, newEdgeVertices, onPositiveSide ? plane.Normal.multiply(-1) : plane.Normal,
                    connectedLoops.Select(loop => loop.IsPositive).ToArray()));
            }
            return solids;
        }
    }
}

