using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using System.Diagnostics;

namespace TVGL.TwoDimensional
{
    /// <summary>
    /// Triangulates a Polygon into faces in O(n log n) time.
    /// </summary>
    ///  <References>
    ///     Trapezoidation algorithm heavily based on: 
    ///     "A Fast Trapezoidation Technique For Planar Polygons" by
    ///     Gian Paolo Lorenzetto, Amitava Datta, and Richard Thomas. 2000.
    ///     http://www.researchgate.net/publication/2547487_A_Fast_Trapezoidation_Technique_For_Planar_Polygons
    ///     This algorithm should run in O(n log n)  time.    
    /// 
    ///     Triangulation method based on Montuno's work, but referenced material and algorithm are from:
    ///     http://www.personal.kent.edu/~rmuhamma/Compgeometry/MyCG/PolyPart/polyPartition.htm
    ///     This algorithm should run in O(n) time.
    /// </References>
    public static partial class PolygonOperations
    {
        /// <summary>
        /// Triangulates a list of loops into faces in O(n*log(n)) time.
        /// </summary>
        /// <param name="loops">The loops.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="groupsOfLoops">The groups of loops.</param>
        /// <param name="isPositive">The is positive.</param>
        /// <param name="ignoreNegativeSpace">if set to <c>true</c> [ignore negative space].</param>
        /// <returns>List&lt;List&lt;Vertex[]&gt;&gt;.</returns>
        public static List<List<Vertex[]>> Triangulate(this IEnumerable<IEnumerable<Vertex>> loops, Vector3 normal,
            out List<List<int>> groupsOfLoops, out bool[] isPositive, bool ignoreNegativeSpace = false)
        {
            //Note: Do NOT merge duplicates unless you have good reason to, since it may make the solid non-watertight   
            var points2D = loops.Select(loop => loop.ProjectTo2DCoordinates(normal, out _).ToArray()).ToArray();
            points2D.CreateShallowPolygonTrees(false, out var polygons, out var connectingIndices);
            var triangleIndices = Triangulate(points2D, out groupsOfLoops, out isPositive, ignoreNegativeSpace);
            var int2VertexDict = new Dictionary<int, Vertex>();
            var vertexID = 0;
            foreach (var loop in loops)
                foreach (var vertex in loop)
                    int2VertexDict.Add(vertexID++, vertex);
            var result = new List<List<Vertex[]>>();
            foreach (var polygonTriangleIndices in triangleIndices)
            {
                var polygonTriangles = new List<Vertex[]>();
                foreach (var triangle in polygonTriangleIndices)
                    polygonTriangles.Add(new[] { int2VertexDict[triangle[0]],
                        int2VertexDict[triangle[1]], int2VertexDict[triangle[2]] });
                result.Add(polygonTriangles);
            }
            return result;
        }

        /// <summary>
        /// Triangulates a list of loops into faces in O(n*log(n)) time.
        /// If ignoring negative space, the function will fill in holes. 
        /// DO NOT USE "ignoreNegativeSpace" for watertight geometry.
        /// </summary>
        /// <param name="points2D">The points2 d.</param>
        /// <param name="groupsOfLoops">The groups of loops.</param>
        /// <param name="isPositive">The is positive.</param>
        /// <param name="ignoreNegativeSpace">if set to <c>true</c> [ignore negative space].</param>
        /// <returns>List&lt;List&lt;Vertex[]&gt;&gt;.</returns>
        /// <exception cref="System.Exception">
        /// Inputs into 'TriangulatePolygon' are unbalanced
        /// or
        /// Duplicate point found
        /// or
        /// Incorrect balance of node types
        /// or
        /// Incorrect balance of node types
        /// or
        /// Negative Loop must be inside a positive loop, but no positive loops are left. Check if loops were created correctly.
        /// or
        /// Trapezoidation failed to complete properly. Check to see that the assumptions are met.
        /// or
        /// Incorrect number of triangles created in triangulate function
        /// or
        /// </exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="Exception"></exception>
        public static List<List<int[]>> Triangulate(this IEnumerable<IEnumerable<Vector2>> paths, out List<List<int>> groupsOfLoops,
            out bool[] isPositive, bool ignoreNegativeSpace = false)
        {
            if (!paths.CreateShallowPolygonTrees(false, out var polygons, out var connectingIndices))
            {

            }
            isPositive = new bool[connectingIndices.Length];
            var index = 0;
            foreach (var poly in polygons)
            {
                var connectingIndex = connectingIndices.FindIndex(x => x == index);
                isPositive[connectingIndex] = true;
                index++;
                foreach (var hole in poly.InnerPolygons)
                {
                    connectingIndex = connectingIndices.FindIndex(x => x == index);
                    isPositive[connectingIndex] = false;
                    index++;
                }
            }
            return polygons.Triangulate(out groupsOfLoops, ignoreNegativeSpace);
        }

        /// <summary>
        /// Triangulates a list of loops into faces in O(n*log(n)) time.
        /// If ignoring negative space, the function will fill in holes. 
        /// DO NOT USE "ignoreNegativeSpace" for watertight geometry.
        /// </summary>
        /// <param name="points2D">The points2 d.</param>
        /// <param name="groupsOfLoops">The groups of loops.</param>
        /// <param name="isPositive">The is positive.</param>
        /// <param name="onlyOuterPolygons">if set to <c>true</c> [ignore negative space].</param>
        /// <returns>List&lt;List&lt;Vertex[]&gt;&gt;.</returns>
        /// <exception cref="System.Exception">
        /// Inputs into 'TriangulatePolygon' are unbalanced
        /// or
        /// Duplicate point found
        /// or
        /// Incorrect balance of node types
        /// or
        /// Incorrect balance of node types
        /// or
        /// Negative Loop must be inside a positive loop, but no positive loops are left. Check if loops were created correctly.
        /// or
        /// Trapezoidation failed to complete properly. Check to see that the assumptions are met.
        /// or
        /// Incorrect number of triangles created in triangulate function
        /// or
        /// </exception>
        /// <exception cref="Exception"></exception>
        /// <exception cref="Exception"></exception>
        //ASSUMPTION: NO lines intersect other lines or points && NO two points in any of the loops are the same.
        //Ex 1) If a negative loop and positive share a point, the negative loop should be inserted into the positive loop after that point and
        //then a slightly altered point (near duplicate) should be inserted after the negative loop such that the lines do not intersect.
        //Ex 2) If a negative loop shares 2 consecutive points on a positive loop, insert the negative loop into the positive loop between those two points.
        //Ex 3) If a positive loop intersects itself, it should be two separate positive loops.

