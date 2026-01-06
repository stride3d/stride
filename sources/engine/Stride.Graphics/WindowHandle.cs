// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Games;

namespace Stride.Graphics;

/// <summary>
///   Encapsulates a platform-specific window handle.
/// </summary>
/// <param name="context">The context type, indicating the platform and UI framework.</param>
/// <param name="nativeWindow">
///   The native window instance (e.g. a Windows Forms' <c>Form</c>, a SDL's <c>SDLWindow</c>, etc.).
/// </param>
/// <param name="handle">The associated handle of <paramref name="nativeWindow"/>.</param>
public class WindowHandle(AppContextType context, object nativeWindow, IntPtr handle)
{
    /// <summary>
    ///   The context type, indicating the platform and graphics API.
    /// </summary>
    public readonly AppContextType Context = context;

    /// <summary>
    ///   Gets the native window as an opaque <see cref="object"/>.
    /// </summary>
    public object NativeWindow { get; } = nativeWindow;

    /// <summary>
    ///   Gets the associated platform-specific handle of <seealso cref="NativeWindow"/>.
    /// </summary>
    public IntPtr Handle { get; } = handle;
}
