// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input;

/// <summary>
///   The capture state of one input device: which owner holds the device or its pointers, and whether
///   game-facing reads of the device are masked.
/// </summary>
/// <remarks>
///   Only <see cref="InputManager"/> changes this state, through its capture methods.
/// </remarks>
public sealed class DeviceCaptureState
{
    private object[] pointerOwners = Array.Empty<object>();
    private int[] pointerPriorities = Array.Empty<int>();
    private bool maskingEnabled = true;

    internal DeviceCaptureState() { }

    /// <summary>
    ///   Gets the owner that holds the whole device, or <c>null</c> when the device is not captured.
    /// </summary>
    public object Owner { get; private set; }

    /// <summary>
    ///   Gets the priority the current owner captured the device with.
    /// </summary>
    public int Priority { get; private set; }

    /// <summary>
    ///   Gets a value indicating whether an owner holds the whole device.
    /// </summary>
    public bool IsCaptured => Owner != null;

    /// <summary>
    ///   Gets a value indicating whether game-facing reads of the device report no input.
    /// </summary>
    public bool IsMasked => maskingEnabled && Owner != null;

    internal int PointerSlotCount => pointerOwners.Length;

    internal event Action<bool> MaskChanged;

    /// <summary>
    ///   Gets the owner that holds a pointer of this device, or <c>null</c> when the pointer is not captured.
    /// </summary>
    /// <param name="pointerId">The pointer identifier.</param>
    public object GetPointerOwner(int pointerId) => (uint)pointerId < (uint)pointerOwners.Length ? pointerOwners[pointerId] : null;

    /// <summary>
    ///   Gets a value indicating whether game-facing reads of a pointer report no input, because the pointer
    ///   or the whole device is captured.
    /// </summary>
    /// <param name="pointerId">The pointer identifier.</param>
    public bool IsPointerMasked(int pointerId) => IsMasked || (maskingEnabled && GetPointerOwner(pointerId) != null);

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