        //ROBUST FEATURES:
        // 1: Two positive loops may share a point, because they are processed separately.
        // 2: Loops can be in given CW or CCW, because as long as the isPositive boolean is correct, 
        // the code recognizes when the loop should be reversed.
        // 3: If isPositive == null, CW and CCW ordering for the loops is unknown. A preprocess step can build a new isPositive variable.
        // 4: It is OK if a positive loop is inside a another positive loop, given that there is a negative loop between them.
        // These "nested" loop cases are handled by ordering the loops (working outward to inward) and the red black tree.
        // 5: If isPositive == null, then 
        public static List<List<int[]>> Triangulate(this IEnumerable<Polygon> polygons, out List<List<int>> groupsOfLoops,
             bool onlyOuterPolygons = false)
        {
            var polygonList = polygons.OrderByDescending(p => p.Area).ToList();
            if (onlyOuterPolygons)
            {   // this is a n^2 check. but no other way to do it. however, there shouldn't be many polygons and having them ordered reduces
                // the number of checks
                for (int i = polygonList.Count - 2; i >= 0; i--)
                {
                    for (int j = polygonList.Count - 1; j > i; j--)
                    {
                        if (polygonList[i].IsPointInsidePolygon(polygonList[j].Vertices[0].Coordinates, out _, out _, out _))
                            polygonList.RemoveAt(j);
                    }
                }
            }
            var successful = false;
            var attempts = 0;
            var random = new Random(1);
            //Create return variables. These intializations are unnecessary but C# won't compile unless it's sure that we set these
            //before exiting. 
            List<List<int[]>> triangleFaceList = new List<List<int[]>>();
            groupsOfLoops = new List<List<int>>();
            do
            {
                try
                {
                    attempts++;
                    //Reset return variables
                    triangleFaceList.Clear();
                    groupsOfLoops.Clear();

                    #region Setup up polygon nodes and segments
                    // 1) For each loop in points2D
                    // 2)   Count the number of points and add to total.
                    // 3)   Create nodes and lines from points, and retain whether a point
                    //      was in a positive or negative loop.
                    // 4)   Add nodes to an ordered loop (same as points2D except now Nodes) 
                    //      and a sorted loop (used for sweeping).
                    var polygonNodes = new List<List<Vertex2D>>();
                    var sortedPolygonNodes = new List<List<Vertex2D>>();
                    var polygonLines = new List<List<PolygonSegment>>();

                    var pointCount = 0;

                    //Since the algorithm may fail for polygonal lines that are perfectly vertical or 
                    //horizontal and since rotating the polygon doesn't change how it should be triangulated - 
                    //a random rotation is applied to all coordinates at the start
                    var theta = 2 * Math.PI * random.NextDouble();
                    var randRotMatrix = Matrix3x3.CreateRotation(theta);
                    var loopsCount = 0;
                    /*
                    foreach (var origLoop in polygons)
                    {
                        var nodes = new List<Vertex2D>();
                        foreach (var coordinates in origLoop)
                            nodes.Add(new Vertex2D(coordinates.Transform(randRotMatrix), pointCount++, loopsCount));
                        var polygon = new Polygon(nodes, true, loopsCount);
                        var sortedLoop = polygon.Vertices.OrderByDescending(node => node.Y).ThenByDescending(node => node.X).ToList();
                        polygonNodes.Add(polygon.Vertices);
                        sortedPolygonNodes.Add(sortedLoop);
                        polygonLines.Add(polygon.Lines);
                        loopsCount++;
                    }
                    */
                    #endregion


                    #region Get the number of positive and negative loops. 
                    var negativeLoopCount = 0;
                    var positiveLoopCount = 0;
                    var isPositive = new bool[loopsCount];
                    //First, find the first node from each loop and then sort them. This determines the order the loops
                    //will be visited in.
                    var sortedFirstNodes = sortedPolygonNodes.Select(sortedLoop => sortedLoop[0])
                        .OrderByDescending(node => node.Y).ThenByDescending(node => node.X).ToList();
                    //Use a red-black tree to track whether loops are inside other loops
                    var tempSortedLoops1 = new List<List<Vertex2D>>(sortedPolygonNodes);
                    while (tempSortedLoops1.Any())
                    {
                        //Set the start loop and remove necessary information
                        var startLoop = sortedPolygonNodes[sortedFirstNodes[0].LoopID];
                        isPositive[sortedFirstNodes[0].LoopID] = true; //The first loop in the group must always be CCW positive
                        sortedFirstNodes.RemoveAt(0);
                        tempSortedLoops1.Remove(startLoop);
                        if (!sortedFirstNodes.Any()) continue; //Exit while loop
                        var sortedGroup = new List<Vertex2D>(startLoop);

                        //Add the remaining first points from each loop into sortedGroup.
                        foreach (var firstNode in sortedFirstNodes)
                        {
                            InsertNodeInSortedList(sortedGroup, firstNode);
                        }

                        //inititallize lineList 
                        var lineList = new List<PolygonSegment>();
                        for (var j = 0; j < sortedGroup.Count; j++)
                        {
                            var node = sortedGroup[j];

                            if (sortedFirstNodes.Contains(node)) //if first point in the sorted loop 
                            {
                                bool isInside;
                                bool isOnLine;
                                //If remainder is not equal to 0, then it is odd.
                                //If both LinesToLeft and LinesToRight are odd, then it must be inside.
                                PolygonSegment leftLine;
                                if (LinesToLeft(node, lineList, out leftLine, out isOnLine) % 2 != 0)
                                {
                                    PolygonSegment rightLine;
                                    isInside = LinesToRight(node, lineList, out rightLine, out isOnLine) % 2 != 0;
                                }
                                else isInside = false;
                                if (isInside) //Merge the loop into this one and remove from the tempList
                                {
                                    isPositive[node.LoopID] = false; //This is a negative loop
                                    sortedFirstNodes.Remove(node);
                                    tempSortedLoops1.Remove(sortedPolygonNodes[node.LoopID]);
                                    if (!tempSortedLoops1.Any()) break; //That was the last loop
                                    MergeSortedListsOfNodes(sortedGroup, sortedPolygonNodes[node.LoopID], node);
                                }
                                else //remove the node from this group and continue
                                {
                                    sortedGroup.Remove(node);
                                    j--; //Pick the same index for the next iteration as the node which was just removed
                                    continue;
                                }
                            }

                            //Add to or remove from Red-Black Tree
                            if (lineList.Contains(node.StartLine))
                            {
                                lineList.Remove(node.StartLine);
                            }
                            else
                            {
                                lineList.Add(node.StartLine);
                            }
                            if (lineList.Contains(node.EndLine))
                            {
                                lineList.Remove(node.EndLine);
                            }
                            else
                            {
                                lineList.Add(node.EndLine);
                            }
                        }
                    }

                    //Check to see that the loops are ordered correctly to their isPositive boolean
                    //If they are incorrectly ordered, reverse the order.
                    var nodesLoopsCorrected = new List<List<Vertex2D>>();
                    for (var j = 0; j < polygonNodes.Count; j++)
                    {
                        var orderedLoop = polygonNodes[j];
                        var index = orderedLoop.IndexOf(sortedPolygonNodes[j][0]); // index of first node in orderedLoop
                        NodeType nodeType;
                        if (index == 0)
                        {
                            nodeType = GetNodeType(orderedLoop.Last(), orderedLoop.First(), orderedLoop[1]);
                        }
                        else if (index == orderedLoop.Count - 1)
                        {
                            nodeType = GetNodeType(orderedLoop[index - 1], orderedLoop.Last(), orderedLoop.First());
                        }
                        else nodeType = GetNodeType(orderedLoop[index - 1], orderedLoop[index], orderedLoop[index + 1]);
                        //If node type is incorrect (loop is CW when it should be CCW or vice versa), 
                        //reverse the order of the loop
                        if ((isPositive[j] && nodeType != NodeType.Peak) || (!isPositive[j] && nodeType != NodeType.UpwardReflex))
                        {
                            orderedLoop.Reverse();
                            //Also, reorder all the lines for these nodes
                            for (int i = 0; i < polygonLines[j].Count; i++)
                                polygonLines[j][i] = polygonLines[j][i].Reverse();
                            //And reorder all the node - line identifiers
                            foreach (var node in orderedLoop)
                            {
                                var tempLine = node.EndLine;
                                node.EndLine = node.StartLine;
                                node.StartLine = tempLine;
                            }
                        }
                        nodesLoopsCorrected.Add(orderedLoop);
                    }
                    polygonNodes = new List<List<Vertex2D>>(nodesLoopsCorrected);

                    #region Ignore Negative Space Alterations
                    //If we are to ignore the negative space, we need to get rid of any loops that are inside other loops.
                    //All negative loops must be inside, so remove them.
                    //Some positive loops may be nested inside a negative then a postive, and therefore they are inside a positive. Remove them.
                    //Note that sorted groups were modifies earlier with this bool.
                    if (onlyOuterPolygons)
                    {
                        var listOfLoopIDsToKeep = new List<int>();
                        foreach (var positiveLoop1 in polygonNodes)
                        {
                            //If its a negative loop, continue
                            if (!isPositive[positiveLoop1.First().LoopID]) continue;
                            var isInside = false;
                            foreach (var positiveLoop2 in polygonNodes)
                            {
                                if (!isPositive[positiveLoop2.First().LoopID]) continue; //Only concerned about positive loops
                                if (positiveLoop1 == positiveLoop2) continue;
                                //If any point (just check the first one) is NOT inside positive loop 2, then keep positive loop 1
                                //Note: If this occurs, any loops inside loop 1 will also be inside loop 2, so no information is lost.
                                var otherLoopID = positiveLoop2.First().LoopID;
                                isInside = IsPointInsidePolygon(new Polygon(polygonNodes[otherLoopID], polygonLines[otherLoopID], otherLoopID),
                                     positiveLoop1.First().Coordinates, out _, out _, out _);
                                if (isInside) break;
                            }
                            //Only keep it if its not inside other loops
                            if (!isInside) listOfLoopIDsToKeep.Add(positiveLoop1.First().LoopID);
                        }

                        //Rewrite the lists 
                        var tempOrderedLoops = new List<List<Vertex2D>>();
                        var tempSortedLoops2 = new List<List<Vertex2D>>();
                        foreach (var index in listOfLoopIDsToKeep)
                        {
                            tempOrderedLoops.Add(polygonNodes[index]);
                            tempSortedLoops2.Add(sortedPolygonNodes[index]);
                        }
                        polygonNodes = new List<List<Vertex2D>>(tempOrderedLoops);
                        sortedPolygonNodes = new List<List<Vertex2D>>(tempSortedLoops2);

                        //Create a new bool list and update the loop IDs and point count.
                        pointCount = 0;
                        for (var j = 0; j < polygonNodes.Count; j++)
                        {
                            //Update the LoopID's. Note that the nodes in the 
                            //sorted loops are the same nodes.
                            pointCount = pointCount + polygonNodes[j].Count;
                            foreach (var node in polygonNodes[j])
                            {
                                node.LoopID = j;
                            }
                        }
                    }
                    #endregion

                    //Set the NodeTypes of every Vertex2D. This step is after "isPositive == null" fuction because
                    //the CW/CCW order of the loops must be accurate.
                    var loopI = 0;
                    foreach (var orderedLoop in polygonNodes)
                    {
                        //Set nodeType for the first node
                        orderedLoop[0].Type = GetNodeType(orderedLoop.Last(), orderedLoop[0], orderedLoop[1]);

                        //Set nodeTypes for other nodes
                        for (var j = 1; j < orderedLoop.Count - 1; j++)
                        {
                            orderedLoop[j].Type = GetNodeType(orderedLoop[j - 1], orderedLoop[j], orderedLoop[j + 1]);
                        }

                        //Set nodeType for the last node
                        //Create last node
                        orderedLoop[orderedLoop.Count - 1].Type = GetNodeType(orderedLoop[orderedLoop.Count - 2], orderedLoop[orderedLoop.Count - 1], orderedLoop[0]);

                        //Debug to see if the proper balance of point types has been used
                        var downwardReflexCount = 0;
                        var upwardReflexCount = 0;
                        var peakCount = 0;
                        var rootCount = 0;
                        foreach (var node in orderedLoop)
                        {
                            if (node.Type == NodeType.DownwardReflex) downwardReflexCount++;
                            else if (node.Type == NodeType.UpwardReflex) upwardReflexCount++;
                            else if (node.Type == NodeType.Peak) peakCount++;
                            else if (node.Type == NodeType.Root) rootCount++;
                            else if (node.Type == NodeType.Duplicate) throw new Exception("Duplicate point found");
                        }
                        if (isPositive[loopI]) //If a positive loop, the following conditions must be balanced
                        {
                            if (peakCount != downwardReflexCount + 1 || rootCount != upwardReflexCount + 1)
                            {
                                throw new Exception("Incorrect balance of node types");
                            }
                        }
                        else //If negative loop, the conditions change
                        {
                            if (peakCount != downwardReflexCount - 1 || rootCount != upwardReflexCount - 1)
                            {
                                throw new Exception("Incorrect balance of node types");
                            }
                        }
                        loopI++;
                    }

                    //Get the number of negative loops
                    foreach (var boolean in isPositive)
                    {
                        if (boolean) positiveLoopCount++;
                        else negativeLoopCount++;
                    }
                    #endregion

                    // 1) For each positive loop
                    // 2)   Remove it from orderedLoops.
                    // 3)   Create a new group
                    // 4)   Insert the first node from all the negative loops remaining into the group list in the correct sorted order.
                    // 5)   Use the red-black tree to determine if the first node from a negative loop is inside the polygon.
                    //         Refer to http://alienryderflex.com/polygon/ for how to determine if a point is inside a polygon.
                    // 6)   If not inside, remove that nodes from the group list. 
                    // 7)      else remove the negative loop from orderedLoops and merge the negative loop with the group list.
                    // 8)   Continue with Trapezoidation
                    var listPositive = isPositive.ToList();
                    var completeListSortedLoops = new List<List<Vertex2D>>(sortedPolygonNodes);
                    var numTriangles = 0;
                    while (polygonNodes.Any())
                    {
                        //Get information about positive loop, remove from loops, and create new group
                        loopI = listPositive.FindIndex(true);
                        if (loopI == -1) throw new Exception("Negative Loop must be inside a positive loop, but no positive loops are left. Check if loops were created correctly.");
                        var sortedGroup = new List<Vertex2D>(sortedPolygonNodes[loopI]);
                        var group = new List<int> { sortedGroup[0].LoopID };
                        listPositive.RemoveAt(loopI);
                        polygonNodes.RemoveAt(loopI);
                        sortedPolygonNodes.RemoveAt(loopI);

                        //Insert the first node from all the negative loops remaining into the group list in the correct sorted order.
                        for (var j = 0; j < polygonNodes.Count; j++)
                        {
                            if (listPositive[j] == false)
                            {
                                InsertNodeInSortedList(sortedGroup, sortedPolygonNodes[j][0]);
                            }
                        }

                        //inititallize lineList and sortedNodes
                        var lineList = new List<PolygonSegment>();

                        #region Trapezoidize Polygons

                        var trapTree = new List<PartialTrapezoid>();
                        var completedTrapezoids = new List<Trapezoid>();

                        //Use the red-black tree to determine if the first node from a negative loop is inside the polygon.
                        //for each node in sortedNodes, update the lineList. Note that at this point each node only has two edges.
                        for (var j = 0; j < sortedGroup.Count; j++)
                        {
                            var node = sortedGroup[j];
                            PolygonSegment leftLine = null;
                            PolygonSegment rightLine = null;

                            //Check if negative loop is inside polygon 
                            //note that listPositive changes order /size , while isPositive is static like loopID.
                            //Similarly points2D is static.
                            if (node == completeListSortedLoops[node.LoopID][0] && !isPositive[node.LoopID])
                            //if first point in the sorted loop and loop is negative 
                            {
                                bool isInside;
                                bool isOnLine;
                                //If remainder is not equal to 0, then it is odd.
                                //If both LinesToLeft and LinesToRight are odd, then it must be inside.
                                if (LinesToLeft(node, lineList, out leftLine, out isOnLine) % 2 != 0)
                                {
                                    isInside = LinesToRight(node, lineList, out rightLine, out isOnLine) % 2 != 0;
                                }
                                else isInside = false;
                                if (isInside)
                                {
                                    //NOTE: This node must be a reflex upward point by observation
                                    //leftLine and rightLine are set in the two previous call and are now not null.
                                    //Add remaining points from loop into sortedGroup.
                                    MergeSortedListsOfNodes(sortedGroup, completeListSortedLoops[node.LoopID], node);

                                    //Remove this loop from lists of loops and the boolean list
                                    var loop = completeListSortedLoops[node.LoopID];
                                    var k = sortedPolygonNodes.FindIndex(loop);
                                    listPositive.RemoveAt(k);
                                    polygonNodes.RemoveAt(k);
                                    sortedPolygonNodes.RemoveAt(k);
                                    group.Add(node.LoopID);
                                }
                                else
                                {
                                    sortedGroup.Remove(node);
                                    j--; //Pick the same index for the next iteration as the node which was just removed
                                    continue;
                                }
                            }

                            //Add to or remove from Red-Black Tree
                            if (lineList.Contains(node.StartLine))
                            {
                                lineList.Remove(node.StartLine);
                            }
                            else
                            {
                                lineList.Add(node.StartLine);
                            }
                            if (lineList.Contains(node.EndLine))
                            {
                                lineList.Remove(node.EndLine);
                            }
                            else
                            {
                                lineList.Add(node.EndLine);
                            }

                            switch (node.Type)
                            {
                                case NodeType.DownwardReflex:
                                    {
                                        FindLeftLine(node, lineList, out leftLine);
                                        FindRightLine(node, lineList, out rightLine);

                                        //Close two trapezoids
                                        //Left trapezoid:
                                        InsertTrapezoid(node, leftLine, node.StartLine, trapTree, completedTrapezoids);
                                        //Right trapezoid:
                                        InsertTrapezoid(node, node.EndLine, rightLine, trapTree, completedTrapezoids);

                                        //Create one new partial trapezoid
                                        var newPartialTrapezoid = new PartialTrapezoid(node, leftLine, rightLine);
                                        trapTree.Add(newPartialTrapezoid);
                                    }
                                    break;
                                case NodeType.UpwardReflex:
                                    {
                                        if (leftLine == null) //If from the first negative point, leftLine and rightLine will already be set.
                                        {
                                            FindLeftLine(node, lineList, out leftLine);
                                            FindRightLine(node, lineList, out rightLine);
                                        }

                                        //Close one trapezoid
                                        InsertTrapezoid(node, leftLine, rightLine, trapTree, completedTrapezoids);

                                        //Create two new partial trapezoids
                                        //Left Trapezoid
                                        var newPartialTrapezoid1 = new PartialTrapezoid(node, leftLine, node.EndLine);
                                        trapTree.Add(newPartialTrapezoid1);
                                        //Right Trapezoid
                                        var newPartialTrapezoid2 = new PartialTrapezoid(node, node.StartLine, rightLine);
                                        trapTree.Add(newPartialTrapezoid2);
                                    }
                                    break;
                                case NodeType.Peak:
                                    {
                                        //Create one new partial trapezoid
                                        var newPartialTrapezoid = new PartialTrapezoid(node, node.StartLine, node.EndLine);
                                        trapTree.Add(newPartialTrapezoid);
                                    }
                                    break;
                                case NodeType.Root:
                                    {
                                        //Close one trapezoid
                                        InsertTrapezoid(node, node.EndLine, node.StartLine, trapTree, completedTrapezoids);
                                    }
                                    break;
                                case NodeType.Left:
                                    {
                                        //Create one trapezoid
                                        FindLeftLine(node, lineList, out leftLine);
                                        rightLine = node.StartLine;
                                        InsertTrapezoid(node, leftLine, rightLine, trapTree, completedTrapezoids);

                                        //Create one new partial trapezoid
                                        var newPartialTrapezoid = new PartialTrapezoid(node, leftLine, node.EndLine);
                                        trapTree.Add(newPartialTrapezoid);
                                    }
                                    break;
                                case NodeType.Right:
                                    {
                                        //Create one trapezoid
                                        FindRightLine(node, lineList, out rightLine);
                                        leftLine = node.EndLine;
                                        InsertTrapezoid(node, leftLine, rightLine, trapTree, completedTrapezoids);

                                        //Create one new partial trapezoid
                                        var newPartialTrapezoid = new PartialTrapezoid(node, node.StartLine, rightLine);
                                        trapTree.Add(newPartialTrapezoid);
                                    }
                                    break;
                            }
                        }
                        if (trapTree.Count > 0)
                        {
                            throw new Exception("Trapezoidation failed to complete properly. Check to see that the assumptions are met.");
                        }
                        #endregion

                        #region Create Monotone Polygons

                        //for each trapezoid with a reflex edge, split in two. 
                        //Insert new trapezoids in correct position in list.
                        for (var j = 0; j < completedTrapezoids.Count; j++)
                        {
                            var trapezoid = completedTrapezoids[j];
                            if (trapezoid.TopNode.Type == NodeType.DownwardReflex) //If upper node is reflex down (bottom node could be reflex up, reflex down, or other)
                            {
                                var newLine = new PolygonSegment(trapezoid.TopNode, trapezoid.BottomNode);
                                completedTrapezoids.RemoveAt(j);
                                var leftTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, trapezoid.LeftLine, newLine);
                                var rightTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, newLine, trapezoid.RightLine);
                                completedTrapezoids.Insert(j, rightTrapezoid); //right trapezoid will end up right below left trapezoid
                                completedTrapezoids.Insert(j, leftTrapezoid); //left trapezoid will end up were the original trapezoid was located
                                j++; //Extra counter to skip extra trapezoid
                            }
                            else if (trapezoid.BottomNode.Type == NodeType.UpwardReflex) //If bottom node is reflex up (if TopNode.Type = 0, this if statement will be skipped).
                            {
                                var newLine = new PolygonSegment(trapezoid.TopNode, trapezoid.BottomNode);
                                completedTrapezoids.RemoveAt(j);
                                var leftTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, trapezoid.LeftLine, newLine);
                                var rightTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, newLine, trapezoid.RightLine);
                                completedTrapezoids.Insert(j, rightTrapezoid); //right trapezoid will end up right below left trapezoid
                                completedTrapezoids.Insert(j, leftTrapezoid); //left trapezoid will end up were the original trapezoid was located
                                j++; //Extra counter to skip extra trapezoid
                            }
                        }

