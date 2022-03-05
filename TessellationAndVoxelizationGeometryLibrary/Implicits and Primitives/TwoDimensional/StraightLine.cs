using System;
using System.Collections.Generic;
using TVGL.Numerics;

namespace TVGL.TwoDimensional
{
    public readonly struct StraightLine : ICurve
    {

        public readonly Vector2 Anchor;

        public readonly Vector2 Direction;

        public StraightLine(Vector2 anchor, Vector2 direction)
        {
            Anchor = anchor;
            Direction = direction;
        }

        public double SquaredErrorOfNewPoint(Vector2 point)
        {
            var fromAnchor = point - Anchor;
            var cross = fromAnchor.Cross(Direction);
            return cross * cross;
        }

        public static bool CreateFromPoints(IEnumerable<Vector2> points, out StraightLine straightLine, out double error)
        {
            double xCoeff;
            double yCoeff;
            bool ConstantIsZero;
            // based on word file, we will solve the two simultaneous equations with substitution
            double g = 0.0, h = 0.0, k = 0.0, m = 0.0, n = 0.0;
            var numPoints = 0;
            foreach (var p in points)
            {
                g += p.X * p.X;
                h += p.X * p.Y;
                k += p.Y * p.Y;
                m += p.X;
                n += p.Y;
                numPoints++;
            }
            if (numPoints < 2)
            {
                straightLine = new StraightLine();
                error = double.PositiveInfinity;
                return false;
            }
            var denom = k * g - h * h;
            if (denom.IsNegligible())
            {
                ConstantIsZero = true;
                if (g.IsNegligible())
                {
                    yCoeff = 0;
                    xCoeff = 1;
                }
                else
                {
                    yCoeff = 1;
                    xCoeff = h / g;
                }
            }
            else
            {
                ConstantIsZero = false;
                yCoeff = (n * g - m * h) / denom;
                xCoeff = (m - yCoeff * h) / g;
            }
            if (yCoeff == 0) //line is vertical
                straightLine = new StraightLine(new Vector2(1 / xCoeff, 0), Vector2.UnitY);
            else
            {
                var anchor = ConstantIsZero ? Vector2.Zero : new Vector2(0, 1 / yCoeff);
                straightLine = new StraightLine(anchor, new Vector2(yCoeff, -xCoeff).Normalize());
            }
            error = 0.0;
            foreach (var p in points)
                error += straightLine.SquaredErrorOfNewPoint(p);
            error /= numPoints;
            return true;
        }
    }
}
