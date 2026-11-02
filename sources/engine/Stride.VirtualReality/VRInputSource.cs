// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Input;

namespace Stride.VirtualReality;

/// <summary>
/// Registers the hand controllers of the current VR device with the <see cref="InputManager"/>, so that they can be
/// captured like any other input device.
/// </summary>
internal sealed class VRInputSource : InputSourceBase
{
    private readonly VRDeviceSystem system;
    private TouchController leftHand;
    private TouchController rightHand;

    public VRInputSource(VRDeviceSystem system)
    {
        this.system = system;
    }

    public override void Initialize(InputManager inputManager)
    {
    }

    /// <summary>
    /// Follows the hands of the current device, which can appear, change or go away while the game runs.
    /// </summary>
    public override void Update()
    {
        Follow(ref leftHand, system.Device?.LeftHand);
        Follow(ref rightHand, system.Device?.RightHand);
    }

    public override void Dispose()
    {
        Follow(ref leftHand, null);
        Follow(ref rightHand, null);
        base.Dispose();
    }

    private void Follow(ref TouchController registered, TouchController current)
    {
        if (ReferenceEquals(registered, current))
            return;

        if (registered != null)
        {
            UnregisterDevice(registered);
            registered.Source = null;
        }

        registered = current;

        if (registered != null)
        {
            registered.Source = this;
            RegisterDevice(registered);
        }
    }
}