                        //Create Monotone Polygons from Trapezoids
                        var currentTrap = completedTrapezoids[0];
                        var monotoneTrapPolygon1 = new List<Trapezoid> { currentTrap };
                        var monotoneTrapPolygons = new List<List<Trapezoid>> { monotoneTrapPolygon1 };
                        //for each trapezoid except the first one, which was added in the intitialization above.
                        for (var j = 1; j < completedTrapezoids.Count; j++)
                        {
                            //Check if next trapezoid can attach to any existing monotone polygon
                            var boolstatus = false;
                            foreach (var monotoneTrapPolygon in monotoneTrapPolygons)
                            {
                                currentTrap = monotoneTrapPolygon.Last();

                                if (currentTrap.BottomNode == completedTrapezoids[j].TopNode &&
                                    (currentTrap.LeftLine == completedTrapezoids[j].LeftLine ||
                                     currentTrap.RightLine == completedTrapezoids[j].RightLine))
                                {
                                    monotoneTrapPolygon.Add(completedTrapezoids[j]);
                                    boolstatus = true;
                                    break;
                                }
                            }
                            // If they cannot be attached to any existing monotone polygon, create a new monotone polygon
                            if (boolstatus == false)
                            {
                                var trapezoidList = new List<Trapezoid> { completedTrapezoids[j] };
                                monotoneTrapPolygons.Add(trapezoidList);
                            }
                        }

