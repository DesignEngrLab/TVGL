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
using System.Threading.Tasks;

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
         //private
        public static BoundingBox OrientedBoundingBox_Test(TessellatedSolid ts, out List<double> times, out List<double> volumes)//, out List<List<double[]>> volumeData2)
        {
            times = new List<double>();
            volumes = new List<double>();
            //var flats = ListFunctions.Flats(ts.Faces.ToList());
            var now = DateTime.Now;
            Debug.WriteLine("Beginning OBB Test");
            var boundingBox1 = OrientedBoundingBox(ts.ConvexHullVertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox1.Volume);
            //Debug.WriteLine("Time Elapsed for PCA Approach = " + );
            //Debug.WriteLine("Volume for PCA Approach= " + boundingBox1.Volume);
            now = DateTime.Now;
            Debug.WriteLine("Beginning OBB Test");

            var boundingBox12 = Find_via_PCA_ApproachNR(ts.ConvexHullVertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox12.Volume);
            //Debug.WriteLine("Time Elapsed for PCA Approach = " + );
            //Debug.WriteLine("Volume for PCA Approach= " + boundingBox1.Volume);
            now = DateTime.Now;
            var boundingBox2 = Find_via_ChanTan_AABB_Approach(ts.ConvexHullVertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox2.Volume);
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


        #region PCA Approaches
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
        private static BoundingBox Find_via_PCA_ApproachNR(IList<Vertex> convexHullVertices)
        {
            var m = new double[3];
            // loop over the points to find the mean point location
            foreach (var point in convexHullVertices)
                m = m.add(point.Position);
            m = m.divide(convexHullVertices.Count);
            var C = new double[3, 3];
            var m00 = m[0] * m[0];
            var m01 = m[0] * m[1];
            var m02 = m[0] * m[2];
            var m11 = m[1] * m[1];
            var m12 = m[1] * m[2];
            var m22 = m[2] * m[2];
            // loop over the points again to build the covariance matrix.  
            // Note that we only have to build terms for the upper 
            // triangular portion since the matrix is symmetric
            double cxx = 0.0, cxy = 0.0, cxz = 0.0, cyy = 0.0, cyz = 0.0, czz = 0.0;
            foreach (var p in convexHullVertices.Select(point => point.Position))
            {
                cxx += p[0] * p[0] - m00;
                cxy += p[0] * p[1] - m01;
                cxz += p[0] * p[2] - m02;
                cyy += p[1] * p[1] - m11;
                cyz += p[1] * p[2] - m12;
                czz += p[2] * p[2] - m22;
            }
            // now build the covariance matrix
            C[0, 0] = cxx;
            C[1, 1] = cyy;
            C[2, 2] = czz;
            C[0, 1] = C[1, 0] = cxy;
            C[0, 2] = C[2, 0] = cxz;
            C[1, 2] = C[2, 1] = cyz;
            //Find eigenvalues of covariance matrix
            double[][] eigenVectors;
            C.GetEigenValuesAndVectors(out eigenVectors);

            var minOBB = new BoundingBox
            {
                Volume = double.PositiveInfinity,
                Directions = eigenVectors
            };
            //Perform a 2D caliper along each eigenvector. 
            for (int i = 0; i < 3; i++)
            {
                var newObb = FindOBBAlongDirection(convexHullVertices, minOBB.Directions[i]);
                if (newObb.Volume.IsLessThanNonNegligible(minOBB.Volume))
                    minOBB = newObb;
            }
            return minOBB;
        }
        private static BoundingBox Find_via_PCA_ApproachBM(IList<Vertex> convexHullVertices, IEnumerable<PolygonalFace> convexHullFaces)
        {
            //Find a continuous set of 3 dimensional vextors with constant density
            var triangles = new List<PolygonalFace>(convexHullFaces);
            var totalArea = triangles.Sum(t => t.Area);
            var minVolume = double.PositiveInfinity;

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
        private static BoundingBox Find_via_ChanTan_AABB_Approach(IList<Vertex> convexHullVertices)
        {
            return Find_via_ChanTan_AABB_Approach(convexHullVertices, new BoundingBox()
            {
                Volume = double.PositiveInfinity,
                Directions = new[] { new[] { 1.0, 0.0, 0.0 }, new[] { 0.0, 1.0, 0.0 }, new[] { 0.0, 0.0, 1.0 } }
            });
        }
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
        /// <param name="ts">The ts.</param>
        /// <param name="volumeData">The volume data.</param>
        /// <returns>TVGL.BoundingBox.</returns>
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

            foreach (var rotateEdge in ts.ConvexHullEdges)
            {
                //Initialize variables
                //rotatorVector is basically the edge in question - the vector that is being rotated about
                var rotatorVector = rotateEdge.Vector.normalize();
                // startDir is the starting direction - based on the OtherFace
                var startDir = rotateEdge.OtherFace.Normal;
                // endDir is the OwnedFace final direction - we go from Other to Owned since in order to be about
                // the positive direction of the rotatorVector
                var endDir = rotateEdge.OwnedFace.Normal;
                // posYDir is the vector for the positive y-direction. Well, this is a simplification of the 
                //gauss sphere to a 2D circle. The direction (such as startDir) represents the x-axis and this
                //, which is the orthogonal is the y direction
                var posYDir = startDir.crossProduct(rotatorVector);
                // make arrays of the dotproducts with start and end directions (x-values) to help subsequent
                // foreach loop which will look up faces multiple times.
                var startingDots = ts.ConvexHullFaces.Select(f => f.Normal.dotProduct(startDir)).ToArray();
               // var endingDots = ts.ConvexHullFaces.Select(f => f.Normal.dotProduct(endDir)).ToArray();
                var orthGaussSphereArcs = new List<GaussSphereArc>();
               // var remainingEdges = new List<GaussSphereArc>();
                foreach (var edge in ts.ConvexHullEdges)
                {
                    var ownedX = startingDots[edge.OwnedFace.IndexInList];
                    var otherX = startingDots[edge.OtherFace.IndexInList];
                    if (otherX * ownedX <= 0)
                        orthGaussSphereArcs.Add(new GaussSphereArc(edge, posYDir, ownedX, otherX));
                    //else
                    //{
                    //    var endOwnedX = endingDots[edge.OwnedFace.IndexInList];
                    //    var endOtherX = endingDots[edge.OtherFace.IndexInList];
                    //    if (endOtherX * endOwnedX <= 0 || ownedX * endOwnedX <= 0 
                    //        || otherX * endOtherX <= 0)
                    //        remainingEdges.Add(new GaussSphereArc(edge, posYDir, ownedX, otherX));
                    //}
                }
                var direction = startDir;
                var angle = 0;
                var backVertexChanged = false;

                while (angle <= rotateEdge.InternalAngle)
                {
                    var vertices = orthGaussSphereArcs.SelectMany(arc => new[] { arc.Edge.From, arc.Edge.To }).Distinct().ToList();
                    var points = MiscFunctions.Get2DProjectionPoints(vertices, direction);
                    var tempBoundingRect = BoundingRectangle(points, false);
                    if (backVertexChanged sameBoundingRectMembers(tempBoundingRect, lastBoundingRect))

                }

                //Check for exceptions and special cases.
                //Skip the edge if its internal angle is practically 0 or 180 since.
                if (Math.Abs(internalAngle - Math.PI) < 0.0001 || Math.Round(internalAngle, 5).IsNegligible()) continue;
                if (rotateEdge.Curvature == CurvatureType.Concave) throw new Exception("Error in internal angle definition");
                //r cross owned face normal should point along the other face normal.
                if (r.crossProduct(n).dotProduct(rotateEdge.OtherFace.Normal) < 0) throw new Exception();

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
                        extremeDepth = MiscFunctions.DistancePointToLine(vertex1.Position, rotateEdge.From.Position, rotateEdge.Vector, out pointOnLine);
                        depthOrtho = vertex1.Position.subtract(pointOnLine).normalize();
                    }

                    if (obb.ExtremeVertices[2] == vertex3 && obb.ExtremeVertices[3] == vertex4 &&
                        obb.ExtremeVertices[4] == vertex5 && obb.ExtremeVertices[5] == vertex6)
                    // &&  obb.EdgeVertices[0] == edgeVertex1 && obb.EdgeVertices[1] == edgeVertex2)
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

    }
}