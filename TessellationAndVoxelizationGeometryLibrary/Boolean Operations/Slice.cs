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
                else if ((toDistance > 0 && fromDistance < 0)
                         || (toDistance < 0 && fromDistance > 0))
                    straddleContactElts.Add(new ThroughFaceContactElement(plane, edge, toDistance));
            }
            foreach (var contactElement in inPlaneContactElts)
            {
                // next, we find any additional vertices that just touch the plane but don't have in-plane edges
                // to facilitate this we negate all vertices already captures in the inPlaneContactElts 
                vertexDistancesToPlane[contactElement.StartVertex.IndexInList] = double.NaN;
                vertexDistancesToPlane[contactElement.EndVertex.IndexInList] = double.NaN;
            }
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                if (!StarMath.IsNegligible(vertexDistancesToPlane[i])) continue;
                var v = ts.Vertices[i];
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
                    contactElt.ContactEdge = new Edge(contactElt.StartVertex, connectingVertex, null, null);
                else if (contactElt is CoincidentEdgeContactElement &&
                         !(nextContactElt is CoincidentEdgeContactElement))
                {
                    contactElements.Add(contactElt);
                    contactElt = new ThroughVertexContactElement(connectingVertex, null, null)
                    {
                        ContactEdge = new Edge(((CoincidentEdgeContactElement)contactElt).EndVertex, connectingVertex, null, null)
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
                    contactElt.ContactEdge = new Edge(connectingVertex, contactElt.StartVertex, null, null);
                else if (contactElt is CoincidentEdgeContactElement &&
                         !(prevContactElt is CoincidentEdgeContactElement))
                {
                    contactElements.Insert(0, contactElt);
                    contactElt = new ThroughVertexContactElement(connectingVertex, null, null)
                    {
                        ContactEdge = new Edge(connectingVertex, ((CoincidentEdgeContactElement)contactElt).StartVertex, null, null)
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
                    contactElements[0].ContactEdge.To, null, null);
                return new Loop(contactElements, plane.Normal, true, true);
            }
            contacts.Remove(firstContactElt); //it didn't work to connect it up, so you're going to have to leave
            // the loop open. Be sure to remove that one contact that you were hoping to re-find. Otherwise, the 
            // outer process will continue to consider it.
            var artificialContactElement = new ArtificialContactElement
            {
                ContactEdge =
                    new Edge(contactElements.Last().ContactEdge.To, contactElements[0].ContactEdge.From, null, null)
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
        /// Performs the slicing operation on the prescribed flat plane. This destructively alters
        /// the tessellated solid into one or more solids which are returned in the "out" parameter
        /// lists.
        /// </summary>
        /// <param name="oldSolid">The old solid.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolids">The solids that are on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolids">The solids on the negative side of the plane.</param>
        public static
        void OnFlat
            (TessellatedSolid ts, Flat plane, out List<TessellatedSolid> positiveSideSolids,
                out List<TessellatedSolid> negativeSideSolids)
        {
            var contactData = DefineContact(plane, ts);
            DivideUpContact(ts, contactData, plane);
            //todo: now need to split the ts into separate lists...perhaps by following faces in tree
            // until contact data is hit?
            positiveSideSolids = new List<TessellatedSolid>();
            negativeSideSolids = new List<TessellatedSolid>();
        }

        /// <summary>
        /// Divides up contact.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="contactData">The contact data.</param>
        /// <param name="plane">The plane.</param>
        /// <exception cref="System.Exception">face is supposed to be split at plane but lives only on one side</exception>
        private static
            void DivideUpContact
            (TessellatedSolid ts, ContactData contactData, Flat plane)
        {
            var edgesToAdd = new List<Edge>();
            var facesToAdd = new List<PolygonalFace>();
            var verticesToAdd = new List<Vertex>();
            var edgesToDelete = new List<Edge>();
            var facesToDelete = new List<PolygonalFace>();
            var edgesToModify = new List<Edge>();
            var allLoops = new List<Loop>(contactData.PositiveLoops);
            allLoops.AddRange(contactData.NegativeLoops);
            foreach (var loop in allLoops)
            {
                foreach (var contactElement in loop)
                {
                    if (contactElement is CoincidentEdgeContactElement)
                        // If the contact element is at a coincident edge, then there is nothing to do in this stage. When contact element was
                        // created, it properly defined SplitFacePositive and SplitFaceNegative.
                        continue;
                    edgesToAdd.Add(contactElement.ContactEdge);
                    var faceToSplit = contactElement.SplitFacePositive;
                    facesToDelete.Add(faceToSplit);
                    var vertPlaneDistances =
                        faceToSplit.Vertices.Select(
                            v => v.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin).ToArray();
                    var maxIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Max());
                    var maxVert = faceToSplit.Vertices[maxIndex];
                    var minIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Min());
                    var minVert = faceToSplit.Vertices[minIndex];
                    var midVert = faceToSplit.Vertices.First(v => v != maxVert && v != minVert);
                    var dxMidVert = midVert.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin;
                    var faceVerts = new List<Vertex>();
                    faceVerts.AddRange(new[] { contactElement.ContactEdge.From, contactElement.ContactEdge.To, maxVert });
                    var positiveFace = new PolygonalFace(faceVerts);
                    //todo var edge1 = new Edge(contactElement.ContactEdge.From, contactElement.ContactEdge.To,positiveFace,)
                    facesToAdd.Add(positiveFace);
                    faceVerts.Clear();
                    faceVerts.AddRange(new[] { contactElement.ContactEdge.To, contactElement.ContactEdge.From, minVert });
                    var negativeFace = new PolygonalFace(faceVerts);
                    facesToAdd.Add(negativeFace);
                    //#+1 add v to f           (both of these are done in the preceding PolygonalFace
                    //#+2 add f to v            constructors as well as the one for thirdFace below)
                    if (contactElement is ThroughFaceContactElement)
                    {
                        faceVerts.Clear();
                        var tfce = (ThroughFaceContactElement)contactElement;
                        edgesToDelete.Add(tfce.SplitEdge);
                        verticesToAdd.Add(tfce.StartVertex);
                        if (tfce.SplitEdge.From == midVert || tfce.SplitEdge.To == midVert)
                        {
                            // then connect third-face to the From of contact element
                            if (dxMidVert < 0)
                                faceVerts.AddRange(new[] { minVert, contactElement.ContactEdge.From, midVert });
                            else faceVerts.AddRange(new[] { contactElement.ContactEdge.From, maxVert, midVert });
                        }
                        else
                        {
                            // then connect third-face to the TO of contact element
                            if (dxMidVert < 0)
                                faceVerts.AddRange(new[] { contactElement.ContactEdge.To, minVert, midVert });
                            else faceVerts.AddRange(new[] { maxVert, contactElement.ContactEdge.To, midVert });
                        }
                        var thirdFace = new PolygonalFace(faceVerts);
                        facesToAdd.Add(thirdFace);
                        edgesToAdd.Add(new Edge(faceVerts[0], faceVerts[1], thirdFace, (dxMidVert < 0) ? negativeFace : positiveFace));
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
            foreach (var edge in edgesToModify)
            {
                var faceToAttach = facesToAdd.First(f => f.Vertices.Contains(edge.To) && f.Vertices.Contains(edge.From));
                faceToAttach.Edges.Add(edge);       //+4 add e to f
                if (edge.OwnedFace == null) edge.OwnedFace = faceToAttach;  //+3 add f to e
                else edge.OtherFace = faceToAttach;
            }
            ts.AddVertices(verticesToAdd);
            ts.AddEdges(edgesToAdd);
            ts.AddFaces(facesToAdd);
        }
    }
}