                        //Convert the lists of trapezoids that form monotone polygons into the monotone polygon class\
                        //This class includes a sorted list of all the points in the monotone polygon and two monotone chains.
                        //Both of these lists are used during traingulation.
                        var monotonePolygons = new List<MonotonePolygon>();
                        foreach (var monotoneTrapPoly in monotoneTrapPolygons)
                        {
                            //Biuld the right left chains and the sorted list of all nodes
                            var monotoneRightChain = new List<Vertex2D>();
                            var monotoneLeftChain = new List<Vertex2D>();
                            var sortedMonotonePolyNodes = new List<Vertex2D>();

                            //Add upper node to both chains and sorted list
                            monotoneRightChain.Add(monotoneTrapPoly[0].TopNode);
                            monotoneLeftChain.Add(monotoneTrapPoly[0].TopNode);
                            sortedMonotonePolyNodes.Add(monotoneTrapPoly[0].TopNode);

                            //Add all the middle nodes to one chain (right or left)
                            for (var j = 1; j < monotoneTrapPoly.Count; j++)
                            {
                                //Add the topNode of each trapezoid (minus the initial trapezoid) to the sorted list.
                                sortedMonotonePolyNodes.Add(monotoneTrapPoly[j].TopNode);

                                //If trapezoid upper node is on the right line, add it to the right chain
                                if (monotoneTrapPoly[j].RightLine.FromPoint == monotoneTrapPoly[j].TopNode ||
                                    monotoneTrapPoly[j].RightLine.ToPoint == monotoneTrapPoly[j].TopNode)
                                {
                                    monotoneRightChain.Add(monotoneTrapPoly[j].TopNode);
                                }
                                //Else add it to the left chain
                                else
                                {
                                    monotoneLeftChain.Add(monotoneTrapPoly[j].TopNode);
                                }
                            }

                            //Add bottom node of last trapezoid to both chains and sorted list
                            monotoneRightChain.Add(monotoneTrapPoly.Last().BottomNode);
                            monotoneLeftChain.Add(monotoneTrapPoly.Last().BottomNode);
                            sortedMonotonePolyNodes.Add(monotoneTrapPoly.Last().BottomNode);

                            //Create new monotone polygon based on these two chains and sorted list.
                            var monotonePolygon = new MonotonePolygon(monotoneLeftChain, monotoneRightChain, sortedMonotonePolyNodes);
                            monotonePolygons.Add(monotonePolygon);
                        }
                        #endregion

