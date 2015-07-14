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
using System.Linq.Expressions;
using ClipperLib;
using MIConvexHull;
using StarMathLib;

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
            return Find_via_MC_ApproachOne(ts);
        }

        private static BoundingBox Find_via_ChanTan_AABB_Approach(TessellatedSolid ts)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Finds the minimum bounding box using a direct approach called continuous PCA.
        /// Variant include All-PCA Min-PCA, Max-PCA, and continuous PCA [http://dl.acm.org/citation.cfm?id=2019641]
        /// The most accurate is continuous PCA, and Dimitrov 2009 has some good improvements
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
        public static double Find_via_ContinuousPCA_Approach(TessellatedSolid ts)
        {
            //Find a continuous set of 3 dimensional vextors with constant density
            var triangles = new List<PolygonalFace>(ts.ConvexHullFaces);

            var totalArea = 0.0;
            var minArea = double.PositiveInfinity;
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

            //Calculate the center of gravity of each triangle
            var c = new double[] { 0.0, 0.0, 0.0 };
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
                    var vector1 = triangle.Vertices[j].Position.subtract(c);
                    var term1 = new[,] { { vector1[0], vector1[1], vector1[2] } };
                    var term3 = term1;
                    var term4 = new[,] { { vector1[0] }, { vector1[1] }, { vector1[2] } };
                    for (var k = 0; k < 3; k++)
                    {
                        var vector2 = triangle.Vertices[k].Position.subtract(c);
                        var term2 = new[,] { { vector2[0] }, { vector2[1] }, { vector2[2] } };
                        jTerm1 = term1.multiply(term2);
                        //todo: Figure out how to add these summations up properly
                    }
                    covarianceI = covarianceI.add(jTerm1.add(term3.multiply(term4)));
                }
                covariance = covariance.add(covarianceI.multiply(1.0 / 12.0));
            }

            //Find eigenvalues of covariance matrix
            double[][] eigenVectors;
            var eigenValues = covariance.GetEigenValuesAndVectors(out eigenVectors);
            //Perform a 2D caliper along each eigenvector. 
            foreach (var eigenVector in eigenVectors)
            {
                var points = MiscFunctions.Get2DProjectionPoints(ts.ConvexHullVertices, eigenVector);
                var cvHull = ConvexHull2D(points);
                double area;
                RotatingCalipers2DMethod(cvHull, out area);
                if (area < minArea)
                {
                    minArea = area;
                }
            }
            return minArea;
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
                var edgeBBs = new BoundingBox[numSamples];
                for (var i = 0; i < numSamples; i++)
                {
                    double[] direction;
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
                        direction = invCrossMatrix.multiply(rotAxis.multiply(Math.Sin(angleChange)));
                    }
                    edgeBBs[i] = FindOBBAlongDirection(ts.ConvexHullVertices, direction);
                    if (edgeBBs[i].Volume < minVolume)
                    {
                        minBox = edgeBBs[i];
                        minVolume = minBox.Volume;
                    }
                }
            }
            return minBox;
        }


        //private static BoundingBox Find_via_BM_ApproachOne(TessellatedSolid ts)
        //{
        //    var gaussianSphere = new GaussianSphere(ts);
        //    var minBox = new BoundingBox();
        //    var minVolume = double.PositiveInfinity;
        //    var minArea = double.PositiveInfinity;
        //    foreach (var arc in gaussianSphere.Arcs)
        //    {
        //        //Create great circle one (GC1) along direction of arc.
        //        //Intersections on this great circle will determine changes in length.

        //        //Create great circle two (GC2) orthoganal to the current normal (n1).
        //        //GC2 contains an arc list of all the arcs it intersects
        //        //The intersections and perspective determine the cross sectional area.
        //        //Find the next node from each line along the direction of the arc (angle of rotation)

        //        //List intersections from GC1 and nodes from GC2 based on angle of rotation to each.
        //        var delta = 0;

        //        //Get the initial length
        //        Vertex vLow;
        //        Vertex vHigh;
        //        var length = GetLengthAndExtremeVertices(arc.Nodes[0],ts.ConvexHullVertices,out vLow, out vHigh);
        //        do
        //        {
        //            RotatingCalipers2DMethod(greatCircle2, minArea);//GC2 explicitley determines the ordered 2D convex hull
        //            var volume = minArea * length;
        //            if (volume < minVolume) minVolume = volume;

        //            //Set delta, where delta is the angle from the original orientation to the next intersection
        //            delta = delta;//+ intersections[0].angle

        //            while (theta <= delta) //Optimize the volume based on changes is projection of the 2D Bounding Box.;
        //            {
        //                //If the 2D bounding box changes vertices with the projection, then we will need RotatingCalipers
        //                //Else, project the same four'ish vertices and recalculate area.  
        //                if (volume < minVolume) minVolume = volume;
        //            }

        //            //After delta is reached, update the volume parameters of either length or cross sectional area (convex hull points).
        //            //If the closest intersection is on GC1, recalculate length.
        //            if (intersections[0].GC = GC1) 
        //            {
        //                length = StarMath.magnitude(arc.Nodes[0].Vector.subtract(intersections[0].Vertex));
        //            }
        //            else
        //            {
        //                var node = intersections[0].Node;
        //                foreach (var arc in node.Arcs)
        //                {
        //                      if (GC2.arcList.Contains(arc) ?  GC2.arcList.remove(arc): GC2.arcList.add(arc);     
        //                }
        //            }
        //        } while (delta <= Maxtheta);
        //    }
        //    return minBox;
        //}

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
        public static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction = null)
        {
            Vertex v1Low, v1High;
            var length = GetLengthAndExtremeVertices(direction, vertices, out v1Low, out v1High);
            double[,] backTransform;
          MiscFunctions.  Get2DProjectionPoints(vertices, direction, out backTransform);

            double minArea;
            var rotateZ = StarMath.RotationZ(RotatingCalipers2DMethod(vertices.Select(v => new Point(v)).ToArray(), out minArea));
            backTransform = backTransform.multiply(rotateZ);
            var dirVectorPlusZero = backTransform.GetColumn(0);
            var nx = new[] { dirVectorPlusZero[0], dirVectorPlusZero[1], dirVectorPlusZero[2] };
            /* temporarily check that nx is the same as direction */
            if (!nx.SequenceEqual(direction)) throw new Exception();
            dirVectorPlusZero = backTransform.GetColumn(1);
            var ny = new[] { dirVectorPlusZero[0], dirVectorPlusZero[1], dirVectorPlusZero[2] };
            dirVectorPlusZero = backTransform.GetColumn(2);
            var nz = new[] { dirVectorPlusZero[0], dirVectorPlusZero[1], dirVectorPlusZero[2] };
            Vertex v2Low, v2High;
            GetLengthAndExtremeVertices(ny, vertices, out v2Low, out v2High);
            Vertex v3Low, v3High;
            GetLengthAndExtremeVertices(nz, vertices, out v3Low, out v3High);
            return new BoundingBox(length * minArea, new[] { v1Low, v1High, v2Low, v2High, v3Low, v3High }, new[] { direction, ny, nz });
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
        public static double GetLengthAndExtremeVertices(double[] dir, IList<Vertex> vertices, out Vertex vLow, out Vertex vHigh)
        {
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
        /// Rotating the calipers2 d method.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="minArea">The minimum area.</param>
        /// <returns>System.Double.</returns>
        public static double RotatingCalipers2DMethod(IList<Point> points, out double minArea)
        {

            #region Initialization
            var cvxPoints = ConvexHull2D(points);
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
            var deltaToUpdateIndex = -1;
            var deltaAngles = new double[4];
            var offsetAngles = new[] { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };
            minArea = double.PositiveInfinity;
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

                var angleVector = new[] { -direction[1], direction[0] };
                var width = Math.Abs(vectorWidth.dotProduct(angleVector));
                var vectorHeight = new[]
                { 
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0], 
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                angleVector = new[] { direction[0], direction[1] };
                var height = Math.Abs(vectorHeight.dotProduct(angleVector));
                var tempArea = height * width;
                #endregion
                if (minArea > tempArea)
                {
                    minArea = tempArea;
                    bestAngle = angle;
                }
            } while (angle < Math.PI / 2); //Don't check beyond a 90 degree angle.
            //If best angle is 90 degrees, then don't bother to rotate. 
            if (bestAngle == Math.PI / 2) { bestAngle = 0; }
            #endregion

            return bestAngle;
        }


        /// <summary>
        /// Rotating the calipers, based on the given gaussian circle.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="minArea">The minimum area.</param>
        /// <returns>System.Double.</returns>
        //public static double RotatingCalipers2DMethod(GreatCircle greatCircle, out double minArea)
        //{
            //create a list of vertices from the list of arcs in GC2  
            //GC2 explicitley determines the ordered 2D convex hull
        //    return bestAngle;
        //}
    }
}