using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TVGL.Numerics;

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
        /// <param name="solids">The resulting solids </param>
        /// <param name="setIntersectionGroups">Determines whether to output the intersections (2D cross sections and other info)</param>
        /// <param name="undoPlaneOffset">Determines whether to construct new faces exactly on the cutting plane</param>
        public static void OnInfiniteFlat(TessellatedSolid ts, Flat plane,
            out List<TessellatedSolid> solids, out ContactData contactData, bool setIntersectionGroups = false,
            bool undoPlaneOffset = false)
        {
            if (!GetContactData(ts, plane, out contactData, setIntersectionGroups, undoPlaneOffset:undoPlaneOffset))
            {
                solids = new List<TessellatedSolid>();
                Debug.WriteLine("CuttingPlane does not cut through the given solid.");
                return;
            }
            MakeSolids(contactData, ts.Units, out solids);
            var totalVolume1 = solids.Sum(solid => solid.Volume);
            var totalVolume2 = contactData.SolidContactData.Sum(solidContactData => solidContactData.Volume());
            if (!totalVolume2.IsPracticallySame(totalVolume1, 100))
            {
                Debug.WriteLine("Error with Volume function calculation in TVGL. SolidContactData Volumes and Solid Volumes should match, since they use all the same faces.");
                Debug.WriteLine("Contact Data Total Volume = " + totalVolume2 + ". Solid Total Volume = " + totalVolume1);
            }
        }

        /// <summary>
        /// This slice function makes a seperate cut for the positive and negative side,
        /// at a specified offset in both directions. It rebuilds straddle triangles, 
        /// but only uses one of the two straddle edge intersection vertices to prevent
        /// tiny triangles from being created.
        /// This version allows the user to input IntersectionGroups ignore by index.
        /// These are set by useing OnInfiniteFlat with setIntersectionGroups = true.
        /// 
        /// Limitation: The finite plane is limited in that it can only ignore entire groups of loops,
        /// so if two positive side groups connect with one negative side loop, they cannot be seperated
        /// individually (e.g., if two pegs are connected to a large face, you cannot remove only one peg
        /// with a flat exactly on the large face. However, you could cut it off by moving the plane
        /// slightly toward the peg. That would make two IntersectionGroups rather than one).
        /// This is because Slice was written to re-triangulate exposed surfaces from the intersection loops.
        /// This cannot currently be done for partial intersection loops. 
        /// </summary>
        public static void OnFiniteFlatByIngoringIntersections(TessellatedSolid ts, Flat plane, 
            out List<TessellatedSolid> solids, ICollection<IntersectionGroup> intersectionsToIgnore, out ContactData newContactData)
        {
            var loopsToIgnore = new List<int>();
            foreach (var intersectionGroup in intersectionsToIgnore)
            {
                loopsToIgnore.AddRange(intersectionGroup.GetLoopIndices());
            }

            if (!GetContactData(ts, plane, out newContactData, false, loopsToIgnore))
            {
                solids = new List<TessellatedSolid>();
                Debug.WriteLine("CuttingPlane does not cut through the given solid.");
                return;
            }
            MakeSolids(newContactData, ts.Units, out solids);
            var totalVolume1 = solids.Sum(solid => solid.Volume);
            var totalVolume2 = newContactData.SolidContactData.Sum(solidContactData => solidContactData.Volume());
            if (!totalVolume2.IsPracticallySame(totalVolume1, 100))
            {
                Debug.WriteLine("Error with Volume function calculation in TVGL. SolidContactData Volumes and Solid Volumes should match, since they use all the same faces.");
                Debug.WriteLine("Contact Data Total Volume = " + totalVolume2 + ". Solid Total Volume = " + totalVolume1);
            }
        }

        /// <summary>
        /// This slice function makes a seperate cut for the positive and negative side,
        /// at a specified offset in both directions. It rebuilds straddle triangles, 
        /// but only uses one of the two straddle edge intersection vertices to prevent
        /// tiny triangles from being created.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="plane">The plane.</param>
        /// <param name="positiveSideSolid">The solid that is on the positive side of the plane
        /// This means that are on the side that the normal faces.</param>
        /// <param name="negativeSideSolid">The solid on the negative side of the plane.</param>
        public static void OnFlatAsSingleSolids(TessellatedSolid ts, Flat plane,
            out TessellatedSolid positiveSideSolid, out TessellatedSolid negativeSideSolid)
        {
            if (!GetContactData(ts, plane, out var contactData, false))
            {
                positiveSideSolid = null;
                negativeSideSolid = null;
                Debug.WriteLine("CuttingPlane does not cut through the given solid.");
                return;
            }
            MakeSingleSolidOnEachSideOfInfitePlane(contactData, ts.Units, out positiveSideSolid, out negativeSideSolid);
        }

        /// <summary>
        /// Gets the contact data for a slice, without making the individual solids.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="plane"></param>
        /// <param name="contactData"></param>
        /// <param name="setIntersectionGroups"></param>
        /// <param name="loopsToIgnore"></param>
        /// <param name="undoPlaneOffset"></param>
        public static bool GetContactData(TessellatedSolid ts, Flat plane, out ContactData contactData, bool setIntersectionGroups,
            ICollection<int> loopsToIgnore = null, bool undoPlaneOffset = false)
        {
            #region Get the loops
            //1. Offset positive and get the positive faces.
            //Straddle faces are split into 2 or 3 new faces.
            //Note that this ensures that the loops are made from all new vertices
            //and are unique for the positive and negative sides.
            var isSuccessful = ShiftPlaneForRobustCut(ts, plane, out var distancesToPlane, out var posPlaneShift,
                out var negPlaneShift);
            if (!isSuccessful)
            {
                contactData = null;
                return false; //This plane does not slice through the solid, or an error occured from the plane shift
            }
            DivideUpFaces(ts, new Flat(plane.DistanceToOrigin + posPlaneShift, plane.Normal),
                out var positiveSideLoops, 1, new List<double>(distancesToPlane), posPlaneShift, loopsToIgnore, undoPlaneOffset);
            DivideUpFaces(ts, new Flat(plane.DistanceToOrigin + negPlaneShift, plane.Normal),
                out var negativeSideLoops, -1, new List<double>(distancesToPlane), negPlaneShift, loopsToIgnore, undoPlaneOffset);
            #endregion

            var groupOfLoops = GroupLoops(positiveSideLoops, negativeSideLoops, plane, setIntersectionGroups,
                out var intersectionLoops);
            var contactDataForEachSolid = MakeContactDataForEachSolid(ts, groupOfLoops);
            contactData = new ContactData(contactDataForEachSolid, intersectionLoops, plane);
            return true;
        }

        /// <summary>
        /// Returns lists of solids, given contact data for this slice
        /// </summary>
        /// <param name="contactData"></param>
        /// <param name="unitType"></param>
        /// <param name="solids"></param>
        public static void MakeSolids(ContactData contactData, UnitType unitType, out List<TessellatedSolid> solids)
        {
            solids = contactData.SolidContactData.Select(solidContactData => new TessellatedSolid(solidContactData.AllFaces, null, true, null, unitType)).ToList();
        }

        /// <summary>
        /// Returns a single positive side and negative side solid, given contact data for this slice
        /// </summary>
        /// <param name="contactData"></param>
        /// <param name="unitType"></param>
        /// <param name="positiveSideSolid"></param>
        /// <param name="negativeSideSolid"></param>
        public static void MakeSingleSolidOnEachSideOfInfitePlane(ContactData contactData, UnitType unitType, out TessellatedSolid positiveSideSolid, out TessellatedSolid negativeSideSolid)
        {
            var positiveSideFaces = new List<PolygonalFace>(contactData.PositiveSideContactData.SelectMany(solidContactData => solidContactData.AllFaces));
            positiveSideSolid = new TessellatedSolid(positiveSideFaces, null, true, null, unitType);
            var negativeSideFaces = new List<PolygonalFace>(contactData.NegativeSideContactData.SelectMany(solidContactData => solidContactData.AllFaces));
            negativeSideSolid = new TessellatedSolid(negativeSideFaces, null, true, null, unitType);
        }


        /// <summary>
        /// Groups the loops and creates triangles for the newly exposed surfaces.
        /// The direction of each loop is not necessary as it can be inferred.
        /// </summary>
        /// <param name="posSideLoops">The on side loops.</param>
        /// <param name="negSideLoops"></param>
        /// <param name="plane"></param>
        /// <param name="setIntersectionGroups"></param>
        /// <param name="intersectionGroups"></param>
        /// <returns>IEnumerable&lt;SolidContactData&gt;.</returns>
        /// <exception cref="System.Exception">
        /// This loop should always be positive. Check to may sure the group was created correctly in 'OrderLoops' 
        /// or
        /// This loop should always be negative. Check to may sure the group was created correctly in 'OrderLoops' 
        /// or
        /// The face should be in this list. Otherwise, it should not have been selected with face wrapping
        /// </exception>
        private static ISet<GroupOfLoops> GroupLoops(IList<Loop> posSideLoops, IList<Loop> negSideLoops, 
            Flat plane, bool setIntersectionGroups, out List<IntersectionGroup> intersectionGroups)
        {
            //Process the positive and negative side loops to create List<GroupOfLoops>. This requires the 
            //directionallity (hole vs. filled) and pairing of loops into groups, and the triangulation of
            //those groups.
            //Since the positive side loops actually need to look in reverse to see the new faces, use the reverse direction.
            //Vise-versa for the negative side.
            var groupsOfLoops = new HashSet<GroupOfLoops>();
            var posSideGroups = new List<GroupOfLoops>();
            var negSideGroups = new List<GroupOfLoops>();
            for (var k = -1; k < 2; k += 2) //-1 for positive side and 1 for negative side.
            {
                var direction = plane.Normal * k;
                var loops = k == -1 ? posSideLoops : negSideLoops;
                var vertexLoops = loops.Select(loop => loop.VertexLoop);

                //Order the loops into groups, determine positive or negative for each loop, and create vertex[]s to use for new faces.
                //Each group consists of one positive loop, but may include no or many negative loops.
                //No negative loop will be inside of two positive loops. No positive loop will be inside another positive loop. 
                //(NOTE: although they technically can be 'inside' other loops, there is no need for such a complicated tree of groupings)
                //Triangulating the polygon reverses loops (internally) if necessary and groups them together.
                //The isPositive output is used to determine whether each loop should be positive or negative.
                //The groupsOfLoopsIndices is a list of groups that was used to triangulate each surface.
                var groupsOfTriangles =
                    TriangulatePolygon.Run(vertexLoops, direction, out var groupsOfLoopsIndices, out var isPositive,
                        false);
                //Reverse loops if necessary to match the isPositive list from the triangulation.
                for (var i = 0; i < isPositive.Length; i++) loops[i].IsPositive = isPositive[i];

                //Put the groups of loops into a GroupOfLoops class.
                for (var i = 0; i < groupsOfLoopsIndices.Count; i++)
                {
                    var groupOfLoopIndices = groupsOfLoopsIndices[i];
                    var groupOfTriangles = groupsOfTriangles[i];
                    var positiveLoop = loops[groupOfLoopIndices.First()];
                    var negativeLoops = new List<Loop>();
                    if (!positiveLoop.IsPositive)
                        throw new Exception(
                            "This loop should always be positive. Check to may sure the group was created correctly in 'OrderLoops' ");
                    //Skip the first loop, since that is the positive loop
                    for (var j = 1; j < groupOfLoopIndices.Count; j++)
                    {
                        var negativeLoop = loops[groupOfLoopIndices[j]];
                        if (negativeLoop.IsPositive)
                            throw new Exception(
                                "This loop should always be negative. Check to may sure the group was created correctly in 'OrderLoops' ");
                        negativeLoops.Add(negativeLoop);
                    }

                    var groupOfOnPlaneFaces =
                        groupOfTriangles.Select(triangle => new PolygonalFace(triangle, direction, false));
                    var groupOfLoops = new GroupOfLoops(positiveLoop, negativeLoops, groupOfOnPlaneFaces);
                    groupsOfLoops.Add(groupOfLoops);
                    if (k == -1) posSideGroups.Add(groupOfLoops);
                    else negSideGroups.Add(groupOfLoops);
                }
            }

            intersectionGroups = new List<IntersectionGroup>();
            if (!setIntersectionGroups) return groupsOfLoops;

            //Pair up the groups of loops
            //Note: since projecting and intersection are time intensive. Do not perform this next operation unless
            //considering finite planes. This information is not necessary for infinite planes.
            var g = 0;
            foreach (var group in groupsOfLoops) group.SetCrossSection2D(plane);
            foreach (var posGroup in posSideGroups)
            {
                //Find all the negative side groups that it intersects with.
                foreach (var negGroup in negSideGroups)
                {
                    var intersection =
                        PolygonOperations.Intersection(posGroup.CrossSection2D, negGroup.CrossSection2D);
                    if (intersection == null || !intersection.Any() ||
                        intersection.Sum(p => p.Area).IsNegligible()) continue;
                    //Check if this intersection should be paired with an existing intersection group.
                    var intersectionGroupFound = false;
                    foreach (var intersectionGroup in intersectionGroups)
                    {
                        if (!intersectionGroup.GroupOfLoops.Contains(posGroup) &&
                            !intersectionGroup.GroupOfLoops.Contains(negGroup)) continue;
                        //Add this intersection to the already existing group.
                        intersectionGroup.GroupOfLoops.Add(posGroup); //This is a hash, so we can add the same item again without issue.
                        intersectionGroup.GroupOfLoops.Add(negGroup);
                        foreach (var poly in intersection)
                        {
                            intersectionGroup.Intersection2D.Add(poly);
                        }
                        intersectionGroupFound = true;
                    }

                    if (intersectionGroupFound) continue;
                    intersectionGroups.Add(new IntersectionGroup(posGroup, negGroup, intersection, g));
                    g++;
                }
            }
            return groupsOfLoops;
        }

        /// <summary>
        /// Uses face wrapping on the groups of loops to identigy multiple solids.
        /// </summary>
        /// <param name="ts"></param>
        /// <param name="groupsOfLoops"></param>
        /// <returns></returns>
        private static IEnumerable<SolidContactData> MakeContactDataForEachSolid(TessellatedSolid ts, ISet<GroupOfLoops> groupsOfLoops)
        {

            //Perform face wrapping (using adjacency to build up a list of all the faces on a solid) -- Similar to 'GetMultipleSolids'
            //The straddle faces form the barrier for the wrapping procedure.
            //Note: Since this function has been updated for finite planes, it is now possible that a negSideLoop is part
            //of the same solid as a posSideLoop (e.g., consider chopping an "S" verically but not through the middle).
            //For this reason, the group of loops from both sides need to be considering at the same time.
            var contactDataForEachSolid = new List<SolidContactData>();
            while (groupsOfLoops.Any())
            {
                var groupOfLoops = groupsOfLoops.First();
                var onPlaneFaces = new List<PolygonalFace>(groupOfLoops.OnPlaneFaces);
                var allLoopsBelongingToSolid = new List<GroupOfLoops>{groupOfLoops};
                groupsOfLoops.Remove(groupOfLoops);
                //Push all the adjacent onside faces to a stack
                //Note that blind pockets and holes are also included in this loop, since the onside faces for every loop in the group are included
                var straddleFaceIndices = new HashSet<int>(groupOfLoops.StraddleFaceIndices);
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
                        //If the groups have been paired with the intersection loops, do not add group2.
                        var notStraddleEdge = true;
                        var groupsToRemove = new List<GroupOfLoops>();
                        foreach (var group2 in groupsOfLoops)
                        {
                            if (!group2.StraddleFaceIndices.Contains(adjacentFace.IndexInList)) continue;
                            //This is a straddle edge for the current group of loops. Since we are considering both sides at the same time, 
                            //this gets a bit tricky because both loops could have the same straddle faces. To solve this, we have added
                            //a vertex hashset that contains the vertices from the straddle faces that are on the positive/negative sides.
                            //If the face (not the adjacentFace/straddleFace) contains a vertex that matches with this loop, add the loop.
                            //Otherwise, it is on the wrong side of approach. Skip it. 
                            if (!group2.StraddleEdgeOnSideVertices.Contains(face.A) &&
                                !group2.StraddleEdgeOnSideVertices.Contains(face.B) &&
                                !group2.StraddleEdgeOnSideVertices.Contains(face.B)) continue;
                            notStraddleEdge = false;
                            //Update the straddleFaceIndexList
                            foreach (var faceIndex in group2.StraddleFaceIndices)
                            {
                                straddleFaceIndices.Add(faceIndex);
                            }
                            //Don't add the straddle face. 
                            //Add all the adjacenet onside face indexes to the stack
                            foreach (var adjOnsideFaceIndex in group2.AdjOnsideFaceIndices)
                            {
                                if (usedFaces.Contains(ts.Faces[adjOnsideFaceIndex])) continue;
                                usedFaces.Add(ts.Faces[adjOnsideFaceIndex]);
                                stack.Push(ts.Faces[adjOnsideFaceIndex]);
                            }
                            //Add this the loops in this group to this solid
                            allLoopsBelongingToSolid.Add(group2);
                            //Add the onPlane faces 
                            onPlaneFaces.AddRange(group2.OnPlaneFaces);
                            //Remove that group from the list of groups
                            groupsToRemove.Add(group2);
                        }
                        foreach (var group2 in groupsToRemove) groupsOfLoops.Remove(group2);
                        if (notStraddleEdge) stack.Push(adjacentFace);
                    }
                }
                contactDataForEachSolid.Add(new SolidContactData(allLoopsBelongingToSolid, facesBelongingToSolid.ToList(), onPlaneFaces));
            }
            return contactDataForEachSolid;
        }

        private static bool ShiftPlaneForRobustCut(TessellatedSolid ts, Flat plane, out List<double> distancesToPlane, out double posPlaneShift, 
            out double negPlaneShift)
        {
            //Set the distance of every vertex in the solid to the plane
            distancesToPlane = new List<double>();
            posPlaneShift = 0;
            negPlaneShift = 0;
            var distancesToPosPlane = new List<double>();
            var distancesToNegPlane = new List<double>();
            var atLeastOneVertexOnPlane = false;
            var pointOnPlane = plane.Normal * plane.DistanceToOrigin;
            foreach (var vertex in ts.Vertices)
            {
                var distance = vertex.Coordinates.Subtract(pointOnPlane).Dot(plane.Normal);
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
                negPlaneShift = -distancesToNegPlane[i - 1] - minimumShift;
            }
            return true;
        }

        ///Returns a list of onSideFaces from the ts (not including straddle faces), and a list of all the new faces that make up the 
        /// halves of the straddle faces that are on this side.
        private static void DivideUpFaces(TessellatedSolid ts, Flat plane, out List<Loop> loops, int isPositiveSide,
            IList<double> distancesToPlane, double planeOffset = double.NaN, ICollection<int> loopsToIgnore = null,
            bool undoPlaneOffset = false)
        {
            loops = new List<Loop>();

            //If offset exists, go ahead and make offset
            if (!double.IsNaN(planeOffset))
            {
                for (var i = 0; i < distancesToPlane.Count; i++)
                {
                    distancesToPlane[i] = distancesToPlane[i] - planeOffset;
                    if (Math.Abs(distancesToPlane[i]) < ts.SameTolerance) throw new Exception("Issue in implementation of shift plane function");
                }
            }

            //Find all the straddle edges and add the new intersect vertices to both the pos and neg loops.
            var straddleEdges = new List<StraddleEdge>();
            var straddleEdgesDict = new Dictionary<int, Edge>();
            foreach (var edge in ts.Edges)
            {
                var toDistance = distancesToPlane[edge.To.IndexInList];
                var fromDistance = distancesToPlane[edge.From.IndexInList];
                //Check for a straddle edge (Signs are different)
                if (Math.Sign(toDistance) == Math.Sign(fromDistance)) continue;

                //If it is a straddle edge, then figure out which vertex is the offSideVertex (the one we aren't keeping)
                Vertex offSideVertex;
                if (isPositiveSide == 1)
                {
                    offSideVertex = toDistance > 0 ? edge.From : edge.To;
                }
                else
                {
                    offSideVertex = toDistance > 0 ? edge.To : edge.From;
                }
                straddleEdges.Add(undoPlaneOffset
                    ? new StraddleEdge(edge, plane, offSideVertex, planeOffset)
                    : new StraddleEdge(edge, plane, offSideVertex));
                straddleEdgesDict.Add(edge.IndexInList, edge);
            }

            //Also, find which faces are on the current side of the plane, by using edges.
            //Every face should have either 2 or 0 straddle edges, but never just 1.
            var straddleFaces = new Dictionary<int, PolygonalFace>();
            //Set the straddle faces and onSide faces
            foreach (var face in ts.Faces)
            {
                var d1 = distancesToPlane[face.Vertices[0].IndexInList];
                var d2 = distancesToPlane[face.Vertices[1].IndexInList];
                var d3 = distancesToPlane[face.Vertices[2].IndexInList];
                //If all the same signs, then this is on either the positive or negative side 
                if (Math.Sign(d1) == Math.Sign(d2) && Math.Sign(d1) == Math.Sign(d3)) continue;
                //else, it must be a straddle face
                straddleFaces.Add(face.IndexInList, face);
            }
            if (straddleFaces.Count != straddleEdges.Count) throw new Exception("These should be equal for closed geometry");

            //Get loops of straddleEdges 
            var loopsOfStraddleEdges = new List<List<StraddleEdge>>();
            var loopsOfStraddleFaceIndices = new List<HashSet<int>>();
            var maxCount = straddleEdges.Count / 3;
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
            if (straddleEdges.Any()) throw new Exception("While loop was unable to complete.");

            //Get loops of vertices, adding newly creates faces as you go
            //This is the brains of this function. It loops through the straddle edges to 
            //create new faces. This function avoids creating two new points that are 
            //extremely close together, which should avoid neglible edges and faces.
            //It also keeps track of how many new vertices should be created.
            var newVertexIndex = ts.NumberOfVertices;
            var tolerance = Math.Sqrt(ts.SameTolerance);
            for (var i = 0; i < loopsOfStraddleEdges.Count; i++)
            {
                var loopIndex = (i + 1) * isPositiveSide;
                if (loopsToIgnore!= null && loopsToIgnore.Contains(loopIndex)) continue;
                var loopOfStraddleEdges = loopsOfStraddleEdges[i];
                var straddleEdgeOnSideVertices = loopOfStraddleEdges.Select(e => e.OnSideVertex);
                var straddleFaceIndices = loopsOfStraddleFaceIndices[i];
                var newFaces = new List<PolygonalFace>();
                var newEdges = new List<Edge>();
                var loopOfVertices = new List<Vertex>();
                var adjOnsideFaceIndices = new HashSet<int>();
                //Find a good starting edge. One with an intersect vertex far enough away from other intersection vertices.
                var k = 0;
                var length1 = MiscFunctions.DistancePointToPoint(loopOfStraddleEdges.Last().IntersectVertex.Coordinates,
                            loopOfStraddleEdges[k].IntersectVertex.Coordinates);
                while (length1.IsNegligible(tolerance) && k + 1 != loopOfStraddleEdges.Count - 1)
                {
                    k++;
                    length1 = MiscFunctions.DistancePointToPoint(loopOfStraddleEdges[k - 1].IntersectVertex.Coordinates,
                        loopOfStraddleEdges[k].IntersectVertex.Coordinates);
                }
                if (k + 1 == loopOfStraddleEdges.Count - 1) throw new Exception("No good starting edge found. Rewrite the function to find a better edge");
                var firstStraddleEdge = loopOfStraddleEdges[k];
                var previousStraddleEdge = firstStraddleEdge;
                var successfull = false;
                do
                {
                    //ToDo: this function allows loops of two vertices if created vertices are too close together
                    k++; //Update the index
                    if (k > loopOfStraddleEdges.Count - 1) k = 0; //Set back to start if necessary
                    var currentStraddleEdge = loopOfStraddleEdges[k];
                    var length = MiscFunctions.DistancePointToPoint(currentStraddleEdge.IntersectVertex.Coordinates,
                            previousStraddleEdge.IntersectVertex.Coordinates);

                    //If finished, then create the final face and end
                    if (currentStraddleEdge == firstStraddleEdge)
                    {
                        if (length.IsNegligible(tolerance)) throw new Exception("pick a different starting edge");
                        if (loopOfVertices.Last() != previousStraddleEdge.IntersectVertex)
                        {
                            previousStraddleEdge.IntersectVertex.IndexInList = newVertexIndex++;
                            loopOfVertices.Add(previousStraddleEdge.IntersectVertex);
                        }
                        newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge, straddleEdgesDict, straddleFaces, newEdges, adjOnsideFaceIndices, true));
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
                            else if (currentStraddleEdge.OtherFace == previousStraddleEdge.OtherFace)
                                previousStraddleEdge.OtherFace = currentStraddleEdge.OwnedFace;
                            else throw new Exception("No shared face exists between these two straddle edges");
                            previousStraddleEdge.OffSideVertex = currentStraddleEdge.OffSideVertex;
                        }
                        else
                        {
                            newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge, straddleEdgesDict, straddleFaces, newEdges, adjOnsideFaceIndices));
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
                        newFaces.AddRange(NewFace(previousStraddleEdge, currentStraddleEdge, straddleEdgesDict, straddleFaces, newEdges, adjOnsideFaceIndices));
                        previousStraddleEdge = currentStraddleEdge;
                    }
                } while (!successfull);
                if (loopOfVertices.Count < 3) throw new Exception("This could be a knife edge. But this error will likely cause errors down the line");
                //The loop index for negative side loops are negative.
                loops.Add(new Loop(loopOfVertices, newFaces, plane.Normal, straddleFaceIndices, adjOnsideFaceIndices,
                    loopIndex, isPositiveSide == 1, straddleEdgeOnSideVertices));
            }
        }

        /// <summary>
        /// Creates a new face given two straddle edges
        /// </summary>
        /// <param name="st1">The ST1.</param>
        /// <param name="st2">The ST2.</param>
        /// <param name="straddleEdgesDict">The straddle edges dictionary.</param>
        /// <param name="straddleFaces">The straddle faces.</param>
        /// <param name="newEdges">The new edges.</param>
        /// <param name="adjOnsideFaceIndices">The adj onside face indices.</param>
        /// <param name="lastNewFace">if set to <c>true</c> [last new face].</param>
        /// <returns>List&lt;PolygonalFace&gt;.</returns>
        /// <exception cref="System.Exception">
        /// No shared face exists between these two straddle edges
        /// or
        /// There should only be one boundary edge. There must be 2 straddle edges for this shared face.
        /// or
        /// All edges of the shared face are straddle edges. This cannot be.
        /// or
        /// This should never be the case. The boundary edge should be have the sharedFace as owned or other
        /// or
        /// There should only be one boundary edge. There must be 2 straddle edges for this shared face.
        /// or
        /// All edges of the shared face are straddle edges. This cannot be.
        /// or
        /// This should never be the case. The boundary edge should be have the sharedFace as owned or other
        /// or
        /// Error, the straddle edges do not match up at a common vertex
        /// </exception>
        public static List<PolygonalFace> NewFace(StraddleEdge st1, StraddleEdge st2, Dictionary<int, Edge> straddleEdgesDict,
            Dictionary<int, PolygonalFace> straddleFaces, List<Edge> newEdges, HashSet<int> adjOnsideFaceIndices, bool lastNewFace = false)
        {
            PolygonalFace sharedFace;
            if (st1.OwnedFace == st2.OwnedFace || st1.OwnedFace == st2.OtherFace) sharedFace = st1.OwnedFace;
            else if (st1.OtherFace == st2.OwnedFace || st1.OtherFace == st2.OtherFace) sharedFace = st1.OtherFace;
            else throw new Exception("No shared face exists between these two straddle edges");

            //Make an extra edge if the first new face
            if (!newEdges.Any())
            {
                var newEdge = new Edge(st1.IntersectVertex, st1.OnSideVertex, false);
                newEdges.Add(newEdge);
            }

            if (st1.IntersectVertex == st2.IntersectVertex)
            {
                //Make one new edge and one new face. Set the ownership of this edge.
                var newFace =
                    new PolygonalFace(new List<Vertex> { st1.OnSideVertex, st1.IntersectVertex, st2.OnSideVertex },
                        sharedFace.Normal, false);
                newEdges.Last().OtherFace = newFace;
                if (!lastNewFace)
                    newEdges.Add(new Edge(st2.IntersectVertex, st2.OnSideVertex, false) { OwnedFace = newFace });
                else newEdges.First().OwnedFace = newFace;

                #region Store index of face on boundary edge.
                //First, find the boundary edge
                Edge boundaryEdge = null;
                foreach (var edge in sharedFace.Edges.Where(edge => !straddleEdgesDict.ContainsKey(edge.IndexInList)))
                {
                    if (boundaryEdge != null) throw new Exception("There should only be one boundary edge. There must be 2 straddle edges for this shared face.");
                    boundaryEdge = edge;
                }
                if (boundaryEdge == null) throw new Exception("All edges of the shared face are straddle edges. This cannot be.");

                //Second, find the boundary face
                //No duplicates are to be included in the adjOnsideFaceIndices HashSet.
                if (boundaryEdge.OwnedFace == sharedFace)
                {
                    //Check if the other face is a straddle face. If it is, it is not needed for face wrapping. 
                    //Note: It is a straddle edge when the boundary edge is above the cutting plane, but both faces are straddling the cutting plane.
                    //This is common. Consider cutting a box at an angle near one of its edges.
                    if (!straddleFaces.ContainsKey(boundaryEdge.OtherFace.IndexInList) && !adjOnsideFaceIndices.Contains(boundaryEdge.OtherFace.IndexInList))
                    {
                        adjOnsideFaceIndices.Add(boundaryEdge.OtherFace.IndexInList);
                    }
                }
                else if (boundaryEdge.OtherFace == sharedFace)
                {
                    if (!straddleFaces.ContainsKey(boundaryEdge.OwnedFace.IndexInList) && !adjOnsideFaceIndices.Contains(boundaryEdge.OwnedFace.IndexInList))
                    {
                        adjOnsideFaceIndices.Add(boundaryEdge.OwnedFace.IndexInList);
                    }
                }
                else throw new Exception("This should never be the case. The boundary edge should be have the sharedFace as owned or other");
                #endregion

                return new List<PolygonalFace> { newFace };
            }
            //If not the same intersect vertex, then the same offSideVertex denotes 
            //two Consecutive curved edges, so this creates two new faces
            if (st1.OffSideVertex == st2.OffSideVertex || 
                st1.OriginalOffSideVertex == st2.OffSideVertex || 
                st1.OffSideVertex == st2.OriginalOffSideVertex) 
            {
                //Create two new faces
                var newFace1 =
                    new PolygonalFace(new List<Vertex> { st1.OnSideVertex, st1.IntersectVertex, st2.IntersectVertex },
                        sharedFace.Normal, false);
                var newFace2 =
                    new PolygonalFace(new List<Vertex> { st1.OnSideVertex, st2.IntersectVertex, st2.OnSideVertex },
                        sharedFace.Normal, false);
                //Update ownership of most recently created edge
                newEdges.Last().OtherFace = newFace1;
                //Create new edges and update their ownership 
                var newEdge1 = new Edge(st1.IntersectVertex, st2.IntersectVertex, false) { OwnedFace = newFace1 };
                var newEdge2 = new Edge(st1.OnSideVertex, st2.IntersectVertex, false) { OwnedFace = newFace2, OtherFace = newFace1 };
                newEdges.AddRange(new List<Edge> { newEdge1, newEdge2 });
                //Create the last edge, if this is not the last new face
                if (!lastNewFace) newEdges.Add(new Edge(st2.IntersectVertex, st2.OnSideVertex, false) { OwnedFace = newFace2 });
                else newEdges.First().OwnedFace = newFace2;

                #region Store index of face on boundary edge.
                //First, find the boundary edge
                Edge boundaryEdge = null;
                foreach (var edge in sharedFace.Edges.Where(edge => !straddleEdgesDict.ContainsKey(edge.IndexInList)))
                {
                    if (boundaryEdge != null) throw new Exception("There should only be one boundary edge. There must be 2 straddle edges for this shared face.");
                    boundaryEdge = edge;
                }
                if (boundaryEdge == null) throw new Exception("All edges of the shared face are straddle edges. This cannot be.");

                //Second, find the boundary face
                //No duplicates are to be included in the adjOnsideFaceIndices HashSet.
                if (boundaryEdge.OwnedFace == sharedFace)
                {
                    //Check if the other face is a straddle face. If it is, it is not needed for face wrapping. 
                    //Note: It is a straddle edge when the boundary edge is above the cutting plane, but both faces are straddling the cutting plane.
                    //This is common. Consider cutting a box at an angle near one of its edges.
                    if (!straddleFaces.ContainsKey(boundaryEdge.OtherFace.IndexInList) && !adjOnsideFaceIndices.Contains(boundaryEdge.OtherFace.IndexInList))
                    {
                        adjOnsideFaceIndices.Add(boundaryEdge.OtherFace.IndexInList);
                    }
                }
                else if (boundaryEdge.OtherFace == sharedFace)
                {
                    if (!straddleFaces.ContainsKey(boundaryEdge.OwnedFace.IndexInList) && !adjOnsideFaceIndices.Contains(boundaryEdge.OwnedFace.IndexInList))
                    {
                        adjOnsideFaceIndices.Add(boundaryEdge.OwnedFace.IndexInList);
                    }
                }
                else throw new Exception("This should never be the case. The boundary edge should be have the sharedFace as owned or other");
                #endregion

                return new List<PolygonalFace> { newFace1, newFace2 };
            }
            if (st1.OnSideVertex == st2.OnSideVertex)
            {
                //Make two new edges and one new face. Set the ownership of the edges.
                var newFace =
                    new PolygonalFace(new List<Vertex> { st1.OnSideVertex, st1.IntersectVertex, st2.IntersectVertex },
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

            internal StraddleEdge(Edge edge, Flat plane, Vertex offSideVertex, double planeOffset = 0D)
            {
                OwnedFace = edge.OwnedFace;
                OtherFace = edge.OtherFace;
                Edge = edge;
                OffSideVertex = offSideVertex;
                OriginalOffSideVertex = offSideVertex;
                OnSideVertex = Edge.OtherVertex(OffSideVertex);
                IntersectVertex = MiscFunctions.PointOnPlaneFromIntersectingLine(plane.Normal,
                    plane.DistanceToOrigin - planeOffset, edge.To, edge.From);
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