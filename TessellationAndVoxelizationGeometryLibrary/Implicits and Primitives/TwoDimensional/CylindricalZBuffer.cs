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
            cylBuff.transform = direction.TransformToXYPlane(out cylBuff.backTransform);
            var rotatedAnchor = anchor.Transform(cylBuff.transform);
            cylBuff.transform *= Matrix4x4.CreateTranslation(-rotatedAnchor.X, -rotatedAnchor.Y, -rotatedAnchor.Z);

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
                var p = solid.Vertices[i].Coordinates.Transform(cylBuff.transform);
                var theta = Math.Atan2(p.Y, p.X);
                var radius = Math.Sqrt(p.X * p.X + p.Y * p.Y);
                cylBuff.Vertices[i] = new Vector2(p.Z, theta);
                cylBuff.VertexZHeights[i] = radius;
                if (radius > maxRadius) maxRadius = radius;
                if (p.Z < minX) minX = p.Z;
                if (p.Z > maxX) maxX = p.Z;
            }
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var y = maxRadius * cylBuff.Vertices[i].Y;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                cylBuff.Vertices[i] = new Vector2(cylBuff.Vertices[i].X, y);
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