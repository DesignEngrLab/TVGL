using System;
using System.Runtime.CompilerServices;

namespace TVGL
{
    internal static class PGA2D
    {
        internal static RationalIP YValueGivenXOnEdge(Int128 xNum, Int128 xDen, PolygonEdge edge, out bool onSegment)
        {
            var yValue = YValueGivenXOnLine(xNum, xDen, edge.Normal);
            if (yValue.IsEqualVectorY(edge.FromPoint.Coordinates))
                onSegment = true;
            onSegment = yValue.IsLessThanVectorY(edge.ToPoint.Coordinates)
                != yValue.IsLessThanVectorY(edge.FromPoint.Coordinates);
            return yValue;
        }
        internal static RationalIP YValueGivenXOnLine(Int128 xNum, Int128 xDen, Vector2IP lineNormal)
        {
            // intersection with vertical, which is {1, 0, w} where w is set so that x is on the plane
            // meaning that the dot product is zero. So, xNum/xDen + 0 + w = 0
            // therefore w = -xNum/xDen or to keep as integers, the vertical line, {1, 0, -xNum /xDen }
            // becomes {xDen, 0, -xNum}.
            return new RationalIP(xNum * lineNormal.X + xDen * lineNormal.W, -xDen * lineNormal.Y);
        }

        internal static RationalIP XValueGivenYOnEdge(Int128 yNum, Int128 yDen, PolygonEdge edge, out bool onSegment)
        {
            var xValue = YValueGivenXOnLine(yNum, yDen, edge.Normal);
            if (xValue.IsEqualVectorX(edge.FromPoint.Coordinates))
                onSegment = true;
            onSegment = xValue.IsLessThanVectorX(edge.ToPoint.Coordinates)
                != xValue.IsLessThanVectorX(edge.FromPoint.Coordinates);
            return xValue;
        }
        internal static RationalIP XValueGivenYOnLine(Int128 yNum, Int128 yDen, Vector2IP lineNormal)
        {
            // intersection with horiztontal, which is {0, 1, w} where w is set so that y is on the plane
            // meaning that the dot product is zero. So, 0 + yNum/yDen + w = 0
            // therefore w = -yNum/yDen or to keep as integers, the vertical line, { 0, 1, -yNum /yDen },
            // becomes {0, yDen, -yNum}.
            return new RationalIP(yNum * lineNormal.Y + yDen * lineNormal.W, -yDen * lineNormal.X);
        }
        internal static Vector2IP PointAtLineIntersection(Vector2IP lineNormal1, Vector2IP lineNormal2)
        {
            return lineNormal1.Cross(lineNormal2);
            // if (intersection.W == 0) //Lines are parallel, but what to return
        }

        internal static Vector2IP PointAtLineAndPolyEdge(Vector2IP lineNormal, PolygonEdge polyEdge)
        {
            var intersection = lineNormal.Cross(polyEdge.Normal);
            if (intersection.W == 0) return polyEdge.FromPoint.Coordinates;
            return intersection;

        }
        internal static Vector2IP PointAtLineAndPolyEdge(Vector2IP lineNormal, PolygonEdge polyEdge,
              out RationalIP t, out bool onSegment)
        {
            var point = lineNormal.Cross(polyEdge.Normal);
            t = FractionOnLineSegment(polyEdge, point, out onSegment);
            return point;
        }
        internal static Vector2IP PointAtPolyEdgeIntersection(PolygonEdge polyEdge1, PolygonEdge polyEdge2)
        {
            return polyEdge1.Normal.Cross(polyEdge2.Normal);
        }
        internal static Vector2IP PointAtPolyEdgeIntersection(PolygonEdge polyEdge1, PolygonEdge polyEdge2,
            out RationalIP t1, out bool onSegment1, out RationalIP t2, out bool onSegment2)
        {
            var point = polyEdge1.Normal.Cross(polyEdge2.Normal);
            t1 = FractionOnLineSegment(polyEdge1, point, out onSegment1);
            t2 = FractionOnLineSegment(polyEdge2, point, out onSegment2);
            return point;
        }

