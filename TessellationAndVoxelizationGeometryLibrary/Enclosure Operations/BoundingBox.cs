// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Matt Campbell
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-12-2015
// ***********************************************************************
// <copyright file="BoundingBox.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using MIConvexHull;
using TVGL.Tessellation;

namespace TVGL
{
    /// <summary>
    /// The BoundingBox struct is a simple structure for representing an arbitrarily oriented box
    /// or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in 
    /// "Directions", the extreme vertices, and the volume.
    /// </summary>
    public struct BoundingBox
    {
        /// <summary>
        /// The volume of the bounding box.
        /// </summary>
        public double Volume;
        /// <summary>
        /// The extreme vertices which are vertices of the tessellated solid that are on the faces
        /// of the bounding box. These are not the corners of the bounding box.
        /// </summary>
        public IVertex[] ExtremeVertices;
        /// <summary>
        /// The Directions are the three unit vectors that describe the orientation of the box.
        /// </summary>
        public double[][] Directions;

        /// <summary>
        /// The corner points
        /// </summary>
        public Point[] CornerPoints;
        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingBox"/> class.
        /// </summary>
        /// <param name="volume">The volume.</param>
        /// <param name="extremeVertices">The extreme vertices.</param>
        /// <param name="directions">The directions.</param>
        internal BoundingBox(double volume, IVertex[] extremeVertices, double[][] directions)
        {
            Volume = volume;
            ExtremeVertices = extremeVertices;
            Directions = directions;

            //todo: find corner points. These are the 8 points at the intersections of the
            // planes defined by the extremeVertices and the direction vectors
            CornerPoints = null;
        }
    }
}
