// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="DetermineIntermediateVertex.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Linq;


namespace TVGL
{
    /// <summary>
    /// This portion of ModifyTessellation includes the functions to refine a solid, which means
    /// adding more elements to it. invoked during the opening of a tessellated solid from "disk", but the repair function
    /// may be called on its own.
    /// </summary>
    public static partial class ModifyTessellation
    {
        /// <summary>
        /// Adjusts the position of kept vertex.
        /// </summary>
        /// <param name="vertexA">The keep vertex.</param>
        /// <param name="vertexB">The other vertex.</param>
        /// <returns>Vector3.</returns>
        internal static Vector3 DetermineIntermediateVertexPosition(Vector3 pt1, Vector3 pt2,
            PrimitiveSurface primitive = null)
        {
            if (primitive == null || primitive is Plane) // then average positions
                return 0.5 * (pt1 + pt2);
            //else we're going to project the average position onto the primitive surface
            if (primitive is Sphere sphere)
                // the direction from the center to the average of the two points is the direction to
                // move out to the sphere surface. Note we can cheese the average by the Normalize
                // function because the direction is all we care about, but we need to find the
                // relative movements so, the points are each subtracted from the center
                return sphere.Center + sphere.Radius * (pt1 + pt2 - 2 * sphere.Center).Normalize();
            if (primitive is Cylinder cylinder)
            {
                var anchorToPt1 = pt1 - cylinder.Anchor;
                var anchorToPt2 = pt2 - cylinder.Anchor;
                var axisT1 = anchorToPt1.Dot(cylinder.Axis);
                var pt1DiskCenter = cylinder.Anchor + axisT1 * cylinder.Axis;
                var axisT2 = anchorToPt2.Dot(cylinder.Axis);
                var pt2DiskCenter = cylinder.Anchor + axisT2 * cylinder.Axis;
                var diskCenter = 0.5 * (pt1DiskCenter + pt2DiskCenter);
                // following sphere example above
                return diskCenter + cylinder.Radius * (pt1 - pt1DiskCenter + pt2 - pt2DiskCenter).Normalize();
            }
            if (primitive is Cone cone)
            {
                var anchorToPt1 = pt1 - cone.Apex;
                var anchorToPt2 = pt2 - cone.Apex;
                var axisT1 = anchorToPt1.Dot(cone.Axis);
                var pt1DiskCenter = cone.Apex + axisT1 * cone.Axis;
                var pt1Radius = axisT1 * cone.Aperture;
                var axisT2 = anchorToPt2.Dot(cone.Axis);
                var pt2DiskCenter = cone.Apex + axisT2 * cone.Axis;
                var diskHeight = 0.5 * (axisT1 + axisT2);
                var diskRadius = diskHeight * cone.Aperture;
                var diskCenter = 0.5 * (pt1DiskCenter + pt2DiskCenter);
                // like cylinder, however we have to be careful since the distances outwards
                // from the 2 given points may be different, so these directions should be normalized
                // before adding. Well, we're going to normalize at the end, so only need to ensure
                // they are the same relative length. Instead of divide, we multiply the other's
                // length.
                return diskCenter + diskRadius * (axisT2 * (pt1 - pt1DiskCenter) +
                 axisT1 * (pt2 - pt2DiskCenter)).Normalize();
            }
            if (primitive is GeneralQuadric quadric)
            {
                var p1Normal = quadric.GetNormalAtPoint(pt1);
                var p2Normal = quadric.GetNormalAtPoint(pt2);
                var pCenter = 0.5 * (pt1 + pt2);
                var planeNormal = (p1Normal.Cross(p2Normal)).Normalize();
                var outwardDir = (pt2 - pt1).Cross(planeNormal);
                // the point we seek is farthest from the line connecting pt1 to pt2
                // we don't know exactly where it is along the line, but we know its
                // normal should be inline with the outwardDir.
                return quadric.PointsWithGradient(outwardDir).MinBy(q => q.DistanceSquared(pCenter));
            }
            if (primitive is Torus torus)
            {
                var anchorToPt1 = pt1 - torus.Center;
                var anchorToPt2 = pt2 - torus.Center;
                var ringPt1 = torus.ClosestPointOnSurfaceToPoint(pt1);
                var ringPt2 = torus.ClosestPointOnSurfaceToPoint(pt2);
                throw new NotImplementedException();
            }
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adjusts the position of kept vertex experimental.
        /// </summary>
        /// <param name="keepVertex">The keep vertex.</param>
        /// <param name="removedVertex">The removed vertex.</param>
        /// <param name="removeFace1">The remove face1.</param>
        /// <param name="removeFace2">The remove face2.</param>
        internal static void AdjustPositionOfKeptVertexExperimental(Vertex keepVertex, Vertex removedVertex,
            TriangleFace removeFace1, TriangleFace removeFace2)
        {
            //average positions
            var newPosition = keepVertex.Coordinates + removedVertex.Coordinates;
            var radius = keepVertex.Coordinates.Distance(removedVertex.Coordinates) / 2.0;
            keepVertex.Coordinates = newPosition.Divide(2);
            var avgNormal = (removeFace1.Normal + removeFace2.Normal).Normalize();
            var otherVertexAvgDistanceToEdgePlane =
                keepVertex.Edges.Select(e => e.OtherVertex(keepVertex).Coordinates.Dot(avgNormal)).Sum() /
                (keepVertex.Edges.Count - 1);
            var distanceOfEdgePlane = keepVertex.Coordinates.Dot(avgNormal);

            // use a sigmoid function to determine how far out to move the vertex
            var x = 0.05 * (distanceOfEdgePlane - otherVertexAvgDistanceToEdgePlane) / radius;
            var length = 2 * radius * x / Math.Sqrt(1 + x * x) - radius;
            keepVertex.Coordinates = keepVertex.Coordinates + (avgNormal * length);
        }
    }
}