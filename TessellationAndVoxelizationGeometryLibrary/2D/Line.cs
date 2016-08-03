using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using StarMathLib;

namespace TVGL
{
    /// <summary>
    /// A 2D Line made up of two points.
    /// </summary>
    public class Line
    {
        #region Constructor

        /// <summary>
        ///     Sets to and from points as well as slope and intercept of line.
        /// </summary>
        /// <param name="fromPoint"></param>
        /// <param name="toPoint"></param>
        /// <param name="twoWayReference"></param>
        internal Line(Point fromPoint, Point toPoint, bool twoWayReference = true)
        {
            FromPoint = fromPoint;
            ToPoint= toPoint;

            //Solve for slope and y intercept. 
            if (ToPoint.X.IsPracticallySame(FromPoint.X)) //If vertical line, set slope = inf.
            {
                Slope = double.PositiveInfinity;
                Yintercept = double.PositiveInfinity;
            }

            else if (ToPoint.Y.IsPracticallySame(FromPoint.Y)) //If horizontal line, set slope = 0.
            {
                Slope = 0.0;
                Yintercept = ToPoint.Y;
            }
            else //Else y = mx + Yintercept
            {
                Slope = (ToPoint.Y - FromPoint.Y) / (ToPoint.X - FromPoint.X);
                Yintercept = ToPoint.Y - Slope * ToPoint.X;
            }

            if (!twoWayReference) return;
            FromPoint.Lines.Add(this);
            ToPoint.Lines.Add(this);
        }
        #endregion
        
        #region Properties
        /// <summary>
        ///     Gets the Pointwhich the line is pointing to. Set is through the constructor.
        /// </summary>
        /// <value>To node.</value>
        internal Point ToPoint { get; private set; }

        /// <summary>
        ///     Gets the Pointwhich the line is pointing away from. Set is through the constructor.
        /// </summary>
        /// <value>From node.</value>
        internal Point FromPoint { get; private set; }
        
        /// <summary>
        ///     Gets the Slope.
        /// </summary>
        /// <value>The Slope.</value>
        internal double Slope { get; }

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        // ReSharper disable once InconsistentNaming
        /// <summary>
        ///     Gets the Yintercept.
        /// </summary>
        /// <value>The Yintercept.</value>
        internal double Yintercept { get; private set; }

        internal int IndexInList { get; set; }

        /// <summary>
        ///     Gets X intercept given Y
        /// </summary>
        /// <param name="y">The y.</param>
        /// <returns>System.Double.</returns>
        internal double Xintercept(double y)
        {
            //If basically a vertical line, return an x value on that line (e.g., ToNode.X)
            if (Slope >= double.PositiveInfinity)
            {
                return FromPoint.X;
            }

            //If a flat line give either positive or negative infinity depending on the direction of the line.
            if (Slope.IsNegligible())
            {
                if (ToPoint.X - FromPoint.X > 0)
                {
                    return double.PositiveInfinity;
                }
                return double.NegativeInfinity;
            }
            return (y - Yintercept) / Slope;
        }
        #endregion

        #region Internal Methods
        /// <summary>
        ///     Reverses this instance.
        /// </summary>
        internal void Reverse()
        {
            var tempPoint= FromPoint;
            FromPoint= ToPoint;
            ToPoint= tempPoint;
        }
        #endregion

        /// <summary>
        /// Gets the other point that makes up this line.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public Point OtherPoint(Point point)
        {
            if (point == FromPoint) return ToPoint;
            return point == ToPoint ? FromPoint : null;
        }
    }
}

