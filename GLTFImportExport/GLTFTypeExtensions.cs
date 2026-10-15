using System.Numerics;

namespace TVGL.GLTFImportExport
{
    internal static class GLTFTypeExtensions
    {
        public static System.Numerics.Matrix4x4 ToNumerics(this TVGL.Matrix4x4 m)
        {
            return new System.Numerics.Matrix4x4(
                (float)m.M11, (float)m.M12, (float)m.M13, (float)m.M14,
                (float)m.M21, (float)m.M22, (float)m.M23, (float)m.M24,
                (float)m.M31, (float)m.M32, (float)m.M33, (float)m.M34,
                (float)m.M41, (float)m.M42, (float)m.M43, (float)m.M44);
        }

        public static TVGL.Matrix4x4 ToTVGL(this System.Numerics.Matrix4x4 m)
        {
            return new TVGL.Matrix4x4(
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44);
        }

        public static System.Numerics.Vector3 ToNumerics(this TVGL.Vector3 v)
        {
            return new System.Numerics.Vector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        public static TVGL.Vector3 ToTVGL(this System.Numerics.Vector3 v)
        {
            return new TVGL.Vector3(v.X, v.Y, v.Z);
        }

        public static System.Numerics.Vector4 ToNumericsVector4(this TVGL.Color c)
        {
            return new System.Numerics.Vector4(c.Rf, c.Gf, c.Bf, c.Af);
        }

        public static TVGL.Color ToTVGLColor(this System.Numerics.Vector4 v)
        {
            return new TVGL.Color(v.X, v.Y, v.Z, v.W);
        }

        public static TVGL.Color ToTVGLColor(this System.Numerics.Vector3 v)
        {
            return new TVGL.Color(v.X, v.Y, v.Z, 1f);
        }
    }
}

