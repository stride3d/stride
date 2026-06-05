// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Stride.Graphics.Regression;

/// <summary>
/// One-shot host facts (OS, CPU brand) for the gold sidecar — lets CompareGold attribute
/// pixel-diff regressions to a specific machine/driver/CPU combination.
/// </summary>
internal static class HostEnvironment
{
    public static string OsDescription => RuntimeInformation.OSDescription;

    public static string CpuName => cpuName.Value;
    private static readonly Lazy<string> cpuName = new(ResolveCpuName);

    private static string ResolveCpuName()
    {
        try
        {
            if (OperatingSystem.IsMacOS())
            {
                // sysctlbyname("machdep.cpu.brand_string") returns e.g. "Apple M4".
                uint len = 256;
                var buf = new byte[(int)len];
                if (sysctlbyname("machdep.cpu.brand_string", buf, ref len, IntPtr.Zero, 0) == 0)
                    return System.Text.Encoding.ASCII.GetString(buf, 0, (int)len).TrimEnd('\0');
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsAndroid())
            {
                foreach (var line in File.ReadAllLines("/proc/cpuinfo"))
                {
                    var idx = line.IndexOf(':');
                    if (idx < 0) continue;
                    var key = line[..idx].Trim();
                    if (key == "model name" || key == "Hardware" || key == "Processor")
                        return line[(idx + 1)..].Trim();
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                // ProcessorNameString registry value is what Task Manager / dxdiag show.
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key?.GetValue("ProcessorNameString") is string name)
                    return name.Trim();
            }
        }
        catch
        {
            // Fall through to architecture-only fallback below.
        }

        return RuntimeInformation.ProcessArchitecture.ToString();
    }

    [DllImport("libc", EntryPoint = "sysctlbyname")]
    private static extern int sysctlbyname(string name, byte[] oldp, ref uint oldlenp, IntPtr newp, uint newlen);
}
