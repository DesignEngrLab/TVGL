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
                    _length = Math.Sqrt(LengthSquared.AsDouble);
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
        internal RationalIP LengthSquared
        {
            get
            {
                if (_lengthSquared.IsNull())
                    _lengthSquared = Vector2IP.DistanceSquared2D(ToPoint.Coordinates, FromPoint.Coordinates);
                return _lengthSquared;
            }
        }
        private RationalIP _lengthSquared;

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
        private Vector2IP _vector = Vector2IP.Zero;


        internal Vector2IP Normal
        {
            get
            {
                if (normal.IsNull())
                    normal = ToPoint.Coordinates.Cross(FromPoint.Coordinates);
                return normal;
            }
        }
        private Vector2IP normal = Vector2IP.Zero;



        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        internal Vector2IP Center
        {
            get
            {
                if (_center.IsNull())
                    _center = Vector2IP.MidPoint(ToPoint.Coordinates, FromPoint.Coordinates);
                return _center;
            }
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector2IP _center;
        private RationalIP xMax;
        private RationalIP xMin;
        private RationalIP yMax;
        private RationalIP yMin;


        /// <summary>
        /// Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        internal RationalIP XMax
        {
            get
            {
                if (xMax.IsNull()) SetXLimits();
                return xMax;
            }
        }
        /// <summary>
        /// Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        internal RationalIP XMin
        {
            get
            {
                if (xMin.IsNull()) SetXLimits();
                return xMin;
            }
        }

        /// <summary>
        /// Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        internal RationalIP YMax
        {
            get
            {
                if (yMax.IsNull()) SetYLimits();
                return yMax;
            }
        }

        /// <summary>
        /// Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        internal RationalIP YMin
        {
            get
            {
                if (yMin.IsNull()) SetYLimits();
                return yMin;
            }
        }

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
        }

        void SetXLimits()
        {
            if (RationalIP.CompareTo(FromPoint.Coordinates.X, FromPoint.Coordinates.W,
                ToPoint.Coordinates.X, ToPoint.Coordinates.W) > 0)
            {
                xMax = new RationalIP(FromPoint.Coordinates.X, FromPoint.Coordinates.W);
                xMin = new RationalIP(ToPoint.Coordinates.X, ToPoint.Coordinates.W);
            }
            else
            {
                xMax = new RationalIP(ToPoint.Coordinates.X, ToPoint.Coordinates.W);
                xMin = new RationalIP(FromPoint.Coordinates.X, FromPoint.Coordinates.W);
            }
        }

        void SetYLimits()
        {
            if (RationalIP.CompareTo(FromPoint.Coordinates.Y, FromPoint.Coordinates.W,
                ToPoint.Coordinates.Y, ToPoint.Coordinates.W) > 0)
            {
                yMax = new RationalIP(FromPoint.Coordinates.Y, FromPoint.Coordinates.W);
                yMin = new RationalIP(ToPoint.Coordinates.Y, ToPoint.Coordinates.W);
            }
            else
            {
                yMax = new RationalIP(ToPoint.Coordinates.Y, ToPoint.Coordinates.W);
                yMin = new RationalIP(FromPoint.Coordinates.Y, FromPoint.Coordinates.W);
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
            xMax = default;
            xMin = default;
            yMax = default;
            yMin = default;
        }

        #endregion Methods
    }
}