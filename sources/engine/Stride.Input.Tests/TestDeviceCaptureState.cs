// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers the capture state that every input device carries.
/// </summary>
public class TestDeviceCaptureState
{
    [Fact]
    public void ThirdPartyDeviceGetsAStableCaptureState()
    {
        IInputDevice device = new ThirdPartyKeyboard();

        Assert.Same(device.CaptureState, device.CaptureState);
        Assert.False(device.CaptureState.IsCaptured);
    }

    [Fact]
    public void MaskedOnlyWhileCapturedAndMaskingEnabled()
    {
        var state = DeviceCaptureStates.GetOrCreate(new ThirdPartyKeyboard());

        state.SetOwner(new object(), 0);
        Assert.True(state.IsMasked);

        state.SetMaskingEnabled(false);
        Assert.False(state.IsMasked);
        Assert.True(state.IsCaptured);
    }

    [Fact]
    public void PointerCaptureMasksOnlyThatPointer()
    {
        var state = DeviceCaptureStates.GetOrCreate(new ThirdPartyKeyboard());

        state.SetPointerOwner(3, new object(), 0);

        Assert.True(state.IsPointerMasked(3));
        Assert.False(state.IsPointerMasked(0));
        Assert.False(state.IsMasked);
    }
}
