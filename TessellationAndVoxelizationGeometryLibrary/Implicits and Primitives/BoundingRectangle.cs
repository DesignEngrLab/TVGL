// Copyright 2015-2020 Design Engineering Lab
// This file is a part of TVGL, Tessellation and Voxelization Geometry Library
// https://github.com/DesignEngrLab/TVGL
// It is licensed under MIT License (see LICENSE.txt for details)
using System;
using System.Collections.Generic;
using TVGL.Numerics;
using TVGL.TwoDimensional;

namespace TVGL.Primitives
{
    /// <summary>
    ///     Bounding rectangle information based on area and point pairs.
    /// </summary>
    public readonly struct BoundingRectangle
    {
        /// <summary>
        ///     The Area of the bounding box.
        /// </summary>
        public readonly double Area;

        /// <summary>
        ///     The point pairs that define the bounding rectangle limits. Unlike bounding box
        ///     these go: dir1-min, dir2-max, dir1-max, dir2-min this is because you are going
        ///     around ccw
        /// </summary>
        public readonly List<Vector2>[] PointsOnSides;

        /// <summary>
        ///     Vector direction of length
        /// </summary>
        public readonly Vector2 Direction1;

        /// <summary>
        ///     Vector direction of  width
        /// </summary>
        public readonly Vector2 Direction2;

        /// <summary>
        ///     Offsets are the distances defining the lines of the rectangle. They are ordered:
        ///     Direction1-min, Direction1-max, Direction2-min, Direction2-max
        /// </summary>
        internal readonly double[] Offsets;

        /// <summary>
        ///     Length of Bounding Rectangle
        /// </summary>
        public readonly double Length1;

        /// <summary>
        ///     Width of Bounding Rectangle
        /// </summary>
        public readonly double Length2;

        /// <summary>
        ///     2D Center Position of the Bounding Rectangle
        /// </summary>
        public readonly Vector2 CenterPosition;

        public BoundingRectangle(Vector2 unitVectorAlongSide, Vector2 unitVectorPointInto, double d1Min, double d1Max, double d2Min,
            double d2Max, double length1 = double.NaN, double length2 = double.NaN, double area = double.NaN,
            List<Vector2>[] sidePoints = null)
        {
            Direction1 = unitVectorAlongSide;
            Direction2 = unitVectorPointInto;
            Offsets = new[] { d1Min, d1Max, d2Min, d2Max };
            Length1 = double.IsNaN(length1) ? d1Max - d1Min : length1;
            Length2 = double.IsNaN(length2) ? d2Max - d2Min : length2;
            Area = double.IsNaN(area) ? Length1 * Length2 : area;
            PointsOnSides = sidePoints;
            CenterPosition = Direction1 * (d1Min + 0.5 * Length1)
                + Direction2 * (d2Min + 0.5 * Length2);
        }


        /// <summary>
        ///     Gets the four points of the bounding rectangle, ordered CCW positive
        /// </summary>
        public Vector2[] CornerPoints()
        {
            var v1Min = Direction1 * Offsets[0];
            var v1Max = Direction1 * Offsets[1];
            var v2Min = Direction2 * Offsets[2];
            var v2Max = Direction2 * Offsets[3];
            var p1 = v1Min + v2Min;
            var p2 = v1Max + v2Min;
            var p3 = v1Max + v2Max;
            var p4 = v1Min + v2Max;
            var cornerPoints = new[] { p1, p2, p3, p4 };
            var areaCheck = cornerPoints.Area();
            if (areaCheck < 0.0)
            {
                cornerPoints = new[] { p4, p3, p2, p1 };
                areaCheck = -areaCheck;
                //Check to make sure the points are ordered correctly (within 1 %)
                if (!areaCheck.IsPracticallySame(Area, 0.01 * Area))
                    throw new Exception("Points are ordered incorrectly");
            }
            return cornerPoints;
        }
    }
}