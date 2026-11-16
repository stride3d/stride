// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace Stride
{
    /// <summary>
    /// Finds <c>adb</c>, the Android Debug Bridge, and lists the Android devices it sees.
    /// </summary>
    internal static class AndroidDeviceEnumerator
    {
        private static string? adbPath;

        private static string? LocateAdb()
        {
            // First, look for a running adb (if the daemon is started)
            var processes = Process.GetProcessesByName("adb");
            foreach (var process in processes)
            {
                try
                {
                    var fileName = process.MainModule?.FileName;
                    if (fileName != null)
                        return fileName;
                }
                catch (Exception)
                {
                    // Mute errors
                }
            }

            // Second, look in the registry, where the Android SDK installer of Windows writes its path
            if (OperatingSystem.IsWindows())
            {
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
                                path = Path.Combine(path, "platform-tools", "adb.exe");
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
            }

            // Then on the PATH
            var pathAdb = FindOnPath();
            if (pathAdb != null)
                return pathAdb;

            // Last, in the Android SDK that the environment names, as the Android build tools do
            foreach (var variable in new[] { "ANDROID_HOME", "ANDROID_SDK_ROOT" })
            {
                var sdk = Environment.GetEnvironmentVariable(variable);
                if (string.IsNullOrEmpty(sdk))
                    continue;
                var path = Path.Combine(sdk, "platform-tools", AdbFileName);
                if (File.Exists(path))
                    return path;
            }

            // Nothing found
            return null;
        }

        private static string AdbFileName => OperatingSystem.IsWindows() ? "adb.exe" : "adb";

        private static string? FindOnPath()
        {
            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
                return null;

            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = Path.Combine(directory.Trim('"'), AdbFileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        public static string? GetAdbPath()
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
                var deviceNameOutputs = ShellHelper.RunProcessAndGetOutput(adbPath, $@"-s {device.Serial} shell cat /system/build.prop");
                foreach (var line in deviceNameOutputs.OutputLines)
                {
                    if (line != null && line.StartsWith(@"ro.product.model", StringComparison.Ordinal)) // correct line
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
    }
}
