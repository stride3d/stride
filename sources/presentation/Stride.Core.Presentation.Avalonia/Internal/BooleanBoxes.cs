// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Presentation.Avalonia.Internal;

internal static class BooleanBoxes
{
    /// <summary>
    /// An object representing the value <c>false</c>.
    /// </summary>
    internal static readonly object FalseBox = false;
    /// <summary>
    /// An object representing the value <c>true</c>.
    /// </summary>
    internal static readonly object TrueBox = true;

    /// <summary>
    /// Returns an object representing the provided <see cref="bool"/> <paramref name="value"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>A boxed <see cref="bool"/> equivalent to the provided <paramref name="value"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object Box(this bool value)
    {
        return value ? TrueBox : FalseBox;
    }
}
