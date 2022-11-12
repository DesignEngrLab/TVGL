using System.Numerics;

namespace PolygonSharp
{
    internal static class Extensions
    {
        internal static float Dot(this Vector2 vec1, Vector2 vec2)
        {
            return Vector2.Dot(vec1, vec2);
        }

        internal static bool IsNull(this Vector2 vec)
        {
            if (float.IsNaN(vec.X)) return true;
            if (float.IsNaN(vec.Y)) return true;
            return false;
        }

        internal static float Distance(this Vector2 vec1, Vector2 vec2)
        { return Vector2.Distance(vec1, vec2); }
        internal static Vector2 Cross(this Vector2 vec1, Vector2 vec2)
        { return Vector2.Cross(vec1, vec2); }
    }
}