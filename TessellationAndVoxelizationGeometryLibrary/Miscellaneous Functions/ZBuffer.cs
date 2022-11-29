using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;

namespace TVGL
{
    public static class ZBuffer
    {
        public static double[,] GetZBuffer(TessellatedSolid solid, Vector3 direction, int pixelsPerRow, out List<double> projectFaceAreas,
            int pixelBorder = 0, IEnumerable<PolygonalFace> subsetFaces = null)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Gets the z-buffer of the TessellatedSolid along the given direction.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row to discretize the model to.</param>
        /// <param name="ProjectedFaceAreas">The projected face areas are a by-product of the method. Merely the areas of the faces in the 
        /// project direction. If less than zero, then the face is away from the direction and not included in the z-buffer anyway.</param>
        /// <param name="pixelBorder">The pixel border is the number of pixels to add around the model.</param>
        /// <param name="subsetFaces">The subset of the solid's faces to find the z-buffer for.</param>
        /// <returns>System.ValueTuple&lt;PolygonalFace, System.Double&gt;[].</returns>
        public static (PolygonalFace, double)[,] GetZBufferWithFaces(TessellatedSolid solid, Vector3 direction, int pixelsPerRow, out double[] ProjectedFaceAreas,
            int pixelBorder = 0, IList<PolygonalFace> subsetFaces = null)
        {
            #region Initialize
            var points = new Vector2[solid.NumberOfVertices];
            var zHeights = new double[solid.NumberOfVertices];
            var minX = double.PositiveInfinity;
            var minY = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var maxY = double.NegativeInfinity;
            // get the transform matrix to apply to every point
            var matrix = direction.TransformToXYPlane(out _);
            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var p = solid.Vertices[i].Coordinates.Transform(matrix);
                points[i] = new Vector2(p.X, p.Y);
                zHeights[i] = p.Z;
                if (p.X < minX) minX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.X > maxX) maxX = p.X;
                if (p.Y > maxY) maxY = p.Y;
            }
            var xLength = maxX - minX;
            var yLength = maxY - minY;
            var maxLength = xLength > yLength ? xLength : yLength;

            //Calculate the size of each pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            var PixelSideLength = maxLength / (pixelsPerRow - pixelBorder * 2);
            var inversePixelSideLength = 1 / PixelSideLength;
            var XCount = (int)Math.Floor(xLength * inversePixelSideLength);
            var YCount = (int)Math.Floor(yLength * inversePixelSideLength);
            // shift the grid slightly so that the part grid points are better aligned with the solid
            var xStickout = xLength - XCount * PixelSideLength;
            minX += xStickout / 2;
            var yStickout = yLength - YCount * PixelSideLength;
            minY += yStickout / 2;
            // add the pixel border...2 since includes both sides (left and right, or top and bottom)
            XCount += pixelBorder * 2;
            YCount += pixelBorder * 2;

            var grid = new (PolygonalFace, double)[XCount, YCount];
            #endregion
            //Foreach face in the surfaces, project it along the transform.
            //For each pixel in the min/max X/Y of the projectect points, add this triangle as a potential intersection.
            //Also store the Z value. 
            //Then, for each pixel, find its first intesection from it's subset of potential faces.
            //Stop at the first intersection from the ordered Z values.
            var faces = subsetFaces != null ? subsetFaces : solid.Faces;
            ProjectedFaceAreas = new double[faces.Count];

