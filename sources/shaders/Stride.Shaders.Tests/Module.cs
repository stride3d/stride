// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Shaders.Parsers.Tests;

internal static class Module
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Default to software rendering (WARP) unless STRIDE_TESTS_GPU=1 is set, so local runs
        // are deterministic and match GPU-less CI runners out of the box.
        // Same convention as Stride.Graphics.Regression.
        if (Environment.GetEnvironmentVariable("STRIDE_TESTS_GPU") != "1"
            && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING")))
            Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");
    }
}