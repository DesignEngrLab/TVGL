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
            var inPlaneContactElts = new List<ContactElement>();
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
                    straddleContactElts.Add(ContactElement.MakeStraddleContactElement(plane, edge, toDistance));
            }
            foreach (var contactElement in inPlaneContactElts)
            {   // next, we find any additional vertices that just touch the plane but don't have in-plane edges
                // to facilitate this we negate all vertices already captures in the inPlaneContactElts 
                vertexDistancesToPlane[contactElement.ReferenceStart.IndexInList] = double.NaN;
                vertexDistancesToPlane[contactElement.ReferenceEnd.IndexInList] = double.NaN;
            }
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                if (!StarMath.IsNegligible(vertexDistancesToPlane[i])) continue;
                var vertexContactElement = ContactElement.MakeVertexOnPlaneContactElement(plane, ts.Vertices[i],
                    vertexDistancesToPlane);
                if (vertexContactElement != null) straddleContactElts.Add(vertexContactElement);
            }
            straddleContactElts.AddRange(inPlaneContactElts);
            var loops = new List<Loop>();
            var numberOfTries = 0;
            while (straddleContactElts.Any() && numberOfTries < straddleContactElts.Count)
            {   // now build loops from stringing together contact elements
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
            var contactElements = new List<ContactElement>();
            var firstContactElt = contacts[0];   // start with the first one, and keep a reference to it so that we 
            // can tell when we've looped around.                      
            // contacts.RemoveAt(0);  why is this commented out? I left it here to help you understand that it is important
            // to identify a loop. When the below while-loop re-finds this firstContactElt
            var contactElt = firstContactElt;
            int nextContactEltIndex = FindNextContactElement(contacts, contactElt);
            if (nextContactEltIndex == -1)
            {  //if failed to find the loop, stick it on the end and start over
                contacts.RemoveAt(0);
                contacts.Add(contactElt);
                return null;
            }
            while (nextContactEltIndex != -1)
            {
                var nextContactElt = contacts[nextContactEltIndex];
                if (contactElt.SplitType != FaceSplitType.CoincidentEdge)
                    contactElt.ContactEdge = new Edge(contactElt.ContactPoint, nextContactElt.ContactPoint, null, null);
                else if (contactElt.SplitType == FaceSplitType.CoincidentEdge &&
                   nextContactElt.SplitType != FaceSplitType.CoincidentEdge)
                {
                    contactElements.Add(contactElt);
                    contactElt = new ContactElement
                    {
                        ContactEdge = new Edge(contactElt.ReferenceEnd, nextContactElt.ContactPoint, null, null),
                        ReferenceStart = contactElt.ReferenceEnd,
                        SplitType = FaceSplitType.ThroughVertex
                    };
                }
                contactElements.Add(contactElt);
                contacts.RemoveAt(nextContactEltIndex);
                contactElt = nextContactElt;
                nextContactEltIndex = FindNextContactElement(contacts, contactElt);
            }
            if (contactElt == firstContactElt)
                return new Loop(contactElements, plane.Normal, true, true);
            //if (!contacts.Any())
            //    return new Loop(contactElements, plane.Normal, false);
            // else work backwards        
            contactElt = firstContactElt;
            contacts.RemoveAt(0); // now it is right to remove the first contact, and instead, we will re-add the last one
            firstContactElt = contactElements.Last();
            contacts.Add(firstContactElt);
            int prevContactEltIndex = FindPrevContactElement(contacts, contactElt);
            while (prevContactEltIndex != -1)
            {
                var prevContactElt = contacts[prevContactEltIndex];
                if (contactElt.SplitType != FaceSplitType.CoincidentEdge)
                    contactElt.ContactEdge = new Edge(prevContactElt.ContactPoint, contactElt.ContactPoint, null, null);
                else if (contactElt.SplitType == FaceSplitType.CoincidentEdge &&
                  prevContactElt.SplitType != FaceSplitType.CoincidentEdge)
                {
                    contactElements.Insert(0, contactElt);
                    contactElt = new ContactElement
                    {
                        ContactEdge = new Edge(contactElt.ReferenceStart, prevContactElt.ContactPoint, null, null),
                        ReferenceEnd = contactElt.ReferenceStart,
                        SplitType = FaceSplitType.ThroughVertex
                    };
                }
                contactElements.Insert(0, contactElt);
                contacts.RemoveAt(prevContactEltIndex);
                contactElt = prevContactElt;
                prevContactEltIndex = FindPrevContactElement(contacts, prevContactElt);
            }
            if (((contactElements[0].ReferenceStart != null)
                && (contactElements[0].ReferenceStart == contactElements.Last().ReferenceEnd))
                || StarMath.IsPracticallySame(contactElements[0].ContactEdge.From.Position,
                contactElements.Last().ContactEdge.To.Position))
            {
                contactElements[0].ContactEdge = new Edge(contactElements.Last().ContactEdge.To,
                    contactElements[0].ContactEdge.To, null, null);
                contactElements[0].ContactEdge.From.Edges.Add(contactElements[0].ContactEdge);
                return new Loop(contactElements, plane.Normal, true, true);
            }
            contacts.Remove(firstContactElt); //it didn't work to connect it up, so you're going to have to leave
            // the loop open. Be sure to remove that one contact that you were hoping to re-find. Otherwise, the 
            // outer process will continue to consider it.
            var artificialContactElement = new ContactElement
            {
                ContactEdge =
                    new Edge(contactElements.Last().ContactEdge.To, contactElements[0].ContactEdge.From, null, null),
                SplitType = FaceSplitType.Artificial
            };
            Debug.WriteLine("Adding an artificial edge to close the loop for plane @" + plane.Normal.MakePrintString()
                + " with a distance of " + plane.DistanceToOrigin);
            contactElements.Add(artificialContactElement);
            return new Loop(contactElements, plane.Normal, true, false);

        }

        private static int FindPrevContactElement(List<ContactElement> contacts, ContactElement current)
        {
            if (current.SplitType == FaceSplitType.ThroughVertex)
                // from a ThroughVertex, it only make sense that you could go to ThroughFace
                return contacts.FindIndex(ce =>
                    (ce.SplitType == FaceSplitType.ThroughFace
                     && ce.SplitFacePositive == current.SplitFaceNegative));
            if (current.SplitType == FaceSplitType.CoincidentEdge)
                // from a Coincident Edge, the valid options are another CoincidentEdge or ThroughFace.
                // It doesn't make sense that you could go to a ThroughVertex (redundant with this)
                return contacts.FindIndex(ce =>
                    (ce.SplitType == FaceSplitType.CoincidentEdge && ce.ReferenceEnd == current.ReferenceStart)
                        //|| (ce.SplitType == FaceSplitType.ThroughFace && ce.SplitFacePositive.Vertices.Contains(current.ReferenceStart)));
            || (ce.SplitType == FaceSplitType.ThroughFace && ce.SplitFacePositive.OtherVertex(ce.SplitEdge) == current.ReferenceStart));
            // finally from ThroughFace, you can go to any of the other three.
            return contacts.FindIndex(ce =>
                (ce.SplitType == FaceSplitType.CoincidentEdge &&
                    //current.SplitFaceNegative.Vertices.Contains(ce.ReferenceEnd))
                    current.SplitFaceNegative.OtherVertex(current.SplitEdge) == ce.ReferenceEnd)
                || (ce.SplitType != FaceSplitType.CoincidentEdge
                    && ce.SplitFacePositive == current.SplitFaceNegative));
        }


        private static int FindNextContactElement(List<ContactElement> contacts, ContactElement current)
        {   // there are six cases to handle: A=ThroughFace, B=ThroughVertex, C = CoincidentEdge
            // current contact element --> next contact element  
            // 1. B --> A            
            // 2. C --> C            
            // 3. C --> A            
            // 4. A --> A            
            // 5. A --> B            
            // 6. A --> C

            if (current.SplitType == FaceSplitType.ThroughVertex)
                // from a ThroughVertex, it only make sense that you could go to ThroughFace
                return contacts.FindIndex(ce =>
                    (ce.SplitType == FaceSplitType.ThroughFace
                     && ce.SplitFaceNegative == current.SplitFacePositive));
            if (current.SplitType == FaceSplitType.CoincidentEdge)
                // from a Coincident Edge, the valid options are another CoincidentEdge or ThroughFace.
                // It doesn't make sense that you could go to a ThroughVertex (redundant with this)
                return contacts.FindIndex(ce =>
                    (ce.SplitType == FaceSplitType.CoincidentEdge && ce.ReferenceStart == current.ReferenceEnd)
                        //|| (ce.SplitType == FaceSplitType.ThroughFace && ce.SplitFaceNegative.Vertices.Contains(current.ReferenceEnd)));
            || (ce.SplitType == FaceSplitType.ThroughFace && ce.SplitFaceNegative.OtherVertex(ce.SplitEdge) == current.ReferenceEnd));
            // finally from ThroughFace, you can go to any of the other three.
            return contacts.FindIndex(ce =>
                (ce.SplitType == FaceSplitType.CoincidentEdge &&
                    //current.SplitFacePositive.Vertices.Contains(ce.ReferenceStart))
                    current.SplitFacePositive.OtherVertex(current.SplitEdge) == ce.ReferenceStart)
                || (ce.SplitType != FaceSplitType.CoincidentEdge
                    && ce.SplitFaceNegative == current.SplitFacePositive));
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
        public static void OnFlat(TessellatedSolid oldSolid, Flat plane, out List<TessellatedSolid> positiveSideSolids,
            out List<TessellatedSolid> negativeSideSolids)
        {
            var ts = oldSolid.Copy();  // user should know this is destructive! why slow down again here.
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
        private static void DivideUpContact(TessellatedSolid ts, ContactData contactData, Flat plane)
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
                    if (contactElement.SplitType == FaceSplitType.CoincidentEdge)
                        // If the contact element is at a coincident edge, then there is nothing to do in this stage. When contact element was
                        // created, it properly defined SplitFacePositive and SplitFaceNegative.
                        continue;
                    edgesToAdd.Add(contactElement.ContactEdge);
                    var faceToSplit = contactElement.SplitFacePositive;
                    facesToDelete.Add(faceToSplit);
                    var vertPlaneDistances =
                        faceToSplit.Vertices.Select(v => v.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin).ToArray();
                    var maxIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Max());
                    var maxVert = faceToSplit.Vertices[maxIndex];
                    var minIndex = vertPlaneDistances.FindIndex(vertPlaneDistances.Min());
                    var minVert = faceToSplit.Vertices[minIndex];
                    var midVert = faceToSplit.Vertices.First(v => v != maxVert && v != minVert);
                    var dxMidVert = midVert.Position.dotProduct(plane.Normal) - plane.DistanceToOrigin;
                    var faceVerts = new List<Vertex>();
                    if (contactElement.SplitType == FaceSplitType.ThroughFace)
                    {
                        edgesToDelete.Add(contactElement.SplitEdge);
                        verticesToAdd.Add(contactElement.ContactPoint);
                        if (contactElement.SplitEdge.From == midVert || contactElement.SplitEdge.To == midVert)
                        {// then connect third-face to the From of contact element
                            if (dxMidVert < 0)
                                faceVerts.AddRange(new[] { contactElement.ContactEdge.From, midVert, minVert });
                            else faceVerts.AddRange(new[] { contactElement.ContactEdge.From, maxVert, midVert });
                        }
                        else
                        { // then connect third-face to the TO of contact element
                            if (dxMidVert < 0)
                                faceVerts.AddRange(new[] { contactElement.ContactEdge.To, minVert, midVert });
                            else faceVerts.AddRange(new[] { contactElement.ContactEdge.To, midVert, maxVert });
                        }
                    }
                    facesToAdd.Add(new PolygonalFace(faceVerts));
                    faceVerts.Clear();
                    faceVerts.AddRange(new[] { contactElement.ContactEdge.From, contactElement.ContactEdge.To, maxVert });
                    facesToAdd.Add(new PolygonalFace(faceVerts));
                    faceVerts.Clear();
                    faceVerts.AddRange(new[] { contactElement.ContactEdge.To, contactElement.ContactEdge.From, midVert });
                    facesToAdd.Add(new PolygonalFace(faceVerts));
                    //#+1 add v to f
                }
            }
            // -1 remove v from f - no need to do this as no v's are removed
            // -5 remove v from e - again not done
            foreach (var face in facesToDelete)
            {
                foreach (var vertex in face.Vertices)
                    vertex.Faces.Remove(face);   //-2 remove f from v
                foreach (var edge in face.Edges)
                {
                    if (edgesToDelete.Contains(edge)) continue;
                    edgesToModify.Add(edge);
                    if (edge.OwnedFace == face) edge.OwnedFace = null;       //-3 remove f from e
                    else edge.OtherFace = null;
                }
            }
            //-4 remove e from f - no need to do as the only edges deleted are the ones between deleted faces
            ts.RemoveFaces(facesToDelete);
            foreach (var edge in edgesToDelete)
            {
                edge.From.Edges.Remove(edge);     //-6 remove e from v
                edge.To.Edges.Remove(edge);
            }
            ts.RemoveEdges(edgesToDelete);
            ts.AddVertices(verticesToAdd);
            ts.AddEdges(edgesToAdd);
            ts.AddFaces(facesToAdd);
            foreach (var face in facesToAdd)
            {
                foreach (var vertex in face.Vertices)
                {
                    vertex.Faces.Add(face);  //+2 add f to v
                    var edge = face.OtherEdge(vertex);
                    //+3 add f to e
                    //+4 add e to f
                    //+5 add v to e
                    //+6 add e to v
                }
            }
        }
    }
}

