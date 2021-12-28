// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Stride
{
    public static class AndroidDeviceEnumerator
    {
        private static string adbPath;

        private static string LocateAdb()
        {
            // First, look for adb.exe process (if daemon is started)
            var processes = Process.GetProcessesByName("adb");
            foreach (var process in processes)
            {
                try
                {
                    return process.MainModule.FileName;
                }
                catch (Exception)
                {
                    // Mute errors
                }
            }

            // Second, look in registry
            try
            {
                var localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                using (var androidSdkToolsKey = localMachine32.OpenSubKey(@"Software\Android SDK Tools\", false))
                {
                    if (androidSdkToolsKey != null)
                    {
                        var path = androidSdkToolsKey.GetValue("Path") as string;
                        if (path != null)
                        {
                            path = Path.Combine(path, @"platform-tools\adb.exe");
                            if (File.Exists(path))
                                return path;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Mute errors (permission, security)
            }

            // Otherwise, check if it is available on the PATH
            if (ProcessHelper.FindExecutableOnPath("adb"))
                return "adb";

            // Nothing found
            return null;
        }

        public static string GetAdbPath()
        {
            return adbPath ?? (adbPath = LocateAdb());
        }

        /// <summary>
        /// Lists all the Android devices accessible from the computer.
        /// </summary>
        /// <returns>The list of all the available Android devices.</returns>
        public static AndroidDeviceDescription[] ListAndroidDevices()
        {
            var devices = new List<AndroidDeviceDescription>();

            ProcessOutputs devicesOutputs;

            var adbPath = GetAdbPath();
            if (adbPath == null)
                return new AndroidDeviceDescription[0];

            try
            {
                devicesOutputs = ShellHelper.RunProcessAndGetOutput(adbPath, @"devices");
                if (devicesOutputs.ExitCode != 0)
                    return new AndroidDeviceDescription[0];
            }
            catch (Exception)
            {
                return new AndroidDeviceDescription[0];
            }

            var whitespace = new[] { ' ', '\t' };
            for (var i = 1; i < devicesOutputs.OutputLines.Count; ++i) // from the second line
            {
                var line = devicesOutputs.OutputLines[i];
                if (line != null)
                {
                    var res = line.Split(whitespace);
                    if (res.Length == 2 && res[1] == "device")
                    {
                        AndroidDeviceDescription device;
                        device.Serial = res[0];
                        device.Name = res[1];
                        devices.Add(device);
                    }
                }
            }

            // Set the real name of the Android device.
            for (var i = 0; i < devices.Count; ++i)
            {
                var device = devices[i];
                //TODO: doing a grep instead will be better
                var deviceNameOutputs = ShellHelper.RunProcessAndGetOutput(adbPath, string.Format(@"-s {0} shell cat /system/build.prop", device.Serial));
                foreach (var line in deviceNameOutputs.OutputLines)
                {
                    if (line != null && line.StartsWith(@"ro.product.model")) // correct line
                    {
                        var parts = line.Split('=');

                        if (parts.Length > 1)
                        {
                            device.Name = parts[1];
                            devices[i] = device;
                        }

                        break; // no need to search further
                    }
                }
            }

            return devices.ToArray();
        }

        public struct AndroidDeviceDescription
        {
            public string Serial;
            public string Name;
        }

        static class ProcessHelper
        {
            public static bool FindExecutableOnPath(string executablePath)
            {
                try
                {
                    PROCESS_INFORMATION pInfo;
                    var lpStartupInfo = new STARTUPINFOEX();
                    lpStartupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFO>();
                    bool result = CreateProcessW(null, executablePath, IntPtr.Zero, IntPtr.Zero, false, ProcessCreationFlags.CREATE_SUSPENDED, IntPtr.Zero, null, ref lpStartupInfo, out pInfo);
                    if (result)
                    {
                        var process = Process.GetProcessById(pInfo.dwProcessId);
                        process.Kill();
                        return true;
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                return false;
            }

            [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CreateProcessW(
                string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes,
                IntPtr lpThreadAttributes, bool bInheritHandles, ProcessCreationFlags dwCreationFlags,
                IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo,
                out PROCESS_INFORMATION lpProcessInformation);

            [Flags]
            public enum ProcessCreationFlags : uint
            {
                ZERO_FLAG = 0x00000000,
                CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
                CREATE_DEFAULT_ERROR_MODE = 0x04000000,
                CREATE_NEW_CONSOLE = 0x00000010,
                CREATE_NEW_PROCESS_GROUP = 0x00000200,
                CREATE_NO_WINDOW = 0x08000000,
                CREATE_PROTECTED_PROCESS = 0x00040000,
                CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
                CREATE_SEPARATE_WOW_VDM = 0x00001000,
                CREATE_SHARED_WOW_VDM = 0x00001000,
                CREATE_SUSPENDED = 0x00000004,
                CREATE_UNICODE_ENVIRONMENT = 0x00000400,
                DEBUG_ONLY_THIS_PROCESS = 0x00000002,
                DEBUG_PROCESS = 0x00000001,
                DETACHED_PROCESS = 0x00000008,
                EXTENDED_STARTUPINFO_PRESENT = 0x00080000,
                INHERIT_PARENT_AFFINITY = 0x00010000
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct PROCESS_INFORMATION
            {
                public IntPtr hProcess;
                public IntPtr hThread;
                public int dwProcessId;
                public int dwThreadId;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct STARTUPINFOEX
            {
                public STARTUPINFO StartupInfo;
                public IntPtr lpAttributeList;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
            private struct STARTUPINFO
            {
                public Int32 cb;
                public string lpReserved;
                public string lpDesktop;
                public string lpTitle;
                public Int32 dwX;
                public Int32 dwY;
                public Int32 dwXSize;
                public Int32 dwYSize;
                public Int32 dwXCountChars;
                public Int32 dwYCountChars;
                public Int32 dwFillAttribute;
                public Int32 dwFlags;
                public Int16 wShowWindow;
                public Int16 cbReserved2;
                public IntPtr lpReserved2;
                public IntPtr hStdInput;
                public IntPtr hStdOutput;
                public IntPtr hStdError;
            }
        }
    }
}
