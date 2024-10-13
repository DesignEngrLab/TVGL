using PointCloud;
using PointCloud.Numerics;
using StarMathLib;

namespace PointCloudNet
{
    public static class PlaneMethods
    {
        /// <summary>
        /// Defines the normal and distance from vertices.
        /// </summary>
        /// <param name="vertices">The vertices.</param>
        /// <param name="distanceToPlane">The distance to plane.</param>
        /// <param name="normal">The normal.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DefineNormalAndDistanceFromVertices(this IEnumerable<Vector3> vertices, out double distanceToPlane,
            out Vector3 normal)
        {
            var pointList = vertices as IList<Vector3> ?? vertices.ToList();
            var numVertices = pointList.Count;
            if (numVertices < 3)
            {
                distanceToPlane = double.NaN;
                normal = Vector3.NaN;
                return false;
            }
            if (numVertices == 3)
            {
                var cross = (pointList[1] - pointList[0]).Cross(pointList[2] - pointList[1]);
                var crossLength = cross.Length();
                if (crossLength.IsNegligible())
                {
                    distanceToPlane = double.NaN;
                    normal = Vector3.NaN;
                    return false;
                }
                normal = cross / crossLength;
                distanceToPlane = normal.Dot((pointList[0] + pointList[1] + pointList[2]) / 3);
                if (distanceToPlane < 0)
                {
                    distanceToPlane = -distanceToPlane;
                    normal = -normal;
                }
                return true;
            }
            double xSum = 0.0, ySum = 0.0, zSum = 0.0;
            double xSq = 0.0;
            double xy = 0.0, ySq = 0.0;
            double xz = 0.0, yz = 0.0, zSq = 0.0;
            var x = pointList.First().X;
            var y = pointList.First().Y;
            var z = pointList.First().Z;
            var xIsConstant = true;
            var yIsConstant = true;
            var zIsConstant = true;
            foreach (var vertex in pointList)
            {
                if (vertex.IsNull()) continue;
                xIsConstant &= vertex.X.IsPracticallySame(x);
                x = vertex.X;
                yIsConstant &= vertex.Y.IsPracticallySame(y);
                y = vertex.Y;
                zIsConstant &= vertex.Z.IsPracticallySame(z);
                z = vertex.Z;
                xSum += x;
                ySum += y;
                zSum += z;
                xSq += x * x;
                ySq += y * y;
                zSq += z * z;
                xy += x * y;
                xz += x * z;
                yz += y * z;
            }
            if ((xIsConstant && yIsConstant) || (xIsConstant && zIsConstant) || (yIsConstant && zIsConstant))
            {
                distanceToPlane = double.NaN;
                normal = Vector3.NaN;
                return false;
            }
            if (xIsConstant)
            {
                if (x < 0)
                {
                    normal = -Vector3.UnitX;
                    distanceToPlane = -x;
                }
                else
                {
                    normal = Vector3.UnitX;
                    distanceToPlane = x;
                }
                return true;
            }
            if (yIsConstant)
            {
                if (y < 0)
                {
                    normal = -Vector3.UnitY;
                    distanceToPlane = -y;
                }
                else
                {
                    normal = Vector3.UnitY;
                    distanceToPlane = y;
                }
                return true;
            }
            if (zIsConstant)
            {
                if (z < 0)
                {
                    normal = -Vector3.UnitZ;
                    distanceToPlane = -z;
                }
                else
                {
                    normal = Vector3.UnitZ;
                    distanceToPlane = z;
                }
                return true;
            }
            var matrix = new double[,] { { xSq, xy, xz }, { xy, ySq, yz }, { xz, yz, zSq } };
            var rhs = new[] { xSum, ySum, zSum };
            if (matrix.solve(rhs, out var normalArray, true))
            {
                normal = (new Vector3(normalArray)).Normalize();
                distanceToPlane = normal.Dot(new Vector3(xSum / numVertices, ySum / numVertices, zSum / numVertices));
                if (distanceToPlane < 0)
                {
                    distanceToPlane = -distanceToPlane;
                    normal = -normal;
                }
                return true;
            }
            else
            {
                normal = Vector3.NaN;
                distanceToPlane = double.NaN;
                return false;
            }
        }

        internal static Enumerable<Vector2> ProjectTo2DCoordinates<TVertex>(this IList<TVertex> vertices, Vector3 planeNormal) where TVertex : IConvexVertex3D
        {
            var transform = TransformToXYPlane(direction, out backTransform);
            foreach (var v in vertices)
                yield return ConvertTo2DCoordinates(v, transform);
        }
    }
}
