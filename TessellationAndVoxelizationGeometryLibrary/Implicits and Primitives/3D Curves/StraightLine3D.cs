using System;
using System.Collections.Generic;
using StarMathLib;
using TVGL.Numerics;

namespace TVGL.Primitives
{
    public readonly struct StraightLine3D 
    // this is 3D so, it doesn't inherit from ICurve, and since it's not a surface - it doesn't inherit
    // from primitive surface. But interestingly, it is basically a cylinder with zero radius
    {

        public readonly Vector3 Anchor;

        public readonly Vector3 Direction;

        public StraightLine3D(Vector3 anchor, Vector3 direction)
        {
            Anchor = anchor;
            Direction = direction;
        }
        public double SquaredErrorOfNewPoint(Vector3 point)
        {
            var cross = (point - Anchor).Cross(Direction);
            return cross.Dot(cross);
        }

        /// <summary>
        /// Defines the best fit of the curve for the given points.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        // this is based on this: https://stackoverflow.com/questions/24747643/3d-linear-regression/67303867#67303867
        public static StraightLine3D CreateFromPoints(IEnumerable<Vector3> points, out double error)
        {
            double x, y, z;
            double xSqd, ySqd, zSqd;
            double xy, xz, yz;
            x = y = z = xSqd = ySqd = zSqd = xy = xz = yz = 0.0;
            var n = 0;
            foreach (var point in points)
            {
                var px = point.X;
                var py = point.Y;
                var pz = point.Z;
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
            var result = new StraightLine3D(new Vector3(x, y, z), direction.Normalize());
            error = 0.0;
            foreach (var point in points)
                error += result.SquaredErrorOfNewPoint(point);
            error /= n;
            return result;
        }
    }
}
