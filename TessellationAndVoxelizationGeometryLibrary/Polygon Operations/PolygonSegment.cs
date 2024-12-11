// ***********************************************************************
// Assembly         : TessellationAndVoxelizationGeometryLibrary
// Author           : matth
// Created          : 04-03-2023
//
// Last Modified By : matth
// Last Modified On : 04-03-2023
// ***********************************************************************
// <copyright file="PolygonSegment.cs" company="Design Engineering Lab">
//     2014
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;

namespace TVGL
{
    /// <summary>
    /// NodeLine
    /// </summary>
    public class PolygonEdge

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
                    _length = Math.Sqrt(LengthSquared);
                return _length;
            }
        }

        /// <summary>
        /// The length
        /// </summary>
        private double _length = double.NaN;

        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <value>The length.</value>
        public double LengthSquared
        {
            get
            {
                if (double.IsNaN(_lengthSquared))
                    _lengthSquared = Vector2IP.DistanceSquared(ToPoint.Coordinates, FromPoint.Coordinates).AsDouble;
                return _lengthSquared;
            }
        }
        private double _lengthSquared = double.NaN;

        /// <summary>
        /// Gets the length of the line.
        /// </summary>
        /// <value>The length.</value>
        internal Vector2IP Vector
        {
            get
            {
                if (_vector.IsNull())
                    _vector = ToPoint.Coordinates - FromPoint.Coordinates;
                return _vector;
            }
        }
        /// <summary>
        /// The vector
        /// </summary>
        private Vector2IP _vector = Vector2IP.Zero;

        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        internal Vector2IP Center
        {
            get
            {
                if (_center.IsNull())
                    _center =Vector2IP.MidPoint(ToPoint.Coordinates,FromPoint.Coordinates);
                return _center;
            }
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector2IP _center;


        /// <summary>
        /// Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        internal RationalIP XMax { get; private set; }
        /// <summary>
        /// Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        internal RationalIP XMin { get; private set; }
        /// <summary>
        /// Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        internal RationalIP YMax { get; private set; }
        /// <summary>
        /// Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        internal RationalIP YMin { get; private set; }

        /// <summary>
        /// Gets the index in list.
        /// </summary>
        /// <value>The index in list.</value>
        public int IndexInList => ToPoint.IndexInList;

        #endregion Properties

        #region Constructor

        /// <summary>
        /// Sets to and from nodes as well as slope and intercept of line.
        /// </summary>
        /// <param name="fromNode">From node.</param>
        /// <param name="toNode">To node.</param>
        internal PolygonEdge(Vertex2D fromNode, Vertex2D toNode)
        {
            FromPoint = fromNode;
            ToPoint = toNode;
            if (RationalIP.CompareTo(FromPoint.Coordinates.X, FromPoint.Coordinates.W,
                ToPoint.Coordinates.X, ToPoint.Coordinates.W) > 0)
            {
                XMax = new RationalIP(FromPoint.Coordinates.X, FromPoint.Coordinates.W);
                XMin = new RationalIP(ToPoint.Coordinates.X, ToPoint.Coordinates.W);
            }
            else
            {
                XMax = new RationalIP(ToPoint.Coordinates.X, ToPoint.Coordinates.W);
                XMin = new RationalIP(FromPoint.Coordinates.X, FromPoint.Coordinates.W);
            }
            if (RationalIP.CompareTo(FromPoint.Coordinates.Y, FromPoint.Coordinates.W,
                ToPoint.Coordinates.Y, ToPoint.Coordinates.W) > 0)
            {
                YMax = new RationalIP(FromPoint.Coordinates.Y, FromPoint.Coordinates.W);
                YMin = new RationalIP(ToPoint.Coordinates.Y, ToPoint.Coordinates.W);
            }
            else
            {
                YMax = new RationalIP(ToPoint.Coordinates.Y, ToPoint.Coordinates.W);
                YMin = new RationalIP(FromPoint.Coordinates.Y, FromPoint.Coordinates.W);
            }
        }

        #endregion Constructor

        /// <summary>
        /// Gets the Vertex2D which the line is pointing to. Set is through the constructor.
        /// </summary>
        /// <value>To node.</value>
        public Vertex2D ToPoint { get; }

        /// <summary>
        /// Gets the Vertex2D which the line is pointing away from. Set is through the constructor.
        /// </summary>
        /// <value>From node.</value>
        public Vertex2D FromPoint { get; }

        #region Methods

        /// <summary>
        /// Determines whether [is adjacent to] [the specified other]. That is, do they share an endpoint or not.
        /// </summary>
        /// <param name="other">The other.</param>
        /// <returns><c>true</c> if [is adjacent to] [the specified other]; otherwise, <c>false</c>.</returns>
        public bool IsAdjacentTo(PolygonEdge other)
        {
            return (FromPoint == other.ToPoint
                || ToPoint == other.FromPoint);
        }

        /// <summary>
        /// Gets the other point that makes up this line.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>Vertex2D.</returns>
        public Vertex2D OtherPoint(Vertex2D point)
        {
            if (point == FromPoint) return ToPoint;
            return point == ToPoint ? FromPoint : null;
        }

        /// <summary>
        /// Reverses this instance.
        /// </summary>
        /// <returns>PolygonEdge.</returns>
        internal PolygonEdge Reverse()
        {
            return new PolygonEdge(ToPoint, FromPoint);
        }

        /// <summary>
        /// Returns Y value given an X value
        /// </summary>
        /// <param name="xval">The xval.</param>
        /// <param name="isBetweenEndPoints">if set to <c>true</c> [is between end points].</param>
        /// <returns>System.Double.</returns>
        public double FindYGivenX(double xval, out bool isBetweenEndPoints)
        {
            if (xval.IsPracticallySame(FromPoint.X))
            {
                isBetweenEndPoints = true;
                return FromPoint.Y;
            }
            if (xval.IsPracticallySame(ToPoint.X))
            {
                isBetweenEndPoints = true;
                return ToPoint.Y;
            }
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
        /// <param name="isBetweenEndPoints">if set to <c>true</c> [is between end points].</param>
        /// <returns>System.Double.</returns>
        public double FindXGivenY(double yval, out bool isBetweenEndPoints)
        {
            if (yval.IsPracticallySame(FromPoint.Y))
            {
                isBetweenEndPoints = true;
                return FromPoint.X;
            }
            if (yval.IsPracticallySame(ToPoint.Y))
            {
                isBetweenEndPoints = true;
                return ToPoint.X;
            }
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

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return "from: " + FromPoint + " to: " + ToPoint;
        }

        /// <summary>
        /// Resets this instance.
        /// </summary>
        internal void Reset()
        {
            _center = default;
            _length = double.NaN;
            _vector = default;
            XMax = (FromPoint.X > ToPoint.X) ? FromPoint.X : ToPoint.X;
            XMin = (FromPoint.X < ToPoint.X) ? FromPoint.X : ToPoint.X;
            YMax = (FromPoint.Y > ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
            YMin = (FromPoint.Y < ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
        }

        #endregion Methods
    }
}