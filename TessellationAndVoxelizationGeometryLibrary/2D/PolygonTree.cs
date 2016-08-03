using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// A list of one outer polygon and all the polygons inside it.
    /// </summary>
    public class PolygonTree
    {
        /// <summary>
        /// The list of all the polygons inside the outer polygon. that make up a polygon.
        /// </summary>
        public readonly IEnumerable<Polygon> InnerPolygons;

        /// <summary>
        /// The outer most polygon. All other polygons are inside it.
        /// </summary>
        public readonly Polygon OuterPolygon;

        internal PolygonTree() { }

        internal PolygonTree(Polygon outerPolygon, IEnumerable<Polygon> innerPolygons)
        {
            if (!outerPolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = outerPolygon;
            InnerPolygons = new List<Polygon>(innerPolygons);
        }
    }
    
    /// <summary>
    /// A list of one positive polygon and all the negative polygons directly inside it.
    /// </summary>
    public class ShallowPolygonTree
    {
        /// <summary>
        /// The list of all the negative polygons inside the positive=outer polygon.
        /// There can be NO positive polygons inside this class, since this is a SHALLOW Polygon Tree
        /// </summary>
        public IList<Polygon> InnerPolygons;

        /// <summary>
        /// The outer most polygon, which is always positive. THe negative polygons are inside it.
        /// </summary>
        public readonly Polygon OuterPolygon;

        internal ShallowPolygonTree() { }

        internal ShallowPolygonTree(Polygon positivePolygon)
        {
            if (!positivePolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            InnerPolygons = new List<Polygon>();
            OuterPolygon = positivePolygon;
        }

        internal ShallowPolygonTree(Polygon positivePolygon, ICollection<Polygon> negativePolygons)
        {
            if (!positivePolygon.IsPositive) throw new Exception("The outer polygon must be positive");
            OuterPolygon = positivePolygon;
            if (negativePolygons.Any(negativePolygon => negativePolygon.IsPositive))
            {
                throw new Exception("The inner polygons must be negative");
            }
            InnerPolygons = new List<Polygon>(negativePolygons);
        }
    }

    /// <summary>
    /// A set of methods for building polygon trees.
    /// The polygons do not need to be ordered correctly, because this function
    /// with fix their CCW+ or CW- ordering based on EvenOdd logic.
    /// The polygons are assumed to be non-selfintersecting and DO NOT INTERSECT ONE ANOTHER
    /// </summary>
    public static class BuildPolygonTrees
    {
        internal static List<ShallowPolygonTree> Shallow(IList<Polygon> polygons)
        {
            var shallowPolygonTrees = new List<ShallowPolygonTree>();

            //Need to put the polygons into a dictionary, so we can grab the correct polygon later by index.
            //Make sure the all the points in the polygon's path know what index the polygon is.
            //Also, make sure that all the polygons are simple.
            var polygonDictionary = new Dictionary<int, Polygon>();
            for (var i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].IsOpen) throw new Exception("cannot handle open polygons");
                if (polygons[i].IsSelfIntersecting) throw new Exception("cannot handle open complex polygons");
                polygonDictionary.Add(i, polygons[i]);
                foreach (var point in polygons[i].Path)
                {
                    point.PolygonIndex = i;
                }
            }

            //Sort the points in each polygon and get a list of the first sorted point in each polygon.
            //These will be used to determine the polygon tree.
            var firstPointFromEachPolygon = new List<Point>();
            var tempSortedPaths = new List<List<Point>>();
            const int precision = 15;
            foreach (var polygon in polygons)
            {
                var sortedPath =
                    new List<Point>(polygon.Path.OrderByDescending(node => Math.Round(node.Y, precision)).ThenByDescending(node =>
                        Math.Round(node.X, precision)).ToList());
                tempSortedPaths.Add(sortedPath);
                firstPointFromEachPolygon.Add(sortedPath[0]);
            }
            //Make a readonly collection for the sorted paths, because we will be referencing this collection and do not want it to be mutated.
            var sortedPaths = new ReadOnlyCollection<List<Point>>(tempSortedPaths);

            //First, find the first node from each loop and then sort them. This determines the order the loops
            //will be visited in.
            var sortedFirstPoints = firstPointFromEachPolygon.OrderByDescending(node => Math.Round(node.Y, precision)).ThenByDescending(node =>
                Math.Round(node.X, precision)).ToList();

            //Use a line sweep to track whether loops are inside other loops
            //Build shallow polygon trees with this operation AND sets CCW+/CW- based on EvenOdd logic.
            while (tempSortedPaths.Any())
            {
                //Set the start polygon and begin creating a new ShallowPolygonTree
                var startPolygon = polygonDictionary[sortedFirstPoints[0].PolygonIndex];
                startPolygon.IsPositive = true; //The first loop in the group must always be CCW positive
                var shallowPolygonTree = new ShallowPolygonTree(startPolygon);
                
                //Set the start path and remove necessary information
                var startPath = sortedPaths[sortedFirstPoints[0].PolygonIndex];
                sortedFirstPoints.RemoveAt(0);
                tempSortedPaths.Remove(startPath);
                if (!sortedFirstPoints.Any()) continue; //Exit while loop
                var sortedGroup = new List<Point>(startPath);

                //Add the remaining first points from each loop into sortedGroup.
                foreach (var firstPoint in sortedFirstPoints)
                {
                    InsertPointInSortedList(sortedGroup, firstPoint);
                }

                //inititallize lineList 
                var lineList = new HashSet<Line>();

                //Visit all the points in the sorted group, paying particular attention to points that were
                //the first point in their sorted path (Contained in sortedFirstPoints). For each of those points
                //we will check the lines to left and right to see whether it is inside the polygon. We do
                //this by using a method called winding numbers, where the point is considered inside a polygon
                //if the lines to left and lines to right are odd. If they are even, then the points is not inside,
                //though it may be inside a hole within a larger polygon. This is why we build shallow polygon trees,
                //rather than polygon trees. To build polygon trees, we need to check the shallow polygon trees positive 
                //loop against other trees inner loops in a similar test.
                for (var j = 0; j < sortedGroup.Count; j++)
                {
                    var point = sortedGroup[j];

                    if (sortedFirstPoints.Contains(point)) //if first point in the sorted loop 
                    {
                        bool isInside;
                        bool isOnLine;
                        
                        //If both LinesToLeft and LinesToRight are odd, then it must be inside.
                        //If only lines to left is odd and linesToRight is positive, there was an error
                        //If remainder is not equal to 0, then it is odd.
                        Line leftLine;
                        var leftIsOdd = LinesToLeft(point, lineList.ToList(), out leftLine, out isOnLine)%2 != 0;
                        Line rightLine;
                        var rightIsOdd = LinesToRight(point, lineList.ToList(), out rightLine, out isOnLine) % 2 != 0;
                        if (leftIsOdd && rightIsOdd)
                        {
                            isInside = true;
                        }
                        else if(leftIsOdd || rightIsOdd) throw new Exception("Both must either be odd or even. Debug LinesToLeft/Right functions to fix.");
                        else isInside = false;
                        
                        //Merge the path into this one and remove from the tempList
                        if (isInside)
                        {
                            //Set this path to negative CW
                            //Add it to the inner polygons of the shallow polygon tree.
                            var negativePath = polygonDictionary[point.PolygonIndex];
                            negativePath.IsPositive = false; 
                            shallowPolygonTree.InnerPolygons.Add(negativePath); 

                            //Update the lists
                            sortedFirstPoints.Remove(point);
                            tempSortedPaths.Remove(sortedPaths[point.PolygonIndex]);
                            if (!tempSortedPaths.Any()) break; //That was the last path

                            //Add all the sorted points from this negative polygon into the sorted group of points.
                            MergeSortedListsOfPoints(sortedGroup, sortedPaths[point.PolygonIndex], point);
                        }
                        else //remove the point from this group and continue
                        {
                            sortedGroup.Remove(point);
                            j--; //Pick the same index for the next iteration as the point which was just removed
                            continue;
                        }
                    }

                    //Add to or remove lines from line sweep
                    foreach (var line in point.Lines)
                    {
                        if (lineList.Contains(line))
                        {
                            lineList.Remove(line);
                        }
                        else lineList.Add(line);
                    }
                }
                shallowPolygonTrees.Add(shallowPolygonTree);
            }
            return shallowPolygonTrees;
        }

        #region Insert Node in Sorted List
        internal static int InsertPointInSortedList(List<Point> sortedNodes, Point point)
        {
            //Search for insertion location starting from the first element in the list.
            for (var i = 0; i < sortedNodes.Count; i++)
            {
                if (point.Y.IsPracticallySame(sortedNodes[i].Y) && point.X.IsPracticallySame(sortedNodes[i].X)) //Descending X
                {
                    throw new Exception("Points are practically the same.");
                }
                if (point.Y.IsPracticallySame(sortedNodes[i].Y) && point.X > sortedNodes[i].X) //Descending X
                {
                    sortedNodes.Insert(i, point);
                    return i;
                }
                if (!(point.Y > sortedNodes[i].Y)) continue; //Descending Y
                sortedNodes.Insert(i, point);
                return i;
            }

            //If not greater than any elements in the list, add the element to the end of the list
            sortedNodes.Add(point);
            return sortedNodes.Count - 1;
        }
        #endregion

        #region Merge Two Sorted Lists of Nodes
        internal static void MergeSortedListsOfPoints(List<Point> sortedPoints, List<Point> negativePath, Point startingPoint)
        {
            //For each node in negativePath, minus the first node (which is already in the list)
            var nodeId = sortedPoints.IndexOf(startingPoint);
            for (var i = 1; i < negativePath.Count; i++)
            {
                var isInserted = false;
                //Starting from after the nodeID, search for an insertion location
                for (var j = nodeId + 1; j < sortedPoints.Count; j++)
                {
                    if (negativePath[i].Y.IsPracticallySame(sortedPoints[j].Y) && negativePath[i].X.IsPracticallySame(sortedPoints[j].X)) //Descending X
                    {
                        throw new Exception("Points are practically the same.");
                    }
                    if (negativePath[i].Y.IsPracticallySame(sortedPoints[j].Y) && negativePath[i].X > sortedPoints[j].X) //Descending X
                    {
                        sortedPoints.Insert(j, negativePath[i]);
                        isInserted = true;
                        break;
                    }
                    if (!(negativePath[i].Y > sortedPoints[j].Y)) continue; //Descending Y
                    sortedPoints.Insert(j, negativePath[i]);
                    isInserted = true;
                    break;
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

        #region Find Lines to Left or Right
        private static int LinesToLeft(Point point, IEnumerable<Line> lineList, out Line leftLine, out bool isOnLine)
        {
            isOnLine = false;
            leftLine = null;
            var xleft = double.NegativeInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                //Check to make sure that the line does not contain the point
                if (line.FromPoint == point || line.ToPoint == point) continue;
                
                //Find distance to line
                var x = line.Xintercept(point.Y);
                var xdif = x - point.X;
                if (xdif.IsNegligible()) isOnLine = true; //If one a line, make true, but don't add to count
                
                //If not Moved to the left by some tolerance, continue;
                if (!(xdif < 0) || xdif.IsNegligible()) continue; 
                counter++;
                if (xdif.IsPracticallySame(xleft)) // if approximately equal
                {
                    //Find the shared point
                    Point pointOnLine;
                    if (leftLine == null) throw new Exception("Null Reference");
                    if (leftLine.ToPoint == line.FromPoint)
                    {
                        pointOnLine = line.FromPoint;
                    }
                    else if (leftLine.FromPoint == line.ToPoint)
                    {
                        pointOnLine = line.ToPoint;
                    }
                    else throw new Exception("Rounding Error");

                    //Choose whichever line has the right most other node
                    //Note that this condition will only occur when line and
                    //leftLine share a node. 
                    if(pointOnLine.Lines.Count != 2) throw new Exception("Lines not created properly. Each point should have 2 lines.");
                    leftLine = pointOnLine.Lines[0].OtherPoint(pointOnLine).X > pointOnLine.Lines[1].OtherPoint(pointOnLine).X ? pointOnLine.Lines[0] : pointOnLine.Lines[1];
                }
                else if (xdif >= xleft)
                {
                    xleft = xdif;
                    leftLine = line;
                }
            }
            return counter;
        }

        internal static void FindLeftLine(Point point, IEnumerable<Line> lineList, out Line leftLine)
        {
            bool isOnLine;
            LinesToLeft(point, lineList, out leftLine, out isOnLine);
            if (leftLine == null) throw new Exception("Failed to find line to left.");
        }

        private static int LinesToRight(Point point, IEnumerable<Line> lineList, out Line rightLine, out bool isOnLine)
        {
            isOnLine = false;
            rightLine = null;
            var xright = double.PositiveInfinity;
            var counter = 0;
            foreach (var line in lineList)
            {
                //Check to make sure that the line does not contain the node
                if (line.FromPoint == point || line.ToPoint == point) continue;
                //Find distance to line
                var x = line.Xintercept(point.Y);
                var xdif = x - point.X;
                if (xdif.IsNegligible()) isOnLine = true; //If one a line, make true, but don't add to count

                //If not Moved to the right by some tolerance, continue;
                if (!(xdif > 0) || xdif.IsNegligible()) continue;
                counter++;
                if (xdif.IsPracticallySame(xright)) // if approximately equal
                {
                    //Choose whichever line has the right most other node
                    //Note that this condition will only occur when line and
                    //leftLine share a node.                        
                    Point pointOnLine;
                    if (rightLine == null) throw new Exception("Null Reference");
                    if (rightLine.ToPoint == line.FromPoint)
                    {
                        pointOnLine = line.FromPoint;
                    }
                    else if (rightLine.FromPoint == line.ToPoint)
                    {
                        pointOnLine = line.ToPoint;
                    }
                    else throw new Exception("Rounding Error");

                    //ToDo: verify this next line, since the comment is opposite from the code
                    //Choose whichever line has the right most other node
                    if (pointOnLine.Lines.Count != 2) throw new Exception("Lines not created properly. Each point should have 2 lines.");
                    rightLine = pointOnLine.Lines[0].OtherPoint(pointOnLine).X > pointOnLine.Lines[1].OtherPoint(pointOnLine).X ? pointOnLine.Lines[1] : pointOnLine.Lines[0];
                }
                else if (xdif <= xright) //If less than
                {
                    xright = xdif;
                    rightLine = line;
                }
            }
            return counter;
        }

        internal static void FindRightLine(Point node, IEnumerable<Line> lineList, out Line rightLine)
        {
            bool isOnLine;
            LinesToRight(node, lineList, out rightLine, out isOnLine);
            if (rightLine == null) throw new Exception("Failed to find line to right.");
        }
        #endregion
    }
}
