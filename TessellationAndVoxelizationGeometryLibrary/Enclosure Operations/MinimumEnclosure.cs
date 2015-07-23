// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-15-2015
// ***********************************************************************
// <copyright file="MinimumBoundingBox.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using StarMathLib;
using TVGL.Enclosure_Operations;


namespace TVGL
{
    /// <summary>
    /// The MinimumEnclosure class includes static functions for defining smallest enclosures for a 
    /// tesselated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {

        /// <summary>
        /// The maximum delta angle
        /// </summary>
        private const double MaxDeltaAngle = Math.PI / 36.0;

        /// <summary>
        ///  Finds the minimum bounding box oriented along a particular direction.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(TessellatedSolid ts)
        {
            return Find_via_PCA_Approach(ts);
        }

        /// <summary>
        /// Finds the minimum bounding rectangle given a set of points. Either send any set of points
        /// OR the convex hull 2D. 
        /// </summary>
        /// <param name="points"></param>
        /// <param name="pointsAreConvexHull"></param>
        /// <returns></returns>
        public static BoundingRectangle BoundingRectangle(IList<Point> points, bool pointsAreConvexHull = false)
        {
            return RotatingCalipers2DMethod(points, pointsAreConvexHull);
        }

        private static BoundingBox Find_via_ChanTan_AABB_Approach(TessellatedSolid ts)
        {
            //Find OBB along x axis <1,0,0>.
            var direction1 = new [] {1.0, 0.0, 0.0};
            var previousObb = FindOBBAlongDirection(ts.ConvexHullVertices, direction1);
            var continueBool = true;
            //While improvement is being made,
            while (continueBool)
            {
                continueBool = false;
                //Find new OBB along OBB.direction2 and OBB.direction3, keeping the best OBB.
                for (var i = 1; i < 3; i++)
                {
                    var newObb = FindOBBAlongDirection(ts.ConvexHullVertices, previousObb.Directions[i]);
                    if (newObb.Volume < previousObb.Volume)
                    {
                        previousObb = newObb;
                        continueBool = true;
                    }
                }
            }
            return previousObb;
        }

