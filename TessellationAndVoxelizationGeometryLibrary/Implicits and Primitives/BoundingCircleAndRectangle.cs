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

using MIConvexHull;
using System;
using System.Collections.Generic;
using System.Linq;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL
{
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
        ///     Gets the four points of the bounding rectangle, ordered CCW positive
        /// </summary>
        public Vector2[] CornerPoints;

        /// <summary>
        ///     The point pairs that define the bounding rectangle limits. Unlike bounding box
        ///     these go: dir1-min, dir2-max, dir1-max, dir2-min this is because you are going
        ///     around ccw
        /// </summary>
        public List<Vector2>[] PointsOnSides;

        /// <summary>
        ///     Vector direction of length 
        /// </summary>
        public Vector2 Direction1;

        /// <summary>
        ///     Vector direction of  width
        /// </summary>
        public Vector2 Direction2;

        /// <summary>
        ///     Offsets are the distances defining the lines of the rectangle. They are ordered:
        ///     Direction1-min, Direction1-max, Direction2-min, Direction2-max
        /// </summary>
        internal double[] Offsets;

        /// <summary>
        ///     Length of Bounding Rectangle
        /// </summary>
        public double Length1;

        /// <summary>
        ///     Width of Bounding Rectangle
        /// </summary>
        public double Length2;

        /// <summary>
        ///     2D Center Position of the Bounding Rectangle
        /// </summary>
        public Vector2 CenterPosition;

        /// <summary>
        /// Sets the corner points and center position for the bounding rectangle
        /// </summary>
        /// <exception cref="Exception"></exception>
        public void SetCornerPoints()
        {
            var v1Min = Direction1 * Offsets[0];
            var v1Max = Direction1 * Offsets[1];
            var v2Min = Direction2 * Offsets[2];
            var v2Max = Direction2 * Offsets[3];
            var p1 = v1Max + v2Max;
            var p2 = v1Min + v2Max;
            var p3 = v1Min + v2Min;
            var p4 = v1Max + v2Min;
            CornerPoints = new[] { p1, p2, p3, p4 };
            var areaCheck = CornerPoints.Area();
            if (areaCheck < 0.0)
            {
                CornerPoints = new[] { p4, p3, p2, p1 };
                areaCheck = -areaCheck;
            }

            //Add in the center
            var centerPosition = new[] { 0.0, 0.0 };
            var cX = 0.0;
            var cY = 0.0;
            foreach (var vertex in CornerPoints)
            {
                cX += vertex.X;
                cY += vertex.Y;
            }

            //Check to make sure the points are ordered correctly (within 1 %)
            if (!areaCheck.IsPracticallySame(Area, 0.01 * Area))
                throw new Exception("Points are ordered incorrectly");
            CenterPosition = new Vector2(0.25 * cX, 0.25 * cY);
        }
    }


    /// <summary>
    ///     Public circle structure, given a center point and radius
    /// </summary>
    public struct BoundingCircle
    {
        /// <summary>
        ///     Center Point of circle
        /// </summary>
        public Vector2 Center;

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
        public BoundingCircle(double radius, Vector2 center)
        {
            Center = center;
            Radius = radius;
            Area = Math.PI * radius * radius;
            Circumference = Constants.TwoPi * radius;
        }
    }
}