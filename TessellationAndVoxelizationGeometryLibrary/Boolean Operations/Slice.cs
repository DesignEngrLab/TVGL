using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using StarMathLib;

namespace TVGL.Boolean_Operations
{
    /// <summary>
    /// The Slice class includes static functions for cutting a tessellated solid.
    /// Slice4 Performs the slicing operation on the prescribed flat plane. This is a NON-Destructive
    /// operation, and returns two of more new tessellated solids  in the "out" parameter
    /// lists.
    /// However, it does reference the solid's faces. So this may conflict with parallel processing.
    /// </summary>
    public static class Slice
    {
        #region Define Contact at a Flat Plane

        /// <summary>
        /// This slice function makes a seperate cut for the positive and negative side,
        /// at a specified offset in both directions. It rebuilds straddle triangles, 
        /// but only uses one of the two straddle edge intersection vertices to prevent
        /// tiny triangles from being created.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolids">The solids that are on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolids">The solids on the negative side of the plane.</param>
        public static void OnFlat(TessellatedSolid ts, Flat plane,
            out List<TessellatedSolid> positiveSideSolids, out List<TessellatedSolid> negativeSideSolids)
        {
            positiveSideSolids = new List<TessellatedSolid>();
            negativeSideSolids = new List<TessellatedSolid>();
            List<PolygonalFace> positiveSideFaces;
            List<PolygonalFace> negativeSideFaces;
            List<Loop> positiveSideLoops;
            List<Loop> negativeSideLoops;
            var isSuccessful = GetLoops(ts, plane, out positiveSideFaces, out negativeSideFaces, out positiveSideLoops, out negativeSideLoops);
            if (!isSuccessful) return;
            var contactDataForEachPositiveSolid = MakeContactDataForEachSolid(ts, positiveSideLoops, positiveSideFaces, plane.Normal);
            positiveSideSolids = contactDataForEachPositiveSolid.Select(contactData => MakeSolid(plane.Normal.multiply(-1), contactData)).ToList();
            var contactDataForEachNegativeSolid = MakeContactDataForEachSolid(ts, negativeSideLoops, negativeSideFaces, plane.Normal);
            negativeSideSolids = contactDataForEachNegativeSolid.Select(contactData => MakeSolid(plane.Normal, contactData)).ToList();
        }

        /// <summary>
        /// This function returns the loops from slicing a solid along a plane. In addition,
        /// it returns the negative and positive side faces. This function does not return
        /// watertight solids. To get watertight solids, send the outputs from GetLoops to 
        /// MakeSolids, or simply call OnFlat which does both.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="plane"></param>
        /// <param name="positiveSideFaces"></param>
        /// <param name="negativeSideFaces"></param>
        /// <param name="positiveSideLoops"></param>
        /// <param name="negativeSideLoops"></param>
        public static bool GetLoops(TessellatedSolid ts, Flat plane, 
            out List<PolygonalFace> positiveSideFaces, out List<PolygonalFace> negativeSideFaces, 
            out List<Loop> positiveSideLoops, out List<Loop> negativeSideLoops)
        {
            positiveSideFaces = new List<PolygonalFace>();
            negativeSideFaces = new List<PolygonalFace>();
            positiveSideLoops = new List<Loop>();
            negativeSideLoops = new List<Loop>();
            //MiscFunctions.IsSolidBroken(ts);
            //1. Offset positive and get the positive faces.
            //Straddle faces are split into 2 or 3 new faces.
            //Note that this ensures that the loops are made from all new vertices
            //and are unique for the positive and negative sides.
            List<double> distancesToPlane;
            double posPlaneShift;
            double negPlaneShift;
            var isSuccessful = ShiftPlaneForRobustCut(ts, plane, out distancesToPlane, out posPlaneShift,
                out negPlaneShift);
            if (!isSuccessful)
            {
                return false; //End with both lists of children empty;
            }
            DivideUpFaces(ts, new Flat(plane.DistanceToOrigin + posPlaneShift, plane.Normal), out positiveSideFaces,
                out positiveSideLoops, 1, new List<double>(distancesToPlane), posPlaneShift);
            foreach (var face in positiveSideFaces)
            {
                face.CreatedInFunction = "Original Positive Side Face";
            }
            DivideUpFaces(ts, new Flat(plane.DistanceToOrigin + negPlaneShift, plane.Normal), out negativeSideFaces,
                out negativeSideLoops, -1, new List<double>(distancesToPlane), negPlaneShift);
            
            foreach (var face in negativeSideFaces)
            {
                face.CreatedInFunction = "Original Negative Side Face";
            }
            return true;
        }

