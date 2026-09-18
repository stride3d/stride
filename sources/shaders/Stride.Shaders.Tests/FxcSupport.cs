// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D.Compilers;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Compiles HLSL with fxc (d3dcompiler_47), what the Direct3D effect compiler runs on the shaders
/// SPIRV-Cross translates.
/// </summary>
internal static unsafe class FxcSupport
{
    public static void SkipUnlessAvailable()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("fxc (d3dcompiler_47) is only available on Windows");
    }

    /// <summary>
    /// Compiles <paramref name="source"/> and returns fxc's diagnostics, or null when it succeeded.
    /// </summary>
    public static string? Compile(string source, string shaderModel, string entryPoint = "main")
    {
        using var compiler = D3DCompiler.GetApi();

        ComPtr<ID3D10Blob> code = default;
        ComPtr<ID3D10Blob> errors = default;
        var sourceBytes = Encoding.ASCII.GetBytes(source);

        HResult hr = compiler.Compile(
            in sourceBytes[0],
            (nuint)sourceBytes.Length,
            nameof(source),
            null,
            ref Unsafe.NullRef<ID3DInclude>(),
            entryPoint,
            shaderModel,
            0,
            0,
            ref code,
            ref errors);

        var diagnostics = errors.Handle is not null ? SilkMarshal.PtrToString((nint)errors.GetBufferPointer()) : null;

        code.Dispose();
        errors.Dispose();

        return hr.IsFailure ? diagnostics ?? $"fxc failed with 0x{hr.Value:X8}" : null;
    }
}
