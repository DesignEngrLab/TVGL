using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace TVGL.STEPImportExport
{
    internal enum StepPrimitiveType : int
    {
        Plane = 0,
        Cylinder = 1,
        Cone = 2,
        Sphere = 3,
        Torus = 4,
        Unknown = 5
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal unsafe struct FacePrimitiveData
    {
        public int Type;
        public int TriangleStartIndex;
        public int TriangleCount;
        public int IsPositive;

        public double OriginX;
        public double OriginY;
        public double OriginZ;

        public double AxisX;
        public double AxisY;
        public double AxisZ;

        public double Radius;
        public double MinorRadius;
        public double Aperture;

        public byte ColorR;
        public byte ColorG;
        public byte ColorB;
        public byte ColorA;

        public fixed byte SurfaceTypeNameRaw[32];

        public string SurfaceTypeName
        {
            get
            {
                fixed (byte* p = SurfaceTypeNameRaw)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty;
                }
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 8)]
    internal unsafe struct BodyMetadata
    {
        public int VertexCount;
        public int TriangleCount;
        public int FaceCount;
        public int IsSheetBody;

        public fixed byte NameRaw[64];

        public string Name
        {
            get
            {
                fixed (byte* p = NameRaw)
                {
                    return Marshal.PtrToStringAnsi((IntPtr)p) ?? string.Empty;
                }
            }
        }
    }

    internal static class NativeMethods
    {
        public const string LibName = "tvgl_step_bridge";

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        static NativeMethods()
        {
            NativeLibrary.SetDllImportResolver(typeof(NativeMethods).Assembly, DllImportResolver);
        }

        private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName != LibName && !libraryName.EndsWith(LibName))
                return IntPtr.Zero;

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var asmDir = Path.GetDirectoryName(assembly.Location) ?? baseDir;

            // Candidate directories containing tvgl_step_bridge.dll and OCCT runtime DLLs
            string[] searchDirs =
            {
                Path.Combine(baseDir, "runtimes", "win-x64", "native"),
                Path.Combine(asmDir, "runtimes", "win-x64", "native"),
                baseDir,
                asmDir
            };

            foreach (var dir in searchDirs)
            {
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    continue;

                var candidate = Path.Combine(dir, $"{LibName}.dll");
                if (File.Exists(candidate))
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        SetDllDirectory(dir);
                    }
                    if (NativeLibrary.TryLoad(candidate, out var handle))
                        return handle;
                }
            }

            return IntPtr.Zero;
        }

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr StepModel_Load(
            string filePath,
            double linearDeflection,
            double angularDeflection
        );

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int StepModel_GetBodyCount(IntPtr handle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool StepModel_GetBodyMetadata(
            IntPtr handle,
            int bodyIndex,
            out BodyMetadata outMetadata
        );

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool StepModel_CopyBodyData(
            IntPtr handle,
            int bodyIndex,
            [Out] double[] outVertices,
            [Out] int[] outTriangles,
            [Out] FacePrimitiveData[] outFaces
        );

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void StepModel_Free(IntPtr handle);

        [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool StepModel_CreateTestStepFile(string filePath);
    }
}
