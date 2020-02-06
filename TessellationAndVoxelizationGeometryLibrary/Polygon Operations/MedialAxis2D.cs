using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TVGL.Numerics;


namespace TVGL._2D
{
    public static class MedialAxis2D
    {
        /// <summary>
        /// Creates the 2D Medial Axis from a part's Silhouette. Currently ignores holes. 
        /// Best way to show is using "Presenter.ShowAndHang(silhouette, medialAxis, "", Plot2DType.Line, false);"
        /// </summary>
        /// <param name="silhouette"></param>
        public static List<List<Vector2>> Run(IEnumerable<List<Vector2>> silhouette)
        {
            //To Get the 2D Medial Axis:
            //The first four steps create a medial axis and the next three steps sort the axis lines into branches
            //1) Sample the silhouette to get more points
            //2) Get the Delaunay triangulation of the sampled silhouette
            //3) For every triangle in the Delaunay, keep all the edges that have a center point inside the sampled silhouette
            //4) If all three edges are inside, collapse one of the edges
            //5) Merge the points from the lines. Note: there seems to be an error with duplicate edges and disconnected branches
            //6) Get all the nodes (3+ lines)
            //7) Connect all the nodes to form branches with the lines
            var allBranches = new List<List<Vector2>>();
            foreach (var positivePolygon in silhouette.Where(p => MiscFunctions.AreaOfPolygon(p) > 0))
            {
                var sampled = PolygonOperations.SampleWithEdgeLength(positivePolygon, MiscFunctions.Perimeter(positivePolygon) / 600);
                var smaller = PolygonOperations.OffsetRound(sampled, -0.001 * MiscFunctions.Perimeter(positivePolygon)).Select(p => new PolygonLight(p)).First();

                //Delaunay Medial Axis             
                var delaunay = MIConvexHull.Triangulation.CreateDelaunay(sampled.Select(p => new[] { p.X, p.Y }).ToList());
                var lines = new List<List<Point>>();
                foreach (var triangle in delaunay.Cells)
                {
                    var triangleCenterLineVertices = new List<Point>();
                    var edge1Center = new Point(triangle.Vertices[0].Position.add(triangle.Vertices[1].Position)
                        .divide(2));
                    if (MiscFunctions.IsPointInsidePolygon(smaller, edge1Center.Light))
                    {
                        triangleCenterLineVertices.Add(edge1Center);
                    }

                    var edge2Center = new Point(triangle.Vertices[1].Position.add(triangle.Vertices[2].Position)
                        .divide(2));
                    if (MiscFunctions.IsPointInsidePolygon(smaller, edge2Center.Light))
                    {
                        triangleCenterLineVertices.Add(edge2Center);
                    }

                    var edge3Center = new Point(triangle.Vertices[2].Position.add(triangle.Vertices[0].Position)
                        .divide(2));
                    if (MiscFunctions.IsPointInsidePolygon(smaller, edge3Center.Light))
                    {
                        triangleCenterLineVertices.Add(edge3Center);
                    }

                    if (triangleCenterLineVertices.Any())
                    {
                        if (triangleCenterLineVertices.Count == 1)
                        {
                            continue; // This vertex has no line associated with it. 
                                      //If the vertex should be attached to a line, it will show up again for another triangle.
                        }
                        if (triangleCenterLineVertices.Count == 3)
                        {
                            //If there are two long edges with one short, collapse the long edges to the middle
                            //of the short edge.
                            //If 
                            //Order the points, such that the larger edge is not included
                            var d0 = (edge1Center - edge2Center).norm2();
                            var d1 = (edge2Center - edge3Center).norm2();
                            var d2 = (edge3Center - edge1Center).norm2();
                            var ds = new List<double>() { d0, d1, d2 };
                            ds.Sort();
                            if (ds[0] - ds[1] > ds[1] - ds[2])
                            {
                                //There is a bigger difference in length between the longest edge than the other two
                                //Therefore, remove the longest edge.
                                if (d0 > d1 && d0 > d2)
                                {
                                    //If d0 is the largest edge, it should be point 2,3,1
                                    lines.Add(new List<Point> { edge2Center, edge3Center });
                                    lines.Add(new List<Point> { edge3Center, edge1Center });
                                }
                                else if (d1 > d2)
                                {
                                    lines.Add(new List<Point> { edge3Center, edge1Center });
                                    lines.Add(new List<Point> { edge1Center, edge2Center });
                                }
                                else
                                {
                                    lines.Add(new List<Point> { edge1Center, edge2Center });
                                    lines.Add(new List<Point> { edge2Center, edge3Center });
                                }
                            }
                            else
                            {
                                Point newPoint;
                                //Create a new center point on the shortest line and set three point sets
                                if (d0 < d1 && d0 < d2)
                                {
                                    newPoint = new Point((edge1Center + edge2Center).divide(2.0, 2));
                                }
                                else if (d1 < d2)
                                {
                                    newPoint = new Point((edge2Center + edge3Center).divide(2.0, 2));
                                }
                                else
                                {
                                    newPoint = new Point((edge3Center + edge1Center).divide(2.0, 2));
                                }

                                lines.Add(new List<Point> { edge1Center, newPoint });
                                lines.Add(new List<Point> { edge2Center, newPoint });
                                lines.Add(new List<Point> { edge3Center, newPoint });
                            }
                        }
                        else
                        {
                            lines.Add(triangleCenterLineVertices);
                        }
                    }
                }

                //Merge all the points 
                var mergerTolerance = 0.0001;
                for (var j = 0; j < lines.Count - 1; j++)
                {
                    var l1 = lines[j];
                    var sameLineInt = -1;
                    for (var k = j + 1; k < lines.Count; k++)
                    {
                        var sameLineCount = 0;
                        var l2 = lines[k];
                        if (MiscFunctions.DistancePointToPoint(l1[0], l2[0]).IsNegligible(mergerTolerance))
                        {
                            l2[0] = l1[0];
                            sameLineCount++;
                        }
                        else if (MiscFunctions.DistancePointToPoint(l1[0], l2[1]).IsNegligible(mergerTolerance))
                        {
                            l2[1] = l1[0];
                            sameLineCount++;
                        }
                        if (MiscFunctions.DistancePointToPoint(l1[1], l2[0]).IsNegligible(mergerTolerance))
                        {
                            l2[0] = l1[1];
                            sameLineCount++;
                        }
                        else if (MiscFunctions.DistancePointToPoint(l1[1], l2[1]).IsNegligible(mergerTolerance))
                        {
                            l2[1] = l1[1];
                            sameLineCount++;
                        }
                        if (sameLineCount == 2)
                        {
                            sameLineInt = k;
                        }
                    }
                    if (sameLineInt != -1)
                    {
                        //lines.RemoveAt(sameLineInt);
                    }
                }

                //Get all the points
                var points = new HashSet<Point>();
                foreach (var line in lines)
                {
                    points.Add(line[0]);
                    points.Add(line[1]);
                }

                //Get all the node points
                //Also create a new branch for each line that attached to this node
                var nodes = new HashSet<Point>();
                var branches = new List<List<Point>>();
                foreach (var p1 in points)
                {
                    var adjacentLineCount = 0;
                    var adjacentLinesOtherPoints = new List<Point>();
                    foreach (var line in lines)
                    {
                        for (var p = 0; p < 2; p++)
                        {
                            var p2 = line[p];
                            if (p1 != p2) continue;
                            adjacentLineCount++;
                            adjacentLinesOtherPoints.Add(p == 0 ? line[1] : line[0]);
                        }
                    }
                    if (adjacentLineCount > 2)
                    {
                        nodes.Add(p1);
                        //Add a new branch for each adjacent line
                        foreach (var p2 in adjacentLinesOtherPoints)
                        {
                            branches.Add(new List<Point> { p1, p2 });
                        }
                    }
                }

                while (branches.Any())
                {
                    //Pop off the first branch
                    var branch = branches.First();
                    branches.RemoveAt(0);

                    //Continue adding points to this branch until it reaches another node
                    //Or the branch reaches its end
                    var hitEndOfBranch = false;
                    var hitNode = false;
                    while (!hitEndOfBranch && !hitNode)
                    {
                        //Search until we find the next line (2-points) that attaches to this branch
                        //Once it is added, remove it from the list of lines to make future searching faster
                        var p0 = branch.First();
                        var p1 = branch.Last();

                        //Check if we reached a node
                        //If we reached a node, then we need to remove a branch from that node
                        foreach (var node in nodes)
                        {
                            if (p1 != node || branch[0] == node) continue;
                            hitNode = true;
                            //Get the branch that has this node and the prior point
                            var p2 = branch[branch.Count - 2];
                            for (var j = 0; j < branches.Count; j++)
                            {
                                if ((branches[j][0] - p1).norm2().IsNegligible(0.0001) &&
                                   (branches[j][1] - p2).norm2().IsNegligible(0.0001))
                                {
                                    branches.RemoveAt(j);
                                    break;
                                }
                            }
                            break;
                        }
                        if (hitNode) continue;

                        hitEndOfBranch = true;
                        for (var j = 0; j < lines.Count; j++)
                        {
                            var line = lines[j];
                            if (line[0] == p1) //.Position.subtract(p1.Position).norm2().IsNegligible(0.0001))
                            {
                                if (line[1] == p0)
                                {
                                    //This line is the starting line for the branch. 
                                    continue;
                                }
                                branch.Add(line[1]);
                                hitEndOfBranch = false;
                                lines.RemoveAt(j);
                                break;
                            }
                            if (line[1] == p1) // .Position.subtract(p1.Position).norm2().IsNegligible(0.0001))
                            {
                                if (line[0] == p0)
                                {
                                    //This line is the starting line for the branch. 
                                    continue;
                                }
                                branch.Add(line[0]);
                                hitEndOfBranch = false;
                                lines.RemoveAt(j);
                                break;
                            }
                        }
                    }
                    allBranches.Add(branch.Select(p => p.Light).ToList());
                }

            }
            return allBranches;
        }

    }
}