        internal static RationalIP ShortestDistancePointToLineSegment(PolygonEdge polygonEdge, Vector2IP point,
            out bool distanceIsAtEndPoint)
        {
            var pointOnLine = ClosestPointOnLineToPoint(polygonEdge.Normal, point);
            var fraction = FractionOnLineSegment(polygonEdge, pointOnLine, out var onSegment);
            if (onSegment)
            {
                distanceIsAtEndPoint = false;
                return Vector2IP.DistanceSquared2D(point, pointOnLine);
            }
            distanceIsAtEndPoint = true;
            if (fraction < RationalIP.Zero)
                return Vector2IP.DistanceSquared2D(point, polygonEdge.FromPoint.Coordinates);
            return Vector2IP.DistanceSquared2D(point, polygonEdge.ToPoint.Coordinates);
        }
        internal static RationalIP ShortestDistancePointToLine(Vector2IP lineNormal, Vector2IP point)
        {
            var pointOnLine = ClosestPointOnLineToPoint(lineNormal, point);
            return Vector2IP.DistanceSquared2D(point, pointOnLine);
        }
        internal static Vector2IP ClosestPointOnLineToPoint(Vector2IP lineNormal, Vector2IP point)
        {
            // find the orthogonal line that passes through the origin (xn2, yn2, 0). This would be found
            // by taking the cross product with the current line normal and the z-axis.
            // var orthPlane = lineNormal.Cross(new Vector2IP(0, 0, 1)); but to be speedier we'll write it 
            // out to avoid the multiplies by zero and one.
            var orthPlaneX = lineNormal.Y;
            var orthPlaneY = -lineNormal.X;
            // now we tilt the plane so that it passes through the point. As a point in the plane, we know
            // that the dot-product will be 0, so we can solve for the w value from this.
            //orthPlane.Dot3D(point) = 0;
            // orthPlaneX * point.X + orthPlaneY * point.Y + orthPlane.W * point.W = 0
            //var orthPlaneW = new RationalIP(-orthPlaneX * point.X - orthPlaneY * point.Y, point.W);
            // the point is the intersection of the two planes. We could call PointAtLineIntersection,
            // but the w now is rational and not integer. So we'll write it out.
            // oh wait, orthPlaneW is a rational, but the good news is that it's denominator is the
            // same as point
            var orthPlaneWNum = -orthPlaneX * point.X - orthPlaneY * point.Y;

            return new Vector2IP(orthPlaneY * point.W * point.W - orthPlaneWNum * point.Y,
                orthPlaneWNum * point.X - orthPlaneX * point.W * point.W,
                (orthPlaneX * point.Y - orthPlaneY * point.X) * point.W);
        }

        internal static RationalIP FractionOnLineSegment(PolygonEdge polygonEdge, Vector2IP point, out bool onSegment)
            => FractionOnLineSegment(polygonEdge.FromPoint.Coordinates, polygonEdge.ToPoint.Coordinates, point, out onSegment);
        internal static RationalIP FractionOnLineSegment(Vector2IP start, Vector2IP end, Vector2IP point,
            out bool onSegment)
        {
            if (end == point)
            {
                onSegment = false; // like indexing, we follow the rule inclusive of the first, exclusive of the last
                return RationalIP.One;
            }
            else if (start == point)
            {
                onSegment = true;
                return RationalIP.Zero;
            }
            var fullVector = end.Cross(start);
            var firstTriangle = point.Cross(start);
            var secondTriangle = end.Cross(point);
            Int128 firstSign, secondSign;
            if (fullVector.W == 0)
            {
                firstSign = Int128.Sign(fullVector.X) != Int128.Sign(firstTriangle.X) &&
                     Int128.Sign(fullVector.Y) != Int128.Sign(firstTriangle.Y) ? Int128.NegativeOne : Int128.One;
                secondSign = Int128.Sign(fullVector.X) != Int128.Sign(secondTriangle.X) &&
                     Int128.Sign(fullVector.Y) != Int128.Sign(secondTriangle.Y) ? Int128.NegativeOne : Int128.One;
            }
            else
            {
                firstSign = Int128.Sign(fullVector.W) != Int128.Sign(firstTriangle.W)
                    ? Int128.NegativeOne : Int128.One;
                secondSign = Int128.Sign(fullVector.W) != Int128.Sign(secondTriangle.W)
                    ? Int128.NegativeOne : Int128.One;
            }
            var firstLength = firstTriangle.Length3D();
            var secondLength = secondTriangle.Length3D();
            if (firstSign == Int128.One && secondSign == Int128.One)
            {
                onSegment = true;
                return new RationalIP(firstLength, firstLength + secondLength);
            }
            onSegment = false;
            var fullLength = fullVector.Length3D();

            if (firstSign == Int128.One)
                return new RationalIP(fullLength + secondLength, fullLength);
            return new RationalIP(-firstLength, fullLength);
        }

        internal static Vector2IP LineJoiningTwoPoints(Vector2IP from, Vector2IP to)
        {
            return to.Cross(from);
        }


        /// <summary>
        /// There currently is no method in C# to take the square root of an Int128.
        /// However, we occasionally need to do this. This method finds the integer
        /// component of the square root - as if you did a Floor function on the
        /// actual square root.
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Int128 SquareRoot(this Int128 num)
        {
            if (num < 0)
                throw new ArgumentException("Negative numbers are not " +
                    "allowed in this square root function.");
            if (num <= long.MaxValue)
                return (Int128)Math.Sqrt((long)num);
            // start out with approximation when converted to double
            var fracTerm = Math.Sqrt((double)num);
            // get the "floor" integer from the double
            var intTerm = (Int128)fracTerm;
            // squaring should give us close to the input, num
            // given that digits were likely lost in the conversion to 
            // double, then this might be larger than the expected answer
            var intTermSqd = intTerm * intTerm;

            if (intTermSqd < 0)
            {   // this happens for large numbers where the casting
                // to double and re-squaring leads to overflow (now a negative result)
                // it seems a slight fractional reduction is enough to solve the problem
                fracTerm /= 1.00000000001;
                intTerm = (Int128)fracTerm;
                intTermSqd = intTerm * intTerm;
            }
            // now here is the difference, generally positive but not always
            var delta = num - intTerm * intTerm;
            // consider the problem as (x + y)^2 = num where x is the integer
            // part and y is the fractional part. if the fractional part is
            // greater than 1 then we add to the integer part.
            intTerm += delta / (intTerm << 1);
            // occasionally this is still larger. this happens because 2xy +y^2 
            // produce and extra unit, but never more than once.
            while (intTerm * intTerm > num)
                intTerm--;
            return intTerm;
        }
    }
}
