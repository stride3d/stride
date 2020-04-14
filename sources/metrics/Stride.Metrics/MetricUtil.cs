using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Stride.Metrics
{
    static class MetricUtil
    {
        public static int GetSystemMemoryInMb()
        {
            long installedMemory = 0;
            var memStatus = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(memStatus))
            {
                installedMemory = memStatus.ullTotalPhys;
            }
            return (int)(installedMemory/(1024*1024));
        }

        public static void GetCpuVendorAndModel(out string cpuVendor, out string cpuModel)
        {
            cpuVendor = string.Empty;
            cpuModel = string.Empty;
            var key = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0");
            if (key != null)
            {
                cpuModel = (key.GetValue("ProcessorNameString") ?? string.Empty).ToString();
                cpuVendor = (key.GetValue("VendorIdentifier") ?? string.Empty).ToString();
            }
        }

        public static string GetUnityVersionInstalled()
        {
            var unityVersionKey = Registry.CurrentUser.OpenSubKey(@"Software\Unity Technologies\Installer\Unity");
            if (unityVersionKey != null && unityVersionKey.GetValue("Version") != null)
            {
                return unityVersionKey.GetValue("Version").ToString();
            }
            return null;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public long ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);
    }
}