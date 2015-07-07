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
            var contactData = DefineContact(plane, ts);

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
        /// described by the return ContactData object. This is a non-destructive function typically
        /// used to find the shape and size of 2D surface on the prescribed plane..
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="ts">The ts.</param>
        /// <returns>ContactData.</returns>
        /// <exception cref="System.Exception">Contact Edges found that are not contained in loop.</exception>
        public static ContactData DefineContact(Flat plane, TessellatedSolid ts)
        {
            var vertexDistancesToPlane = new double[ts.NumberOfVertices];
            for (int i = 0; i < ts.NumberOfVertices; i++)
                vertexDistancesToPlane[i] = ts.Vertices[i].Position.dotProduct(plane.Normal) - plane.DistanceToOrigin;
            // the edges serve as the easiest way to identify where the solid is interacting with the plane.
            // Instead of a foreach, the while loop lets us look ahead to known edges that are irrelevant.
            var edgeHashSet = new HashSet<Edge>(ts.Edges);
            // Contact elements are constructed and then later arranged into loops. Loops make up the returned object, ContactData. 
            var straddleContactElts = new List<ContactElement>();
            var inPlaneContactElts = new List<CoincidentEdgeContactElement>();
            var inPlaneVertices = new List<Vertex>();
            while (edgeHashSet.Any())
            {
                // instead of the foreach, we have this while statement and these first 2 lines to enumerate over the edges.
                var edge = edgeHashSet.First();
                edgeHashSet.Remove(edge);
                var toDistance = vertexDistancesToPlane[edge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[edge.From.IndexInList];
                if (StarMath.IsNegligible(toDistance) && StarMath.IsNegligible(fromDistance))
                    ContactElement.MakeInPlaneContactElement(plane, edge, edgeHashSet, vertexDistancesToPlane,
                        inPlaneContactElts);
                else if (StarMath.IsNegligible(toDistance)) inPlaneVertices.Add(edge.To);
                else if (StarMath.IsNegligible(fromDistance)) inPlaneVertices.Add(edge.From);
                else if ((toDistance > 0 && fromDistance < 0)
                         || (toDistance < 0 && fromDistance > 0))
                    straddleContactElts.Add(new ThroughFaceContactElement(plane, edge, toDistance));
            }
            foreach (var v in inPlaneVertices )
            {
                PolygonalFace negativeFace, positiveFace;
                if (ThroughVertexContactElement.FindNegativeAndPositiveFaces(plane, v, vertexDistancesToPlane,
                    out negativeFace, out positiveFace))
                    straddleContactElts.Add(new ThroughVertexContactElement(v, negativeFace, positiveFace));
            }
            straddleContactElts.AddRange(inPlaneContactElts);
            var loops = new List<Loop>();
            var numberOfTries = 0;
            while (straddleContactElts.Any() && numberOfTries < straddleContactElts.Count)
            {
                // now build loops from stringing together contact elements
                var loop = FindLoop(plane, straddleContactElts, vertexDistancesToPlane);
                if (loop != null)
                {
                    Debug.WriteLine(loops.Count + ": " + loop.MakeDebugContactString() + "  ");
                    loops.Add(loop);
                    numberOfTries = 0;
                }
                else numberOfTries++;
            }
            if (straddleContactElts.Any()) Debug.WriteLine("Contact Edges found that are not contained in loop.");
            return new ContactData(loops);
        }

        /// <summary>
        /// Finds the loop.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="contacts">The contacts.</param>
        /// <param name="vertexDistancesToPlane"></param>
        /// <returns>Loop.</returns>
        private static Loop FindLoop(Flat plane, List<ContactElement> contacts, double[] vertexDistancesToPlane)
        {
            Vertex connectingVertex;
            var contactElements = new List<ContactElement>();
            var firstContactElt = contacts[0]; // start with the first one, and keep a reference to it so that we 
            // can tell when we've looped around.                      
            // contacts.RemoveAt(0);  why is this commented out? I left it here to help you understand that it is important
            // to identify a loop. When the below while-loop re-finds this firstContactElt
            var contactElt = firstContactElt;
            int nextContactEltIndex = FindNextContactElement(contacts, contactElt, out connectingVertex);
            if (nextContactEltIndex == -1)
            {
                //if failed to find the loop, stick it on the end and start over
                contacts.RemoveAt(0);
                contacts.Add(contactElt);
                return null;
            }
            while (nextContactEltIndex != -1)
            {
                var nextContactElt = contacts[nextContactEltIndex];
                if (!(contactElt is CoincidentEdgeContactElement))
                    contactElt.ContactEdge = new Edge(contactElt.StartVertex, connectingVertex, false);
                else if (contactElt is CoincidentEdgeContactElement &&
                         !(nextContactElt is CoincidentEdgeContactElement))
                {
                    contactElements.Add(contactElt);
                    contactElt = new ThroughVertexContactElement(connectingVertex, null, nextContactElt.SplitFaceNegative)
                    {
                        ContactEdge = new Edge(((CoincidentEdgeContactElement)contactElt).EndVertex, connectingVertex, false)
                    };
                }
                contactElements.Add(contactElt);
                contacts.RemoveAt(nextContactEltIndex);
                contactElt = nextContactElt;

                nextContactEltIndex = FindNextContactElement(contacts, contactElt, out connectingVertex);
            }
            if (contactElt == firstContactElt)
                return new Loop(contactElements, plane.Normal, true, false);
            // else work backwards        
            contactElt = firstContactElt;
            contacts.RemoveAt(0);
            // now it is right to remove the first contact, and instead, we will re-add the last one
            firstContactElt = contactElements.Last();
            contacts.Add(firstContactElt);
            int prevContactEltIndex = FindPrevContactElement(contacts, contactElt, out connectingVertex);
            while (prevContactEltIndex != -1)
            {
                var prevContactElt = contacts[prevContactEltIndex];
                if (!(contactElt is CoincidentEdgeContactElement))
                    contactElt.ContactEdge = new Edge(connectingVertex, contactElt.StartVertex, false);
                else if (contactElt is CoincidentEdgeContactElement &&
                         !(prevContactElt is CoincidentEdgeContactElement))
                {
                    contactElements.Insert(0, contactElt);
                    contactElt = new ThroughVertexContactElement(connectingVertex, prevContactElt.SplitFacePositive, null)
                    {
                        ContactEdge = new Edge(connectingVertex, ((CoincidentEdgeContactElement)contactElt).StartVertex, false)
                    };
                }
                contactElements.Insert(0, contactElt);
                contacts.RemoveAt(prevContactEltIndex);
                contactElt = prevContactElt;
                prevContactEltIndex = FindPrevContactElement(contacts, contactElt, out connectingVertex);
            }
            if (contactElements[0].ContactEdge.From == contactElements.Last().ContactEdge.To)
                return new Loop(contactElements, plane.Normal, true, false);
            if (StarMath.IsPracticallySame(contactElements[0].ContactEdge.From.Position,
                  contactElements.Last().ContactEdge.To.Position))
            {
                contactElements[0].ContactEdge = new Edge(contactElements.Last().ContactEdge.To,
                    contactElements[0].ContactEdge.To, false);
                return new Loop(contactElements, plane.Normal, true, true);
            }
            contacts.Remove(firstContactElt); //it didn't work to connect it up, so you're going to have to leave
            // the loop open. Be sure to remove that one contact that you were hoping to re-find. Otherwise, the 
            // outer process will continue to consider it.
            var artificialContactElement = new ArtificialContactElement
            {
                ContactEdge =
                    new Edge(contactElements.Last().ContactEdge.To, contactElements[0].ContactEdge.From, false)
            };
            Debug.WriteLine("Adding an artificial edge to close the loop for plane @" + plane.Normal.MakePrintString()
                            + " with a distance of " + plane.DistanceToOrigin);
            contactElements.Add(artificialContactElement);
            return new Loop(contactElements, plane.Normal, true, true);

        }


        private static int FindNextContactElement(List<ContactElement> contacts, ContactElement current,
            out Vertex connectingVertex)
        {
            // there are six cases to handle: A=ThroughFace (abbreviated as face below), ThroughVertex (vertex), CoincidentEdge (edge)
            // current contact element --> next contact element  
            // 1. vertex --> face            
            // 2. edge --> edge            
            // 3. edge --> face            
            // 4. face --> face            
            // 5. face --> vertex            
            // 6. face --> edge                              
            if (current is ThroughVertexContactElement)
            {
                // from a ThroughVertex, it only make sense that you could go to ThroughFace
                for (int i = 0; i < contacts.Count; i++)
                {
                    var ce = contacts[i];
                    if (ce is ThroughFaceContactElement && ce.SplitFaceNegative == current.SplitFacePositive)
                    {
                        connectingVertex = ce.StartVertex;
                        return i;
                    }
                }
                connectingVertex = null;
                return -1;
            }
            else if (current is CoincidentEdgeContactElement)
            {
                // from a Coincident Edge, the valid options are another CoincidentEdge or ThroughFace.
                // It doesn't make sense that you could go to a ThroughVertex (redundant with this)
                for (int i = 0; i < contacts.Count; i++)
                {
                    var ce = contacts[i];
                    if ((ce is CoincidentEdgeContactElement &&
                         ((CoincidentEdgeContactElement)ce).StartVertex ==
                         ((CoincidentEdgeContactElement)current).EndVertex)
                        || (ce is ThroughFaceContactElement
                            &&
                            ((ThroughFaceContactElement)ce).SplitFaceNegative.OtherVertex(
                                ((ThroughFaceContactElement)ce).SplitEdge) ==
                            ((CoincidentEdgeContactElement)current).EndVertex))
                    {
                        connectingVertex = ce.StartVertex;
                        return i;
                    }
                }
                connectingVertex = null;
                return -1;
            }
            // finally from ThroughFace, you can go to any of the other three.     
            for (int i = 0; i < contacts.Count; i++)
            {
                var ce = contacts[i];
                if ((ce is CoincidentEdgeContactElement &&
                    current.SplitFacePositive.OtherVertex(((ThroughFaceContactElement)current).SplitEdge) ==
                    ((CoincidentEdgeContactElement)ce).StartVertex)
                ||
                (!(ce is CoincidentEdgeContactElement) && ce.SplitFaceNegative == current.SplitFacePositive))
                {
                    connectingVertex = ce.StartVertex;
                    return i;
                }
            }
            connectingVertex = null;
            return -1;
        }

        private static int FindPrevContactElement(List<ContactElement> contacts, ContactElement current,
            out Vertex connectingVertex)
        {
            // this is the same as "FindNextContactElement" except it works backwards. Some subtle differences
            // in the queries between the two functions
            if (current is ThroughVertexContactElement)
            {
                for (int i = 0; i < contacts.Count; i++)
                {
                    var ce = contacts[i];
                    if (ce is ThroughFaceContactElement && ce.SplitFacePositive == current.SplitFaceNegative)
                    {
                        connectingVertex = ce.StartVertex;
                        return i;
                    }
                }
                connectingVertex = null;
                return -1;
            }
            else if (current is CoincidentEdgeContactElement)
            {
                for (int i = 0; i < contacts.Count; i++)
                {
                    var ce = contacts[i];
                    if (ce is CoincidentEdgeContactElement &&
                        ((CoincidentEdgeContactElement)ce).EndVertex ==
                        ((CoincidentEdgeContactElement)current).StartVertex)
                    {
                        connectingVertex = ((CoincidentEdgeContactElement)ce).EndVertex;
                        return i;
                    }
                    else if (ce is ThroughFaceContactElement
                             &&
                             ((ThroughFaceContactElement)ce).SplitFacePositive.OtherVertex(
                                 ((ThroughFaceContactElement)ce).SplitEdge) ==
                             ((CoincidentEdgeContactElement)current).EndVertex)
                    {
                        connectingVertex = ce.StartVertex;
                        return i;
                    }
                }
                connectingVertex = null;
                return -1;
            }
            for (int i = 0; i < contacts.Count; i++)
            {
                var ce = contacts[i];
                if ((ce is CoincidentEdgeContactElement &&
                    current.SplitFacePositive.OtherVertex(((ThroughFaceContactElement)current).SplitEdge) ==
                    ((CoincidentEdgeContactElement)ce).EndVertex)
                ||
                (!(ce is CoincidentEdgeContactElement) && ce.SplitFacePositive == current.SplitFaceNegative))
                {
                    connectingVertex = ce.StartVertex;
                    return i;
                }
            }
            connectingVertex = null;
            return -1;
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
                    if (!ce.ContactEdge.From.Edges.Contains(ce.ContactEdge)) ce.ContactEdge.From.Edges.Add(ce.ContactEdge);
                    if (!ce.ContactEdge.To.Edges.Contains(ce.ContactEdge)) ce.ContactEdge.To.Edges.Add(ce.ContactEdge);
                    if (ce is CoincidentEdgeContactElement)
                        // If the contact element is at a coincident edge, then there is nothing to do in this stage. When contact element was
                        // created, it properly defined SplitFacePositive and SplitFaceNegative.
                        continue;
                    edgesToAdd.Add(ce.ContactEdge); // the contact edge is a new edge for the solid
                    edgesToModify.Add(ce.ContactEdge); // the contact edge will need to be linked to vertices and faces further down.
                    var faceToSplit = ce.SplitFacePositive; //faceToSplit will be removed, but before we do that, we use
                    facesToDelete.Add(faceToSplit);         // use it to build the new 2 to 3 triangles

                    PolygonalFace positiveFace, negativeFace;
                    if (ce is ThroughVertexContactElement)
                    {
                        var vertPlaneDistances =              //signed distances of faceToSplit's vertices from the plane
                        faceToSplit.Vertices.Select(
                            v => v.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin).ToArray();
                        var maxIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Max());
                        var maxVert = faceToSplit.Vertices[maxIndex];
                        var minIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Min());
                        var minVert = faceToSplit.Vertices[minIndex];
                        positiveFace = new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, maxVert }, faceToSplit.Normal);
                        facesToAdd.Add(positiveFace);
                        negativeFace = new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, minVert }, faceToSplit.Normal);
                        facesToAdd.Add(negativeFace);
                    } //#+1 add v to f           (both of these are done in the preceding PolygonalFace
                    //#+2 add f to v            constructors as well as the one for thirdFace below)
                    else if (ce is ThroughFaceContactElement)
                    {
                        var tfce = (ThroughFaceContactElement)ce; // ce is renamed and recast as tfce 
                        edgesToDelete.Add(tfce.SplitEdge);
                        verticesToAdd.Add(tfce.StartVertex);
                        Vertex positiveVertex, negativeVertex;
                        if (tfce.SplitEdge.To.Position.dotProduct(plane.Normal) > plane.DistanceToOrigin)
                        {
                            positiveVertex = tfce.SplitEdge.To;
                            negativeVertex = tfce.SplitEdge.From;
                        }
                        else
                        {
                            positiveVertex = tfce.SplitEdge.From;
                            negativeVertex = tfce.SplitEdge.To;
                        }
                        positiveFace =
                           new PolygonalFace(new[] { ce.ContactEdge.To, ce.ContactEdge.From, positiveVertex }, faceToSplit.Normal);
                        facesToAdd.Add(positiveFace);
                        negativeFace =
                           new PolygonalFace(new[] { ce.ContactEdge.From, ce.ContactEdge.To, negativeVertex }, faceToSplit.Normal);
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
                            thirdFace = new PolygonalFace(new[] { ce.ContactEdge.To, otherVertex, positiveVertex }, faceToSplit.Normal);
                            facesToAdd.Add(thirdFace);
                            edgesToAdd.Add(new Edge(ce.ContactEdge.To, positiveVertex, positiveFace, thirdFace));
                        }
                        else
                        {
                            thirdFace = new PolygonalFace(new[] { ce.ContactEdge.To, negativeVertex, otherVertex }, faceToSplit.Normal);
                            facesToAdd.Add(thirdFace);
                            edgesToAdd.Add(new Edge(negativeVertex, ce.ContactEdge.To, negativeFace, thirdFace));
                        }
                        ts.HasUniformColor = false;
                        thirdFace.color = new Color(KnownColors.Turquoise);
                        negativeFace.color = new Color(KnownColors.CornflowerBlue);
                        positiveFace.color = new Color(KnownColors.HotPink);
                        // for the new edges in a through face this line accomplishes: +3 add f to e; +4 add e to f; +5 add v to e; 
                        //    +6 add e to v 
                    }
                    else  //then artificial. How to handle this?
                    {
                        negativeFace = null;
                        positiveFace = null;
                    }
                    loop[i] = new CoincidentEdgeContactElement
                    {
                        ContactEdge = ce.ContactEdge,
                        EndVertex = ce.ContactEdge.To,
                        StartVertex = ce.ContactEdge.From,
                        SplitFaceNegative = negativeFace,
                        SplitFacePositive = positiveFace
                    };
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

