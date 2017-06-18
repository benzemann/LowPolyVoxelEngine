using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PluginWrapper
{
    public static class PluginWrapper
    {
        [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "FillArray")]
        public static unsafe extern void FillArray(out int len, out float* data);

        [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "GetArray")]
        public static unsafe extern float* GetArray(out int len);

        [DllImport("LowPolyVoxelEnginePlugin", EntryPoint = "GetDebugString", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static unsafe extern IntPtr GetDebugString();

        public static string GetDebugText()
        {
            unsafe
            {

                return Marshal.PtrToStringAnsi(GetDebugString());
            }
        }

        public static int CanYouSeeMe()
        {
            return 42;
        }


        public static float[] GetArrayFromPluging()
        {
            float[] arr;
            unsafe
            {
                int size;
                float* arrPtr = GetArray(out size);
                arr = new float[size];
                for (int i = 0; i < size; i++)
                {
                    arr[i] = arrPtr[i];
                }
            }
            return arr;
        }
    }
}
