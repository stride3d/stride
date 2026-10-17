// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Input;

/// <summary>
///   Holds the capture state of devices that do not provide their own, such as third-party devices that
///   implement the device interfaces directly.
/// </summary>
internal static class DeviceCaptureStates
{
    private static readonly ConditionalWeakTable<IInputDevice, DeviceCaptureState> States = new();

    public static DeviceCaptureState GetOrCreate(IInputDevice device) => States.GetValue(device, static _ => new DeviceCaptureState());
}
