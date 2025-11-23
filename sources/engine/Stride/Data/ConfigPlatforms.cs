// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#pragma warning disable SA1300 // Element should begin with upper-case letter

using System;

using Stride.Core;

namespace Stride.Data;

/// <summary>
///   Represents a set of platform configurations.
/// </summary>
/// <remarks>
///   This enumeration allows specifying one or more platforms by combining values using bitwise
///   operations. For example, you can use <c><see cref="Windows"/> | <see cref="Android"/></c>
///   to indicate both Windows and Android platforms.
/// </remarks>
/// <seealso cref="PlatformType"/>
[Flags]
public enum ConfigPlatforms
{
    None = 0,

    /// <summary>
    ///   The Windows operating system for desktop applications.
    /// </summary>
    Windows = 1 << PlatformType.Windows,

    /// <summary>
    ///   The Universal Windows Platform (UWP) for applications that run on Windows 10 and later devices, and XBox gaming consoles.
    /// </summary>
    UWP     = 1 << PlatformType.UWP,

    /// <summary>
    ///   The iOS operating system for Apple mobile devices such as iPhone and iPad.
    /// </summary>
    iOS     = 1 << PlatformType.iOS,

    /// <summary>
    ///   The Android operating system for mobile devices and tablets.
    /// </summary>
    Android = 1 << PlatformType.Android,

    /// <summary>
    ///   The Linux operating system, typically used for servers and desktops.
    /// </summary>
    Linux   = 1 << PlatformType.Linux,

    /// <summary>
    ///   The macOS operating system for Apple desktop and laptop computers.
    /// </summary>
    macOS   = 1 << PlatformType.macOS
}
