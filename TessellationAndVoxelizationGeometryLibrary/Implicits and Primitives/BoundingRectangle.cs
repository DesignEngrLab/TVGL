// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="BoundingRectangle.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;



namespace TVGL
{
    /// <summary>
    /// Bounding rectangle information based on area and point pairs.
    /// </summary>
    public readonly struct BoundingRectangle
    {
        /// <summary>
        /// The Area of the bounding box.
        /// </summary>
        public readonly double Area;

        /// <summary>
        /// The point pairs that define the bounding rectangle limits. Unlike bounding box
        /// these go: dir1-min, dir2-max, dir1-max, dir2-min this is because you are going
        /// around ccw
        /// </summary>
        public readonly List<Vector2>[] PointsOnSides;

        /// <summary>
        /// Vector direction of length
        /// </summary>
        public readonly Vector2 Direction1;

        /// <summary>
        /// Vector direction of  width
        /// </summary>
        public readonly Vector2 Direction2;

        /// <summary>
        /// Offsets are the distances defining the lines of the rectangle. They are ordered:
        /// Direction1-min, Direction1-max, Direction2-min, Direction2-max
        /// </summary>
        internal double Offsets(int index)
        {
            if (index == 0) return MinD1;
            if (index == 1) return MaxD1;
            if (index == 2) return MinD2;
            return MaxD2;
        }
        public double MinD1 { get; private init; }
        public double MaxD1 { get; private init; }
        public double MinD2 { get; private init; }
        public double MaxD2 { get; private init; }

        /// <summary>
        /// Length of Bounding Rectangle
        /// </summary>
        public readonly double Length1;

        /// <summary>
        /// Width of Bounding Rectangle
        /// </summary>
        public readonly double Length2;

        /// <summary>
        /// 2D Center Coordinates of the Bounding Rectangle
        /// </summary>
        public readonly Vector2 CenterPosition;

        /// <summary>
        /// Initializes a new instance of the <see cref="BoundingRectangle"/> struct.
        /// </summary>
        /// <param name="unitVectorAlongSide">The unit vector along side.</param>
        /// <param name="unitVectorPointInto">The unit vector point into.</param>
        /// <param name="d1Min">The d1 minimum.</param>
        /// <param name="d1Max">The d1 maximum.</param>
        /// <param name="d2Min">The d2 minimum.</param>
        /// <param name="d2Max">The d2 maximum.</param>
        /// <param name="length1">The length1.</param>
        /// <param name="length2">The length2.</param>
        /// <param name="area">The area.</param>
        /// <param name="sidePoints">The side points.</param>
        public BoundingRectangle(Vector2 unitVectorAlongSide, Vector2 unitVectorPointInto, double d1Min, double d1Max, double d2Min,
            double d2Max, double length1 = double.NaN, double length2 = double.NaN, double area = double.NaN,
            List<Vector2>[] sidePoints = null)
        {
            Direction1 = unitVectorAlongSide;
            Direction2 = unitVectorPointInto;
            MinD1 = d1Min;
            MaxD1 = d1Max;
            MinD2 = d2Min;
            MaxD2 = d2Max;
            Length1 = double.IsNaN(length1) ? d1Max - d1Min : length1;
            Length2 = double.IsNaN(length2) ? d2Max - d2Min : length2;
            Area = double.IsNaN(area) ? Length1 * Length2 : area;
            PointsOnSides = sidePoints;
            CenterPosition = Direction1 * (d1Min + 0.5 * Length1)
                + Direction2 * (d2Min + 0.5 * Length2);
        }

        public Matrix3x3 TransformToSquaredAtOrigin
        {
            get
            {
                var minPoint = Direction1 * MinD1 + Direction2 * MinD2;
                var translation = Matrix3x3.CreateTranslation(-minPoint);
                var rotation = Matrix3x3.CreateRotation(-Math.Atan2(Direction1.Y, Direction1.X));
                return translation * rotation;
            }
        }


        /// <summary>
        /// Gets the four points of the bounding rectangle, ordered CCW positive
        /// </summary>
        /// <returns>Vector2[].</returns>
        /// <exception cref="System.Exception">Points are ordered incorrectly</exception>
        public Vector2[] CornerPoints()
        {
            var v1Min = Direction1 * MinD1;
            var v1Max = Direction1 * MaxD1;
            var v2Min = Direction2 * MinD2;
            var v2Max = Direction2 * MaxD2;
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

        public static double OverlappingArea(BoundingRectangle a, BoundingRectangle b)
        {
           return Math.Max(0, (Math.Min(a.MaxD1, b.MaxD1) - Math.Max(a.MinD1, b.MinD1)))
                * Math.Max(0, (Math.Min(a.MaxD2, b.MaxD2) - Math.Max(a.MinD2, b.MinD2)));
        }

        public static double DifferenceArea(BoundingRectangle a, BoundingRectangle b)
        {
            return a.Area + b.Area - 2 * OverlappingArea(a, b);
        }

    }
}