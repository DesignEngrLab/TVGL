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

        internal static Vector3 DetermineIntermediateVertexPosition(Edge edge)
        {
            var fromVertex = edge.From;
            var toVertex = edge.To;
            var primitive1 = edge.OwnedFace?.BelongsToPrimitive;
            var primitive2 = edge.OtherFace == null ? primitive1 : edge.OtherFace.BelongsToPrimitive;
            if (primitive1 == null) primitive1 = primitive2;
            if (primitive1 == primitive2)
                return DetermineIntermediateVertexPosition(edge.From.Coordinates, edge.To.Coordinates, primitive1);
            else
                return DetermineIntermediateVertexPosition(edge.From.Coordinates, edge.To.Coordinates, primitive1, primitive2);
        }

        internal static Vector3 DetermineIntermediateVertexPosition(Vector3 pt1, Vector3 pt2,
            PrimitiveSurface primitive)
        {
            if (primitive == null || primitive is Plane) // then average positions
                return 0.5 * (pt1 + pt2);
            //else we're going to project the average position onto the primitive surface
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
            {   // this one is a bit complicated. The obvious solution to divide the line in half and then project 
                // it to the quadric is not the best, but it is kept here as a fallback at the end. Instead, we first
                // find the plane made by the normal of the midpoint and the original line segment (it would seem better
                // to use the endpoints' normals, but this was not the case). From this plane, we slice to find the conic
                // on that plane made by the quadric, then find the point on the curve between the end points that is farthest
                // from the original line. This point has the gradient (normal of the conic) defined by the outwardDirc
                var midPoint = 0.5 * (pt1 + pt2);
                var newPoint = quadric.ClosestPointOnSurfaceToPoint(midPoint);
                if (!newPoint.IsNull())
                    return newPoint;
                else return midPoint;
                /*
                if (!quadric.QuadricValue(pt1).IsNegligible() || !quadric.QuadricValue(pt2).IsNegligible())
                    return midPoint;
                var prevVector = pt2 - pt1; //line from-to of orig edge
                //var midPtNormal = quadric.GetNormalAtPoint(midPoint);
                var outwardNormal = (quadric.GetNormalAtPoint(pt1)
                    + quadric.GetNormalAtPoint(pt2)).Normalize();
                var planenormal = outwardNormal.Cross(prevVector).Normalize();
                outwardNormal = prevVector.Cross(planenormal).Normalize();
                var plane = new Plane(planenormal.Dot(pt1), planenormal);
                // this is the plane in which both pt1 and pt2 reside
                // switch to the conic produced on this plane
                var conic = GeneralConicSection.CreateFromQuadric(quadric, plane);
                if (conic.CurveType == PrimitiveCurveType.StraightLine)
                    return midPoint;
                var p12D = pt1.ConvertTo2DCoordinates(planenormal, out _);
                var p22D = pt2.ConvertTo2DCoordinates(planenormal, out _);

                var idealCaseSuccess = conic.CurveType != PrimitiveCurveType.Hyperbola
                    || conic.PointsOnSameHyperbolaBranch(p12D, p22D);
                var newPoint = Vector3.Null;
                if (idealCaseSuccess)
                {
                    var outwarddir2d = outwardNormal.ConvertTo2DCoordinates(planenormal, out _);
                    var lineDir2D = (p22D - p12D).Normalize();
                    // we want the point that is farthest from the line, and the outwardDir is
                    // perpendicular to the line so the point has the gradient in this same dir
                    // but we're actually going to convert to 2D and solve for it there
                    var posSuccess = conic.PointsAtGivenGradient(outwarddir2d, out var posPt);
                    var negSuccess = conic.PointsAtGivenGradient(-outwarddir2d, out var negPt);
                    if (posSuccess && (!negSuccess || MiscFunctions.DistancePointToLine(posPt, p12D, lineDir2D, out _)
                        <= MiscFunctions.DistancePointToLine(negPt, p12D, lineDir2D, out _)))
                        newPoint = posPt.ConvertTo3DLocation(plane.AsTransformFromXYPlane);
                    else if (negSuccess)
                        newPoint = negPt.ConvertTo3DLocation(plane.AsTransformFromXYPlane);
                }
                else if (quadric.Type == QuadricType.HyperboloidOneSheet)
                {
                    // if the points are on different branches of a hyperbola,
                    // then we can't use this method since there is no point between them
                    // on the conic. Instead, we project to the plane that would slice the
                    // hyperboloid in halves
                    var oddAxis = quadric.OddAxis;
                    var stationaryPt = quadric.StationaryPoint;
                    newPoint = MiscFunctions.PointOnPlaneFromLine(oddAxis, oddAxis.Dot(stationaryPt), midPoint,
                        oddAxis, out _);
                }
                //else return 0.5 * (pt1 + pt2);

                if (!(newPoint.DistanceSquared(midPoint) < 0.5*pt1.DistanceSquared(pt2)))
                    // this is written as negative to catch when newPoint is null as well
                    newPoint = midPoint;
                return newPoint;
                */
            }
            if (primitive is Sphere sphere)
                // the direction from the center to the average of the two points is the direction to
                // move out to the sphere surface. Note we can cheese the average by the Normalize
                // function because the direction is all we care about, but we need to find the
                // relative movements so, the points are each subtracted from the center
                return sphere.Center + sphere.Radius * (pt1 + pt2 - 2 * sphere.Center).Normalize();
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



        private static Vector3 DetermineIntermediateVertexPosition(Vector3 coordinates1, Vector3 coordinates2, PrimitiveSurface primitive1, PrimitiveSurface primitive2)
        {
            return Vector3.Null;
            //throw new NotImplementedException();
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