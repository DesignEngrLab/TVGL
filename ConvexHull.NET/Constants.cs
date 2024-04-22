namespace PointCloudNet;
internal static class Constants
{
    // for single precision
    internal const double BaseTolerance = 1e-8;
    // for double precision
    //internal const double BaseTolerance = 1e-12;

    internal static Vector2 Subtract(this Vector2 a, Vector2 b)
    {
#if CUSTOMVECTOR
        return new Vector2(a.X - b.X, + a.Y - b.Y);
#else
        return a - b;
#endif
    }
    internal static Vector3 Subtract(this Vector3 a, Vector3 b)
    {
#if CUSTOMVECTOR

        return new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
#else
        return a - b;
#endif
    }
    internal static Vector4 Subtract(this Vector4 a, Vector4 b)
    {
#if CUSTOMVECTOR
        return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
#else
        return a - b;
#endif
    }

    internal static Vector2 Normalize(this Vector2 vector)
    {
#if !CUSTOMVECTOR
        var length = Math.Sqrt(vector.Dot(vector));
        return new Vector2(vector.X / length,  vector.Y / length);
#else
        return Vector2.Normalize(vector);
#endif
    }

    internal static Vector3 Normalize(this Vector3 vector)
    {
#if CUSTOMVECTOR
        var length = Math.Sqrt(vector.Dot(vector));
        return new Vector3(vector.X / length,  vector.Y / length,  vector.Z / length);
#else
        return Vector3.Normalize(vector);
#endif
    }

    internal static Vector4 Normalize(this Vector4 vector)
    {
#if CUSTOMVECTOR
        var length = Math.Sqrt(vector.Dot(vector));
        return new Vector4(vector.X / length,  vector.Y / length,  vector.Z / length,  vector.W / length);
#else
        return Vector4.Normalize(vector);
#endif
    }


    internal static double Dot(this Vector2 a, Vector2 b)
    {
#if CUSTOMVECTOR
        return a.X * b.X + a.Y * b.Y;
#else
        return Vector2.Dot(a, b);
#endif
    }
    internal static double Dot(this Vector3 a, Vector3 b)
    {
#if CUSTOMVECTOR

        return a.X * b.X + a.Y * b.Y + a.Z * b.Z;
#else
        return Vector3.Dot(a, b);
#endif
    }
    internal static double Dot(this Vector4 a, Vector4 b)
    {
#if CUSTOMVECTOR
        return a.X * b.X + a.Y * b.Y + a.Z * b.Z + a.W * b.W;
#else
        return Vector4.Dot(a, b);
#endif
    }
    internal static double Cross(this Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }
    internal static Vector3 Cross(this Vector3 a, Vector3 b)
    {
#if CUSTOMVECTOR
        return new Vector3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
#else
        return Vector3.Cross(a, b);
#endif
    }
    internal static Vector4 Cross(this Vector4 a, Vector4 b)
    {
        return new Vector4(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X, a.W * b.W);
    }

    internal static void SwapItemsInList<T>(int i, int j, IList<T> points)
    {
        var temp = points[i];
        points[i] = points[j];
        points[j] = temp;
    }

    internal static double GetCoord(this Vector2 vector, int index)
    {
        if (index == 0)
            return vector.X;
        return vector.Y;
    }

    internal static double GetCoord(this Vector3 vector, int index)
    {
        if (index == 0)
            return vector.X;
        if (index == 1)
            return vector.Y;
        return vector.Z;
    }

    internal static double GetCoord(this Vector4 vector, int index)
    {
        if (index == 0)
            return vector.X;
        if (index == 1)
            return vector.Y;
        if (index == 2)
            return vector.Z;
        return vector.W;
    }

    internal static bool IsNegligible(this double value)
    {
        return Math.Abs(value) < BaseTolerance;
    }
    internal static bool IsNegligible(this float value)
    {
        return Math.Abs(value) < BaseTolerance;
    }
}