        /// <summary>
        /// Finds the minimum bounding box using a direct approach called PCA.
        /// Variants include All-PCA, Min-PCA, Max-PCA, and continuous PCA [http://dl.acm.org/citation.cfm?id=2019641]
        /// The one implemented looks at Min-PCA, Max-PCA and Mid-PCA (considers all three of the eigen vectors).
        /// The most accurate is continuous PCA, and Dimitrov 2009 has some improvements
        /// Dimitrov, Holst, and Kriegel. "Closed-Form Solutions for Continuous PCA and Bounding Box Algorithms"
        /// http://link.springer.com/chapter/10.1007%2F978-3-642-10226-4_3
        /// Simple implementation (2/5)
        /// </summary>
        /// <timeDomain>
        /// O(nlog(n)) time
        /// </timeDomain>
        /// <accuracy>
        /// Generally fairly accurate, but suboptimal solutions. 
        /// Particular cases can yeild very poor results.
        /// Ex. Dimitrov showed in 2009 that continuous PCA yeilds a volume 4x optimal for a octahedron
        /// http://page.mi.fu-berlin.de/rote/Papers/pdf/Bounds+on+the+quality+of+the+PCA+bounding+boxes.pdf
        /// </accuracy>
        private static BoundingBox Find_via_PCA_Approach(TessellatedSolid ts)
        {
            //Find a continuous set of 3 dimensional vextors with constant density
            var triangles = new List<PolygonalFace>(ts.ConvexHullFaces);

            var totalArea = 0.0;
            var minVolume = double.PositiveInfinity;
            //Set the area for each triangle and its center vertex 
            //Also, aggregate to get the surface area of the convex hull
            foreach (var triangle in triangles)
            {
                var vector1 = triangle.Vertices[0].Position.subtract(triangle.Vertices[1].Position);
                var vector2 = triangle.Vertices[0].Position.subtract(triangle.Vertices[2].Position);
                var cross = vector1.crossProduct(vector2);
                triangle.Area = 0.5 * (Math.Sqrt(cross[0] * cross[0] + cross[1] * cross[1] + cross[2] * cross[2]));
                totalArea = totalArea + triangle.Area;
                var xAve = (triangle.Vertices[0].X + triangle.Vertices[1].X + triangle.Vertices[2].X) / 3;
                var yAve = (triangle.Vertices[0].Y + triangle.Vertices[1].Y + triangle.Vertices[2].Y) / 3;
                var zAve = (triangle.Vertices[0].Z + triangle.Vertices[1].Z + triangle.Vertices[2].Z) / 3;
                triangle.Center = new[] { xAve, yAve, zAve };
            }

            //Calculate the center of gravity of combined triangles
            var c = new [] { 0.0, 0.0, 0.0 };
            foreach (var triangle in triangles)
            {
                //Find the triangle weight based proportional to area
                var w = triangle.Area / totalArea;
                //Find the center of gravity
                c = c.add(triangle.Center.multiply(w));
            }

            //Find the covariance matrix  of the convex hull
            var covariance = new double[3, 3];
            foreach (var triangle in triangles)
            {
                var covarianceI = new[,] { { 0.0, 0.0, 0.0 }, { 0.0, 0.0, 0.0 }, { 0.0, 0.0, 0.0 } };
                for (var j = 0; j < 3; j++)
                {
                    var jTerm1 = new double[3, 3];
                    var jTerm1Total = new double[3, 3];
                    var vector1 = triangle.Vertices[j].Position.subtract(c);
                    var term1 = new[,] { { vector1[0], vector1[1], vector1[2] } };
                    var term3 = term1;
                    var term4 = new[,] { { vector1[0] }, { vector1[1] }, { vector1[2] } };
                    for (var k = 0; k < 3; k++)
                    {
                        var vector2 = triangle.Vertices[k].Position.subtract(c);
                        var term2 = new[,] { { vector2[0] }, { vector2[1] }, { vector2[2] } };
                        jTerm1 = term2.multiply(term1);
                        jTerm1Total = jTerm1Total.add(jTerm1);
                    }
                    var jTerm2 = term4.multiply(term3);
                    var jTermTotal = jTerm1.add(jTerm2);
                    covarianceI = covarianceI.add(jTermTotal);
                }
                covariance = covariance.add(covarianceI.multiply(1.0 / 12.0));
            }

            //Find eigenvalues of covariance matrix
            double[][] eigenVectors;
            covariance.GetEigenValuesAndVectors(out eigenVectors);

            var bestOBB = new BoundingBox();
            //Perform a 2D caliper along each eigenvector. 
            foreach (var eigenVector in eigenVectors)
            {
                var OBB = FindOBBAlongDirection(ts.ConvexHullVertices, eigenVector.normalize());
                if (OBB.Volume < minVolume)
                {
                    minVolume = OBB.Volume;
                    bestOBB = OBB;
                }
            }
            return bestOBB;
        }

