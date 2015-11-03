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
using System.Diagnostics;


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
        private const double MaxDeltaAngle = Math.PI / 180.0;

        /// <summary>
        ///  Finds the minimum bounding box oriented along a particular direction.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox_Test(TessellatedSolid ts, out List<List<double[]>> volumeData1)//, out List<List<double[]>> volumeData2)
        {
            volumeData1 = null;
            //var flats = ListFunctions.Flats(ts.Faces.ToList());
            var now = DateTime.Now;
            Debug.WriteLine("Beginning OBB Test");
            var boundingBox1 = OrientedBoundingBox(ts);
            Debug.WriteLine("Time Elapsed for PCA Approach = " + (DateTime.Now - now));
            Debug.WriteLine("Volume for PCA Approach= " + boundingBox1.Volume);
            now = DateTime.Now;
            var boundingBox2 = Find_via_ChanTan_AABB_Approach(ts);
            Debug.WriteLine("Time Elapsed for ChanTan Approach = " + (DateTime.Now - now));
            Debug.WriteLine("Volume for ChanTan Approach = " + boundingBox2.Volume);
            //now = DateTime.Now;
            //Debug.WriteLine("Beginning OBB Test");
            //var boundingBox1 = Find_via_MC_ApproachOne(ts, out volumeData1);
            //Debug.WriteLine("Time Elapsed for MC Approach One = " + (DateTime.Now - now));
            //now = DateTime.Now;
            //var boundingBox2 = Find_via_BM_ApproachTwo(ts, out volumeData2);
            //Debug.WriteLine("Time Elapsed for BM Approach Two = " + (DateTime.Now - now));

            return boundingBox2;
        }

        /// <summary>
        /// Finds the minimum bounding box.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(TessellatedSolid ts)
        {
            return Find_via_PCA_Approach(ts.ConvexHullVertices, ts.ConvexHullFaces);
        }

        /// <summary>
        /// Finds the minimum bounding box.
        /// </summary>
        /// <param name="convexHullVertices">The convex hull vertices.</param>
        /// <param name="convexHullFaces">The convex hull faces.</param>
        /// <returns>BoundingBox.</returns>
        public static BoundingBox OrientedBoundingBox(IList<Vertex> convexHullVertices, IEnumerable<PolygonalFace> convexHullFaces)
        {
            return Find_via_PCA_Approach(convexHullVertices, convexHullFaces);
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

        #region ChanTan AABB Approach
        private static BoundingBox Find_via_ChanTan_AABB_Approach(TessellatedSolid ts)
        {
            var minOBB = new BoundingBox()
            {
                Volume = double.PositiveInfinity,
                Directions = new[] { new[] { 1.0, 0.0, 0.0 }, new[] { 0.0, 1.0, 0.0 }, new[] { 0.0, 0.0, 1.0 } }
            };
            bool improvementFound;
            do
            {
                improvementFound = false;
                //Find new OBB along OBB.direction2 and OBB.direction3, keeping the best OBB.
                for (var i = 1; i < 3; i++)
                {
                    var newObb = FindOBBAlongDirection(ts.ConvexHullVertices, minOBB.Directions[i]);
                    if (newObb.Volume >= minOBB.Volume) continue;
                    minOBB = newObb;
                    improvementFound = true;
                    break;
                }
            } while (improvementFound);
            return minOBB;
        }
        #endregion

        #region PCA Approach
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
        private static BoundingBox Find_via_PCA_Approach(IList<Vertex> convexHullVertices, IEnumerable<PolygonalFace> convexHullFaces)
        {
            //Find a continuous set of 3 dimensional vextors with constant density
            var triangles = new List<PolygonalFace>(convexHullFaces);
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
            var c = new[] { 0.0, 0.0, 0.0 };
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
                var OBB = FindOBBAlongDirection(convexHullVertices, eigenVector.normalize());
                if (OBB.Volume < minVolume)
                {
                    minVolume = OBB.Volume;
                    bestOBB = OBB;
                }
            }
            return bestOBB;
        }
        #endregion

        #region ORourke Approach
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
        #endregion

        #region MC ApproachOne
        /// <summary>
        /// The MC_ApproachOne rotates around each edge of the convex hull between the owned and 
        /// other faces. In this way, it gaurantees a much more optimal solution than the flat
        /// with face algorithm, but is, therefore, slower. 
        /// </summary>
        /// <timeDomain>
        /// Since the computation cost for each Bounding Box is linear O(n),
        /// and the approximate worse case number of normals considered is n*PI/maxDeltaAngle,
        /// Lower Bound O(n^2). Upper Bound O(n^(2)*PI/maxDeltaAngle). [ex.  upper bound is O(36*n^2) when MaxDeltaAngle = 5 degrees.]
        /// </timeDomain>
        /// <accuracy>
        /// Garantees the optimial orientation is within MaxDeltaAngle error.
        /// </accuracy>
        private static BoundingBox Find_via_MC_ApproachOne(TessellatedSolid ts, out List<List<double[]>> volumeData)
        {
            volumeData = new List<List<double[]>>();
            BoundingBox minBox = new BoundingBox();
            var minVolume = double.PositiveInfinity;
            //Get a list of flats
            //var faces = ListFunctions.FacesWithDistinctNormals(ts.ConvexHullFaces.ToList());
            //var flats = ListFunctions.Flats(ts.ConvexHullFaces.ToList());

            foreach (var convexHullEdge in ts.ConvexHullEdges)
            {
                //Initialize variables
                var seriesData = new List<double[]>();
                var r = convexHullEdge.Vector.normalize();
                var n = convexHullEdge.OwnedFace.Normal;
                var internalAngle = convexHullEdge.InternalAngle;

                //Check for exceptions and special cases.
                //Skip the edge if its internal angle is practically 0 or 180 since.
                if (Math.Abs(internalAngle - Math.PI) < 0.0001 || Math.Round(internalAngle, 5).IsNegligible()) continue;
                if (convexHullEdge.Curvature == CurvatureType.Concave) throw new Exception("Error in internal angle definition");
                //r cross owned face normal should point along the other face normal.
                if (r.crossProduct(n).dotProduct(convexHullEdge.OtherFace.Normal) < 0) throw new Exception();

                //DEBUG STOP
                var debugDepth1 = false;
                if (volumeData.Count == 68) debugDepth1 = true;

                //Set the sampling parameters
                var numSamples = (int)Math.Ceiling((Math.PI - internalAngle) / MaxDeltaAngle);
                if (numSamples < 20) numSamples = 20; //At minimum, take tewnty samples
                var deltaAngle = (Math.PI - internalAngle) / numSamples;
                if (Math.Round(internalAngle, 5).IsNegligible()) continue;

                #region Initialize Variables
                Vertex vertex1 = null;
                Vertex vertex3 = null;
                Vertex vertex4 = null;
                Vertex vertex5 = null;
                Vertex vertex6 = null;
                Vertex edgeVertex1 = null;
                Vertex edgeVertex2 = null;
                var vertexList = new List<Vertex>();
                var extremeDepth = 0.0;
                double[] depthOrtho = null;
                double[] direction = null;
                double[] rotatingCaliperEdgeVector = null;
                //var tolerance = 0.0001;
                #endregion

                for (var i = 0; i < numSamples + 1; i++)
                {
                    var angleChange = 0.0;
                    if (i == 0) direction = n;
                    else
                    {
                        if (i == numSamples) angleChange = Math.PI - internalAngle;
                        else angleChange = i * deltaAngle;
                        var s = Math.Sin(angleChange);
                        var c = Math.Cos(angleChange);
                        var t = 1.0 - c;
                        //Source http://math.kennesaw.edu/~plaval/math4490/rotgen.pdf
                        var rotMatrix = new[,]
                        {
                            {t*r[0]*r[0]+c, t*r[0]*r[1]-s*r[2], t*r[0]*r[2]+s*r[1]},
                            {t*r[0]*r[1]+s*r[2], t*r[1]*r[1]+c, t*r[1]*r[2]-s*r[0]},
                            {t*r[0]*r[2]-s*r[1], t*r[1]*r[2]+s*r[0], t*r[2]*r[2]+c}
                        };
                        direction = rotMatrix.multiply(n).normalize();
                    }
                    if (double.IsNaN(direction[0])) throw new Exception();

                    if (debugDepth1 && seriesData.Count == 15) debugDepth1 = true;
                    var obb = FindOBBAlongDirection(ts.ConvexHullVertices, direction);


                    #region Test for computing OBB volume when vertices are constant
                    if (obb.ExtremeVertices[1] == vertex1)
                    {
                        //Check if length function is correct
                        double depth = -extremeDepth * depthOrtho.dotProduct(direction);
                        //if (Math.Abs(depth - obb.Depth) > tolerance) throw new Exception("equation incorrect");
                    }
                    else //Update the vertices and find the smallest vector from the edge to v1High
                    {
                        vertex1 = obb.ExtremeVertices[1];
                        double[] pointOnLine;
                        extremeDepth = MiscFunctions.DistancePointToLine(vertex1.Position, convexHullEdge.From.Position, convexHullEdge.Vector, out pointOnLine);
                        depthOrtho = vertex1.Position.subtract(pointOnLine).normalize();
                    }

                    if (obb.ExtremeVertices[2] == vertex3 && obb.ExtremeVertices[3] == vertex4 &&
                        obb.ExtremeVertices[4] == vertex5 && obb.ExtremeVertices[5] == vertex6 &&
                        obb.EdgeVertices[0] == edgeVertex1 && obb.EdgeVertices[1] == edgeVertex2)
                    {
                        //Calculate new area, given that the edge from rotating calipers and all the extreme vertices are the same
                        var direction1 = rotatingCaliperEdgeVector.crossProduct(direction);
                        var direction2 = direction1.crossProduct(direction);
                        Vertex vLow, vHigh;
                        var length = GetLengthAndExtremeVertices(direction1, vertexList, out vLow, out vHigh);
                        var width = GetLengthAndExtremeVertices(direction2, vertexList, out vLow, out vHigh);
                        var area = length * width;
                        //if (Math.Abs(area - obb.Area) > tolerance) throw new Exception("equation incorrect");
                    }
                    else //Update the vertices
                    {
                        vertex3 = obb.ExtremeVertices[2];
                        vertex4 = obb.ExtremeVertices[3];
                        vertex5 = obb.ExtremeVertices[4];
                        vertex6 = obb.ExtremeVertices[5];
                        edgeVertex1 = obb.EdgeVertices[0];
                        edgeVertex2 = obb.EdgeVertices[1];
                        vertexList = new List<Vertex> { vertex3, vertex4, vertex5, vertex6 };
                        rotatingCaliperEdgeVector = obb.EdgeVector.normalize();
                    }
                    #endregion

                    var dataPoint = new double[] { angleChange, obb.Volume };
                    seriesData.Add(dataPoint);
                    if (obb.Volume < minVolume)
                    {
                        minBox = obb;
                        minVolume = minBox.Volume;
                    }
                }
                if (numSamples > 0) volumeData.Add(seriesData);
            }
            return minBox;
        }
        #endregion

        #region Find Minimum OBB Between Two Angles via Golden Sections
        /// <summary>
        /// This golden section search finds the minimum between two angles, with the assumption that only one minimum exists, 
        /// and that both angle (data points) decrease in volume as they near the minimum (negative slopes).
        /// </summary>
        private static BoundingBox FindMinimumBetweenTwoAngles_GoldenSections(IList<Vertex> convexHullVertices, double minAngle,
            double maxAngle, double[] faceNormal, double[] axisOfRotation, out double[] dataPoint)
        {
            dataPoint = new double[2];
            var goldenRatio = (Math.Sqrt(5) - 1) / 2;
            var tolerance = 1e-6;
            //Set x values
            var a = minAngle;
            var b = maxAngle;
            var c = b - goldenRatio * (b - a);
            var d = a + goldenRatio * (b - a);

            //Perform golden section search
            while (Math.Abs(c - d) > tolerance)
            {
                var fc = OBBAlongDirectionFromAngle(faceNormal, axisOfRotation, c, convexHullVertices).Volume;
                var fd = OBBAlongDirectionFromAngle(faceNormal, axisOfRotation, d, convexHullVertices).Volume;
                if (fc < fd)
                {
                    b = d;
                    d = c;
                    c = b - goldenRatio * (b - a);
                }
                else
                {
                    a = c;
                    c = d;
                    d = a + goldenRatio * (b - a);
                }
            }
            var minObb = OBBAlongDirectionFromAngle(faceNormal, axisOfRotation, (b + a) / 2, convexHullVertices);
            dataPoint[0] = (b + a) / 2;
            dataPoint[1] = minObb.Volume;
            return minObb;
        }
        #endregion

        #region Find OBB Along a Direction from given angle change
        private static BoundingBox OBBAlongDirectionFromAngle(double[] faceNormal, double[] axisOfRotation, double angleChange, IList<Vertex> convexHullVertices)
        {
            var s = Math.Sin(angleChange);
            var c = Math.Cos(angleChange);
            var t = 1.0 - c;
            var n = faceNormal;
            var r = axisOfRotation;
            //Source http://math.kennesaw.edu/~plaval/math4490/rotgen.pdf
            var rotMatrix = new[,]
                            {
                                {t*r[0]*r[0]+c, t*r[0]*r[1]-s*r[2], t*r[0]*r[2]+s*r[1]},
                                {t*r[0]*r[1]+s*r[2], t*r[1]*r[1]+c, t*r[1]*r[2]-s*r[0]},
                                {t*r[0]*r[2]-s*r[1], t*r[1]*r[2]+s*r[0], t*r[2]*r[2]+c}
                            };
            var direction = rotMatrix.multiply(n);
            if (double.IsNaN(direction[0])) throw new Exception();

            //Find OBB along direction and save minimum bounding box
            return FindOBBAlongDirection(convexHullVertices, direction.normalize());
        }
        #endregion

        #region BM ApproachTwo
        /// <summary>
        /// The BM_ApproachTwo intelligently finds all the potential changes in vertices of the OBB
        /// when rotated around each edge in the convex hull. This forms a discrete selection of angles
        /// to check between the two face normals that form an edge.
        /// All of these angles are then computed with FindOBBAlongDirection.
        /// </summary>
        /// <timeDomain>
        /// Visiting each edge pair takes O(n^2) time. Then the number of additional linear operations is based on 
        /// how often the vertices change. Worst case, we assume near O(n). Together, with a O(n) convex hull, 
        /// this second portion is then O(n^3) time which dominates the first portion O(n^2),
        /// making the total time approximately O(n^3) in worst case.
        /// The algorithm can be improved to very near O(n^2) if the OBB function is removed, and instead we track the 
        /// 2d convex hull and furthest point from the gaussian sphere.
        /// </timeDomain>
        /// <accuracy>
        /// Garantees the optimial orientation.
        /// </accuracy>
        private static BoundingBox Find_via_BM_ApproachTwo(TessellatedSolid ts, out List<List<double[]>> volumeData)
        {
            volumeData = new List<List<double[]>>();
            var minBox = new BoundingBox();
            var minVolume = double.PositiveInfinity;
            foreach (var convexHullEdge in ts.ConvexHullEdges)
            {
                var n = convexHullEdge.OwnedFace.Normal;
                var internalAngle = convexHullEdge.InternalAngle;
                var seriesData = new List<double[]>();
                var angleList = new List<double>();

                //Check for exceptions and special cases.
                //Skip the edge if its internal angle is practically 0 or 180.
                if (internalAngle.IsPracticallySame(Math.PI, Constants.OBBAngleTolerance) || internalAngle.IsPracticallySame(0.0, Constants.OBBAngleTolerance)) continue;
                if (convexHullEdge.Curvature == CurvatureType.Concave) throw new Exception("Error in internal angle definition");
                //r cross owned face normal should point along the other face normal.
                var r = convexHullEdge.Vector.normalize();
                if (r.crossProduct(n).dotProduct(convexHullEdge.OtherFace.Normal) < 0) throw new Exception();

                //DEBUG STOP
                var debugDepth1 = false;
                if (volumeData.Count == 68) debugDepth1 = true;

                //Find the angle between the two faces that form this edge
                var maxTheta = Math.PI - convexHullEdge.InternalAngle;
                angleList.Add(0.0);
                angleList.Add(maxTheta);

                //Build rotation matrix to align the edge.OwnedFace.Normal along the primary axis.
                var xp = n;
                var zp = convexHullEdge.Vector.normalize();
                var yp = zp.crossProduct(xp).normalize();
                var rotMatrix1 = new[,]
                        {
                            {xp[0], xp[1], xp[2]},
                            {yp[0], yp[1], yp[2]},
                            {zp[0], zp[1], zp[2]}
                        };

                //Determine the sign of maxTheta
                //If the new y value is pointing in the positive y direction, the rotation is positive (CCW around z)
                var n2 = rotMatrix1.multiply(convexHullEdge.OtherFace.Normal);
                var rotationDirection = Math.Sign(n2[1]); //CCW +

                //Debug
                var n1 = rotMatrix1.multiply(n);
                if (!n1[0].IsPracticallySame(1.0)) throw new Exception(); //Should be pointing along x axis
                if (!n2[2].IsNegligible()) throw new Exception(); //Should be in XY plane

                //Find all the changes in visible vertices along rotation from face to face
                //In addition, find whenever the rear extreme vertex changes
                foreach (var otherConvexHullEdge in ts.ConvexHullEdges)
                {
                    //Rotate face normal to a new position on the theoretical gaussian sphere\
                    var positions = new List<double[]>();
                    //if (debugDepth1 && otherConvexHullEdge.EdgeReference == 10860440) positions = new List<double[]>();
                    //Check both owned and other since not all faces are owned (necessarily)
                    positions.Add(rotMatrix1.multiply(otherConvexHullEdge.OtherFace.Normal).normalize());
                    positions.Add(rotMatrix1.multiply(otherConvexHullEdge.OwnedFace.Normal).normalize());

                    #region Get Rotation Angle
                    foreach (var position in positions)
                    {
                        //Find the angle of the new position with respect to the direction of rotation in z
                        var rotAngle = 0.0;
                        if (Math.Abs(position[0]).IsPracticallySame(1)) //Check if on new x axis 
                        {
                            //Regardless of which direction, it is at a change of 90 degrees.
                            rotAngle = Math.PI / 2;
                        }
                        else if (Math.Abs(position[1]).IsPracticallySame(1)) //Check if on new y axis
                        {
                            rotAngle = 0.0; //Don't add angle to list. 
                        }
                        else if (Math.Abs(position[2]).IsPracticallySame(1)) //Check if on new z axis
                        {
                            continue; //Skip to next. 
                        }
                        else
                        {
                            rotAngle = -rotationDirection * Math.Atan(position[0] / position[1]);
                        }
                        //Make adjustment to bound rotAngle between 0 and 180
                        if (Math.Sign(rotAngle) < 0) rotAngle = Math.PI + rotAngle;

                        //Add angle to list if it is within the bounds
                        if (rotAngle > 0.0 && rotAngle < maxTheta) angleList.Add(rotAngle);
                    }
                    #endregion

                    #region Check if this edge changes the rear extreme vertex
                    //Check whether this edge changes the rear extreme vertex 
                    //First, it will have a to/from position on both sides of the XY plane
                    //Or the one or both of the positions will be on the XY plane
                    if (positions[0][2].IsNegligible() || positions[1][2].IsNegligible() || Math.Sign(positions[0][2]) * Math.Sign(positions[1][2]) < 0)
                    {
                        var arc1 = new[] { n1, n2 };
                        var arc2 = new[] { positions[0], positions[1] };
                        List<Vertex> intersections;
                        if (!ArcGreatCircleIntersection(arc1, arc2, out intersections)) continue; //if no intersection, continue
                        foreach (var intersection in intersections)
                        {
                            //Get rotation angle to the intersection
                            var rotAngle = rotationDirection * Math.Atan(intersections[0].Y / intersections[0].X);
                            if (rotAngle > 0.0 && rotAngle < maxTheta) angleList.Add(rotAngle);
                        }
                    }
                    #endregion
                }

                #region Find OBB along direction at each angle of change
                //sort the angles. Not strictly necessary, but useful for debugging.
                //Debug: ToDo: Remove sort when finished.
                angleList.Sort();
                double[] direction;
                var stepSize = 1E-8;
                var priorAngle = double.NegativeInfinity;
                //Remove Duplicate Angles
                for (var i = 0; i < angleList.Count(); i++)
                {
                    var angle = angleList[i];
                    //if (Math.Abs(angle - priorAngle) < stepSize) 
                    if (angle.IsPracticallySame(priorAngle))
                    {
                        angleList.Remove(angle);
                        i--;
                    }
                    priorAngle = angle;
                }
                double[] dataPoint1 = new double[] { 0.0, 0.0 };
                double[] dataPoint2 = new double[] { 0.0, 0.0 };
                double left1 = double.PositiveInfinity;
                double right1 = double.PositiveInfinity;
                double left2 = double.PositiveInfinity;
                double right2 = double.PositiveInfinity;
                var obbVertices = new List<Vertex>(); //Reset list
                foreach (var angle in angleList)
                {
                    //Find three rotation matrices (two are tests)
                    for (var i = -1; i < 2; i++)
                    {
                        if (angleList.IndexOf(angle) == 0 && i == -1) continue; //Don't add left data to first data point
                        if (angleList.IndexOf(angle) == angleList.Count() - 1 && i == 1) continue; //Don't add right data to last data point
                        var angleChange = angle + stepSize * i;
                        var obb = OBBAlongDirectionFromAngle(n, r, angleChange, ts.ConvexHullVertices);
                        var dataPoint = new double[] { angleChange, obb.Volume };
                        seriesData.Add(dataPoint);
                        if (i == -1) left2 = obb.Volume;
                        if (i == 0) dataPoint2 = new double[] { dataPoint[0], dataPoint[1] };
                        if (i == 1) right2 = obb.Volume;
                        if (obb.Volume < minVolume)
                        {
                            minBox = obb;
                            minVolume = minBox.Volume;
                        }
                    }
                    if (right1 < dataPoint1[1] && left2 < dataPoint2[1] && dataPoint1[0] + stepSize < dataPoint2[0])
                    {
                        var dataPoint = new double[2];
                        var obb = FindMinimumBetweenTwoAngles_GoldenSections(ts.ConvexHullVertices, dataPoint1[0], dataPoint2[0], n, r, out dataPoint);
                        var index = seriesData.Count - 2;
                        seriesData.Insert(index, dataPoint);
                    }
                    right1 = right2;
                    left1 = left2;
                    dataPoint1 = new double[] { dataPoint2[0], dataPoint2[1] };
                    priorAngle = angle;
                }
                if (angleList.Count > 0) volumeData.Add(seriesData);
                #endregion
            }
            return minBox;
        }
        #endregion

        #region BM ApproachOne
        private static BoundingBox Find_via_BM_ApproachOne(TessellatedSolid ts)
        {
            var gaussianSphere = new GaussianSphere(ts);
            var minBox = new BoundingBox();
            var minVolume = double.PositiveInfinity;
            foreach (var arc in gaussianSphere.Arcs)
            {
                //Create great circle one (GC1) along direction of arc.
                //Intersections on this great circle will determine changes in length.
                var greatCircle1 = new GreatCircleAlongArc(gaussianSphere, arc.Nodes[0].Vector, arc.Nodes[1].Vector, arc);

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
                    delta = +delta;//+ intersections[0].angle

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
        #endregion

        #region Bounding Rectangle Corners (DELETE IF UNUSED IN DETERMINING MAX AREA)
        //private static void BoundingRectangleCorners(double[][] directions, List<Vertex> extremeVertices, Vertex vertexOnPlane, out List<Vertex> corners)
        //{
        //    //Check if conditions are satisfied
        //    if (directions.Count() != 3) throw new Exception("Incorrect number of directions. Should be three directions (the first is normal to the Bounding Rectangle)");
        //    if (directions[0].Count() != 3) throw new Exception("Incorrect dimension of directions. The directions must be a 3D vector.");
        //    if (extremeVertices.Count != 4) throw new Exception("Incorrect number of points. Should be four");

        //    corners = new List<Vertex>();
        //    var normalMatrix = new[,] {{directions[0][0],directions[0][1],directions[0][2]}, 
        //                                {directions[1][0],directions[1][1],directions[1][2]},
        //                                {directions[2][0],directions[2][1],directions[2][2]}};
        //    var count = 0;
        //    var xPrime = vertexOnPlane.Position.dotProduct(directions[0].normalize());
        //    for (var i = 0; i < 2; i++)
        //    {
        //        var yPrime = extremeVertices[i].Position.dotProduct(directions[1]);
        //        for (var j = 0; j < 2; j++)
        //        {
        //            var zPrime = extremeVertices[j + 2].Position.dotProduct(directions[2]);
        //            var offAxisPosition = new[] { xPrime, yPrime, zPrime };
        //            var position = normalMatrix.transpose().multiply(offAxisPosition);
        //            corners.Add(new Vertex(position));
        //            count++;
        //        }
        //    }
        //}
        #endregion

        #region ArcGreatCircleIntersection
        internal static bool ArcGreatCircleIntersection(double[][] arc1Vectors, double[][] arc2Vectors, out List<Vertex> intersections)
        {
            intersections = new List<Vertex>();
            var tolerance = StarMath.EqualityTolerance;
            //Get the arc across the great circle from arc1.
            var antipodalArc = new double[][] { arc1Vectors[0].multiply(-1), arc1Vectors[1].multiply(-1) };
            //Create two planes given arc1 and arc2
            var arc1Length = Math.Acos(arc1Vectors[0].dotProduct(arc1Vectors[1]));
            var dot3 = arc2Vectors[0].dotProduct(arc2Vectors[1]);
            if (Math.Abs(dot3).IsPracticallySame(1.0) || double.IsNaN(dot3)) return false;
            var arc2Length = Math.Acos(dot3);
            if (arc2Length.IsNegligible() || double.IsNaN(arc2Length)) return false;
            var norm1 = arc1Vectors[0].crossProduct(arc1Vectors[1]).normalize(); //unit normal
            var norm2 = arc2Vectors[0].crossProduct(arc2Vectors[1]).normalize(); //unit normal

            //Find the intersection points
            if (arc2Vectors[0][2].IsNegligible() || arc2Vectors[0][1].IsNegligible())
            {
                //One or both of arc2's vectors lay on the great circle. The vector(s) is/are the intersection point(s)
                //Case 1: One, Both, or none of arc2's points intersect the antipodal arc
                for (var i = 0; i < 2; i++)
                {
                    var l1 = Math.Acos(antipodalArc[0].dotProduct(arc2Vectors[i]));
                    var l2 = Math.Acos(antipodalArc[1].dotProduct(arc2Vectors[i]));
                    var total = arc1Length - l1 - l2;
                    if (!total.IsNegligible()) continue;
                    intersections.Add(new Vertex(arc2Vectors[i])); //0-2 intersections are possible
                }
                if (intersections.Count < 1) return false; //no intersections found
                return true;
            }
            //Case 2: If none of the vectors intersect the great circle, there must be 1 new intersection.
            //First, get the two possible intersections with the great circle.
            var position1 = norm1.crossProduct(norm2).normalize();
            var vertices = new[] { position1, position1.multiply(-1) };
            //Check to see if the intersections are on arc 2. 
            for (var i = 0; i < 2; i++)
            {
                var dot1 = arc2Vectors[0].dotProduct(vertices[i]);
                if (dot1 > 1.0) dot1 = 1.0;
                if (dot1 < -1.0) dot1 = -1.0;
                var dot2 = arc2Vectors[1].dotProduct(vertices[i]);
                if (dot2 > 1.0) dot2 = 1.0;
                if (dot2 < -1.0) dot2 = -1.0;
                var l1 = Math.Acos(dot1);
                var l2 = Math.Acos(dot2);
                var total = (arc2Length - l1 - l2);
                if (Math.Abs(total) < 1e-6) //Needed more leniancy because of multiple Acos functions. 
                {
                    intersections.Add(new Vertex(vertices[i]));
                }
                if (i == 1 && intersections.Count != 1) throw new Exception(); //must have 1 intersection
            }
            //Then check to see if the intersection is on the antipodal arc.
            var l3 = Math.Acos(antipodalArc[0].dotProduct(intersections[0].Position));
            var l4 = Math.Acos(antipodalArc[1].dotProduct(intersections[0].Position));
            var total2 = arc1Length - l3 - l4;
            if (Math.Abs(total2) < 1e-6) return true;
            return false;
        }
        #endregion

        #region Find OBB Along Direction
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
            var depth = GetLengthAndExtremeVertices(direction, vertices, out v1Low, out v1High);
            double[,] backTransform;
            var points = MiscFunctions.Get2DProjectionPoints(vertices, direction, out backTransform, true);
            var boundingRectangle = RotatingCalipers2DMethod(points);
            //Get reference vertices from boundingRectangle
            var v2Low = boundingRectangle.PointPairs[0][0].References[0];
            var v2High = boundingRectangle.PointPairs[0][1].References[0];
            var v3Low = boundingRectangle.PointPairs[1][0].References[0];
            var v3High = boundingRectangle.PointPairs[1][1].References[0];

            //Get the direction vectors from rotating caliper and projection.
            var tempDirection = new[]
            {
                boundingRectangle.Directions[0][0], boundingRectangle.Directions[0][1],
                boundingRectangle.Directions[0][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction2 = new[] { tempDirection[0], tempDirection[1], tempDirection[2] };
            tempDirection = new[]
            {
                boundingRectangle.Directions[1][0], boundingRectangle.Directions[1][1],
                boundingRectangle.Directions[1][2], 1.0
            };
            tempDirection = backTransform.multiply(tempDirection);
            var direction3 = new[] { tempDirection[0], tempDirection[1], tempDirection[2] };
            var direction1 = direction2.crossProduct(direction3);
            depth = GetLengthAndExtremeVertices(direction1, vertices, out v1Low, out v1High);
            //todo: Fix Get2DProjectionPoints, which seems to be transforming the points to 2D, but not normal to
            //the given direction vector. If it was normal, direction1 should equal direction or its direction.inverse.

            return new BoundingBox(depth, boundingRectangle.Area, new[] { v1Low, v1High, v2Low, v2High, v3Low, v3High },
                new[] { direction1, direction2, direction3 }, boundingRectangle.EdgeVertices);
        }
        #endregion

        #region Get Length And Extreme Vertices
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
        #endregion

        #region 2D Rotating Calipers
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

            //        extremeIndices[3] => max-Y
            extremeIndices[3] = cvxPoints.Count - 1;
            //Check if first point has a higher y value (only when point is both min-x and max-Y)
            if (cvxPoints[0][1] > cvxPoints[extremeIndices[3]][1]) extremeIndices[3] = 0;
            else
            {
                while (extremeIndices[3] >= 1 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1])
                    extremeIndices[3]--;
            }

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
            var stallCounter = 0;
            extremeIndices[0] = extremeIndices[1];
            do
            {
                currentIndex = extremeIndices[0];
                extremeIndices[0]--;
                if (extremeIndices[0] < 0) { extremeIndices[0] = numCvxPoints - 1; }
                previousIndex = extremeIndices[0];
                stallCounter++;
            } while (cvxPoints[currentIndex][0] >= cvxPoints[previousIndex][0] && stallCounter < points.Count());
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
            Vertex edgeVertex1 = null;
            Vertex edgeVertex2 = null;
            var minArea = double.PositiveInfinity;
            var flag = false;
            var cons = Math.PI / 2;
            do
            {
                #region update the deltaAngles from the current orientation
                //For each of the 4 supporting points (those forming the rectangle),
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
                angle = delta;
                if (angle > Math.PI / 2 && !angle.IsPracticallySame(Math.PI / 2))
                {
                    flag = true; //Exit while
                    continue;
                }

                deltaToUpdateIndex = deltaAngles.FindIndex(delta);
                #endregion

                var currentPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];
                extremeIndices[deltaToUpdateIndex]--;
                if (extremeIndices[deltaToUpdateIndex] < 0) { extremeIndices[deltaToUpdateIndex] = numCvxPoints - 1; }
                var previousPoint = cvxPoints[extremeIndices[deltaToUpdateIndex]];

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
                    pointPair1 = new[] { cvxPoints[extremeIndices[2]], cvxPoints[extremeIndices[0]] };
                    pointPair2 = new[] { cvxPoints[extremeIndices[3]], cvxPoints[extremeIndices[1]] };
                    direction1 = new[] { angleVector1[0], angleVector1[1], 0.0 };
                    direction2 = new[] { angleVector2[0], angleVector2[1], 0.0 };
                    edgeVertex1 = previousPoint.References[0];
                    edgeVertex2 = currentPoint.References[0];
                }

            } while (!flag || angle.IsPracticallySame(Math.PI / 2)); //Don't check beyond a 90 degree angle.
            //If best angle is 90 degrees, then don't bother to rotate. 
            if (bestAngle.IsPracticallySame(Math.PI / 2)) { bestAngle = 0.0; }
            #endregion

            var directions = new List<double[]> { direction1, direction2 };
            var extremePoints = new List<Point[]> { pointPair1, pointPair2 };
            if (pointPair1 == null) minArea = 0.0;
            var boundingRectangle = new BoundingRectangle(minArea, bestAngle, directions, extremePoints, new[] { edgeVertex1, edgeVertex2 });
            return boundingRectangle;
        }
        #endregion
    }
}