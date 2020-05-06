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
        public Vector2 LengthDirection;

        /// <summary>
        ///     Vector direction of  width
        /// </summary>
        public Vector2 WidthDirection;

        /// <summary>
        ///     Maximum distance along Direction 1 (length)
        /// </summary>
        internal double LengthDirectionMax;

        /// <summary>
        ///     Minimum distance along Direction 1 (length)
        /// </summary>
        internal double LengthDirectionMin;

        /// <summary>
        ///     Maximum distance along Direction 2 (width)
        /// </summary>
        internal double WidthDirectionMax;

        /// <summary>
        ///     Minimum distance along Direction 2 (width)
        /// </summary>
        internal double WidthDirectionMin;

        /// <summary>
        ///     Length of Bounding Rectangle
        /// </summary>
        public double Length;

        /// <summary>
        ///     Width of Bounding Rectangle
        /// </summary>
        public double Width;

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
            var v1Max = LengthDirection * LengthDirectionMax;
            var v1Min = LengthDirection * LengthDirectionMin;
            var v2Max = WidthDirection * WidthDirectionMax;
            var v2Min = WidthDirection * WidthDirectionMin;
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