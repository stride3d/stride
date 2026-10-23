// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Stride.Core.Threading;

/// <summary>
/// GC safe point for busy-wait loops under Wine: .NET 9+ suspends threads for GC via special user APCs,
/// which Wine doesn't deliver to a spinning thread. A regular P/Invoke lets the GC proceed without yielding.
/// <see cref="IsWine"/> is static readonly, so tiered JIT removes the check on other platforms.
/// </summary>
internal static partial class WineGCSafePoint
{
    public static readonly bool IsWine = DetectWine();

    public static void Poll() => GetCurrentThreadId();

    private static bool DetectWine()
    {
        return OperatingSystem.IsWindows()
            && NativeLibrary.TryLoad("ntdll.dll", out var ntdll)
            && NativeLibrary.TryGetExport(ntdll, "wine_get_version", out _);
    }

    // Must not be [SuppressGCTransition]: the GC transition is what makes this a safe point
    [LibraryImport("kernel32.dll")]
    private static partial uint GetCurrentThreadId();
}
