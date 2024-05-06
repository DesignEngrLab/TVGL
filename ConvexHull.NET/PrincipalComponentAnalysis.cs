namespace PointCloudNet;

/// <summary>
/// Principal Component Analysis (PCA) is a statistical procedure that is 
/// implemented here for 2,3, and 4 dimensions. This is partly motivated
/// by an issue in convexhull when the input data is represented by a 
/// lower dimension.
public static class PrincipalComponentAnalysis
{
    public static bool Reduce3To2Dimensions(IEnumerable<Vector3> vertices, out double distanceToPlane,
            out Vector3 normal)
    {
        var pointList = vertices as IList<Vector3> ?? vertices.ToList();
        var numVertices = pointList.Count;
        if (numVertices < 3)
        {
            distanceToPlane = double.NaN;
            normal = default;
            return false;
        }
        if (numVertices == 3)
        {
            var cross = (pointList[1] - pointList[0]).Cross(pointList[2] - pointList[1]);
            var crossLength = cross.Length();
            if (crossLength.IsNegligible())
            {
                distanceToPlane = double.NaN;
                normal = default;
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
            if (double.IsNaN(vertex.X) || double.IsNaN(vertex.Y) || double.IsNaN(vertex.Z)) continue;
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
            normal = default;
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
            normal = default;
            distanceToPlane = double.NaN;
            return false;
        }
    }

}