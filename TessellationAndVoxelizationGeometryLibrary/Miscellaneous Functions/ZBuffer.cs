using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TVGL
{
    public sealed class ZBuffer : Grid<(PolygonalFace, double)>
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
                    foreach(var (index, i, j) in Indices())
                        zHeightsOnly[i, j] = Values[index].Item2;
                }
                return zHeightsOnly;
            }
        }
        double[,] zHeightsOnly;
        /// <summary>
        /// Gets the projected face areas in the z-buffer direction. This is found through
        /// the course of the "Run" computation and might be useful elsewhere.
        /// </summary>
        /// <value>The projected face areas.</value>
        public Dictionary<PolygonalFace, double> ProjectedFaceAreas { get; set; }
        /// <summary>
        /// Gets the projected 2D vertices of all the 3D vertices of the tessellated solid.
        /// This is found through the course of the "Run" computation and might be useful elsewhere.
        /// </summary>
        /// <value>The vertices.</value>
        public Vector2[] Vertices { get; private set; }
        /// <summary>
        /// Gets the z-heightsof all the 3D vertices of the tessellated solid on the project plane.
        /// This is found through the course of the "Run" computation and might be useful elsewhere.
        /// </summary>
        /// <value>The vertex z heights.</value>
        public double[] VertexZHeights { get; private set; }

        private Matrix4x4 transform;
        private Matrix4x4 backTransform;
        private PolygonalFace[] solidFaces;

        public ZBuffer(TessellatedSolid solid, Vector3 direction, int pixelsPerRow, int pixelBorder = 2)
        {
            Vertices = new Vector2[solid.NumberOfVertices];
            VertexZHeights = new double[solid.NumberOfVertices];
            // get the transform matrix to apply to every point
            transform = (-direction).TransformToXYPlane(out backTransform);
            solidFaces = solid.Faces;

            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            var MinX = double.PositiveInfinity;
            var MinY = double.PositiveInfinity;
            var MaxX = double.NegativeInfinity;
            var MaxY = double.NegativeInfinity;
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

            //Finish initializing the grid now that we have the bounds.
            Initialize(MinX, MaxX, MinY, MaxY, pixelsPerRow, pixelBorder);
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
            var faces = subsetFaces != null ? subsetFaces : solidFaces;
            ProjectedFaceAreas = new Dictionary<PolygonalFace, double>();

            foreach (PolygonalFace face in faces)
                ProjectedFaceAreas.Add(face, UpdateZBufferWithFace(face));
        }

        public IEnumerable<(int, int, double)> GetLinePixels(Edge edge)
        {
            //Get projected vertices and then use the this.PlotLine() function.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Updates the z-buffer with information from each face.
        /// This is the big tricky function. In the end, implemented a custom function
        /// that scans the triangle from left to right. This is similar to the approach 
        /// described here: http://www.sunshine2k.de/coding/java/TriangleRasterization/TriangleRasterization.html
        /// </summary>
        /// <param name="face">The face.</param>
        /// <returns>System.Double.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private double UpdateZBufferWithFace(PolygonalFace face)
        {
            #region Initialization
            // get the 3 vertices and their zheights
            var vA = Vertices[face.A.IndexInList];
            var zA = VertexZHeights[face.A.IndexInList];
            var vB = Vertices[face.B.IndexInList];
            var zB = VertexZHeights[face.B.IndexInList];
            var vC = Vertices[face.C.IndexInList];
            var zC = VertexZHeights[face.C.IndexInList];

            var area = (vB - vA).Cross(vC - vA);
            // if the area is negative the triangle is facing the wrong way.
            if (area <= 0) return area;

            // next re-organize the vertices as vMin, vMed, vMax - ordered by 
            // their x-values
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
            // the following 3 indices are the pixels where the 3 vertices reside in x
            var xStartIndex = GetXIndex(vMin.X);
            var xSwitchIndex = GetXIndex(vMed.X);
            var xEndIndex = GetXIndex(vMax.X);
            // x is snapped to the grid. This value should be a little less than vMin.X
            var x = GetSnappedX(xStartIndex);  //snapped vMin.X value;

            // set the y heights at the start to the same as the y-value of vMin
            var yBtm = vMin.Y;
            var yTop = vMin.Y;
            var yMin = Math.Min(vA.Y, Math.Min(vB.Y, vC.Y));
            var yBtmIndex = GetYIndex(yMin);
            var yMax = Math.Max(vA.Y, Math.Max(vB.Y, vC.Y));

            // define the lines emanating from vMin. Assume the intermediate vertex
            // is on the bottom path. Switch if that's wrong.
            // note the main variable below is called "slopeStep". Following the Bresanham
            // approach we don't need to deal with slopes, but rather the amount that we step
            // change from one pixel to the next. Hence, we multiple the slope by the pixel length.
            var slopeStepBtm = PixelSideLength * (vMed.Y - vMin.Y) / (vMed.X - vMin.X);
            var slopeStepTop = PixelSideLength * (vMax.Y - vMin.Y) / (vMax.X - vMin.X);
            var switchOnBottom = slopeStepBtm < slopeStepTop;
            if (!switchOnBottom)
            {
                var temp = slopeStepTop;
                slopeStepTop = slopeStepBtm;
                slopeStepBtm = temp;
            }
            //next, we have to handle the special cases where the x-values  are in the same column
            // first, the extremem case where all vertices in the same column or xStartIndex == xEndIndex
            if (xStartIndex >= xEndIndex)
            {
                slopeStepBtm = slopeStepTop = 0;
                yBtm = Math.Min(vA.Y, Math.Min(vB.Y, vC.Y));
                yTop = Math.Max(vA.Y, Math.Max(vB.Y, vC.Y));
                xSwitchIndex = -1;
            }
            // check if first 2 are in the same column
            else if (xStartIndex >= xSwitchIndex)
            {
                xSwitchIndex = -1; // this is to avoid problems in the loop below where we check when the index should switch
                if (switchOnBottom)
                {
                    // like above, but not the starting line goes from vMed to vMax
                    slopeStepBtm = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    yBtm = vMed.Y;
                }
                else
                {
                    slopeStepTop = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    yTop = vMed.Y;
                }
            }
            // very subtle issue! remember above where we set yBtm and yTop to vMin.Y. 
            // well, since x will be on the grid we need to move these slightly to be on the closest
            // grid.
            if (switchOnBottom && xSwitchIndex < 0)
                yBtm += (x - vMed.X) * slopeStepBtm * inversePixelSideLength;
            else yBtm += (x - vMin.X) * slopeStepBtm * inversePixelSideLength;
            if (!switchOnBottom && xSwitchIndex < 0)
                yTop += (x - vMed.X) * slopeStepTop * inversePixelSideLength;
            else yTop += (x - vMin.X) * slopeStepTop * inversePixelSideLength;
            // it doesn't hurt to move yTop up a little more to avoid rounding errors in the loop's 
            // exit condition. One-hundredth of the pixel side length doesn't require anymore iteration
            // but ensures that y<= yTop won't mess up
            yTop += 0.01 * PixelSideLength;
            #endregion
            // *** main loop ***
            var vBAx = vB.X - vA.X;
            var vBAy = vB.Y - vA.Y;
            var qVaX = x - vA.X;
            var vCAx = vC.X - vA.X;
            var vCAy = vC.Y - vA.Y;
            for (var xIndex = xStartIndex; xIndex <= xEndIndex; xIndex++)
            {
                var yIndex =  Math.Max(GetYIndex(yBtm), yBtmIndex);
                var yBtmSnapped = GetSnappedY(yIndex);
                var vBAy_multiply_qVaX = vBAy * qVaX;
                var vCAy_multiply_qVaX = vCAy * qVaX;
                var index = GetIndex(xIndex, yIndex);
                var stop = Math.Min(yTop, yMax);
                for (var y = yBtmSnapped; y <= stop; y+= PixelSideLength)
                {
                    var qVaY = y - vA.Y;

                    // check the values of x and y  with the barycentric approach
                    //borrowing notation from:https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/barycentric-coordinates
                    //Area = (vB - vA).Cross(q - vA)
                    var area2 = vBAx * qVaY - vBAy_multiply_qVaX;
                    if (area2 >= 0 && area2 <= area)
                    {
                        //Area = (q - vA).Cross(vC - vA)
                        var area3 = vCAy_multiply_qVaX - qVaY * vCAx;
                        if (area3 >= 0 && area3 <= area)
                        {
                            var v = area2 / area;
                            var u = area3 / area;
                            var w = 1 - v - u;
                            if (w >= 0.0 && w <= 1.0)
                            {  
                                var zIntercept = w * zA + u * zB + v * zC;
                                // since the grid is not initialized, we update it if the grid cell is empty or if we found a better face
                                var tuple = Values[index];
                                if (tuple == default || zIntercept > tuple.Item2)
                                    Values[index] = (face, zIntercept);
                            }
                        }
                    }
                    index++;
                }
                // step change in the y values.
                qVaX += PixelSideLength;
                yBtm += slopeStepBtm;
                yTop += slopeStepTop;
                // if we are at the intermediate vertex, then we switch. Should this be before the step change? No, that produces
                // incorrect results. Why? I don't know.
                if (xIndex == xSwitchIndex)
                {
                    if (switchOnBottom)
                        slopeStepBtm = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                    else
                        slopeStepTop = PixelSideLength * (vMax.Y - vMed.Y) / (vMax.X - vMed.X);
                }
            }
            return area / 2; // the areas in this function were actually parallelogram areas. need to divide by 2 for triangle area
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
        public Vector3 Get3DPointTransformed(int i, int j)
        {
            return new Vector3(MinX + i * PixelSideLength, MinY + j * PixelSideLength, Values[YCount * i + j].Item2);
        }
        /// <summary>
        /// Gets the 3D point on the solid corresponding to pixel i, j.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="j">The j.</param>
        /// <returns>Vector3.</returns>
        public Vector3 Get3DPoint(int i, int j)
        {
            return Get3DPointTransformed(i, j).Transform(backTransform);
        }
    }
}