        /// <summary>
        /// Finds the minimum bounding box using a brute force method based on the 2D Caliper Approach 
        /// Based on: O'Rourke. "Finding Minimal Enclosing Boxes." 1985.
        /// http://cs.smith.edu/~orourke/Papers/MinVolBox.pdf
        /// Difficult implementation (5/5)
        /// </summary>
        /// <timeDomain>
        /// (n^3) time
        /// </timeDomain>
        /// <accuracy>
        /// Gaurantees and optimal solution.
        /// </accuracy>
        private static BoundingBox Find_via_ORourke_Approach(TessellatedSolid ts)
        {
            //todo: Create a Gausian sphere from the vertices and faces in the convexHull
            //for each face normal, create a vertex on the unit sphere 
            //for every edge, create an arc connecting the two faces adjacent to the edge

            //for all pairs of edges e1 and e2: completed in O(n^2) time
            var edges = new List<Edge>();
            var minVolume = double.PositiveInfinity;
            for (var i = 0; i < edges.Count - 1; i++)
            {
                for (var j = i + 1; j < edges.Count; j++)
                {
                    var edge1 = edges[i];
                    var edge2 = edges[j];

                    //Find the three normals defined by the two edges being flush with two adjacent faces
                    //Note that the three normals are dependent on one another based on lemma #3 in O'Rourke
                    //Now use the Gaussian sphere to pick values for n1. (Difficult)
                    //todo: While....
                    double theta = 0.0; //todo: Find theta?
                    double phi = 0.0;   //Find phi?
                    var e1 = new[] { 0.0, 0.0, 1.0 };
                    var e2 = new[] { 0.0, Math.Cos(phi), Math.Sin(phi) };
                    var normal1 = new[] { Math.Cos(theta), Math.Sin(theta), 0 };
                    var R = Math.Sqrt(Math.Pow(Math.Cos(theta), 2) +
                                      Math.Pow(Math.Sin(theta), 2) *
                                      Math.Pow(Math.Sin(phi), 2));
                    var x2 = -Math.Sign(Math.Sin(phi)) * Math.Sin(phi) * Math.Sin(theta) * Math.Sin(phi) / R;
                    var y2 = Math.Sign(Math.Sin(phi)) * Math.Sin(phi) * Math.Cos(theta) * Math.Sin(phi) / R;
                    var z2 = -Math.Cos(theta) * Math.Cos(phi) / R;
                    var normal2 = new[] { x2, y2, z2 };
                    var normal3 = normal1.crossProduct(normal2);
                    var normals = new double[][] { normal1, normal2, normal3 };


                    //actually, only do this portion once.
                    //The gaussian sphere determines the new contacts
                    //Now search along the normals to find the furthest point and get the minimum volume.
                    var lengths = new List<double>();
                    foreach (var normal in normals)
                    {
                        Vertex vLow;
                        Vertex vHigh;
                        var length = GetLengthAndExtremeVertices(normal, ts.ConvexHullVertices, out vLow, out vHigh);
                        lengths.Add(length);
                    }
                    var volume = lengths[0] * lengths[1] * lengths[2];
                    if (volume < minVolume)
                    {
                        minVolume = volume;
                    }

                    //End while
                }
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finds the minimum bounding box using an iterative optimization based on the genetic algorithm and Nelder-Mead algorithm
        /// Based on: CHANG, GORISSEN, and MELCHIOR. "Fast Oriented Bounding Box Optimization on the Rotation Group SO(3, R)." Oct 2011.
        /// http://dl.acm.org/citation.cfm?id=2019641
        /// Difficult implementation (4/5)
        /// </summary>
        /// <timeDomain>
        /// Much faster than O'Rourke's, more accurate than hueristic based (PCA).
        /// Near linear in practice
        /// </timeDomain>
        /// <accuracy>
        /// Often exact, but some small error may be present.
        /// </accuracy>
        private static BoundingBox Find_via_HYBBRID_Approach(TessellatedSolid ts)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The MC_ApproachOne is a brute force method which includes the flush face algorithm, 
        /// but limits the maximum angle of rotation between faces. 
        /// In this way, it gaurantees a much more optimal solution than the O(n^2) time algorithm.
        /// </summary>
        /// <timeDomain>
        /// Since the computation cost for each Bounding Box is linear O(n),
        /// and the approximate worse case number of normals considered is n*PI/maxDeltaAngle,
        /// Lower Bound O(n^2). Upper Bound O(n^(2)*PI/maxDeltaAngle). [ex.  upper bound is O(36*n^2) when MaxDeltaAngle = 5 degrees.]
        /// </timeDomain>
        /// <accuracy>
        /// Garantees the optimial orientation is within MaxDeltaAngle error.
        /// </accuracy>
        private static BoundingBox Find_via_MC_ApproachOne(TessellatedSolid ts)
        {
            BoundingBox minBox = new BoundingBox();
            var minVolume = double.PositiveInfinity;
            foreach (var convexHullEdge in ts.ConvexHullEdges)
            {
                var rotAxis = convexHullEdge.Vector.normalize();
                var n = convexHullEdge.OwnedFace.Normal;
                var numSamples = (int)Math.Ceiling((Math.PI - convexHullEdge.InternalAngle) / MaxDeltaAngle);
                var deltaAngle = (Math.PI - convexHullEdge.InternalAngle) / numSamples;
                double[] direction;
                for (var i = 0; i < numSamples; i++)
                {
                    if (i == 0) direction = n;
                    else
                    {
                        var angleChange = i * deltaAngle;
                        var invCrossMatrix = new[,]
                        {
                            {n[0]*n[0], n[0]*n[1], n[0]*n[2]},
                            {n[1]*n[0], n[1]*n[1], n[1]*n[2]},
                            {n[2]*n[0], n[2]*n[1], n[2]*n[2]}
                        };
                        direction = invCrossMatrix.multiply(rotAxis.multiply(Math.Sin(angleChange))).normalize();
                    }
                    if (double.IsNaN(direction[0])) continue;
                    //todo: figure out why direction is NaN
                    var obb = FindOBBAlongDirection(ts.ConvexHullVertices, direction.normalize());
                    if (obb.Volume < minVolume)
                    {
                        minBox = obb;
                        minVolume = minBox.Volume;
                    }
                }
            }
            return minBox;
        }


        private static BoundingBox Find_via_BM_ApproachOne(TessellatedSolid ts)
        {
            var gaussianSphere = new GaussianSphere(ts);
            var minBox = new BoundingBox();
            var minVolume = double.PositiveInfinity;
            foreach (var arc in gaussianSphere.Arcs)
            {
                //Create great circle one (GC1) along direction of arc.
                //Intersections on this great circle will determine changes in length.
                var greatCircle1 = new GreatCircleAlongArc(gaussianSphere, arc.Nodes[0].Vector, arc.Nodes[1].Vector,arc);

                //Create great circle two (GC2) orthoganal to the current normal (n1).
                //GC2 contains an arc list of all the arcs it intersects
                //The intersections and perspective determine the cross sectional area.
                var vector1 = arc.Nodes[0].Vector.crossProduct(arc.Nodes[1].Vector);
                //vector 2 is + 90 degrees in the direction of the arc.
                var vector2 = vector1.crossProduct(arc.Nodes[0].Vector);
                var greatCircle2 = new GreatCircleOrthogonalToArc(gaussianSphere, vector1, vector2, arc);
                
                //List intersections from GC1 and nodes from GC2 based on angle of rotation to each.
                var delta = 0.0;
                var totalChange = 0.0;
                var maxChange = arc.ArcLength;
                var intersections = new List<Intersection>();
                do
                {
                    var boundingBox = FindOBBAlongDirection(greatCircle2.ReferenceVertices, greatCircle2.Normal);
                    if (boundingBox.Volume < minVolume) minVolume = boundingBox.Volume;

                    //Set delta, where delta is the angle from the original orientation to the next intersection
                    delta =+ delta;//+ intersections[0].angle

                    while (totalChange <= delta) //Optimize the volume based on changes is projection of the 2D Bounding Box.;
                    {
                        //If the 2D bounding box changes vertices with the projection, then we will need RotatingCalipers
                        //Else, project the same four'ish vertices and recalculate area.  
                        if (boundingBox.Volume < minVolume) minVolume = boundingBox.Volume;
                    }

                    //After delta is reached, update the volume parameters of either length or cross sectional area (convex hull points).
                    //If the closest intersection is on GC1, recalculate length.
                    if (greatCircle1.Intersections[0].SphericalDistance < greatCircle2.Intersections[0].SphericalDistance)
                    {
                        var vector = arc.Nodes[0].Vector.subtract(intersections[0].Node.Vector);
                    }
                    else
                    {
                        var node = intersections[0].Node;
                        foreach (var intersectedArc in node.Arcs)
                        {
                            if (greatCircle2.ArcList.Contains(intersectedArc))
                                greatCircle2.ArcList.Remove(intersectedArc);
                            else greatCircle2.ArcList.Add(intersectedArc);
                        }
                    }
                } while (delta <= maxChange);
            }
            return minBox;
        }

        /// <summary>
        /// Finds the minimum oriented bounding rectangle (2D). The 3D points of a tessellated solid
        /// are projected to the plane defined by "direction". This returns a BoundingBox structure
        /// where the first direction is the same as the prescribed direction and the other two are
        /// in-plane unit vectors.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>BoundingBox.</returns>
        /// <exception cref="System.Exception"></exception>
        public static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction)
        {
            Vertex v1Low, v1High;
            var length = GetLengthAndExtremeVertices(direction, vertices, out v1Low, out v1High);
            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(vertices, direction, out backTransform, true);
            var boundingRectangle = RotatingCalipers2DMethod(points);
            //Get reference vertices from boundingRectangle
            var v2Low = boundingRectangle.PointPairs[0][0].References[0];
            var v2High = boundingRectangle.PointPairs[0][1].References[0];
            var v3Low = boundingRectangle.PointPairs[1][0].References[0];
            var v3High = boundingRectangle.PointPairs[1][1].References[0];

            //Get the direction vectors from rotating caliper and projection.
            var tempDirection = new []
            {
                boundingRectangle.Directions[0][0], boundingRectangle.Directions[0][1], 
                boundingRectangle.Directions[0][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction2 = new [] {tempDirection[0], tempDirection[1], tempDirection[2]};
            tempDirection = new []
            {
                boundingRectangle.Directions[1][0], boundingRectangle.Directions[1][1], 
                boundingRectangle.Directions[1][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction3 = new[] { tempDirection[0], tempDirection[1], tempDirection[2] };
            var direction1 = direction2.crossProduct(direction3);
            length = GetLengthAndExtremeVertices(direction1, vertices, out v1Low, out v1High);
            //todo: Fix Get2DProjectionPoints, which seems to be transforming the points to 2D, but not normal to
            //the given direction vector. If it was normal, direction1 should equal direction or its direction.inverse.

            return new BoundingBox(length * boundingRectangle.Area, new[] { v1Low, v1High, v2Low, v2High, v3Low, v3High},
                new[] { direction1, direction2, direction3});
        }


        /// <summary>
        /// Given a direction, dir, this function returns the maximum length along this direction
        /// for the provided vertices as well as the two vertices that represent the extremes.
        /// </summary>
        /// <param name="dir">The dir.</param>
        /// <param name="vertices">The vertices.</param>
        /// <param name="vLow">The v low.</param>
        /// <param name="vHigh">The v high.</param>
        /// <returns>System.Double.</returns>
        public static double GetLengthAndExtremeVertices(double[] direction, IList<Vertex> vertices, out Vertex vLow, out Vertex vHigh)
        {
            var dir = direction.normalize();
            var dotProducts = new double[vertices.Count];
            var i = 0;
            foreach (var v in vertices)
                dotProducts[i++] = dir.dotProduct(v.Position);
            var min_d = dotProducts.Min();
            var max_d = dotProducts.Max();
            vLow = vertices[dotProducts.FindIndex(min_d)];
            vHigh = vertices[dotProducts.FindIndex(max_d)];
            return max_d - min_d;
        }

        /// <summary>
        /// Determines if a point is inside a polyhedron (tesselated solid).
        /// And the polygon is not self-intersecting
        /// http://www.cescg.org/CESCG-2012/papers/Horvat-Ray-casting_point-in-polyhedron_test.pdf
        /// </summary>
        /// <returns></returns>
        public static bool IsVertexInsidePolyhedron(TessellatedSolid ts, Vertex vertexInQuestion, bool onBoundaryIsInside = true)
        {
            var numberOfIntercepts = 0;
            var direction = new[] {0.0, 0.0, 1.0};
            foreach (var face in ts.Faces) 
            {
                var distanceToOrigin = face.Normal.dotProduct(face.Vertices[0].Position);
                var t = -(vertexInQuestion.Position.dotProduct(face.Normal) + distanceToOrigin) /
                        (direction.dotProduct(face.Normal));
                if (t < 0) continue;
                //ToDo: figure out boundary conditions
                //Note that if t == 0, then it is on the face, which is considered inside for this method
                //else, find the intersection point and determine if it is inside the polygon (face)
                var newVertex = new Vertex(vertexInQuestion.Position.add(direction.multiply(t)));
                var points = MiscFunctions.Get2DProjectionPoints(face.Vertices.ToArray(), face.Normal);
                var pointInQuestion = MiscFunctions.Get2DProjectionPoints(new List<Vertex> { newVertex }, face.Normal);
                if (IsPointInsidePolygon(points.ToList(), pointInQuestion[0], true)) numberOfIntercepts++;
            }
            if (numberOfIntercepts == 0) return false;
            return numberOfIntercepts%2 == 0; //Even number of intercepts, means the vertex is inside
        }


        /// <summary>
        /// Determines if a point is inside a polygon, where a polygon is an ordered list of 2D points.
        /// And the polygon is not self-intersecting
        /// </summary>
        /// <returns></returns>
        public static bool IsPointInsidePolygon(List<Point> points, Point pointInQuestion, bool onBoundaryIsInside = true)
        {
            //If the point in question is == a point in points, then it is inside the polygon
            if (points.Any(point => point.X.IsPracticallySame(pointInQuestion.X) && point.Y.IsPracticallySame(pointInQuestion.Y)))
            {
                return true;
            }
            //Create nodes and add them to a list
            var nodes = points.Select(point => new Node(point, 0, 0)).ToList();
            
            //Add first line to list and update nodes with information
            var lines = new List<Line>();
            var line = new Line(nodes.Last(),nodes[0]);
            lines.Add(line);
            nodes.Last().StartLine = line;
            nodes[0].EndLine = line;
            //Create all other lines and add them to the list
            for (var i = 1; i < points.Count; i++)
            {
                line = new Line(nodes[i-1], nodes[i]);
                lines.Add(line);
                nodes[i - 1].StartLine = line;
                nodes[i].EndLine = line;
            }

            //sort points by descending y, then descending x
            nodes.Add(new Node(pointInQuestion,0,0));
            var sortedNodes = nodes.OrderByDescending(node => node.Y).ThenByDescending(node => node.X).ToList<Node>();
            var lineList = new List<Line>();

            //Use red-black tree sweep to determine which lines should be tested for intersection
            foreach (var node in sortedNodes)
            {
                //Add to or remove from Red-Black Tree
                //If reached the point in question, then find intercepts on the lineList
                if (node.StartLine == null)
                {
                    if (lineList.Count % 2 != 0 || lineList.Count < 1) return false; 
                    Line leftLine, rightLine;
                    //Check if the point is on the left line or right line (note that one direction search is sufficient).
                    if (TriangulatePolygon.LinesToLeft(node, lineList, out leftLine, false) !=
                        TriangulatePolygon.LinesToLeft(node, lineList, out leftLine, true)) return onBoundaryIsInside;
                    //Else, not on a boundary, so check to see that it is in between an odd number of lines to left and right
                    if (TriangulatePolygon.LinesToLeft(node, lineList, out leftLine, false) %2 == 0) return false;
                    return TriangulatePolygon.LinesToRight(node, lineList, out rightLine, false) % 2 != 0;  
                }
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
            //If not returned, throw error
            throw new System.ArgumentException("Failed to return intercept information");
        }

        /// <summary>
        /// Rotating the calipers2 d method.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="pointsAreConvexHull"></param>
        /// <returns>System.Double.</returns>
        private static BoundingRectangle RotatingCalipers2DMethod(IList<Point> points, bool pointsAreConvexHull = false)
        {

            #region Initialization
            var cvxPoints = pointsAreConvexHull ? points : ConvexHull2D(points);
            var numCvxPoints = cvxPoints.Count;
            var extremeIndices = new int[4];

            extremeIndices[3] = cvxPoints.Count;

            //        extremeIndices[3] => max-Y
            extremeIndices[3] = cvxPoints.Count - 1;
            while (extremeIndices[3] >= 1 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1])
                extremeIndices[3]--;

            //        extremeIndices[2] => max-X
            extremeIndices[2] = extremeIndices[3];
            while (extremeIndices[2] >= 1 && cvxPoints[extremeIndices[2]][0] <= cvxPoints[extremeIndices[2] - 1][0])
                extremeIndices[2]--;


            //        extremeIndices[1] => min-Y
            extremeIndices[1] = extremeIndices[2];
            while (extremeIndices[1] >= 1 && cvxPoints[extremeIndices[1]][1] >= cvxPoints[extremeIndices[1] - 1][1])
                extremeIndices[1]--;

            //        extremeIndices[0] => min-X 
            // A bit more complicated, since it needs to look past the zero index.
            var currentIndex = -1;
            var previousIndex = -1;
            extremeIndices[0] = extremeIndices[1];
            do
            {
                currentIndex = extremeIndices[0];
                extremeIndices[0]--;
                if (extremeIndices[0] < 0) { extremeIndices[0] = numCvxPoints - 1; }
                previousIndex = extremeIndices[0];
            } while (cvxPoints[currentIndex][0] >= cvxPoints[previousIndex][0]);
            extremeIndices[0]++;
            if (extremeIndices[0] > numCvxPoints - 1) { extremeIndices[0] = 0; }

            #endregion

            #region Cycle through 90-degrees
            var angle = 0.0;
            var bestAngle = double.NegativeInfinity;
            var direction1 = new double[3];
            var direction2 = new double[3];
            var deltaToUpdateIndex = -1;
            var deltaAngles = new double[4];
            var offsetAngles = new[] { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };
            Point[] pointPair1 = null;
            Point[] pointPair2 = null;
            var minArea = double.PositiveInfinity;
            do
            {
                //For each of the 4 supporting points (those forming the rectangle),
                #region update the deltaAngles from the current orientation
                for (var i = 0; i < 4; i++)
                {
                    //Update all angles on first pass. For each additional pass, only update one deltaAngle.
                    if (deltaToUpdateIndex == -1 || i == deltaToUpdateIndex)
                    {
                        var index = extremeIndices[i];
                        var prev = (index == 0) ? numCvxPoints - 1 : index - 1;
                        var tempDelta = Math.Atan2(cvxPoints[prev][1] - cvxPoints[index][1],
                             cvxPoints[prev][0] - cvxPoints[index][0]);
                        deltaAngles[i] = offsetAngles[i] - tempDelta;
                        //If the angle has rotated beyond the 90 degree bounds, it will be negative
                        //And should never be chosen from then on.
                        if (deltaAngles[i] < 0) { deltaAngles[i] = 2 * Math.PI; }
                    }
                }
                var delta = deltaAngles.Min();
                deltaToUpdateIndex = deltaAngles.FindIndex(delta);
                #endregion
                var currentPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];
                extremeIndices[deltaToUpdateIndex]--;
                if (extremeIndices[deltaToUpdateIndex] < 0) { extremeIndices[deltaToUpdateIndex] = numCvxPoints - 1; }
                var previousPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];
                angle = delta;
                #region find area
                //Get unit normal for current edge
                var direction = previousPoint.Position2D.subtract(currentPoint.Position2D).normalize();
                //If point type = 1 or 3, then use inversed direction
                if (deltaToUpdateIndex == 1 || deltaToUpdateIndex == 3) { direction = new[] { -direction[1], direction[0] }; }
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[2]][0] - cvxPoints[extremeIndices[0]][0],
                    cvxPoints[extremeIndices[2]][1] - cvxPoints[extremeIndices[0]][1]
                };

                var angleVector1 = new[] { -direction[1], direction[0] };
                var width = Math.Abs(vectorWidth.dotProduct(angleVector1));
                var vectorHeight = new[]
                { 
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0], 
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                var angleVector2 = new[] { direction[0], direction[1] };
                var height = Math.Abs(vectorHeight.dotProduct(angleVector2));
                var tempArea = height * width;
                #endregion
                if (minArea > tempArea)
                {
                    minArea = tempArea;
                    bestAngle = angle;
                    pointPair1 = new [] { cvxPoints[extremeIndices[2]], cvxPoints[extremeIndices[0]]};
                    pointPair2 = new [] { cvxPoints[extremeIndices[3]], cvxPoints[extremeIndices[1]] };
                    direction1 = new [] { angleVector1[0], angleVector1[1], 0.0};
                    direction2 = new [] { angleVector2[0], angleVector2[1], 0.0 };
                }
            } while (angle < Math.PI / 2); //Don't check beyond a 90 degree angle.
            //If best angle is 90 degrees, then don't bother to rotate. 
            if (bestAngle.IsPracticallySame(Math.PI / 2)) { bestAngle = 0.0; }
            #endregion

            var directions = new List<double[]>{direction1, direction2};
            var extremePoints = new List<Point[]> {pointPair1, pointPair2};
            var boundingRectangle = new BoundingRectangle(minArea, bestAngle, directions, extremePoints);
            return boundingRectangle;
        }
    }
}