                        #region DEBUG: Find a chain containing a particular vertex
                        //var vertexInQuestion = new Vertex(new Vector2 { 200.0, 100.0, 750.0 });
                        //foreach (var monotonePolygon in monotonePolygons)
                        //{
                        //    foreach (var node in monotonePolygon.SortedNodes)
                        //    {
                        //        var vertex = node.Point.References[0];
                        //        if (vertex.X.IsPracticallySame(vertexInQuestion.X) &&
                        //            vertex.Y.IsPracticallySame(vertexInQuestion.Y) &&
                        //            vertex.Z.IsPracticallySame(vertexInQuestion.Z))
                        //        {
                        //            break;
                        //        }
                        //    }
                        //}
                        #endregion

                        #region Triangulate Monotone Polygons
                        //Triangulates the monotone polygons
                        var newTriangles = new List<int[]>();
                        foreach (var monotonePolygon2 in monotonePolygons)
                            newTriangles.AddRange(Triangulate(monotonePolygon2));
                        triangleFaceList.Add(newTriangles);
                        numTriangles += newTriangles.Count;
                        groupsOfLoops.Add(group);
                        #endregion
                    }
                    //Check to see if the proper number of triangles were created from this set of loops
                    //For a polygon: triangles = (number of vertices) - 2
                    //The addition of negative loops makes this: triangles = (number of vertices) + 2*(number of negative loops) - 2
                    //The most general form (by inspection) is then: triangles = (number of vertices) + 2*(number of negative loops) - 2*(number of positive loops)
                    //You could individually solve the equation for each positive loop, but simpler just to use most general form.
                    if (numTriangles != pointCount + 2 * negativeLoopCount - 2 * positiveLoopCount)
                    {
                        throw new Exception("Incorrect number of triangles created in triangulate function");
                    }
                    successful = true;

