// ***********************************************************************
// Assembly         : TVGL Presenter
// Author           : Matt
using System;

namespace TVGL
{
// This is not well described but supposed to move things to your laptop GPU
    public static partial class Presenter
    {
        public static void NVEnable()
        {
            NVOptimusEnabler nvEnabler = new NVOptimusEnabler();

        }
    }

    public sealed class NVOptimusEnabler
    {
        static NVOptimusEnabler()
        {
            try
            {

                if (Environment.Is64BitProcess)
                    NativeMethods.LoadNvApi64();
                else
                    NativeMethods.LoadNvApi32();
            }
            catch { } // will always fail since 'fake' entry point doesn't exists
        }
    };

    internal static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("nvapi64.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi64();

        [System.Runtime.InteropServices.DllImport("nvapi.dll", EntryPoint = "fake")]
        internal static extern int LoadNvApi32();
    }
}
