// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Stride.ExecServer
{
    /// <summary>
    /// Helper class to launch a detached process.
    /// </summary>
    internal static class ProcessHelper
    {
        /// <summary>
        /// Helper method to launch a completely detached process. (.NET Process object is not working well in our case)
        /// </summary>
        /// <param name="executablePath">Path of the executable to launch</param>
        /// <param name="arguments">Arguments of the executable</param>
        /// <param name="processId">The process id returned if launch was successfull</param>
        public static bool LaunchProcess(string executablePath, string arguments, out IntPtr processHandle, out int processId)
        {
            //var startInfo = new ProcessStartInfo
            //{
            //    FileName = executablePath,
            //    Arguments = arguments,
            //    WorkingDirectory = Path.GetDirectoryName(executablePath),
            //    CreateNoWindow = false,
            //    UseShellExecute = false,
            //};

            //var process = new Process { StartInfo = startInfo };
            //return process.Start();

            PROCESS_INFORMATION pInfo;
            var lpStartupInfo = new STARTUPINFOEX { StartupInfo = { dwFlags = 0x01 } }; // Flags to enable no new window for child-process
            lpStartupInfo.StartupInfo.cb = Marshal.SizeOf<STARTUPINFO>();
            //lpStartupInfo.StartupInfo.wShowWindow
            var pSec = new SECURITY_ATTRIBUTES();
            var tSec = new SECURITY_ATTRIBUTES();
            pSec.nLength = Marshal.SizeOf(pSec);
            tSec.nLength = Marshal.SizeOf(tSec);
            var result =  CreateProcessW(executablePath, "\"" + executablePath + "\" " + arguments, ref pSec, ref tSec, false, CREATE_DEFAULT_ERROR_MODE | CREATE_NO_WINDOW | DETACHED_PROCESS, IntPtr.Zero, Path.GetDirectoryName(executablePath), ref lpStartupInfo, out pInfo);

            processHandle = IntPtr.Zero;
            processId = 0;
            if (result)
            {
                processHandle = pInfo.hProcess;
                processId = pInfo.dwProcessId;
            }
            return result;
        }

        [DllImport("kernel32.dll", EntryPoint = "CreateProcessW", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessW(
            string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags,
            IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        // ReSharper disable InconsistentNaming
        // ReSharper disable FieldCanBeMadeReadOnly.Local
        // ReSharper disable MemberCanBePrivate.Local

        const uint CREATE_DEFAULT_ERROR_MODE = 0x04000000;
        const uint CREATE_NO_WINDOW = 0x08000000;
        const uint DETACHED_PROCESS = 0x00000008;
        const uint CREATE_NEW_CONSOLE = 0x00000010;

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
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

        // ReSharper restore MemberCanBePrivate.Local
        // ReSharper restore FieldCanBeMadeReadOnly.Local
        // ReSharper restore InconsistentNaming
    }
}
