// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 03-05-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 05-28-2016
// ***********************************************************************
// <copyright file="RefineTessellation.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using StarMathLib;
using System.Collections.Generic;
using System.Linq;

namespace TVGL
{
    /// <summary>
    ///  This portion of ModifyTessellation includes the functions to refine a solid, which means 
    ///  adding more elements to it. invoked during the opening of a tessellated solid from "disk", but the repair function
    ///  may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        const double MaxDistanceFactor = 3.0;

        /// <summary>
        ///     Adjusts the position of kept vertex.
        /// </summary>
        /// <param name="vertexA">The keep vertex.</param>
        /// <param name="vertexB">The other vertex.</param>
        internal static double[] DetermineIntermediateVertexPosition(Vertex vertexA, Vertex vertexB)
        {
            //average positions
            var newPosition = vertexA.Position.add(vertexB.Position);
            return newPosition.divide(2);
        }

        private static bool DetermineIntermediateVertexPosition(Edge edge, out double[] position,
            IEnumerable<PrimitiveSurface> primitives)
        {
            position = null;
            if (primitives == null || !primitives.Any())
            {
                position = DetermineIntermediateVertexPosition(edge.To, edge.From);
                return true;
            }
            if (primitives.Count() >= 3)
            {
                return false;
            }
            var edgeUnitVector = edge.Vector.normalize();
            if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Flat &&
             edgeUnitVector.dotProduct(((Flat)p).Normal, 3).IsPracticallySame(1.0))) return false;
            if (primitives.Count() == 2)
            {
                #region Two Primitives of the Same Type (5 possibilities)
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Flat))
                {
                    var flat1 = (Flat)primitives.First();
                    var flat2 = (Flat)primitives.Last();
                    if (flat1.Normal.dotProduct(flat2.Normal, 3).IsPracticallySame(1.0)) return false;
                    var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                    position = MiscFunctions.PointCommonToThreePlanes(edgeUnitVector,
                        midpoint.dotProduct(edgeUnitVector),
                        flat1.Normal, flat1.DistanceToOrigin, flat2.Normal, flat2.DistanceToOrigin);
                    if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                        return false;
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Cylinder))
                {
                    var cyl1 = (Cylinder)primitives.First();
                    var cyl2 = (Cylinder)primitives.Last();
                    var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                    throw new NotImplementedException();
                    if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                        return false;
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Sphere))
                {
                    var s1 = (Sphere)primitives.First();
                    var s2 = (Sphere)primitives.Last();
                    var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                    throw new NotImplementedException();
                    if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                        return false;
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Cone))
                {
                    var c1 = (Cone)primitives.First();
                    var c2 = (Cone)primitives.Last();
                    var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                    throw new NotImplementedException();
                    if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                        return false;
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Torus))
                {
                    var t1 = (Torus)primitives.First();
                    var t2 = (Torus)primitives.Last();
                    var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                    throw new NotImplementedException();
                    if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                        return false;
                }
                #endregion
                #region Two Different Primitives (25 possibilities!)

                #endregion
            }

            #region One Primitives (5 possibilities)
            if (primitives.First().Type == PrimitiveSurfaceType.Flat)
            {
                var flat1 = (Flat)primitives.First();
                var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                double[] pointOnLine, directionOfLine;
                MiscFunctions.LineIntersectingTwoPlanes(edgeUnitVector,
                    midpoint.dotProduct(edgeUnitVector), flat1.Normal, flat1.DistanceToOrigin, out directionOfLine, out pointOnLine);
                MiscFunctions.DistancePointToLine(midpoint, pointOnLine, directionOfLine, out position);
                if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                    return false;
            }
            if (primitives.First().Type == PrimitiveSurfaceType.Cylinder)
            {
                var cyl1 = (Cylinder)primitives.First();
                var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                double[] pointOnAxis;
                MiscFunctions.DistancePointToLine(midpoint, cyl1.Anchor, cyl1.Axis, out pointOnAxis);
                var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                position = pointOnAxis.add(unitDirection.multiply(cyl1.Radius));
                if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                    return false;
            }
            if (primitives.First().Type == PrimitiveSurfaceType.Sphere)
            {
                var s1 = (Sphere)primitives.First();
                var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                var unitDirection = midpoint.subtract(s1.Center).normalize();
                position = s1.Center.add(unitDirection.multiply(s1.Radius));
                if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                    return false;
            }
            if (primitives.First().Type == PrimitiveSurfaceType.Cone)
            {
                var cone = (Cone)primitives.First();
                var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                double[] pointOnAxis;
                MiscFunctions.DistancePointToLine(midpoint, cone.Apex, cone.Axis, out pointOnAxis);
                var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                var radius =Math.Tan(cone.Aperture/2)*(pointOnAxis.subtract(cone.Apex).norm2());
                position = pointOnAxis.add(unitDirection.multiply(radius));
                if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                    return false;
            }
            if (primitives.First().Type == PrimitiveSurfaceType.Torus)
            {
                var t1 = (Torus)primitives.First();
                var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
                throw new NotImplementedException();
                if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                    return false;
            }
            #endregion
            position = null;
            return false;
        }

        /// <summary>
        ///     Adjusts the position of kept vertex experimental.
        /// </summary>
        /// <param name="keepVertex">The keep vertex.</param>
        /// <param name="removedVertex">The removed vertex.</param>
        /// <param name="removeFace1">The remove face1.</param>
        /// <param name="removeFace2">The remove face2.</param>
        internal static void AdjustPositionOfKeptVertexExperimental(Vertex keepVertex, Vertex removedVertex,
            PolygonalFace removeFace1, PolygonalFace removeFace2)
        {
            //average positions
            var newPosition = keepVertex.Position.add(removedVertex.Position);
            var radius = keepVertex.Position.subtract(removedVertex.Position).norm2() / 2.0;
            keepVertex.Position = newPosition.divide(2);
            var avgNormal = removeFace1.Normal.add(removeFace2.Normal).normalize();
            var otherVertexAvgDistanceToEdgePlane =
                keepVertex.Edges.Select(e => e.OtherVertex(keepVertex).Position.dotProduct(avgNormal)).Sum() /
                (keepVertex.Edges.Count - 1);
            var distanceOfEdgePlane = keepVertex.Position.dotProduct(avgNormal);

            // use a sigmoid function to determine how far out to move the vertex
            var x = 0.05 * (distanceOfEdgePlane - otherVertexAvgDistanceToEdgePlane) / radius;
            var length = 2 * radius * x / Math.Sqrt(1 + x * x) - radius;
            keepVertex.Position = keepVertex.Position.add(avgNormal.multiply(length));
        }
    }
}