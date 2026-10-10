// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input;

/// <summary>
///   Describes a change of the owner that holds a device, or one pointer of a device.
/// </summary>
public sealed class DeviceCaptureChangedEventArgs : EventArgs
{
    public DeviceCaptureChangedEventArgs(IInputDevice device, int? pointerId, object previousOwner, object newOwner)
    {
        Device = device;
        PointerId = pointerId;
        PreviousOwner = previousOwner;
        NewOwner = newOwner;
    }

    /// <summary>
    ///   Gets the device whose capture changed.
    /// </summary>
    public IInputDevice Device { get; }

    /// <summary>
    ///   Gets the pointer whose capture changed, or <c>null</c> when the whole device changed.
    /// </summary>
    public int? PointerId { get; }

    /// <summary>
    ///   Gets the owner that held the device or pointer before, or <c>null</c>.
    /// </summary>
    public object PreviousOwner { get; }

    /// <summary>
    ///   Gets the owner that holds the device or pointer now, or <c>null</c> when it was released.
    /// </summary>
    public object NewOwner { get; }
}
