using System;
using System.Collections.Generic;
using System.Text;
using TVGL.Numerics;

namespace TVGL.Curves
{
    /// <summary>
    ///     Class Circle.
    /// </summary>
    public class Circle : ConicSection
    {
        #region Constructor

        #endregion

        private readonly Vector2 _dummyPoint = new Vector2(double.NaN, double.NaN);

        public Circle(double radius, Vector3 center)
        {
            Radius = radius; 
            Center = center;
        }

        /// <summary>
        ///     Finds the furthest the specified point.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <param name="furthestPoint">The furthest point.</param>
        /// <param name="previousPoint1"></param>
        /// <param name="previousPoint2"></param>
        /// <exception cref="ArgumentNullException">previousPoints cannot be null</exception>
        internal void Furthest(Vector2 point, out Vector2 furthestPoint, out Vector2 previousPoint1, out Vector2 previousPoint2, out int numPreviousPoints)
        {
            //Distance between point and center is greater than radius, it is outside the circle
            //DO P0, then P1, then P2
            numPreviousPoints = (NumPointsDefiningCircle == 3) ? 2 : 1;
            previousPoint2 = _dummyPoint;
            var p0SquareDistance = Math.Pow(Point0.X - point.X, 2) + Math.Pow(Point0.Y - point.Y, 2);
            var p1SquareDistance = Math.Pow(Point1.X - point.X, 2) + Math.Pow(Point1.Y - point.Y, 2);
            if (p0SquareDistance > p1SquareDistance)
            {
                previousPoint1 = Point1;
                if (NumPointsDefiningCircle == 3)
                {
                    var p2SquareDistance = Math.Pow(Point2.X - point.X, 2) + Math.Pow(Point2.Y - point.Y, 2);
                    if (p0SquareDistance > p2SquareDistance)
                    {
                        furthestPoint = Point0;
                        previousPoint2 = Point2;
                    }
                    else
                    {
                        //If P2 > P0 and P0 > P1, P2 must also be greater than P1.
                        furthestPoint = Point2;
                        previousPoint2 = Point0;
                    }
                }
                else
                {
                    furthestPoint = Point0;
                }
            }
            else
            {
                previousPoint1 = Point0;
                if (NumPointsDefiningCircle == 3)
                {
                    var p2SquareDistance = Math.Pow(Point2.X - point.X, 2) + Math.Pow(Point2.Y - point.Y, 2);
                    if (p1SquareDistance > p2SquareDistance)
                    {
                        furthestPoint = Point1;
                        previousPoint2 = Point2;
                    }
                    else
                    {
                        furthestPoint = Point2;
                        previousPoint2 = Point1;
                    }
                }
                else
                {
                    furthestPoint = Point1;
                }
            }
        }

        #region Properties

        /// <summary>
        ///     Gets one point of the circle.
        /// </summary>
        /// <value>The points.</value>
        internal Vector2 Point0 { get; }

        /// <summary>
        ///     Gets one point of the circle.
        /// </summary>
        /// <value>The points.</value>
        internal Vector2 Point1 { get; }

        /// <summary>
        ///     Gets one point of the circle. This point may not exist.
        /// </summary>
        /// <value>The points.</value>
        internal Vector2 Point2 { get; }

        /// <summary>
        ///     Gets the number of points that define the circle. 2 or 3.
        /// </summary>
        /// <value>The points.</value>
        internal int NumPointsDefiningCircle { get; }

        internal Vector3 Center { get; }


        internal double Radius { get; set; }

        #endregion
    }
}

