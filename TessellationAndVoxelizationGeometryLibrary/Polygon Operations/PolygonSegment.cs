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
                    _length = Vector.Length();
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
        public Vector2 Vector
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
        private Vector2 _vector = Vector2.Null;

        /// <summary>
        /// Gets the center.
        /// </summary>
        /// <value>The center.</value>
        public Vector2 Center
        {
            get
            {
                if (_center.IsNull())
                    _center = new Vector2((ToPoint.X + FromPoint.X) / 2, (ToPoint.Y + FromPoint.Y) / 2);
                return _center;
            }
        }

        /// <summary>
        /// The center
        /// </summary>
        private Vector2 _center = Vector2.Null;

        /// <summary>
        /// Gets the y intercept.
        /// </summary>
        /// <value>The y intercept.</value>
        public double YIntercept
        {
            get
            {
                if (double.IsNaN(_yIntercept))
                    _yIntercept = FindYGivenX(0, out _);
                return _yIntercept;
            }
        }

        /// <summary>
        /// The y intercept
        /// </summary>
        private double _yIntercept = double.NaN;

        /// <summary>
        /// Gets the x intercept.
        /// </summary>
        /// <value>The x intercept.</value>
        public double XIntercept
        {
            get
            {
                if (double.IsNaN(_xIntercept))
                    _xIntercept = FindXGivenY(0, out _);
                return _xIntercept;
            }
        }

        /// <summary>
        /// The x intercept
        /// </summary>
        private double _xIntercept = double.NaN;

        /// <summary>
        /// Gets the vertical slope (delta-Y / delta-X). A vertical line would have infinite slope.
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

        /// <summary>
        /// The vertical slope
        /// </summary>
        private double _verticalSlope = double.NaN;

        /// <summary>
        /// Gets the horizontal slope (delta X/delta Y). A horizontal line would have infinite slope.
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

        /// <summary>
        /// The horizontal slope
        /// </summary>
        private double _horizontalSlope = double.NaN;

        /// <summary>
        /// Gets the x maximum.
        /// </summary>
        /// <value>The x maximum.</value>
        public double XMax { get; private set; }
        /// <summary>
        /// Gets the x minimum.
        /// </summary>
        /// <value>The x minimum.</value>
        public double XMin { get; private set; }
        /// <summary>
        /// Gets the y maximum.
        /// </summary>
        /// <value>The y maximum.</value>
        public double YMax { get; private set; }
        /// <summary>
        /// Gets the y minimum.
        /// </summary>
        /// <value>The y minimum.</value>
        public double YMin { get; private set; }

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
            XMax = (FromPoint.X > ToPoint.X) ? FromPoint.X : ToPoint.X;
            XMin = (FromPoint.X < ToPoint.X) ? FromPoint.X : ToPoint.X;
            YMax = (FromPoint.Y > ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
            YMin = (FromPoint.Y < ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
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
            _center = Vector2.Null;
            _horizontalSlope = double.NaN;
            _length = double.NaN;
            _vector = Vector2.Null;
            _verticalSlope = double.NaN;
            _xIntercept = double.NaN;
            _yIntercept = double.NaN;
            XMax = (FromPoint.X > ToPoint.X) ? FromPoint.X : ToPoint.X;
            XMin = (FromPoint.X < ToPoint.X) ? FromPoint.X : ToPoint.X;
            YMax = (FromPoint.Y > ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
            YMin = (FromPoint.Y < ToPoint.Y) ? FromPoint.Y : ToPoint.Y;
        }

        #endregion Methods
    }
}