        /// <summary>
        /// Takes the loops and faces from GetLoops and outputs watertight solids.
        /// </summary>
        /// <param name="normal"></param>
        /// <param name="solidContactData"></param>
        public static TessellatedSolid MakeSolid(double[] normal, ContactData solidContactData)
        {
            var allOnSideFaces = solidContactData.AllOnSideFaces;
            //3. Triangulate that empty space and add to list 
            List<List<Vertex[]>> triangleFaceList;
            var onSideVertexLoops = new List<List<Vertex>>();
            onSideVertexLoops.AddRange(solidContactData.AllLoops.Select(n => n.VertexLoop));
            var triangles = TriangulatePolygon.Run(onSideVertexLoops, normal, out triangleFaceList);
            allOnSideFaces.AddRange(triangles.Select(triangle => new PolygonalFace(triangle, normal, false){CreatedInFunction = "Slice4: Triangulation"}));
            //Create a new solid
            return new TessellatedSolid(allOnSideFaces);
        }

        /// <summary>
        /// Seperates the loops into multiple solids prior to making solids by using
        /// loop information and face wrapping to identify which loops belong together.
        /// The direction of each loop is not necessary as it can be inferred.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="onSideLoops"></param>
        /// <param name="normal"></param>
        /// <returns></returns>
        private static List<ContactData> MakeContactDataForEachSolid(TessellatedSolid ts, List<Loop> onSideLoops, List<PolygonalFace> onSideFaces, double[] normal)
        {
            var contactDataForEachSolid = new List<ContactData>();
            var hashSetFaces = new HashSet<PolygonalFace>(onSideFaces);
            //Order the loops into groups and determine positive or negative for each loop 
            //Each group consists of one positive loop, but may include no or many negative loops.
            //No negative loop will be inside of two positive loops. No positive loop will be inside another positive loop. 
            //(NOTE: although they technically can be 'inside' other loops, there is no need for such a complicated tree of groupings)
            var onSideVertexLoops = onSideLoops.Select(loop => loop.VertexLoop).ToList();
            var isPositive = new bool[onSideLoops.Count];
            //ToDo: Could come up with a more efficient way to order the loops, but this works.
            var groupsOfLoopsIndices = TriangulatePolygon.OrderLoops(onSideVertexLoops, normal, ref isPositive, false);
            for (var i = 0; i < isPositive.Length; i++)
            {
                onSideLoops[i].IsPositive = isPositive[i];
            }
            //Put the groups of loops into a GroupOfLoops class.
            var groupsOfLoops = new List<GroupOfLoops>();
            foreach (var group in groupsOfLoopsIndices)
            {
                var positiveLoop = onSideLoops[group.First()];
                var negativeLoops = new List<Loop>();
                if (!positiveLoop.IsPositive) throw new Exception("This loop should always be positive. Check to may sure the group was created correctly in 'OrderLoops' ");
                //Skip the first loop, since that is the positive loop
                for (var j = 1; j < group.Count; j++)
                {
                    var negativeLoop = onSideLoops[group[j]];
                    if (negativeLoop.IsPositive) throw new Exception("This loop should always be negative. Check to may sure the group was created correctly in 'OrderLoops' ");
                    negativeLoops.Add(negativeLoop);
                }
                var groupOfLoops = new GroupOfLoops(positiveLoop, negativeLoops);
                groupsOfLoops.Add(groupOfLoops);
            }

            //Perform face wrapping (using adjacency to build up a list of all the faces on a solid) -- Similar to 'GetMultipleSolids'
            //The straddle faces form the barrier for the wrapping procedure.
            while(groupsOfLoops.Any())
            {
                var groupOfLoops = groupsOfLoops.First();
                var allLoopsBelongingToSolid = new List<Loop>(groupOfLoops.AllLoops);
                groupsOfLoops.RemoveAt(0);
                //Push all the adjacent onside faces to a stack
                //Note that blind pockets and holes are also included in this loop, since the onside faces for every loop in the group are included
                var straddleFaceIndices = groupOfLoops.StraddleFaceIndices;
                var facesBelongingToSolid = new HashSet<PolygonalFace>();
                var stack = new Stack<PolygonalFace>();
                var usedFaces = new HashSet<PolygonalFace>();
                foreach (var adjOnsideFaceIndex in groupOfLoops.AdjOnsideFaceIndices)
                {
                    stack.Push(ts.Faces[adjOnsideFaceIndex]);
                    usedFaces.Add(ts.Faces[adjOnsideFaceIndex]);
                }
                while (stack.Any())
                {
                    var face = stack.Pop();
                    if (!hashSetFaces.Contains(face)) throw new Exception("The face should be in this list. Otherwise, it should not have been selected with face wrapping"); //ToDo: Remove this line and 'onSideFaces' from this function when convinced its working well
                    if (facesBelongingToSolid.Contains(face)) continue;
                    facesBelongingToSolid.Add(face);
                    foreach (var adjacentFace in face.AdjacentFaces)
                    {
                        if (usedFaces.Contains(adjacentFace)) continue;
                        usedFaces.Add(adjacentFace);
                        if (adjacentFace == null) continue; //This is an error. Handle it in the error function.
                        if (straddleFaceIndices.Contains(adjacentFace.IndexInList)) continue; //Don't add the straddle face. 
                        //If the wrapping gets to any faces that are straddle faces for other loops, then that group of loops is part of the same solid.
                        //Note that blind pockets and holes are also included in this loop, since the onside faces for every loop in the group are included
                        var notStraddleEdge = true;
                        for (var i = 0; i < groupsOfLoops.Count; i++)
                        {
                            if (!groupsOfLoops[i].StraddleFaceIndices.Contains(adjacentFace.IndexInList)) continue;
                            //Update the straddleFaceIndexList
                            foreach (var faceIndex in groupsOfLoops[i].StraddleFaceIndices)
                            {
                                straddleFaceIndices.Add(faceIndex);
                            }
                            notStraddleEdge = false;
                            //Don't add the straddle face. 
                            //Add all the adjacenet onside face indexes to the stack
                            foreach (var adjOnsideFaceIndex in groupsOfLoops[i].AdjOnsideFaceIndices)
                            {
                                if (usedFaces.Contains(ts.Faces[adjOnsideFaceIndex])) continue;
                                usedFaces.Add(ts.Faces[adjOnsideFaceIndex]);
                                stack.Push(ts.Faces[adjOnsideFaceIndex]);
                            }
                            //Add this the loops in this group to this solid
                            allLoopsBelongingToSolid.AddRange(groupsOfLoops[i].AllLoops);
                            //Remove that group from the list of groups
                            groupsOfLoops.RemoveAt(i);
                        } 
                        if (notStraddleEdge) stack.Push(adjacentFace);
                    }
                }
                contactDataForEachSolid.Add(new ContactData(allLoopsBelongingToSolid, facesBelongingToSolid.ToList()));
            } 
            return contactDataForEachSolid;
        }

