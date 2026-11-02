// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input;

/// <summary>
/// The capture state of one input device: which owner holds the device or its pointers, and whether
/// reads of the device by scripts and systems are masked.
/// </summary>
/// <remarks>
/// Only <see cref="InputManager"/> changes this state, through its capture methods.
/// </remarks>
public sealed class DeviceCaptureState
{
    private object[] pointerOwners = Array.Empty<object>();
    private int[] pointerPriorities = Array.Empty<int>();
    private bool maskingEnabled = true;

    public DeviceCaptureState() { }

    /// <summary>
    /// Gets the owner that holds the whole device, or <c>null</c> when the device is not captured.
    /// </summary>
    public object Owner { get; private set; }

    /// <summary>
    /// Gets the priority the current owner captured the device with.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    /// Gets a value indicating whether an owner holds the whole device.
    /// </summary>
    public bool IsCaptured => Owner != null;

    /// <summary>
    /// Gets a value indicating whether reads of the device by scripts and systems report no input.
    /// </summary>
    public bool IsMasked => maskingEnabled && Owner != null;

    internal int PointerSlotCount => pointerOwners.Length;

    /// <summary>
    /// Set by <see cref="TrackButtons{T}"/>: the device's own state already reflects masking, including capture edges.
    /// The shared query methods then read that state as it is instead of hiding it.
    /// </summary>
    internal bool DeviceMasksOwnState { get; set; }

    /// <summary>
    /// Gets a value indicating whether the shared query methods must hide the device's state themselves.
    /// </summary>
    internal bool QueriesMustMask => IsMasked && !DeviceMasksOwnState;

    /// <summary>
    /// Set by <see cref="InputManager"/> while capture changes happen before scripts and systems have read the current frame,
    /// during its capture phase. Input pressed in that frame was never seen, so it gets no release edge.
    /// </summary>
    internal bool ChangingBeforeGameReads { get; set; }

    internal event Action<bool> MaskChanged;

    /// <summary>
    /// Raised by <see cref="InputManager"/> right after the device has updated for a new frame.
    /// </summary>
    internal event Action DeviceUpdated;

    internal void RaiseDeviceUpdated() => DeviceUpdated?.Invoke();

    /// <summary>
    /// Gets the owner that holds a pointer of this device, or <c>null</c> when the pointer is not captured.
    /// </summary>
    /// <param name="pointerId">The pointer identifier.</param>
    public object GetPointerOwner(int pointerId) => (uint)pointerId < (uint)pointerOwners.Length ? pointerOwners[pointerId] : null;

    /// <summary>
    /// Gets a value indicating whether reads of a pointer by scripts and systems report no input, because the pointer
    /// or the whole device is captured.
    /// </summary>
    /// <param name="pointerId">The pointer identifier.</param>
    public bool IsPointerMasked(int pointerId) => IsMasked || (maskingEnabled && GetPointerOwner(pointerId) != null);

    /// <summary>
    /// Creates a tracker that presents a device's down, pressed and released sets with this capture state applied.
    /// </summary>
    /// <typeparam name="T">The key or button type.</typeparam>
    /// <param name="down">The input that is down.</param>
    /// <param name="pressed">The input pressed this frame.</param>
    /// <param name="released">The input released this frame.</param>
    /// <param name="newPresses">
    /// The input newly pressed this frame, excluding repeats. Defaults to <paramref name="pressed"/> for devices
    /// that do not repeat.
    /// </param>
    /// <returns>The tracker, which follows this capture state for the lifetime of the device.</returns>
    /// <remarks>
    /// The device must expose the tracker's <see cref="ButtonCaptureTracker{T}.Down"/>, <see cref="ButtonCaptureTracker{T}.Pressed"/>
    /// and <see cref="ButtonCaptureTracker{T}.Released"/> sets as its own. The shared query methods, such as
    /// <c>IsKeyDown</c>, then read the device's sets as they are, so a device that exposes its raw sets instead would
    /// show captured input.
    /// </remarks>
    public ButtonCaptureTracker<T> TrackButtons<T>(Core.Collections.IReadOnlySet<T> down, Core.Collections.IReadOnlySet<T> pressed, Core.Collections.IReadOnlySet<T> released, Core.Collections.IReadOnlySet<T> newPresses = null)
    {
        DeviceMasksOwnState = true;
        return new(this, down, pressed, released, newPresses);
    }

    internal int GetPointerPriority(int pointerId) => (uint)pointerId < (uint)pointerPriorities.Length ? pointerPriorities[pointerId] : 0;

    internal void SetOwner(object owner, int priority)
    {
        var wasMasked = IsMasked;
        Owner = owner;
        Priority = owner != null ? priority : 0;
        RaiseIfMaskChanged(wasMasked);
    }

    internal void SetPointerOwner(int pointerId, object owner, int priority)
    {
        if (pointerId >= pointerOwners.Length)
        {
            if (owner == null)
                return;

            Array.Resize(ref pointerOwners, pointerId + 1);
            Array.Resize(ref pointerPriorities, pointerId + 1);
        }

        pointerOwners[pointerId] = owner;
        pointerPriorities[pointerId] = owner != null ? priority : 0;
    }

    internal void SetMaskingEnabled(bool enabled)
    {
        var wasMasked = IsMasked;
        maskingEnabled = enabled;
        RaiseIfMaskChanged(wasMasked);
    }

    private void RaiseIfMaskChanged(bool wasMasked)
    {
        if (wasMasked != IsMasked)
            MaskChanged?.Invoke(IsMasked);
    }
}