                    #region DEBUG: Find a particular triangle or all triangles with a particular vertex
                    //Find all triangles with a particular vertex
                    //var vertexInQuestion1 = new Vertex(new Vector2 { 200.0, 100.0, 750.0 });
                    //var vertexInQuestion2 = new Vertex(new Vector2 { 50.0, 100.0, 784.99993896484375 });
                    //var vertexInQuestion3 = new Vertex(new Vector2 { 250.0, 100.0, 657.68762588382879 });
                    //var trianglesInQuestion = new List<Vertex[]>();
                    //foreach (var triangle in triangles)
                    //{
                    //    foreach (var vertex in triangle)
                    //    {
                    //        if (vertex.X.IsPracticallySame(vertexInQuestion1.X) && 
                    //            vertex.Y.IsPracticallySame(vertexInQuestion1.Y) && 
                    //            vertex.Z.IsPracticallySame(vertexInQuestion1.Z))
                    //        {
                    //            trianglesInQuestion.Add(triangle);
                    //            break;
                    //        }
                    //    }
                    //}
                    //trianglesInQuestion.Clear();
                    //var p = -1;
                    //for (var q = 0; q < triangles.Count(); q++ )
                    //{
                    //    var triangle = triangles[q];
                    //    foreach (var vertex in triangle)
                    //    {
                    //        if (vertex.X.IsPracticallySame(vertexInQuestion1.X) &&
                    //            vertex.Y.IsPracticallySame(vertexInQuestion1.Y) &&
                    //            vertex.Z.IsPracticallySame(vertexInQuestion1.Z))
                    //        {
                    //            foreach (var vertex2 in triangle)
                    //            {
                    //                if (vertex2.X.IsPracticallySame(vertexInQuestion2.X) &&
                    //                    vertex2.Y.IsPracticallySame(vertexInQuestion2.Y) &&
                    //                    vertex2.Z.IsPracticallySame(vertexInQuestion2.Z))
                    //                {
                    //                    foreach (var vertex3 in triangle)
                    //                    {
                    //                        if (vertex3.X.IsPracticallySame(vertexInQuestion3.X) &&
                    //                            vertex3.Y.IsPracticallySame(vertexInQuestion3.Y) &&
                    //                            vertex3.Z.IsPracticallySame(vertexInQuestion3.Z))
                    //                        {
                    //                            trianglesInQuestion.Add(triangle);
                    //                            p = q;
                    //                            break;
                    //                        }
                    //                    }
                    //                }
                    //            }
                    //        }
                    //    }
                    //}
                    //trianglesInQuestion.Clear();
                    #endregion

                }
                catch (Exception e)
                {
                    if (attempts >= 3)
                    {
                        throw new Exception("Triangulation failed after " + attempts +
                                            " attempts. Points may be too close to distinguish.");
                    }
                    attempts++;
                }
            }
            while (!successful);
            return triangleFaceList;
        }


        #region Get Node Type
        /// <summary>
        /// Gets the type of node for B.
        /// </summary>
        /// A, B, and C are counterclockwise ordered points.
        internal static NodeType GetNodeType(Vertex2D a, Vertex2D b, Vertex2D c)
        {
            var angle = MiscFunctions.InteriorAngleBetweenEdgesInCCWList(a.Coordinates, b.Coordinates, c.Coordinates);
            if (angle > Math.PI * 2) throw new Exception();
            if (a.Y.IsPracticallySame(b.Y))
            {
                if (c.Y.IsPracticallySame(b.Y))
                {
                    if (a.X.IsPracticallySame(c.X)) return NodeType.Duplicate; //signifies an error (two points with practically the same coordinates)
                    return a.X > c.X ? NodeType.Right : NodeType.Left;
                }
                if (c.Y > b.Y) return angle > Math.PI ? NodeType.DownwardReflex : NodeType.Left;
                //else c.Y < b.Y
                return angle > Math.PI ? NodeType.UpwardReflex : NodeType.Right;
            }

            if (a.Y < b.Y)
            {
                if (c.Y.IsPracticallySame(b.Y)) return angle < Math.PI ? NodeType.Peak : NodeType.Left;
                if (c.Y > b.Y) return NodeType.Left;
                //else c.Y < b.Y
                return angle < Math.PI ? NodeType.Peak : NodeType.UpwardReflex;
            }

            //else a.Y > b.Y)
            if (c.Y.IsPracticallySame(b.Y)) return angle < Math.PI ? NodeType.Root : NodeType.Right;
            if (c.Y > b.Y) return angle < Math.PI ? NodeType.Root : NodeType.DownwardReflex;
            //else (c.Y < b.Y)
            return NodeType.Right;
        }
        #endregion

        #region Create Trapezoid and Insert Into List
        internal static void InsertTrapezoid(Vertex2D node, PolygonSegment leftLine, PolygonSegment rightLine, List<PartialTrapezoid> trapTree, List<Trapezoid> completedTrapezoids)
        {
            var matchesTrap = false;
            var i = 0;
            while (matchesTrap == false && i < trapTree.Count)
            {
                var partialTrap = trapTree[i];
                if (partialTrap.Contains(leftLine, rightLine))
                {
                    var newTrapezoid = new Trapezoid(partialTrap.TopNode, node, partialTrap.LeftLine, partialTrap.RightLine);
                    completedTrapezoids.Add(newTrapezoid);
                    trapTree.Remove(partialTrap);
                    matchesTrap = true;
                }
                i++;
            }
            if (matchesTrap == false) throw new Exception("Trapezoid failed to find left or right line.");
        }
        #endregion

        #region Find Lines to Left or Right
        internal static int LinesToLeft(Vertex2D node, IEnumerable<PolygonSegment> lineList, out PolygonSegment leftLine, out bool isOnLine)
        {
            isOnLine = false;
            leftLine = null;
            var xleft = double.NegativeInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                //Check to make sure that the line does not contain the node
                if (line.FromPoint == node || line.ToPoint == node) continue;
                //Find distance to line
                var x = line.XGivenY(node.Y, out _);
                var xdif = x - node.X;
                if (xdif.IsNegligible()) isOnLine = true; //If one a line, make true, but don't add to count
                if (xdif < 0 && !xdif.IsNegligible())//Moved to the left by some tolerance 
                {

                    counter++;
                    if (xdif.IsPracticallySame(xleft)) // if approximately equal
                    {
                        //Find the shared node
                        Vertex2D nodeOnLine;
                        if (leftLine == null) throw new Exception("Null Reference");
                        if (leftLine.ToPoint == line.FromPoint)
                        {
                            nodeOnLine = line.FromPoint;
                        }
                        else if (leftLine.FromPoint == line.ToPoint)
                        {
                            nodeOnLine = line.ToPoint;
                        }
                        else throw new Exception("Rounding Error");

                        //Choose whichever line has the right most other node
                        //Note that this condition will only occur when line and
                        //leftLine share a node. 
                        leftLine = nodeOnLine.EndLine.FromPoint.X > nodeOnLine.StartLine.ToPoint.X ? nodeOnLine.EndLine : nodeOnLine.StartLine;
                    }
                    else if (xdif >= xleft)
                    {
                        xleft = xdif;
                        leftLine = line;
                    }
                }
            }
            return counter;
        }

        internal static void FindLeftLine(Vertex2D node, IEnumerable<PolygonSegment> lineList, out PolygonSegment leftLine)
        {
            bool isOnLine;
            LinesToLeft(node, lineList, out leftLine, out isOnLine);
            if (leftLine == null) throw new Exception("Failed to find line to left.");
        }

        internal static int LinesToRight(Vertex2D node, IEnumerable<PolygonSegment> lineList, out PolygonSegment rightLine, out bool isOnLine)
        {
            isOnLine = false;
            rightLine = null;
            var xright = double.PositiveInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                //Check to make sure that the line does not contain the node
                if (line.FromPoint == node || line.ToPoint == node) continue;
                //Find distance to line
                var x = line.XGivenY(node.Y, out _);
                var xdif = x - node.X;
                if (xdif.IsNegligible()) isOnLine = true; //If one a line, make true, but don't add to count
                if (xdif > 0 && !xdif.IsNegligible())//Moved to the right by some tolerance
                {
                    counter++;
                    if (xdif.IsPracticallySame(xright)) // if approximately equal
                    {
                        //Choose whichever line has the right most other node
                        //Note that this condition will only occur when line and
                        //leftLine share a node.                        
                        Vertex2D nodeOnLine;
                        if (rightLine == null) throw new Exception("Null Reference");
                        if (rightLine.ToPoint == line.FromPoint)
                        {
                            nodeOnLine = line.FromPoint;
                        }
                        else if (rightLine.FromPoint == line.ToPoint)
                        {
                            nodeOnLine = line.ToPoint;
                        }
                        else throw new Exception("Rounding Error");

                        //Choose whichever line has the right most other node
                        rightLine = nodeOnLine.EndLine.FromPoint.X > nodeOnLine.StartLine.ToPoint.X ? nodeOnLine.StartLine : nodeOnLine.EndLine;
                    }
                    else if (xdif <= xright) //If less than
                    {
                        xright = xdif;
                        rightLine = line;
                    }
                }
            }
            return counter;
        }

        internal static void FindRightLine(Vertex2D node, IEnumerable<PolygonSegment> lineList, out PolygonSegment rightLine)
        {
            bool isOnLine;
            LinesToRight(node, lineList, out rightLine, out isOnLine);
            if (rightLine == null) throw new Exception("Failed to find line to right.");
        }
        #endregion

        #region Insert Node in Sorted List
        internal static int InsertNodeInSortedList(List<Vertex2D> sortedNodes, Vertex2D node)
        {
            //Search for insertion location starting from the first element in the list.
            for (var i = 0; i < sortedNodes.Count; i++)
            {
                if (node.Y.IsPracticallySame(sortedNodes[i].Y) && node.X.IsPracticallySame(sortedNodes[i].X)) //Descending X
                {
                    throw new Exception("Points are practically the same.");
                }
                if (node.Y.IsPracticallySame(sortedNodes[i].Y) && node.X > sortedNodes[i].X) //Descending X
                {
                    sortedNodes.Insert(i, node);
                    return i;
                }
                if (node.Y > sortedNodes[i].Y) //Descending Y
                {
                    sortedNodes.Insert(i, node);
                    return i;
                }
            }

            //If not greater than any elements in the list, add the element to the end of the list
            sortedNodes.Add(node);
            return sortedNodes.Count - 1;
        }
        #endregion

        #region Merge Two Sorted Lists of Nodes
        internal static void MergeSortedListsOfNodes(List<Vertex2D> sortedNodes, List<Vertex2D> negativeLoop, Vertex2D startingNode)
        {
            //For each node in negativeLoop, minus the first node (which is already in the list)
            var nodeId = sortedNodes.IndexOf(startingNode);
            for (var i = 1; i < negativeLoop.Count; i++)
            {
                var isInserted = false;
                //Starting from after the nodeID, search for an insertion location
                for (var j = nodeId + 1; j < sortedNodes.Count; j++)
                {
                    if (negativeLoop[i].Y.IsPracticallySame(sortedNodes[j].Y) && negativeLoop[i].X.IsPracticallySame(sortedNodes[j].X)) //Descending X
                    {
                        throw new Exception("Points are practically the same.");
                    }
                    if (negativeLoop[i].Y.IsPracticallySame(sortedNodes[j].Y) && negativeLoop[i].X > sortedNodes[j].X) //Descending X
                    {
                        sortedNodes.Insert(j, negativeLoop[i]);
                        isInserted = true;
                        break;
                    }
                    if (negativeLoop[i].Y > sortedNodes[j].Y) //Descending Y
                    {
                        sortedNodes.Insert(j, negativeLoop[i]);
                        isInserted = true;
                        break;
                    }
                }

                //If not greater than any elements in the list, ERROR. This should never happen since the negative loop is assumed
                //to be fully encapsulated by the positive polygon.
                if (isInserted == false)
                {
                    throw new Exception("Negative loop must be fully enclosed");
                }
            }
        }
        #endregion

        #region Triangulate Monotone Polygon
        internal static List<int[]> Triangulate(MonotonePolygon monotonePolygon)
        {
            var triangles = new List<int[]>();
            var scan = new List<Vertex2D>();
            var leftChain = monotonePolygon.LeftChain;
            var rightChain = monotonePolygon.RightChain;
            var sortedNodes = monotonePolygon.SortedNodes;


            //For each node other than the start and finish, add a chain affiliation.
            //Note that this is updated each time the triangulate function is called, 
            //thus allowing a node to be part of multiple monotone polygons
            for (var i = 1; i < leftChain.Count; i++)
            {
                var node = leftChain[i];
                node.IsRightChain = false;
                node.IsLeftChain = true;
            }
            for (var i = 1; i < rightChain.Count; i++)
            {
                var node = rightChain[i];
                node.IsRightChain = true;
                node.IsLeftChain = false;
            }
            //The start node belongs to both chains
            var startNode = sortedNodes[0];
            startNode.IsRightChain = true;
            startNode.IsLeftChain = true;
            //The end node belongs to both chains
            var endNode = sortedNodes.Last();
            endNode.IsRightChain = true;
            endNode.IsLeftChain = true;

            //Add first two nodes to scan 
            scan.Add(startNode);
            scan.Add(sortedNodes[1]);

            //Begin to find triangles
            for (var i = 2; i < sortedNodes.Count; i++)
            {
                var node = sortedNodes[i];
                ////If the root, make the final triangle regardless of angle/area tolerance
                if (i == sortedNodes.Count - 1 && scan.Count == 2)
                {
                    AddTriangle(triangles, node, scan[0], scan[1]);
                    continue;
                }
                //If the nodes is on the opposite chain from any other node (s). 
                if ((node.IsLeftChain && (scan.Last().IsLeftChain == false || scan[scan.Count - 2].IsLeftChain == false)) ||
                    (node.IsRightChain && (scan.Last().IsRightChain == false || scan[scan.Count - 2].IsRightChain == false)))
                {
                    while (scan.Count > 1)
                    {
                        //Do not skip, even if angle is close to Math.PI, because skipping could break the algorithm (create incorrect triangles)
                        //Better to output negligible triangles.
                        AddTriangle(triangles, node, scan[0], scan[1]);
                        scan.RemoveAt(0);
                        //Make the new scan[0] point both left and right for the remaining chain
                        //Essentially this moves the peak. 
                        //Though not mentioned explicitly in algorithm description, this step is required.
                        scan[0].IsLeftChain = true;
                        scan[0].IsRightChain = true;
                    }
                    //If we haven't added the node to the list, add node to end of scan list
                    scan.Add(node);
                }
                else
                {
                    var exitBool = false;
                    while (scan.Count > 1 && exitBool == false)
                    {
                        //Check to see if the angle is concave (Strictly less than PI). Exit if it is convex.
                        //Note that if the chain is the right chain, the order of nodes will be backwards 
                        var angle = (node.IsRightChain)
                            ? MiscFunctions.InteriorAngleBetweenEdgesInCCWList(node.Coordinates, scan.Last().Coordinates, scan[scan.Count - 2].Coordinates)
                            : MiscFunctions.ExteriorAngleBetweenEdgesInCCWList(node.Coordinates, scan.Last().Coordinates, scan[scan.Count - 2].Coordinates);
                        //Skip if greater than OR close to Math.PI, because that will yield a Negligible area triangle
                        if (angle > Math.PI || Math.Abs(angle - Math.PI) < 1E-6)
                        {
                            exitBool = true;
                            continue;
                        }
                        AddTriangle(triangles, scan[scan.Count - 2], scan.Last(), node);
                        //Remove last node from scan list 
                        scan.Remove(scan.Last());
                    }
                    //Regardless of whether the while loop is activated, add node to scan list
                    scan.Add(node);
                }
            }
            //Check to see if the proper number of triangles were created from this monotone polygon
            if (triangles.Count != sortedNodes.Count - 2)
            {
                throw new Exception("Incorrect number of triangles created in triangulate monotone polgon function. This is likely due to angle and area tolerances.");
            }
            return triangles;
        }

        private static void AddTriangle(List<int[]> triangles, Vertex2D node1, Vertex2D node2, Vertex2D node3)
        {
            var cross = (node2.Coordinates - node1.Coordinates).Cross(node3.Coordinates - node1.Coordinates);
            if (cross >= 0) triangles.Add(new[] { node1.ReferenceID, node2.ReferenceID, node3.ReferenceID });
            else triangles.Add(new[] { node3.ReferenceID, node2.ReferenceID, node1.ReferenceID });
        }
        #endregion
    }

}