        private static bool ShiftPlaneForRobustCut(TessellatedSolid ts, Flat plane, out List<double> distancesToPlane, out double posPlaneShift, out double negPlaneShift)
        {
            //Set the distance of every vertex in the solid to the plane
            distancesToPlane = new List<double>();
            posPlaneShift = 0;
            negPlaneShift = 0;
            var distancesToPosPlane = new List<double>();
            var distancesToNegPlane = new List<double>();
            var atLeastOneVertexOnPlane = false;
            var pointOnPlane = plane.Normal.multiply(plane.DistanceToOrigin);
            foreach (var vertex in ts.Vertices)
            {
                var distance = vertex.Position.subtract(pointOnPlane).dotProduct(plane.Normal);
                distancesToPlane.Add(distance);
                if (distance > 0) distancesToPosPlane.Add(distance);
                else if (distance < 0) distancesToNegPlane.Add(Math.Abs(distance));
                else atLeastOneVertexOnPlane = true;
            } 

            //Make sure the plane actually cuts the part into two or more parts
            if (!distancesToNegPlane.Any() || !distancesToPosPlane.Any()) return false;
            
            //Sort Results
            distancesToPosPlane.Sort();
            //This will sort it from small negative to large negative values (magnitude), since the input was the 
            //absolute value of distance
            distancesToNegPlane.Sort();

            //Check if EXACT current plane is sufficient
            var minimumShift = Math.Sqrt(ts.SameTolerance);
            if (!atLeastOneVertexOnPlane && distancesToPosPlane[0] > minimumShift &&
                distancesToNegPlane[0] > minimumShift) return true;
      
            //Shift the plane a small amount positive and negative, creating the respective disctanceToPlane lists
            //This forces NO vertices to be "on plane," making the slice function simpler in that it only deals
            //with straddle edges. 
            //However, if there are no "in plane edges" currently, then the plane does not need to be shifted
            //Because of the way distance to origin is found in relation to the normal, always add a positive offset to move further 
            //along direction of normal, and add a  negative offset to move backward along normal.
            var i = 0;
            var difference = distancesToPosPlane[i];
            if (difference >= 2 * minimumShift) posPlaneShift = minimumShift;
            else
            {
                while (difference < 2 * minimumShift)
                {
                    i++;
                    if (i == distancesToPosPlane.Count) return false; //This plane is essentially on an outer face. Don't pursue.
                    difference = distancesToPosPlane[i] - distancesToPosPlane[i - 1];
                }
                //i will be greater than 1 since the first difference must be less than ts.SameTolerance
                posPlaneShift = distancesToPosPlane[i - 1] + minimumShift;
            }

            //Now do the negative side
            i = 0;
            difference = distancesToNegPlane[i];
            if (difference >= 2 * minimumShift) negPlaneShift = -minimumShift;
            else
            {
                while (difference < 2 * minimumShift)
                {
                    i++;
                    if (i == distancesToNegPlane.Count) return false; //This plane is essentially on an outer face. Don't pursue.
                    difference = distancesToNegPlane[i] - distancesToNegPlane[i - 1];
                }
                //Subtract the distance to plane and minimum shift to make a negative shift to the plane
                negPlaneShift = - distancesToNegPlane[i - 1] - minimumShift;
            }
            return true;
        }

