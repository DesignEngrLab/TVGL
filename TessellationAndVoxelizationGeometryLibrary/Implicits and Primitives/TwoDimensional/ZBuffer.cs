// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="ZBuffer.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace TVGL
{
    /// <summary>
    /// Class ZBuffer. This class cannot be inherited.
    /// </summary>
    public class ZBuffer : Grid<(TriangleFace, double)>
    {
        /// <summary>
        /// Gets the z-heights as a matrix of doubles. There is no time saved in getting this by itself as the main
        /// method keeps track of faces. In other words, ZHeightsWithFaces is what is found by the Run routine.
        /// </summary>
        /// <value>The z heights only.</value>
        public double[,] ZHeightsOnly
        {
            get
            {
                if (zHeightsOnly == null)
                {
                    zHeightsOnly = new double[XCount, YCount];
                    foreach (var (index, i, j) in Indices())
                        zHeightsOnly[i, j] = Values[index].Item2;
                }
                return zHeightsOnly;
            }
        }
        /// <summary>
        /// The z heights only
        /// </summary>
        double[,] zHeightsOnly;

        /// <summary>
        /// Gets the projected 2D vertices of all the 3D vertices of the tessellated solid.
        /// This is found through the course of the "Run" computation and might be useful elsewhere.
        /// </summary>
        /// <value>The vertices.</value>
        public Vector2[] Vertices { get; protected set; }
        /// <summary>
        /// Gets the z-heightsof all the 3D vertices of the tessellated solid on the project plane.
        /// This is found through the course of the "Run" computation and might be useful elsewhere.
        /// </summary>
        /// <value>The vertex z heights.</value>
        public double[] VertexZHeights { get; protected set; }

        /// <summary>
        /// The transform
        /// </summary>
        private Matrix4x4 transform;
        /// <summary>
        /// The back transform
        /// </summary>
        private Matrix4x4 backTransform;
        /// <summary>
        /// The solid faces
        /// </summary>
        protected TriangleFace[] solidFaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="ZBuffer"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public static ZBuffer Run(TessellatedSolid solid, Vector3 direction, int pixelsPerRow,
            int pixelBorder = 2, IEnumerable<TriangleFace> subsetFaces = null)
        {
            var zBuff = new ZBuffer(solid);
            // get the transform matrix to apply to every point
            zBuff.transform = direction.TransformToXYPlane(out zBuff.backTransform);

            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            var MinX = double.PositiveInfinity;
            var MinY = double.PositiveInfinity;
            var MaxX = double.NegativeInfinity;
            var MaxY = double.NegativeInfinity;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var p = solid.Vertices[i].Coordinates.Transform(zBuff.transform);
                zBuff.Vertices[i] = new Vector2(p.X, p.Y);
                zBuff.VertexZHeights[i] = p.Z;
                if (p.X < MinX) MinX = p.X;
                if (p.Y < MinY) MinY = p.Y;
                if (p.X > MaxX) MaxX = p.X;
                if (p.Y > MaxY) MaxY = p.Y;
            }

            //Finish initializing the grid now that we have the bounds.
            zBuff.Initialize(MinX, MaxX, MinY, MaxY, pixelsPerRow, pixelBorder);
            var faces = subsetFaces != null ? subsetFaces : zBuff.solidFaces;
            foreach (TriangleFace face in faces)
                zBuff.UpdateZBufferWithFace(face);
            return zBuff;
        }

        private protected ZBuffer(TessellatedSolid solid)
        {
            Vertices = new Vector2[solid.NumberOfVertices];
            VertexZHeights = new double[solid.NumberOfVertices];
            solidFaces = solid.Faces;
        }


        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>System.Double.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void UpdateZBufferWithFace(TriangleFace face)
        {
            //return CheckZBufferWithFace(face, true, out _, out _);
            foreach (var indexAndHeight in GetIndicesCoveredByFace(face))
            {
                var index = indexAndHeight.index;
                var zHeight = indexAndHeight.zHeight;
                // since the grid is not initialized, we update it if the grid cell is empty or if we found a better face
                var tuple = Values[index];
                if (tuple == default || zHeight >= tuple.Item2)
                    Values[index] = (face, zHeight);
            }
        }

        public void CheckZBufferWithFace(TriangleFace face, out int visibleGridPoints, out int totalGridPointsCovered)
        {
            var tolerance = 0.5 * PixelSideLength;
            totalGridPointsCovered = 0;
            visibleGridPoints = 0;
            foreach (var indexAndHeight in GetIndicesCoveredByFace(face))
            {
                totalGridPointsCovered++;
                var index = indexAndHeight.index;
                var zHeight = indexAndHeight.zHeight;
                var tuple = Values[index];
                if (tuple == default || face == tuple.Item1 || zHeight.IsPracticallySame(tuple.Item2, tolerance))
                    visibleGridPoints++;
            }
        }



        /// <summary>
        /// Gets all the indices that are covered by a face.
        /// This is the big tricky function. In the end, implemented a custom function
        /// that scans the triangle from left to right. This is similar to the approach
        /// described here: http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html
        /// This is slow and possibly could be improved 
        /// </summary>
        /// <param name="face"></param>
        /// <returns></returns>
        private IEnumerable<(int index, double zHeight)> GetIndicesCoveredByFace(TriangleFace face)
        {
            // get the 3 vertices and their zheights
            var vA = Vertices[face.A.IndexInList];
            var zA = VertexZHeights[face.A.IndexInList];
            var vB = Vertices[face.B.IndexInList];
            var zB = VertexZHeights[face.B.IndexInList];
            var vC = Vertices[face.C.IndexInList];
            var zC = VertexZHeights[face.C.IndexInList];
            return GetIndicesCoveredByFace(vA, zA, vB, zB, vC, zC);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<(int index, double zHeight)> GetIndicesCoveredByFace(Vector2 vA, double zA, Vector2 vB,
        double zB, Vector2 vC, double zC)
        {
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

                // first search down until we leave the triangle. here we start with the central pixel
                // but in the search-up loop we start one above the central pixel. This is 
                var ySnap = ySnapStart;
                var yIndex = yStartIndex;
                while (true)
                {
                    if (PixelIsInside(ySnap - vAy, vBAx, vBAy_by_pixXVAx, area, oneOverArea,
                        vCAy_by_pixXVAx, vCAx, zA, zB, zC, out var zHeight))
                        yield return (GetIndex(xIndex, yIndex), zHeight);
                    else break;
                    yIndex--;
                    ySnap -= PixelSideLength;
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
                        vCAy_by_pixXVAx, vCAx, zA, zB, zC, out var zHeight))
                        yield return (GetIndex(xIndex, yIndex), zHeight);
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

        bool PixelIsInside(double qVaY, double vBAx, double vBAy_multiply_qVaX, double area,
        double oneOverArea, double vCAy_multiply_qVaX, double vCAx, double zA, double zB, double zC, out double zHeight)
        {
            var area2 = vBAx * qVaY - vBAy_multiply_qVaX;
            //if (area2.IsLessThanNonNegligible(0, negligible) || area2.IsGreaterThanNonNegligible(area, negligible))
            if (area2 < 0 || area2 > area)
            {
                zHeight = double.NaN;
                return false;
            }
            var area3 = vCAy_multiply_qVaX - qVaY * vCAx;
            //if (area3.IsLessThanNonNegligible(0, negligible) || area3.IsGreaterThanNonNegligible(area, negligible))
            if (area3 < 0 || area3 > area)
            {
                zHeight = double.NaN;
                return false;
            }
            var v = area2 * oneOverArea;
            var u = area3 * oneOverArea;
            var w = 1 - v - u;
            //if (w.IsLessThanNonNegligible(0, negligible) || w.IsGreaterThanNonNegligible(1, negligible))
            if (w.IsLessThanNonNegligible(0))
            {
                zHeight = double.NaN;
                return false;
            }
            zHeight = w * zA + u * zB + v * zC;
            return true;
        }

        private static void OrderByIncreasingXValue(Vector2 vA, Vector2 vB, Vector2 vC, out Vector2 vMin, out Vector2 vMed, out Vector2 vMax)
        {
            if (vA.X <= vB.X && vA.X <= vC.X)
            {
                vMin = vA;
                if (vB.X <= vC.X)
                {
                    vMed = vB;
                    vMax = vC;
                }
                else
                {
                    vMed = vC;
                    vMax = vB;
                }
            }
            else if (vB.X <= vA.X && vB.X <= vC.X)
            {
                vMin = vB;
                if (vA.X <= vC.X)
                {
                    vMed = vA;
                    vMax = vC;
                }
                else
                {
                    vMed = vC;
                    vMax = vA;
                }
            }
            else
            {
                vMin = vC;
                if (vA.X <= vB.X)
                {
                    vMed = vA;
                    vMax = vB;
                }
                else
                {
                    vMed = vB;
                    vMax = vA;
                }
            }
        }
        /// <summary>
        /// Gets the 2D point of pixel i,j.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector2.</returns>
        public Vector2 Get2DPoint(int i, int j)
        {
            return new Vector2(MinX + i * PixelSideLength, MinY + j * PixelSideLength);
        }
        /// <summary>
        /// Gets the 3D transformed point of pixel i,j to the x-y plane, with z being the z-buffer height.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector3.</returns>
        public Vector3 Get3DPointTransformed(int i, int j, double defaultZHeight = 0.0)
        {
            var zHeight = Values[YCount * i + j].Item1 == null ? defaultZHeight : Values[YCount * i + j].Item2;
            return new Vector3(Get2DPoint(i, j), zHeight);
        }
        /// <summary>
        /// Gets the 3D point on the solid corresponding to pixel i, j.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector3.</returns>
        public Vector3 Get3DPoint(int i, int j, double defaultZHeight = 0.0)
        {
            return Get3DPointTransformed(i, j, defaultZHeight).Transform(backTransform);
        }


        public HashSet<int> GetCircleIndices(Vector2 center, double radius)
        {
            var outline = new HashSet<int>();
            var xStartIndex = Math.Max(GetXIndex(center.X - radius), 0);
            var xEndIndex = Math.Min(GetXIndex(center.X + radius), XCount - 1);
            var yStartIndex = Math.Max(GetYIndex(center.Y - radius), 0);
            var yEndIndex = Math.Min(GetYIndex(center.Y + radius), YCount - 1);
            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                var xReal = GetSnappedX(xIndex);
                var dxSq = (xReal - center.X) * (xReal - center.X);
                for (var yIndex = yStartIndex; yIndex <= yEndIndex; yIndex++)
                {
                    var yReal = GetSnappedY(yIndex);
                    var dySq = (yReal - center.Y) * (yReal - center.Y);
                    var distance = Math.Sqrt(dxSq + dySq);
                    if (distance.IsPracticallySame(radius, PixelSideLength * 2))
                    {
                        var index = GetIndex(xIndex, yIndex);
                        outline.Add(index);
                    }
                }
            }
            return outline;
        }
    }
}
