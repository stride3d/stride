// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1300 // Element should begin with upper-case letter

namespace Stride.Core;

/// <summary>
///   Defines the platform Stride is running on.
/// </summary>
/// <remarks>
///   The platform can define <strong>the target operating system</strong> or <strong>environment</strong>
///   where the application is running.
/// </remarks>
#if STRIDE_ASSEMBLY_PROCESSOR
// To avoid a CS1503 error when compiling projects that are using both the AssemblyProcessor
// and Stride.Core.
internal enum PlatformType
#else
[DataContract("PlatformType")]
public enum PlatformType
#endif
{
    // ***************************************************************
    // NOTE: This file is shared with the AssemblyProcessor.
    // If this file is modified, the AssemblyProcessor has to be
    // recompiled separately. See build\Stride-AssemblyProcessor.sln
    // ***************************************************************

    /// <summary>
    /// This is shared across platforms
    /// </summary>
    Shared,

    /// <summary>
    ///   The Windows operating system for desktop applications.
    /// </summary>
    Windows,

    /// <summary>
    ///   The Android operating system for mobile devices and tablets.
    /// </summary>
    Android,

    /// <summary>
    ///   The iOS operating system for Apple mobile devices such as iPhone and iPad.
    /// </summary>
    iOS,

    /// <summary>
    ///   The Universal Windows Platform (UWP) for applications that run on Windows 10 and later devices, and XBox gaming consoles.
    /// </summary>
    UWP,

    /// <summary>
    ///   The Linux operating system, typically used for servers and desktops.
    /// </summary>
    Linux,

    /// <summary>
    ///   The macOS operating system for Apple desktop and laptop computers.
    /// </summary>
    macOS
}
