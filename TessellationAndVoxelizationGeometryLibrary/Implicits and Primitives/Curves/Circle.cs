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

        /// <summary>
        ///     Create a new circle from 3 points
        /// </summary>
        /// <param name="p0"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        internal Circle(Vector2 p0, Vector2 p1, Vector2 p2)
        {
            NumPointsDefiningCircle = 3;
            Point0 = p0;
            Point1 = p1;
            Point2 = p2;
            var point0X = Point0.X;
            var point0Y = Point0.Y;
            var point1X = Point1.X;
            var point1Y = Point1.Y;
            var point2X = Point2.X;
            var point2Y = Point2.Y;

            //Find Circle center and radius
            //Assume NO two points are exactly the same 
            var rise1 = point1Y - point0Y;
            var run1 = point1X - point0X;
            var rise2 = point2Y - point1Y;
            var run2 = point2X - point1X;
            double x;
            double y;
            //Check for special cases of vertical or horizontal lines
            if (rise1.IsNegligible(Constants.BaseTolerance)) //If rise is zero, x can be found directly
            {
                x = (point0X + point1X) / 2;
                //If run of other line is approximately zero as well, y can be found directly
                if (run2.IsNegligible(Constants.BaseTolerance))
                //If run is approximately zero, y can be found directly
                {
                    y = (point1Y + point2Y) / 2;
                }
                else
                {
                    //Find perpendicular slope, and midpoint of line 2. 
                    //Then use the midpoint to find "b" and solve y = mx+b
                    //This is condensed into a single line because VS rounds the numbers 
                    //during division.
                    y = (point1Y + point2Y) / 2 + -run2 / rise2 * (x - (point1X + point2X) / 2);
                }
            }
            else if (rise2.IsNegligible(Constants.BaseTolerance))
            //If rise is approximately zero, x can be found directly
            {
                x = (point1X + point2X) / 2;
                //If run of other line is approximately zero as well, y can be found directly
                if (run1.IsNegligible(Constants.BaseTolerance))
                //If run is approximately zero, y can be found directly
                {
                    y = (point0Y + point1Y) / 2;
                }
                else
                {
                    //Find perpendicular slope, and midpoint of line 2. 
                    //Then use the midpoint to find "b" and solve y = mx+b
                    //This is condensed into a single line because VS rounds the numbers 
                    //during division.
                    y = (point0Y + point1Y) / 2 + -run1 / rise1 * (x - (point0X + point1X) / 2);
                }
            }
            else if (run1.IsNegligible(Constants.BaseTolerance))
            //If run is approximately zero, y can be found directly
            {
                y = (point0Y + point1Y) / 2;
                //Find perpendicular slope, and midpoint of line 2. 
                //Then use the midpoint to find "b" and solve y = mx+b
                //This is condensed into a single line because VS rounds the numbers 
                //during division.
                x = (y - ((point1Y + point2Y) / 2 - -run2 / rise2 *
                          (point1X + point2X) / 2)) / (-run2 / rise2);
            }
            else if (run2.IsNegligible(Constants.BaseTolerance))
            //If run is approximately zero, y can be found directly
            {
                y = (point1Y + point2Y) / 2;
                //Find perpendicular slope, and midpoint of line 2. 
                //Then use the midpoint to find "b" and solve y = mx+b
                //This is condensed into a single line because VS rounds the numbers 
                //during division.
                x = (y - ((point1Y + point0Y) / 2 - -run1 / rise1 *
                          (point1X + point0X) / 2)) / (-run1 / rise1);
            }
            else
            {
                //Didn't solve for slopes first because of rounding error in division
                //ToDo: This does not always find a good center. Figure out why.
                x = (rise1 / run1 * (rise2 / run2) * (point2Y - point0Y) +
                     rise1 / run1 * (point1X + point2X) -
                     rise2 / run2 * (point0X + point1X)) / (2 * (rise1 / run1 - rise2 / run2));
                y = -(1 / (rise1 / run1)) * (x - (point0X + point1X) / 2) +
                    (point0Y + point1Y) / 2;
            }

            var dx = x - point0X;
            var dy = y - point0Y;
            CenterX = x;
            CenterY = y;
            SqRadius = dx * dx + dy * dy;
        }

        internal Circle(Vector2 p0, Vector2 p1)
        {
            NumPointsDefiningCircle = 2;
            Point0 = p0;
            Point1 = p1;
            var point0X = p0.X;
            var point0Y = p0.Y;
            CenterX = point0X + (Point1.X - point0X) / 2;
            CenterY = point0Y + (Point1.Y - point0Y) / 2;
            var dx = CenterX - point0X;
            var dy = CenterY - point0Y;
            SqRadius = dx * dx + dy * dy;
        }
        #endregion

        private readonly Vector2 _dummyPoint = new Vector2(double.NaN, double.NaN);

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

        internal double CenterX { get; }

        internal double CenterY { get; }

        internal double SqRadius { get; set; }

        #endregion
    }
}

