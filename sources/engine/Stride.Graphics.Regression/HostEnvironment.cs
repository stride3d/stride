// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
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

    /// <summary>Vector instruction sets the CPU supports, as seen by the .NET runtime ("V512" when it accelerates 512-bit vectors).</summary>
    public static string CpuFeatures => cpuFeatures.Value;
    private static readonly Lazy<string> cpuFeatures = new(ResolveCpuFeatures);

    private static string ResolveCpuFeatures()
    {
        var features = new List<string>();
        void Add(bool supported, string name) { if (supported) features.Add(name); }
        Add(System.Runtime.Intrinsics.X86.Avx2.IsSupported, "AVX2");
        Add(System.Runtime.Intrinsics.X86.Fma.IsSupported, "FMA");
        Add(System.Runtime.Intrinsics.X86.AvxVnni.IsSupported, "AVX-VNNI");
        Add(System.Runtime.Intrinsics.X86.Avx512F.IsSupported, "AVX512F");
        Add(System.Runtime.Intrinsics.X86.Avx512F.VL.IsSupported, "AVX512VL");
        Add(System.Runtime.Intrinsics.X86.Avx512DQ.IsSupported, "AVX512DQ");
        Add(System.Runtime.Intrinsics.X86.Avx512BW.IsSupported, "AVX512BW");
        Add(System.Runtime.Intrinsics.X86.Avx512Vbmi.IsSupported, "AVX512VBMI");
        Add(System.Runtime.Intrinsics.X86.Avx10v1.IsSupported, "AVX10.1");
        Add(System.Runtime.Intrinsics.Arm.AdvSimd.IsSupported, "NEON");
        Add(System.Runtime.Intrinsics.Vector512.IsHardwareAccelerated, "V512");
        return string.Join(' ', features);
    }

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
