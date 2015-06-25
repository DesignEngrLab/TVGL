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
using MIConvexHull;
using StarMathLib;
using TVGL.Tessellation;

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
        /// Orienteds the bounding box.
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


        private static BoundingBox Find_via_PCA_Approach(TessellatedSolid ts)
        {
            throw new NotImplementedException();

        }

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

        private static BoundingBox Find_via_MC_ApproachTwo(TessellatedSolid ts)
        {
            throw new NotImplementedException();

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
        public static BoundingBox FindOBBAlongDirection(IList<Vertex> vertices, double[] direction = null)
        {
            Vertex v1Low, v1High;
            var length = GetLengthAndExtremeVertices(direction, vertices, out v1Low, out v1High);
            double[,] backTransform;
            Get2DProjectionPoints(vertices, direction, out backTransform);

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
        /// Returns the positions (array of 3D arrays) of the vertices as that they would be represented in 
        /// the x-y plane (although the z-values will be non-zero). This does not destructively alter
        /// the vertices. 
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[] direction)
        {
            var xDir = direction[0];
            var yDir = direction[1];
            var zDir = direction[2];

            var rotateY = StarMath.RotationY(-Math.Atan(xDir / zDir), true);
            var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
            var rotateX = StarMath.RotationX(Math.Atan(yDir / baseLength), true);
            var transform = rotateX.multiply(rotateY);


            var points = new Point[vertices.Count];
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            for (var i = 0; i < vertices.Count; i++)
            {
                pointAs4[0] = vertices[i].Position[0];
                pointAs4[1] = vertices[i].Position[1];
                pointAs4[2] = vertices[i].Position[2];
                pointAs4 = transform.multiply(pointAs4);
                points[i] = new Point(vertices[i], pointAs4[0], pointAs4[1], pointAs4[2]);
            }
            return points;
        }

        /// <summary>
        /// Returns the positions (array of 3D arrays) of the vertices as that they would be represented in 
        /// the x-y plane (although the z-values will be non-zero). This does not destructively alter
        /// the vertices. 
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="direction">The direction.</param>
        /// <returns>Point2D[].</returns>
        public static Point[] Get2DProjectionPoints(IList<Vertex> vertices, double[] direction, out double[,] backTransform)
        {
            var xDir = direction[0];
            var yDir = direction[1];
            var zDir = direction[2];

            var rotateY = StarMath.RotationY(-Math.Atan(xDir / zDir), true);
            var backRotateY = StarMath.RotationY(Math.Atan(xDir / zDir), true);
            var baseLength = Math.Sqrt(xDir * xDir + zDir * zDir);
            var rotateX = StarMath.RotationX(Math.Atan(yDir / baseLength), true);
            var backRotateX = StarMath.RotationX(-Math.Atan(yDir / baseLength), true);
            var transform = rotateX.multiply(rotateY);
            backTransform = backRotateY.multiply(backRotateX);


            var points = new Point[vertices.Count];
            var pointAs4 = new[] { 0.0, 0.0, 0.0, 1.0 };
            for (var i = 0; i < vertices.Count; i++)
            {
                pointAs4[0] = vertices[i].Position[0];
                pointAs4[1] = vertices[i].Position[1];
                pointAs4[2] = vertices[i].Position[2];
                pointAs4 = transform.multiply(pointAs4);
                points[i] = new Point(vertices[i], pointAs4[0], pointAs4[1], pointAs4[2]);
            }
            return points;
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
        /// Rotatings the calipers2 d method.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="minArea">The minimum area.</param>
        /// <returns>System.Double.</returns>
        private static double RotatingCalipers2DMethod(IList<Point> points, out double minArea)
        {      
            #region Initialization
            var cvxPoints = ConvexHull2D(points);
            var numCvxPoints = cvxPoints.Count;
            /* the cvxPoints are counter-clockwise starting with a vertex that is minimum in X. */
            var extremeIndices = new int[4];
            extremeIndices[3] = cvxPoints.Count;
            do extremeIndices[3]--;
            while (extremeIndices[3] >= 1 && cvxPoints[extremeIndices[3]][1] <= cvxPoints[extremeIndices[3] - 1][1]);
            extremeIndices[2] = extremeIndices[3];
            while (extremeIndices[2] >= 1 && cvxPoints[extremeIndices[2]][0] <= cvxPoints[extremeIndices[2] - 1][0])
                extremeIndices[2]--;
            extremeIndices[1] = extremeIndices[2];
            while (extremeIndices[1] >= 1 && cvxPoints[extremeIndices[1]][1] >= cvxPoints[extremeIndices[1] - 1][1])
                extremeIndices[1]--;
            extremeIndices[0] = extremeIndices[1];
            while (extremeIndices[0] >= 1 && cvxPoints[extremeIndices[0]][0] >= cvxPoints[extremeIndices[0] - 1][0])
                extremeIndices[0]--;
            #endregion
            #region Cycle through 90-degrees
            var angle = 0.0;
            var bestAngle = double.NegativeInfinity;
            var deltaToUpdate = -1;
            var deltaAngles = new double[4];
            var offsetAngles = new[] { Math.PI / 2, Math.PI, -Math.PI / 2, 0.0 };
            minArea = double.PositiveInfinity;
            do
            {
                for (var i = 0; i < 4; i++)
                {
                    if (deltaToUpdate == -1 || i == deltaToUpdate)
                    {
                        var index = extremeIndices[i];
                        var prev = (index == 0) ? numCvxPoints - 1 : index - 1;
                        var tempDelta = Math.Atan2(cvxPoints[prev][1] - cvxPoints[index][1],
                             cvxPoints[prev][0] - cvxPoints[index][0]);
                        //if (tempDelta < 0) tempDelta += 2 * Math.PI;
                        deltaAngles[i] = offsetAngles[i] - tempDelta;
                    }
                }
                var delta = deltaAngles.Min();
                deltaToUpdate = deltaAngles.FindIndex(delta);
                extremeIndices[deltaToUpdate]--;
                if (extremeIndices[deltaToUpdate] < 0) extremeIndices[deltaToUpdate] = numCvxPoints - 1;
                angle += delta;
                var sinAngle = Math.Sin(angle);
                var cosAngle = Math.Cos(angle);
                var vectorWidth = new[]
                {
                    cvxPoints[extremeIndices[2]][0] - cvxPoints[extremeIndices[0]][0],
                    cvxPoints[extremeIndices[2]][1] - cvxPoints[extremeIndices[0]][1]
                };
                var angleVector = new[] { cosAngle, sinAngle };
                var tempArea = Math.Abs(vectorWidth.dotProduct(angleVector));
                var vectorHeight = new[]
                { 
                    cvxPoints[extremeIndices[3]][0] - cvxPoints[extremeIndices[1]][0], 
                    cvxPoints[extremeIndices[3]][1] - cvxPoints[extremeIndices[1]][1]
                };
                angleVector = new[] { -sinAngle, cosAngle };
                tempArea *= Math.Abs(vectorHeight.dotProduct(angleVector));
                if (minArea > tempArea)
                {
                    minArea = tempArea;
                    bestAngle = angle;
                }
            } while (angle <= Math.PI / 2);
            #endregion

            return bestAngle;
        }
    }
}