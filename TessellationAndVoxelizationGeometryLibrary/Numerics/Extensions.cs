using System;
using System.Collections.Generic;
using System.Text;

namespace TVGL.Numerics
{
    public static class Extensions
    {
        #region for Vector2
        public static double Distance(this Vector2 value1, Vector2 value2)
        { return Vector2.Distance(value1, value2); }

        public static double DistanceSquared(this Vector2 value1, Vector2 value2)
        { return Vector2.DistanceSquared(value1, value2); }

        public static Vector2 Normalize(this Vector2 value)
        { return Vector2.Normalize(value); }

        public static Vector2 Reflect(this Vector2 vector, Vector2 normal)
        { return Vector2.Reflect(vector, normal); }

        public static Vector2 Clamp(this Vector2 value1, Vector2 min, Vector2 max)
        { return Vector2.Clamp(value1, min, max); }

        public static Vector2 Lerp(this Vector2 value1, Vector2 value2, double amount)
        { return Vector2.Lerp(value1, value2, amount); }

        public static Vector2 Transform(this Vector2 position, Matrix3x3 matrix)
        { return Vector2.Transform(position, matrix); }

        public static Vector2 Transform(this Vector2 position, Matrix4x4 matrix)
        { return Vector2.Transform(position, matrix); }

        public static Vector2 TransformNormal(this Vector2 normal, Matrix4x4 matrix)
        { return Vector2.TransformNormal(normal, matrix); }

        public static Vector2 Transform(this Vector2 value, Quaternion rotation)
        { return Vector2.Transform(value, rotation); }

        public static Vector2 Add(this Vector2 left, Vector2 right)
        { return Vector2.Add(left, right); }

        public static Vector2 Subtract(this Vector2 left, Vector2 right)
        { return Vector2.Subtract(left, right); }


        public static Vector2 Multiply(this Vector2 left, Vector2 right)
        { return Vector2.Multiply(left, right); }

        public static Vector2 Multiply(this Vector2 left, double right)
        { return Vector2.Multiply(left, right); }

        public static Vector2 Multiply(this double left, Vector2 right)
        { return Vector2.Multiply(left, right); }

        public static Vector2 Divide(this Vector2 left, Vector2 right)
        { return Vector2.Divide(left, right); }

        public static Vector2 Divide(this Vector2 left, double divisor)
        { return Vector2.Divide(left, divisor); }

        public static Vector2 Negate(this Vector2 value)
        { return Vector2.Negate(value); }


        public static double Dot(this Vector2 value1, Vector2 value2)
        { return Vector2.Dot(value1, value2); }

        public static double Cross(this Vector2 value1, Vector2 value2)
        { return Vector2.Cross(value1, value2); }


        #endregion

        public static double Distance(this Vector3 value1, Vector3 value2)
        { return Vector3.Distance(value1, value2); }

        public static double DistanceSquared(this Vector3 value1, Vector3 value2)
        { return Vector3.DistanceSquared(value1, value2); }

        public static Vector3 Normalize(this Vector3 value)
        { return Vector3.Normalize(value); }


        public static Vector3 Cross(this Vector3 vector1, Vector3 vector2)
        { return Vector3.Cross(vector1, vector2); }


        public static Vector3 Reflect(this Vector3 vector, Vector3 normal)
        { return Vector3.Reflect(vector, normal); }


        public static Vector3 Clamp(this Vector3 value1, Vector3 min, Vector3 max)
        { return Vector3.Clamp(value1, min, max); }


        public static Vector3 Lerp(this Vector3 value1, Vector3 value2, double amount)
        { return Vector3.Lerp(value1, value2, amount); }

        public static Vector3 Transform(this Vector3 position, Matrix4x4 matrix)
        { return Vector3.Transform(position, matrix); }

        public static Vector3 Transform(this Vector3 position, Matrix3x3 matrix)
        { return Vector3.Transform(position, matrix); }

        public static Vector3 TransformNormal(this Vector3 normal, Matrix4x4 matrix)
        { return Vector3.TransformNormal(normal, matrix); }

        public static Vector3 Transform(this Vector3 value, Quaternion rotation)
        { return Vector3.Transform(value, rotation); }

        public static Vector3 Add(this Vector3 left, Vector3 right)
        { return Vector3.Add(left, right); }

        public static Vector3 Subtract(this Vector3 left, Vector3 right)
        { return Vector3.Subtract(left, right); }


        public static Vector3 Multiply(this Vector3 left, Vector3 right)
        { return Vector3.Multiply(left, right); }

        public static Vector3 Multiply(this Vector3 left, double right)
        { return Vector3.Multiply(left, right); }

        public static Vector3 Multiply(this double left, Vector3 right)
        { return Vector3.Multiply(left, right); }

        public static Vector3 Divide(this Vector3 left, Vector3 right)
        { return Vector3.Divide(left, right); }

        public static Vector3 Divide(this Vector3 left, double divisor)
        { return Vector3.Divide(left, divisor); }

        public static Vector3 Negate(this Vector3 value)
        { return Vector3.Negate(value); }

        public static double Dot(this Vector3 vector1, Vector3 vector2)
        { return Vector3.Dot(vector1, vector2); }


        public static Matrix3x3 Transpose(this Matrix3x3 matrix)
        { return Matrix3x3.Transpose(matrix); }


        public static Matrix4x4 Transpose(this Matrix4x4 matrix)
        { return Matrix4x4.Transpose(matrix); }



    }
}
