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
    public class SphericalBuffer : ZBuffer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SphericalBuffer"/> class.
        /// </summary>
        /// <param name="solid">The solid.</param>
        /// <param name="direction">The direction.</param>
        /// <param name="pixelsPerRow">The pixels per row.</param>
        /// <param name="pixelBorder">The pixel border.</param>
        public static SphericalBuffer Run(TessellatedSolid solid, Vector3 center,
            int pixelsPerRow, int pixelBorder = 2, IEnumerable<TriangleFace> subsetFaces = null)
        {
            var sphBuffer = new SphericalBuffer(solid);

            var minAzimuth = double.PositiveInfinity;
            var maxAzimuth = double.NegativeInfinity;
            var minPolar = double.PositiveInfinity;
            var maxPolar = double.NegativeInfinity;
            var maxRadius = 0.0;
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var v = solid.Vertices[i].Coordinates - center;
                var radius = v.Length();
                var polarAngle = Math.Acos(v.Z / radius);
                var azimuthAngle = Math.Atan2(v.Y, v.X);
                sphBuffer.Vertices[i] = new Vector2(polarAngle, azimuthAngle);
                sphBuffer.VertexZHeights[i] = radius;
                if (radius > maxRadius) maxRadius = radius;
            }
            for (int i = 0; i < solid.NumberOfVertices; i++)
            {
                var v = sphBuffer.Vertices[i];
                v *= maxRadius;
                if (v.X < minPolar) minPolar = v.X;
                if (v.X > maxPolar) maxPolar = v.X;
                if (v.Y < minAzimuth) minAzimuth = v.Y;
                if (v.Y > maxAzimuth) maxAzimuth = v.Y;
                sphBuffer.Vertices[i] = v;
            }
            //Finish initializing the grid now that we have the bounds.
            sphBuffer.Initialize(minAzimuth, maxAzimuth, minPolar, maxPolar, pixelsPerRow, pixelBorder);
            var faces = subsetFaces != null ? subsetFaces : sphBuffer.solidFaces;
            foreach (TriangleFace face in faces)
                sphBuffer.UpdateZBufferWithFace(face);
            return sphBuffer;
        }
        private SphericalBuffer(TessellatedSolid solid) : base(solid) { }
    }
}
