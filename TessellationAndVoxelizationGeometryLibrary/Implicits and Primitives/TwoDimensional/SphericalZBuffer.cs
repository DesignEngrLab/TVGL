// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-14-2023
// ***********************************************************************
// <copyright file="SphericalBuffer.cs" company="Design Engineering Lab">
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
    /// Class SphericalBuffer. This class cannot be inherited.
    /// </summary>
    public class SphericalBuffer : CylindricalBuffer
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
            int pixelBorder, IEnumerable<TriangleFace> subsetFaces)
        { throw new ArgumentException("The direction and the anchor (possibly just the origin?) are needed for the cylindrical buffer."); }

        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalBuffer"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public static SphericalBuffer Run(TessellatedSolid solid, Vector3 center,
            int pixelsPerRow, IEnumerable<TriangleFace> subsetFaces = null)
        {
            var sphBuffer = new SphericalBuffer(solid);
            sphBuffer.center = center;
            sphBuffer.baseRadius = 0.0;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var v = solid.Vertices[i].Coordinates - center;
                var azimuthAngle = Math.Atan2(v.Y, v.X);
                var radius = v.Length();
                var polarAngle = Math.Acos(v.Z / radius);
                sphBuffer.Vertices[i] = new Vector2(azimuthAngle, polarAngle);
                sphBuffer.VertexZHeights[i] = radius;
                if (radius > sphBuffer.baseRadius)
                    sphBuffer.baseRadius = radius;
            }
            for (int i = 0; i < solid.NumberOfVertices; i++)
                sphBuffer.Vertices[i] *= sphBuffer.baseRadius;

            //Finish initializing the grid now that we have the bounds.
            var piRadius = Math.PI * sphBuffer.baseRadius;
            sphBuffer.Initialize(-piRadius, piRadius, 0, piRadius,
                pixelsPerRow);

            //Finish initializing the grid now that we have the bounds.
            for (int i = 0; i < solid.Edges.Length; i++)
            {
                Edge edge = solid.Edges[i];
                var rightIsOutward = edge.Vector.Cross(Vector3.UnitZ);
                var dot = rightIsOutward.Dot(edge.From.Coordinates - center);
                if (dot.IsNegligible()) continue;
                var stepIsPositive = dot > 0;
                var toVertexX = sphBuffer.Vertices[edge.To.IndexInList].X;
                var fromVertexX = sphBuffer.Vertices[edge.From.IndexInList].X;
                if (fromVertexX > toVertexX == stepIsPositive)
                    sphBuffer.edgeWrapsAround[i] = true;
            }
            var faces = subsetFaces != null ? subsetFaces : sphBuffer.solidFaces;
            foreach (TriangleFace face in faces)
                sphBuffer.UpdateZBufferWithFace(face);
            return sphBuffer;
        }
        private SphericalBuffer(TessellatedSolid solid) : base(solid) { }
        /// <summary>
        /// Initializes the specified minimum x.
        /// </summary>
        /// <param name="minX">The minimum x.</param>
        /// <param name="maxX">The maximum x.</param>
        /// <param name="minY">The minimum y.</param>
        /// <param name="maxY">The maximum y.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public new void Initialize(double minX, double maxX, double minY, double maxY,
            int pixelsPerRow)
        {
            MinX = minX;
            MinY = minY;
            var XLength = maxX - MinX;

            //Calculate the size of a pixel based on the max of the two dimensions in question. 
            //Subtract pixelsPerRow by 1, since we will be adding a half a pixel to each side.
            PixelSideLength = XLength / pixelsPerRow;
            inversePixelSideLength = 1 / PixelSideLength;
            XCount = pixelsPerRow;
            YCount = pixelsPerRow / 2; // unlike the linear ZBuffer and Cylindrical ZBuffer,
            MinX += PixelSideLength / 2;
            MinY += PixelSideLength / 2;
            MaxIndex = XCount * YCount - 1;
            Values = new (TriangleFace, double)[XCount * YCount];
        }


        private Vector3 center;



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
            var azimuthAngle = flatPoint.X / baseRadius;
            var polarAngle = flatPoint.Y / baseRadius;
            var radius = baseRadius + flatPoint.Z;
            var point = SphericalAnglePair.ConvertSphericalToCartesian(radius, polarAngle, azimuthAngle);
            return point + center;
        }
    }
}
