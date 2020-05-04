// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : Design Engineering Lab
// Created          : 04-18-2016
//
// Last Modified By : Design Engineering Lab
// Last Modified On : 05-26-2016
// ***********************************************************************
// <copyright file="SpecialClasses.cs" company="Design Engineering Lab">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using TVGL.Numerics;


namespace TVGL.TwoDimensional
{
    /// <summary>
    ///     NodeLine
    /// </summary>
    public class PolygonSegment
    {
        #region Properties
        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <value>The length.</value>
        public double Length
        {
            get
            {
                if (double.IsNaN(_length))
                    _length = Vector.Length();
                return _length;
            }
        }
        double _length = double.NaN;

        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <value>The length.</value>
        public Vector2 Vector
        {
            get
            {
                if (_vector.IsNull())
                    _vector = ToPoint.Coordinates - FromPoint.Coordinates;
                return _vector;
            }
        }
        Vector2 _vector = Vector2.Null;


        public Vector2 Center
        {
            get
            {
                if (_center.IsNull())
                    _center = new Vector2((ToPoint.X + FromPoint.X) / 2, (ToPoint.Y + FromPoint.Y) / 2);
                return _center;
            }
        }
        Vector2 _center = Vector2.Null;

        public double YIntercept
        {
            get
            {
                if (double.IsNaN(_yIntercept))
                    _yIntercept = YGivenX(0, out _);
                return _yIntercept;
            }
        }
        double _yIntercept = double.NaN;
        public double XIntercept
        {
            get
            {
                if (double.IsNaN(_xIntercept))
                    _xIntercept = XGivenY(0, out _);
                return _xIntercept;
            }
        }
        double _xIntercept = double.NaN;


        /// <summary>
        /// Gets the vertical slope.
        /// </summary>
        /// <value>The vertical slope.</value>
        public double VerticalSlope
        {
            get
            {
                if (double.IsNaN(_verticalSlope))
                    _verticalSlope = Vector.Y / Vector.X;
                return _verticalSlope;
            }
        }
        double _verticalSlope = double.NaN;

        /// <summary>
        /// Gets the horizontal slope.
        /// </summary>
        /// <value>The horizontal slope.</value>
        public double HorizontalSlope
        {
            get
            {
                if (double.IsNaN(_horizontalSlope))
                    _horizontalSlope = Vector.X / Vector.Y;
                return _horizontalSlope;
            }
        }
        double _horizontalSlope = double.NaN;
        #endregion

        #region Constructor
        /// <summary>
        ///     Sets to and from nodes as well as slope and intercept of line.
        /// </summary>
        /// <param name="fromNode">From node.</param>
        /// <param name="toNode">To node.</param>
        internal PolygonSegment(Vertex2D fromNode, Vertex2D toNode)
        {
            FromPoint = fromNode;
            ToPoint = toNode;
        }
        #endregion
        /// <summary>
        ///     Gets the Vertex2D which the line is pointing to. Set is through the constructor.
        /// </summary>
        /// <value>To node.</value>
        internal Vertex2D ToPoint { get; }

        /// <summary>
        ///     Gets the Vertex2D which the line is pointing away from. Set is through the constructor.
        /// </summary>
        /// <value>From node.</value>
        internal Vertex2D FromPoint { get; }



        #region Methods
        /// <summary>
        /// Gets the other point that makes up this line.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Vertex2D OtherPoint(Vertex2D point)
        {
            if (point == FromPoint) return ToPoint;
            return point == ToPoint ? FromPoint : null;
        }
        /// <summary>
        ///     Reverses this instance.
        /// </summary>
        internal PolygonSegment Reverse()
        {
            return new PolygonSegment(ToPoint, FromPoint);
        }

        /// <summary>
        /// Returns Y value given an X value
        /// </summary>
        /// <param name="xval"></param>
        /// <returns></returns>
        public double YGivenX(double xval, out bool isBetweenEndPoints)
        {
            isBetweenEndPoints = (FromPoint.X < xval) != (ToPoint.X < xval);
            // if both true or both false then endpoints are on same side of point
            if (FromPoint.Y.IsPracticallySame(ToPoint.Y))
            {
                //Any y value on the line will do
                return FromPoint.Y;
            }
            if (FromPoint.X.IsPracticallySame(ToPoint.X))
            {
                isBetweenEndPoints = (xval.IsPracticallySame(FromPoint.X));
                //return either positive or negative infinity depending on the direction of the line.
                if (ToPoint.Y - FromPoint.Y > 0)
                    return double.MaxValue;
                return double.MinValue;
            }
            return VerticalSlope * (xval - FromPoint.X) + FromPoint.Y;
        }

        /// <summary>
        /// Returns X value given a Y value
        /// </summary>
        /// <param name="yval">The y.</param>
        /// <returns>System.Double.</returns>
        public double XGivenY(double yval, out bool isBetweenEndPoints)
        {
            isBetweenEndPoints = (FromPoint.Y < yval) != (ToPoint.Y < yval);
            // if both true or both false then endpoints are on same side of point
            //If a vertical line, return an x value on that line (e.g., ToNode.X)
            if (FromPoint.X.IsPracticallySame(ToPoint.X))
            {
                return FromPoint.X;
            }

            //If a flat line give either positive or negative infinity depending on the direction of the line.
            if (FromPoint.Y.IsPracticallySame(ToPoint.Y))
            {
                isBetweenEndPoints = (yval.IsPracticallySame(FromPoint.Y));
                if (ToPoint.X - FromPoint.X > 0)
                    return double.MaxValue;
                return double.MinValue;
            }
            return HorizontalSlope * (yval - FromPoint.Y) + FromPoint.X;
        }
        #endregion
    }

}