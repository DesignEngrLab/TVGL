// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="CylindricalBuffer.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Class CylindricalBuffer. This class cannot be inherited.
    /// </summary>
    public class CylindricalBuffer : ZBuffer
    {

        /// <summary>
        /// This Run method is only here to block ZBuffer.Run from being invoked accidentally.
        /// For completing a proper Cylindrical ZBuffer, be sure to use the overload that includes
        /// the center or anchor point.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        /// <param name="subsetFaces">The subset faces.</param>
        /// <returns>A ZBuffer.</returns>
        public static new ZBuffer Run(TessellatedSolid solid, Vector3 direction, int pixelsPerRow,
            int pixelBorder = 2, IEnumerable<TriangleFace> subsetFaces = null)
        { throw new ArgumentException("The direction and the anchor (possibly just the origin?) are needed for the cylindrical buffer."); }
        /// <summary>
        /// Initializes a new instance of the <see cref="CylindricalBuffer"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="axis">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public static CylindricalBuffer Run(TessellatedSolid solid, Vector3 axis, Vector3 anchor,
            int pixelsPerRow, IEnumerable<TriangleFace> subsetFaces = null)
        {
            var cylBuff = new CylindricalBuffer(solid);

            // get the transform matrix to apply to every point
            cylBuff.transform = axis.TransformToXYPlane(out cylBuff.backTransform);
            var rotatedAnchor = anchor.Transform(cylBuff.transform);
            cylBuff.transform *= Matrix4x4.CreateTranslation(-rotatedAnchor.X, -rotatedAnchor.Y, -rotatedAnchor.Z);
            cylBuff.backTransform = Matrix4x4.CreateTranslation(rotatedAnchor.X, rotatedAnchor.Y, rotatedAnchor.Z)
              * cylBuff.backTransform;

            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            //var minX = double.PositiveInfinity;
            //var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;
            cylBuff.baseRadius = 0.0;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var p = solid.Vertices[i].Coordinates.Transform(cylBuff.transform);
                var theta = Math.Atan2(p.Y, p.X);
                var radius = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                cylBuff.Vertices[i] = new Vector2(theta, p.Z);
                cylBuff.VertexZHeights[i] = radius;
                if (radius > cylBuff.baseRadius) cylBuff.baseRadius = radius;
                if (p.Z < minY) minY = p.Z;
                if (p.Z > maxY) maxY = p.Z;
            }
            cylBuff.circumference = Math.PI * 2 * cylBuff.baseRadius;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var x = cylBuff.baseRadius * cylBuff.Vertices[i].X;
                //if (x < minX) minX = x;
                //if (x > maxX) maxX = x;
                cylBuff.Vertices[i] = new Vector2(x, cylBuff.Vertices[i].Y);
            }
            //Finish initializing the grid now that we have the bounds.
            cylBuff.Initialize(-Math.PI*cylBuff.baseRadius, Math.PI * cylBuff.baseRadius,
                minY, maxY, pixelsPerRow, 2);
            for (int i = 0; i < solid.Edges.Length; i++)
            {
                Edge edge = solid.Edges[i];
                var rightIsOutward = edge.Vector.Cross(axis);
                var dot = rightIsOutward.Dot(edge.From.Coordinates - anchor);
                if (dot.IsNegligible()) continue;
                var stepIsPositive = dot > 0;
                var toVertexX = cylBuff.Vertices[edge.To.IndexInList].X;
                var fromVertexX = cylBuff.Vertices[edge.From.IndexInList].X;
                if (fromVertexX > toVertexX == stepIsPositive)
                    cylBuff.edgeWrapsAround[i] = true;
            }
            var faces = subsetFaces != null ? subsetFaces : cylBuff.solidFaces;
            foreach (TriangleFace face in faces)
                cylBuff.UpdateZBufferWithFace(face);

            return cylBuff;
        }
        private CylindricalBuffer(TessellatedSolid solid) : base(solid)
        {
            edgeWrapsAround = new bool[solid.NumberOfEdges];
        }
        /// <summary>
        /// Initializes the specified minimum x.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public new void Initialize(double minX, double maxX, double minY, double maxY, int pixelsPerRow, int axialPixelBorder = 2)
        {
            MinX = minX;
            MinY = minY;
            var XLength = maxX - MinX;
            var YLength = maxY - MinY;

            //Calculate the size of a pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = XLength / pixelsPerRow;
            inversePixelSideLength = 1 / PixelSideLength;
            XCount = pixelsPerRow;
            YCount = (int)Math.Ceiling(YLength * inversePixelSideLength);
            // shift the grid slightly so that the part grid points are better aligned within the bounds
            var yStickout = YCount * PixelSideLength - YLength;
            MinY -= yStickout / 2;
            // add the pixel border...2 since includes both sides (left and right, or top and bottom)
            YCount += axialPixelBorder * 2;
            MinY -= axialPixelBorder * PixelSideLength;

            MaxIndex = XCount * YCount - 1;
            Values = new (TriangleFace, double)[XCount * YCount];
        }




        private readonly bool[] edgeWrapsAround;
        private double circumference;
        private double baseRadius;

        /// <summary>
        /// Gets all the indices that are covered by a face.
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        protected override IEnumerable<(int index, double zHeight)> GetIndicesCoveredByFace(TriangleFace face)
        {
            // get the 3 vertices and their zheights
            var vA = Vertices[face.A.IndexInList];
            var zA = VertexZHeights[face.A.IndexInList];
            var vB = Vertices[face.B.IndexInList];
            var zB = VertexZHeights[face.B.IndexInList];
            var vC = Vertices[face.C.IndexInList];
            var zC = VertexZHeights[face.C.IndexInList];

            var abWraps = edgeWrapsAround[face.AB.IndexInList];
            var bcWraps = edgeWrapsAround[face.BC.IndexInList];
            var caWraps = edgeWrapsAround[face.CA.IndexInList];
            if (abWraps && bcWraps && caWraps)
                ; // Message.output("All three edges wrap around...can't be!");
            else if ((abWraps && !bcWraps && !caWraps) || (!abWraps && bcWraps && !caWraps) || (!abWraps && !bcWraps && caWraps))
                ; //  Message.output("Only one edge wraps around...wha?!");
            else foreach (var item in GetIndicesCoveredByFace(vA, zA, abWraps && caWraps, vB, zB, abWraps && bcWraps, vC, zC, bcWraps && caWraps))
                    yield return item;
            yield break;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<(int index, double zHeight)> GetIndicesCoveredByFace(Vector2 vA, double zA, bool aWraps, Vector2 vB, double zB, bool bWraps, Vector2 vC, double zC, bool cWraps)
        {
            if (aWraps)
            {
                if (vA.X < vB.X && vA.X < vC.X) vA = new Vector2(vA.X + circumference, vA.Y);
                else
                {
                    vC = new Vector2(vC.X + circumference, vC.Y);
                    vB = new Vector2(vB.X + circumference, vB.Y);
                }
            }
            else if (bWraps)
            {
                if (vB.X < vA.X && vB.X < vC.X) vB = new Vector2(vB.X + circumference, vB.Y);
                else
                {
                    vC = new Vector2(vC.X + circumference, vC.Y);
                    vA = new Vector2(vA.X + circumference, vA.Y);
                }
            }
            else if (cWraps)
            {
                if (vC.X < vA.X && vC.X < vB.X) vC = new Vector2(vC.X + circumference, vC.Y);
                else
                {
                    vB = new Vector2(vB.X + circumference, vB.Y);
                    vA = new Vector2(vA.X + circumference, vA.Y);
                }
            }
            var area = (vB - vA).Cross(vC - vA);
            // if the area is negative the triangle is facing the wrong way.
            if (area <= 0) yield break;
            var oneOverArea = 1 / area;
            var vAx = vA.X;
            var vAy = vA.Y;
            var vBx = vB.X;
            var vBy = vB.Y;
            var vCx = vC.X;
            var vCy = vC.Y;
            var vBAx = vBx - vAx;
            var vBAy = vBy - vAy;
            var vCAx = vCx - vAx;
            var vCAy = vCy - vAy;
            // next re-organize the vertices as vMin, vMed, vMax - ordered by 
            // their x-values
            OrderByIncreasingXValue(vA, vB, vC, out var vMin, out var vMed, out var vMax);
            // the following 3 indices are the pixels where the 3 vertices reside in x
            var xStartIndex = GetXIndex(vMin.X);
            var xSwitchIndex = GetXIndex(vMed.X);
            var xEndIndex = GetXIndex(vMax.X);
            // x is snapped to the grid. This value should be a little less than vMin.X
            var xSnap = GetSnappedX(xStartIndex);  //snapped vMin.X value;
            var yStart = vMin.Y;
            // the triangles line slopes are really normalized by the pixel side length
            // this is essentially the amount of movement in y for each pixel in x
            var slopeStepMed = PixelSideLength * (vMed.Y - vMin.Y) / (vMed.X - vMin.X);
            var slopeStepMax = PixelSideLength * (vMax.Y - vMin.Y) / (vMax.X - vMin.X);
            // the average slope is used to determine the middle most y values for the triangle
            // this is the basis of this method. With the middle most pixel, we search up and down
            // until we leave the triangle. Sometimes the triangle is to thin that none are found.
            // sometimes only the +up or the -down find a pixel.
            var averageSlope = 0.5 * (slopeStepMax + slopeStepMed);
            // chances are, we skip the first pixel in x. This is because the xSnap is less than vMin.X
            if (xSnap.IsLessThanNonNegligible(vMin.X))
            {   // but skipping the first pixel means we need to adjust the yStart
                if (xStartIndex == xSwitchIndex)
                {
                    if (vMed.X == vMin.X)
                        // slope would be infiity if x values are the same. catch it with this if statement
                        yStart = 0.5 * (vMin.Y + vMed.Y);
                    else yStart += averageSlope * (vMed.X - vMin.X) * inversePixelSideLength;
                    // it is important here (and at the end of the main loop) to switch at the middle vertex
                    // given how extreme triangles can be - it is crucial that our calculation for the center
                    // spine of the triangle is as accurate as possible.
                    slopeStepMed = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    averageSlope = 0.5 * (slopeStepMax + slopeStepMed);
                    yStart += averageSlope * (PixelSideLength - vMed.X + xSnap) * inversePixelSideLength;
                }
                else
                    yStart += averageSlope * (PixelSideLength - vMin.X + xSnap) * inversePixelSideLength;
                // now we need to increment the xSnap and xStartIndex
                xStartIndex++;
                xSnap += PixelSideLength;
            }
            var pixXMinusVAx = xSnap - vAx;
            // *** main loop ***
            var xIndex = xStartIndex;
            while (true)
            {
                // the following 2 lines are calculated to save repeat calculations in the PixelIsInside method
                var vBAy_by_pixXVAx = vBAy * pixXMinusVAx;
                var vCAy_by_pixXVAx = vCAy * pixXMinusVAx;
                // get the starting y index and the snapped y value
                var yStartIndex = GetYIndex(yStart);
                var ySnapStart = GetSnappedY(yStartIndex);
                if (PixelIsInside(ySnapStart - vAy, vBAx, vBAy_by_pixXVAx, area, oneOverArea,
                    vCAy_by_pixXVAx, vCAx, zA, zB, zC, out var zHeight))
                    yield return (GetIndex(xIndex % XCount, yStartIndex), zHeight);
                // first search down until we leave the triangle.  
                var ySnap = ySnapStart;
                var yIndex = yStartIndex;
                while (true)
                {
                    yIndex--;
                    ySnap -= PixelSideLength;
                    if (PixelIsInside(ySnap - vAy, vBAx, vBAy_by_pixXVAx, area, oneOverArea,
                        vCAy_by_pixXVAx, vCAx, zA, zB, zC, out zHeight))
                        yield return (GetIndex(xIndex % XCount, yIndex), zHeight);
                    else break;
                }
                // now in the positive direction
                ySnap = ySnapStart;
                yIndex = yStartIndex;
                while (true)
                {   // note that the yIndex and ySnap are incremented at the end of the loop
                    // this is subtle but save a re-calculation of the center pixel
                    yIndex++;
                    ySnap += PixelSideLength;
                    if (PixelIsInside(ySnap - vAy, vBAx, vBAy_by_pixXVAx, area, oneOverArea,
                        vCAy_by_pixXVAx, vCAx, zA, zB, zC, out zHeight))
                        yield return (GetIndex(xIndex % XCount, yIndex), zHeight);
                    else break;
                }

                // here is the main loop exit condition
                if (xIndex >= xEndIndex) break;

                // like the beginning, if the middle vertex is encountered, we must take
                // special care to get the correct yStart value
                if (xIndex == xSwitchIndex)
                {
                    var leftXDelta = vMed.X - GetSnappedX(xIndex);
                    yStart += averageSlope * leftXDelta * inversePixelSideLength;
                    slopeStepMed = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    averageSlope = 0.5 * (slopeStepMax + slopeStepMed);
                    var rightXDelta = PixelSideLength - leftXDelta;
                    yStart += averageSlope * rightXDelta * inversePixelSideLength;
                }
                // otherwise, we just increment the yStart by the average slope
                else yStart += averageSlope;

                // finally, we increment the xIndex and xSnap (well, xSnap is not really
                // needed but it's difference from vA.X is used repeatedly in the PixelIsInside method)
                xIndex++;
                pixXMinusVAx += PixelSideLength;
            }
        }

        /// <summary>
        /// Gets the 3D transformed point of pixel i,j to the x-y plane, with z being the z-buffer height.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 Get3DPointTransformed(int i, int j, double defaultZHeight)
        {
            var index = GetIndex(i, j);
            var zHeight = Values[index].Item1 == null ? defaultZHeight : Values[index].Item2;
            return new Vector3(Get2DPoint(i, j), zHeight);
        }
        /// <summary>
        /// Gets the 3D point on the solid corresponding to pixel i, j.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector3.</returns>
        public override Vector3 Get3DPoint(int i, int j, double defaultZHeight = 0.0)
        {
            var flatPoint = Get3DPointTransformed(i, j, defaultZHeight);
            var theta = flatPoint.X / baseRadius;
            var axialLength = flatPoint.Y;
            var radius = baseRadius + flatPoint.Z;
            var x = radius * Math.Cos(theta);
            var y = radius * Math.Sin(theta);
            var point = new Vector3(x, y, axialLength);
            return point.Transform(backTransform);
        }
        /// <summary>}
    }
}
