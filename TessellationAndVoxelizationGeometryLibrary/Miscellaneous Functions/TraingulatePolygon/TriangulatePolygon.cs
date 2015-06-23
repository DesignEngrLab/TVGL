using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.ServiceModel.Channels;
using StarMathLib;
using MIConvexHull;

namespace TVGL.Miscellaneous_Functions.TraingulatePolygon
{
    public static class TriangulatePolygon
    {
        /// <summary>
        ///     Triangulates a Polygon into faces.
        /// </summary>
        /// <param name="points2D">The 2D points represented in loops.</param>
        /// <param name="isPositive">Indicates whether the corresponding loop is positive or not.</param>
        /// <returns>List&lt;Point[]&gt;, which represents vertices of new faces.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static List<Point[]> Run(List<Point[]> points2D, Boolean[] isPositive)
        {

            #region Preprocessing
            //Preprocessing
            // 1) For each loop in points2D
            // 2)   Create nodes and lines from points, and retain whether a point
            //      was in a positive or negative loop.
            // 3)   Add nodes to an ordered loop (same as points2D except now Nodes) 
            //      and a sorted loop (used for sweeping).
            var i = 0;
            var orderedLoops = new List<List<Node>>();
            var sortedLoops = new List<List<Node>>();
            var listPositive = isPositive.ToList<bool>();

            foreach (var loop in points2D)  
            {
                var orderedLoop = new List<Node>(); 

                //Create first node
                var nodeType = NodeType(loop.Last(),loop[0],loop[1]);
                var firstNode = new Node(loop.Last(), nodeType, i);
                var previousNode = firstNode;
                Line line = null;
                orderedLoop.Add(firstNode);

                //Create other nodes
                for (var j = 1; j < loop.Count()-1; j++)
                {
                    //Create New Node
                    nodeType = NodeType(loop[j-1], loop[j], loop[j+1]);
                    var node = new Node(loop[j], nodeType, i);
                  
                    //Add node to the ordered loop
                    orderedLoop.Add(node);

                    //Create New Line
                    line = new Line(previousNode, node);
                    previousNode.StartLine = line;
                    node.EndLine = line;
                    
                    previousNode = node;
                 }

                //Create last node
                nodeType = NodeType(loop[loop.Count() - 2], loop[loop.Count()-1], loop[0]);
                var lastNode = new Node(loop[loop.Count() - 1], nodeType, i);
                orderedLoop.Add(lastNode);

                //Create both missing lines 
                line = new Line(previousNode, lastNode);
                previousNode.StartLine = line;
                lastNode.EndLine = line;
                line = new Line(lastNode, firstNode);
                lastNode.StartLine = line;
                firstNode.EndLine = line;

                //Sort nodes by descending Y, ascending X
                var sortedLoop = orderedLoop.OrderByDescending(node => node.Y).ThenBy(node => node.X).ToList<Node>();
                orderedLoops.Add(orderedLoop);
                sortedLoops.Add(sortedLoop);
                i++;
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
            var completeListSortedLoops = sortedLoops;
            while (orderedLoops.Any())
            {
                //Get information about positive loop, remove from loops, and create new group
                i = listPositive.FindIndex(true);
                var posOrderedLoop = orderedLoops[i];
                var sortedGroup = sortedLoops[i];
                listPositive.RemoveAt(i);
                orderedLoops.RemoveAt(i);
                sortedLoops.RemoveAt(i);

                //Insert the first node from all the negative loops remaining into the group list in the correct sorted order.
                for (var j = 0; j < orderedLoops.Count(); j++)
                {
                    if (listPositive[j] == false)
                    {
                        InsertNodeInSortedList(sortedGroup,sortedLoops[j][0]);
                    }
                }


                //inititallize lineList and sortedNodes
                var lineList = new List<Line>();
                var nodes = new List<Node>();
            

                #region Trapezoidize Polygons

                var trapTree = new List<PartialTrapezoid>();
                var completedTrapezoids = new List<Trapezoid>();

                //Use the red-black tree to determine if the first node from a negative loop is inside the polygon.
                //for each node in sortedNodes, update the lineList. Note that at this point each node only has two edges.
                foreach (var node in sortedGroup)
                {
                    Line leftLine = null;
                    Line rightLine = null;

                    //Check if negative loop is inside polygon 
                    //note that listPositive changes order /size , while isPositive is static like loopID.
                    //Similarly points2D is static.
                    if (node == completeListSortedLoops[node.LoopID][0] && isPositive[node.LoopID] == false) //if first point in the sorted loop and loop is negative 
                    {
                        if (LinesToLeft(node, lineList, leftLine) % 2 != 0) //If remainder is not equal to 0, then it is odd. 
                        {
                            if (LinesToRight(node, lineList, rightLine) % 2 == 0) //If remainder is not equal to 0, then it is odd. 
                            {
                                //NOTE: This node must be a reflex upward point by observation
                                //leftLine and rightLine are set in the two previous call and are now not null.

                                //Add remaining points from loop into sortedGroup.
                                MergeSortedListsOfNodes(sortedGroup, completeListSortedLoops[node.LoopID], node);
                            }
                            else //Number of lines is even. Remove from group and go to next node
                            {
                                sortedGroup.Remove(node);
                                continue;
                            }
                        }
                        else //Number of lines is even. Remove from group and go to next node
                        {
                            sortedGroup.Remove(node);
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
                        case 0: //Downward Reflex
                        {
                            leftLine = FindLeftLine(node, lineList);
                            rightLine = FindRightLine(node, lineList);

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
                        case 1: //Upward Reflex
                        {
                            if (leftLine == null) //If from the first negative point, leftLine and rightLine will already be set.
                            {
                                leftLine = FindLeftLine(node, lineList);
                                rightLine = FindRightLine(node, lineList);
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
                        case 2: //Peak
                        {
                            //Create one new partial trapezoid
                            var newPartialTrapezoid = new PartialTrapezoid(node, node.StartLine, node.EndLine);
                            trapTree.Add(newPartialTrapezoid);
                        }
                            break;
                        case 3: //Root
                        {
                            //Close one trapezoid
                            InsertTrapezoid(node, node.EndLine, node.StartLine, trapTree, completedTrapezoids);
                        }
                            break;
                        case 4: //Left
                        {
                            //Create one trapezoid
                            leftLine = FindLeftLine(node, lineList);
                            rightLine = node.StartLine;
                            InsertTrapezoid(node, leftLine, rightLine, trapTree, completedTrapezoids);

                            //Create one new partial trapezoid
                            var newPartialTrapezoid = new PartialTrapezoid(node, leftLine, node.EndLine);
                            trapTree.Add(newPartialTrapezoid);
                        }
                            break;
                        case 5: //Right
                        {
                            //Create one trapezoid
                            rightLine = FindRightLine(node, lineList);
                            leftLine = node.EndLine;
                            InsertTrapezoid(node, leftLine, rightLine, trapTree, completedTrapezoids);

                            //Create one new partial trapezoid
                            var newPartialTrapezoid = new PartialTrapezoid(node, leftLine, node.EndLine);
                            trapTree.Add(newPartialTrapezoid);
                        }
                            break;
                    }
                }
                #endregion

                #region Create Monotone Polygons

                //for each trapezoid with a reflex edge, split in two. 
                //Insert new trapezoids in correct position in list.
                for (var j = 0; j < completedTrapezoids.Count;j++)
                {
                    var trapezoid = completedTrapezoids[j];
                    if (trapezoid.TopNode.Type == 0) //If upper node is reflex down (bottom node could be reflex up, reflex down, or other)
                    {
                        var newLine = new Line(trapezoid.TopNode, trapezoid.BottomNode);
                        completedTrapezoids.RemoveAt(j);
                        var leftTrapezoid = new Trapezoid(trapezoid.TopNode,trapezoid.BottomNode,trapezoid.LeftLine,newLine);
                        var rightTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, newLine, trapezoid.RightLine);
                        completedTrapezoids.Insert(j, rightTrapezoid); //right trapezoid will end up right below left trapezoid
                        completedTrapezoids.Insert(j, leftTrapezoid); //left trapezoid will end up were the original trapezoid was located
                    }
                    else if (trapezoid.BottomNode.Type == 1) //If bottom node is reflex up (if TopNode.Type = 0, this if statement will be skipped).
                    {
                        var newLine = new Line(trapezoid.TopNode, trapezoid.BottomNode);
                        completedTrapezoids.RemoveAt(j);
                        var leftTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, trapezoid.LeftLine, newLine);
                        var rightTrapezoid = new Trapezoid(trapezoid.TopNode, trapezoid.BottomNode, newLine, trapezoid.RightLine);
                        completedTrapezoids.Insert(j, rightTrapezoid); //right trapezoid will end up right below left trapezoid
                        completedTrapezoids.Insert(j, leftTrapezoid); //left trapezoid will end up were the original trapezoid was located
                    }
                }
                
                //Create Monotone Polygons from Trapezoids
                var currentTrap = completedTrapezoids[0];
                var monotoneTrapPolygons = new List<List<Trapezoid>> ();
                monotoneTrapPolygons[0].Add(currentTrap);
                //for each trapezoid except the first one, which was added in the intitialization above.
                for (var j =1; j < completedTrapezoids.Count; j++)
                {
                    //Check if next trapezoid can attach to any existing monotone polygon
                    var boolstatus = false;
                    for (var k = 0; k < monotoneTrapPolygons.Count; k++)
                    {
                        currentTrap = monotoneTrapPolygons[k].Last();
                        
                        if (currentTrap.BottomNode == completedTrapezoids[j].TopNode)
                        {
                            if (currentTrap.LeftLine == completedTrapezoids[j].LeftLine ||
                                currentTrap.RightLine == completedTrapezoids[j].RightLine)
                            {
                                monotoneTrapPolygons[k].Add(completedTrapezoids[j]);
                                boolstatus = true;
                                break;
                            }
                        }
                    }
                    // If they cannot be attached to any existing monotone polygon, create a new monotone polygon
                    if (boolstatus == false)
                    {
                        var trapezoidList = new List<Trapezoid> {completedTrapezoids[j]};
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
                    var monotoneRightChain = new List<Node>();
                    var monotoneLeftChain = new List<Node>();
                    var sortedMonotonePolyNodes = new List<Node>();

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
                        if (monotoneTrapPoly[j].RightLine.FromNode == monotoneTrapPoly[j].TopNode ||
                            monotoneTrapPoly[j].RightLine.ToNode == monotoneTrapPoly[j].TopNode)
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

                #region Triangulate Monotone Polygons
                //Triangulates the monotone polygons
                var triangles = new List<Point[]>();
                foreach (var monotonePolygon2 in monotonePolygons)
                {
                    Triangulate(monotonePolygon2, triangles);
                }
                #endregion               
            }
            return null;
        }

        #region Get Node Type
        /// <summary>
        /// Gets the type of node for B.
        /// </summary>
        /// A, B, & C are counterclockwise ordered points.
        internal static int NodeType(Point a, Point b, Point c)
        {
            if (a.Y < b.Y)
            {
                if (c.Y < b.Y)
                {
                    return GetAngle(a, b, c) < 180 ? 2 : 1;
                }
                if (c.Y > b.Y)
                {
                    return 4;
                }
                return GetAngle(a, b, c) < 180 ? 4 : 1;
                
            }

            if (a.Y > b.Y)
            {
                if (c.Y > b.Y)
                {
                    return GetAngle(a, b, c) < 180 ? 3 : 0;
                }
                if (c.Y < b.Y)
                {
                    return 5;
                }
                return GetAngle(a, b, c) < 180 ? 5 : 0;
            }

            if (c.Y > b.Y)
            {
                return GetAngle(a, b, c) > 180 ? 4 : 3;
            }
            if (c.Y < b.Y)
            {
                return GetAngle(a, b, c) > 180 ? 5 : 2;
            }   
            if (a.X > c.X)
            {
                return 4;
            }
            return a.X < c.X ? 5 : 11; //11 signifies an error (two points with exactly the same coordinates)     
        }
        #endregion
        
        #region Get Angle Between Three Points That Form Two Vectors
        /// <summary>
        /// Gets the  clockwise angle from line AB to BC. 
        /// </summary>
        /// A, B, & C are counterclockwise ordered points.
        /// "If" statements were determined by observation
        public static double GetAngle(Point a, Point b, Point c)
        {
            var edgeVectors = new double[1][];
            edgeVectors[0] = StarMath.normalize(StarMath.subtract(b.Position, a.Position));
            edgeVectors[1] = StarMath.normalize(StarMath.subtract(c.Position, b.Position));
            

            //Since these points are in 2D, use crossProduct2
            var tempCross = StarMath.crossProduct2(edgeVectors[0], edgeVectors[1]);
            var tempDot = StarMath.dotProduct(edgeVectors[0],edgeVectors[1]);
            var theta = Math.Asin(tempCross);
            if (tempDot >= 0)
            {
                return (tempCross >= 0) ? 180 - theta : 180 + theta;
            }
            //Else, tempDot < 0 ...
            return (tempCross < 0) ? 360 - theta : theta;
        }
        #endregion

        #region Create Trapezoid and Insert Into List
        internal static void InsertTrapezoid(Node node, Line leftLine, Line rightLine, IList<PartialTrapezoid> trapTree, ICollection<Trapezoid> completedTrapezoids )
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
        }
        #endregion

        #region Find Lines to Left or Right
        internal static double LinesToLeft(Node node, IEnumerable<Line> lineList, Line leftLine)
        {
            var xleft = double.NegativeInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                var x = line.Xintercept(node.Y);
                var xdif = x - node.X;
                if (xdif < 0 )
                {
                    counter++;
                    if (xdif > xleft)
                    {
                        xleft = xdif;
                        leftLine = line;
                    }
                }
            }
            return counter;
        }

        internal static Line FindLeftLine(Node node, IEnumerable<Line> lineList)
        {
            Line leftLine = null;
            var counter = LinesToLeft(node, lineList, leftLine);
            return leftLine;
        }

        internal static double LinesToRight(Node node, IEnumerable<Line> lineList, Line rightLine)
        {
            var xright = double.PositiveInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                var x = line.Xintercept(node.Y);
                var xdif = x - node.X;
                if (xdif > 0)
                {
                    counter++;
                    if (xdif < xright)
                    {
                        xright = xdif;
                        rightLine = line;
                    }
                }
            }
            return counter;
        }

        internal static Line FindRightLine(Node node, IEnumerable<Line> lineList)
        {
            Line rightLine = null;
            var counter = LinesToLeft(node, lineList, rightLine);
            return rightLine;
        }
        #endregion
        
        #region Insert Node in Sorted List
        internal static int InsertNodeInSortedList(List<Node> sortedNodes, Node node)
        {
            //Search for insertion location starting from the first element in the list.
            for (int i = 0; i < sortedNodes.Count(); i++)
            {
                if (node.Y > sortedNodes[i].Y)
                {
                    sortedNodes.Insert(i, node);
                    return i;
                }
                if (Math.Abs(node.Y - sortedNodes[i].Y) < 0.000001 && node.X < sortedNodes[i].X) //approaximately equal
                {
                    sortedNodes.Insert(i, node);
                    return i;
                }
            }

            //If not greater than any elements in the list, add the element to the end of the list
            sortedNodes.Add(node);
            return sortedNodes.Count() - 1;
        }
        #endregion

        #region Merge Two Sorted Lists of Nodes
        internal static void MergeSortedListsOfNodes(List<Node> sortedNodes, List<Node> negativeLoop, Node node)
        {
            //For each node in negativeLoop, minus the first node (which is already in the list)
            int nodeID = sortedNodes.IndexOf(node);
            for (int i = 1; i < negativeLoop.Count(); i++ )
            {
                var isInserted = false;
                //Starting from after the nodeID, search for an insertion location
                for (int j = nodeID+1; j < sortedNodes.Count; j++)
                {
                    if (negativeLoop[i].Y > sortedNodes[j].Y)
                    {
                        sortedNodes.Insert(j, negativeLoop[i]);
                        isInserted = true;
                        break;
                    }
                    if (Math.Abs(negativeLoop[i].Y - sortedNodes[j].Y) < 0.000001 && negativeLoop[i].X < sortedNodes[j].X) //approaximately equal
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
                    throw new System.ArgumentException("Negative loop must be fully enclosed");
                }
            }
        }
        #endregion

        #region Triangulate Monotone Polygon
        internal static void Triangulate(MonotonePolygon monotonePolygon, List<Point[]> triangles)
        {
            var scan = new List<Node>();
            var leftChain = monotonePolygon.LeftChain;
            var rightChain = monotonePolygon.RightChain;
            var sortedNodes = monotonePolygon.SortedNodes;

            //Add first two nodes to scan
            scan.Add(sortedNodes[0]);
            scan.Add(sortedNodes[1]);

            for (var i = 2; i < sortedNodes.Count; i++ )
            {
                var node = sortedNodes[i];
                var boolstatus = false;
                if (leftChain[1] == node && rightChain[1] == scan.Last())
                {
                    boolstatus = true;
                }
                else if (leftChain[1] == scan.Last() && rightChain[1] == node)
                {
                    boolstatus = true;
                }
                //If either condition above was true, do the following
                if (boolstatus == true)
                {
                    while (scan.Count > 1)
                    {
                        //Add triangle to list 
                        var triangle = new Point[] { node.Point, scan[0].Point, scan[1].Point };
                        triangles.Add(triangle);

                        //Remove first item in scan list and add node to end of scan list
                        scan.RemoveAt(0);
                        scan.Add(node);
                    }
                }
                else 
                {
                    while (GetAngle(node, scan.Last(), scan[scan.Count - 2]) < 180 && scan.Count() > 1)
                    {
                        //Add triangle to list 
                        var triangle = new Point[] { node.Point, scan.Last().Point, scan[scan.Count - 2].Point};
                        triangles.Add(triangle);

                        //Remove last node from scan list 
                        scan.Remove(scan.Last());
                    }
                    //Regardless of whether the while loop is activated, add node to scan list
                    scan.Add(node);
                }
            }
        }
        #endregion
    }      

}
