// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 02-27-2015
//
// Last Modified By : Matt Campbell
// Last Modified On : 02-12-2016
// ***********************************************************************
// <copyright file="BoundingBox.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace TVGL
{
    /// <summary>
    ///     The BoundingBox struct is a simple structure for representing an arbitrarily oriented box
    ///     or 3D prismatic rectangle. It simply includes the orientation as three unit vectors in
    ///     "Directions2D", the extreme vertices, and the volume.
    /// </summary>
    public struct BoundingBox
    {
        /// <summary>
        ///     The volume of the bounding box.
        /// </summary>
        public double Volume;

        /// <summary>
        ///     The dimensions of the bounding box. The 3 values correspond to the 3 direction.
        /// </summary>
        public double[] Dimensions;

        /// <summary>
        ///     The PointsOnFaces is an array of 6 lists which are vertices of the tessellated solid that are on the faces
        ///     of the bounding box. These are not the corners of the bounding box. They are in the order of direction1-low,
        ///     direction1-high, direction2-low, direction2-high, direction3-low, direction3-high.
        /// </summary>
        public List<Vertex>[] PointsOnFaces;

        /// <summary>
        ///     The Directions normal are the three unit vectors that describe the orientation of the box.
        /// </summary>
        public double[][] Directions;

        /// <summary>
        ///     The corner points
        /// </summary>
        public Vertex[] CornerVertices;

        /// <summary>
        ///     The center point
        /// </summary>
        public Vertex Center;
    }

    /// <summary>
    ///     Bounding rectangle information based on area and point pairs.
    /// </summary>
    public struct BoundingRectangle
    {
        /// <summary>
        ///     The Area of the bounding box.
        /// </summary>
        public double Area;

        /// <summary>
        ///     The point pairs that define the bounding rectangle limits
        /// </summary>
        public List<Point>[] PointsOnSides;

        /// <summary>
        ///     Vector directions of length and width of rectangle
        /// </summary>
        public double[][] Directions2D;

        /// <summary>
        ///     Length and Width of Bounding Rectangle
        /// </summary>
        public double[] Dimensions;
    }


    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public struct BoundingCircle
    {
        /// <summary>
        ///     Center Point of circle
        /// </summary>
        public Point Center;

        /// <summary>
        ///     Radius of circle
        /// </summary>
        public double Radius;

        /// <summary>
        ///     Area of circle
        /// </summary>
        public double Area;

        /// <summary>
        ///     Circumference of circle
        /// </summary>
        public double Circumference;

        /// <summary>
        ///     Creates a circle, given a radius. Center point is optional
        /// </summary>
        /// <param name="radius">The radius.</param>
        /// <param name="center">The center.</param>
        public BoundingCircle(double radius, Point center = null)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI*radius*radius;
            Circumference = Constants.TwoPi*radius;
        }
    }

    /// <summary>
    ///     Public cylinder structure
    /// </summary>
    public struct BoundingCylinder
    {
        /// <summary>
        ///     Center axis along depth
        /// </summary>
        public double[] Axis;

        /// <summary>
        ///     Bounding Circle on one end of the cylinder
        /// </summary>
        public BoundingCircle BoundingCircle;

        /// <summary>
        ///     Height of cylinder
        /// </summary>
        public double Height;

        /// <summary>
        ///     Volume
        /// </summary>
        public double Volume;
    }
}