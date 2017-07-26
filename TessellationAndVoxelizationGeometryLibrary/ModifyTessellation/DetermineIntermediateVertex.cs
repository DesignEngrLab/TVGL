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
using System.Diagnostics;
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
            var midpoint = DetermineIntermediateVertexPosition(edge.From, edge.To);
            position = null;
            #region no primitives
            if (primitives == null || !primitives.Any())
            {
                position = midpoint;
                return true;
            }
            #endregion
            #region More than 3 primitives
            if (primitives.Count() > 3)
            {
                return false;
            }
            #endregion
            #region Three Primitives
            if (primitives.Count() == 3)
            {
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Flat))
                {
                    var flats = primitives.Cast<Flat>().ToArray();
                    if (RedundantSurfaces(flats[0], flats[1])) return false;
                    if (RedundantSurfaces(flats[1], flats[2])) return false;
                    if (RedundantSurfaces(flats[2], flats[0])) return false;
                    position = MiscFunctions.PointCommonToThreePlanes(flats[0].Normal, flats[0].DistanceToOrigin,
                        flats[1].Normal, flats[1].DistanceToOrigin,
                        flats[2].Normal, flats[2].DistanceToOrigin);
                    return WithinReasonableDistanceToMidPoint(midpoint, position, edge);
                }
                Debug.WriteLine("Not implemented: find intersection between: " +
                    string.Join(", ", primitives.Select(p => p.Type.ToString())));
                position = midpoint;
                return true;
            }
            #endregion
            if (primitives.Count() == 2)
            {
                #region Two Primitives of the Same Type (5 possibilities)
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Flat))
                {
                    var edgeUnitVector = edge.Vector.normalize();
                    var flat1 = (Flat)primitives.First();
                    var flat1Normal = flat1.Normal;
                    var flat1Distance = flat1.DistanceToOrigin;
                    var flat2 = (Flat)primitives.Last();
                    var flat2Normal = flat2.Normal;
                    var flat2Distance = flat2.DistanceToOrigin;
                    var onlyOneViableFace = false;
                    if (RedundantSurfaces(flat1, flat2)) return false;
                    if (RedundantSurfaces(flat1, edgeUnitVector))
                    {
                        flat1Normal = edgeUnitVector;
                        flat1Distance = midpoint.dotProduct(edgeUnitVector);
                        onlyOneViableFace = true;
                    }
                    else if (RedundantSurfaces(flat2, edgeUnitVector))
                    {
                        flat2Normal = edgeUnitVector;
                        flat2Distance = midpoint.dotProduct(edgeUnitVector);
                        onlyOneViableFace = true;
                    }
                    if (onlyOneViableFace)
                    {
                        double[] pointOnLine, directionOfLine;
                        MiscFunctions.LineIntersectingTwoPlanes(flat1Normal, flat1Distance, flat2Normal, flat2Distance,
                            out directionOfLine, out pointOnLine);
                        MiscFunctions.DistancePointToLine(midpoint, pointOnLine, directionOfLine, out position);
                    }
                    else
                        position = MiscFunctions.PointCommonToThreePlanes(edgeUnitVector,
                         midpoint.dotProduct(edgeUnitVector),
                         flat1.Normal, flat1.DistanceToOrigin, flat2.Normal, flat2.DistanceToOrigin);
                    return WithinReasonableDistanceToMidPoint(midpoint, position, edge);
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Cylinder))
                {
                    var cyl1 = (Cylinder)primitives.First();
                    var cyl2 = (Cylinder)primitives.Last();
                    position = DetermineIntermediateVertexPosition(edge.To, edge.From);
                    return true;
                    throw new NotImplementedException();
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Sphere))
                {
                    var s1 = (Sphere)primitives.First();
                    var s2 = (Sphere)primitives.Last();
                    position = DetermineIntermediateVertexPosition(edge.To, edge.From);
                    return true;
                    throw new NotImplementedException();
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Cone))
                {
                    var c1 = (Cone)primitives.First();
                    var c2 = (Cone)primitives.Last();
                    position = DetermineIntermediateVertexPosition(edge.To, edge.From);
                    return true;
                    throw new NotImplementedException();
                }
                if (primitives.All(p => p.Type == PrimitiveSurfaceType.Torus))
                {
                    var t1 = (Torus)primitives.First();
                    var t2 = (Torus)primitives.Last();
                    position = DetermineIntermediateVertexPosition(edge.To, edge.From);
                    return true;
                    throw new NotImplementedException();
                }
                #endregion
                #region Two Different Primitives (10 possibilities!)
                if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Flat))
                {
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cylinder))
                    {
                        var flat = primitives.First(p => p.Type == PrimitiveSurfaceType.Flat) as Flat;
                        var cyl = primitives.First(p => p.Type == PrimitiveSurfaceType.Cylinder) as Cylinder;
                        double[] pointOnAxis;
                        MiscFunctions.DistancePointToLine(midpoint, cyl.Anchor, cyl.Axis, out pointOnAxis);
                        var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                        position = pointOnAxis.add(unitDirection.multiply(cyl.Radius));
                        position = MiscFunctions.PointOnPlaneFromRay(flat.Normal, flat.DistanceToOrigin, position, cyl.Axis);
                        return WithinReasonableDistanceToMidPoint(midpoint, position, edge);
                    }
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Sphere))
                    {
                        var edgeUnitVector = edge.Vector.normalize();
                        var flat = primitives.First(p => p.Type == PrimitiveSurfaceType.Flat) as Flat;
                        var sphere = primitives.First(p => p.Type == PrimitiveSurfaceType.Sphere) as Sphere;
                        var distanceFromCenter = midpoint.subtract(sphere.Center).norm2();
                        var distanceToGo = sphere.Radius - distanceFromCenter;
                        var direction = flat.Normal.crossProduct(edgeUnitVector).normalize(3);
                        position = midpoint.add(direction.multiply(distanceToGo));
                        return WithinReasonableDistanceToMidPoint(midpoint, position, edge);
                    }
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cone))
                    {
                        var flat = primitives.First(p => p.Type == PrimitiveSurfaceType.Flat) as Flat;
                        var cone = primitives.First(p => p.Type == PrimitiveSurfaceType.Cone) as Cone;
                        double[] pointOnAxis;
                        MiscFunctions.DistancePointToLine(midpoint, cone.Apex, cone.Axis, out pointOnAxis);
                        var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                        var radius = Math.Tan(cone.Aperture / 2) * (pointOnAxis.subtract(cone.Apex).norm2());
                        position = pointOnAxis.add(unitDirection.multiply(radius));
                        var vectorAlongCone = position.subtract(cone.Apex);
                        position = MiscFunctions.PointOnPlaneFromRay(flat.Normal, flat.DistanceToOrigin, position, vectorAlongCone);
                        return WithinReasonableDistanceToMidPoint(midpoint, position, edge);
                    }
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Torus))
                    {
                        throw new NotImplementedException();
                    }
                }
                if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cylinder))
                {
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Sphere))
                    { }

                    else if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cone))
                    { }
                    else if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Torus))
                    {
                        throw new NotImplementedException();
                    }
                }
                if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Sphere))
                {
                    if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cone))
                    { }
                    else if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Torus))
                    {
                        throw new NotImplementedException();
                    }
                }
                if (primitives.Any(p => p.Type == PrimitiveSurfaceType.Cone)
                    && primitives.Any(p => p.Type == PrimitiveSurfaceType.Torus))
                {
                    throw new NotImplementedException();
                }
                #endregion
            }
            #region One Primitives (5 possibilities)
            if (primitives.Count() == 1)
            {
                if (primitives.First().Type == PrimitiveSurfaceType.Flat)
                {
                    var edgeUnitVector = edge.Vector.normalize();
                    var flat1 = (Flat)primitives.First();
                    if (RedundantSurfaces(flat1, edgeUnitVector)) return false;
                    if (flat1.Normal.dotProduct(edgeUnitVector, 3).IsNegligible())
                    {
                        position = midpoint;
                        return true;
                    }
                    double[] pointOnLine, directionOfLine;
                    MiscFunctions.LineIntersectingTwoPlanes(edgeUnitVector,
                        midpoint.dotProduct(edgeUnitVector), flat1.Normal, flat1.DistanceToOrigin, out directionOfLine,
                                    out pointOnLine);
                    MiscFunctions.DistancePointToLine(midpoint, pointOnLine, directionOfLine, out position);
                }
                if (primitives.First().Type == PrimitiveSurfaceType.Cylinder)
                {
                    var cyl1 = (Cylinder)primitives.First();
                    double[] pointOnAxis;
                    MiscFunctions.DistancePointToLine(midpoint, cyl1.Anchor, cyl1.Axis, out pointOnAxis);
                    var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                    position = pointOnAxis.add(unitDirection.multiply(cyl1.Radius));
                }
                if (primitives.First().Type == PrimitiveSurfaceType.Sphere)
                {
                    var s1 = (Sphere)primitives.First();
                    var unitDirection = midpoint.subtract(s1.Center).normalize();
                    position = s1.Center.add(unitDirection.multiply(s1.Radius));
                }
                if (primitives.First().Type == PrimitiveSurfaceType.Cone)
                {
                    var cone = (Cone)primitives.First();
                    double[] pointOnAxis;
                    MiscFunctions.DistancePointToLine(midpoint, cone.Apex, cone.Axis, out pointOnAxis);
                    var unitDirection = midpoint.subtract(pointOnAxis).normalize();
                    var radius = Math.Tan(cone.Aperture / 2) * (pointOnAxis.subtract(cone.Apex).norm2());
                    position = pointOnAxis.add(unitDirection.multiply(radius));
                }
                if (primitives.First().Type == PrimitiveSurfaceType.Torus)
                {
                    var t1 = (Torus)primitives.First();
                    throw new NotImplementedException();
                }
            }

            #endregion
            if (position == null || position.Any(x => double.IsNaN(x))) return false;
            if (midpoint.subtract(position).norm2() > MaxDistanceFactor * edge.Length)
                return false;
            return true;
        }

        private static bool WithinReasonableDistanceToMidPoint(double[] midpoint, double[] position,
            Edge edge)
        {
            if (position == null || position.Any(x => double.IsNaN(x))) return false;
            return (midpoint.subtract(position).norm2() <= MaxDistanceFactor * edge.Length);
        }

        static bool RedundantSurfaces(Flat f1, Flat f2)
        {
            return ((Math.Abs(f1.Normal.dotProduct(f2.Normal, 3))).IsPracticallySame(1.0));
        }
        static bool RedundantSurfaces(Flat f1, double[] normal)
        {
            return ((Math.Abs(f1.Normal.dotProduct(normal, 3))).IsPracticallySame(1.0));
        }
        static bool RedundantSurfaces(Cylinder c1, Cylinder c2)
        {
            return ((Math.Abs(c1.Axis.dotProduct(c2.Axis, 3))).IsPracticallySame(1.0));
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