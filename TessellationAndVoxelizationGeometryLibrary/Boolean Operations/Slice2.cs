using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StarMathLib;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// </summary>
    public static class Slice2
    {
        #region Define Contact at a Flat Plane

        /// <summary>
        /// When the tessellated solid is sliced at the specified plane, the contact surfaces are
        /// described by the returned ContactData object. This is a non-destructive function typically
        /// used to find the shape and size of 2D surface on the prescribed plane.
        /// </summary>
        /// <param name="plane">The plane.</param>
        /// <param name="ts">The ts.</param>
        /// <param name="artificiallyCloseOpenLoops">The artificially close open loops.</param>
        /// <returns>ContactData.</returns>
        /// <exception cref="System.Exception">Contact Edges found that are not contained in loop.</exception>
        public static List<List<Loop>> DefineContact(Flat plane, TessellatedSolid ts, out List<PolygonalFace> inPlaneFaces, bool artificiallyCloseOpenLoops = true)
        {
            // Contact elements are constructed and then later arranged into loops. Loops make up the returned object, ContactData.
            // Throughout the operations in this method, the distance a given vertex is from the plane is needed. In order to avoid 
            // calculating these distances multiple times, we first construct an array of distances.
            var distancesToPlane = new List<double>();
            for (int i = 0; i < ts.NumberOfVertices; i++)
            {
                var pointOnPlane = plane.Normal.multiply(plane.DistanceToOrigin);
                distancesToPlane.Add(ts.Vertices[i].Position.subtract(pointOnPlane).dotProduct(plane.Normal));
                ts.Vertices[i].IndexInList = i;
            }  
            // **** GetContactElements is the first main function of this method. *****
            var contactElements = GetContactElements(plane, ts, distancesToPlane, out inPlaneFaces);
            DivideUpContact(ts, contactElements, inPlaneFaces, plane);
            DuplicateVerticesAtContact(contactElements, ts.NumberOfEdges);
            // Now arrange contact elements into loops. This is what the following while-loop accomplishes
            var positiveSolidLoops = new List<Loop>();
            var negativeSolidLoops = new List<Loop>();
            var allLoops = new List<Loop>();
            var numberOfTries = 0;
            while (numberOfTries < contactElements.Count)
            {   // if at first you don't succeed, try, try again! The loop stops when the number of failed
                // attempts is equal to the number of remainging contact elements.
                // **** FindLoop is the second main function of this method. *****
                var loop = FindLoop(ref contactElements, plane, distancesToPlane, artificiallyCloseOpenLoops);
                if (loop != null)
                {
                    Debug.WriteLine(allLoops.Count+ ": " + loop.MakeDebugContactString() + "  ");
                    allLoops.Add(loop);
                    if (loop.OnNegativeSolids) negativeSolidLoops.Add(loop);
                    if (loop.OnPositiveSolids) positiveSolidLoops.Add(loop);
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
            if (numberOfTries > 0) Debug.WriteLine("{0} Contact Elements found that are not contained in loop.", contactElements.Count);
            return new List<List<Loop>> { allLoops, negativeSolidLoops, positiveSolidLoops };
        }

        #region  Get Contact Elements
        private static List<ContactElement> GetContactElements(Flat plane, TessellatedSolid ts, List<double> distancesToPlane, out List<PolygonalFace> inPlaneFaces)
        {
            // the edges serve as the easiest way to identify where the solid is interacting with the plane, so we search over those
            // and organize the edges (or vertices into the following three categories: edges that straddle the plane (straddleEdges),
            // edges that are on the plane (inPlaneEdges), and edges endpoints (or rather just the vertex in question) that are in the
            // plane.
            var straddleEdges = new List<Edge>();
            var inPlaneEdges = new List<Edge>();
            var inPlaneVerticesHash = new HashSet<Vertex>();  //since these will be found multiple times, in the following loop, 
            // the hash-set allows us to quickly check if the v is already included
            foreach (var edge in ts.Edges)
            {
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                if (toDistance.IsNegligible())
                    // the head of the edge is on the plane--> inPlaneVertex
                    if (!inPlaneVerticesHash.Contains(edge.To)) inPlaneVerticesHash.Add(edge.To);
                if (fromDistance.IsNegligible())
                    // the tail of the edge is on the plane--> inPlaneVertex
                    if (!inPlaneVerticesHash.Contains(edge.From)) inPlaneVerticesHash.Add(edge.From);
                if (toDistance.IsNegligible() && fromDistance.IsNegligible())
                    // both the to and from vertices are on the plane --> inPlaneEdge
                    inPlaneEdges.Add(edge);
                else if ((toDistance > 0 && fromDistance < 0) || (toDistance < 0 && fromDistance > 0))
                    // the to and from are on either side --> straddle edge
                    straddleEdges.Add(edge);
            }
            // the following, contactElements, is what is returned by this method.
            List<ContactElement> contactElements = new List<ContactElement>();
            var inPlaneFacesHash = new HashSet<PolygonalFace>();
            foreach (var inPlaneEdge in inPlaneEdges)
            {   // inPlaneEdges are the easiest to make into ContactElements, since the end vertices
                // are simply known vertices in the solid (as opposed to new vertices that need to be made) 
                // but there are some subtle issues related to this (see preceding comments):
                // inner edges and convexity of the edges (as occurs later on).   
                var ownedFaceOtherVertex = inPlaneEdge.OwnedFace.OtherVertex(inPlaneEdge);
                var planeDistOwnedFOV = distancesToPlane[ownedFaceOtherVertex.IndexInList];
                var otherFaceOtherVertex = inPlaneEdge.OtherFace.OtherVertex(inPlaneEdge);
                var planeDistOtherFOV = distancesToPlane[otherFaceOtherVertex.IndexInList];
                var ownedFaceIsOutOfPlane = true;
                var onPositiveSide = true;
                // if both faces are in the plane then do not include this edge as a contact element.  
                if (planeDistOwnedFOV.IsNegligible()) 
                { 
                    if (!inPlaneFacesHash.Contains(inPlaneEdge.OwnedFace)) inPlaneFacesHash.Add(inPlaneEdge.OwnedFace);
                    ownedFaceIsOutOfPlane = false;
                    onPositiveSide = (planeDistOtherFOV > 0);
                }
                if (planeDistOtherFOV.IsNegligible())
                {
                    if (!inPlaneFacesHash.Contains(inPlaneEdge.OtherFace)) inPlaneFacesHash.Add(inPlaneEdge.OtherFace);
                    ownedFaceIsOutOfPlane = true;
                    onPositiveSide = (planeDistOwnedFOV > 0);
                }
                if (planeDistOwnedFOV.IsNegligible() && planeDistOtherFOV.IsNegligible()) continue;
                // if one of the faces is in the plane and the other is not, then edges are effected and a 
                // contact element should be made for them (in the last line of the foreach). This is regardless
                // whether edge is convex or concave.
                if (planeDistOwnedFOV * planeDistOtherFOV > 0) continue; //if both distances have the same sign, then 
                // this is "knife-edge" on the plane - we don't need that
                contactElements.Add(new ContactElement(inPlaneEdge, ownedFaceIsOutOfPlane, onPositiveSide));
            }
            // now things get complicated. For each straddle edge make a dictionary entry to includes the
            // straddle edge as the key and a StraddleData class for the value, which for the meantime is
            // comprised only of the new vertex on that edge. The reason for this is to ensure that newly
            // defined ContactElements use the same vertices - that adjacent elements don't make duplicates of these 
            // vertices. When a previously defined straddle is found, we update the dictionary entries.
            // These are the faces on either side of the edge that are in the backward or forward direction of the loop.
            var splitEdgeDict = straddleEdges.ToDictionary(edge => edge,
                edge => new StraddleData()
                {
                    SplitVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.From, edge.To)
                });
            // next add 0,1,or 2 ContactElements for the inPlane Vertices. Why is this not known? Because some of the vertices
            // are ends of inPlaneEdges, which are defined in the previous loop and some of edges that terminate at the plane
            foreach (var startingVertex in inPlaneVerticesHash)
            {
                Edge otherEdge;
                var straddleFace = FindForwardStraddleFace(plane, startingVertex, distancesToPlane, out otherEdge);
                if (straddleFace != null)
                {
                    var connectingData = splitEdgeDict[otherEdge];
                    contactElements.Add(new ContactElement(startingVertex, null, connectingData.SplitVertex, otherEdge, straddleFace, ContactTypes.ThroughVertex));
                    // update the dictionary entry with the fact that the face on the backward side of this forward edge has been found. A "through vertex"
                    // contact element is created for this straddle vertex.
                    connectingData.BackwardFace = straddleFace;
                }
                straddleFace = FindBackwardStraddleFace(plane, startingVertex, distancesToPlane, out otherEdge);
                if (straddleFace != null)
                {
                    var connectingData = splitEdgeDict[otherEdge];
                    contactElements.Add(new ContactElement(connectingData.SplitVertex, otherEdge, startingVertex, null, straddleFace, ContactTypes.ThroughVertex));
                    connectingData.ForwardFace = straddleFace;
                }
            }
            foreach (var keyValuePair in splitEdgeDict)
            {   // finally, we make ContactElements for the straddleEdges. This is a little tricky.
                var edge = keyValuePair.Key;
                var newVertex = keyValuePair.Value.SplitVertex;
                var backwardFace = edge.OwnedFace;
                var forwardFace = edge.OtherFace;
                if (distancesToPlane[edge.To.IndexInList] < 0)
                {   // The assignment should be reversed, given that the head of the arc
                    // is on the negative side of the plane and not the positive
                    backwardFace = edge.OtherFace;
                    forwardFace = edge.OwnedFace;
                }
                if (keyValuePair.Value.BackwardFace == null)
                {
                    var otherEdge =
                        backwardFace.Edges.FirstOrDefault(
                            e =>
                                e != edge &&
                                ((distancesToPlane[e.To.IndexInList] < 0 &&
                                  distancesToPlane[e.From.IndexInList] > 0)
                                 ||
                                 (distancesToPlane[e.To.IndexInList] > 0 &&
                                  distancesToPlane[e.From.IndexInList] < 0)));
                    if (otherEdge != null && splitEdgeDict.ContainsKey(otherEdge))
                    {
                        contactElements.Add(new ContactElement(splitEdgeDict[otherEdge].SplitVertex, otherEdge,
                            newVertex, edge, backwardFace, ContactTypes.ThroughFace));
                        splitEdgeDict[otherEdge].ForwardFace = backwardFace;
                    }
                }
                if (keyValuePair.Value.ForwardFace == null)
                {
                    var otherEdge =
                        forwardFace.Edges.FirstOrDefault(
                            e =>
                                e != edge &&
                                ((distancesToPlane[e.To.IndexInList] < 0 &&
                                  distancesToPlane[e.From.IndexInList] > 0)
                                 ||
                                 (distancesToPlane[e.To.IndexInList] > 0 &&
                                  distancesToPlane[e.From.IndexInList] < 0)));
                    if (otherEdge != null && splitEdgeDict.ContainsKey(otherEdge))
                    {
                        contactElements.Add(new ContactElement(newVertex, edge, splitEdgeDict[otherEdge].SplitVertex,
                            otherEdge, forwardFace, ContactTypes.ThroughFace));
                        splitEdgeDict[otherEdge].BackwardFace = forwardFace;
                    }
                }
            }
            inPlaneFaces = new List<PolygonalFace>(inPlaneFacesHash);
            return contactElements;
        }

        class StraddleData
        {
            internal Vertex SplitVertex;
            internal PolygonalFace BackwardFace;
            internal PolygonalFace ForwardFace;
        }

        internal static PolygonalFace FindForwardStraddleFace(Flat plane, Vertex onPlaneVertex, List<double> vertexDistancesToPlane, out Edge edge)
        {
            edge = null;
            foreach (var face in onPlaneVertex.Faces)
            {
                var otherEdge = face.OtherEdge(onPlaneVertex);
                var toDistance = vertexDistancesToPlane[otherEdge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[otherEdge.From.IndexInList];
                if ((toDistance.IsGreaterThanNonNegligible() && fromDistance.IsLessThanNonNegligible() && face == otherEdge.OwnedFace)
                    || (toDistance.IsLessThanNonNegligible() && fromDistance.IsGreaterThanNonNegligible() && face == otherEdge.OtherFace))
                {
                    edge = otherEdge;
                    return face;
                }
            }
            return null;
        }
        internal static PolygonalFace FindBackwardStraddleFace(Flat plane, Vertex onPlaneVertex, List<double> vertexDistancesToPlane, out Edge edge)
        {
            edge = null;
            foreach (var face in onPlaneVertex.Faces)
            {
                var otherEdge = face.OtherEdge(onPlaneVertex);
                var toDistance = vertexDistancesToPlane[otherEdge.To.IndexInList];
                var fromDistance = vertexDistancesToPlane[otherEdge.From.IndexInList];
                if ((toDistance.IsGreaterThanNonNegligible() && fromDistance.IsLessThanNonNegligible() && face == otherEdge.OtherFace) ||
                    (toDistance.IsLessThanNonNegligible() && fromDistance.IsGreaterThanNonNegligible() && face == otherEdge.OwnedFace))
                {
                    edge = otherEdge;
                    return face;
                }
            }
            return null;
        }
        #endregion

        #region Find Loop
        private static Loop FindLoop(ref List<ContactElement> contactElements, Flat plane, List<double> vertexDistancesToPlane, bool artificiallyCloseOpenLoops)
        {
            var startCE = contactElements[0];
            var thisCE = startCE;
            var loop = new List<ContactElement>();
            var remainingCEs = new List<ContactElement>(contactElements);
            do
            {
                var onNegativeSolid = false;
                var onPositiveSolid = false;
                if (thisCE.SplitFaceNegative != null) onNegativeSolid = true;
                if (thisCE.SplitFacePositive != null) onPositiveSolid = true;
                loop.Add(thisCE);
                remainingCEs.Remove(thisCE);
                var newStartVertex = thisCE.EndVertex;
                if (loop[0].StartVertex == newStartVertex) // then a loop is found!
                {
                    contactElements = remainingCEs;
                    return new Loop(loop, plane.Normal, true, false, false, onNegativeSolid, onPositiveSolid);
                }
                var possibleNextCEs = remainingCEs.Where(ce => ce.StartVertex == newStartVertex).ToList();
                if (!possibleNextCEs.Any())
                    possibleNextCEs = remainingCEs.Where(ce => ce.StartVertex.Position.IsPracticallySame(newStartVertex.Position)).ToList();
                if (possibleNextCEs.Count == 1) thisCE = possibleNextCEs[0];
                else if (possibleNextCEs.Count > 1)
                {
                    var minIndex = -1;
                    var minAngle = double.PositiveInfinity;
                    for (int i = 0; i < possibleNextCEs.Count; i++)
                    {
                        var angleChange = MiscFunctions.AngleBetweenEdgesCCW(thisCE.Vector, possibleNextCEs[i].Vector,
                            plane.Normal);
                        if (angleChange < minAngle)
                        {
                            minAngle = angleChange;
                            minIndex = i;
                        }
                    }
                    thisCE = possibleNextCEs[minIndex];
                }
                else if (artificiallyCloseOpenLoops)
                {
                    contactElements = remainingCEs;
                    loop.Add(new ContactElement(newStartVertex, null, loop[0].StartVertex, null, null,
                        ContactTypes.Artificial));
                    return new Loop(loop, plane.Normal, true, true, false);
                }
                else
                {   // failed to find a loop. Let's move this start contact element to the end of the list 
                    // and try again.
                    contactElements.RemoveAt(0);
                    contactElements.Add(startCE);
                    return null;
                }
            } while (true);
        }
        #endregion

        #endregion

        #region Slice On Flat
        /// <summary>
        /// Performs the slicing operation on the prescribed flat plane. This destructively alters
        /// the tessellated solid into one or more solids which are returned in the "out" parameter
        /// lists.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolids">The solids that are on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolids">The solids on the negative side of the plane.</param>
        public static void OnFlat(TessellatedSolid ts, Flat plane,
            out List<TessellatedSolid> positiveSideSolids, out List<TessellatedSolid> negativeSideSolids)
        {
            List<PolygonalFace> inPlaneFaces;
            var contactData = DefineContact(plane, ts, out inPlaneFaces, false);
            if (!contactData[0].Any() || !contactData[1].Any())
            {
                Debug.WriteLine("Plane does not pass through solid.");
                negativeSideSolids = new List<TessellatedSolid>();
                positiveSideSolids = new List<TessellatedSolid>();
                if (ts.Center.dotProduct(plane.Normal) > plane.DistanceToOrigin)
                    positiveSideSolids.Add(ts);
                else negativeSideSolids.Add(ts);
                return;
            }

            #region make negative side solids
            var negativeSolidLoops = contactData[1];
            negativeSideSolids = new List<TessellatedSolid>();
            while (negativeSolidLoops.Any())
            {
                var negativeLoops = new List<Loop>();
                var positiveLoops = new List<Loop>();
                var loop = negativeSolidLoops[0];
                List<Loop> loopsOnThisSolid = new List<Loop>(negativeSolidLoops);
                List<PolygonalFace> negativeFaceList = FindSolidWithThisLoop(loop[0], ref loopsOnThisSolid, false);
                foreach (var loopOnThisSolid in loopsOnThisSolid)
                {
                    if (loopOnThisSolid.IsPositive) positiveLoops.Add(loopOnThisSolid);
                    else negativeLoops.Add(loopOnThisSolid);
                    negativeSolidLoops.Remove(loopOnThisSolid);
                }   
                //Check if any of the remaining positive loops are between any of the current positive and negative loops.
                //The positive 
                var remainingLoops = new List<Loop>(negativeSolidLoops);
                var flag = true;
                while (remainingLoops.Any())
                {
                    var remainingLoop = remainingLoops[0];
                    remainingLoops.RemoveAt(0);
                    if (!remainingLoop.IsPositive) continue;
                    var verticesOfRemainingLoop = remainingLoop.Select(ce => ce.StartVertex).ToArray();
                    var pointsOfRemainingLoop = MiscFunctions.Get2DProjectionPoints(verticesOfRemainingLoop, plane.Normal);
                    foreach (var positiveLoop in positiveLoops)
                    {
                        var verticesOfPositiveLoop = positiveLoop.Select(ce => ce.StartVertex).ToArray();
                        var pointsOfPositiveLoop = MiscFunctions.Get2DProjectionPoints(verticesOfPositiveLoop, plane.Normal);
                        foreach (var negativeLoop in negativeLoops)
                        {
                            var verticesOfNegativeLoop = negativeLoop.Select(ce => ce.StartVertex).ToArray();
                            var pointsOfNegativeLoop = MiscFunctions.Get2DProjectionPoints(verticesOfNegativeLoop, plane.Normal);
                                
                            //Check if negative loop is inside positive loop. If no, continue to next negative loop.
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfPositiveLoop.ToList(), pointsOfNegativeLoop[0])) continue;

                            //Check if remaining loop is between positive loop and negative loop.
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfPositiveLoop.ToList(), pointsOfRemainingLoop[0])) continue;
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfRemainingLoop.ToList(), pointsOfNegativeLoop[0])) continue;
                                
                            //Add and remove loop from lists.
                            List<Loop> additionalLoopsOnThisSolid = new List<Loop>(negativeSolidLoops);
                            List<PolygonalFace> additionalFaces = FindSolidWithThisLoop(remainingLoop[0], ref additionalLoopsOnThisSolid, false);
                            negativeFaceList.AddRange(additionalFaces);
                            foreach (var additionalLoopOnThisSolid in additionalLoopsOnThisSolid)
                            {
                                if (additionalLoopOnThisSolid.IsPositive) positiveLoops.Add(additionalLoopOnThisSolid);
                                else negativeLoops.Add(additionalLoopOnThisSolid);
                                loopsOnThisSolid.Add(additionalLoopOnThisSolid);
                                negativeSolidLoops.Remove(additionalLoopOnThisSolid);
                                remainingLoops.Remove(additionalLoopOnThisSolid);
                            }
                            flag = false;
                            if (flag == false) break;
                        }
                        if (flag == false) break;
                    }                    
                }
                var numLoops = loopsOnThisSolid.Count;
                var verticesOnPlane = new Vertex[numLoops][];
                var points2D = new Point[numLoops][];
                for (int i = 0; i < numLoops; i++)
                {
                    verticesOnPlane[i] = loopsOnThisSolid[i].Select(ce => ce.StartVertex).ToArray();
                    points2D[i] = MiscFunctions.Get2DProjectionPoints(verticesOnPlane[i], plane.Normal, true);
                }
                var patchTriangles = TriangulatePolygon.Run(points2D.ToList(),
                    loopsOnThisSolid.Select(l => l.IsPositive).ToArray());
                foreach (var triangle in patchTriangles)
                    negativeFaceList.Add(new PolygonalFace(triangle, plane.Normal));
                negativeSideSolids.Add(
                    new TessellatedSolid(negativeFaceList,
                        negativeFaceList.SelectMany(f => f.Vertices).Distinct().OrderBy(v => v.IndexInList).ToList()));
            }
            #endregion
            #region make positive side solids
            var positiveSolidLoops = contactData[2];
            positiveSideSolids = new List<TessellatedSolid>();
            while (positiveSolidLoops.Any())
            {
                var negativeLoops = new List<Loop>();
                var positiveLoops = new List<Loop>();
                var loop = positiveSolidLoops[0];
                List<Loop> loopsOnThisSolid = new List<Loop>(positiveSolidLoops);
                List<PolygonalFace> positiveFaceList = FindSolidWithThisLoop(loop[0], ref loopsOnThisSolid, true);
                foreach (var loopOnThisSolid in loopsOnThisSolid)
                {
                    if (loopOnThisSolid.IsPositive) positiveLoops.Add(loopOnThisSolid);
                    else negativeLoops.Add(loopOnThisSolid);
                    positiveSolidLoops.Remove(loopOnThisSolid);
                }   
                //Check if any of the remaining positive loops are between any of the current positive and negative loops.
                //The positive 
                var remainingLoops = new List<Loop>(positiveSolidLoops);
                var flag = true;
                while (remainingLoops.Any())
                {
                    var remainingLoop = remainingLoops[0];
                    remainingLoops.RemoveAt(0);
                    if (!remainingLoop.IsPositive) continue;
                    var verticesOfRemainingLoop = remainingLoop.Select(ce => ce.StartVertex).ToArray();
                    var pointsOfRemainingLoop = MiscFunctions.Get2DProjectionPoints(verticesOfRemainingLoop, plane.Normal);
                    foreach (var positiveLoop in positiveLoops)
                    {
                        var verticesOfPositiveLoop = positiveLoop.Select(ce => ce.StartVertex).ToArray();
                        var pointsOfPositiveLoop = MiscFunctions.Get2DProjectionPoints(verticesOfPositiveLoop, plane.Normal);
                        foreach (var negativeLoop in negativeLoops)
                        {
                            var verticesOfNegativeLoop = negativeLoop.Select(ce => ce.StartVertex).ToArray();
                            var pointsOfNegativeLoop = MiscFunctions.Get2DProjectionPoints(verticesOfNegativeLoop, plane.Normal);

                            //Check if negative loop is inside positive loop. If no, continue to next negative loop.
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfPositiveLoop.ToList(), pointsOfNegativeLoop[0])) continue;

                            //Check if remaining loop is between positive loop and negative loop.
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfPositiveLoop.ToList(), pointsOfRemainingLoop[0])) continue;
                            if (!MiscFunctions.IsPointInsidePolygon(pointsOfRemainingLoop.ToList(), pointsOfNegativeLoop[0])) continue;

                            //Add and remove loop from lists.
                            List<Loop> additionalLoopsOnThisSolid = new List<Loop>(positiveSolidLoops);
                            List<PolygonalFace> additionalFaces = FindSolidWithThisLoop(remainingLoop[0], ref additionalLoopsOnThisSolid, false);
                            positiveFaceList.AddRange(additionalFaces);
                            foreach (var additionalLoopOnThisSolid in additionalLoopsOnThisSolid)
                            {
                                if (additionalLoopOnThisSolid.IsPositive) positiveLoops.Add(additionalLoopOnThisSolid);
                                else negativeLoops.Add(additionalLoopOnThisSolid);
                                loopsOnThisSolid.Add(additionalLoopOnThisSolid);
                                positiveSolidLoops.Remove(additionalLoopOnThisSolid);
                                remainingLoops.Remove(additionalLoopOnThisSolid);
                            }
                            flag = false;
                            if (flag == false) break;
                        }
                        if (flag == false) break;
                    }
                }
                var numLoops = loopsOnThisSolid.Count;
                var verticesOnPlane = new Vertex[numLoops][];
                var points2D = new Point[numLoops][];
                for (int i = 0; i < numLoops; i++)
                {
                    verticesOnPlane[i] = loopsOnThisSolid[i].Select(ce => ce.DuplicateVertex).ToArray();
                    points2D[i] = MiscFunctions.Get2DProjectionPoints(verticesOnPlane[i], plane.Normal, true);
                }
                var patchTriangles = TriangulatePolygon.Run(points2D.ToList(),
                    loopsOnThisSolid.Select(l => l.IsPositive).ToArray());
                foreach (var triangle in patchTriangles)
                    positiveFaceList.Add(new PolygonalFace(triangle, plane.Normal.multiply(-1)));
                positiveSideSolids.Add(
                    new TessellatedSolid(positiveFaceList,
                        positiveFaceList.SelectMany(f => f.Vertices).Distinct().OrderBy(v => v.IndexInList).ToList()));
            }
            #endregion
        }

        #region Divide Up Contact Data
        /// <summary>
        /// Divides up contact.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="contactData">The contact data.</param>
        /// <param name="plane">The plane.</param>
        /// <exception cref="System.Exception">face is supposed to be split at plane but lives only on one side</exception>
        private static void DivideUpContact(TessellatedSolid ts, List<ContactElement> contactElements, List<PolygonalFace> inPlaneFaces, Flat plane)
        {
            var edgesToAdd = new List<Edge>();
            var facesToAdd = new List<PolygonalFace>();
            var verticesToAdd = new List<Vertex>();
            var edgesToDelete = new List<Edge>();
            var facesToDelete = new List<PolygonalFace>(inPlaneFaces);
            var edgesToKeep = new List<Edge>();
            foreach (var face in inPlaneFaces)
            {
                foreach (var edge in face.Edges)
                {
                    //If two faces are in plane faces, delete their common edge
                    if (edgesToKeep.Contains(edge))
                    {
                        edgesToDelete.Add(edge);
                        edgesToKeep.Remove(edge);
                    }
                    else edgesToKeep.Add(edge);
                }
            }
            var edgesToModify = new List<Edge>();
            foreach (var ce in contactElements)
            {
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
                if (!ts.Vertices.Contains(ce.StartVertex)) verticesToAdd.Add(ce.StartVertex);
                if (ce.StartEdge != null) edgesToDelete.Add(ce.StartEdge);

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
                    if (ce.StartEdge != null)
                    {
                        var positiveEdge = new Edge(maxVert, ce.ContactEdge.From, positiveFace, null);
                        edgesToAdd.Add(positiveEdge);
                        edgesToModify.Add(positiveEdge);
                        var negativeEdge = new Edge(ce.ContactEdge.From, minVert, negativeFace, null);
                        edgesToAdd.Add(negativeEdge);
                        edgesToModify.Add(negativeEdge);
                    }
                    ce.ContactType = ContactTypes.AlongEdge;
                    ce.SplitFacePositive = positiveFace;
                    ce.SplitFaceNegative = negativeFace;
                } //#+1 add v to f           (both of these are done in the preceding PolygonalFace
                //#+2 add f to v            constructors as well as the one for thirdFace below)
                else if (ce.ContactType == ContactTypes.ThroughFace)
                {
                    Vertex positiveVertex, negativeVertex;
                    if (ce.StartEdge.To.Position.dotProduct(plane.Normal) > plane.DistanceToOrigin)
                    {
                        positiveVertex = ce.StartEdge.To;
                        negativeVertex = ce.StartEdge.From;
                    }
                    else
                    {
                        positiveVertex = ce.StartEdge.From;
                        negativeVertex = ce.StartEdge.To;
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
                    ce.ContactType = ContactTypes.AlongEdge;
                    ce.SplitFacePositive = positiveFace;
                    ce.SplitFaceNegative = negativeFace;
                    // for the new edges in a through face this line accomplishes: +3 add f to e; +4 add e to f; +5 add v to e; 
                    //    +6 add e to v 
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
                var facesToAttach = new List<PolygonalFace>();
                foreach (var face in facesToAdd)
                {
                    if (face.Vertices.Contains(edge.To) && face.Vertices.Contains(edge.From) && face != edge.OwnedFace && face != edge.OtherFace) facesToAttach.Add(face);
                }
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
        #endregion

        #region Duplicate Vertices At Contact
        private static void DuplicateVerticesAtContact(List<ContactElement> contactElements, int numberOfEdges)
        {
            foreach (var ce in contactElements)
            {
                //disconnect faces from edge (this prevents the depth first search from crossing over the splitting
                //plane when locating all the faces in a new solid
                if(ce.SplitFaceNegative != null) ce.SplitFaceNegative.Edges.Remove(ce.ContactEdge);
                if (ce.SplitFacePositive != null) ce.SplitFacePositive.Edges.Remove(ce.ContactEdge);
                ce.StartVertex.Edges.Remove(ce.ContactEdge);
                ce.EndVertex.Edges.Remove(ce.ContactEdge);
            }
            foreach (var ce in contactElements)
            {
                var negVertex = ce.StartVertex;
                var posVertex = negVertex.Copy();
                var negFaces = new List<PolygonalFace>();   
                var negEdges = new List<Edge>();        
                if (ce.SplitFaceNegative != null)
                {
                    negFaces = new List<PolygonalFace>(new[] { ce.SplitFaceNegative });
                    var thisNegFace = ce.SplitFaceNegative;
                    var nextNegEdge = ce.SplitFaceNegative.Edges.FirstOrDefault(e => e != null &&
                                                                                (e.To == ce.StartVertex ||
                                                                                    e.From == ce.StartVertex));
                    while (nextNegEdge != null && negEdges.Count() < numberOfEdges)
                    {
                        negEdges.Add(nextNegEdge);
                        thisNegFace = nextNegEdge.OwnedFace == thisNegFace ? nextNegEdge.OtherFace : nextNegEdge.OwnedFace;
                        if (thisNegFace == null) thisNegFace = nextNegEdge.OtherFace;
                        negFaces.Add(thisNegFace);
                        nextNegEdge = thisNegFace.Edges.FirstOrDefault(e => e != null && e != nextNegEdge &&
                            (e.To == ce.StartVertex || e.From == ce.StartVertex));
                    }
                    if (negEdges.Count() > 500 || negEdges.Count() == numberOfEdges - 1) throw new Exception();
                }
                posVertex.Faces.AddRange(negVertex.Faces.Where(f => !negFaces.Contains(f)));
                posVertex.Edges.AddRange(negVertex.Edges.Where(e => !negEdges.Contains(e)));
                negVertex.Faces.Clear();
                negVertex.Faces.AddRange(negFaces);
                negVertex.Edges.Clear();
                negVertex.Edges.AddRange(negEdges);
                foreach (var edge in posVertex.Edges)
                {
                    if (edge.To == negVertex) edge.To = posVertex;
                    else edge.From = posVertex;
                }
                foreach (var face in posVertex.Faces)
                {
                    var index = face.Vertices.IndexOf(negVertex);
                    face.Vertices[index] = posVertex;
                }
                ce.DuplicateVertex = posVertex;
            }
        }
        #endregion

        #region Find Solid With This Loop
        private static List<PolygonalFace> FindSolidWithThisLoop(ContactElement startCE, ref List<Loop> loopsOnThisSolid,  bool OnPositiveSide)
        {
            var totalNumberOfLoops = loopsOnThisSolid.Count();
            var startFace = OnPositiveSide ? startCE.SplitFacePositive : startCE.SplitFaceNegative;
            var faces = new HashSet<PolygonalFace>();
            var stack = new Stack<PolygonalFace>(new[] { startFace });
            var connectingLoops = new List<Loop>();
            while (stack.Any() && connectingLoops.Count() - 1 < totalNumberOfLoops)
            {
                var face = stack.Pop();
                if (faces.Contains(face)) continue;
                faces.Add(face);
                foreach (var adjacentFace in face.AdjacentFaces)
                {
                    if (adjacentFace == null)
                    {
                        //Add the loop containing this null adjacent face to "connectingLoops"
                        if (OnPositiveSide)
                        {
                            foreach (var loopOnThisSolid in loopsOnThisSolid)
                            {
                                foreach (var contactElement in loopOnThisSolid)
                                {
                                    if (contactElement.SplitFacePositive == face)
                                    {
                                        if (!connectingLoops.Contains(loopOnThisSolid))
                                            connectingLoops.Add(loopOnThisSolid);
                                        goto exitFor;
                                    }
                                }
                            }
                        }
                        else
                        {
                            foreach (var loopOnThisSolid in loopsOnThisSolid)
                            {
                                foreach (var contactElement in loopOnThisSolid)
                                {
                                    if (contactElement.SplitFaceNegative == face)
                                    {
                                        if (!connectingLoops.Contains(loopOnThisSolid))
                                            connectingLoops.Add(loopOnThisSolid);
                                        goto exitFor;
                                    }
                                }
                            }
                        }
                    exitFor: ;

                        //var newConnectingLoop = (OnPositiveSide) 
                        //    ? loopsOnThisSolid.First(l => l.Any(ce => ce.SplitFacePositive == face))
                        //    : loopsOnThisSolid.First(l => l.Any(ce => ce.SplitFaceNegative == face));
                        //if (!connectingLoops.Contains(newConnectingLoop))
                        //    connectingLoops.Add(newConnectingLoop);
                    }
                    //else, add the adjacent faces to "faces" (if not already there)
                    else if (!faces.Contains(adjacentFace))
                        stack.Push(adjacentFace);
                }
            }
            loopsOnThisSolid = connectingLoops;
            return new List<PolygonalFace>(faces);
        }
        #endregion

        #endregion
    }
}