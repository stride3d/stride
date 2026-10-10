// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Stride.Input;

/// <summary>
///   A list of this frame's input events as the game sees them: events from masked devices or pointers are left out.
/// </summary>
/// <remarks>
///   The filtered list is rebuilt on read when the frame or any capture has changed since it was last built, so a
///   capture taken after the capture phase applies from the next read. The backing list is reused, so there is no
///   allocation per frame once it has grown.
/// </remarks>
internal sealed class MaskedEventList<T>(InputManager manager, List<T> raw) : IReadOnlyList<T> where T : InputEvent
{
    private readonly List<T> filtered = new();
    private int builtFrame = -1;
    private int builtVersion = -1;

    public int Count
    {
        get
        {
            EnsureBuilt();
            return filtered.Count;
        }
    }

    public T this[int index]
    {
        get
        {
            EnsureBuilt();
            return filtered[index];
        }
    }

    public List<T>.Enumerator GetEnumerator()
    {
        EnsureBuilt();
        return filtered.GetEnumerator();
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void EnsureBuilt()
    {
        if (builtFrame == manager.FrameIndex && builtVersion == manager.CaptureVersion)
            return;

        filtered.Clear();
        foreach (var inputEvent in raw)
        {
            if (!InputManager.IsMasked(inputEvent))
                filtered.Add(inputEvent);
        }

        builtFrame = manager.FrameIndex;
        builtVersion = manager.CaptureVersion;
    }
}
