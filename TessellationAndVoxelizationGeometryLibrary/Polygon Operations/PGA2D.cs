using System;
using System.Runtime.CompilerServices;

namespace TVGL
{
    internal static class PGA2D
    {
        internal static Vector2IP PointAtLineIntersection(Vector2IP lineNormal1, Vector2IP lineNormal2)
        {
            return lineNormal1.Cross(lineNormal2);
        }

        internal static Vector2IP PointAtLineAndPolyEdge(Vector2IP lineNormal, PolygonEdge polyEdge)
        {
            return lineNormal.Cross(polyEdge.Normal);
        }
        internal static Vector2IP PointAtLineAndPolyEdge(Vector2IP lineNormal, PolygonEdge polyEdge,
              out RationalIP t, out bool onSegment)
        {
            var point= lineNormal.Cross(polyEdge.Normal);
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
            intTerm += Int128.DivRem(delta, intTerm << 1).Quotient;
            // occasionally this is still larger. this happens because 2xy +y^2 
            // produce and extra unit, but never more than once.
            while (intTerm * intTerm > num)
                intTerm--;
            return intTerm;
        }
    }
}
