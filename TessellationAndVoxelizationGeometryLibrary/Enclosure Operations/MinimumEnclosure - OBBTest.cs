// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-15-2015
// ***********************************************************************
// <copyright file="MinimumBoundingBox.cs" company="Design Engineering Lab">
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
    ///     The MinimumEnclosure class includes static functions for defining smallest enclosures for a
    ///     tessellated solid. For example: convex hull, minimum bounding box, or minimum bounding sphere.
    /// </summary>
    public static partial class MinimumEnclosure
    {
        /// <summary>
        ///     Finds the minimum bounding box oriented along a particular Direction.
        /// </summary>
        /// <param name="ts">The ts.</param>
        /// <param name="times"></param>
        /// <param name="volumes"></param>
        /// <returns>BoundingBox.</returns>
        //private
        public static BoundingBox OrientedBoundingBox_Test(TessellatedSolid ts, out List<double> times,
            out List<double> volumes) //, out List<List<double[]>> volumeData2)
        {
            times = new List<double>();
            volumes = new List<double>();
            //var flats = ListFunctions.Flats(ts.Faces.ToList());
            var now = DateTime.Now;
            Message.output("Beginning OBB Test", 2);
            var boundingBox1 = OrientedBoundingBox(ts.ConvexHull.Vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox1.Volume);
            //Message.output("Time Elapsed for PCA Approach = " ,4);
            //Message.output("Volume for PCA Approach= " + boundingBox1.Volume,4);
            now = DateTime.Now;
            Message.output("Beginning OBB Test", 2);

            var boundingBox12 = Find_via_PCA_ApproachNR(ts.ConvexHull.Vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox12.Volume);
            //Message.output("Time Elapsed for PCA Approach = " ,4 );
            //Message.output("Volume for PCA Approach= " + boundingBox1.Volume);
            now = DateTime.Now;
            var boundingBox2 = Find_via_ChanTan_AABB_Approach(ts.ConvexHull.Vertices);
            times.Add((DateTime.Now - now).TotalMilliseconds);
            volumes.Add(boundingBox2.Volume);
            Message.output("Time Elapsed for ChanTan Approach = " + (DateTime.Now - now), 4);
            Message.output("Volume for ChanTan Approach = " + boundingBox2.Volume, 4);
            //now = DateTime.Now;
            //Message.output("Beginning OBB Test");
            //var boundingBox1 = Find_via_MC_ApproachOne(ts, out volumeData1);
            //Message.output("Time Elapsed for MC Approach One = " + (DateTime.Now - now),4);
            //now = DateTime.Now;
            //var boundingBox2 = Find_via_BM_ApproachTwo(ts, out volumeData2);
            //Message.output("Time Elapsed for BM Approach Two = " + (DateTime.Now - now),4);

            return boundingBox2;
        }

        private static BoundingBox Find_via_ChanTan_AABB_Approach(IList<Vertex> convexHullVertices)
        {
            return Find_via_ChanTan_AABB_Approach(convexHullVertices, new BoundingBox
            {
                Directions = new[] {new[] {1.0, 0.0, 0.0}, new[] {0.0, 1.0, 0.0}, new[] {0.0, 0.0, 1.0}},
                Volume = double.PositiveInfinity
            });
        }

        private class BoundingBoxData
        {
            private BoundingBoxData()
            {
            }

            internal BoundingBoxData(double[] startDir, double[] yDir, Edge rotatorEdge, double[] rotatorVector,
                TVGLConvexHull convexHull)
            {
                Direction = startDir;
                PosYDir = yDir;
                RotatorEdge = rotatorEdge;
                RotatorVector = rotatorVector;
                OrthGaussSphereArcs = new List<GaussSphereArc>();
                // make arrays of the dotproducts with start and end directions (x-values) to help subsequent
                // foreach loop which will look up faces multiple times.
                var startingDots = new double[convexHull.Faces.Length];
                for (var i = 0; i < convexHull.Faces.Length; i++)
                {
                    var face = convexHull.Faces[i];
                    face.IndexInList = i;
                    startingDots[i] = face.Normal.dotProduct(startDir);
                }
                foreach (var edge in convexHull.Edges)
                {
                    var ownedX = startingDots[edge.OwnedFace.IndexInList];
                    var otherX = startingDots[edge.OtherFace.IndexInList];
                    if (otherX*ownedX <= 0)
                    {
                        var ownedY = edge.OwnedFace.Normal.dotProduct(yDir);
                        var otherY = edge.OtherFace.Normal.dotProduct(yDir);
                        //if ((ownedX < 0 && ownedY > 0) || (ownedX > 0 && ownedY < 0))
                        //    OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace));
                        //else if ((otherX < 0 && otherY > 0) || (otherX > 0 && otherY < 0))
                        //    OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OtherFace));
                        if ((ownedX <= 0 && ownedY > 0) || (ownedX >= 0 && ownedY < 0))
                            OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace));
                        else if ((otherX <= 0 && otherY > 0) || (otherX >= 0 && otherY < 0))
                            OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OtherFace));
                    }
                }
                OrthVertices =
                    OrthGaussSphereArcs.SelectMany(arc => new[] {arc.Edge.From, arc.Edge.To}).Distinct().ToList();
                var maxDistance = double.NegativeInfinity;
                foreach (var v in convexHull.Vertices)
                {
                    var distance = rotatorEdge.From.Position.subtract(v.Position).dotProduct(startDir);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        BackVertex = v;
                    }
                }
            }

            public BoundingBox Box { get; set; }
            public double[] Direction { get; set; }
            public double Angle { get; set; }
            public List<Vertex> OrthVertices { get; private set; }
            public List<GaussSphereArc> OrthGaussSphereArcs { get; private set; }
            public Vertex BackVertex { get; set; }
            public Edge BackEdge { get; set; }
            public double[] PosYDir { get; set; }
            public double[] RotatorVector { get; private set; }
            public Edge RotatorEdge { get; private set; }

            public BoundingBoxData Copy()
            {
                return new BoundingBoxData
                {
                    Angle = Angle,
                    BackVertex = BackVertex,
                    BackEdge = BackEdge,
                    Box = new BoundingBox
                    {
                        CornerVertices = Box.CornerVertices != null ? (Vertex[]) Box.CornerVertices.Clone() : null,
                        Center = Box.Center != null ? new Vertex(Box.Center.Position) : null,
                        Dimensions = Box.Dimensions != null ? (double[]) Box.Dimensions.Clone() : null,
                        Directions = Box.Directions != null ? (double[][]) Box.Directions.Clone() : null,
                        PointsOnFaces = Box.PointsOnFaces != null ? (List<Vertex>[]) Box.PointsOnFaces.Clone() : null,
                        Volume = Box.Volume
                    },
                    Direction = (double[]) Direction.Clone(),
                    OrthGaussSphereArcs = new List<GaussSphereArc>(OrthGaussSphereArcs),
                    OrthVertices = new List<Vertex>(OrthVertices),
                    PosYDir = (double[]) PosYDir.Clone(),
                    RotatorVector = (double[]) RotatorVector.Clone(),
                    RotatorEdge = RotatorEdge
                };
            }
        }

        #region PCA Approaches

        /// <summary>
        ///     Finds the minimum bounding box using a direct approach called PCA.
        ///     Variants include All-PCA, Min-PCA, Max-PCA, and continuous PCA [http://dl.acm.org/citation.cfm?id=2019641]
        ///     The one implemented looks at Min-PCA, Max-PCA and Mid-PCA (considers all three of the eigen vectors).
        ///     The most accurate is continuous PCA, and Dimitrov 2009 has some improvements
        ///     Dimitrov, Holst, and Kriegel. "Closed-Form Solutions for Continuous PCA and Bounding Box Algorithms"
        ///     http://link.springer.com/chapter/10.1007%2F978-3-642-10226-4_3
        ///     Simple implementation (2/5)
        /// </summary>
        /// <timeDomain>
        ///     O(nlog(n)) time
        /// </timeDomain>
        /// <accuracy>
        ///     Generally fairly accurate, but suboptimal solutions.
        ///     Particular cases can yield very poor results.
        ///     Ex. Dimitrov showed in 2009 that continuous PCA yields a volume 4x optimal for a octahedron
        ///     http://page.mi.fu-berlin.de/rote/Papers/pdf/Bounds+on+the+quality+of+the+PCA+bounding+boxes.pdf
        /// </accuracy>
        private static BoundingBox Find_via_PCA_ApproachNR(IList<Vertex> convexHullVertices)
        {
            var m = new double[3];
            // loop over the points to find the mean point location
            m = convexHullVertices.Aggregate(m, (current, point) => current.add(point.Position));
            m = m.divide(convexHullVertices.Count);
            var C = new double[3, 3];
            var m00 = m[0]*m[0];
            var m01 = m[0]*m[1];
            var m02 = m[0]*m[2];
            var m11 = m[1]*m[1];
            var m12 = m[1]*m[2];
            var m22 = m[2]*m[2];
            // loop over the points again to build the covariance matrix.  
            // Note that we only have to build terms for the upper 
            // triangular portion since the matrix is symmetric
            double cxx = 0.0, cxy = 0.0, cxz = 0.0, cyy = 0.0, cyz = 0.0, czz = 0.0;
            foreach (var p in convexHullVertices.Select(point => point.Position))
            {
                cxx += p[0]*p[0] - m00;
                cxy += p[0]*p[1] - m01;
                cxz += p[0]*p[2] - m02;
                cyy += p[1]*p[1] - m11;
                cyz += p[1]*p[2] - m12;
                czz += p[2]*p[2] - m22;
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
                Directions = eigenVectors,
                Volume = double.PositiveInfinity
            };

            //Perform a 2D caliper along each eigenvector. 
            for (var i = 0; i < 3; i++)
            {
                var newObb = FindOBBAlongDirection(convexHullVertices, minOBB.Directions[i]);
                if (newObb.Volume.IsLessThanNonNegligible(minOBB.Volume))
                    minOBB = newObb;
            }
            return minOBB;
        }

        private static BoundingBox Find_via_PCA_ApproachBM(IList<Vertex> convexHullVertices,
            IEnumerable<PolygonalFace> convexHullFaces)
        {
            //Find a continuous set of 3 dimensional vectors with constant density
            var triangles = new List<PolygonalFace>(convexHullFaces);
            var totalArea = triangles.Sum(t => t.Area);

            //Calculate the center of gravity of combined triangles
            var c = new[] {0.0, 0.0, 0.0};
            foreach (var triangle in triangles)
            {
                //Find the triangle weight based proportional to area
                var w = triangle.Area/totalArea;
                //Find the center of gravity
                c = c.add(triangle.Center.multiply(w));
            }

            //Find the covariance matrix  of the convex hull
            var covariance = new double[3, 3];
            foreach (var triangle in triangles)
            {
                var covarianceI = new[,] {{0.0, 0.0, 0.0}, {0.0, 0.0, 0.0}, {0.0, 0.0, 0.0}};
                for (var j = 0; j < 3; j++)
                {
                    var jTerm1 = new double[3, 3];
                    var jTerm1Total = new double[3, 3];
                    var vector1 = triangle.Vertices[j].Position.subtract(c);
                    var term1 = new[,] {{vector1[0], vector1[1], vector1[2]}};
                    var term3 = term1;
                    var term4 = new[,] {{vector1[0]}, {vector1[1]}, {vector1[2]}};
                    for (var k = 0; k < 3; k++)
                    {
                        var vector2 = triangle.Vertices[k].Position.subtract(c);
                        var term2 = new[,] {{vector2[0]}, {vector2[1]}, {vector2[2]}};
                        jTerm1 = term2.multiply(term1);
                        jTerm1Total = jTerm1Total.add(jTerm1);
                    }
                    var jTerm2 = term4.multiply(term3);
                    var jTermTotal = jTerm1.add(jTerm2);
                    covarianceI = covarianceI.add(jTermTotal);
                }
                covariance = covariance.add(covarianceI.multiply(1.0/12.0));
            }

            //Find eigenvalues of covariance matrix
            double[][] eigenVectors;
            covariance.GetEigenValuesAndVectors(out eigenVectors);

            var bestOBB = new BoundingBox {Volume = double.PositiveInfinity};
            // Perform a 2D caliper along each eigenvector. 
            foreach (var eigenVector in eigenVectors)
            {
                var OBB = FindOBBAlongDirection(convexHullVertices, eigenVector.normalize());
                if (OBB.Volume < bestOBB.Volume)
                    bestOBB = OBB;
            }
            return bestOBB;
        }

        #endregion

        #region MC ApproachOne

        /// <summary>
        ///     The MC_ApproachOne rotates around each edge of the convex hull between the owned and
        ///     other faces. In this way, it guarantees a much more optimal solution than the flat
        ///     with face algorithm, but is, therefore, slower.
        /// </summary>
        /// <timeDomain>
        ///     Since the computation cost for each Bounding Box is linear O(n),
        ///     and the approximate worse case number of normals considered is n*PI/maxDeltaAngle,
        ///     Lower Bound O(n^2). Upper Bound O(n^(2)*PI/maxDeltaAngle). [ex.  upper bound is O(36*n^2) when MaxDeltaAngle = 5
        ///     degrees.]
        /// </timeDomain>
        /// <accuracy>
        ///     Garantees the optimial orientation is within maxDeltaAngle error.
        /// </accuracy>
        private static BoundingBox OrientedBoundingBox(TVGLConvexHull convexHull)
        {
            var minBox = new BoundingBox {Volume = double.PositiveInfinity};
            foreach (var rotateEdge in convexHull.Edges)
            {
                #region Initialize variables

                //Initialize variables
                //rotatorVector is basically the edge in question - the vector that is being rotated about
                var rotatorVector = rotateEdge.Vector.normalize();
                // startDir is the starting Direction - based on the OtherFace
                var startDir = rotateEdge.OtherFace.Normal;
                // endDir is the OwnedFace final Direction - we go from Other to Owned since in order to be about
                // the positive Direction of the rotatorVector
                var endDir = rotateEdge.OwnedFace.Normal;
                // posYDir is the vector for the positive y-Direction. Well, this is a simplification of the 
                //gauss sphere to a 2D circle. The Direction (such as startDir) represents the x-axis and this
                //, which is the orthogonal is the y Direction
                var origPosYDir = rotatorVector.crossProduct(startDir).normalize();
                var totalAngle = Math.PI - rotateEdge.InternalAngle;
                var thisBoxData = new BoundingBoxData(startDir, origPosYDir, rotateEdge, rotatorVector, convexHull);

                #endregion

                FindOBBAlongDirection(thisBoxData);
                if (thisBoxData.Box.Volume < minBox.Volume) minBox = thisBoxData.Box;
                var angle = 0.0;
                var deltaAngleToBackChange = 0.0;
                var deltaAngleOrthSet = 0.0;
                BoundingBoxData backChangeBox = null;
                BoundingBoxData sideChangeBox = null;
                do
                {
                    if (deltaAngleToBackChange <= 0)
                    {
                        backChangeBox = thisBoxData.Copy();
                        deltaAngleToBackChange = UpdateBackAngle(backChangeBox);
                    }
                    if (deltaAngleOrthSet <= 0)
                    {
                        sideChangeBox = thisBoxData.Copy();
                        deltaAngleOrthSet = UpdateOrthAngle(sideChangeBox);
                    }
                    BoundingBoxData nextBoxData;
                    if (deltaAngleOrthSet < deltaAngleToBackChange)
                    {
                        deltaAngleToBackChange -= deltaAngleOrthSet;
                        angle += deltaAngleOrthSet;
                        deltaAngleOrthSet = 0;
                        nextBoxData = sideChangeBox;
                    }
                    else if (deltaAngleToBackChange < deltaAngleOrthSet)
                    {
                        deltaAngleOrthSet -= deltaAngleToBackChange;
                        angle += deltaAngleToBackChange;
                        deltaAngleToBackChange = 0;
                        nextBoxData = backChangeBox;
                    }
                    else // if they are equal to each other
                    {
                        angle += deltaAngleToBackChange;
                        deltaAngleOrthSet = deltaAngleToBackChange = 0;
                        nextBoxData = backChangeBox;
                    }
                    if (angle > totalAngle)
                    {
                        // nextBoxData = new BoundingBoxData(endDir, rotatorVector.crossProduct(endDir).normalize(), rotateEdge, rotatorVector, convexHull);
                        nextBoxData.Angle = totalAngle;
                        nextBoxData.Direction = endDir;
                    }
                    else
                    {
                        nextBoxData.Angle = angle;
                        nextBoxData.Direction = UpdateDirection(startDir, rotatorVector, origPosYDir, angle);
                    }
                    nextBoxData.PosYDir = nextBoxData.RotatorVector.crossProduct(nextBoxData.Direction).normalize();

                    /****************/
                    FindOBBAlongDirection(nextBoxData);
                    /****************/
                    if (DifferentMembershipInExtrema(thisBoxData, nextBoxData))
                    {
                        var lowerBox = thisBoxData;
                        var upperBox = nextBoxData;
                        var midBox = thisBoxData.Copy();
                        while (!lowerBox.Angle.IsPracticallySame(upperBox.Angle, Constants.OBBAngleTolerance))
                        {
                            midBox.Direction = lowerBox.Direction.add(upperBox.Direction).divide(2).normalize();
                            midBox.Angle = (lowerBox.Angle + upperBox.Angle)/2.0;
                            FindOBBAlongDirection(midBox);
                            if (midBox.Box.Volume > lowerBox.Box.Volume && midBox.Box.Volume > upperBox.Box.Volume)
                                break;
                            if (!DifferentMembershipInExtrema(lowerBox, midBox))
                                lowerBox = midBox;
                            else if (!DifferentMembershipInExtrema(upperBox, midBox))
                                upperBox = midBox;
                            else throw new Exception("new midbox is different from BOTH neighbors!");
                        }
                        if (thisBoxData.Box.Volume < minBox.Volume) minBox = midBox.Box;
                    }
                    thisBoxData = nextBoxData;
                    if (thisBoxData.Box.Volume < minBox.Volume) minBox = thisBoxData.Box;
                } while (angle < totalAngle);
            }
            return minBox;
        }

        private static double UpdateOrthAngle(BoundingBoxData boxData)
        {
            GaussSphereArc arcToRemove = null;
            var minSlope = double.PositiveInfinity;
            boxData.PosYDir = boxData.RotatorVector.crossProduct(boxData.Direction).normalize();
            foreach (var arc in boxData.OrthGaussSphereArcs)
            {
                var x = boxData.Direction.dotProduct(arc.ToFace.Normal);
                var y = boxData.PosYDir.dotProduct(arc.ToFace.Normal);
                if (y == 0.0) continue;
                var tempSlope = -x/y;
                if (!(tempSlope < minSlope)) continue;
                minSlope = tempSlope;
                arcToRemove = arc;
            }
            if (minSlope < 0) return double.PositiveInfinity;
            var edgesAtJunction = new List<Edge>(arcToRemove.ToFace.Edges);
            for (var i = boxData.OrthGaussSphereArcs.Count - 1; i >= 0; i--)
            {
                var index = edgesAtJunction.FindIndex(boxData.OrthGaussSphereArcs[i].Edge);
                if (index >= 0)
                {
                    boxData.OrthGaussSphereArcs.RemoveAt(i);
                    boxData.OrthVertices.Remove(edgesAtJunction[index].From);
                    boxData.OrthVertices.Remove(edgesAtJunction[index].To);
                    edgesAtJunction.RemoveAt(index);
                }
            }
            foreach (var edge in edgesAtJunction)
            {
                if (!boxData.OrthVertices.Contains(edge.From)) boxData.OrthVertices.Add(edge.From);
                if (!boxData.OrthVertices.Contains(edge.To)) boxData.OrthVertices.Add(edge.To);
                boxData.OrthGaussSphereArcs.Add(new GaussSphereArc(edge, edge.OwnedFace == arcToRemove.ToFace
                    ? edge.OtherFace
                    : edge.OwnedFace));
            }
            return Math.Atan(minSlope);
        }

        private static double UpdateBackAngle(BoundingBoxData boxData)
        {
            Edge nextEdge = null;
            var yDir = boxData.RotatorVector.crossProduct(boxData.Direction);
            var minSlope = double.PositiveInfinity;
            foreach (var edge in boxData.BackVertex.Edges)
            {
                var otherVertex = edge.OtherVertex(boxData.BackVertex);
                var vector = otherVertex.Position.subtract(boxData.BackVertex.Position);
                var y = yDir.dotProduct(vector);
                if (y < 0)
                {
                    // the x-value is boxData.Direction.dotProduct(vector) and it's positive for all edges since it's the back vertex
                    var slope = -boxData.Direction.dotProduct(vector)/y;
                    if (slope < minSlope)
                    {
                        minSlope = slope;
                        nextEdge = edge;
                    }
                }
            }
            if (minSlope < 0) return double.PositiveInfinity;
            boxData.BackVertex = nextEdge.OtherVertex(boxData.BackVertex);
            boxData.BackEdge = nextEdge;
            return Math.Atan(minSlope);
        }


        private static bool DifferentMembershipInExtrema(BoundingBoxData boxDataA, BoundingBoxData boxDataB)
        {
            var boxASides = boxDataA.Box.PointsOnFaces.Skip(2);
            var boxBSides = boxDataB.Box.PointsOnFaces.Skip(2).ToList();
            foreach (var boxASide in boxASides)
            {
                if (!boxBSides.Any(boxBSide => boxASide.Intersect(boxBSide).Any()))
                    return true;
            }
            return false;
        }


        private static double[] UpdateDirection(double[] startDir, double[] rotator, double[] posYDir, double angle)
        {
            var a = new double[3, 3];
            a.SetRow(0, rotator);
            a.SetRow(1, startDir);
            a.SetRow(2, posYDir);
            var b = new[] {0.0, Math.Cos(angle), Math.Cos(angle + Math.PI/2)};
            return StarMath.solve(a, b);
        }

        #endregion
    }
}