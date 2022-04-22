using System;
using System.Collections.Generic;
using System.Linq;
using MIConvexHull;
using StarMathLib;


namespace TVGL
{
    public readonly struct StraightLine3D : ICurve
    {

        public readonly Vector3 Anchor;

        public readonly Vector3 Direction;

        public StraightLine3D(Vector3 anchor, Vector3 direction)
        {
            Anchor = anchor;
            Direction = direction;
        }

        public double SquaredErrorOfNewPoint<T>(T point) where T : IVertex2D
        {
            if (point is IVertex3D vector3D)
            {
                var cross = (new Vector3(vector3D.X - Anchor.X, vector3D.Y - Anchor.Y, vector3D.Z - Anchor.Z)).Cross(Direction);
                return cross.Dot(cross);
            }
            else return double.PositiveInfinity;
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        // this is based on this: https://stackoverflow.com/questions/24747643/3d-linear-regression/67303867#67303867

        public static bool CreateFromPoints<T>(IEnumerable<T> points, out ICurve curve, out double error) where T : IVertex2D
        {
            double x, y, z;
            double xSqd, ySqd, zSqd;
            double xy, xz, yz;
            x = y = z = xSqd = ySqd = zSqd = xy = xz = yz = 0.0;
            var n = 0;
            foreach (var point in points)
            {
                if (!(point is IVertex3D point3D))
                {
                    curve = null;
                    error = 0;
                    return false;
                }
                var px = point.X;
                var py = point.Y;
                var pz = ((IVertex3D)point).Z;
                x += px;
                y += py;
                z += pz;
                xSqd += px * px;
                ySqd += py * py;
                zSqd += pz * pz;
                xy += px * py;
                xz += px * pz;
                yz += py * pz;
                n++;
            }
            x /= n;
            y /= n;
            z /= n;
            xSqd /= n;
            ySqd /= n;
            zSqd /= n;
            xy /= n;
            xz /= n;
            yz /= n;
            if (n == 2)
            {
                var p1 = points.First();
                var p2 = points.Last();
                var anchor = new Vector3(p1.X, p1.Y, ((IVertex3D)p1).Z);
                var dir = new Vector3(p2.X, p2.Y, ((IVertex3D)p2).Z) - anchor;
                curve = new StraightLine3D(anchor, dir - anchor);
                error = 0;
                return !dir.IsNegligible();
            }

            var matrix = new double[,] {
                { xSqd - x * x, xy - x * y,   xz - x * z},
                { xy - x * y,   ySqd - y * y, yz - y * z }   ,
                { xz - x * z,   yz - y * z,   zSqd - z * z }
            };
            var eigens = StarMath.GetEigenValuesAndVectors(matrix, out var eigenVectors);
            var indexOfLargestEigenvalue =
                (Math.Abs(eigens[0].Real) >= Math.Abs(eigens[1].Real) &&
                Math.Abs(eigens[0].Real) >= Math.Abs(eigens[2].Real))
                ? 0 :
                (Math.Abs(eigens[1].Real) >= Math.Abs(eigens[0].Real) &&
                Math.Abs(eigens[1].Real) >= Math.Abs(eigens[2].Real))
                ? 1 : 2;
            var direction = new Vector3(eigenVectors[indexOfLargestEigenvalue]);
            curve = new StraightLine3D(new Vector3(x, y, z), direction.Normalize());
            //var result = new StraightLine3D(new Vector3(x, y, z), direction.Normalize());
            error = 0.0;
            foreach (var point in points)
                error += curve.SquaredErrorOfNewPoint(point);
            error /= n;
            return true;
        }
    }
}
