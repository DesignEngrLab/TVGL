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
                #region Initialize variables
                var rotatorVector = rotateEdge.Vector.normalize();
                var startDir = rotateEdge.OtherFace.Normal;
                var direction = startDir;
                var endDir = rotateEdge.OwnedFace.Normal;
                var origPosYDir = rotatorVector.crossProduct(startDir).normalize();
                var posYDir = origPosYDir;
                var totalAngle = Math.PI - rotateEdge.InternalAngle;
                var startingDots = ts.ConvexHullFaces.Select(f => f.Normal.dotProduct(startDir)).ToArray();
                var angle = 0.0;
                var deltaAngleToBackChange = 0.0;
                var deltaAngleOrthSet = 0.0;

                #region Set up orthogonal arcs and vertices
                var orthGaussSphereArcs = new List<GaussSphereArc>();
                foreach (var edge in ts.ConvexHullEdges)
                {
                    var ownedX = startingDots[edge.OwnedFace.IndexInList];
                    var otherX = startingDots[edge.OtherFace.IndexInList];
                    if (otherX * ownedX <= 0)
                        orthGaussSphereArcs.Add(new GaussSphereArc(edge, posYDir, ownedX, otherX));
                }
                var orthVertices = orthGaussSphereArcs.SelectMany(arc => new[] { arc.Edge.From, arc.Edge.To }).Distinct().ToList();
                #endregion
                #region find back vertex
                Vertex backVertex = null;
                Edge backEdge = null;
                var maxDistance = double.NegativeInfinity;
                foreach (var v in ts.ConvexHullVertices)
                {
                    var distance = v.Position.subtract(rotateEdge.From.Position).dotProduct(startDir);
                    if (distance > maxDistance)
                    {
                        distance = maxDistance;
                        backVertex = v;
                    }
                }
                #endregion
                #endregion
                List<BoundingBoxData> boundingBoxes = new List<BoundingBoxData>();
                do
                {
                    boundingBoxes.Add(new BoundingBoxData
                    {
                        angle = angle,
                        direction = direction,
                        box = FindOBBAlongDirection(orthVertices, direction, rotateEdge.From, rotateEdge.To)
                    });
                    if (deltaAngleToBackChange <= 0)
                        deltaAngleToBackChange = UpdateBackAngle(direction, rotatorVector, posYDir, ref backVertex, ref backEdge);
                    if (deltaAngleOrthSet <= 0)
                        deltaAngleOrthSet = UpdateOrthAngle(direction, ref orthGaussSphereArcs, ref orthVertices);
                    var angleStep = Math.Min(deltaAngleOrthSet, deltaAngleToBackChange);
                    angle += angleStep; deltaAngleToBackChange -= angleStep; deltaAngleOrthSet -= angleStep;
                    if (angle < totalAngle)
                    {
                        direction = UpdateDirection(startDir, rotatorVector, origPosYDir, angle);
                        posYDir = rotatorVector.crossProduct(direction).normalize();
                        UpdateSlopes(direction, posYDir, orthGaussSphereArcs);
                    }
                    else
                        boundingBoxes.Add(new BoundingBoxData
                        {
                            angle = totalAngle,
                            direction = endDir,
                            box = FindOBBAlongDirection(orthVertices, endDir, rotateEdge.From, rotateEdge.To)
                        });
                } while (angle < totalAngle);
                //todo: need to check here if the adjacent bb's are the same signature. If not you need to check in between
            }
            //todo: save best from each and check it with the best found so far.
            return new BoundingBox();
        }

        private static void UpdateSlopes(double[] direction, double[] posYDir, List<GaussSphereArc> orthGaussSphereArcs)
        {
            foreach (var arc in orthGaussSphereArcs)
            {
                arc.Xeffective = direction.dotProduct(arc.ToFace.Normal);
                arc.Yeffective = posYDir.dotProduct(arc.ToFace.Normal);
            }
        }
        
        private static double UpdateOrthAngle(double[] direction, ref List<GaussSphereArc> orthGaussSphereArcs, ref List<Vertex> orthVertices)
        {
            GaussSphereArc arcToRemove = null;
            var minSlope = double.PositiveInfinity;
            foreach (var arc in orthGaussSphereArcs)
            {
                var tempSlope = arc.Yeffective / arc.Xeffective;
                if (tempSlope < minSlope)
                {
                    minSlope = tempSlope;
                    arcToRemove = arc;
                }
            }
            var edgesAtJunction = new List<Edge>(arcToRemove.ToFace.Edges);
            for (int i = orthGaussSphereArcs.Count - 1; i >= 0; i--)
            {
                var index = edgesAtJunction.FindIndex(orthGaussSphereArcs[i].Edge);
                if (index >= 0)
                {
                    orthGaussSphereArcs.RemoveAt(i);
                    orthVertices.Remove(edgesAtJunction[index].From);
                    orthVertices.Remove(edgesAtJunction[index].To);
                    edgesAtJunction.RemoveAt(index);
                }
            }
            foreach (var edge in edgesAtJunction)
            {
                if (!orthVertices.Contains(edge.From)) orthVertices.Add(edge.From);
                if (!orthVertices.Contains(edge.To)) orthVertices.Add(edge.To);
                orthGaussSphereArcs.Add(new GaussSphereArc(edge, arcToRemove.ToFace, (edge.OwnedFace == arcToRemove.ToFace)
                    ? edge.OtherFace : edge.OwnedFace));
            }
            return Math.Atan(minSlope);
        }

        private static double[] UpdateDirection(double[] startDir, double[] rotator, double[] posYDir, double angle)
        {
            var A = new double[3, 3];
            A.SetRow(0, rotator); A.SetRow(1, startDir); A.SetRow(2, posYDir);
            var b = new[] { 0.0, Math.Cos(angle), Math.Cos(angle + Math.PI / 2) };
            return StarMath.solve(A, b);
        }

        private static double UpdateBackAngle(double[] direction, double[] rotator, double[] posYDir, ref Vertex backVertex, ref Edge backEdge)
        {
            Edge nextEdge = null;
            var yDotWithOtherFace = double.NegativeInfinity;
            foreach (var edge in backVertex.Edges)
            {
                if (rotator.dotProduct(edge.From.Position) * rotator.dotProduct(edge.To.Position) <= 0)
                {
                    if (edge != backEdge && edge.OtherVertex(backVertex).Position.dotProduct(posYDir) > yDotWithOtherFace)
                        nextEdge = edge;
                }
            }
            var positionVector = (new[] { nextEdge.Vector.dotProduct(direction), nextEdge.Vector.dotProduct(posYDir) }).normalize();
            backVertex = nextEdge.OtherVertex(backVertex);
            backEdge = nextEdge;
            return Math.Acos(positionVector.dotProduct(posYDir));
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

        private struct BoundingBoxData
        {
            public BoundingBox box;
            public double[] direction;
            public double angle;
        }

    }
}