        ///Returns a list of onSideFaces from the ts (not including straddle faces), and a list of all the new faces that make up the 
        /// halves of the straddle faces that are on this side.
        private static void DivideUpFaces(TessellatedSolid ts, Flat plane, out List<PolygonalFace> onSideFaces, out List<Loop> loops,
            int isPositiveSide, IList<double> distancesToPlane, double planeOffset = double.NaN)
        {
            onSideFaces = new List<PolygonalFace>();
            loops = new List<Loop>();

            //If offset exists, go ahead and make offset
            if (!double.IsNaN(planeOffset))
            {
                for (var i = 0; i < distancesToPlane.Count; i++)
                {
                    distancesToPlane[i] = distancesToPlane[i] - planeOffset;
                    if(Math.Abs(distancesToPlane[i]) < ts.SameTolerance) throw new Exception("Issue in implementation of shift plane function");
                }
            }
            
            //Find all the straddle edges and add the new intersect vertices to both the pos and nef loops.
            //Also, find which faces are on the current side of the plane, by using edges.
            //Every face should have either 2 or 0 straddle edges, but never just 1.
            var straddleEdges = new List<StraddleEdge>();
            var straddleFaces = new Dictionary<int, PolygonalFace>();
            var tempOnSideFaces = new HashSet<int>();
            var listEdges = new Dictionary<int, Edge>();
            foreach (var edge in ts.Edges)
            {
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                //Check for a straddle edge (Signs are different)
                if (Math.Sign(toDistance) == Math.Sign(fromDistance))
                {
                    if (Math.Sign(toDistance) == Math.Sign(isPositiveSide))
                    {
                        for (var i = 0; i < 2; i++)
                        {
                            var face = i == 0 ? edge.OwnedFace : edge.OtherFace;
                            if(tempOnSideFaces.Contains(face.IndexInList))
                            {
                                onSideFaces.Add(face);
                                tempOnSideFaces.Remove(face.IndexInList);
                            }
                            else if (straddleFaces.ContainsKey(face.IndexInList))
                            {
                                tempOnSideFaces.Add(face.IndexInList);
                                straddleFaces.Remove(face.IndexInList);
                            }
                            else
                            {
                                straddleFaces.Add(face.IndexInList, face);
                            }
                        }
                    }
                    continue;
                }
                //If it is a straddle edge, then figure out which vertex is the offSideVertex (the one we aren't keeping)
                Vertex offSideVertex;
                if (isPositiveSide == 1) offSideVertex = toDistance > 0 ? edge.From : edge.To;
                else offSideVertex = toDistance > 0 ? edge.To : edge.From;
                straddleEdges.Add(new StraddleEdge(edge, plane, offSideVertex));
                listEdges.Add(edge.IndexInList, edge);
            }
            if(tempOnSideFaces.Any()) throw new Exception("Every face should have either 2 or 0 straddle edges, but never just 1.");

            //Get all the edges that make up the boundary being kept
            var boundaryEdges = new Dictionary<int, Edge>();
            foreach (var face in straddleFaces.Values)
            {
                face.CreatedInFunction = "Original Straddle Face";
                foreach (var edge in face.Edges.Where(edge => !listEdges.ContainsKey(edge.IndexInList) && !boundaryEdges.ContainsKey(edge.IndexInList)))
                {
                    boundaryEdges.Add(edge.IndexInList, edge);
                }
            }
            //Get loops of straddleEdges 
            var loopsOfStraddleEdges = new List<List<StraddleEdge>>();
            var loopsOfStraddleFaceIndices = new List<HashSet<int>>();
            var maxCount = straddleEdges.Count/3;
            var attempts = 0;
            while (straddleEdges.Any() && attempts < maxCount)
            {
                attempts++;
                var loopOfStraddleEdges = new List<StraddleEdge>();
                var loopOfStraddleFaceIndices = new HashSet<int>();
                var straddleEdge = straddleEdges[0];
                loopOfStraddleEdges.Add(straddleEdge);
                straddleEdges.RemoveAt(0);
                var startFace = straddleEdge.Edge.OwnedFace;
                loopOfStraddleFaceIndices.Add(startFace.IndexInList);
                var newStartFace = straddleEdge.NextFace(startFace);
                do
                {
                    loopOfStraddleFaceIndices.Add(newStartFace.IndexInList);
                    var possibleStraddleEdges = new List<StraddleEdge>();
                    foreach (var edge in newStartFace.Edges)
                    {
                        var possibleStraddleEdge = straddleEdges.FirstOrDefault(e => e.Edge == edge);
                        if (possibleStraddleEdge != null)
                        {
                            possibleStraddleEdges.Add(possibleStraddleEdge);
                        }
                    }
                    
                    //Only two straddle edges are possible per face, and the other has already been removed from straddleEdges.
                    if (possibleStraddleEdges.Count != 1) throw new Exception("This should never happen and will cause errors down the line. Prevent it.");
                    straddleEdge = possibleStraddleEdges[0];
                    loopOfStraddleEdges.Add(straddleEdge);
                    straddleEdges.Remove(straddleEdge);
                    var currentFace = newStartFace;
                    newStartFace = straddleEdge.NextFace(currentFace);
                } while (newStartFace != startFace);
                if (loopOfStraddleEdges.Count < 3) continue; //Ignore this loop, since it seems to be a knife edge 
                loopsOfStraddleEdges.Add(loopOfStraddleEdges);
                loopsOfStraddleFaceIndices.Add(loopOfStraddleFaceIndices);
            }
            if(straddleEdges.Any()) throw new Exception("While loop was unable to complete.");
            
            //Get loops of vertices, adding newly creates faces to onSideFaces as you go
            //This is the brains of this function. It loops through the straddle edges to 
            //create new faces. This function avoids creating two new points that are 
            //extremely close together, which should avoid neglible edges and faces.
            //It also keeps track of how many new vertices should be created.
            var newVertexIndex = ts.NumberOfVertices;
            var allNewFaces = new List<PolygonalFace>();
            var tolerance = Math.Sqrt(ts.SameTolerance);
            for (var i = 0; i < loopsOfStraddleEdges.Count; i++) 
            {
                var loopOfStraddleEdges = loopsOfStraddleEdges[i];
                var straddleFaceIndices = loopsOfStraddleFaceIndices[i];
                var newFaces = new List<PolygonalFace>();
                var newEdges = new List<Edge>();
                var loopOfVertices = new List<Vertex>();
                var adjOnsideFaceIndices = new HashSet<int>();
                //Find a good starting edge. One with an intersect vertex far enough away from other intersection vertices.
                var k = 0; 
                var length1 = MiscFunctions.DistancePointToPoint(loopOfStraddleEdges.Last().IntersectVertex.Position,
                            loopOfStraddleEdges[k].IntersectVertex.Position);
                while (length1.IsNegligible(tolerance) && k + 1 != loopOfStraddleEdges.Count - 1)
                {
                    k++;   
                    length1 = MiscFunctions.DistancePointToPoint(loopOfStraddleEdges[k-1].IntersectVertex.Position,
                        loopOfStraddleEdges[k].IntersectVertex.Position);
                }
                if (k +1 == loopOfStraddleEdges.Count-1) throw new Exception("No good starting edge found. Rewrite the function to find a better edge");
                var firstStraddleEdge = loopOfStraddleEdges[k];
                var previousStraddleEdge = firstStraddleEdge;
                var successfull = false;
                do 
                {
                    //ToDo: this function allows loops of two vertices if created vertices are too close together
                    k++; //Update the index
                    if (k > loopOfStraddleEdges.Count - 1) k = 0; //Set back to start if necessary
                    var currentStraddleEdge = loopOfStraddleEdges[k];
                    var length = MiscFunctions.DistancePointToPoint(currentStraddleEdge.IntersectVertex.Position,
                            previousStraddleEdge.IntersectVertex.Position);
                    
                    //If finished, then create the final face and end
                    if (currentStraddleEdge == firstStraddleEdge)
                    {
                        if (length.IsNegligible(tolerance)) throw new Exception("pick a different starting edge");
                        if (loopOfVertices.Last() != previousStraddleEdge.IntersectVertex)
                        {
                            previousStraddleEdge.IntersectVertex.IndexInList = newVertexIndex++;
                            loopOfVertices.Add(previousStraddleEdge.IntersectVertex);
                        }
                        newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge , ref newEdges, ref adjOnsideFaceIndices, true));                   
                        successfull = true;
                    }
                    //If too close together for a good triangle
                    else if (length.IsNegligible(tolerance))
                    {
                        currentStraddleEdge.IntersectVertex = previousStraddleEdge.IntersectVertex;
                        if (!loopOfVertices.Any() || loopOfVertices.Last() != previousStraddleEdge.IntersectVertex)
                        {
                            previousStraddleEdge.IntersectVertex.IndexInList = newVertexIndex++;
                            loopOfVertices.Add(previousStraddleEdge.IntersectVertex);
                        }
                        if (currentStraddleEdge.OnSideVertex == previousStraddleEdge.OnSideVertex)
                        {
                            if (currentStraddleEdge.OwnedFace == previousStraddleEdge.OwnedFace)
                                previousStraddleEdge.OwnedFace = currentStraddleEdge.OtherFace;
                            else if (currentStraddleEdge.OwnedFace == previousStraddleEdge.OtherFace) 
                                previousStraddleEdge.OtherFace = currentStraddleEdge.OtherFace;
                            else if (currentStraddleEdge.OtherFace == previousStraddleEdge.OwnedFace)
                                previousStraddleEdge.OwnedFace = currentStraddleEdge.OwnedFace;
                            else if(currentStraddleEdge.OtherFace == previousStraddleEdge.OtherFace) 
                                previousStraddleEdge.OtherFace = currentStraddleEdge.OwnedFace;
                            else throw new Exception("No shared face exists between these two straddle edges");
                            previousStraddleEdge.OffSideVertex = currentStraddleEdge.OffSideVertex;
                        }
                        else
                        {
                            newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge, ref newEdges, ref adjOnsideFaceIndices)); 
                            previousStraddleEdge = currentStraddleEdge;
                        }
                    }
                    else
                    {
                        if (!loopOfVertices.Any() || loopOfVertices.Last() != previousStraddleEdge.IntersectVertex)
                        {
                            previousStraddleEdge.IntersectVertex.IndexInList = newVertexIndex++;
                            loopOfVertices.Add(previousStraddleEdge.IntersectVertex);
                        }
                        newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge, ref newEdges, ref adjOnsideFaceIndices)); 
                        previousStraddleEdge = currentStraddleEdge;
                    }
                } while (!successfull);
                if (loopOfVertices.Count < 3) throw new Exception("This could be a knife edge. But this error will likely cause errors down the line");
                loops.Add(new Loop(loopOfVertices, newFaces, plane.Normal, straddleFaceIndices, adjOnsideFaceIndices)); 
                allNewFaces.AddRange(newFaces);
            }
            
            foreach (var face in allNewFaces)
            {
                face.CreatedInFunction = "Slice4: Divide up faces";
            }
            onSideFaces.AddRange(allNewFaces);
        }

        /// <summary>
        /// Creates a new face given two straddle edges
        /// </summary>
        /// <param name="st1"></param>
        /// <param name="st2"></param>
        /// <param name="newEdges"></param>
        /// <param name="adjOnsideFaceIndices"></param>
        /// <param name="lastNewFace"></param>
        /// <returns></returns>
        public static List<PolygonalFace> NewFace(StraddleEdge st1, StraddleEdge st2, ref List<Edge> newEdges, ref HashSet<int> adjOnsideFaceIndices, bool lastNewFace = false )
        {
            PolygonalFace sharedFace;
            if (st1.OwnedFace == st2.OwnedFace || st1.OwnedFace == st2.OtherFace) sharedFace = st1.OwnedFace;
            else if (st1.OtherFace == st2.OwnedFace || st1.OtherFace == st2.OtherFace) sharedFace = st1.OtherFace;
            else throw new Exception("No shared face exists between these two straddle edges");

            //Make an extra edge if the first new face
            if (!newEdges.Any())
            {
                var newEdge = new Edge(st1.IntersectVertex, st1.OnSideVertex,  false);
                newEdges.Add(newEdge);
            }

            if (st1.IntersectVertex == st2.IntersectVertex)
            {
                //Make one new edge and one new face. Set the ownership of this edge.
                var newFace =
                    new PolygonalFace(new List<Vertex> {st1.OnSideVertex, st1.IntersectVertex, st2.OnSideVertex},
                        sharedFace.Normal, false);
                newEdges.Last().OtherFace = newFace;
                if (!lastNewFace)
                    newEdges.Add(new Edge(st2.IntersectVertex, st2.OnSideVertex, false) {OwnedFace = newFace});
                else newEdges.First().OwnedFace = newFace;

                //Store index of face on boundary edge.
                foreach (var edge in sharedFace.Edges)
                {
                    //First, find the boundary edge
                    //No duplicates are to be included in the adjOnsideFaceIndices HashSet.
                    if (edge == st1.Edge || edge == st2.Edge) continue;
                    var boundaryEdge = edge;
                    if (boundaryEdge.OwnedFace == sharedFace)
                    {
                        if (adjOnsideFaceIndices.Contains(boundaryEdge.OtherFace.IndexInList)) continue;
                        adjOnsideFaceIndices.Add(boundaryEdge.OtherFace.IndexInList);
                    }
                    else if (boundaryEdge.OtherFace == sharedFace)
                    {
                        if (adjOnsideFaceIndices.Contains(boundaryEdge.OwnedFace.IndexInList)) continue;
                        adjOnsideFaceIndices.Add(boundaryEdge.OwnedFace.IndexInList);
                    }
                    else throw new Exception("This should never be the case. The boundary edge should be have the sharedFace as owned or other");
                    break;
                }
                return new List<PolygonalFace> {newFace};
            }
            if (st1.OffSideVertex == st2.OffSideVertex || st1.OriginalOffSideVertex == st2.OffSideVertex || st1.OffSideVertex == st2.OriginalOffSideVertex) //If not the same intersect vertex, then the same offSideVertex denotes two Consecutive curved edges, so this creates two new faces
            {
                //Create two new faces
                var newFace1 =
                    new PolygonalFace(new List<Vertex> {st1.OnSideVertex, st1.IntersectVertex, st2.IntersectVertex},
                        sharedFace.Normal, false); 
                var newFace2 =
                    new PolygonalFace(new List<Vertex> {st1.OnSideVertex, st2.IntersectVertex, st2.OnSideVertex},
                        sharedFace.Normal, false); 
                //Update ownership of most recently created edge
                newEdges.Last().OtherFace = newFace1;
                //Create new edges and update their ownership 
                var newEdge1 = new Edge(st1.IntersectVertex, st2.IntersectVertex, false) { OwnedFace = newFace1};
                var newEdge2 = new Edge(st1.OnSideVertex, st2.IntersectVertex, false) { OwnedFace = newFace2, OtherFace = newFace1};
                newEdges.AddRange(new List<Edge> { newEdge1, newEdge2});
                //Create the last edge, if this is not the last new face
                if (!lastNewFace) newEdges.Add(new Edge(st2.IntersectVertex, st2.OnSideVertex, false) { OwnedFace = newFace2});
                else newEdges.First().OwnedFace = newFace2;

                //Store index of face on boundary edge.
                //No duplicates are to be included in the adjOnsideFaceIndices HashSet.
                foreach (var edge in sharedFace.Edges)
                {
                    //First, find the boundary edge
                    if (edge == st1.Edge || edge == st2.Edge) continue;
                    var boundaryEdge = edge;
                    if (boundaryEdge.OwnedFace == sharedFace)
                    {
                        if (adjOnsideFaceIndices.Contains(boundaryEdge.OtherFace.IndexInList)) continue;
                        adjOnsideFaceIndices.Add(boundaryEdge.OtherFace.IndexInList);
                    }
                    else if (boundaryEdge.OtherFace == sharedFace)
                    {
                        if (adjOnsideFaceIndices.Contains(boundaryEdge.OwnedFace.IndexInList)) continue;
                        adjOnsideFaceIndices.Add(boundaryEdge.OwnedFace.IndexInList);
                    }
                    else throw new Exception("This should never be the case. The boundary edge should be have the sharedFace as owned or other");
                    break;
                }
                return new List<PolygonalFace> {newFace1, newFace2};
            }
            if (st1.OnSideVertex == st2.OnSideVertex)
            {
                //Make two new edges and one new face. Set the ownership of the edges.
                var newFace =
                    new PolygonalFace(new List<Vertex> {st1.OnSideVertex, st1.IntersectVertex, st2.IntersectVertex},
                        sharedFace.Normal, false);
                //Update ownership of most recently created edge
                newEdges.Last().OtherFace = newFace;
                //Create new edges and update their ownership 
                newEdges.Add(new Edge(st1.IntersectVertex, st2.IntersectVertex, false) { OwnedFace = newFace });
                if (!lastNewFace) newEdges.Add(new Edge(st2.IntersectVertex, st2.OnSideVertex, false) { OwnedFace = newFace }); 
                else newEdges.First().OwnedFace = newFace;
                return new List<PolygonalFace> { newFace };
            }
            throw new Exception("Error, the straddle edges do not match up at a common vertex");
        }

        /// <summary>
        /// Straddle edge references original edge and an intersection vertex.
        /// </summary>
        public class StraddleEdge
        {
            /// <summary>
            /// Point of edge / plane intersection
            /// </summary>
            public Vertex IntersectVertex;

            /// <summary>
            /// Vertex on side of plane that will not be kept
            /// </summary>
            public Vertex OffSideVertex;

            /// <summary>
            /// Vertex on side of plane that will not be kept (Used when collapsing an edge)
            /// </summary>
            public Vertex OriginalOffSideVertex;

            /// <summary>
            /// Vertex on side of plane that will be kept
            /// </summary>
            public Vertex OnSideVertex;

            /// <summary>
            /// Connect back to the base edge
            /// </summary>
            public Edge Edge;

            /// <summary>
            /// OwnedFace (may change if collapsed into another straddle edge)
            /// </summary>
            public PolygonalFace OwnedFace;

            /// <summary>
            /// OtherFace (may change if collapsed into another straddle edge)
            /// </summary>
            public PolygonalFace OtherFace;

            internal StraddleEdge(Edge edge, Flat plane, Vertex offSideVertex)
            {
                OwnedFace = edge.OwnedFace;
                OtherFace = edge.OtherFace;
                Edge = edge;
                OffSideVertex = offSideVertex;
                OriginalOffSideVertex = offSideVertex;
                OnSideVertex = Edge.OtherVertex(OffSideVertex);
                IntersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal, plane.DistanceToOrigin, edge.To, edge.From);
                if (IntersectVertex == null) throw new Exception("Cannot Be Null");
            }

            /// <summary>
            /// Gets the next face in the loop from this edge, given the current face
            /// </summary>
            /// <param name="face"></param>
            /// <returns></returns>
            public PolygonalFace NextFace(PolygonalFace face)
            {
                return Edge.OwnedFace == face ? Edge.OtherFace : Edge.OwnedFace;
            }
        }
        #endregion
    }
}