            for (int k = 0; k < faces.Count; k++)
            {
                PolygonalFace face = faces[k];
                var facePoints = new Vector2[3];
                var faceZHeights = new double[3];
                for (int i = 0; i < 3; i++)
                {
                    facePoints[i] = points[face.Vertices[i].IndexInList];
                    faceZHeights[i] = zHeights[face.Vertices[i].IndexInList];
                }
                var faceArea = UpdateZBufferWithFace(face, grid, facePoints, zHeights, PixelSideLength, inversePixelSideLength, minX, minY);
                ProjectedFaceAreas[k] = faceArea;
            }
            return grid;
        }

        private static void UpdateZBufferWithFaceScan(PolygonalFace face, (PolygonalFace, double)[,] grid, Vector2[] points,
            double[] zHeigts, double PixelSideLength, double inversePixelSideLength, double XMinGlobal, double YMinGlobal)
        {
            var area = (points[1] - points[0]).Cross(points[2] - points[0]);
            if (area <= 0) return;
            Vector2 pMin, pMed, pMax;
            double zAtMin, zAtMed, zAtMax;

            if (points[0].X <= points[1].X && points[0].X <= points[2].X)
            {
                pMin = points[0];
                zAtMin = zHeigts[0];
                if (points[1].X <= points[2].X)
                {
                    pMed = points[1];
                    zAtMed = zHeigts[1];
                    pMax = points[2];
                    zAtMax = zHeigts[2];
                }
                else
                {
                    pMed = points[2];
                    zAtMed = zHeigts[2];
                    pMax = points[1];
                    zAtMax = zHeigts[1];
                }
            }
            else if (points[1].X <= points[0].X && points[1].X <= points[2].X)
            {
                pMin = points[1];
                zAtMin = zHeigts[1];
                if (points[0].X <= points[2].X)
                {
                    pMed = points[0];
                    zAtMed = zHeigts[0];
                    pMax = points[2];
                    zAtMax = zHeigts[2];
                }
                else
                {
                    pMed = points[2];
                    zAtMed = zHeigts[2];
                    pMax = points[0];
                    zAtMax = zHeigts[0];
                }
            }
            else
            {
                pMin = points[2];
                zAtMin = zHeigts[2];
                if (points[0].X <= points[1].X)
                {
                    pMed = points[0];
                    zAtMed = zHeigts[0];
                    pMax = points[1];
                    zAtMax = zHeigts[1];
                }
                else
                {
                    pMed = points[1];
                    zAtMed = zHeigts[1];
                    pMax = points[0];
                    zAtMax = zHeigts[0];
                }
            }
            var xStartIndex = (int)((pMin.X - XMinGlobal) * inversePixelSideLength);
            var yStartIndex = (int)((pMin.Y - YMinGlobal) * inversePixelSideLength);
            var xEndIndex = (int)((pMax.X - XMinGlobal) * inversePixelSideLength);
            var xSwitchIndex = (int)((pMed.X - XMinGlobal) * inversePixelSideLength);

            (Vector2, Vector2) lineBtm;
            (Vector2, Vector2) lineTop;
            var switchOnBottom = false;
            if (pMed.Y <= pMax.Y)
            {
                lineBtm = (pMin, pMed);
                lineTop = (pMin, pMax);
                switchOnBottom = true;
            }
            else
            {
                lineBtm = (pMin, pMax);
                lineTop = (pMin, pMed);
            }
            var slopeBtm = (lineBtm.Item2.Y - lineBtm.Item1.Y) / (lineBtm.Item2.X - lineBtm.Item1.X);
            //var yBtmOffset =
            var slopeTop = (lineTop.Item2.Y - lineTop.Item1.Y) / (lineTop.Item2.X - lineTop.Item1.X);

            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                var x = XMinGlobal + xIndex * PixelSideLength;
                var yBtm = (x - lineBtm.Item1.X) * slopeBtm + lineBtm.Item1.Y;
                var yTop = (x - lineBtm.Item1.X) * slopeBtm + lineBtm.Item1.Y;
                var yt = YMinGlobal + yBtm * PixelSideLength;
                //for (var y = bottom; y <= top; y++)
                //{
                var yIndex = -1;
                //borrowing notation from:https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
                var q = new Vector2(x, yt);
                var area2 = (points[1] - points[0]).Cross(q - points[0]);
                if (area2 >= 0 && area2 <= area)
                {
                    var v = area2 / area;
                    var area3 = (q - points[0]).Cross(points[2] - points[0]);
                    if (area3 >= 0 && area3 <= area)
                    {
                        var u = area3 / area;
                        var w = 1 - v - u;

                        if (w >= 0 && w <= 1)
                        {
                            var zIntercept = w * zHeigts[0] + u * zHeigts[1] + v * zHeigts[2];
                            if (zIntercept < grid[xIndex, yIndex].Item2)
                                grid[xIndex, yIndex] = (face, zIntercept);
                        }
                    }
                }
                yt += PixelSideLength;
            }
        }

        private static double UpdateZBufferWithFace(PolygonalFace face, (PolygonalFace, double)[,] grid, Vector2[] points,
            double[] zHeights, double PixelSideLength, double inversePixelSideLength, double XMinGlobal, double YMinGlobal)
        {
            var area = (points[1] - points[0]).Cross(points[2] - points[0]);
            if (area <= 0) return 0;
            var xMin = Math.Min(points[0].X, Math.Min(points[1].X, points[2].X));
            var xMax = Math.Max(points[0].X, Math.Max(points[1].X, points[2].X));
            var yMin = Math.Min(points[0].Y, Math.Min(points[1].Y, points[2].Y));
            var yMax = Math.Max(points[0].Y, Math.Max(points[1].Y, points[2].Y));

            var xStartIndex = (int)((xMin - XMinGlobal) * inversePixelSideLength);
            var yStartIndex = (int)((yMin - YMinGlobal) * inversePixelSideLength);
            var xEndIndex = (int)((xMax - XMinGlobal) * inversePixelSideLength);
            var yEndIndex = (int)((yMax - YMinGlobal) * inversePixelSideLength);

            var x = XMinGlobal + xStartIndex * PixelSideLength;
            var y = YMinGlobal + yStartIndex * PixelSideLength;
            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                for (var yIndex = yStartIndex; yIndex <= yEndIndex; yIndex++)
                {
                    //borrowing notation from:https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
                    var q = new Vector2(x, y);
                    var area2 = (points[1] - points[0]).Cross(q - points[0]);
                    if (area2 >= 0 && area2 <= area)
                    {
                        var v = area2 / area;
                        var area3 = (q - points[0]).Cross(points[2] - points[0]);
                        if (area3 >= 0 && area3 <= area)
                        {
                            var u = area3 / area;
                            var w = 1 - v - u;

                            if (w >= 0 && w <= 1)
                            {
                                var zIntercept = w * zHeights[0] + u * zHeights[1] + v * zHeights[2];
                                if (grid[xIndex, yIndex] == default || zIntercept < grid[xIndex, yIndex].Item2)
                                    grid[xIndex, yIndex] = (face, zIntercept);
                            }
                        }
                    }
                    y += PixelSideLength;
                }
                x += PixelSideLength;
            }
            return area / 2;
        }
    }
}
