using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace TVGL.PointMatcherNet
{
    public static class VectorHelpers
    {
        public static Vector3 Mean(DataPoint[] points)
        {
            return Sum(points) / points.Length;
        }

        public static Vector3 Sum(IEnumerable<DataPoint> points)
        {
            return Sum(points.Select(p => p.point));
        }

        public static Vector3 Sum(IEnumerable<Vector3> vectors)
        {
            var sum = Vector3.Zero;
            foreach (var v in vectors)
            {
                sum += v;
            }

            return sum;
        }

        public static double AngularDistance(Quaternion q1, Quaternion q2)
        {
            var dot = Quaternion.Dot(q1, q2);
            dot = Math.Min(dot, 1);
            dot = Math.Max(dot, -1);

            return 2 * Math.Acos(dot);
        }

        public static double AverageSqDistance(DataPoints points, DataPoints points2)
        {
            var sum = 0.0;
            for (int i = 0; i < points.points.Length; i++)
            {
                sum += (points.points[i].point - points2.points[i].point).LengthSquared();
            }

            return sum / points.points.Length;
        }
    }
}
