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
using System.Linq;

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
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public static CylindricalBuffer Run(TessellatedSolid solid, Vector3 direction, Vector3 anchor,
            int pixelsPerRow, int pixelBorder = 2, IEnumerable<TriangleFace> subsetFaces = null)
        {
            var cylBuff = new CylindricalBuffer(solid);

            // get the transform matrix to apply to every point
            var transform = direction.TransformToXYPlane(out var backTransform);
            var rotatedAnchor = anchor.Transform(transform);
            transform *= Matrix4x4.CreateTranslation(-rotatedAnchor.X, -rotatedAnchor.Y, -rotatedAnchor.Z);

            // transform points so that z-axis is aligned for the z-buffer. 
            // store x-y pairs as Vector2's (points) and z values in zHeights
            // also get the bounding box so determine pixel size
            var minX = double.PositiveInfinity;
            var maxX = double.NegativeInfinity;
            var minY = double.PositiveInfinity;
            var maxY = double.NegativeInfinity;
            var maxRadius = 0.0;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var p = solid.Vertices[i].Coordinates.Transform(transform);
                var theta =Math.PI+ Math.Atan2(p.Y, p.X);
                var radius = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                cylBuff.Vertices[i] = new Vector2(theta, p.Z);
                cylBuff.VertexZHeights[i] = radius;
                if (radius > maxRadius) maxRadius = radius;
                if (p.Z < minY) minY = p.Z;
                if (p.Z > maxY) maxY = p.Z;
            }
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var x = maxRadius * cylBuff.Vertices[i].X;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                cylBuff.Vertices[i] = new Vector2(x, cylBuff.Vertices[i].Y);
            }

            //Finish initializing the grid now that we have the bounds.
            cylBuff.Initialize(minX, maxX, minY, maxY, pixelsPerRow, pixelBorder);
            var faces = subsetFaces != null ? subsetFaces : cylBuff.solidFaces;
            foreach (TriangleFace face in faces)
                cylBuff.UpdateZBufferWithFace(face);
            return cylBuff;
        }
        private CylindricalBuffer(TessellatedSolid solid) : base(solid) { }
    }
}
