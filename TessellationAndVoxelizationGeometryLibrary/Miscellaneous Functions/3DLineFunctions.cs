using System;
using StarMathLib;
using TVGL.Tessellation;

namespace TVGL
{
    static class LineFunctions
    {
        internal static double SmallerAngleBetweenEdges(Edge edge1, Edge edge2)
        {
            throw new NotImplementedException();
        }

        internal static double ClockwiseAngleBetweenEdges(Edge edge1, Edge edge2, double[] axis)
        {
            throw new NotImplementedException();

        }
        internal static double CounterClockwiseAngleBetweenEdges(Edge edge1, Edge edge2, double[] axis)
        {

            throw new NotImplementedException();
        }
        internal static double AngleBetweenEdgesCW(Point a, Point b, Point c)
        {

        }
        internal static double AngleBetweenEdgesCCW(Point a, Point b, Point c)
        {
            var angleAB = Math.Atan2(b.Y - a.Y, b.X - a.X);
            var angleBC = Math.Atan2(c.Y - b.Y, c.X - b.X);
            var angleChange = Math.PI - (angleBC - angleAB);
            if (angleChange < 0) return angleChange + Math.PI;  
            return angleChange;
        }

        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2, out double[] DirectionOfLine, out double[] PointOnLine)
        {
            DirectionOfLine = n1.crossProduct(n2).normalize();
            LineIntersectingTwoPlanes(n1, d1, n2, d2, DirectionOfLine, out PointOnLine);
        }
        internal static void LineIntersectingTwoPlanes(double[] n1, double d1, double[] n2, double d2, double[] DirectionOfLine, out double[] PointOnLine)
        {
            /* to find the point on the line...well a point on the line, it turns out that one has three unknowns (px, py, pz)
             * and only two equations. Let's put the point on the plane going through the origin. So this plane would have a normal 
             * of v (or DirectionOfLine). */
            var A = new double[3, 3];
            A.SetRow(0, n1);
            A.SetRow(1, n2);
            A.SetRow(2, DirectionOfLine);
            var b = new[] { d1, d2, 0 };
            PointOnLine = StarMath.solve(A, b);
        }
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2)
        {
            double[] center;
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out t1, out t2);
        }
        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2,
            out double[] center)
        {
            double t1, t2;
            return SkewedLineIntersection(p1, n1, p2, n2, out center, out t1, out t2);
        }

        internal static double SkewedLineIntersection(double[] p1, double[] n1, double[] p2, double[] n2, out double[] center,
            out double t1, out double t2)
        {
            var a00 = n1[0] * n1[0] + n1[1] * n1[1] + n1[2] * n1[2];
            var a01 = -n1[0] * n2[0] - n1[1] * n2[1] - n1[2] * n2[2];
            var a10 = n1[0] * n2[0] + n1[1] * n2[1] + n1[2] * n2[2];
            var a11 = -n2[0] * n2[0] - n2[1] * n2[1] - n2[2] * n2[2];
            var b0 = n1[0] * (p2[0] - p1[0]) + n1[1] * (p2[1] - p1[1]) + n1[2] * (p2[2] - p1[2]);
            var b1 = n2[0] * (p2[0] - p1[0]) + n2[1] * (p2[1] - p1[1]) + n2[2] * (p2[2] - p1[2]);
            var A = new[,] { { a00, a01 }, { a10, a11 } };
            var b = new[] { b0, b1 };
            var t = StarMath.solve(A, b);
            t1 = t[0];
            t2 = t[1];
            var interSect1 = new[] { p1[0] + n1[0] * t1, p1[1] + n1[1] * t1, p1[2] + n1[2] * t1 };
            var interSect2 = new[] { p2[0] + n2[0] * t2, p2[1] + n2[1] * t2, p2[2] + n2[2] * t2 };
            center = new[] { (interSect1[0] + interSect2[0]) / 2, (interSect1[1] + interSect2[1]) / 2, (interSect1[2] + interSect2[2]) / 2 };
            return DistancePointToPoint(interSect1, interSect2);
        }

        /// <summary>
        /// Returns the distance the point to line.
        /// </summary>
        /// <param name="qPoint">The q point that is off of the line.</param>
        /// <param name="lineRefPt">The line reference point on the line.</param>
        /// <param name="lineVector">The line direction vector.</param>
        /// <returns></returns>
        internal static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector)
        {
            double[] dummy;
            return DistancePointToLine(qPoint, lineRefPt, lineVector, out dummy);
        }
        /// <summary>
        /// Distances the point to line.
        /// </summary>
        /// <param name="qPoint">q is the point that is off of the line.</param>
        /// <param name="lineRefPt">p is a reference point on the line.</param>
        /// <param name="lineVector">n is the vector of the line direction.</param>
        /// <param name="pointOnLine">The point on line closest to point, q.</param>
        /// <returns></returns>
        internal static double DistancePointToLine(double[] qPoint, double[] lineRefPt, double[] lineVector, out double[] pointOnLine)
        {
            /* pointOnLine is found by setting the dot-product of the lineVector and the vector formed by (pointOnLine-p) 
             * set equal to zero. This is really just solving to "t" the distance along the line from the lineRefPt. */
            var t = (lineVector[0] * (qPoint[0] - lineRefPt[0]) + lineVector[1] * (qPoint[1] - lineRefPt[1]) + lineVector[2] * (qPoint[2] - lineRefPt[2]))
                / (lineVector[0] * lineVector[0] + lineVector[1] * lineVector[1] + lineVector[2] * lineVector[2]);
            pointOnLine = new[] { lineRefPt[0] + lineVector[0] * t, lineRefPt[1] + lineVector[1] * t, lineRefPt[2] + lineVector[2] * t };
            return DistancePointToPoint(qPoint, pointOnLine);
        }

        /// <summary>
        /// Distances the point to point.
        /// </summary>
        /// <param name="p1">point, p1.</param>
        /// <param name="p2">point, p2.</param>
        /// <returns>the distance between the two 3D points.</returns>
        internal static double DistancePointToPoint(double[] p1, double[] p2)
        {
            var dX = p1[0] - p2[0];
            var dY = p1[1] - p2[1];
            var dZ = p1[2] - p2[2];
            return Math.Sqrt(dX * dX + dY * dY + dZ * dZ);
        }

        internal static Vertex PointOnPlaneFromIntersectingLine(double[] normalOfPlane, double distOfPlane, Vertex point1, Vertex point2)
        {
            var d1 = normalOfPlane.dotProduct(point1.Position);
            var d2 = normalOfPlane.dotProduct(point2.Position);
            var fraction = (d1 - distOfPlane) / (d1 - d2);
            var position = new double[3];
            for (var i = 0; i < 3; i++)
                position[i] = point2.Position[i] * fraction + point1.Position[i] * (1 - fraction);
            return new Vertex(position);

        }
    }
}
