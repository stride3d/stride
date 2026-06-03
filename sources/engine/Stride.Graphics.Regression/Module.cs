// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

namespace Stride.Graphics.Regression;

internal static class Module
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Crash-dialog suppression + (opt-in) SEH minidump capture. Shared with Stride.Games.AutoTesting.
        NativeCrashHandler.Install();

        // Default to software rendering unless STRIDE_TESTS_GPU=1 is set.
        // This ensures Test Explorer and dotnet test match the gold images out of the box.
        // macOS defaults to GPU (MoltenVK) since that's the real-world Apple renderer;
        // Lavapipe is still selectable on macOS by setting STRIDE_TESTS_GPU=0 explicitly.
        var gpu = Environment.GetEnvironmentVariable("STRIDE_TESTS_GPU");
        if (gpu == null && OperatingSystem.IsMacOS())
            gpu = "1";
        if (gpu != "1")
        {
            Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STRIDE_MAX_PARALLELISM")))
                Environment.SetEnvironmentVariable("STRIDE_MAX_PARALLELISM", "8");

#if STRIDE_GRAPHICS_API_VULKAN && STRIDE_PLATFORM_DESKTOP
            ConfigureLavapipe();
#endif
        }
    }

#if STRIDE_GRAPHICS_API_VULKAN && STRIDE_PLATFORM_DESKTOP
    // Isolated so the JIT only loads Stride.Dependencies.Lavapipe (whose ModuleInitializer
    // sets VK_DRIVER_FILES) when this method actually runs. Inlining it into Initialize() would
    // resolve the Lavapipe type ref at Initialize's JIT time and load the assembly on the GPU
    // path too, racing BundledMoltenVK for VK_DRIVER_FILES.
    // Desktop only: the Lavapipe package ships win/linux/osx software-Vulkan (see csproj) and
    // throws from its ModuleInitializer on mobile; Android/iOS provide Vulkan via emulator/device.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConfigureLavapipe()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES")))
            Stride.Dependencies.Lavapipe.Lavapipe.TryConfigure();
    }
#endif
}
