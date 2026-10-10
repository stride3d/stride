// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Generic;

namespace Stride.Input;

/// <summary>
///   Presents the down, pressed and released sets of a device as the game sees them while the device's
///   capture state changes.
/// </summary>
/// <remarks>
///   <para>
///     While the device is masked, nothing is down or pressed. When masking begins, everything that was already down
///     is reported as released until the next device update, so the game never sees input stuck down.
///   </para>
///   <para>
///     When masking ends, input that is still held stays hidden, and its eventual release is hidden too, until
///     it is pressed again. The game never sees input it did not see pressed.
///   </para>
/// </remarks>
/// <typeparam name="T">The key or button type.</typeparam>
internal sealed class CaptureEdgeTracker<T>
{
    private readonly DeviceCaptureState capture;
    private readonly Core.Collections.IReadOnlySet<T> rawDown;
    private readonly Core.Collections.IReadOnlySet<T> rawPressed;
    private readonly Core.Collections.IReadOnlySet<T> rawReleased;
    private readonly Core.Collections.IReadOnlySet<T> rawNewPresses;
    private readonly HashSet<T> releasedOnCapture = new();
    private readonly HashSet<T> suppressed = new();
    private readonly HashSet<T> releasedWhileSuppressed = new();
    private readonly List<T> scratch = new();

    /// <param name="capture">The capture state of the device.</param>
    /// <param name="rawDown">The input that is down.</param>
    /// <param name="rawPressed">The input pressed this frame.</param>
    /// <param name="rawReleased">The input released this frame.</param>
    /// <param name="rawNewPresses">
    ///   The input newly pressed this frame, excluding repeats. Defaults to <paramref name="rawPressed"/> for devices
    ///   that do not repeat.
    /// </param>
    public CaptureEdgeTracker(DeviceCaptureState capture, Core.Collections.IReadOnlySet<T> rawDown, Core.Collections.IReadOnlySet<T> rawPressed, Core.Collections.IReadOnlySet<T> rawReleased, Core.Collections.IReadOnlySet<T> rawNewPresses = null)
    {
        this.capture = capture;
        this.rawDown = rawDown;
        this.rawPressed = rawPressed;
        this.rawReleased = rawReleased;
        this.rawNewPresses = rawNewPresses ?? rawPressed;
        Down = new View(this, ViewKind.Down);
        Pressed = new View(this, ViewKind.Pressed);
        Released = new View(this, ViewKind.Released);
    }

    public Core.Collections.IReadOnlySet<T> Down { get; }

    public Core.Collections.IReadOnlySet<T> Pressed { get; }

    public Core.Collections.IReadOnlySet<T> Released { get; }

    /// <summary>
    ///   Gets the input that was held when masking ended and has not been pressed again since.
    /// </summary>
    public HashSet<T> Suppressed => suppressed;

    public void OnMaskChanged(bool masked)
    {
        // During the capture phase, game code has not read the frame yet, so input newly pressed in it was never seen
        var unseenNewPresses = capture.ChangingBeforeGameReads;

        releasedOnCapture.Clear();
        if (masked)
        {
            foreach (var item in rawDown)
            {
                if (!suppressed.Contains(item) && !(unseenNewPresses && rawNewPresses.Contains(item)))
                    releasedOnCapture.Add(item);
            }

            // Input released this frame keeps its release, unless the game never saw it down
            foreach (var item in rawReleased)
            {
                if (!releasedWhileSuppressed.Contains(item) && !(unseenNewPresses && rawNewPresses.Contains(item)))
                    releasedOnCapture.Add(item);
            }

            suppressed.Clear();
        }
        else
        {
            suppressed.Clear();
            foreach (var item in rawDown)
            {
                if (!(unseenNewPresses && rawNewPresses.Contains(item)))
                    suppressed.Add(item);
            }
        }
    }

    /// <summary>
    ///   Advances the tracked edges after the device has updated its raw sets for a new frame.
    /// </summary>
    public void AfterDeviceUpdate()
    {
        releasedOnCapture.Clear();
        releasedWhileSuppressed.Clear();

        scratch.Clear();
        foreach (var item in suppressed)
        {
            if (rawNewPresses.Contains(item))
            {
                scratch.Add(item);
            }
            else if (!rawDown.Contains(item))
            {
                scratch.Add(item);
                if (rawReleased.Contains(item))
                    releasedWhileSuppressed.Add(item);
            }
        }

        foreach (var item in scratch)
            suppressed.Remove(item);
    }

    private bool Contains(ViewKind kind, T item) => kind switch
    {
        ViewKind.Down => !capture.IsMasked && rawDown.Contains(item) && !suppressed.Contains(item),
        ViewKind.Pressed => !capture.IsMasked && rawPressed.Contains(item) && !suppressed.Contains(item),
        _ => capture.IsMasked ? releasedOnCapture.Contains(item) : rawReleased.Contains(item) && !releasedWhileSuppressed.Contains(item),
    };

    private int Count(ViewKind kind) => kind switch
    {
        ViewKind.Down => capture.IsMasked ? 0 : rawDown.Count - CountContained(suppressed, rawDown),
        ViewKind.Pressed => capture.IsMasked ? 0 : rawPressed.Count - CountContained(suppressed, rawPressed),
        _ => capture.IsMasked ? releasedOnCapture.Count : rawReleased.Count - CountContained(releasedWhileSuppressed, rawReleased),
    };

    private static int CountContained(HashSet<T> items, Core.Collections.IReadOnlySet<T> set)
    {
        int count = 0;
        foreach (var item in items)
        {
            if (set.Contains(item))
                count++;
        }
        return count;
    }

    private IEnumerable<T> Source(ViewKind kind) => kind switch
    {
        ViewKind.Down => rawDown,
        ViewKind.Pressed => rawPressed,
        _ => capture.IsMasked ? releasedOnCapture : rawReleased,
    };

    private enum ViewKind
    {
        Down,
        Pressed,
        Released,
    }

    private sealed class View(CaptureEdgeTracker<T> tracker, ViewKind kind) : Core.Collections.IReadOnlySet<T>
    {
        public int Count => tracker.Count(kind);

        public bool Contains(T item) => tracker.Contains(kind, item);

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in tracker.Source(kind))
            {
                if (tracker.Contains(kind, item))
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
