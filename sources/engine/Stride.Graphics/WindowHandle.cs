// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Games;

namespace Stride.Graphics;

public class WindowHandle(AppContextType context, object nativeWindow, IntPtr handle)
{
    /// <summary>
    /// A platform specific window handle.
    /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowHandle"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="nativeWindow">The native window instance (Winforms, SDLWindow, ...).</param>
        /// <param name="handle">The associated handle of <paramref name="nativeWindow"/>.</param>
    public readonly AppContextType Context = context;

        /// <summary>
        /// The context.
        /// </summary>
        /// <summary>
        /// The native windows as an opaque <see cref="System.Object"/>.
        /// </summary>
    public object NativeWindow { get; } = nativeWindow;

        /// <summary>
        /// The associated platform specific handle of <seealso cref="NativeWindow"/>.
        /// </summary>
    public IntPtr Handle { get; } = handle;
}
