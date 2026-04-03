// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Stride.Graphics.Regression;

internal static class Module
{
    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint uMode);

    [ModuleInitializer]
    internal static void Initialize()
    {
        // Prevent Windows Error Reporting dialogs on native crashes (e.g. GPU driver issues)
        // so that CI and automated test runs don't hang on a blocking dialog.
        if (OperatingSystem.IsWindows())
            SetErrorMode(0x0001 /* SEM_FAILCRITICALERRORS */ | 0x0002 /* SEM_NOGPFAULTERRORBOX */ | 0x8000 /* SEM_NOOPENFILEERRORBOX */);

        // Auto-configure SwiftShader ICD path for Vulkan software rendering
        if (Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING") == "1"
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES")))
        {
            // Check next to the binary (NuGet package deploys here)
            var icdPath = Path.Combine(AppContext.BaseDirectory, "vk_swiftshader_icd.json");
            if (File.Exists(icdPath))
                Environment.SetEnvironmentVariable("VK_DRIVER_FILES", icdPath);
        }
    }
}
