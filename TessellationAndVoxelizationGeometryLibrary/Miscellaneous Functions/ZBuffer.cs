using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace TVGL
{
    public class ZBuffer
    {
        public double[,] ZHeightsOnly
        {
            get
            {
                if (zHeightsOnly == null)
                {
                    zHeightsOnly = new double[XCount, YCount];
                    for (int i = 0; i < XCount; i++)
                        for (int j = 0; j < YCount; j++)
                            zHeightsOnly[i, j] = ZHeightsWithFaces[i, j].Item2;
                }
                return zHeightsOnly;
            }
        }
        double[,] zHeightsOnly;
        public (PolygonalFace, double)[,] ZHeightsWithFaces { get; private set; }
        public Dictionary<PolygonalFace, double> ProjectedFaceAreas { get; private set; }
        public Vector2[] Vertices { get; private set; }
        public double[] VertexZHeights { get; private set; }
        public double MinX { get; private set; }
        public double MinY { get; private set; }
        public double MaxX { get; private set; }
        public double MaxY { get; private set; }
        public double XLength { get; private set; }
        public double YLength { get; private set; }
        public double MaxLength { get; private set; }
        public double PixelSideLength { get; private set; }
        public double InversePixelSideLength { get; private set; }
        public int XCount { get; private set; }
        public int YCount { get; private set; }
        private Matrix4x4 transform;
        private Matrix4x4 backTransform;
        private PolygonalFace[] solidFaces;

        public ZBuffer(TessellatedSolid solid, Vector3 direction, int pixelsPerRow, int pixelBorder = 0)
        {
            Vertices = new Vector2[solid.NumberOfVertices];
            VertexZHeights = new double[solid.NumberOfVertices];
            MinX = double.PositiveInfinity;
            MinY = double.PositiveInfinity;
            MaxX = double.NegativeInfinity;
            MaxY = double.NegativeInfinity;
            // get the transform matrix to apply to every point
            transform = direction.TransformToXYPlane(out backTransform);
            solidFaces = solid.Faces;
            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var p = solid.Vertices[i].Coordinates.Transform(transform);
                Vertices[i] = new Vector2(p.X, p.Y);
                VertexZHeights[i] = p.Z;
                if (p.X < MinX) MinX = p.X;
                if (p.Y < MinY) MinY = p.Y;
                if (p.X > MaxX) MaxX = p.X;
                if (p.Y > MaxY) MaxY = p.Y;
            }
            XLength = MaxX - MinX;
            YLength = MaxY - MinY;
            MaxLength = XLength > YLength ? XLength : YLength;

            //Calculate the size of each pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = MaxLength / (pixelsPerRow - pixelBorder * 2);
            InversePixelSideLength = 1 / PixelSideLength;
            XCount = (int)Math.Ceiling(XLength * InversePixelSideLength);
            YCount = (int)Math.Ceiling(YLength * InversePixelSideLength);
            // shift the grid slightly so that the part grid points are better aligned with the solid
            var xStickout = XLength - XCount * PixelSideLength;
            MinX += xStickout / 2;
            var yStickout = YLength - YCount * PixelSideLength;
            MinY += yStickout / 2;
            // add the pixel border...2 since includes both sides (left and right, or top and bottom)
            XCount += pixelBorder * 2;
            YCount += pixelBorder * 2;
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
        public void Run(IList<PolygonalFace> subsetFaces = null)
        {
            ZHeightsWithFaces = new (PolygonalFace, double)[XCount, YCount];
            //Foreach face in the surfaces, project it along the transform.
            //For each pixel in the min/max X/Y of the projectect points, add this triangle as a potential intersection.
            //Also store the Z value. 
            //Then, for each pixel, find its first intesection from it's subset of potential faces.
            //Stop at the first intersection from the ordered Z values.
            var faces = subsetFaces != null ? subsetFaces : solidFaces;
            ProjectedFaceAreas = new Dictionary<PolygonalFace, double>();

            foreach (PolygonalFace face in faces)
                ProjectedFaceAreas.Add(face, UpdateZBufferWithFace(face));
        }

        public IEnumerable<(int, int, double)> GetLinePixels(Edge edge)
        {
            throw new NotImplementedException();
        }

        private double UpdateZBufferWithFaceScan(PolygonalFace face)
        //, (PolygonalFace, double)[,] grid, Vector2[] points,
        //double[] zHeigts, double PixelSideLength, double inversePixelSideLength, double XMinGlobal, double YMinGlobal)
        {
            var vA = Vertices[face.A.IndexInList];
            var zA = VertexZHeights[face.A.IndexInList];
            var vB = Vertices[face.B.IndexInList];
            var zB = VertexZHeights[face.B.IndexInList];
            var vC = Vertices[face.C.IndexInList];
            var zC = VertexZHeights[face.C.IndexInList];
            var area = (vB - vA).Cross(vC - vA);
            if (area <= 0) return area;
            Vector2 vMin, vMed, vMax;

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
            var xStartIndex = (int)((vMin.X - MinX) * InversePixelSideLength);
            var xEndIndex = (int)((vMax.X - MinX) * InversePixelSideLength);
            var xSwitchIndex = (int)((vMed.X - MinX) * InversePixelSideLength);

            double slopeStepBtm, slopeStepTop;
            var switchOnBottom = vMed.Y <= vMax.Y;
            if (switchOnBottom)
            {
                slopeStepBtm = PixelSideLength * (vMed.Y - vMin.Y) / (vMed.X - vMin.X);
                slopeStepTop = PixelSideLength * (vMax.Y - vMin.Y) / (vMax.X - vMin.X);
            }
            else
            {
                slopeStepBtm = PixelSideLength * (vMax.Y - vMin.Y) / (vMax.X - vMin.X);
                slopeStepTop = PixelSideLength * (vMed.Y - vMin.Y) / (vMed.X - vMin.X);
            }

            var x = vMin.X;
            var yBtm = vMin.Y;
            var yTop = vMin.Y;
            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                var yIndex = (int)((yBtm - MinY) * InversePixelSideLength);
                for (var y = yBtm; y <= yTop; y += PixelSideLength)
                {
                    //borrowing notation from:https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
                    var q = new Vector2(x, y);
                    var area2 = (vB - vA).Cross(q - vA);
                    if (area2 >= 0 && area2 <= area)
                    {
                        var v = area2 / area;
                        var area3 = (q - vA).Cross(vC - vA);
                        if (area3 >= 0 && area3 <= area)
                        {
                            var u = area3 / area;
                            var w = 1 - v - u;

                            if (w >= 0 && w <= 1)
                            {
                                var zIntercept = w * zA + u * zB + v * zC;
                                if (ZHeightsWithFaces[xIndex, yIndex] == default || zIntercept < ZHeightsWithFaces[xIndex, yIndex].Item2)
                                    ZHeightsWithFaces[xIndex, yIndex] = (face, zIntercept);
                            }
                        }
                    }
                    yBtm += slopeStepBtm;
                    yTop += slopeStepTop;
                    yIndex++;
                }
                x += PixelSideLength;
                if (xIndex == xSwitchIndex)
                {
                    if (switchOnBottom)
                        slopeStepBtm = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    else
                        slopeStepTop = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                }
            }
            return area;
        }

        private double UpdateZBufferWithFace(PolygonalFace face)
        {
            var vA = Vertices[face.A.IndexInList];
            var zA = VertexZHeights[face.A.IndexInList];
            var vB = Vertices[face.B.IndexInList];
            var zB = VertexZHeights[face.B.IndexInList];
            var vC = Vertices[face.C.IndexInList];
            var zC = VertexZHeights[face.C.IndexInList];
            var area = (vB - vA).Cross(vC - vA);
            if (area <= 0) return area;
            var xMin = Math.Min(vA.X, Math.Min(vB.X, vC.X));
            var xMax = Math.Max(vA.X, Math.Max(vB.X, vC.X));
            var yMin = Math.Min(vA.Y, Math.Min(vB.Y, vC.Y));
            var yMax = Math.Max(vA.Y, Math.Max(vB.Y, vC.Y));

            var xStartIndex = (int)((xMin - MinX) * InversePixelSideLength);
            var yStartIndex = (int)((yMin - MinY) * InversePixelSideLength);
            var xEndIndex = (int)((xMax - MinX) * InversePixelSideLength);
            var yEndIndex = (int)((yMax - MinY) * InversePixelSideLength);

            var x = MinX + xStartIndex * PixelSideLength;
            var y = MinY + yStartIndex * PixelSideLength;
            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                for (var yIndex = yStartIndex; yIndex <= yEndIndex; yIndex++)
                {
                    //borrowing notation from:https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
                    var q = new Vector2(x, y);
                    var area2 = (vB - vA).Cross(q - vA);
                    if (area2 >= 0 && area2 <= area)
                    {
                        var v = area2 / area;
                        var area3 = (q - vA).Cross(vC - vA);
                        if (area3 >= 0 && area3 <= area)
                        {
                            var u = area3 / area;
                            var w = 1 - v - u;

                            if (w >= 0 && w <= 1)
                            {
                                var zIntercept = w * zA + u * zB + v * zC;
                                if (ZHeightsWithFaces[xIndex, yIndex] == default || zIntercept < ZHeightsWithFaces[xIndex, yIndex].Item2)
                                    ZHeightsWithFaces[xIndex, yIndex] = (face, zIntercept);
                            }
                        }
                    }
                    y += PixelSideLength;
                }
                x += PixelSideLength;
            }
            return area / 2;
        }

        public Vector2 Get2DPoint(int i, int j)
        {
            return new Vector2(MinX + i * PixelSideLength, MinY + j * PixelSideLength);
        }
        public Vector3 Get3DPointTransformed(int i, int j)
        {
            return new Vector3(MinX + i * PixelSideLength, MinY + j * PixelSideLength, ZHeightsWithFaces[i, j].Item2);
        }
        public Vector3 Get3DPoint(int i, int j)
        {
            return Get3DPointTransformed(i, j).Transform(backTransform);
        }
    }
}
