// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders.Compilers;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Availability of the native spirv-cross library used by <see cref="SpirvTranslator"/>.
/// Stride.Dependencies.SpirvCross only ships a win-x64 binary, so SPIR-V to HLSL
/// translation checks are skipped on other platforms.
/// </summary>
internal static class SpirvCrossSupport
{
    public static bool Available { get; } = Probe();

    private static bool Probe()
    {
        try
        {
            NativeLibraryHelper.PreloadLibrary("spirv-cross", typeof(SpirvTranslator));
            return true;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
    }

    public static void SkipUnlessAvailable()
    {
        if (!Available)
            Assert.Skip("Native spirv-cross library is not available on this platform");
    }
}
