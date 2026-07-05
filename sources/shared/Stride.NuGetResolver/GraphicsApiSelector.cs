// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;

namespace Stride.Core.Assets;

/// <summary>
/// Picks the graphics API a multi-API host (GameStudio, AssetCompiler) loads at startup (one live API
/// per process). Precedence: <c>--graphics-api Vulkan</c> arg &gt; <c>STRIDE_GRAPHICS_API</c> env &gt;
/// <see cref="HostPreference"/> &gt; caller default. The <c>=</c> form is rejected (parsers don't
/// reliably catch it, so it would silently run the default).
/// </summary>
public static class GraphicsApiSelector
{
    // Canonical names matching the per-API package/bin subfolders and StrideGraphicsApi msbuild values,
    // in fallback-preference order.
    internal static readonly string[] KnownApis = ["Direct3D11", "Direct3D12", "Vulkan"];

    /// <summary>API used when nothing is selected — Windows uses Direct3D11, other platforms Vulkan.</summary>
    public static string Default => OperatingSystem.IsWindows() ? "Direct3D11" : "Vulkan";

    /// <summary>
    /// A startup error, recorded (not thrown) for GUI hosts to surface once their UI is up. While set,
    /// GraphicsApiHostResolver refuses to serve graphics, so an unchecked host fails loudly not silently.
    /// </summary>
    public static string? StartupError { get; private set; }

    /// <summary>Persisted preference a host sets before <see cref="Resolve"/> runs (after arg/env, before the default).</summary>
    public static string? HostPreference { get; set; }

    public static string? Resolve()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 1; i < args.Length; i++)
        {
            var arg = args[i];
            if (arg.StartsWith("--graphics-api=", StringComparison.OrdinalIgnoreCase))
                return FailStartup($"Use a space, not '=': --graphics-api <{string.Join("|", KnownApis)}>.");
            if (arg.Equals("--graphics-api", StringComparison.OrdinalIgnoreCase))
            {
                var value = i + 1 < args.Length ? args[i + 1] : null;
                return Normalize(value)
                    ?? FailStartup($"Invalid --graphics-api value '{value ?? "(missing)"}'. Expected one of: {string.Join(", ", KnownApis)}.");
            }
        }

        var env = Normalize(Environment.GetEnvironmentVariable("STRIDE_GRAPHICS_API"));
        return env ?? Normalize(HostPreference);
    }

    static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return KnownApis.FirstOrDefault(a => a.Equals(value.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Console hosts print to stderr and exit; GUI hosts record it in <see cref="StartupError"/>. Returns null.</summary>
    internal static string? FailStartup(string message)
    {
#if STRIDE_NUGET_RESOLVER_UI
        StartupError = message;
#else
        Console.Error.WriteLine(message);
        Environment.Exit(1);
#endif
        return null;
    }
}
